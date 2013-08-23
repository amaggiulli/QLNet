/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008 Andrea Maggiulli

 * This file is part of QLNet Project http://qlnet.sourceforge.net/

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

namespace QLNet {
    public static partial class Utils {
        /*! Follows somewhat the advice of Knuth on checking for floating-point
            equality. The closeness relationship is:
            \f[
            \mathrm{close}(x,y,n) \equiv |x-y| \leq \varepsilon |x|
                                  \wedge |x-y| \leq \varepsilon |y|
            \f]
            where \f$ \varepsilon \f$ is \f$ n \f$ times the machine accuracy;
            \f$ n \f$ equals 42 if not given.  */
        public static bool close(double x, double y) { return close(x, y, 42); }
        public static bool close(double x, double y, int n) {
            double diff = System.Math.Abs(x - y), tolerance = n * Const.QL_Epsilon;
            return diff <= tolerance * System.Math.Abs(x) && diff <= tolerance * System.Math.Abs(y);
        }

        public static bool close(Money m1, Money m2) {
            return close(m1, m2, 42);
        }

        public static bool close_enough(double x, double y) {
            return close_enough(x,y,42);
        }

        public static bool close_enough(double x, double y, int n) {
            double diff = Math.Abs(x-y), tolerance = n*Const.QL_Epsilon;
            return diff <= tolerance*Math.Abs(x) || diff <= tolerance*Math.Abs(y);
        }

        public static bool close(Money m1, Money m2, int n) {
            if (m1.currency == m2.currency) {
                return close(m1.value, m2.value, n);
            } else if (Money.conversionType == Money.ConversionType.BaseCurrencyConversion) {
                Money tmp1 = m1;
                Money.convertToBase(ref tmp1);
                Money tmp2 = m2;
                Money.convertToBase(ref tmp2);
                return close(tmp1, tmp2, n);
            } else if (Money.conversionType == Money.ConversionType.AutomatedConversion) {
                Money tmp = m2;
                Money.convertTo(ref tmp, m1.currency);
                return close(m1, tmp, n);
            } else {
                throw new Exception("currency mismatch and no conversion specified");
            }
        }
    }
}
