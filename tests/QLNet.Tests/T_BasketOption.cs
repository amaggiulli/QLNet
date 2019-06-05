/*
 Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)

 This file is part of QLNet Project https://github.com/amaggiulli/qlnet
 QLNet is free software: you can redistribute it and/or modify it
 under the terms of the QLNet license.  You should have received a
 copy of the license along with this program; if not, license is
 available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.

 QLNet is a based on QuantLib, a free-software/open-source library
 for financial quantitative analysts and developers - http://quantlib.org/
 The QuantLib license is available online at http://quantlib.org/license.shtml.

 This program is distributed in the hope that it will be useful, but WITHOUT
 ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 FOR A PARTICULAR PURPOSE.  See the license for more details.
*/

using System;
using System.Collections.Generic;
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
   public class T_BasketOption
   {
      public enum BasketType { MinBasket, MaxBasket, SpreadBasket }
      public struct BasketOptionTwoData
      {
         public BasketOptionTwoData(BasketType _basketType, Option.Type _type, double _strike, double _s1, double _s2, double _q1,
                                    double _q2, double _r, double _t, double _v1, double _v2, double _rho, double _result, double _tol)
         {

            basketType = _basketType;
            type = _type;
            strike = _strike;
            s1 = _s1;
            s2 = _s2;
            q1 = _q1;
            q2 = _q2;
            r = _r;
            t = _t; // years
            v1 = _v1;
            v2 = _v2;
            rho = _rho;
            result = _result;
            tol = _tol;
         }

         public BasketType basketType;
         public Option.Type type;
         public double strike;
         public double s1;
         public double s2;
         public double q1;
         public double q2;
         public double r;
         public double t; // years
         public double v1;
         public double v2;
         public double rho;
         public double result;
         public double tol;
      }
      public BasketPayoff basketTypeToPayoff(BasketType basketType, Payoff p)
      {
         switch (basketType)
         {
            case BasketType.MinBasket:
               return new MinBasketPayoff(p);
            case BasketType.MaxBasket:
               return new MaxBasketPayoff(p);
            case BasketType.SpreadBasket:
               return new SpreadBasketPayoff(p);
         }
         Utils.QL_FAIL("unknown basket option type");
         return null;
      }
      public string basketTypeToString(BasketType basketType)
      {
         switch (basketType)
         {
            case BasketType.MinBasket:
               return "MinBasket";
            case BasketType.MaxBasket:
               return "MaxBasket";
            case BasketType.SpreadBasket:
               return "Spread";
         }
         Utils.QL_FAIL("unknown basket option type");
         return String.Empty;
      }
      public void REPORT_FAILURE_2(String greekName, BasketType basketType, PlainVanillaPayoff payoff, Exercise exercise,
                                   double s1, double s2, double q1, double q2, double r, Date today, double v1, double v2, double rho,
                                   double expected, double calculated, double error, double tolerance)
      {
         QAssert.Fail(Utilities.exerciseTypeToString(exercise) + " "
                      + payoff.optionType() + " option on "
                      + basketTypeToString(basketType)
                      + " with " + Utilities.payoffTypeToString(payoff) + " payoff:\n"
                      + "1st underlying value: " + s1 + "\n"
                      + "2nd underlying value: " + s2 + "\n"
                      + "              strike: " + payoff.strike() + "\n"
                      + "  1st dividend yield: " + q1 + "\n"
                      + "  2nd dividend yield: " + q2 + "\n"
                      + "      risk-free rate: " + r + "\n"
                      + "      reference date: " + today + "\n"
                      + "            maturity: " + exercise.lastDate() + "\n"
                      + "1st asset volatility: " + v1 + "\n"
                      + "2nd asset volatility: " + v2 + "\n"
                      + "         correlation: " + rho + "\n\n"
                      + "    expected   " + greekName + ": " + expected + "\n"
                      + "    calculated " + greekName + ": " + calculated + "\n"
                      + "    error:            " + error + "\n"
                      + "    tolerance:        " + tolerance);

      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testEuroTwoValues()
      {
         // Testing two-asset European basket options...

         /*
            Data from:
            Excel spreadsheet www.maths.ox.ac.uk/~firth/computing/excel.shtml
            and
            "Option pricing formulas", E.G. Haug, McGraw-Hill 1998 pag 56-58
            European two asset max basket options
         */
         BasketOptionTwoData[] values =
         {
            // basketType,   optionType, strike,    s1,    s2,   q1,   q2,    r,    t,   v1,   v2,  rho, result, tol
            // data from http://www.maths.ox.ac.uk/~firth/computing/excel.shtml
            new BasketOptionTwoData(BasketType.MinBasket, Option.Type.Call,  100.0, 100.0, 100.0, 0.00, 0.00, 0.05, 1.00, 0.30, 0.30, 0.90, 10.898, 1.0e-3),
            new BasketOptionTwoData(BasketType.MinBasket, Option.Type.Call,  100.0, 100.0, 100.0, 0.00, 0.00, 0.05, 1.00, 0.30, 0.30, 0.70,  8.483, 1.0e-3),
            new BasketOptionTwoData(BasketType.MinBasket, Option.Type.Call,  100.0, 100.0, 100.0, 0.00, 0.00, 0.05, 1.00, 0.30, 0.30, 0.50,  6.844, 1.0e-3),
            new BasketOptionTwoData(BasketType.MinBasket, Option.Type.Call,  100.0, 100.0, 100.0, 0.00, 0.00, 0.05, 1.00, 0.30, 0.30, 0.30,  5.531, 1.0e-3),
            new BasketOptionTwoData(BasketType.MinBasket, Option.Type.Call,  100.0, 100.0, 100.0, 0.00, 0.00, 0.05, 1.00, 0.30, 0.30, 0.10,  4.413, 1.0e-3),
            new BasketOptionTwoData(BasketType.MinBasket, Option.Type.Call,  100.0, 100.0, 100.0, 0.00, 0.00, 0.05, 1.00, 0.50, 0.70, 0.00,  4.981, 1.0e-3),
            new BasketOptionTwoData(BasketType.MinBasket, Option.Type.Call,  100.0, 100.0, 100.0, 0.00, 0.00, 0.05, 1.00, 0.50, 0.30, 0.00,  4.159, 1.0e-3),
            new BasketOptionTwoData(BasketType.MinBasket, Option.Type.Call,  100.0, 100.0, 100.0, 0.00, 0.00, 0.05, 1.00, 0.50, 0.10, 0.00,  2.597, 1.0e-3),
            new BasketOptionTwoData(BasketType.MinBasket, Option.Type.Call,  100.0, 100.0, 100.0, 0.00, 0.00, 0.05, 1.00, 0.50, 0.10, 0.50,  4.030, 1.0e-3),

            new BasketOptionTwoData(BasketType.MaxBasket, Option.Type.Call,  100.0, 100.0, 100.0, 0.00, 0.00, 0.05, 1.00, 0.30, 0.30, 0.90, 17.565, 1.0e-3),
            new BasketOptionTwoData(BasketType.MaxBasket, Option.Type.Call,  100.0, 100.0, 100.0, 0.00, 0.00, 0.05, 1.00, 0.30, 0.30, 0.70, 19.980, 1.0e-3),
            new BasketOptionTwoData(BasketType.MaxBasket, Option.Type.Call,  100.0, 100.0, 100.0, 0.00, 0.00, 0.05, 1.00, 0.30, 0.30, 0.50, 21.619, 1.0e-3),
            new BasketOptionTwoData(BasketType.MaxBasket, Option.Type.Call,  100.0, 100.0, 100.0, 0.00, 0.00, 0.05, 1.00, 0.30, 0.30, 0.30, 22.932, 1.0e-3),
            new BasketOptionTwoData(BasketType.MaxBasket, Option.Type.Call,  100.0, 100.0, 100.0, 0.00, 0.00, 0.05, 1.00, 0.30, 0.30, 0.10, 24.049, 1.1e-3),
            new BasketOptionTwoData(BasketType.MaxBasket, Option.Type.Call,  100.0,  80.0, 100.0, 0.00, 0.00, 0.05, 1.00, 0.30, 0.30, 0.30, 16.508, 1.0e-3),
            new BasketOptionTwoData(BasketType.MaxBasket, Option.Type.Call,  100.0,  80.0,  80.0, 0.00, 0.00, 0.05, 1.00, 0.30, 0.30, 0.30,  8.049, 1.0e-3),
            new BasketOptionTwoData(BasketType.MaxBasket, Option.Type.Call,  100.0,  80.0, 120.0, 0.00, 0.00, 0.05, 1.00, 0.30, 0.30, 0.30, 30.141, 1.0e-3),
            new BasketOptionTwoData(BasketType.MaxBasket, Option.Type.Call,  100.0, 120.0, 120.0, 0.00, 0.00, 0.05, 1.00, 0.30, 0.30, 0.30, 42.889, 1.0e-3),

            new BasketOptionTwoData(BasketType.MinBasket,  Option.Type.Put,  100.0, 100.0, 100.0, 0.00, 0.00, 0.05, 1.00, 0.30, 0.30, 0.90, 11.369, 1.0e-3),
            new BasketOptionTwoData(BasketType.MinBasket,  Option.Type.Put,  100.0, 100.0, 100.0, 0.00, 0.00, 0.05, 1.00, 0.30, 0.30, 0.70, 12.856, 1.0e-3),
            new BasketOptionTwoData(BasketType.MinBasket,  Option.Type.Put,  100.0, 100.0, 100.0, 0.00, 0.00, 0.05, 1.00, 0.30, 0.30, 0.50, 13.890, 1.0e-3),
            new BasketOptionTwoData(BasketType.MinBasket,  Option.Type.Put,  100.0, 100.0, 100.0, 0.00, 0.00, 0.05, 1.00, 0.30, 0.30, 0.30, 14.741, 1.0e-3),
            new BasketOptionTwoData(BasketType.MinBasket,  Option.Type.Put,  100.0, 100.0, 100.0, 0.00, 0.00, 0.05, 1.00, 0.30, 0.30, 0.10, 15.485, 1.0e-3),

            new BasketOptionTwoData(BasketType.MinBasket,  Option.Type.Put,  100.0, 100.0, 100.0, 0.00, 0.00, 0.05, 0.50, 0.30, 0.30, 0.10, 11.893, 1.0e-3),
            new BasketOptionTwoData(BasketType.MinBasket,  Option.Type.Put,  100.0, 100.0, 100.0, 0.00, 0.00, 0.05, 0.25, 0.30, 0.30, 0.10,  8.881, 1.0e-3),
            new BasketOptionTwoData(BasketType.MinBasket,  Option.Type.Put,  100.0, 100.0, 100.0, 0.00, 0.00, 0.05, 2.00, 0.30, 0.30, 0.10, 19.268, 1.0e-3),

            new BasketOptionTwoData(BasketType.MaxBasket,  Option.Type.Put,  100.0, 100.0, 100.0, 0.00, 0.00, 0.05, 1.00, 0.30, 0.30, 0.90,  7.339, 1.0e-3),
            new BasketOptionTwoData(BasketType.MaxBasket,  Option.Type.Put,  100.0, 100.0, 100.0, 0.00, 0.00, 0.05, 1.00, 0.30, 0.30, 0.70,  5.853, 1.0e-3),
            new BasketOptionTwoData(BasketType.MaxBasket,  Option.Type.Put,  100.0, 100.0, 100.0, 0.00, 0.00, 0.05, 1.00, 0.30, 0.30, 0.50,  4.818, 1.0e-3),
            new BasketOptionTwoData(BasketType.MaxBasket,  Option.Type.Put,  100.0, 100.0, 100.0, 0.00, 0.00, 0.05, 1.00, 0.30, 0.30, 0.30,  3.967, 1.1e-3),
            new BasketOptionTwoData(BasketType.MaxBasket,  Option.Type.Put,  100.0, 100.0, 100.0, 0.00, 0.00, 0.05, 1.00, 0.30, 0.30, 0.10,  3.223, 1.0e-3),

            //      basketType,   optionType, strike,    s1,    s2,   q1,   q2,    r,    t,   v1,   v2,  rho,  result, tol
            // data from "Option pricing formulas" VB code + spreadsheet
            new BasketOptionTwoData(BasketType.MinBasket, Option.Type.Call,   98.0, 100.0, 105.0, 0.00, 0.00, 0.05, 0.50, 0.11, 0.16, 0.63,  4.8177, 1.0e-4),
            new BasketOptionTwoData(BasketType.MaxBasket, Option.Type.Call,   98.0, 100.0, 105.0, 0.00, 0.00, 0.05, 0.50, 0.11, 0.16, 0.63, 11.6323, 1.0e-4),
            new BasketOptionTwoData(BasketType.MinBasket, Option.Type.Put,    98.0, 100.0, 105.0, 0.00, 0.00, 0.05, 0.50, 0.11, 0.16, 0.63,  2.0376, 1.0e-4),
            new BasketOptionTwoData(BasketType.MaxBasket, Option.Type.Put,    98.0, 100.0, 105.0, 0.00, 0.00, 0.05, 0.50, 0.11, 0.16, 0.63,  0.5731, 1.0e-4),
            new BasketOptionTwoData(BasketType.MinBasket, Option.Type.Call,   98.0, 100.0, 105.0, 0.06, 0.09, 0.05, 0.50, 0.11, 0.16, 0.63,  2.9340, 1.0e-4),
            new BasketOptionTwoData(BasketType.MinBasket, Option.Type.Put,    98.0, 100.0, 105.0, 0.06, 0.09, 0.05, 0.50, 0.11, 0.16, 0.63,  3.5224, 1.0e-4),
            // data from "Option pricing formulas", E.G. Haug, McGraw-Hill 1998 pag 58
            new BasketOptionTwoData(BasketType.MaxBasket, Option.Type.Call,   98.0, 100.0, 105.0, 0.06, 0.09, 0.05, 0.50, 0.11, 0.16, 0.63,  8.0701, 1.0e-4),
            new BasketOptionTwoData(BasketType.MaxBasket, Option.Type.Put,    98.0, 100.0, 105.0, 0.06, 0.09, 0.05, 0.50, 0.11, 0.16, 0.63,  1.2181, 1.0e-4),

            /* "Option pricing formulas", E.G. Haug, McGraw-Hill 1998 pag 59-60
               Kirk approx. for a european spread option on two futures*/

            new BasketOptionTwoData(BasketType.SpreadBasket, Option.Type.Call, 3.0,  122.0, 120.0, 0.0, 0.0, 0.10,  0.1, 0.20, 0.20, -0.5, 4.7530, 1.0e-3),
            new BasketOptionTwoData(BasketType.SpreadBasket, Option.Type.Call, 3.0,  122.0, 120.0, 0.0, 0.0, 0.10,  0.1, 0.20, 0.20,  0.0, 3.7970, 1.0e-3),
            new BasketOptionTwoData(BasketType.SpreadBasket, Option.Type.Call, 3.0,  122.0, 120.0, 0.0, 0.0, 0.10,  0.1, 0.20, 0.20,  0.5, 2.5537, 1.0e-3),
            new BasketOptionTwoData(BasketType.SpreadBasket, Option.Type.Call, 3.0,  122.0, 120.0, 0.0, 0.0, 0.10,  0.1, 0.25, 0.20, -0.5, 5.4275, 1.0e-3),
            new BasketOptionTwoData(BasketType.SpreadBasket, Option.Type.Call, 3.0,  122.0, 120.0, 0.0, 0.0, 0.10,  0.1, 0.25, 0.20,  0.0, 4.3712, 1.0e-3),
            new BasketOptionTwoData(BasketType.SpreadBasket, Option.Type.Call, 3.0,  122.0, 120.0, 0.0, 0.0, 0.10,  0.1, 0.25, 0.20,  0.5, 3.0086, 1.0e-3),
            new BasketOptionTwoData(BasketType.SpreadBasket, Option.Type.Call, 3.0,  122.0, 120.0, 0.0, 0.0, 0.10,  0.1, 0.20, 0.25, -0.5, 5.4061, 1.0e-3),
            new BasketOptionTwoData(BasketType.SpreadBasket, Option.Type.Call, 3.0,  122.0, 120.0, 0.0, 0.0, 0.10,  0.1, 0.20, 0.25,  0.0, 4.3451, 1.0e-3),
            new BasketOptionTwoData(BasketType.SpreadBasket, Option.Type.Call, 3.0,  122.0, 120.0, 0.0, 0.0, 0.10,  0.1, 0.20, 0.25,  0.5, 2.9723, 1.0e-3),
            new BasketOptionTwoData(BasketType.SpreadBasket, Option.Type.Call, 3.0,  122.0, 120.0, 0.0, 0.0, 0.10,  0.5, 0.20, 0.20, -0.5, 10.7517, 1.0e-3),
            new BasketOptionTwoData(BasketType.SpreadBasket, Option.Type.Call, 3.0,  122.0, 120.0, 0.0, 0.0, 0.10,  0.5, 0.20, 0.20,  0.0, 8.7020, 1.0e-3),
            new BasketOptionTwoData(BasketType.SpreadBasket, Option.Type.Call, 3.0,  122.0, 120.0, 0.0, 0.0, 0.10,  0.5, 0.20, 0.20,  0.5, 6.0257, 1.0e-3),
            new BasketOptionTwoData(BasketType.SpreadBasket, Option.Type.Call, 3.0,  122.0, 120.0, 0.0, 0.0, 0.10,  0.5, 0.25, 0.20, -0.5, 12.1941, 1.0e-3),
            new BasketOptionTwoData(BasketType.SpreadBasket, Option.Type.Call, 3.0,  122.0, 120.0, 0.0, 0.0, 0.10,  0.5, 0.25, 0.20,  0.0, 9.9340, 1.0e-3),
            new BasketOptionTwoData(BasketType.SpreadBasket, Option.Type.Call, 3.0,  122.0, 120.0, 0.0, 0.0, 0.10,  0.5, 0.25, 0.20,  0.5, 7.0067, 1.0e-3),
            new BasketOptionTwoData(BasketType.SpreadBasket, Option.Type.Call, 3.0,  122.0, 120.0, 0.0, 0.0, 0.10,  0.5, 0.20, 0.25, -0.5, 12.1483, 1.0e-3),
            new BasketOptionTwoData(BasketType.SpreadBasket, Option.Type.Call, 3.0,  122.0, 120.0, 0.0, 0.0, 0.10,  0.5, 0.20, 0.25,  0.0, 9.8780, 1.0e-3),
            new BasketOptionTwoData(BasketType.SpreadBasket, Option.Type.Call, 3.0,  122.0, 120.0, 0.0, 0.0, 0.10,  0.5, 0.20, 0.25,  0.5, 6.9284, 1.0e-3)
         };

         DayCounter dc = new Actual360();


         Date today = Date.Today;

         SimpleQuote spot1 = new SimpleQuote(0.0);
         SimpleQuote spot2 = new SimpleQuote(0.0);

         SimpleQuote qRate1 = new SimpleQuote(0.0);
         YieldTermStructure qTS1 = Utilities.flatRate(today, qRate1, dc);
         SimpleQuote qRate2 = new SimpleQuote(0.0);
         YieldTermStructure qTS2 = Utilities.flatRate(today, qRate2, dc);

         SimpleQuote rRate = new SimpleQuote(0.0);
         YieldTermStructure rTS = Utilities.flatRate(today, rRate, dc);

         SimpleQuote vol1 = new SimpleQuote(0.0);
         BlackVolTermStructure volTS1 = Utilities.flatVol(today, vol1, dc);
         SimpleQuote vol2 = new SimpleQuote(0.0);
         BlackVolTermStructure volTS2 = Utilities.flatVol(today, vol2, dc);

         //double mcRelativeErrorTolerance = 0.01;
         //double fdRelativeErrorTolerance = 0.01;

         for (int i = 0; i < values.Length; i++)
         {

            PlainVanillaPayoff payoff = new PlainVanillaPayoff(values[i].type, values[i].strike);

            Date exDate = today + (int)(values[i].t * 360 + 0.5);
            Exercise exercise = new EuropeanExercise(exDate);

            spot1 .setValue(values[i].s1);
            spot2 .setValue(values[i].s2);
            qRate1.setValue(values[i].q1);
            qRate2.setValue(values[i].q2);
            rRate .setValue(values[i].r);
            vol1  .setValue(values[i].v1);
            vol2  .setValue(values[i].v2);


            IPricingEngine analyticEngine = null;
            GeneralizedBlackScholesProcess p1 = null, p2 = null;

            switch (values[i].basketType)
            {
               case BasketType.MaxBasket:
               case BasketType.MinBasket:
                  p1 = new BlackScholesMertonProcess(new Handle<Quote>(spot1),
                                                     new Handle<YieldTermStructure>(qTS1),
                                                     new Handle<YieldTermStructure>(rTS),
                                                     new Handle<BlackVolTermStructure>(volTS1));
                  p2 = new BlackScholesMertonProcess(new Handle<Quote>(spot2),
                                                     new Handle<YieldTermStructure>(qTS2),
                                                     new Handle<YieldTermStructure>(rTS),
                                                     new Handle<BlackVolTermStructure>(volTS2));
                  analyticEngine = new StulzEngine(p1, p2, values[i].rho);
                  break;

               case BasketType.SpreadBasket:
                  p1 = new BlackProcess(new Handle<Quote>(spot1),
                                        new Handle<YieldTermStructure>(rTS),
                                        new Handle<BlackVolTermStructure>(volTS1));
                  p2 = new BlackProcess(new Handle<Quote>(spot2),
                                        new Handle<YieldTermStructure>(rTS),
                                        new Handle<BlackVolTermStructure>(volTS2));

                  analyticEngine = new KirkEngine((BlackProcess)p1, (BlackProcess)p2, values[i].rho);
                  break;

               default:
                  Utils.QL_FAIL("unknown basket type");
                  break;
            }


            List<StochasticProcess1D> procs = new List<StochasticProcess1D> {p1, p2};

            Matrix correlationMatrix = new Matrix(2, 2, values[i].rho);
            for (int j = 0; j < 2; j++)
            {
               correlationMatrix[j, j] = 1.0;
            }

            StochasticProcessArray process = new StochasticProcessArray(procs, correlationMatrix);

            //IPricingEngine mcEngine = MakeMCEuropeanBasketEngine<PseudoRandom, Statistics>(process)
            //                           .withStepsPerYear(1)
            //                           .withSamples(10000)
            //                           .withSeed(42);



            //IPricingEngine fdEngine = new Fd2dBlackScholesVanillaEngine(p1, p2, values[i].rho, 50, 50, 15);

            BasketOption basketOption = new BasketOption(basketTypeToPayoff(values[i].basketType, payoff), exercise);

            // analytic engine
            basketOption.setPricingEngine(analyticEngine);
            double calculated = basketOption.NPV();
            double expected = values[i].result;
            double error = Math.Abs(calculated - expected);
            if (error > values[i].tol)
            {
               REPORT_FAILURE_2("value", values[i].basketType, payoff, exercise,
                                values[i].s1, values[i].s2, values[i].q1,
                                values[i].q2, values[i].r, today, values[i].v1,
                                values[i].v2, values[i].rho, values[i].result,
                                calculated, error, values[i].tol);
            }

            // // fd engine
            // basketOption.setPricingEngine(fdEngine);
            // calculated = basketOption.NPV();
            // double relError = relativeError(calculated, expected, expected);
            // if (relError > mcRelativeErrorTolerance )
            // {
            //    REPORT_FAILURE_2("FD value", values[i].basketType, payoff,
            //                      exercise, values[i].s1, values[i].s2,
            //                      values[i].q1, values[i].q2, values[i].r,
            //                      today, values[i].v1, values[i].v2, values[i].rho,
            //                      values[i].result, calculated, relError,
            //                      fdRelativeErrorTolerance);
            // }

            //// mc engine
            //basketOption.setPricingEngine(mcEngine);
            //calculated = basketOption.NPV();
            //relError = relativeError(calculated, expected, values[i].s1);
            //if (relError > mcRelativeErrorTolerance )
            //{
            //    REPORT_FAILURE_2("MC value", values[i].basketType, payoff,
            //                     exercise, values[i].s1, values[i].s2,
            //                     values[i].q1, values[i].q2, values[i].r,
            //                     today, values[i].v1, values[i].v2, values[i].rho,
            //                     values[i].result, calculated, relError,
            //                     mcRelativeErrorTolerance);
            //}
         }
      }
   }
}
