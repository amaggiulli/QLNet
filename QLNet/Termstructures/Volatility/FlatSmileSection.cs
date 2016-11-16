/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)
  
 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

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

namespace QLNet {
    public class FlatSmileSection : SmileSection {
        private double vol_;
        private double? atmLevel_;
        public override double? atmLevel() 
        {
           //Utils.QL_REQUIRE( atmLevel_.HasValue, () => "FlatSmileSection.atmLevel is null" );
           return atmLevel_;
        }

        public FlatSmileSection(Date d, double vol, DayCounter dc, Date referenceDate = null, double? atmLevel = null)
            : base(d, dc, referenceDate) {
            vol_ = vol;
            atmLevel_ = atmLevel;
        }

        public FlatSmileSection(double exerciseTime, double vol, DayCounter dc, double? atmLevel = null)
            : base(exerciseTime, dc) {
            vol_ = vol;
            atmLevel_ = atmLevel;
        }

        public override double minStrike () {
            return double.MinValue;
        }

        public override double maxStrike () {
            return double.MaxValue;
        }

        protected override double volatilityImpl(double d) {
            return vol_;
        }
    }
}
