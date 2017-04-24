/*
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
using QLNet;

namespace EquityOption {
    class EquityOption {
        static void Main(string[] args) {

            DateTime timer = DateTime.Now;

            // set up dates
            Calendar calendar = new TARGET();
            Date todaysDate = new Date(15, Month.May, 1998);
            Date settlementDate = new Date(17, Month.May, 1998);
            Settings.setEvaluationDate(todaysDate);

            // our options
            Option.Type type = Option.Type.Put;
            double underlying = 36;
            double strike = 40;
            double dividendYield = 0.00;
            double riskFreeRate = 0.06;
            double volatility = 0.20;
            Date maturity = new Date(17, Month.May, 1999);
            DayCounter dayCounter = new Actual365Fixed();

            Console.WriteLine("Option type = " + type);
            Console.WriteLine("Maturity = "    + maturity);
            Console.WriteLine("Underlying price = " + underlying);
            Console.WriteLine("Strike = "      + strike);
            Console.WriteLine("Risk-free interest rate = {0:0.000000%}", riskFreeRate);
            Console.WriteLine("Dividend yield = {0:0.000000%}", dividendYield);
            Console.WriteLine("Volatility = {0:0.000000%}", volatility);
            Console.Write("\n");

            string method;

            Console.Write("\n");

            // write column headings
            int[] widths = new int[]{ 35, 14, 14, 14 };
            Console.Write("{0,-" + widths[0] + "}", "Method");
            Console.Write("{0,-" + widths[1] + "}", "European");
            Console.Write("{0,-" + widths[2] + "}", "Bermudan");
            Console.WriteLine("{0,-" + widths[3] + "}", "American");

            List<Date> exerciseDates = new List<Date>(); ;
            for (int i = 1; i <= 4; i++)
                exerciseDates.Add(settlementDate + new Period(3 * i, TimeUnit.Months));

            Exercise europeanExercise = new EuropeanExercise(maturity);
            Exercise bermudanExercise = new BermudanExercise(exerciseDates);
            Exercise americanExercise = new AmericanExercise(settlementDate, maturity);

            Handle<Quote> underlyingH = new Handle<Quote>(new SimpleQuote(underlying));

            // bootstrap the yield/dividend/vol curves
            var flatTermStructure = new Handle<YieldTermStructure>(new FlatForward(settlementDate, riskFreeRate, dayCounter));
            var flatDividendTS = new Handle<YieldTermStructure>(new FlatForward(settlementDate, dividendYield, dayCounter));
            var flatVolTS = new Handle<BlackVolTermStructure>(new BlackConstantVol(settlementDate, calendar, volatility, dayCounter));
            StrikedTypePayoff payoff = new PlainVanillaPayoff(type, strike);
            var bsmProcess = new BlackScholesMertonProcess(underlyingH, flatDividendTS, flatTermStructure, flatVolTS);

            // options
            VanillaOption europeanOption = new VanillaOption(payoff, europeanExercise);
            VanillaOption bermudanOption = new VanillaOption(payoff, bermudanExercise);
            VanillaOption americanOption = new VanillaOption(payoff, americanExercise);


            // Analytic formulas:

            // Black-Scholes for European
            method = "Black-Scholes";
            europeanOption.setPricingEngine(new AnalyticEuropeanEngine(bsmProcess));

            Console.Write("{0,-" + widths[0] + "}", method);
            Console.Write("{0,-" + widths[1] + ":0.000000}", europeanOption.NPV());
            Console.Write("{0,-" + widths[2] + "}", "N/A");
            Console.WriteLine("{0,-" + widths[3] + "}", "N/A");


            // Barone-Adesi and Whaley approximation for American
            method = "Barone-Adesi/Whaley";
            americanOption.setPricingEngine(new BaroneAdesiWhaleyApproximationEngine(bsmProcess));

            Console.Write("{0,-" + widths[0] + "}", method);
            Console.Write("{0,-" + widths[1] + "}", "N/A");
            Console.Write("{0,-" + widths[2] + "}", "N/A");
            Console.WriteLine("{0,-" + widths[3] + ":0.000000}", americanOption.NPV());


            // Bjerksund and Stensland approximation for American
            method = "Bjerksund/Stensland";
            americanOption.setPricingEngine(new BjerksundStenslandApproximationEngine(bsmProcess));

            Console.Write("{0,-" + widths[0] + "}", method);
            Console.Write("{0,-" + widths[1] + "}", "N/A");
            Console.Write("{0,-" + widths[2] + "}", "N/A");
            Console.WriteLine("{0,-" + widths[3] + ":0.000000}", americanOption.NPV());

            // Integral
            method = "Integral";
            europeanOption.setPricingEngine(new IntegralEngine(bsmProcess));

            Console.Write("{0,-" + widths[0] + "}", method);
            Console.Write("{0,-" + widths[1] + ":0.000000}", europeanOption.NPV());
            Console.Write("{0,-" + widths[2] + "}", "N/A");
            Console.WriteLine("{0,-" + widths[3] + "}", "N/A");


            // Finite differences
            int timeSteps = 801;
            method = "Finite differences";
            europeanOption.setPricingEngine(new FDEuropeanEngine(bsmProcess,timeSteps,timeSteps-1));
            bermudanOption.setPricingEngine(new FDBermudanEngine(bsmProcess,timeSteps,timeSteps-1));
            americanOption.setPricingEngine(new FDAmericanEngine(bsmProcess,timeSteps,timeSteps-1));

            Console.Write("{0,-" + widths[0] + "}", method);
            Console.Write("{0,-" + widths[1] + ":0.000000}", europeanOption.NPV());
            Console.Write("{0,-" + widths[2] + ":0.000000}", bermudanOption.NPV());
            Console.WriteLine("{0,-" + widths[3] + ":0.000000}", americanOption.NPV());

            // Binomial method: Jarrow-Rudd
            method = "Binomial Jarrow-Rudd";
            europeanOption.setPricingEngine(new BinomialVanillaEngine<JarrowRudd>(bsmProcess,timeSteps));
            bermudanOption.setPricingEngine(new BinomialVanillaEngine<JarrowRudd>(bsmProcess,timeSteps));
            americanOption.setPricingEngine(new BinomialVanillaEngine<JarrowRudd>(bsmProcess,timeSteps));

            Console.Write("{0,-" + widths[0] + "}", method);
            Console.Write("{0,-" + widths[1] + ":0.000000}", europeanOption.NPV());
            Console.Write("{0,-" + widths[2] + ":0.000000}", bermudanOption.NPV());
            Console.WriteLine("{0,-" + widths[3] + ":0.000000}", americanOption.NPV());


            method = "Binomial Cox-Ross-Rubinstein";
            europeanOption.setPricingEngine(new BinomialVanillaEngine<CoxRossRubinstein>(bsmProcess, timeSteps));
            bermudanOption.setPricingEngine(new BinomialVanillaEngine<CoxRossRubinstein>(bsmProcess, timeSteps));
            americanOption.setPricingEngine(new BinomialVanillaEngine<CoxRossRubinstein>(bsmProcess, timeSteps));

            Console.Write("{0,-" + widths[0] + "}", method);
            Console.Write("{0,-" + widths[1] + ":0.000000}", europeanOption.NPV());
            Console.Write("{0,-" + widths[2] + ":0.000000}", bermudanOption.NPV());
            Console.WriteLine("{0,-" + widths[3] + ":0.000000}", americanOption.NPV());

            // Binomial method: Additive equiprobabilities
            method = "Additive equiprobabilities";
            europeanOption.setPricingEngine(new BinomialVanillaEngine<AdditiveEQPBinomialTree>(bsmProcess, timeSteps));
            bermudanOption.setPricingEngine(new BinomialVanillaEngine<AdditiveEQPBinomialTree>(bsmProcess, timeSteps));
            americanOption.setPricingEngine(new BinomialVanillaEngine<AdditiveEQPBinomialTree>(bsmProcess, timeSteps));

            Console.Write("{0,-" + widths[0] + "}", method);
            Console.Write("{0,-" + widths[1] + ":0.000000}", europeanOption.NPV());
            Console.Write("{0,-" + widths[2] + ":0.000000}", bermudanOption.NPV());
            Console.WriteLine("{0,-" + widths[3] + ":0.000000}", americanOption.NPV());

            // Binomial method: Binomial Trigeorgis
            method = "Binomial Trigeorgis";
            europeanOption.setPricingEngine(new BinomialVanillaEngine<Trigeorgis>(bsmProcess,timeSteps));
            bermudanOption.setPricingEngine(new BinomialVanillaEngine<Trigeorgis>(bsmProcess,timeSteps));
            americanOption.setPricingEngine(new BinomialVanillaEngine<Trigeorgis>(bsmProcess,timeSteps));

            Console.Write("{0,-" + widths[0] + "}", method);
            Console.Write("{0,-" + widths[1] + ":0.000000}", europeanOption.NPV());
            Console.Write("{0,-" + widths[2] + ":0.000000}", bermudanOption.NPV());
            Console.WriteLine("{0,-" + widths[3] + ":0.000000}", americanOption.NPV());

            // Binomial method: Binomial Tian
            method = "Binomial Tian";
            europeanOption.setPricingEngine(new BinomialVanillaEngine<Tian>(bsmProcess,timeSteps));
            bermudanOption.setPricingEngine(new BinomialVanillaEngine<Tian>(bsmProcess,timeSteps));
            americanOption.setPricingEngine(new BinomialVanillaEngine<Tian>(bsmProcess,timeSteps));

            Console.Write("{0,-" + widths[0] + "}", method);
            Console.Write("{0,-" + widths[1] + ":0.000000}", europeanOption.NPV());
            Console.Write("{0,-" + widths[2] + ":0.000000}", bermudanOption.NPV());
            Console.WriteLine("{0,-" + widths[3] + ":0.000000}", americanOption.NPV());

            // Binomial method: Binomial Leisen-Reimer
            method = "Binomial Leisen-Reimer";
            europeanOption.setPricingEngine(new BinomialVanillaEngine<LeisenReimer>(bsmProcess,timeSteps));
            bermudanOption.setPricingEngine(new BinomialVanillaEngine<LeisenReimer>(bsmProcess,timeSteps));
            americanOption.setPricingEngine(new BinomialVanillaEngine<LeisenReimer>(bsmProcess,timeSteps));

            Console.Write("{0,-" + widths[0] + "}", method);
            Console.Write("{0,-" + widths[1] + ":0.000000}", europeanOption.NPV());
            Console.Write("{0,-" + widths[2] + ":0.000000}", bermudanOption.NPV());
            Console.WriteLine("{0,-" + widths[3] + ":0.000000}", americanOption.NPV());

            // Binomial method: Binomial Joshi
            method = "Binomial Joshi";
            europeanOption.setPricingEngine(new BinomialVanillaEngine<Joshi4>(bsmProcess,timeSteps));
            bermudanOption.setPricingEngine(new BinomialVanillaEngine<Joshi4>(bsmProcess,timeSteps));
            americanOption.setPricingEngine(new BinomialVanillaEngine<Joshi4>(bsmProcess,timeSteps));

            Console.Write("{0,-" + widths[0] + "}", method);
            Console.Write("{0,-" + widths[1] + ":0.000000}", europeanOption.NPV());
            Console.Write("{0,-" + widths[2] + ":0.000000}", bermudanOption.NPV());
            Console.WriteLine("{0,-" + widths[3] + ":0.000000}", americanOption.NPV());


            // Monte Carlo Method: MC (crude)
            timeSteps = 1;
            method = "MC (crude)";
            ulong mcSeed = 42;
            IPricingEngine mcengine1 = new MakeMCEuropeanEngine<PseudoRandom>(bsmProcess)
                                            .withSteps(timeSteps)
                                            .withAbsoluteTolerance(0.02)
                                            .withSeed(mcSeed)
                                            .value();
            europeanOption.setPricingEngine(mcengine1);
            // Real errorEstimate = europeanOption.errorEstimate();
            Console.Write("{0,-" + widths[0] + "}", method);
            Console.Write("{0,-" + widths[1] + ":0.000000}", europeanOption.NPV());
            Console.Write("{0,-" + widths[2] + ":0.000000}", "N/A");
            Console.WriteLine("{0,-" + widths[3] + ":0.000000}", "N/A");


            // Monte Carlo Method: QMC (Sobol)
            method = "QMC (Sobol)";
            int nSamples = 32768;  // 2^15

            IPricingEngine mcengine2 = new MakeMCEuropeanEngine<LowDiscrepancy>(bsmProcess)
                                            .withSteps(timeSteps)
                                            .withSamples(nSamples)
                                            .value();
            europeanOption.setPricingEngine(mcengine2);
            Console.Write("{0,-" + widths[0] + "}", method);
            Console.Write("{0,-" + widths[1] + ":0.000000}", europeanOption.NPV());
            Console.Write("{0,-" + widths[2] + ":0.000000}", "N/A");
            Console.WriteLine("{0,-" + widths[3] + ":0.000000}", "N/A");

            // Monte Carlo Method: MC (Longstaff Schwartz)
            method = "MC (Longstaff Schwartz)";
            IPricingEngine mcengine3 = new MakeMCAmericanEngine<PseudoRandom>(bsmProcess)
                                        .withSteps(100)
                                        .withAntitheticVariate()
                                        .withCalibrationSamples(4096)
                                        .withAbsoluteTolerance(0.02)
                                        .withSeed(mcSeed)
                                        .value();
            americanOption.setPricingEngine(mcengine3);
            Console.Write("{0,-" + widths[0] + "}", method);
            Console.Write("{0,-" + widths[1] + ":0.000000}", "N/A");
            Console.Write("{0,-" + widths[2] + ":0.000000}", "N/A");
            Console.WriteLine("{0,-" + widths[3] + ":0.000000}", americanOption.NPV());

            // End test
            Console.WriteLine(" \nRun completed in {0}", DateTime.Now - timer);
            Console.WriteLine();

            Console.Write("Press any key to continue ...");
            Console.ReadKey();
        }
    }
}
