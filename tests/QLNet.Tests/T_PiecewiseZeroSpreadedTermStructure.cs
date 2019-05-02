/*
 Copyright (C) 2008-2016  Andrea Maggiulli (a.maggiulli@gmail.com)

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
using System.Linq;
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
   public class T_PiecewiseZeroSpreadedTermStructure
   {
      public class CommonVars
      {
         // common data
         public Calendar calendar;
         public int settlementDays;
         public DayCounter dayCount;
         public Compounding compounding;
         public YieldTermStructure termStructure;
         public Date today;
         public Date settlementDate;

         // cleanup
         public SavedSettings backup;

         // setup
         public CommonVars()
         {
            // force garbage collection
            // garbage collection in .NET is rather weird and we do need when we run several tests in a row
            GC.Collect();

            // data
            calendar = new TARGET();
            settlementDays = 2;
            today = new Date(9, Month.June, 2009);
            compounding = Compounding.Continuous;
            dayCount = new Actual360();
            settlementDate = calendar.advance(today, settlementDays, TimeUnit.Days);

            Settings.setEvaluationDate(today);

            int[] ts = new int[] { 13, 41, 75, 165, 256, 345, 524, 703 };
            double[] r = new double[] { 0.035, 0.033, 0.034, 0.034, 0.036, 0.037, 0.039, 0.040 };
            List<double> rates = new List<double>() { 0.035 };
            List<Date> dates = new List<Date>() { settlementDate };
            for (int i = 0; i < 8; ++i)
            {
               dates.Add(calendar.advance(today, ts[i], TimeUnit.Days));
               rates.Add(r[i]);
            }
            termStructure = new InterpolatedZeroCurve<Linear>(dates, rates, dayCount);
         }
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testFlatInterpolationLeft()
      {
         // Testing flat interpolation before the first spreaded date...

         CommonVars vars = new CommonVars();

         List<Handle<Quote>> spreads = new List<Handle<Quote>>();
         SimpleQuote spread1 = new SimpleQuote(0.02);
         SimpleQuote spread2 = new SimpleQuote(0.03);
         spreads.Add(new Handle<Quote>(spread1));
         spreads.Add(new Handle<Quote>(spread2));

         List<Date> spreadDates = new List<Date>();
         spreadDates.Add(vars.calendar.advance(vars.today, 8, TimeUnit.Months));
         spreadDates.Add(vars.calendar.advance(vars.today, 15, TimeUnit.Months));

         Date interpolationDate = vars.calendar.advance(vars.today, 6, TimeUnit.Months);

         ZeroYieldStructure spreadedTermStructure =
            new PiecewiseZeroSpreadedTermStructure(
            new Handle<YieldTermStructure>(vars.termStructure),
            spreads, spreadDates);

         double t = vars.dayCount.yearFraction(vars.today, interpolationDate);
         double interpolatedZeroRate = spreadedTermStructure.zeroRate(t, vars.compounding).value();

         double tolerance = 1e-9;
         double expectedRate = vars.termStructure.zeroRate(t, vars.compounding).value() + spread1.value();

         if (Math.Abs(interpolatedZeroRate - expectedRate) > tolerance)
            QAssert.Fail("unable to reproduce interpolated rate\n"
                         + "    calculated: " + interpolatedZeroRate + "\n"
                         + "    expected: " + expectedRate);
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testFlatInterpolationRight()
      {
         // Testing flat interpolation after the last spreaded date...

         CommonVars vars = new CommonVars();

         List<Handle<Quote>> spreads = new List<Handle<Quote>>();
         SimpleQuote spread1 = new SimpleQuote(0.02);
         SimpleQuote spread2 = new SimpleQuote(0.03);
         spreads.Add(new Handle<Quote>(spread1));
         spreads.Add(new Handle<Quote>(spread2));

         List<Date> spreadDates = new List<Date>();
         spreadDates.Add(vars.calendar.advance(vars.today, 8, TimeUnit.Months));
         spreadDates.Add(vars.calendar.advance(vars.today, 15, TimeUnit.Months));

         Date interpolationDate = vars.calendar.advance(vars.today, 20, TimeUnit.Months);

         ZeroYieldStructure spreadedTermStructure =
            new PiecewiseZeroSpreadedTermStructure(
            new Handle<YieldTermStructure>(vars.termStructure),
            spreads, spreadDates);
         spreadedTermStructure.enableExtrapolation();

         double t = vars.dayCount.yearFraction(vars.today, interpolationDate);
         double interpolatedZeroRate = spreadedTermStructure.zeroRate(t, vars.compounding).value();

         double tolerance = 1e-9;
         double expectedRate = vars.termStructure.zeroRate(t, vars.compounding).value() + spread2.value();

         if (Math.Abs(interpolatedZeroRate - expectedRate) > tolerance)
            QAssert.Fail("unable to reproduce interpolated rate\n"
                         + "    calculated: " + interpolatedZeroRate + "\n"
                         + "    expected: " + expectedRate);
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testLinearInterpolationMultipleSpreads()
      {
         // Testing linear interpolation with more than two spreaded dates...

         CommonVars vars = new CommonVars();

         List<Handle<Quote>> spreads = new List<Handle<Quote>>();
         SimpleQuote spread1 = new SimpleQuote(0.02);
         SimpleQuote spread2 = new SimpleQuote(0.02);
         SimpleQuote spread3 = new SimpleQuote(0.035);
         SimpleQuote spread4 = new SimpleQuote(0.04);
         spreads.Add(new Handle<Quote>(spread1));
         spreads.Add(new Handle<Quote>(spread2));
         spreads.Add(new Handle<Quote>(spread3));
         spreads.Add(new Handle<Quote>(spread4));

         List<Date> spreadDates = new List<Date>();
         spreadDates.Add(vars.calendar.advance(vars.today, 90, TimeUnit.Days));
         spreadDates.Add(vars.calendar.advance(vars.today, 150, TimeUnit.Days));
         spreadDates.Add(vars.calendar.advance(vars.today, 30, TimeUnit.Months));
         spreadDates.Add(vars.calendar.advance(vars.today, 40, TimeUnit.Months));

         Date interpolationDate = vars.calendar.advance(vars.today, 120, TimeUnit.Days);

         ZeroYieldStructure spreadedTermStructure =
            new PiecewiseZeroSpreadedTermStructure(
            new Handle<YieldTermStructure>(vars.termStructure),
            spreads, spreadDates);

         double t = vars.dayCount.yearFraction(vars.today, interpolationDate);
         double interpolatedZeroRate = spreadedTermStructure.zeroRate(t, vars.compounding).value();

         double tolerance = 1e-9;
         double expectedRate = vars.termStructure.zeroRate(t, vars.compounding).value() +
                               spread1.value();

         if (Math.Abs(interpolatedZeroRate - expectedRate) > tolerance)
            QAssert.Fail(
               "unable to reproduce interpolated rate\n"

               + "    calculated: " + interpolatedZeroRate + "\n"
               + "    expected: " + expectedRate);
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testLinearInterpolation()
      {
         // Testing linear interpolation between two dates...

         CommonVars vars = new CommonVars();

         List<Handle<Quote>> spreads = new List<Handle<Quote>>();
         SimpleQuote spread1 = new SimpleQuote(0.02);
         SimpleQuote spread2 = new SimpleQuote(0.03);
         spreads.Add(new Handle<Quote>(spread1));
         spreads.Add(new Handle<Quote>(spread2));

         List<Date> spreadDates = new List<Date>();
         spreadDates.Add(vars.calendar.advance(vars.today, 100, TimeUnit.Days));
         spreadDates.Add(vars.calendar.advance(vars.today, 150, TimeUnit.Days));

         Date interpolationDate = vars.calendar.advance(vars.today, 120, TimeUnit.Days);

         ZeroYieldStructure spreadedTermStructure =
            new InterpolatedPiecewiseZeroSpreadedTermStructure<Linear>(
            new Handle<YieldTermStructure>(vars.termStructure),
            spreads, spreadDates);

         Date d0 = vars.calendar.advance(vars.today, 100, TimeUnit.Days);
         Date d1 = vars.calendar.advance(vars.today, 150, TimeUnit.Days);
         Date d2 = vars.calendar.advance(vars.today, 120, TimeUnit.Days);

         double m = (0.03 - 0.02) / vars.dayCount.yearFraction(d0, d1);
         double expectedRate = m * vars.dayCount.yearFraction(d0, d2) + 0.054;

         double t = vars.dayCount.yearFraction(vars.settlementDate, interpolationDate);
         double interpolatedZeroRate = spreadedTermStructure.zeroRate(t, vars.compounding).value();

         double tolerance = 1e-9;

         if (Math.Abs(interpolatedZeroRate - expectedRate) > tolerance)
            QAssert.Fail(
               "unable to reproduce interpolated rate\n"
               + "    calculated: " + interpolatedZeroRate + "\n"
               + "    expected: " + expectedRate);
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testForwardFlatInterpolation()
      {
         // Testing forward flat interpolation between two dates...

         CommonVars vars = new CommonVars();

         List<Handle<Quote>> spreads = new List<Handle<Quote>>();
         SimpleQuote spread1 = new SimpleQuote(0.02);
         SimpleQuote spread2 = new SimpleQuote(0.03);
         spreads.Add(new Handle<Quote>(spread1));
         spreads.Add(new Handle<Quote>(spread2));

         List<Date> spreadDates = new List<Date>();
         spreadDates.Add(vars.calendar.advance(vars.today, 75, TimeUnit.Days));
         spreadDates.Add(vars.calendar.advance(vars.today, 260, TimeUnit.Days));

         Date interpolationDate = vars.calendar.advance(vars.today, 100, TimeUnit.Days);

         ZeroYieldStructure spreadedTermStructure =
            new InterpolatedPiecewiseZeroSpreadedTermStructure<ForwardFlat>(
            new Handle<YieldTermStructure>(vars.termStructure),
            spreads, spreadDates);

         double t = vars.dayCount.yearFraction(vars.today, interpolationDate);
         double interpolatedZeroRate = spreadedTermStructure.zeroRate(t, vars.compounding).value();

         double tolerance = 1e-9;
         double expectedRate = vars.termStructure.zeroRate(t, vars.compounding).value() +
                               spread1.value();

         if (Math.Abs(interpolatedZeroRate - expectedRate) > tolerance)
            QAssert.Fail(
               "unable to reproduce interpolated rate\n"
               + "    calculated: " + interpolatedZeroRate + "\n"
               + "    expected: " + expectedRate);
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testBackwardFlatInterpolation()
      {
         // Testing backward flat interpolation between two dates...

         CommonVars vars = new CommonVars();

         List<Handle<Quote>> spreads = new List<Handle<Quote>>();
         SimpleQuote spread1 = new SimpleQuote(0.02);
         SimpleQuote spread2 = new SimpleQuote(0.03);
         SimpleQuote spread3 = new SimpleQuote(0.04);
         spreads.Add(new Handle<Quote>(spread1));
         spreads.Add(new Handle<Quote>(spread2));
         spreads.Add(new Handle<Quote>(spread3));

         List<Date> spreadDates = new List<Date>();
         spreadDates.Add(vars.calendar.advance(vars.today, 100, TimeUnit.Days));
         spreadDates.Add(vars.calendar.advance(vars.today, 200, TimeUnit.Days));
         spreadDates.Add(vars.calendar.advance(vars.today, 300, TimeUnit.Days));

         Date interpolationDate = vars.calendar.advance(vars.today, 110, TimeUnit.Days);

         ZeroYieldStructure spreadedTermStructure =
            new InterpolatedPiecewiseZeroSpreadedTermStructure<BackwardFlat>(
            new Handle<YieldTermStructure>(vars.termStructure),
            spreads, spreadDates);

         double t = vars.dayCount.yearFraction(vars.today, interpolationDate);
         double interpolatedZeroRate = spreadedTermStructure.zeroRate(t, vars.compounding).value();

         double tolerance = 1e-9;
         double expectedRate = vars.termStructure.zeroRate(t, vars.compounding).value() +
                               spread2.value();

         if (Math.Abs(interpolatedZeroRate - expectedRate) > tolerance)
            QAssert.Fail(
               "unable to reproduce interpolated rate\n"
               + "    calculated: " + interpolatedZeroRate + "\n"
               + "    expected: " + expectedRate);

      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testDefaultInterpolation()
      {
         // Testing default interpolation between two dates...

         CommonVars vars = new CommonVars();

         List<Handle<Quote>> spreads = new List<Handle<Quote>>();
         SimpleQuote spread1 = new SimpleQuote(0.02);
         SimpleQuote spread2 = new SimpleQuote(0.02);
         spreads.Add(new Handle<Quote>(spread1));
         spreads.Add(new Handle<Quote>(spread2));

         List<Date> spreadDates = new List<Date>();
         spreadDates.Add(vars.calendar.advance(vars.today, 75, TimeUnit.Days));
         spreadDates.Add(vars.calendar.advance(vars.today, 160, TimeUnit.Days));

         Date interpolationDate = vars.calendar.advance(vars.today, 100, TimeUnit.Days);

         ZeroYieldStructure spreadedTermStructure =
            new PiecewiseZeroSpreadedTermStructure(
            new Handle<YieldTermStructure>(vars.termStructure),
            spreads, spreadDates);

         double t = vars.dayCount.yearFraction(vars.today, interpolationDate);
         double interpolatedZeroRate = spreadedTermStructure.zeroRate(t, vars.compounding).value();

         double tolerance = 1e-9;
         double expectedRate = vars.termStructure.zeroRate(t, vars.compounding).value() +
                               spread1.value();

         if (Math.Abs(interpolatedZeroRate - expectedRate) > tolerance)
            QAssert.Fail(
               "unable to reproduce interpolated rate\n"
               + "    calculated: " + interpolatedZeroRate + "\n"
               + "    expected: " + expectedRate);
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testSetInterpolationFactory()
      {
         // Testing factory constructor with additional parameters...

         CommonVars vars = new CommonVars();

         List<Handle<Quote>> spreads = new List<Handle<Quote>>();
         SimpleQuote spread1 = new SimpleQuote(0.02);
         SimpleQuote spread2 = new SimpleQuote(0.03);
         SimpleQuote spread3 = new SimpleQuote(0.01);
         spreads.Add(new Handle<Quote>(spread1));
         spreads.Add(new Handle<Quote>(spread2));
         spreads.Add(new Handle<Quote>(spread3));

         List<Date> spreadDates = new List<Date>();
         spreadDates.Add(vars.calendar.advance(vars.today, 8, TimeUnit.Months));
         spreadDates.Add(vars.calendar.advance(vars.today, 15, TimeUnit.Months));
         spreadDates.Add(vars.calendar.advance(vars.today, 25, TimeUnit.Months));

         Date interpolationDate = vars.calendar.advance(vars.today, 11, TimeUnit.Months);

         ZeroYieldStructure spreadedTermStructure;

         Frequency freq = Frequency.NoFrequency;

         Cubic factory = new Cubic(CubicInterpolation.DerivativeApprox.Spline,
                                   false,
                                   CubicInterpolation.BoundaryCondition.SecondDerivative, 0,
                                   CubicInterpolation.BoundaryCondition.SecondDerivative, 0);

         spreadedTermStructure =
            new InterpolatedPiecewiseZeroSpreadedTermStructure<Cubic>(
            new Handle<YieldTermStructure>(vars.termStructure),
            spreads, spreadDates, vars.compounding,
            freq, vars.dayCount, factory);

         double t = vars.dayCount.yearFraction(vars.today, interpolationDate);
         double interpolatedZeroRate = spreadedTermStructure.zeroRate(t, vars.compounding).value();

         double tolerance = 1e-9;
         double expectedRate = vars.termStructure.zeroRate(t, vars.compounding).value() +
                               0.026065770863;

         if (Math.Abs(interpolatedZeroRate - expectedRate) > tolerance)
            QAssert.Fail(
               "unable to reproduce interpolated rate\n"
               + "    calculated: " + interpolatedZeroRate + "\n"
               + "    expected: " + expectedRate);
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testMaxDate()
      {
         // Testing term structure max date...

         CommonVars vars = new CommonVars();

         List<Handle<Quote>> spreads = new List<Handle<Quote>>();
         SimpleQuote spread1 = new SimpleQuote(0.02);
         SimpleQuote spread2 = new SimpleQuote(0.03);
         spreads.Add(new Handle<Quote>(spread1));
         spreads.Add(new Handle<Quote>(spread2));

         List<Date> spreadDates = new List<Date>();
         spreadDates.Add(vars.calendar.advance(vars.today, 8, TimeUnit.Months));
         spreadDates.Add(vars.calendar.advance(vars.today, 15, TimeUnit.Months));

         ZeroYieldStructure spreadedTermStructure =
            new PiecewiseZeroSpreadedTermStructure(
            new Handle<YieldTermStructure>(vars.termStructure),
            spreads, spreadDates);

         Date maxDate = spreadedTermStructure.maxDate();

         Date expectedDate = vars.termStructure.maxDate() < spreadDates.Last() ? vars.termStructure.maxDate() : spreadDates.Last();

         if (maxDate != expectedDate)
            QAssert.Fail(
               "unable to reproduce max date\n"
               + "    calculated: " + maxDate + "\n"
               + "    expected: " + expectedDate);
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testQuoteChanging()
      {
         // Testing quote update...

         CommonVars vars = new CommonVars();

         List<Handle<Quote>> spreads = new List<Handle<Quote>>();
         SimpleQuote spread1 = new SimpleQuote(0.02);
         SimpleQuote spread2 = new SimpleQuote(0.03);
         spreads.Add(new Handle<Quote>(spread1));
         spreads.Add(new Handle<Quote>(spread2));

         List<Date> spreadDates = new List<Date>();
         spreadDates.Add(vars.calendar.advance(vars.today, 100, TimeUnit.Days));
         spreadDates.Add(vars.calendar.advance(vars.today, 150, TimeUnit.Days));

         Date interpolationDate = vars.calendar.advance(vars.today, 120, TimeUnit.Days);

         ZeroYieldStructure spreadedTermStructure =
            new InterpolatedPiecewiseZeroSpreadedTermStructure<BackwardFlat>(
            new Handle<YieldTermStructure>(vars.termStructure),
            spreads, spreadDates);

         double t = vars.dayCount.yearFraction(vars.settlementDate, interpolationDate);
         double interpolatedZeroRate = spreadedTermStructure.zeroRate(t, vars.compounding).value();
         double tolerance = 1e-9;
         double expectedRate = vars.termStructure.zeroRate(t, vars.compounding).value() +
                               0.03;

         if (Math.Abs(interpolatedZeroRate - expectedRate) > tolerance)
            QAssert.Fail(
               "unable to reproduce interpolated rate\n"
               + "    calculated: " + interpolatedZeroRate + "\n"
               + "    expected: " + expectedRate);

         spread2.setValue(0.025);

         interpolatedZeroRate = spreadedTermStructure.zeroRate(t, vars.compounding).value();
         expectedRate = vars.termStructure.zeroRate(t, vars.compounding).value() +
                        0.025;

         if (Math.Abs(interpolatedZeroRate - expectedRate) > tolerance)
            QAssert.Fail(
               "unable to reproduce interpolated rate\n"
               + "    calculated: " + interpolatedZeroRate + "\n"
               + "    expected: " + expectedRate);
      }
   }
}
