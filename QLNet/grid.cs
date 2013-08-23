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

        public static Vector CenteredGrid(double center, double dx, int steps) {
            Vector result = new Vector(steps+1);
            for (int i=0; i<steps+1; i++)
                result[i] = center + (i - steps/2.0)*dx;
            return result;
        }

        public static Vector BoundedGrid(double xMin, double xMax, int steps) {
            return new Vector(steps + 1, xMin, (xMax - xMin) / steps);
        }

        public static Vector BoundedLogGrid(double xMin, double xMax, int steps) {
            Vector result = new Vector(steps+1);
            double gridLogSpacing = (Math.Log(xMax) - Math.Log(xMin)) / 
                (steps);
            double edx = Math.Exp(gridLogSpacing);
            result[0] = xMin;
            for (int j=1; j < steps+1; j++) {
                result[j] = result[j-1]*edx;
            }
            return result;
        }

    }
}
