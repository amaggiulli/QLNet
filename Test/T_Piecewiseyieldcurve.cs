/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008, 2009 , 2010  Andrea Maggiulli (a.maggiulli@gmail.com)
  
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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QLNet;

namespace TestSuite {
    [TestClass()]
    public class T_PiecewiseyieldCurve {
        public class CommonVars {
            #region Values
            public struct Datum {
                public int n;
                public TimeUnit units;
                public double rate;
            };
            public struct BondDatum {
                public int n;
                public TimeUnit units;
                public int length;
                public Frequency frequency;
                public double coupon;
                public double price;
            };

            public Datum[] depositData = new Datum[] {
                new Datum { n = 1, units = TimeUnit.Weeks,  rate = 4.559 },
                new Datum { n = 1, units = TimeUnit.Months, rate = 4.581 },
                new Datum { n = 2, units = TimeUnit.Months, rate = 4.573 },
                new Datum { n = 3, units = TimeUnit.Months, rate = 4.557 },
                new Datum { n = 6, units = TimeUnit.Months, rate = 4.496 },
                new Datum { n = 9, units = TimeUnit.Months, rate = 4.490 }
            };

            public Datum[] fraData = new Datum[] {
                new Datum { n = 1, units = TimeUnit.Months, rate = 4.581 },
                new Datum { n = 2, units = TimeUnit.Months, rate = 4.573 },
                new Datum { n = 3, units = TimeUnit.Months, rate = 4.557 },
                new Datum { n = 6, units = TimeUnit.Months, rate = 4.496 },
                new Datum { n = 9, units = TimeUnit.Months, rate = 4.490 }
            };

            public Datum[] swapData = new Datum[] {
                new Datum { n =  1, units = TimeUnit.Years, rate = 4.54 },
                new Datum { n =  2, units = TimeUnit.Years, rate = 4.63 },
                new Datum { n =  3, units = TimeUnit.Years, rate = 4.75 },
                new Datum { n =  4, units = TimeUnit.Years, rate = 4.86 },
                new Datum { n =  5, units = TimeUnit.Years, rate = 4.99 },
                new Datum { n =  6, units = TimeUnit.Years, rate = 5.11 },
                new Datum { n =  7, units = TimeUnit.Years, rate = 5.23 },
                new Datum { n =  8, units = TimeUnit.Years, rate = 5.33 },
                new Datum { n =  9, units = TimeUnit.Years, rate = 5.41 },
                new Datum { n = 10, units = TimeUnit.Years, rate = 5.47 },
                new Datum { n = 12, units = TimeUnit.Years, rate = 5.60 },
                new Datum { n = 15, units = TimeUnit.Years, rate = 5.75 },
                new Datum { n = 20, units = TimeUnit.Years, rate = 5.89 },
                new Datum { n = 25, units = TimeUnit.Years, rate = 5.95 },
                new Datum { n = 30, units = TimeUnit.Years, rate = 5.96 }
            };

            public BondDatum[] bondData = new BondDatum[] {
                new BondDatum { n =  6, units = TimeUnit.Months, length = 5,  frequency = Frequency.Semiannual, coupon = 4.75, price = 101.320 },
                new BondDatum { n =  1, units = TimeUnit.Years,  length = 3,  frequency = Frequency.Semiannual, coupon = 2.75, price = 100.590 },
                new BondDatum { n =  2, units = TimeUnit.Years,  length = 5,  frequency = Frequency.Semiannual, coupon = 5.00, price = 105.650 },
                new BondDatum { n =  5, units = TimeUnit.Years,  length = 11, frequency = Frequency.Semiannual, coupon = 5.50, price = 113.610 },
                new BondDatum { n = 10, units = TimeUnit.Years,  length = 11, frequency = Frequency.Semiannual, coupon = 3.75, price = 104.070 }
            };

            public Datum[] bmaData = new Datum[] {
                new Datum { n =  1, units = TimeUnit.Years, rate = 67.56 },
                new Datum { n =  2, units = TimeUnit.Years, rate = 68.00 },
                new Datum { n =  3, units = TimeUnit.Years, rate = 68.25 },
                new Datum { n =  4, units = TimeUnit.Years, rate = 68.50 },
                new Datum { n =  5, units = TimeUnit.Years, rate = 68.81 },
                new Datum { n =  7, units = TimeUnit.Years, rate = 69.50 },
                new Datum { n = 10, units = TimeUnit.Years, rate = 70.44 },
                new Datum { n = 15, units = TimeUnit.Years, rate = 71.69 },
                new Datum { n = 20, units = TimeUnit.Years, rate = 72.69 },
                new Datum { n = 30, units = TimeUnit.Years, rate = 73.81 }
            };
            #endregion

            // global variables
            public Calendar calendar;
            public int settlementDays;
            public Date today, settlement;
            public BusinessDayConvention fixedLegConvention;
            public Frequency fixedLegFrequency;
            public DayCounter fixedLegDayCounter;
            public int bondSettlementDays;
            public DayCounter bondDayCounter;
            public BusinessDayConvention bondConvention;
            public double bondRedemption;
            public Frequency bmaFrequency;
            public BusinessDayConvention bmaConvention;
            public DayCounter bmaDayCounter;

            public int deposits, fras, swaps, bonds, bmas;
            public List<SimpleQuote> rates, fraRates, prices, fractions;
            public List<RateHelper> instruments, fraHelpers, bondHelpers, bmaHelpers;
            public List<Schedule> schedules;
            public YieldTermStructure termStructure;

            // cleanup
            // SavedSettings backup = new SavedSettings();
            // IndexHistoryCleaner cleaner;

            // setup
            public CommonVars() {

                //cleaner = new IndexHistoryCleaner();
                ////GC.Collect();
                //// force garbage collection
                //// garbage collection in .NET is rather weird and we do need when we run several tests in a row
                //GC.Collect();

                // data
                calendar = new TARGET();
                settlementDays = 2;
                today = calendar.adjust(Date.Today);
                Settings.setEvaluationDate(today);
                settlement = calendar.advance(today, settlementDays, TimeUnit.Days);
                fixedLegConvention = BusinessDayConvention.Unadjusted;
                fixedLegFrequency = Frequency.Annual;
                fixedLegDayCounter = new Thirty360(Thirty360.Thirty360Convention.European);
                bondSettlementDays = 3;
                bondDayCounter = new ActualActual();
                bondConvention = BusinessDayConvention.Following;
                bondRedemption = 100.0;
                bmaFrequency = Frequency.Quarterly;
                bmaConvention = BusinessDayConvention.Following;
                bmaDayCounter = new ActualActual();

                deposits = depositData.Length;
                fras = fraData.Length;
                swaps = swapData.Length;
                bonds = bondData.Length;
                bmas = bmaData.Length;

                // market elements
                rates = new List<SimpleQuote>(deposits + swaps);
                fraRates = new List<SimpleQuote>(fras);
                prices = new List<SimpleQuote>(bonds);
                fractions = new List<SimpleQuote>(bmas);
                for (int i = 0; i < deposits; i++) {
                    rates.Add(new SimpleQuote(depositData[i].rate / 100));
                }
                for (int i = 0; i < swaps; i++) {
                    rates.Add(new SimpleQuote(swapData[i].rate / 100));
                }
                for (int i = 0; i < fras; i++) {
                    fraRates.Add(new SimpleQuote(fraData[i].rate / 100));
                }
                for (int i = 0; i < bonds; i++) {
                    prices.Add(new SimpleQuote(bondData[i].price));
                }
                for (int i = 0; i < bmas; i++) {
                    fractions.Add(new SimpleQuote(bmaData[i].rate / 100));
                }

                // rate helpers
                instruments = new List<RateHelper>(deposits + swaps);
                fraHelpers = new List<RateHelper>(fras);
                bondHelpers = new List<RateHelper>(bonds);
                schedules = new List<Schedule>(bonds);
                bmaHelpers = new List<RateHelper>(bmas);

                IborIndex euribor6m = new Euribor6M();
                for (int i = 0; i < deposits; i++) {
                    Handle<Quote> r = new Handle<Quote>(rates[i]);
                    instruments.Add(new DepositRateHelper(r, new Period(depositData[i].n, depositData[i].units),
                                      euribor6m.fixingDays(), calendar,
                                      euribor6m.businessDayConvention(),
                                      euribor6m.endOfMonth(),
                                      euribor6m.dayCounter()));
                }
                for (int i = 0; i < swaps; i++) {
                    Handle<Quote> r = new Handle<Quote>(rates[i + deposits]);
                    instruments.Add(new SwapRateHelper(r, new Period(swapData[i].n, swapData[i].units), calendar,
                                    fixedLegFrequency, fixedLegConvention, fixedLegDayCounter, euribor6m));
                }

                Euribor3M euribor3m = new Euribor3M();
                for (int i = 0; i < fras; i++) {
                    Handle<Quote> r = new Handle<Quote>(fraRates[i]);
                    fraHelpers.Add(new FraRateHelper(r, fraData[i].n, fraData[i].n + 3,
                                      euribor3m.fixingDays(),
                                      euribor3m.fixingCalendar(),
                                      euribor3m.businessDayConvention(),
                                      euribor3m.endOfMonth(),
                                      euribor3m.dayCounter()));
                }

                for (int i = 0; i < bonds; i++) {
                    Handle<Quote> p = new Handle<Quote>(prices[i]);
                    Date maturity = calendar.advance(today, bondData[i].n, bondData[i].units);
                    Date issue = calendar.advance(maturity, -bondData[i].length, TimeUnit.Years);
                    List<double> coupons = new List<double>() { bondData[i].coupon / 100.0 };
                    schedules.Add(new Schedule(issue, maturity, new Period(bondData[i].frequency), calendar,
                                               bondConvention, bondConvention, DateGeneration.Rule.Backward, false));
                    bondHelpers.Add(new FixedRateBondHelper(p, bondSettlementDays, bondRedemption, schedules[i],
                                                coupons, bondDayCounter, bondConvention, bondRedemption, issue));
                }
            }
        }

        //[TestMethod()]
        public void testLogCubicDiscountConsistency() {
            // "Testing consistency of piecewise-log-cubic discount curve...");

            CommonVars vars = new CommonVars();

				testCurveConsistency<Discount, LogCubic, IterativeBootstrapForYield>( vars,
                        new LogCubic(CubicInterpolation.DerivativeApprox.Spline, true,
                            CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0,
                            CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0));
				testBMACurveConsistency<Discount, LogCubic, IterativeBootstrapForYield>( vars,
                        new LogCubic(CubicInterpolation.DerivativeApprox.Spline, true,
                            CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0,
                            CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0));
        }

        [TestMethod()]
        public void testLogLinearDiscountConsistency() {
            // "Testing consistency of piecewise-log-linear discount curve...");

            CommonVars vars = new CommonVars();

				testCurveConsistency<Discount, LogLinear, IterativeBootstrapForYield>( vars );
				testBMACurveConsistency<Discount, LogLinear, IterativeBootstrapForYield>( vars );
        }

        [TestMethod()]
        public void testLinearDiscountConsistency() {
            // "Testing consistency of piecewise-linear discount curve..."

            CommonVars vars = new CommonVars();

				testCurveConsistency<Discount, Linear, IterativeBootstrapForYield>( vars );
				testBMACurveConsistency<Discount, Linear, IterativeBootstrapForYield>( vars );
        }

        [TestMethod()]
        public void testLogLinearZeroConsistency() {
            // "Testing consistency of piecewise-log-linear zero-yield curve...");
			  
			  // if rates can be negative it makes no sense to interpolate loglinearly
			  if ( Utils.is_QL_NEGATIVE_RATES() )
				return;
			  else
			  {
            CommonVars vars = new CommonVars();
	
				testCurveConsistency<ZeroYield, LogLinear, IterativeBootstrapForYield>( vars );
				testBMACurveConsistency<ZeroYield, LogLinear, IterativeBootstrapForYield>( vars );
			  }
        }

        [TestMethod()]
        public void testLinearZeroConsistency() {
            // "Testing consistency of piecewise-linear zero-yield curve...");

            CommonVars vars = new CommonVars();

				testCurveConsistency<ZeroYield, Linear, IterativeBootstrapForYield>( vars );
				testBMACurveConsistency<ZeroYield, Linear, IterativeBootstrapForYield>( vars );
        }

        [TestMethod()]
        public void testSplineZeroConsistency() {

            //"Testing consistency of piecewise-cubic zero-yield curve...");

            CommonVars vars = new CommonVars();

				testCurveConsistency<ZeroYield, Cubic, IterativeBootstrapForYield>(
                           vars,
                           new Cubic(CubicInterpolation.DerivativeApprox.Spline, true,
                                 CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0,
                                 CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0));
				testBMACurveConsistency<ZeroYield, Cubic, IterativeBootstrapForYield>(
                           vars,
                           new Cubic(CubicInterpolation.DerivativeApprox.Spline, true,
                                 CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0,
                                 CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0));
        }

        [TestMethod()]
        public void testLinearForwardConsistency() {
            // "Testing consistency of piecewise-linear forward-rate curve...");

            CommonVars vars = new CommonVars();

				testCurveConsistency<ForwardRate, Linear, IterativeBootstrapForYield>( vars );
				testBMACurveConsistency<ForwardRate, Linear, IterativeBootstrapForYield>( vars );
        }

        [TestMethod()]
        public void testFlatForwardConsistency() {

            //"Testing consistency of piecewise-flat forward-rate curve...");

            CommonVars vars = new CommonVars();

				testCurveConsistency<ForwardRate, BackwardFlat, IterativeBootstrapForYield>( vars );
				testBMACurveConsistency<ForwardRate, BackwardFlat, IterativeBootstrapForYield>( vars );
        }

        //[TestMethod()]
        public void testSplineForwardConsistency() {

            //"Testing consistency of piecewise-cubic forward-rate curve...");

            CommonVars vars = new CommonVars();

				testCurveConsistency<ForwardRate, Cubic, IterativeBootstrapForYield>(
                           vars,
                           new Cubic(CubicInterpolation.DerivativeApprox.Spline, true,
                                 CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0,
                                 CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0));
				testBMACurveConsistency<ForwardRate, Cubic, IterativeBootstrapForYield>(
                           vars,
                           new Cubic(CubicInterpolation.DerivativeApprox.Spline, true,
                                 CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0,
                                 CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0));
        }

        [TestMethod()]
        public void testConvexMonotoneForwardConsistency() {
            //"Testing consistency of convex monotone forward-rate curve...");

            CommonVars vars = new CommonVars();

				testCurveConsistency<ForwardRate, ConvexMonotone, IterativeBootstrapForYield>( vars );
            testBMACurveConsistency<ForwardRate,ConvexMonotone,IterativeBootstrapForYield>(vars);
        }

        [TestMethod()]
        public void testLocalBootstrapConsistency() {
            //"Testing consistency of local-bootstrap algorithm...");

            CommonVars vars = new CommonVars();
				testCurveConsistency<ForwardRate, ConvexMonotone, LocalBootstrapForYield>( vars, new ConvexMonotone(), 1.0e-7 );
				testBMACurveConsistency<ForwardRate, ConvexMonotone, LocalBootstrapForYield>( vars, new ConvexMonotone(), 1.0e-9 );
        }

        [TestMethod()]
        public void testObservability() {
            // "Testing observability of piecewise yield curve...");

            CommonVars vars = new CommonVars();

            vars.termStructure = new PiecewiseYieldCurve<Discount, LogLinear>(vars.settlementDays,
                                                           vars.calendar, vars.instruments, new Actual360());
            Flag f = new Flag();
            vars.termStructure.registerWith(f.update);

            for (int i = 0; i < vars.deposits + vars.swaps; i++) {
                double testTime = new Actual360().yearFraction(vars.settlement, vars.instruments[i].latestDate());
                double discount = vars.termStructure.discount(testTime);
                f.lower();
                vars.rates[i].setValue(vars.rates[i].value() * 1.01);
                if (!f.isUp())
                    Assert.Fail("Observer was not notified of underlying rate change");
                double discount_new = vars.termStructure.discount(testTime, true);
                if (discount_new == discount)
                    Assert.Fail("rate change did not trigger recalculation");
                vars.rates[i].setValue(vars.rates[i].value() / 1.01);
            }

            f.lower();
            Settings.setEvaluationDate(vars.calendar.advance(vars.today, 15, TimeUnit.Days));
            if (!f.isUp())
                Assert.Fail("Observer was not notified of date change");
        }

        [TestMethod()]
        public void testLiborFixing() {

            // "Testing use of today's LIBOR fixings in swap curve...");

            CommonVars vars = new CommonVars();

            var swapHelpers = new InitializedList<RateHelper>();
            IborIndex euribor6m = new Euribor6M();

            for (int i=0; i<vars.swaps; i++) {
                Handle<Quote> r = new Handle<Quote>(vars.rates[i+vars.deposits]);
                swapHelpers.Add(new SwapRateHelper(r, new Period(vars.swapData[i].n, vars.swapData[i].units),
                                   vars.calendar,
                                   vars.fixedLegFrequency, vars.fixedLegConvention,
                                   vars.fixedLegDayCounter, euribor6m));
            }

            vars.termStructure = new PiecewiseYieldCurve<Discount, LogLinear>(vars.settlement, swapHelpers, new Actual360());

            Handle<YieldTermStructure> curveHandle = new Handle<YieldTermStructure>(vars.termStructure);

            IborIndex index = new Euribor6M(curveHandle);
            for (int i=0; i<vars.swaps; i++) {
                Period tenor = new Period(vars.swapData[i].n, vars.swapData[i].units);

                VanillaSwap swap = new MakeVanillaSwap(tenor, index, 0.0)
                                        .withEffectiveDate(vars.settlement)
                                        .withFixedLegDayCount(vars.fixedLegDayCounter)
                                        .withFixedLegTenor(new Period(vars.fixedLegFrequency))
                                        .withFixedLegConvention(vars.fixedLegConvention)
                                        .withFixedLegTerminationDateConvention(vars.fixedLegConvention)
                                        .value();

                double expectedRate = vars.swapData[i].rate / 100,
                     estimatedRate = swap.fairRate();
                double tolerance = 1.0e-9;
                if (Math.Abs(expectedRate-estimatedRate) > tolerance) {
                    Assert.Fail("before LIBOR fixing:\n"
                                + vars.swapData[i].n + " year(s) swap:\n"
                                + "    estimated rate: "
                                + (estimatedRate) + "\n"
                                + "    expected rate:  "
                                + (expectedRate));
                }
            }

            Flag f = new Flag();
            vars.termStructure.registerWith(f.update);
            f.lower();

            index.addFixing(vars.today, 0.0425);

            if (!f.isUp())
                Assert.Fail("Observer was not notified of rate fixing");

            for (int i=0; i<vars.swaps; i++) {
                Period tenor = new Period(vars.swapData[i].n, vars.swapData[i].units);

                VanillaSwap swap = new MakeVanillaSwap(tenor, index, 0.0)
                    .withEffectiveDate(vars.settlement)
                    .withFixedLegDayCount(vars.fixedLegDayCounter)
                    .withFixedLegTenor(new Period(vars.fixedLegFrequency))
                    .withFixedLegConvention(vars.fixedLegConvention)
                    .withFixedLegTerminationDateConvention(vars.fixedLegConvention)
                    .value();

                double expectedRate = vars.swapData[i].rate / 100,
                     estimatedRate = swap.fairRate();
                double tolerance = 1.0e-9;
                if (Math.Abs(expectedRate-estimatedRate) > tolerance) {
                    Assert.Fail("after LIBOR fixing:\n"
                                + vars.swapData[i].n + " year(s) swap:\n"
                                + "    estimated rate: "
                                + (estimatedRate) + "\n"
                                + "    expected rate:  "
                                + (expectedRate));
                }
            }
        }

        [TestMethod()]
        public void testForwardRateDayCounter() {

            CommonVars vars = new CommonVars();
            DayCounter d = new ActualActual();
            DayCounter d1 = new Actual360();

            vars.termStructure = new PiecewiseYieldCurve<Discount, LogLinear>(vars.settlementDays,
                                                           vars.calendar, vars.instruments, d);

            InterestRate ir = vars.termStructure.forwardRate(vars.settlement, vars.settlement + 30, d1, Compounding.Simple);

            if (ir.dayCounter().name() != d1.name())
                Assert.Fail("PiecewiseYieldCurve forwardRate dayCounter error" +
                            " Actual daycounter : " + vars.termStructure.dayCounter().name() +
                            " Expetced DayCounter : " + d1.name());


        }

        [TestMethod()]
        public void testJpyLibor() {
            //"Testing bootstrap over JPY LIBOR swaps...");

            CommonVars vars = new CommonVars();

            vars.today = new Date(4, Month.October, 2007);
            Settings.setEvaluationDate(vars.today);

            vars.calendar = new Japan();
            vars.settlement = vars.calendar.advance(vars.today,vars.settlementDays, TimeUnit.Days);

            // market elements
            vars.rates = new InitializedList<SimpleQuote>(vars.swaps);
            for (int i=0; i<vars.swaps; i++) {
                vars.rates[i] = new SimpleQuote(vars.swapData[i].rate/100);
            }

            // rate helpers
            vars.instruments = new InitializedList<RateHelper>(vars.swaps);

            IborIndex index = new JPYLibor(new Period(6, TimeUnit.Months));
            for (int i=0; i<vars.swaps; i++) {
                Handle<Quote> r = new Handle<Quote>(vars.rates[i]);
                vars.instruments[i] = new SwapRateHelper(r, new Period(vars.swapData[i].n, vars.swapData[i].units),
                                                          vars.calendar,
                                                          vars.fixedLegFrequency, vars.fixedLegConvention,
                                                          vars.fixedLegDayCounter, index);
            }

            vars.termStructure = new PiecewiseYieldCurve<Discount,LogLinear>(
                                               vars.settlement, vars.instruments,
                                               new Actual360(),
                                               new List<Handle<Quote>>(),
                                               new List<Date>(),
                                               1.0e-12);

            RelinkableHandle<YieldTermStructure> curveHandle = new RelinkableHandle<YieldTermStructure>();
            curveHandle.linkTo(vars.termStructure);

            // check swaps
            IborIndex jpylibor6m = new JPYLibor(new Period(6, TimeUnit.Months),curveHandle);
            for (int i=0; i<vars.swaps; i++) {
                Period tenor = new Period(vars.swapData[i].n, vars.swapData[i].units);

                VanillaSwap swap = new MakeVanillaSwap(tenor, jpylibor6m, 0.0)
                    .withEffectiveDate(vars.settlement)
                    .withFixedLegDayCount(vars.fixedLegDayCounter)
                    .withFixedLegTenor(new Period(vars.fixedLegFrequency))
                    .withFixedLegConvention(vars.fixedLegConvention)
                    .withFixedLegTerminationDateConvention(vars.fixedLegConvention)
                    .withFixedLegCalendar(vars.calendar)
                    .withFloatingLegCalendar(vars.calendar)
                    .value();

                double expectedRate = vars.swapData[i].rate/100,
                     estimatedRate = swap.fairRate();
                double error = Math.Abs(expectedRate-estimatedRate);
                double tolerance = 1.0e-9;

                if (error > tolerance) {
                    Assert.Fail(vars.swapData[i].n + " year(s) swap:\n"
                                + "\n estimated rate: " + (estimatedRate)
                                + "\n expected rate:  " + (expectedRate)
                                + "\n error:          " + (error)
                                + "\n tolerance:      " + (tolerance));
                }
            }
        }

        [TestMethod()]
        public void testDiscountCopy() {
            //BOOST_MESSAGE("Testing copying of discount curve...");

            CommonVars vars = new CommonVars();
            testCurveCopy<Discount, LogLinear>(vars);
        }

        [TestMethod()]
        public void testForwardCopy() {
            //BOOST_MESSAGE("Testing copying of forward-rate curve...");

            CommonVars vars = new CommonVars();
            testCurveCopy<ForwardRate, BackwardFlat>(vars);
        }

        [TestMethod()]
        public void testZeroCopy() {
            //BOOST_MESSAGE("Testing copying of zero-rate curve...");

            CommonVars vars = new CommonVars();
            testCurveCopy<ZeroYield, Linear>(vars);
        }


        public void testCurveConsistency<T, I, B>(CommonVars vars)
			  where T : ITraits<YieldTermStructure>, new()
           where I : IInterpolationFactory, new()
			  where B : IBootStrap<PiecewiseYieldCurve>, new() { testCurveConsistency<T, I, B>( vars, new I(), 1.0e-9 ); }
        public void testCurveConsistency<T, I, B>(CommonVars vars, I interpolator)
			  where T : ITraits<YieldTermStructure>, new()
           where I : IInterpolationFactory, new()
			  where B : IBootStrap<PiecewiseYieldCurve>, new() { testCurveConsistency<T, I, B>( vars, new I(), 1.0e-9 ); }
        public void testCurveConsistency<T, I, B>(CommonVars vars, I interpolator, double tolerance)
			  where T : ITraits<YieldTermStructure>, new()
           where I : IInterpolationFactory, new()
			  where B : IBootStrap<PiecewiseYieldCurve>, new()
		  {

            vars.termStructure = new PiecewiseYieldCurve<T, I, B>(vars.settlement, vars.instruments,
                                    new Actual360(), new List<Handle<Quote>>(), new List<Date>(), 1.0e-12, interpolator);

            RelinkableHandle<YieldTermStructure> curveHandle = new RelinkableHandle<YieldTermStructure>();
            curveHandle.linkTo(vars.termStructure);

            // check deposits
            for (int i = 0; i < vars.deposits; i++) {
                Euribor index = new Euribor(new Period(vars.depositData[i].n, vars.depositData[i].units), curveHandle);
                double expectedRate = vars.depositData[i].rate / 100,
                       estimatedRate = index.fixing(vars.today);
                if (Math.Abs(expectedRate - estimatedRate) > tolerance) {
                    Console.WriteLine(vars.depositData[i].n + " "
                           + (vars.depositData[i].units == TimeUnit.Weeks ? "week(s)" : "month(s)")
                           + " deposit:"
                           + "\n    estimated rate: " + estimatedRate
                           + "\n    expected rate:  " + expectedRate);
                }
            }

            // check swaps
            IborIndex euribor6m = new Euribor6M(curveHandle);
            for (int i = 0; i < vars.swaps; i++) {
                Period tenor = new Period(vars.swapData[i].n, vars.swapData[i].units);

                VanillaSwap swap = new MakeVanillaSwap(tenor, euribor6m, 0.0)
                    .withEffectiveDate(vars.settlement)
                    .withFixedLegDayCount(vars.fixedLegDayCounter)
                    .withFixedLegTenor(new Period(vars.fixedLegFrequency))
                    .withFixedLegConvention(vars.fixedLegConvention)
                    .withFixedLegTerminationDateConvention(vars.fixedLegConvention);

                double expectedRate = vars.swapData[i].rate / 100,
                     estimatedRate = swap.fairRate();
                double error = Math.Abs(expectedRate - estimatedRate);
                if (error > tolerance) {
                    Console.WriteLine(vars.swapData[i].n + " year(s) swap:\n"
                        + "\n estimated rate: " + estimatedRate
                        + "\n expected rate:  " + expectedRate
                        + "\n error:          " + error
                        + "\n tolerance:      " + tolerance);
                }
            }

            // check bonds
            vars.termStructure = new PiecewiseYieldCurve<T, I, B>(vars.settlement, vars.bondHelpers,
                                     new Actual360(), new List<Handle<Quote>>(), new List<Date>(), 1.0e-12, interpolator);
            curveHandle.linkTo(vars.termStructure);

            for (int i = 0; i < vars.bonds; i++) {
                Date maturity = vars.calendar.advance(vars.today, vars.bondData[i].n, vars.bondData[i].units);
                Date issue = vars.calendar.advance(maturity, -vars.bondData[i].length, TimeUnit.Years);
                List<double> coupons = new List<double>() { vars.bondData[i].coupon / 100.0 };

                FixedRateBond bond = new FixedRateBond(vars.bondSettlementDays, 100.0,
                                   vars.schedules[i], coupons,
                                   vars.bondDayCounter, vars.bondConvention,
                                   vars.bondRedemption, issue);

                IPricingEngine bondEngine = new DiscountingBondEngine(curveHandle);
                bond.setPricingEngine(bondEngine);

                double expectedPrice = vars.bondData[i].price,
                       estimatedPrice = bond.cleanPrice();
                if (Math.Abs(expectedPrice - estimatedPrice) > tolerance) {
                    Console.WriteLine(i + 1 + " bond failure:" +
                                "\n  estimated price: " + estimatedPrice +
                                "\n  expected price:  " + expectedPrice);
                }
            }

            // check FRA
            vars.termStructure = new PiecewiseYieldCurve<T, I, B>(vars.settlement, vars.fraHelpers,
                                        new Actual360(), new List<Handle<Quote>>(), new List<Date>(), 1.0e-12, interpolator);
            curveHandle.linkTo(vars.termStructure);

            IborIndex euribor3m = new Euribor3M(curveHandle);
            for (int i = 0; i < vars.fras; i++) {
                Date start = vars.calendar.advance(vars.settlement,
                                          vars.fraData[i].n,
                                          vars.fraData[i].units,
                                          euribor3m.businessDayConvention(),
                                          euribor3m.endOfMonth());
                Date end = vars.calendar.advance(start, 3, TimeUnit.Months,
                                                 euribor3m.businessDayConvention(),
                                                 euribor3m.endOfMonth());

                ForwardRateAgreement fra = new ForwardRateAgreement(start, end, Position.Type.Long, vars.fraData[i].rate / 100,
                                                                    100.0, euribor3m, curveHandle);
                double expectedRate = vars.fraData[i].rate / 100,
                       estimatedRate = fra.forwardRate().rate();
                if (Math.Abs(expectedRate - estimatedRate) > tolerance) {
                    Console.WriteLine(i + 1 + " FRA failure:" +
                                "\n  estimated rate: " + estimatedRate +
                                "\n  expected rate:  " + expectedRate);
                }
            }
        }

        public void testBMACurveConsistency<T, I, B>(CommonVars vars)
			  where T : ITraits<YieldTermStructure>, new()
           where I : IInterpolationFactory, new()
			  where B : IBootStrap<PiecewiseYieldCurve>, new() { testBMACurveConsistency<T, I, B>( vars, new I(), 1.0e-7 ); }
        public void testBMACurveConsistency<T, I, B>(CommonVars vars, I interpolator)
			  where T : ITraits<YieldTermStructure>, new()
           where I : IInterpolationFactory, new()
			  where B : IBootStrap<PiecewiseYieldCurve>, new() { testBMACurveConsistency<T, I, B>( vars, interpolator, 1.0e-7 ); }
        public void testBMACurveConsistency<T, I, B>(CommonVars vars, I interpolator, double tolerance)
			  where T : ITraits<YieldTermStructure>, new()
           where I : IInterpolationFactory, new()
			  where B : IBootStrap<PiecewiseYieldCurve>, new()
		  {

            // readjust settlement
            vars.calendar = new JointCalendar(new BMAIndex().fixingCalendar(),
                                          new USDLibor(new Period(3, TimeUnit.Months)).fixingCalendar(),
                                          JointCalendar.JointCalendarRule.JoinHolidays);
            vars.today = vars.calendar.adjust(Date.Today);
            Settings.setEvaluationDate(vars.today);
            vars.settlement = vars.calendar.advance(vars.today, vars.settlementDays, TimeUnit.Days);

            Handle<YieldTermStructure> riskFreeCurve = new Handle<YieldTermStructure>(
                                                       new FlatForward(vars.settlement, 0.04, new Actual360()));

            BMAIndex bmaIndex = new BMAIndex();
            IborIndex liborIndex = new USDLibor(new Period(3, TimeUnit.Months), riskFreeCurve);
            for (int i = 0; i < vars.bmas; ++i) {
                Handle<Quote> f = new Handle<Quote>(vars.fractions[i]);
                vars.bmaHelpers.Add(new BMASwapRateHelper(f, new Period(vars.bmaData[i].n, vars.bmaData[i].units),
                                                vars.settlementDays,
                                                vars.calendar,
                                                new Period(vars.bmaFrequency),
                                                vars.bmaConvention,
                                                vars.bmaDayCounter,
                                                bmaIndex,
                                                liborIndex));
            }

            int w = vars.today.weekday();
            Date lastWednesday = (w >= 4) ? vars.today - (w - 4) : vars.today + (4 - w - 7);
            Date lastFixing = bmaIndex.fixingCalendar().adjust(lastWednesday);
            bmaIndex.addFixing(lastFixing, 0.03);

            vars.termStructure = new PiecewiseYieldCurve<T, I, B>(vars.settlement, vars.bmaHelpers,
                                     new Actual360(), new List<Handle<Quote>>(), new List<Date>(), 1.0e-12, interpolator);

            RelinkableHandle<YieldTermStructure> curveHandle = new RelinkableHandle<YieldTermStructure>();
            curveHandle.linkTo(vars.termStructure);

            // check BMA swaps
            BMAIndex bma = new BMAIndex(curveHandle);
            IborIndex libor3m = new USDLibor(new Period(3, TimeUnit.Months), riskFreeCurve);
            for (int i = 0; i < vars.bmas; i++) {
                Period tenor = new Period(vars.bmaData[i].n, vars.bmaData[i].units);

                Schedule bmaSchedule = new MakeSchedule().from(vars.settlement)
                              .to(vars.settlement + tenor)
                              .withFrequency(vars.bmaFrequency)
                              .withCalendar(bma.fixingCalendar())
                              .withConvention(vars.bmaConvention)
                              .backwards()
                              .value();

                Schedule liborSchedule = new MakeSchedule().from(vars.settlement)
                               .to(vars.settlement + tenor)
                               .withTenor(libor3m.tenor())
                               .withCalendar(libor3m.fixingCalendar())
                               .withConvention(libor3m.businessDayConvention())
                               .endOfMonth(libor3m.endOfMonth())
                               .backwards()
                               .value();
                  
                BMASwap swap = new BMASwap(BMASwap.Type.Payer, 100.0, liborSchedule, 0.75, 0.0,
                                           libor3m, libor3m.dayCounter(), bmaSchedule, bma, vars.bmaDayCounter);
                swap.setPricingEngine(new DiscountingSwapEngine(libor3m.forwardingTermStructure()));

                double expectedFraction = vars.bmaData[i].rate / 100,
                     estimatedFraction = swap.fairLiborFraction();
                double error = Math.Abs(expectedFraction - estimatedFraction);
                if (error > tolerance) {
                    Console.WriteLine(vars.bmaData[i].n + " year(s) BMA swap:\n"
                                + "\n estimated libor fraction: " + estimatedFraction
                                + "\n expected libor fraction:  " + expectedFraction
                                + "\n error:          " + error
                                + "\n tolerance:      " + tolerance);
                }
            }

            // this is a workaround for grabage collection
            // garbage collection needs a proper solution
            IndexManager.instance().clearHistories();
        }

        public void testCurveCopy<T, I>(CommonVars vars)
			  where T : ITraits<YieldTermStructure>, new()
            where I : IInterpolationFactory, new() {
            testCurveCopy<T, I>(vars, new I());
        }
        public void testCurveCopy<T, I>(CommonVars vars, I interpolator)
			  where T : ITraits<YieldTermStructure>, new()
            where I : IInterpolationFactory, new() {

            PiecewiseYieldCurve<T,I> curve = new PiecewiseYieldCurve<T,I>(vars.settlement, vars.instruments,
                                                                          new Actual360(),
                                                                          new List<Handle<Quote>>(),
                                                                          new List<Date>(),
                                                                          1.0e-12,
                                                                          interpolator);
            // necessary to trigger bootstrap
            curve.recalculate();

            PiecewiseYieldCurve copiedCurve = curve.Clone() as PiecewiseYieldCurve;

            // the two curves should be the same.
            double t = 2.718;
            var r1 = curve.zeroRate(t, Compounding.Continuous).value();
            var r2 = copiedCurve.zeroRate(t, Compounding.Continuous).value();
            if (!Utils.close(r1, r2)) {
                Assert.Fail("failed to link original and copied curve");
            }

            for (int i=0; i<vars.rates.Count; ++i) {
                vars.rates[i].setValue(vars.rates[i].value() + 0.001);
            }

            // now the original curve should have changed; the copied
            // curve should not.
            double r3 = curve.zeroRate(t, Compounding.Continuous).value();
            double r4 = copiedCurve.zeroRate(t, Compounding.Continuous).value();
            if (Utils.close(r1, r3)) {
                Assert.Fail("failed to modify original curve");
            }
            if (!Utils.close(r2,r4)) {
                Assert.Fail("failed to break link between original and copied curve");
            }
        }
   }
}
