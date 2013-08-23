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

	//! Pricing Engine for barrier options using analytical formulae
//    ! The formulas are taken from "Option pricing formulas",
//         E.G. Haug, McGraw-Hill, p.69 and following.
//
//        \ingroup barrierengines
//
//        \test the correctness of the returned value is tested by
//              reproducing results available in literature.
//    
	public class AnalyticBarrierEngine : BarrierOption.Engine
	{
		public AnalyticBarrierEngine(GeneralizedBlackScholesProcess process)
		{
			process_ = process;
			//registerWith(process_);
            process_.registerWith(update);
		}
		public override void calculate()
		{
	
			PlainVanillaPayoff payoff = arguments_.payoff as PlainVanillaPayoff;

			if (payoff == null)
                throw new ApplicationException("non-plain payoff given");
			if (!(payoff.strike()>0.0))
                throw new ApplicationException("strike must be positive");

			double strike = payoff.strike();
			double spot = process_.x0();

			if (!(spot >= 0.0))
                throw new ApplicationException("negative or null underlying given");
			if (triggered(spot))
                throw new ApplicationException("barrier touched");
	
			Barrier.Type barrierType = arguments_.barrierType;
	
			switch (payoff.optionType())
			{
			  case Option.Type.Call:
				switch (barrierType)
				{
				  case Barrier.Type.DownIn:
					if (strike >= barrier())
						results_.value = C(1, 1) + E(1);
					else
						results_.value = A(1) - B(1) + D(1, 1) + E(1);
					break;
				  case Barrier.Type.UpIn:
					if (strike >= barrier())
						results_.value = A(1) + E(-1);
					else
						results_.value = B(1) - C(-1, 1) + D(-1, 1) + E(-1);
					break;
				  case Barrier.Type.DownOut:
					if (strike >= barrier())
						results_.value = A(1) - C(1, 1) + F(1);
					else
						results_.value = B(1) - D(1, 1) + F(1);
					break;
				  case Barrier.Type.UpOut:
					if (strike >= barrier())
						results_.value = F(-1);
					else
						results_.value = A(1) - B(1) + C(-1, 1) - D(-1, 1) + F(-1);
					break;
				}
				break;
			  case Option.Type.Put:
				switch (barrierType)
				{
				  case Barrier.Type.DownIn:
					if (strike >= barrier())
						results_.value = B(-1) - C(1, -1) + D(1, -1) + E(1);
					else
						results_.value = A(-1) + E(1);
					break;
				  case Barrier.Type.UpIn:
					if (strike >= barrier())
						results_.value = A(-1) - B(-1) + D(-1, -1) + E(-1);
					else
						results_.value = C(-1, -1) + E(-1);
					break;
				  case Barrier.Type.DownOut:
					if (strike >= barrier())
						results_.value = A(-1) - B(-1) + C(1, -1) - D(1, -1) + F(1);
					else
						results_.value = F(1);
					break;
				  case Barrier.Type.UpOut:
					if (strike >= barrier())
						results_.value = B(-1) - D(-1, -1) + F(-1);
					else
						results_.value = A(-1) - C(-1, -1) + F(-1);
					break;
				}
				break;
			  default:
                throw new ApplicationException("unknown type");
			}
		}
		private GeneralizedBlackScholesProcess process_;
		private CumulativeNormalDistribution f_ = new CumulativeNormalDistribution();

		private double underlying()
		{
			return process_.x0();
		}

		private double strike()
		{
            PlainVanillaPayoff payoff = arguments_.payoff as PlainVanillaPayoff;
            if (payoff == null)
                throw new ApplicationException("non-plain payoff given");
			return payoff.strike();
		}
		private double residualTime()
		{
			return process_.time(arguments_.exercise.lastDate());
		}
		private double volatility()
		{
			return process_.blackVolatility().link.blackVol(residualTime(), strike());
		}
		private double barrier()
		{
            return arguments_.barrier.GetValueOrDefault();
		}
		private double rebate()
		{
            return arguments_.rebate.GetValueOrDefault();
		}
		private double stdDeviation()
		{
			return volatility() * Math.Sqrt(residualTime());
		}
		private double riskFreeRate()
		{
			return process_.riskFreeRate().link.zeroRate(residualTime(), Compounding.Continuous, Frequency.NoFrequency).rate();
		}
		private double riskFreeDiscount()
		{
			return process_.riskFreeRate().link.discount(residualTime());
		}
		private double dividendYield()
		{
            return process_.dividendYield().link.zeroRate(residualTime(), Compounding.Continuous, Frequency.NoFrequency).rate();
		}
		private double dividendDiscount()
		{
			return process_.dividendYield().link.discount(residualTime());
		}
		private double mu()
		{
			double vol = volatility();
			return (riskFreeRate() - dividendYield())/(vol * vol) - 0.5;
		}
		private double muSigma()
		{
			return (1 + mu()) * stdDeviation();
		}
		private double A(double phi)
		{
			double x1 = Math.Log(underlying()/strike())/stdDeviation() + muSigma();
			double N1 = f_.value(phi *x1);
            double N2 = f_.value(phi * (x1 - stdDeviation()));
			return phi*(underlying() * dividendDiscount() * N1 - strike() * riskFreeDiscount() * N2);
		}
		private double B(double phi)
		{
			double x2 = Math.Log(underlying()/barrier())/stdDeviation() + muSigma();
            double N1 = f_.value(phi * x2);
            double N2 = f_.value(phi * (x2 - stdDeviation()));
			return phi*(underlying() * dividendDiscount() * N1 - strike() * riskFreeDiscount() * N2);
		}
		private double C(double eta, double phi)
		{
			double HS = barrier()/underlying();
			double powHS0 = Math.Pow(HS, 2 * mu());
			double powHS1 = powHS0 * HS * HS;
			double y1 = Math.Log(barrier()*HS/strike())/stdDeviation() + muSigma();
            double N1 = f_.value(eta * y1);
            double N2 = f_.value(eta * (y1 - stdDeviation()));
			return phi*(underlying() * dividendDiscount() * powHS1 * N1 - strike() * riskFreeDiscount() * powHS0 * N2);
		}
		private double D(double eta, double phi)
		{
			double HS = barrier()/underlying();
			double powHS0 = Math.Pow(HS, 2 * mu());
			double powHS1 = powHS0 * HS * HS;
			double y2 = Math.Log(barrier()/underlying())/stdDeviation() + muSigma();
            double N1 = f_.value(eta * y2);
            double N2 = f_.value(eta * (y2 - stdDeviation()));
			return phi*(underlying() * dividendDiscount() * powHS1 * N1 - strike() * riskFreeDiscount() * powHS0 * N2);
		}
		private double E(double eta)
		{
			if (rebate() > 0)
			{
				double powHS0 = Math.Pow(barrier()/underlying(), 2 * mu());
				double x2 = Math.Log(underlying()/barrier())/stdDeviation() + muSigma();
				double y2 = Math.Log(barrier()/underlying())/stdDeviation() + muSigma();
                double N1 = f_.value(eta * (x2 - stdDeviation()));
                double N2 = f_.value(eta * (y2 - stdDeviation()));
				return rebate() * riskFreeDiscount() * (N1 - powHS0 * N2);
			}
			else
			{
				return 0.0;
			}
		}
		private double F(double eta)
		{
			if (rebate() > 0)
			{
				double m = mu();
				double vol = volatility();
				double lambda = Math.Sqrt(m *m + 2.0 *riskFreeRate()/(vol * vol));
				double HS = barrier()/underlying();
				double powHSplus = Math.Pow(HS, m + lambda);
				double powHSminus = Math.Pow(HS, m - lambda);
	
				double sigmaSqrtT = stdDeviation();
				double z = Math.Log(barrier()/underlying())/sigmaSqrtT + lambda * sigmaSqrtT;

                double N1 = f_.value(eta * z);
                double N2 = f_.value(eta * (z - 2.0 * lambda * sigmaSqrtT));
				return rebate() * (powHSplus * N1 + powHSminus * N2);
			}
			else
			{
				return 0.0;
			}
		}
	}
}