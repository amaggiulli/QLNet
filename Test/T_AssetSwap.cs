/*
 Copyright (C) 2008-2013  Andrea Maggiulli (a.maggiulli@gmail.com)

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
   public class T_AssetSwap
   {
      class CommonVars
      {
         // common data
         public IborIndex iborIndex;
         public SwapIndex swapIndex;
         public IborCouponPricer pricer;
         public CmsCouponPricer cmspricer;
         public double spread;
         public double nonnullspread;
         public double faceAmount;
         public Compounding compounding;
         public RelinkableHandle<YieldTermStructure> termStructure = new RelinkableHandle<YieldTermStructure>(); 

         // clean-up
         public SavedSettings backup;
         //public IndexHistoryCleaner indexCleaner;

         // initial setup
         public CommonVars() 
         {
            backup = new SavedSettings();
            //indexCleaner = new IndexHistoryCleaner();
            termStructure = new RelinkableHandle<YieldTermStructure>();
            int swapSettlementDays = 2;
            faceAmount = 100.0;
            BusinessDayConvention fixedConvention = BusinessDayConvention.Unadjusted;
            compounding = Compounding.Continuous;
            Frequency fixedFrequency = Frequency.Annual;
            Frequency floatingFrequency = Frequency.Semiannual;
            iborIndex = new Euribor(new Period(floatingFrequency), termStructure);
            Calendar calendar = iborIndex.fixingCalendar();
            swapIndex=  new SwapIndex("EuriborSwapIsdaFixA", new Period(10,TimeUnit.Years), swapSettlementDays,
                                      iborIndex.currency(), calendar,
                                      new Period(fixedFrequency), fixedConvention,
                                      iborIndex.dayCounter(), iborIndex);
            spread = 0.0;
            nonnullspread = 0.003;
            Date today = new Date(24,Month.April,2007);
            Settings.setEvaluationDate(today);

            //Date today = Settings::instance().evaluationDate();
            termStructure.linkTo(Utilities.flatRate(today, 0.05, new Actual365Fixed()));
            
            pricer = new BlackIborCouponPricer();
            Handle<SwaptionVolatilityStructure> swaptionVolatilityStructure = 
               new Handle<SwaptionVolatilityStructure>(new ConstantSwaptionVolatility(today, 
               new NullCalendar(),BusinessDayConvention.Following, 0.2, new Actual365Fixed()));
            
            Handle<Quote> meanReversionQuote = new Handle<Quote>(new SimpleQuote(0.01));
            cmspricer = new AnalyticHaganPricer(swaptionVolatilityStructure, GFunctionFactory.YieldCurveModel.Standard, meanReversionQuote);
        }
      }

      [TestMethod()]
      public void testConsistency()
      {

         // Testing consistency between fair price and fair spread...");
         CommonVars vars = new CommonVars();

         Calendar bondCalendar = new TARGET();
         int settlementDays = 3;

         // Fixed Underlying bond (Isin: DE0001135275 DBR 4 01/04/37)
         // maturity doesn't occur on a business day

         Schedule bondSchedule = new Schedule(new Date(4, Month.January, 2005),
                                              new Date(4, Month.January, 2037),
                                              new Period(Frequency.Annual), bondCalendar,
                                              BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                              DateGeneration.Rule.Backward, false);
         Bond bond = new FixedRateBond(settlementDays, vars.faceAmount,
                                       bondSchedule, new List<double>() { 0.04 },
                                       new ActualActual(ActualActual.Convention.ISDA),
                                       BusinessDayConvention.Following,
                                       100.0, new Date(4, Month.January, 2005));

         bool payFixedRate = true;
         double bondPrice = 95.0;

         bool isPar = true;

         AssetSwap parAssetSwap = new AssetSwap(payFixedRate, bond, bondPrice, vars.iborIndex, vars.spread,
                                                null, vars.iborIndex.dayCounter(), isPar);

         IPricingEngine swapEngine = new DiscountingSwapEngine(vars.termStructure, true, bond.settlementDate(),
                                                               Settings.evaluationDate());

         parAssetSwap.setPricingEngine(swapEngine);
         double fairCleanPrice = parAssetSwap.fairCleanPrice();
         double fairSpread = parAssetSwap.fairSpread();

         double tolerance = 1.0e-13;

         AssetSwap assetSwap2 = new AssetSwap(payFixedRate, bond, fairCleanPrice, vars.iborIndex, vars.spread,
                                              null, vars.iborIndex.dayCounter(), isPar);

         assetSwap2.setPricingEngine(swapEngine);
         if (Math.Abs(assetSwap2.NPV()) > tolerance)
         {
            Assert.Fail("npar asset swap fair clean price doesn't zero the NPV: " +
                        "\n  clean price:      " + bondPrice +
                        "\n  fair clean price: " + fairCleanPrice +
                        "\n  NPV:              " + assetSwap2.NPV() +
                        "\n  tolerance:        " + tolerance);
         }
         if (Math.Abs(assetSwap2.fairCleanPrice() - fairCleanPrice) > tolerance)
         {
            Assert.Fail("\npar asset swap fair clean price doesn't equal input clean price at zero NPV: " +
                       "\n  input clean price: " + fairCleanPrice +
                       "\n  fair clean price:  " + assetSwap2.fairCleanPrice() +
                       "\n  NPV:               " + assetSwap2.NPV() +
                       "\n  tolerance:         " + tolerance);
         }
         if (Math.Abs(assetSwap2.fairSpread() - vars.spread) > tolerance)
         {
            Assert.Fail("\npar asset swap fair spread doesn't equal input spread at zero NPV: " +
                       "\n  input spread: " + vars.spread +
                       "\n  fair spread:  " + assetSwap2.fairSpread() +
                       "\n  NPV:          " + assetSwap2.NPV() +
                       "\n  tolerance:    " + tolerance);
         }

         AssetSwap assetSwap3 = new AssetSwap(payFixedRate, bond, bondPrice, vars.iborIndex, fairSpread,
                                              null, vars.iborIndex.dayCounter(), isPar);

         assetSwap3.setPricingEngine(swapEngine);
         if (Math.Abs(assetSwap3.NPV()) > tolerance)
         {
            Assert.Fail("\npar asset swap fair spread doesn't zero the NPV: " +
                       "\n  spread:      " + vars.spread +
                       "\n  fair spread: " + fairSpread +
                       "\n  NPV:         " + assetSwap3.NPV() +
                       "\n  tolerance:   " + tolerance);
         }
         if (Math.Abs(assetSwap3.fairCleanPrice() - bondPrice) > tolerance)
         {
            Assert.Fail("\npar asset swap fair clean price doesn't equal input clean price at zero NPV: " +
                       "\n  input clean price: " + bondPrice +
                       "\n  fair clean price:  " + assetSwap3.fairCleanPrice() +
                       "\n  NPV:               " + assetSwap3.NPV() +
                       "\n  tolerance:         " + tolerance);
         }
         if (Math.Abs(assetSwap3.fairSpread() - fairSpread) > tolerance)
         {
            Assert.Fail("\npar asset swap fair spread doesn't equal input spread at  zero NPV: " +
                       "\n  input spread: " + fairSpread +
                       "\n  fair spread:  " + assetSwap3.fairSpread() +
                       "\n  NPV:          " + assetSwap3.NPV() +
                       "\n  tolerance:    " + tolerance);
         }

         // let's change the npv date
         swapEngine = new DiscountingSwapEngine(vars.termStructure, true, bond.settlementDate(), bond.settlementDate());

         parAssetSwap.setPricingEngine(swapEngine);
         // fair clean price and fair spread should not change
         if (Math.Abs(parAssetSwap.fairCleanPrice() - fairCleanPrice) > tolerance)
         {
            Assert.Fail("\npar asset swap fair clean price changed with NpvDate:" +
                       "\n expected clean price: " + fairCleanPrice +
                       "\n fair clean price:     " + parAssetSwap.fairCleanPrice() +
                       "\n tolerance:            " + tolerance);
         }
         if (Math.Abs(parAssetSwap.fairSpread() - fairSpread) > tolerance)
         {
            Assert.Fail("\npar asset swap fair spread changed with NpvDate:" +
                       "\n  expected spread: " + fairSpread +
                       "\n  fair spread:     " + parAssetSwap.fairSpread() +
                       "\n  tolerance:       " + tolerance);
         }

         assetSwap2 = new AssetSwap(payFixedRate, bond, fairCleanPrice, vars.iborIndex, vars.spread,
                                    null, vars.iborIndex.dayCounter(), isPar);
         assetSwap2.setPricingEngine(swapEngine);
         if (Math.Abs(assetSwap2.NPV()) > tolerance)
         {
            Assert.Fail("\npar asset swap fair clean price doesn't zero the NPV: " +
                       "\n  clean price:      " + bondPrice +
                       "\n  fair clean price: " + fairCleanPrice +
                       "\n  NPV:              " + assetSwap2.NPV() +
                       "\n  tolerance:        " + tolerance);
         }
         if (Math.Abs(assetSwap2.fairCleanPrice() - fairCleanPrice) > tolerance)
         {
            Assert.Fail("\npar asset swap fair clean price doesn't equal input clean price at zero NPV: " +
                       "\n  input clean price: " + fairCleanPrice +
                       "\n  fair clean price:  " + assetSwap2.fairCleanPrice() +
                       "\n  NPV:               " + assetSwap2.NPV() +
                       "\n  tolerance:         " + tolerance);
         }
         if (Math.Abs(assetSwap2.fairSpread() - vars.spread) > tolerance)
         {
            Assert.Fail("\npar asset swap fair spread doesn't equal input spread at zero NPV: " +
                       "\n  input spread: " + vars.spread +
                       "\n  fair spread:  " + assetSwap2.fairSpread() +
                       "\n  NPV:          " + assetSwap2.NPV() +
                       "\n  tolerance:    " + tolerance);
         }

         assetSwap3 = new AssetSwap(payFixedRate, bond, bondPrice, vars.iborIndex, fairSpread,
                                    null, vars.iborIndex.dayCounter(), isPar);
         assetSwap3.setPricingEngine(swapEngine);
         if (Math.Abs(assetSwap3.NPV()) > tolerance)
         {
            Assert.Fail("\npar asset swap fair spread doesn't zero the NPV: " +
                       "\n  spread:      " + vars.spread +
                       "\n  fair spread: " + fairSpread +
                       "\n  NPV:         " + assetSwap3.NPV() +
                       "\n  tolerance:   " + tolerance);
         }
         if (Math.Abs(assetSwap3.fairCleanPrice() - bondPrice) > tolerance)
         {
            Assert.Fail("\npar asset swap fair clean price doesn't equal input clean price at zero NPV: " +
                       "\n  input clean price: " + bondPrice +
                       "\n  fair clean price:  " + assetSwap3.fairCleanPrice() +
                       "\n  NPV:               " + assetSwap3.NPV() +
                       "\n  tolerance:         " + tolerance);
         }
         if (Math.Abs(assetSwap3.fairSpread() - fairSpread) > tolerance)
         {
            Assert.Fail("\npar asset swap fair spread doesn't equal input spread at zero NPV: " +
                       "\n  input spread: " + fairSpread +
                       "\n  fair spread:  " + assetSwap3.fairSpread() +
                       "\n  NPV:          " + assetSwap3.NPV() +
                       "\n  tolerance:    " + tolerance);

         }

         // now market asset swap
         isPar = false;
         AssetSwap mktAssetSwap = new AssetSwap(payFixedRate, bond, bondPrice, vars.iborIndex, vars.spread,
                                                null, vars.iborIndex.dayCounter(), isPar);

         swapEngine = new DiscountingSwapEngine(vars.termStructure, true, bond.settlementDate(),
                                                Settings.evaluationDate());

         mktAssetSwap.setPricingEngine(swapEngine);
         fairCleanPrice = mktAssetSwap.fairCleanPrice();
         fairSpread = mktAssetSwap.fairSpread();

         AssetSwap assetSwap4 = new AssetSwap(payFixedRate, bond, fairCleanPrice, vars.iborIndex, vars.spread,
                                              null, vars.iborIndex.dayCounter(), isPar);
         assetSwap4.setPricingEngine(swapEngine);
         if (Math.Abs(assetSwap4.NPV()) > tolerance)
         {
            Assert.Fail("\nmarket asset swap fair clean price doesn't zero the NPV: " +
                       "\n  clean price:      " + bondPrice +
                       "\n  fair clean price: " + fairCleanPrice +
                       "\n  NPV:              " + assetSwap4.NPV() +
                       "\n  tolerance:        " + tolerance);
         }
         if (Math.Abs(assetSwap4.fairCleanPrice() - fairCleanPrice) > tolerance)
         {
            Assert.Fail("\nmarket asset swap fair clean price doesn't equal input clean price at zero NPV: " +
                       "\n  input clean price: " + fairCleanPrice +
                       "\n  fair clean price:  " + assetSwap4.fairCleanPrice() +
                       "\n  NPV:               " + assetSwap4.NPV() +
                       "\n  tolerance:         " + tolerance);
         }
         if (Math.Abs(assetSwap4.fairSpread() - vars.spread) > tolerance)
         {
            Assert.Fail("\nmarket asset swap fair spread doesn't equal input spread at zero NPV: " +
                       "\n  input spread: " + vars.spread +
                       "\n  fair spread:  " + assetSwap4.fairSpread() +
                       "\n  NPV:          " + assetSwap4.NPV() +
                       "\n  tolerance:    " + tolerance);
         }

         AssetSwap assetSwap5 = new AssetSwap(payFixedRate, bond, bondPrice, vars.iborIndex, fairSpread,
                                              null, vars.iborIndex.dayCounter(), isPar);
         assetSwap5.setPricingEngine(swapEngine);
         if (Math.Abs(assetSwap5.NPV()) > tolerance)
         {
            Assert.Fail("\nmarket asset swap fair spread doesn't zero the NPV: " +
                       "\n  spread:      " + vars.spread +
                       "\n  fair spread: " + fairSpread +
                       "\n  NPV:         " + assetSwap5.NPV() +
                       "\n  tolerance:   " + tolerance);
         }
         if (Math.Abs(assetSwap5.fairCleanPrice() - bondPrice) > tolerance)
         {
            Assert.Fail("\nmarket asset swap fair clean price doesn't equal input clean price at zero NPV: " +
                       "\n  input clean price: " + bondPrice +
                       "\n  fair clean price:  " + assetSwap5.fairCleanPrice() +
                       "\n  NPV:               " + assetSwap5.NPV() +
                       "\n  tolerance:         " + tolerance);
         }
         if (Math.Abs(assetSwap5.fairSpread() - fairSpread) > tolerance)
         {
            Assert.Fail("\nmarket asset swap fair spread doesn't equal input spread at zero NPV: " +
                       "\n  input spread: " + fairSpread +
                       "\n  fair spread:  " + assetSwap5.fairSpread() +
                       "\n  NPV:          " + assetSwap5.NPV() +
                       "\n  tolerance:    " + tolerance);
         }

         // let's change the npv date
         swapEngine = new DiscountingSwapEngine(vars.termStructure, true, bond.settlementDate(), bond.settlementDate());
         mktAssetSwap.setPricingEngine(swapEngine);

         // fair clean price and fair spread should not change
         if (Math.Abs(mktAssetSwap.fairCleanPrice() - fairCleanPrice) > tolerance)
         {
            Assert.Fail("\nmarket asset swap fair clean price changed with NpvDate:" +
                       "\n  expected clean price: " + fairCleanPrice +
                       "\n  fair clean price:  " + mktAssetSwap.fairCleanPrice() +
                       "\n  tolerance:         " + tolerance);
         }
         if (Math.Abs(mktAssetSwap.fairSpread() - fairSpread) > tolerance)
         {
            Assert.Fail("\nmarket asset swap fair spread changed with NpvDate:" +
                       "\n  expected spread: " + fairSpread +
                       "\n  fair spread:  " + mktAssetSwap.fairSpread() +
                       "\n  tolerance:    " + tolerance);
         }

         assetSwap4 = new AssetSwap(payFixedRate, bond, fairCleanPrice, vars.iborIndex, vars.spread,
                                    null, vars.iborIndex.dayCounter(), isPar);
         assetSwap4.setPricingEngine(swapEngine);
         if (Math.Abs(assetSwap4.NPV()) > tolerance)
         {
            Assert.Fail("\nmarket asset swap fair clean price doesn't zero the NPV: " +
                       "\n  clean price:      " + bondPrice +
                       "\n  fair clean price: " + fairCleanPrice +
                       "\n  NPV:              " + assetSwap4.NPV() +
                       "\n  tolerance:        " + tolerance);
         }
         if (Math.Abs(assetSwap4.fairCleanPrice() - fairCleanPrice) > tolerance)
         {
            Assert.Fail("\nmarket asset swap fair clean price doesn't equal input clean price at zero NPV: " +
                       "\n  input clean price: " + fairCleanPrice +
                       "\n  fair clean price:  " + assetSwap4.fairCleanPrice() +
                       "\n  NPV:               " + assetSwap4.NPV() +
                       "\n  tolerance:         " + tolerance);
         }
         if (Math.Abs(assetSwap4.fairSpread() - vars.spread) > tolerance)
         {
            Assert.Fail("\nmarket asset swap fair spread doesn't equal input spread at zero NPV: " +
                       "\n  input spread: " + vars.spread +
                       "\n  fair spread:  " + assetSwap4.fairSpread() +
                       "\n  NPV:          " + assetSwap4.NPV() +
                       "\n  tolerance:    " + tolerance);
         }

         assetSwap5 = new AssetSwap(payFixedRate, bond, bondPrice, vars.iborIndex, fairSpread,
                                    null, vars.iborIndex.dayCounter(), isPar);
         assetSwap5.setPricingEngine(swapEngine);
         if (Math.Abs(assetSwap5.NPV()) > tolerance)
         {
            Assert.Fail("\nmarket asset swap fair spread doesn't zero the NPV: " +
                       "\n  spread:      " + vars.spread +
                       "\n  fair spread: " + fairSpread +
                       "\n  NPV:         " + assetSwap5.NPV() +
                       "\n  tolerance:   " + tolerance);
         }
         if (Math.Abs(assetSwap5.fairCleanPrice() - bondPrice) > tolerance)
         {
            Assert.Fail("\nmarket asset swap fair clean price doesn't equal input clean price at zero NPV: " +
                       "\n  input clean price: " + bondPrice +
                       "\n  fair clean price:  " + assetSwap5.fairCleanPrice() +
                       "\n  NPV:               " + assetSwap5.NPV() +
                       "\n  tolerance:         " + tolerance);
         }
         if (Math.Abs(assetSwap5.fairSpread() - fairSpread) > tolerance)
         {
            Assert.Fail("\nmarket asset swap fair spread doesn't equal input spread at zero NPV: " +
                       "\n  input spread: " + fairSpread +
                       "\n  fair spread:  " + assetSwap5.fairSpread() +
                       "\n  NPV:          " + assetSwap5.NPV() +
                       "\n  tolerance:    " + tolerance);
         }
      }

      [TestMethod()]
      public void testImpliedValue() 
      {
         // Testing implied bond value against asset-swap fair price with null spread
         CommonVars vars = new CommonVars();

         Calendar bondCalendar = new TARGET();
         int settlementDays = 3;
         int fixingDays = 2;
         bool payFixedRate = true;
         bool parAssetSwap = true;
         bool inArrears = false;

         // Fixed Underlying bond (Isin: DE0001135275 DBR 4 01/04/37)
         // maturity doesn't occur on a business day

         Schedule fixedBondSchedule1 = new Schedule( new Date(4,Month.January,2005),
                                                     new Date(4,Month.January,2037),
                                                     new Period(Frequency.Annual), bondCalendar,
                                                     BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                                     DateGeneration.Rule.Backward, false);
         Bond fixedBond1 = new FixedRateBond(settlementDays, vars.faceAmount,
                                                fixedBondSchedule1,
                                                new List<double>(){0.04},
                                                new ActualActual(ActualActual.Convention.ISDA),
                                                BusinessDayConvention.Following,
                                                100.0, new Date(4,Month.January,2005));

         IPricingEngine bondEngine = new DiscountingBondEngine(vars.termStructure);
         IPricingEngine swapEngine = new DiscountingSwapEngine(vars.termStructure);
         fixedBond1.setPricingEngine(bondEngine);

         double fixedBondPrice1 = fixedBond1.cleanPrice();
         AssetSwap fixedBondAssetSwap1 = new AssetSwap(payFixedRate, fixedBond1, fixedBondPrice1, vars.iborIndex, vars.spread,
                                                       null, vars.iborIndex.dayCounter(), parAssetSwap);
         fixedBondAssetSwap1.setPricingEngine(swapEngine);
         double fixedBondAssetSwapPrice1 = fixedBondAssetSwap1.fairCleanPrice();
         double tolerance = 1.0e-13;
         double error1 = Math.Abs(fixedBondAssetSwapPrice1-fixedBondPrice1);

         if (error1>tolerance) {
            Assert.Fail("wrong zero spread asset swap price for fixed bond:" +
                        "\n  bond's clean price:    " + fixedBondPrice1 +
                        "\n  asset swap fair price: " + fixedBondAssetSwapPrice1 +
                        "\n  error:                 " + error1 +
                        "\n  tolerance:             " + tolerance);
         }

         // Fixed Underlying bond (Isin: IT0006527060 IBRD 5 02/05/19)
         // maturity occurs on a business day

         Schedule fixedBondSchedule2 = new Schedule( new Date(5,Month.February,2005),
                                                     new Date(5,Month.February,2019),
                                                     new Period(Frequency.Annual), bondCalendar,
                                                     BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                                     DateGeneration.Rule.Backward, false);
         Bond fixedBond2 = new FixedRateBond(settlementDays, vars.faceAmount,
                                             fixedBondSchedule2,
                                             new List<double>(){0.05},
                                             new Thirty360(Thirty360.Thirty360Convention.BondBasis),
                                             BusinessDayConvention.Following,
                                             100.0, new Date(5,Month.February,2005));

         fixedBond2.setPricingEngine(bondEngine);

         double fixedBondPrice2 = fixedBond2.cleanPrice();
         AssetSwap fixedBondAssetSwap2 = new AssetSwap(payFixedRate, fixedBond2, fixedBondPrice2, vars.iborIndex, vars.spread,
                                                       null, vars.iborIndex.dayCounter(),  parAssetSwap);
         fixedBondAssetSwap2.setPricingEngine(swapEngine);
         double fixedBondAssetSwapPrice2 = fixedBondAssetSwap2.fairCleanPrice();
         double error2 = Math.Abs(fixedBondAssetSwapPrice2-fixedBondPrice2);

         if (error2>tolerance) {
            Assert.Fail("wrong zero spread asset swap price for fixed bond:" +
                        "\n  bond's clean price:    " + fixedBondPrice2 +
                        "\n  asset swap fair price: " + fixedBondAssetSwapPrice2 +
                        "\n  error:                 " + error2 +
                        "\n  tolerance:             " + tolerance);
         }

         // FRN Underlying bond (Isin: IT0003543847 ISPIM 0 09/29/13)
         // maturity doesn't occur on a business day

         Schedule floatingBondSchedule1 = new Schedule( new Date(29,Month.September,2003),
                                                        new Date(29,Month.September,2013),
                                                        new Period(Frequency.Semiannual), bondCalendar,
                                                        BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                                        DateGeneration.Rule.Backward, false);

         Bond floatingBond1 = new FloatingRateBond(settlementDays, vars.faceAmount,
                                                   floatingBondSchedule1,
                                                   vars.iborIndex, new Actual360(),
                                                   BusinessDayConvention.Following, fixingDays,
                                                   new List<double>(){1},
                                                   new List<double>(){0.0056},
                                                   new List<double>(),
                                                   new List<double>(),
                                                   inArrears,
                                                   100.0, new Date(29,Month.September,2003));

         floatingBond1.setPricingEngine(bondEngine);

         Utils.setCouponPricer(floatingBond1.cashflows(), vars.pricer);
         vars.iborIndex.addFixing(new Date(27,Month.March,2007), 0.0402);
         double floatingBondPrice1 = floatingBond1.cleanPrice();
         AssetSwap floatingBondAssetSwap1 = new AssetSwap(payFixedRate, floatingBond1, floatingBondPrice1, vars.iborIndex, vars.spread,
                                                          null, vars.iborIndex.dayCounter(), parAssetSwap);
         floatingBondAssetSwap1.setPricingEngine(swapEngine);
         double floatingBondAssetSwapPrice1 = floatingBondAssetSwap1.fairCleanPrice();
         double error3 = Math.Abs(floatingBondAssetSwapPrice1-floatingBondPrice1);

         if (error3>tolerance) {
            Assert.Fail("wrong zero spread asset swap price for floater:" +
                        "\n  bond's clean price:    " + floatingBondPrice1 +
                        "\n  asset swap fair price: " + floatingBondAssetSwapPrice1 +
                        "\n  error:                 " + error3 +
                        "\n  tolerance:             " + tolerance);
         }

         // FRN Underlying bond (Isin: XS0090566539 COE 0 09/24/18)
         // maturity occurs on a business day

         Schedule floatingBondSchedule2 = new Schedule( new Date(24,Month.September,2004),
                                                        new Date(24,Month.September,2018),
                                                        new Period(Frequency.Semiannual), bondCalendar,
                                                        BusinessDayConvention.ModifiedFollowing, BusinessDayConvention.ModifiedFollowing,
                                                        DateGeneration.Rule.Backward, false);
         Bond floatingBond2 = new FloatingRateBond( settlementDays, vars.faceAmount,
                                                    floatingBondSchedule2,
                                                    vars.iborIndex, new Actual360(),
                                                    BusinessDayConvention.ModifiedFollowing, fixingDays,
                                                    new List<double>(){1},
                                                    new List<double>(){0.0025},
                                                    new List<double>(),
                                                    new List<double>(),
                                                    inArrears,
                                                    100.0, new Date(24,Month.September,2004));

         floatingBond2.setPricingEngine(bondEngine);

         Utils.setCouponPricer(floatingBond2.cashflows(), vars.pricer);
         vars.iborIndex.addFixing( new Date(22,Month.March,2007), 0.04013);
         double currentCoupon=0.04013+0.0025;
         double floatingCurrentCoupon= floatingBond2.nextCouponRate();
         double error4= Math.Abs(floatingCurrentCoupon-currentCoupon);
         if (error4>tolerance) {
            Assert.Fail("wrong current coupon is returned for floater bond:" +
                        "\n  bond's calculated current coupon:      " +
                        currentCoupon +
                        "\n  current coupon asked to the bond: " +
                        floatingCurrentCoupon +
                        "\n  error:                 " + error4 +
                        "\n  tolerance:             " + tolerance);
         }

         double floatingBondPrice2 = floatingBond2.cleanPrice();
         AssetSwap floatingBondAssetSwap2 = new AssetSwap(payFixedRate,floatingBond2, floatingBondPrice2, vars.iborIndex, vars.spread,
                                                          null, vars.iborIndex.dayCounter(), parAssetSwap);
         floatingBondAssetSwap2.setPricingEngine(swapEngine);
         double floatingBondAssetSwapPrice2 = floatingBondAssetSwap2.fairCleanPrice();
         double error5 = Math.Abs(floatingBondAssetSwapPrice2-floatingBondPrice2);

         if (error5>tolerance) {
            Assert.Fail("wrong zero spread asset swap price for floater:" +
                        "\n  bond's clean price:    " + floatingBondPrice2 +
                        "\n  asset swap fair price: " + floatingBondAssetSwapPrice2 +
                        "\n  error:                 " + error5 +
                        "\n  tolerance:             " + tolerance);
         }

         // CMS Underlying bond (Isin: XS0228052402 CRDIT 0 8/22/20)
         // maturity doesn't occur on a business day

         Schedule cmsBondSchedule1 = new Schedule( new Date(22,Month.August,2005),
                                 new Date(22,Month.August,2020),
                                 new Period(Frequency.Annual), bondCalendar,
                                 BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                 DateGeneration.Rule.Backward, false);
         Bond cmsBond1 = new CmsRateBond(settlementDays, vars.faceAmount,
                                             cmsBondSchedule1,
                                             vars.swapIndex, new Thirty360(),
                                             BusinessDayConvention.Following, fixingDays,
                                             new List<double>(){1.0},
                                             new List<double>(){0.0},
                                             new List<double>(){0.055},
                                             new List<double>(){0.025},
                                             inArrears,
                                             100.0, new Date(22,Month.August,2005));

         cmsBond1.setPricingEngine(bondEngine);

         Utils.setCouponPricer(cmsBond1.cashflows(), vars.cmspricer);
         vars.swapIndex.addFixing( new Date(18,Month.August,2006), 0.04158);
         double cmsBondPrice1 = cmsBond1.cleanPrice();
         AssetSwap cmsBondAssetSwap1 = new AssetSwap(payFixedRate, cmsBond1, cmsBondPrice1, vars.iborIndex, vars.spread,
                                                     null,vars.iborIndex.dayCounter(), parAssetSwap);
         cmsBondAssetSwap1.setPricingEngine(swapEngine);
         double cmsBondAssetSwapPrice1 = cmsBondAssetSwap1.fairCleanPrice();
         double error6 = Math.Abs(cmsBondAssetSwapPrice1-cmsBondPrice1);

         if (error6>tolerance) {
            Assert.Fail("wrong zero spread asset swap price for cms bond:" +
                        "\n  bond's clean price:    " + cmsBondPrice1 +
                        "\n  asset swap fair price: " + cmsBondAssetSwapPrice1 +
                        "\n  error:                 " + error6 +
                        "\n  tolerance:             " + tolerance);
         }

         // CMS Underlying bond (Isin: XS0218766664 ISPIM 0 5/6/15)
         // maturity occurs on a business day

         Schedule cmsBondSchedule2 = new Schedule( new Date(06,Month.May,2005),
                                                   new Date(06,Month.May,2015),
                                                   new Period(Frequency.Annual), bondCalendar,
                                                   BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                                   DateGeneration.Rule.Backward, false);
        Bond cmsBond2 = new CmsRateBond(settlementDays, vars.faceAmount, cmsBondSchedule2,
                        vars.swapIndex, new Thirty360(),
                        BusinessDayConvention.Following, fixingDays,
                        new List<double>(){0.84}, new List<double>(){0.0},
                        new List<double>(), new List<double>(),
                        inArrears,
                        100.0, new Date(06,Month.May,2005));

         cmsBond2.setPricingEngine(bondEngine);

         Utils.setCouponPricer(cmsBond2.cashflows(), vars.cmspricer);
         vars.swapIndex.addFixing( new Date(04,Month.May,2006), 0.04217);
         double cmsBondPrice2 = cmsBond2.cleanPrice();
         AssetSwap cmsBondAssetSwap2 = new AssetSwap(payFixedRate,cmsBond2, cmsBondPrice2, vars.iborIndex, vars.spread,
                                                     null, vars.iborIndex.dayCounter(), parAssetSwap);
         cmsBondAssetSwap2.setPricingEngine(swapEngine);
         double cmsBondAssetSwapPrice2 = cmsBondAssetSwap2.fairCleanPrice();
         double error7 = Math.Abs(cmsBondAssetSwapPrice2-cmsBondPrice2);

         if (error7>tolerance) {
            Assert.Fail("wrong zero spread asset swap price for cms bond:" +
                        "\n  bond's clean price:    " + cmsBondPrice2 +
                        "\n  asset swap fair price: " + cmsBondAssetSwapPrice2 +
                        "\n  error:                 " + error7 +
                        "\n  tolerance:             " + tolerance);
         }

         // Zero Coupon bond (Isin: DE0004771662 IBRD 0 12/20/15)
         // maturity doesn't occur on a business day

         Bond zeroCpnBond1 = new ZeroCouponBond(settlementDays, bondCalendar, vars.faceAmount,
                                                new Date(20,Month.December,2015),
                                                BusinessDayConvention.Following,
                                                100.0, new Date(19,Month.December,1985));

         zeroCpnBond1.setPricingEngine(bondEngine);

         double zeroCpnBondPrice1 = zeroCpnBond1.cleanPrice();
         AssetSwap zeroCpnAssetSwap1 = new AssetSwap(payFixedRate,zeroCpnBond1, zeroCpnBondPrice1, vars.iborIndex, vars.spread,
                                                     null, vars.iborIndex.dayCounter(), parAssetSwap);
         zeroCpnAssetSwap1.setPricingEngine(swapEngine);
         double zeroCpnBondAssetSwapPrice1 = zeroCpnAssetSwap1.fairCleanPrice();
         double error8 = Math.Abs(cmsBondAssetSwapPrice1-cmsBondPrice1);

         if (error8>tolerance) {
            Assert.Fail("wrong zero spread asset swap price for zero cpn bond:" +
                        "\n  bond's clean price:    " + zeroCpnBondPrice1 +
                        "\n  asset swap fair price: " + zeroCpnBondAssetSwapPrice1 +
                        "\n  error:                 " + error8 +
                        "\n  tolerance:             " + tolerance);
         }

         // Zero Coupon bond (Isin: IT0001200390 ISPIM 0 02/17/28)
         // maturity occurs on a business day

         Bond zeroCpnBond2 = new ZeroCouponBond(settlementDays, bondCalendar, vars.faceAmount,
                           new Date(17,Month.February,2028),
                           BusinessDayConvention.Following,
                           100.0, new Date(17,Month.February,1998));

         zeroCpnBond2.setPricingEngine(bondEngine);

         double zeroCpnBondPrice2 = zeroCpnBond2.cleanPrice();
         AssetSwap zeroCpnAssetSwap2 = new AssetSwap(payFixedRate, zeroCpnBond2, zeroCpnBondPrice2,  vars.iborIndex, vars.spread,
                                                     null,vars.iborIndex.dayCounter(), parAssetSwap);
         zeroCpnAssetSwap2.setPricingEngine(swapEngine);
         double zeroCpnBondAssetSwapPrice2 = zeroCpnAssetSwap2.fairCleanPrice();
         double error9 = Math.Abs(cmsBondAssetSwapPrice2-cmsBondPrice2);

         if (error9>tolerance) {
            Assert.Fail("wrong zero spread asset swap price for zero cpn bond:" +
                        "\n  bond's clean price:      " + zeroCpnBondPrice2 +
                        "\n  asset swap fair price:   " + zeroCpnBondAssetSwapPrice2 +
                        "\n  error:                   " + error9 +
                        "\n  tolerance:               " + tolerance);
         }

      }

      [TestMethod()]
      public void testMarketASWSpread() 
      {
         // Testing relationship between market asset swap and par asset swap...
         CommonVars vars = new CommonVars();

         Calendar bondCalendar = new TARGET();
         int settlementDays = 3;
         int fixingDays = 2;
         bool payFixedRate = true;
         bool parAssetSwap = true;
         bool mktAssetSwap = false;
         bool inArrears = false;

         // Fixed Underlying bond (Isin: DE0001135275 DBR 4 01/04/37)
         // maturity doesn't occur on a business day

         Schedule fixedBondSchedule1 = new Schedule(new Date(4,Month.January,2005),
                                    new Date(4,Month.January,2037),
                                    new Period(Frequency.Annual), bondCalendar,
                                    BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                    DateGeneration.Rule.Backward, false);
         Bond fixedBond1 = new FixedRateBond(settlementDays, vars.faceAmount, fixedBondSchedule1,
                           new List<double>{0.04},
                           new ActualActual(ActualActual.Convention.ISDA),BusinessDayConvention.Following,
                           100.0, new Date(4,Month.January,2005));

         IPricingEngine bondEngine = new DiscountingBondEngine(vars.termStructure);
         IPricingEngine swapEngine = new DiscountingSwapEngine(vars.termStructure);
         fixedBond1.setPricingEngine(bondEngine);

         double fixedBondMktPrice1 = 89.22 ; // market price observed on 7th June 2007
         double fixedBondMktFullPrice1=fixedBondMktPrice1+fixedBond1.accruedAmount();
         AssetSwap fixedBondParAssetSwap1 = new AssetSwap(payFixedRate,
                                          fixedBond1, fixedBondMktPrice1,
                                          vars.iborIndex, vars.spread,
                                          null,
                                          vars.iborIndex.dayCounter(),
                                          parAssetSwap);
         fixedBondParAssetSwap1.setPricingEngine(swapEngine);
         double fixedBondParAssetSwapSpread1 = fixedBondParAssetSwap1.fairSpread();
         AssetSwap fixedBondMktAssetSwap1 = new AssetSwap(payFixedRate,
                                          fixedBond1, fixedBondMktPrice1,
                                          vars.iborIndex, vars.spread,
                                          null,
                                          vars.iborIndex.dayCounter(),
                                          mktAssetSwap);
         fixedBondMktAssetSwap1.setPricingEngine(swapEngine);
         double fixedBondMktAssetSwapSpread1 = fixedBondMktAssetSwap1.fairSpread();

         double tolerance = 1.0e-13;
         double error1 = Math.Abs(fixedBondMktAssetSwapSpread1- 100*fixedBondParAssetSwapSpread1/fixedBondMktFullPrice1);

         if (error1>tolerance) {
            Assert.Fail("wrong asset swap spreads for fixed bond:" +
                        "\n  market ASW spread: " + fixedBondMktAssetSwapSpread1 +
                        "\n  par ASW spread:    " + fixedBondParAssetSwapSpread1 +
                        "\n  error:             " + error1 +
                        "\n  tolerance:         " + tolerance);
         }

         // Fixed Underlying bond (Isin: IT0006527060 IBRD 5 02/05/19)
         // maturity occurs on a business day

         Schedule fixedBondSchedule2 = new Schedule(new Date(5,Month.February,2005),
                                    new Date(5,Month.February,2019),
                                    new Period(Frequency.Annual), bondCalendar,
                                    BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                    DateGeneration.Rule.Backward, false);
         Bond fixedBond2 = new FixedRateBond(settlementDays, vars.faceAmount, fixedBondSchedule2,
                           new List<double>{ 0.05},
                           new Thirty360(Thirty360.Thirty360Convention.BondBasis), BusinessDayConvention.Following,
                           100.0, new Date(5,Month.February,2005));

         fixedBond2.setPricingEngine(bondEngine);

         double fixedBondMktPrice2 = 99.98 ; // market price observed on 7th June 2007
         double fixedBondMktFullPrice2=fixedBondMktPrice2+fixedBond2.accruedAmount();
         AssetSwap fixedBondParAssetSwap2 = new AssetSwap(payFixedRate,
                                          fixedBond2, fixedBondMktPrice2,
                                          vars.iborIndex, vars.spread,
                                          null,
                                          vars.iborIndex.dayCounter(),
                                          parAssetSwap);
         fixedBondParAssetSwap2.setPricingEngine(swapEngine);
         double fixedBondParAssetSwapSpread2 = fixedBondParAssetSwap2.fairSpread();
         AssetSwap fixedBondMktAssetSwap2 = new AssetSwap(payFixedRate,
                                          fixedBond2, fixedBondMktPrice2,
                                          vars.iborIndex, vars.spread,
                                          null,
                                          vars.iborIndex.dayCounter(),
                                          mktAssetSwap);
         fixedBondMktAssetSwap2.setPricingEngine(swapEngine);
         double fixedBondMktAssetSwapSpread2 = fixedBondMktAssetSwap2.fairSpread();
         double error2 = Math.Abs(fixedBondMktAssetSwapSpread2-
                     100*fixedBondParAssetSwapSpread2/fixedBondMktFullPrice2);

         if (error2>tolerance) {
            Assert.Fail("wrong asset swap spreads for fixed bond:" +
                        "\n  market ASW spread: " + fixedBondMktAssetSwapSpread2 +
                        "\n  par ASW spread:    " + fixedBondParAssetSwapSpread2 +
                        "\n  error:             " + error2 +
                        "\n  tolerance:         " + tolerance);
         }

         // FRN Underlying bond (Isin: IT0003543847 ISPIM 0 09/29/13)
         // maturity doesn't occur on a business day

         Schedule floatingBondSchedule1 = new Schedule( new Date(29,Month.September,2003),
                                       new Date(29,Month.September,2013),
                                       new Period(Frequency.Semiannual), bondCalendar,
                                       BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                       DateGeneration.Rule.Backward, false);

         Bond floatingBond1 = new FloatingRateBond(settlementDays, vars.faceAmount,
                              floatingBondSchedule1,
                              vars.iborIndex, new Actual360(),
                              BusinessDayConvention.Following, fixingDays,
                              new List<double>{1}, new List<double>{0.0056},
                              new List<double>(), new List<double>(),
                              inArrears,
                              100.0, new Date(29,Month.September,2003));

         floatingBond1.setPricingEngine(bondEngine);

         Utils.setCouponPricer(floatingBond1.cashflows(), vars.pricer);
         vars.iborIndex.addFixing(new Date(27,Month.March,2007), 0.0402);
         // market price observed on 7th June 2007
         double floatingBondMktPrice1 = 101.64 ;
         double floatingBondMktFullPrice1 = floatingBondMktPrice1+floatingBond1.accruedAmount();
         AssetSwap floatingBondParAssetSwap1 = new AssetSwap(payFixedRate,
                                             floatingBond1, floatingBondMktPrice1,
                                             vars.iborIndex, vars.spread,
                                             null,
                                             vars.iborIndex.dayCounter(),
                                             parAssetSwap);
         floatingBondParAssetSwap1.setPricingEngine(swapEngine);
         double floatingBondParAssetSwapSpread1 = floatingBondParAssetSwap1.fairSpread();
         AssetSwap floatingBondMktAssetSwap1= new AssetSwap(payFixedRate,
                                             floatingBond1, floatingBondMktPrice1,
                                             vars.iborIndex, vars.spread,
                                             null,
                                             vars.iborIndex.dayCounter(),
                                             mktAssetSwap);
         floatingBondMktAssetSwap1.setPricingEngine(swapEngine);
         double floatingBondMktAssetSwapSpread1 = floatingBondMktAssetSwap1.fairSpread();
         double error3 = Math.Abs(floatingBondMktAssetSwapSpread1-
                     100*floatingBondParAssetSwapSpread1/floatingBondMktFullPrice1);

         if (error3>tolerance) {
            Assert.Fail("wrong asset swap spreads for floating bond:" +
                        "\n  market ASW spread: " + floatingBondMktAssetSwapSpread1 +
                        "\n  par ASW spread:    " + floatingBondParAssetSwapSpread1 +
                        "\n  error:             " + error3 +
                        "\n  tolerance:         " + tolerance);
         }

         // FRN Underlying bond (Isin: XS0090566539 COE 0 09/24/18)
         // maturity occurs on a business day

         Schedule floatingBondSchedule2 = new Schedule( new Date(24,Month.September,2004),
                                       new Date(24,Month.September,2018),
                                       new Period(Frequency.Semiannual), bondCalendar,
                                       BusinessDayConvention.ModifiedFollowing, BusinessDayConvention.ModifiedFollowing,
                                       DateGeneration.Rule.Backward, false);
         Bond floatingBond2 = new FloatingRateBond(settlementDays, vars.faceAmount,
                              floatingBondSchedule2,
                              vars.iborIndex, new Actual360(),
                              BusinessDayConvention.ModifiedFollowing, fixingDays,
                              new List<double>{1}, new List<double>{0.0025},
                              new List<double>(), new List<double>(),
                              inArrears,
                              100.0, new Date(24,Month.September,2004));

         floatingBond2.setPricingEngine(bondEngine);

         Utils.setCouponPricer(floatingBond2.cashflows(), vars.pricer);
         vars.iborIndex.addFixing(new Date(22,Month.March,2007), 0.04013);
         // market price observed on 7th June 2007
         double floatingBondMktPrice2 = 101.248 ;
         double floatingBondMktFullPrice2 = floatingBondMktPrice2+floatingBond2.accruedAmount();
         AssetSwap floatingBondParAssetSwap2= new AssetSwap(payFixedRate,
                                             floatingBond2, floatingBondMktPrice2,
                                             vars.iborIndex, vars.spread,
                                             null,
                                             vars.iborIndex.dayCounter(),
                                             parAssetSwap);
         floatingBondParAssetSwap2.setPricingEngine(swapEngine);
         double floatingBondParAssetSwapSpread2 = floatingBondParAssetSwap2.fairSpread();
         AssetSwap floatingBondMktAssetSwap2 = new AssetSwap(payFixedRate,
                                             floatingBond2, floatingBondMktPrice2,
                                             vars.iborIndex, vars.spread,
                                             null,
                                             vars.iborIndex.dayCounter(),
                                             mktAssetSwap);
         floatingBondMktAssetSwap2.setPricingEngine(swapEngine);
         double floatingBondMktAssetSwapSpread2 = floatingBondMktAssetSwap2.fairSpread();
         double error4 = Math.Abs(floatingBondMktAssetSwapSpread2-
                     100*floatingBondParAssetSwapSpread2/floatingBondMktFullPrice2);

         if (error4>tolerance) {
            Assert.Fail("wrong asset swap spreads for floating bond:" +
                        "\n  market ASW spread: " + floatingBondMktAssetSwapSpread2 +
                        "\n  par ASW spread:    " + floatingBondParAssetSwapSpread2 +
                        "\n  error:             " + error4 +
                        "\n  tolerance:         " + tolerance);
         }

         // CMS Underlying bond (Isin: XS0228052402 CRDIT 0 8/22/20)
         // maturity doesn't occur on a business day

         Schedule cmsBondSchedule1 = new Schedule( new Date(22,Month.August,2005),
                                 new Date(22,Month.August,2020),
                                 new Period(Frequency.Annual), bondCalendar,
                                 BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                 DateGeneration.Rule.Backward, false);
         Bond cmsBond1 = new CmsRateBond(settlementDays, vars.faceAmount, cmsBondSchedule1,
                        vars.swapIndex, new Thirty360(),
                        BusinessDayConvention.Following, fixingDays,
                        new List<double>{1.0}, new List<double>{0.0},
                        new List<double>{0.055}, new List<double>{0.025},
                        inArrears,
                        100.0, new Date(22,Month.August,2005));

         cmsBond1.setPricingEngine(bondEngine);

         Utils.setCouponPricer(cmsBond1.cashflows(), vars.cmspricer);
         vars.swapIndex.addFixing(new Date(18,Month.August,2006), 0.04158);
         double cmsBondMktPrice1 = 88.45 ; // market price observed on 7th June 2007
         double cmsBondMktFullPrice1 = cmsBondMktPrice1+cmsBond1.accruedAmount();
         AssetSwap cmsBondParAssetSwap1 = new AssetSwap(payFixedRate,
                                       cmsBond1, cmsBondMktPrice1,
                                       vars.iborIndex, vars.spread,
                                       null,
                                       vars.iborIndex.dayCounter(),
                                       parAssetSwap);
         cmsBondParAssetSwap1.setPricingEngine(swapEngine);
         double cmsBondParAssetSwapSpread1 = cmsBondParAssetSwap1.fairSpread();
         AssetSwap cmsBondMktAssetSwap1 = new AssetSwap(payFixedRate,
                                       cmsBond1, cmsBondMktPrice1,
                                       vars.iborIndex, vars.spread,
                                       null,
                                       vars.iborIndex.dayCounter(),
                                       mktAssetSwap);
         cmsBondMktAssetSwap1.setPricingEngine(swapEngine);
         double cmsBondMktAssetSwapSpread1 = cmsBondMktAssetSwap1.fairSpread();
         double error5 = Math.Abs(cmsBondMktAssetSwapSpread1-
                     100*cmsBondParAssetSwapSpread1/cmsBondMktFullPrice1);

         if (error5>tolerance) {
            Assert.Fail("wrong asset swap spreads for cms bond:" +
                        "\n  market ASW spread: " + cmsBondMktAssetSwapSpread1 +
                        "\n  par ASW spread:    " + cmsBondParAssetSwapSpread1 +
                        "\n  error:             " + error5 +
                        "\n  tolerance:         " + tolerance);
         }

         // CMS Underlying bond (Isin: XS0218766664 ISPIM 0 5/6/15)
         // maturity occurs on a business day

         Schedule cmsBondSchedule2 = new Schedule(new Date(06,Month.May,2005),
                                 new Date(06,Month.May,2015),
                                 new Period(Frequency.Annual), bondCalendar,
                                 BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                 DateGeneration.Rule.Backward, false);
         Bond cmsBond2 = new CmsRateBond(settlementDays, vars.faceAmount, cmsBondSchedule2,
                        vars.swapIndex, new Thirty360(),
                        BusinessDayConvention.Following, fixingDays,
                        new List<double>{0.84}, new List<double>{0.0},
                        new List<double>(), new List<double>(),
                        inArrears,
                        100.0, new Date(06,Month.May,2005));

         cmsBond2.setPricingEngine(bondEngine);

         Utils.setCouponPricer(cmsBond2.cashflows(), vars.cmspricer);
         vars.swapIndex.addFixing(new Date(04,Month.May,2006), 0.04217);
         double cmsBondMktPrice2 = 94.08 ; // market price observed on 7th June 2007
         double cmsBondMktFullPrice2 = cmsBondMktPrice2+cmsBond2.accruedAmount();
         AssetSwap cmsBondParAssetSwap2 = new AssetSwap(payFixedRate,
                                       cmsBond2, cmsBondMktPrice2,
                                       vars.iborIndex, vars.spread,
                                       null,
                                       vars.iborIndex.dayCounter(),
                                       parAssetSwap);
         cmsBondParAssetSwap2.setPricingEngine(swapEngine);
         double cmsBondParAssetSwapSpread2 = cmsBondParAssetSwap2.fairSpread();
         AssetSwap cmsBondMktAssetSwap2 = new AssetSwap(payFixedRate,
                                       cmsBond2, cmsBondMktPrice2,
                                       vars.iborIndex, vars.spread,
                                       null,
                                       vars.iborIndex.dayCounter(),
                                       mktAssetSwap);
         cmsBondMktAssetSwap2.setPricingEngine(swapEngine);
         double cmsBondMktAssetSwapSpread2 = cmsBondMktAssetSwap2.fairSpread();
         double error6 = Math.Abs(cmsBondMktAssetSwapSpread2-
                     100*cmsBondParAssetSwapSpread2/cmsBondMktFullPrice2);

         if (error6>tolerance) {
            Assert.Fail("wrong asset swap spreads for cms bond:" +
                        "\n  market ASW spread: " + cmsBondMktAssetSwapSpread2 +
                        "\n  par ASW spread:    " + cmsBondParAssetSwapSpread2 +
                        "\n  error:             " + error6 +
                        "\n  tolerance:         " + tolerance);
         }

         // Zero Coupon bond (Isin: DE0004771662 IBRD 0 12/20/15)
         // maturity doesn't occur on a business day

         Bond zeroCpnBond1 = new ZeroCouponBond(settlementDays, bondCalendar, vars.faceAmount,
                          new  Date(20,Month.December,2015), BusinessDayConvention.Following,
                           100.0, new Date(19,Month.December,1985));

         zeroCpnBond1.setPricingEngine(bondEngine);

         // market price observed on 12th June 2007
         double zeroCpnBondMktPrice1 = 70.436 ;
         double zeroCpnBondMktFullPrice1 = zeroCpnBondMktPrice1+zeroCpnBond1.accruedAmount();
         AssetSwap zeroCpnBondParAssetSwap1 = new AssetSwap(payFixedRate,zeroCpnBond1,
                                          zeroCpnBondMktPrice1,
                                          vars.iborIndex, vars.spread,
                                          null,
                                          vars.iborIndex.dayCounter(),
                                          parAssetSwap);
         zeroCpnBondParAssetSwap1.setPricingEngine(swapEngine);
         double zeroCpnBondParAssetSwapSpread1 = zeroCpnBondParAssetSwap1.fairSpread();
         AssetSwap zeroCpnBondMktAssetSwap1 = new AssetSwap(payFixedRate,zeroCpnBond1,
                                          zeroCpnBondMktPrice1,
                                          vars.iborIndex, vars.spread,
                                          null,
                                          vars.iborIndex.dayCounter(),
                                          mktAssetSwap);
         zeroCpnBondMktAssetSwap1.setPricingEngine(swapEngine);
         double zeroCpnBondMktAssetSwapSpread1 = zeroCpnBondMktAssetSwap1.fairSpread();
         double error7 = Math.Abs(zeroCpnBondMktAssetSwapSpread1-
                     100*zeroCpnBondParAssetSwapSpread1/zeroCpnBondMktFullPrice1);

         if (error7>tolerance) {
            Assert.Fail("wrong asset swap spreads for zero cpn bond:" +
                        "\n  market ASW spread: " + zeroCpnBondMktAssetSwapSpread1 +
                        "\n  par ASW spread:    " + zeroCpnBondParAssetSwapSpread1 +
                        "\n  error:             " + error7 +
                        "\n  tolerance:         " + tolerance);
         }

         // Zero Coupon bond (Isin: IT0001200390 ISPIM 0 02/17/28)
         // maturity occurs on a business day

         Bond zeroCpnBond2 = new ZeroCouponBond(settlementDays, bondCalendar, vars.faceAmount,
                           new Date(17,Month.February,2028),
                           BusinessDayConvention.Following,
                           100.0, new Date(17,Month.February,1998));

         zeroCpnBond2.setPricingEngine(bondEngine);

         // Real zeroCpnBondPrice2 = zeroCpnBond2->cleanPrice();

         // market price observed on 12th June 2007
         double zeroCpnBondMktPrice2 = 35.160 ;
         double zeroCpnBondMktFullPrice2 = zeroCpnBondMktPrice2+zeroCpnBond2.accruedAmount();
         AssetSwap zeroCpnBondParAssetSwap2 = new AssetSwap(payFixedRate,zeroCpnBond2,
                                          zeroCpnBondMktPrice2,
                                          vars.iborIndex, vars.spread,
                                          null,
                                          vars.iborIndex.dayCounter(),
                                          parAssetSwap);
         zeroCpnBondParAssetSwap2.setPricingEngine(swapEngine);
         double zeroCpnBondParAssetSwapSpread2 = zeroCpnBondParAssetSwap2.fairSpread();
         AssetSwap zeroCpnBondMktAssetSwap2 = new AssetSwap(payFixedRate,zeroCpnBond2,
                                          zeroCpnBondMktPrice2,
                                          vars.iborIndex, vars.spread,
                                          null,
                                          vars.iborIndex.dayCounter(),
                                          mktAssetSwap);
         zeroCpnBondMktAssetSwap2.setPricingEngine(swapEngine);
         double zeroCpnBondMktAssetSwapSpread2 = zeroCpnBondMktAssetSwap2.fairSpread();
         double error8 = Math.Abs(zeroCpnBondMktAssetSwapSpread2-
                     100*zeroCpnBondParAssetSwapSpread2/zeroCpnBondMktFullPrice2);

         if (error8>tolerance) {
            Assert.Fail("wrong asset swap spreads for zero cpn bond:" +
                        "\n  market ASW spread: " + zeroCpnBondMktAssetSwapSpread2 +
                        "\n  par ASW spread:    " + zeroCpnBondParAssetSwapSpread2 +
                        "\n  error:             " + error8 +
                        "\n  tolerance:         " + tolerance);
         }
   }

      [TestMethod()]
      public void testZSpread() 
      {
         // Testing clean and dirty price with null Z-spread against theoretical prices...
         CommonVars vars = new CommonVars();

         Calendar bondCalendar = new TARGET();
         int settlementDays = 3;
         int fixingDays = 2;
         bool inArrears = false;

         // Fixed bond (Isin: DE0001135275 DBR 4 01/04/37)
         // maturity doesn't occur on a business day

         Schedule fixedBondSchedule1 = new Schedule(new Date(4,Month.January,2005),
                                    new Date(4,Month.January,2037),
                                    new Period(Frequency.Annual), bondCalendar,
                                    BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                    DateGeneration.Rule.Backward, false);
         Bond fixedBond1 = new FixedRateBond(settlementDays, vars.faceAmount, fixedBondSchedule1,
                           new List<double>{0.04},
                           new ActualActual(ActualActual.Convention.ISDA), BusinessDayConvention.Following,
                           100.0, new Date(4,Month.January,2005));

         IPricingEngine bondEngine = new DiscountingBondEngine(vars.termStructure);
         fixedBond1.setPricingEngine(bondEngine);

         double fixedBondImpliedValue1 = fixedBond1.cleanPrice();
         Date fixedBondSettlementDate1= fixedBond1.settlementDate();
         // standard market conventions:
         // bond's frequency + coumpounding and daycounter of the YC...
         double fixedBondCleanPrice1 = BondFunctions.cleanPrice( fixedBond1, vars.termStructure, vars.spread,
            new Actual365Fixed(), vars.compounding, Frequency.Annual, fixedBondSettlementDate1);
         double tolerance = 1.0e-13;
         double error1 = Math.Abs(fixedBondImpliedValue1-fixedBondCleanPrice1);
         if (error1>tolerance) {
            Assert.Fail("wrong clean price for fixed bond:" +
                        "\n  market asset swap spread: " +
                        fixedBondImpliedValue1 +
                        "\n  par asset swap spread: " + fixedBondCleanPrice1 +
                        "\n  error:                 " + error1 +
                        "\n  tolerance:             " + tolerance);
         }

         // Fixed bond (Isin: IT0006527060 IBRD 5 02/05/19)
         // maturity occurs on a business day

         Schedule fixedBondSchedule2 = new Schedule(new Date(5,Month.February,2005),
                                    new Date(5,Month.February,2019),
                                    new Period(Frequency.Annual), bondCalendar,
                                    BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                    DateGeneration.Rule.Backward, false);
         Bond fixedBond2 = new FixedRateBond(settlementDays, vars.faceAmount, fixedBondSchedule2,
                           new List<double>{0.05},
                           new Thirty360(Thirty360.Thirty360Convention.BondBasis), BusinessDayConvention.Following,
                           100.0, new Date(5,Month.February,2005));

         fixedBond2.setPricingEngine(bondEngine);

         double fixedBondImpliedValue2 = fixedBond2.cleanPrice();
         Date fixedBondSettlementDate2= fixedBond2.settlementDate();
         // standard market conventions:
         // bond's frequency + coumpounding and daycounter of the YieldCurve
         double fixedBondCleanPrice2 = BondFunctions.cleanPrice(fixedBond2, vars.termStructure, vars.spread,
            new Actual365Fixed(), vars.compounding, Frequency.Annual, fixedBondSettlementDate2);
         double error3 = Math.Abs(fixedBondImpliedValue2-fixedBondCleanPrice2);
         if (error3>tolerance) {
            Assert.Fail("wrong clean price for fixed bond:" +
                        "\n  market asset swap spread: " +
                        fixedBondImpliedValue2 +
                        "\n  par asset swap spread: " + fixedBondCleanPrice2 +
                        "\n  error:                 " + error3 +
                        "\n  tolerance:             " + tolerance);
         }

         // FRN bond (Isin: IT0003543847 ISPIM 0 09/29/13)
         // maturity doesn't occur on a business day

         Schedule floatingBondSchedule1 = new Schedule(new Date(29,Month.September,2003),
                                       new Date(29,Month.September,2013),
                                       new Period(Frequency.Semiannual), bondCalendar,
                                       BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                       DateGeneration.Rule.Backward, false);

         Bond floatingBond1 = new FloatingRateBond(settlementDays, vars.faceAmount,
                              floatingBondSchedule1,
                              vars.iborIndex, new Actual360(),
                              BusinessDayConvention.Following, fixingDays,
                              new List<double>{1}, new List<double>{0.0056},
                              new List<double>(), new List<double>(),
                              inArrears,
                              100.0, new Date(29,Month.September,2003));

         floatingBond1.setPricingEngine(bondEngine);

         Utils.setCouponPricer(floatingBond1.cashflows(), vars.pricer);
         vars.iborIndex.addFixing(new Date(27,Month.March,2007), 0.0402);
         double floatingBondImpliedValue1 = floatingBond1.cleanPrice();
         // standard market conventions:
         // bond's frequency + coumpounding and daycounter of the YieldCurve
         double floatingBondCleanPrice1 = BondFunctions.cleanPrice(floatingBond1, vars.termStructure, vars.spread,
            new Actual365Fixed(), vars.compounding, Frequency.Semiannual, fixedBondSettlementDate1);
         double error5 = Math.Abs(floatingBondImpliedValue1-floatingBondCleanPrice1);
         if (error5>tolerance) {
            Assert.Fail("wrong clean price for fixed bond:" +
                        "\n  market asset swap spread: " +
                        floatingBondImpliedValue1 +
                        "\n  par asset swap spread: " + floatingBondCleanPrice1 +
                        "\n  error:                 " + error5 +
                        "\n  tolerance:             " + tolerance);
         }

         // FRN bond (Isin: XS0090566539 COE 0 09/24/18)
         // maturity occurs on a business day

         Schedule floatingBondSchedule2 = new Schedule(new Date(24,Month.September,2004),
                                       new Date(24,Month.September,2018),
                                       new Period(Frequency.Semiannual), bondCalendar,
                                       BusinessDayConvention.ModifiedFollowing, BusinessDayConvention.ModifiedFollowing,
                                       DateGeneration.Rule.Backward, false);
         Bond floatingBond2 = new FloatingRateBond(settlementDays, vars.faceAmount,
                              floatingBondSchedule2,
                              vars.iborIndex, new Actual360(),
                              BusinessDayConvention.ModifiedFollowing, fixingDays,
                              new List<double>{1}, new List<double>{0.0025},
                              new List<double>(), new List<double>(),
                              inArrears,
                              100.0, new Date(24,Month.September,2004));

         floatingBond2.setPricingEngine(bondEngine);

         Utils.setCouponPricer(floatingBond2.cashflows(), vars.pricer);
         vars.iborIndex.addFixing(new Date(22,Month.March,2007), 0.04013);
         double floatingBondImpliedValue2 = floatingBond2.cleanPrice();
         // standard market conventions:
         // bond's frequency + coumpounding and daycounter of the YieldCurve
         double floatingBondCleanPrice2 = BondFunctions.cleanPrice(floatingBond2, vars.termStructure,
            vars.spread, new Actual365Fixed(), vars.compounding, Frequency.Semiannual, fixedBondSettlementDate1);
         double error7 = Math.Abs(floatingBondImpliedValue2-floatingBondCleanPrice2);
         if (error7>tolerance) {
            Assert.Fail("wrong clean price for fixed bond:"
                        + "\n  market asset swap spread: " +
                        floatingBondImpliedValue2
                        + "\n  par asset swap spread: " + floatingBondCleanPrice2
                        + "\n  error:                 " + error7
                        + "\n  tolerance:             " + tolerance);
         }

         //// CMS bond (Isin: XS0228052402 CRDIT 0 8/22/20)
         //// maturity doesn't occur on a business day

         Schedule cmsBondSchedule1 = new Schedule(new Date(22,Month.August,2005),
                                 new Date(22,Month.August,2020),
                                 new Period(Frequency.Annual), bondCalendar,
                                 BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                 DateGeneration.Rule.Backward, false);
         Bond cmsBond1 = new CmsRateBond(settlementDays, vars.faceAmount, cmsBondSchedule1,
                        vars.swapIndex, new Thirty360(),
                        BusinessDayConvention.Following, fixingDays,
                        new List<double>{1.0}, new List<double>{0.0},
                        new List<double>{0.055}, new List<double>{0.025},
                        inArrears,
                        100.0, new Date(22,Month.August,2005));

         cmsBond1.setPricingEngine(bondEngine);

         Utils.setCouponPricer(cmsBond1.cashflows(), vars.cmspricer);
         vars.swapIndex.addFixing(new Date(18,Month.August,2006), 0.04158);
         double cmsBondImpliedValue1 = cmsBond1.cleanPrice();
         Date cmsBondSettlementDate1= cmsBond1.settlementDate();
         // standard market conventions:
         // bond's frequency + coumpounding and daycounter of the YieldCurve
         double cmsBondCleanPrice1 = BondFunctions.cleanPrice(cmsBond1, vars.termStructure, vars.spread,
            new Actual365Fixed(), vars.compounding, Frequency.Annual, cmsBondSettlementDate1);
         double error9 = Math.Abs(cmsBondImpliedValue1-cmsBondCleanPrice1);
         if (error9>tolerance) {
            Assert.Fail("wrong clean price for fixed bond:"
                        + "\n  market asset swap spread: " + cmsBondImpliedValue1
                        + "\n  par asset swap spread: " + cmsBondCleanPrice1
                        + "\n  error:                 " + error9
                        + "\n  tolerance:             " + tolerance);
         }

         // CMS bond (Isin: XS0218766664 ISPIM 0 5/6/15)
         // maturity occurs on a business day

         Schedule cmsBondSchedule2 = new Schedule(new Date(06,Month.May,2005),
                                 new Date(06,Month.May,2015),
                                 new Period(Frequency.Annual), bondCalendar,
                                 BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                 DateGeneration.Rule.Backward, false);
         Bond cmsBond2 = new  CmsRateBond(settlementDays, vars.faceAmount, cmsBondSchedule2,
                        vars.swapIndex, new Thirty360(),
                        BusinessDayConvention.Following, fixingDays,
                        new List<double>{0.84}, new List<double>{0.0},
                        new List<double>(), new List<double>(),
                        inArrears,
                        100.0, new Date(06,Month.May,2005));

         cmsBond2.setPricingEngine(bondEngine);

         Utils.setCouponPricer(cmsBond2.cashflows(), vars.cmspricer);
         vars.swapIndex.addFixing(new Date(04,Month.May,2006), 0.04217);
         double cmsBondImpliedValue2 = cmsBond2.cleanPrice();
         Date cmsBondSettlementDate2= cmsBond2.settlementDate();
         // standard market conventions:
         // bond's frequency + coumpounding and daycounter of the YieldCurve
         double cmsBondCleanPrice2 = BondFunctions.cleanPrice(cmsBond2, vars.termStructure, vars.spread,
            new Actual365Fixed(), vars.compounding, Frequency.Annual, cmsBondSettlementDate2);
         double error11 = Math.Abs(cmsBondImpliedValue2-cmsBondCleanPrice2);
         if (error11>tolerance) {
            Assert.Fail("wrong clean price for fixed bond:"
                        + "\n  market asset swap spread: " + cmsBondImpliedValue2
                        + "\n  par asset swap spread: " + cmsBondCleanPrice2
                        + "\n  error:                 " + error11
                        + "\n  tolerance:             " + tolerance);
         }

         // Zero-Coupon bond (Isin: DE0004771662 IBRD 0 12/20/15)
         // maturity doesn't occur on a business day

         Bond zeroCpnBond1 = new ZeroCouponBond(settlementDays, bondCalendar, vars.faceAmount,
                           new Date(20,Month.December,2015),
                           BusinessDayConvention.Following,
                           100.0, new Date(19,Month.December,1985));

         zeroCpnBond1.setPricingEngine(bondEngine);

         double zeroCpnBondImpliedValue1 = zeroCpnBond1.cleanPrice();
         Date zeroCpnBondSettlementDate1= zeroCpnBond1.settlementDate();
         // standard market conventions:
         // bond's frequency + coumpounding and daycounter of the YieldCurve
         double zeroCpnBondCleanPrice1 = BondFunctions.cleanPrice(zeroCpnBond1,vars.termStructure, vars.spread,
                                 new Actual365Fixed(), vars.compounding, Frequency.Annual, zeroCpnBondSettlementDate1);
         double error13 = Math.Abs(zeroCpnBondImpliedValue1-zeroCpnBondCleanPrice1);
         if (error13>tolerance) {
            Assert.Fail("wrong clean price for zero coupon bond:"
                        + "\n  zero cpn implied value: " +
                        zeroCpnBondImpliedValue1
                        + "\n  zero cpn price: " + zeroCpnBondCleanPrice1
                        + "\n  error:                 " + error13
                        + "\n  tolerance:             " + tolerance);
         }

         // Zero Coupon bond (Isin: IT0001200390 ISPIM 0 02/17/28)
         // maturity doesn't occur on a business day

         Bond zeroCpnBond2 = new ZeroCouponBond(settlementDays, bondCalendar, vars.faceAmount,
                           new Date(17,Month.February,2028),
                           BusinessDayConvention.Following,
                           100.0, new Date(17,Month.February,1998));

         zeroCpnBond2.setPricingEngine(bondEngine);

         double zeroCpnBondImpliedValue2 = zeroCpnBond2.cleanPrice();
         Date zeroCpnBondSettlementDate2= zeroCpnBond2.settlementDate();
         // standard market conventions:
         // bond's frequency + coumpounding and daycounter of the YieldCurve
         double zeroCpnBondCleanPrice2 = BondFunctions.cleanPrice(zeroCpnBond2,vars.termStructure, vars.spread,
                                 new Actual365Fixed(), vars.compounding, Frequency.Annual, zeroCpnBondSettlementDate2);
         double error15 = Math.Abs(zeroCpnBondImpliedValue2-zeroCpnBondCleanPrice2);
         if (error15>tolerance) {
            Assert.Fail("wrong clean price for zero coupon bond:"
                        + "\n  zero cpn implied value: " +
                        zeroCpnBondImpliedValue2
                        + "\n  zero cpn price: " + zeroCpnBondCleanPrice2
                        + "\n  error:                 " + error15
                        + "\n  tolerance:             " + tolerance);
         }
      }

      [TestMethod()]
      public void testGenericBondImplied() 
      {

         // Testing implied generic-bond value against asset-swap fair price with null spread...

         CommonVars vars = new CommonVars();

         Calendar bondCalendar = new TARGET();
         int settlementDays = 3;
         int fixingDays = 2;
         bool payFixeddouble = true;
         bool parAssetSwap = true;
         bool inArrears = false;

         // Fixed Underlying bond (Isin: DE0001135275 DBR 4 01/04/37)
         // maturity doesn't occur on a business day
         Date fixedBondStartDate1 =new Date(4,Month.January,2005);
         Date fixedBondMaturityDate1 =new Date(4,Month.January,2037);
         Schedule fixedBondSchedule1 = new Schedule(fixedBondStartDate1,
                                    fixedBondMaturityDate1,
                                    new Period(Frequency.Annual), bondCalendar,
                                    BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                    DateGeneration.Rule.Backward, false);
         List<CashFlow> fixedBondLeg1 = new FixedRateLeg(fixedBondSchedule1)
            .withCouponRates(0.04, new ActualActual(ActualActual.Convention.ISDA))
            .withNotionals(vars.faceAmount);
         Date fixedbondRedemption1 = bondCalendar.adjust(fixedBondMaturityDate1,
                                                         BusinessDayConvention.Following);
         fixedBondLeg1.Add((new SimpleCashFlow(100.0, fixedbondRedemption1)));
         Bond fixedBond1 = new Bond(settlementDays, bondCalendar, vars.faceAmount,
                  fixedBondMaturityDate1, fixedBondStartDate1, fixedBondLeg1);
         IPricingEngine bondEngine = new DiscountingBondEngine(vars.termStructure);
         IPricingEngine swapEngine= new DiscountingSwapEngine(vars.termStructure);
         fixedBond1.setPricingEngine(bondEngine);

         double fixedBondPrice1 = fixedBond1.cleanPrice();
         AssetSwap fixedBondAssetSwap1 = new AssetSwap(payFixeddouble,
                                       fixedBond1, fixedBondPrice1,
                                       vars.iborIndex, vars.spread,
                                       null,
                                       vars.iborIndex.dayCounter(),
                                       parAssetSwap);
         fixedBondAssetSwap1.setPricingEngine(swapEngine);
         double fixedBondAssetSwapPrice1 = fixedBondAssetSwap1.fairCleanPrice();
         double tolerance = 1.0e-13;
         double error1 = Math.Abs(fixedBondAssetSwapPrice1-fixedBondPrice1);

         if (error1>tolerance) {
            Assert.Fail("wrong zero spread asset swap price for fixed bond:"
                        + "\n  bond's clean price:    " + fixedBondPrice1
                        + "\n  asset swap fair price: " + fixedBondAssetSwapPrice1
                        + "\n  error:                 " + error1
                        + "\n  tolerance:             " + tolerance);
         }

         // Fixed Underlying bond (Isin: IT0006527060 IBRD 5 02/05/19)
         // maturity occurs on a business day
         Date fixedBondStartDate2 =new Date(5,Month.February,2005);
         Date fixedBondMaturityDate2 =new Date(5,Month.February,2019);
         Schedule fixedBondSchedule2= new Schedule(fixedBondStartDate2,
                                    fixedBondMaturityDate2,
                                    new Period(Frequency.Annual), bondCalendar,
                                    BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                    DateGeneration.Rule.Backward, false);
         List<CashFlow> fixedBondLeg2 = new FixedRateLeg(fixedBondSchedule2)
            .withCouponRates(0.05, new Thirty360(Thirty360.Thirty360Convention.BondBasis))
            .withNotionals(vars.faceAmount);
         Date fixedbondRedemption2 = bondCalendar.adjust(fixedBondMaturityDate2,BusinessDayConvention.Following);
         fixedBondLeg2.Add(new SimpleCashFlow(100.0, fixedbondRedemption2));
         Bond fixedBond2 = new Bond(settlementDays, bondCalendar, vars.faceAmount,
                  fixedBondMaturityDate2, fixedBondStartDate2, fixedBondLeg2);
         fixedBond2.setPricingEngine(bondEngine);

         double fixedBondPrice2 = fixedBond2.cleanPrice();
         AssetSwap fixedBondAssetSwap2= new AssetSwap(payFixeddouble,
                                       fixedBond2, fixedBondPrice2,
                                       vars.iborIndex, vars.spread,
                                       null,
                                       vars.iborIndex.dayCounter(),
                                       parAssetSwap);
         fixedBondAssetSwap2.setPricingEngine(swapEngine);
         double fixedBondAssetSwapPrice2 = fixedBondAssetSwap2.fairCleanPrice();
         double error2 = Math.Abs(fixedBondAssetSwapPrice2-fixedBondPrice2);

         if (error2>tolerance) {
            Assert.Fail("wrong zero spread asset swap price for fixed bond:"
                        + "\n  bond's clean price:    " + fixedBondPrice2
                        + "\n  asset swap fair price: " + fixedBondAssetSwapPrice2
                        + "\n  error:                 " + error2
                        + "\n  tolerance:             " + tolerance);
         }

         // FRN Underlying bond (Isin: IT0003543847 ISPIM 0 09/29/13)
         // maturity doesn't occur on a business day
         Date floatingBondStartDate1 =new Date(29,Month.September,2003);
         Date floatingBondMaturityDate1 =new Date(29,Month.September,2013);
         Schedule floatingBondSchedule1 = new Schedule(floatingBondStartDate1,
                                       floatingBondMaturityDate1,
                                       new Period(Frequency.Semiannual), bondCalendar,
                                       BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                       DateGeneration.Rule.Backward, false);
         List<CashFlow> floatingBondLeg1 = new IborLeg(floatingBondSchedule1, vars.iborIndex)
            .withPaymentDayCounter(new Actual360())
            .withFixingDays(fixingDays)
            .withSpreads(0.0056)
            .inArrears(inArrears)
            .withNotionals(vars.faceAmount);
         Date floatingbondRedemption1 =
            bondCalendar.adjust(floatingBondMaturityDate1, BusinessDayConvention.Following);
         floatingBondLeg1.Add(new SimpleCashFlow(100.0, floatingbondRedemption1));
         Bond floatingBond1 = new Bond(settlementDays, bondCalendar, vars.faceAmount,
                  floatingBondMaturityDate1, floatingBondStartDate1, floatingBondLeg1);
         floatingBond1.setPricingEngine(bondEngine);

         Utils.setCouponPricer(floatingBond1.cashflows(), vars.pricer);
         vars.iborIndex.addFixing(new Date(27,Month.March,2007), 0.0402);
         double floatingBondPrice1 = floatingBond1.cleanPrice();
         AssetSwap floatingBondAssetSwap1= new AssetSwap(payFixeddouble,
                                          floatingBond1, floatingBondPrice1,
                                          vars.iborIndex, vars.spread,
                                          null,
                                          vars.iborIndex.dayCounter(),
                                          parAssetSwap);
         floatingBondAssetSwap1.setPricingEngine(swapEngine);
         double floatingBondAssetSwapPrice1 = floatingBondAssetSwap1.fairCleanPrice();
         double error3 = Math.Abs(floatingBondAssetSwapPrice1-floatingBondPrice1);

         if (error3>tolerance) {
            Assert.Fail("wrong zero spread asset swap price for floater:"
                        + "\n  bond's clean price:    " + floatingBondPrice1
                        + "\n  asset swap fair price: " +
                        floatingBondAssetSwapPrice1
                        + "\n  error:                 " + error3
                        + "\n  tolerance:             " + tolerance);
         }

         // FRN Underlying bond (Isin: XS0090566539 COE 0 09/24/18)
         // maturity occurs on a business day
         Date floatingBondStartDate2 =new Date(24,Month.September,2004);
         Date floatingBondMaturityDate2 =new Date(24,Month.September,2018);
         Schedule floatingBondSchedule2 = new Schedule(floatingBondStartDate2,
                                       floatingBondMaturityDate2,
                                       new Period(Frequency.Semiannual), bondCalendar,
                                       BusinessDayConvention.ModifiedFollowing, BusinessDayConvention.ModifiedFollowing,
                                       DateGeneration.Rule.Backward, false);
         List<CashFlow> floatingBondLeg2 = new IborLeg(floatingBondSchedule2, vars.iborIndex)
            .withPaymentDayCounter(new Actual360())
            .withFixingDays(fixingDays)
            .withSpreads(0.0025)
            .inArrears(inArrears)
            .withNotionals(vars.faceAmount)
            .withPaymentAdjustment(BusinessDayConvention.ModifiedFollowing);
         Date floatingbondRedemption2 =
            bondCalendar.adjust(floatingBondMaturityDate2, BusinessDayConvention.ModifiedFollowing);
         floatingBondLeg2.Add(new SimpleCashFlow(100.0, floatingbondRedemption2));
         Bond floatingBond2 = new Bond(settlementDays, bondCalendar, vars.faceAmount,
                  floatingBondMaturityDate2, floatingBondStartDate2, floatingBondLeg2);
         floatingBond2.setPricingEngine(bondEngine);

         Utils.setCouponPricer(floatingBond2.cashflows(), vars.pricer);
         vars.iborIndex.addFixing(new Date(22,Month.March,2007), 0.04013);
         double currentCoupon=0.04013+0.0025;
         double floatingCurrentCoupon= floatingBond2.nextCouponRate();
         double error4= Math.Abs(floatingCurrentCoupon-currentCoupon);
         if (error4>tolerance) {
            Assert.Fail("wrong current coupon is returned for floater bond:"
                        + "\n  bond's calculated current coupon:      " +
                        currentCoupon
                        + "\n  current coupon asked to the bond: " +
                        floatingCurrentCoupon
                        + "\n  error:                 " + error4
                        + "\n  tolerance:             " + tolerance);
         }

         double floatingBondPrice2 = floatingBond2.cleanPrice();
         AssetSwap floatingBondAssetSwap2= new AssetSwap(payFixeddouble,
                                          floatingBond2, floatingBondPrice2,
                                          vars.iborIndex, vars.spread,
                                          null,
                                          vars.iborIndex.dayCounter(),
                                          parAssetSwap);
         floatingBondAssetSwap2.setPricingEngine(swapEngine);
         double floatingBondAssetSwapPrice2 = floatingBondAssetSwap2.fairCleanPrice();
         double error5 = Math.Abs(floatingBondAssetSwapPrice2-floatingBondPrice2);

         if (error5>tolerance) {
            Assert.Fail("wrong zero spread asset swap price for floater:"
                        + "\n  bond's clean price:    " + floatingBondPrice2
                        + "\n  asset swap fair price: " +
                        floatingBondAssetSwapPrice2
                        + "\n  error:                 " + error5
                        + "\n  tolerance:             " + tolerance);
         }

         // CMS Underlying bond (Isin: XS0228052402 CRDIT 0 8/22/20)
         // maturity doesn't occur on a business day
         Date cmsBondStartDate1 =new Date(22,Month.August,2005);
         Date cmsBondMaturityDate1 =new Date(22,Month.August,2020);
         Schedule cmsBondSchedule1= new Schedule(cmsBondStartDate1,
                                 cmsBondMaturityDate1,
                                 new Period(Frequency.Annual), bondCalendar,
                                 BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                 DateGeneration.Rule.Backward, false);
         List<CashFlow> cmsBondLeg1 = new CmsLeg(cmsBondSchedule1, vars.swapIndex)
            .withFixingDays(fixingDays)
            .withPaymentDayCounter(new Thirty360())
            .withCaps(0.055)
            .withFloors(0.025)
            .inArrears(inArrears)
            .withNotionals(vars.faceAmount);
         Date cmsbondRedemption1 = bondCalendar.adjust(cmsBondMaturityDate1, BusinessDayConvention.Following);
         cmsBondLeg1.Add( new SimpleCashFlow(100.0, cmsbondRedemption1));
         Bond cmsBond1 = new Bond(settlementDays, bondCalendar, vars.faceAmount,
                  cmsBondMaturityDate1, cmsBondStartDate1, cmsBondLeg1);
         cmsBond1.setPricingEngine(bondEngine);

         Utils.setCouponPricer(cmsBond1.cashflows(), vars.cmspricer);
         vars.swapIndex.addFixing(new Date(18,Month.August,2006), 0.04158);
         double cmsBondPrice1 = cmsBond1.cleanPrice();
         AssetSwap cmsBondAssetSwap1 = new AssetSwap(payFixeddouble,
                                    cmsBond1, cmsBondPrice1,
                                    vars.iborIndex, vars.spread,
                                    null,
                                    vars.iborIndex.dayCounter(),
                                    parAssetSwap);
         cmsBondAssetSwap1.setPricingEngine(swapEngine);
         double cmsBondAssetSwapPrice1 = cmsBondAssetSwap1.fairCleanPrice();
         double error6 = Math.Abs(cmsBondAssetSwapPrice1-cmsBondPrice1);

         if (error6>tolerance) {
            Assert.Fail("wrong zero spread asset swap price for cms bond:"
                        + "\n  bond's clean price:    " + cmsBondPrice1
                        + "\n  asset swap fair price: " + cmsBondAssetSwapPrice1
                        + "\n  error:                 " + error6
                        + "\n  tolerance:             " + tolerance);
         }

         // CMS Underlying bond (Isin: XS0218766664 ISPIM 0 5/6/15)
         // maturity occurs on a business day
         Date cmsBondStartDate2 =new Date(06,Month.May,2005);
         Date cmsBondMaturityDate2 =new Date(06,Month.May,2015);
         Schedule cmsBondSchedule2= new Schedule(cmsBondStartDate2,
                                 cmsBondMaturityDate2,
                                 new Period(Frequency.Annual), bondCalendar,
                                 BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                 DateGeneration.Rule.Backward, false);
         List<CashFlow> cmsBondLeg2 = new CmsLeg(cmsBondSchedule2, vars.swapIndex)
            .withFixingDays(fixingDays)
            .withGearings(0.84)
            .inArrears(inArrears)
            .withPaymentDayCounter(new Thirty360())
            .withNotionals(vars.faceAmount);
         Date cmsbondRedemption2 = bondCalendar.adjust(cmsBondMaturityDate2,BusinessDayConvention.Following);
         cmsBondLeg2.Add(new SimpleCashFlow(100.0, cmsbondRedemption2));
         Bond cmsBond2 = new Bond(settlementDays, bondCalendar, vars.faceAmount,
                  cmsBondMaturityDate2, cmsBondStartDate2, cmsBondLeg2);
         cmsBond2.setPricingEngine(bondEngine);

         Utils.setCouponPricer(cmsBond2.cashflows(), vars.cmspricer);
         vars.swapIndex.addFixing(new Date(04,Month.May,2006), 0.04217);
         double cmsBondPrice2 = cmsBond2.cleanPrice();
         AssetSwap cmsBondAssetSwap2= new AssetSwap(payFixeddouble,
                                    cmsBond2, cmsBondPrice2,
                                    vars.iborIndex, vars.spread,
                                    null,
                                    vars.iborIndex.dayCounter(),
                                    parAssetSwap);
         cmsBondAssetSwap2.setPricingEngine(swapEngine);
         double cmsBondAssetSwapPrice2 = cmsBondAssetSwap2.fairCleanPrice();
         double error7 = Math.Abs(cmsBondAssetSwapPrice2-cmsBondPrice2);

         if (error7>tolerance) {
            Assert.Fail("wrong zero spread asset swap price for cms bond:"
                        + "\n  bond's clean price:    " + cmsBondPrice2
                        + "\n  asset swap fair price: " + cmsBondAssetSwapPrice2
                        + "\n  error:                 " + error7
                        + "\n  tolerance:             " + tolerance);
         }

         // Zero Coupon bond (Isin: DE0004771662 IBRD 0 12/20/15)
         // maturity doesn't occur on a business day
         Date zeroCpnBondStartDate1 =new Date(19,Month.December,1985);
         Date zeroCpnBondMaturityDate1 =new Date(20,Month.December,2015);
         Date zeroCpnBondRedemption1 = bondCalendar.adjust(zeroCpnBondMaturityDate1,BusinessDayConvention.Following);
         List<CashFlow>zeroCpnBondLeg1 = new List<CashFlow>{new SimpleCashFlow(100.0, zeroCpnBondRedemption1)};
         Bond zeroCpnBond1 = new Bond(settlementDays, bondCalendar, vars.faceAmount,
                  zeroCpnBondMaturityDate1, zeroCpnBondStartDate1, zeroCpnBondLeg1);
         zeroCpnBond1.setPricingEngine(bondEngine);

         double zeroCpnBondPrice1 = zeroCpnBond1.cleanPrice();
         AssetSwap zeroCpnAssetSwap1 = new AssetSwap(payFixeddouble,
                                    zeroCpnBond1, zeroCpnBondPrice1,
                                    vars.iborIndex, vars.spread,
                                    null,
                                    vars.iborIndex.dayCounter(),
                                    parAssetSwap);
         zeroCpnAssetSwap1.setPricingEngine(swapEngine);
         double zeroCpnBondAssetSwapPrice1 = zeroCpnAssetSwap1.fairCleanPrice();
         double error8 = Math.Abs(zeroCpnBondAssetSwapPrice1-zeroCpnBondPrice1);

         if (error8>tolerance) {
            Assert.Fail("wrong zero spread asset swap price for zero cpn bond:"
                        + "\n  bond's clean price:    " + zeroCpnBondPrice1
                        + "\n  asset swap fair price: " + zeroCpnBondAssetSwapPrice1
                        + "\n  error:                 " + error8
                        + "\n  tolerance:             " + tolerance);
         }

         // Zero Coupon bond (Isin: IT0001200390 ISPIM 0 02/17/28)
         // maturity occurs on a business day
         Date zeroCpnBondStartDate2 =new Date(17,Month.February,1998);
         Date zeroCpnBondMaturityDate2 =new Date(17,Month.February,2028);
         Date zerocpbondRedemption2 = bondCalendar.adjust(zeroCpnBondMaturityDate2,BusinessDayConvention.Following);
         List<CashFlow>zeroCpnBondLeg2 = new List<CashFlow>{new SimpleCashFlow(100.0, zerocpbondRedemption2)};
         Bond zeroCpnBond2 = new  Bond(settlementDays, bondCalendar, vars.faceAmount,
                  zeroCpnBondMaturityDate2, zeroCpnBondStartDate2, zeroCpnBondLeg2);
         zeroCpnBond2.setPricingEngine(bondEngine);

         double zeroCpnBondPrice2 = zeroCpnBond2.cleanPrice();
         AssetSwap zeroCpnAssetSwap2= new AssetSwap(payFixeddouble,
                                    zeroCpnBond2, zeroCpnBondPrice2,
                                    vars.iborIndex, vars.spread,
                                    null,
                                    vars.iborIndex.dayCounter(),
                                    parAssetSwap);
         zeroCpnAssetSwap2.setPricingEngine(swapEngine);
         double zeroCpnBondAssetSwapPrice2 = zeroCpnAssetSwap2.fairCleanPrice();
         double error9 = Math.Abs(cmsBondAssetSwapPrice2-cmsBondPrice2);

         if (error9>tolerance) {
            Assert.Fail("wrong zero spread asset swap price for zero cpn bond:"
                        + "\n  bond's clean price:    " + zeroCpnBondPrice2
                        + "\n  asset swap fair price: " + zeroCpnBondAssetSwapPrice2
                        + "\n  error:                 " + error9
                        + "\n  tolerance:             " + tolerance);
         }
   }

      [TestMethod()]
      public void testMASWWithGenericBond() 
      {
         // Testing market asset swap against par asset swap with generic bond...

         CommonVars vars = new CommonVars();

         Calendar bondCalendar = new TARGET();
         int settlementDays = 3;
         int fixingDays = 2;
         bool payFixedRate = true;
         bool parAssetSwap = true;
         bool mktAssetSwap = false;
         bool inArrears = false;

         // Fixed Underlying bond (Isin: DE0001135275 DBR 4 01/04/37)
         // maturity doesn't occur on a business day

         Date fixedBondStartDate1 = new Date(4,Month.January,2005);
         Date fixedBondMaturityDate1 = new Date(4,Month.January,2037);
         Schedule fixedBondSchedule1 = new Schedule(fixedBondStartDate1,
                                    fixedBondMaturityDate1,
                                    new Period(Frequency.Annual), bondCalendar,
                                    BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                    DateGeneration.Rule.Backward, false);
         List<CashFlow> fixedBondLeg1 = new FixedRateLeg(fixedBondSchedule1)
            .withCouponRates(0.04, new ActualActual(ActualActual.Convention.ISDA))
            .withNotionals(vars.faceAmount);
         Date fixedbondRedemption1 = bondCalendar.adjust(fixedBondMaturityDate1,   BusinessDayConvention.Following);
         fixedBondLeg1.Add(new SimpleCashFlow(100.0, fixedbondRedemption1));
         Bond fixedBond1 = new Bond(settlementDays, bondCalendar, vars.faceAmount, fixedBondMaturityDate1, 
            fixedBondStartDate1, fixedBondLeg1);
         IPricingEngine bondEngine = new DiscountingBondEngine(vars.termStructure);
         IPricingEngine swapEngine = new DiscountingSwapEngine(vars.termStructure);
         fixedBond1.setPricingEngine(bondEngine);

         double fixedBondMktPrice1 = 89.22 ; // market price observed on 7th June 2007
         double fixedBondMktFullPrice1=fixedBondMktPrice1+fixedBond1.accruedAmount();
         AssetSwap fixedBondParAssetSwap1= new AssetSwap(payFixedRate,
                                          fixedBond1, fixedBondMktPrice1,
                                          vars.iborIndex, vars.spread,
                                          null,
                                          vars.iborIndex.dayCounter(),
                                          parAssetSwap);
         fixedBondParAssetSwap1.setPricingEngine(swapEngine);
         double fixedBondParAssetSwapSpread1 = fixedBondParAssetSwap1.fairSpread();
         AssetSwap fixedBondMktAssetSwap1 = new AssetSwap(payFixedRate,
                                          fixedBond1, fixedBondMktPrice1,
                                          vars.iborIndex, vars.spread,
                                          null,
                                          vars.iborIndex.dayCounter(),
                                          mktAssetSwap);
         fixedBondMktAssetSwap1.setPricingEngine(swapEngine);
         double fixedBondMktAssetSwapSpread1 = fixedBondMktAssetSwap1.fairSpread();

         double tolerance = 1.0e-13;
         double error1 =
            Math.Abs(fixedBondMktAssetSwapSpread1-
                     100*fixedBondParAssetSwapSpread1/fixedBondMktFullPrice1);

         if (error1>tolerance)
            Assert.Fail("wrong asset swap spreads for fixed bond:" +
                        "\n  market asset swap spread: " + fixedBondMktAssetSwapSpread1 +
                        "\n  par asset swap spread:    " + fixedBondParAssetSwapSpread1 +
                        "\n  error:                    " + error1 +
                        "\n  tolerance:                " + tolerance);

         // Fixed Underlying bond (Isin: IT0006527060 IBRD 5 02/05/19)
         // maturity occurs on a business day

         Date fixedBondStartDate2 = new Date(5,Month.February,2005);
         Date fixedBondMaturityDate2 = new Date(5,Month.February,2019);
         Schedule fixedBondSchedule2 = new Schedule(fixedBondStartDate2,
                                    fixedBondMaturityDate2,
                                    new Period(Frequency.Annual), bondCalendar,
                                    BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                    DateGeneration.Rule.Backward, false);
         List<CashFlow> fixedBondLeg2 = new FixedRateLeg(fixedBondSchedule2)
            .withCouponRates(0.05, new Thirty360(Thirty360.Thirty360Convention.BondBasis))
            .withNotionals(vars.faceAmount);
         Date fixedbondRedemption2 = bondCalendar.adjust(fixedBondMaturityDate2,  BusinessDayConvention.Following);
         fixedBondLeg2.Add(new SimpleCashFlow(100.0, fixedbondRedemption2));
         Bond fixedBond2 = new Bond(settlementDays, bondCalendar, vars.faceAmount, fixedBondMaturityDate2, fixedBondStartDate2, 
            fixedBondLeg2);
         fixedBond2.setPricingEngine(bondEngine);

         double fixedBondMktPrice2 = 99.98 ; // market price observed on 7th June 2007
         double fixedBondMktFullPrice2=fixedBondMktPrice2+fixedBond2.accruedAmount();
         AssetSwap fixedBondParAssetSwap2= new AssetSwap(payFixedRate,
                                          fixedBond2, fixedBondMktPrice2,
                                          vars.iborIndex, vars.spread,
                                          null,
                                          vars.iborIndex.dayCounter(),
                                          parAssetSwap);
         fixedBondParAssetSwap2.setPricingEngine(swapEngine);
         double fixedBondParAssetSwapSpread2 = fixedBondParAssetSwap2.fairSpread();
         AssetSwap fixedBondMktAssetSwap2= new AssetSwap(payFixedRate,
                                          fixedBond2, fixedBondMktPrice2,
                                          vars.iborIndex, vars.spread,
                                          null,
                                          vars.iborIndex.dayCounter(),
                                          mktAssetSwap);
         fixedBondMktAssetSwap2.setPricingEngine(swapEngine);
         double fixedBondMktAssetSwapSpread2 = fixedBondMktAssetSwap2.fairSpread();
         double error2 = Math.Abs(fixedBondMktAssetSwapSpread2-
                     100*fixedBondParAssetSwapSpread2/fixedBondMktFullPrice2);

         if (error2>tolerance)
            Assert.Fail("wrong asset swap spreads for fixed bond:" +
                        "\n  market asset swap spread: " + fixedBondMktAssetSwapSpread2 +
                        "\n  par asset swap spread:    " + fixedBondParAssetSwapSpread2 +
                        "\n  error:                    " + error2 +
                        "\n  tolerance:                " + tolerance);

         // FRN Underlying bond (Isin: IT0003543847 ISPIM 0 09/29/13)
         // maturity doesn't occur on a business day

         Date floatingBondStartDate1 = new Date(29,Month.September,2003);
         Date floatingBondMaturityDate1 = new Date(29,Month.September,2013);
         Schedule floatingBondSchedule1= new Schedule(floatingBondStartDate1,
                                       floatingBondMaturityDate1,
                                       new Period(Frequency.Semiannual), bondCalendar,
                                       BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                       DateGeneration.Rule.Backward, false);
         List<CashFlow> floatingBondLeg1 = new IborLeg(floatingBondSchedule1, vars.iborIndex)
            .withPaymentDayCounter(new Actual360())
            .withFixingDays(fixingDays)
            .withSpreads(0.0056)
            .inArrears(inArrears)
            .withNotionals(vars.faceAmount);
         Date floatingbondRedemption1 =
            bondCalendar.adjust(floatingBondMaturityDate1, BusinessDayConvention.Following);
         floatingBondLeg1.Add(new SimpleCashFlow(100.0, floatingbondRedemption1));
         Bond floatingBond1 = new Bond(settlementDays, bondCalendar, vars.faceAmount,  floatingBondMaturityDate1, 
            floatingBondStartDate1, floatingBondLeg1);
         floatingBond1.setPricingEngine(bondEngine);

         Utils.setCouponPricer(floatingBond1.cashflows(), vars.pricer);
         vars.iborIndex.addFixing(new Date(27,Month.March,2007), 0.0402);
         // market price observed on 7th June 2007
         double floatingBondMktPrice1 = 101.64 ;
         double floatingBondMktFullPrice1 =
            floatingBondMktPrice1+floatingBond1.accruedAmount();
         AssetSwap floatingBondParAssetSwap1 = new AssetSwap(payFixedRate,
                                             floatingBond1, floatingBondMktPrice1,
                                             vars.iborIndex, vars.spread,
                                             null,
                                             vars.iborIndex.dayCounter(),
                                             parAssetSwap);
         floatingBondParAssetSwap1.setPricingEngine(swapEngine);
         double floatingBondParAssetSwapSpread1 =
            floatingBondParAssetSwap1.fairSpread();
         AssetSwap floatingBondMktAssetSwap1 = new AssetSwap(payFixedRate,
                                             floatingBond1, floatingBondMktPrice1,
                                             vars.iborIndex, vars.spread,
                                             null,
                                             vars.iborIndex.dayCounter(),
                                             mktAssetSwap);
         floatingBondMktAssetSwap1.setPricingEngine(swapEngine);
         double floatingBondMktAssetSwapSpread1 =
            floatingBondMktAssetSwap1.fairSpread();
         double error3 = Math.Abs(floatingBondMktAssetSwapSpread1-
                     100*floatingBondParAssetSwapSpread1/floatingBondMktFullPrice1);

         if (error3>tolerance)
            Assert.Fail("wrong asset swap spreads for floating bond:" +
                        "\n  market asset swap spread: " + floatingBondMktAssetSwapSpread1 +
                        "\n  par asset swap spread:    " + floatingBondParAssetSwapSpread1 +
                        "\n  error:                    " + error3 +
                        "\n  tolerance:                " + tolerance);

         // FRN Underlying bond (Isin: XS0090566539 COE 0 09/24/18)
         // maturity occurs on a business day

         Date floatingBondStartDate2 = new Date(24,Month.September,2004);
         Date floatingBondMaturityDate2 = new Date(24,Month.September,2018);
         Schedule floatingBondSchedule2 = new Schedule(floatingBondStartDate2,
                                       floatingBondMaturityDate2,
                                       new Period(Frequency.Semiannual), bondCalendar,
                                       BusinessDayConvention.ModifiedFollowing, BusinessDayConvention.ModifiedFollowing,
                                       DateGeneration.Rule.Backward, false);
         List<CashFlow> floatingBondLeg2 = new IborLeg(floatingBondSchedule2, vars.iborIndex)
            .withFixingDays(fixingDays)
            .withSpreads(0.0025)
            .inArrears(inArrears)
            .withPaymentDayCounter(new Actual360())
            .withPaymentAdjustment(BusinessDayConvention.ModifiedFollowing)
            .withNotionals(vars.faceAmount);
         Date floatingbondRedemption2 =
            bondCalendar.adjust(floatingBondMaturityDate2, BusinessDayConvention.ModifiedFollowing);
         floatingBondLeg2.Add(new
            SimpleCashFlow(100.0, floatingbondRedemption2));
         Bond floatingBond2 = new Bond(settlementDays, bondCalendar, vars.faceAmount, floatingBondMaturityDate2, 
            floatingBondStartDate2, floatingBondLeg2);
         floatingBond2.setPricingEngine(bondEngine);

         Utils.setCouponPricer(floatingBond2.cashflows(), vars.pricer);
         vars.iborIndex.addFixing(new Date(22,Month.March,2007), 0.04013);
         // market price observed on 7th June 2007
         double floatingBondMktPrice2 = 101.248 ;
         double floatingBondMktFullPrice2 =
            floatingBondMktPrice2+floatingBond2.accruedAmount();
         AssetSwap floatingBondParAssetSwap2 = new AssetSwap(payFixedRate,
                                             floatingBond2, floatingBondMktPrice2,
                                             vars.iborIndex, vars.spread,
                                             null,
                                             vars.iborIndex.dayCounter(),
                                             parAssetSwap);
         floatingBondParAssetSwap2.setPricingEngine(swapEngine);
         double floatingBondParAssetSwapSpread2 = floatingBondParAssetSwap2.fairSpread();
         AssetSwap floatingBondMktAssetSwap2 = new AssetSwap(payFixedRate,
                                             floatingBond2, floatingBondMktPrice2,
                                             vars.iborIndex, vars.spread,
                                             null,
                                             vars.iborIndex.dayCounter(),
                                             mktAssetSwap);
         floatingBondMktAssetSwap2.setPricingEngine(swapEngine);
         double floatingBondMktAssetSwapSpread2 =
            floatingBondMktAssetSwap2.fairSpread();
         double error4 = Math.Abs(floatingBondMktAssetSwapSpread2-
                     100*floatingBondParAssetSwapSpread2/floatingBondMktFullPrice2);

         if (error4>tolerance)
            Assert.Fail("wrong asset swap spreads for floating bond:" +
                        "\n  market asset swap spread: " + floatingBondMktAssetSwapSpread2 +
                        "\n  par asset swap spread:    " + floatingBondParAssetSwapSpread2 +
                        "\n  error:                    " + error4 +
                        "\n  tolerance:                " + tolerance);

         // CMS Underlying bond (Isin: XS0228052402 CRDIT 0 8/22/20)
         // maturity doesn't occur on a business day

         Date cmsBondStartDate1 = new Date(22,Month.August,2005);
         Date cmsBondMaturityDate1 = new Date(22,Month.August,2020);
         Schedule cmsBondSchedule1= new Schedule(cmsBondStartDate1,
                                 cmsBondMaturityDate1,
                                 new Period(Frequency.Annual), bondCalendar,
                                 BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                 DateGeneration.Rule.Backward, false);
         List<CashFlow> cmsBondLeg1 = new CmsLeg(cmsBondSchedule1, vars.swapIndex)
            .withPaymentDayCounter(new Thirty360())
            .withFixingDays(fixingDays)
            .withCaps(0.055)
            .withFloors(0.025)
            .inArrears(inArrears)
            .withNotionals(vars.faceAmount);
         Date cmsbondRedemption1 = bondCalendar.adjust(cmsBondMaturityDate1, BusinessDayConvention.Following);
         cmsBondLeg1.Add(new SimpleCashFlow(100.0, cmsbondRedemption1));
         Bond cmsBond1 = new Bond(settlementDays, bondCalendar, vars.faceAmount, cmsBondMaturityDate1, cmsBondStartDate1, 
            cmsBondLeg1);
         cmsBond1.setPricingEngine(bondEngine);

         Utils.setCouponPricer(cmsBond1.cashflows(), vars.cmspricer);
         vars.swapIndex.addFixing(new Date(18,Month.August,2006), 0.04158);
         double cmsBondMktPrice1 = 88.45 ; // market price observed on 7th June 2007
         double cmsBondMktFullPrice1 = cmsBondMktPrice1+cmsBond1.accruedAmount();
         AssetSwap cmsBondParAssetSwap1 = new AssetSwap(payFixedRate,
                                       cmsBond1, cmsBondMktPrice1,
                                       vars.iborIndex, vars.spread,
                                       null,
                                       vars.iborIndex.dayCounter(),
                                       parAssetSwap);
         cmsBondParAssetSwap1.setPricingEngine(swapEngine);
         double cmsBondParAssetSwapSpread1 = cmsBondParAssetSwap1.fairSpread();
         AssetSwap cmsBondMktAssetSwap1 = new AssetSwap(payFixedRate,
                                       cmsBond1, cmsBondMktPrice1,
                                       vars.iborIndex, vars.spread,
                                       null,
                                       vars.iborIndex.dayCounter(),
                                       mktAssetSwap);
         cmsBondMktAssetSwap1.setPricingEngine(swapEngine);
         double cmsBondMktAssetSwapSpread1 = cmsBondMktAssetSwap1.fairSpread();
         double error5 =
            Math.Abs(cmsBondMktAssetSwapSpread1-
                     100*cmsBondParAssetSwapSpread1/cmsBondMktFullPrice1);

         if (error5>tolerance)
            Assert.Fail("wrong asset swap spreads for cms bond:" +
                        "\n  market asset swap spread: " + cmsBondMktAssetSwapSpread1 +
                        "\n  par asset swap spread:    " + cmsBondParAssetSwapSpread1 +
                        "\n  error:                    " + error5 +
                        "\n  tolerance:                " + tolerance);

         // CMS Underlying bond (Isin: XS0218766664 ISPIM 0 5/6/15)
         // maturity occurs on a business day

         Date cmsBondStartDate2 = new Date(06,Month.May,2005);
         Date cmsBondMaturityDate2 = new Date(06,Month.May,2015);
         Schedule cmsBondSchedule2= new Schedule(cmsBondStartDate2,
                                 cmsBondMaturityDate2,
                                 new Period(Frequency.Annual), bondCalendar,
                                 BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                 DateGeneration.Rule.Backward, false);
         List<CashFlow> cmsBondLeg2 = new CmsLeg(cmsBondSchedule2, vars.swapIndex)
            .withPaymentDayCounter(new Thirty360())
            .withFixingDays(fixingDays)
            .withGearings(0.84)
            .inArrears(inArrears)
            .withNotionals(vars.faceAmount);
         Date cmsbondRedemption2 = bondCalendar.adjust(cmsBondMaturityDate2,  BusinessDayConvention.Following);
         cmsBondLeg2.Add(new SimpleCashFlow(100.0, cmsbondRedemption2));
         Bond cmsBond2 = new Bond(settlementDays, bondCalendar, vars.faceAmount,  cmsBondMaturityDate2, cmsBondStartDate2, 
            cmsBondLeg2);
         cmsBond2.setPricingEngine(bondEngine);

         Utils.setCouponPricer(cmsBond2.cashflows(), vars.cmspricer);
         vars.swapIndex.addFixing(new Date(04,Month.May,2006), 0.04217);
         double cmsBondMktPrice2 = 94.08 ; // market price observed on 7th June 2007
         double cmsBondMktFullPrice2 = cmsBondMktPrice2+cmsBond2.accruedAmount();
         AssetSwap cmsBondParAssetSwap2 = new AssetSwap(payFixedRate,
                                       cmsBond2, cmsBondMktPrice2,
                                       vars.iborIndex, vars.spread,
                                       null,
                                       vars.iborIndex.dayCounter(),
                                       parAssetSwap);
         cmsBondParAssetSwap2.setPricingEngine(swapEngine);
         double cmsBondParAssetSwapSpread2 = cmsBondParAssetSwap2.fairSpread();
         AssetSwap cmsBondMktAssetSwap2 = new AssetSwap(payFixedRate,
                                       cmsBond2, cmsBondMktPrice2,
                                       vars.iborIndex, vars.spread,
                                       null,
                                       vars.iborIndex.dayCounter(),
                                       mktAssetSwap);
         cmsBondMktAssetSwap2.setPricingEngine(swapEngine);
         double cmsBondMktAssetSwapSpread2 = cmsBondMktAssetSwap2.fairSpread();
         double error6 =
            Math.Abs(cmsBondMktAssetSwapSpread2-
                     100*cmsBondParAssetSwapSpread2/cmsBondMktFullPrice2);

         if (error6>tolerance)
            Assert.Fail("wrong asset swap spreads for cms bond:" +
                        "\n  market asset swap spread: " + cmsBondMktAssetSwapSpread2 +
                        "\n  par asset swap spread:    " + cmsBondParAssetSwapSpread2 +
                        "\n  error:                    " + error6 +
                        "\n  tolerance:                " + tolerance);

         // Zero Coupon bond (Isin: DE0004771662 IBRD 0 12/20/15)
         // maturity doesn't occur on a business day

         Date zeroCpnBondStartDate1 = new Date(19,Month.December,1985);
         Date zeroCpnBondMaturityDate1 = new Date(20,Month.December,2015);
         Date zeroCpnBondRedemption1 = bondCalendar.adjust(zeroCpnBondMaturityDate1,
                                                         BusinessDayConvention.Following);
         List<CashFlow> zeroCpnBondLeg1 = new List<CashFlow>{new SimpleCashFlow(100.0, zeroCpnBondRedemption1)};
         Bond zeroCpnBond1 = new  Bond(settlementDays, bondCalendar, vars.faceAmount, zeroCpnBondMaturityDate1, 
            zeroCpnBondStartDate1, zeroCpnBondLeg1);
         zeroCpnBond1.setPricingEngine(bondEngine);

         // market price observed on 12th June 2007
         double zeroCpnBondMktPrice1 = 70.436 ;
         double zeroCpnBondMktFullPrice1 =
            zeroCpnBondMktPrice1+zeroCpnBond1.accruedAmount();
         AssetSwap zeroCpnBondParAssetSwap1 = new AssetSwap(payFixedRate,zeroCpnBond1,
                                          zeroCpnBondMktPrice1,
                                          vars.iborIndex, vars.spread,
                                          null,
                                          vars.iborIndex.dayCounter(),
                                          parAssetSwap);
         zeroCpnBondParAssetSwap1.setPricingEngine(swapEngine);
         double zeroCpnBondParAssetSwapSpread1 = zeroCpnBondParAssetSwap1.fairSpread();
         AssetSwap zeroCpnBondMktAssetSwap1 = new AssetSwap(payFixedRate,zeroCpnBond1,
                                          zeroCpnBondMktPrice1,
                                          vars.iborIndex, vars.spread,
                                          null,
                                          vars.iborIndex.dayCounter(),
                                          mktAssetSwap);
         zeroCpnBondMktAssetSwap1.setPricingEngine(swapEngine);
         double zeroCpnBondMktAssetSwapSpread1 = zeroCpnBondMktAssetSwap1.fairSpread();
         double error7 =
            Math.Abs(zeroCpnBondMktAssetSwapSpread1-
                     100*zeroCpnBondParAssetSwapSpread1/zeroCpnBondMktFullPrice1);

         if (error7>tolerance)
            Assert.Fail("wrong asset swap spreads for zero cpn bond:" +
                        "\n  market asset swap spread: " + zeroCpnBondMktAssetSwapSpread1 +
                        "\n  par asset swap spread:    " + zeroCpnBondParAssetSwapSpread1 +
                        "\n  error:                    " + error7 +
                        "\n  tolerance:                " + tolerance);

         // Zero Coupon bond (Isin: IT0001200390 ISPIM 0 02/17/28)
         // maturity occurs on a business day

         Date zeroCpnBondStartDate2 = new Date(17,Month.February,1998);
         Date zeroCpnBondMaturityDate2 = new Date(17,Month.February,2028);
         Date zerocpbondRedemption2 = bondCalendar.adjust(zeroCpnBondMaturityDate2,
                                                         BusinessDayConvention.Following);
         List<CashFlow> zeroCpnBondLeg2 = new List<CashFlow>{new SimpleCashFlow(100.0, zerocpbondRedemption2)};
         Bond zeroCpnBond2 = new Bond(settlementDays, bondCalendar, vars.faceAmount,
                  zeroCpnBondMaturityDate2, zeroCpnBondStartDate2, zeroCpnBondLeg2);
         zeroCpnBond2.setPricingEngine(bondEngine);

         // double zeroCpnBondPrice2 = zeroCpnBond2.cleanPrice();
         // market price observed on 12th June 2007
         double zeroCpnBondMktPrice2 = 35.160 ;
         double zeroCpnBondMktFullPrice2 =
            zeroCpnBondMktPrice2+zeroCpnBond2.accruedAmount();
         AssetSwap zeroCpnBondParAssetSwap2 = new AssetSwap(payFixedRate,zeroCpnBond2,
                                          zeroCpnBondMktPrice2,
                                          vars.iborIndex, vars.spread,
                                          null,
                                          vars.iborIndex.dayCounter(),
                                          parAssetSwap);
         zeroCpnBondParAssetSwap2.setPricingEngine(swapEngine);
         double zeroCpnBondParAssetSwapSpread2 = zeroCpnBondParAssetSwap2.fairSpread();
         AssetSwap zeroCpnBondMktAssetSwap2 = new AssetSwap(payFixedRate,zeroCpnBond2,
                                          zeroCpnBondMktPrice2,
                                          vars.iborIndex, vars.spread,
                                          null,
                                          vars.iborIndex.dayCounter(),
                                          mktAssetSwap);
         zeroCpnBondMktAssetSwap2.setPricingEngine(swapEngine);
         double zeroCpnBondMktAssetSwapSpread2 = zeroCpnBondMktAssetSwap2.fairSpread();
         double error8 =
            Math.Abs(zeroCpnBondMktAssetSwapSpread2-
                     100*zeroCpnBondParAssetSwapSpread2/zeroCpnBondMktFullPrice2);

         if (error8>tolerance)
            Assert.Fail("wrong asset swap spreads for zero cpn bond:" +
                        "\n  market asset swap spread: " + zeroCpnBondMktAssetSwapSpread2 +
                        "\n  par asset swap spread:    " + zeroCpnBondParAssetSwapSpread2 +
                        "\n  error:                    " + error8 +
                        "\n  tolerance:                " + tolerance);
   }

      [TestMethod()]
      public void testZSpreadWithGenericBond() 
      {
         // Testing clean and dirty price with null Z-spread against theoretical prices...

         CommonVars vars = new CommonVars();

         Calendar bondCalendar = new TARGET();
         int settlementDays = 3;
         int fixingDays = 2;
         bool inArrears = false;

         // Fixed Underlying bond (Isin: DE0001135275 DBR 4 01/04/37)
         // maturity doesn't occur on a business day

         Date fixedBondStartDate1 = new Date(4,Month.January,2005);
         Date fixedBondMaturityDate1 = new Date(4,Month.January,2037);
         Schedule fixedBondSchedule1= new Schedule(fixedBondStartDate1,
                                    fixedBondMaturityDate1,
                                    new Period(Frequency.Annual), bondCalendar,
                                    BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                    DateGeneration.Rule.Backward, false);
         List<CashFlow> fixedBondLeg1 = new FixedRateLeg(fixedBondSchedule1)
            .withCouponRates(0.04, new ActualActual(ActualActual.Convention.ISDA))
            .withNotionals(vars.faceAmount);
         Date fixedbondRedemption1 = bondCalendar.adjust(fixedBondMaturityDate1,
                                                         BusinessDayConvention.Following);
         fixedBondLeg1.Add(new SimpleCashFlow(100.0, fixedbondRedemption1));
         Bond fixedBond1 = new Bond(settlementDays, bondCalendar, vars.faceAmount, fixedBondMaturityDate1, fixedBondStartDate1,
                  fixedBondLeg1);
         IPricingEngine bondEngine = new DiscountingBondEngine(vars.termStructure);
         fixedBond1.setPricingEngine(bondEngine);

         double fixedBondImpliedValue1 = fixedBond1.cleanPrice();
         Date fixedBondSettlementDate1= fixedBond1.settlementDate();
         // standard market conventions:
         // bond's frequency + coumpounding and daycounter of the YieldCurve
         double fixedBondCleanPrice1 = BondFunctions.cleanPrice(fixedBond1, vars.termStructure, vars.spread,
            new Actual365Fixed(), vars.compounding, Frequency.Annual, fixedBondSettlementDate1);
         double tolerance = 1.0e-13;
         double error1 = Math.Abs(fixedBondImpliedValue1-fixedBondCleanPrice1);
         if (error1>tolerance) {
            Assert.Fail("wrong clean price for fixed bond:"
                        + "\n  market asset swap spread: "
                        + fixedBondImpliedValue1
                        + "\n  par asset swap spread: " + fixedBondCleanPrice1
                        + "\n  error:                 " + error1
                        + "\n  tolerance:             " + tolerance);
         }

         // Fixed Underlying bond (Isin: IT0006527060 IBRD 5 02/05/19)
         // maturity occurs on a business day

         Date fixedBondStartDate2 = new Date(5,Month.February,2005);
         Date fixedBondMaturityDate2 = new Date(5,Month.February,2019);
         Schedule fixedBondSchedule2= new Schedule(fixedBondStartDate2,
                                    fixedBondMaturityDate2,
                                    new Period(Frequency.Annual), bondCalendar,
                                    BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                    DateGeneration.Rule.Backward, false);
         List<CashFlow> fixedBondLeg2 = new FixedRateLeg(fixedBondSchedule2)
            .withCouponRates(0.05, new Thirty360(Thirty360.Thirty360Convention.BondBasis))
            .withNotionals(vars.faceAmount);
         Date fixedbondRedemption2 = bondCalendar.adjust(fixedBondMaturityDate2, BusinessDayConvention.Following);
         fixedBondLeg2.Add(new SimpleCashFlow(100.0, fixedbondRedemption2));
         Bond fixedBond2 = new Bond(settlementDays, bondCalendar, vars.faceAmount,
                  fixedBondMaturityDate2, fixedBondStartDate2, fixedBondLeg2);
         fixedBond2.setPricingEngine(bondEngine);

         double fixedBondImpliedValue2 = fixedBond2.cleanPrice();
         Date fixedBondSettlementDate2= fixedBond2.settlementDate();
         // standard market conventions:
         // bond's frequency + coumpounding and daycounter of the YieldCurve

         double fixedBondCleanPrice2 = BondFunctions.cleanPrice(fixedBond2, vars.termStructure, vars.spread,
            new Actual365Fixed(), vars.compounding, Frequency.Annual, fixedBondSettlementDate2);
         double error3 = Math.Abs(fixedBondImpliedValue2-fixedBondCleanPrice2);
         if (error3>tolerance) {
            Assert.Fail("wrong clean price for fixed bond:"
                        + "\n  market asset swap spread: "
                        + fixedBondImpliedValue2
                        + "\n  par asset swap spread: " + fixedBondCleanPrice2
                        + "\n  error:                 " + error3
                        + "\n  tolerance:             " + tolerance);
         }

         // FRN Underlying bond (Isin: IT0003543847 ISPIM 0 09/29/13)
         // maturity doesn't occur on a business day

         Date floatingBondStartDate1 = new Date(29,Month.September,2003);
         Date floatingBondMaturityDate1 = new Date(29,Month.September,2013);
         Schedule floatingBondSchedule1= new Schedule(floatingBondStartDate1,
                                       floatingBondMaturityDate1,
                                       new Period(Frequency.Semiannual), bondCalendar,
                                       BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                       DateGeneration.Rule.Backward, false);
         List<CashFlow> floatingBondLeg1 = new IborLeg(floatingBondSchedule1, vars.iborIndex)
            .withPaymentDayCounter(new Actual360())
            .withFixingDays(fixingDays)
            .withSpreads(0.0056)
            .inArrears(inArrears)
            .withNotionals(vars.faceAmount);
         Date floatingbondRedemption1 =
            bondCalendar.adjust(floatingBondMaturityDate1, BusinessDayConvention.Following);
         floatingBondLeg1.Add(new SimpleCashFlow(100.0, floatingbondRedemption1));
         Bond floatingBond1 = new Bond(settlementDays, bondCalendar, vars.faceAmount,
                  floatingBondMaturityDate1, floatingBondStartDate1,
                  floatingBondLeg1);
         floatingBond1.setPricingEngine(bondEngine);

         Utils.setCouponPricer(floatingBond1.cashflows(), vars.pricer);
         vars.iborIndex.addFixing(new Date(27,Month.March,2007), 0.0402);
         double floatingBondImpliedValue1 = floatingBond1.cleanPrice();
         // standard market conventions:
         // bond's frequency + coumpounding and daycounter of the YieldCurve
         double floatingBondCleanPrice1 = BondFunctions.cleanPrice(floatingBond1, vars.termStructure, vars.spread, 
            new Actual365Fixed(), vars.compounding, Frequency.Semiannual, fixedBondSettlementDate1);
         double error5 = Math.Abs(floatingBondImpliedValue1-floatingBondCleanPrice1);
         if (error5>tolerance) {
            Assert.Fail("wrong clean price for fixed bond:"
                        + "\n  market asset swap spread: " +
                        floatingBondImpliedValue1
                        + "\n  par asset swap spread: " + floatingBondCleanPrice1
                        + "\n  error:                 " + error5
                        + "\n  tolerance:             " + tolerance);
         }

         // FRN Underlying bond (Isin: XS0090566539 COE 0 09/24/18)
         // maturity occurs on a business day

         Date floatingBondStartDate2 = new Date(24,Month.September,2004);
         Date floatingBondMaturityDate2 = new Date(24,Month.September,2018);
         Schedule floatingBondSchedule2 = new Schedule(floatingBondStartDate2,
                                       floatingBondMaturityDate2,
                                       new Period(Frequency.Semiannual), bondCalendar,
                                       BusinessDayConvention.ModifiedFollowing, BusinessDayConvention.ModifiedFollowing,
                                       DateGeneration.Rule.Backward, false);
         List<CashFlow> floatingBondLeg2 = new IborLeg(floatingBondSchedule2, vars.iborIndex)
            .withFixingDays(fixingDays)
            .withSpreads(0.0025)
            .withPaymentDayCounter(new Actual360())
            .inArrears(inArrears)
            .withPaymentAdjustment(BusinessDayConvention.ModifiedFollowing)
            .withNotionals(vars.faceAmount);
         Date floatingbondRedemption2 = bondCalendar.adjust(floatingBondMaturityDate2, BusinessDayConvention.ModifiedFollowing);
         floatingBondLeg2.Add(new SimpleCashFlow(100.0, floatingbondRedemption2));
         Bond floatingBond2 = new Bond(settlementDays, bondCalendar, vars.faceAmount, floatingBondMaturityDate2, 
            floatingBondStartDate2, floatingBondLeg2);
         floatingBond2.setPricingEngine(bondEngine);

         Utils.setCouponPricer(floatingBond2.cashflows(), vars.pricer);
         vars.iborIndex.addFixing(new Date(22,Month.March,2007), 0.04013);
         double floatingBondImpliedValue2 = floatingBond2.cleanPrice();
         // standard market conventions:
         // bond's frequency + coumpounding and daycounter of the YieldCurve
         double floatingBondCleanPrice2 = BondFunctions.cleanPrice(floatingBond2, vars.termStructure, vars.spread, 
            new Actual365Fixed(), vars.compounding, Frequency.Semiannual,  fixedBondSettlementDate1);
         double error7 = Math.Abs(floatingBondImpliedValue2-floatingBondCleanPrice2);
         if (error7>tolerance) {
            Assert.Fail("wrong clean price for fixed bond:"
                        + "\n  market asset swap spread: " +
                        floatingBondImpliedValue2
                        + "\n  par asset swap spread: " + floatingBondCleanPrice2
                        + "\n  error:                 " + error7
                        + "\n  tolerance:             " + tolerance);
         }

         // CMS Underlying bond (Isin: XS0228052402 CRDIT 0 8/22/20)
         // maturity doesn't occur on a business day

         Date cmsBondStartDate1 = new Date(22,Month.August,2005);
         Date cmsBondMaturityDate1 = new Date(22,Month.August,2020);
         Schedule cmsBondSchedule1= new Schedule(cmsBondStartDate1,
                                 cmsBondMaturityDate1,
                                 new Period(Frequency.Annual), bondCalendar,
                                 BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                 DateGeneration.Rule.Backward, false);
         List<CashFlow> cmsBondLeg1 = new CmsLeg(cmsBondSchedule1, vars.swapIndex)
            .withPaymentDayCounter(new Thirty360())
            .withFixingDays(fixingDays)
            .withCaps(0.055)
            .withFloors(0.025)
            .inArrears(inArrears)
            .withNotionals(vars.faceAmount);
         Date cmsbondRedemption1 = bondCalendar.adjust(cmsBondMaturityDate1, BusinessDayConvention.Following);
         cmsBondLeg1.Add(new SimpleCashFlow(100.0, cmsbondRedemption1));
         Bond cmsBond1 = new Bond(settlementDays, bondCalendar, vars.faceAmount, cmsBondMaturityDate1, cmsBondStartDate1, 
            cmsBondLeg1);
         cmsBond1.setPricingEngine(bondEngine);

         Utils.setCouponPricer(cmsBond1.cashflows(), vars.cmspricer);
         vars.swapIndex.addFixing(new Date(18,Month.August,2006), 0.04158);
         double cmsBondImpliedValue1 = cmsBond1.cleanPrice();
         Date cmsBondSettlementDate1= cmsBond1.settlementDate();
         // standard market conventions:
         // bond's frequency + coumpounding and daycounter of the YieldCurve
         double cmsBondCleanPrice1 = BondFunctions.cleanPrice(cmsBond1, vars.termStructure, vars.spread,
            new Actual365Fixed(), vars.compounding, Frequency.Annual,
            cmsBondSettlementDate1);
         double error9 = Math.Abs(cmsBondImpliedValue1-cmsBondCleanPrice1);
         if (error9>tolerance) {
            Assert.Fail("wrong clean price for fixed bond:"
                        + "\n  market asset swap spread: " + cmsBondImpliedValue1
                        + "\n  par asset swap spread: " + cmsBondCleanPrice1
                        + "\n  error:                 " + error9
                        + "\n  tolerance:             " + tolerance);
         }

         // CMS Underlying bond (Isin: XS0218766664 ISPIM 0 5/6/15)
         // maturity occurs on a business day

         Date cmsBondStartDate2 = new Date(06,Month.May,2005);
         Date cmsBondMaturityDate2 = new Date(06,Month.May,2015);
         Schedule cmsBondSchedule2= new Schedule(cmsBondStartDate2,
                                 cmsBondMaturityDate2,
                                 new Period(Frequency.Annual), bondCalendar,
                                 BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                 DateGeneration.Rule.Backward, false);
         List<CashFlow> cmsBondLeg2 = new CmsLeg(cmsBondSchedule2, vars.swapIndex)
            .withPaymentDayCounter(new Thirty360())
            .withFixingDays(fixingDays)
            .withGearings(0.84)
            .inArrears(inArrears)
            .withNotionals(vars.faceAmount);
         Date cmsbondRedemption2 = bondCalendar.adjust(cmsBondMaturityDate2,  BusinessDayConvention.Following);
         cmsBondLeg2.Add(new SimpleCashFlow(100.0, cmsbondRedemption2));
         Bond cmsBond2 = new Bond(settlementDays, bondCalendar, vars.faceAmount,
                  cmsBondMaturityDate2, cmsBondStartDate2, cmsBondLeg2);
         cmsBond2.setPricingEngine(bondEngine);

         Utils.setCouponPricer(cmsBond2.cashflows(), vars.cmspricer);
         vars.swapIndex.addFixing(new Date(04,Month.May,2006), 0.04217);
         double cmsBondImpliedValue2 = cmsBond2.cleanPrice();
         Date cmsBondSettlementDate2= cmsBond2.settlementDate();
         // standard market conventions:
         // bond's frequency + coumpounding and daycounter of the YieldCurve
         double cmsBondCleanPrice2 = BondFunctions.cleanPrice(cmsBond2, vars.termStructure, vars.spread,
            new Actual365Fixed(), vars.compounding, Frequency.Annual,
            cmsBondSettlementDate2);
         double error11 = Math.Abs(cmsBondImpliedValue2-cmsBondCleanPrice2);
         if (error11>tolerance) {
            Assert.Fail("wrong clean price for fixed bond:"
                        + "\n  market asset swap spread: " + cmsBondImpliedValue2
                        + "\n  par asset swap spread: " + cmsBondCleanPrice2
                        + "\n  error:                 " + error11
                        + "\n  tolerance:             " + tolerance);
         }

         // Zero Coupon bond (Isin: DE0004771662 IBRD 0 12/20/15)
         // maturity doesn't occur on a business day

         Date zeroCpnBondStartDate1 = new Date(19,Month.December,1985);
         Date zeroCpnBondMaturityDate1 = new Date(20,Month.December,2015);
         Date zeroCpnBondRedemption1 = bondCalendar.adjust(zeroCpnBondMaturityDate1,
                                                         BusinessDayConvention.Following);
         List<CashFlow> zeroCpnBondLeg1 = new List<CashFlow>{new SimpleCashFlow(100.0, zeroCpnBondRedemption1)};
         Bond zeroCpnBond1 = new Bond(settlementDays, bondCalendar, vars.faceAmount,
                  zeroCpnBondMaturityDate1, zeroCpnBondStartDate1, zeroCpnBondLeg1);
         zeroCpnBond1.setPricingEngine(bondEngine);

         double zeroCpnBondImpliedValue1 = zeroCpnBond1.cleanPrice();
         Date zeroCpnBondSettlementDate1= zeroCpnBond1.settlementDate();
         // standard market conventions:
         // bond's frequency + coumpounding and daycounter of the YieldCurve
         double zeroCpnBondCleanPrice1 =
            BondFunctions.cleanPrice(zeroCpnBond1,
                                 vars.termStructure,
                                 vars.spread,
                                 new Actual365Fixed(),
                                 vars.compounding, Frequency.Annual,
                                 zeroCpnBondSettlementDate1);
         double error13 = Math.Abs(zeroCpnBondImpliedValue1-zeroCpnBondCleanPrice1);
         if (error13>tolerance) {
            Assert.Fail("wrong clean price for zero coupon bond:"
                        + "\n  zero cpn implied value: " +
                        zeroCpnBondImpliedValue1
                        + "\n  zero cpn price: " + zeroCpnBondCleanPrice1
                        + "\n  error:                 " + error13
                        + "\n  tolerance:             " + tolerance);
         }

         // Zero Coupon bond (Isin: IT0001200390 ISPIM 0 02/17/28)
         // maturity occurs on a business day

         Date zeroCpnBondStartDate2 = new Date(17,Month.February,1998);
         Date zeroCpnBondMaturityDate2 = new Date(17,Month.February,2028);
         Date zerocpbondRedemption2 = bondCalendar.adjust(zeroCpnBondMaturityDate2,
                                                         BusinessDayConvention.Following);
         List<CashFlow> zeroCpnBondLeg2 = new List<CashFlow>{new SimpleCashFlow(100.0, zerocpbondRedemption2)};
         Bond zeroCpnBond2 = new Bond(settlementDays, bondCalendar, vars.faceAmount,
                  zeroCpnBondMaturityDate2, zeroCpnBondStartDate2, zeroCpnBondLeg2);
         zeroCpnBond2.setPricingEngine(bondEngine);

         double zeroCpnBondImpliedValue2 = zeroCpnBond2.cleanPrice();
         Date zeroCpnBondSettlementDate2= zeroCpnBond2.settlementDate();
         // standard market conventions:
         // bond's frequency + coumpounding and daycounter of the YieldCurve
         double zeroCpnBondCleanPrice2 =
            BondFunctions.cleanPrice(zeroCpnBond2,
                                 vars.termStructure,
                                 vars.spread,
                                 new Actual365Fixed(),
                                 vars.compounding, Frequency.Annual,
                                 zeroCpnBondSettlementDate2);
         double error15 = Math.Abs(zeroCpnBondImpliedValue2-zeroCpnBondCleanPrice2);
         if (error15>tolerance) {
            Assert.Fail("wrong clean price for zero coupon bond:"
                        + "\n  zero cpn implied value: " +
                        zeroCpnBondImpliedValue2
                        + "\n  zero cpn price: " + zeroCpnBondCleanPrice2
                        + "\n  error:                 " + error15
                        + "\n  tolerance:             " + tolerance);
         }
   }

      [TestMethod()]
      public void testSpecializedBondVsGenericBond() 
      {
         // Testing clean and dirty prices for specialized bond against equivalent generic bond...
         CommonVars vars = new CommonVars();

         Calendar bondCalendar = new TARGET();
         int settlementDays = 3;
         int fixingDays = 2;
         bool inArrears = false;

         // Fixed Underlying bond (Isin: DE0001135275 DBR 4 01/04/37)
         // maturity doesn't occur on a business day
         Date fixedBondStartDate1 = new Date(4,Month.January,2005);
         Date fixedBondMaturityDate1 = new Date(4,Month.January,2037);
         Schedule fixedBondSchedule1= new Schedule(fixedBondStartDate1,
                                    fixedBondMaturityDate1,
                                    new Period(Frequency.Annual), bondCalendar,
                                    BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                    DateGeneration.Rule.Backward, false);
         List<CashFlow> fixedBondLeg1 = new FixedRateLeg(fixedBondSchedule1)
            .withCouponRates(0.04, new ActualActual(ActualActual.Convention.ISDA))
            .withNotionals(vars.faceAmount);
         Date fixedbondRedemption1 = bondCalendar.adjust(fixedBondMaturityDate1,
                                                         BusinessDayConvention.Following);
         fixedBondLeg1.Add(new SimpleCashFlow(100.0, fixedbondRedemption1));
         // generic bond
         Bond fixedBond1 = new  Bond(settlementDays, bondCalendar, vars.faceAmount,
                  fixedBondMaturityDate1, fixedBondStartDate1, fixedBondLeg1);
         IPricingEngine bondEngine = new DiscountingBondEngine(vars.termStructure);
         fixedBond1.setPricingEngine(bondEngine);

         // equivalent specialized fixed rate bond
         Bond fixedSpecializedBond1 = new FixedRateBond(settlementDays, vars.faceAmount, fixedBondSchedule1,
                           new List<double>{0.04},
                           new ActualActual(ActualActual.Convention.ISDA), BusinessDayConvention.Following,
                           100.0, new Date(4,Month.January,2005) );
         fixedSpecializedBond1.setPricingEngine(bondEngine);

         double fixedBondTheoValue1 = fixedBond1.cleanPrice();
         double fixedSpecializedBondTheoValue1 = fixedSpecializedBond1.cleanPrice();
         double tolerance = 1.0e-13;
         double error1 = Math.Abs(fixedBondTheoValue1-fixedSpecializedBondTheoValue1);
         if (error1>tolerance) {
            Assert.Fail("wrong clean price for fixed bond:"
                        + "\n  specialized fixed rate bond's theo clean price: "
                        + fixedBondTheoValue1
                        + "\n  generic equivalent bond's theo clean price: "
                        + fixedSpecializedBondTheoValue1
                        + "\n  error:                 " + error1
                        + "\n  tolerance:             " + tolerance);
         }
         double fixedBondTheoDirty1 = fixedBondTheoValue1+fixedBond1.accruedAmount();
         double fixedSpecializedTheoDirty1 = fixedSpecializedBondTheoValue1+
                                       fixedSpecializedBond1.accruedAmount();
         double error2 = Math.Abs(fixedBondTheoDirty1-fixedSpecializedTheoDirty1);
         if (error2>tolerance) {
            Assert.Fail("wrong dirty price for fixed bond:"
                        + "\n  specialized fixed rate bond's theo dirty price: "
                        + fixedBondTheoDirty1
                        + "\n  generic equivalent bond's theo dirty price: "
                        + fixedSpecializedTheoDirty1
                        + "\n  error:                 " + error2
                        + "\n  tolerance:             " + tolerance);
         }

         // Fixed Underlying bond (Isin: IT0006527060 IBRD 5 02/05/19)
         // maturity occurs on a business day
         Date fixedBondStartDate2 = new Date(5,Month.February,2005);
         Date fixedBondMaturityDate2 = new Date(5,Month.February,2019);
         Schedule fixedBondSchedule2= new Schedule(fixedBondStartDate2,
                                    fixedBondMaturityDate2,
                                    new Period(Frequency.Annual), bondCalendar,
                                    BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                    DateGeneration.Rule.Backward, false);
         List<CashFlow> fixedBondLeg2 = new FixedRateLeg(fixedBondSchedule2)
            .withCouponRates(0.05, new Thirty360(Thirty360.Thirty360Convention.BondBasis))
            .withNotionals(vars.faceAmount);
         Date fixedbondRedemption2 = bondCalendar.adjust(fixedBondMaturityDate2,  BusinessDayConvention.Following);
         fixedBondLeg2.Add(new SimpleCashFlow(100.0, fixedbondRedemption2));

         // generic bond
         Bond fixedBond2 = new Bond(settlementDays, bondCalendar, vars.faceAmount,
                  fixedBondMaturityDate2, fixedBondStartDate2, fixedBondLeg2);
         fixedBond2.setPricingEngine(bondEngine);

         // equivalent specialized fixed rate bond
         Bond fixedSpecializedBond2 = new  FixedRateBond(settlementDays, vars.faceAmount, fixedBondSchedule2,
                           new List<double>{0.05},
                           new Thirty360(Thirty360.Thirty360Convention.BondBasis), BusinessDayConvention.Following,
                           100.0, new Date(5,Month.February,2005));
         fixedSpecializedBond2.setPricingEngine(bondEngine);

         double fixedBondTheoValue2 = fixedBond2.cleanPrice();
         double fixedSpecializedBondTheoValue2 = fixedSpecializedBond2.cleanPrice();

         double error3 = Math.Abs(fixedBondTheoValue2-fixedSpecializedBondTheoValue2);
         if (error3>tolerance) {
            Assert.Fail("wrong clean price for fixed bond:"
                        + "\n  specialized fixed rate bond's theo clean price: "
                        + fixedBondTheoValue2
                        + "\n  generic equivalent bond's theo clean price: "
                        + fixedSpecializedBondTheoValue2
                        + "\n  error:                 " + error3
                        + "\n  tolerance:             " + tolerance);
         }
         double fixedBondTheoDirty2 = fixedBondTheoValue2+
                                    fixedBond2.accruedAmount();
         double fixedSpecializedBondTheoDirty2 = fixedSpecializedBondTheoValue2+
                                          fixedSpecializedBond2.accruedAmount();

         double error4 = Math.Abs(fixedBondTheoDirty2-fixedSpecializedBondTheoDirty2);
         if (error4>tolerance) {
            Assert.Fail("wrong dirty price for fixed bond:"
                        + "\n  specialized fixed rate bond's dirty clean price: "
                        + fixedBondTheoDirty2
                        + "\n  generic equivalent bond's theo dirty price: "
                        + fixedSpecializedBondTheoDirty2
                        + "\n  error:                 " + error4
                        + "\n  tolerance:             " + tolerance);
         }

         // FRN Underlying bond (Isin: IT0003543847 ISPIM 0 09/29/13)
         // maturity doesn't occur on a business day
         Date floatingBondStartDate1 = new Date(29,Month.September,2003);
         Date floatingBondMaturityDate1 = new Date(29,Month.September,2013);
         Schedule floatingBondSchedule1= new Schedule(floatingBondStartDate1,
                                       floatingBondMaturityDate1,
                                       new Period(Frequency.Semiannual), bondCalendar,
                                       BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                       DateGeneration.Rule.Backward, false);
         List<CashFlow> floatingBondLeg1 = new IborLeg(floatingBondSchedule1, vars.iborIndex)
            .withPaymentDayCounter(new Actual360())
            .withFixingDays(fixingDays)
            .withSpreads(0.0056)
            .inArrears(inArrears)
            .withNotionals(vars.faceAmount);
         Date floatingbondRedemption1 = bondCalendar.adjust(floatingBondMaturityDate1, BusinessDayConvention.Following);
         floatingBondLeg1.Add(new SimpleCashFlow(100.0, floatingbondRedemption1));
         // generic bond
         Bond floatingBond1 = new Bond(settlementDays, bondCalendar, vars.faceAmount,
                  floatingBondMaturityDate1, floatingBondStartDate1, floatingBondLeg1);
         floatingBond1.setPricingEngine(bondEngine);

         // equivalent specialized floater
         Bond floatingSpecializedBond1= new FloatingRateBond(settlementDays, vars.faceAmount,
                                 floatingBondSchedule1,
                                 vars.iborIndex, new Actual360(),
                                 BusinessDayConvention.Following, fixingDays,
                                 new List<double>{1},
                                 new List<double>{0.0056},
                                 new List<double>(), new List<double>(),
                                 inArrears,
                                 100.0, new Date(29,Month.September,2003));
         floatingSpecializedBond1.setPricingEngine(bondEngine);

         Utils.setCouponPricer(floatingBond1.cashflows(), vars.pricer);
         Utils.setCouponPricer(floatingSpecializedBond1.cashflows(), vars.pricer);
         vars.iborIndex.addFixing(new Date(27,Month.March,2007), 0.0402);
         double floatingBondTheoValue1 = floatingBond1.cleanPrice();
         double floatingSpecializedBondTheoValue1 =
            floatingSpecializedBond1.cleanPrice();

         double error5 = Math.Abs(floatingBondTheoValue1-
                                 floatingSpecializedBondTheoValue1);
         if (error5>tolerance) {
            Assert.Fail("wrong clean price for fixed bond:"
                        + "\n  generic fixed rate bond's theo clean price: "
                        + floatingBondTheoValue1
                        + "\n  equivalent specialized bond's theo clean price: "
                        + floatingSpecializedBondTheoValue1
                        + "\n  error:                 " + error5
                        + "\n  tolerance:             " + tolerance);
         }
         double floatingBondTheoDirty1 = floatingBondTheoValue1+
                                       floatingBond1.accruedAmount();
         double floatingSpecializedBondTheoDirty1 =
            floatingSpecializedBondTheoValue1+
            floatingSpecializedBond1.accruedAmount();
         double error6 = Math.Abs(floatingBondTheoDirty1-
                                 floatingSpecializedBondTheoDirty1);
         if (error6>tolerance) {
            Assert.Fail("wrong dirty price for frn bond:"
                        + "\n  generic frn bond's dirty clean price: "
                        + floatingBondTheoDirty1
                        + "\n  equivalent specialized bond's theo dirty price: "
                        + floatingSpecializedBondTheoDirty1
                        + "\n  error:                 " + error6
                        + "\n  tolerance:             " + tolerance);
         }

         // FRN Underlying bond (Isin: XS0090566539 COE 0 09/24/18)
         // maturity occurs on a business day
         Date floatingBondStartDate2 = new Date(24,Month.September,2004);
         Date floatingBondMaturityDate2 = new Date(24,Month.September,2018);
         Schedule floatingBondSchedule2 = new Schedule(floatingBondStartDate2,
                                       floatingBondMaturityDate2,
                                       new Period(Frequency.Semiannual), bondCalendar,
                                       BusinessDayConvention.ModifiedFollowing, BusinessDayConvention.ModifiedFollowing,
                                       DateGeneration.Rule.Backward, false);
         List<CashFlow> floatingBondLeg2 = new IborLeg(floatingBondSchedule2, vars.iborIndex)
            .withPaymentDayCounter(new Actual360())
            .withFixingDays(fixingDays)
            .withSpreads(0.0025)
            .inArrears(inArrears)
            .withPaymentAdjustment(BusinessDayConvention.ModifiedFollowing)
            .withNotionals(vars.faceAmount);
         Date floatingbondRedemption2 =
            bondCalendar.adjust(floatingBondMaturityDate2, BusinessDayConvention.ModifiedFollowing);
         floatingBondLeg2.Add(new  SimpleCashFlow(100.0, floatingbondRedemption2));
         // generic bond
         Bond floatingBond2 = new Bond(settlementDays, bondCalendar, vars.faceAmount,
                  floatingBondMaturityDate2, floatingBondStartDate2, floatingBondLeg2);
         floatingBond2.setPricingEngine(bondEngine);

         // equivalent specialized floater
         Bond floatingSpecializedBond2 = new FloatingRateBond(settlementDays, vars.faceAmount,
                              floatingBondSchedule2,
                              vars.iborIndex, new Actual360(),
                              BusinessDayConvention.ModifiedFollowing, fixingDays,
                              new List<double>{1},
                              new List<double>{0.0025},
                              new List<double>(), new List<double>(),
                              inArrears,
                              100.0, new Date(24,Month.September,2004));
         floatingSpecializedBond2.setPricingEngine(bondEngine);

         Utils.setCouponPricer(floatingBond2.cashflows(), vars.pricer);
         Utils.setCouponPricer(floatingSpecializedBond2.cashflows(), vars.pricer);

         vars.iborIndex.addFixing(new Date(22,Month.March,2007), 0.04013);

         double floatingBondTheoValue2 = floatingBond2.cleanPrice();
         double floatingSpecializedBondTheoValue2 =
            floatingSpecializedBond2.cleanPrice();

         double error7 =
            Math.Abs(floatingBondTheoValue2-floatingSpecializedBondTheoValue2);
         if (error7>tolerance) {
            Assert.Fail("wrong clean price for floater bond:"
                        + "\n  generic floater bond's theo clean price: "
                        + floatingBondTheoValue2
                        + "\n  equivalent specialized bond's theo clean price: "
                        + floatingSpecializedBondTheoValue2
                        + "\n  error:                 " + error7
                        + "\n  tolerance:             " + tolerance);
         }
         double floatingBondTheoDirty2 = floatingBondTheoValue2+
                                       floatingBond2.accruedAmount();
         double floatingSpecializedTheoDirty2 = floatingSpecializedBondTheoValue2+
                                          floatingSpecializedBond2.accruedAmount();

         double error8 =
            Math.Abs(floatingBondTheoDirty2-floatingSpecializedTheoDirty2);
         if (error8>tolerance) {
            Assert.Fail("wrong dirty price for floater bond:"
                        + "\n  generic floater bond's theo dirty price: "
                        + floatingBondTheoDirty2
                        + "\n  equivalent specialized  bond's theo dirty price: "
                        + floatingSpecializedTheoDirty2
                        + "\n  error:                 " + error8
                        + "\n  tolerance:             " + tolerance);
         }


         // CMS Underlying bond (Isin: XS0228052402 CRDIT 0 8/22/20)
         // maturity doesn't occur on a business day
         Date cmsBondStartDate1 = new Date(22,Month.August,2005);
         Date cmsBondMaturityDate1 = new Date(22,Month.August,2020);
         Schedule cmsBondSchedule1= new Schedule(cmsBondStartDate1,
                                 cmsBondMaturityDate1,
                                 new Period(Frequency.Annual), bondCalendar,
                                 BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                 DateGeneration.Rule.Backward, false);
         List<CashFlow> cmsBondLeg1 = new CmsLeg(cmsBondSchedule1, vars.swapIndex)
            .withPaymentDayCounter(new Thirty360())
            .withFixingDays(fixingDays)
            .withCaps(0.055)
            .withFloors(0.025)
            .inArrears(inArrears)
            .withNotionals(vars.faceAmount);
         Date cmsbondRedemption1 = bondCalendar.adjust(cmsBondMaturityDate1,  BusinessDayConvention.Following);
         cmsBondLeg1.Add(new SimpleCashFlow(100.0, cmsbondRedemption1));
         // generic cms bond
         Bond cmsBond1 = new  Bond(settlementDays, bondCalendar, vars.faceAmount,
                  cmsBondMaturityDate1, cmsBondStartDate1, cmsBondLeg1);
         cmsBond1.setPricingEngine(bondEngine);

         // equivalent specialized cms bond
         Bond cmsSpecializedBond1  = new  CmsRateBond(settlementDays, vars.faceAmount, cmsBondSchedule1,
                     vars.swapIndex, new Thirty360(),
                     BusinessDayConvention.Following, fixingDays,
                     new List<double>{1.0}, new List<double>{0.0},
                     new List<double>{0.055}, new List<double>{0.025},
                     inArrears,
                     100.0, new Date(22,Month.August,2005));
         cmsSpecializedBond1.setPricingEngine(bondEngine);

         Utils.setCouponPricer(cmsBond1.cashflows(), vars.cmspricer);
         Utils.setCouponPricer(cmsSpecializedBond1.cashflows(), vars.cmspricer);
         vars.swapIndex.addFixing(new Date(18,Month.August,2006), 0.04158);
         double cmsBondTheoValue1 = cmsBond1.cleanPrice();
         double cmsSpecializedBondTheoValue1 = cmsSpecializedBond1.cleanPrice();
         double error9 = Math.Abs(cmsBondTheoValue1-cmsSpecializedBondTheoValue1);
         if (error9>tolerance) {
            Assert.Fail("wrong clean price for cms bond:"
                        + "\n  generic cms bond's theo clean price: "
                        + cmsBondTheoValue1
                        +  "\n  equivalent specialized bond's theo clean price: "
                        + cmsSpecializedBondTheoValue1
                        + "\n  error:                 " + error9
                        + "\n  tolerance:             " + tolerance);
         }
         double cmsBondTheoDirty1 = cmsBondTheoValue1+cmsBond1.accruedAmount();
         double cmsSpecializedBondTheoDirty1 = cmsSpecializedBondTheoValue1+
                                       cmsSpecializedBond1.accruedAmount();
         double error10 = Math.Abs(cmsBondTheoDirty1-cmsSpecializedBondTheoDirty1);
         if (error10>tolerance) {
            Assert.Fail("wrong dirty price for cms bond:"
                        + "\n generic cms bond's theo dirty price: "
                        + cmsBondTheoDirty1
                        + "\n  specialized cms bond's theo dirty price: "
                        + cmsSpecializedBondTheoDirty1
                        + "\n  error:                 " + error10
                        + "\n  tolerance:             " + tolerance);
         }

         // CMS Underlying bond (Isin: XS0218766664 ISPIM 0 5/6/15)
         // maturity occurs on a business day
         Date cmsBondStartDate2 = new Date(06,Month.May,2005);
         Date cmsBondMaturityDate2 = new Date(06,Month.May,2015);
         Schedule cmsBondSchedule2 = new Schedule(cmsBondStartDate2,
                                 cmsBondMaturityDate2,
                                 new Period(Frequency.Annual), bondCalendar,
                                 BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                 DateGeneration.Rule.Backward, false);
         List<CashFlow> cmsBondLeg2 = new CmsLeg(cmsBondSchedule2, vars.swapIndex)
            .withPaymentDayCounter(new Thirty360())
            .withFixingDays(fixingDays)
            .withGearings(0.84)
            .inArrears(inArrears)
            .withNotionals(vars.faceAmount);
         Date cmsbondRedemption2 = bondCalendar.adjust(cmsBondMaturityDate2, BusinessDayConvention.Following);
         cmsBondLeg2.Add(new SimpleCashFlow(100.0, cmsbondRedemption2));
         // generic bond
         Bond cmsBond2 = new  Bond(settlementDays, bondCalendar, vars.faceAmount,
                  cmsBondMaturityDate2, cmsBondStartDate2, cmsBondLeg2);
         cmsBond2.setPricingEngine(bondEngine);

         // equivalent specialized cms bond
         Bond cmsSpecializedBond2 = new CmsRateBond(settlementDays, vars.faceAmount, cmsBondSchedule2,
                     vars.swapIndex, new Thirty360(),
                     BusinessDayConvention.Following, fixingDays,
                     new List<double>{0.84}, new List<double>{0.0},
                     new List<double>(), new List<double>(),
                     inArrears, 100.0, new Date(06,Month.May,2005));
         cmsSpecializedBond2.setPricingEngine(bondEngine);

         Utils.setCouponPricer(cmsBond2.cashflows(), vars.cmspricer);
         Utils.setCouponPricer(cmsSpecializedBond2.cashflows(), vars.cmspricer);
         vars.swapIndex.addFixing(new Date(04,Month.May,2006), 0.04217);
         double cmsBondTheoValue2 = cmsBond2.cleanPrice();
         double cmsSpecializedBondTheoValue2 = cmsSpecializedBond2.cleanPrice();

         double error11 = Math.Abs(cmsBondTheoValue2-cmsSpecializedBondTheoValue2);
         if (error11>tolerance) {
            Assert.Fail("wrong clean price for cms bond:"
                        + "\n  generic cms bond's theo clean price: "
                        + cmsBondTheoValue2
                        + "\n  cms bond's theo clean price: "
                        + cmsSpecializedBondTheoValue2
                        + "\n  error:                 " + error11
                        + "\n  tolerance:             " + tolerance);
         }
         double cmsBondTheoDirty2 = cmsBondTheoValue2+cmsBond2.accruedAmount();
         double cmsSpecializedBondTheoDirty2 =
            cmsSpecializedBondTheoValue2+cmsSpecializedBond2.accruedAmount();
         double error12 = Math.Abs(cmsBondTheoDirty2-cmsSpecializedBondTheoDirty2);
         if (error12>tolerance) {
            Assert.Fail("wrong dirty price for cms bond:"
                        + "\n  generic cms bond's dirty price: "
                        + cmsBondTheoDirty2
                        + "\n  specialized cms bond's theo dirty price: "
                        + cmsSpecializedBondTheoDirty2
                        + "\n  error:                 " + error12
                        + "\n  tolerance:             " + tolerance);
         }

         // Zero Coupon bond (Isin: DE0004771662 IBRD 0 12/20/15)
         // maturity doesn't occur on a business day
         Date zeroCpnBondStartDate1 = new Date(19,Month.December,1985);
         Date zeroCpnBondMaturityDate1 = new Date(20,Month.December,2015);
         Date zeroCpnBondRedemption1 = bondCalendar.adjust(zeroCpnBondMaturityDate1,
                                                         BusinessDayConvention.Following);
         List<CashFlow> zeroCpnBondLeg1 = new List<CashFlow>{new SimpleCashFlow(100.0, zeroCpnBondRedemption1)};
         // generic bond
         Bond zeroCpnBond1 = new Bond(settlementDays, bondCalendar, vars.faceAmount, zeroCpnBondMaturityDate1, 
            zeroCpnBondStartDate1, zeroCpnBondLeg1);
         zeroCpnBond1.setPricingEngine(bondEngine);

         // specialized zerocpn bond
         Bond zeroCpnSpecializedBond1 = new ZeroCouponBond(settlementDays, bondCalendar, vars.faceAmount,
                     new Date(20,Month.December,2015),
                     BusinessDayConvention.Following,
                     100.0, new Date(19,Month.December,1985));
         zeroCpnSpecializedBond1.setPricingEngine(bondEngine);

         double zeroCpnBondTheoValue1 = zeroCpnBond1.cleanPrice();
         double zeroCpnSpecializedBondTheoValue1 =
            zeroCpnSpecializedBond1.cleanPrice();

         double error13 =
            Math.Abs(zeroCpnBondTheoValue1-zeroCpnSpecializedBondTheoValue1);
         if (error13>tolerance) {
            Assert.Fail("wrong clean price for zero coupon bond:"
                        + "\n  generic zero bond's clean price: "
                        + zeroCpnBondTheoValue1
                        + "\n  specialized zero bond's clean price: "
                        + zeroCpnSpecializedBondTheoValue1
                        + "\n  error:                 " + error13
                        + "\n  tolerance:             " + tolerance);
         }
         double zeroCpnBondTheoDirty1 = zeroCpnBondTheoValue1+
                                    zeroCpnBond1.accruedAmount();
         double zeroCpnSpecializedBondTheoDirty1 =
            zeroCpnSpecializedBondTheoValue1+
            zeroCpnSpecializedBond1.accruedAmount();
         double error14 =
            Math.Abs(zeroCpnBondTheoDirty1-zeroCpnSpecializedBondTheoDirty1);
         if (error14>tolerance) {
            Assert.Fail("wrong dirty price for zero bond:"
                        + "\n  generic zerocpn bond's dirty price: "
                        + zeroCpnBondTheoDirty1
                        + "\n  specialized zerocpn bond's clean price: "
                        + zeroCpnSpecializedBondTheoDirty1
                        + "\n  error:                 " + error14
                        + "\n  tolerance:             " + tolerance);
         }

         // Zero Coupon bond (Isin: IT0001200390 ISPIM 0 02/17/28)
         // maturity occurs on a business day
         Date zeroCpnBondStartDate2 = new Date(17,Month.February,1998);
         Date zeroCpnBondMaturityDate2 = new Date(17,Month.February,2028);
         Date zerocpbondRedemption2 = bondCalendar.adjust(zeroCpnBondMaturityDate2,
                                                         BusinessDayConvention.Following);
         List<CashFlow> zeroCpnBondLeg2 = new List<CashFlow>{new SimpleCashFlow(100.0, zerocpbondRedemption2)};
         // generic bond
         Bond zeroCpnBond2 = new  Bond(settlementDays, bondCalendar, vars.faceAmount,
                  zeroCpnBondMaturityDate2, zeroCpnBondStartDate2, zeroCpnBondLeg2);
         zeroCpnBond2.setPricingEngine(bondEngine);

         // specialized zerocpn bond
         Bond zeroCpnSpecializedBond2 = new ZeroCouponBond(settlementDays, bondCalendar, vars.faceAmount,
                        new Date(17,Month.February,2028),
                        BusinessDayConvention.Following,
                        100.0, new Date(17,Month.February,1998));
         zeroCpnSpecializedBond2.setPricingEngine(bondEngine);

         double zeroCpnBondTheoValue2 = zeroCpnBond2.cleanPrice();
         double zeroCpnSpecializedBondTheoValue2 =
            zeroCpnSpecializedBond2.cleanPrice();

         double error15 =
            Math.Abs(zeroCpnBondTheoValue2 -zeroCpnSpecializedBondTheoValue2);
         if (error15>tolerance) {
            Assert.Fail("wrong clean price for zero coupon bond:"
                        + "\n  generic zerocpn bond's clean price: "
                        + zeroCpnBondTheoValue2
                        + "\n  specialized zerocpn bond's clean price: "
                        + zeroCpnSpecializedBondTheoValue2
                        + "\n  error:                 " + error15
                        + "\n  tolerance:             " + tolerance);
         }
         double zeroCpnBondTheoDirty2 = zeroCpnBondTheoValue2+
                                    zeroCpnBond2.accruedAmount();

         double zeroCpnSpecializedBondTheoDirty2 =
            zeroCpnSpecializedBondTheoValue2+
            zeroCpnSpecializedBond2.accruedAmount();

         double error16 =
            Math.Abs(zeroCpnBondTheoDirty2-zeroCpnSpecializedBondTheoDirty2);
         if (error16>tolerance) {
            Assert.Fail("wrong dirty price for zero coupon bond:"
                        + "\n  generic zerocpn bond's dirty price: "
                        + zeroCpnBondTheoDirty2
                        + "\n  specialized zerocpn bond's dirty price: "
                        + zeroCpnSpecializedBondTheoDirty2
                        + "\n  error:                 " + error16
                        + "\n  tolerance:             " + tolerance);
         }
   }

      [TestMethod()]
      public void testSpecializedBondVsGenericBondUsingAsw() 
      {
         // Testing asset-swap prices and spreads for specialized bond against equivalent generic bond...
         CommonVars vars = new CommonVars();

         Calendar bondCalendar = new TARGET();
         int settlementDays = 3;
         int fixingDays = 2;
         bool payFixedRate = true;
         bool parAssetSwap = true;
         bool inArrears = false;

         // Fixed bond (Isin: DE0001135275 DBR 4 01/04/37)
         // maturity doesn't occur on a business day
         Date fixedBondStartDate1 = new Date(4,Month.January,2005);
         Date fixedBondMaturityDate1 = new Date(4,Month.January,2037);
         Schedule fixedBondSchedule1 = new Schedule(fixedBondStartDate1,
                                    fixedBondMaturityDate1,
                                    new Period(Frequency.Annual), bondCalendar,
                                    BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                    DateGeneration.Rule.Backward, false);
         List<CashFlow> fixedBondLeg1 = new FixedRateLeg(fixedBondSchedule1)
            .withCouponRates(0.04, new ActualActual(ActualActual.Convention.ISDA))
            .withNotionals(vars.faceAmount);
         Date fixedbondRedemption1 = bondCalendar.adjust(fixedBondMaturityDate1, BusinessDayConvention.Following);
         fixedBondLeg1.Add(new SimpleCashFlow(100.0, fixedbondRedemption1));
         // generic bond
         Bond fixedBond1 = new Bond(settlementDays, bondCalendar, vars.faceAmount,
                  fixedBondMaturityDate1, fixedBondStartDate1, fixedBondLeg1);
         IPricingEngine bondEngine = new DiscountingBondEngine(vars.termStructure);
         IPricingEngine swapEngine = new DiscountingSwapEngine(vars.termStructure);
         fixedBond1.setPricingEngine(bondEngine);

         // equivalent specialized fixed rate bond
         Bond fixedSpecializedBond1 = new FixedRateBond(settlementDays, vars.faceAmount, fixedBondSchedule1,
                           new List<double>{0.04},
                           new ActualActual(ActualActual.Convention.ISDA), BusinessDayConvention.Following,
                           100.0, new Date(4,Month.January,2005));
         fixedSpecializedBond1.setPricingEngine(bondEngine);

         double fixedBondPrice1 = fixedBond1.cleanPrice();
         double fixedSpecializedBondPrice1 = fixedSpecializedBond1.cleanPrice();
         AssetSwap fixedBondAssetSwap1 = new AssetSwap(payFixedRate,
                                       fixedBond1, fixedBondPrice1,
                                       vars.iborIndex, vars.nonnullspread,
                                       null,
                                       vars.iborIndex.dayCounter(),
                                       parAssetSwap);
         fixedBondAssetSwap1.setPricingEngine(swapEngine);
         AssetSwap fixedSpecializedBondAssetSwap1 = new AssetSwap(payFixedRate,
                                                fixedSpecializedBond1,
                                                fixedSpecializedBondPrice1,
                                                vars.iborIndex,
                                                vars.nonnullspread,
                                                null,
                                                vars.iborIndex.dayCounter(),
                                                parAssetSwap);
         fixedSpecializedBondAssetSwap1.setPricingEngine(swapEngine);
         double fixedBondAssetSwapPrice1 = fixedBondAssetSwap1.fairCleanPrice();
         double fixedSpecializedBondAssetSwapPrice1 =
            fixedSpecializedBondAssetSwap1.fairCleanPrice();
         double tolerance = 1.0e-13;
         double error1 =
            Math.Abs(fixedBondAssetSwapPrice1-fixedSpecializedBondAssetSwapPrice1);
         if (error1>tolerance) {
            Assert.Fail("wrong clean price for fixed bond:"
                        + "\n  generic  fixed rate bond's  clean price: "
                        + fixedBondAssetSwapPrice1
                        + "\n  equivalent specialized bond's clean price: "
                        + fixedSpecializedBondAssetSwapPrice1
                        + "\n  error:                 " + error1
                        + "\n  tolerance:             " + tolerance);
         }
         // market executable price as of 4th sept 2007
         double fixedBondMktPrice1= 91.832;
         AssetSwap fixedBondASW1 = new AssetSwap(payFixedRate,
                                 fixedBond1, fixedBondMktPrice1,
                                 vars.iborIndex, vars.spread,
                                 null,
                                 vars.iborIndex.dayCounter(),
                                 parAssetSwap);
         fixedBondASW1.setPricingEngine(swapEngine);
         AssetSwap fixedSpecializedBondASW1 = new AssetSwap(payFixedRate,
                                          fixedSpecializedBond1,
                                          fixedBondMktPrice1,
                                          vars.iborIndex, vars.spread,
                                          null,
                                          vars.iborIndex.dayCounter(),
                                          parAssetSwap);
         fixedSpecializedBondASW1.setPricingEngine(swapEngine);
         double fixedBondASWSpread1 = fixedBondASW1.fairSpread();
         double fixedSpecializedBondASWSpread1 = fixedSpecializedBondASW1.fairSpread();
         double error2 = Math.Abs(fixedBondASWSpread1-fixedSpecializedBondASWSpread1);
         if (error2>tolerance) {
            Assert.Fail("wrong asw spread  for fixed bond:"
                        + "\n  generic  fixed rate bond's  asw spread: "
                        + fixedBondASWSpread1
                        + "\n  equivalent specialized bond's asw spread: "
                        + fixedSpecializedBondASWSpread1
                        + "\n  error:                 " + error2
                        + "\n  tolerance:             " + tolerance);
         }

         //Fixed bond (Isin: IT0006527060 IBRD 5 02/05/19)
         //maturity occurs on a business day

         Date fixedBondStartDate2 = new Date(5,Month.February,2005);
         Date fixedBondMaturityDate2 = new Date(5,Month.February,2019);
         Schedule fixedBondSchedule2= new Schedule(fixedBondStartDate2,
                                    fixedBondMaturityDate2,
                                    new Period(Frequency.Annual), bondCalendar,
                                    BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                    DateGeneration.Rule.Backward, false);
         List<CashFlow> fixedBondLeg2 = new FixedRateLeg(fixedBondSchedule2)
            .withCouponRates(0.05, new Thirty360(Thirty360.Thirty360Convention.BondBasis))
            .withNotionals(vars.faceAmount);
         Date fixedbondRedemption2 = bondCalendar.adjust(fixedBondMaturityDate2, BusinessDayConvention.Following);
         fixedBondLeg2.Add(new SimpleCashFlow(100.0, fixedbondRedemption2));

         // generic bond
         Bond fixedBond2 = new Bond(settlementDays, bondCalendar, vars.faceAmount,
                  fixedBondMaturityDate2, fixedBondStartDate2, fixedBondLeg2);
         fixedBond2.setPricingEngine(bondEngine);

         // equivalent specialized fixed rate bond
         Bond fixedSpecializedBond2 = new FixedRateBond(settlementDays, vars.faceAmount, fixedBondSchedule2,
                           new List<double>{ 0.05},
                           new Thirty360(Thirty360.Thirty360Convention.BondBasis), BusinessDayConvention.Following,
                           100.0, new Date(5,Month.February,2005));
         fixedSpecializedBond2.setPricingEngine(bondEngine);

         double fixedBondPrice2 = fixedBond2.cleanPrice();
         double fixedSpecializedBondPrice2 = fixedSpecializedBond2.cleanPrice();
         AssetSwap fixedBondAssetSwap2 = new AssetSwap(payFixedRate,
                                       fixedBond2, fixedBondPrice2,
                                       vars.iborIndex, vars.nonnullspread,
                                       null,
                                       vars.iborIndex.dayCounter(),
                                       parAssetSwap);
         fixedBondAssetSwap2.setPricingEngine(swapEngine);
         AssetSwap fixedSpecializedBondAssetSwap2 = new AssetSwap(payFixedRate,
                                                fixedSpecializedBond2,
                                                fixedSpecializedBondPrice2,
                                                vars.iborIndex,
                                                vars.nonnullspread,
                                                null,
                                                vars.iborIndex.dayCounter(),
                                                parAssetSwap);
         fixedSpecializedBondAssetSwap2.setPricingEngine(swapEngine);
         double fixedBondAssetSwapPrice2 = fixedBondAssetSwap2.fairCleanPrice();
         double fixedSpecializedBondAssetSwapPrice2 = fixedSpecializedBondAssetSwap2.fairCleanPrice();

         double error3 = Math.Abs(fixedBondAssetSwapPrice2-fixedSpecializedBondAssetSwapPrice2);
         if (error3>tolerance) {
            Assert.Fail("wrong clean price for fixed bond:"
                        + "\n  generic  fixed rate bond's clean price: "
                        + fixedBondAssetSwapPrice2
                        + "\n  equivalent specialized  bond's clean price: "
                        + fixedSpecializedBondAssetSwapPrice2
                        + "\n  error:                 " + error3
                        + "\n  tolerance:             " + tolerance);
         }
         // market executable price as of 4th sept 2007
         double fixedBondMktPrice2= 102.178;
         AssetSwap fixedBondASW2 = new AssetSwap(payFixedRate,
                                 fixedBond2, fixedBondMktPrice2,
                                 vars.iborIndex, vars.spread,
                                 null,
                                 vars.iborIndex.dayCounter(),
                                 parAssetSwap);
         fixedBondASW2.setPricingEngine(swapEngine);
         AssetSwap fixedSpecializedBondASW2 = new AssetSwap(payFixedRate,
                                          fixedSpecializedBond2,
                                          fixedBondMktPrice2,
                                          vars.iborIndex, vars.spread,
                                          null,
                                          vars.iborIndex.dayCounter(),
                                          parAssetSwap);
         fixedSpecializedBondASW2.setPricingEngine(swapEngine);
         double fixedBondASWSpread2 = fixedBondASW2.fairSpread();
         double fixedSpecializedBondASWSpread2 = fixedSpecializedBondASW2.fairSpread();
         double error4 = Math.Abs(fixedBondASWSpread2-fixedSpecializedBondASWSpread2);
         if (error4>tolerance) {
            Assert.Fail("wrong asw spread for fixed bond:"
                        + "\n  generic  fixed rate bond's  asw spread: "
                        + fixedBondASWSpread2
                        + "\n  equivalent specialized bond's asw spread: "
                        + fixedSpecializedBondASWSpread2
                        + "\n  error:                 " + error4
                        + "\n  tolerance:             " + tolerance);
         }


         //FRN bond (Isin: IT0003543847 ISPIM 0 09/29/13)
         //maturity doesn't occur on a business day
         Date floatingBondStartDate1 = new Date(29,Month.September,2003);
         Date floatingBondMaturityDate1 = new Date(29,Month.September,2013);
         Schedule floatingBondSchedule1= new Schedule(floatingBondStartDate1,
                                       floatingBondMaturityDate1,
                                       new Period(Frequency.Semiannual), bondCalendar,
                                       BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                       DateGeneration.Rule.Backward, false);
         List<CashFlow> floatingBondLeg1 = new IborLeg(floatingBondSchedule1, vars.iborIndex)
            .withPaymentDayCounter(new Actual360())
            .withFixingDays(fixingDays)
            .withSpreads(0.0056)
            .inArrears(inArrears)
            .withNotionals(vars.faceAmount);
         Date floatingbondRedemption1 = bondCalendar.adjust(floatingBondMaturityDate1, BusinessDayConvention.Following);
         floatingBondLeg1.Add(new SimpleCashFlow(100.0, floatingbondRedemption1));
         // generic bond
         Bond floatingBond1 = new Bond(settlementDays, bondCalendar, vars.faceAmount,
                  floatingBondMaturityDate1, floatingBondStartDate1, floatingBondLeg1);
         floatingBond1.setPricingEngine(bondEngine);

         // equivalent specialized floater
         Bond floatingSpecializedBond1 = new FloatingRateBond(settlementDays, vars.faceAmount,
                                 floatingBondSchedule1,
                                 vars.iborIndex, new Actual360(),
                                 BusinessDayConvention.Following, fixingDays,
                                 new List<double>{1},
                                 new List<double>{0.0056},
                                 new List<double>(), new List<double>(),
                                 inArrears,
                                 100.0, new Date(29,Month.September,2003));
         floatingSpecializedBond1.setPricingEngine(bondEngine);

         Utils.setCouponPricer(floatingBond1.cashflows(), vars.pricer);
         Utils.setCouponPricer(floatingSpecializedBond1.cashflows(), vars.pricer);
         vars.iborIndex.addFixing(new Date(27,Month.March,2007), 0.0402);
         double floatingBondPrice1 = floatingBond1.cleanPrice();
         double floatingSpecializedBondPrice1= floatingSpecializedBond1.cleanPrice();
         AssetSwap floatingBondAssetSwap1= new AssetSwap(payFixedRate,
                                          floatingBond1, floatingBondPrice1,
                                          vars.iborIndex, vars.nonnullspread,
                                          null,
                                          vars.iborIndex.dayCounter(),
                                          parAssetSwap);
         floatingBondAssetSwap1.setPricingEngine(swapEngine);
         AssetSwap floatingSpecializedBondAssetSwap1= new AssetSwap(payFixedRate,
                                                   floatingSpecializedBond1,
                                                   floatingSpecializedBondPrice1,
                                                   vars.iborIndex,
                                                   vars.nonnullspread,
                                                   null,
                                                   vars.iborIndex.dayCounter(),
                                                   parAssetSwap);
         floatingSpecializedBondAssetSwap1.setPricingEngine(swapEngine);
         double floatingBondAssetSwapPrice1 = floatingBondAssetSwap1.fairCleanPrice();
         double floatingSpecializedBondAssetSwapPrice1 =
            floatingSpecializedBondAssetSwap1.fairCleanPrice();

         double error5 =
            Math.Abs(floatingBondAssetSwapPrice1-floatingSpecializedBondAssetSwapPrice1);
         if (error5>tolerance) {
            Assert.Fail("wrong clean price for frnbond:"
                        + "\n  generic frn rate bond's clean price: "
                        + floatingBondAssetSwapPrice1
                        + "\n  equivalent specialized  bond's price: "
                        + floatingSpecializedBondAssetSwapPrice1
                        + "\n  error:                 " + error5
                        + "\n  tolerance:             " + tolerance);
         }
         // market executable price as of 4th sept 2007
         double floatingBondMktPrice1= 101.33;
         AssetSwap floatingBondASW1= new AssetSwap(payFixedRate,
                                    floatingBond1, floatingBondMktPrice1,
                                    vars.iborIndex, vars.spread,
                                    null,
                                    vars.iborIndex.dayCounter(),
                                    parAssetSwap);
         floatingBondASW1.setPricingEngine(swapEngine);
         AssetSwap floatingSpecializedBondASW1= new AssetSwap(payFixedRate,
                                             floatingSpecializedBond1,
                                             floatingBondMktPrice1,
                                             vars.iborIndex, vars.spread,
                                             null,
                                             vars.iborIndex.dayCounter(),
                                             parAssetSwap);
         floatingSpecializedBondASW1.setPricingEngine(swapEngine);
         double floatingBondASWSpread1 = floatingBondASW1.fairSpread();
         double floatingSpecializedBondASWSpread1 =
            floatingSpecializedBondASW1.fairSpread();
         double error6 =
            Math.Abs(floatingBondASWSpread1-floatingSpecializedBondASWSpread1);
         if (error6>tolerance) {
            Assert.Fail("wrong asw spread for fixed bond:"
                        + "\n  generic  frn rate bond's  asw spread: "
                        + floatingBondASWSpread1
                        + "\n  equivalent specialized bond's asw spread: "
                        + floatingSpecializedBondASWSpread1
                        + "\n  error:                 " + error6
                        + "\n  tolerance:             " + tolerance);
         }
         //FRN bond (Isin: XS0090566539 COE 0 09/24/18)
         //maturity occurs on a business day
         Date floatingBondStartDate2 = new Date(24,Month.September,2004);
         Date floatingBondMaturityDate2 = new Date(24,Month.September,2018);
         Schedule floatingBondSchedule2= new Schedule(floatingBondStartDate2,
                                       floatingBondMaturityDate2,
                                       new Period(Frequency.Semiannual), bondCalendar,
                                       BusinessDayConvention.ModifiedFollowing, BusinessDayConvention.ModifiedFollowing,
                                       DateGeneration.Rule.Backward, false);
         List<CashFlow> floatingBondLeg2 = new IborLeg(floatingBondSchedule2, vars.iborIndex)
            .withPaymentDayCounter(new Actual360())
            .withFixingDays(fixingDays)
            .withSpreads(0.0025)
            .inArrears(inArrears)
            .withPaymentAdjustment(BusinessDayConvention.ModifiedFollowing)
            .withNotionals(vars.faceAmount);
         Date floatingbondRedemption2 = bondCalendar.adjust(floatingBondMaturityDate2, BusinessDayConvention.ModifiedFollowing);
         floatingBondLeg2.Add(new SimpleCashFlow(100.0, floatingbondRedemption2));
         // generic bond
         Bond floatingBond2 = new  Bond(settlementDays, bondCalendar, vars.faceAmount,
                  floatingBondMaturityDate2, floatingBondStartDate2,floatingBondLeg2);
         floatingBond2.setPricingEngine(bondEngine);

         // equivalent specialized floater
         Bond floatingSpecializedBond2 = new FloatingRateBond(settlementDays, vars.faceAmount,
                              floatingBondSchedule2,
                              vars.iborIndex, new Actual360(),
                              BusinessDayConvention.ModifiedFollowing, fixingDays,
                              new List<double>{1},
                              new List<double>{0.0025},
                              new List<double>(), new List<double>(),
                              inArrears,
                              100.0, new Date(24,Month.September,2004));
         floatingSpecializedBond2.setPricingEngine(bondEngine);

         Utils.setCouponPricer(floatingBond2.cashflows(), vars.pricer);
         Utils.setCouponPricer(floatingSpecializedBond2.cashflows(), vars.pricer);

         vars.iborIndex.addFixing(new Date(22,Month.March,2007), 0.04013);

         double floatingBondPrice2 = floatingBond2.cleanPrice();
         double floatingSpecializedBondPrice2= floatingSpecializedBond2.cleanPrice();
         AssetSwap floatingBondAssetSwap2= new AssetSwap(payFixedRate,
                                          floatingBond2, floatingBondPrice2,
                                          vars.iborIndex, vars.nonnullspread,
                                          null,
                                          vars.iborIndex.dayCounter(),
                                          parAssetSwap);
         floatingBondAssetSwap2.setPricingEngine(swapEngine);
         AssetSwap floatingSpecializedBondAssetSwap2= new AssetSwap(payFixedRate,
                                                   floatingSpecializedBond2,
                                                   floatingSpecializedBondPrice2,
                                                   vars.iborIndex,
                                                   vars.nonnullspread,
                                                   null,
                                                   vars.iborIndex.dayCounter(),
                                                   parAssetSwap);
         floatingSpecializedBondAssetSwap2.setPricingEngine(swapEngine);
         double floatingBondAssetSwapPrice2 = floatingBondAssetSwap2.fairCleanPrice();
         double floatingSpecializedBondAssetSwapPrice2 =
            floatingSpecializedBondAssetSwap2.fairCleanPrice();
         double error7 =
            Math.Abs(floatingBondAssetSwapPrice2-floatingSpecializedBondAssetSwapPrice2);
         if (error7>tolerance) {
            Assert.Fail("wrong clean price for frnbond:"
                        + "\n  generic frn rate bond's clean price: "
                        + floatingBondAssetSwapPrice2
                        + "\n  equivalent specialized frn  bond's price: "
                        + floatingSpecializedBondAssetSwapPrice2
                        + "\n  error:                 " + error7
                        + "\n  tolerance:             " + tolerance);
         }
         // market executable price as of 4th sept 2007
         double floatingBondMktPrice2 = 101.26;
         AssetSwap floatingBondASW2= new AssetSwap(payFixedRate,
                                    floatingBond2, floatingBondMktPrice2,
                                    vars.iborIndex, vars.spread,
                                    null,
                                    vars.iborIndex.dayCounter(),
                                    parAssetSwap);
         floatingBondASW2.setPricingEngine(swapEngine);
         AssetSwap floatingSpecializedBondASW2= new AssetSwap(payFixedRate,
                                             floatingSpecializedBond2,
                                             floatingBondMktPrice2,
                                             vars.iborIndex, vars.spread,
                                             null,
                                             vars.iborIndex.dayCounter(),
                                             parAssetSwap);
         floatingSpecializedBondASW2.setPricingEngine(swapEngine);
         double floatingBondASWSpread2 = floatingBondASW2.fairSpread();
         double floatingSpecializedBondASWSpread2 =
            floatingSpecializedBondASW2.fairSpread();
         double error8 =
            Math.Abs(floatingBondASWSpread2-floatingSpecializedBondASWSpread2);
         if (error8>tolerance) {
            Assert.Fail("wrong asw spread for frn bond:"
                        + "\n  generic  frn rate bond's  asw spread: "
                        + floatingBondASWSpread2
                        + "\n  equivalent specialized bond's asw spread: "
                        + floatingSpecializedBondASWSpread2
                        + "\n  error:                 " + error8
                        + "\n  tolerance:             " + tolerance);
         }

         // CMS bond (Isin: XS0228052402 CRDIT 0 8/22/20)
         // maturity doesn't occur on a business day
         Date cmsBondStartDate1 = new Date(22,Month.August,2005);
         Date cmsBondMaturityDate1 = new Date(22,Month.August,2020);
         Schedule cmsBondSchedule1= new Schedule(cmsBondStartDate1,
                                 cmsBondMaturityDate1,
                                 new Period(Frequency.Annual), bondCalendar,
                                 BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                 DateGeneration.Rule.Backward, false);
         List<CashFlow> cmsBondLeg1 = new CmsLeg(cmsBondSchedule1, vars.swapIndex)
            .withPaymentDayCounter(new Thirty360())
            .withFixingDays(fixingDays)
            .withCaps(0.055)
            .withFloors(0.025)
            .inArrears(inArrears)
            .withNotionals(vars.faceAmount);
         Date cmsbondRedemption1 = bondCalendar.adjust(cmsBondMaturityDate1, BusinessDayConvention.Following);
         cmsBondLeg1.Add(new SimpleCashFlow(100.0, cmsbondRedemption1));
         // generic cms bond
         Bond cmsBond1 = new  Bond(settlementDays, bondCalendar, vars.faceAmount,
                  cmsBondMaturityDate1, cmsBondStartDate1, cmsBondLeg1);
         cmsBond1.setPricingEngine(bondEngine);

         // equivalent specialized cms bond
         Bond cmsSpecializedBond1 = new CmsRateBond(settlementDays, vars.faceAmount, cmsBondSchedule1,
                     vars.swapIndex, new Thirty360(),
                     BusinessDayConvention.Following, fixingDays,
                     new List<double>{1.0}, new List<double>{0.0},
                     new List<double>{0.055}, new List<double>{0.025},
                     inArrears,
                     100.0, new Date(22,Month.August,2005));
         cmsSpecializedBond1.setPricingEngine(bondEngine);


         Utils.setCouponPricer(cmsBond1.cashflows(), vars.cmspricer);
         Utils.setCouponPricer(cmsSpecializedBond1.cashflows(), vars.cmspricer);
         vars.swapIndex.addFixing(new Date(18,Month.August,2006), 0.04158);
         double cmsBondPrice1 = cmsBond1.cleanPrice();
         double cmsSpecializedBondPrice1 = cmsSpecializedBond1.cleanPrice();
         AssetSwap cmsBondAssetSwap1= new AssetSwap(payFixedRate,cmsBond1, cmsBondPrice1,
                                    vars.iborIndex, vars.nonnullspread,
                                    null,vars.iborIndex.dayCounter(),
                                    parAssetSwap);
         cmsBondAssetSwap1.setPricingEngine(swapEngine);
         AssetSwap cmsSpecializedBondAssetSwap1= new AssetSwap(payFixedRate,cmsSpecializedBond1,
                                                cmsSpecializedBondPrice1,
                                                vars.iborIndex,
                                                vars.nonnullspread,
                                                null,
                                                vars.iborIndex.dayCounter(),
                                                parAssetSwap);
         cmsSpecializedBondAssetSwap1.setPricingEngine(swapEngine);
         double cmsBondAssetSwapPrice1 = cmsBondAssetSwap1.fairCleanPrice();
         double cmsSpecializedBondAssetSwapPrice1 =
            cmsSpecializedBondAssetSwap1.fairCleanPrice();
         double error9 =
            Math.Abs(cmsBondAssetSwapPrice1-cmsSpecializedBondAssetSwapPrice1);
         if (error9>tolerance) {
            Assert.Fail("wrong clean price for cmsbond:"
                        + "\n  generic bond's clean price: "
                        + cmsBondAssetSwapPrice1
                        + "\n  equivalent specialized cms rate bond's price: "
                        + cmsSpecializedBondAssetSwapPrice1
                        + "\n  error:                 " + error9
                        + "\n  tolerance:             " + tolerance);
         }
         double cmsBondMktPrice1 = 87.02;// market executable price as of 4th sept 2007
         AssetSwap cmsBondASW1= new AssetSwap(payFixedRate,
                              cmsBond1, cmsBondMktPrice1,
                              vars.iborIndex, vars.spread,
                              null,
                              vars.iborIndex.dayCounter(),
                              parAssetSwap);
         cmsBondASW1.setPricingEngine(swapEngine);
         AssetSwap cmsSpecializedBondASW1= new AssetSwap(payFixedRate,
                                          cmsSpecializedBond1,
                                          cmsBondMktPrice1,
                                          vars.iborIndex, vars.spread,
                                          null,
                                          vars.iborIndex.dayCounter(),
                                          parAssetSwap);
         cmsSpecializedBondASW1.setPricingEngine(swapEngine);
         double cmsBondASWSpread1 = cmsBondASW1.fairSpread();
         double cmsSpecializedBondASWSpread1 = cmsSpecializedBondASW1.fairSpread();
         double error10 = Math.Abs(cmsBondASWSpread1-cmsSpecializedBondASWSpread1);
         if (error10>tolerance) {
            Assert.Fail("wrong asw spread for cm bond:"
                        + "\n  generic cms rate bond's  asw spread: "
                        + cmsBondASWSpread1
                        + "\n  equivalent specialized bond's asw spread: "
                        + cmsSpecializedBondASWSpread1
                        + "\n  error:                 " + error10
                        + "\n  tolerance:             " + tolerance);
         }

         //CMS bond (Isin: XS0218766664 ISPIM 0 5/6/15)
         //maturity occurs on a business day
         Date cmsBondStartDate2 = new Date(06,Month.May,2005);
         Date cmsBondMaturityDate2 = new Date(06,Month.May,2015);
         Schedule cmsBondSchedule2= new Schedule(cmsBondStartDate2,
                                 cmsBondMaturityDate2,
                                 new Period(Frequency.Annual), bondCalendar,
                                 BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                 DateGeneration.Rule.Backward, false);
         List<CashFlow> cmsBondLeg2 = new CmsLeg(cmsBondSchedule2, vars.swapIndex)
            .withPaymentDayCounter(new Thirty360())
            .withFixingDays(fixingDays)
            .withGearings(0.84)
            .inArrears(inArrears)
            .withNotionals(vars.faceAmount);
         Date cmsbondRedemption2 = bondCalendar.adjust(cmsBondMaturityDate2,
                                                      BusinessDayConvention.Following);
         cmsBondLeg2.Add(new SimpleCashFlow(100.0, cmsbondRedemption2));
         // generic bond
         Bond cmsBond2 = new  Bond(settlementDays, bondCalendar, vars.faceAmount,
                  cmsBondMaturityDate2, cmsBondStartDate2, cmsBondLeg2);
         cmsBond2.setPricingEngine(bondEngine);

         // equivalent specialized cms bond
         Bond cmsSpecializedBond2 = new  CmsRateBond(settlementDays, vars.faceAmount, cmsBondSchedule2,
                     vars.swapIndex, new Thirty360(),
                     BusinessDayConvention.Following, fixingDays,
                     new List<double>{0.84}, new List<double>{0.0},
                     new List<double>(), new List<double>(),
                     inArrears,
                     100.0, new Date(06,Month.May,2005));
         cmsSpecializedBond2.setPricingEngine(bondEngine);

         Utils.setCouponPricer(cmsBond2.cashflows(), vars.cmspricer);
         Utils.setCouponPricer(cmsSpecializedBond2.cashflows(), vars.cmspricer);
         vars.swapIndex.addFixing(new Date(04,Month.May,2006), 0.04217);
         double cmsBondPrice2 = cmsBond2.cleanPrice();
         double cmsSpecializedBondPrice2 = cmsSpecializedBond2.cleanPrice();
         AssetSwap cmsBondAssetSwap2= new AssetSwap(payFixedRate,cmsBond2, cmsBondPrice2,
                                    vars.iborIndex, vars.nonnullspread,
                                    null,
                                    vars.iborIndex.dayCounter(),
                                    parAssetSwap);
         cmsBondAssetSwap2.setPricingEngine(swapEngine);
         AssetSwap cmsSpecializedBondAssetSwap2= new AssetSwap(payFixedRate,cmsSpecializedBond2,
                                                cmsSpecializedBondPrice2,
                                                vars.iborIndex,
                                                vars.nonnullspread,
                                                null,
                                                vars.iborIndex.dayCounter(),
                                                parAssetSwap);
         cmsSpecializedBondAssetSwap2.setPricingEngine(swapEngine);
         double cmsBondAssetSwapPrice2 = cmsBondAssetSwap2.fairCleanPrice();
         double cmsSpecializedBondAssetSwapPrice2 =
            cmsSpecializedBondAssetSwap2.fairCleanPrice();
         double error11 =
            Math.Abs(cmsBondAssetSwapPrice2-cmsSpecializedBondAssetSwapPrice2);
         if (error11>tolerance) {
            Assert.Fail("wrong clean price for cmsbond:"
                        + "\n  generic  bond's clean price: "
                        + cmsBondAssetSwapPrice2
                        + "\n  equivalent specialized cms rate bond's price: "
                        + cmsSpecializedBondAssetSwapPrice2
                        + "\n  error:                 " + error11
                        + "\n  tolerance:             " + tolerance);
         }
         double cmsBondMktPrice2 = 94.35;// market executable price as of 4th sept 2007
         AssetSwap cmsBondASW2= new AssetSwap(payFixedRate,
                              cmsBond2, cmsBondMktPrice2,
                              vars.iborIndex, vars.spread,
                              null,
                              vars.iborIndex.dayCounter(),
                              parAssetSwap);
         cmsBondASW2.setPricingEngine(swapEngine);
         AssetSwap cmsSpecializedBondASW2= new AssetSwap(payFixedRate,
                                          cmsSpecializedBond2,
                                          cmsBondMktPrice2,
                                          vars.iborIndex, vars.spread,
                                          null,
                                          vars.iborIndex.dayCounter(),
                                          parAssetSwap);
         cmsSpecializedBondASW2.setPricingEngine(swapEngine);
         double cmsBondASWSpread2 = cmsBondASW2.fairSpread();
         double cmsSpecializedBondASWSpread2 = cmsSpecializedBondASW2.fairSpread();
         double error12 = Math.Abs(cmsBondASWSpread2-cmsSpecializedBondASWSpread2);
         if (error12>tolerance) {
            Assert.Fail("wrong asw spread for cm bond:"
                        + "\n  generic cms rate bond's  asw spread: "
                        + cmsBondASWSpread2
                        + "\n  equivalent specialized bond's asw spread: "
                        + cmsSpecializedBondASWSpread2
                        + "\n  error:                 " + error12
                        + "\n  tolerance:             " + tolerance);
         }


      //  Zero-Coupon bond (Isin: DE0004771662 IBRD 0 12/20/15)
      //  maturity doesn't occur on a business day
         Date zeroCpnBondStartDate1 = new Date(19,Month.December,1985);
         Date zeroCpnBondMaturityDate1 = new Date(20,Month.December,2015);
         Date zeroCpnBondRedemption1 = bondCalendar.adjust(zeroCpnBondMaturityDate1,
                                                         BusinessDayConvention.Following);
         List<CashFlow> zeroCpnBondLeg1 = new List<CashFlow>{new SimpleCashFlow(100.0, zeroCpnBondRedemption1)};
         // generic bond
         Bond zeroCpnBond1 = new Bond(settlementDays, bondCalendar, vars.faceAmount,
                  zeroCpnBondMaturityDate1, zeroCpnBondStartDate1, zeroCpnBondLeg1);
         zeroCpnBond1.setPricingEngine(bondEngine);

         // specialized zerocpn bond
         Bond zeroCpnSpecializedBond1= new ZeroCouponBond(settlementDays, bondCalendar, vars.faceAmount,
                     new Date(20,Month.December,2015),
                     BusinessDayConvention.Following,
                     100.0, new Date(19,Month.December,1985));
         zeroCpnSpecializedBond1.setPricingEngine(bondEngine);

         double zeroCpnBondPrice1 = zeroCpnBond1.cleanPrice();
         double zeroCpnSpecializedBondPrice1 = zeroCpnSpecializedBond1.cleanPrice();
         AssetSwap zeroCpnBondAssetSwap1= new AssetSwap(payFixedRate,zeroCpnBond1,
                                       zeroCpnBondPrice1,
                                       vars.iborIndex, vars.nonnullspread,
                                       null,
                                       vars.iborIndex.dayCounter(),
                                       parAssetSwap);
         zeroCpnBondAssetSwap1.setPricingEngine(swapEngine);
         AssetSwap zeroCpnSpecializedBondAssetSwap1= new AssetSwap(payFixedRate,
                                                   zeroCpnSpecializedBond1,
                                                   zeroCpnSpecializedBondPrice1,
                                                   vars.iborIndex,
                                                   vars.nonnullspread,
                                                   null,
                                                   vars.iborIndex.dayCounter(),
                                                   parAssetSwap);
         zeroCpnSpecializedBondAssetSwap1.setPricingEngine(swapEngine);
         double zeroCpnBondAssetSwapPrice1 = zeroCpnBondAssetSwap1.fairCleanPrice();
         double zeroCpnSpecializedBondAssetSwapPrice1 =
            zeroCpnSpecializedBondAssetSwap1.fairCleanPrice();
         double error13 =
            Math.Abs(zeroCpnBondAssetSwapPrice1-zeroCpnSpecializedBondAssetSwapPrice1);
         if (error13>tolerance) {
            Assert.Fail("wrong clean price for zerocpn bond:"
                        + "\n  generic zero cpn bond's clean price: "
                        + zeroCpnBondAssetSwapPrice1
                        + "\n  specialized equivalent bond's price: "
                        + zeroCpnSpecializedBondAssetSwapPrice1
                        + "\n  error:                 " + error13
                        + "\n  tolerance:             " + tolerance);
         }
         // market executable price as of 4th sept 2007
         double zeroCpnBondMktPrice1 = 72.277;
         AssetSwap zeroCpnBondASW1= new AssetSwap(payFixedRate,
                                 zeroCpnBond1,zeroCpnBondMktPrice1,
                                 vars.iborIndex, vars.spread,
                                 null,
                                 vars.iborIndex.dayCounter(),
                                 parAssetSwap);
         zeroCpnBondASW1.setPricingEngine(swapEngine);
         AssetSwap zeroCpnSpecializedBondASW1= new AssetSwap(payFixedRate,
                                             zeroCpnSpecializedBond1,
                                             zeroCpnBondMktPrice1,
                                             vars.iborIndex, vars.spread,
                                             null,
                                             vars.iborIndex.dayCounter(),
                                             parAssetSwap);
         zeroCpnSpecializedBondASW1.setPricingEngine(swapEngine);
         double zeroCpnBondASWSpread1 = zeroCpnBondASW1.fairSpread();
         double zeroCpnSpecializedBondASWSpread1 =
            zeroCpnSpecializedBondASW1.fairSpread();
         double error14 =
            Math.Abs(zeroCpnBondASWSpread1-zeroCpnSpecializedBondASWSpread1);
         if (error14>tolerance) {
            Assert.Fail("wrong asw spread for zeroCpn bond:"
                        + "\n  generic zeroCpn bond's  asw spread: "
                        + zeroCpnBondASWSpread1
                        + "\n  equivalent specialized bond's asw spread: "
                        + zeroCpnSpecializedBondASWSpread1
                        + "\n  error:                 " + error14
                        + "\n  tolerance:             " + tolerance);
         }


      //  Zero Coupon bond (Isin: IT0001200390 ISPIM 0 02/17/28)
      //  maturity doesn't occur on a business day
         Date zeroCpnBondStartDate2 = new Date(17,Month.February,1998);
         Date zeroCpnBondMaturityDate2 = new Date(17,Month.February,2028);
         Date zerocpbondRedemption2 = bondCalendar.adjust(zeroCpnBondMaturityDate2,
                                                         BusinessDayConvention.Following);
         List<CashFlow> zeroCpnBondLeg2 = new List<CashFlow>{new SimpleCashFlow(100.0, zerocpbondRedemption2)};
         // generic bond
         Bond zeroCpnBond2 = new Bond(settlementDays, bondCalendar, vars.faceAmount,
                  zeroCpnBondMaturityDate2, zeroCpnBondStartDate2, zeroCpnBondLeg2);
         zeroCpnBond2.setPricingEngine(bondEngine);

         // specialized zerocpn bond
         Bond zeroCpnSpecializedBond2 = new ZeroCouponBond(settlementDays, bondCalendar, vars.faceAmount,
                        new Date(17,Month.February,2028),
                        BusinessDayConvention.Following,
                        100.0, new Date(17,Month.February,1998));
         zeroCpnSpecializedBond2.setPricingEngine(bondEngine);

         double zeroCpnBondPrice2 = zeroCpnBond2.cleanPrice();
         double zeroCpnSpecializedBondPrice2 = zeroCpnSpecializedBond2.cleanPrice();

         AssetSwap zeroCpnBondAssetSwap2= new AssetSwap(payFixedRate,zeroCpnBond2,
                                       zeroCpnBondPrice2,
                                       vars.iborIndex, vars.nonnullspread,
                                       null,
                                       vars.iborIndex.dayCounter(),
                                       parAssetSwap);
         zeroCpnBondAssetSwap2.setPricingEngine(swapEngine);
         AssetSwap zeroCpnSpecializedBondAssetSwap2= new AssetSwap(payFixedRate,
                                                   zeroCpnSpecializedBond2,
                                                   zeroCpnSpecializedBondPrice2,
                                                   vars.iborIndex,
                                                   vars.nonnullspread,
                                                   null,
                                                   vars.iborIndex.dayCounter(),
                                                   parAssetSwap);
         zeroCpnSpecializedBondAssetSwap2.setPricingEngine(swapEngine);
         double zeroCpnBondAssetSwapPrice2 = zeroCpnBondAssetSwap2.fairCleanPrice();
         double zeroCpnSpecializedBondAssetSwapPrice2 =
                                    zeroCpnSpecializedBondAssetSwap2.fairCleanPrice();
         double error15 = Math.Abs(zeroCpnBondAssetSwapPrice2
                                 -zeroCpnSpecializedBondAssetSwapPrice2);
         if (error8>tolerance) {
            Assert.Fail("wrong clean price for zerocpn bond:"
                        + "\n  generic zero cpn bond's clean price: "
                        + zeroCpnBondAssetSwapPrice2
                        + "\n  equivalent specialized bond's price: "
                        + zeroCpnSpecializedBondAssetSwapPrice2
                        + "\n  error:                 " + error15
                        + "\n  tolerance:             " + tolerance);
         }
         // market executable price as of 4th sept 2007
         double zeroCpnBondMktPrice2 = 72.277;
         AssetSwap zeroCpnBondASW2= new AssetSwap(payFixedRate,
                                 zeroCpnBond2,zeroCpnBondMktPrice2,
                                 vars.iborIndex, vars.spread,
                                 null,
                                 vars.iborIndex.dayCounter(),
                                 parAssetSwap);
         zeroCpnBondASW2.setPricingEngine(swapEngine);
         AssetSwap zeroCpnSpecializedBondASW2= new AssetSwap(payFixedRate,
                                             zeroCpnSpecializedBond2,
                                             zeroCpnBondMktPrice2,
                                             vars.iborIndex, vars.spread,
                                             null,
                                             vars.iborIndex.dayCounter(),
                                             parAssetSwap);
         zeroCpnSpecializedBondASW2.setPricingEngine(swapEngine);
         double zeroCpnBondASWSpread2 = zeroCpnBondASW2.fairSpread();
         double zeroCpnSpecializedBondASWSpread2 =
            zeroCpnSpecializedBondASW2.fairSpread();
         double error16 =
            Math.Abs(zeroCpnBondASWSpread2-zeroCpnSpecializedBondASWSpread2);
         if (error16>tolerance) {
            Assert.Fail("wrong asw spread for zeroCpn bond:"
                        + "\n  generic zeroCpn bond's  asw spread: "
                        + zeroCpnBondASWSpread2
                        + "\n  equivalent specialized bond's asw spread: "
                        + zeroCpnSpecializedBondASWSpread2
                        + "\n  error:                 " + error16
                        + "\n  tolerance:             " + tolerance);
         }
   }


   }
}
