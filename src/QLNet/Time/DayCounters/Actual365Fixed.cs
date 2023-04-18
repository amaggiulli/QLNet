/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2022 Andrea Maggiulli (a.maggiulli@gmail.com)

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

namespace QLNet
{
   /* "Actual/365 (Fixed)" day count convention, also know as "Act/365 (Fixed)", "A/365 (Fixed)", or "A/365F".
     According to ISDA, "Actual/365" (without "Fixed") is an alias for "Actual/Actual (ISDA)" (see ActualActual.)
    * If Actual/365 is not explicitly specified as fixed in an instrument specification,
    * you might want to double-check its meaning.   */
   public class Actual365Fixed : DayCounter
   {
      public enum Convention { Standard, Canadian, NoLeap };
      public Actual365Fixed(Convention c = Convention.Standard) : base(Implementation(c)) { }

     private static DayCounterImpl Implementation(Convention c)
     {
        switch (c) {
            case Convention.Standard:
               return new Impl();
            case Convention.Canadian:
               return new CA_Impl();
            case Convention.NoLeap:
               return new NL_Impl();
            default:
               Utils.QL_FAIL("unknown Actual/365 (Fixed) convention");
               break;
         }
        return null;
     }
      private class Impl : DayCounterImpl
      {
         public override string name() { return "Actual/365 (Fixed)"; }
         public override double yearFraction(Date d1, Date d2, Date refPeriodStart, Date refPeriodEnd)
         {
            return Date.daysBetween(d1, d2) / 365.0;
         }

      }
      private class CA_Impl : DayCounterImpl
      {
         public override string name() { return "Actual/365 (Fixed) Canadian Bond"; }
         public override double yearFraction(Date d1, Date d2, Date refPeriodStart, Date refPeriodEnd)
         {
            if (d1 == d2)
               return 0.0;

            // We need the period to calculate the frequency
            Utils.QL_REQUIRE(refPeriodStart != null, ()=> "invalid refPeriodStart");
            Utils.QL_REQUIRE(refPeriodEnd != null, ()=> "invalid refPeriodEnd");

            var dcs = Date.daysBetween(d1,d2);
            var dcc = Date.daysBetween(refPeriodStart,refPeriodEnd);
            var months = (int)(Math.Round(12 * dcc / 365));
            Utils.QL_REQUIRE(months != 0, ()=> "invalid reference period for Act/365 Canadian; must be longer than a month");
            var frequency = (int)(12 / months);

            if (dcs < (int)(365/frequency))
               return dcs/365.0;

            return 1.0/frequency - (dcc-dcs)/365.0;
         }

      }
      private class NL_Impl : DayCounterImpl
      {
         public override string name() { return "Actual/365 (No Leap)"; }
         public override int dayCount(Date d1, Date d2)
         {
             int[] monthOffset = {
               0,  31,  59,  90, 120, 151,  // Jan - Jun
               181, 212, 243, 273, 304, 334   // Jun - Dec
            };

            var s1 = d1.Day + monthOffset[d1.month()-1] + (d1.year() * 365);
            var s2 = d2.Day + monthOffset[d2.month()-1] + (d2.year() * 365);

            if (d1.month() == (int)Month.Feb && d1.Day == 29)
            {
               --s1;
            }

            if (d2.month() == (int)Month.Feb && d2.Day == 29)
            {
               --s2;
            }

            return s2 - s1;
         }
         public override double yearFraction(Date d1, Date d2, Date refPeriodStart, Date refPeriodEnd)
         {
            return dayCount(d1, d2)/365.0;
         }

      }
   }
}
