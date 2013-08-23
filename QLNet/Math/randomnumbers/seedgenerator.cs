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
    //! Random seed generator
    /*! Random number generator used for automatic generation of initialization seeds. */
    public class SeedGenerator {
        private static MersenneTwisterUniformRng rng_;

        private static readonly SeedGenerator instance_ = new SeedGenerator();
        private SeedGenerator() {
            rng_ = new MersenneTwisterUniformRng(42UL);
            initialize();
        }

        public ulong get() {
            return rng_.nextInt32();
        }

        public static SeedGenerator instance() { return instance_; }

        private void initialize() {
            // firstSeed is chosen based on clock() and used for the first rng
            ulong firstSeed = (ulong)DateTime.Now.Ticks; // (std::time(0));
            MersenneTwisterUniformRng first = new MersenneTwisterUniformRng(firstSeed);

            // secondSeed is as random as it could be
            // feel free to suggest improvements
            ulong secondSeed = first.nextInt32();

            MersenneTwisterUniformRng second = new MersenneTwisterUniformRng(secondSeed);

            // use the second rng to initialize the final one
            ulong skip = second.nextInt32() % 1000;
            List<ulong> init = new InitializedList<ulong>(4);
            init[0]=second.nextInt32();
            init[1]=second.nextInt32();
            init[2]=second.nextInt32();
            init[3]=second.nextInt32();

            rng_ = new MersenneTwisterUniformRng(init);

            for (ulong i=0; i<skip ; i++)
                rng_.nextInt32();
        }
    }
}
