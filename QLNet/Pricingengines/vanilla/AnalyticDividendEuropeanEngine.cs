/*
 Copyright (C) 2008-2013  Andrea Maggiulli (a.maggiulli@gmail.com)

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
	//! Analytic pricing engine for European options with discrete dividends
	/*! \ingroup vanillaengines

		 \test the correctness of the returned greeks is tested by
				 reproducing numerical derivatives.
	*/
	public class AnalyticDividendEuropeanEngine : DividendVanillaOption.Engine
	{
		public AnalyticDividendEuropeanEngine(GeneralizedBlackScholesProcess process)
		{
			process_ = process;
			process_.registerWith(update);
		}

		public override void calculate()
		{
         Utils.QL_REQUIRE( arguments_.exercise.type() == Exercise.Type.European, () => "not an European option" );

			StrikedTypePayoff payoff = arguments_.payoff as StrikedTypePayoff;
         Utils.QL_REQUIRE( payoff != null, () => "non-striked payoff given" );

			Date settlementDate = process_.riskFreeRate().link.referenceDate();
			double riskless = 0.0;
			int i;
			for (i=0; i<arguments_.cashFlow.Count; i++)
				if (arguments_.cashFlow[i].date() >= settlementDate)
						riskless += arguments_.cashFlow[i].amount() *
							process_.riskFreeRate().link.discount(arguments_.cashFlow[i].date());

			double spot = process_.stateVariable().link.value() - riskless;
         Utils.QL_REQUIRE( spot > 0.0, () => "negative or null underlying after subtracting dividends" );

			double dividendDiscount = process_.dividendYield().link.discount(arguments_.exercise.lastDate());
			double riskFreeDiscount = process_.riskFreeRate().link.discount(arguments_.exercise.lastDate());
			double forwardPrice = spot * dividendDiscount / riskFreeDiscount;

			double variance =	process_.blackVolatility().link.blackVariance( arguments_.exercise.lastDate(),
																                     payoff.strike());

			BlackCalculator black = new BlackCalculator(payoff, forwardPrice, Math.Sqrt(variance),	riskFreeDiscount);

			results_.value = black.value();
			results_.delta = black.delta(spot);
			results_.gamma = black.gamma(spot);

			DayCounter rfdc  = process_.riskFreeRate().link.dayCounter();
			DayCounter voldc = process_.blackVolatility().link.dayCounter();
			double t = voldc.yearFraction( process_.blackVolatility().link.referenceDate(),arguments_.exercise.lastDate());
			results_.vega = black.vega(t);

			double delta_theta = 0.0, delta_rho = 0.0;
			for (i = 0; i < arguments_.cashFlow.Count; i++) 
			{
				Date d = arguments_.cashFlow[i].date();
				if (d >= settlementDate) 
				{
						delta_theta -= arguments_.cashFlow[i].amount() *
						process_.riskFreeRate().link.zeroRate(d,rfdc,Compounding.Continuous,Frequency.Annual).value() *
						process_.riskFreeRate().link.discount(d);
						double t1 = process_.time(d);
						delta_rho += arguments_.cashFlow[i].amount() * t1 *
										process_.riskFreeRate().link.discount(t1);
				}
			}
			t = process_.time(arguments_.exercise.lastDate());
			try 
			{
				results_.theta = black.theta(spot, t) +
										delta_theta * black.delta(spot);
			} 
			catch (Exception) 
			{
				results_.theta = null;
			}

			results_.rho = black.rho(t) +	delta_rho * black.delta(spot);
		
		}

		
		private GeneralizedBlackScholesProcess process_;
	}
}
