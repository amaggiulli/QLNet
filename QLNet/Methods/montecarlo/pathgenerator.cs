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
    //! Generates random paths using a sequence generator
    /*! Generates random paths with drift(S,t) and variance(S,t)
        using a gaussian sequence generator

        \ingroup mcarlo

        \test the generated paths are checked against cached results
    */
    public class PathGenerator<GSG> where GSG : IRNG {
        // typedef Sample<Path> sample_type;

        private bool brownianBridge_;
        private GSG generator_;
        private int dimension_;
        private TimeGrid timeGrid_;
        private StochasticProcess1D process_;
        private Sample<Path> next_;
        private List<double> temp_;
        private BrownianBridge bb_;

        // constructors
        public PathGenerator(StochasticProcess process, double length, int timeSteps, GSG generator, bool brownianBridge) {
            brownianBridge_ = brownianBridge;
            generator_ = generator;
            dimension_ = generator_.dimension();
            timeGrid_ = new TimeGrid(length, timeSteps);
            process_ = process as StochasticProcess1D;
            next_ = new Sample<Path>(new Path(timeGrid_),1.0);
            temp_ = new InitializedList<double>(dimension_);
            bb_ = new BrownianBridge(timeGrid_);
            if (dimension_ != timeSteps) 
                throw new ApplicationException("sequence generator dimensionality (" + dimension_
                       + ") != timeSteps (" + timeSteps + ")");
        }

        public PathGenerator(StochasticProcess process, TimeGrid timeGrid, GSG generator, bool brownianBridge) {
            brownianBridge_ = brownianBridge;
            generator_ = generator;
            dimension_ = generator_.dimension();
            timeGrid_ = timeGrid;
            process_ = process as StochasticProcess1D;
            next_ = new Sample<Path>(new Path(timeGrid_),1.0);
            temp_ = new InitializedList<double>(dimension_);
            bb_ = new BrownianBridge(timeGrid_);

            if (dimension_ != timeGrid_.size() - 1)
                throw new ApplicationException("sequence generator dimensionality (" + dimension_
                       + ") != timeSteps (" + (timeGrid_.size() - 1) + ")");
        }

        public Sample<Path> next() { return next(false); }
        public Sample<Path> antithetic() { return next(true); }
        private Sample<Path> next(bool antithetic) {
            // typedef typename GSG::sample_type sequence_type;
            Sample<List<double>> sequence_ =
                antithetic ? generator_.lastSequence()
                           : generator_.nextSequence();

            if (brownianBridge_) {
                bb_.transform(sequence_.value, temp_);
            } else {
                temp_ = new List<double>(sequence_.value);
            }

            next_.weight = sequence_.weight;

            Path path = next_.value;
            path.setFront(process_.x0());

            for (int i=1; i<path.length(); i++) {
                double t = timeGrid_[i-1];
                double dt = timeGrid_.dt(i - 1);
                path[i] = process_.evolve(t, path[i-1], dt,
                                           antithetic ? -temp_[i-1] :
                                                         temp_[i-1]);
            }
            return next_;
        }
    }
}
