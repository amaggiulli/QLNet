/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
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

namespace QLNet
{
    //! Base class for model arguments
    public class Parameter
    {
        protected Impl impl_;
        public Impl implementation() { return impl_; }

        protected Vector params_;
        public Vector parameters() { return params_; }

        protected Constraint constraint_;

        public Parameter()
        {
            constraint_ = new NoConstraint();
        }

        protected Parameter(int size, Impl impl, Constraint constraint)
        {
            impl_ = impl;
            params_ = new Vector(size);
            constraint_ = constraint;
        }

        public void setParam(int i, double x) { params_[i] = x; }
        public bool testParams(Vector p) { return constraint_.test(p); }

        public int size() { return params_.size(); }
        public double value(double t) { return impl_.value(params_, t); }

        //! Base class for model parameter implementation
        public abstract class Impl
        {
            public abstract double value(Vector p, double t);
        }
    }

    //! Standard constant parameter \f$ a(t) = a \f$
    public class ConstantParameter : Parameter
    {
        new private class Impl : Parameter.Impl
        {
            public override double value(Vector parameters, double UnnamedParameter1)
            {
                return parameters[0];
            }
        }
        public ConstantParameter(Constraint constraint)
            : base(1, new ConstantParameter.Impl(), constraint)
        {
        }

        public ConstantParameter(double value, Constraint constraint)
            : base(1, new ConstantParameter.Impl(), constraint)
        {
            params_[0] = value;

            if (!(testParams(params_)))
                throw new ApplicationException(": invalid value");
        }

    }

    //! %Parameter which is always zero \f$ a(t) = 0 \f$
    public class NullParameter : Parameter
    {
        new private class Impl : Parameter.Impl
        {
            public override double value(Vector UnnamedParameter1, double UnnamedParameter2)
            {
                return 0.0;
            }
        }
        public NullParameter()
            : base(0, new NullParameter.Impl(), new NoConstraint())
        {
        }
    }

    //! Piecewise-constant parameter
    //    ! \f$ a(t) = a_i if t_{i-1} \geq t < t_i \f$.
    //        This kind of parameter is usually used to enhance the fitting of a
    //        model
    //    
    public class PiecewiseConstantParameter : Parameter
    {
        new private class Impl : Parameter.Impl
        {
            public Impl(List<double> times)
            {
                times_ = times;
            }

            public override double value(Vector parameters, double t)
            {
                int size = times_.Count;
                for (int i = 0; i < size; i++)
                {
                    if (t < times_[i])
                        return parameters[i];
                }
                return parameters[size];
            }
            private List<double> times_;
        }
        public PiecewiseConstantParameter(List<double> times)
            : base(times.Count + 1, new PiecewiseConstantParameter.Impl(times), new NoConstraint())
        {
        }
    }

    //! Deterministic time-dependent parameter used for yield-curve fitting
    public class TermStructureFittingParameter : Parameter
    {
        public class NumericalImpl : Parameter.Impl
        {
            private List<double> times_;
            private List<double> values_;
            private Handle<YieldTermStructure> termStructure_;

            public NumericalImpl(Handle<YieldTermStructure> termStructure)
            {
                times_ = new List<double>();
                values_ = new List<double>();
                termStructure_ = termStructure;
            }

            public void setvalue(double t, double x)
            {
                times_.Add(t);
                values_.Add(x);
            }

            public void change(double x)
            {
                values_[values_.Count - 1] = x;
            }

            public void reset()
            {
                times_.Clear();
                values_.Clear();
            }
            public override double value(Vector UnnamedParameter1, double t)
            {
                //std::vector<Time>::const_iterator result =
                //     std::find(times_.begin(), times_.end(), t);
                // QL_REQUIRE(result!=times_.end(),
                //            "fitting parameter not set!");
                // return values_[result - times_.begin()];

                //throw new NotImplementedException("Need to implement the FindIndex method()");

                //int nIndex = times_.FindIndex( delegate(double val) { return val == locVal; });
                int nIndex = times_.FindIndex(val => val == t);
                if (nIndex == -1)
                    throw new ApplicationException("fitting parameter not set!");

                return values_[nIndex];
            }

            public Handle<YieldTermStructure> termStructure() { return termStructure_; }
        }

        public TermStructureFittingParameter(Parameter.Impl impl)
            : base(0, impl, new NoConstraint())
        {
        }

        public TermStructureFittingParameter(Handle<YieldTermStructure> term)
            : base(0, new NumericalImpl(term), new NoConstraint())
        {
        }
    }

}
