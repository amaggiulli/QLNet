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
    //! Sobol low-discrepancy sequence generator
    /*! A Gray code counter and bitwise operations are used for very
        fast sequence generation.

        The implementation relies on primitive polynomials modulo two
        from the book "Monte Carlo Methods in Finance" by Peter
        Jдckel.

        21 200 primitive polynomials modulo two are provided in QuantLib.
        Jдckel has calculated 8 129 334 polynomials: if you need that many
        dimensions you can replace the primitivepolynomials.c file included
        in QuantLib with the one provided in the CD of the "Monte Carlo
        Methods in Finance" book.

        The choice of initialization numbers (also know as free direction
        integers) is crucial for the homogeneity properties of the sequence.
        Sobol defines two homogeneity properties: Property A and Property A'.

        The unit initialization numbers suggested in "Numerical
        Recipes in C", 2nd edition, by Press, Teukolsky, Vetterling,
        and Flannery (section 7.7) fail the test for Property A even
        for low dimensions.

        Bratley and Fox published coefficients of the free direction
        integers up to dimension 40, crediting unpublished work of
        Sobol' and Levitan. See Bratley, P., Fox, B.L. (1988)
        "Algorithm 659: Implementing Sobol's quasirandom sequence
        generator," ACM Transactions on Mathematical Software
        14:88-100. These values satisfy Property A for d<=20 and d =
        23, 31, 33, 34, 37; Property A' holds for d<=6.

        Jдckel provides in his book (section 8.3) initialization
        numbers up to dimension 32. Coefficients for d<=8 are the same
        as in Bradley-Fox, so Property A' holds for d<=6 but Property
        A holds for d<=32.

        The implementation of Lemieux, Cieslak, and Luttmer includes
        coefficients of the free direction integers up to dimension
        360.  Coefficients for d<=40 are the same as in Bradley-Fox.
        For dimension 40<d<=360 the coefficients have
        been calculated as optimal values based on the "resolution"
        criterion. See "RandQMC user's guide - A package for
        randomized quasi-Monte Carlo methods in C," by C. Lemieux,
        M. Cieslak, and K. Luttmer, version January 13 2004, and
        references cited there
        (http://www.math.ucalgary.ca/~lemieux/randqmc.html).
        The values up to d<=360 has been provided to the QuantLib team by
        Christiane Lemieux, private communication, September 2004.

        For more info on Sobol' sequences see also "Monte Carlo
        Methods in Financial Engineering," by P. Glasserman, 2004,
        Springer, section 5.2.3

        The Joe--Kuo numbers and the Kuo numbers are due to Stephen Joe
        and Frances Kuo.

        S. Joe and F. Y. Kuo, Constructing Sobol sequences with better
        two-dimensional projections, preprint Nov 22 2007

        See http://web.maths.unsw.edu.au/~fkuo/sobol/ for more information.

        Note that the Kuo numbers were generated to work with a
        different ordering of primitive polynomials for the first 40
        or so dimensions which is why we have the Alternative
        Primitive Polynomials.

        \test
        - the correctness of the returned values is tested by
          reproducing known good values.
        - the correctness of the returned values is tested by checking
          their discrepancy against known good values.
    */
    public partial class SobolRsg : IRNG {
        //typedef Sample<List<double>> sample_type;

        const int bits_ = 8 * sizeof(ulong);
        // 1/(2^bits_) (written as (1/2)/(2^(bits_-1)) to avoid long overflow)
        const double normalizationFactor_ = 0.5 / (1UL << (bits_ - 1));

        private int dimensionality_;
        private ulong sequenceCounter_;
        private bool firstDraw_;
        private Sample<List<double>> sequence_;
        private List<ulong> integerSequence_;
        private List<List<ulong>> directionIntegers_;

        public enum DirectionIntegers {
            Unit, Jaeckel, SobolLevitan, SobolLevitanLemieux,
            JoeKuoD5, JoeKuoD6, JoeKuoD7,
            Kuo, Kuo2, Kuo3 };

        // required for generics
        public SobolRsg() { }

        /*! \pre dimensionality must be <= PPMT_MAX_DIM */
        public SobolRsg(int dimensionality) : this(dimensionality, 0, DirectionIntegers.Jaeckel) { }
        public SobolRsg(int dimensionality, ulong seed) : this(dimensionality, seed, DirectionIntegers.Jaeckel) { }
        public SobolRsg(int dimensionality, ulong seed, DirectionIntegers directionIntegers) {
            dimensionality_ = dimensionality;
            sequenceCounter_ = 0;
            firstDraw_ = true;

            sequence_ = new Sample<List<double>>(new InitializedList<double>(dimensionality), 1.0);
            integerSequence_ = new InitializedList<ulong>(dimensionality);
            
            directionIntegers_ = new InitializedList<List<ulong>>(dimensionality, new List<ulong>(bits_));
            for (int i = 0; i < dimensionality; i++)
                directionIntegers_[i] = new InitializedList<ulong>(bits_);

            if (!(dimensionality>0)) throw new ApplicationException("dimensionality must be greater than 0");
            if (!(dimensionality<=PPMT_MAX_DIM))
                throw new ApplicationException("dimensionality " + dimensionality + " exceeds the number of available "
                       + "primitive polynomials modulo two (" + PPMT_MAX_DIM + ")");

            // initializes coefficient array of the k-th primitive polynomial
            // and degree of the k-th primitive polynomial
            List<uint> degree = new InitializedList<uint>(dimensionality_);
            List<long> ppmt = new InitializedList<long>(dimensionality_);

            bool useAltPolynomials = false;

            if (directionIntegers == DirectionIntegers.Kuo || directionIntegers == DirectionIntegers.Kuo2 
                || directionIntegers == DirectionIntegers.Kuo3 || directionIntegers == DirectionIntegers.SobolLevitan 
                || directionIntegers == DirectionIntegers.SobolLevitanLemieux)
                useAltPolynomials = true;

            // degree 0 is not used
            ppmt[0]=0;
            degree[0]=0;
            int k, index;
            uint currentDegree=1;
            k=1;
            index=0;

            uint altDegree = useAltPolynomials ? maxAltDegree : 0;

            for (; k<Math.Min(dimensionality_,altDegree); k++,index++)
            {
                ppmt[k] = AltPrimitivePolynomials[currentDegree-1][index];
                if (ppmt[k]==-1)
                {
                    ++currentDegree;
                    index=0;
                    ppmt[k] = AltPrimitivePolynomials[currentDegree-1][index];
                }

                degree[k] = currentDegree;
            }

            for (; k<dimensionality_; k++,index++)
            {
                ppmt[k] = PrimitivePolynomials[currentDegree-1][index];
                if (ppmt[k]==-1)
                {
                    ++currentDegree;
                    index=0;
                    ppmt[k] = PrimitivePolynomials[currentDegree-1][index];
                }
                degree[k] = currentDegree;

            }

            // initializes bits_ direction integers for each dimension
            // and store them into directionIntegers_[dimensionality_][bits_]
            //
            // In each dimension k with its associated primitive polynomial,
            // the first degree_[k] direction integers can be chosen freely
            // provided that only the l leftmost bits can be non-zero, and
            // that the l-th leftmost bit must be set

            // degenerate (no free direction integers) first dimension
            int j;
            for (j=0; j<bits_; j++)
                directionIntegers_[0][j] = (1UL<<(bits_-j-1));

            int maxTabulated = 0;
            // dimensions from 2 (k=1) to maxTabulated (k=maxTabulated-1) included
            // are initialized from tabulated coefficients
            switch (directionIntegers) {
                case DirectionIntegers.Unit:
                    maxTabulated=dimensionality_;
                    for (k=1; k<maxTabulated; k++) {
                        for (int l=1; l<=degree[k]; l++) {
                            directionIntegers_[k][l-1] = 1UL;
                            directionIntegers_[k][l-1] <<= (bits_-l);
                        }
                    }
                    break;
                case DirectionIntegers.Jaeckel:
                    // maxTabulated=32
                    maxTabulated = initializers.Length + 1;
                    for (k=1; k<Math.Min(dimensionality_, maxTabulated); k++) {
                        j = 0;
                        // 0UL marks coefficients' end for a given dimension
                        while (initializers[k-1][j] != 0UL) {
                            directionIntegers_[k][j] = initializers[k-1][j];
                            directionIntegers_[k][j] <<= (bits_-j-1);
                            j++;
                        }
                    }
                    break;
                case DirectionIntegers.SobolLevitan:
                    // maxTabulated=40
                    maxTabulated = SLinitializers.Length + 1;
                    for (k=1; k<Math.Min(dimensionality_, maxTabulated); k++) {
                        j = 0;
                        // 0UL marks coefficients' end for a given dimension
                        while (SLinitializers[k-1][j] != 0UL) {
                            directionIntegers_[k][j] = SLinitializers[k-1][j];
                            directionIntegers_[k][j] <<= (bits_-j-1);
                            j++;
                        }
                    }
                    break;
                case DirectionIntegers.SobolLevitanLemieux:
                    // maxTabulated=360
                    maxTabulated = Linitializers.Length + 1;
                    for (k=1; k<Math.Min(dimensionality_, maxTabulated); k++) {
                        j = 0;
                        // 0UL marks coefficients' end for a given dimension
                        while (Linitializers[k-1][j] != 0UL) {
                            directionIntegers_[k][j] = Linitializers[k-1][j];
                            directionIntegers_[k][j] <<= (bits_-j-1);
                            j++;
                        }
                    }
                    break;
                case DirectionIntegers.JoeKuoD5:
                    // maxTabulated=1898
                    maxTabulated = JoeKuoD5initializers.Length + 1;
                    for (k=1; k<Math.Min(dimensionality_, maxTabulated); k++) {
                        j = 0;
                        // 0UL marks coefficients' end for a given dimension
                        while (JoeKuoD5initializers[k-1][j] != 0UL) {
                            directionIntegers_[k][j] = JoeKuoD5initializers[k-1][j];
                            directionIntegers_[k][j] <<= (bits_-j-1);
                            j++;
                        }
                    }
                    break;
                case DirectionIntegers.JoeKuoD6:
                    // maxTabulated=1799
                    maxTabulated = JoeKuoD6initializers.Length + 1;
                    for (k=1; k<Math.Min(dimensionality_, maxTabulated); k++) {
                        j = 0;
                        // 0UL marks coefficients' end for a given dimension
                        while (JoeKuoD6initializers[k-1][j] != 0UL) {
                            directionIntegers_[k][j] = JoeKuoD6initializers[k-1][j];
                            directionIntegers_[k][j] <<= (bits_-j-1);
                            j++;
                        }
                    }
                    break;
                case DirectionIntegers.JoeKuoD7:
                    // maxTabulated=1898
                    maxTabulated = JoeKuoD7initializers.Length + 1;
                    for (k=1; k<Math.Min(dimensionality_, maxTabulated); k++) {
                        j = 0;
                        // 0UL marks coefficients' end for a given dimension
                        while (JoeKuoD7initializers[k-1][j] != 0UL) {
                            directionIntegers_[k][j] = JoeKuoD7initializers[k-1][j];
                            directionIntegers_[k][j] <<= (bits_-j-1);
                            j++;
                        }
                    }
                    break;



                case DirectionIntegers.Kuo:
                    // maxTabulated=4925
                    maxTabulated = Kuoinitializers.Length + 1;
                    for (k=1; k<Math.Min(dimensionality_, maxTabulated); k++) {
                        j = 0;
                        // 0UL marks coefficients' end for a given dimension
                        while (Kuoinitializers[k-1][j] != 0UL) {
                            directionIntegers_[k][j] = Kuoinitializers[k-1][j];
                            directionIntegers_[k][j] <<= (bits_-j-1);
                            j++;
                        }
                    }
                    break;
                case DirectionIntegers.Kuo2:
                    // maxTabulated=3946
                    maxTabulated = Kuo2initializers.Length+1;
                    for (k=1; k<Math.Min(dimensionality_, maxTabulated); k++) {
                        j = 0;
                        // 0UL marks coefficients' end for a given dimension
                        while (Kuo2initializers[k-1][j] != 0UL) {
                            directionIntegers_[k][j] = Kuo2initializers[k-1][j];
                            directionIntegers_[k][j] <<= (bits_-j-1);
                            j++;
                        }
                    }
                    break;

                case DirectionIntegers.Kuo3:
                    // maxTabulated=4585
                    maxTabulated = Kuo3initializers.Length+1;
                    for (k=1; k<Math.Min(dimensionality_, maxTabulated); k++) {
                        j = 0;
                        // 0UL marks coefficients' end for a given dimension
                        while (Kuo3initializers[k-1][j] != 0UL) {
                            directionIntegers_[k][j] = Kuo3initializers[k-1][j];
                            directionIntegers_[k][j] <<= (bits_-j-1);
                            j++;
                        }
                    }
                    break;

                default:
                    break;
            }

            // random initialization for higher dimensions
            if (dimensionality_>maxTabulated) {
                MersenneTwisterUniformRng uniformRng = new MersenneTwisterUniformRng(seed);
                for (k=maxTabulated; k<dimensionality_; k++) {
                    for (int l=1; l<=degree[k]; l++) {
                        do {
                            // u is in (0,1)
                            double u = uniformRng.next().value;
                            // the direction integer has at most the
                            // rightmost l bits non-zero
                            directionIntegers_[k][l-1] = (ulong)(u*(1UL<<l));
                        } while (!((directionIntegers_[k][l-1] & 1UL) != 0));
                        // iterate until the direction integer is odd
                        // that is it has the rightmost bit set

                        // shifting bits_-l bits to the left
                        // we are guaranteed that the l-th leftmost bit
                        // is set, and only the first l leftmost bit
                        // can be non-zero
                        directionIntegers_[k][l-1] <<= (bits_-l);
                    }
                }
            }

            // computation of directionIntegers_[k][l] for l>=degree_[k]
            // by recurrence relation
            for (k=1; k<dimensionality_; k++) {
                uint gk = degree[k];
                for (int l=(int)gk; l<bits_; l++) {
                    // eq. 8.19 "Monte Carlo Methods in Finance" by P. Jдckel
                    ulong n = (directionIntegers_[k][(int)(l - gk)] >> (int)gk);
                    // a[k][j] are the coefficients of the monomials in ppmt[k]
                    // The highest order coefficient a[k][0] is not actually
                    // used in the recurrence relation, and the lowest order
                    // coefficient a[k][gk] is always set: this is the reason
                    // why the highest and lowest coefficient of
                    // the polynomial ppmt[k] are not included in its encoding,
                    // provided that its degree is known.
                    // That is: a[k][j] = ppmt[k] >> (gk-j-1)
                    for (uint z=1; z<gk; z++) {
                        // XORed with a selection of (unshifted) direction
                        // integers controlled by which of the a[k][j] are set
                        if ((((ulong)ppmt[k] >> (int)(gk - z - 1)) & 1UL) != 0)
                            n ^= directionIntegers_[k][(int)(l-z)];
                    }
                    // a[k][gk] is always set, so directionIntegers_[k][l-gk]
                    // will always enter
                    n ^= directionIntegers_[k][(int)(l-gk)];
                    directionIntegers_[k][l]=n;
                }
            }

            // in case one needs to check the directionIntegers used
            /* bool printDirectionIntegers = false;
               if (printDirectionIntegers) {
                   std::ofstream outStream("directionIntegers.txt");
                   for (k=0; k<std::min(32UL,dimensionality_); k++) {
                       outStream << std::endl << k+1       << "\t"
                                              << degree[k] << "\t"
                                              << ppmt[k]   << "\t";
                       for (j=0; j<10; j++) {
                           outStream << io::power_of_two(
                               directionIntegers_[k][j]) << "\t";
                       }
                   }
                   outStream.close();
               }
            */

            // initialize the Sobol integer/double vectors
            // first draw
            for (k=0; k<dimensionality_; k++) {
                integerSequence_[k]=directionIntegers_[k][0];
            }
        }

        /*! skip to the n-th sample in the low-discrepancy sequence */
        public void skipTo(ulong skip) {
            ulong N = skip+1;
            uint ops = (uint)(Math.Log((double)N)/Const.M_LN2)+1;

            // Convert to Gray code
            ulong G = N ^ (N>>1);
            for (int k=0; k<dimensionality_; k++) {
                integerSequence_[k] = 0;
                for (int index = 0; index < ops; index++) {
                    if ((G>>index & 1) != 0)
                        integerSequence_[k] ^= directionIntegers_[k][index];
                }
            }
            sequenceCounter_ = skip;
        }

        public List<ulong> nextInt32Sequence()    {
            if (firstDraw_) {
                // it was precomputed in the constructor
                firstDraw_ = false;
                return integerSequence_;
            }
            // increment the counter
            sequenceCounter_++;
            // did we overflow?
            if (sequenceCounter_ == 0) throw new ApplicationException("period exceeded");

            // instead of using the counter n as new unique generating integer
            // for the n-th draw use the Gray code G(n) as proposed
            // by Antonov and Saleev
            ulong n = sequenceCounter_;
            // Find rightmost zero bit of n
            int j = 0;
            while ((n & 1) != 0) { n >>= 1; j++; }
            for (int k=0; k<dimensionality_; k++) {
                // XOR the appropriate direction number into each component of
                // the integer sequence to obtain a new Sobol integer for that
                // component
                integerSequence_[k] ^= directionIntegers_[k][j];
            }
            return integerSequence_;
        }

        #region IRNG interface
        public Sample<List<double>> nextSequence() {
            List<ulong> v = nextInt32Sequence();
            // normalize to get a double in (0,1)
            for (int k = 0; k < dimensionality_; ++k)
                sequence_.value[k] = v[k] * normalizationFactor_;
            return sequence_;
        }

        public Sample<List<double>> lastSequence() { return sequence_; }

        public int dimension() { return dimensionality_; }

        public IRNG factory(int dimensionality, ulong seed) {
            return new SobolRsg(dimensionality, seed);
        } 
        #endregion
    }
}
