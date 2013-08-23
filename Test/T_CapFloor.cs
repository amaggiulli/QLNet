/*
 Copyright (C) 2008 Andrea Maggiulli
  
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

namespace TestSuite {
    [TestClass()]
    public class T_CapFloor {
        class CommonVars {
            // common data
            public Date settlement;
            public List<double> nominals;
            public BusinessDayConvention convention;
            public Frequency frequency;
            public IborIndex index;
            public Calendar calendar;
            public int fixingDays;
            public RelinkableHandle<YieldTermStructure> termStructure = new RelinkableHandle<YieldTermStructure>();

            // setup
            public CommonVars() {
                nominals = new List<double>() { 100 };
                frequency = Frequency.Semiannual;
                index = (IborIndex)new Euribor6M(termStructure);
                calendar = index.fixingCalendar();
                convention = BusinessDayConvention.ModifiedFollowing;
                Date today = calendar.adjust(Date.Today);
                Settings.setEvaluationDate(today);
                int settlementDays = 2;
                fixingDays = 2;
                settlement = calendar.advance(today, settlementDays, TimeUnit.Days);
                termStructure.linkTo(Utilities.flatRate(settlement, 0.05,
                                              new ActualActual(ActualActual.Convention.ISDA)));

            }

            // utilities
            public List<CashFlow> makeLeg(Date startDate, int length) {
                Date endDate = calendar.advance(startDate, new Period(length, TimeUnit.Years), convention);
                Schedule schedule = new Schedule(startDate, endDate, new Period(frequency), calendar,
                                                 convention, convention, DateGeneration.Rule.Forward,
                                                 false);
                return new IborLeg(schedule, index)
                    .withPaymentDayCounter(index.dayCounter())
                    .withFixingDays(fixingDays)
                    .withNotionals(nominals)
                    .withPaymentAdjustment(convention);
            }

            public IPricingEngine makeEngine(double volatility) {
                Handle<Quote> vol = new Handle<Quote>(new SimpleQuote(volatility));

                return (IPricingEngine)new BlackCapFloorEngine(termStructure, vol);

            }

            public CapFloor makeCapFloor(CapFloorType type,
                                  List<CashFlow> leg,
                                  double strike,
                                  double volatility) {
                CapFloor result;
                switch (type) {
                    case CapFloorType.Cap:
                        result = (CapFloor)new Cap(leg, new List<double>() { strike });
                        break;
                    case CapFloorType.Floor:
                        result = (CapFloor)new Floor(leg, new List<double>() { strike });
                        break;
                    default:
                        throw new ArgumentException("unknown cap/floor type");
                }
                result.setPricingEngine(makeEngine(volatility));
                return result;
            }

        }

        bool checkAbsError(double x1, double x2, double tolerance) {
            return Math.Abs(x1 - x2) < tolerance;
        }

        string typeToString(CapFloorType type) {
            switch (type) {
                case CapFloorType.Cap:
                    return "cap";
                case CapFloorType.Floor:
                    return "floor";
                case CapFloorType.Collar:
                    return "collar";
                default:
                    throw new ArgumentException("unknown cap/floor type");
            }

        }

        [TestMethod()]
        public void testVega() {
            CommonVars vars = new CommonVars();

            int[] lengths = { 1, 2, 3, 4, 5, 6, 7, 10, 15, 20, 30 };
            double[] vols = { 0.01, 0.05, 0.10, 0.15, 0.20 };
            double[] strikes = { 0.01, 0.02, 0.03, 0.04, 0.05, 0.06, 0.07, 0.08, 0.09 };
            CapFloorType[] types = { CapFloorType.Cap, CapFloorType.Floor };

            Date startDate = vars.termStructure.link.referenceDate();
            double shift = 1e-8;
            double tolerance = 0.005;

            for (int i = 0; i < lengths.Length; i++) {
                for (int j = 0; j < vols.Length; j++) {
                    for (int k = 0; k < strikes.Length; k++) {
                        for (int h = 0; h < types.Length; h++) {
                            List<CashFlow> leg = vars.makeLeg(startDate, lengths[i]);
                            CapFloor capFloor = vars.makeCapFloor(types[h], leg, strikes[k], vols[j]);
                            CapFloor shiftedCapFloor2 = vars.makeCapFloor(types[h], leg, strikes[k], vols[j] + shift);
                            CapFloor shiftedCapFloor1 = vars.makeCapFloor(types[h], leg, strikes[k], vols[j] - shift);

                            double value1 = shiftedCapFloor1.NPV();
                            double value2 = shiftedCapFloor2.NPV();

                            double numericalVega = (value2 - value1) / (2 * shift);


                            if (numericalVega > 1.0e-4) {
                                double analyticalVega = (double)capFloor.result("vega");
                                double discrepancy = Math.Abs(numericalVega - analyticalVega);
                                discrepancy /= numericalVega;
                                if (discrepancy > tolerance)
                                    Assert.Fail(
                                        "failed to compute cap/floor vega:" +
                                        "\n   lengths:     " + new Period(lengths[j], TimeUnit.Years) +
                                        "\n   strike:      " + strikes[k] +
                                        "\n   types:       " + types[h] +
                                        "\n   calculated:  " + analyticalVega +
                                        "\n   expected:    " + numericalVega +
                                        "\n   discrepancy: " + discrepancy +
                                        "\n   tolerance:   " + tolerance);

                            }
                        }
                    }
                }
            }
        }

        [TestMethod()]
        public void testStrikeDependency() {

            CommonVars vars = new CommonVars();

            int[] lengths = { 1, 2, 3, 5, 7, 10, 15, 20 };
            double[] vols = { 0.01, 0.05, 0.10, 0.15, 0.20 };
            double[] strikes = { 0.03, 0.04, 0.05, 0.06, 0.07 };

            Date startDate = vars.termStructure.link.referenceDate();

            for (int i = 0; i < lengths.Length; i++) {
                for (int j = 0; j < vols.Length; j++) {
                    // store the results for different strikes...
                    List<double> cap_values = new List<double>(), floor_values = new List<double>();

                    for (int k = 0; k < strikes.Length; k++) {
                        List<CashFlow> leg = vars.makeLeg(startDate, lengths[i]);
                        Instrument cap = vars.makeCapFloor(CapFloorType.Cap, leg,
                                              strikes[k], vols[j]);
                        cap_values.Add(cap.NPV());
                        Instrument floor = vars.makeCapFloor(CapFloorType.Floor, leg,
                                              strikes[k], vols[j]);
                        floor_values.Add(floor.NPV());
                    }
                    // and check that they go the right way
                    for (int k = 0; k < cap_values.Count - 1; k++) {
                        if (cap_values[k] < cap_values[k + 1])
                            Assert.Fail(
                              "NPV is increasing with the strike in a cap: \n"
                              + "    length:     " + lengths[i] + " years\n"
                              + "    volatility: " + vols[j] + "\n"
                              + "    value:      " + cap_values[k]
                              + " at strike: " + strikes[k] + "\n"
                              + "    value:      " + cap_values[k + 1]
                              + " at strike: " + strikes[k + 1]);
                    }

                    // same for floors
                    for (int k = 0; k < floor_values.Count - 1; k++) {
                        if (floor_values[k] > floor_values[k + 1])
                            Assert.Fail(
                              "NPV is decreasing with the strike in a floor: \n"
                              + "    length:     " + lengths[i] + " years\n"
                              + "    volatility: " + vols[j] + "\n"
                              + "    value:      " + floor_values[k]
                              + " at strike: " + strikes[k] + "\n"
                              + "    value:      " + floor_values[k + 1]
                              + " at strike: " + strikes[k + 1]);
                    }
                }
            }
        }

        [TestMethod()]
        public void testConsistency() {
            CommonVars vars = new CommonVars();

            int[] lengths = { 1, 2, 3, 5, 7, 10, 15, 20 };
            double[] cap_rates = { 0.03, 0.04, 0.05, 0.06, 0.07 };
            double[] floor_rates = { 0.03, 0.04, 0.05, 0.06, 0.07 };
            double[] vols = { 0.01, 0.05, 0.10, 0.15, 0.20 };

            Date startDate = vars.termStructure.link.referenceDate();

            for (int i = 0; i < lengths.Length; i++) {
                for (int j = 0; j < cap_rates.Length; j++) {
                    for (int k = 0; k < floor_rates.Length; k++) {
                        for (int l = 0; l < vols.Length; l++) {

                            List<CashFlow> leg = vars.makeLeg(startDate, lengths[i]);
                            Instrument cap = vars.makeCapFloor(CapFloorType.Cap, leg,
                                                  cap_rates[j], vols[l]);
                            Instrument floor = vars.makeCapFloor(CapFloorType.Floor, leg,
                                                  floor_rates[k], vols[l]);
                            Collar collar = new Collar(leg, new InitializedList<double>(1, cap_rates[j]),
                                          new InitializedList<double>(1, floor_rates[k]));
                            collar.setPricingEngine(vars.makeEngine(vols[l]));

                            if (Math.Abs((cap.NPV() - floor.NPV()) - collar.NPV()) > 1e-10) {
                                Assert.Fail(
                                  "inconsistency between cap, floor and collar:\n"
                                  + "    length:       " + lengths[i] + " years\n"
                                  + "    volatility:   " + vols[l] + "\n"
                                  + "    cap value:    " + cap.NPV()
                                  + " at strike: " + cap_rates[j] + "\n"
                                  + "    floor value:  " + floor.NPV()
                                  + " at strike: " + floor_rates[k] + "\n"
                                  + "    collar value: " + collar.NPV());
                            }
                        }
                    }
                }
            }
        }

        [TestMethod()]
        public void testParity() {
            CommonVars vars = new CommonVars();

            int[] lengths = { 1, 2, 3, 5, 7, 10, 15, 20 };
            double[] strikes = { 0.0, 0.03, 0.04, 0.05, 0.06, 0.07 };
            double[] vols = { 0.01, 0.05, 0.10, 0.15, 0.20 };

            Date startDate = vars.termStructure.link.referenceDate();

            for (int i = 0; i < lengths.Length; i++) {
                for (int j = 0; j < strikes.Length; j++) {
                    for (int k = 0; k < vols.Length; k++) {

                        List<CashFlow> leg = vars.makeLeg(startDate, lengths[i]);
                        Instrument cap = vars.makeCapFloor(CapFloorType.Cap, leg, strikes[j], vols[k]);
                        Instrument floor = vars.makeCapFloor(CapFloorType.Floor, leg, strikes[j], vols[k]);
                        Date maturity = vars.calendar.advance(startDate, lengths[i], TimeUnit.Years, vars.convention);
                        Schedule schedule = new Schedule(startDate, maturity,
                                                         new Period(vars.frequency), vars.calendar,
                                                         vars.convention, vars.convention,
                                                         DateGeneration.Rule.Forward, false);
                        VanillaSwap swap = new VanillaSwap(VanillaSwap.Type.Payer, vars.nominals[0],
                                                           schedule, strikes[j], vars.index.dayCounter(),
                                                           schedule, vars.index, 0.0,
                                                           vars.index.dayCounter());
                        swap.setPricingEngine((IPricingEngine)new DiscountingSwapEngine(vars.termStructure));
                        // FLOATING_POINT_EXCEPTION
                        if (Math.Abs((cap.NPV() - floor.NPV()) - swap.NPV()) > 1.0e-10) {
                            Assert.Fail(
                                "put/call parity violated:\n"
                                + "    length:      " + lengths[i] + " years\n"
                                + "    volatility:  " + vols[k] + "\n"
                                + "    strike:      " + strikes[j] + "\n"
                                + "    cap value:   " + cap.NPV() + "\n"
                                + "    floor value: " + floor.NPV() + "\n"
                                + "    swap value:  " + swap.NPV());
                        }
                    }
                }
            }
        }

        [TestMethod()]
        public void testATMRate() {
            CommonVars vars = new CommonVars();

            int[] lengths = { 1, 2, 3, 5, 7, 10, 15, 20 };
            double[] strikes = { 0.0, 0.03, 0.04, 0.05, 0.06, 0.07 };
            double[] vols = { 0.01, 0.05, 0.10, 0.15, 0.20 };

            Date startDate = vars.termStructure.link.referenceDate();

            for (int i = 0; i < lengths.Length; i++) {
                List<CashFlow> leg = vars.makeLeg(startDate, lengths[i]);
                Date maturity = vars.calendar.advance(startDate, lengths[i], TimeUnit.Years, vars.convention);
                Schedule schedule = new Schedule(startDate, maturity,
                                                 new Period(vars.frequency), vars.calendar,
                                                 vars.convention, vars.convention,
                                                 DateGeneration.Rule.Forward, false);

                for (int j = 0; j < strikes.Length; j++) {
                    for (int k = 0; k < vols.Length; k++) {

                        CapFloor cap = vars.makeCapFloor(CapFloorType.Cap, leg, strikes[j], vols[k]);
                        CapFloor floor = vars.makeCapFloor(CapFloorType.Floor, leg, strikes[j], vols[k]);
                        double capATMRate = cap.atmRate(vars.termStructure);
                        double floorATMRate = floor.atmRate(vars.termStructure);

                        if (!checkAbsError(floorATMRate, capATMRate, 1.0e-10))
                            Assert.Fail(
                              "Cap ATM Rate and floor ATM Rate should be equal :\n"
                              + "   length:        " + lengths[i] + " years\n"
                              + "   volatility:    " + vols[k] + "\n"
                              + "   strike:        " + strikes[j] + "\n"
                              + "   cap ATM rate:  " + capATMRate + "\n"
                              + "   floor ATM rate:" + floorATMRate + "\n"
                              + "   relative Error:"
                              + Utilities.relativeError(capATMRate, floorATMRate, capATMRate) * 100 + "%");
                        VanillaSwap swap = new VanillaSwap(VanillaSwap.Type.Payer, vars.nominals[0],
                                                           schedule, floorATMRate,
                                                           vars.index.dayCounter(),
                                                           schedule, vars.index, 0.0,
                                                           vars.index.dayCounter());
                        swap.setPricingEngine((IPricingEngine)(
                                               new DiscountingSwapEngine(vars.termStructure)));
                        double swapNPV = swap.NPV();
                        if (!checkAbsError(swapNPV, 0, 1.0e-10))
                            Assert.Fail(
                              "the NPV of a Swap struck at ATM rate "
                              + "should be equal to 0:\n"
                              + "   length:        " + lengths[i] + " years\n"
                              + "   volatility:    " + vols[k] + "\n"
                              + "   ATM rate:      " + floorATMRate + "\n"
                              + "   swap NPV:      " + swapNPV);

                    }
                }
            }
        }

        [TestMethod()]
        public void testImpliedVolatility() {
            CommonVars vars = new CommonVars();

            int maxEvaluations = 100;
            double tolerance = 1.0e-6;

            CapFloorType[] types = { CapFloorType.Cap, CapFloorType.Floor };
            double[] strikes = { 0.02, 0.03, 0.04 };
            int[] lengths = { 1, 5, 10 };

            // test data
            double[] rRates = { 0.02, 0.03, 0.04 };
            double[] vols = { 0.01, 0.20, 0.30, 0.70, 0.90 };

            for (int k = 0; k < lengths.Length; k++) {
                List<CashFlow> leg = vars.makeLeg(vars.settlement, lengths[k]);

                for (int i = 0; i < types.Length; i++) {
                    for (int j = 0; j < strikes.Length; j++) {
                        CapFloor capfloor = vars.makeCapFloor(types[i], leg, strikes[j], 0.0);

                        for (int n = 0; n < rRates.Length; n++) {
                            for (int m = 0; m < vols.Length; m++) {
                                double r = rRates[n];
                                double v = vols[m];
                                vars.termStructure.linkTo(Utilities.flatRate(vars.settlement, r, new Actual360()));
                                capfloor.setPricingEngine(vars.makeEngine(v));

                                double value = capfloor.NPV();
                                double implVol = 0.0;

                                try {
                                    implVol = capfloor.impliedVolatility(value,
                                                                         vars.termStructure,
                                                                         0.10,
                                                                         tolerance,
                                                                         maxEvaluations);
                                } catch (Exception e) {
                                    // couldn't bracket?
                                    capfloor.setPricingEngine(vars.makeEngine(0.0));
                                    double value2 = capfloor.NPV();
                                    if (Math.Abs(value - value2) < tolerance) {
                                        // ok, just skip:
                                        continue;
                                    }

                                    // otherwise, report error
                                    Assert.Fail("implied vol failure: " + typeToString(types[i]) +
                                        "  strike:     " + strikes[j] +
                                        "  risk-free:  " + r +
                                        "  length:     " + lengths[k] + "Y" +
                                        "  volatility: " + v + e.Message);
                                }
                                if (Math.Abs(implVol - v) > tolerance) {
                                    // the difference might not matter
                                    capfloor.setPricingEngine(vars.makeEngine(implVol));
                                    double value2 = capfloor.NPV();
                                    if (Math.Abs(value - value2) > tolerance) {
                                        Assert.Fail(
                                            typeToString(types[i]) + ":"
                                            + "    strike:           "
                                            + strikes[j] + "\n"
                                            + "    risk-free rate:   "
                                            + r + "\n"
                                            + "    length:         "
                                            + lengths[k] + " years\n\n"
                                            + "    original volatility: "
                                            + v + "\n"
                                            + "    price:               "
                                            + value + "\n"
                                            + "    implied volatility:  "
                                            + implVol + "\n"
                                            + "    corresponding price: " + value2);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        [TestMethod()]
        public void testCachedValue() {
            CommonVars vars = new CommonVars();

            Date cachedToday = new Date(14, Month.March, 2002),
                 cachedSettlement = new Date(18, Month.March, 2002);
            Settings.setEvaluationDate(cachedToday);
            vars.termStructure.linkTo(Utilities.flatRate(cachedSettlement, 0.05, new Actual360()));
            Date startDate = vars.termStructure.link.referenceDate();
            List<CashFlow> leg = vars.makeLeg(startDate, 20);
            Instrument cap = vars.makeCapFloor(CapFloorType.Cap, leg, 0.07, 0.20);
            Instrument floor = vars.makeCapFloor(CapFloorType.Floor, leg, 0.03, 0.20);

            // par coupon price
            double cachedCapNPV = 6.87570026732,
                   cachedFloorNPV = 2.65812927959;

            // index fixing price
            //Real cachedCapNPV   = 6.87630307745,
            //   cachedFloorNPV = 2.65796764715;

            // test Black cap price against cached value
            if (Math.Abs(cap.NPV() - cachedCapNPV) > 1.0e-11)
                Assert.Fail("failed to reproduce cached cap value:\n"
                            + "    calculated: " + cap.NPV() + "\n"
                            + "    expected:   " + cachedCapNPV);

            // test Black floor price against cached value
            if (Math.Abs(floor.NPV() - cachedFloorNPV) > 1.0e-11)
                Assert.Fail("failed to reproduce cached floor value:\n"
                            + "    calculated: " + floor.NPV() + "\n"
                            + "    expected:   " + cachedFloorNPV);
        }
    }
}
