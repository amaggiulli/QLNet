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
    public class T_Swaption
    {
        public Period[] exercises = new Period[] { new Period(1, TimeUnit.Years),
                                            new Period(2, TimeUnit.Years),
                                            new Period(3, TimeUnit.Years),
                                            new Period(5, TimeUnit.Years), 
                                            new Period(7, TimeUnit.Years),
                                            new Period(10, TimeUnit.Years) };

        public Period[] lengths = new Period[] { new Period(1, TimeUnit.Years),
                                            new Period(2, TimeUnit.Years),
                                            new Period(3, TimeUnit.Years),
                                            new Period(5, TimeUnit.Years), 
                                            new Period(7, TimeUnit.Years),
                                            new Period(10, TimeUnit.Years),
                                            new Period(15, TimeUnit.Years),
                                            new Period(20, TimeUnit.Years) };

        public VanillaSwap.Type[] type = new VanillaSwap.Type[] { VanillaSwap.Type.Receiver, VanillaSwap.Type.Payer };


        public class CommonVars
        {
            // global data
            public Date today, settlement;
            public double nominal;
            public Calendar calendar;
            public BusinessDayConvention fixedConvention, floatingConvention;
            public Frequency fixedFrequency;
            public DayCounter fixedDayCount;
            public Period floatingTenor;
            public IborIndex index;
            public int settlementDays;
            public RelinkableHandle<YieldTermStructure> termStructure = new RelinkableHandle<YieldTermStructure>();

            // cleanup
            // SavedSettings backup;

            // utilities
            public Swaption makeSwaption(VanillaSwap swap,Date exercise,double volatility,Settlement.Type settlementType)
            {
                Handle<Quote> vol=new Handle <Quote>(new SimpleQuote(volatility));
                IPricingEngine engine=new BlackSwaptionEngine(termStructure, vol);
                Swaption result=new Swaption(swap,new EuropeanExercise(exercise),settlementType);
                result.setPricingEngine(engine);
                return result;
            }

            public Swaption makeSwaption(VanillaSwap swap,Date exercise,double volatility)
            {
                Settlement.Type settlementType= Settlement.Type.Physical;
                Handle<Quote> vol=new Handle <Quote>(new SimpleQuote(volatility));
                IPricingEngine engine=new BlackSwaptionEngine(termStructure, vol);
                Swaption result=new Swaption(swap,new EuropeanExercise(exercise),settlementType);
                result.setPricingEngine(engine);
                return result;
            }

            public IPricingEngine makeEngine(double volatility) 
            {
                Handle<Quote> h = new Handle < Quote >( new SimpleQuote(volatility));
                return (IPricingEngine)(new BlackSwaptionEngine(termStructure, h));
            }

            public CommonVars()
            {
                settlementDays = 2;
                nominal = 1000000.0;
                fixedConvention = BusinessDayConvention.Unadjusted;
                
                fixedFrequency = Frequency.Annual;
                fixedDayCount = new Thirty360();

                index =new Euribor6M(termStructure);
                floatingConvention = index.businessDayConvention();
                floatingTenor = index.tenor();
                calendar = index.fixingCalendar();
                today = calendar.adjust(Date.Today);
                Settings.setEvaluationDate(today);
                settlement = calendar.advance(today, settlementDays, TimeUnit.Days);

                termStructure.linkTo(Utilities.flatRate(settlement, 0.05, new Actual365Fixed()));
            }
        }

        [TestMethod()]
        public void testStrikeDependency()
        {
            //("Testing swaption dependency on strike......");

            CommonVars vars = new CommonVars();
            double[] strikes = new double[] { 0.03, 0.04, 0.05, 0.06, 0.07 };

            for (int i = 0; i <  exercises.Length; i++)  {
                for (int j = 0; j < lengths.Length; j++) {
                    for (int k=0; k < type.Length ; k++) {
                        Date exerciseDate = vars.calendar.advance(vars.today,
                                                          exercises[i]);
                        Date startDate = vars.calendar.advance(exerciseDate,
                                         vars.settlementDays, TimeUnit.Days);
                        // store the results for different rates...
                        List<double> values = new InitializedList<double>(strikes.Length);
                        List<double> values_cash = new InitializedList<double>(strikes.Length);
                        double vol = 0.20;
                        
                        for (int l=0; l< strikes.Length ; l++) {
                            VanillaSwap swap    = new MakeVanillaSwap(lengths[j], vars.index, strikes[l])
                                                    .withEffectiveDate(startDate)
                                                    .withFloatingLegSpread(0.0)
                                                    .withType(type[k]);
                            Swaption swaption   = vars.makeSwaption(swap,exerciseDate,vol);
                            // FLOATING_POINT_EXCEPTION
                            values[l]=swaption.NPV();
                            Swaption swaption_cash = vars.makeSwaption( swap,exerciseDate,vol,
                                                                        Settlement.Type.Cash);
                            values_cash[l]=swaption_cash.NPV();
                        }
                        
                        // and check that they go the right way
                        if (type[k]==VanillaSwap.Type.Payer) {
                            for (int z = 0; z < values.Count - 1; z++) 
                            {
                                if( values[z]<values[z+1]){
                                Assert.Fail("NPV of Payer swaption with delivery settlement"+
                                            "is increasing with the strike:" +
                                            "\noption tenor: " + exercises[i] +
                                            "\noption date:  " + exerciseDate +
                                            "\nvolatility:   " + vol +
                                            "\nswap tenor:   " + lengths[j] +
                                            "\nvalue:        " + values[z  ] +" at strike: " + strikes[z  ] +
                                            "\nvalue:        " + values[z+1] + " at strike: " + strikes[z+1]);
                                }
                            }
                            for (int z = 0; z < values_cash.Count - 1; z++)
                            {
                                if (values_cash[z] < values_cash[z + 1])
                                {
                                    Assert.Fail("NPV of Payer swaption with cash settlement" +
                                        "is increasing with the strike:" +
                                        "\noption tenor: " + exercises[i] +
                                        "\noption date:  " + exerciseDate +
                                        "\nvolatility:   " + vol +
                                        "\nswap tenor:   " + lengths[j] +
                                        "\nvalue:        " + values_cash[z] + " at strike: " + strikes[z] +
                                        "\nvalue:        " + values_cash[z + 1] + " at strike: " + strikes[z + 1]);
                                }
                            }
                        }
                        else {
                            for (int z = 0; z < values.Count - 1; z++){
                                if (values[z] > values[z+1]){
                                    Assert.Fail("NPV of Receiver swaption with delivery settlement" +
                                                "is increasing with the strike:" +
                                                "\noption tenor: " + exercises[i] +
                                                "\noption date:  " + exerciseDate +
                                                "\nvolatility:   " + vol +
                                                "\nswap tenor:   " + lengths[j] +
                                                "\nvalue:        " + values[z] + " at strike: " + strikes[z] +
                                                "\nvalue:        " + values[z + 1] + " at strike: " + strikes[z + 1]);
                                }
                            }
                            for (int z = 0; z < values_cash.Count - 1; z++)
                            {
                                if (values[z] > values[z+1])
                                {
                                    Assert.Fail("NPV of Receiver swaption with cash settlement" +
                                        "is increasing with the strike:" +
                                        "\noption tenor: " + exercises[i] +
                                        "\noption date:  " + exerciseDate +
                                        "\nvolatility:   " + vol +
                                        "\nswap tenor:   " + lengths[j] +
                                        "\nvalue:        " + values_cash[z] + " at strike: " + strikes[z] +
                                        "\nvalue:        " + values_cash[z + 1] + " at strike: " + strikes[z + 1]);
                                }
                            }
                        }
                    }
                }
            }
        }

        [TestMethod()]
        public void testSpreadDependency() 
        {
            //"Testing swaption dependency on spread...";

            CommonVars vars = new CommonVars();

            double[] spreads = { -0.002, -0.001, 0.0, 0.001, 0.002 };

            for (int i=0; i<exercises.Length ; i++) {
                for (int j=0; j<lengths.Length ; j++) {
                    for (int k=0; k<type.Length ; k++) {
                        Date exerciseDate = vars.calendar.advance(vars.today,
                                                                  exercises[i]);
                        Date startDate =
                            vars.calendar.advance(exerciseDate,
                                                  vars.settlementDays,TimeUnit.Days);
                        // store the results for different rates...
                        List<double> values=new InitializedList<double>(spreads.Length);
                        List<double> values_cash = new InitializedList<double>(spreads.Length);
                        for (int l=0; l<spreads.Length; l++) {
                             VanillaSwap swap =
                               new MakeVanillaSwap(lengths[j], vars.index, 0.06)
                                        .withEffectiveDate(startDate)
                                        .withFloatingLegSpread(spreads[l])
                                        .withType(type[k]);
                             Swaption swaption =
                                vars.makeSwaption(swap,exerciseDate,0.20);
                            // FLOATING_POINT_EXCEPTION
                            values[l]=swaption.NPV();
                            Swaption swaption_cash =
                                vars.makeSwaption(swap,exerciseDate,0.20,
                                                  Settlement.Type.Cash);
                            values_cash[l]=swaption_cash.NPV();
                        }
                        // and check that they go the right way
                        if (type[k]==VanillaSwap.Type.Payer) {
                            for (int n = 0; n < spreads.Length - 1; n++)
                            {
                                if (values[n] > values[n + 1])
                                    Assert.Fail("NPV is decreasing with the spread " +
                                        "in a payer swaption (physical delivered):" +
                                        "\nexercise date: " + exerciseDate +
                                        "\nlength:        " + lengths[j] +
                                        "\nvalue:         " + values[n] + " for spread: " + spreads[n] +
                                        "\nvalue:         " + values[n + 1] + " for spread: " + spreads[n + 1]);
                                
                                if (values_cash[n] > values_cash[n + 1])
                                    Assert.Fail("NPV is decreasing with the spread " +
                                        "in a payer swaption (cash delivered):" +
                                        "\nexercise date: " + exerciseDate +
                                        "\nlength: " + lengths[j] +
                                        "\nvalue:  " + values_cash[n] + " for spread: " + spreads[n] +
                                        "\nvalue:  " + values_cash[n + 1] + " for spread: " + spreads[n + 1]);
                            }
                        } 
                        else 
                        {
                            for (int n = 0; n < spreads.Length - 1; n++)
                            {
                                if (values[n] < values[n + 1])
                                    Assert.Fail("NPV is increasing with the spread " +
                                        "in a receiver swaption (physical delivered):" +
                                        "\nexercise date: " + exerciseDate +
                                        "\nlength: " + lengths[j] +
                                        "\nvalue:  " + values[n] + " for spread: " + spreads[n] +
                                        "\nvalue:  " + values[n + 1] + " for spread: " + spreads[n + 1]);
                                
                                if (values_cash[n] < values_cash[n+1])
                                Assert.Fail("NPV is increasing with the spread " +
                                    "in a receiver swaption (cash delivered):" +
                                    "\nexercise date: " + exerciseDate +
                                    "\nlength: " + lengths[j] +
                                    "\nvalue:  " + values_cash[n  ] + " for spread: " + spreads[n] +
                                    "\nvalue:  " + values_cash[n+1] + " for spread: " + spreads[n+1]);
                            }
                        }
                    }
                }
            }
        }
    
        [TestMethod()]
        public void testSpreadTreatment() 
        {
            //"Testing swaption treatment of spread...";

            CommonVars vars = new CommonVars();

            double[] spreads = { -0.002, -0.001, 0.0, 0.001, 0.002 };

            for (int i=0; i<exercises.Length; i++) {
                for (int j=0; j<lengths.Length ; j++) {
                    for (int k=0; k<type.Length ; k++) {
                        Date exerciseDate = vars.calendar.advance(vars.today,
                                                                  exercises[i]);
                        Date startDate =
                            vars.calendar.advance(exerciseDate,
                                                  vars.settlementDays,TimeUnit.Days);
                        for (int l=0; l<spreads.Length ; l++) {
                            VanillaSwap swap =
                                new MakeVanillaSwap(lengths[j], vars.index, 0.06)
                                        .withEffectiveDate(startDate)
                                        .withFloatingLegSpread(spreads[l])
                                        .withType(type[k]);
                            // FLOATING_POINT_EXCEPTION
                            double correction = spreads[l] *
                                                swap.floatingLegBPS() /
                                                swap.fixedLegBPS();
                            VanillaSwap equivalentSwap =
                                new MakeVanillaSwap(lengths[j], vars.index, 0.06+correction)
                                        .withEffectiveDate(startDate)
                                        .withFloatingLegSpread(0.0)
                                        .withType(type[k]);
                            Swaption swaption1 =
                                vars.makeSwaption(swap,exerciseDate,0.20);
                            Swaption swaption2 =
                                vars.makeSwaption(equivalentSwap,exerciseDate,0.20);
                            Swaption swaption1_cash =
                                vars.makeSwaption(swap,exerciseDate,0.20,
                                                  Settlement.Type.Cash);
                            Swaption swaption2_cash =
                                vars.makeSwaption(equivalentSwap,exerciseDate,0.20,
                                                  Settlement.Type.Cash);
                            if (Math.Abs(swaption1.NPV()-swaption2.NPV()) > 1.0e-6)
                                Assert.Fail("wrong spread treatment:" +
                                    "\nexercise: " + exerciseDate +
                                    "\nlength:   " + lengths[j] +
                                    "\ntype      " + type[k] +
                                    "\nspread:   " + spreads[l] +
                                    "\noriginal swaption value:   " + swaption1.NPV() +
                                    "\nequivalent swaption value: " + swaption2.NPV());

                            if (Math.Abs(swaption1_cash.NPV()-swaption2_cash.NPV()) > 1.0e-6)
                                Assert.Fail("wrong spread treatment:" +
                                    "\nexercise date: " + exerciseDate +
                                    "\nlength: " + lengths[j] +
                                    //"\npay " + (type[k] ? "fixed" : "floating") +
                                    "\nspread: " + spreads[l] +
                                    "\nvalue of original swaption:   "  + swaption1_cash.NPV() +
                                    "\nvalue of equivalent swaption: "  + swaption2_cash.NPV());
                        }
                    }
                }
            }
        }
        
        [TestMethod()]
        public void testCachedValue() 
        {
            //"Testing swaption value against cached value...");

            CommonVars vars = new CommonVars();

            vars.today = new Date(13, 3, 2002);
            vars.settlement = new Date(15, 3, 2002);
            Settings.setEvaluationDate( vars.today);
            vars.termStructure.linkTo(Utilities.flatRate(vars.settlement, 0.05, new Actual365Fixed()));
            Date exerciseDate = vars.calendar.advance(vars.settlement, new Period(5,TimeUnit.Years));
            Date startDate = vars.calendar.advance(exerciseDate,
                                                   vars.settlementDays,TimeUnit.Days);
            VanillaSwap swap =
                new MakeVanillaSwap(new Period(10,TimeUnit.Years), vars.index, 0.06)
                .withEffectiveDate(startDate);

            Swaption swaption =
                vars.makeSwaption(swap, exerciseDate, 0.20);
            //#if QL_USE_INDEXED_COUPON
                double cachedNPV = 0.036418158579;
            //#else
            //    double cachedNPV = 0.036421429684;
            //#endif

            // FLOATING_POINT_EXCEPTION
            if (Math.Abs(swaption.NPV()-cachedNPV) > 1.0e-12)
                Assert.Fail ("failed to reproduce cached swaption value:\n" +
                            //QL_FIXED + std::setprecision(12) +
                            "\ncalculated: " + swaption.NPV() +
                            "\nexpected:   " + cachedNPV);
        }

        [TestMethod()]
        public void testVega() 
        {
            //"Testing swaption vega...";

            CommonVars vars = new CommonVars();

            Settlement.Type[] types = { Settlement.Type.Physical, Settlement.Type.Cash };
            double[] strikes = { 0.03, 0.04, 0.05, 0.06, 0.07 };
            double[] vols = { 0.01, 0.20, 0.30, 0.70, 0.90 };
            double shift = 1e-8;
            for (int i=0; i<exercises.Length ; i++) {
                Date exerciseDate = vars.calendar.advance(vars.today, exercises[i]);
                // A VERIFIER§§§§
                Date startDate = vars.calendar.advance(exerciseDate,
                                                   vars.settlementDays, TimeUnit.Days);
                for (int j=0; j<lengths.Length ; j++) {
                    for (int t=0; t<strikes.Length ; t++) {
                        for (int h=0; h<type.Length ; h++) {
                            VanillaSwap swap =
                                new MakeVanillaSwap(lengths[j], vars.index, strikes[t])
                                        .withEffectiveDate(startDate)
                                        .withFloatingLegSpread(0.0)
                                        .withType(type[h]);
                            for (int u=0; u<vols.Length ; u++) {
                                Swaption swaption =
                                    vars.makeSwaption(swap, exerciseDate,
                                                      vols[u], types[h]);
                                // FLOATING_POINT_EXCEPTION
                                Swaption swaption1 =
                                    vars.makeSwaption(swap, exerciseDate,
                                                      vols[u]-shift, types[h]);
                                Swaption swaption2 =
                                    vars.makeSwaption(swap, exerciseDate,
                                                      vols[u]+shift, types[h]);

                                double swaptionNPV = swaption.NPV();
                                double numericalVegaPerPoint =
                                    (swaption2.NPV()-swaption1.NPV())/(200.0*shift);
                                // check only relevant vega
                                if (numericalVegaPerPoint/swaptionNPV>1.0e-7) {
                                    double analyticalVegaPerPoint =
                                        (double)swaption.result("vega")/100.0;
                                    double discrepancy = Math.Abs(analyticalVegaPerPoint
                                        - numericalVegaPerPoint);
                                    discrepancy /= numericalVegaPerPoint;
                                    double tolerance = 0.015;
                                    if (discrepancy > tolerance)
                                        Assert.Fail ("failed to compute swaption vega:" +
                                            "\n  option tenor:    " + exercises[i] +
                                            "\n  volatility:      " + vols[u] +
                                            "\n  option type:     " + swaption.type() +
                                            "\n  swap tenor:      " + lengths[j] +
                                            "\n  strike:          " + strikes[t] +
                                            "\n  settlement:      " + types[h] +
                                            "\n  nominal:         " + swaption.underlyingSwap().nominal +
                                            "\n  npv:             " + swaptionNPV +
                                            "\n  calculated vega: " + analyticalVegaPerPoint +
                                            "\n  expected vega:   " + numericalVegaPerPoint +
                                            "\n  discrepancy:     " + discrepancy +
                                            "\n  tolerance:       " + tolerance);
                                }
                            }
                        }
                    }
                }
            }
        }

        [TestMethod()]
        public void testImpliedVolatility()
        {
            //"Testing implied volatility for swaptions...";

            CommonVars vars=new CommonVars();

            int maxEvaluations = 100;
            double tolerance = 1.0e-08;

            Settlement.Type[] types = { Settlement.Type.Physical, Settlement.Type.Cash };
            // test data
            double[] strikes = { 0.02, 0.03, 0.04, 0.05, 0.06, 0.07 };
            double[] vols = { 0.01, 0.05, 0.10, 0.20, 0.30, 0.70, 0.90 };

            for (int i = 0; i < exercises.Length; i++)
            {
                for (int j = 0; j < lengths.Length; j++)
                {
                    Date exerciseDate = vars.calendar.advance(vars.today, exercises[i]);
                    Date startDate = vars.calendar.advance(exerciseDate,
                                                           vars.settlementDays, TimeUnit.Days);
                    Date maturity = vars.calendar.advance(startDate, lengths[j],
                                                          vars.floatingConvention);
                    for (int t = 0; t < strikes.Length; t++)
                    {
                        for (int k = 0; k < type.Length; k++)
                        {
                            VanillaSwap swap = new MakeVanillaSwap(lengths[j], vars.index, strikes[t])
                                        .withEffectiveDate(startDate)
                                        .withFloatingLegSpread(0.0)
                                        .withType(type[k]);
                            for (int h = 0; h < types.Length; h++)
                            {
                                for (int u = 0; u < vols.Length; u++)
                                {
                                    Swaption swaption = vars.makeSwaption(swap, exerciseDate,
                                                                            vols[u], types[h]);
                                    // Black price
                                    double value = swaption.NPV();
                                    double implVol = 0.0;
                                    try
                                    {
                                        implVol =
                                          swaption.impliedVolatility(value,
                                                                      vars.termStructure,
                                                                      0.10,
                                                                      tolerance,
                                                                      maxEvaluations);
                                    }
                                    catch (System.Exception e)
                                    {
                                        // couldn't bracket?
                                        swaption.setPricingEngine(vars.makeEngine(0.0));
                                        double value2 = swaption.NPV();
                                        if (Math.Abs(value - value2) < tolerance)
                                        {
                                            // ok, just skip:
                                            continue;
                                        }
                                        // otherwise, report error
                                        Assert.Fail("implied vol failure: " +
                                                    exercises[i] + "x" + lengths[j] + " " + type[k] +
                                                    "\nsettlement: " + types[h] +
                                                    "\nstrike      " + strikes[t] +
                                                    "\natm level:  " + swap.fairRate() +
                                                    "\nvol:        " + vols[u] +
                                                    "\nprice:      " + value +
                                                    "\n" + e.Message.ToString());
                                    }
                                    if (Math.Abs(implVol - vols[u]) > tolerance)
                                    {
                                        // the difference might not matter
                                        swaption.setPricingEngine(vars.makeEngine(implVol));
                                        double value2 = swaption.NPV();
                                        if (Math.Abs(value - value2) > tolerance)
                                        {
                                            Assert.Fail("implied vol failure: " +
                                                exercises[i] + "x" + lengths[j] + " " + type[k] +
                                                "\nsettlement:    " + types[h] +
                                                "\nstrike         " + strikes[t] +
                                                "\natm level:     " + swap.fairRate() +
                                                "\nvol:           " + vols[u] +
                                                "\nprice:         " + value +
                                                "\nimplied vol:   " + implVol +
                                                "\nimplied price: " + value2);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        
        public void T_Swaption_suite()
        {
            testStrikeDependency();
            testSpreadDependency();
            testSpreadTreatment();
            testCachedValue();
            testVega();
            testImpliedVolatility();
        }
    }
}
