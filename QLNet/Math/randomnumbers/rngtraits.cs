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
    public interface IRNGTraits {
        ulong nextInt32();
        Sample<double> next();

        IRNGTraits factory(ulong seed);
    }

    public interface IRSG {
        int allowsErrorEstimate { get; }
        object make_sequence_generator(int dimension, ulong seed);
    }

    // random number traits
    public class GenericPseudoRandom<URNG, IC> : IRSG where URNG : IRNGTraits, new() where IC : IValue, new() {
        // data
        public static IC icInstance = new IC();

        //// typedefs
        //typedef URNG urng_type;
        //typedef InverseCumulativeRng<urng_type,IC> rng_type;
        //typedef RandomSequenceGenerator<urng_type> ursg_type;
        //typedef InverseCumulativeRsg<ursg_type,IC> rsg_type;

        // more traits
        public int allowsErrorEstimate { get { return 1; } }

        // factory
        public object make_sequence_generator(int dimension, ulong seed) {
            RandomSequenceGenerator<URNG> g = new RandomSequenceGenerator<URNG>(dimension, seed);
            return (icInstance != null ? new InverseCumulativeRsg<RandomSequenceGenerator<URNG>, IC>(g, icInstance)
                                       : new InverseCumulativeRsg<RandomSequenceGenerator<URNG>, IC>(g));
        }
    }

    //! default traits for pseudo-random number generation
    /*! \test a sequence generator is generated and tested by comparing samples against known good values. */
    // typedef GenericPseudoRandom<MersenneTwisterUniformRng, InverseCumulativeNormal> PseudoRandom;
    public class PseudoRandom : GenericPseudoRandom<MersenneTwisterUniformRng, InverseCumulativeNormal> { }

    //! traits for Poisson-distributed pseudo-random number generation
    /*! \test sequence generators are generated and tested by comparing
              samples against known good values.
    */
    // typedef GenericPseudoRandom<MersenneTwisterUniformRng, InverseCumulativePoisson> PoissonPseudoRandom;
    public class PoissonPseudoRandom : GenericPseudoRandom<MersenneTwisterUniformRng, InverseCumulativePoisson> { }


    public class GenericLowDiscrepancy<URSG, IC> : IRSG where URSG : IRNG, new() where IC : IValue, new() {
        // typedefs
        //typedef URSG ursg_type;
        //typedef InverseCumulativeRsg<ursg_type,IC> rsg_type;

        // data
        public static IC icInstance = new IC();

        // more traits
        public int allowsErrorEstimate { get { return 0; } }

        // factory
        public object make_sequence_generator(int dimension, ulong seed) {
            URSG g = (URSG)new URSG().factory(dimension, seed);
            return (icInstance != null ? new InverseCumulativeRsg<URSG, IC>(g, icInstance)
                                       : new InverseCumulativeRsg<URSG, IC>(g));
        }
    }

    //! default traits for low-discrepancy sequence generation
    //typedef GenericLowDiscrepancy<SobolRsg, InverseCumulativeNormal> LowDiscrepancy;
    public class LowDiscrepancy : GenericLowDiscrepancy<SobolRsg, InverseCumulativeNormal> { }
}
