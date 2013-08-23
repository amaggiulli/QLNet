/*
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

	public abstract class BasketPayoff : Payoff
	{
		private Payoff basePayoff_;
		public BasketPayoff(Payoff p)
		{
			basePayoff_ = p;
		}
		public override string name()
		{
			return basePayoff_.name();
		}
        public override string description()
		{
			return basePayoff_.description();
		}
        public override double value(double price)
		{
			return basePayoff_.value(price);
		}
		public virtual double value(Vector a)
		{
			return basePayoff_.value(accumulate(a));
		}

        public abstract double accumulate(Vector a);

		public Payoff basePayoff()
		{
			return basePayoff_;
		}
	}

	public class MinBasketPayoff : BasketPayoff
	{
		public MinBasketPayoff(Payoff p) : base(p)
		{
		}
        public override double accumulate(Vector a)
		{
//			return std.min_element(a.begin(), a.end());
            return a.Min();
		}
	}

	public class MaxBasketPayoff : BasketPayoff
	{
		public MaxBasketPayoff(Payoff p) : base(p)
		{
		}
        public override double accumulate(Vector a)
		{
//			return std.max_element(a.begin(), a.end());
            return a.Max();
		}
	}

	public class AverageBasketPayoff : BasketPayoff
	{
		public AverageBasketPayoff(Payoff p, Vector a) : base(p)
		{
			weights_ = a;
		}
		public AverageBasketPayoff(Payoff p, int n) : base(p)
		{
			weights_ = new Vector(n, 1.0/(double)(n));
		}
        public override double accumulate(Vector a)
		{
//			return std.inner_product(weights_.begin(), weights_.end(), a.begin(), 0.0);
            double tally = weights_ * a;
            return tally;
		}
		private Vector weights_;
	}


	//! Basket option on a number of assets
	//! \ingroup instruments 
	public class BasketOption : MultiAssetOption
	{
	    new class Engine : GenericEngine<BasketOption.Arguments, BasketOption.Results> 
        {
        };

		public BasketOption(BasketPayoff payoff, Exercise exercise) : base(payoff, exercise)
		{
		}
	}
}
