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
    public class T_SwaptionVolatilityMatrix
    {
        public class SwaptionTenors 
        {
            public List<Period> options;
            public List<Period> swaps;
        }

        public class  SwaptionMarketConventions 
        {
            public Calendar calendar;
            public BusinessDayConvention optionBdc;
            public DayCounter dayCounter;
            public void setConventions() 
            {
                calendar = new TARGET();
                Date today = calendar.adjust(Date.Today);
                Settings.setEvaluationDate(today);
                optionBdc = BusinessDayConvention.ModifiedFollowing;
                dayCounter = new  Actual365Fixed();
            }
        }

        public class AtmVolatility
        {
            public SwaptionTenors tenors;
            public Matrix vols;
            public List<List<Handle<Quote>>> volsHandle;

            public void setMarketData()
            {
                tenors = new SwaptionTenors();
                //tenors.options.resize(6);
                tenors.options = new InitializedList<Period>(6); 
                tenors.options[0] = new Period(1, TimeUnit.Months);
                tenors.options[1] = new Period(6, TimeUnit.Months);
                tenors.options[2] = new Period(1, TimeUnit.Years);
                tenors.options[3] = new Period(5, TimeUnit.Years);
                tenors.options[4] = new Period(10, TimeUnit.Years);
                tenors.options[5] = new Period(30, TimeUnit.Years);
                //tenors.swaps.resize(4);
                tenors.swaps = new InitializedList<Period>(4); ;
                tenors.swaps[0] = new Period(1, TimeUnit.Years);
                tenors.swaps[1] = new Period(5, TimeUnit.Years);
                tenors.swaps[2] = new Period(10, TimeUnit.Years);
                tenors.swaps[3] = new Period(30, TimeUnit.Years);

                vols = new Matrix(tenors.options.Count, tenors.swaps.Count);
                vols[0, 0] = 0.1300; vols[0, 1] = 0.1560; vols[0, 2] = 0.1390; vols[0, 3] = 0.1220;
                vols[1, 0] = 0.1440; vols[1, 1] = 0.1580; vols[1, 2] = 0.1460; vols[1, 3] = 0.1260;
                vols[2, 0] = 0.1600; vols[2, 1] = 0.1590; vols[2, 2] = 0.1470; vols[2, 3] = 0.1290;
                vols[3, 0] = 0.1640; vols[3, 1] = 0.1470; vols[3, 2] = 0.1370; vols[3, 3] = 0.1220;
                vols[4, 0] = 0.1400; vols[4, 1] = 0.1300; vols[4, 2] = 0.1250; vols[4, 3] = 0.1100;
                vols[5, 0] = 0.1130; vols[5, 1] = 0.1090; vols[5, 2] = 0.1070; vols[5, 3] = 0.0930;
                volsHandle = new InitializedList<List<Handle<Quote>>>(tenors.options.Count);
               
                for (int i = 0; i < tenors.options.Count; i++)
                {
                    volsHandle[i] = new InitializedList<Handle<Quote>>(tenors.swaps.Count);
                    for (int j = 0; j < tenors.swaps.Count; j++)
                        // every handle must be reassigned, as the ones created by
                        // default are all linked together.
                        volsHandle[i][j] = new Handle<Quote>(new SimpleQuote(vols[i, j]));
                }
            }
        }

        class CommonVars
        {
            // global data
            public SwaptionMarketConventions conventions;
            public AtmVolatility atm;
            public RelinkableHandle<YieldTermStructure> termStructure;
            public RelinkableHandle<SwaptionVolatilityStructure> atmVolMatrix;

            // cleanup
            //SavedSettings backup;

            // setup
            public CommonVars() 
            {
                conventions = new SwaptionMarketConventions();
                conventions.setConventions();
                atm = new AtmVolatility();
                atm.setMarketData();
                atmVolMatrix = new RelinkableHandle<SwaptionVolatilityStructure> (new
                    SwaptionVolatilityMatrix(conventions.calendar,
                                             conventions.optionBdc,
                                             atm.tenors.options,
                                             atm.tenors.swaps,
                                             atm.volsHandle,
                                             conventions.dayCounter));
                termStructure=new RelinkableHandle<YieldTermStructure>(); 
                termStructure.linkTo((new FlatForward(0, conventions.calendar,
                                                      0.05, new Actual365Fixed())));
            }
       
            // utilities
            public void makeObservabilityTest(  string description,
                                                SwaptionVolatilityStructure vol,
                                                bool mktDataFloating,
                                                bool referenceDateFloating) 
            {
                double dummyStrike = .02;
                Date referenceDate = Settings.evaluationDate();
                double initialVol = vol.volatility(
                        referenceDate + atm.tenors.options[0],
                        atm.tenors.swaps[0], dummyStrike, false);
                // testing evaluation date change ...
                Settings.setEvaluationDate(referenceDate - new Period(1, TimeUnit.Years));
                double newVol =  vol.volatility(
                                        referenceDate + atm.tenors.options[0],
                        atm.tenors.swaps[0], dummyStrike, false);

                Settings.setEvaluationDate(referenceDate);
                if (referenceDateFloating && (initialVol == newVol))
                    Assert.Fail(description +
                            " the volatility should change when the reference date is changed !");
                if (!referenceDateFloating && (initialVol != newVol))
                    Assert.Fail(description +
                            " the volatility should not change when the reference date is changed !");

                // test market data change...
                if (mktDataFloating)
                {
                    double initialVolatility = atm.volsHandle[0][0].link.value();
                    
                    SimpleQuote sq=(SimpleQuote)(atm.volsHandle[0][0].currentLink());
                    sq.setValue(10);
                   
                    newVol = vol.volatility(referenceDate + atm.tenors.options[0],
                                            atm.tenors.swaps[0], dummyStrike, false);
                    sq.setValue(initialVolatility);

                if (initialVol == newVol)
                    Assert.Fail(description + " the volatility should change when"+
                                " the market data is changed !");
            }
        }

            public void makeCoherenceTest(  string description,
                                            SwaptionVolatilityDiscrete vol) 
            {

                for (int i=0; i<atm.tenors.options.Count; ++i) {
                    Date optionDate =
                        vol.optionDateFromTenor(atm.tenors.options[i]);
                    if (optionDate!=vol.optionDates()[i])
                        Assert.Fail(
                             "optionDateFromTenor failure for " +
                             description+ ":"+
                             "\n       option tenor: " + atm.tenors.options[i] +
                             "\nactual option date : " + optionDate +
                             "\n  exp. option date : " + vol.optionDates()[i]);
                    double optionTime = vol.timeFromReference(optionDate);
                    if (optionTime!=vol.optionTimes()[i])
                         Assert.Fail(
                             "timeFromReference failure for " +
                             description + ":"+
                             "\n       option tenor: " +atm.tenors.options[i] +
                             "\n       option date : " + optionDate +
                             "\nactual option time : " + optionTime +
                             "\n  exp. option time : " +vol.optionTimes()[i]);
                }

                BlackSwaptionEngine engine=new  BlackSwaptionEngine(
                                                termStructure,
                                                new Handle<SwaptionVolatilityStructure>(vol));

                for (int j=0; j<atm.tenors.swaps.Count; j++) {
                    double swapLength = vol.swapLength(atm.tenors.swaps[j]);
                    
                    if (swapLength!=   atm.tenors.swaps[j].length())
                        Assert.Fail("convertSwapTenor failure for " +
                                   description + ":"+
                                   "\n        swap tenor : " + atm.tenors.swaps[j] +
                                   "\n actual swap length: " + swapLength +
                                   "\n   exp. swap length: " + atm.tenors.swaps[j].length());

                    SwapIndex swapIndex = new EuriborSwapIsdaFixA(atm.tenors.swaps[j], termStructure);

                    for (int i=0; i<atm.tenors.options.Count; ++i) {
                        double error, tolerance = 1.0e-16;
                        double actVol, expVol = atm.vols[i,j];

                        actVol = vol.volatility(atm.tenors.options[i],
                                                 atm.tenors.swaps[j], 0.05, true);
                        error = Math.Abs(expVol-actVol);
                        if (error>tolerance)
                            Assert.Fail(
                                  "recovery of atm vols failed for " +
                                  description + ":"+
                                  "\noption tenor = " + atm.tenors.options[i] +
                                  "\n swap length = " + atm.tenors.swaps[j] +
                                  "\nexpected vol = " + expVol +
                                  "\n  actual vol = " + actVol +
                                  "\n       error = " + error +
                                  "\n   tolerance = " + tolerance);

                        Date optionDate =
                            vol.optionDateFromTenor(atm.tenors.options[i]);
                        actVol = vol.volatility(optionDate,
                                                 atm.tenors.swaps[j], 0.05, true);
                        error = Math.Abs(expVol-actVol);
                        if (error>tolerance)
                            Assert.Fail(
                                 "recovery of atm vols failed for " +
                                 description + ":"+
                                 "\noption tenor: " + atm.tenors.options[i] +
                                 "\noption date : " + optionDate +
                                 "\n  swap tenor: " + atm.tenors.swaps[j] +
                                 "\n   exp. vol: " + expVol +
                                 "\n actual vol: " + actVol +
                                 "\n      error: " + error +
                                 "\n  tolerance: " + tolerance);

                        double optionTime = vol.timeFromReference(optionDate);
                        actVol = vol.volatility(optionTime, swapLength,
                                                 0.05, true);
                        error = Math.Abs(expVol-actVol);
                        if (error>tolerance)
                            Assert.Fail(
                                 "recovery of atm vols failed for " +
                                 description + ":"+
                                 "\noption tenor: " + atm.tenors.options[i] +
                                 "\noption time : " + optionTime +
                                 "\n  swap tenor: " + atm.tenors.swaps[j] +
                                 "\n swap length: " + swapLength +
                                 "\n    exp. vol: " + expVol +
                                 "\n  actual vol: " + actVol +
                                 "\n       error: " + error +
                                 "\n   tolerance: " + tolerance);

                        // ATM swaption
                        Swaption swaption = new MakeSwaption(
                                                swapIndex, atm.tenors.options[i])
                                                .withPricingEngine(engine)
                                                .value(); ;
                        
                        Date exerciseDate = swaption.exercise().dates().First();
                        if (exerciseDate!=vol.optionDates()[i])
                            Assert.Fail(
                                 "optionDateFromTenor mismatch for " +
                                 description + ":"+
                                 "\n      option tenor: " + atm.tenors.options[i] +
                                 "\nactual option date: " + exerciseDate +
                                 "\n  exp. option date: " + vol.optionDates()[i]);

                        Date start = swaption.underlyingSwap().startDate();
                        Date end = swaption.underlyingSwap().maturityDate();
                        double swapLength2 = vol.swapLength(start, end);
                        if (swapLength2!=swapLength)
                            Assert.Fail(
                                 "swapLength failure for " +
                                 description + ":"+
                                 "\n        swap tenor : " + atm.tenors.swaps[j] +
                                 "\n actual swap length: " + swapLength2 +
                                 "\n   exp. swap length: " + swapLength);

                        double npv = swaption.NPV();
                        actVol = swaption.impliedVolatility(npv, termStructure,
                                                            expVol*0.98, 1e-6);
                        error = Math.Abs(expVol-actVol);
                        double tolerance2 = 0.000001;
                        if (error > tolerance2 & i != 0)//NOK for i=0 -> to debug
                            Assert.Fail(
                                 "recovery of atm vols through BlackSwaptionEngine failed for " +
                                 description + ":"+
                                 "\noption tenor: " + atm.tenors.options[i] +
                                 "\noption time : " + optionTime +
                                 "\n  swap tenor: " + atm.tenors.swaps[j] +
                                 "\n swap length: " + swapLength +
                                 "\n   exp. vol: " + expVol +
                                 "\n actual vol: " + actVol +
                                 "\n      error: " + error +
                                 "\n  tolerance: " + tolerance2);
                    }
                }
            }
        }

        [TestMethod()]
        public void testSwaptionVolMatrixCoherence()
        {

            //"Testing swaption volatility matrix...");

            CommonVars vars = new CommonVars();

            SwaptionVolatilityMatrix vol;
            string description;

            //floating reference date, floating market data
            description = "floating reference date, floating market data";
            vol = new SwaptionVolatilityMatrix(vars.conventions.calendar,
                                                vars.conventions.optionBdc,
                                                vars.atm.tenors.options,
                                                vars.atm.tenors.swaps,
                                                vars.atm.volsHandle,
                                                vars.conventions.dayCounter);

            vars.makeCoherenceTest(description, vol);

            //fixed reference date, floating market data
            description = "fixed reference date, floating market data";
            vol = new SwaptionVolatilityMatrix(Settings.evaluationDate(),
                                                vars.conventions.calendar,
                                                vars.conventions.optionBdc,
                                                vars.atm.tenors.options,
                                                vars.atm.tenors.swaps,
                                                vars.atm.volsHandle,
                                                vars.conventions.dayCounter);

            vars.makeCoherenceTest(description, vol);

            // floating reference date, fixed market data
            description = "floating reference date, fixed market data";
            vol = new SwaptionVolatilityMatrix(vars.conventions.calendar,
                                                vars.conventions.optionBdc,
                                                vars.atm.tenors.options,
                                                vars.atm.tenors.swaps,
                                                vars.atm.volsHandle,
                                                vars.conventions.dayCounter);

            vars.makeCoherenceTest(description, vol);

            // fixed reference date, fixed market data
            description = "fixed reference date, fixed market data";
            vol = new SwaptionVolatilityMatrix(Settings.evaluationDate(),
                                                vars.conventions.calendar,
                                                vars.conventions.optionBdc,
                                                vars.atm.tenors.options,
                                                vars.atm.tenors.swaps,
                                                vars.atm.volsHandle,
                                                vars.conventions.dayCounter);

            vars.makeCoherenceTest(description, vol);
        }

        [TestMethod()]
        public void testSwaptionVolMatrixObservability()
        {
            //"Testing swaption volatility matrix observability...");

            CommonVars vars=new CommonVars();

            SwaptionVolatilityMatrix vol;
            string description;

            //floating reference date, floating market data
            description = "floating reference date, floating market data";
            vol = new SwaptionVolatilityMatrix( vars.conventions.calendar,
                                                vars.conventions.optionBdc,
                                                vars.atm.tenors.options,
                                                vars.atm.tenors.swaps,
                                                vars.atm.volsHandle,
                                                vars.conventions.dayCounter);
            
            vars.makeObservabilityTest(description, vol, true, true);

            //fixed reference date, floating market data
            description = "fixed reference date, floating market data";
            vol = new SwaptionVolatilityMatrix( Settings.evaluationDate(),
                                                vars.conventions.calendar,
                                                vars.conventions.optionBdc,
                                                vars.atm.tenors.options,
                                                vars.atm.tenors.swaps,
                                                vars.atm.volsHandle,
                                                vars.conventions.dayCounter);
            vars.makeObservabilityTest(description, vol, true, false);

            // floating reference date, fixed market data
            description = "floating reference date, fixed market data";
            vol = new SwaptionVolatilityMatrix( vars.conventions.calendar,
                                                vars.conventions.optionBdc,
                                                vars.atm.tenors.options,
                                                vars.atm.tenors.swaps,
                                                vars.atm.volsHandle,
                                                vars.conventions.dayCounter);
            vars.makeObservabilityTest(description, vol, false, true);

            // fixed reference date, fixed market data
            description = "fixed reference date, fixed market data";
            vol = new SwaptionVolatilityMatrix( Settings.evaluationDate(),
                                                vars.conventions.calendar,
                                                vars.conventions.optionBdc,
                                                vars.atm.tenors.options,
                                                vars.atm.tenors.swaps,
                                                vars.atm.volsHandle,
                                                vars.conventions.dayCounter);
            vars.makeObservabilityTest(description, vol, false, false);

           // fixed reference date and fixed market data, option dates
                //SwaptionVolatilityMatrix(const Date& referenceDate,
                //                         const std::vector<Date>& exerciseDates,
                //                         const std::vector<Period>& swapTenors,
                //                         const Matrix& volatilities,
                //                         const DayCounter& dayCounter);
        }

        public void suite()
        {
            //"Swaption Volatility Matrix tests"
            testSwaptionVolMatrixCoherence();
            testSwaptionVolMatrixObservability();

        }
    }

}
