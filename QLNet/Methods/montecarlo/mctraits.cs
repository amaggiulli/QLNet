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
    // path generation and pricing traits

    //! default Monte Carlo traits for single-variate models
    //template <class RNG = PseudoRandom>
    public struct SingleVariate {
        //typedef RNG rng_traits;
        //typedef Path path_type;
        //typedef PathPricer<path_type> path_pricer_type;
        //typedef typename RNG::rsg_type rsg_type;
        //typedef PathGenerator<rsg_type> path_generator_type;
        //enum { allowsErrorEstimate = RNG::allowsErrorEstimate };
    };

    //! default Monte Carlo traits for multi-variate models
    //template <class RNG = PseudoRandom>
    public struct MultiVariate {
        //typedef RNG rng_traits;
        //typedef MultiPath path_type;
        //typedef PathPricer<path_type> path_pricer_type;
        //typedef typename RNG::rsg_type rsg_type;
        //typedef MultiPathGenerator<rsg_type> path_generator_type;
        //enum { allowsErrorEstimate = RNG::allowsErrorEstimate };
    };
}
