/*
 Copyright (C) 2009 Philippe Real (ph_real@hotmail.com)
  
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

namespace QLNet
{
    public class LmFixedVolatilityModel : LmVolatilityModel
    {

        private Vector volatilities_;
        private List<double> startTimes_;

        public LmFixedVolatilityModel(Vector volatilities,
                                        List<double> startTimes)
            : base(startTimes.Count, 0)
        {
            volatilities_ = volatilities;
            startTimes_ = startTimes;
            if (!(startTimes_.Count > 1))
                throw new ApplicationException("too few dates"); 

            if (!(volatilities_.size() == startTimes_.Count))
                throw new ApplicationException("volatility array and fixing time array have to have the same size"); 

            for (int i = 1; i < startTimes_.Count; i++) {
                if (!(startTimes_[i] > startTimes_[i-1]))
                    throw new ApplicationException( "invalid time ("+startTimes_[i]+", vs "+startTimes_[i-1]+")"); 
            }
        }

        public override Vector volatility(double t){
            return volatility(t, null);
        }

        public override Vector volatility(double t, Vector x)
        {
            if (!(t >= startTimes_.First() && t <= startTimes_.Last()))
                throw new ApplicationException("invalid time given for volatility model"); 

            int ti = startTimes_.GetRange(0,startTimes_.Count -1).BinarySearch(t);
            if (ti < 0)
                // The upper_bound() algorithm finds the last position in a sequence that value can occupy 
                // without violating the sequence's ordering
                // if BinarySearch does not find value the value, the index of the next larger item is returned
                ti = ~ti - 1;

            // impose limits. we need the one before last at max or the first at min
            ti = Math.Max(Math.Min(ti, startTimes_.Count - 2), 0);

            Vector tmp = new Vector(size_, 0.0);

            for (int i = ti; i < size_; ++i)
            {
                tmp[i] = volatilities_[i - ti];
            }

            return tmp;

        }

        public override double volatility(int i, double t, Vector x)
        {
            if (!(t >= startTimes_.First() && t <= startTimes_.Last()))
                throw new ApplicationException("invalid time given for volatility model");

            int ti = startTimes_.GetRange(0, startTimes_.Count - 1).BinarySearch(t);
            if (ti < 0)
                // The upper_bound() algorithm finds the last position in a sequence that value can occupy 
                // without violating the sequence's ordering
                // if BinarySearch does not find value the value, the index of the next larger item is returned
                ti = ~ti - 1;

            // impose limits. we need the one before last at max or the first at min
            ti = Math.Max(Math.Min(ti, startTimes_.Count - 2), 0);

            return volatilities_[i-ti];
        }

        public override double volatility(int i, double t){
            return volatility(i, t, null);
        }

        public override void generateArguments() {
            return;
        }
    }
}
