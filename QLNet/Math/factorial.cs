/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
  
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
using System.Linq;
using System.Text;

namespace QLNet {
    //! %Factorial numbers calculator
    /*! \test the correctness of the returned value is tested by
              checking it against numerical calculations.
    */
    public static class Factorial {
        public static double get(uint i) {
            if (i<=tabulated) {
                return firstFactorials[i];
            } else {
                return Math.Exp(GammaFunction.logValue(i+1));
            }
        }

        public static double ln(int i) {
            if (i<=tabulated) {
                return Math.Log(firstFactorials[i]);
            } else {
                return GammaFunction.logValue(i+1);
            }
        }

        static double[] firstFactorials = {
                                   1.0,                                   1.0,
                                   2.0,                                   6.0,
                                  24.0,                                 120.0,
                                 720.0,                                5040.0,
                               40320.0,                              362880.0,
                             3628800.0,                            39916800.0,
                           479001600.0,                          6227020800.0,
                         87178291200.0,                       1307674368000.0,
                      20922789888000.0,                     355687428096000.0,
                    6402373705728000.0,                  121645100408832000.0,
                 2432902008176640000.0,                51090942171709440000.0,
              1124000727777607680000.0,             25852016738884976640000.0,
            620448401733239439360000.0,          15511210043330985984000000.0,
         403291461126605635584000000.0,       10888869450418352160768000000.0
        };

        static int tabulated = firstFactorials.Length;
    }
}
