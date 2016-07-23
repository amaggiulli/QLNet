/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
  
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

        private readonly WeakEventSource eventSource = new WeakEventSource();
        public event Callback notifyObserversEvent
        {
           add { eventSource.Subscribe(value); }
           remove { eventSource.Unsubscribe(value); }
        }

        public void registerWith(Callback handler) { notifyObserversEvent += handler; }
        public void unregisterWith(Callback handler) { notifyObserversEvent -= handler; }
        protected void notifyObservers()
        {
           eventSource.Raise();
        }
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

        private readonly WeakEventSource eventSource = new WeakEventSource();
        public event Callback notifyObserversEvent
        {
           add { eventSource.Subscribe(value); }
           remove { eventSource.Unsubscribe(value); }
        }

        public void registerWith(Callback handler) { notifyObserversEvent += handler; }
        public void unregisterWith(Callback handler) { notifyObserversEvent -= handler; }
        protected void notifyObservers()
        {
           eventSource.Raise();
        }
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
        public void calibrate(List<CalibrationHelper> instruments, 
                              OptimizationMethod method, 
                              EndCriteria endCriteria,
                              Constraint additionalConstraint = null, 
                              List<double> weights = null,
                              List<bool> fixParameters = null) 
        {
           if ( weights == null ) weights = new List<double>();
           if ( additionalConstraint == null ) additionalConstraint = new Constraint();
           Utils.QL_REQUIRE( weights.empty() || weights.Count == instruments.Count,()=>
               "mismatch between number of instruments (" +
               instruments.Count + ") and weights(" +
               weights.Count + ")" );

            Constraint c;
            if (additionalConstraint.empty())
                c = constraint_;
            else
                c = new CompositeConstraint(constraint_,additionalConstraint);
            List<double> w = weights.Count == 0 ? new InitializedList<double>(instruments.Count, 1.0): weights;

            Vector prms = parameters();
            List<bool> all = new InitializedList<bool>(prms.size(), false);
            Projection proj = new Projection(prms,fixParameters ?? all);
            CalibrationFunction f = new CalibrationFunction(this,instruments,w,proj);
            ProjectedConstraint pc = new ProjectedConstraint(c,proj);
            Problem prob = new Problem(f, pc, proj.project(prms));
            shortRateEndCriteria_ = method.minimize(prob, endCriteria);
            Vector result = new Vector(prob.currentValue());
            setParams(proj.include(result));
            Vector shortRateProblemValues_ = prob.values(result);

            notifyObservers();

            //CalibrationFunction f = new CalibrationFunction(this, instruments, w);

            //Problem prob = new Problem(f, c, parameters());
            //shortRateEndCriteria_ = method.minimize(prob, endCriteria);
            //Vector result = new Vector(prob.currentValue());
            //setParams(result);
            //// recheck
            //Vector shortRateProblemValues_ = prob.values(result);

            //notifyObservers();
        }

        public double value(Vector parameters, List<CalibrationHelper> instruments) {
            List<double> w = new InitializedList<double>(instruments.Count, 1.0);
            Projection p = new Projection(parameters);
            CalibrationFunction f = new CalibrationFunction(this, instruments, w, p);
            return f.value(parameters);
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
                    if (p==parameters.Count) throw new Exception("parameter array too small");
                    arguments_[i].setParam(j, parameters[p++]);
                }
            }

            if (p!=parameters.Count) throw new Exception("parameter array too big!");
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

               public Vector upperBound(Vector parameters)
               {
                  int k = 0, k2 = 0;
                  int totalSize = 0;
                  for (int i = 0; i < arguments_.Count; i++) 
                  {
                     totalSize += arguments_[i].size();
                  }
                
                  Vector result = new Vector(totalSize);
                  for (int i = 0; i < arguments_.Count; i++) 
                  {
                     int size = arguments_[i].size();
                     Vector partialParams = new Vector(size);
                     for (int j = 0; j < size; j++, k++)
                        partialParams[j] = parameters[k];
                     Vector tmpBound = arguments_[i].constraint().upperBound(partialParams);
                     for (int j = 0; j < size; j++, k2++)
                        result[k2] = tmpBound[j];
                  }
                  return result;
               }

               public Vector lowerBound(Vector parameters)
               {
                  int k = 0, k2 = 0;
                  int totalSize = 0;
                  for (int i = 0; i < arguments_.Count; i++) 
                  {
                     totalSize += arguments_[i].size();
                  }
                  Vector result = new Vector(totalSize);
                  for (int i = 0; i < arguments_.Count; i++) 
                  {
                     int size = arguments_[i].size();
                     Vector partialParams=new Vector(size);
                     for (int j = 0; j < size; j++, k++)
                        partialParams[j] = parameters[k];
                     Vector tmpBound = arguments_[i].constraint().lowerBound(partialParams);
                     for (int j = 0; j < size; j++, k2++)
                        result[k2] = tmpBound[j];
                  }
                  return result;
               }
            }
        }

        //! Calibration cost function class
        private class CalibrationFunction : CostFunction
        {
           public CalibrationFunction( CalibratedModel model, 
                                       List<CalibrationHelper> instruments, 
                                       List<double> weights,
                                       Projection projection)
           {
              // recheck
              model_ = model;
              instruments_ = instruments;
              weights_ = weights;
              projection_ = projection;
           }

           public override double value( Vector p )
           {
              model_.setParams( projection_.include(p) );

              double value = 0.0;
              for ( int i = 0; i < instruments_.Count; i++ )
              {
                 double diff = instruments_[i].calibrationError();
                 value += diff * diff * weights_[i];
              }

              return Math.Sqrt( value );
           }

           public override Vector values( Vector p )
           {
              model_.setParams( projection_.include(p) );

              Vector values = new Vector( instruments_.Count );
              for ( int i = 0; i < instruments_.Count; i++ )
              {
                 values[i] = instruments_[i].calibrationError() * Math.Sqrt( weights_[i] );
              }
              return values;
           }

           public override double finiteDifferenceEpsilon() { return 1e-6; }

           private CalibratedModel model_;
           private List<CalibrationHelper> instruments_;
           private List<double> weights_;
           private Projection projection_;

        }


        #region Observer & Observable
        private readonly WeakEventSource eventSource = new WeakEventSource();
        public event Callback notifyObserversEvent
        {
           add { eventSource.Subscribe(value); }
           remove { eventSource.Unsubscribe(value); }
        }

        public void registerWith(Callback handler) { notifyObserversEvent += handler; }
        public void unregisterWith(Callback handler) { notifyObserversEvent -= handler; }
        public void notifyObservers()
        {
           eventSource.Raise();
        }

        public void update()
        {
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
