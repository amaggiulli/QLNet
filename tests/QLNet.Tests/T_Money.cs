/*
 Copyright (C) 2008 Andrea Maggiulli

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
   public class T_Money
   {
#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testNone()
      {
         Currency EUR = new EURCurrency();

         Money m1 = 50000.0 * EUR;
         Money m2 = 100000.0 * EUR;
         Money m3 = 500000.0 * EUR;

         Money.conversionType = Money.ConversionType.NoConversion;

         Money calculated = m1 * 3.0 + 2.5 * m2 - m3 / 5.0;
         double x = m1.value * 3.0 + 2.5 * m2.value - m3.value / 5.0;
         Money expected = new Money(x, EUR);

         if (calculated != expected)
            QAssert.Fail("Wrong result: expected: " + expected + " calculated: " + calculated);
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testBaseCurrency()
      {
         Currency EUR = new EURCurrency(), GBP = new GBPCurrency(), USD = new USDCurrency();

         Money m1 = 50000.0 * GBP;
         Money m2 = 100000.0 * EUR;
         Money m3 = 500000.0 * USD;

         ExchangeRateManager.Instance.clear();
         ExchangeRate eur_usd = new ExchangeRate(EUR, USD, 1.2042);
         ExchangeRate eur_gbp = new ExchangeRate(EUR, GBP, 0.6612);
         ExchangeRateManager.Instance.add(eur_usd);
         ExchangeRateManager.Instance.add(eur_gbp);

         Money.conversionType = Money.ConversionType.BaseCurrencyConversion;
         Money.baseCurrency = EUR;

         Money calculated = m1 * 3.0 + 2.5 * m2 - m3 / 5.0;

         Rounding round = Money.baseCurrency.rounding;
         double x = round.Round(m1.value * 3.0 / eur_gbp.rate) + 2.5 * m2.value
                    - round.Round(m3.value / (5.0 * eur_usd.rate));
         Money expected = new Money(x, EUR);

         Money.conversionType = Money.ConversionType.NoConversion;

         if (calculated != expected)
         {
            QAssert.Fail("Wrong result: expected: " + expected + "calculated: " + calculated);
         }
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testAutomated()
      {
         Currency EUR = new EURCurrency(), GBP = new GBPCurrency(), USD = new USDCurrency();

         Money m1 = 50000.0 * GBP;
         Money m2 = 100000.0 * EUR;
         Money m3 = 500000.0 * USD;

         ExchangeRateManager.Instance.clear();
         ExchangeRate eur_usd = new ExchangeRate(EUR, USD, 1.2042);
         ExchangeRate eur_gbp = new ExchangeRate(EUR, GBP, 0.6612);
         ExchangeRateManager.Instance.add(eur_usd);
         ExchangeRateManager.Instance.add(eur_gbp);

         Money.conversionType = Money.ConversionType.AutomatedConversion;

         Money calculated = (m1 * 3.0 + 2.5 * m2) - m3 / 5.0;

         Rounding round = m1.currency.rounding;
         double x = m1.value * 3.0 + round.Round(2.5 * m2.value * eur_gbp.rate)
                    - round.Round((m3.value / 5.0) * eur_gbp.rate / eur_usd.rate);
         Money expected = new Money(x, GBP);

         Money.conversionType = Money.ConversionType.NoConversion;

         if (calculated != expected)
         {
            QAssert.Fail("Wrong result: " + "expected: " + expected + " calculated: " + calculated);
         }
      }

   }
}

