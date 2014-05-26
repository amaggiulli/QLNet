/*
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008, 2009 , 2010  Andrea Maggiulli (a.maggiulli@gmail.com)
  
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

namespace QLNet {
    public static partial class Utils {
        //! utility function giving the inflation period for a given date
        public static KeyValuePair<Date, Date> inflationPeriod(Date d, Frequency frequency) {
            Month month = (Month)d.Month;
            int year = d.Year;

            Month startMonth;
            Month endMonth;
            switch (frequency) {
                case Frequency.Annual:
                    startMonth = Month.January;
                    endMonth = Month.December;
                    break;
                case Frequency.Semiannual:
                    startMonth = (Month)(6 * ((int)month - 1) / 6 + 1);
                    endMonth = (Month)(startMonth + 5);
                    break;
                case Frequency.Quarterly:
                    startMonth = (Month)(3 * ((int)month - 1) / 3 + 1);
                    endMonth = (Month)(startMonth + 2);
                    break;
                case Frequency.Monthly:
                    startMonth = endMonth = month;
                    break;
                default:
                    throw new ApplicationException("Frequency not handled: " + frequency);
            }

            Date startDate = new Date(1, startMonth, year);
            Date endDate = Date.endOfMonth(new Date(1, endMonth, year));

            return new KeyValuePair<Date, Date>(startDate, endDate);
        }

       public static double inflationYearFraction(Frequency f, bool indexIsInterpolated,
                                    DayCounter dayCounter,
                                    Date d1, Date d2) 
       {
          double t=0;
          if (indexIsInterpolated) 
          {
            // N.B. we do not use linear interpolation between flat
            // fixing forecasts for forecasts.  This avoids awkwardnesses
            // when bootstrapping the inflation curve.
            t = dayCounter.yearFraction(d1, d2);
          } 
          else 
          {
            // I.e. fixing is constant for the whole inflation period.
            // Use the value for half way along the period.
            // But the inflation time is the time between period starts
            KeyValuePair<Date,Date> limD1 = inflationPeriod(d1, f);
            KeyValuePair<Date,Date> limD2 = inflationPeriod(d2, f);
            t = dayCounter.yearFraction(limD1.Key, limD2.Key);
          }
          return t;
       }
    }

   //! Interface for inflation term structures.
   //! \ingroup inflationtermstructures 
   public class InflationTermStructure : TermStructure 
   {
      public InflationTermStructure() { }

      //! \name Constructors
      //@{
      public InflationTermStructure(double baseRate,
                                     Period observationLag,
                                     Frequency frequency,
                                     bool indexIsInterpolated,
                                     Handle<YieldTermStructure> yTS,
                                     DayCounter dayCounter = null,
                                     Seasonality seasonality = null)
         :base(dayCounter)
      {
         nominalTermStructure_ = yTS;
         observationLag_ = observationLag;
         frequency_ = frequency;
         indexIsInterpolated_ = indexIsInterpolated;
         baseRate_= baseRate;
         nominalTermStructure_.registerWith(update);
         setSeasonality(seasonality);
      }

   public InflationTermStructure(Date referenceDate,
                                 double baseRate,
                                 Period observationLag,
                                 Frequency frequency,
                                 bool indexIsInterpolated,
                                 Handle<YieldTermStructure> yTS,
                                 Calendar calendar,
                                 DayCounter dayCounter = null,
                                 Seasonality seasonality = null)
      : base(referenceDate, calendar, dayCounter)
   {
      nominalTermStructure_ = yTS;
      observationLag_ = observationLag;
      frequency_ = frequency;
      indexIsInterpolated_ = indexIsInterpolated;
      baseRate_ = baseRate;
      nominalTermStructure_.registerWith(update);
      setSeasonality(seasonality);
   }

   public InflationTermStructure(int settlementDays,
                              Calendar calendar,
                              double baseRate,
                              Period observationLag,
                              Frequency frequency,
                              bool indexIsInterpolated,
                              Handle<YieldTermStructure> yTS,
                              DayCounter dayCounter = null,
                              Seasonality seasonality = null)
      : base(settlementDays, calendar, dayCounter)
   {
      nominalTermStructure_ = yTS;
      observationLag_ = observationLag;
      frequency_ = frequency;
      indexIsInterpolated_ = indexIsInterpolated;
      baseRate_ = baseRate;
      nominalTermStructure_.registerWith(update);
      setSeasonality(seasonality);
   }
   //@}

      //! \name Inflation interface
      //@{
      //! The TS observes with a lag that is usually different from the
      //! availability lag of the index.  An inflation rate is given,
      //! by default, for the maturity requested assuming this lag.
      public virtual Period observationLag() { return observationLag_; }
      public virtual Frequency frequency() { return frequency_; }
      public virtual bool indexIsInterpolated() { return indexIsInterpolated_; }
      public virtual double baseRate() { return baseRate_; }
      public virtual Handle<YieldTermStructure> nominalTermStructure() 
         { return nominalTermStructure_; }

      //! minimum (base) date
      /*! Important in inflation since it starts before nominal
          reference date.  Changes depending whether index is
          interpolated or not.  When interpolated the base date
          is just observation lag before nominal.  When not
          interpolated it is the beginning of the relevant period
          (hence it is easy to create interpolated fixings from
           a not-interpolated curve because interpolation, usually,
           of fixings is forward looking).
      */
      public virtual Date baseDate() { return null; }
      //@}

      //! Functions to set and get seasonality.
      /*! Calling setSeasonality with no arguments means unsetting
          as the default is used to choose unsetting.
      */
      public void setSeasonality(Seasonality seasonality = null)
      {
         // always reset, whether with null or new pointer
         seasonality_ = seasonality;
         if (seasonality_ != null) 
         {
            if (!seasonality_.isConsistent(this))
               throw new ApplicationException("Seasonality inconsistent with " +
                                             "inflation term structure");
         }
         notifyObservers();
      }

      public Seasonality seasonality() { return seasonality_; }
      public bool hasSeasonality() { return seasonality_ != null; }


      protected Handle<YieldTermStructure> nominalTermStructure_;
      protected Period observationLag_;
      protected Frequency frequency_;
      protected bool indexIsInterpolated_;
      protected double baseRate_;

      // This next part is required for piecewise- constructors
      // because, for inflation, they need more than just the
      // instruments to build the term structure, since the rate at
      // time 0-lag is non-zero, since we deal (effectively) with
      // "forwards".
      protected virtual void setBaseRate(double r){ baseRate_ = r; }

      // range-checking
      protected override void checkRange(Date d,bool extrapolate)
      {
         if (d < baseDate())
            throw new ApplicationException("date (" + d + ") is before base date");

         if (!extrapolate && allowsExtrapolation() && d > maxDate())
            throw new ApplicationException("date (" + d + ") is past max curve date ("
                                            + maxDate() + ")");
      }

      private Seasonality seasonality_;
   }


    //! Interface for zero inflation term structures.
    // Child classes use templates but do not want that exposed to
    // general users.
    public class ZeroInflationTermStructure : InflationTermStructure 
    {
       public ZeroInflationTermStructure() { }

       //! \name Constructors
       //@{
       public ZeroInflationTermStructure(DayCounter dayCounter,
                                         double baseZeroRate,
                                         Period observationLag,
                                         Frequency frequency,
                                         bool indexIsInterpolated,
                                         Handle<YieldTermStructure> yTS,
                                         Seasonality seasonality = null)
          :base(baseZeroRate, observationLag, frequency, indexIsInterpolated,
                yTS, dayCounter, seasonality)
       {}
      
       public ZeroInflationTermStructure(Date referenceDate,
                                          Calendar calendar,
                                          DayCounter dayCounter,
                                          double baseZeroRate,
                                          Period observationLag,
                                          Frequency frequency,
                                          bool indexIsInterpolated,
                                          Handle<YieldTermStructure> yTS,
                                          Seasonality seasonality = null)
           : base(referenceDate, baseZeroRate, observationLag, frequency, indexIsInterpolated,
                  yTS, calendar, dayCounter, seasonality) {}

       public ZeroInflationTermStructure(int settlementDays,
                                         Calendar calendar,
                                         DayCounter dayCounter,
                                         double baseZeroRate,
                                         Period observationLag,
                                         Frequency frequency,
                                         bool indexIsInterpolated,
                                         Handle<YieldTermStructure> yTS,
                                         Seasonality seasonality = null)
          :base(settlementDays, calendar, baseZeroRate, observationLag, frequency, 
                indexIsInterpolated, yTS, dayCounter, seasonality) { }
       //@}


       //! \name Inspectors
       //@{
       //! zero-coupon inflation rate for an instrument with maturity (pay date) d
       //! that observes with given lag and interpolation.
       //! Since inflation is highly linked to dates (lags, interpolation, months for seasonality, etc)
       //! we do NOT provide a "time" version of the rate lookup.
       /*! Essentially the fair rate for a zero-coupon inflation swap
           (by definition), i.e. the zero term structure uses yearly
           compounding, which is assumed for ZCIIS instrument quotes.
           N.B. by default you get the same as lag and interpolation
           as the term structure.
           If you want to get predictions of RPI/CPI/etc then use an
           index.
       */
       public double zeroRate(Date d) 
       {
          return zeroRate(d, new Period(-1,TimeUnit.Days),false,false) ;
       }
       public double zeroRate(Date d, Period instObsLag)
       {
          return zeroRate(d, instObsLag,false, false) ;
       }
       public double zeroRate(Date d, Period instObsLag,bool forceLinearInterpolation)
       {
          return zeroRate(d,instObsLag,forceLinearInterpolation,false);
       }

       public double zeroRate(Date d, Period instObsLag,
                              bool forceLinearInterpolation,
                              bool extrapolate)
       {
          Period useLag = instObsLag;
          if (instObsLag == new Period(-1,TimeUnit.Days)) 
          {
             useLag = observationLag();
          }
         
          double zeroRate;
          if (forceLinearInterpolation) 
          {
            KeyValuePair<Date,Date> dd = Utils.inflationPeriod(d-useLag, frequency());
            Date ddValue = dd.Value + new Period(1,TimeUnit.Days);
            double dp = ddValue - dd.Key;
            double dt = d - dd.Key;
            // if we are interpolating we only check the exact point
            // this prevents falling off the end at curve maturity
            base.checkRange(d, extrapolate);
            double t1 = timeFromReference(dd.Key);
            double t2 = timeFromReference(ddValue);
            zeroRate = zeroRateImpl(t1) + zeroRateImpl(t2) * (dt/dp);
          } 
          else 
          {
             if (indexIsInterpolated()) 
             {
                base.checkRange(d-useLag, extrapolate);
                double t = timeFromReference(d-useLag);
                zeroRate = zeroRateImpl(t);
             } 
             else 
             {
                KeyValuePair<Date,Date> dd = Utils.inflationPeriod(d-useLag, frequency());
                base.checkRange(dd.Key, extrapolate);
                double t = timeFromReference(dd.Key);
                zeroRate = zeroRateImpl(t);
            }
        }

        if (hasSeasonality()) 
        {
            zeroRate = seasonality().correctZeroRate(d-useLag, zeroRate, this);
        }
        
        
          return zeroRate;
       }

       //@}
   
       //! to be defined in derived classes
       protected virtual double zeroRateImpl(double t) {return 0;}
   
   }


   
   //! Base class for year-on-year inflation term structures.
    public class YoYInflationTermStructure : InflationTermStructure
    {
       public YoYInflationTermStructure() { }
       //! \name Constructors
       //@{
       public YoYInflationTermStructure(DayCounter dayCounter,
                                        double baseYoYRate,
                                        Period observationLag,
                                        Frequency frequency,
                                        bool indexIsInterpolated,
                                        Handle<YieldTermStructure> yTS,
                                        Seasonality seasonality = null)
          : base(baseYoYRate, observationLag, frequency, indexIsInterpolated,
                yTS, dayCounter, seasonality) { }

       public YoYInflationTermStructure(Date referenceDate,
                                        Calendar calendar,
                                        DayCounter dayCounter,
                                        double baseYoYRate,
                                        Period observationLag,
                                        Frequency frequency,
                                        bool indexIsInterpolated,
                                        Handle<YieldTermStructure> yTS,
                                        Seasonality seasonality = null)
          : base(referenceDate, baseYoYRate, observationLag, frequency, indexIsInterpolated,
                              yTS, calendar, dayCounter, seasonality) { }

		 public YoYInflationTermStructure(int settlementDays,
                                        Calendar calendar,
                                        DayCounter dayCounter,
                                        double baseYoYRate,
                                        Period observationLag,
                                        Frequency frequency,
                                        bool indexIsInterpolated,
                                        Handle<YieldTermStructure> yTS,
                                        Seasonality seasonality = null)
          : base(settlementDays, calendar, baseYoYRate, observationLag,
                              frequency, indexIsInterpolated,
                              yTS, dayCounter, seasonality) { }
       //@}

       //! \name Inspectors
       //@{
       //! year-on-year inflation rate, forceLinearInterpolation
       //! is relative to the frequency of the TS.
       //! Since inflation is highly linked to dates (lags, interpolation, months for seasonality etc)
       //! we do NOT provide a "time" version of the rate lookup.
       /*! \note this is not the year-on-year swap (YYIIS) rate. */
       public double yoyRate(Date d)
       {
          return yoyRate(d, new Period(-1, TimeUnit.Days), false, false);
       }
       public double yoyRate(Date d, Period instObsLag)
       {
          return yoyRate(d, instObsLag, false, false);
       }
       public double yoyRate(Date d, Period instObsLag, bool forceLinearInterpolation)
       {
          return yoyRate(d, instObsLag, forceLinearInterpolation, false);
       }

       public double yoyRate(Date d, Period instObsLag, bool forceLinearInterpolation,
                             bool extrapolate)
       {
          Period useLag = instObsLag;
          if (instObsLag == new Period(-1, TimeUnit.Days))
          {
             useLag = observationLag();
          }

          double yoyRate;
          if (forceLinearInterpolation)
          {
             KeyValuePair<Date, Date> dd = Utils.inflationPeriod(d - useLag, frequency());
             Date ddValue = dd.Value + new Period(1, TimeUnit.Days);
             double dp = ddValue - dd.Key;
             double dt = (d - useLag) - dd.Key;
             // if we are interpolating we only check the exact point
             // this prevents falling off the end at curve maturity
             base.checkRange(d, extrapolate);
             double t1 = timeFromReference(dd.Key);
             double t2 = timeFromReference(dd.Value);
             yoyRate = yoyRateImpl(t1) + (yoyRateImpl(t2) - yoyRateImpl(t1)) * (dt / dp);
          }
          else
          {
             if (indexIsInterpolated())
             {
                base.checkRange(d - useLag, extrapolate);
                double t = timeFromReference(d - useLag);
                yoyRate = yoyRateImpl(t);
             }
             else
             {
                KeyValuePair<Date, Date> dd = Utils.inflationPeriod(d - useLag, frequency());
                base.checkRange(dd.Key, extrapolate);
                double t = timeFromReference(dd.Key);
                yoyRate = yoyRateImpl(t);
             }
          }

          if (hasSeasonality())
          {
             yoyRate = seasonality().correctYoYRate(d - useLag, yoyRate, this);
          }
          return yoyRate;
       }
       //@}

       //! to be defined in derived classes
       protected virtual double yoyRateImpl(double time) { return 0; }

    }


}
