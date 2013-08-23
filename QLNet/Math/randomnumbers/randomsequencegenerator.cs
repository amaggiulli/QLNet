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
    public interface IRNG {
        int dimension();
        Sample<List<double>> nextSequence();
        Sample<List<double>> lastSequence();

        IRNG factory(int dimensionality, ulong seed);
    }

    /*! Random sequence generator based on a pseudo-random number generator RNG.

        Class RNG must implement the following interface:
        \code
            RNG::sample_type RNG::next() const;
        \endcode

        \warning do not use with low-discrepancy sequence generator.
    */
    public class RandomSequenceGenerator<RNG> : IRNG where RNG : IRNGTraits, new() {
        // typedef Sample<std::vector<Real> > sample_type;
        private int dimensionality_;

        private RNG rng_;
        private Sample<List<double>> sequence_;
        private List<ulong> int32Sequence_;

        public RandomSequenceGenerator(int dimensionality, RNG rng) {
            dimensionality_ = dimensionality;
            rng_ = rng;
            
            List<double> ls = new List<double>();
            for (int i = 0; i < dimensionality; i++)
                ls.Add(0.0);
            sequence_ = new Sample<List<double>>(ls, 1.0);
            int32Sequence_ = new InitializedList<ulong>(dimensionality);

            if (!(dimensionality>0)) throw new ApplicationException("dimensionality must be greater than 0");
        }

        //public RandomSequenceGenerator(int dimensionality, long seed = 0) {
        public RandomSequenceGenerator(int dimensionality, ulong seed) {
            dimensionality_ = dimensionality;
            rng_ = (RNG)new RNG().factory(seed);
            sequence_ = new Sample<List<double>>(new InitializedList<double>(dimensionality), 1.0);
            int32Sequence_ = new InitializedList<ulong>(dimensionality);
        }

        public List<ulong> nextInt32Sequence() {
            for (int i = 0; i < dimensionality_; i++) {
                int32Sequence_[i] = rng_.nextInt32();
            }
            return int32Sequence_;
        }

        #region IRGN interface
        public Sample<List<double>> nextSequence() {
            sequence_.weight = 1.0;
            for (int i = 0; i < dimensionality_; i++) {
                Sample<double> x = rng_.next();  // typename RNG::sample_type x(rng_.next());
                sequence_.value[i] = x.value;
                sequence_.weight *= x.weight;
            }
            return sequence_;
        }

        public Sample<List<double>> lastSequence() { return sequence_; }

        public int dimension() { return dimensionality_; }

        public IRNG factory(int dimensionality, ulong seed) {
            return new RandomSequenceGenerator<RNG>(dimensionality, seed);
        }
        #endregion
    }
}
