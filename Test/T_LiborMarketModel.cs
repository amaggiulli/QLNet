/*
 Copyright (C) 2009 Philippe Real (ph_real@hotmail.com)
 Copyright (C) 2008-2014 Andrea Maggiulli (a.maggiulli@gmail.com)
  
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
    public class T_LiborMarketModel
    {

        IborIndex makeIndex(List<Date> dates,
                            List<double> rates)
        {
            DayCounter dayCounter = new Actual360();

            RelinkableHandle<YieldTermStructure> termStructure = new RelinkableHandle<YieldTermStructure>(); ;
            IborIndex index = new Euribor6M(termStructure);

            Date todaysDate =
            index.fixingCalendar().adjust(new Date(4, 9, 2005));
            Settings.setEvaluationDate(todaysDate);

            dates[0] = index.fixingCalendar().advance(todaysDate,
                                                   index.fixingDays(), TimeUnit.Days);
            Linear Interpolator = new Linear();
            termStructure.linkTo(new InterpolatedZeroCurve<Linear>(dates, rates, dayCounter, Interpolator));

            return index;
        }

        IborIndex makeIndex()
        {
            List<Date> dates=new List<Date>();
            List<double> rates = new List<double>();
            dates.Add(new Date(4, 9, 2005));
            dates.Add(new Date(4, 9, 2018));
            rates.Add(0.039);
            rates.Add(0.041);

            return makeIndex(dates, rates);
        }

        OptionletVolatilityStructure makeCapVolCurve(Date todaysDate) 
        {
            double[] vols = {14.40, 17.15, 16.81, 16.64, 16.17,
                                 15.78, 15.40, 15.21, 14.86};

            List<Date> dates=new List<Date>() ;
            List<double> capletVols=new List<double>();
            LiborForwardModelProcess process=
                                   new LiborForwardModelProcess(10, makeIndex());

            for (int i=0; i < 9; ++i) {
                capletVols.Add(vols[i]/100);
                dates.Add(process.fixingDates()[i+1]);
            }

            return new CapletVarianceCurve(todaysDate, dates,
                                           capletVols,new Actual360());
        }
        
        [TestMethod()]
        public void testSimpleCovarianceModels() 
        {
            //"Testing simple covariance models...";

            //SavedSettings backup;

            const int size = 10;
            const double tolerance = 1e-14;
            int i;

            LmCorrelationModel corrModel=new LmExponentialCorrelationModel(size, 0.1);

            Matrix recon = corrModel.correlation(0.0,null)
                - corrModel.pseudoSqrt(0.0,null)*Matrix.transpose(corrModel.pseudoSqrt(0.0,null));

            for (i=0; i<size; ++i) {
                for (int j=0; j<size; ++j) {
                    if (Math.Abs(recon[i,j]) > tolerance)
                        Assert.Fail("Failed to reproduce correlation matrix"
                                    + "\n    calculated: " + recon[i,j]
                                    + "\n    expected:   " + 0);
                }
            }

            List<double> fixingTimes=new InitializedList<double>(size);
            for (i=0; i<size; ++i) {
                fixingTimes[i] = 0.5*i;
            }

            const double a=0.2;
            const double b=0.1;
            const double c=2.1;
            const double d=0.3;

            LmVolatilityModel volaModel=new LmLinearExponentialVolatilityModel(fixingTimes, a, b, c, d);

            LfmCovarianceProxy covarProxy=new LfmCovarianceProxy(volaModel, corrModel);

            LiborForwardModelProcess process=new LiborForwardModelProcess(size, makeIndex());

            LiborForwardModel liborModel=new LiborForwardModel(process, volaModel, corrModel);

            for (double t=0; t<4.6; t+=0.31) {
                recon = covarProxy.covariance(t,null)
                    - covarProxy.diffusion(t,null)*Matrix.transpose(covarProxy.diffusion(t,null));

                for (int k=0; k<size; ++k) {
                    for (int j=0; j<size; ++j) {
                        if (Math.Abs(recon[k,j]) > tolerance)
                            Assert.Fail("Failed to reproduce correlation matrix"
                                        + "\n    calculated: " + recon[k,j]
                                        + "\n    expected:   " + 0);
                    }
                }

                Vector volatility = volaModel.volatility(t,null);

                for (int k=0; k<size; ++k) {
                    double expected = 0;
                    if (k>2*t) {
                        double T = fixingTimes[k];
                        expected=(a*(T-t)+d)*Math.Exp(-b*(T-t)) + c;
                    }

                    if (Math.Abs(expected - volatility[k]) > tolerance)
                        Assert.Fail("Failed to reproduce volatities"
                                    + "\n    calculated: " + volatility[k]
                                    + "\n    expected:   " + expected);
                }
            }
        }

        [TestMethod()]
        public void testCapletPricing() 
        {
            //"Testing caplet pricing...";

            //SavedSettings backup;

            const int size = 10;
            #if QL_USE_INDEXED_COUPON
            const double tolerance = 1e-5;
            #else
            const double tolerance = 1e-12;
            #endif

            IborIndex index = makeIndex();
            LiborForwardModelProcess process=new LiborForwardModelProcess(size, index);

            // set-up pricing engine
            OptionletVolatilityStructure capVolCurve = makeCapVolCurve(Settings.evaluationDate());

            Vector variances = new LfmHullWhiteParameterization(process, capVolCurve).covariance(0.0,null).diagonal();

            LmVolatilityModel volaModel = new LmFixedVolatilityModel(Vector.Sqrt(variances),process.fixingTimes());

            LmCorrelationModel corrModel = new LmExponentialCorrelationModel(size, 0.3);

            IAffineModel model = (IAffineModel)(new LiborForwardModel(process, volaModel, corrModel));

            Handle<YieldTermStructure> termStructure = process.index().forwardingTermStructure();

            AnalyticCapFloorEngine engine1 = new AnalyticCapFloorEngine(model, termStructure);

            Cap cap1 = new Cap( process.cashFlows(),
                                new InitializedList<double>(size, 0.04));
            cap1.setPricingEngine(engine1);

            const double expected = 0.015853935178;
            double calculated = cap1.NPV();

            if (Math.Abs(expected - calculated) > tolerance)
                Assert.Fail("Failed to reproduce npv"
                            + "\n    calculated: " + calculated
                            + "\n    expected:   " + expected);
        }
    
        [TestMethod()]
        public void testCalibration()
        {
            //("Testing calibration of a Libor forward model...");

            //SavedSettings backup;

            const int size = 14;
            const double tolerance = 8e-3;

            double[] capVols = {0.145708,0.158465,0.166248,0.168672,
                                    0.169007,0.167956,0.166261,0.164239,
                                    0.162082,0.159923,0.157781,0.155745,
                                    0.153776,0.151950,0.150189,0.148582,
                                    0.147034,0.145598,0.144248};

            double[] swaptionVols = {0.170595, 0.166844, 0.158306, 0.147444,
                                         0.136930, 0.126833, 0.118135, 0.175963,
                                         0.166359, 0.155203, 0.143712, 0.132769,
                                         0.122947, 0.114310, 0.174455, 0.162265,
                                         0.150539, 0.138734, 0.128215, 0.118470,
                                         0.110540, 0.169780, 0.156860, 0.144821,
                                         0.133537, 0.123167, 0.114363, 0.106500,
                                         0.164521, 0.151223, 0.139670, 0.128632,
                                         0.119123, 0.110330, 0.103114, 0.158956,
                                         0.146036, 0.134555, 0.124393, 0.115038,
                                         0.106996, 0.100064};

            IborIndex index = makeIndex();
            LiborForwardModelProcess process = new LiborForwardModelProcess(size, index);
            Handle<YieldTermStructure> termStructure = index.forwardingTermStructure();

            // set-up the model
            LmVolatilityModel volaModel = new LmExtLinearExponentialVolModel(process.fixingTimes(),
                                                                             0.5,0.6,0.1,0.1);

            LmCorrelationModel corrModel = new LmLinearExponentialCorrelationModel(size, 0.5, 0.8);

            LiborForwardModel  model = new LiborForwardModel(process, volaModel, corrModel);

            int swapVolIndex = 0;
            DayCounter dayCounter = index.forwardingTermStructure().link.dayCounter();

            // set-up calibration helper
            List<CalibrationHelper> calibrationHelper = new List<CalibrationHelper>();

            int i;
            for (i=2; i < size; ++i) {
                Period maturity = i*index.tenor();
                Handle<Quote> capVol = new Handle<Quote>(new SimpleQuote(capVols[i-2]));

                CalibrationHelper caphelper = new CapHelper(maturity, capVol, index,Frequency.Annual,
                                  index.dayCounter(), true, termStructure, CalibrationHelper.CalibrationErrorType.ImpliedVolError);

                caphelper.setPricingEngine(new AnalyticCapFloorEngine(model, termStructure));

                calibrationHelper.Add(caphelper);

                if (i<= size/2) {
                    // add a few swaptions to test swaption calibration as well
                    for (int j=1; j <= size/2; ++j) {
                        Period len = j*index.tenor();
                        Handle<Quote> swaptionVol =  new Handle<Quote>(
                                                         new SimpleQuote(swaptionVols[swapVolIndex++]));

                        CalibrationHelper swaptionHelper =
                            new SwaptionHelper(maturity, len, swaptionVol, index,
                                               index.tenor(), dayCounter,
                                               index.dayCounter(),
															  termStructure, CalibrationHelper.CalibrationErrorType.ImpliedVolError );

                        swaptionHelper.setPricingEngine(new LfmSwaptionEngine(model,termStructure));

                        calibrationHelper.Add(swaptionHelper);
                    }
                }
            }

            LevenbergMarquardt om = new LevenbergMarquardt(1e-6, 1e-6, 1e-6);
            //ConjugateGradient gc = new ConjugateGradient();
       
            model.calibrate(calibrationHelper,
                            om, 
                            new EndCriteria(2000, 100, 1e-6, 1e-6, 1e-6),
                            new Constraint(), 
                            new List<double>());

            // measure the calibration error
            double calculated = 0.0;
            for (i=0; i<calibrationHelper.Count ; ++i) {
                double diff = calibrationHelper[i].calibrationError();
                calculated += diff*diff;
            }

            if (Math.Sqrt(calculated) > tolerance)
                Assert.Fail("Failed to calibrate libor forward model"
                            + "\n    calculated diff: " + Math.Sqrt(calculated)
                            + "\n    expected : smaller than  " + tolerance);
        }

        [TestMethod()]
        public void testSwaptionPricing() 
        {
            //"Testing forward swap and swaption pricing...");

            //SavedSettings backup;

            const int size  = 10;
            const int steps = 8*size;
            #if QL_USE_INDEXED_COUPON
            const double tolerance = 1e-6;
            #else
            const double tolerance = 1e-12;
            #endif

            List<Date> dates = new List<Date>();
            List<double> rates = new List<double>();
            dates.Add(new Date(4,9,2005));
            dates.Add(new Date(4,9,2011));
            rates.Add(0.04);
            rates.Add(0.08);

            IborIndex index = makeIndex(dates, rates);

            LiborForwardModelProcess process = new LiborForwardModelProcess(size, index);

            LmCorrelationModel corrModel = new LmExponentialCorrelationModel(size, 0.5);

            LmVolatilityModel volaModel = new LmLinearExponentialVolatilityModel(process.fixingTimes(),
                                                                                0.291, 1.483, 0.116, 0.00001);

           // set-up pricing engine
            process.setCovarParam((LfmCovarianceParameterization)
                                       new LfmCovarianceProxy(volaModel, corrModel));

            // set-up a small Monte-Carlo simulation to price swations
            List<double> tmp = process.fixingTimes();
           
            TimeGrid grid=new TimeGrid(tmp ,steps);

            List<int> location=new List<int>();
            for (int i=0; i < tmp.Count; ++i) {
                location.Add(grid.index(tmp[i])) ;
            }
            
            ulong seed=42;
            const int nrTrails = 5000;
            LowDiscrepancy.icInstance = new InverseCumulativeNormal();

            IRNG rsg = (InverseCumulativeRsg<RandomSequenceGenerator<MersenneTwisterUniformRng>
                                                                    ,InverseCumulativeNormal>)
            new PseudoRandom().make_sequence_generator(process.factors()*(grid.size()-1),seed);



            MultiPathGenerator<IRNG> generator=new MultiPathGenerator<IRNG>(process,
                                                                            grid,
                                                                            rsg, false);

            LiborForwardModel liborModel = new LiborForwardModel(process, volaModel, corrModel);

            Calendar calendar = index.fixingCalendar();
            DayCounter dayCounter = index.forwardingTermStructure().link.dayCounter();
            BusinessDayConvention convention = index.businessDayConvention();

            Date settlement = index.forwardingTermStructure().link.referenceDate();

            SwaptionVolatilityMatrix m = liborModel.getSwaptionVolatilityMatrix();

            for (int i=1; i < size; ++i) {
                for (int j=1; j <= size-i; ++j) {
                    Date fwdStart    = settlement + new Period(6*i, TimeUnit.Months);
                    Date fwdMaturity = fwdStart + new Period(6*j, TimeUnit.Months);

                    Schedule schedule =new Schedule(fwdStart, fwdMaturity, index.tenor(), calendar,
                                       convention, convention, DateGeneration.Rule.Forward, false);

                    double swapRate  = 0.0404;
                    VanillaSwap forwardSwap = new VanillaSwap(VanillaSwap.Type.Receiver, 1.0,
                                                                schedule, swapRate, dayCounter,
                                                                schedule, index, 0.0, index.dayCounter());
                    forwardSwap.setPricingEngine(new DiscountingSwapEngine(index.forwardingTermStructure()));

                    // check forward pricing first
                    double expected = forwardSwap.fairRate();
                    double calculated = liborModel.S_0(i-1,i+j-1);

                    if (Math.Abs(expected - calculated) > tolerance)
                        Assert.Fail("Failed to reproduce fair forward swap rate"
                                    + "\n    calculated: " + calculated
                                    + "\n    expected:   " + expected);

                    swapRate = forwardSwap.fairRate();
                    forwardSwap = 
                        new VanillaSwap(VanillaSwap.Type.Receiver, 1.0,
                                        schedule, swapRate, dayCounter,
                                        schedule, index, 0.0, index.dayCounter());
                    forwardSwap.setPricingEngine(new DiscountingSwapEngine(index.forwardingTermStructure()));

                    if (i == j && i<=size/2) {
                        IPricingEngine engine =
                            new LfmSwaptionEngine(liborModel, index.forwardingTermStructure());
                        Exercise exercise =
                            new EuropeanExercise(process.fixingDates()[i]);

                        Swaption swaption =
                            new Swaption(forwardSwap, exercise);
                        swaption.setPricingEngine(engine);

                        GeneralStatistics stat = new GeneralStatistics();

                        for (int n=0; n<nrTrails; ++n) {
                            Sample<MultiPath> path = (n%2!=0) ? generator.antithetic()
                                                     : generator.next();
                            
                            //Sample<MultiPath> path = generator.next();
                            List<double> rates_ = new InitializedList<double>(size);
                            for (int k=0; k<process.size(); ++k) {
                                rates_[k] = path.value[k][location[i]];
                            }
                            List<double> dis = process.discountBond(rates_);

                            double npv=0.0;
                            for (int k=i; k < i+j; ++k) {
                                npv += (swapRate - rates_[k])
                                       * (  process.accrualEndTimes()[k]
                                          - process.accrualStartTimes()[k])*dis[k];
                            }
                            stat.add(Math.Max(npv, 0.0));
                        }

                        if (Math.Abs(swaption.NPV() - stat.mean())
                            > stat.errorEstimate()*2.35)
                            Assert.Fail("Failed to reproduce swaption npv"
                                        + "\n    calculated: " + stat.mean()
                                        + "\n    expected:   " + swaption.NPV());
                    }
                }
            }
        }
    }
}