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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QLNet;

namespace TestSuite
{
	[TestClass()]
	public class T_DividendOption
	{

		public void REPORT_FAILURE(string greekName, StrikedTypePayoff payoff, Exercise exercise, double s, double q, 
			                        double r,Date today, double v, double expected, double calculated, double error, 
			                        double tolerance)
        {
            Assert.Fail(exercise + " "
                   + payoff.optionType() + " option with "
                   + payoff + " payoff:\n"
                   + "    spot value:       " + s + "\n"
                   + "    strike:           " + payoff.strike() + "\n"
                   + "    dividend yield:   " + q + "\n"
                   + "    risk-free rate:   " + r + "\n"
                   + "    reference date:   " + today + "\n"
                   + "    maturity:         " + exercise.lastDate() + "\n"
                   + "    volatility:       " + v + "\n\n"
                   + "    expected " + greekName + ":   " + expected + "\n"
                   + "    calculated " + greekName + ": " + calculated + "\n"
                   + "    error:            " + error + "\n"
                   + "    tolerance:        " + tolerance);
        }


		void testFdGreeks<Engine>(Date today, Exercise exercise) where Engine : IFDEngine, new()
		{
			Dictionary<string, double> calculated = new Dictionary<string, double>(),
											  expected = new Dictionary<string, double>(),
											  tolerance = new Dictionary<string, double>();
			tolerance.Add("delta", 5.0e-3);
			tolerance.Add("gamma", 7.0e-3);
			// tolerance["theta"] = 1.0e-2;

			Option.Type[] types = { Option.Type.Call, Option.Type.Put };
			double[] strikes = { 50.0, 99.5, 100.0, 100.5, 150.0 };
			double[] underlyings = { 100.0 };
			double[] qRates = { 0.00, 0.10, 0.20 };
			double[] rRates = { 0.01, 0.05, 0.15 };
			double[] vols = { 0.05, 0.20, 0.50 };

			DayCounter dc = new Actual360();

			SimpleQuote spot = new SimpleQuote(0.0);
			SimpleQuote qRate = new SimpleQuote(0.0);
			Handle<YieldTermStructure> qTS = new Handle<YieldTermStructure>(Utilities.flatRate(qRate, dc));
			SimpleQuote rRate = new SimpleQuote(0.0);
			Handle<YieldTermStructure> rTS = new Handle<YieldTermStructure>(Utilities.flatRate(rRate, dc));
			SimpleQuote vol = new SimpleQuote(0.0);
			Handle<BlackVolTermStructure> volTS = new Handle<BlackVolTermStructure>(Utilities.flatVol(vol, dc));

			for (int i = 0; i < types.Length; i++)
			{
				for (int j = 0; j < strikes.Length; j++)
				{
					List<Date> dividendDates = new List<Date>();
					List<double> dividends = new List<double>();
					for (Date d = today + new Period(3, TimeUnit.Months);
							d < exercise.lastDate();
							d += new Period(6, TimeUnit.Months))
					{
						dividendDates.Add(d);
						dividends.Add(5.0);
					}

					StrikedTypePayoff payoff = new PlainVanillaPayoff(types[i], strikes[j]);

					BlackScholesMertonProcess stochProcess = new BlackScholesMertonProcess(new Handle<Quote>(spot),
																												  qTS, rTS, volTS);

					IPricingEngine engine = new Engine().factory(stochProcess);
					DividendVanillaOption option = new DividendVanillaOption(payoff, exercise, dividendDates, dividends);
					option.setPricingEngine(engine);

					for (int l = 0; l < underlyings.Length; l++)
					{
						for (int m = 0; m < qRates.Length; m++)
						{
							for (int n = 0; n < rRates.Length; n++)
							{
								for (int p = 0; p < vols.Length; p++)
								{
									double u = underlyings[l];
									double q = qRates[m],
											 r = rRates[n];
									double v = vols[p];
									spot.setValue(u);
									qRate.setValue(q);
									rRate.setValue(r);
									vol.setValue(v);

									// FLOATING_POINT_EXCEPTION
									double value = option.NPV();
									calculated["delta"] = option.delta();
									calculated["gamma"] = option.gamma();
									// calculated["theta"]  = option.theta();

									if (value > spot.value() * 1.0e-5)
									{
										// perturb spot and get delta and gamma
										double du = u * 1.0e-4;
										spot.setValue(u + du);
										double value_p = option.NPV(),
												 delta_p = option.delta();
										spot.setValue(u - du);
										double value_m = option.NPV(),
												 delta_m = option.delta();
										spot.setValue(u);
										expected["delta"] = (value_p - value_m) / (2 * du);
										expected["gamma"] = (delta_p - delta_m) / (2 * du);

										// perturb date and get theta
										/*
											Time dT = dc.yearFraction(today-1, today+1);
											Settings::instance().evaluationDate() = today-1;
											value_m = option.NPV();
											Settings::instance().evaluationDate() = today+1;
											value_p = option.NPV();
											Settings::instance().evaluationDate() = today;
											expected["theta"] = (value_p - value_m)/dT;
										*/

										// compare
										foreach (string greek in calculated.Keys)
										{
											double expct = expected[greek],
													 calcl = calculated[greek],
													 tol = tolerance[greek];
											double error = Utilities.relativeError(expct, calcl, u);
											if (error > tol)
											{
												REPORT_FAILURE(greek, payoff, exercise,
																	u, q, r, today, v,
																	expct, calcl, error, tol);
											}
										}
									}
								}
							}
						}
					}
				}
			}
		}
			
      void testFdDegenerate<Engine>(Date today,Exercise exercise) where Engine : IFDEngine, new()
		{
			DayCounter dc = new Actual360();
			SimpleQuote spot = new SimpleQuote(54.625);
			Handle<YieldTermStructure> rTS = new Handle<YieldTermStructure>(Utilities.flatRate(0.052706, dc));
			Handle<YieldTermStructure> qTS = new Handle<YieldTermStructure>(Utilities.flatRate(0.0, dc));
			Handle<BlackVolTermStructure> volTS = new Handle<BlackVolTermStructure>(Utilities.flatVol(0.282922, dc));

			BlackScholesMertonProcess process = new BlackScholesMertonProcess(new Handle<Quote>(spot),
																				               qTS, rTS, volTS);

			int timeSteps = 300;
			int gridPoints = 300;

			IPricingEngine engine = new Engine().factory(process,timeSteps,gridPoints);

			StrikedTypePayoff payoff = new PlainVanillaPayoff(Option.Type.Call, 55.0);

			double tolerance = 3.0e-3;

			List<double> dividends = new List<double>();
			List<Date> dividendDates = new List<Date>();

			DividendVanillaOption option1 = new DividendVanillaOption(payoff, exercise,dividendDates, dividends);
			option1.setPricingEngine(engine);

			// FLOATING_POINT_EXCEPTION
			double refValue = option1.NPV();

			for (int i=0; i<=6; i++) 
			{
				dividends.Add(0.0);
				dividendDates.Add(today+i);

				DividendVanillaOption option = new DividendVanillaOption(payoff, exercise,	dividendDates, dividends);
				option.setPricingEngine(engine);
				double value = option.NPV();

				if (Math.Abs(refValue-value) > tolerance)
						Assert.Fail("NPV changed by null dividend :\n"
									   + "    previous value: " + value + "\n"
									   + "    current value:  " + refValue + "\n"
									   + "    change:         " + (value-refValue));
			}
		
		}


		[TestMethod()]
		public void testEuropeanValues() 
		{
			// Testing dividend European option values with no dividends...

			SavedSettings backup = new SavedSettings();

			double tolerance = 1.0e-5;

			Option.Type[] types = { Option.Type.Call, Option.Type.Put };
			double[] strikes = { 50.0, 99.5, 100.0, 100.5, 150.0 };
			double[] underlyings = { 100.0 };
			double[] qRates = { 0.00, 0.10, 0.30 };
			double[] rRates = { 0.01, 0.05, 0.15 };
			int[] lengths = { 1, 2 };
			double[] vols = { 0.05, 0.20, 0.70 };

			DayCounter dc = new Actual360();
			Date today = Date.Today;
			Settings.setEvaluationDate(today);

			SimpleQuote spot = new SimpleQuote(0.0) ;
			SimpleQuote qRate = new SimpleQuote(0.0);
			Handle<YieldTermStructure> qTS = new Handle<YieldTermStructure>(Utilities.flatRate(qRate, dc));
			SimpleQuote rRate = new SimpleQuote(0.0) ;
			Handle<YieldTermStructure> rTS = new Handle<YieldTermStructure>(Utilities.flatRate(rRate, dc));
			SimpleQuote vol = new SimpleQuote(0.0);
			Handle<BlackVolTermStructure> volTS = new Handle<BlackVolTermStructure>(Utilities.flatVol(vol, dc));

			for (int i=0; i< types.Length; i++) 
			{
				for (int j=0; j<strikes.Length; j++) 
				{
					for (int k=0; k<lengths.Length; k++) 
					{
						Date exDate = today + new Period(lengths[k],TimeUnit.Years);
						Exercise exercise = new EuropeanExercise(exDate);

						List<Date> dividendDates = new List<Date>();
						List<double> dividends = new List<double>();
						for (Date d = today + new Period(3,TimeUnit.Months);
								d < exercise.lastDate();
								d += new Period(6,TimeUnit.Months)) 
								 
						{
							dividendDates.Add(d);
							dividends.Add(0.0);
						}

						StrikedTypePayoff payoff = new PlainVanillaPayoff(types[i], strikes[j]);

						BlackScholesMertonProcess stochProcess =  new BlackScholesMertonProcess(new Handle<Quote>(spot),
																					qTS, rTS, volTS);

						IPricingEngine ref_engine = new AnalyticEuropeanEngine(stochProcess);

						IPricingEngine engine = new AnalyticDividendEuropeanEngine(stochProcess);

						DividendVanillaOption option = new DividendVanillaOption(payoff, exercise, dividendDates, dividends);
						option.setPricingEngine(engine);

						VanillaOption ref_option = new VanillaOption(payoff, exercise);
						ref_option.setPricingEngine(ref_engine);

						for (int l=0; l<underlyings.Length; l++) 
						{
							for (int m=0; m<qRates.Length; m++) 
							{
								for (int n=0; n<rRates.Length; n++) 
								{
									for (int p=0; p<vols.Length; p++) 
									{
										double u = underlyings[l];
										double q = qRates[m],
										r = rRates[n];
										double v = vols[p];
										spot.setValue(u);
										qRate.setValue(q);
										rRate.setValue(r);
										vol.setValue(v);

										double calculated = option.NPV();
										double expected = ref_option.NPV();
										double error = Math.Abs(calculated-expected);
										if (error > tolerance) {
											REPORT_FAILURE("value start limit",
																payoff, exercise,
																u, q, r, today, v,
																expected, calculated,
																error, tolerance);
										}
									}
								}
							}
						}
					}
				}
			}
		}

		// Reference pg. 253 - Hull - Options, Futures, and Other Derivatives 5th ed
		// Exercise 12.8
		// Doesn't quite work.  Need to deal with date conventions
		[TestMethod()]
		void testEuropeanKnownValue() 
		{

			// Testing dividend European option values with known value...

			SavedSettings backup = new SavedSettings();

			double tolerance = 1.0e-2;
			double expected = 3.67;

			DayCounter dc = new Actual360();
			Date today = Date.Today;
			Settings.setEvaluationDate(today);

			SimpleQuote spot = new SimpleQuote(0.0);
			SimpleQuote qRate = new SimpleQuote(0.0);
			Handle<YieldTermStructure> qTS = new Handle<YieldTermStructure>(Utilities.flatRate(qRate, dc));
			SimpleQuote rRate = new SimpleQuote(0.0);
			Handle<YieldTermStructure> rTS = new Handle<YieldTermStructure>(Utilities.flatRate(rRate, dc));
			SimpleQuote vol = new SimpleQuote(0.0);
			Handle<BlackVolTermStructure> volTS = new Handle<BlackVolTermStructure>(Utilities.flatVol(vol, dc));

			Date exDate = today + new Period(6,TimeUnit.Months);
			Exercise exercise = new EuropeanExercise(exDate);

			List<Date> dividendDates = new List<Date>();
			List<double> dividends = new List<double>();
			dividendDates.Add(today + new Period(2,TimeUnit.Months));
			dividends.Add(0.50);
			dividendDates.Add(today + new Period(5,TimeUnit.Months));
			dividends.Add(0.50);

			StrikedTypePayoff payoff = new PlainVanillaPayoff(Option.Type.Call, 40.0);

			BlackScholesMertonProcess stochProcess = new BlackScholesMertonProcess(new Handle<Quote>(spot),
																						               qTS, rTS, volTS);

			IPricingEngine engine = new AnalyticDividendEuropeanEngine(stochProcess);

			DividendVanillaOption option = new DividendVanillaOption(payoff, exercise,	dividendDates, dividends);
			option.setPricingEngine(engine);

			double u = 40.0;
			double q = 0.0, r = 0.09;
			double v = 0.30;
			spot.setValue(u);
			qRate.setValue(q);
			rRate.setValue(r);
			vol.setValue(v);

			double calculated = option.NPV();
			double error = Math.Abs(calculated-expected);
			if (error > tolerance) 
			{
				REPORT_FAILURE("value start limit",
									payoff, exercise,
									u, q, r, today, v,
									expected, calculated,
									error, tolerance);
			}
		}

		[TestMethod()] 
		public void testEuropeanStartLimit() 
		{

			// Testing dividend European option with a dividend on today's date...

			SavedSettings backup = new SavedSettings();

			double tolerance = 1.0e-5;
			double dividendValue = 10.0;

			Option.Type[] types = { Option.Type.Call, Option.Type.Put };
			double[] strikes = { 50.0, 99.5, 100.0, 100.5, 150.0 };
			double[] underlyings = { 100.0 };
			double[] qRates = { 0.00, 0.10, 0.30 };
			double[] rRates = { 0.01, 0.05, 0.15 };
			int[] lengths = { 1, 2 };
			double[] vols = { 0.05, 0.20, 0.70 };

			DayCounter dc = new Actual360();
			Date today = Date.Today;
			Settings.setEvaluationDate(today);

			SimpleQuote spot = new SimpleQuote(0.0);
			SimpleQuote qRate = new SimpleQuote(0.0);
			Handle<YieldTermStructure> qTS = new Handle<YieldTermStructure>(Utilities.flatRate(qRate, dc));
			SimpleQuote rRate = new SimpleQuote(0.0);
			Handle<YieldTermStructure> rTS = new Handle<YieldTermStructure>(Utilities.flatRate(rRate, dc));
			SimpleQuote vol = new SimpleQuote(0.0);
			Handle<BlackVolTermStructure> volTS = new Handle<BlackVolTermStructure>(Utilities.flatVol(vol, dc));

			for (int i=0; i< types.Length; i++) 
			{
				for (int j=0; j< strikes.Length; j++) 
				{
					for (int k=0; k< lengths.Length; k++) 
					{
						Date exDate = today + new Period(lengths[k],TimeUnit.Years);
						Exercise exercise = new EuropeanExercise(exDate);

						List<Date> dividendDates = new List<Date>();
						List<double> dividends = new List<double>();
						dividendDates.Add(today);
						dividends.Add(dividendValue);

						StrikedTypePayoff payoff = new PlainVanillaPayoff(types[i], strikes[j]);

						BlackScholesMertonProcess stochProcess = new BlackScholesMertonProcess(new Handle<Quote>(spot),
																						                       qTS, rTS, volTS);

						IPricingEngine engine = new AnalyticDividendEuropeanEngine(stochProcess);

						IPricingEngine ref_engine = new AnalyticEuropeanEngine(stochProcess);

						DividendVanillaOption option = new DividendVanillaOption(payoff, exercise,dividendDates, dividends);
						option.setPricingEngine(engine);

						VanillaOption ref_option = new VanillaOption(payoff, exercise);
						ref_option.setPricingEngine(ref_engine);

						for (int l=0; l< underlyings.Length; l++) 
						{
							for (int m=0; m<qRates.Length; m++) 
							{
								for (int n=0; n<rRates.Length; n++) 
								{
									for (int p=0; p<vols.Length; p++) 
									{
										double u = underlyings[l];
										double q = qRates[m],
												r = rRates[n];
										double v = vols[p];
										spot.setValue(u);
										qRate.setValue(q);
										rRate.setValue(r);
										vol.setValue(v);

										double calculated = option.NPV();
										spot.setValue(u-dividendValue);
										double expected = ref_option.NPV();
										double error = Math.Abs(calculated-expected);
										if (error > tolerance) 
										{
											REPORT_FAILURE("value", payoff, exercise,
																u, q, r, today, v,
																expected, calculated,
																error, tolerance);
								
										}
									}
								}
							}
						}
					}
				}
			}
		}

		[TestMethod()]
		public void testEuropeanGreeks() 
		{

			// Testing dividend European option greeks...

			SavedSettings backup = new SavedSettings();

			Dictionary<string,double> calculated = new Dictionary<string,double>(), 
				                       expected = new Dictionary<string,double>(), 
											  tolerance = new Dictionary<string,double>();
			tolerance["delta"] = 1.0e-5;
			tolerance["gamma"] = 1.0e-5;
			tolerance["theta"] = 1.0e-5;
			tolerance["rho"]   = 1.0e-5;
			tolerance["vega"]  = 1.0e-5;

			Option.Type[] types = { Option.Type.Call, Option.Type.Put };
			double[] strikes = { 50.0, 99.5, 100.0, 100.5, 150.0 };
			double[] underlyings = { 100.0 };
			double[] qRates = { 0.00, 0.10, 0.30 };
			double[] rRates = { 0.01, 0.05, 0.15 };
			int[] lengths = { 1, 2 };
			double[] vols = { 0.05, 0.20, 0.40 };

			DayCounter dc = new Actual360();
			Date today = Date.Today;
			Settings.setEvaluationDate(today);

			SimpleQuote spot = new SimpleQuote(0.0);
			SimpleQuote qRate = new SimpleQuote(0.0);
			Handle<YieldTermStructure> qTS = new Handle<YieldTermStructure>(Utilities.flatRate(qRate, dc));
			SimpleQuote rRate = new SimpleQuote(0.0);
			Handle<YieldTermStructure> rTS = new Handle<YieldTermStructure>(Utilities.flatRate(rRate, dc));
			SimpleQuote vol = new SimpleQuote(0.0);
			Handle<BlackVolTermStructure> volTS = new Handle<BlackVolTermStructure>(Utilities.flatVol(vol, dc));

			for (int i=0; i< types.Length; i++) 
			{
				for (int j=0; j<strikes.Length; j++) 
				{
					for (int k=0; k<lengths.Length; k++) 
					{
						Date exDate = today + new Period(lengths[k],TimeUnit.Years);
						Exercise exercise = new EuropeanExercise(exDate);

						List<Date> dividendDates = new List<Date>();
						List<double> dividends = new List<double>();
						for (Date d = today + new Period ( 3,TimeUnit.Months);
							  d < exercise.lastDate();
							  d += new Period(6,TimeUnit.Months)) 
						{
							dividendDates.Add(d);
							dividends.Add(5.0);
						}

						StrikedTypePayoff payoff = new PlainVanillaPayoff(types[i], strikes[j]);

						BlackScholesMertonProcess stochProcess = new BlackScholesMertonProcess(new Handle<Quote>(spot),
																					                          qTS, rTS, volTS);

						IPricingEngine engine = new AnalyticDividendEuropeanEngine(stochProcess);

						DividendVanillaOption option = new DividendVanillaOption(payoff, exercise,dividendDates, 
							                                                      dividends);
						option.setPricingEngine(engine);

						for (int l=0; l<underlyings.Length; l++) 
						{
							for (int m=0; m<qRates.Length; m++) 
							{
								for (int n=0; n<rRates.Length; n++) 
								{
									for (int p=0; p<vols.Length; p++) 
									{
										double u = underlyings[l];
										double q = qRates[m],
										r = rRates[n];
										double v = vols[p];
										spot.setValue(u);
										qRate.setValue(q);
										rRate.setValue(r);
										vol.setValue(v);

										double value = option.NPV();
										calculated["delta"]  = option.delta();
										calculated["gamma"]  = option.gamma();
										calculated["theta"]  = option.theta();
										calculated["rho"]    = option.rho();
										calculated["vega"]   = option.vega();

										if (value > spot.value()*1.0e-5) 
										{
											// perturb spot and get delta and gamma
											double du = u*1.0e-4;
											spot.setValue(u+du);
											double value_p = option.NPV(),
													delta_p = option.delta();
											spot.setValue(u-du);
											double value_m = option.NPV(),
													delta_m = option.delta();
											spot.setValue(u);
											expected["delta"] = (value_p - value_m)/(2*du);
											expected["gamma"] = (delta_p - delta_m)/(2*du);

											// perturb risk-free rate and get rho
											double dr = r*1.0e-4;
											rRate.setValue(r+dr);
											value_p = option.NPV();
											rRate.setValue(r-dr);
											value_m = option.NPV();
											rRate.setValue(r);
											expected["rho"] = (value_p - value_m)/(2*dr);

											// perturb volatility and get vega
											double dv = v*1.0e-4;
											vol.setValue(v+dv);
											value_p = option.NPV();
											vol.setValue(v-dv);
											value_m = option.NPV();
											vol.setValue(v);
											expected["vega"] = (value_p - value_m)/(2*dv);

											// perturb date and get theta
											double dT = dc.yearFraction(today-1, today+1);
											Settings.setEvaluationDate(today-1);
											value_m = option.NPV();
											Settings.setEvaluationDate(today+1);
											value_p = option.NPV();
											Settings.setEvaluationDate(today);
											expected["theta"] = (value_p - value_m)/dT;

											// compare
											foreach ( KeyValuePair<string,double> it in calculated)
											{
												string greek = it.Key;
												double expct = expected  [greek],
														 calcl = calculated[greek],
														 tol   = tolerance [greek];
												double error = Utilities.relativeError(expct,calcl,u);
												if (error>tol) 
												{
													REPORT_FAILURE(greek, payoff, exercise,
																		u, q, r, today, v,
																		expct, calcl, error, tol);
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
		}

		[TestMethod()]
		public void testFdEuropeanValues() 
		{
			// Testing finite-difference dividend European option values...

			SavedSettings backup = new SavedSettings();

			double tolerance = 1.0e-2;
			int gridPoints = 300;
			int timeSteps = 40;

			Option.Type[] types = { Option.Type.Call, Option.Type.Put };
			double[] strikes = { 50.0, 99.5, 100.0, 100.5, 150.0 };
			double[] underlyings = { 100.0 };
			// Rate qRates[] = { 0.00, 0.10, 0.30 };
			// Analytic dividend may not be handling q correctly
			double[] qRates = { 0.00 };
			double[] rRates = { 0.01, 0.05, 0.15 };
			int[] lengths = { 1, 2 };
			double[] vols = { 0.05, 0.20, 0.40 };

			DayCounter dc = new Actual360();
			Date today = Date.Today;
			Settings.setEvaluationDate(today);

			SimpleQuote spot = new SimpleQuote(0.0);
			SimpleQuote qRate = new SimpleQuote(0.0);
			Handle<YieldTermStructure> qTS = new Handle<YieldTermStructure>(Utilities.flatRate(qRate, dc));
			SimpleQuote rRate = new SimpleQuote(0.0);
			Handle<YieldTermStructure> rTS = new Handle<YieldTermStructure>(Utilities.flatRate(rRate, dc));
			SimpleQuote vol = new SimpleQuote(0.0);
			Handle<BlackVolTermStructure> volTS = new Handle<BlackVolTermStructure>(Utilities.flatVol(vol, dc));

			for (int i=0; i<types.Length; i++) 
			{
				for (int j=0; j<strikes.Length; j++) 
				{
					for (int k=0; k<lengths.Length; k++) 
					{
						Date exDate = today + new Period(lengths[k],TimeUnit.Years);
						Exercise exercise = new EuropeanExercise(exDate);

						List<Date> dividendDates = new List<Date>();
						List<double> dividends = new List<double>();
						for (Date d = today + new Period(3,TimeUnit.Months);
							  d < exercise.lastDate();	
							  d += new Period(6,TimeUnit.Months)) 
						{
							dividendDates.Add(d);
							dividends.Add(5.0);
						}

						StrikedTypePayoff payoff = new PlainVanillaPayoff(types[i], strikes[j]);

						BlackScholesMertonProcess stochProcess = new BlackScholesMertonProcess(new Handle<Quote>(spot),
																					                          qTS, rTS, volTS);

						IPricingEngine engine = new FDDividendEuropeanEngine(stochProcess, timeSteps, gridPoints);

						IPricingEngine ref_engine = new AnalyticDividendEuropeanEngine(stochProcess);

						DividendVanillaOption option = new DividendVanillaOption(payoff, exercise,dividendDates, dividends);
						option.setPricingEngine(engine);

						DividendVanillaOption ref_option = new DividendVanillaOption(payoff, exercise, dividendDates, dividends);
						ref_option.setPricingEngine(ref_engine);

						for (int l=0; l<underlyings.Length; l++) 
						{
							for (int m=0; m<qRates.Length; m++) 
							{
								for (int n=0; n<rRates.Length; n++) 
								{
									for (int p=0; p<vols.Length; p++) 
									{
										double u = underlyings[l];
										double q = qRates[m],
												 r = rRates[n];
										double v = vols[p];
										spot.setValue(u);
										qRate.setValue(q);
										rRate.setValue(r);
										vol.setValue(v);
										// FLOATING_POINT_EXCEPTION
										double calculated = option.NPV();
										if (calculated > spot.value()*1.0e-5) 
										{
											double expected = ref_option.NPV();
											double error = Math.Abs(calculated-expected);
											if (error > tolerance) 
											{
												REPORT_FAILURE("value", payoff, exercise,
																	u, q, r, today, v,
																	expected, calculated,
																	error, tolerance);
											}
										}
									}
								}
							}
						}
					}
				}
			}
		}

		[TestMethod()]
		public void testFdEuropeanGreeks()
		{

			// Testing finite-differences dividend European option greeks...
			SavedSettings backup = new SavedSettings();

			Date today = Date.Today;
			Settings.setEvaluationDate(today);
			int[] lengths = { 1, 2 };

			for (int i=0; i<lengths.Length; i++) 
			{
				Date exDate = today + new Period(lengths[i],TimeUnit.Years);
				Exercise exercise = new EuropeanExercise(exDate);
				testFdGreeks<FDDividendEuropeanEngine>(today,exercise);
			}
		}

		[TestMethod()]
		public void testFdAmericanGreeks() 
		{
			// Testing finite-differences dividend American option greeks...

			SavedSettings backup = new SavedSettings();

			Date today = Date.Today;
			Settings.setEvaluationDate(today);
			int[] lengths = { 1, 2 };

			for (int i=0; i<lengths.Length; i++) 
			{
				Date exDate = today + new Period(lengths[i],TimeUnit.Years);
				Exercise exercise = new AmericanExercise(exDate);
				testFdGreeks<FDDividendAmericanEngine>(today, exercise);
			}
	
		}

		[TestMethod()]
		public void testFdEuropeanDegenerate() 
		{
			// Testing degenerate finite-differences dividend European option...

			SavedSettings backup = new SavedSettings();

			Date today = new Date(27,Month.February,2005);
			Settings.setEvaluationDate(today);
			Date exDate = new Date(13,Month.April,2005);

			Exercise exercise = new EuropeanExercise(exDate);

			testFdDegenerate<FDDividendEuropeanEngine>(today,exercise);
		}

		[TestMethod()]
		public void testFdAmericanDegenerate()
		{
			// Testing degenerate finite-differences dividend American option...

			SavedSettings backup = new SavedSettings();

			Date today = new Date(27, Month.February, 2005);
			Settings.setEvaluationDate(today);
			Date exDate = new Date(13, Month.April, 2005);

			Exercise exercise = new AmericanExercise	(exDate);

			testFdDegenerate<FDDividendAmericanEngine>(today, exercise);
		}
	}
}
