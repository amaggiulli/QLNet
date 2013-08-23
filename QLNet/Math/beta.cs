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
    public partial class Utils {
        public static double betaFunction(double z, double w) {
            return Math.Exp(GammaFunction.logValue(z) +
                            GammaFunction.logValue(w) -
                            GammaFunction.logValue(z+w));
        }

        public static double betaContinuedFraction(double a, double b, double x) {
            return betaContinuedFraction(a, b, x, 1e-16, 100);
        }
        public static double betaContinuedFraction(double a, double b, double x, double accuracy, int maxIteration) {
            double aa, del;
            double qab = a+b;
            double qap = a+1.0;
            double qam = a-1.0;
            double c = 1.0;
            double d = 1.0-qab*x/qap;
            if (Math.Abs(d) < Const.QL_Epsilon)
                d = Const.QL_Epsilon;
            d = 1.0/d;
            double result = d;

            int m, m2;
            for (m=1; m<=maxIteration; m++) {
                m2=2*m;
                aa=m*(b-m)*x/((qam+m2)*(a+m2));
                d=1.0+aa*d;
                if (Math.Abs(d) < Const.QL_Epsilon) d=Const.QL_Epsilon;
                c=1.0+aa/c;
                if (Math.Abs(c) < Const.QL_Epsilon) c=Const.QL_Epsilon;
                d=1.0/d;
                result *= d*c;
                aa = -(a+m)*(qab+m)*x/((a+m2)*(qap+m2));
                d=1.0+aa*d;
                if (Math.Abs(d) < Const.QL_Epsilon) d=Const.QL_Epsilon;
                c=1.0+aa/c;
                if (Math.Abs(c) < Const.QL_Epsilon) c=Const.QL_Epsilon;
                d=1.0/d;
                del=d*c;
                result *= del;
                if (Math.Abs(del-1.0) < accuracy)
                    return result;
            }
            throw new ApplicationException("a or b too big, or maxIteration too small in betacf");
        }

        /*! Incomplete Beta function

            The implementation of the algorithm was inspired by
            "Numerical Recipes in C", 2nd edition,
            Press, Teukolsky, Vetterling, Flannery, chapter 6
        */
        public static double incompleteBetaFunction(double a, double b, double x) {
            return incompleteBetaFunction(a, b, x, 1e-16, 100);
        }
        public static double incompleteBetaFunction(double a, double b, double x, double accuracy, int maxIteration) {

            if (!(a > 0.0)) throw new ApplicationException("a must be greater than zero");
            if (!(b > 0.0)) throw new ApplicationException("b must be greater than zero");


            if (x == 0.0)
                return 0.0;
            else if (x == 1.0)
                return 1.0;
            else
                if (!(x>0.0 && x<1.0)) throw new ApplicationException("x must be in [0,1]");

            double result = Math.Exp(GammaFunction.logValue(a+b) -
                GammaFunction.logValue(a) - GammaFunction.logValue(b) +
                a*Math.Log(x) + b*Math.Log(1.0-x));

            if (x < (a+1.0)/(a+b+2.0))
                return result *
                    betaContinuedFraction(a, b, x, accuracy, maxIteration)/a;
            else
                return 1.0 - result *
                    betaContinuedFraction(b, a, 1.0-x, accuracy, maxIteration)/b;
        }
    }
}
