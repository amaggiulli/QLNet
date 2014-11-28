/*
 Copyright (C) 2008, 2009 Siarhei Novik (snovik@gmail.com)
  
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
    public class InterpolatedForwardCurve<Interpolator> : ForwardRateStructure, InterpolatedCurve
        where Interpolator : IInterpolationFactory, new() {

        #region InterpolatedCurve
        public List<double> times_ { get; set; }
        public List<double> times() { return this.times_; }

        public List<Date> dates_ { get; set; }
        public List<Date> dates() { return dates_; }
        public override Date maxDate() { return dates_.Last(); }

        public List<double> data_ { get; set; }
        public List<double> forwards() { return this.data_; }
        public List<double> data() { return forwards(); }

        public Interpolation interpolation_ { get; set; }
        public IInterpolationFactory interpolator_ { get; set; }

        public Dictionary<Date, double> nodes() {
            Dictionary<Date, double> results = new Dictionary<Date, double>();
            dates_.ForEach((i, x) => results.Add(x, data_[i]));
            return results;
        }

        public void setupInterpolation() {
            interpolation_ = interpolator_.interpolate(times_, times_.Count, data_);
        }

        public object Clone() {
            InterpolatedCurve copy = this.MemberwiseClone() as InterpolatedCurve;
            copy.times_ = new List<double>(times_);
            copy.data_ = new List<double>(data_);
            copy.interpolator_ = interpolator_;
            copy.setupInterpolation();
            return copy;
        }
        #endregion


        public InterpolatedForwardCurve(List<Date> dates, List<double> yields, DayCounter dayCounter, List<Handle<Quote>> jumps = null, List<Date> jumpDates = null , Interpolator interpolator = default(Interpolator))
            : base(dates.First(), new Calendar(), dayCounter,jumps,jumpDates) {

            times_ = new List<double>();
            dates_ = dates;
            data_ = yields;
            interpolator_ = interpolator;

            if (!(dates_.Count > 1)) throw new ApplicationException("too few dates");
            if (data_.Count != dates_.Count) throw new ApplicationException("dates/yields count mismatch");

            times_ = new InitializedList<double>(dates_.Count);
            times_[0] = 0.0;
            for (int i = 1; i < dates_.Count; i++) {
                if (!(dates_[i] > dates_[i - 1]))
                    throw new ApplicationException("invalid date (" + dates_[i] + ", vs " + dates_[i - 1] + ")");

                #if !QL_NEGATIVE_RATES
                Utils.QL_REQUIRE( this.data_[i] >= 0.0, () => "negative forward" );
                #endif

                times_[i] = dayCounter.yearFraction(dates_[0], dates_[i]);

                if (Utils.close(times_[i], times_[i - 1]))
                    throw new ApplicationException("two dates correspond to the same time " +
                                                   "under this curve's day count convention");
            }

            setupInterpolation();
            interpolation_.update();
        }


        protected override double forwardImpl(double t) { return interpolation_.value(t, true); }
        protected override double zeroYieldImpl(double t) {
            if (t == 0.0)
                return forwardImpl(0.0);
            else
                return interpolation_.primitive(t, true)/t;
        }
    }
}
