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
    //! liquid market instrument used during calibration
    public abstract class CalibrationHelper : IObserver, IObservable {
        protected double marketValue_;
        public double marketValue() { return marketValue_; }

        protected Handle<Quote> volatility_;
        protected Handle<YieldTermStructure> termStructure_;
        protected IPricingEngine engine_;

        private bool calibrateVolatility_;

        //public CalibrationHelper(Handle<Quote> volatility, Handle<YieldTermStructure> termStructure,
        //                  bool calibrateVolatility = false)
        public CalibrationHelper(Handle<Quote> volatility, Handle<YieldTermStructure> termStructure, bool calibrateVolatility) {
            volatility_ = volatility;
            termStructure_ = termStructure;
            calibrateVolatility_ = calibrateVolatility;

            volatility_.registerWith(update);
            termStructure_.registerWith(update);
        }

        //! returns the price of the instrument according to the model
        public abstract double modelValue();

        //! returns the error resulting from the model valuation
        public virtual double calibrationError() {
            if (calibrateVolatility_) {
                double lowerPrice = blackPrice(0.001);
                double upperPrice = blackPrice(10);
                double modelPrice = modelValue();

                double implied;
                if (modelPrice <= lowerPrice)
                    implied = 0.001;
                else
                    if (modelPrice >= upperPrice)
                        implied = 10.0;
                    else
                        implied = this.impliedVolatility(modelPrice, 1e-12, 5000, 0.001, 10);

                return implied - volatility_.link.value();
            }
            else {
                return Math.Abs(marketValue() - modelValue())/marketValue();
            }
        }

        public abstract void addTimesTo(List<double> times);

        //! Black volatility implied by the model
        public double impliedVolatility(double targetValue, double accuracy, int maxEvaluations, double minVol, double maxVol) {

            ImpliedVolatilityHelper f = new ImpliedVolatilityHelper(this, targetValue);
            Brent solver = new Brent();
            solver.setMaxEvaluations(maxEvaluations);
            return solver.solve(f,accuracy,volatility_.link.value(), minVol, maxVol);
        }

        //! Black price given a volatility
        public abstract double blackPrice(double volatility);

        public void setPricingEngine(IPricingEngine engine) {
            engine_ = engine;
        }


        #region Observer & Observable
        public event Callback notifyObserversEvent;

        // this method is required for calling from derived classes
        protected void notifyObservers() {
            Callback handler = notifyObserversEvent;
            if (handler != null) {
                handler();
            }
        }
        public void registerWith(Callback handler) { notifyObserversEvent += handler; }
        public void unregisterWith(Callback handler) { notifyObserversEvent -= handler; }

        public void update() {
            marketValue_ = blackPrice(volatility_.link.value());
            notifyObservers();
        } 
	    #endregion

        private class ImpliedVolatilityHelper : ISolver1d {
            private CalibrationHelper helper_;
            private double value_;

            public ImpliedVolatilityHelper(CalibrationHelper helper, double value) {
                helper_ = helper;
                value_ = value;
            }

            public override double value(double x) {
                return value_ - helper_.blackPrice(x);
            }
        }
    }
}
