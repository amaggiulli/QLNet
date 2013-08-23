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
    //! Black volatility curve modelled as variance curve
    /*! This class calculates time-dependent Black volatilities using
        as input a vector of (ATM) Black volatilities observed in the
        market.

        The calculation is performed interpolating on the variance curve.
        Linear interpolation is used as default; this can be changed
        by the setInterpolation() method.

        For strike dependence, see BlackVarianceSurface.

        \todo check time extrapolation

    */
    public class BlackVarianceCurve : BlackVarianceTermStructure {
        DayCounter dayCounter_;
        public override DayCounter dayCounter() { return dayCounter_; }

        Date maxDate_;
        List<double> times_;
        List<double> variances_;
        Interpolation varianceCurve_;

        // required for Handle
        public BlackVarianceCurve() { }

        //public BlackVarianceCurve(Date referenceDate, List<Date> dates, List<double> blackVolCurve, DayCounter dayCounter,
        //                          bool forceMonotoneVariance = true);
        public BlackVarianceCurve(Date referenceDate, List<Date> dates, List<double> blackVolCurve, DayCounter dayCounter,
                                  bool forceMonotoneVariance)
            : base(referenceDate) {

            dayCounter_ = dayCounter;
            maxDate_ = dates.Last();

            if (!(dates.Count == blackVolCurve.Count))
                throw new ApplicationException("mismatch between date vector and black vol vector");

            // cannot have dates[0]==referenceDate, since the
            // value of the vol at dates[0] would be lost
            // (variance at referenceDate must be zero)
            if (!(dates[0]>referenceDate))
                throw new ApplicationException("cannot have dates[0] <= referenceDate");

            variances_ = new InitializedList<double>(dates.Count+1);
            times_ = new InitializedList<double>(dates.Count + 1);
            variances_[0] = 0.0;
            times_[0] = 0.0;
            for (int j = 1; j <= blackVolCurve.Count; j++) {
                times_[j] = timeFromReference(dates[j-1]);

                if (!(times_[j]>times_[j-1]))
                    throw new ApplicationException("dates must be sorted unique!");
                variances_[j] = times_[j] * blackVolCurve[j-1]*blackVolCurve[j-1];
                if (!(variances_[j]>=variances_[j-1] || !forceMonotoneVariance))
                    throw new ApplicationException("variance must be non-decreasing");
            }

            // default: linear interpolation
            setInterpolation<Linear>();
        }

        protected override double blackVarianceImpl(double t, double x) {
            if (t<=times_.Last()) {
                return varianceCurve_.value(t, true);
            } else {
                // extrapolate with flat vol
                return varianceCurve_.value(times_.Last(), true) * t / times_.Last();
            }
        }

        public void setInterpolation<Interpolator>() where Interpolator : IInterpolationFactory, new() {
            setInterpolation<Interpolator>(new Interpolator());
        }
        public void setInterpolation<Interpolator>(Interpolator i) where Interpolator : IInterpolationFactory, new() {
            varianceCurve_ = i.interpolate(times_, times_.Count, variances_);
            varianceCurve_.update();
            notifyObservers();
        }

        public override Date maxDate() { return maxDate_; }
        public override double minStrike() { return double.MinValue; }
        public override double maxStrike() { return double.MaxValue; }
    }
}
