/*
 Copyright (C) 2008-2013  Andrea Maggiulli (a.maggiulli@gmail.com)
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
  
 This file is part of QLNet Project http://qlnet.sourceforge.net/

 QLNet is free software: you can redistribute it and/or modify it
 under the terms of the QLNet license.  You should have received a
 copy of the license along with this program; if not, license is  
 available online at <http://qlnet.sourceforge.net/License.html>.
  
 QLNet is a based on QuantLib, a free-software/open-source library
 for financial quantitative analysts and developers - http://quantlib.org/
 The QuantLib license is available online at http://quantlib.org/license.shtml.
 
 This program is distributed in the hope that it will be useful, but WITHOUT
 ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 FOR A PARTICULAR PURPOSE.  See the license for more details.
*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace QLNet {
   //! Payment schedule
   public class Schedule 
   {

      #region Properties

		private bool fullInterface_;
      private Period tenor_;
      private Calendar calendar_;
      private BusinessDayConvention convention_, terminationDateConvention_;
      private DateGeneration.Rule rule_;
      private bool endOfMonth_;
      private Date firstDate_, nextToLastDate_;
      private List<Date> dates_ = new List<Date>();
      private List<bool> isRegular_ = new List<bool>();

      #endregion

      #region Constructors

      public Schedule() { }

      public Schedule(List<Date> dates, Calendar calendar = null, BusinessDayConvention convention = BusinessDayConvention.Unadjusted)
      {
         if (calendar == null)
            calendar_ = new NullCalendar();
         else
            calendar_ = calendar;

         fullInterface_ = false;
         tenor_ = new Period();
         convention_ = convention;
         terminationDateConvention_ = convention;
         rule_ = DateGeneration.Rule.Forward;
         endOfMonth_ = false;
         dates_ = dates;
      }


      public Schedule( Date effectiveDate, Date terminationDate, Period tenor, Calendar calendar,
                       BusinessDayConvention convention, BusinessDayConvention terminationDateConvention,
                       DateGeneration.Rule rule, bool endOfMonth, Date firstDate = null, Date nextToLastDate = null)
      {
         // first save the properties
         fullInterface_ = true;
         tenor_ = tenor;
			if ( calendar == null )
				calendar_ = new NullCalendar();
			else
				calendar_ = calendar;
         convention_ = convention;
         terminationDateConvention_ = terminationDateConvention;
         rule_ = rule;
         endOfMonth_ = endOfMonth;

         if ( firstDate == effectiveDate )
            firstDate_ = null;
         else
            firstDate_ = firstDate;
  
          if ( nextToLastDate == terminationDate )
            nextToLastDate_ = null;
         else
            nextToLastDate_ = nextToLastDate;

         // sanity checks
          Utils.QL_REQUIRE( terminationDate != null, () => "null termination date" );
         
         // in many cases (e.g. non-expired bonds) the effective date is not
         // really necessary. In these cases a decent placeholder is enough
         if ( effectiveDate == null && firstDate == null && rule== DateGeneration.Rule.Backward) 
         {
            Date evalDate = Settings.evaluationDate();
            Utils.QL_REQUIRE( evalDate < terminationDate, () => "null effective date" );
            int y;
            if (nextToLastDate != null) 
            {
                y = (nextToLastDate - evalDate)/366 + 1;
                effectiveDate = nextToLastDate - new Period(y,TimeUnit.Years);
            } 
            else 
            {
                y = (terminationDate - evalDate)/366 + 1;
                effectiveDate = terminationDate - new Period(y,TimeUnit.Years);
            }
         } 
         else
            Utils.QL_REQUIRE( effectiveDate != null, () => "null effective date" );

         if (tenor_.length() == 0)
            rule_ = DateGeneration.Rule.Zero;
         else
            Utils.QL_REQUIRE( tenor.length() > 0, () => "non positive tenor (" + tenor + ") not allowed" );

         if (firstDate_ != null)
         {
            switch (rule_)
            {
               case DateGeneration.Rule.Backward:
               case DateGeneration.Rule.Forward:
                  Utils.QL_REQUIRE(firstDate_ > effectiveDate &&
                                   firstDate_ < terminationDate, () =>
                                   "first date (" + firstDate_ + ") out of effective-termination date range [" +
                                   effectiveDate + ", " + terminationDate + ")");
                  // we should ensure that the above condition is still verified after adjustment
                  break;
               case DateGeneration.Rule.ThirdWednesday:
                  Utils.QL_REQUIRE( IMM.isIMMdate( firstDate_, false ), () => "first date (" + firstDate_ + ") is not an IMM date" );
                  break;
               case DateGeneration.Rule.Zero:
               case DateGeneration.Rule.Twentieth:
               case DateGeneration.Rule.TwentiethIMM:
               case DateGeneration.Rule.OldCDS:
               case DateGeneration.Rule.CDS:
                  Utils.QL_FAIL("first date incompatible with " + rule_ + " date generation rule");
                  break;
               default:
                  Utils.QL_FAIL("unknown rule (" + rule_ + ")");
                  break;
            }
         }

         if (nextToLastDate_ != null)
         {
            switch (rule_)
            {
               case DateGeneration.Rule.Backward:
               case DateGeneration.Rule.Forward:
                  Utils.QL_REQUIRE( nextToLastDate_ > effectiveDate && nextToLastDate_ < terminationDate, () =>
                                   "next to last date (" + nextToLastDate_ + ") out of effective-termination date range (" +
                                   effectiveDate + ", " + terminationDate + "]");
                  // we should ensure that the above condition is still verified after adjustment
                  break;
               case DateGeneration.Rule.ThirdWednesday:
                  Utils.QL_REQUIRE( IMM.isIMMdate( nextToLastDate_, false ), () => "next-to-last date (" + nextToLastDate_ +
                                    ") is not an IMM date");                  
                  break;
               case DateGeneration.Rule.Zero:
               case DateGeneration.Rule.Twentieth:
               case DateGeneration.Rule.TwentiethIMM:
               case DateGeneration.Rule.OldCDS:
               case DateGeneration.Rule.CDS:
                  Utils.QL_FAIL("next to last date incompatible with " + rule_ + " date generation rule");
                  break;
               default:
                  Utils.QL_FAIL("unknown rule (" + rule_ + ")");
                  break;
            }
         }

         // calendar needed for endOfMonth adjustment
         Calendar nullCalendar = new NullCalendar();
         int periods = 1;
         Date seed = new Date() , exitDate;
         switch (rule_)
         {
            case DateGeneration.Rule.Zero:
               tenor_ = new Period(0, TimeUnit.Years);
               dates_.Add(effectiveDate);
               dates_.Add(terminationDate);
               isRegular_.Add(true);
               break;

            case DateGeneration.Rule.Backward:
               dates_.Add(terminationDate);

               seed = terminationDate;
               if (nextToLastDate_ != null)
               {
                  dates_.Insert(0, nextToLastDate_);
                  Date temp = nullCalendar.advance(seed, -periods * tenor_, convention_, endOfMonth_);
                  if (temp != nextToLastDate_)
                     isRegular_.Insert(0, false);
                  else
                     isRegular_.Insert(0, true);
                  seed = nextToLastDate_;
               }
               exitDate = effectiveDate;
               if (firstDate_ != null)
                  exitDate = firstDate_;

               while (true)
               {
                  Date temp = nullCalendar.advance(seed, -periods * tenor_, convention_, endOfMonth_);
                  if (temp < exitDate)
                  {
                     if (firstDate_ != null && (calendar_.adjust(dates_.First(), convention_) !=
                          calendar_.adjust(firstDate_, convention_)))
                     {
                        dates_.Insert(0, firstDate_);
                        isRegular_.Insert(0, false);
                     }
                     break;
                  }
                  else
                  {
                     // skip dates that would result in duplicates
                     // after adjustment
                     if (calendar_.adjust(dates_.First(), convention_) != calendar_.adjust(temp, convention_))
                     {
                        dates_.Insert(0, temp);
                        isRegular_.Insert(0, true);
                     }
                     ++periods;
                  }
               }

               if (calendar_.adjust(dates_.First(), convention) != calendar_.adjust(effectiveDate, convention))
               {
                  dates_.Insert(0, effectiveDate);
                  isRegular_.Insert(0, false);
               }

               break;

            case DateGeneration.Rule.Twentieth:
            case DateGeneration.Rule.TwentiethIMM:
            case DateGeneration.Rule.ThirdWednesday:
            case DateGeneration.Rule.OldCDS:
            case DateGeneration.Rule.CDS:
               Utils.QL_REQUIRE( !endOfMonth, () => "endOfMonth convention incompatible with " + rule_ + " date generation rule" );
               goto case DateGeneration.Rule.Forward;			// fall through

            case DateGeneration.Rule.Forward:
               if (rule_ == DateGeneration.Rule.CDS)
               {
                  dates_.Add(previousTwentieth(effectiveDate, DateGeneration.Rule.CDS));
               }
               else
               {
                  dates_.Add(effectiveDate);
               }

               seed = dates_.Last();
               if (firstDate_ != null)
               {
                  dates_.Add(firstDate_);
                  Date temp = nullCalendar.advance(seed, periods * tenor_, convention_, endOfMonth_);
                  if (temp != firstDate_)
                     isRegular_.Add(false);
                  else
                     isRegular_.Add(true);
                  seed = firstDate_;
               }
               else if (rule_ == DateGeneration.Rule.Twentieth ||
                        rule_ == DateGeneration.Rule.TwentiethIMM ||
                        rule_ == DateGeneration.Rule.OldCDS ||
                        rule_ == DateGeneration.Rule.CDS)
               {
                  Date next20th = nextTwentieth(effectiveDate, rule_);
                  if (rule_ == DateGeneration.Rule.OldCDS)
                  {
                     // distance rule inforced in natural days
                     long stubDays = 30;
                     if (next20th - effectiveDate < stubDays)
                     {
                        // +1 will skip this one and get the next
                        next20th = nextTwentieth(next20th + 1, rule_);
                     }
                  }
                  if (next20th != effectiveDate)
                  {
                     dates_.Add(next20th);
                     isRegular_.Add(false);
                     seed = next20th;
                  }
               }

               exitDate = terminationDate;
               if (nextToLastDate_ != null)
                  exitDate = nextToLastDate_;
               while (true)
               {
                  Date temp = nullCalendar.advance(seed, periods * tenor_, convention_, endOfMonth_);
                  if (temp > exitDate)
                  {
                     if (nextToLastDate_ != null &&
                         (calendar_.adjust(dates_.Last(), convention_) !=  calendar_.adjust(nextToLastDate_, convention_)))
                     {
                        dates_.Add(nextToLastDate_);
                        isRegular_.Add(false);
                     }
                     break;
                  }
                  else
                  {
                     // skip dates that would result in duplicates
                     // after adjustment
                     if (calendar_.adjust(dates_.Last(), convention_) != calendar_.adjust(temp, convention_))
                     {
                        dates_.Add(temp);
                        isRegular_.Add(true);
                     }
                     ++periods;
                  }
               }

               if (calendar_.adjust(dates_.Last(), terminationDateConvention_) != 
                   calendar_.adjust(terminationDate, terminationDateConvention_))
               {
                  if (rule_ == DateGeneration.Rule.Twentieth ||
                      rule_ == DateGeneration.Rule.TwentiethIMM ||
                      rule_ == DateGeneration.Rule.OldCDS ||
                      rule_ == DateGeneration.Rule.CDS)
                  {
                     dates_.Add(nextTwentieth(terminationDate, rule_));
                     isRegular_.Add(true);
                  }
                  else
                  {
                     dates_.Add(terminationDate);
                     isRegular_.Add(false);
                  }
               }
               break;

            default:
               Utils.QL_FAIL("unknown rule (" + rule_ + ")");
               break;
         }

         // adjustments
         if (rule_ == DateGeneration.Rule.ThirdWednesday)
            for (int i = 1; i < dates_.Count-1; ++i)
               dates_[i] = Date.nthWeekday(3, DayOfWeek.Wednesday, dates_[i].Month, dates_[i].Year);

         if (endOfMonth && calendar_.isEndOfMonth(seed))
         {
            // adjust to end of month
            if (convention_ == BusinessDayConvention.Unadjusted)
            {
               for (int i = 0; i < dates_.Count-1; ++i)
                  dates_[i] = Date.endOfMonth(dates_[i]);
            }
            else
            {
               for (int i = 0; i < dates_.Count-1; ++i)
                  dates_[i] = calendar_.endOfMonth(dates_[i]);
            }
            if (terminationDateConvention_ != BusinessDayConvention.Unadjusted)
               dates_[dates_.Count - 1] = calendar_.endOfMonth(dates_.Last());
         }
         else
         {
            // first date not adjusted for CDS schedules
            if (rule_ != DateGeneration.Rule.OldCDS)
               dates_[0] = calendar_.adjust(dates_[0], convention_);
            for (int i = 1; i < dates_.Count-1; ++i)
               dates_[i] = calendar_.adjust(dates_[i], convention_);

            // termination date is NOT adjusted as per ISDA specifications, unless otherwise specified in the
            // confirmation of the deal or unless we're creating a CDS schedule
            if (terminationDateConvention_ != BusinessDayConvention.Unadjusted
                || rule_ == DateGeneration.Rule.Twentieth
                || rule_ == DateGeneration.Rule.TwentiethIMM
                || rule_ == DateGeneration.Rule.OldCDS
                || rule_ == DateGeneration.Rule.CDS)
               dates_[dates_.Count - 1] = calendar_.adjust(dates_.Last(), terminationDateConvention_);
         }

         // final safety check to remove duplicated last dates, if any
         // it can happen if EOM is applied to two near dates
         if (dates_.Count >= 2 &&  dates_[dates_.Count - 2] >= dates_.Last())
         {
            isRegular_[dates_.Count() - 2] = (dates_[dates_.Count() - 2] == dates_.Last());
            dates_[dates_.Count() - 2] = dates_.Last();
            
            dates_.RemoveAt(dates_.Count - 1);
            isRegular_.RemoveAt(isRegular_.Count - 1);
         }
      }
      #endregion

      //! \name Date access
      //@{
      int size() { return dates_.Count(); }
      public Date this[int i]
      {
         get
         {
            if (i >= dates_.Count)
               throw new ArgumentException("i (" + i + ") must be less than or equal to " + (dates_.Count - 1));
            return dates_[i];
         }
      }
      public Date at(int i) { return this[i]; }
      public Date date(int i) { return this[i]; }
      public Date previousDate(Date d)
      {
         int i = dates_.BinarySearch(d);
         if (i <= 0)
            return null;
         else
            return dates_[i - 1];
      }
      public Date nextDate(Date d)
      {
         int i = dates_.BinarySearch(d);
         if (i < 0 || i == dates_.Count-1)
            return null;
         else
            return dates_[i + 1];
      }
      public List<Date> dates() { return dates_; }
      public bool isRegular(int i)
      {
         Utils.QL_REQUIRE( fullInterface_, () => "full interface not available" );
         Utils.QL_REQUIRE( i <= isRegular_.Count() && i > 0, () => "index (" + i + ") must be in [1, " + isRegular_.Count() + "]" );
         return isRegular_[i - 1];
      }
      //@}

      //! \name Other inspectors
      //@{
      public bool empty() { return dates_.Count == 0; }
      public Calendar calendar() { return calendar_; }
      public Date startDate() { return dates_.First(); }
      public Date endDate() { return dates_.Last(); }
      public Period tenor() { Utils.QL_REQUIRE( fullInterface_, () => "full interface not available" ); return tenor_; }
      public BusinessDayConvention businessDayConvention() { return convention_; }
      public BusinessDayConvention terminationDateBusinessDayConvention() { Utils.QL_REQUIRE( fullInterface_, () => "full interface not available" ); return terminationDateConvention_; }
      public DateGeneration.Rule rule() { Utils.QL_REQUIRE( fullInterface_, () => "full interface not available" ); return rule_; }
      public bool endOfMonth() { Utils.QL_REQUIRE( fullInterface_, () => "full interface not available" ); return endOfMonth_; }
      //@}

      //@{
      //! truncated schedule
      public Schedule until(Date truncationDate)
      {
         Schedule result = (Schedule)this.MemberwiseClone();

         Utils.QL_REQUIRE( truncationDate > result.dates_[0], () =>
                   "truncation date " + truncationDate +
                   " must be later than schedule first date " +
                   result.dates_[0]);

         if (truncationDate < result.dates_.Last())
         {
            // remove later dates
            while (result.dates_.Last() > truncationDate)
            {
               result.dates_.RemoveAt(dates_.Count - 1);
               result.isRegular_.RemoveAt(isRegular_.Count - 1);
            }

            // add truncationDate if missing
            if (truncationDate != result.dates_.Last())
            {
               result.dates_.Add(truncationDate);
               result.isRegular_.Add(false);
               result.terminationDateConvention_ = BusinessDayConvention.Unadjusted;
            }
            else
            {
               result.terminationDateConvention_ = convention_;
            }

            if (result.nextToLastDate_ >= truncationDate)
               result.nextToLastDate_ = null;
            if (result.firstDate_ >= truncationDate)
               result.firstDate_ = null;
         }

         return result;
      }
      //@}

      Date nextTwentieth(Date d, DateGeneration.Rule rule)
      {
         Date result = new Date(20, d.month(), d.year());
         if (result < d)
            result += new Period(1, TimeUnit.Months);
         if (rule == DateGeneration.Rule.TwentiethIMM ||
             rule == DateGeneration.Rule.OldCDS ||
             rule == DateGeneration.Rule.CDS)
         {
            int m = result.month();
            if (m % 3 != 0)
            { // not a main IMM nmonth
               int skip = 3 - m % 3;
               result += new Period(skip, TimeUnit.Months);
            }
         }
         return result;
      }
      Date previousTwentieth(Date d, DateGeneration.Rule rule)
      {
         Date result = new Date(20, d.month(), d.year());
         if (result > d)
            result -= new Period(1, TimeUnit.Months);
         if (rule == DateGeneration.Rule.TwentiethIMM ||
             rule == DateGeneration.Rule.OldCDS ||
             rule == DateGeneration.Rule.CDS)
         {
            int m = result.month();
            if (m % 3 != 0)
            { // not a main IMM nmonth
               int skip = m % 3;
               result -= new Period(skip, TimeUnit.Months);
            }
         }
         return result;
      }

      public int Count { get { return dates_.Count; } }

	}

    //! helper class
    /*! This class provides a more comfortable interface to the argument list of Schedule's constructor. */
    public class MakeSchedule {
        private Calendar calendar_;
        private Date effectiveDate_, terminationDate_;
        private Period tenor_;
        private Nullable<BusinessDayConvention> convention_, terminationDateConvention_;
        private DateGeneration.Rule rule_;
        private bool endOfMonth_;
        private Date firstDate_, nextToLastDate_;

        public MakeSchedule() { rule_ = DateGeneration.Rule.Backward; endOfMonth_ = false; }

        public MakeSchedule from(Date effectiveDate)
        {
           effectiveDate_ = effectiveDate;
           return this;
        }

        public MakeSchedule to(Date terminationDate) 
        {
           terminationDate_ = terminationDate;
           return this;
        }

        public MakeSchedule withTenor(Period tenor) 
        {
           tenor_ = tenor;
           return this;
        }

        public MakeSchedule withFrequency(Frequency frequency) 
        {
           tenor_ = new Period(frequency);
           return this;
        }

        public MakeSchedule withCalendar(Calendar calendar) 
        {
           calendar_ = calendar;
           return this;
        }

        public MakeSchedule withConvention(BusinessDayConvention conv) 
        {
           convention_ = conv;
           return this;
        }

        public MakeSchedule withTerminationDateConvention(BusinessDayConvention conv)
        {
           terminationDateConvention_ = conv;
           return this;
        }

        public MakeSchedule withRule(DateGeneration.Rule r)
        {
           rule_ = r;
           return this;
        }

        public MakeSchedule forwards()
        {
           rule_ = DateGeneration.Rule.Forward;
           return this;
        }

        public MakeSchedule backwards()
        {
           rule_ = DateGeneration.Rule.Backward;
           return this;
        }

        public MakeSchedule endOfMonth(bool flag = true)
        {
           endOfMonth_ = flag;
           return this;
        }

        public MakeSchedule withFirstDate(Date d)
        {
           firstDate_ = d;
           return this;
        }

        public MakeSchedule withNextToLastDate(Date d)
        {
           nextToLastDate_ = d;
           return this;
        }

        //public MakeSchedule(Date effectiveDate, Date terminationDate, Period tenor, Calendar calendar,
        //                    BusinessDayConvention convention) {
        //    calendar_ = calendar;
        //    effectiveDate_ = effectiveDate;
        //    terminationDate_ = terminationDate;
        //    tenor_ = tenor;
        //    convention_ = convention;
        //    terminationDateConvention_ = convention;
        //    rule_ = DateGeneration.Rule.Backward;
        //    endOfMonth_ = false;
        //    firstDate_ = nextToLastDate_ = null;
        //}

        public Schedule value() 
        {

           // check for mandatory arguments
           if (effectiveDate_ == null)
              throw new ApplicationException("effective date not provided");
           if (terminationDate_ == null)
              throw new ApplicationException("termination date not provided");
           if ((object)tenor_ == null)
              throw new ApplicationException("tenor/frequency not provided");

			  // if no calendar was set...
           if (calendar_ == null)
           {
               // ...we use a null one.
               calendar_ = new NullCalendar();
           }

			  // set dynamic defaults:
           BusinessDayConvention convention;
           // if a convention was set, we use it.
           if (convention_ != null )
           {
              convention = convention_.Value;
           }
           else
           {
              if (!calendar_.empty())
              {
                 // ...if we set a calendar, we probably want it to be used;
                 convention = BusinessDayConvention.Following;
              }
              else
              {
                 // if not, we don't care.
                 convention = BusinessDayConvention.Unadjusted;
              }
           }

           BusinessDayConvention terminationDateConvention;
           // if set explicitly, we use it;
           if (terminationDateConvention_ != null )
           {
              terminationDateConvention = terminationDateConvention_.Value;
           }
           else
           {
              // Unadjusted as per ISDA specification
              terminationDateConvention = convention;
           }

            return new Schedule(effectiveDate_, terminationDate_, tenor_, calendar_,
                                convention, terminationDateConvention,
                                rule_, endOfMonth_, firstDate_, nextToLastDate_);
        }
    }
}