/*
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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QLNet {
    //! interest rate volatility smile section
    /*! This abstract class provides volatility smile section interface */
    public abstract class SmileSection : IObservable, IObserver {
        private bool isFloating_;
        private Date referenceDate_;

        private DayCounter dc_;
        public DayCounter dayCounter() { return dc_; }

        private Date exerciseDate_;
        public Date exerciseDate() { return exerciseDate_; }

        private double exerciseTime_;
        public double exerciseTime() { return exerciseTime_; }

        #region ctors
		public SmileSection() {}

        // public SmileSection(Date d, DayCounter dc = DayCounter(), Date referenceDate = Date())
        public SmileSection(Date d, DayCounter dc) : this(d, dc, null) { }
        public SmileSection(Date d, DayCounter dc, Date referenceDate) {
            exerciseDate_ = d;
            dc_ = dc;

            isFloating_ = referenceDate == null;
            
            if (isFloating_) {
                Settings.registerWith(update);
                referenceDate_ = Settings.evaluationDate();
            } else
                referenceDate_ = referenceDate;

            initializeExerciseTime();
        }

        public SmileSection(double exerciseTime) : this(exerciseTime, new DayCounter()) { }
        public SmileSection(double exerciseTime, DayCounter dc) {
            isFloating_ = false;
            dc_ = dc;
            exerciseTime_ = exerciseTime;

            if (!(exerciseTime_>=0.0))
                throw new ApplicationException("expiry time must be positive: " + exerciseTime_ + " not allowed");
        }
    	#endregion


        public double variance(double strike) {
            if (strike == default(double))
                strike = atmLevel();
            return varianceImpl(strike);
        }

        public double volatility(double strike) {
            if (strike == default(double))
                strike = atmLevel();
            return volatilityImpl(strike);
        }

        public void initializeExerciseTime() {
            if (!(exerciseDate_>=referenceDate_))
                throw new ApplicationException("expiry date (" + exerciseDate_ + ") must be greater than reference date (" +
                       referenceDate_ + ")");
            exerciseTime_ = dc_.yearFraction(referenceDate_, exerciseDate_);
        }

        public abstract double minStrike();
        public abstract double maxStrike();
        public abstract double atmLevel();

        protected virtual double varianceImpl(double strike) {
            double v = volatilityImpl(strike);
            return v * v * exerciseTime();
        }
        protected abstract double volatilityImpl(double k);


        #region Observable & Observer
        public event Callback notifyObserversEvent;
        public void registerWith(Callback handler) { notifyObserversEvent += handler; }
        public void unregisterWith(Callback handler) { notifyObserversEvent -= handler; }
        protected void notifyObservers() {
            Callback handler = notifyObserversEvent;
            if (handler != null) {
                handler();
            }
        }

        // observer
        public virtual void update() {
            if (isFloating_) {
                referenceDate_ = Settings.evaluationDate();
                initializeExerciseTime();
            }
            //LazyObject::update();
        } 
        #endregion
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
                throw new ApplicationException("at the money forward rate must be: " + forward_ + " not allowed");
            Utils.validateSabrParameters(alpha_, beta_, nu_, rho_);
        }

        // public SabrSmileSection(Date d, double forward, List<double> sabrParameters, DayCounter dc = Actual365Fixed());
        public SabrSmileSection(Date d, double forward, List<double> sabrParams, DayCounter dc)
            : base(d, dc) {
            forward_ = forward;

            alpha_ = sabrParams[0];
            beta_ = sabrParams[1];
            nu_ = sabrParams[2];
            rho_ = sabrParams[3];

            if (!(forward_>0.0))
                throw new ApplicationException("at the money forward rate must be: " + forward_ + " not allowed");
            Utils.validateSabrParameters(alpha_, beta_, nu_, rho_);
        }

        public override double minStrike () { return 0.0; }
        public override double maxStrike() { return double.MaxValue; }
        public override double atmLevel() { return forward_; }
        
        protected override double varianceImpl(double strike) {
            double vol = Utils.unsafeSabrVolatility(strike, forward_, exerciseTime(), alpha_, beta_, nu_, rho_);
            return vol*vol*exerciseTime();
         }

         protected override double volatilityImpl(double strike) {
            return Utils.unsafeSabrVolatility(strike, forward_, exerciseTime(), alpha_, beta_, nu_, rho_);
         }
    }
}
