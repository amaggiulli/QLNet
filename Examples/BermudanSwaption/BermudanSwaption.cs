/*
 Copyright (C) 2010 Philippe Real (ph_real@hotmail.com)
  
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
using QLNet;

namespace BermudanSwaption
{
    class BermudanSwaption
    {

        //Number of swaptions to be calibrated to...
        const int NumRows = 5;
        const int NumCols = 5;

        static readonly int[] SwapLenghts = { 1, 2, 3, 4, 5 };
        static readonly double[] SwaptionVols = {
          0.1490, 0.1340, 0.1228, 0.1189, 0.1148,
          0.1290, 0.1201, 0.1146, 0.1108, 0.1040,
          0.1149, 0.1112, 0.1070, 0.1010, 0.0957,
          0.1047, 0.1021, 0.0980, 0.0951, 0.1270,
          0.1000, 0.0950, 0.0900, 0.1230, 0.1160};

        static void CalibrateModel(ShortRateModel model,
                            List<CalibrationHelper> helpers)
        {
            if (model == null) throw new ArgumentNullException("model");
            var om = new LevenbergMarquardt();
            model.calibrate(helpers, om,
                             new EndCriteria(400, 100, 1.0e-8, 1.0e-8, 1.0e-8), new Constraint(),
                                    new List<double>());

            // Output the implied Black volatilities
            for (int i = 0; i < NumRows; i++)
            {
                int j = NumCols - i - 1; // 1x5, 2x4, 3x3, 4x2, 5x1
                int k = i * NumCols + j;
                double npv = helpers[i].modelValue();
                double implied = helpers[i].impliedVolatility(npv, 1e-4,
                                                                   1000, 0.05, 0.50);
                double diff = implied - SwaptionVols[k];
                Console.WriteLine("{0}x{1}: model {2:0.00000 %}, market {3:0.00000 %}, diff {4:0.00000 %} ",
                                    i + 1, SwapLenghts[j], implied, SwaptionVols[k], diff);
            }
        }

        static void Main(string[] args)
        {

            DateTime timer = DateTime.Now;

            Date todaysDate = new Date(15, 2, 2002);
            Calendar calendar = new TARGET();
            Date settlementDate = new Date(19, 2, 2002);
            Settings.setEvaluationDate(todaysDate);

            // flat yield term structure impling 1x5 swap at 5%
            Quote flatRate = new SimpleQuote(0.04875825);
            Handle<YieldTermStructure> rhTermStructure = new Handle<YieldTermStructure>(
                          new FlatForward(settlementDate, new Handle<Quote>(flatRate),
                                          new Actual365Fixed()));

            // Define the ATM/OTM/ITM swaps
            Frequency fixedLegFrequency = Frequency.Annual;
            BusinessDayConvention fixedLegConvention = BusinessDayConvention.Unadjusted;
            BusinessDayConvention floatingLegConvention = BusinessDayConvention.ModifiedFollowing;
            DayCounter fixedLegDayCounter = new Thirty360(Thirty360.Thirty360Convention.European);
            Frequency floatingLegFrequency = Frequency.Semiannual;
            VanillaSwap.Type type = VanillaSwap.Type.Payer;
            double dummyFixedRate = 0.03;
            IborIndex indexSixMonths = new Euribor6M(rhTermStructure);

            Date startDate = calendar.advance(settlementDate, 1, TimeUnit.Years,
                                              floatingLegConvention);
            Date maturity = calendar.advance(startDate, 5, TimeUnit.Years,
                                             floatingLegConvention);
            Schedule fixedSchedule = new Schedule(startDate, maturity, new Period(fixedLegFrequency),
                                                    calendar, fixedLegConvention, fixedLegConvention,
                                                    DateGeneration.Rule.Forward, false);
            Schedule floatSchedule = new Schedule(startDate, maturity, new Period(floatingLegFrequency),
                                                    calendar, floatingLegConvention, floatingLegConvention,
                                                    DateGeneration.Rule.Forward, false);

            VanillaSwap swap = new VanillaSwap(
                type, 1000.0,
                fixedSchedule, dummyFixedRate, fixedLegDayCounter,
                floatSchedule, indexSixMonths, 0.0,
                indexSixMonths.dayCounter());
            swap.setPricingEngine(new DiscountingSwapEngine(rhTermStructure));
            double fixedAtmRate = swap.fairRate();
            double fixedOtmRate = fixedAtmRate * 1.2;
            double fixedItmRate = fixedAtmRate * 0.8;

            VanillaSwap atmSwap = new VanillaSwap(
                type, 1000.0,
                fixedSchedule, fixedAtmRate, fixedLegDayCounter,
                floatSchedule, indexSixMonths, 0.0,
                indexSixMonths.dayCounter());
            VanillaSwap otmSwap = new VanillaSwap(
                type, 1000.0,
                fixedSchedule, fixedOtmRate, fixedLegDayCounter,
                floatSchedule, indexSixMonths, 0.0,
                indexSixMonths.dayCounter());
            VanillaSwap itmSwap = new VanillaSwap(
                type, 1000.0,
                fixedSchedule, fixedItmRate, fixedLegDayCounter,
                floatSchedule, indexSixMonths, 0.0,
                indexSixMonths.dayCounter());

            // defining the swaptions to be used in model calibration
            List<Period> swaptionMaturities = new List<Period>(5);
            swaptionMaturities.Add(new Period(1, TimeUnit.Years));
            swaptionMaturities.Add(new Period(2, TimeUnit.Years));
            swaptionMaturities.Add(new Period(3, TimeUnit.Years));
            swaptionMaturities.Add(new Period(4, TimeUnit.Years));
            swaptionMaturities.Add(new Period(5, TimeUnit.Years));

            List<CalibrationHelper> swaptions = new List<CalibrationHelper>();

            // List of times that have to be included in the timegrid
            List<double> times = new List<double>();

            for (int i = 0; i < NumRows; i++)
            {
                int j = NumCols - i - 1; // 1x5, 2x4, 3x3, 4x2, 5x1
                int k = i * NumCols + j;
                Quote vol = new SimpleQuote(SwaptionVols[k]);
                swaptions.Add(new SwaptionHelper(swaptionMaturities[i],
                                   new Period(SwapLenghts[j], TimeUnit.Years),
                                   new Handle<Quote>(vol),
                                   indexSixMonths,
                                   indexSixMonths.tenor(),
                                   indexSixMonths.dayCounter(),
                                   indexSixMonths.dayCounter(),
                                   rhTermStructure));
                swaptions.Last().addTimesTo(times);
            }

            // Building time-grid
            TimeGrid grid = new TimeGrid(times, 30);


            // defining the models
            G2 modelG2 = new G2(rhTermStructure);
            HullWhite modelHw = new HullWhite(rhTermStructure);
            HullWhite modelHw2 = new HullWhite(rhTermStructure);
            BlackKarasinski modelBk = new BlackKarasinski(rhTermStructure);


            // model calibrations

            Console.WriteLine("G2 (analytic formulae) calibration");
            for (int i = 0; i < swaptions.Count; i++)
                swaptions[i].setPricingEngine(new G2SwaptionEngine(modelG2, 6.0, 16));
            CalibrateModel(modelG2, swaptions);
            Console.WriteLine("calibrated to:\n" +
                                "a     = {0:0.000000}, " +
                                "sigma = {1:0.0000000}\n" +
                                "b     = {2:0.000000}, " +
                                "eta   = {3:0.0000000}\n" +
                                "rho   = {4:0.00000}\n",
                                modelG2.parameters()[0],
                                modelG2.parameters()[1],
                                modelG2.parameters()[2],
                                modelG2.parameters()[3],
                                modelG2.parameters()[4]);

            Console.WriteLine("Hull-White (analytic formulae) calibration");
            for (int i = 0; i < swaptions.Count; i++)
                swaptions[i].setPricingEngine(new JamshidianSwaptionEngine(modelHw));
            CalibrateModel(modelHw, swaptions);
            Console.WriteLine("calibrated to:\n" +
                              "a = {0:0.000000}, " +
                              "sigma = {1:0.0000000}\n",
                              modelHw.parameters()[0],
                              modelHw.parameters()[1]);

            Console.WriteLine("Hull-White (numerical) calibration");
            for (int i = 0; i < swaptions.Count(); i++)
                swaptions[i].setPricingEngine(new TreeSwaptionEngine(modelHw2, grid));
            CalibrateModel(modelHw2, swaptions);
            Console.WriteLine("calibrated to:\n" +
                              "a = {0:0.000000}, " +
                              "sigma = {1:0.0000000}\n",
                              modelHw2.parameters()[0],
                              modelHw2.parameters()[1]);

            Console.WriteLine("Black-Karasinski (numerical) calibration");
            for (int i = 0; i < swaptions.Count; i++)
                swaptions[i].setPricingEngine(new TreeSwaptionEngine(modelBk, grid));
            CalibrateModel(modelBk, swaptions);
            Console.WriteLine("calibrated to:\n" +
                              "a = {0:0.000000}, " +
                              "sigma = {1:0.00000}\n",
                              modelBk.parameters()[0],
                              modelBk.parameters()[1]);


            // ATM Bermudan swaption pricing
            Console.WriteLine("Payer bermudan swaption "
                              + "struck at {0:0.00000 %} (ATM)",
                              fixedAtmRate);

            List<Date> bermudanDates = new List<Date>();
            List<CashFlow> leg = swap.fixedLeg();
            for (int i = 0; i < leg.Count; i++)
            {
                Coupon coupon = (Coupon)leg[i];
                bermudanDates.Add(coupon.accrualStartDate());
            }

            Exercise bermudanExercise = new BermudanExercise(bermudanDates);

            Swaption bermudanSwaption = new Swaption(atmSwap, bermudanExercise);

            // Do the pricing for each model

            // G2 price the European swaption here, it should switch to bermudan
            bermudanSwaption.setPricingEngine(new TreeSwaptionEngine(modelG2, 50));
            Console.WriteLine("G2:       {0:0.00}", bermudanSwaption.NPV());

            bermudanSwaption.setPricingEngine(new TreeSwaptionEngine(modelHw, 50));
            Console.WriteLine("HW:       {0:0.000}", bermudanSwaption.NPV());

            bermudanSwaption.setPricingEngine(new TreeSwaptionEngine(modelHw2, 50));
            Console.WriteLine("HW (num): {0:0.000}", bermudanSwaption.NPV());

            bermudanSwaption.setPricingEngine(new TreeSwaptionEngine(modelBk, 50));
            Console.WriteLine("BK:       {0:0.000}", bermudanSwaption.NPV());


            // OTM Bermudan swaption pricing
            Console.WriteLine("Payer bermudan swaption "
                              + "struck at {0:0.00000 %} (OTM)",
                              fixedOtmRate);

            Swaption otmBermudanSwaption = new Swaption(otmSwap, bermudanExercise);

            // Do the pricing for each model
            otmBermudanSwaption.setPricingEngine(new TreeSwaptionEngine(modelG2, 50));
            Console.WriteLine("G2:       {0:0.0000}", otmBermudanSwaption.NPV());

            otmBermudanSwaption.setPricingEngine(new TreeSwaptionEngine(modelHw, 50));
            Console.WriteLine("HW:       {0:0.0000}", otmBermudanSwaption.NPV());

            otmBermudanSwaption.setPricingEngine(new TreeSwaptionEngine(modelHw2, 50));
            Console.WriteLine("HW (num): {0:0.000}", otmBermudanSwaption.NPV());

            otmBermudanSwaption.setPricingEngine(new TreeSwaptionEngine(modelBk, 50));
            Console.WriteLine("BK:       {0:0.0000}", otmBermudanSwaption.NPV());

            // ITM Bermudan swaption pricing
            Console.WriteLine("Payer bermudan swaption "
                              + "struck at {0:0.00000 %} (ITM)",
                              fixedItmRate);

            Swaption itmBermudanSwaption = new Swaption(itmSwap, bermudanExercise);

            // Do the pricing for each model
            itmBermudanSwaption.setPricingEngine(new TreeSwaptionEngine(modelG2, 50));
            Console.WriteLine("G2:       {0:0.000}", itmBermudanSwaption.NPV());

            itmBermudanSwaption.setPricingEngine(new TreeSwaptionEngine(modelHw, 50));
            Console.WriteLine("HW:       {0:0.000}", itmBermudanSwaption.NPV());

            itmBermudanSwaption.setPricingEngine(new TreeSwaptionEngine(modelHw2, 50));
            Console.WriteLine("HW (num): {0:0.000}", itmBermudanSwaption.NPV());

            itmBermudanSwaption.setPricingEngine(new TreeSwaptionEngine(modelBk, 50));
            Console.WriteLine("BK:       {0:0.000}", itmBermudanSwaption.NPV());


            Console.WriteLine(" \nRun completed in {0}", DateTime.Now - timer);
            Console.WriteLine();

            Console.Write("Press any key to continue ...");
            Console.ReadKey();
        }
    }
}