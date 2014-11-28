/*
 Copyright (C) 2008, 2009 , 2010  Andrea Maggiulli (a.maggiulli@gmail.com)
 * 
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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QLNet
{
   //! Base class for inflation-rate indexes,
   public class InflationIndex : Index,IObserver
   {
      /*! An inflation index may return interpolated
          values.  These are linearly interpolated
          values with act/act convention within a period.
          Note that stored "fixings" are always flat (constant)
          within a period and interpolated as needed.  This
          is because interpolation adds an addional availability
          lag (because you always need the next period to
          give the previous period's value)
          and enables storage of the most recent uninterpolated value.
      */

      public InflationIndex(string familyName, Region region, bool revised, bool interpolated,
                            Frequency frequency, Period availabilitiyLag, Currency currency)
      {
         familyName_ = familyName;
         region_ = region;
         revised_ = revised;
         interpolated_ = interpolated;
         frequency_ = frequency;
         availabilityLag_ = availabilitiyLag;
         currency_ = currency;
         Settings.registerWith(update);
         IndexManager.instance().notifier(name()).registerWith(update);
      }


      //! \name Index interface
      //@{
      public override string name() { return region_.name() + " " + familyName_; }

      /*! Inflation indices do not have fixing calendars.  An
          inflation index value is valid for every day (including
          weekends) of a calendar period.  I.e. it uses the
          NullCalendar as its fixing calendar.
      */
      public override Calendar fixingCalendar() { return new NullCalendar(); }

      public override bool isValidFixingDate(Date fixingDate) { return true; }

      /*! Forecasting index values requires an inflation term
          structure.  The inflation term structure (ITS) defines the
          usual lag (not the index).  I.e.  an ITS is always relatve
          to a base date that is earlier than its asof date.  This
          must be so because indices are available only with a lag.
          However, the index availability lag only sets a minimum
          lag for the ITS.  An ITS may be relative to an earlier
          date, e.g. an index may have a 2-month delay in
          publication but the inflation swaps may take as their base
          the index 3 months before.
      */
      public override double fixing(Date fixingDate) { return fixing(fixingDate, false); }
      public override double fixing(Date fixingDate, bool forecastTodaysFixing) { return 0; }

      /*! this method creates all the "fixings" for the relevant
          period of the index.  E.g. for monthly indices it will put
          the same value in every calendar day in the month.
      */
      public override void addFixing(Date fixingDate, double fixing) { addFixing(fixingDate, fixing, false); }
      public override void addFixing(Date fixingDate, double fixing, bool forceOverwrite)
      {
         KeyValuePair<Date,Date> lim = Utils.inflationPeriod(fixingDate, frequency_);
         int n = lim.Value - lim.Key + 1;
         List<Date> dates = new List<Date>(n);
         List<double> rates = new List<double>(n);
        
         for (int i=0; i<n; ++i) 
         {
            dates.Add(lim.Key + i);
            rates.Add(fixing);
         }

         base.addFixings(dates,rates,forceOverwrite);

      }
      //@}

      //! \name Observer interface
      //@{
      public void update() { notifyObservers(); }
      //@}

      //! \name Inspectors
      //@{
      public string familyName() { return familyName_;}
      public Region region() { return region_;}
      public bool revised() { return revised_;}
      /*! Forecasting index values using an inflation term structure
         uses the interpolation of the inflation term structure
         unless interpolation is set to false.  In this case the
         extrapolated values are constant within each period taking
         the mid-period extrapolated value.
      */
      public bool interpolated() {return interpolated_;}
      public Frequency frequency() {return frequency_;}
      /*! The availability lag describes when the index is
         <i>available</i>, not how it is used.  Specifically the
         fixing for, say, January, may only be available in April
         but the index will always return the index value
         applicable for January as its January fixing (independent
         of the lag in availability).
      */
      public Period availabilityLag() {return availabilityLag_;}
      public Currency currency() { return currency_; }
      //@}

      protected  Date referenceDate_;
      protected string familyName_;
      protected Region region_;
      protected bool revised_;
      protected bool interpolated_;
      protected Frequency frequency_;
      protected Period availabilityLag_;
      protected Currency currency_;

   }


   //! Base class for zero inflation indices.
   public class ZeroInflationIndex : InflationIndex 
   {
      //! Always use the evaluation date as the reference date
      public ZeroInflationIndex(string familyName,
                                Region region,
                                bool revised,
                                bool interpolated,
                                Frequency frequency,
                                Period availabilityLag,
                                Currency currency)
         :this(familyName,region,revised,interpolated,frequency,availabilityLag,
               currency,new Handle<ZeroInflationTermStructure>()){}

      public ZeroInflationIndex(string familyName,
                                Region region,
                                bool revised,
                                bool interpolated,
                                Frequency frequency,
                                Period availabilityLag,
                                Currency currency,
                                Handle<ZeroInflationTermStructure> ts)
         : base(familyName, region, revised, interpolated,
                     frequency, availabilityLag, currency)
      {
         zeroInflation_ = ts;
         zeroInflation_.registerWith (update);
      }

        /*! \warning the forecastTodaysFixing parameter (required by
                     the Index interface) is currently ignored.
        */
        public override double fixing(Date aFixingDate, bool forecastTodaysFixing)
        {
           if (!needsForecast(aFixingDate)) 
           {
              if (!IndexManager.instance().getHistory(name()).value().ContainsKey(aFixingDate))
                 throw new ApplicationException("Missing " + name() + " fixing for " + aFixingDate);

               double pastFixing = IndexManager.instance().getHistory(name()).value()[aFixingDate];
               double theFixing = pastFixing;

               if (interpolated_) 
               {
                  // fixings stored flat & for every day
                  Date fixingDate2 = aFixingDate + new Period(frequency_);
                  if (!IndexManager.instance().getHistory(name()).value().ContainsKey(fixingDate2))
                      throw new ApplicationException("Missing " + name() + " fixing for " + fixingDate2);

                  double pastFixing2 = IndexManager.instance().getHistory(name()).value()[fixingDate2];

                  // now linearly interpolate
                  KeyValuePair<Date,Date> lim2 = Utils.inflationPeriod(aFixingDate, frequency_);
                  double daysInPeriod = lim2.Value+1 - lim2.Key;
                  theFixing = pastFixing
                       + (pastFixing2 -pastFixing)*(aFixingDate-lim2.Key)/daysInPeriod;
               }
               return theFixing;
           } 
           else 
           {
              return forecastFixing(aFixingDate);
           }
        }

        bool needsForecast(Date fixingDate)
        {
            // Stored fixings are always non-interpolated.
            // If an interpolated fixing is required then
            // the availability lag + one inflation period
            // must have passed to use historical fixings
            // (because you need the next one to interpolate).
            // The interpolation is calculated (linearly) on demand.

            Date today = Settings.evaluationDate();
            Date todayMinusLag = today - availabilityLag_;

            Date historicalFixingKnown = Utils.inflationPeriod(todayMinusLag, frequency_).Key - 1;
            Date latestNeededDate = fixingDate;

            if (interpolated_) { // might need the next one too
                KeyValuePair<Date, Date> p = Utils.inflationPeriod(fixingDate, frequency_);
                if (fixingDate > p.Key)
                    latestNeededDate = latestNeededDate + new Period(frequency_);
            }

            if (latestNeededDate <= historicalFixingKnown) {
                // the fixing date is well before the availability lag, so
                // we know that fixings were provided.
                return false;
            } else if (latestNeededDate > today) {
                // the fixing can't be available, no matter what's in the
                // time series
                return true;
            } else {
                // we're not sure, but the fixing might be there so we
                // check.  Todo: check which fixings are not possible, to
                // avoid using fixings in the future
                return IndexManager.instance().getHistory(name()).value().ContainsKey(latestNeededDate);
            }
        }


        public Handle<ZeroInflationTermStructure> zeroInflationTermStructure() {return zeroInflation_;}

        public ZeroInflationIndex clone(Handle<ZeroInflationTermStructure> h) 
        {
   
           return new ZeroInflationIndex(familyName_, region_, revised_,
                                             interpolated_, frequency_,
                                             availabilityLag_, currency_, h);
        }


        private double forecastFixing(Date fixingDate)
        {
           // the term structure is relative to the fixing value at the base date.
           Date baseDate = zeroInflation_.link.baseDate();
           Utils.QL_REQUIRE( !needsForecast( baseDate ), () => name() + " index fixing at base date is not available" );
           double baseFixing = fixing(baseDate, false);
           Date effectiveFixingDate;
           if (interpolated()) 
           {
              effectiveFixingDate = fixingDate;
           } 
           else 
           {
              // start of period is the convention
              // so it's easier to do linear interpolation on fixings
              effectiveFixingDate = Utils.inflationPeriod(fixingDate, frequency()).Key;
           }

           // no observation lag because it is the fixing for the date
           // but if index is not interpolated then that fixing is constant
           // for each period, hence the t uses the effectiveFixingDate
           // However, it's slightly safe to get the zeroRate with the
           // fixingDate to avoid potential problems at the edges of periods
           double t = zeroInflation_.link.dayCounter().yearFraction(baseDate, effectiveFixingDate);
           bool forceLinearInterpolation = false;
           double zero = zeroInflation_.link.zeroRate(fixingDate, new Period(0,TimeUnit.Days), forceLinearInterpolation);
           // Annual compounding is the convention for zero inflation rates (or quotes)
           return baseFixing * Math.Pow(1.0 + zero, t);
        }

        private Handle<ZeroInflationTermStructure> zeroInflation_;
    };
    
   //! Base class for year-on-year inflation indices.
   /*! These may be genuine indices published on, say, Bloomberg, or
       "fake" indices that are defined as the ratio of an index at
       different time points.
   */
   public class YoYInflationIndex : InflationIndex
   {

      public YoYInflationIndex(string familyName,
                               Region region,
                               bool revised,
                               bool interpolated,
                               bool ratio, // is this one a genuine index or a ratio?
                               Frequency frequency,
                               Period availabilityLag,
                               Currency currency)
         :this(familyName,region,revised,interpolated,ratio,frequency,availabilityLag,currency,
               new Handle<YoYInflationTermStructure>()) {}

      public YoYInflationIndex(string familyName,
                               Region region,
                               bool revised,
                               bool interpolated,
                               bool ratio, // is this one a genuine index or a ratio?
                               Frequency frequency,
                               Period availabilityLag,
                               Currency currency,
                               Handle<YoYInflationTermStructure> yoyInflation)
         : base(familyName, region, revised, interpolated,frequency, availabilityLag, currency)
      {
         ratio_ = ratio;
         yoyInflation_ = yoyInflation;
         yoyInflation_.registerWith(update);
      }

      /*! \warning the forecastTodaysFixing parameter (required by
           the Index interface) is currently ignored.
      */
      public override double fixing(Date fixingDate) { return fixing(fixingDate,false); }
      public override double fixing(Date fixingDate, bool forecastTodaysFixing)
      {
         Date today = Settings.evaluationDate();
         Date todayMinusLag = today - availabilityLag_;
         KeyValuePair<Date,Date> limm = Utils.inflationPeriod(todayMinusLag, frequency_);
         Date lastFix = limm.Key-1;

         Date flatMustForecastOn = lastFix+1;
         Date interpMustForecastOn = lastFix+1 - new Period(frequency_);


         if (interpolated() && fixingDate >= interpMustForecastOn) {
            return forecastFixing(fixingDate);
         }

         if (!interpolated() && fixingDate >= flatMustForecastOn) {
            return forecastFixing(fixingDate);
         }

         // four cases with ratio() and interpolated()
         if (ratio()) 
         {
            if(interpolated())
            {
               // IS ratio, IS interpolated
               KeyValuePair<Date,Date> lim = Utils.inflationPeriod(fixingDate, frequency_);
               Date fixMinus1Y= new NullCalendar().advance(fixingDate, new Period(-1,TimeUnit.Years),BusinessDayConvention.ModifiedFollowing);
               KeyValuePair<Date,Date> limBef = Utils.inflationPeriod(fixMinus1Y, frequency_);
               double dp= lim.Value + 1 - lim.Key;
               double dpBef=limBef.Value + 1 - limBef.Key;
               double dl = fixingDate-lim.Key;
                // potentially does not work on 29th Feb
                double dlBef = fixMinus1Y - limBef.Key;
                // get the four relevant fixings
                // recall that they are stored flat for every day
                double? limFirstFix =
                IndexManager.instance().getHistory(name()).value()[lim.Key];
                if( limFirstFix == null)
                   throw new ApplicationException("Missing " + name() + " fixing for "
                                                  + lim.Key );
                double? limSecondFix =
                IndexManager.instance().getHistory(name()).value()[lim.Value+1];
                if ( limSecondFix == null )
                   throw new ApplicationException("Missing " + name() + " fixing for "
                                                  + lim.Value+1 );
                double? limBefFirstFix =
                IndexManager.instance().getHistory(name()).value()[limBef.Key];
                if ( limBefFirstFix == null )
                   throw new ApplicationException("Missing " + name() + " fixing for "
                                                  + limBef.Key );
                double? limBefSecondFix =
                IndexManager.instance().getHistory(name()).value()[limBef.Value+1];
                if ( limBefSecondFix == null )
                   throw new ApplicationException("Missing " + name() + " fixing for "
                                                  + limBef.Value+1 );

                double linearNow = limFirstFix.Value + (limSecondFix.Value-limFirstFix.Value)*dl/dp;
                double linearBef = limBefFirstFix.Value + (limBefSecondFix.Value-limBefFirstFix.Value)*dlBef/dpBef;
                double wasYES = linearNow / linearBef - 1.0;

                return wasYES;

            } 
            else 
            {    
               // IS ratio, NOT interpolated
               double? pastFixing =
                    IndexManager.instance().getHistory(name()).value()[fixingDate];
               if ( pastFixing == null )
                  throw new ApplicationException("Missing " + name() + " fixing for "
                                                 + fixingDate);
                Date previousDate = fixingDate - new Period(1,TimeUnit.Years);
                double? previousFixing =
                IndexManager.instance().getHistory(name()).value()[previousDate];
                if( previousFixing == null )
                   throw new ApplicationException("Missing " + name() + " fixing for "
                                                  + previousDate );

                return pastFixing.Value/previousFixing.Value - 1.0;
            }
         } 
         else 
         {  
            // NOT ratio
            if (interpolated()) 
            { 
               // NOT ratio, IS interpolated
                KeyValuePair<Date,Date> lim = Utils.inflationPeriod(fixingDate, frequency_);
                double dp= lim.Value + 1 - lim.Key;
                double dl = fixingDate-lim.Key;
                double? limFirstFix =
                IndexManager.instance().getHistory(name()).value()[lim.Key];
                if ( limFirstFix == null )
                   throw new ApplicationException("Missing " + name() + " fixing for "
                                                  + lim.Key );
                double? limSecondFix =
                IndexManager.instance().getHistory(name()).value()[lim.Value+1];
                if ( limSecondFix == null )
                   throw new ApplicationException("Missing " + name() + " fixing for "
                                                  + lim.Value+1 );
                double linearNow = limFirstFix.Value + (limSecondFix.Value-limFirstFix.Value)*dl/dp;

                return linearNow;

            } 
            else 
            { 
               // NOT ratio, NOT interpolated
               // so just flat
                double? pastFixing =
                    IndexManager.instance().getHistory(name()).value()[fixingDate];
                if ( pastFixing == null ) 
                   throw new ApplicationException("Missing " + name() + " fixing for "
                                                  + fixingDate);
                return pastFixing.Value;

            }
         }

         // QL_FAIL("YoYInflationIndex::fixing, should never get here");

        }

        public bool ratio() { return ratio_; }
        public Handle<YoYInflationTermStructure> yoyInflationTermStructure() 
        { return yoyInflation_; }

        public YoYInflationIndex clone(Handle<YoYInflationTermStructure> h)
        {
            return new YoYInflationIndex(familyName_, region_, revised_,
                                         interpolated_, ratio_, frequency_,
                                         availabilityLag_, currency_, h);
        }
      
      
        private double forecastFixing(Date fixingDate)
        {
           Date d;
           if (interpolated()) 
           {
               d = fixingDate;
           } 
           else 
           {
               // if the value is not interpolated use the starting value
               // by internal convention this will be consistent
               KeyValuePair<Date,Date> lim = Utils.inflationPeriod(fixingDate, frequency_);
               d = lim.Key;
           }
           return yoyInflation_.link.yoyRate(d,new Period(0,TimeUnit.Days));
        }
        
      private bool ratio_;
      private Handle<YoYInflationTermStructure> yoyInflation_;
   }
}
