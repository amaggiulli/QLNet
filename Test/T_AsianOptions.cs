/*
 Copyright (C) 2009 Philippe Real (ph_real@hotmail.com)
  
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
    //[TestClass()]
    //public class T_AsianOptions
    //{
    //    public struct DiscreteAverageData
    //    {
    //        public Option.Type type;
    //        public double underlying;
    //        public double strike;
    //        public double dividendYield;
    //        public double riskFreeRate;
    //        public double first;
    //        public double length;
    //        public int fixings;
    //        public double volatility;
    //        public bool controlVariate;
    //        public double result;

    //        public DiscreteAverageData(Option.Type Type,
    //                                    double Underlying,
    //                                    double Strike,
    //                                    double DividendYield,
    //                                    double RiskFreeRate,
    //                                    double First,
    //                                    double Length,
    //                                    int Fixings,
    //                                    double Volatility,
    //                                    bool ControlVariate,
    //                                    double Result)
    //        {
    //            type = Type;
    //            underlying = Underlying;
    //            strike = Strike;
    //            dividendYield = DividendYield;
    //            riskFreeRate = RiskFreeRate;
    //            first = First;
    //            length = Length;
    //            fixings = Fixings;
    //            volatility = Volatility;
    //            controlVariate = ControlVariate;
    //            result = Result;
    //        }
    //    }

    //    public void REPORT_FAILURE(string greekName, Average.Type averageType,
    //                                double runningAccumulator, int pastFixings,
    //                                List<Date> fixingDates, StrikedTypePayoff payoff,
    //                                Exercise exercise, double s, double q, double r,
    //                                Date today, double v, double expected,
    //                                double calculated, double tolerance)
    //    {
    //        Assert.Fail(exercise + " "
    //        + exercise
    //        + " Asian option with "
    //        + averageType + " and "
    //        + payoff + " payoff:\n"
    //        + "    running variable: "
    //        + runningAccumulator + "\n"
    //        + "    past fixings:     "
    //        + pastFixings + "\n"
    //        + "    future fixings:   " + fixingDates.Count() + "\n"
    //        + "    underlying value: " + s + "\n"
    //        + "    strike:           " + payoff.strike() + "\n"
    //        + "    dividend yield:   " + q + "\n"
    //        + "    risk-free rate:   " + r + "\n"
    //        + "    reference date:   " + today + "\n"
    //        + "    maturity:         " + exercise.lastDate() + "\n"
    //        + "    volatility:       " + v + "\n\n"
    //        + "    expected   " + greekName + ": " + expected + "\n"
    //        + "    calculated " + greekName + ": " + calculated + "\n"
    //        + "    error:            " + Math.Abs(expected - calculated)
    //        + "\n"
    //        + "    tolerance:        " + tolerance);
    //    }

    //    public string averageTypeToString(Average.Type averageType)
    //    {

    //        if (averageType == Average.Type.Geometric)
    //            return "Geometric Averaging";
    //        else if (averageType == Average.Type.Arithmetic)
    //            return "Arithmetic Averaging";
    //        else
    //            throw new ApplicationException("unknown averaging");
    //    }

    //    [TestMethod()]
    //    public void testAnalyticContinuousGeometricAveragePrice()
    //    {

    //        //("Testing analytic continuous geometric average-price Asians...");
    //        // data from "Option Pricing Formulas", Haug, pag.96-97

    //        DayCounter dc = new Actual360();
    //        Date today = Date.Today;

    //        SimpleQuote spot = new SimpleQuote(80.0);
    //        SimpleQuote qRate = new SimpleQuote(-0.03);
    //        YieldTermStructure qTS = Utilities.flatRate(today, qRate, dc);
    //        SimpleQuote rRate = new SimpleQuote(0.05);
    //        YieldTermStructure rTS = Utilities.flatRate(today, rRate, dc);
    //        SimpleQuote vol = new SimpleQuote(0.20);
    //        BlackVolTermStructure volTS = Utilities.flatVol(today, vol, dc);

    //        BlackScholesMertonProcess stochProcess = new
    //            BlackScholesMertonProcess(new Handle<Quote>(spot),
    //                                      new Handle<YieldTermStructure>(qTS),
    //                                      new Handle<YieldTermStructure>(rTS),
    //                                      new Handle<BlackVolTermStructure>(volTS));

    //        IPricingEngine engine = new
    //            AnalyticContinuousGeometricAveragePriceAsianEngine(stochProcess);

    //        Average.Type averageType = Average.Type.Geometric;
    //        Option.Type type = Option.Type.Put;
    //        double strike = 85.0;
    //        Date exerciseDate = today + 90;

    //        int pastFixings = 0; //Null<int>();
    //        double runningAccumulator = 0.0; //Null<Real>();

    //        StrikedTypePayoff payoff = new PlainVanillaPayoff(type, strike);

    //        Exercise exercise = new EuropeanExercise(exerciseDate);

    //        ContinuousAveragingAsianOption option =
    //            new ContinuousAveragingAsianOption(averageType, payoff, exercise);
    //        option.setPricingEngine(engine);

    //        double calculated = option.NPV();
    //        double expected = 4.6922;
    //        double tolerance = 1.0e-4;
    //        if (Math.Abs(calculated - expected) > tolerance)
    //        {
    //            REPORT_FAILURE("value", averageType, runningAccumulator, pastFixings,
    //                           new List<Date>(), payoff, exercise, spot.value(),
    //                           qRate.value(), rRate.value(), today,
    //                           vol.value(), expected, calculated, tolerance);
    //        }

    //        // trying to approximate the continuous version with the discrete version
    //        runningAccumulator = 1.0;
    //        pastFixings = 0;
    //        List<Date> fixingDates = new InitializedList<Date>(exerciseDate - today + 1);
    //        for (int i = 0; i < fixingDates.Count; i++)
    //        {
    //            fixingDates[i] = today + i;
    //        }
    //        IPricingEngine engine2 = new
    //            AnalyticDiscreteGeometricAveragePriceAsianEngine(stochProcess);

    //        DiscreteAveragingAsianOption option2 =
    //            new DiscreteAveragingAsianOption(averageType,
    //                                             runningAccumulator, pastFixings,
    //                                             fixingDates,
    //                                             payoff,
    //                                             exercise);
    //        option2.setPricingEngine(engine2);

    //        calculated = option2.NPV();
    //        tolerance = 3.0e-3;
    //        /*if (Math.Abs(calculated - expected) > tolerance)
    //        {
    //            REPORT_FAILURE("value", averageType, runningAccumulator, pastFixings,
    //                           fixingDates, payoff, exercise, spot.value(),
    //                           qRate.value(), rRate.value(), today,
    //                           vol.value(), expected, calculated, tolerance);
    //        }*/

    //    }

    //    [TestMethod()]
    //    public void testAnalyticContinuousGeometricAveragePriceGreeks()
    //    {

    //        //BOOST_MESSAGE("Testing analytic continuous geometric average-price Asian "
    //        //              "greeks...");

    //        //std::map<std::string,Real> 
    //        Dictionary<string, double> calculated, expected, tolerance;
    //        calculated = new Dictionary<string, double>(6);
    //        expected = new Dictionary<string, double>(6);
    //        tolerance = new Dictionary<string, double>(6);
    //        tolerance["delta"] = 1.0e-5;
    //        tolerance["gamma"] = 1.0e-5;
    //        tolerance["theta"] = 1.0e-5;
    //        tolerance["rho"] = 1.0e-5;
    //        tolerance["divRho"] = 1.0e-5;
    //        tolerance["vega"] = 1.0e-5;

    //        Option.Type[] types = { Option.Type.Call, Option.Type.Put };
    //        double[] underlyings = { 100.0 };
    //        double[] strikes = { 90.0, 100.0, 110.0 };
    //        double[] qRates = { 0.04, 0.05, 0.06 };
    //        double[] rRates = { 0.01, 0.05, 0.15 };
    //        int[] lengths = { 1, 2 };
    //        double[] vols = { 0.11, 0.50, 1.20 };

    //        DayCounter dc = new Actual360();
    //        Date today = Date.Today;
    //        Settings.setEvaluationDate(today);

    //        SimpleQuote spot = new SimpleQuote(0.0);
    //        SimpleQuote qRate = new SimpleQuote(0.0);
    //        Handle<YieldTermStructure> qTS =
    //            new Handle<YieldTermStructure>(Utilities.flatRate(qRate, dc));
    //        SimpleQuote rRate = new SimpleQuote(0.0);
    //        Handle<YieldTermStructure> rTS =
    //            new Handle<YieldTermStructure>(Utilities.flatRate(rRate, dc));
    //        SimpleQuote vol = new SimpleQuote(0.0);
    //        Handle<BlackVolTermStructure> volTS =
    //            new Handle<BlackVolTermStructure>(Utilities.flatVol(vol, dc));

    //        BlackScholesMertonProcess process =
    //             new BlackScholesMertonProcess(new Handle<Quote>(spot), qTS, rTS, volTS);

    //        for (int i = 0; i < types.Length; i++)
    //        {
    //            for (int j = 0; j < strikes.Length; j++)
    //            {
    //                for (int k = 0; k < lengths.Length; k++)
    //                {

    //                    EuropeanExercise maturity =
    //                        //new EuropeanExercise(today + lengths[k]*Years);
    //                                        new EuropeanExercise(today +
    //                                            new Period(lengths[k], TimeUnit.Years));
    //                    PlainVanillaPayoff payoff =
    //                                        new PlainVanillaPayoff(types[i], strikes[j]);

    //                    IPricingEngine engine = new
    //                         AnalyticContinuousGeometricAveragePriceAsianEngine(process);

    //                    ContinuousAveragingAsianOption option =
    //                        new ContinuousAveragingAsianOption(Average.Type.Geometric,
    //                                                            payoff, maturity);
    //                    option.setPricingEngine(engine);

    //                    int pastFixings = 0; //Null<Size>();
    //                    double runningAverage = 0; //Null<Real>();

    //                    for (int l = 0; l < underlyings.Length; l++)
    //                    {
    //                        for (int m = 0; m < qRates.Length; m++)
    //                        {
    //                            for (int n = 0; n < rRates.Length; n++)
    //                            {
    //                                for (int p = 0; p < vols.Length; p++)
    //                                {

    //                                    double u = underlyings[l];
    //                                    double q = qRates[m],
    //                                         r = rRates[n];
    //                                    double v = vols[p];
    //                                    spot.setValue(u);
    //                                    qRate.setValue(q);
    //                                    rRate.setValue(r);
    //                                    vol.setValue(v);

    //                                    double value = option.NPV();
    //                                    calculated["delta"] = option.delta();
    //                                    calculated["gamma"] = option.gamma();
    //                                    calculated["theta"] = option.theta();
    //                                    calculated["rho"] = option.rho();
    //                                    calculated["divRho"] = option.dividendRho();
    //                                    calculated["vega"] = option.vega();

    //                                    if (value > spot.value() * 1.0e-5)
    //                                    {
    //                                        // perturb spot and get delta and gamma
    //                                        double du = u * 1.0e-4;
    //                                        spot.setValue(u + du);
    //                                        double value_p = option.NPV(),
    //                                             delta_p = option.delta();
    //                                        spot.setValue(u - du);
    //                                        double value_m = option.NPV(),
    //                                             delta_m = option.delta();
    //                                        spot.setValue(u);
    //                                        expected["delta"] = (value_p - value_m) / (2 * du);
    //                                        expected["gamma"] = (delta_p - delta_m) / (2 * du);

    //                                        // perturb rates and get rho and dividend rho
    //                                        double dr = r * 1.0e-4;
    //                                        rRate.setValue(r + dr);
    //                                        value_p = option.NPV();
    //                                        rRate.setValue(r - dr);
    //                                        value_m = option.NPV();
    //                                        rRate.setValue(r);
    //                                        expected["rho"] = (value_p - value_m) / (2 * dr);

    //                                        double dq = q * 1.0e-4;
    //                                        qRate.setValue(q + dq);
    //                                        value_p = option.NPV();
    //                                        qRate.setValue(q - dq);
    //                                        value_m = option.NPV();
    //                                        qRate.setValue(q);
    //                                        expected["divRho"] = (value_p - value_m) / (2 * dq);

    //                                        // perturb volatility and get vega
    //                                        double dv = v * 1.0e-4;
    //                                        vol.setValue(v + dv);
    //                                        value_p = option.NPV();
    //                                        vol.setValue(v - dv);
    //                                        value_m = option.NPV();
    //                                        vol.setValue(v);
    //                                        expected["vega"] = (value_p - value_m) / (2 * dv);

    //                                        // perturb date and get theta
    //                                        double dT = dc.yearFraction(today - 1, today + 1);
    //                                        Settings.setEvaluationDate(today - 1);
    //                                        value_m = option.NPV();
    //                                        Settings.setEvaluationDate(today + 1);
    //                                        value_p = option.NPV();
    //                                        Settings.setEvaluationDate(today);
    //                                        expected["theta"] = (value_p - value_m) / dT;

    //                                        // compare
    //                                        foreach (KeyValuePair<string, double> kvp in calculated)
    //                                        {
    //                                            string greek = kvp.Key;
    //                                            double expct = expected[greek],
    //                                                 calcl = calculated[greek],
    //                                                 tol = tolerance[greek];
    //                                            double error = Utilities.relativeError(expct, calcl, u);
    //                                            if (error > tol)
    //                                            {
    //                                                REPORT_FAILURE(greek, Average.Type.Geometric,
    //                                                               runningAverage, pastFixings,
    //                                                               new List<Date>(),
    //                                                               payoff, maturity,
    //                                                               u, q, r, today, v,
    //                                                               expct, calcl, tol);
    //                                            }
    //                                        }
    //                                    }
    //                                }
    //                            }
    //                        }
    //                    }
    //                }
    //            }
    //        }
    //    }

    //    [TestMethod()]
    //    public void testAnalyticDiscreteGeometricAveragePrice()
    //    {

    //        //BOOST_MESSAGE("Testing analytic discrete geometric average-price Asians...");

    //        // data from "Implementing Derivatives Model",
    //        // Clewlow, Strickland, p.118-123

    //        DayCounter dc = new Actual360();
    //        Date today = Date.Today;

    //        SimpleQuote spot = new SimpleQuote(100.0);
    //        SimpleQuote qRate = new SimpleQuote(0.03);
    //        YieldTermStructure qTS = Utilities.flatRate(today, qRate, dc);
    //        SimpleQuote rRate = new SimpleQuote(0.06);
    //        YieldTermStructure rTS = Utilities.flatRate(today, rRate, dc);
    //        SimpleQuote vol = new SimpleQuote(0.20);
    //        BlackVolTermStructure volTS = Utilities.flatVol(today, vol, dc);

    //        BlackScholesMertonProcess stochProcess = new
    //            BlackScholesMertonProcess(new Handle<Quote>(spot),
    //                                      new Handle<YieldTermStructure>(qTS),
    //                                      new Handle<YieldTermStructure>(rTS),
    //                                      new Handle<BlackVolTermStructure>(volTS));

    //        IPricingEngine engine =
    //              new AnalyticDiscreteGeometricAveragePriceAsianEngine(stochProcess);

    //        Average.Type averageType = Average.Type.Geometric;
    //        double runningAccumulator = 1.0;
    //        int pastFixings = 0;
    //        int futureFixings = 10;
    //        Option.Type type = Option.Type.Call;
    //        double strike = 100.0;
    //        StrikedTypePayoff payoff = new PlainVanillaPayoff(type, strike);

    //        Date exerciseDate = today + 360;
    //        Exercise exercise = new EuropeanExercise(exerciseDate);

    //        List<Date> fixingDates = new InitializedList<Date>(futureFixings);
    //        int dt = (int)(360 / futureFixings + 0.5);
    //        fixingDates[0] = today + dt;
    //        for (int j = 1; j < futureFixings; j++)
    //            fixingDates[j] = fixingDates[j - 1] + dt;

    //        DiscreteAveragingAsianOption option =
    //            new DiscreteAveragingAsianOption(averageType, runningAccumulator,
    //                                            pastFixings, fixingDates,
    //                                            payoff, exercise);
    //        option.setPricingEngine(engine);

    //        double calculated = option.NPV();
    //        double expected = 5.3425606635;
    //        double tolerance = 1e-10;
    //        if (Math.Abs(calculated - expected) > tolerance)
    //        {
    //            REPORT_FAILURE("value", averageType, runningAccumulator, pastFixings,
    //                           fixingDates, payoff, exercise, spot.value(),
    //                           qRate.value(), rRate.value(), today,
    //                           vol.value(), expected, calculated, tolerance);
    //        }
    //    }

    //    [TestMethod()]
    //    public void testMCDiscreteGeometricAveragePrice() {

    //    //BOOST_MESSAGE("Testing Monte Carlo discrete geometric average-price Asians...");

    //    // data from "Implementing Derivatives Model",
    //    // Clewlow, Strickland, p.118-123

    //    DayCounter dc = new Actual360();
    //    Date today = Date.Today;

    //    SimpleQuote spot = new SimpleQuote(100.0);
    //    SimpleQuote qRate = new SimpleQuote(0.03);
    //    YieldTermStructure qTS = Utilities.flatRate(today, qRate, dc);
    //    SimpleQuote rRate = new SimpleQuote(0.06);
    //    YieldTermStructure rTS = Utilities.flatRate(today, rRate, dc);
    //    SimpleQuote vol  = new SimpleQuote(0.20);
    //    BlackVolTermStructure volTS =Utilities.flatVol(today, vol, dc);

    //    BlackScholesMertonProcess stochProcess = 
    //        new BlackScholesMertonProcess(new Handle<Quote>(spot),
    //                                    new Handle<YieldTermStructure>(qTS),
    //                                    new Handle<YieldTermStructure>(rTS),
    //                                    new Handle<BlackVolTermStructure>(volTS));

    //    double tolerance = 4.0e-3;

    //    IPricingEngine engine =
    //    new MakeMCDiscreteGeometricAPEngine
    //                            <LowDiscrepancy,Statistics>(stochProcess)
    //                            .withStepsPerYear(1)
    //                            .withSamples(8191)
    //                            .value(); 

    //    Average.Type averageType = Average.Type.Geometric;
    //    double runningAccumulator = 1.0;
    //    int pastFixings = 0;
    //    int futureFixings = 10;
    //    Option.Type type = Option.Type.Call;
    //    double strike = 100.0;
    //    StrikedTypePayoff payoff = new PlainVanillaPayoff(type, strike); 

    //    Date exerciseDate = today + 360;
    //    Exercise exercise = new EuropeanExercise(exerciseDate);

    //    List<Date> fixingDates = new InitializedList<Date>(futureFixings);
    //    int dt = (int)(360/futureFixings+0.5);
    //    fixingDates[0] = today + dt;
    //    for (int j=1; j<futureFixings; j++)
    //        fixingDates[j] = fixingDates[j-1] + dt;

    //    DiscreteAveragingAsianOption  option =
    //        new DiscreteAveragingAsianOption(averageType, runningAccumulator,
    //                                        pastFixings, fixingDates,
    //                                        payoff, exercise);
    //    option.setPricingEngine(engine);

    //    double calculated = option.NPV();

    //    IPricingEngine engine2 =
    //          new AnalyticDiscreteGeometricAveragePriceAsianEngine(stochProcess);
    //    option.setPricingEngine(engine2);
    //    double expected = option.NPV();

    //    if (Math.Abs(calculated-expected) > tolerance) {
    //        REPORT_FAILURE("value", averageType, runningAccumulator, pastFixings,
    //                       fixingDates, payoff, exercise, spot.value(),
    //                       qRate.value(), rRate.value(), today,
    //                       vol.value(), expected, calculated, tolerance);
    //    }
    //}

    //    [TestMethod()]
    //    public void testMCDiscreteArithmeticAveragePrice() {

    //        //BOOST_MESSAGE("Testing Monte Carlo discrete arithmetic average-price Asians...");

    //        //QL_TEST_START_TIMING

    //        // data from "Asian Option", Levy, 1997
    //        // in "Exotic Options: The State of the Art",
    //        // edited by Clewlow, Strickland

    //        DiscreteAverageData[] cases4 = {
    //        new DiscreteAverageData(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 0.0,11.0/12.0, 2,0.13, true, 1.3942835683),
    //        new DiscreteAverageData(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 0.0,11.0/12.0, 4,0.13, true, 1.5852442983),
    //        new DiscreteAverageData(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 0.0,11.0/12.0, 8,0.13, true, 1.66970673),
    //        new DiscreteAverageData(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 0.0,11.0/12.0, 12,0.13, true, 1.6980019214),
    //        new DiscreteAverageData(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 0.0,11.0/12.0, 26,0.13, true, 1.7255070456),
    //        new DiscreteAverageData(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 0.0,11.0/12.0, 52,0.13, true, 1.7401553533),
    //        new DiscreteAverageData(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 0.0,11.0/12.0, 100,0.13, true, 1.7478303712),
    //        new DiscreteAverageData(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 0.0,11.0/12.0, 250,0.13, true, 1.7490291943),
    //        new DiscreteAverageData(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 0.0,11.0/12.0, 500,0.13, true, 1.7515113291),
    //        new DiscreteAverageData(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 0.0,11.0/12.0, 1000,0.13, true, 1.7537344885),
    //        new DiscreteAverageData(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 1.0/12.0,11.0/12.0, 2,0.13, true, 1.8496053697),
    //        new DiscreteAverageData(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 1.0/12.0,11.0/12.0, 4,0.13, true, 2.0111495205),
    //        new DiscreteAverageData(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 1.0/12.0,11.0/12.0, 8,0.13, true, 2.0852138818),
    //        new DiscreteAverageData(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 1.0/12.0,11.0/12.0, 12,0.13, true, 2.1105094397),
    //        new DiscreteAverageData(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 1.0/12.0,11.0/12.0, 26,0.13, true, 2.1346526695),
    //        new DiscreteAverageData(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 1.0/12.0,11.0/12.0, 52,0.13, true, 2.147489651),
    //        new DiscreteAverageData(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 1.0/12.0,11.0/12.0, 100,0.13, true, 2.154728109),
    //        new DiscreteAverageData(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 1.0/12.0,11.0/12.0, 250,0.13, true, 2.1564276565),
    //        new DiscreteAverageData(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 1.0/12.0,11.0/12.0, 500,0.13, true, 2.1594238588),
    //        new DiscreteAverageData(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 1.0/12.0,11.0/12.0, 1000,0.13, true, 2.1595367326),
    //        new DiscreteAverageData(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 3.0/12.0,11.0/12.0, 2,0.13, true, 2.63315092584),
    //        new DiscreteAverageData(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 3.0/12.0,11.0/12.0, 4,0.13, true, 2.76723962361),
    //        new DiscreteAverageData(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 3.0/12.0,11.0/12.0, 8,0.13, true, 2.83124836881),
    //        new DiscreteAverageData(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 3.0/12.0,11.0/12.0, 12,0.13, true, 2.84290301412),
    //        new DiscreteAverageData(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 3.0/12.0,11.0/12.0, 26,0.13, true, 2.88179560417),
    //        new DiscreteAverageData(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 3.0/12.0,11.0/12.0, 52,0.13, true, 2.88447044543),
    //        new DiscreteAverageData(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 3.0/12.0,11.0/12.0, 100,0.13, true, 2.89985329603),
    //        new DiscreteAverageData(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 3.0/12.0,11.0/12.0, 250,0.13, true, 2.90047296063),
    //        new DiscreteAverageData(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 3.0/12.0,11.0/12.0, 500,0.13, true, 2.89813412160),
    //        new DiscreteAverageData(Option.Type.Put, 90.0, 87.0, 0.06, 0.025, 3.0/12.0,11.0/12.0, 1000,0.13, true, 2.89703362437)
    //        };
          
    //        DayCounter dc = new Actual360();
    //        Date today = Date.Today ;

    //        SimpleQuote spot = new SimpleQuote(100.0);
    //        SimpleQuote qRate = new SimpleQuote(0.03);
    //        YieldTermStructure qTS = Utilities.flatRate(today, qRate, dc);
    //        SimpleQuote rRate = new SimpleQuote(0.06);
    //        YieldTermStructure rTS = Utilities.flatRate(today, rRate, dc);
    //        SimpleQuote vol = new SimpleQuote(0.20);
    //        BlackVolTermStructure volTS = Utilities.flatVol(today, vol, dc);



    //        Average.Type averageType = Average.Type.Arithmetic;
    //        double runningSum = 0.0;
    //        int pastFixings = 0;
    //        for (int l=0; l<cases4.Length ; l++) {

    //            StrikedTypePayoff payoff = new
    //                PlainVanillaPayoff(cases4[l].type, cases4[l].strike);

    //            double dt = cases4[l].length/(cases4[l].fixings-1);
    //            List<double> timeIncrements = new QLNet.InitializedList<double>(cases4[l].fixings);
    //            List<Date> fixingDates = new QLNet.InitializedList<Date>(cases4[l].fixings);
    //            timeIncrements[0] = cases4[l].first;
    //            fixingDates[0] = today + (int)(timeIncrements[0]*360+0.5);
    //            for (int i=1; i<cases4[l].fixings; i++) {
    //                timeIncrements[i] = i*dt + cases4[l].first;
    //                fixingDates[i] = today + (int)(timeIncrements[i]*360+0.5);
    //            }
    //            Exercise exercise = new EuropeanExercise(fixingDates[cases4[l].fixings-1]); 

    //            spot.setValue(cases4[l].underlying);
    //            qRate.setValue(cases4[l].dividendYield);
    //            rRate.setValue(cases4[l].riskFreeRate);
    //            vol.setValue(cases4[l].volatility);

    //            BlackScholesMertonProcess stochProcess = 
    //                new BlackScholesMertonProcess(new Handle<Quote>(spot),
    //                                            new Handle<YieldTermStructure>(qTS),
    //                                            new Handle<YieldTermStructure>(rTS),
    //                                            new Handle<BlackVolTermStructure>(volTS));

    //            ulong seed=42;
    //            const int nrTrails = 5000;
    //            LowDiscrepancy.icInstance = new InverseCumulativeNormal();
    //            IRNG rsg = (IRNG)new LowDiscrepancy().make_sequence_generator(nrTrails,seed);

    //            new PseudoRandom().make_sequence_generator(nrTrails,seed);

    //            IPricingEngine engine =
    //                new MakeMCDiscreteArithmeticAPEngine<LowDiscrepancy, Statistics>(stochProcess)
    //                    .withStepsPerYear(1)
    //                    .withSamples(2047)
    //                    .withControlVariate()
    //                    .value();
    //            DiscreteAveragingAsianOption option= 
    //                new DiscreteAveragingAsianOption(averageType, runningSum,
    //                                                pastFixings, fixingDates,
    //                                                payoff, exercise);
    //            option.setPricingEngine(engine);

    //            double calculated = option.NPV();
    //            double expected = cases4[l].result;
    //            double tolerance = 2.0e-2;
    //            if (Math.Abs(calculated-expected) > tolerance) {
    //                REPORT_FAILURE("value", averageType, runningSum, pastFixings,
    //                            fixingDates, payoff, exercise, spot.value(),
    //                            qRate.value(), rRate.value(), today,
    //                            vol.value(), expected, calculated, tolerance);
    //            }
    //        }
    //    }
     
    //    [TestMethod()]
    //    public void testMCDiscreteArithmeticAverageStrike() {

    //        //BOOST_MESSAGE("Testing Monte Carlo discrete arithmetic average-strike Asians...");

    //        //QL_TEST_START_TIMING

    //        // data from "Asian Option", Levy, 1997
    //        // in "Exotic Options: The State of the Art",
    //        // edited by Clewlow, Strickland
    //        DiscreteAverageData[] cases5 = {
    //            new DiscreteAverageData(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 0.0, 11.0/12.0, 2,
    //              0.13, true, 1.51917595129 ),
    //            new DiscreteAverageData(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 0.0, 11.0/12.0, 4,
    //              0.13, true, 1.67940165674 ),
    //            new DiscreteAverageData(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 0.0, 11.0/12.0, 8,
    //              0.13, true, 1.75371215251 ),
    //            new DiscreteAverageData(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 0.0, 11.0/12.0, 12,
    //              0.13, true, 1.77595318693 ),
    //            new DiscreteAverageData(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 0.0, 11.0/12.0, 26,
    //              0.13, true, 1.81430536630 ),
    //            new DiscreteAverageData(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 0.0, 11.0/12.0, 52,
    //              0.13, true, 1.82269246898 ),
    //            new DiscreteAverageData(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 0.0, 11.0/12.0, 100,
    //              0.13, true, 1.83822402464 ),
    //            new DiscreteAverageData(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 0.0, 11.0/12.0, 250,
    //              0.13, true, 1.83875059026 ),
    //            new DiscreteAverageData(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 0.0, 11.0/12.0, 500,
    //              0.13, true, 1.83750703638 ),
    //            new DiscreteAverageData(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 0.0, 11.0/12.0, 1000,
    //              0.13, true, 1.83887181884 ),
    //            new DiscreteAverageData(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 1.0/12.0, 11.0/12.0, 2,
    //              0.13, true, 1.51154400089 ),
    //            new DiscreteAverageData(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 1.0/12.0, 11.0/12.0, 4,
    //              0.13, true, 1.67103508506 ),
    //            new DiscreteAverageData(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 1.0/12.0, 11.0/12.0, 8,
    //              0.13, true, 1.74529684070 ),
    //            new DiscreteAverageData(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 1.0/12.0, 11.0/12.0, 12,
    //              0.13, true, 1.76667074564 ),
    //            new DiscreteAverageData(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 1.0/12.0, 11.0/12.0, 26,
    //              0.13, true, 1.80528400613 ),
    //            new DiscreteAverageData(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 1.0/12.0, 11.0/12.0, 52,
    //              0.13, true, 1.81400883891 ),
    //            new DiscreteAverageData(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 1.0/12.0, 11.0/12.0, 100,
    //              0.13, true, 1.82922901451 ),
    //            new DiscreteAverageData(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 1.0/12.0, 11.0/12.0, 250,
    //              0.13, true, 1.82937111773 ),
    //            new DiscreteAverageData(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 1.0/12.0, 11.0/12.0, 500,
    //              0.13, true, 1.82826193186 ),
    //            new DiscreteAverageData(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 1.0/12.0, 11.0/12.0, 1000,
    //              0.13, true, 1.82967846654 ),
    //            new DiscreteAverageData(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 3.0/12.0, 11.0/12.0, 2,
    //              0.13, true, 1.49648170891 ),
    //            new DiscreteAverageData(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 3.0/12.0, 11.0/12.0, 4,
    //              0.13, true, 1.65443100462 ),
    //            new DiscreteAverageData(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 3.0/12.0, 11.0/12.0, 8,
    //              0.13, true, 1.72817806731 ),
    //            new DiscreteAverageData(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 3.0/12.0, 11.0/12.0, 12,
    //              0.13, true, 1.74877367895 ),
    //            new DiscreteAverageData(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 3.0/12.0, 11.0/12.0, 26,
    //              0.13, true, 1.78733801988 ),
    //            new DiscreteAverageData(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 3.0/12.0, 11.0/12.0, 52,
    //              0.13, true, 1.79624826757 ),
    //            new DiscreteAverageData(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 3.0/12.0, 11.0/12.0, 100,
    //              0.13, true, 1.81114186876 ),
    //            new DiscreteAverageData(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 3.0/12.0, 11.0/12.0, 250,
    //              0.13, true, 1.81101152587 ),
    //            new DiscreteAverageData(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 3.0/12.0, 11.0/12.0, 500,
    //              0.13, true, 1.81002311939 ),
    //            new DiscreteAverageData(Option.Type.Call, 90.0, 87.0, 0.06, 0.025, 3.0/12.0, 11.0/12.0, 1000,
    //              0.13, true, 1.81145760308 )
    //        };

    //        DayCounter dc = new Actual360();
    //        Date today = Date.Today ;

    //        SimpleQuote spot = new SimpleQuote(100.0);
    //        SimpleQuote qRate = new SimpleQuote(0.03);
    //        YieldTermStructure qTS =Utilities.flatRate(today, qRate, dc);
    //        SimpleQuote rRate = new SimpleQuote(0.06);
    //        YieldTermStructure rTS = Utilities.flatRate(today, rRate, dc);
    //        SimpleQuote vol = new SimpleQuote(0.20);
    //        BlackVolTermStructure volTS = Utilities.flatVol(today, vol, dc);

    //        Average.Type averageType = QLNet.Average.Type.Arithmetic;
    //        double runningSum = 0.0;
    //        int pastFixings = 0;
    //        for (int l=0; l<cases5.Length; l++) {

    //            StrikedTypePayoff payoff = 
    //                new PlainVanillaPayoff(cases5[l].type, cases5[l].strike);

    //            double dt = cases5[l].length/(cases5[l].fixings-1);
    //            List<double> timeIncrements = new InitializedList<double>(cases5[l].fixings);
    //            List<Date> fixingDates = new InitializedList<Date>(cases5[l].fixings);
    //            timeIncrements[0] = cases5[l].first;
    //            fixingDates[0] = today + (int)(timeIncrements[0]*360+0.5);
    //            for (int i=1; i<cases5[l].fixings; i++) {
    //                timeIncrements[i] = i*dt + cases5[l].first;
    //                fixingDates[i] = today + (int)(timeIncrements[i]*360+0.5);
    //            }
    //            Exercise exercise = new EuropeanExercise(fixingDates[cases5[l].fixings-1]);

    //            spot.setValue(cases5[l].underlying);
    //            qRate.setValue(cases5[l].dividendYield);
    //            rRate.setValue(cases5[l].riskFreeRate);
    //            vol.setValue(cases5[l].volatility);

    //            BlackScholesMertonProcess stochProcess = 
    //                new BlackScholesMertonProcess(new Handle<Quote>(spot),
    //                                            new Handle<YieldTermStructure>(qTS),
    //                                            new Handle<YieldTermStructure>(rTS),
    //                                            new Handle<BlackVolTermStructure>(volTS));

    //            IPricingEngine engine =
    //                new MakeMCDiscreteArithmeticASEngine<LowDiscrepancy,Statistics>(stochProcess)
    //                .withSeed(3456789)
    //                .withSamples(1023)
    //                .value() ;

    //            DiscreteAveragingAsianOption option = 
    //                new DiscreteAveragingAsianOption(averageType, runningSum,
    //                                                pastFixings, fixingDates,
    //                                                payoff, exercise);
    //            option.setPricingEngine(engine);

    //            double calculated = option.NPV();
    //            double expected = cases5[l].result;
    //            double tolerance = 2.0e-2;
    //            if (Math.Abs(calculated-expected) > tolerance) {
    //                REPORT_FAILURE("value", averageType, runningSum, pastFixings,
    //                               fixingDates, payoff, exercise, spot.value(),
    //                               qRate.value(), rRate.value(), today,
    //                               vol.value(), expected, calculated, tolerance);
    //            }
    //        }
    //    }
            
    //    [TestMethod()]
    //    public void testAnalyticDiscreteGeometricAveragePriceGreeks() {

    //         //BOOST_MESSAGE("Testing discrete-averaging geometric Asian greeks...");

    //         //SavedSettings backup;

    //         Dictionary<string,double> calculated, expected, tolerance;
    //         calculated = new Dictionary<string, double>(6);
    //         expected = new Dictionary<string, double>(6);
    //         tolerance = new Dictionary<string, double>(6);
    //         tolerance["delta"]  = 1.0e-5;
    //         tolerance["gamma"]  = 1.0e-5;
    //         tolerance["theta"]  = 1.0e-5;
    //         tolerance["rho"]    = 1.0e-5;
    //         tolerance["divRho"] = 1.0e-5;
    //         tolerance["vega"]   = 1.0e-5;

    //         Option.Type[] types = { Option.Type.Call, Option.Type.Put };
    //         double[] underlyings = { 100.0 };
    //         double[] strikes = { 90.0, 100.0, 110.0 };
    //         double[] qRates = { 0.04, 0.05, 0.06 };
    //         double[] rRates = { 0.01, 0.05, 0.15 };
    //         int[] lengths = { 1, 2 };
    //         double[] vols = { 0.11, 0.50, 1.20 };

    //         DayCounter dc = new Actual360();
    //         Date today = Date.Today;
    //         Settings.setEvaluationDate(today);

    //         SimpleQuote spot = new SimpleQuote(0.0);
    //         SimpleQuote qRate = new SimpleQuote(0.0);
    //         Handle<YieldTermStructure> qTS = new Handle<YieldTermStructure>
    //                                                (Utilities.flatRate(qRate, dc));
    //         SimpleQuote rRate = new SimpleQuote(0.0);
    //         Handle<YieldTermStructure> rTS = new Handle<YieldTermStructure>
    //                                                (Utilities.flatRate(rRate, dc));
    //         SimpleQuote vol = new SimpleQuote(0.0);
    //         Handle<BlackVolTermStructure> volTS = new Handle<BlackVolTermStructure>
    //                                                (Utilities.flatVol(vol, dc));

    //         BlackScholesMertonProcess process = 
    //              new BlackScholesMertonProcess(new Handle<Quote>(spot), qTS, rTS, volTS);

    //         for (int i=0; i<types.Length ; i++) {
    //           for (int j=0; j<strikes.Length ; j++) {
    //             for (int k=0; k<lengths.Length ; k++) {

    //                 EuropeanExercise maturity =
    //                                   new EuropeanExercise(
    //                                       today + new Period(lengths[k],TimeUnit.Years));

    //                 PlainVanillaPayoff payoff =
    //                                     new PlainVanillaPayoff(types[i], strikes[j]);

    //                 double runningAverage = 120;
    //                 int pastFixings = 1;

    //                 List<Date> fixingDates = new List<Date>();
    //                 for (Date d = today + new Period(3, TimeUnit.Months);
    //                           d <= maturity.lastDate();
    //                           d += new Period(3, TimeUnit.Months))
    //                     fixingDates.Add(d);


    //                 IPricingEngine engine = 
    //                    new AnalyticDiscreteGeometricAveragePriceAsianEngine(process);

    //                 DiscreteAveragingAsianOption option = 
    //                     new DiscreteAveragingAsianOption(Average.Type.Geometric,
    //                                                     runningAverage, pastFixings,
    //                                                     fixingDates, payoff, maturity);
    //                 option.setPricingEngine(engine);

    //                 for (int l=0; l<underlyings.Length ; l++) {
    //                   for (int m=0; m<qRates.Length ; m++) {
    //                     for (int n=0; n<rRates.Length ; n++) {
    //                       for (int p=0; p<vols.Length ; p++) {

    //                           double u = underlyings[l];
    //                           double q = qRates[m],
    //                                r = rRates[n];
    //                           double v = vols[p];
    //                           spot.setValue(u);
    //                           qRate.setValue(q);
    //                           rRate.setValue(r);
    //                           vol.setValue(v);

    //                           double value = option.NPV();
    //                           calculated["delta"]  = option.delta();
    //                           calculated["gamma"]  = option.gamma();
    //                           calculated["theta"]  = option.theta();
    //                           calculated["rho"]    = option.rho();
    //                           calculated["divRho"] = option.dividendRho();
    //                           calculated["vega"]   = option.vega();

    //                           if (value > spot.value()*1.0e-5) {
    //                               // perturb spot and get delta and gamma
    //                               double du = u*1.0e-4;
    //                               spot.setValue(u+du);
    //                               double value_p = option.NPV(),
    //                                    delta_p = option.delta();
    //                               spot.setValue(u-du);
    //                               double value_m = option.NPV(),
    //                                    delta_m = option.delta();
    //                               spot.setValue(u);
    //                               expected["delta"] = (value_p - value_m)/(2*du);
    //                               expected["gamma"] = (delta_p - delta_m)/(2*du);

    //                               // perturb rates and get rho and dividend rho
    //                               double dr = r*1.0e-4;
    //                               rRate.setValue(r+dr);
    //                               value_p = option.NPV();
    //                               rRate.setValue(r-dr);
    //                               value_m = option.NPV();
    //                               rRate.setValue(r);
    //                               expected["rho"] = (value_p - value_m)/(2*dr);

    //                               double dq = q*1.0e-4;
    //                               qRate.setValue(q+dq);
    //                               value_p = option.NPV();
    //                               qRate.setValue(q-dq);
    //                               value_m = option.NPV();
    //                               qRate.setValue(q);
    //                               expected["divRho"] = (value_p - value_m)/(2*dq);

    //                               // perturb volatility and get vega
    //                               double dv = v*1.0e-4;
    //                               vol.setValue(v+dv);
    //                               value_p = option.NPV();
    //                               vol.setValue(v-dv);
    //                               value_m = option.NPV();
    //                               vol.setValue(v);
    //                               expected["vega"] = (value_p - value_m)/(2*dv);

    //                               // perturb date and get theta
    //                               double dT = dc.yearFraction(today-1, today+1);
    //                               Settings.setEvaluationDate(today-1);
    //                               value_m = option.NPV();
    //                               Settings.setEvaluationDate(today+1);
    //                               value_p = option.NPV();
    //                               Settings.setEvaluationDate(today);
    //                               expected["theta"] = (value_p - value_m)/dT;

    //                               // compare
    //                               foreach (KeyValuePair<string, double> kvp in calculated){
    //                                   string greek = kvp.Key;
    //                                   double expct = expected[greek],
    //                                        calcl = calculated[greek],
    //                                        tol   = tolerance [greek];
    //                                   double error =Utilities.relativeError(expct,calcl,u);
    //                                   if (error>tol) {
    //                                       REPORT_FAILURE(greek, Average.Type.Geometric,
    //                                                      runningAverage, pastFixings,
    //                                                      new List<Date>(),
    //                                                      payoff, maturity,
    //                                                      u, q, r, today, v,
    //                                                      expct, calcl, tol);
    //                                   }
    //                               }
    //                           }
    //                       }
    //                     }
    //                   }
    //                 }
    //             }
    //           }
    //         }
    //     }

    //    [TestMethod()]
    //    public void testPastFixings() {

    //        //BOOST_MESSAGE("Testing use of past fixings in Asian options...");
    //        DayCounter dc = new Actual360();
    //        Date today = Date.Today ;

    //        SimpleQuote spot = new SimpleQuote(100.0);
    //        SimpleQuote qRate = new SimpleQuote(0.03);
    //        YieldTermStructure qTS = Utilities.flatRate(today, qRate, dc);
    //        SimpleQuote rRate = new SimpleQuote(0.06);
    //        YieldTermStructure rTS = Utilities.flatRate(today, rRate, dc);
    //        SimpleQuote vol = new SimpleQuote(0.20);
    //        BlackVolTermStructure volTS = Utilities.flatVol(today, vol, dc);

    //        StrikedTypePayoff payoff = new PlainVanillaPayoff(Option.Type.Put, 100.0);


    //        Exercise exercise = new EuropeanExercise(today + new Period(1,TimeUnit.Years));

    //        BlackScholesMertonProcess stochProcess = 
    //            new BlackScholesMertonProcess(new Handle<Quote>(spot),
    //                                          new Handle<YieldTermStructure>(qTS),
    //                                          new Handle<YieldTermStructure>(rTS),
    //                                          new Handle<BlackVolTermStructure>(volTS));

    //        // MC arithmetic average-price
    //        double runningSum = 0.0;
    //        int pastFixings = 0;
    //        List<Date> fixingDates1 = new InitializedList<Date>();
    //        for (int i=0; i<=12; ++i)
    //            fixingDates1.Add(today + new Period(i,TimeUnit.Months));

    //        DiscreteAveragingAsianOption option1 = 
    //            new DiscreteAveragingAsianOption(Average.Type.Arithmetic, runningSum,
    //                                             pastFixings, fixingDates1,
    //                                             payoff, exercise);

    //        pastFixings = 2;
    //        runningSum = pastFixings * spot.value() * 0.8;
    //        List<Date> fixingDates2 = new InitializedList<Date>();
    //        for (int i=-2; i<=12; ++i)
    //            fixingDates2.Add(today + new Period(i,TimeUnit.Months));

    //        DiscreteAveragingAsianOption option2 = 
    //            new DiscreteAveragingAsianOption(Average.Type.Arithmetic, runningSum,
    //                                             pastFixings, fixingDates2,
    //                                             payoff, exercise);

    //        IPricingEngine engine =
    //           new MakeMCDiscreteArithmeticAPEngine<LowDiscrepancy,Statistics>(stochProcess)
    //            .withStepsPerYear(1)
    //            .withSamples(2047)
    //            .value() ;

    //        option1.setPricingEngine(engine);
    //        option2.setPricingEngine(engine);

    //        double price1 = option1.NPV();
    //        double price2 = option2.NPV();

    //        if (Utils.close(price1, price2)) {
    //            Assert.Fail(
    //                 "past fixings had no effect on arithmetic average-price option"
    //                 + "\n  without fixings: " + price1
    //                 + "\n  with fixings:    " + price2);
    //        }

    //        // MC arithmetic average-strike
    //        engine = new MakeMCDiscreteArithmeticASEngine<LowDiscrepancy,Statistics>(stochProcess) 
    //            .withSamples(2047)
    //            .value();
                
    //        option1.setPricingEngine(engine);
    //        option2.setPricingEngine(engine);

    //        price1 = option1.NPV();
    //        price2 = option2.NPV();

    //        if (Utils.close(price1, price2)) {
    //            Assert.Fail(
    //                 "past fixings had no effect on arithmetic average-strike option"
    //                 + "\n  without fixings: " + price1
    //                 + "\n  with fixings:    " + price2);
    //        }

    //        // analytic geometric average-price
    //        double runningProduct = 1.0;
    //        pastFixings = 0;

    //        DiscreteAveragingAsianOption option3 = 
    //            new DiscreteAveragingAsianOption(Average.Type.Geometric, runningProduct,
    //                                             pastFixings, fixingDates1,
    //                                             payoff, exercise);

    //        pastFixings = 2;
    //        runningProduct = spot.value() * spot.value();

    //        DiscreteAveragingAsianOption option4 = 
    //            new DiscreteAveragingAsianOption(Average.Type.Geometric, runningProduct,
    //                                             pastFixings, fixingDates2,
    //                                             payoff, exercise);

    //        engine = new AnalyticDiscreteGeometricAveragePriceAsianEngine(stochProcess);

    //        option3.setPricingEngine(engine);
    //        option4.setPricingEngine(engine);

    //        double price3 = option3.NPV();
    //        double price4 = option4.NPV();

    //        if (Utils.close(price3, price4)) {
    //            Assert.Fail(
    //                 "past fixings had no effect on geometric average-price option"
    //                 + "\n  without fixings: " + price3
    //                 + "\n  with fixings:    " + price4);
    //        }

    //        // MC geometric average-price
    //        engine = new MakeMCDiscreteGeometricAPEngine<LowDiscrepancy,Statistics>(stochProcess)
    //                    .withStepsPerYear(1)
    //                    .withSamples(2047)
    //                    .value();

    //        option3.setPricingEngine(engine);
    //        option4.setPricingEngine(engine);

    //        price3 = option3.NPV();
    //        price4 = option4.NPV();

    //        if (Utils.close(price3, price4)) {
    //            Assert.Fail(
    //                 "past fixings had no effect on geometric average-price option"
    //                 + "\n  without fixings: " + price3
    //                 + "\n  with fixings:    " + price4);
    //        }
    //    }
        
    //    public void suite() {
    //    //BOOST_TEST_SUITE("Asian option tests");
    //        testAnalyticContinuousGeometricAveragePrice();
    //        testAnalyticContinuousGeometricAveragePriceGreeks();
    //        testAnalyticDiscreteGeometricAveragePrice();
    //        testMCDiscreteGeometricAveragePrice();
    //        testMCDiscreteArithmeticAveragePrice();
    //        testMCDiscreteArithmeticAverageStrike();
    //        testAnalyticDiscreteGeometricAveragePriceGreeks();
    //        testPastFixings();

    //    }
    //}
}