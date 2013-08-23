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
    //! condition to be applied at every time step
    /*! \ingroup findiff */
    public interface IStepCondition<array_type> where array_type : Vector {
        void applyTo(object o, double t);
    }


    /* Abstract base class which allows step conditions to use both payoff and array functions */
    public class CurveDependentStepCondition<array_type> : IStepCondition<array_type> where array_type : Vector {
        CurveWrapper curveItem_;

        protected CurveDependentStepCondition(Option.Type type, double strike) {
            curveItem_ = new PayoffWrapper(type, strike);
        }
        protected CurveDependentStepCondition(Payoff p) {
            curveItem_ = new PayoffWrapper(p);
        }
        protected CurveDependentStepCondition(array_type a) {
            curveItem_ = new ArrayWrapper(a);
        }

        protected double getValue(array_type a, int index) {
            return curveItem_.getValue(a, index);
        }

        public void applyTo(object o, double t) {
            Vector a = (Vector)o;
            for (int i = 0; i < a.size(); i++) {
                a[i] = applyToValue(a[i], getValue((array_type)o, i));
            }
        }

        protected virtual double applyToValue(double a, double b) { throw new NotImplementedException(); }

        protected abstract class CurveWrapper {
            public abstract double getValue(array_type a, int i);
        }

        protected class ArrayWrapper : CurveWrapper {
            private array_type value_;
            
            public ArrayWrapper(array_type a) {
                value_ = a;
            }

            public override double getValue(array_type a, int i) {
                return value_[i];
            }
        }

        protected class PayoffWrapper : CurveWrapper {
            private Payoff payoff_;

            public PayoffWrapper(Payoff p) {
                payoff_ = p;
                }
            public PayoffWrapper(Option.Type type, double strike) {
                payoff_ = new PlainVanillaPayoff(type, strike);
            }
            public override double getValue(array_type a, int i) {
                return a[i];
            }
        }
    }


    //! %null step condition
    /*! \ingroup findiff */
    public class NullCondition<array_type> : IStepCondition<array_type> where array_type : Vector {
        public void applyTo(object a, double t) { }
    }
}
