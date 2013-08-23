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
    public class T_LiborMarketModelProcess
    {

        int len = 10;

        IborIndex makeIndex() 
        {
            DayCounter dayCounter = new Actual360();
            List<Date> dates = new List<Date>();
            List<double> rates = new List<double>();
            dates.Add(new Date(4,9,2005));
            dates.Add(new Date(4,9,2018));
            rates.Add(0.01);
            rates.Add(0.08);
            Linear Interpolator=new Linear();
            RelinkableHandle<YieldTermStructure> termStructure= new RelinkableHandle<YieldTermStructure>();;
            //termStructure.linkTo(new InterpolatedZeroCurve<Linear>(dates, rates, dayCounter, Interpolator));

            IborIndex index = new Euribor1Y(termStructure);

            Date todaysDate =
            index.fixingCalendar().adjust(new Date(4,9,2005));
            Settings.setEvaluationDate(todaysDate);

            dates[0] = index.fixingCalendar().advance(todaysDate,
                                                   index.fixingDays(), TimeUnit.Days);

            //termStructure.linkTo(new ZeroCurve(dates, rates, dayCounter));
            termStructure.linkTo(new InterpolatedZeroCurve<Linear>(dates, rates, dayCounter, Interpolator));

            return index;
        }

        CapletVarianceCurve makeCapVolCurve(Date todaysDate) 
        {
            double[] vols = {14.40, 17.15, 16.81, 16.64, 16.17,
                             15.78, 15.40, 15.21, 14.86, 14.54};

            List<Date> dates = new List<Date>();
            List<double> capletVols = new List<double>();
            LiborForwardModelProcess process= new LiborForwardModelProcess(len+1, makeIndex(),null);

            for (int i=0; i < len; ++i) 
            {
                capletVols.Add(vols[i]/100);
                dates.Add(process.fixingDates()[i+1]);
            }

            return new CapletVarianceCurve( todaysDate, dates,
                                            capletVols, new ActualActual());
        }

        LiborForwardModelProcess makeProcess()
        {
            Matrix volaComp=new Matrix();;
            return makeProcess(volaComp);
        }
        
        LiborForwardModelProcess makeProcess(Matrix volaComp)
        {
            int factors = (volaComp.empty() ? 1 : volaComp.columns());

            IborIndex index = makeIndex();
            LiborForwardModelProcess process= new LiborForwardModelProcess(len, index,null);

            LfmCovarianceParameterization fct=new LfmHullWhiteParameterization(
                                                    process,
                                                    makeCapVolCurve(Settings.evaluationDate()),
                                                    volaComp * Matrix.transpose(volaComp), factors);

            process.setCovarParam(fct);

            return process;
        }
        
        [TestMethod()]
        public void testInitialisation() 
        {
            //"Testing caplet LMM process initialisation..."

            //SavedSettings backup;

            DayCounter dayCounter = new Actual360();
            RelinkableHandle<YieldTermStructure> termStructure= new RelinkableHandle<YieldTermStructure>();;
            termStructure.linkTo(Utilities.flatRate(Date.Today, 0.04, dayCounter));

            IborIndex index=new Euribor6M(termStructure);
            OptionletVolatilityStructure capletVol = new ConstantOptionletVolatility(
                                                        termStructure.currentLink().referenceDate(),
                                                        termStructure.currentLink().calendar(),
                                                        BusinessDayConvention.Following,
                                                        0.2,
                                                        termStructure.currentLink().dayCounter());

            Calendar calendar = index.fixingCalendar();

            for (int daysOffset=0; daysOffset < 1825 /* 5 year*/; daysOffset+=8) {
                Date todaysDate = calendar.adjust(Date.Today+daysOffset);
                Settings.setEvaluationDate(todaysDate);
                Date settlementDate =
                    calendar.advance(todaysDate, index.fixingDays(), TimeUnit.Days);

                termStructure.linkTo(Utilities.flatRate(settlementDate, 0.04, dayCounter));

                LiborForwardModelProcess process=new LiborForwardModelProcess(60, index);

                List<double> fixings = process.fixingTimes();
                for (int i=1; i < fixings.Count-1; ++i) {
                    int ileft  = process.nextIndexReset(fixings[i]-0.000001);
                    int iright = process.nextIndexReset(fixings[i]+0.000001);
                    int ii     = process.nextIndexReset(fixings[i]);

                    if ((ileft != i) || (iright != i+1) || (ii != i+1)) {
                        Assert.Fail("Failed to next index resets");
                    }
                }

            }
        }
        
        [TestMethod()]
        public void testLambdaBootstrapping() 
        {
            //"Testing caplet LMM lambda bootstrapping..."

            //SavedSettings backup;

            double tolerance = 1e-10;
            double[] lambdaExpected = {14.3010297550, 19.3821411939, 15.9816590141,
                                          15.9953118303, 14.0570815635, 13.5687599894,
                                          12.7477197786, 13.7056638165, 11.6191989567};

            LiborForwardModelProcess process = makeProcess();
            Matrix covar = process.covariance(0.0, null, 1.0);

            for (int i=0; i<9; ++i) {
                double calculated = Math.Sqrt(covar[i+1,i+1]);
                double expected   = lambdaExpected[i]/100;

                if (Math.Abs(calculated - expected) > tolerance)
                    Assert.Fail("Failed to reproduce expected lambda values"
                                + "\n    calculated: " + calculated
                                + "\n    expected:   " + expected);
            }

            LfmCovarianceParameterization param =  process.covarParam();

            List<double> tmp = process.fixingTimes();
            TimeGrid grid= new TimeGrid(tmp.Last(), 14);

            for (int t=0; t<grid.size(); ++t) {
                //verifier la presence du null
                Matrix diff = param.integratedCovariance(grid[t],null)
                            - param.integratedCovariance(grid[t], null);

                for (int i=0; i<diff.rows(); ++i) 
                {
                    for (int j=0; j<diff.columns(); ++j) 
                    {
                        if (Math.Abs(diff[i,j]) > tolerance) 
                        {
                             Assert.Fail("Failed to reproduce integrated covariance"
                                          + "\n    calculated: " + diff[i,j]
                                          + "\n    expected:   " + 0);
                        }
                    }
                }
            }
        }

        [TestMethod()]
        public void testMonteCarloCapletPricing() 
        {
            //"Testing caplet LMM Monte-Carlo caplet pricing..."

            //SavedSettings backup;

            /* factor loadings are taken from Hull & White article
               plus extra normalisation to get orthogonal eigenvectors
               http://www.rotman.utoronto.ca/~amackay/fin/libormktmodel2.pdf */
            double[] compValues = {0.85549771, 0.46707264, 0.22353259,
                                 0.91915359, 0.37716089, 0.11360610,
                                 0.96438280, 0.26413316,-0.01412414,
                                 0.97939148, 0.13492952,-0.15028753,
                                 0.95970595,-0.00000000,-0.28100621,
                                 0.97939148,-0.13492952,-0.15028753,
                                 0.96438280,-0.26413316,-0.01412414,
                                 0.91915359,-0.37716089, 0.11360610,
                                 0.85549771,-0.46707264, 0.22353259};

            Matrix volaComp=new Matrix(9,3);
            List<double> lcompValues=new InitializedList<double>(27,0);
            List<double> ltemp = new InitializedList<double>(3, 0);
            lcompValues=compValues.ToList(); 
            //std::copy(compValues, compValues+9*3, volaComp.begin());
            for (int i = 0; i < 9; i++)
            {
                ltemp = lcompValues.GetRange(3*i, 3);
                for (int j = 0; j < 3; j++)
                    volaComp[i, j] = ltemp[j];
            }
            LiborForwardModelProcess process1 = makeProcess();
            LiborForwardModelProcess process2 = makeProcess(volaComp);

            List<double> tmp = process1.fixingTimes();
            TimeGrid grid=new TimeGrid(tmp ,12);

            List<int> location=new List<int>();
            for (int i=0; i < tmp.Count; ++i) {
                location.Add(grid.index(tmp[i])) ;
            }

            // set-up a small Monte-Carlo simulation to price caplets
            // and ratchet caps using a one- and a three factor libor market model

             ulong seed = 42;
             LowDiscrepancy.icInstance = new InverseCumulativeNormal();
             IRNG rsg1 = (IRNG)new LowDiscrepancy().make_sequence_generator(
                                                            process1.factors()*(grid.size()-1), seed);
             IRNG rsg2 = (IRNG)new LowDiscrepancy().make_sequence_generator(
                                                            process2.factors()*(grid.size()-1), seed);

            MultiPathGenerator<IRNG> generator1=new MultiPathGenerator<IRNG> (process1, grid, rsg1, false);
            MultiPathGenerator<IRNG> generator2=new MultiPathGenerator<IRNG> (process2, grid, rsg2, false);

            const int nrTrails = 250000;
            List<GeneralStatistics> stat1 = new InitializedList<GeneralStatistics>(process1.size());
            List<GeneralStatistics> stat2 = new InitializedList<GeneralStatistics>(process2.size());
            List<GeneralStatistics> stat3 = new InitializedList<GeneralStatistics>(process2.size() - 1);
            for (int i=0; i<nrTrails; ++i) {
                Sample<MultiPath> path1 = generator1.next();
                Sample<MultiPath> path2 = generator2.next();

                List<double> rates1=new InitializedList<double>(len);
                List<double> rates2 = new InitializedList<double>(len);
                for (int j=0; j<process1.size(); ++j) {
                    rates1[j] = path1.value[j][location[j]];
                    rates2[j] = path2.value[j][location[j]];
                }

                List<double> dis1 = process1.discountBond(rates1);
                List<double> dis2 = process2.discountBond(rates2);

                for (int k=0; k<process1.size(); ++k) {
                    double accrualPeriod =  process1.accrualEndTimes()[k]
                                        - process1.accrualStartTimes()[k];
                    // caplet payoff function, cap rate at 4%
                    double payoff1 = Math.Max(rates1[k] - 0.04, 0.0) * accrualPeriod;

                    double payoff2 = Math.Max(rates2[k] - 0.04, 0.0) * accrualPeriod;
                    stat1[k].add(dis1[k] * payoff1);
                    stat2[k].add(dis2[k] * payoff2);

                    if (k != 0) {
                        // ratchet cap payoff function
                        double payoff3 =  Math.Max(rates2[k] - (rates2[k-1]+0.0025), 0.0)
                                      * accrualPeriod;
                        stat3[k-1].add(dis2[k] * payoff3);
                    }
                }

            }

            double[] capletNpv = {0.000000000000, 0.000002841629, 0.002533279333,
                                0.009577143571, 0.017746502618, 0.025216116835,
                                0.031608230268, 0.036645683881, 0.039792254012,
                                0.041829864365};

            double[] ratchetNpv = {0.0082644895, 0.0082754754, 0.0082159966,
                                 0.0082982822, 0.0083803357, 0.0084366961,
                                 0.0084173270, 0.0081803406, 0.0079533814};

            for (int k=0; k < process1.size(); ++k) {

                double calculated1 = stat1[k].mean();
                double tolerance1  = stat1[k].errorEstimate();
                double expected    = capletNpv[k];

                if (Math.Abs(calculated1 - expected) > tolerance1) {
                    Assert.Fail("Failed to reproduce expected caplet NPV"
                                + "\n    calculated: " + calculated1
                                + "\n    error int:  " + tolerance1
                                + "\n    expected:   " + expected);
                }

                double calculated2 = stat2[k].mean();
                double tolerance2  = stat2[k].errorEstimate();

                if (Math.Abs(calculated2 - expected) > tolerance2) {
                    Assert.Fail("Failed to reproduce expected caplet NPV"
                                + "\n    calculated: " + calculated2
                                + "\n    error int:  " + tolerance2
                                + "\n    expected:   " + expected);
                }

                if (k != 0) {
                    double calculated3 = stat3[k-1].mean();
                    double tolerance3  = stat3[k-1].errorEstimate();
                    expected    = ratchetNpv[k-1];

                    double refError = 1e-5; // 1e-5. error bars of the reference values

                    if (Math.Abs(calculated3 - expected) > tolerance3 + refError) {
                        Assert.Fail("Failed to reproduce expected caplet NPV"
                                    + "\n    calculated: " + calculated3
                                    + "\n    error int:  " + tolerance3 + refError
                                    + "\n    expected:   " + expected);
                    }
                }
            }
        }
     
        public void T_LiborMarketModelProcess_suite()
        {
            testInitialisation();
            testLambdaBootstrapping();
            testMonteCarloCapletPricing();
        }
    }

}
