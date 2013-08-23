/*
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 * 
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

    //! Base class for line search
    public abstract class LineSearch {
        //! current values of the search direction
        protected Vector searchDirection_;
        //! new x and its gradient
        protected Vector xtd_;
        protected Vector gradient_ = new Vector();
        //! cost function value and gradient norm corresponding to xtd_
        protected double qt_;
        protected double qpt_;
        //! flag to know if linesearch succeed
        protected bool succeed_;

        //! Default constructor
        public LineSearch() : this(0.0) { }
        public LineSearch(double UnnamedParameter1) {
            qt_ = 0.0;
            qpt_ = 0.0;
            succeed_ = true;
        }

        //! return last x value
        public Vector lastX() { return xtd_; }
        //! return last cost function value
        public double lastFunctionValue() { return qt_; }
        //! return last gradient
        public Vector lastGradient() { return gradient_; }
        //! return square norm of last gradient
        public double lastGradientNorm2() { return qpt_; }

        public bool succeed() { return succeed_; }

        //! Perform line search
        public abstract double value(Problem P, ref EndCriteria.Type ecType, EndCriteria NamelessParameter3, double t_ini); // initial value of line-search step
        public double update(ref Vector data, Vector direction, double beta, Constraint constraint) {

            double diff = beta;
            Vector newParams = data + diff * direction;
            bool valid = constraint.test(newParams);
            int icount = 0;
            while (!valid) {
                if (icount > 200)
                    throw new ApplicationException("can't update linesearch");
                diff *= 0.5;
                icount++;
                newParams = data + diff * direction;
                valid = constraint.test(newParams);
            }
            data += diff * direction;
            return diff;
        }

        //! current value of the search direction
        public Vector searchDirection {
            get { return searchDirection_; }
            set { searchDirection_ = value; }
        }
    }
}