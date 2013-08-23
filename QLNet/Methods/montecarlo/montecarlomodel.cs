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
    //! General-purpose Monte Carlo model for path samples
    /*! The template arguments of this class correspond to available
        policies for the particular model to be instantiated---i.e.,
        whether it is single- or multi-asset, or whether it should use
        pseudo-random or low-discrepancy numbers for path
        generation. Such decisions are grouped in trait classes so as
        to be orthogonal---see mctraits.hpp for examples.

        The constructor accepts two safe references, i.e. two smart
        pointers, one to a path generator and the other to a path
        pricer.  In case of control variate technique the user should
        provide the additional control option, namely the option path
        pricer and the option value.

        \ingroup mcarlo
    */
    public class MonteCarloModel<MC, RNG, S> where S : IGeneralStatistics {
        //typedef MC<RNG> mc_traits;
        //typedef RNG rng_traits;
        //typedef typename MC<RNG>::path_generator_type path_generator_type;
        //typedef typename MC<RNG>::path_pricer_type path_pricer_type;
        //typedef typename path_generator_type::sample_type sample_type;
        //typedef typename path_pricer_type::result_type result_type;
        //typedef S stats_type;

        // path_generator_type = InverseCumulativeRsg<RandomSequenceGenerator<MersenneTwisterUniformRng>, InverseCumulativeNormal>
        // sample_type = Sample<List<double>>

        private PathGenerator<IRNG> pathGenerator_;
        private PathPricer<IPath> pathPricer_;
        private S sampleAccumulator_;
        private bool isAntitheticVariate_;
        private PathPricer<IPath> cvPathPricer_;
        private double cvOptionValue_;
        private bool isControlVariate_;
        private PathGenerator<IRNG> cvPathGenerator_;

        // constructor
        //public MonteCarloModel(PathGenerator<IRNG> pathGenerator, IPathPricer<Path> pathPricer, S sampleAccumulator,
        //          bool antitheticVariate,
        //          IPathPricer<Path> cvPathPricer = boost::shared_ptr<path_pricer_type>(),
        //          result_type cvOptionValue = result_type(),
        //          PathGenerator<IRNG> cvPathGenerator = path_generator_type()) {
        public MonteCarloModel(PathGenerator<IRNG> pathGenerator, PathPricer<IPath> pathPricer, S sampleAccumulator,
                  bool antitheticVariate)
            : this(pathGenerator, pathPricer, sampleAccumulator, antitheticVariate, null, 0, null) { }
        public MonteCarloModel(PathGenerator<IRNG> pathGenerator, PathPricer<IPath> pathPricer, S sampleAccumulator,
                               bool antitheticVariate, PathPricer<IPath> cvPathPricer, double cvOptionValue,
                               PathGenerator<IRNG> cvPathGenerator) {
            pathGenerator_ = pathGenerator;
            pathPricer_ = pathPricer;
            sampleAccumulator_ = sampleAccumulator;
            isAntitheticVariate_ = antitheticVariate;
            cvPathPricer_ = cvPathPricer;
            cvOptionValue_ = cvOptionValue;
            cvPathGenerator_ = cvPathGenerator;
            if (cvPathPricer_ == null)
                isControlVariate_ = false;
            else
                isControlVariate_ = true;
        }


        public void addSamples(int samples) {
            for(int j = 1; j <= samples; j++) {

                Sample<Path> path = pathGenerator_.next();
                double price = pathPricer_.value(path.value);

                if (isControlVariate_) {
                    if (cvPathGenerator_ == null) {
                        price += cvOptionValue_ - cvPathPricer_.value(path.value);
                    } else {
                        Sample<Path> cvPath = cvPathGenerator_.next();
                        price += cvOptionValue_ - cvPathPricer_.value(cvPath.value);
                    }
                }

                if (isAntitheticVariate_) {
                    path = pathGenerator_.antithetic();
                    double price2 = pathPricer_.value(path.value);
                    if (isControlVariate_) {
                        if (cvPathGenerator_ == null)
                            price2 += cvOptionValue_ - cvPathPricer_.value(path.value);
                        else {
                            Sample<Path> cvPath = cvPathGenerator_.antithetic();
                            price2 += cvOptionValue_ - cvPathPricer_.value(cvPath.value);
                        }
                    }

                    sampleAccumulator_.add((price+price2)/2.0, path.weight);
                } else {
                    sampleAccumulator_.add(price, path.weight);
                }
            }
        }

        public S sampleAccumulator() { return sampleAccumulator_; }
    }
}
