/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)

 * This file is part of QLNet Project https://github.com/amaggiulli/qlnet

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

namespace QLNet
{
   public static partial class Utils
   {
      // Follows somewhat the advice of Knuth on checking for floating-point equality.
      public static bool close(double x, double y) { return close(x, y, 42); }

      public static bool close(double x, double y, int n)
      {
         if (x.IsEqual(y))
            return true;

         double diff = Math.Abs(x - y), tolerance = n * Const.QL_EPSILON;

         if ((x * y).IsEqual(0.0)) // x or y = 0.0
            return diff < (tolerance * tolerance);

         return diff <= tolerance * Math.Abs(x) &&
               diff <= tolerance * Math.Abs(y);
      }

      public static bool close(Money m1, Money m2)
      {
         return close(m1, m2, 42);
      }

      public static bool close_enough(double x, double y)
      {
         return close_enough(x, y, 42);
      }

      public static bool close_enough(double x, double y, int n)
      {
         // Deals with +infinity and -infinity representations etc.
         if (x.IsEqual(y))
            return true;

         double diff = Math.Abs(x - y), tolerance = n * Const.QL_EPSILON;

         if ((x * y).IsEqual(0.0)) // x or y = 0.0
            return diff < (tolerance * tolerance);

         return diff <= tolerance * Math.Abs(x) ||
               diff <= tolerance * Math.Abs(y);
      }

      public static bool close(Money m1, Money m2, int n)
      {
         if (m1.currency == m2.currency)
         {
            return close(m1.value, m2.value, n);
         }
         if (Money.conversionType == Money.ConversionType.BaseCurrencyConversion)
         {
            Money tmp1 = m1;
            Money.convertToBase(ref tmp1);
            Money tmp2 = m2;
            Money.convertToBase(ref tmp2);
            return close(tmp1, tmp2, n);
         }
         if (Money.conversionType == Money.ConversionType.AutomatedConversion)
         {
            Money tmp = m2;
            Money.convertTo(ref tmp, m1.currency);
            return close(m1, tmp, n);
         }
         Utils.QL_FAIL("currency mismatch and no conversion specified");
         return false;
      }
   }
}
