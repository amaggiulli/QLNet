/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
 
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
    public interface IConstraint {
        bool test(Vector param);
    }

    //! Base constraint class
    public class Constraint {
        protected IConstraint impl_;
        public bool empty() { return impl_ == null; }

        public Constraint() : this(null) { }
        public Constraint(IConstraint impl) {
            impl_ = impl;
        }

        public double update(Vector p, Vector direction, double beta) {
            double diff=beta;
            Vector newParams = p + diff * direction;
            bool valid = test(newParams);
            int icount = 0;
            while (!valid) {
                if (icount > 200)
                    throw new ApplicationException("can't update parameter vector");
                diff *= 0.5;
                icount ++;
                newParams = p + diff*direction;
                valid = test(newParams);
            }
            p += diff * direction;
            return diff;
        }

        public virtual bool test(Vector p) { return impl_.test(p); }
    }


    //! No constraint
    public class NoConstraint : Constraint {
        private class Impl : IConstraint {
            public bool test(Vector v) { return true; }
        }
        public NoConstraint() : base(new Impl()) {}
    };

	//! %Constraint imposing positivity to all arguments
	public class PositiveConstraint : Constraint
	{
        public PositiveConstraint()
            : base(new PositiveConstraint.Impl())
        {
        }

        private class Impl : IConstraint
		{
			public bool test(Vector v)
			{
				for (int i =0; i<v.Count; ++i)
				{
					if (v[i] <= 0.0)
						return false;
				}
				return true;
			}
		}
	}

	//! %Constraint imposing all arguments to be in [low,high]
	public class BoundaryConstraint : Constraint
	{
        public BoundaryConstraint(double low, double high)
            : base(new BoundaryConstraint.Impl(low, high))
        {
        }

        private class Impl : IConstraint
		{
            private double low_;
            private double high_;

            public Impl(double low, double high)
			{
				low_ = low;
				high_ = high;
			}
			public bool test(Vector v)
			{
                for (int i = 0; i < v.Count; i++)
				{
                    if ((v[i] < low_) || (v[i] > high_))
						return false;
				}
				return true;
			}
		}
	}

    //! %Constraint enforcing both given sub-constraints
    public class CompositeConstraint : Constraint {
        public CompositeConstraint(Constraint c1, Constraint c2) : base(new Impl(c1,c2)) { }

        private class Impl : IConstraint {
            private Constraint c1_, c2_;

            public Impl(Constraint c1, Constraint c2) {
                c1_ = c1;
                c2_ = c2;
            }
            
            public bool test(Vector p) {
                return c1_.test(p) && c2_.test(p);
            }
        }
    }
}
