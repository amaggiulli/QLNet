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
    //! Affine model class
    /*! Base class for analytically tractable models.

        \ingroup shortrate
    */
    public abstract class AffineModel : IObservable {
        //! Implied discount curve
        public abstract double discount(double t);
        public abstract double discountBond(double now, double maturity, Vector factors);
        public abstract double discountBondOption(Option.Type type, double strike, double maturity, double bondMaturity);

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
    }

    //Affince Model Interface used for multihritage in 
    //liborforwardmodel.cs & analyticcapfloorengine.cs
    public interface IAffineModel : IObservable
    {
        double discount(double t);
        double discountBond(double now, double maturity, Vector factors);
        double discountBondOption(Option.Type type, double strike, double maturity, double bondMaturity);
        //event Callback notifyObserversEvent;
        // this method is required for calling from derived classes
    }

    //TermStructureConsistentModel used in analyticcapfloorengine.cs
    public class TermStructureConsistentModel : IObservable
    {
        public TermStructureConsistentModel(Handle<YieldTermStructure> termStructure)
        {
            termStructure_ = termStructure;
        }

        public Handle<YieldTermStructure> termStructure()
        {
            return termStructure_;
        }
        private Handle<YieldTermStructure> termStructure_;

        public event Callback notifyObserversEvent;
        // this method is required for calling from derived classes
        protected void notifyObservers()
        {
            Callback handler = notifyObserversEvent;
            if (handler != null)
            {
                handler();
            }
        }
        public void registerWith(Callback handler) { notifyObserversEvent += handler; }
        public void unregisterWith(Callback handler) { notifyObserversEvent -= handler; }
    }

    //ITermStructureConsistentModel used ins shortratemodel blackkarasinski.cs/hullwhite.cs
    public interface ITermStructureConsistentModel
    {
        Handle<YieldTermStructure> termStructure();
        Handle<YieldTermStructure> termStructure_ { get; set; }
        void notifyObservers();
        event Callback notifyObserversEvent;
        void registerWith(Callback handler);
        void unregisterWith(Callback handler);
        void update();
    }
    
    //! Calibrated model class
    public class CalibratedModel : IObserver, IObservable {
        protected List<Parameter> arguments_;

        protected Constraint constraint_;
        public Constraint constraint() { return constraint_; }
        
        protected EndCriteria.Type shortRateEndCriteria_;
        public EndCriteria.Type endCriteria() { return shortRateEndCriteria_; }


        public CalibratedModel(int nArguments) {
            arguments_ = new InitializedList<Parameter>(nArguments);
            constraint_ = new PrivateConstraint(arguments_);
            shortRateEndCriteria_ = EndCriteria.Type.None;
        }

        //! Calibrate to a set of market instruments (caps/swaptions)
        /*! An additional constraint can be passed which must be
            satisfied in addition to the constraints of the model.
        */
        //public void calibrate(List<CalibrationHelper> instruments, OptimizationMethod method, EndCriteria endCriteria,
        //           Constraint constraint = new Constraint(), List<double> weights = new List<double>()) {
        public void calibrate(List<CalibrationHelper> instruments, OptimizationMethod method, EndCriteria endCriteria,
                   Constraint additionalConstraint, List<double> weights) {

            if (!(weights.Count == 0 || weights.Count == instruments.Count))
                throw new ApplicationException("mismatch between number of instruments and weights");

            Constraint c;
            if (additionalConstraint.empty())
                c = constraint_;
            else
                c = new CompositeConstraint(constraint_,additionalConstraint);
            List<double> w = weights.Count == 0 ? new InitializedList<double>(instruments.Count, 1.0): weights;
            CalibrationFunction f = new CalibrationFunction(this, instruments, w);

            Problem prob = new Problem(f, c, parameters());
            shortRateEndCriteria_ = method.minimize(prob, endCriteria);
            Vector result = new Vector(prob.currentValue());
            setParams(result);
            // recheck
            Vector shortRateProblemValues_ = prob.values(result);

            notifyObservers();
        }

        public double value(Vector p, List<CalibrationHelper> instruments) {
            List<double> w = new InitializedList<double>(instruments.Count, 1.0);
            CalibrationFunction f = new CalibrationFunction(this, instruments, w);
            return f.value(p);
        }

        //! Returns array of arguments on which calibration is done
        public Vector parameters() {
            int size = 0, i;
            for (i=0; i<arguments_.Count; i++)
                size += arguments_[i].size();
            Vector p = new Vector(size);
            int k = 0;
            for (i = 0; i < arguments_.Count; i++) {
                for (int j=0; j<arguments_[i].size(); j++, k++) {
                    p[k] = arguments_[i].parameters()[j];
                }
            }
            return p;
        }

        public virtual void setParams(Vector parameters) {
            int p = 0;
            for (int i = 0; i < arguments_.Count; ++i) {
                for (int j=0; j<arguments_[i].size(); ++j) {
                    if (p==parameters.Count) throw new ApplicationException("parameter array too small");
                    arguments_[i].setParam(j, parameters[p++]);
                }
            }

            if (p!=parameters.Count) throw new ApplicationException("parameter array too big!");
            update();
        }

        protected virtual void generateArguments() {}


        //! Constraint imposed on arguments
        private class PrivateConstraint : Constraint {
            public PrivateConstraint(List<Parameter> arguments) : base(new Impl(arguments)) { }

            private class Impl : IConstraint {
                private List<Parameter> arguments_;

                public Impl(List<Parameter> arguments) {
                    arguments_ = arguments;
                }

                public bool test(Vector p) {
                    int k=0;
                    for (int i=0; i<arguments_.Count; i++) {
                        int size = arguments_[i].size();
                        Vector testParams = new Vector(size);
                        for (int j=0; j<size; j++, k++)
                            testParams[j] = p[k];
                        if (!arguments_[i].testParams(testParams))
                            return false;
                    }
                    return true;
                }
            }
        }

        //! Calibration cost function class
        private class CalibrationFunction : CostFunction  {
            private CalibratedModel model_;
            private List<CalibrationHelper> instruments_;
            List<double> weights_;

            public CalibrationFunction(CalibratedModel model, List<CalibrationHelper> instruments, List<double> weights) {
                // recheck
                model_ = model;
                instruments_ = instruments;
                weights_ = weights;
            }

            public override double value(Vector p) {
                model_.setParams(p);

                double value = 0.0;
                for (int i=0; i<instruments_.Count; i++) {
                    double diff = instruments_[i].calibrationError();
                    value += diff*diff*weights_[i];
                }

                return Math.Sqrt(value);
            }

            public override Vector values(Vector p) {
                model_.setParams(p);

                Vector values = new Vector(instruments_.Count);
                for (int i=0; i<instruments_.Count; i++) {
                    values[i] = instruments_[i].calibrationError() *Math.Sqrt(weights_[i]);
                }
                return values;
            }

            public override double finiteDifferenceEpsilon() { return 1e-6; }
        }


        #region Observer & Observable
        public event Callback notifyObserversEvent;

        // this method is required for calling from derived classes
        public void notifyObservers() {
            Callback handler = notifyObserversEvent;
            if (handler != null) {
                handler();
            }
        }
        public void registerWith(Callback handler) { notifyObserversEvent += handler; }
        public void unregisterWith(Callback handler) { notifyObserversEvent -= handler; }

        public void update() {
            generateArguments();
            notifyObservers();
        } 
	    #endregion
    }

    //! Abstract short-rate model class
    /*! \ingroup shortrate */
    public abstract class ShortRateModel : CalibratedModel {
        public ShortRateModel(int nArguments) : base(nArguments) { }

        public abstract Lattice tree(TimeGrid t);
    }
}
