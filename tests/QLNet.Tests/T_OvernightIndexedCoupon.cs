//  Copyright (C) 2008-2022 Andrea Maggiulli (a.maggiulli@gmail.com)
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
//  FOR A PARTICULAR PURPOSE.  See the license for more details.using System;

using System;
using System.Linq;
using QLNet;
using Xunit;
namespace TestSuite
{
   [Collection("QLNet CI Tests")]
   public class T_OvernightIndexedCoupon
   {
      private class CommonVars
      {
         // cleanup
         //SavedSettings backup;
         //IndexHistoryCleaner cleaner;

         public Date today;
         public double notional = 10000.0;
         public OvernightIndex sofr;
         public RelinkableHandle<YieldTermStructure> forecastCurve = new RelinkableHandle<YieldTermStructure>();

         public OvernightIndexedCoupon makeCoupon(Date startDate, Date endDate)
         {
            return new OvernightIndexedCoupon(endDate, notional, startDate, endDate, sofr);
         }

         public CommonVars()
         {
            today = new Date(23, Month.November, 2021);

            Settings.setEvaluationDate(today);

            sofr = new Sofr(forecastCurve);

            Date[] pastDates =
            {
               new Date(18, Month.October, 2021), new Date(19, Month.October, 2021), new Date(20, Month.October, 2021),
               new Date(21, Month.October, 2021), new Date(22, Month.October, 2021), new Date(25, Month.October, 2021),
               new Date(26, Month.October, 2021), new Date(27, Month.October, 2021), new Date(28, Month.October, 2021),
               new Date(29, Month.October, 2021), new Date(1, Month.November, 2021), new Date(2, Month.November, 2021),
               new Date(3, Month.November, 2021), new Date(4, Month.November, 2021), new Date(5, Month.November, 2021),
               new Date(8, Month.November, 2021), new Date(9, Month.November, 2021), new Date(10, Month.November, 2021),
               new Date(12, Month.November, 2021), new Date(15, Month.November, 2021),
               new Date(16, Month.November, 2021),
               new Date(17, Month.November, 2021), new Date(18, Month.November, 2021),
               new Date(19, Month.November, 2021),
               new Date(22, Month.November, 2021)
            };

            double[] pastRates =
            {
               0.0008, 0.0009, 0.0008,
               0.0010, 0.0012, 0.0011,
               0.0013, 0.0012, 0.0012,
               0.0008, 0.0009, 0.0010,
               0.0011, 0.0014, 0.0013,
               0.0011, 0.0009, 0.0008,
               0.0007, 0.0008, 0.0008,
               0.0007, 0.0009, 0.0010,
               0.0009
            };

            sofr.addFixings(pastDates.ToList(), pastRates.ToList());
         }
      }
      private void CHECK_OIS_COUPON_RESULT(string what, double calculated, double expected, double tolerance)
      {
         if (Math.Abs(calculated - expected) > tolerance)
         {
            QAssert.Fail("Failed to reproduce " + what + ":"
                         + "\n    expected:   " + expected
                         + "\n    calculated: " + calculated
                         + "\n    error:      " + Math.Abs(calculated - expected));
         }
      }

      [Fact]
      public void testPastCouponRate()
      {
         // Testing rate for past overnight-indexed coupon
         var vars = new CommonVars();

         // coupon entirely in the past
         var pastCoupon = vars.makeCoupon(new Date(18, Month.October, 2021), new Date(18, Month.November, 2021));

         // expected values here and below come from manual calculations based on past dates and rates
         double expectedRate = 0.000987136104;
         double expectedAmount = vars.notional * expectedRate * 31.0 / 360;
         CHECK_OIS_COUPON_RESULT("coupon rate", pastCoupon.rate(), expectedRate, 1e-12);
         CHECK_OIS_COUPON_RESULT("coupon amount", pastCoupon.amount(), expectedAmount, 1e-8);
      }

      [Fact]
      public void testCurrentCouponRate()
      {
         // Testing rate for current overnight-indexed coupon
         var vars = new CommonVars();
         vars.forecastCurve.linkTo(Utilities.flatRate(0.0010, new Actual360()));

         // coupon partly in the past, today not fixed
         var currentCoupon = vars.makeCoupon(new Date(10, Month.November, 2021),new Date(10, Month.December, 2021));

         double expectedRate = 0.000926701551;
         double expectedAmount = vars.notional * expectedRate * 30.0 / 360;
         CHECK_OIS_COUPON_RESULT("coupon rate", currentCoupon.rate(), expectedRate, 1e-12);
         CHECK_OIS_COUPON_RESULT("coupon amount", currentCoupon.amount(), expectedAmount, 1e-8);

         // coupon partly in the past, today fixed
         vars.sofr.addFixing(new Date(23, Month.November, 2021), 0.0007);

         expectedRate = 0.000916700760;
         expectedAmount = vars.notional* expectedRate * 30.0/360;
         CHECK_OIS_COUPON_RESULT("coupon rate", currentCoupon.rate(), expectedRate, 1e-12);
         CHECK_OIS_COUPON_RESULT("coupon amount", currentCoupon.amount(), expectedAmount, 1e-8);
      }

      [Fact]
      public void testFutureCouponRate()
      {
         // Testing rate for future overnight-indexed coupon
         var vars = new CommonVars();
         vars.forecastCurve.linkTo(Utilities.flatRate(0.0010, new Actual360()));

         // coupon entirely in the future
         var futureCoupon = vars.makeCoupon(new Date(10, Month.December, 2021), new Date(10, Month.January, 2022));

         double expectedRate = 0.001000043057;
         double expectedAmount = vars.notional * expectedRate * 31.0 / 360;
         CHECK_OIS_COUPON_RESULT("coupon rate", futureCoupon.rate(), expectedRate, 1e-12);
         CHECK_OIS_COUPON_RESULT("coupon amount", futureCoupon.amount(), expectedAmount, 1e-8);
      }

      [Fact]
      public void testRateWhenTodayIsHoliday()
      {
         // Testing rate for overnight-indexed coupon when today is a holiday
         var vars = new CommonVars();

         Settings.setEvaluationDate(new Date(20, Month.November, 2021));
         vars.forecastCurve.linkTo(Utilities.flatRate(0.0010, new Actual360()));

         var coupon = vars.makeCoupon(new Date(10, Month.November, 2021), new Date(10, Month.December, 2021));

         double expectedRate = 0.000930035180;
         double expectedAmount = vars.notional * expectedRate * 30.0 / 360;
         CHECK_OIS_COUPON_RESULT("coupon rate", coupon.rate(), expectedRate, 1e-12);
         CHECK_OIS_COUPON_RESULT("coupon amount", coupon.amount(), expectedAmount, 1e-8);
      }

      [Fact]
      public void testAccruedAmountInThePast()
      {
         // Testing accrued amount in the past for overnight-indexed coupon
         var vars = new CommonVars();

         var coupon = vars.makeCoupon(new Date(18, Month.October, 2021), new Date(18, Month.January, 2022));

         double expectedAmount = vars.notional * 0.000987136104 * 31.0 / 360;
         CHECK_OIS_COUPON_RESULT("coupon amount", coupon.accruedAmount(new Date(18, Month.November, 2021)), expectedAmount, 1e-8);
      }

      [Fact]
      public void testAccruedAmountSpanningToday()
      {
         // Testing accrued amount spanning today for current overnight-indexed coupon
         var vars = new CommonVars();

         vars.forecastCurve.linkTo(Utilities.flatRate(0.0010, new Actual360()));

         // coupon partly in the past, today not fixed

         var coupon = vars.makeCoupon(new Date(10, Month.November, 2021), new Date(10, Month.January, 2022));

         var expectedAmount = vars.notional * 0.000926701551 * 30.0 / 360;
         CHECK_OIS_COUPON_RESULT("coupon amount", coupon.accruedAmount( new Date(10, Month.December, 2021)), expectedAmount, 1e-8);

         // coupon partly in the past, today fixed
         vars.sofr.addFixing(new Date(23, Month.November, 2021), 0.0007);

         expectedAmount = vars.notional* 0.000916700760 * 30.0/360;
         CHECK_OIS_COUPON_RESULT("coupon amount", coupon.accruedAmount(new Date(10, Month.December, 2021)), expectedAmount, 1e-8);
      }

      [Fact]
      public void testAccruedAmountInTheFuture()
      {
         // Testing accrued amount in the future for overnight-indexed coupon
         var vars = new CommonVars();

         vars.forecastCurve.linkTo(Utilities.flatRate(0.0010, new Actual360()));

         // coupon entirely in the future

         var coupon = vars.makeCoupon(new Date(10, Month.December, 2021), new Date(10, Month.March, 2022));

         var accrualDate = new Date(10, Month.January, 2022);
         var expectedRate = 0.001000043057;
         var expectedAmount = vars.notional * expectedRate * 31.0 / 360;
         CHECK_OIS_COUPON_RESULT("coupon amount", coupon.accruedAmount(accrualDate), expectedAmount, 1e-8);
      }

      [Fact]
      public void testAccruedAmountOnPastHoliday()
      {
         // Testing accrued amount on a past holiday for overnight-indexed coupon
         var vars = new CommonVars();

         // coupon entirely in the past
         var coupon = vars.makeCoupon(new Date(18, Month.October, 2021),
            new Date(18, Month.January, 2022));

         var accrualDate = new Date(13, Month.November, 2021);
         var expectedAmount = vars.notional * 0.000074724810;
         CHECK_OIS_COUPON_RESULT("coupon amount", coupon.accruedAmount(accrualDate), expectedAmount, 1e-8);
      }

      [Fact]
      public void testAccruedAmountOnFutureHoliday()
      {
         // Testing accrued amount on a future holiday for overnight-indexed coupon
         var vars = new CommonVars();

         vars.forecastCurve.linkTo(Utilities.flatRate(0.0010, new Actual360()));

         // coupon entirely in the future

         var coupon = vars.makeCoupon(new Date(10, Month.December, 2021),
            new Date(10, Month.March, 2022));

         var accrualDate = new Date(15, Month.January, 2022);
         var expectedAmount = vars.notional * 0.000100005012;
         CHECK_OIS_COUPON_RESULT("coupon amount", coupon.accruedAmount(accrualDate), expectedAmount, 1e-8);
      }
   }
}
