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
// ===========================================================================
// NOTE: The following copyright notice applies to the original code,
//
// Copyright (C) 2002 Peter Jäckel "Monte Carlo Methods in Finance".
// All rights reserved.
//
// Permission to use, copy, modify, and distribute this software is freely
// granted, provided that this notice is preserved.
// ===========================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QLNet
{

    //! Halton low-discrepancy sequence generator
    /*! Halton algorithm for low-discrepancy sequence.  For more
        details see chapter 8, paragraph 2 of "Monte Carlo Methods in
        Finance", by Peter Jäckel

        \test
        - the correctness of the returned values is tested by
          reproducing known good values.
        - the correctness of the returned values is tested by checking
          their discrepancy against known good values.
    */
    public class HaltonRsg : IRNG
    {
      
        //typedef Sample<std::vector<Real> > sample_type;
        private int dimensionality_;
        private ulong sequenceCounter_;
        private  Sample<List<double> > sequence_;
        private List<ulong> randomStart_;
        private List<double>  randomShift_;

        public HaltonRsg(int dimensionality,
                         ulong seed = 0,
                         bool randomStart = true,
                         bool randomShift = false)
        {
            dimensionality_=dimensionality;
            sequenceCounter_ = 0;
            sequence_ = new Sample<List<double>>(new InitializedList<double>(dimensionality), 1.0);
            randomStart_= new InitializedList<ulong>(dimensionality, 0UL);
            randomShift_ = new InitializedList<double>(dimensionality, 0.0);

            if(!(dimensionality>0)) 
                throw new ArgumentException("dimensionality must be greater than 0");

            if (randomStart || randomShift) 
            {
                RandomSequenceGenerator<MersenneTwisterUniformRng>uniformRsg = 
                    new RandomSequenceGenerator<MersenneTwisterUniformRng>(dimensionality_, seed);
                if (randomStart)
                    randomStart_ = uniformRsg.nextInt32Sequence();
                if (randomShift)
                    randomShift_ = uniformRsg.nextSequence().value;
            }
        }

        public Sample<List<double> >  nextSequence(){
            ++sequenceCounter_;
            ulong b, k;
            double f, h;
            for (int i=0; i<dimensionality_; ++i) {
                h = 0.0;
                b = PrimeNumbers.get(i);
                f = 1.0;
                k = sequenceCounter_+randomStart_[i];
                while (k!=0) {
                    f /= b;
                    h += (k%b)*f;
                    k /= b;
                }
                sequence_.value[i] = h+randomShift_[i];
                sequence_.value[i] -= (long)(sequence_.value[i]);
            }
            return sequence_;
        }

        public Sample<List<double> >  lastSequence(){
            return sequence_;
        }

        public IRNG factory(int dimensionality, ulong seed){
            throw new System.NotImplementedException();
        }

        public int dimension() {return dimensionality_;}
      
    }
}

