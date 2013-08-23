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
    //! Uniform random number generator
    /*! Mersenne Twister random number generator of period 2**19937-1

        For more details see http://www.math.keio.ac.jp/matumoto/emt.html

        \test the correctness of the returned values is tested by
              checking them against known good results.
    */
    public class MersenneTwisterUniformRng : IRNGTraits {
        //typedef Sample<Real> sample_type;

        /*! if the given seed is 0, a random seed will be chosen based on clock() */
        public MersenneTwisterUniformRng() : this(0) { }
        public MersenneTwisterUniformRng(ulong seed) {
            mt = new InitializedList<ulong>(N);
            seedInitialization(seed);
        }

        public MersenneTwisterUniformRng(List<ulong> seeds) {
            mt = new InitializedList<ulong>(N);

            seedInitialization(19650218UL);
            int i = 1, j = 0, k = (N > seeds.Count ? N : seeds.Count);
            for (; k!=0; k--) {
                mt[i] = (mt[i] ^ ((mt[i - 1] ^ (mt[i - 1] >> 30)) * 1664525UL)) + seeds[j] + (ulong)j; /* non linear */
                mt[i] &= 0xffffffffUL; /* for WORDSIZE > 32 machines */
                i++; j++;
                if (i>=N) { mt[0] = mt[N-1]; i=1; }
                if (j>=seeds.Count) j=0;
            }
            for (k=N-1; k!=0; k--) {
                mt[i] = (mt[i] ^ ((mt[i-1] ^ (mt[i-1] >> 30)) * 1566083941UL)) - (ulong)i; /* non linear */
                mt[i] &= 0xffffffffUL; /* for WORDSIZE > 32 machines */
                i++;
                if (i>=N) { mt[0] = mt[N-1]; i=1; }
            }

            mt[0] = 0x80000000UL; /*MSB is 1; assuring non-zero initial array*/
        }

        public IRNGTraits factory(ulong seed) { return new MersenneTwisterUniformRng(seed); }


        /*! returns a sample with weight 1.0 containing a random number on (0.0, 1.0)-real-interval  */
        public Sample<double> next() {
            // divide by 2^32
            double result = ((double)nextInt32() + 0.5)/4294967296.0;
            return new Sample<double>(result,1.0);
        }

        //! return  a random number on [0,0xffffffff]-interval
        public ulong nextInt32() {
            ulong y;
            ulong[] mag01 = { 0x0UL, MATRIX_A };
            /* mag01[x] = x * MATRIX_A  for x=0,1 */

            if (mti >= N) { /* generate N words at one time */
                int kk;

                for (kk=0;kk<N-M;kk++) {
                    y = (mt[kk]&UPPER_MASK)|(mt[kk+1]&LOWER_MASK);
                    mt[kk] = mt[kk+M] ^ (y >> 1) ^ mag01[y & 0x1UL];
                }
                for (;kk<N-1;kk++) {
                    y = (mt[kk]&UPPER_MASK)|(mt[kk+1]&LOWER_MASK);
                    mt[kk] = mt[kk+(M-N)] ^ (y >> 1) ^ mag01[y & 0x1UL];
                }
                y = (mt[N-1]&UPPER_MASK)|(mt[0]&LOWER_MASK);
                mt[N-1] = mt[M-1] ^ (y >> 1) ^ mag01[y & 0x1UL];

                mti = 0;
            }

            y = mt[mti++];

            /* Tempering */
            y ^= (y >> 11);
            y ^= (y << 7) & 0x9d2c5680UL;
            y ^= (y << 15) & 0xefc60000UL;
            y ^= (y >> 18);

            return y;
        }

        private void seedInitialization(ulong seed) {
            /* initializes mt with a seed */
            ulong s = (seed != 0 ? seed : SeedGenerator.instance().get());
            mt[0]= s & 0xffffffffUL;
            for (mti=1; mti<N; mti++) {
                mt[mti] = (1812433253UL * (mt[mti - 1] ^ (mt[mti - 1] >> 30)) + (ulong)mti);
                /* See Knuth TAOCP Vol2. 3rd Ed. P.106 for multiplier. */
                /* In the previous versions, MSBs of the seed affect   */
                /* only MSBs of the array mt[].                        */
                /* 2002/01/09 modified by Makoto Matsumoto             */
                mt[mti] &= 0xffffffffUL;
                /* for >32 bit machines */
            }
        }

        private List<ulong> mt;
        private int mti;


        // Period parameters
        const int N = 624;
        const int M = 397;
        // constant vector a
        const ulong MATRIX_A = 0x9908b0dfUL;
        // most significant w-r bits
        const ulong UPPER_MASK=0x80000000UL;
        // least significant r bits
        const ulong LOWER_MASK=0x7fffffffUL;
    }
}
