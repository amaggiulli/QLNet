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
    public interface ISchemeFactory {
        IMixedScheme factory(object L, object bcs);
    }

    public interface IMixedScheme {
        void step(ref object a, double t);
        void setStep(double dt);
    }

    //! Mixed (explicit/implicit) scheme for finite difference methods
    /*! In this implementation, the passed operator must be derived
        from either TimeConstantOperator or TimeDependentOperator.
        Also, it must implement at least the following interface:

        \code
        typedef ... array_type;

        \ingroup findiff
    */
    public class MixedScheme<Operator> : IMixedScheme where Operator : IOperator {
        protected Operator L_, I_, explicitPart_, implicitPart_;
        protected double dt_;
        protected double theta_;
        protected List<BoundaryCondition<IOperator>> bcs_;

        // constructors
        public MixedScheme() { }  // required for generics
        public MixedScheme(Operator L, double theta, List<BoundaryCondition<IOperator>> bcs) {
            L_ = (Operator)L.Clone();
            I_ = (Operator)L.identity(L.size());
            dt_ = 0.0;
            theta_ = theta;
            bcs_ = bcs;
        }

        public void step(ref object o, double t) {
            Vector a = (Vector)o;

            int i;
            for (i=0; i<bcs_.Count; i++)
                bcs_[i].setTime(t);
            if (theta_!=1.0) { // there is an explicit part
                if (L_.isTimeDependent()) {
                    L_.setTime(t);
                    explicitPart_ = (Operator)L_.subtract(I_, L_.multiply((1.0 - theta_) * dt_, L_));
                }
                for (i = 0; i < bcs_.Count; i++)
                    bcs_[i].applyBeforeApplying(explicitPart_);
                a = explicitPart_.applyTo(a);
                for (i = 0; i < bcs_.Count; i++)
                    bcs_[i].applyAfterApplying(a);
            }
            if (theta_!=0.0) { // there is an implicit part
                if (L_.isTimeDependent()) {
                    L_.setTime(t-dt_);
                    implicitPart_ = (Operator)L_.add(I_, L_.multiply(theta_ * dt_, L_));
                }
                for (i = 0; i < bcs_.Count; i++)
                    bcs_[i].applyBeforeSolving(implicitPart_,a);
                a = implicitPart_.solveFor(a);
                for (i = 0; i < bcs_.Count; i++)
                    bcs_[i].applyAfterSolving(a);
            }

            o = a;
        }

        public void setStep(double dt) {
            dt_ = dt;
            if (theta_!=1.0) // there is an explicit part
                explicitPart_ = (Operator)L_.subtract(I_, L_.multiply((1.0 - theta_) * dt_, L_));
            if (theta_!=0.0) // there is an implicit part
                implicitPart_ = (Operator)L_.add(I_, L_.multiply(theta_ * dt_, L_));
        }
    }
}
