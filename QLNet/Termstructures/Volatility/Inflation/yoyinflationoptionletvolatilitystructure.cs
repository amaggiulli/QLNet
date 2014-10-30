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

   /*! Abstract interface ... no data, only results.

    Basically used to change the BlackVariance() methods to
    totalVariance.  Also deal with lagged observations of an index
    with a (usually different) availability lag.
    */

   public class YoYOptionletVolatilitySurface :  VolatilityTermStructure 
   {
      public YoYOptionletVolatilitySurface()
      : base (BusinessDayConvention.Following,null) {}

      //! \name Constructor
      //! calculate the reference date based on the global evaluation date
      public YoYOptionletVolatilitySurface(int settlementDays,
                                           Calendar cal,
                                           BusinessDayConvention bdc,
                                           DayCounter dc,
                                           Period observationLag,
                                           Frequency frequency,
                                           bool indexIsInterpolated)
         : base(settlementDays, cal, bdc, dc)
      {
         baseLevel_ = null;
         observationLag_ = observationLag;
         frequency_ = frequency;
         indexIsInterpolated_ = indexIsInterpolated;

      }


      //! \name Volatility (only)
      //@{
      //! Returns the volatility for a given maturity date and strike rate
      //! that observes inflation, by default, with the observation lag
      //! of the term structure.
      //! Because inflation is highly linked to dates (for interpolation, periods, etc)
      //! we do NOT provide a time version.
      public double volatility(Date maturityDate, double strike)
      {
         return volatility(maturityDate, strike, new Period(-1, TimeUnit.Days), false);
      }
      public double volatility(Date maturityDate, double strike, Period obsLag) 
      {
         return volatility(maturityDate,strike,obsLag,false) ;
      }

      public double volatility(Date maturityDate,double strike,Period obsLag, bool extrapolate) 
      {
         Period useLag = obsLag;
         if (obsLag == new Period(-1,TimeUnit.Days)) 
         {
            useLag = observationLag();
         }

        if (indexIsInterpolated()) 
        {
            checkRange(maturityDate-useLag, strike, extrapolate);
            double t = timeFromReference(maturityDate-useLag);
            return volatilityImpl(t,strike);
        } 
        else 
        {
            KeyValuePair<Date,Date> dd = Utils.inflationPeriod(maturityDate-useLag, frequency());
            checkRange(dd.Key, strike, extrapolate);
            double t = timeFromReference(dd.Key);
            return volatilityImpl(t,strike);
        }
      }

      public double volatility(Period optionTenor, double strike)
      {
         Date maturityDate = optionDateFromTenor(optionTenor);
         return volatility(maturityDate, strike, new Period(-1,TimeUnit.Days), false);
      }

      public double volatility(Period optionTenor, double strike, Period obsLag)
      {
         Date maturityDate = optionDateFromTenor(optionTenor);
         return volatility(maturityDate, strike, obsLag, false);
      }

      public double volatility(Period optionTenor,double strike,Period obsLag,bool extrapolate) 
      {
         Date maturityDate = optionDateFromTenor(optionTenor);
         return volatility(maturityDate, strike, obsLag, extrapolate);
      }

      //! Returns the total integrated variance for a given exercise date and strike rate.
      /*! Total integrated variance is useful because it scales out
       t for the optionlet pricing formulae.  Note that it is
       called "total" because the surface does not know whether
       it represents Black, Bachelier or Displaced Diffusion
       variance.  These are virtual so alternate connections
       between const vol and total var are possible.

       Because inflation is highly linked to dates (for interpolation, periods, etc)
       we do NOT provide a time version
       */
      public virtual double totalVariance(Date maturityDate, double strike)
      {
         return totalVariance(maturityDate, strike, new Period(-1, TimeUnit.Days), false);
      }
      public virtual double totalVariance(Date maturityDate, double strike, Period obsLag)
      {
         return totalVariance(maturityDate, strike, obsLag, false);
      }
      public virtual double totalVariance(Date maturityDate, double strike, Period obsLag, bool extrapolate)
      {
         double vol = volatility(maturityDate, strike, obsLag, extrapolate);
         double t = timeFromBase(maturityDate, obsLag);
         return vol * vol * t;
      }

      public virtual double totalVariance(Period tenor, double strike)
      {
         Date maturityDate = optionDateFromTenor(tenor);
         return totalVariance(maturityDate, strike, new Period(-1,TimeUnit.Days), false);
      }
      public virtual double totalVariance(Period tenor, double strike, Period obsLag)
      {
         Date maturityDate = optionDateFromTenor(tenor);
         return totalVariance(maturityDate, strike, obsLag, false);
      }
      public virtual double totalVariance(Period tenor, double strike, Period obsLag, bool extrap)
      {
         Date maturityDate = optionDateFromTenor(tenor);
         return totalVariance(maturityDate, strike, obsLag, extrap);
      }

      //! The TS observes with a lag that is usually different from the
      //! availability lag of the index.  An inflation rate is given,
      //! by default, for the maturity requested assuming this lag.
      public virtual Period observationLag()  { return observationLag_; }
      public virtual Frequency frequency() { return frequency_; }
      public virtual bool indexIsInterpolated() { return indexIsInterpolated_; }
      
      public virtual Date baseDate() 
      {

         // Depends on interpolation, or not, of observed index
         // and observation lag with which it was built.
         // We want this to work even if the index does not
         // have a yoy term structure.
         if (indexIsInterpolated()) 
         {
            return referenceDate() - observationLag();
         } 
         else 
         {
            return Utils.inflationPeriod(referenceDate() - observationLag(),
                                         frequency()).Key;
         }
      }

      //! base date will be in the past because of observation lag
      public virtual double timeFromBase(Date maturityDate)
      {
         return timeFromBase(maturityDate, new Period(-1, TimeUnit.Days));
      }

      //! needed for total variance calculations
      public virtual double timeFromBase(Date maturityDate,Period obsLag) 
      {

         Period useLag = obsLag;
         if (obsLag==new Period(-1,TimeUnit.Days)) 
         {
             useLag = observationLag();
         }

         Date useDate;
         if (indexIsInterpolated()) 
         {
             useDate = maturityDate - useLag;
         } 
         else 
         {
            useDate = Utils.inflationPeriod(maturityDate - useLag,frequency()).Key;
         }

         // This assumes that the inflation term structure starts
         // as late as possible given the inflation index definition,
         // which is the usual case.
         return dayCounter().yearFraction(baseDate(), useDate);
      }
      //@}
   
      //! \name Limits
      //@{
      //! the minimum strike for which the term structure can return vols
      public override double minStrike() { return  0 ;}
      //! the maximum strike for which the term structure can return vols
      public override double maxStrike() { return 0; }
      //@}
      
      // acts as zero time value for boostrapping
      public virtual double baseLevel() 
      {
         if (baseLevel_ == null)
            throw new ApplicationException("Base volatility, for baseDate(), not set.");
         return baseLevel_.Value;
      }


      protected virtual void checkRange(Date d, double strike,bool extrapolate) 
      {

         if ( d < baseDate() )
            throw new ApplicationException ("date (" + d + ") is before base date");

         if ( !extrapolate && !allowsExtrapolation() && d > maxDate())
            throw new ApplicationException ("date (" + d + ") is past max curve date ("
                                         + maxDate() + ")");


         if ( !extrapolate && !allowsExtrapolation() && 
              ( strike < minStrike() || strike > maxStrike()))
            throw new ApplicationException ("strike (" + strike + ") is outside the curve domain ["
                + minStrike() + "," + maxStrike()+ "]] at date = " + d);
      }
    
      protected virtual void checkRange(double t, double strike,bool extrapolate) 
      {
         if ( t < timeFromReference(baseDate()) )
            throw new ApplicationException("time (" + t + ") is before base date");

         if ( !extrapolate && !allowsExtrapolation() && t > maxTime() )
            throw new ApplicationException("time (" + t + ") is past max curve time ("
                                           + maxTime() + ")");

         if ( !extrapolate && !allowsExtrapolation() && 
              (strike < minStrike() || strike > maxStrike()))
            throw new ApplicationException("strike (" + strike + ") is outside the curve domain ["
                   + minStrike() + "," + maxStrike()+ "] at time = " + t);
    
      }


      //! Implements the actual volatility surface calculation in
      //! derived classes e.g. bilinear interpolation.  N.B. does
      //! not derive the surface.
      protected virtual double volatilityImpl(double length, double strike) { return 0; }


      // acts as zero time value for boostrapping
      protected virtual void setBaseLevel(double? v) { baseLevel_ = v; }
      protected double? baseLevel_;

      // so you do not need an index
      protected Period observationLag_;
      protected Frequency frequency_;
      protected bool indexIsInterpolated_;
   }

	//! Constant surface, no K or T dependence.
   public class ConstantYoYOptionletVolatility  : YoYOptionletVolatilitySurface 
	{
		
		//! \name Constructor
		//@{
		//! calculate the reference date based on the global evaluation date
		public ConstantYoYOptionletVolatility(double v,
														  int settlementDays,
														  Calendar cal,
															 BusinessDayConvention bdc,
															 DayCounter dc,
															 Period observationLag,
															Frequency frequency,
															bool indexIsInterpolated,
															double minStrike = -1.0,  // -100%
															double maxStrike = 100.0)  // +10,000%
			:base(settlementDays, cal, bdc, dc, observationLag, frequency, indexIsInterpolated)
		{
			volatility_ = v;
			minStrike_ = minStrike; 
			maxStrike_ = maxStrike; 
		}
      
        //@}

        //! \name Limits
        //@{
        public override Date maxDate()  { return Date.maxDate(); }
        //! the minimum strike for which the term structure can return vols
		  public override double minStrike() { return minStrike_; }
        //! the maximum strike for which the term structure can return vols
		  public override double maxStrike() { return maxStrike_; }
        //@}

    
        //! implements the actual volatility calculation in derived classes
		  protected override double volatilityImpl( double length, double strike )
		  {
			  return volatility_;
		  }

        protected double volatility_;
        protected double minStrike_, maxStrike_;
    };

}
