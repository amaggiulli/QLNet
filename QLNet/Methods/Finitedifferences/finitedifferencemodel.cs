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
    public class FiniteDifferenceModel<Evolver> where Evolver : IMixedScheme, ISchemeFactory, new() {
        private Evolver evolver_;
        public Evolver evolver() { return evolver_; }

        private List<double> stoppingTimes_;

        // constructors
        public FiniteDifferenceModel(object L, object bcs)
            : this(L, bcs, new List<double>()) { }
        public FiniteDifferenceModel(object L, object bcs, List<double> stoppingTimes) {
            evolver_ = (Evolver)new Evolver().factory(L, bcs);
            stoppingTimes_ = stoppingTimes;
            stoppingTimes_.Sort();
            stoppingTimes_.Distinct();
        }

        //public FiniteDifferenceModel(Evolver evolver, List<double> stoppingTimes = List<double>())
        public FiniteDifferenceModel(Evolver evolver, List<double> stoppingTimes) {
            evolver_ = evolver;

            stoppingTimes_ = stoppingTimes;
            stoppingTimes_.Sort();
            stoppingTimes_.Distinct();
        }

        /*! solves the problem between the given times, applying a condition at every step.
            \warning being this a rollback, <tt>from</tt> must be a later time than <tt>to</tt>. */
        public void rollback(ref object a, double from, double to, int steps) { rollbackImpl(ref a, from, to, steps, null); }
        public void rollback(ref object a, double from, double to, int steps, IStepCondition<Vector> condition) {
            rollbackImpl(ref a,from,to,steps, condition);
        }

        private void rollbackImpl(ref object o, double from, double to, int steps, IStepCondition<Vector> condition) {

            if (!(from >= to)) throw new ApplicationException("trying to roll back from " + from + " to " + to);

            double dt = (from - to) / steps, t = from;
            evolver_.setStep(dt);

            for (int i=0; i<steps; ++i, t -= dt) {
                double now = t, next = t - dt;
                bool hit = false;
                for (int j = stoppingTimes_.Count-1; j >= 0 ; --j) {
                    if (next <= stoppingTimes_[j] && stoppingTimes_[j] < now) {
                        // a stopping time was hit
                        hit = true;

                        // perform a small step to stoppingTimes_[j]...
                        evolver_.setStep(now-stoppingTimes_[j]);
                        evolver_.step(ref o, now);
                        if (condition != null)
                            condition.applyTo(o,stoppingTimes_[j]);
                        // ...and continue the cycle
                        now = stoppingTimes_[j];
                    }
                }
                // if we did hit...
                if (hit) {
                    // ...we might have to make a small step to
                    // complete the big one...
                    if (now > next) {
                        evolver_.setStep(now - next);
                        evolver_.step(ref o,now);
                        if (condition != null)
                            condition.applyTo(o,next);
                    }
                    // ...and in any case, we have to reset the
                    // evolver to the default step.
                    evolver_.setStep(dt);
                } else {
                    // if we didn't, the evolver is already set to the
                    // default step, which is ok for us.
                    evolver_.step(ref o,now);
                    if (condition != null)
                        condition.applyTo(o, next);
                }
            }
        }

    }
}
