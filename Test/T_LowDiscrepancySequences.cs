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

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QLNet;

#region IRNGFactory
    public interface IRNGFactory
    {
        string name();
        IRNG make(int dim, ulong seed);
    }
    
    public class MersenneFactory : IRNGFactory
    {
        //typedef RandomSequenceGenerator<MersenneTwisterUniformRng> MersenneTwisterUniformRsg;
        //typedef MersenneTwisterUniformRsg generator_type;
        public IRNG make(int dim,ulong seed) {
            return new RandomSequenceGenerator<MersenneTwisterUniformRng>(dim, seed);
        }

        public string name() { return "Mersenne Twister"; }

    };

    public class SobolFactory : IRNGFactory
    {
       //typedef SobolRsg generator_type;

        public SobolFactory(SobolRsg.DirectionIntegers unit){
            unit_=unit; 
        }

        public IRNG make(int dim, ulong seed)  {
            return new SobolRsg(dim,seed,unit_);
        }
        public string name()  {
            string prefix="";
            switch (unit_) {
              case SobolRsg.DirectionIntegers.Unit:
                prefix = "unit-initialized ";
                break;
              case SobolRsg.DirectionIntegers.Jaeckel:
                prefix = "Jäckel-initialized ";
                break;
              case SobolRsg.DirectionIntegers.SobolLevitan:
                prefix = "SobolLevitan-initialized ";
                break;
              case SobolRsg.DirectionIntegers.SobolLevitanLemieux:
                prefix = "SobolLevitanLemieux-initialized ";
                break;
              case SobolRsg.DirectionIntegers.Kuo:
                prefix = "Kuo";
                break;
              case SobolRsg.DirectionIntegers.Kuo2:
                prefix = "Kuo2";
                break;
              case SobolRsg.DirectionIntegers.Kuo3:
                prefix = "Kuo3";
                break;
              default:
                Assert.Fail("unknown direction integers");
                    break;
            }
            return prefix + "Sobol sequences: ";
      
        }
      private SobolRsg.DirectionIntegers unit_;
    };

    public class HaltonFactory : IRNGFactory
    {
      
        //typedef HaltonRsg generator_type;
        public HaltonFactory(bool randomStart, bool randomShift){
            start_ = randomStart;
            shift_ = randomShift;
        }

        public IRNG make(int dim,ulong seed){
            return new HaltonRsg(dim,seed,start_,shift_);
        }
        
        public string name()  {
            string prefix = start_ ?"random-start " :"";
            if (shift_)
                prefix += "random-shift ";
            return prefix + "Halton";
        }
    
        private bool start_, shift_;
    };
#endregion


    [TestClass()]
    public class T_LowDiscrepancySequences
    {
        public void testSeedGenerator() {
            //("Testing random-seed generator...");
            SeedGenerator.instance().get();
        }

        [TestMethod()]
        public void testPolynomialsModuloTwo() {

            //("Testing " + PPMT_MAX_DIM +
            //              " primitive polynomials modulo two...");

            int[] jj = {
                         1,       1,       2,       2,       6,       6,      18,
                        16,      48,      60,     176,     144,     630,     756,
                      1800,    2048,    7710,    7776,   27594,   24000,   84672,
                    120032,  356960,  276480, 1296000, 1719900, 4202496
            };

            int i=0,j=0,n=0;
            ulong polynomial=0;
            while (n < SobolRsg.PPMT_MAX_DIM || (int)polynomial != -1)
            {
                if ((int)polynomial==-1) {
                    ++i; // Increase degree index
                    j=0; // Reset index of polynomial in degree.
                }
                polynomial = (ulong)SobolRsg.PrimitivePolynomials[i][j];
                if ((int)polynomial==-1) {
                    --n;
                    if (j!=jj[i]) {
                        Assert.Fail("Only " + j + " polynomials in degree " + i+1
                                    + " instead of " + jj[i]);
                    }
                }
                ++j; // Increase index of polynomial in degree i+1
                ++n; // Increase overall polynomial counter
            }

        }

        [TestMethod()]
        public void testSobol()
        {

            //("Testing Sobol sequences up to dimension "
            //              + PPMT_MAX_DIM + "...");

            List<double> point;
            double tolerance = 1.0e-15;

            // testing max dimensionality
            int dimensionality =(int)SobolRsg.PPMT_MAX_DIM;
            ulong seed = 123456;
            SobolRsg rsg=new SobolRsg(dimensionality, seed);
            int points = 100, i;
            for (i=0; i<points; i++) 
            {
                point = rsg.nextSequence().value;
                if (point.Count!=dimensionality) {
                    Assert.Fail("Sobol sequence generator returns "+
                                " a sequence of wrong dimensionality: " + point.Count 
                                + " instead of  " + dimensionality);
                }
            }

            // testing homogeneity properties
            dimensionality = 33;
            seed = 123456;
            rsg = new SobolRsg(dimensionality, seed);
            SequenceStatistics stat=new SequenceStatistics(dimensionality);
            List<double> mean;
            int k = 0;
            for (int j=1; j<5; j++) { // five cycle
                points = (int)(Utils.Pow(2.0, j)-1); // base 2
                for (; k<points; k++) {
                    point = rsg.nextSequence().value;
                    stat.add(point);
                }
                mean = stat.mean();
                for (i=0; i<dimensionality; i++) {
                    double error = Math.Abs(mean[i]-0.5);
                    if (error > tolerance) {
                        Assert.Fail(i+1 + " dimension: "
                                   // + QL_FIXED
                                    + "mean (" + mean[i]
                                    + ") at the end of the " + j+1
                                    + " cycle in Sobol sequence is not " + 0.5
                                    //+ QL_SCIENTIFIC
                                    + " (error = " + error + ")");
                    }
                }
            }

            // testing first dimension (van der Corput sequence)
            double[]  vanderCorputSequenceModuloTwo= {
                // first cycle (zero excluded)
                0.50000,
                // second cycle
                0.75000, 0.25000,
                // third cycle
                0.37500, 0.87500, 0.62500, 0.12500,
                // fourth cycle
                0.18750, 0.68750, 0.93750, 0.43750, 0.31250, 0.81250, 0.56250, 0.06250,
                // fifth cycle
                0.09375, 0.59375, 0.84375, 0.34375, 0.46875, 0.96875, 0.71875, 0.21875,
                0.15625, 0.65625, 0.90625, 0.40625, 0.28125, 0.78125, 0.53125, 0.03125
            };

            dimensionality = 1;
            rsg = new SobolRsg(dimensionality);
            points = (int)(Utils.Pow(2.0, 5))-1; // five cycles
            for (i=0; i<points; i++) {
                point = rsg.nextSequence().value;
                double error =Math.Abs(point[0]-vanderCorputSequenceModuloTwo[i]);
                if (error > tolerance) {
                    Assert.Fail(i+1 + " draw ("
                                //+ QL_FIXED 
                                + point[0]
                                + ") in 1-D Sobol sequence is not in the "
                                + "van der Corput sequence modulo two: "
                                + "it should have been "
                                + vanderCorputSequenceModuloTwo[i]
                                //+ QL_SCIENTIFIC
                                + " (error = " + error + ")");
                }
            }
        }

/*public void testFaure() {

    //("Testing Faure sequences...");

    List<double> point;
    double tolerance = 1.0e-15;

    // testing "high" dimensionality
    int dimensionality = PPMT_MAX_DIM;
    FaureRsg rsg(dimensionality);
    int points = 100, i;
    for (i=0; i<points; i++) {
        point = rsg.nextSequence().value;
        if (point.size()!=dimensionality) {
            Assert.Fail("Faure sequence generator returns "
                        " a sequence of wrong dimensionality: " + point.size()
                        + " instead of  " + dimensionality);
        }
    }

    // 1-dimension Faure (van der Corput sequence base 2)
     double vanderCorputSequenceModuloTwo[] = {
        // first cycle (zero excluded)
        0.50000,
        // second cycle
        0.75000, 0.25000,
        // third cycle
        0.37500, 0.87500, 0.62500, 0.12500,
        // fourth cycle
        0.18750, 0.68750, 0.93750, 0.43750, 0.31250, 0.81250, 0.56250, 0.06250,
        // fifth cycle
        0.09375, 0.59375, 0.84375, 0.34375, 0.46875, 0.96875, 0.71875, 0.21875,
        0.15625, 0.65625, 0.90625, 0.40625, 0.28125, 0.78125, 0.53125, 0.03125
    };
    dimensionality = 1;
    rsg = FaureRsg(dimensionality);
    points = int(std::pow(2.0, 5))-1; // five cycles
    for (i=0; i<points; i++) {
        point = rsg.nextSequence().value;
        double error = std::fabs(point[0]-vanderCorputSequenceModuloTwo[i]);
        if (error > tolerance) {
            Assert.Fail(io::ordinal(i+1) + " draw, dimension 1 ("
                        + QL_FIXED + point[0]
                        + ") in 3-D Faure sequence should have been "
                        + vanderCorputSequenceModuloTwo[i]
                        + QL_SCIENTIFIC
                        + " (error = " + error + ")");
        }
    }

    // 2nd dimension of the 2-dimensional Faure sequence
    // (shuffled van der Corput sequence base 2)
    // checked with the code provided with "Economic generation of
    // low-discrepancy sequences with a b-ary gray code", by E. Thiemard
     double FaureDimensionTwoOfTwo[] = {
        // first cycle (zero excluded)
        0.50000,
        // second cycle
        0.25000, 0.75000,
        // third cycle
        0.37500, 0.87500, 0.12500, 0.62500,
        // fourth cycle
        0.31250, 0.81250, 0.06250, 0.56250, 0.18750, 0.68750, 0.43750, 0.93750,
        // fifth cycle
        0.46875, 0.96875, 0.21875, 0.71875, 0.09375, 0.59375, 0.34375, 0.84375,
        0.15625, 0.65625, 0.40625, 0.90625, 0.28125, 0.78125, 0.03125, 0.53125
    };
    dimensionality = 2;
    rsg = FaureRsg(dimensionality);
    points = int(std::pow(2.0, 5))-1; // five cycles
    for (i=0; i<points; i++) {
        point = rsg.nextSequence().value;
        double error = std::fabs(point[0]-vanderCorputSequenceModuloTwo[i]);
        if (error > tolerance) {
            Assert.Fail(io::ordinal(i+1) + " draw, dimension 1 ("
                        + QL_FIXED + point[0]
                        + ") in 3-D Faure sequence should have been "
                        + vanderCorputSequenceModuloTwo[i]
                        + QL_SCIENTIFIC
                        + " (error = " + error + ")");
        }
        error = std::fabs(point[1]-FaureDimensionTwoOfTwo[i]);
        if (error > tolerance) {
            Assert.Fail(io::ordinal(i+1) + " draw, dimension 2 ("
                        + QL_FIXED + point[1]
                        + ") in 3-D Faure sequence should have been "
                        + FaureDimensionTwoOfTwo[i]
                        + QL_SCIENTIFIC
                        + " (error = " + error + ")");
        }
    }

    // 3-dimension Faure sequence (shuffled van der Corput sequence base 3)
    // see "Monte Carlo Methods in Financial Engineering,"
    // by Paul Glasserman, 2004 Springer Verlag, pag. 299
     double FaureDimensionOneOfThree[] = {
        // first cycle (zero excluded)
        1.0/3,  2.0/3,
        // second cycle
        7.0/9,  1.0/9,  4.0/9,  5.0/9,  8.0/9,  2.0/9
    };
     double FaureDimensionTwoOfThree[] = {
        // first cycle (zero excluded)
        1.0/3,  2.0/3,
        // second cycle
        1.0/9,  4.0/9,  7.0/9,  2.0/9,  5.0/9,  8.0/9
    };
     double FaureDimensionThreeOfThree[] = {
        // first cycle (zero excluded)
        1.0/3,  2.0/3,
        // second cycle
        4.0/9,  7.0/9,  1.0/9,  8.0/9,  2.0/9,  5.0/9
    };

    dimensionality = 3;
    rsg = FaureRsg(dimensionality);
    points = int(std::pow(3.0, 2))-1; // three cycles
    for (i=0; i<points; i++) {
        point = rsg.nextSequence().value;
        double error = std::fabs(point[0]-FaureDimensionOneOfThree[i]);
        if (error > tolerance) {
            Assert.Fail(io::ordinal(i+1) + " draw, dimension 1 ("
                        + QL_FIXED + point[0]
                        + ") in 3-D Faure sequence should have been "
                        + FaureDimensionOneOfThree[i]
                        + QL_SCIENTIFIC
                        + " (error = " + error + ")");
        }
        error = std::fabs(point[1]-FaureDimensionTwoOfThree[i]);
        if (error > tolerance) {
            Assert.Fail(io::ordinal(i+1) + " draw, dimension 2 ("
                        + QL_FIXED + point[1]
                        + ") in 3-D Faure sequence should have been "
                        + FaureDimensionTwoOfThree[i]
                        + QL_SCIENTIFIC
                        + " (error = " + error + ")");
        }
        error = std::fabs(point[2]-FaureDimensionThreeOfThree[i]);
        if (error > tolerance) {
            Assert.Fail(io::ordinal(i+1) + " draw, dimension 3 ("
                        + QL_FIXED + point[2]
                        + ") in 3-D Faure sequence should have been "
                        + FaureDimensionThreeOfThree[i]
                        + QL_SCIENTIFIC
                        + " (error = " + error + ")");
        }
    }
}*/

        [TestMethod()]
        public void testHalton()
        {

            //("Testing Halton sequences...");

            List<double> point;
            double tolerance = 1.0e-15;

            // testing "high" dimensionality
            int dimensionality = (int)SobolRsg.PPMT_MAX_DIM;
            HaltonRsg rsg = new HaltonRsg(dimensionality, 0, false, false);
            int points = 100, i, k;
            for (i = 0; i < points; i++)
            {
                point = rsg.nextSequence().value;
                if (point.Count != dimensionality)
                {
                    Assert.Fail("Halton sequence generator returns "+
                    " a sequence of wrong dimensionality: " + point.Count
                    + " instead of  " + dimensionality)
                    ;
                }
            }

            // testing first and second dimension (van der Corput sequence)
            double[] vanderCorputSequenceModuloTwo = {
                                                         // first cycle (zero excluded)
                                                         0.50000,
                                                         // second cycle
                                                         0.25000, 0.75000,
                                                         // third cycle
                                                         0.12500, 0.62500, 0.37500, 0.87500,
                                                         // fourth cycle
                                                         0.06250, 0.56250, 0.31250, 0.81250, 0.18750, 0.68750, 0.43750,
                                                         0.93750,
                                                         // fifth cycle
                                                         0.03125, 0.53125, 0.28125, 0.78125, 0.15625, 0.65625, 0.40625,
                                                         0.90625,
                                                         0.09375, 0.59375, 0.34375, 0.84375, 0.21875, 0.71875, 0.46875,
                                                         0.96875,
                                                     };

            dimensionality = 1;
            rsg = new HaltonRsg(dimensionality, 0, false, false);
            points = (int) (Math.Pow(2.0, 5)) - 1; // five cycles
            for (i = 0; i < points; i++)
            {
                point = rsg.nextSequence().value;
                double error = Math.Abs(point[0] - vanderCorputSequenceModuloTwo[i]);
                if (error > tolerance)
                {
                    Assert.Fail(i + 1 + " draw ("
                                + /*QL_FIXED*/ + point[0]
                                + ") in 1-D Halton sequence is not in the "
                                + "van der Corput sequence modulo two: "
                                + "it should have been "
                                + vanderCorputSequenceModuloTwo[i]
                                //+ QL_SCIENTIFIC
                                + " (error = " + error + ")");
                }
            }

            double[] vanderCorputSequenceModuloThree = {
                                                           // first cycle (zero excluded)
                                                           1.0/3, 2.0/3,
                                                           // second cycle
                                                           1.0/9, 4.0/9, 7.0/9, 2.0/9, 5.0/9, 8.0/9,
                                                           // third cycle
                                                           1.0/27, 10.0/27, 19.0/27, 4.0/27, 13.0/27, 22.0/27,
                                                           7.0/27, 16.0/27, 25.0/27, 2.0/27, 11.0/27, 20.0/27,
                                                           5.0/27, 14.0/27, 23.0/27, 8.0/27, 17.0/27, 26.0/27
                                                       };

            dimensionality = 2;
            rsg = new HaltonRsg(dimensionality, 0, false, false);
            points = (int) (Math.Pow(3.0, 3)) - 1; // three cycles of the higher dimension
            for (i = 0; i < points; i++)
            {
                point = rsg.nextSequence().value;
                double error = Math.Abs(point[0] - vanderCorputSequenceModuloTwo[i]);
                if (error > tolerance)
                {
                    Assert.Fail("First component of " + i + 1
                                + " draw (" + /*QL_FIXED*/ + point[0]
                                + ") in 2-D Halton sequence is not in the "
                                + "van der Corput sequence modulo two: "
                                + "it should have been "
                                + vanderCorputSequenceModuloTwo[i]
                                //+ QL_SCIENTIFIC
                                + " (error = " + error + ")");
                }
                error = Math.Abs(point[1] - vanderCorputSequenceModuloThree[i]);
                if (error > tolerance)
                {
                    Assert.Fail("Second component of " + i + 1
                                + " draw (" + /*QL_FIXED*/ + point[1]
                                + ") in 2-D Halton sequence is not in the "
                                + "van der Corput sequence modulo three: "
                                + "it should have been "
                                + vanderCorputSequenceModuloThree[i]
                                //+ QL_SCIENTIFIC
                                + " (error = " + error + ")");
                }
            }

            // testing homogeneity properties
            dimensionality = 33;
            rsg = new HaltonRsg(dimensionality, 0, false, false);
            SequenceStatistics stat = new SequenceStatistics(dimensionality);
            List<double> mean; //, stdev, variance, skewness, kurtosis;
            k = 0;
            int j;
            for (j = 1; j < 5; j++)
            {
                // five cycle
                points = (int) (Math.Pow(2.0, j)) - 1; // base 2
                for (; k < points; k++)
                {
                    point = rsg.nextSequence().value;
                    stat.add(point);
                }
                mean = stat.mean();
                double error = Math.Abs(mean[0] - 0.5);
                if (error > tolerance)
                {
                    Assert.Fail("First dimension mean (" + /*QL_FIXED*/ + mean[0]
                                + ") at the end of the " + j + 1
                                + " cycle in Halton sequence is not " + 0.5
                                //+ QL_SCIENTIFIC
                                + " (error = " + error + ")");
                }
            }

            // reset generator and gaussianstatistics
            rsg = new HaltonRsg(dimensionality, 0, false, false);
            stat.reset(dimensionality);
            k = 0;
            for (j = 1; j < 3; j++)
            {
                // three cycle
                points = (int) (Math.Pow(3.0, j)) - 1; // base 3
                for (; k < points; k++)
                {
                    point = rsg.nextSequence().value;
                    stat.add(point);
                }
                mean = stat.mean();
                double error = Math.Abs(mean[1] - 0.5);
                if (error > tolerance)
                {
                    Assert.Fail("Second dimension mean (" + /*QL_FIXED*/ + mean[1]
                                + ") at the end of the " + j + 1
                                + " cycle in Halton sequence is not " + 0.5
                                //+ QL_SCIENTIFIC
                                + " (error = " + error + ")");
                }
            }
        }

        public void testGeneratorDiscrepancy(IRNGFactory generatorFactory, double[][] discrepancy)
        {
            //QL_TEST_START_TIMING
            double tolerance = 1.0e-2;
            List<double> point;
            int dim;
            ulong seed = 123456;
            double discr;
            // more than 1 discrepancy measures take long time
            int sampleLoops = Math.Max(1, discrepancyMeasuresNumber);

            for (int i = 0; i < 8; i++)
            {
                dim = dimensionality[i];
                DiscrepancyStatistics stat = new DiscrepancyStatistics(dim);

                IRNG rsg = generatorFactory.make(dim, seed);

                int j, k = 0, jMin = 10;
                stat.reset(dim);

                for (j = jMin; j < jMin + sampleLoops; j++)
                {
                    int points = (int)(Utils.Pow(2.0, (int)(j))) - 1;
                    for (; k < points; k++)
                    {
                        point = rsg.nextSequence().value;
                        stat.add(point);
                    }

                    discr = stat.discrepancy();

                    if (Math.Abs(discr - discrepancy[i][j - jMin]) > tolerance * discr)
                    {
                        Assert.Fail(generatorFactory.name()
                                    + "discrepancy dimension " + dimensionality[i]
                                    + " at " + points + " samples is "
                                    + discr + " instead of "
                                    + discrepancy[i][j - jMin]);
                    }
                }
            }
        }

        #region testMersenneTwisterDiscrepancy
        public void testMersenneTwisterDiscrepancy()
        {
            //("Testing Mersenne-twister discrepancy...");

            double[][] discrepancy = {
                dim002DiscrMersenneTwis, dim003DiscrMersenneTwis,
                dim005DiscrMersenneTwis, dim010DiscrMersenneTwis,
                dim015DiscrMersenneTwis, dim030DiscrMersenneTwis,
                dim050DiscrMersenneTwis, dim100DiscrMersenneTwis
            };

            testGeneratorDiscrepancy(new MersenneFactory(),
                                     discrepancy/*,"MersenneDiscrepancy.txt",
                                     "DiscrMersenneTwis"*/
                                                           );
        }
        #endregion

        #region testAltonDiscrepancy
        public void testPlainHaltonDiscrepancy()
        {

            //("Testing plain Halton discrepancy...");

            double[][] discrepancy = {
                dim002DiscrPlain_Halton, dim003DiscrPlain_Halton,
                dim005DiscrPlain_Halton, dim010DiscrPlain_Halton,
                dim015DiscrPlain_Halton, dim030DiscrPlain_Halton,
                dim050DiscrPlain_Halton, dim100DiscrPlain_Halton};

            testGeneratorDiscrepancy(new HaltonFactory(false, false),
                                     discrepancy/*,"PlainHaltonDiscrepancy.txt",
                                     "DiscrPlain_Halton"*/
                                                          );
        }

        public void testRandomStartHaltonDiscrepancy()
        {

            //("Testing random-start Halton discrepancy...");

            double[][] discrepancy = {
                dim002DiscrRStartHalton, dim003DiscrRStartHalton,
                dim005DiscrRStartHalton, dim010DiscrRStartHalton,
                dim015DiscrRStartHalton, dim030DiscrRStartHalton,
                dim050DiscrRStartHalton, dim100DiscrRStartHalton};

            testGeneratorDiscrepancy(new HaltonFactory(true, false),
                                     discrepancy/*,"RandomStartHaltonDiscrepancy.txt",
                                     "DiscrRStartHalton"*/
                                                          );
        }

        public void testRandomShiftHaltonDiscrepancy()
        {

            //("Testing random-shift Halton discrepancy...");

            double[][] discrepancy = {
                dim002DiscrRShiftHalton, dim003DiscrRShiftHalton,
                dim005DiscrRShiftHalton, dim010DiscrRShiftHalton,
                dim015DiscrRShiftHalton, dim030DiscrRShiftHalton,
                dim050DiscrRShiftHalton, dim100DiscrRShiftHalton};

            testGeneratorDiscrepancy(new HaltonFactory(false, true),
                                     discrepancy/*,"RandomShiftHaltonDiscrepancy.txt",
                                     "DiscrRShiftHalton"*/
                                                          );
        }

        public void testRandomStartRandomShiftHaltonDiscrepancy()
        {

            //("Testing random-start, random-shift Halton discrepancy...");

            double[][] discrepancy = {
                dim002DiscrRStRShHalton, dim003DiscrRStRShHalton,
                dim005DiscrRStRShHalton, dim010DiscrRStRShHalton,
                dim015DiscrRStRShHalton, dim030DiscrRStRShHalton,
                dim050DiscrRStRShHalton, dim100DiscrRStRShHalton};

            testGeneratorDiscrepancy(new HaltonFactory(true, true),
                                    discrepancy/*,"RandomStartRandomShiftHaltonDiscrepancy.txt",
                                     "DiscrRStRShHalton"*/
                                                         );
        }

        //[TestMethod()]
        public void _testDiscrepancy_Alton()
        {
            testPlainHaltonDiscrepancy();
            testRandomStartHaltonDiscrepancy();
            testRandomShiftHaltonDiscrepancy();
            testRandomStartRandomShiftHaltonDiscrepancy();
        }
        #endregion Halton

        #region testSobolDiscrepancy
        public void testJackelSobolDiscrepancy()
        {

            //("Testing Jaeckel-Sobol discrepancy...");
            double[][] discrepancy = {
                dim002Discr_Sobol, dim003Discr_Sobol,
                dim005Discr_Sobol, dim010DiscrJackel_Sobol,
                dim015DiscrJackel_Sobol, dim030DiscrJackel_Sobol,
                dim050DiscrJackel_Sobol, dim100DiscrJackel_Sobol};

            testGeneratorDiscrepancy(new SobolFactory(SobolRsg.DirectionIntegers.Jaeckel),
                                    discrepancy/*,"JackelSobolDiscrepancy.txt","DiscrJackel_Sobol"*/);
        }

        public void testSobolLevitanSobolDiscrepancy()
        {

            //("Testing Levitan-Sobol discrepancy...");

            double[][] discrepancy = {
                dim002Discr_Sobol, dim003Discr_Sobol,
                dim005Discr_Sobol, dim010DiscrSobLev_Sobol,
                dim015DiscrSobLev_Sobol, dim030DiscrSobLev_Sobol,
                dim050DiscrSobLev_Sobol, dim100DiscrSobLev_Sobol};

            testGeneratorDiscrepancy(new SobolFactory(SobolRsg.DirectionIntegers.SobolLevitan),
                                     discrepancy/*,"SobolLevitanSobolDiscrepancy.txt",                                                        "DiscrSobLev_Sobol"*/);
        }

        public void testSobolLevitanLemieuxSobolDiscrepancy()
        {

            //("Testing Levitan-Lemieux-Sobol discrepancy...");

            double[][] discrepancy = {
                dim002Discr_Sobol, dim003Discr_Sobol,
                dim005Discr_Sobol, dim010DiscrSobLev_Sobol,
                dim015DiscrSobLev_Sobol, dim030DiscrSobLev_Sobol,
                dim050DiscrSobLem_Sobol, dim100DiscrSobLem_Sobol};

            testGeneratorDiscrepancy(new SobolFactory(SobolRsg.DirectionIntegers.SobolLevitanLemieux),
                                    discrepancy/*,
                                     "SobolLevitanLemieuxSobolDiscrepancy.txt",
                                     "DiscrSobLevLem_Sobol"*/
                                                            );
        }

        public void testUnitSobolDiscrepancy()
        {

            //("Testing unit Sobol discrepancy...");

            double[][] discrepancy = {
                dim002Discr__Unit_Sobol, dim003Discr__Unit_Sobol,
                dim005Discr__Unit_Sobol, dim010Discr__Unit_Sobol,
                dim015Discr__Unit_Sobol, dim030Discr__Unit_Sobol,
                dim050Discr__Unit_Sobol, dim100Discr__Unit_Sobol};

            testGeneratorDiscrepancy(new SobolFactory(SobolRsg.DirectionIntegers.Unit),
                                     discrepancy/*,"UnitSobolDiscrepancy.txt",
                                      "Discr__Unit_Sobol"*/
                                                          );
        }

        //[TestMethod()]
        public void _testDiscrepancy_Sobol()
        {
            testJackelSobolDiscrepancy();
            testSobolLevitanSobolDiscrepancy();
            testSobolLevitanLemieuxSobolDiscrepancy();
            testUnitSobolDiscrepancy();
        }

        #endregion

        [TestMethod()]
        public void testSobolSkipping()
        {

            //("Testing Sobol sequence skipping...");

            ulong seed = 42;
            int[] dimensionality = { 1, 10, 100, 1000 };
            ulong[] skip = { 0, 1, 42, 512, 100000 };
            SobolRsg.DirectionIntegers[] integers = {SobolRsg.DirectionIntegers.Unit,
                                                     SobolRsg.DirectionIntegers.Jaeckel,
                                                     SobolRsg.DirectionIntegers.SobolLevitan,
                                                     SobolRsg.DirectionIntegers.SobolLevitanLemieux};
            for (int i = 0; i < integers.Length; i++)
            {
                for (int j = 0; j < dimensionality.Length; j++)
                {
                    for (int k = 0; k < skip.Length; k++)
                    {

                        // extract n samples
                        SobolRsg rsg1 = new SobolRsg(dimensionality[j], seed, integers[i]);
                        for (int l = 0; l < (int)skip[k]; l++)
                            rsg1.nextInt32Sequence();

                        // skip n samples at once
                        SobolRsg rsg2 = new SobolRsg(dimensionality[j], seed, integers[i]);
                        rsg2.skipTo(skip[k]);

                        // compare next 100 samples
                        for (int m = 0; m < 100; m++)
                        {
                            List<ulong> s1 = rsg1.nextInt32Sequence();
                            List<ulong> s2 = rsg2.nextInt32Sequence();
                            for (int n = 0; n < s1.Count; n++)
                            {
                                if (s1[n] != s2[n])
                                {
                                    Assert.Fail("Mismatch after skipping:"
                                                + "\n  size:     " + dimensionality[j]
                                                + "\n  integers: " + integers[i]
                                                + "\n  skipped:  " + skip[k]
                                                + "\n  at index: " + n
                                                + "\n  expected: " + s1[n]
                                                + "\n  found:    " + s2[n]);
                                }
                            }
                        }
                    }
                }
            }
        }

        #region values_definition
            double[] dim002Discr_Sobol = {
        8.33e-004, 4.32e-004, 2.24e-004, 1.12e-004,
        5.69e-005, 2.14e-005 // , null
    };
            double[] dim002DiscrMersenneTwis = {
        8.84e-003, 5.42e-003, 5.23e-003, 4.47e-003,
        4.75e-003, 3.11e-003, 2.97e-003
    };
            double[] dim002DiscrPlain_Halton = {
        1.26e-003, 6.73e-004, 3.35e-004, 1.91e-004,
        1.11e-004, 5.05e-005, 2.42e-005
    };
            double[] dim002DiscrRShiftHalton = { 1.32e-003, 7.25e-004 };
            double[] dim002DiscrRStRShHalton = { 1.35e-003, 9.43e-004 };
            double[] dim002DiscrRStartHalton = { 1.08e-003, 6.40e-004 };
            double[] dim002Discr__Unit_Sobol = {
        8.33e-004, 4.32e-004, 2.24e-004, 1.12e-004,
        5.69e-005, 2.14e-005 // , null
    };

            double[] dim003Discr_Sobol = {
        1.21e-003, 6.37e-004, 3.40e-004, 1.75e-004,
        9.21e-005, 4.79e-005, 2.56e-005
    };
            double[] dim003DiscrMersenneTwis = {
        7.02e-003, 4.94e-003, 4.82e-003, 4.91e-003,
        3.33e-003, 2.80e-003, 2.62e-003
    };
            double[] dim003DiscrPlain_Halton = {
        1.63e-003, 9.62e-004, 4.83e-004, 2.67e-004,
        1.41e-004, 7.64e-005, 3.93e-005
    };
            double[] dim003DiscrRShiftHalton = { 1.96e-003, 1.03e-003 };
            double[] dim003DiscrRStRShHalton = { 2.17e-003, 1.54e-003 };
            double[] dim003DiscrRStartHalton = { 1.48e-003, 7.77e-004 };
            double[] dim003Discr__Unit_Sobol = {
        1.21e-003, 6.37e-004, 3.40e-004, 1.75e-004,
        9.21e-005, 4.79e-005, 2.56e-005
    };

            double[] dim005Discr_Sobol = {
        1.59e-003, 9.55e-004, 5.33e-004, 3.22e-004,
        1.63e-004, 9.41e-005, 5.19e-005
    };
            double[] dim005DiscrMersenneTwis = {
        4.28e-003, 3.48e-003, 2.48e-003, 1.98e-003,
        1.57e-003, 1.39e-003, 6.33e-004
    };
            double[] dim005DiscrPlain_Halton = {
        1.93e-003, 1.23e-003, 6.89e-004, 4.22e-004,
        2.13e-004, 1.25e-004, 7.17e-005
    };
            double[] dim005DiscrRShiftHalton = { 2.02e-003, 1.36e-003 };
            double[] dim005DiscrRStRShHalton = { 2.11e-003, 1.25e-003 };
            double[] dim005DiscrRStartHalton = { 1.74e-003, 1.08e-003 };
            double[] dim005Discr__Unit_Sobol = {
        1.85e-003, 9.39e-004, 5.19e-004, 2.99e-004,
        1.75e-004, 9.51e-005, 5.55e-005
    };

            double[] dim010DiscrJackel_Sobol = {
        7.08e-004, 5.31e-004, 3.60e-004, 2.18e-004,
        1.57e-004, 1.12e-004, 6.39e-005
    };
            double[] dim010DiscrSobLev_Sobol = {
        7.01e-004, 5.10e-004, 3.28e-004, 2.21e-004,
        1.57e-004, 1.08e-004, 6.38e-005
    };
            double[] dim010DiscrMersenneTwis = {
        8.83e-004, 6.56e-004, 4.87e-004, 3.37e-004,
        3.06e-004, 1.73e-004, 1.43e-004
    };
            double[] dim010DiscrPlain_Halton = {
        1.23e-003, 6.89e-004, 4.03e-004, 2.83e-004,
        1.61e-004, 1.08e-004, 6.69e-005
    };
            double[] dim010DiscrRShiftHalton = { 9.25e-004, 6.40e-004 };
            double[] dim010DiscrRStRShHalton = { 8.41e-004, 5.42e-004 };
            double[] dim010DiscrRStartHalton = { 7.89e-004, 5.33e-004 };
            double[] dim010Discr__Unit_Sobol = {
        7.67e-004, 4.92e-004, 3.47e-004, 2.34e-004,
        1.39e-004, 9.47e-005, 5.72e-005
    };

            double[] dim015DiscrJackel_Sobol = {
        1.59e-004, 1.23e-004, 7.73e-005, 5.51e-005,
        3.91e-005, 2.73e-005, 1.96e-005
    };
            double[] dim015DiscrSobLev_Sobol = {
        1.48e-004, 1.06e-004, 8.19e-005, 6.29e-005,
        4.16e-005, 2.54e-005, 1.73e-005
    };
            double[] dim015DiscrMersenneTwis = {
        1.63e-004, 1.12e-004, 8.36e-005, 6.09e-005,
        4.34e-005, 2.95e-005, 2.10e-005
    };
            double[] dim015DiscrPlain_Halton = {
        5.75e-004, 3.12e-004, 1.70e-004, 9.89e-005,
        5.33e-005, 3.45e-005, 2.11e-005
    };
            double[] dim015DiscrRShiftHalton = { 1.75e-004, 1.19e-004 };
            double[] dim015DiscrRStRShHalton = { 1.66e-004, 1.34e-004 };
            double[] dim015DiscrRStartHalton = { 2.09e-004, 1.30e-004 };
            double[] dim015Discr__Unit_Sobol = {
        2.24e-004, 1.39e-004, 9.86e-005, 6.02e-005,
        4.39e-005, 3.06e-005, 2.32e-005
    };

            double[] dim030DiscrJackel_Sobol = {
        6.43e-007, 5.28e-007, 3.88e-007, 2.49e-007,
        2.09e-007, 1.55e-007, 1.07e-007
    };
            double[] dim030DiscrSobLev_Sobol = {
        1.03e-006, 6.06e-007, 3.81e-007, 2.71e-007,
        2.68e-007, 1.73e-007, 1.21e-007
    };
            double[] dim030DiscrMersenneTwis = {
        4.38e-007, 3.25e-007, 4.47e-007, 2.85e-007,
        2.03e-007, 1.50e-007, 1.17e-007
    };
            double[] dim030DiscrPlain_Halton = {
        4.45e-004, 2.23e-004, 1.11e-004, 5.56e-005,
        2.78e-005, 1.39e-005, 6.95e-006
    };
            double[] dim030DiscrRShiftHalton = { 8.11e-007, 6.05e-007 };
            double[] dim030DiscrRStRShHalton = { 1.85e-006, 1.03e-006 };
            double[] dim030DiscrRStartHalton = { 4.42e-007, 4.64e-007 };
            double[] dim030Discr__Unit_Sobol = {
        4.35e-005, 2.17e-005, 1.09e-005, 5.43e-006,
        2.73e-006, 1.37e-006, 6.90e-007
    };

            double[] dim050DiscrJackel_Sobol = {
        2.98e-010, 2.91e-010, 2.62e-010, 1.53e-010,
        1.48e-010, 1.15e-010, 8.41e-011
    };
            double[] dim050DiscrSobLev_Sobol = {
        3.11e-010, 2.52e-010, 1.61e-010, 1.54e-010,
        1.11e-010, 8.60e-011, 1.17e-010
    };
            double[] dim050DiscrSobLem_Sobol = {
        4.57e-010, 6.84e-010, 3.68e-010, 2.20e-010,
        1.81e-010, 1.14e-010, 8.31e-011
    };
            double[] dim050DiscrMersenneTwis = {
        3.27e-010, 2.42e-010, 1.47e-010, 1.98e-010,
        2.31e-010, 1.30e-010, 8.09e-011
    };
            double[] dim050DiscrPlain_Halton = {
        4.04e-004, 2.02e-004, 1.01e-004, 5.05e-005,
        2.52e-005, 1.26e-005, 6.31e-006
    };
            double[] dim050DiscrRShiftHalton = { 1.14e-010, 1.25e-010 };
            double[] dim050DiscrRStRShHalton = { 2.92e-010, 5.02e-010 };
            double[] dim050DiscrRStartHalton = { 1.93e-010, 6.82e-010 };
            double[] dim050Discr__Unit_Sobol = {
        1.63e-005, 8.14e-006, 4.07e-006, 2.04e-006,
        1.02e-006, 5.09e-007, 2.54e-007
    };

            double[] dim100DiscrJackel_Sobol = {
        1.26e-018, 1.55e-018, 8.46e-019, 4.43e-019,
        4.04e-019, 2.44e-019, 4.86e-019
    };
            double[] dim100DiscrSobLev_Sobol = {
        1.17e-018, 2.65e-018, 1.45e-018, 7.28e-019,
        6.33e-019, 3.36e-019, 3.43e-019
    };
            double[] dim100DiscrSobLem_Sobol = {
        8.79e-019, 4.60e-019, 6.69e-019, 7.17e-019,
        5.81e-019, 2.97e-019, 2.64e-019
    };
            double[] dim100DiscrMersenneTwis = {
        5.30e-019, 7.29e-019, 3.71e-019, 3.33e-019,
        1.33e-017, 6.70e-018, 3.36e-018
    };
            double[] dim100DiscrPlain_Halton = {
        3.63e-004, 1.81e-004, 9.07e-005, 4.53e-005,
        2.27e-005, 1.13e-005, 5.66e-006
    };
            double[] dim100DiscrRShiftHalton = { 3.36e-019, 2.19e-019 };
            double[] dim100DiscrRStRShHalton = { 4.44e-019, 2.24e-019 };
            double[] dim100DiscrRStartHalton = { 9.85e-020, 8.34e-019 };
            double[] dim100Discr__Unit_Sobol = {
        4.97e-006, 2.48e-006, 1.24e-006, 6.20e-007,
        3.10e-007, 1.55e-007, 7.76e-008
    };

            int[] dimensionality = { 2, 3, 5, 10, 15, 30, 50, 100 };

            // 7 discrepancy measures for each dimension of all sequence generators
            // would take a few days ... too long for usual/frequent test running
            int discrepancyMeasuresNumber = 1;

            // let's add some generality here...
        #endregion

    }



