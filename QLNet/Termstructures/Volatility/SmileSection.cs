/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)
  
 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

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

namespace QLNet 
{
   //! interest rate volatility smile section
   /*! This abstract class provides volatility smile section interface */
   public abstract class SmileSection : LazyObject
   {
      public SmileSection( Date d, DayCounter dc = null, Date referenceDate = null,
         VolatilityType type = VolatilityType.ShiftedLognormal, double shift = 0.0 )
      {
         exerciseDate_ = d;
         dc_ = dc;
         volatilityType_ = type;
         shift_ = shift;

         isFloating_ = referenceDate == null;
         if ( isFloating_ )
         {
            Settings.registerWith( update );
            referenceDate_ = Settings.evaluationDate();
         }
         else
            referenceDate_ = referenceDate;
         initializeExerciseTime();
      }
      public SmileSection( double exerciseTime, DayCounter dc = null,
         VolatilityType type = VolatilityType.ShiftedLognormal, double shift = 0.0 )
      {
         isFloating_ = false;
         referenceDate_ = null;
         dc_ = dc;
         exerciseTime_ = exerciseTime;
         volatilityType_ = type;
         shift_ = shift;

         Utils.QL_REQUIRE( exerciseTime_ >= 0.0, () => "expiry time must be positive: " + exerciseTime_ + " not allowed" );
      }
      public SmileSection() { }


      public override void update()
      {
         if ( isFloating_ )
         {
            referenceDate_ = Settings.evaluationDate();
            initializeExerciseTime();
         }
      }
      public abstract double minStrike();
      public abstract double maxStrike();
      public double variance( double strike ) { return varianceImpl( strike ); }
      public double volatility( double strike ) { return volatilityImpl( strike ); }
      public abstract double? atmLevel();
      public virtual Date exerciseDate() { return exerciseDate_; }
      public virtual VolatilityType volatilityType() { return volatilityType_; }
      public virtual double shift() { return shift_; }
      public virtual Date referenceDate()
      {
         Utils.QL_REQUIRE( referenceDate_ != null, () => "referenceDate not available for this instance" );
         return referenceDate_;
      }
      public virtual double exerciseTime() { return exerciseTime_; }
      public virtual DayCounter dayCounter() { return dc_; }
      public virtual double optionPrice( double strike, Option.Type type = Option.Type.Call, double discount = 1.0 )
      {
         double? atm = atmLevel();
         Utils.QL_REQUIRE( atm != null, () => "smile section must provide atm level to compute option price" );
         // if lognormal or shifted lognormal,
         // for strike at -shift, return option price even if outside
         // minstrike, maxstrike interval
         if ( volatilityType() == VolatilityType.ShiftedLognormal )
            return Utils.blackFormula( type, strike, atm.Value, Math.Abs( strike + shift() ) < Const.QL_EPSILON ?
                            0.2 : Math.Sqrt( variance( strike ) ), discount, shift() );
         else
            return Utils.bachelierBlackFormula( type, strike, atm.Value, Math.Sqrt( variance( strike ) ), discount );
      }
      public virtual double digitalOptionPrice( double strike, Option.Type type = Option.Type.Call, double discount = 1.0,
         double gap = 1.0e-5 )
      {
         double m = volatilityType() == VolatilityType.ShiftedLognormal ? -shift() : -double.MaxValue;
         double kl = Math.Max( strike - gap / 2.0, m );
         double kr = kl + gap;
         return ( type == Option.Type.Call ? 1.0 : -1.0 ) *
            ( optionPrice( kl, type, discount ) - optionPrice( kr, type, discount ) ) / gap;
      }
      public virtual double vega( double strike, double discount = 1.0 )
      {
         double? atm = atmLevel();
         Utils.QL_REQUIRE( atm != null, () =>
                    "smile section must provide atm level to compute option vega" );
         if ( volatilityType() == VolatilityType.ShiftedLognormal )
            return Utils.blackFormulaVolDerivative( strike, atmLevel().Value,
                                             Math.Sqrt( variance( strike ) ),
                                             exerciseTime(), discount, shift() ) * 0.01;
         else
            Utils.QL_FAIL( "vega for normal smilesection not yet implemented" );

         return 0;

      }
      public virtual double density( double strike, double discount = 1.0, double gap = 1.0E-4 )
      {
         double m = volatilityType() == VolatilityType.ShiftedLognormal ? -shift() : -double.MaxValue;
         double kl = Math.Max( strike - gap / 2.0, m );
         double kr = kl + gap;
         return ( digitalOptionPrice( kl, Option.Type.Call, discount, gap ) -
                digitalOptionPrice( kr, Option.Type.Call, discount, gap ) ) / gap;
      }
      public double volatility( double strike, VolatilityType volatilityType, double shift = 0.0 )
      {

         if ( volatilityType == volatilityType_ && Utils.close( shift, this.shift() ) )
            return volatility( strike );
         double? atm = atmLevel();
         Utils.QL_REQUIRE( atm != null, () => "smile section must provide atm level to compute converted volatilties" );
         Option.Type type = strike >= atm ? Option.Type.Call : Option.Type.Put;
         double premium = optionPrice( strike, type );
         double premiumAtm = optionPrice( atm.Value, type );
         if ( volatilityType == VolatilityType.ShiftedLognormal )
         {
            try
            {
               return Utils.blackFormulaImpliedStdDev( type, strike, atm.Value, premium, 1.0, shift ) /
                  Math.Sqrt( exerciseTime() );
            }
            catch ( Exception )
            {
               return Utils.blackFormulaImpliedStdDevChambers( type, strike, atm.Value, premium, premiumAtm, 1.0, shift ) /
                  Math.Sqrt( exerciseTime() );
            }
         }
         else
         {
            return Utils.bachelierBlackFormulaImpliedVol( type, strike, atm.Value, exerciseTime(), premium );
         }
      }

      protected virtual void initializeExerciseTime()
      {
         Utils.QL_REQUIRE( exerciseDate_ >= referenceDate_, () =>
                   "expiry date (" + exerciseDate_ +
                   ") must be greater than reference date (" +
                   referenceDate_ + ")" );
         exerciseTime_ = dc_.yearFraction( referenceDate_, exerciseDate_ );
      }
      protected virtual double varianceImpl( double strike )
      {
         double v = volatilityImpl( strike );
         return v * v * exerciseTime();
      }
      protected abstract double volatilityImpl( double strike );


      private bool isFloating_;
      private Date referenceDate_;
      private Date exerciseDate_;
      private DayCounter dc_;
      private double exerciseTime_;
      private VolatilityType volatilityType_;
      private double shift_;

      //#region Observable & Observer
      //public event Callback notifyObserversEvent;
      //public void registerWith( Callback handler ) { notifyObserversEvent += handler; }
      //public void unregisterWith( Callback handler ) { notifyObserversEvent -= handler; }
      //protected void notifyObservers()
      //{
      //   Callback handler = notifyObserversEvent;
      //   if ( handler != null )
      //   {
      //      handler();
      //   }
      //}

      //#endregion
   }
    public class SabrSmileSection : SmileSection {
        private double alpha_, beta_, nu_, rho_, forward_;

        public SabrSmileSection(double timeToExpiry, double forward, List<double> sabrParams)
            : base(timeToExpiry) {
            forward_ = forward;

            alpha_ = sabrParams[0];
            beta_ = sabrParams[1];
            nu_ = sabrParams[2];
            rho_ = sabrParams[3];

            if (!(forward_>0.0))
                throw new Exception("at the money forward rate must be: " + forward_ + " not allowed");
            Utils.validateSabrParameters(alpha_, beta_, nu_, rho_);
        }

        public SabrSmileSection( Date d, double forward, List<double> sabrParams, DayCounter dc = null )
            : base(d, dc ?? new Actual365Fixed()) {
            forward_ = forward;

            alpha_ = sabrParams[0];
            beta_ = sabrParams[1];
            nu_ = sabrParams[2];
            rho_ = sabrParams[3];

            if (!(forward_>0.0))
                throw new Exception("at the money forward rate must be: " + forward_ + " not allowed");
            Utils.validateSabrParameters(alpha_, beta_, nu_, rho_);
        }

        public override double minStrike () { return 0.0; }
        public override double maxStrike() { return double.MaxValue; }
        public override double? atmLevel() { return forward_; }
        
        protected override double varianceImpl(double strike) {
            double vol = Utils.unsafeSabrVolatility(strike, forward_, exerciseTime(), alpha_, beta_, nu_, rho_);
            return vol*vol*exerciseTime();
         }

         protected override double volatilityImpl(double strike) {
            return Utils.unsafeSabrVolatility(strike, forward_, exerciseTime(), alpha_, beta_, nu_, rho_);
         }
    }
}
