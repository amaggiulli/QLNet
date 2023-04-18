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
//  FOR A PARTICULAR PURPOSE.  See the license for more details.
using System;
using System.Collections.Generic;
using QLNet;
using Xunit;

namespace TestSuite
{
   [Collection("QLNet CI Tests")]
   public class T_BasisSwapRateHelpers
   {
      private struct BasisSwapQuote
      {
         public int n { get; set; }
         public TimeUnit units { get; set; }
         public double basis { get; set; }
      }

      private void testIborIborBootstrap(bool bootstrapBaseCurve)
      {
         BasisSwapQuote[] quotes = {
            new BasisSwapQuote{ n = 1, units = TimeUnit.Years,  basis = 0.0010 },
            new BasisSwapQuote{ n = 2, units = TimeUnit.Years,  basis = 0.0012 },
            new BasisSwapQuote{ n = 3, units = TimeUnit.Years,  basis = 0.0015 },
            new BasisSwapQuote{ n = 5, units = TimeUnit.Years,  basis = 0.0015 },
            new BasisSwapQuote{ n = 8, units = TimeUnit.Years,  basis = 0.0018 },
            new BasisSwapQuote{ n = 10, units = TimeUnit.Years, basis = 0.0020 },
            new BasisSwapQuote{ n = 15, units = TimeUnit.Years, basis = 0.0021 },
            new BasisSwapQuote{ n = 20, units = TimeUnit.Years, basis = 0.0021 },
        };

         var settlementDays = 2;
         var calendar = new UnitedStates(UnitedStates.Market.GovernmentBond);
         var convention = BusinessDayConvention.Following;
         var endOfMonth = false;

         var knownForecastCurve = new Handle<YieldTermStructure>(Utilities.flatRate(0.01, new Actual365Fixed()));
         var discountCurve = new Handle<YieldTermStructure>(Utilities.flatRate(0.005, new Actual365Fixed()));
         IborIndex baseIndex, otherIndex;

         if (bootstrapBaseCurve)
         {
            baseIndex = new USDLibor(new Period(3 , TimeUnit.Months));
            otherIndex = new USDLibor(new Period(6, TimeUnit.Months), knownForecastCurve);
         }
         else
         {
            baseIndex = new USDLibor(new Period(3, TimeUnit.Months), knownForecastCurve);
            otherIndex = new USDLibor(new Period(6, TimeUnit.Months));
         }

         var helpers = new List<RateHelper>();

         foreach (var q in quotes)
         {
            var h = new IborIborBasisSwapRateHelper(
                new Handle<Quote>(new SimpleQuote(q.basis)),
                new Period(q.n, q.units), settlementDays, calendar, convention, endOfMonth,
                baseIndex, otherIndex, discountCurve, bootstrapBaseCurve);
            helpers.Add(h);
         }

         var bootstrappedCurve = new PiecewiseYieldCurve<ZeroYield, Linear> (0, calendar, helpers, new Actual365Fixed());

         var today = Settings.evaluationDate();
         var spot = calendar.advance(today, settlementDays, TimeUnit.Days);

         if (bootstrapBaseCurve)
         {
            baseIndex = new USDLibor(new Period(3 , TimeUnit.Months), new Handle<YieldTermStructure>(bootstrappedCurve));
            otherIndex = new USDLibor(new Period(6, TimeUnit.Months), knownForecastCurve);
         }
         else
         {
            baseIndex = new USDLibor(new Period(3, TimeUnit.Months), knownForecastCurve);
            otherIndex = new USDLibor(new Period(6, TimeUnit.Months), new Handle<YieldTermStructure>(bootstrappedCurve));
         }

         foreach (var q in quotes)
         {
            // create swaps and check they're fair
            var maturity = calendar.advance(spot, q.n, q.units, convention);

            Schedule s1 = 
                new MakeSchedule()
                .from(spot).to(maturity)
                .withTenor(baseIndex.tenor())
                .withCalendar(calendar)
                .withConvention(convention)
                .withRule(DateGeneration.Rule.Forward).value();
            var leg1 = new IborLeg(s1, baseIndex)
                .withSpreads(q.basis)
                .withNotionals(100.0);

            Schedule s2 =
                new MakeSchedule()
                .from(spot).to(maturity)
                .withTenor(otherIndex.tenor())
                .withCalendar(calendar)
                .withConvention(convention)
                .withRule(DateGeneration.Rule.Forward).value();
            var leg2 = new IborLeg(s2, otherIndex)
                .withNotionals(100.0);

            var swap = new Swap(leg1, leg2);
            swap.setPricingEngine(new DiscountingSwapEngine(discountCurve));

            double NPV = swap.NPV();
            double tolerance = 1e-8;
            if (Math.Abs(NPV) > tolerance)
            {
               QAssert.Fail("Failed to price fair " + q.n + "-year(s) swap:" + "\n    calculated: " + NPV);
            }
         }

      }

      private void testOvernightIborBootstrap(bool externalDiscountCurve)
      {
         BasisSwapQuote[] quotes = {
            new BasisSwapQuote{ n = 1, units = TimeUnit.Years,  basis= 0.0010 },
            new BasisSwapQuote{ n = 2, units = TimeUnit.Years,  basis= 0.0012 },
            new BasisSwapQuote{ n =3, units = TimeUnit.Years,  basis= 0.0015 },
            new BasisSwapQuote{ n =5, units = TimeUnit.Years,  basis= 0.0015 },
            new BasisSwapQuote{ n =8, units = TimeUnit.Years,  basis= 0.0018 },
            new BasisSwapQuote{ n =10, units = TimeUnit.Years, basis= 0.0020 },
            new BasisSwapQuote{ n =15, units = TimeUnit.Years, basis= 0.0021 },
            new BasisSwapQuote{ n =20, units = TimeUnit.Years, basis= 0.0021 }
        };

         var settlementDays = 2;
         var calendar = new UnitedStates(UnitedStates.Market.GovernmentBond);
         var convention = BusinessDayConvention.Following;
         var endOfMonth = false;

         Handle<YieldTermStructure> knownForecastCurve = new Handle<YieldTermStructure>(Utilities.flatRate(0.01, new Actual365Fixed()));
         RelinkableHandle<YieldTermStructure> discountCurve = new RelinkableHandle<YieldTermStructure>();
         if (externalDiscountCurve)
            discountCurve.linkTo(Utilities.flatRate(0.005, new Actual365Fixed()));

         var baseIndex = new Sofr(knownForecastCurve);
         var otherIndex = new USDLibor(new Period(6 , TimeUnit.Months));

         var helpers = new List<RateHelper>();
         foreach (var q in quotes)
         {
            var h = new OvernightIborBasisSwapRateHelper(new Handle<Quote>(new SimpleQuote(q.basis)),
                new Period(q.n, q.units), settlementDays, calendar, convention, endOfMonth,
                baseIndex, otherIndex, discountCurve);
            helpers.Add(h);
         }

         var bootstrappedCurve = new PiecewiseYieldCurve<ZeroYield, Linear>(0, calendar, helpers, new Actual365Fixed());

         Date today = Settings.evaluationDate();
         Date spot = calendar.advance(today, settlementDays, TimeUnit.Days);

         otherIndex = new USDLibor(new Period(6 , TimeUnit.Months), new Handle<YieldTermStructure>(bootstrappedCurve));

         foreach (var q in quotes)
         {
            // create swaps and check they're fair
            Date maturity = calendar.advance(spot, q.n, q.units, convention);

            Schedule s =
                new MakeSchedule()
                .from(spot).to(maturity)
                .withTenor(otherIndex.tenor())
                .withCalendar(calendar)
                .withConvention(convention)
                .withRule(DateGeneration.Rule.Forward).value();

            var leg1 = new OvernightLeg(s, baseIndex)
                .withSpreads(q.basis)
                .withNotionals(100.0);
            var leg2 = new IborLeg(s, otherIndex)
                .withNotionals(100.0);

            var swap = new Swap(leg1, leg2);
            if (externalDiscountCurve)
            {
               swap.setPricingEngine(new DiscountingSwapEngine(discountCurve));
            }
            else
            {
               swap.setPricingEngine(new DiscountingSwapEngine(new Handle<YieldTermStructure>(bootstrappedCurve)));
            }

            double NPV = swap.NPV();
            double tolerance = 1e-8;
            if (Math.Abs(NPV) > tolerance)
            {
               QAssert.Fail("Failed to price fair " + q.n + "-year(s) swap:" + "\n    calculated: " + NPV);
            }
         }
      }

      [Fact]
      public void testIborIborBaseCurveBootstrap()
      {
         // Testing IBOR-IBOR basis-swap rate helpers (base curve bootstrap)
         testIborIborBootstrap(true);
      }

      [Fact]
      public void testIborIborOtherCurveBootstrap()
      {
         // Testing IBOR-IBOR basis-swap rate helpers (other curve bootstrap)
         testIborIborBootstrap(false);
      }

      [Fact]
      public void testOvernightIborBootstrapNoDiscountCurve()
      {
         // Testing overnight-IBOR basis-swap rate helpers
        testOvernightIborBootstrap(false);
      }

      [Fact]
      public void testOvernightIborBootstrapWithDiscountCurve()
      {
         // Testing overnight-IBOR basis-swap rate helpers with external discount curve
         testOvernightIborBootstrap(true);
      }

   }
}
