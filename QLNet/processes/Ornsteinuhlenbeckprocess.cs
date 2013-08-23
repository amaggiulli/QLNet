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

	//! Ornstein-Uhlenbeck process class
//    ! This class describes the Ornstein-Uhlenbeck process governed by
//        \f[
//            dx = a (r - x_t) dt + \sigma dW_t.
//        \f]
//
//        \ingroup processes
//    
	public class OrnsteinUhlenbeckProcess : StochasticProcess1D
	{
        private double x0_;
        private double speed_;
        private double level_;
        private double volatility_;

        public OrnsteinUhlenbeckProcess(double speed, double vol, double x0)
            : this(speed, vol, x0, 0.0)
		{
		}
		public OrnsteinUhlenbeckProcess(double speed, double vol) : this(speed, vol, 0.0, 0.0)
		{
		}
		public OrnsteinUhlenbeckProcess(double speed, double vol, double x0, double level)
		{
			x0_ = x0;
			speed_ = speed;
			level_ = level;
			volatility_ = vol;
			if (!(speed_ >= 0.0))
                throw new ApplicationException("negative speed given");

			if (!(volatility_ >= 0.0))
                throw new ApplicationException("negative volatility given");
		}
		//! \name StochasticProcess interface
		//@{
        public override double x0()
		{
			return x0_;
		}
		public double speed()
		{
			return speed_;
		}
		public double volatility()
		{
			return volatility_;
		}
		public double level()
		{
			return level_;
		}
        public override double drift(double UnnamedParameter1, double x)
		{
			return speed_ * (level_ - x);
		}
        public override double diffusion(double UnnamedParameter1, double UnnamedParameter2)
		{
			return volatility_;
		}
        public override double expectation(double UnnamedParameter1, double x0, double dt)
		{
			return level_ + (x0 - level_) * Math.Exp(-speed_ *dt);
		}
        public override double stdDeviation(double t, double x0, double dt)
		{
			return Math.Sqrt(variance(t, x0, dt));
		}
        public override double variance(double UnnamedParameter1, double UnnamedParameter2, double dt)
		{
			if (speed_ < Math.Sqrt(((Const.QL_Epsilon))))
			{
				 // algebraic limit for small speed
				return volatility_ *volatility_ *dt;
			}
			else
			{
				return 0.5 *volatility_ *volatility_/speed_* (1.0 - Math.Exp(-2.0 *speed_ *dt));
			}
		}
	}

}
