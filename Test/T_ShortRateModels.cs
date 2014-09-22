/*
 Copyright (C) 2010 Philippe Real (ph_real@hotmail.com)
 Copyright (C) 2008-2014 Andrea Maggiulli (a.maggiulli@gmail.com)
  
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
    public class T_ShortRateModels
    {

        public class CalibrationData
        {
            public int start;
            public int length;
            public double volatility;
            public CalibrationData(int s, int l, double v)
            {
                start = s;
                length = l;
                volatility = v;
            }
        }

      [TestMethod()]
      public void testCachedHullWhite() {
         //("Testing Hull-White calibration against cached values...");

         Date today=new Date(15, Month.February, 2002);
         Date settlement=new Date(19, Month.February, 2002);
         Settings.setEvaluationDate(today);
         Handle<YieldTermStructure> termStructure= 
         new Handle<YieldTermStructure>(Utilities.flatRate(settlement, 0.04875825, new Actual365Fixed()));
         //termStructure.link
         HullWhite model=new HullWhite(termStructure);

         CalibrationData[] data = { new CalibrationData( 1, 5, 0.1148 ),
                                    new CalibrationData( 2, 4, 0.1108 ),
                                    new CalibrationData( 3, 3, 0.1070 ),
                                    new CalibrationData( 4, 2, 0.1021 ),
                                    new CalibrationData( 5, 1, 0.1000 )};
         IborIndex index = new Euribor6M(termStructure);

         IPricingEngine engine = new JamshidianSwaptionEngine(model);

         List<CalibrationHelper> swaptions = new List<CalibrationHelper>();
         for (int i=0; i<data.Length; i++) {
               Quote vol = new SimpleQuote(data[i].volatility);
               CalibrationHelper helper =
                                    new SwaptionHelper(new Period(data[i].start,TimeUnit.Years),
                                                      new Period(data[i].length, TimeUnit.Years),
                                                      new Handle<Quote>(vol),
                                                      index,
                                                      new Period(1, TimeUnit.Years), 
                                                      new Thirty360(),
                                                      new Actual360(), 
                                                      termStructure);
               helper.setPricingEngine(engine);
               swaptions.Add(helper);
         }

         // Set up the optimization problem
         // Real simplexLambda = 0.1;
         // Simplex optimizationMethod(simplexLambda);
         LevenbergMarquardt optimizationMethod = new LevenbergMarquardt(1.0e-8,1.0e-8,1.0e-8);
         EndCriteria endCriteria = new EndCriteria(10000, 100, 1e-6, 1e-8, 1e-8);

         //Optimize
         model.calibrate(swaptions, optimizationMethod, endCriteria, new Constraint(),new List<double>());
         EndCriteria.Type ecType = model.endCriteria();

         // Check and print out results
         #if QL_USE_INDEXED_COUPON
         double cachedA = 0.0488199, cachedSigma = 0.00593579;
         #else
         double cachedA = 0.0488565, cachedSigma = 0.00593662;
         #endif
         double tolerance = 1.120e-5;
         //double tolerance = 1.0e-6;
         Vector xMinCalculated = model.parameters();
         double yMinCalculated = model.value(xMinCalculated, swaptions);
         Vector xMinExpected = new Vector(2);
         xMinExpected[0]= cachedA;
         xMinExpected[1]= cachedSigma;
         double yMinExpected = model.value(xMinExpected, swaptions);
         if (Math.Abs(xMinCalculated[0]-cachedA) > tolerance
               || Math.Abs(xMinCalculated[1]-cachedSigma) > tolerance) {
               Assert.Fail ("Failed to reproduce cached calibration results:\n"
                           + "calculated: a = " + xMinCalculated[0] + ", "
                           + "sigma = " + xMinCalculated[1] + ", "
                           + "f(a) = " + yMinCalculated + ",\n"
                           + "expected:   a = " + xMinExpected[0] + ", "
                           + "sigma = " + xMinExpected[1] + ", "
                           + "f(a) = " + yMinExpected + ",\n"
                           + "difference: a = " + (xMinCalculated[0]-xMinExpected[0]) + ", "
                           + "sigma = " + (xMinCalculated[1]-xMinExpected[1]) + ", "
                           + "f(a) = " + (yMinCalculated - yMinExpected) + ",\n"
                           + "end criteria = " + ecType );
         }
      }

		  [TestMethod()]
        public void testSwaps() {
            //BOOST_MESSAGE("Testing Hull-White swap pricing against known values...");

            Date today;  //=Settings::instance().evaluationDate();;
            
            Calendar calendar = new TARGET();
            today = calendar.adjust(Date.Today);
            Settings.setEvaluationDate(today);

            Date settlement = calendar.advance(today, 2, TimeUnit.Days);

            Date[] dates = {
                settlement,
                calendar.advance(settlement,1,TimeUnit.Weeks),
                calendar.advance(settlement,1,TimeUnit.Months),
                calendar.advance(settlement,3,TimeUnit.Months),
                calendar.advance(settlement,6,TimeUnit.Months),
                calendar.advance(settlement,9,TimeUnit.Months),
                calendar.advance(settlement,1,TimeUnit.Years),
                calendar.advance(settlement,2,TimeUnit.Years),
                calendar.advance(settlement,3,TimeUnit.Years),
                calendar.advance(settlement,5,TimeUnit.Years),
                calendar.advance(settlement,10,TimeUnit.Years),
                calendar.advance(settlement,15,TimeUnit.Years)
            };
            double[] discounts = {
                1.0,
                0.999258,
                0.996704,
                0.990809,
                0.981798,
                0.972570,
                0.963430,
                0.929532,
                0.889267,
                0.803693,
                0.596903,
                0.433022
            };

            //for (int i = 0; i < dates.Length; i++)
            //    dates[i] + dates.Length;

            LogLinear Interpolator = new LogLinear();

            Handle<YieldTermStructure> termStructure = 
               new Handle<YieldTermStructure>(
                   new InterpolatedDiscountCurve<LogLinear>(
                       dates.ToList<Date>(),
                       discounts.ToList<double>(),
                       new Actual365Fixed(),new Calendar(), null, null , Interpolator)
            );

            HullWhite model = new HullWhite(termStructure);

            int[] start = { -3, 0, 3 };
            int[] length = { 2, 5, 10 };
            double[] rates = { 0.02, 0.04, 0.06 };
            IborIndex euribor = new Euribor6M(termStructure);

            IPricingEngine engine = new TreeVanillaSwapEngine(model, 120, termStructure);

            #if QL_USE_INDEXED_COUPON
            double tolerance = 4.0e-3;
            #else
            double tolerance = 1.0e-8;
            #endif

            for (int i=0; i<start.Length; i++) {

                Date startDate = calendar.advance(settlement,start[i],TimeUnit.Months);
                if (startDate < today) {
                    Date fixingDate = calendar.advance(startDate,-2,TimeUnit.Days);
                    //TimeSeries<double> pastFixings;
                    ObservableValue<TimeSeries<double>> pastFixings = new ObservableValue<TimeSeries<double>>();
                    pastFixings.value()[fixingDate] = 0.03;
                    IndexManager.instance().setHistory(euribor.name(),
                                                        pastFixings);
                }

                for (int j=0; j<length.Length; j++) {

                    Date maturity = calendar.advance(startDate, length[i], TimeUnit.Years);
                    Schedule fixedSchedule = new Schedule(startDate, maturity, new Period(Frequency.Annual),
                                           calendar, BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                           DateGeneration.Rule.Forward, false);
                    Schedule floatSchedule = new Schedule(startDate, maturity, new Period(Frequency.Semiannual),
                                           calendar, BusinessDayConvention.Following, BusinessDayConvention.Following,
                                           DateGeneration.Rule.Forward, false);
                    for (int k=0; k<rates.Length; k++) {

                        VanillaSwap swap = new VanillaSwap(VanillaSwap.Type.Payer, 1000000.0,
                                         fixedSchedule, rates[k], new Thirty360(),
                                         floatSchedule, euribor, 0.0, new Actual360());
                        swap.setPricingEngine(new DiscountingSwapEngine(termStructure));
                        double expected = swap.NPV();
                        swap.setPricingEngine(engine);
                        double calculated = swap.NPV();

                        double error = Math.Abs((expected-calculated)/expected);
                        if (error > tolerance) {
                            Assert.Fail("Failed to reproduce swap NPV:"
                                        //+ QL_FIXED << std::setprecision(9)
                                        + "\n    calculated: " + calculated
                                        + "\n    expected:   " + expected
                                        //+ QL_SCIENTIFIC
                                        + "\n    rel. error: " + error);
                        }
                    }
                }
            }
        }

        [TestMethod()]
        public void testFuturesConvexityBias()
        {
            //BOOST_MESSAGE("Testing Hull-White futures convexity bias...");

            // G. Kirikos, D. Novak, "Convexity Conundrums", Risk Magazine, March 1997
            double futureQuote = 94.0;
            double a = 0.03;
            double sigma = 0.015;
            double t = 5.0;
            double T = 5.25;

            double expectedForward = 0.0573037;
            double tolerance = 0.0000001;

            double futureImpliedRate = (100.0 - futureQuote) / 100.0;
            double calculatedForward =
                futureImpliedRate - HullWhite.convexityBias(futureQuote, t, T, sigma, a);

            double error = Math.Abs(calculatedForward - expectedForward);

            if (error > tolerance)
            {
                Assert.Fail("Failed to reproduce convexity bias:"
                            + "\ncalculated: " + calculatedForward
                            + "\n  expected: " + expectedForward
                    //+ QL_SCIENTIFIC
                            + "\n     error: " + error
                            + "\n tolerance: " + tolerance);
            }

        }
    }
}