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
   public class T_SofrFutures
   {
      private struct SofrQuotes
      {
         public Frequency freq { get; set; }
         public Month month { get; set; }
         public int year { get; set; }
         public double price { get; set; }
         public RateAveragingType averagingMethod { get; set; }
      };

      [Fact]
      public void testBootstrap()
      {
         // Testing bootstrap over SOFR futures

         var today = new Date(26, Month.Oct, 2018);
         Settings.setEvaluationDate(today);

         SofrQuotes[] sofrQuotes = {
            new SofrQuotes{ freq = Frequency.Monthly, month = Month.Oct, year = 2018, price = 97.8175, averagingMethod = RateAveragingType.Simple},
            new SofrQuotes{ freq = Frequency.Monthly, month = Month.Nov, year = 2018, price = 97.770, averagingMethod = RateAveragingType.Simple},
            new SofrQuotes{ freq = Frequency.Monthly, month = Month.Dec, year = 2018, price = 97.685, averagingMethod = RateAveragingType.Simple},
            new SofrQuotes{ freq = Frequency.Monthly, month = Month.Jan, year = 2019, price = 97.595, averagingMethod = RateAveragingType.Simple},
            new SofrQuotes{ freq = Frequency.Monthly, month = Month.Feb, year = 2019, price = 97.590, averagingMethod = RateAveragingType.Simple},
            new SofrQuotes{ freq = Frequency.Monthly, month = Month.Mar, year = 2019, price = 97.525, averagingMethod = RateAveragingType.Simple},
            new SofrQuotes{ freq = Frequency.Quarterly, month = Month.Mar, year = 2019, price = 97.440, averagingMethod = RateAveragingType.Compound},
            new SofrQuotes{ freq = Frequency.Quarterly, month = Month.Jun, year = 2019, price = 97.295, averagingMethod = RateAveragingType.Compound},
            new SofrQuotes{ freq = Frequency.Quarterly, month = Month.Sep, year = 2019, price = 97.220, averagingMethod = RateAveragingType.Compound},
            new SofrQuotes{ freq = Frequency.Quarterly, month = Month.Dec, year = 2019, price = 97.170, averagingMethod = RateAveragingType.Compound},
            new SofrQuotes{ freq = Frequency.Quarterly, month = Month.Mar, year = 2020, price = 97.160, averagingMethod = RateAveragingType.Compound},
            new SofrQuotes{ freq = Frequency.Quarterly, month = Month.Jun, year = 2020, price = 97.165, averagingMethod = RateAveragingType.Compound},
            new SofrQuotes{ freq = Frequency.Quarterly, month = Month.Sep, year = 2020, price = 97.175, averagingMethod = RateAveragingType.Compound},
         };

         OvernightIndex index = new Sofr();
         index.addFixing(new Date(1, Month.October, 2018), 0.0222);
         index.addFixing(new Date(2, Month.October, 2018), 0.022);
         index.addFixing(new Date(3, Month.October, 2018), 0.022);
         index.addFixing(new Date(4, Month.October, 2018), 0.0218);
         index.addFixing(new Date(5, Month.October, 2018), 0.0216);
         index.addFixing(new Date(9, Month.October, 2018), 0.0215);
         index.addFixing(new Date(10, Month.October, 2018), 0.0215);
         index.addFixing(new Date(11, Month.October, 2018), 0.0217);
         index.addFixing(new Date(12, Month.October, 2018), 0.0218);
         index.addFixing(new Date(15, Month.October, 2018), 0.0221);
         index.addFixing(new Date(16, Month.October, 2018), 0.0218);
         index.addFixing(new Date(17, Month.October, 2018), 0.0218);
         index.addFixing(new Date(18, Month.October, 2018), 0.0219);
         index.addFixing(new Date(19, Month.October, 2018), 0.0219);
         index.addFixing(new Date(22, Month.October, 2018), 0.0218);
         index.addFixing(new Date(23, Month.October, 2018), 0.0217);
         index.addFixing(new Date(24, Month.October, 2018), 0.0218);
         index.addFixing(new Date(25, Month.October, 2018), 0.0219);

         List<RateHelper> helpers = new List<RateHelper>();
         foreach (var sofrQuote in sofrQuotes) {
            helpers.Add(new SofrFutureRateHelper(sofrQuote.price, sofrQuote.month, sofrQuote.year, sofrQuote.freq));
         }

         var curve = new PiecewiseYieldCurve<Discount, Linear>(today, helpers,new Actual365Fixed());

         // test curve with one of the futures
         OvernightIndex sofr = new Sofr(new Handle<YieldTermStructure>(curve));
         OvernightIndexFuture sf = new OvernightIndexFuture(sofr, new Date(20, Month.March, 2019), new Date(19, Month.June, 2019));

         double expected_price = 97.44;
         double tolerance = 1.0e-9;

         double error = Math.Abs(sf.NPV() - expected_price);
         if (error > tolerance)
         {
            QAssert.Fail("sample futures:\n"
                        + "\n estimated price: " + sf.NPV()
                        + "\n expected price:  " + expected_price
                        + "\n error:           " + error
                        + "\n tolerance:       " + tolerance);
         }
      }
   }
}
