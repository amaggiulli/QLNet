/*
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

namespace QLNet
{
   //! A transformation of an existing inflation swap rate.
   /*! This is an abstract class and contains the functions
       correctXXXRate which returns rates with the seasonality
       correction.  Currently only the price multiplicative version
       is implemented, but this covers stationary (1-year) and
       non-stationary (multi-year) seasonality depending on how many
       years of factors are given.  Seasonality is piecewise
       constant, hence it will work with un-interpolated inflation
       indices.

       A seasonality assumption can be used to fill in inflation swap
       curves between maturities that are usually given in integer
       numbers of years, e.g. 8,9,10,15,20, etc.  Historical
       seasonality may be observed in reported CPI values,
       alternatively it may be affected by known future events, e.g.
       announced changes in VAT rates.  Thus seasonality may be
       stationary or non-stationary.

       If seasonality is additive then both swap rates will show
       affects.  Additive seasonality is not implemented.
   */
   public class Seasonality
   {
      //! \name Seasonality interface
      //@{
      public virtual double correctZeroRate(Date d, double r,InflationTermStructure iTS) 
      {
         return 0;
      }
        
      public virtual double correctYoYRate(Date d, double r,InflationTermStructure iTS) 
      {
         return 0;
      }
        
      /*! It is possible for multi-year seasonalities to be
          inconsistent with the inflation term structure they are
          given to.  This method enables testing - but programmers
          are not required to implement it.  E.g. for price
          seasonality the corrections at whole years after the
          inflation curve base date should be the same or else there
          can be an inconsistency with quoted instruments.
          Alternatively, the seasonality can be set _before_ the
          inflation curve is bootstrapped.
      */
      public virtual bool isConsistent(InflationTermStructure iTS)
      {
         return true;
      }
      //@}
   }

   //! Multiplicative seasonality in the price index (CPI/RPI/HICP/etc).

   /*! Stationary multiplicative seasonality in CPI/RPI/HICP (i.e. in
     price) implies that zero inflation swap rates are affected,
     but that year-on-year inflation swap rates show no effect.  Of
     course, if the seasonality in CPI/RPI/HICP is non-stationary
     then both swap rates will be affected.

     Factors must be in multiples of the minimum required for one
     year, e.g. 12 for monthly, and these factors are reused for as
     long as is required, i.e. they wrap around.  So, for example,
     if 24 factors are given this repeats every two years.  True
     stationary seasonality can be obtained by giving the same
     number of factors as the frequency dictates e.g. 12 for
     monthly seasonality.

     \warning Multi-year seasonality (i.e. non-stationary) is
              fragile: the user <b>must</b> ensure that corrections
              at whole years before and after the inflation term
              structure base date are the same.  Otherwise there
              can be an inconsistency with quoted rates.  This is
              enforced if the frequency is lower than daily.  This
              is not enforced for daily seasonality because this
              will always be inconsistent due to weekends,
              holidays, leap years, etc.  If you use multi-year
              daily seasonality it is up to you to check.

     \note Factors are normalized relative to their appropriate
           reference dates.  For zero inflation this is the
           inflation curve true base date: since you have a fixing
           for that date the seasonality factor must be one.  For
           YoY inflation the reference is always one year earlier.

     Seasonality is treated as piecewise constant, hence it works
     correctly with uninterpolated indices if the seasonality
     correction factor frequency is the same as the index frequency
     (or less).
   */
   
   public class MultiplicativePriceSeasonality : Seasonality
   {
      private Date seasonalityBaseDate_;
      private Frequency frequency_;
      private List<double> seasonalityFactors_;

      //Constructors
      //
      public MultiplicativePriceSeasonality() { }

      public MultiplicativePriceSeasonality(Date seasonalityBaseDate, Frequency frequency,
                                            List<double> seasonalityFactors)
      {
         set(seasonalityBaseDate, frequency, seasonalityFactors);
      }


      public virtual void set(Date seasonalityBaseDate, Frequency frequency,
                              List<double> seasonalityFactors)
      {

         frequency_ = frequency;
         seasonalityFactors_ = new List<double>(seasonalityFactors.Count);
        
         for(int i=0; i<seasonalityFactors.Count; i++) 
         {
            seasonalityFactors_.Add(seasonalityFactors[i]);
         }

         seasonalityBaseDate_ = seasonalityBaseDate;
         validate();
      }

      //! inspectors
      //@{
      public virtual Date seasonalityBaseDate() { return seasonalityBaseDate_; }
      public virtual Frequency frequency() { return frequency_; }
      public virtual List<double> seasonalityFactors() { return seasonalityFactors_; }
      //! The factor returned is NOT normalized relative to ANYTHING.
      public virtual double seasonalityFactor(Date to)
      {
         Date from = seasonalityBaseDate();
         Frequency factorFrequency = frequency();
         int nFactors = seasonalityFactors().Count;
         Period factorPeriod = new Period(factorFrequency);
         int which = 0;
         if (from==to) 
         {
            which = 0;
         } 
         else 
         {
            // days, weeks, months, years are the only time unit possibilities
            int diffDays = Math.Abs(to - from);  // in days
            int dir = 1;
            if(from > to)dir = -1;
            int diff;
            if (factorPeriod.units() == TimeUnit.Days)
            {
                diff = dir*diffDays;
            }
            else if (factorPeriod.units() == TimeUnit.Weeks)
            {
                diff = dir * (diffDays / 7);
            }
            else if (factorPeriod.units() == TimeUnit.Months)
            {
                KeyValuePair<Date,Date> lim = Utils.inflationPeriod(to, factorFrequency);
                diff = diffDays / (31*factorPeriod.length());
                Date go = from + dir*diff*factorPeriod;
                while ( !(lim.Key <= go && go <= lim.Value) ) 
                {
                    go += dir*factorPeriod;
                    diff++;
                }
                diff=dir*diff;
            }
            else if (factorPeriod.units() == TimeUnit.Years) 
            {
                throw new ApplicationException(
                   "seasonality period time unit is not allowed to be : " + factorPeriod.units());
            } 
            else 
            {
                throw new ApplicationException("Unknown time unit: " + factorPeriod.units());
            }
            // now adjust to the available number of factors, direction dependent

            if (dir==1) 
            {
                which = diff % nFactors;
            } 
            else 
            {
                which = (nFactors - (-diff % nFactors)) % nFactors;
            }
        }
        return seasonalityFactors()[which];
      }
      //@}

      //! \name Seasonality interface
      //@{

      public override double correctZeroRate(Date d,double r,InflationTermStructure iTS)
      {
        KeyValuePair<Date,Date> lim = Utils.inflationPeriod(iTS.baseDate(), iTS.frequency());
        Date curveBaseDate = lim.Value;
        return seasonalityCorrection(r, d, iTS.dayCounter(), curveBaseDate, true);

      }
      
      public override double correctYoYRate(Date d, double r, InflationTermStructure iTS)
      {
         KeyValuePair<Date,Date> lim = Utils.inflationPeriod(iTS.baseDate(), iTS.frequency());
         Date curveBaseDate = lim.Value;
         return seasonalityCorrection(r, d, iTS.dayCounter(), curveBaseDate, false);
      }

      public override bool isConsistent(InflationTermStructure iTS) 
      {

         // If multi-year is the specification consistent with the term structure start date?
         // We do NOT test daily seasonality because this will, in general, never be consistent
         // given weekends, holidays, leap years, etc.
         if(this.frequency() == Frequency.Daily) return true;
         if( (int)this.frequency() == seasonalityFactors().Count ) return true;

         // how many years do you need to test?
         int nTest = seasonalityFactors().Count / (int)this.frequency();
         // ... relative to the start of the inflation curve
         KeyValuePair<Date,Date> lim = Utils.inflationPeriod(iTS.baseDate(), iTS.frequency());
         Date curveBaseDate = lim.Value;
         double factorBase = this.seasonalityFactor(curveBaseDate);

         double eps = 0.00001;
         for (int i = 1; i < nTest; i++) 
         {
            double factorAt = this.seasonalityFactor(curveBaseDate+new Period(i,TimeUnit.Years));
            if (Math.Abs(factorAt-factorBase)>=eps)
               throw new ApplicationException("seasonality is inconsistent with inflation " +
                        "term structure, factors " + factorBase + " and later factor " 
                        + factorAt + ", " + i + " years later from inflation curve "
                        + " with base date at " + curveBaseDate);
         }

         return true;

      }
      //@}
      protected virtual void validate()
      {
         switch (this.frequency()) 
         {
            case Frequency.Semiannual:        //2
            case Frequency.EveryFourthMonth:  //3
            case Frequency.Quarterly:         //4
            case Frequency.Bimonthly:         //6
            case Frequency.Monthly:           //12
            case Frequency.Biweekly:          // etc.
            case Frequency.Weekly:
            case Frequency.Daily:
               if ((this.seasonalityFactors().Count % (int)this.frequency()) != 0)
                  throw new ApplicationException(
                           "For frequency " + this.frequency()
                           + " require multiple of " + ((int)this.frequency()) + " factors "
                           + this.seasonalityFactors().Count + " were given.");
            break;
            default:
               throw new ApplicationException("bad frequency specified: " + this.frequency()
                        + ", only semi-annual through daily permitted.");
        }

      }
      protected virtual double seasonalityCorrection(double rate, Date atDate, DayCounter dc,
                                                   Date curveBaseDate, bool isZeroRate)
      {
         // need _two_ corrections in order to get: seasonality = factor[atDate-seasonalityBase] / factor[reference-seasonalityBase]
         // i.e. for ZERO inflation rates you have the true fixing at the curve base so this factor must be normalized to one
         //      for YoY inflation rates your reference point is the year before

         double factorAt = this.seasonalityFactor(atDate);

         //Getting seasonality correction for either ZC or YoY
         double f;
         if (isZeroRate)
         {
            double factorBase = this.seasonalityFactor(curveBaseDate);
            double seasonalityAt = factorAt / factorBase;
            double timeFromCurveBase = dc.yearFraction(curveBaseDate, atDate);
            f = Math.Pow(seasonalityAt, 1 / timeFromCurveBase);
         }
         else
         {
            double factor1Ybefore = this.seasonalityFactor(atDate - new Period(1, TimeUnit.Years));
            f = factorAt / factor1Ybefore;
         }

         return (rate + 1) * f - 1;
      }
   }
}
