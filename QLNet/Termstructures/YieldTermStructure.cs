/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008 Andrea Maggiulli 
  
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
using System.Linq;
using System.Collections.Generic;

namespace QLNet
{
   //! Interest-rate term structure
   /*! This abstract class defines the interface of concrete
      interest rate structures which will be derived from this one.

      \ingroup yieldtermstructures

      \test observability against evaluation date changes is checked.
   */
   public class YieldTermStructure : TermStructure 
   {
      private const double dt = 0.0001;

      #region Constructors

      public YieldTermStructure()
         : this(null,null,null)
      {}

      public YieldTermStructure(DayCounter dc = null,List<Handle<Quote> > jumps = null,List<Date> jumpDates = null)
         :base(dc)
      {
         if ( jumps != null )
            jumps_ = jumps;
         else
            jumps_ = new List<Handle<Quote>>();

         if ( jumpDates != null )
            jumpDates_ = jumpDates;
         else
            jumpDates_ = new List<Date>();

         jumpTimes_ = new List<double>(jumpDates_.Count);
         nJumps_ = jumps_.Count;
         setJumps();
         for (int i=0; i<nJumps_; ++i)
            jumps_[i].registerWith(update);
      }
      public YieldTermStructure(Date referenceDate,Calendar cal = null,DayCounter dc = null,
                                List<Handle<Quote> > jumps = null,List<Date> jumpDates = null)
         :base(referenceDate, cal, dc) 
      {
         if ( jumps != null )
            jumps_ = jumps;
         else
            jumps_ = new List<Handle<Quote>>();

         if ( jumpDates != null )
            jumpDates_ = jumpDates;
         else
            jumpDates_ = new List<Date>();

         jumpTimes_ = new List<double>(jumpDates_.Count);
         nJumps_ = jumps_.Count;
         setJumps();
         for (int i=0; i<nJumps_; ++i)
            jumps_[i].registerWith(update);
      }
      public YieldTermStructure(int settlementDays,Calendar cal,DayCounter dc = null,
                                List<Handle<Quote> > jumps = null,List<Date> jumpDates = null)
         : base(settlementDays, cal, dc)
      {
         if ( jumps != null )
            jumps_ = jumps;
         else
            jumps_ = new List<Handle<Quote>>();

         if ( jumpDates != null )
            jumpDates_ = jumpDates;
         else
            jumpDates_ = new List<Date>();

         jumpTimes_ = new List<double>(jumpDates_.Count);
         nJumps_ = jumps_.Count;
         setJumps();
         for (int i=0; i<nJumps_; ++i)
            jumps_[i].registerWith(update);
      }

      #endregion

      #region Discount factors

      //    These methods return the discount factor from a given date or time
      //    to the reference date.  In the latter case, the time is calculated
      //    as a fraction of year from the reference date.

      public double discount(Date d, bool extrapolate = false)
      {
         return discount(timeFromReference(d), extrapolate);
      }
      
      /*! The same day-counting rule used by the term structure
          should be used for calculating the passed time t.
      */
      public double discount(double t, bool extrapolate = false)
      {
         checkRange(t, extrapolate);

        if (jumps_.empty())
            return discountImpl(t);

        double jumpEffect = 1.0;
        for (int i=0; i<nJumps_; ++i) 
        {
            if (jumpTimes_[i]>0 && jumpTimes_[i]<t) 
            {
                Utils.QL_REQUIRE(jumps_[i].link.isValid(),"invalid " + (i+1) + " jump quote");
                double thisJump = jumps_[i].link.value();
                Utils.QL_REQUIRE(thisJump>0.0 && thisJump<=1.0, "invalid " + (i+1) + " jump value: " + thisJump);
                jumpEffect *= thisJump;
            }
         }
         return jumpEffect * discountImpl(t);
      }
      
      #endregion

      #region Zero-yield rates

      //    These methods return the implied zero-yield rate for a
      //    given date or time.  In the former case, the time is
      //    calculated as a fraction of year from the reference date.

      /*! The resulting interest rate has the required daycounting
          rule.
      */
      public InterestRate zeroRate(Date d,DayCounter dayCounter,Compounding comp,Frequency freq = Frequency.Annual,
                                   bool extrapolate = false)
      {
         if (d==referenceDate()) 
         {
            double compound = 1.0/discount(dt, extrapolate);
            // t has been calculated with a possibly different daycounter
            // but the difference should not matter for very small times
            return InterestRate. impliedRate(compound, dayCounter, comp, freq,  dt);
         }
         double compound1 = 1.0/discount(d, extrapolate);
         return InterestRate.impliedRate(compound1, dayCounter, comp, freq, referenceDate(), d);
      }

      /*! The resulting interest rate has the same day-counting rule
          used by the term structure. The same rule should be used
          for calculating the passed time t.
      */
      public InterestRate zeroRate(double t, Compounding comp,Frequency freq = Frequency.Annual,bool extrapolate = false)
      {
         if (t==0.0) t = dt;
         double compound = 1.0/discount(t, extrapolate);
         return InterestRate.impliedRate(compound,dayCounter(), comp, freq, t);
      }

      #endregion

      #region Forward rates

      //    These methods returns the forward interest rate between two dates
      //    or times.  In the former case, times are calculated as fractions
      //    of year from the reference date.
      //
      //    If both dates (times) are equal the instantaneous forward rate is
      //    returned.

      /*! The resulting interest rate has the required day-counting
          rule.
      */
      public InterestRate forwardRate(Date d1, Date d2, DayCounter dayCounter, Compounding comp,
         Frequency freq = Frequency.Annual,bool extrapolate = false) 
      {
         if (d1==d2) 
         {
            checkRange(d1, extrapolate);
            double t1 = Math.Max(timeFromReference(d1) - dt/2.0, 0.0);
            double t2 = t1 + dt;
            double compound = discount(t1, true)/discount(t2, true);
            // times have been calculated with a possibly different daycounter
            // but the difference should not matter for very small times
            return InterestRate.impliedRate(compound, dayCounter, comp, freq, dt);
         }
         Utils.QL_REQUIRE(d1 < d2,  d1 + " later than " + d2);
         double compound1 = discount(d1, extrapolate)/discount(d2, extrapolate);
         return InterestRate.impliedRate(compound1, dayCounter, comp, freq, d1, d2);
      }

      /*! The resulting interest rate has the required day-counting
          rule.
          \warning dates are not adjusted for holidays
      */
      public InterestRate forwardRate(Date d,Period p,DayCounter dayCounter,Compounding comp,
                               Frequency freq = Frequency.Annual, bool extrapolate = false)
      {
          return forwardRate(d, d+p, dayCounter, comp, freq, extrapolate);
      }

      /*! The resulting interest rate has the same day-counting rule
          used by the term structure. The same rule should be used
          for calculating the passed times t1 and t2.
      */
      public InterestRate forwardRate(double t1, double t2, Compounding comp, Frequency freq = Frequency.Annual, 
         bool extrapolate = false)
      {
         double compound;
         if (t2==t1) 
         {
            checkRange(t1, extrapolate);
            t1 = Math.Max(t1 - dt/2.0, 0.0);
            t2 = t1 + dt;
            compound = discount(t1, true)/discount(t2, true);
         } 
         else 
         {
            Utils.QL_REQUIRE(t2>t1, "t2 (" + t2 + ") < t1 (" + t2 + ")");
            compound = discount(t1, extrapolate)/discount(t2, extrapolate);
         }
         return InterestRate.impliedRate(compound, dayCounter(), comp, freq, t2-t1);
      }

      #endregion

      #region Jump inspectors

      public List<Date> jumpDates() 
      {
         return this.jumpDates_;
      }
      public List<double> jumpTimes()
      {
         return this.jumpTimes_;
      }

      #endregion

      #region Observer interface
      
      public override void update()
      {
         base.update();
         if (referenceDate() != latestReference_)
            setJumps();
      }

      #endregion

      #region Calculations

      //    This method must be implemented in derived classes to
      //    perform the actual calculations. When it is called,
      //    range check has already been performed; therefore, it
      //    must assume that extrapolation is required.

      //! discount factor calculation
      protected virtual double discountImpl(double d) { throw new NotSupportedException(); }
      
      #endregion
      
      // methods
      private void setJumps()
      {
         if (jumpDates_.empty() && !jumps_.empty()) 
         { 
            // turn of year dates
            jumpDates_.Clear();
            jumpTimes_.Clear();
            int y = referenceDate().year();
            for (int i=0; i<nJumps_; ++i)
                jumpDates_.Add(new Date(31, Month.December, y+i));
            
         } 
         else 
         { 
            // fixed dats
            Utils.QL_REQUIRE(jumpDates_.Count ==nJumps_,
                       "mismatch between number of jumps (" + nJumps_ +
                       ") and jump dates (" + jumpDates_.Count + ")");
        }
        for (int i=0; i<nJumps_; ++i)
            jumpTimes_.Add(timeFromReference(jumpDates_[i]));
        
         latestReference_ = base.referenceDate();
      }
      
      // data members
      private List<Handle<Quote> > jumps_;
      private List<Date> jumpDates_;
      private List<double> jumpTimes_;
      private int nJumps_;
      private Date latestReference_;
    };


   ///// <summary>
   ///// Interest-rate term structure
   ///// This class defines the interface of concrete rate structures which will be derived from 
   ///// this one.
   ///// Rates are assumed to be annual continuous compounding.
   ///// \ingroup yieldtermstructures
   ///// \test observability against evaluation date changes is checked.
   ///// </summary>
   // public class YieldTermStructure : TermStructure
   // {
   //    #region Constructors
   //    /// See the TermStructure documentation for issues regarding constructors.

   //    public YieldTermStructure() : this(new DayCounter()) { }
   //     /// <summary>
   //     /// Default constructor
   //     /// <remarks>
   //     /// \warning term structures initialized by means of this
   //     /// constructor must manage their own reference date
   //     /// by overriding the referenceDate() method.
   //     /// </remarks> 
   //     /// </summary>
   //     public YieldTermStructure(DayCounter dc) : base(dc) { }

   //     /// <summary>
   //     /// initialize with a fixed reference date
   //     /// </summary>
   //     public YieldTermStructure(Date referenceDate) : this(referenceDate, new Calendar(), new DayCounter()) { }
   //     public YieldTermStructure(Date referenceDate, Calendar cal) : this(referenceDate, cal, new DayCounter()) { }
   //     public YieldTermStructure(Date referenceDate, Calendar cal, DayCounter dc) : base(referenceDate, cal, dc) { }

   //     public YieldTermStructure(int settlementDays, Calendar cal) :
   //         this(settlementDays, cal, new DayCounter()) { }
   //     public YieldTermStructure(int settlementDays, Calendar cal, DayCounter dc) :
   //         base(settlementDays, cal, dc) { }

   //    #endregion

   //    #region zero-yield rates
   //    /// These methods return the implied zero-yield rate for a given date or time.  In the former case, the time is calculated as a fraction of year from the reference date.

   //     // The resulting interest rate has the required daycounting rule.
   //     public InterestRate zeroRate(Date d, DayCounter dayCounter, Compounding comp) {
   //         return zeroRate(d, dayCounter, comp, Frequency.Annual, false);
   //     }
   //     public InterestRate zeroRate(Date d, DayCounter dayCounter, Compounding comp, Frequency freq) {
   //         return zeroRate(d, dayCounter, comp, freq, false);
   //     }
   //     public InterestRate zeroRate(Date d, DayCounter dayCounter, Compounding comp, Frequency freq, bool extrapolate)
   //     {
   //         if (d == referenceDate())
   //         {
   //             double t = 0.0001;
   //             double compound = 1 / discount(t, extrapolate);
   //             return InterestRate.impliedRate(compound, t, dayCounter, comp, freq);
   //         }
   //         else
   //         {
   //             double compound = 1 / discount(d, extrapolate);
   //             return InterestRate.impliedRate(compound, referenceDate(), d, dayCounter, comp, freq);
   //         }
   //     }

   //     // The resulting interest rate has the same day-counting rule  used by the term structure. The same rule should be used for calculating the passed time t.
   //     public InterestRate zeroRate(double t, Compounding comp) { return zeroRate(t, comp, Frequency.Annual, false); }
   //     public InterestRate zeroRate(double t, Compounding comp, Frequency freq) { return zeroRate(t, comp, freq, false); }
   //     public InterestRate zeroRate(double t, Compounding comp, Frequency freq, bool extrapolate)
   //     {
   //         if (t == 0) t = 0.0001;
   //         double compound = 1 / discount(t, extrapolate);
   //         return InterestRate.impliedRate(compound, t, dayCounter(), comp, freq);
   //     }
   //     #endregion

   //    #region discount factors
   //     /// These methods return the discount factor for a given date or time.  In the former case, the time is calculated as a fraction of year from the reference date.
         
   //     public double discount(Date d) { return discount(timeFromReference(d), false); }
   //     public double discount(Date d, bool extrapolate) { return discount(timeFromReference(d), extrapolate); }

   //     // The same day-counting rule used by the term structure should be used for calculating the passed time t.
   //     public double discount(double t) { return discount(t, false); }
   //     public double discount(double t, bool extrapolate) { checkRange(t, extrapolate); return discountImpl(t); }
   //     #endregion

   //    #region forward rates
   //    /// These methods returns the implied forward interest rate between two dates or times.  In the former case, times are calculated as fractions of year from the reference date.
   //    /// The resulting interest rate has the required day-counting rule.
   //     public InterestRate forwardRate(Date d1, Date d2, DayCounter resultDayCounter, Compounding comp)
   //     {
   //         return forwardRate(d1, d2, resultDayCounter, comp, Frequency.Annual, false);
   //     }
   //     public InterestRate forwardRate(Date d1, Date d2, DayCounter resultDayCounter, Compounding comp, Frequency freq)
   //     {
   //         return forwardRate(d1, d2, resultDayCounter, comp, freq, false);
   //     }
   //     public InterestRate forwardRate(Date d1, Date d2, DayCounter resultDayCounter, Compounding comp, Frequency freq, bool extrapolate)
   //     {
   //         if (d1 == d2)
   //         {
   //             double t1 = timeFromReference(d1);
   //             double t2 = t1 + 0.0001;
   //             double compound = discount(t1, extrapolate) / discount(t2, extrapolate);
   //             return InterestRate.impliedRate(compound, t2 - t1, resultDayCounter, comp, freq);
   //         }
   //         else
   //         {
   //             if (!(d1 < d2)) throw new ArgumentException(d1 + " later than " + d2);
   //             double compound = discount(d1, extrapolate) / discount(d2, extrapolate);
   //             return InterestRate.impliedRate(compound, d1, d2, resultDayCounter, comp, freq);
   //         }
   //     }

   //     // The resulting interest rate has the required day-counting rule.
   //     // warning dates are not adjusted for holidays
   //     public InterestRate forwardRate(Date d, Period p, DayCounter resultDayCounter, Compounding comp)
   //     {
   //         return forwardRate(d, d + p, resultDayCounter, comp, Frequency.Annual, false);
   //     }
   //     public InterestRate forwardRate(Date d, Period p, DayCounter resultDayCounter, Compounding comp, Frequency freq)
   //     {
   //         return forwardRate(d, d + p, resultDayCounter, comp, freq, false);
   //     }
   //     public InterestRate forwardRate(Date d, Period p, DayCounter resultDayCounter, Compounding comp, Frequency freq, bool extrapolate)
   //     {
   //         return forwardRate(d, d + p, resultDayCounter, comp, freq, extrapolate);
   //     }

   //     // The resulting interest rate has the same day-counting rule used by the term structure. The same rule should be used for the calculating the passed times t1 and t2.
   //     public InterestRate forwardRate(double t1, double t2, Compounding comp)
   //     {
   //         return forwardRate(t1, t2, comp, Frequency.Annual, false);
   //     }
   //     public InterestRate forwardRate(double t1, double t2, Compounding comp, Frequency freq)
   //     {
   //         return forwardRate(t1, t2, comp, freq, false);
   //     }
   //     public InterestRate forwardRate(double t1, double t2, Compounding comp, Frequency freq, bool extrapolate)
   //     {
   //         if (t2 == t1) t2 = t1 + 0.0001;
   //         if (!(t2 > t1)) throw new ArgumentException("t2 (" + t2 + ") < t1 (" + t2 + ")");
   //         double compound = discount(t1, extrapolate) / discount(t2, extrapolate);
   //         return InterestRate.impliedRate(compound, t2 - t1, dayCounter(), comp, freq);
   //     }
   //     #endregion

   //    #region parRates
   //    /// These methods returns the implied par rate for a given
   //    /// sequence of payments at the given dates or times.  In the
   //    /// former case, times are calculated as fractions of year
   //    /// from the reference date.
   //    /// 
   //    /// \warning though somewhat related to a swap rate, this
   //    ///          method is not to be used for the fair rate of a
   //    ///          real swap, since it does not take into account
   //    ///          all the market conventions' details. The correct
   //    ///          way to evaluate such rate is to instantiate a
   //    ///          SimpleSwap with the correct conventions, pass it
   //    ///          the term structure and call the swap's fairRate()
   //    ///          method.
   //    /// 
       
   //    public double parRate(int tenor, Date startDate) { return parRate(tenor, startDate, Frequency.Annual, false); }
   //    public double parRate(int tenor, Date startDate, Frequency freq) { return parRate(tenor, startDate, freq, false); }
   //    public double parRate(int tenor, Date startDate, Frequency freq, bool extrapolate)
   //    {
   //        List<Date> dates = new List<Date>(); dates.Add(startDate);
   //        for (int i = 1; i <= tenor; ++i)
   //            dates.Add(startDate + new Period(i, TimeUnit.Years));
   //        return parRate(dates, freq, extrapolate);
   //    }

   //    // the first date in the vector must equal the start date; the following dates must equal the payment dates.
   //    public double parRate(List<Date> dates) { return parRate(dates, Frequency.Annual, false); }
   //    public double parRate(List<Date> dates, Frequency freq) { return parRate(dates, freq, false); }
   //    public double parRate(List<Date> dates, Frequency freq, bool extrapolate)
   //    {
   //         List<double> times = new List<double>(dates.Count);
   //         for (int i = 0; i < dates.Count; i++)
   //             times[i] = timeFromReference(dates[i]);
   //         return parRate(times, freq, extrapolate);
   //     }

   //     // the first time in the vector must equal the start time; the following times must equal the payment times.
   //     public double parRate(List<double> times) { return parRate(times, Frequency.Annual, false); }
   //     public double parRate(List<double> times, Frequency freq) { return parRate(times, freq, false); }
   //     public double parRate(List<double> times, Frequency freq, bool extrapolate)
   //     {
   //         if (!(times.Count >= 2)) throw new ArgumentException("at least two times are required");
   //         checkRange(times.Last(), extrapolate);
   //         double sum = 0;
   //         for (int i = 1; i < times.Count; i++)
   //             sum += discountImpl(times[i]);
   //         double result = discountImpl(times.First()) - discountImpl(times.Last());
   //         result *= ((double)freq) / sum;
   //         return result;
   //     }
   //     #endregion

   //     /// <summary>
   //     /// TermStructure interface
   //     /// </summary>
   //     public override Date maxDate() { throw new NotImplementedException(); }

   //    /// <summary>
   //    /// Returns the discount factor for the given date calculating it from the zero yield.
   //    /// This method must be implemented in derived classes to perform the actual discount a
   //    /// nd rate calculations.
   //    /// When they are called, range check has already been performed
   //    /// therefore, they must assume that extrapolation is required.
   //    /// </summary>
   //     protected virtual double discountImpl(double t) { throw new NotImplementedException(); }
   // }
}
