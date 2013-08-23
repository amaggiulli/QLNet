/*
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
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

    public abstract class Integrator {
        private double absoluteAccuracy_;
        private double absoluteError_;
        private int maxEvaluations_;
        private int evaluations_;

        public Integrator(double absoluteAccuracy, int maxEvaluations) {
            absoluteAccuracy_ = absoluteAccuracy;
            maxEvaluations_ = maxEvaluations;
            if (!(absoluteAccuracy > ((Double.Epsilon))))
                throw new ApplicationException("required tolerance (" + absoluteAccuracy + ") not allowed. It must be > " + Double.Epsilon);
        }

        public double value(Func<double, double> f, double a, double b) {
            evaluations_ = 0;
            if (a == b)
                return 0.0;
            if (b > a)
                return integrate(f, a, b);
            else
                return -integrate(f, b, a);
        }

        //! \name Modifiers
        //@{
        public void setAbsoluteAccuracy(double accuracy) {
            absoluteAccuracy_ = accuracy;
        }
        public void setMaxEvaluations(int maxEvaluations) {
            maxEvaluations_ = maxEvaluations;
        }
        //@}

        //! \name Inspectors
        //@{
        public double absoluteAccuracy() {
            return absoluteAccuracy_;
        }
        public int maxEvaluations() {
            return maxEvaluations_;
        }
        //@}

        public double absoluteError() {
            return absoluteError_;
        }

        public int numberOfEvaluations() {
            return evaluations_;
        }

        public bool integrationSuccess() {
            return evaluations_ <= maxEvaluations_ && absoluteError_ <= absoluteAccuracy_;
        }

        protected abstract double integrate(Func<double, double> f, double a, double b);

        protected void setAbsoluteError(double error) {
            absoluteError_ = error;
        }
        protected void setNumberOfEvaluations(int evaluations) {
            evaluations_ = evaluations;
        }
        protected void increaseNumberOfEvaluations(int increase) {
            evaluations_ += increase;
        }
    }

}
