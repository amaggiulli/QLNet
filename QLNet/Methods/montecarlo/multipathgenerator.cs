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
    //! Generates a multipath from a random number generator.
    /*! RSG is a sample generator which returns a random sequence.
        It must have the minimal interface:
        \code
        RSG {
            Sample<Array> next();
        };
        \endcode

        \ingroup mcarlo

        \test the generated paths are checked against cached results
    */
    public class MultiPathGenerator<GSG> where GSG : IRNG {
        // typedef Sample<MultiPath> sample_type;
        
        private bool brownianBridge_;
        private StochasticProcess process_;
        private GSG generator_;
        private Sample<MultiPath> next_;

        public MultiPathGenerator(StochasticProcess process, TimeGrid times, GSG generator, bool brownianBridge) {
            brownianBridge_ = brownianBridge;
            process_ = process;
            generator_ = generator;
            next_ = new Sample<MultiPath>(new MultiPath(process.size(), times), 1.0);

            if (generator_.dimension() != process.factors()*(times.size()-1))
                throw new ApplicationException("dimension (" + generator_.dimension()
                       + ") is not equal to ("
                       + process.factors() + " * " + (times.size()-1)
                       + ") the number of factors "
                       + "times the number of time steps");
            if (!(times.size() > 1))
                throw new ApplicationException("no times given");
        }

        public Sample<MultiPath> next() { return next(false); }
        public Sample<MultiPath> antithetic() { return next(true); }
        private Sample<MultiPath> next(bool antithetic) {
            if (brownianBridge_) {
                throw new ApplicationException("Brownian bridge not supported");
            } else {
                // typedef typename GSG::sample_type sequence_type;
                Sample<List<double>> sequence_ =
                    antithetic ? generator_.lastSequence()
                               : generator_.nextSequence();

                int m = process_.size();
                int n = process_.factors();

                MultiPath path = next_.value;

                Vector asset = process_.initialValues();
                for (int j=0; j<m; j++)
                    path[j].setFront(asset[j]);

                Vector temp; // = new Vector(n);
                next_.weight = sequence_.weight;

                TimeGrid timeGrid = path[0].timeGrid();
                double t, dt;
                for (int i = 1; i < path.pathSize(); i++) {
                    int offset = (i-1)*n;
                    t = timeGrid[i-1];
                    dt = timeGrid.dt(i-1);
                    if (antithetic)
                        temp = -1 * new Vector(sequence_.value.GetRange(offset, n));
                    else
                        temp = new Vector(sequence_.value.GetRange(offset, n));

                    asset = process_.evolve(t, asset, dt, temp);
                    for (int j=0; j<m; j++)
                        path[j][i] = asset[j];
                }
                return next_;
            }
        }
    }
}
