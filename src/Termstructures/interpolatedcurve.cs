/*
 Copyright (C) 2009 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2016  Andrea Maggiulli (a.maggiulli@gmail.com)
  
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
using System;
using System.Collections.Generic;

namespace QLNet {
    //! Helper class to build interpolated term structures
    /*! Interpolated term structures can use protected or private
        inheritance from this class to obtain the relevant data
        members and implement correct copy behavior.
    */
    public interface InterpolatedCurve : ICloneable {
        List<double> times_ { get; set; }
        List<double> times();

        List<Date> dates_ { get; set; }
        List<Date> dates();
        Date maxDate();

        List<double> data_ { get; set; }
        List<double> data();

        Dictionary<Date, double> nodes();

        Interpolation interpolation_ { get; set; }
        IInterpolationFactory interpolator_ { get; set; }
        void setupInterpolation();
        // Usually, the maximum date is the one corresponding to the
        // last node. However, it might happen that a bit of
        // extrapolation is used by construction; for instance, when a
        // curve is bootstrapped and the last relevant date for an
        // instrument is after the corresponding pillar.
        // We provide here a slot to store this information, so that
        // it's available to all derived classes (we should have
        // probably done the same with the dates_ vector, but moving
        // it here might not be entirely backwards-compatible).
        Date maxDate_{ get; set; }

    }
}
