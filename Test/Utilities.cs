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
using QLNet;

namespace TestSuite {
    public class Flag : IObserver {
        private bool up_;

        public Flag() {
            up_ = false;
        }

        public void raise() { up_ = true; }
        public void lower() { up_ = false; }
        public bool isUp() { return up_; }
        public void update() { raise(); }
    };

    public static class Utilities {
        public static YieldTermStructure flatRate(Date today, double forward, DayCounter dc) {
            return new FlatForward(today, new SimpleQuote(forward), dc);
        }

        public static YieldTermStructure flatRate(Date today, Quote forward, DayCounter dc) {
            return new FlatForward(today, forward, dc);
        }

        //philippe2009_17
        public static YieldTermStructure flatRate(double forward, DayCounter dc)
        {
            return flatRate(new SimpleQuote(forward), dc);
        }

        public static YieldTermStructure flatRate(Quote forward, DayCounter dc)
        {
            return new FlatForward(0, new NullCalendar(), forward, dc);
        }

        public static BlackVolTermStructure flatVol(Date today, double vol, DayCounter dc) {
            return flatVol(today, new SimpleQuote(vol), dc);
        }

        public static BlackVolTermStructure flatVol(Date today, Quote vol, DayCounter dc) {
            return new BlackConstantVol(today, new NullCalendar(), new Handle<Quote>(vol), dc);
        }
        //philippe2009_17
        public static BlackVolTermStructure flatVol(Quote vol, DayCounter dc)
        {
            return new BlackConstantVol(0, new NullCalendar(), new Handle<Quote>(vol), dc);
        }

        public static BlackVolTermStructure flatVol(double vol, DayCounter dc)
        {
            return flatVol(new SimpleQuote(vol), dc);
        }

        public static double norm(Vector v, int size, double h) {
            // squared values
            List<double> f2 = new InitializedList<double>(size);

            for (int i = 0; i < v.Count; i++)
                f2[i] = v[i] * v[i];

            // numeric integral of f^2
            double I = h * (f2.Sum() - 0.5 * f2.First() - 0.5 * f2.Last());
            return Math.Sqrt(I);
        }

        public static double relativeError(double x1, double x2, double reference) {
            if (reference != 0.0)
                return Math.Abs(x1 - x2) / reference;
            else
                // fall back to absolute error
                return Math.Abs(x1 - x2);
        }
    }

    // this cleans up index-fixing histories when destroyed
    public class IndexHistoryCleaner {
        ~IndexHistoryCleaner() { IndexManager.instance().clearHistories(); }
    };
}
