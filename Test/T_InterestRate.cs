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
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QLNet;

namespace TestSuite
{
   [TestClass()]
   public class T_InterestRate
   {
      public struct InterestRateData
      {
         public double r;
         public Compounding comp;
         public Frequency freq;
         public double t;
         public Compounding comp2;
         public Frequency freq2;
         public double expected;
         public int precision;

         public InterestRateData(double _r, Compounding _comp, Frequency _freq, double _t, Compounding _comp2, Frequency _freq2, double _expected, int _precision)
         {
            r = _r;
            comp = _comp;
            freq = _freq;
            t = _t;
            comp2 = _comp2;
            freq2 = _freq2;
            expected = _expected;
            precision = _precision;
         }

      };


      [TestMethod()]
      public void testConversions()
      {
         InterestRateData[] cases = {
         // data from "Option Pricing Formulas", Haug, pag.181-182
         // Rate,Compounding,        Frequency,   Time, Compounding2,      Frequency2,  Rate2, precision
         new InterestRateData(0.0800, Compounding.Compounded, Frequency.Quarterly, 1.00, Compounding.Continuous, Frequency.Annual,     0.0792, 4),
         new InterestRateData(0.1200, Compounding.Continuous, Frequency.Annual,    1.00, Compounding.Compounded, Frequency.Annual,     0.1275, 4),
         new InterestRateData(0.0800, Compounding.Compounded, Frequency.Quarterly, 1.00, Compounding.Compounded, Frequency.Annual,     0.0824, 4),
         new InterestRateData(0.0700, Compounding.Compounded, Frequency.Quarterly, 1.00, Compounding.Compounded, Frequency.Semiannual, 0.0706, 4),
         // undocumented, but reasonable :)
         new InterestRateData(0.0100, Compounding.Compounded, Frequency.Annual,            1.00,   Compounding.Simple,     Frequency.Annual,           0.0100, 4),
         new InterestRateData(0.0200, Compounding.Simple,     Frequency.Annual,            1.00,   Compounding.Compounded, Frequency.Annual,           0.0200, 4),
         new InterestRateData(0.0300, Compounding.Compounded, Frequency.Semiannual,        0.50,   Compounding.Simple,     Frequency.Annual,           0.0300, 4),
         new InterestRateData(0.0400, Compounding.Simple,     Frequency.Annual,            0.50,   Compounding.Compounded, Frequency.Semiannual,       0.0400, 4),
         new InterestRateData(0.0500, Compounding.Compounded, Frequency.EveryFourthMonth,  1.0/3,  Compounding.Simple,     Frequency.Annual,           0.0500, 4),
         new InterestRateData(0.0600, Compounding.Simple,     Frequency.Annual,            1.0/3,  Compounding.Compounded, Frequency.EveryFourthMonth, 0.0600, 4),
         new InterestRateData(0.0500, Compounding.Compounded, Frequency.Quarterly,         0.25,   Compounding.Simple,     Frequency.Annual,           0.0500, 4),
         new InterestRateData(0.0600, Compounding.Simple,     Frequency.Annual,            0.25,   Compounding.Compounded, Frequency.Quarterly,        0.0600, 4),
         new InterestRateData(0.0700, Compounding.Compounded, Frequency.Bimonthly,         1.0/6,  Compounding.Simple,     Frequency.Annual,           0.0700, 4),
         new InterestRateData(0.0800, Compounding.Simple,     Frequency.Annual,            1.0/6,  Compounding.Compounded, Frequency.Bimonthly,        0.0800, 4),
         new InterestRateData(0.0900, Compounding.Compounded, Frequency.Monthly,           1.0/12, Compounding.Simple,     Frequency.Annual,           0.0900, 4),
         new InterestRateData(0.1000, Compounding.Simple,     Frequency.Annual,            1.0/12, Compounding.Compounded, Frequency.Monthly,          0.1000, 4),

         new InterestRateData(0.0300, Compounding.SimpleThenCompounded, Frequency.Semiannual, 0.25, Compounding.Simple,     Frequency.Annual,     0.0300, 4),
         new InterestRateData(0.0300, Compounding.SimpleThenCompounded, Frequency.Semiannual, 0.25, Compounding.Simple,     Frequency.Semiannual, 0.0300, 4),
         new InterestRateData(0.0300, Compounding.SimpleThenCompounded, Frequency.Semiannual, 0.25, Compounding.Simple,     Frequency.Quarterly,  0.0300, 4),
         new InterestRateData(0.0300, Compounding.SimpleThenCompounded, Frequency.Semiannual, 0.50, Compounding.Simple,     Frequency.Annual,     0.0300, 4),
         new InterestRateData(0.0300, Compounding.SimpleThenCompounded, Frequency.Semiannual, 0.50, Compounding.Simple,     Frequency.Semiannual, 0.0300, 4),
         new InterestRateData(0.0300, Compounding.SimpleThenCompounded, Frequency.Semiannual, 0.75, Compounding.Compounded, Frequency.Semiannual, 0.0300, 4),

         new InterestRateData(0.0400, Compounding.Simple, Frequency.Semiannual, 0.25, Compounding.SimpleThenCompounded, Frequency.Quarterly,  0.0400, 4),
         new InterestRateData(0.0400, Compounding.Simple, Frequency.Semiannual, 0.25, Compounding.SimpleThenCompounded, Frequency.Semiannual, 0.0400, 4),
         new InterestRateData(0.0400, Compounding.Simple, Frequency.Semiannual, 0.25, Compounding.SimpleThenCompounded, Frequency.Annual,     0.0400, 4),

         new InterestRateData(0.0400, Compounding.Compounded, Frequency.Quarterly,  0.50, Compounding.SimpleThenCompounded, Frequency.Quarterly,  0.0400, 4),
         new InterestRateData(0.0400, Compounding.Simple,     Frequency.Semiannual, 0.50, Compounding.SimpleThenCompounded, Frequency.Semiannual, 0.0400, 4),
         new InterestRateData(0.0400, Compounding.Simple,     Frequency.Semiannual, 0.50, Compounding.SimpleThenCompounded, Frequency.Annual,     0.0400, 4),

         new InterestRateData(0.0400, Compounding.Compounded, Frequency.Quarterly,  0.75, Compounding.SimpleThenCompounded, Frequency.Quarterly,  0.0400, 4),
         new InterestRateData(0.0400, Compounding.Compounded, Frequency.Semiannual, 0.75, Compounding.SimpleThenCompounded, Frequency.Semiannual, 0.0400, 4),
         new InterestRateData(0.0400, Compounding.Simple,     Frequency.Semiannual, 0.75, Compounding.SimpleThenCompounded, Frequency.Annual,     0.0400, 4)
         };

         Rounding roundingPrecision;
         double r3;
         double r2;
         Date d1 = Date.Today;
         Date d2;
         InterestRate ir;
         InterestRate ir2;
         InterestRate ir3;
         InterestRate expectedIR;
         double compoundf;
         double error;
         double disc;

         for (int i = 0; i < cases.Length-1 ; i++)
         {
            ir = new InterestRate(cases[i].r, new Actual360(), cases[i].comp, cases[i].freq);
            d2 = d1 + new Period((int)(360 * cases[i].t + 0.5) ,TimeUnit.Days);
            roundingPrecision = new Rounding(cases[i].precision);

            // check that the compound factor is the inverse of the discount factor
            compoundf = ir.compoundFactor(d1, d2);
            disc = ir.discountFactor(d1, d2);
            error = Math.Abs(disc - 1.0 / compoundf);
            if (error > 1e-15)
               Assert.Fail(ir + "  1.0/compound_factor: " + 1.0 / compoundf);

            // check that the equivalent InterestRate with *same* daycounter,
            // compounding, and frequency is the *same* InterestRate
            //ir2 = ir.equivalentRate(d1, d2, ir.dayCounter(), ir.compounding(), ir.frequency());
            ir2 = ir.equivalentRate(ir.dayCounter(), ir.compounding(), ir.frequency(), d1, d2);
            error = Math.Abs(ir.rate() - ir2.rate());
            if (error > 1e-15)
               Assert.Fail("original interest rate: " + ir + " equivalent interest rate: " + ir2 + " rate error: " + error);
            if (ir.dayCounter() != ir2.dayCounter())
               Assert.Fail("day counter error original interest rate: " + ir + " equivalent interest rate: " + ir2);
            if (ir.compounding() != ir2.compounding())
               Assert.Fail("compounding error original interest rate: " + ir + " equivalent interest rate: " + ir2);
            if (ir.frequency() != ir2.frequency())
               Assert.Fail("frequency error original interest rate: " + ir + " equivalent interest rate: " + ir2);

            // check that the equivalent rate with *same* daycounter,
            // compounding, and frequency is the *same* rate
            //r2 = ir.equivalentRate(d1, d2, ir.dayCounter(), ir.compounding(), ir.frequency()).rate();
            r2 = ir.equivalentRate(ir.dayCounter(), ir.compounding(), ir.frequency(),d1, d2).value();
            error = Math.Abs(ir.rate() - r2);
            if (error > 1e-15)
               Assert.Fail("original rate: " + ir + " equivalent rate: " + r2 + " error: " + error);

            // check that the equivalent InterestRate with *different*
            // compounding, and frequency is the *expected* InterestRate
            //ir3 = ir.equivalentRate(d1, d2, ir.dayCounter(), cases[i].comp2, cases[i].freq2);
            ir3 = ir.equivalentRate(ir.dayCounter(), cases[i].comp2, cases[i].freq2, d1, d2);
            expectedIR = new InterestRate(cases[i].expected, ir.dayCounter(), cases[i].comp2, cases[i].freq2);
            r3 = roundingPrecision.Round(ir3.rate());
            error = Math.Abs(r3 - expectedIR.rate());
            if (error > 1.0e-17)
               Assert.Fail("original interest rate: " + ir + " calculated equivalent interest rate: " + ir3 + " truncated equivalent rate: " + r3 + " expected equivalent interest rate: " + expectedIR + " rate error: " + error);
            if (ir3.dayCounter() != expectedIR.dayCounter())
               Assert.Fail("day counter error original interest rate: " + ir3 + " equivalent interest rate: " + expectedIR);
            if (ir3.compounding() != expectedIR.compounding())
               Assert.Fail("compounding error original interest rate: " + ir3 + " equivalent interest rate: " + expectedIR);
            if (ir3.frequency() != expectedIR.frequency())
               Assert.Fail("frequency error original interest rate: " + ir3 + " equivalent interest rate: " + expectedIR);

            // check that the equivalent rate with *different*
            // compounding, and frequency is the *expected* rate
            //r3 = ir.equivalentRate(d1, d2, ir.dayCounter(), cases[i].comp2, cases[i].freq2).rate();
            r3 = ir.equivalentRate(ir.dayCounter(), cases[i].comp2, cases[i].freq2,  d1, d2).value();
            r3 = roundingPrecision.Round(r3);
            error = Math.Abs(r3 - cases[i].expected);
            if (error > 1.0e-17)
               Assert.Fail("calculated equivalent rate: " + r3 + " expected equivalent rate: " + cases[i].expected + " error: " + error);

         }

      }

   }
}
