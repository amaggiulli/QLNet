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
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QLNet;

namespace TestSuite
{
    [TestClass()]
    public class T_Pathgenerator
    {
        public void testSingle(StochasticProcess1D process,
                        string tag, 
                        bool brownianBridge,
                        double expected, 
                        double antithetic) 
        {
            ulong seed = 42;
            double length = 10;
            int timeSteps = 12;

            var rsg = (InverseCumulativeRsg<RandomSequenceGenerator<MersenneTwisterUniformRng>
                                                                    ,InverseCumulativeNormal>)
                       new PseudoRandom().make_sequence_generator(timeSteps, seed);

         

            PathGenerator<IRNG> generator = new PathGenerator<IRNG>(process, 
                                                                    length, 
                                                                    timeSteps,
                                                                    rsg, 
                                                                    brownianBridge);
            int i;
            for (i=0; i<100; i++)
                generator.next();

            Sample<Path> sample = generator.next();
            double calculated = sample.value.back();
            double error = Math.Abs(calculated-expected);
            double tolerance = 2.0e-8;
            if (error > tolerance) 
            {
                Assert.Fail("using " + tag + " process "
                            + (brownianBridge ? "with " : "without ")
                            + "brownian bridge:\n"
                            //+ std::setprecision(13)
                            + "    calculated: " + calculated + "\n"
                            + "    expected:   " + expected + "\n"
                            + "    error:      " + error + "\n"
                            + "    tolerance:  " + tolerance);
            }

            sample = generator.antithetic();
            calculated = sample.value.back();
            error = Math.Abs(calculated-antithetic);
            tolerance = 2.0e-7;
            if (error > tolerance) 
            {
                Assert.Fail("using " + tag + " process "
                        + (brownianBridge ? "with " : "without ")
                        + "brownian bridge:\n"
                        + "antithetic sample:\n"
                        //+ setprecision(13)
                        + "    calculated: " + calculated + "\n"
                        + "    expected:   " + antithetic + "\n"
                        + "    error:      " + error + "\n"
                        + "    tolerance:  " + tolerance);
            }
        }

        public void testMultiple(StochasticProcess process,
                                 string tag,
                                 double[] expected, 
                                 double[] antithetic )
        {
       

            ulong seed = 42;
            double length = 10;
            int timeSteps = 12;
            int assets = process.size();
        
            var rsg = (InverseCumulativeRsg<RandomSequenceGenerator<MersenneTwisterUniformRng>
                                                                    ,InverseCumulativeNormal>)
                       new PseudoRandom().make_sequence_generator(timeSteps*assets, seed);

            MultiPathGenerator<IRNG> generator=new MultiPathGenerator<IRNG>(process,
                                                                            new TimeGrid(length, timeSteps),
                                                                            rsg, false);
            int i;
            for (i=0; i<100; i++)
                generator.next();

            Sample<MultiPath> sample = generator.next();
            Vector calculated = new Vector(assets);
            double error, tolerance = 2.0e-7;
            
            for (int j=0; j<assets; j++)
                calculated[j] = sample.value[j].back() ;
            
            for (int j=0; j<assets; j++) {
                error = Math.Abs(calculated[j]-expected[j]);
                if (error > tolerance) {
                    Assert.Fail("using " + tag + " process "
                                + "(" + j+1 + " asset:)\n"
                                //+ std::setprecision(13)
                                + "    calculated: " + calculated[j] + "\n"
                                + "    expected:   " + expected[j] + "\n"
                                + "    error:      " + error + "\n"
                                + "    tolerance:  " + tolerance);
                }
            }

            sample = generator.antithetic();
            for (int j=0; j<assets; j++)
                calculated[j] = sample.value[j].back();
            for (int j=0; j<assets; j++) {
                error = Math.Abs(calculated[j]-antithetic[j]);
                if (error > tolerance) {
                    Assert.Fail("using " + tag + " process "
                                + "(" + j+1 + " asset:)\n"
                                + "antithetic sample:\n"
                                //+ std::setprecision(13)
                                + "    calculated: " + calculated[j] + "\n"
                                + "    expected:   " + antithetic[j] + "\n"
                                + "    error:      " + error + "\n"
                                + "    tolerance:  " + tolerance);
                }
            }
        }

        [TestMethod()]
        public void testPathGenerator() {

            //"Testing 1-D path generation against cached values...");

            //SavedSettings backup;

            Settings.setEvaluationDate(new Date(26,4,2005));

            Handle<Quote> x0=new Handle<Quote> (new SimpleQuote(100.0));
            Handle<YieldTermStructure> r =new Handle<YieldTermStructure> (Utilities.flatRate(0.05, new Actual360()));
            Handle<YieldTermStructure> q=new Handle<YieldTermStructure> (Utilities.flatRate(0.02, new Actual360()));
            Handle<BlackVolTermStructure> sigma = new Handle<BlackVolTermStructure>(Utilities.flatVol(0.20, new Actual360()));
            // commented values must be used when Halley's correction is enabled
            testSingle( new BlackScholesMertonProcess(x0,q,r,sigma),
                       "Black-Scholes", false, 26.13784357783, 467.2928561411);
                                            // 26.13784357783, 467.2928562519);
            //Error make the borwnian bridge test first
            testSingle(new BlackScholesMertonProcess(x0,q,r,sigma),
                       "Black-Scholes", true, 60.28215549393, 202.6143139999);
                                           // 60.28215551021, 202.6143139437);

            testSingle(new GeometricBrownianMotionProcess(100.0, 0.03, 0.20),
                       "geometric Brownian", false, 27.62223714065, 483.6026514084);
                                                 // 27.62223714065, 483.602651493);

            testSingle(new OrnsteinUhlenbeckProcess(0.1, 0.20),
                       "Ornstein-Uhlenbeck", false, -0.8372003433557, 0.8372003433557);

            testSingle(new SquareRootProcess(0.1, 0.1, 0.20, 10.0),
                       "square-root", false, 1.70608664108, 6.024200546031);
        }

        [TestMethod()]
        public void testMultiPathGenerator()
        {

            //("Testing n-D path generation against cached values...");

            //SavedSettings backup;

            Settings.setEvaluationDate(new Date(26,4,2005));

            Handle<Quote> x0=new Handle<Quote> (new SimpleQuote(100.0));
            Handle<YieldTermStructure> r =new Handle<YieldTermStructure> (Utilities.flatRate(0.05, new Actual360()));
            Handle<YieldTermStructure> q=new Handle<YieldTermStructure> (Utilities.flatRate(0.02, new Actual360()));
            Handle<BlackVolTermStructure> sigma=new Handle<BlackVolTermStructure> (Utilities.flatVol(0.20, new Actual360()));

            Matrix correlation=new Matrix(3,3);
            correlation[0,0] = 1.0; correlation[0,1] = 0.9; correlation[0,2] = 0.7;
            correlation[1,0] = 0.9; correlation[1,1] = 1.0; correlation[1,2] = 0.4;
            correlation[2,0] = 0.7; correlation[2,1] = 0.4; correlation[2,2] = 1.0;

            List<StochasticProcess1D>  processes = new List<StochasticProcess1D>(3);
            StochasticProcess process;

            processes.Add(new BlackScholesMertonProcess(x0,q,r,sigma));
            processes.Add(new BlackScholesMertonProcess(x0,q,r,sigma));
            processes.Add(new BlackScholesMertonProcess(x0,q,r,sigma));
            process = new StochasticProcessArray(processes,correlation);
            // commented values must be used when Halley's correction is enabled
            double[] result1 = {
                188.2235868185,
                270.6713069569,
                113.0431145652 };
            // Real result1[] = {
            //     188.2235869273,
            //     270.6713071508,
            //     113.0431145652 };
            double[] result1a = {
                64.89105742957,
                45.12494404804,
                108.0475146914 };
            // Real result1a[] = {
            //     64.89105739157,
            //     45.12494401537,
            //     108.0475146914 };
            testMultiple(process, "Black-Scholes", result1, result1a);

            processes[0] = new GeometricBrownianMotionProcess(100.0, 0.03, 0.20);
            processes[1] = new GeometricBrownianMotionProcess(100.0, 0.03, 0.20);
            processes[2] = new GeometricBrownianMotionProcess(100.0, 0.03, 0.20);
            process = new StochasticProcessArray(processes,correlation);
            double[] result2 = {
                174.8266131680,
                237.2692443633,
                119.1168555440 };
            // Real result2[] = {
            //     174.8266132344,
            //     237.2692444869,
            //     119.1168555605 };
            double[] result2a = {
                57.69082393020,
                38.50016862915,
                116.4056510107 };
            // Real result2a[] = {
            //     57.69082387657,
            //     38.50016858691,
            //     116.4056510107 };
            testMultiple(process, "geometric Brownian", result2, result2a);

            processes[0] = new OrnsteinUhlenbeckProcess(0.1, 0.20);
            processes[1] = new OrnsteinUhlenbeckProcess(0.1, 0.20);
            processes[2] = new OrnsteinUhlenbeckProcess(0.1, 0.20);
            process = new StochasticProcessArray(processes,correlation);
            double[] result3 = {
                0.2942058437284,
                0.5525006418386,
                0.02650931054575 };
            double[] result3a = {
                -0.2942058437284,
                -0.5525006418386,
                -0.02650931054575 };
            testMultiple(process, "Ornstein-Uhlenbeck", result3, result3a);

            processes[0] = new SquareRootProcess(0.1, 0.1, 0.20, 10.0);
            processes[1] = new SquareRootProcess(0.1, 0.1, 0.20, 10.0);
            processes[2] = new SquareRootProcess(0.1, 0.1, 0.20, 10.0);
            process = new StochasticProcessArray(processes,correlation);
            double[] result4 = {
                4.279510844897,
                4.943783503533,
                3.590930385958 };
            double[] result4a = {
                2.763967737724,
                2.226487196647,
                3.503859264341 };
            testMultiple(process, "square-root", result4, result4a);
        }
    }
}
