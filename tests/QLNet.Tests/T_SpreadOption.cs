//  Copyright (C) 2008-2018 Andrea Maggiulli (a.maggiulli@gmail.com)
//
//  This file is part of QLNet Project https://github.com/amaggiulli/qlnet
//  QLNet is free software: you can redistribute it and/or modify it
//  under the terms of the QLNet license.  You should have received a
//  copy of the license along with this program; if not, license is
//  available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.
//
//  QLNet is a based on QuantLib, a free-software/open-source library
//  for financial quantitative analysts and developers - http://quantlib.org/
//  The QuantLib license is available online at http://quantlib.org/license.shtml.
//
//  This program is distributed in the hope that it will be useful, but WITHOUT
//  ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
//  FOR A PARTICULAR PURPOSE.  See the license for more details.
using System;
#if NET452
using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
using Xunit;
#endif
using QLNet;

namespace TestSuite
{
#if NET452
   [TestClass()]
#endif
   public class T_SpreadOption
   {
      private void REPORT_FAILURE(string greekName,
                                  StrikedTypePayoff payoff,
                                  Exercise exercise,
                                  double expected,
                                  double calculated,
                                  double tolerance,
                                  Date today)
      {
         QAssert.Fail(exercise + " "
                      + "Spread option with "
                      + payoff + " payoff:\n"
                      + "    strike:           " + payoff.strike() + "\n"
                      + "    reference date:   " + today + "\n"
                      + "    maturity:         " + exercise.lastDate() + "\n"
                      + "    expected " + greekName + ":   " + expected + "\n"
                      + "    calculated " + greekName + ": " + calculated + "\n"
                      + "    error:            " + Math.Abs(expected - calculated) + "\n"
                      + "    tolerance:        " + tolerance);
      }

      private struct Case
      {
         public double F1;
         public double F2;
         public double X;
         public double r;
         public double sigma1;
         public double sigma2;
         public double rho;
         public int length;
         public double value;
         public double theta;

         public Case(double f1, double f2, double x, double r, double sigma1,
                     double sigma2, double rho, int length, double value, double theta)
         {
            F1 = f1;
            F2 = f2;
            X = x;
            this.r = r;
            this.sigma1 = sigma1;
            this.sigma2 = sigma2;
            this.rho = rho;
            this.length = length;
            this.value = value;
            this.theta = theta;
         }
      }
#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testKirkEngine()
      {
         // Testing Kirk approximation for spread options

         /* The example data below are from "complete guide to option
            pricing formulas", Espen Gaarder Haug, p 60

            Expected values of option theta were calculated using automatic
            differentiation of the pricing function. The engine uses closed-form
            formula */

         Case[] cases =
         {
            new Case(28.0,  20.0, 7.0, 0.05, 0.29, 0.36,  0.42, 90,  2.1670,  3.0431),
            new Case(122.0, 120.0, 3.0, 0.10, 0.20, 0.20, -0.5,  36,  4.7530, 25.5905),
            new Case(122.0, 120.0, 3.0, 0.10, 0.20, 0.20,  0.0,  36,  3.7970, 20.8841),
            new Case(122.0, 120.0, 3.0, 0.10, 0.20, 0.20,  0.5,  36,  2.5537, 14.7260),
            new Case(122.0, 120.0, 3.0, 0.10, 0.20, 0.20, -0.5, 180, 10.7517, 10.0847),
            new Case(122.0, 120.0, 3.0, 0.10, 0.20, 0.20,  0.0, 180,  8.7020,  8.2619),
            new Case(122.0, 120.0, 3.0, 0.10, 0.20, 0.20,  0.5, 180,  6.0257,  5.8661),
            new Case(122.0, 120.0, 3.0, 0.10, 0.25, 0.20, -0.5,  36,  5.4275, 28.9013),
            new Case(122.0, 120.0, 3.0, 0.10, 0.25, 0.20,  0.0,  36,  4.3712, 23.7133),
            new Case(122.0, 120.0, 3.0, 0.10, 0.25, 0.20,  0.5,  36,  3.0086, 16.9864),
            new Case(122.0, 120.0, 3.0, 0.10, 0.25, 0.20, -0.5, 180, 12.1941, 11.3603),
            new Case(122.0, 120.0, 3.0, 0.10, 0.25, 0.20,  0.0, 180,  9.9340,  9.3589),
            new Case(122.0, 120.0, 3.0, 0.10, 0.25, 0.20,  0.5, 180,  7.0067,  6.7463),
            new Case(122.0, 120.0, 3.0, 0.10, 0.20, 0.25, -0.5,  36,  5.4061, 28.7963),
            new Case(122.0, 120.0, 3.0, 0.10, 0.20, 0.25,  0.0,  36,  4.3451, 23.5848),
            new Case(122.0, 120.0, 3.0, 0.10, 0.20, 0.25,  0.5,  36,  2.9723, 16.8060),
            new Case(122.0, 120.0, 3.0, 0.10, 0.20, 0.25, -0.5, 180, 12.1483, 11.3200),
            new Case(122.0, 120.0, 3.0, 0.10, 0.20, 0.25,  0.0, 180,  9.8780,  9.3091),
            new Case(122.0, 120.0, 3.0, 0.10, 0.20, 0.25,  0.5, 180,  6.9284,  6.6761)
         };

         for (int i = 0; i < cases.Length; ++i)
         {

            // First step: preparing the test values
            // Useful dates
            DayCounter dc = new Actual360();
            Date today = Date.Today;
            Date exerciseDate = today + cases[i].length;

            // Futures values
            SimpleQuote F1 = new SimpleQuote(cases[i].F1);
            SimpleQuote F2 = new SimpleQuote(cases[i].F2);

            // Risk-free interest rate
            double riskFreeRate = cases[i].r;
            YieldTermStructure forwardRate = Utilities.flatRate(today, riskFreeRate, dc);

            // Correlation
            Quote rho = new SimpleQuote(cases[i].rho);

            // Volatilities
            double vol1 = cases[i].sigma1;
            double vol2 = cases[i].sigma2;
            BlackVolTermStructure volTS1 = Utilities.flatVol(today, vol1, dc);
            BlackVolTermStructure volTS2 = Utilities.flatVol(today, vol2, dc);

            // Black-Scholes Processes
            // The BlackProcess is the relevant class for futures contracts
            BlackProcess stochProcess1 = new BlackProcess(new Handle<Quote>(F1),
                                                          new Handle<YieldTermStructure>(forwardRate),
                                                          new Handle<BlackVolTermStructure>(volTS1));

            BlackProcess stochProcess2 = new BlackProcess(new Handle<Quote>(F2),
                                                          new Handle<YieldTermStructure>(forwardRate),
                                                          new Handle<BlackVolTermStructure>(volTS2));

            // Creating the pricing engine
            IPricingEngine engine = new KirkSpreadOptionEngine(stochProcess1,
                                                               stochProcess2, new Handle<Quote>(rho));

            // Finally, create the option:
            Option.Type type = Option.Type.Call;
            double strike = cases[i].X;
            PlainVanillaPayoff payoff = new PlainVanillaPayoff(type, strike);
            Exercise exercise = new EuropeanExercise(exerciseDate);

            SpreadOption option = new SpreadOption(payoff, exercise);
            option.setPricingEngine(engine);

            // And test the data
            double value = option.NPV();
            double theta = option.theta();
            double tolerance = 1e-4;

            if (Math.Abs(value - cases[i].value) > tolerance)
            {
               REPORT_FAILURE("value",
                              payoff, exercise,
                              cases[i].value, value, tolerance, today);
            }

            if (Math.Abs(theta - cases[i].theta) > tolerance)
            {
               REPORT_FAILURE("theta",
                              payoff, exercise,
                              cases[i].theta, theta, tolerance, today);
            }
         }
      }
   }

}
