//  Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)
//  
//  This file is part of QLNet Project https://github.com/amaggiulli/qlnet
//  QLNet is free software: you can redistribute it and/or modify it
//  under the terms of the QLNet license.  You should have received a
//  copy of the license along with this program; if not, license is  
//  available online at <http://qlnet.sourceforge.net/License.html>.
//   
//  QLNet is a based on QuantLib, a free-software/open-source library
//  for financial quantitative analysts and developers - http://quantlib.org/
//  The QuantLib license is available online at http://quantlib.org/license.shtml.
//  
//  This program is distributed in the hope that it will be useful, but WITHOUT
//  ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
//  FOR A PARTICULAR PURPOSE.  See the license for more details.
using System;
using System.Collections.Generic;
#if QL_DOTNET_FRAMEWORK
using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
   using Xunit;
#endif
using QLNet;

namespace TestSuite
{
#if QL_DOTNET_FRAMEWORK
   [TestClass()]
#endif
   public class T_DigitalCoupon
   {
      private class CommonVars
      {
         // global data
         public Date today, settlement;
         public double nominal;
         public Calendar calendar;
         public IborIndex index;
         public int fixingDays;
         public RelinkableHandle<YieldTermStructure> termStructure;
         public double optionTolerance;
         public double blackTolerance;

         // cleanup
         SavedSettings backup;

         // setup
         public CommonVars() 
         {
            backup = new SavedSettings();
            termStructure = new RelinkableHandle<YieldTermStructure>();
            fixingDays = 2;
            nominal = 1000000.0;
            index = new Euribor6M(termStructure);
            calendar = index.fixingCalendar();
            today = calendar.adjust(Settings.evaluationDate());
            Settings.setEvaluationDate(today);
            settlement = calendar.advance(today,fixingDays,TimeUnit.Days);
            termStructure.linkTo(Utilities.flatRate(settlement,0.05,new Actual365Fixed()));
            optionTolerance = 1.0e-04;
            blackTolerance = 1e-10;
         }
      }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
      public void testAssetOrNothing() 
      {

         // Testing European asset-or-nothing digital coupon

         /*  Call Payoff = (aL+b)Heaviside(aL+b-X) =  a Max[L-X'] + (b+aX')Heaviside(L-X')
            Value Call = aF N(d1') + bN(d2')
            Put Payoff =  (aL+b)Heaviside(X-aL-b) = -a Max[X-L'] + (b+aX')Heaviside(X'-L)
            Value Put = aF N(-d1') + bN(-d2')
            where:
            d1' = ln(F/X')/stdDev + 0.5*stdDev;
         */

         CommonVars vars = new CommonVars();

         double[] vols = { 0.05, 0.15, 0.30 };
         double[] strikes = { 0.01, 0.02, 0.03, 0.04, 0.05, 0.06, 0.07 };
         double[] gearings = { 1.0, 2.8 };
         double[] spreads = { 0.0, 0.005 };

         double gap = 1e-7; /* low, in order to compare digital option value
                           with black formula result */
         DigitalReplication replication = new DigitalReplication(Replication.Type.Central, gap);
         for (int i = 0; i< vols.Length; i++) 
         {
            double capletVol = vols[i];
            RelinkableHandle<OptionletVolatilityStructure> vol = new RelinkableHandle<OptionletVolatilityStructure>();
            vol.linkTo(new ConstantOptionletVolatility(vars.today,vars.calendar, BusinessDayConvention.Following, 
               capletVol, new Actual360()));
            for (int j=0; j<strikes.Length; j++) 
            {
               double strike = strikes[j];
               for (int k=9; k<10; k++) 
               {
                  Date startDate = vars.calendar.advance(vars.settlement,new Period(k+1,TimeUnit.Years));
                  Date endDate = vars.calendar.advance(vars.settlement,new Period(k+2,TimeUnit.Years));
                  double? nullstrike = null;
                  Date paymentDate = endDate;
                  for (int h=0; h<gearings.Length; h++) 
                  {
                     double gearing = gearings[h];
                     double spread = spreads[h];

                     FloatingRateCoupon underlying = new IborCoupon(paymentDate, vars.nominal,startDate, endDate,
                        vars.fixingDays, vars.index,gearing, spread);
                     // Floating Rate Coupon - Call Digital option
                     DigitalCoupon digitalCappedCoupon = new DigitalCoupon(underlying,
                                          strike, Position.Type.Short, false, nullstrike,
                                          nullstrike, Position.Type.Short, false, nullstrike,
                                          replication);
                     IborCouponPricer pricer = new BlackIborCouponPricer(vol);
                     digitalCappedCoupon.setPricer(pricer);

                     // Check digital option price vs N(d1) price
                     double accrualPeriod = underlying.accrualPeriod();
                     double discount = vars.termStructure.link.discount(endDate);
                     Date exerciseDate = underlying.fixingDate();
                     double forward = underlying.rate();
                     double effFwd = (forward-spread)/gearing;
                     double effStrike = (strike-spread)/gearing;
                     double stdDev = Math.Sqrt(vol.link.blackVariance(exerciseDate, effStrike));
                     CumulativeNormalDistribution phi = new CumulativeNormalDistribution();
                     double d1 = Math.Log(effFwd/effStrike)/stdDev + 0.5*stdDev;
                     double d2 = d1 - stdDev;
                     double N_d1 = phi.value(d1);
                     double N_d2 = phi.value(d2);
                     double nd1Price = (gearing * effFwd * N_d1 + spread * N_d2)
                                      * vars.nominal * accrualPeriod * discount;
                     double optionPrice = digitalCappedCoupon.callOptionRate() *
                                       vars.nominal * accrualPeriod * discount;
                     double error = Math.Abs(nd1Price - optionPrice);
                     if (error>vars.optionTolerance)
                        QAssert.Fail("\nDigital Call Option:" +
                              "\nVolatility = " + (capletVol) +
                              "\nStrike = " + (strike) +
                              "\nExercise = " + k+1 + " years" +
                              "\nOption price by replication = "  + optionPrice +
                              "\nOption price by Cox-Rubinstein formula = " + nd1Price +
                              "\nError " + error);

                     // Check digital option price vs N(d1) price using Vanilla Option class
                     if (spread==0.0) 
                     {
                        Exercise exercise = new EuropeanExercise(exerciseDate);
                        double discountAtFixing = vars.termStructure.link.discount(exerciseDate);
                        SimpleQuote fwd = new SimpleQuote(effFwd*discountAtFixing);
                        SimpleQuote qRate = new SimpleQuote(0.0);
                        YieldTermStructure qTS = Utilities.flatRate(vars.today, qRate, new Actual360());
                        SimpleQuote vol1 = new SimpleQuote(0.0);
                        BlackVolTermStructure volTS = Utilities.flatVol(vars.today, capletVol, new Actual360());
                        StrikedTypePayoff callPayoff = new AssetOrNothingPayoff(Option.Type.Call,effStrike);
                        BlackScholesMertonProcess stochProcess = new BlackScholesMertonProcess(
                           new Handle<Quote>(fwd),
                           new Handle<YieldTermStructure>(qTS),
                           new Handle<YieldTermStructure>(vars.termStructure),
                           new Handle<BlackVolTermStructure>(volTS));
                        IPricingEngine engine = new AnalyticEuropeanEngine(stochProcess);
                        VanillaOption callOpt = new VanillaOption(callPayoff, exercise);
                        callOpt.setPricingEngine(engine);
                        double callVO = vars.nominal * gearing
                                                * accrualPeriod * callOpt.NPV()
                                                * discount / discountAtFixing
                                                * forward / effFwd;
                        error = Math.Abs(nd1Price - callVO);
                        if (error>vars.blackTolerance)
                           QAssert.Fail("\nDigital Call Option:" +
                           "\nVolatility = " + (capletVol) +
                           "\nStrike = " + (strike) +
                           "\nExercise = " + k+1 + " years" +
                           "\nOption price by Black asset-ot-nothing payoff = " + callVO +
                           "\nOption price by Cox-Rubinstein = " + nd1Price +
                           "\nError " + error );
                        }

                        // Floating Rate Coupon + Put Digital option
                        DigitalCoupon digitalFlooredCoupon = new DigitalCoupon(underlying,nullstrike, Position.Type.Long, 
                           false, nullstrike,strike, Position.Type.Long, false, nullstrike,replication);
                        digitalFlooredCoupon.setPricer(pricer);

                        // Check digital option price vs N(d1) price
                        N_d1 = phi.value(-d1);
                        N_d2 = phi.value(-d2);
                        nd1Price = (gearing * effFwd * N_d1 + spread * N_d2)
                                 * vars.nominal * accrualPeriod * discount;
                        optionPrice = digitalFlooredCoupon.putOptionRate() *
                                       vars.nominal * accrualPeriod * discount;
                        error = Math.Abs(nd1Price - optionPrice);
                        if (error>vars.optionTolerance)
                           QAssert.Fail("\nDigital Put Option:" +
                                       "\nVolatility = " + (capletVol) +
                                       "\nStrike = " + (strike) +
                                       "\nExercise = " + k+1 + " years" +
                                       "\nOption price by replication = "  + optionPrice +
                                       "\nOption price by Cox-Rubinstein = " + nd1Price +
                                       "\nError " + error );

                        // Check digital option price vs N(d1) price using Vanilla Option class
                        if (spread==0.0) 
                        {
                           Exercise exercise = new EuropeanExercise(exerciseDate);
                           double discountAtFixing = vars.termStructure.link.discount(exerciseDate);
                           SimpleQuote fwd = new SimpleQuote(effFwd*discountAtFixing);
                           SimpleQuote qRate = new SimpleQuote(0.0);
                           YieldTermStructure qTS = Utilities.flatRate(vars.today, qRate, new Actual360());
                           //SimpleQuote vol = new SimpleQuote(0.0);
                           BlackVolTermStructure volTS = Utilities.flatVol(vars.today, capletVol, new Actual360());
                           BlackScholesMertonProcess stochProcess = new BlackScholesMertonProcess(
                              new Handle<Quote>(fwd),
                              new Handle<YieldTermStructure>(qTS),
                              new Handle<YieldTermStructure>(vars.termStructure),
                              new Handle<BlackVolTermStructure>(volTS));
                           StrikedTypePayoff putPayoff = new AssetOrNothingPayoff(Option.Type.Put, effStrike);
                           IPricingEngine engine = new AnalyticEuropeanEngine(stochProcess);
                           VanillaOption putOpt = new VanillaOption(putPayoff, exercise);
                           putOpt.setPricingEngine(engine);
                           double putVO  = vars.nominal * gearing
                                                   * accrualPeriod * putOpt.NPV()
                                                   * discount / discountAtFixing
                                                   * forward / effFwd;
                           error = Math.Abs(nd1Price - putVO);
                           if (error>vars.blackTolerance)
                                 QAssert.Fail("\nDigital Put Option:" +
                                 "\nVolatility = " + (capletVol) +
                                 "\nStrike = " + (strike) +
                                 "\nExercise = " + k+1 + " years" +
                                 "\nOption price by Black asset-ot-nothing payoff = " + putVO +
                                 "\nOption price by Cox-Rubinstein = " + nd1Price +
                                 "\nError " + error );
                     }
                  }
               }
            }
         }
      }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
      public void testAssetOrNothingDeepInTheMoney() 
      {
         // Testing European deep in-the-money asset-or-nothing digital coupon
         CommonVars vars = new CommonVars();

         double gearing = 1.0;
         double spread = 0.0;

         double capletVolatility = 0.0001;
         RelinkableHandle<OptionletVolatilityStructure> volatility = new RelinkableHandle<OptionletVolatilityStructure>();
         volatility.linkTo(new ConstantOptionletVolatility(vars.today, vars.calendar, BusinessDayConvention.Following, 
            capletVolatility, new Actual360()));
         double gap = 1e-4;
         DigitalReplication replication = new DigitalReplication(Replication.Type.Central, gap);

         for (int k = 0; k<10; k++) 
         {   
            // Loop on start and end dates
            Date startDate = vars.calendar.advance(vars.settlement,new Period(k+1,TimeUnit.Years));
            Date endDate = vars.calendar.advance(vars.settlement,new Period(k+2,TimeUnit.Years));
            double? nullstrike = null;
            Date paymentDate = endDate;

            FloatingRateCoupon underlying = new IborCoupon(paymentDate, vars.nominal,startDate, endDate,
               vars.fixingDays, vars.index, gearing, spread);

            // Floating Rate Coupon - Deep-in-the-money Call Digital option
            double strike = 0.001;
            DigitalCoupon digitalCappedCoupon = new DigitalCoupon(underlying,strike, Position.Type.Short, false, 
               nullstrike,nullstrike, Position.Type.Short, false, nullstrike,replication);
            IborCouponPricer pricer = new BlackIborCouponPricer(volatility);
            digitalCappedCoupon.setPricer(pricer);

            // Check price vs its target price
            double accrualPeriod = underlying.accrualPeriod();
            double discount = vars.termStructure.link.discount(endDate);

            double targetOptionPrice = underlying.price(vars.termStructure);
            double targetPrice = 0.0;
            double digitalPrice = digitalCappedCoupon.price(vars.termStructure);
            double error = Math.Abs(targetPrice - digitalPrice);
            double tolerance = 1e-08;
            if (error>tolerance)
               QAssert.Fail("\nFloating Coupon - Digital Call Option:" +
                           "\nVolatility = " + (capletVolatility) +
                           "\nStrike = " + (strike) +
                           "\nExercise = " + k+1 + " years" +
                           "\nCoupon Price = "  + digitalPrice +
                           "\nTarget price = " + targetPrice +
                           "\nError = " + error );

            // Check digital option price
            double replicationOptionPrice = digitalCappedCoupon.callOptionRate() *
                                          vars.nominal * accrualPeriod * discount;
            error = Math.Abs(targetOptionPrice - replicationOptionPrice);
            double optionTolerance = 1e-08;
            if (error>optionTolerance)
               QAssert.Fail("\nDigital Call Option:" +
                           "\nVolatility = " + +(capletVolatility) +
                           "\nStrike = " + +(strike) +
                           "\nExercise = " + k+1 + " years" +
                           "\nPrice by replication = " + replicationOptionPrice +
                           "\nTarget price = " + targetOptionPrice +
                           "\nError = " + error);

            // Floating Rate Coupon + Deep-in-the-money Put Digital option
            strike = 0.99;
            DigitalCoupon digitalFlooredCoupon = new DigitalCoupon(underlying,nullstrike, Position.Type.Long, false, 
               nullstrike,strike, Position.Type.Long, false, nullstrike,replication);
            digitalFlooredCoupon.setPricer(pricer);

            // Check price vs its target price
            targetOptionPrice = underlying.price(vars.termStructure);
            targetPrice = underlying.price(vars.termStructure) + targetOptionPrice ;
            digitalPrice = digitalFlooredCoupon.price(vars.termStructure);
            error = Math.Abs(targetPrice - digitalPrice);
            tolerance = 2.5e-06;
            if (error>tolerance)
               QAssert.Fail("\nFloating Coupon + Digital Put Option:" +
                           "\nVolatility = " + (capletVolatility) +
                           "\nStrike = " + (strike) +
                           "\nExercise = " + k+1 + " years" +
                           "\nDigital coupon price = "  + digitalPrice +
                           "\nTarget price = " + targetPrice +
                           "\nError " + error);

            // Check digital option
            replicationOptionPrice = digitalFlooredCoupon.putOptionRate() *
                                    vars.nominal * accrualPeriod * discount;
            error = Math.Abs(targetOptionPrice - replicationOptionPrice);
            optionTolerance = 2.5e-06;
            if (error>optionTolerance)
               QAssert.Fail("\nDigital Put Option:" +
                           "\nVolatility = " + (capletVolatility) +
                           "\nStrike = " + (strike) +
                           "\nExercise = " + k+1 + " years" +
                           "\nPrice by replication = " + replicationOptionPrice +
                           "\nTarget price = " + targetOptionPrice +
                           "\nError " + error);
         }
      }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
      public void testAssetOrNothingDeepOutTheMoney() 
      {
         // Testing European deep out-the-money asset-or-nothing digital coupon
         CommonVars vars = new CommonVars();

         double gearing = 1.0;
         double spread = 0.0;

         double capletVolatility = 0.0001;
         RelinkableHandle<OptionletVolatilityStructure> volatility = new RelinkableHandle<OptionletVolatilityStructure>();
         volatility.linkTo(new ConstantOptionletVolatility(vars.today, vars.calendar, BusinessDayConvention.Following, 
            capletVolatility, new Actual360()));
         double gap = 1e-4;
         DigitalReplication replication = new DigitalReplication(Replication.Type.Central, gap);

         for (int k = 0; k<10; k++) 
         { 
            // loop on start and end dates
            Date startDate = vars.calendar.advance(vars.settlement,new Period(k+1,TimeUnit.Years));
            Date endDate = vars.calendar.advance(vars.settlement,new Period(k+2,TimeUnit.Years));
            double? nullstrike = null;
            Date paymentDate = endDate;

            FloatingRateCoupon underlying =  new IborCoupon(paymentDate, vars.nominal,startDate, endDate,
               vars.fixingDays, vars.index, gearing, spread);

            // Floating Rate Coupon - Deep-out-of-the-money Call Digital option
            double strike = 0.99;
            DigitalCoupon digitalCappedCoupon = new DigitalCoupon(underlying,strike, Position.Type.Short, false, 
               nullstrike,nullstrike, Position.Type.Long, false, nullstrike,replication/*Replication::Central, gap*/);
            IborCouponPricer pricer = new BlackIborCouponPricer(volatility);
            digitalCappedCoupon.setPricer(pricer);

            // Check price vs its target
            double accrualPeriod = underlying.accrualPeriod();
            double discount = vars.termStructure.link.discount(endDate);

            double targetPrice = underlying.price(vars.termStructure);
            double digitalPrice = digitalCappedCoupon.price(vars.termStructure);
            double error = Math.Abs(targetPrice - digitalPrice);
            double tolerance = 1e-10;
            if (error>tolerance)
               QAssert.Fail("\nFloating Coupon - Digital Call Option :" +
                           "\nVolatility = " + (capletVolatility) +
                           "\nStrike = " + (strike) +
                           "\nExercise = " + k+1 + " years" +
                           "\nCoupon price = "  + digitalPrice +
                           "\nTarget price = " + targetPrice +
                           "\nError = " + error );

            // Check digital option price
            double targetOptionPrice = 0.0;
            double replicationOptionPrice = digitalCappedCoupon.callOptionRate() *
                                          vars.nominal * accrualPeriod * discount;
            error = Math.Abs(targetOptionPrice - replicationOptionPrice);
            double optionTolerance = 1e-08;
            if (error>optionTolerance)
               QAssert.Fail("\nDigital Call Option:" +
                           "\nVolatility = " + (capletVolatility) +
                           "\nStrike = " + (strike) +
                           "\nExercise = " + k+1 + " years" +
                           "\nPrice by replication = "  + replicationOptionPrice +
                           "\nTarget price = " + targetOptionPrice +
                           "\nError = " + error );

            // Floating Rate Coupon - Deep-out-of-the-money Put Digital option
            strike = 0.01;
            DigitalCoupon digitalFlooredCoupon = new DigitalCoupon(underlying,nullstrike, Position.Type.Long, false, 
               nullstrike,strike, Position.Type.Long, false, nullstrike,replication);
            digitalFlooredCoupon.setPricer(pricer);

            // Check price vs its target
            targetPrice = underlying.price(vars.termStructure);
            digitalPrice = digitalFlooredCoupon.price(vars.termStructure);
            tolerance = 1e-08;
            error = Math.Abs(targetPrice - digitalPrice);
            if (error>tolerance)
               QAssert.Fail("\nFloating Coupon + Digital Put Coupon:" +
                           "\nVolatility = " + (capletVolatility) +
                           "\nStrike = " + (strike) +
                           "\nExercise = " + k+1 + " years" +
                           "\nCoupon price = "  + digitalPrice +
                           "\nTarget price = " + targetPrice +
                           "\nError = " + error );

            // Check digital option
            targetOptionPrice = 0.0;
            replicationOptionPrice = digitalFlooredCoupon.putOptionRate() *
                                    vars.nominal * accrualPeriod * discount;
            error = Math.Abs(targetOptionPrice - replicationOptionPrice);
            if (error>optionTolerance)
               QAssert.Fail("\nDigital Put Coupon:" +
                           "\nVolatility = " + (capletVolatility) +
                           "\nStrike = " + (strike) +
                           "\nExercise = " + k+1 + " years" +
                           "\nPrice by replication = " + replicationOptionPrice +
                           "\nTarget price = " + targetOptionPrice +
                           "\nError = " + error );
         }
      }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
      public void testCashOrNothing() 
      {
         // Testing European cash-or-nothing digital coupon

         /*  Call Payoff = R Heaviside(aL+b-X)
            Value Call = R N(d2')
            Put Payoff =  R Heaviside(X-aL-b)
            Value Put = R N(-d2')
            where:
            d2' = ln(F/X')/stdDev - 0.5*stdDev;
         */

         CommonVars vars = new CommonVars();

         double[] vols = { 0.05, 0.15, 0.30 };
         double[] strikes = { 0.01, 0.02, 0.03, 0.04, 0.05, 0.06, 0.07 };

         double gearing = 3.0;
         double spread = -0.0002;

         double gap = 1e-08; /* very low, in order to compare digital option value
                                          with black formula result */
         DigitalReplication replication = new DigitalReplication(Replication.Type.Central, gap);
         RelinkableHandle<OptionletVolatilityStructure> vol = new RelinkableHandle<OptionletVolatilityStructure>();

         for (int i = 0; i< vols.Length; i++) 
         {
            double capletVol = vols[i];
            vol.linkTo(new ConstantOptionletVolatility(vars.today,vars.calendar, BusinessDayConvention.Following, 
               capletVol, new Actual360()));
            for (int j = 0; j< strikes.Length; j++) 
            {
               double strike = strikes[j];
               for (int k = 0; k<10; k++) 
               {
                  Date startDate = vars.calendar.advance(vars.settlement,new Period(k+1,TimeUnit.Years));
                  Date endDate = vars.calendar.advance(vars.settlement,new Period(k+2,TimeUnit.Years));
                  double? nullstrike = null;
                  double cashRate = 0.01;

                  Date paymentDate = endDate;
                  FloatingRateCoupon underlying = new IborCoupon(paymentDate, vars.nominal,startDate, endDate,
                     vars.fixingDays, vars.index,gearing, spread);
                  // Floating Rate Coupon - Call Digital option
                  DigitalCoupon digitalCappedCoupon = new DigitalCoupon(underlying,strike, Position.Type.Short, false, 
                     cashRate,nullstrike, Position.Type.Short, false, nullstrike,replication);
                  IborCouponPricer pricer = new BlackIborCouponPricer(vol);
                  digitalCappedCoupon.setPricer(pricer);

                  // Check digital option price vs N(d2) price
                  Date exerciseDate = underlying.fixingDate();
                  double forward = underlying.rate();
                  double effFwd = (forward-spread)/gearing;
                  double effStrike = (strike-spread)/gearing;
                  double accrualPeriod = underlying.accrualPeriod();
                  double discount = vars.termStructure.link.discount(endDate);
                  double stdDev = Math.Sqrt(vol.link.blackVariance(exerciseDate, effStrike));
                  double ITM = Utils.blackFormulaCashItmProbability(Option.Type.Call, effStrike,effFwd, stdDev);
                  double nd2Price = ITM * vars.nominal * accrualPeriod * discount * cashRate;
                  double optionPrice = digitalCappedCoupon.callOptionRate() *
                                    vars.nominal * accrualPeriod * discount;
                  double error = Math.Abs(nd2Price - optionPrice);
                  if (error>vars.optionTolerance)
                     QAssert.Fail("\nDigital Call Option:" +
                                 "\nVolatility = " + (capletVol) +
                                 "\nStrike = " + (strike) +
                                 "\nExercise = " + k+1 + " years" +
                                 "\nPrice by replication = " + optionPrice +
                                 "\nPrice by Reiner-Rubinstein = " + nd2Price +
                                 "\nError = " + error );

                  // Check digital option price vs N(d2) price using Vanilla Option class
                  Exercise exercise = new EuropeanExercise(exerciseDate);
                  double discountAtFixing = vars.termStructure.link.discount(exerciseDate);
                  SimpleQuote fwd = new SimpleQuote(effFwd*discountAtFixing);
                  SimpleQuote qRate = new SimpleQuote(0.0);
                  YieldTermStructure qTS = Utilities.flatRate(vars.today, qRate, new Actual360());
                  //SimpleQuote vol = new SimpleQuote(0.0);
                  BlackVolTermStructure volTS = Utilities.flatVol(vars.today, capletVol, new Actual360());
                  StrikedTypePayoff callPayoff = new CashOrNothingPayoff(Option.Type.Call, effStrike, cashRate);
                  BlackScholesMertonProcess stochProcess = new BlackScholesMertonProcess(
                     new Handle<Quote>(fwd),
                     new Handle<YieldTermStructure>(qTS),
                     new Handle<YieldTermStructure>(vars.termStructure),
                     new Handle<BlackVolTermStructure>(volTS));
                  IPricingEngine engine = new AnalyticEuropeanEngine(stochProcess);
                  VanillaOption callOpt = new VanillaOption(callPayoff, exercise);
                  callOpt.setPricingEngine(engine);
                  double callVO = vars.nominal * accrualPeriod * callOpt.NPV()
                                       * discount / discountAtFixing;
                  error = Math.Abs(nd2Price - callVO);
                  if (error>vars.blackTolerance)
                     QAssert.Fail("\nDigital Call Option:" +
                        "\nVolatility = " + (capletVol) +
                        "\nStrike = " + (strike) +
                        "\nExercise = " + k+1 + " years" +
                        "\nOption price by Black asset-ot-nothing payoff = " + callVO +
                        "\nOption price by Reiner-Rubinstein = " + nd2Price +
                        "\nError " + error );

                  // Floating Rate Coupon + Put Digital option
                  DigitalCoupon digitalFlooredCoupon = new DigitalCoupon(underlying,nullstrike, Position.Type.Long, false, 
                     nullstrike,strike, Position.Type.Long, false, cashRate,replication);
                  digitalFlooredCoupon.setPricer(pricer);


                  // Check digital option price vs N(d2) price
                  ITM = Utils.blackFormulaCashItmProbability(Option.Type.Put,effStrike,effFwd,stdDev);
                  nd2Price = ITM * vars.nominal * accrualPeriod * discount * cashRate;
                  optionPrice = digitalFlooredCoupon.putOptionRate() *
                              vars.nominal * accrualPeriod * discount;
                  error = Math.Abs(nd2Price - optionPrice);
                  if (error>vars.optionTolerance)
                     QAssert.Fail("\nPut Digital Option:" +
                                 "\nVolatility = " + (capletVol) +
                                 "\nStrike = " + (strike) +
                                 "\nExercise = " + k+1 + " years" +
                                 "\nPrice by replication = "  + optionPrice +
                                 "\nPrice by Reiner-Rubinstein = " + nd2Price +
                                 "\nError = " + error );

                  // Check digital option price vs N(d2) price using Vanilla Option class
                  StrikedTypePayoff putPayoff = new CashOrNothingPayoff(Option.Type.Put, effStrike, cashRate);
                  VanillaOption putOpt = new VanillaOption(putPayoff, exercise);
                  putOpt.setPricingEngine(engine);
                  double putVO  = vars.nominal * accrualPeriod * putOpt.NPV()
                                       * discount / discountAtFixing;
                  error = Math.Abs(nd2Price - putVO);
                  if (error>vars.blackTolerance)
                     QAssert.Fail("\nDigital Put Option:" +
                        "\nVolatility = " + (capletVol) +
                        "\nStrike = " + (strike) +
                        "\nExercise = " + k+1 + " years" +
                        "\nOption price by Black asset-ot-nothing payoff = "  + putVO +
                        "\nOption price by Reiner-Rubinstein = " + nd2Price +
                        "\nError " + error );
               }
            }
         }
      }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
      public void testCashOrNothingDeepInTheMoney() 
      {
         // Testing European deep in-the-money cash-or-nothing digital coupon
         CommonVars vars = new CommonVars();

         double gearing = 1.0;
         double spread = 0.0;

         double capletVolatility = 0.0001;
         RelinkableHandle<OptionletVolatilityStructure> volatility = new RelinkableHandle<OptionletVolatilityStructure>();
         volatility.linkTo(new ConstantOptionletVolatility(vars.today, vars.calendar, BusinessDayConvention.Following, 
            capletVolatility, new Actual360()));

         for (int k = 0; k<10; k++) 
         {   
            // Loop on start and end dates
            Date startDate = vars.calendar.advance(vars.settlement,new Period(k+1,TimeUnit.Years));
            Date endDate = vars.calendar.advance(vars.settlement,new Period(k+2,TimeUnit.Years));
            double? nullstrike = null;
            double cashRate = 0.01;
            double gap = 1e-4;
            DigitalReplication replication = new DigitalReplication(Replication.Type.Central, gap);
            Date paymentDate = endDate;

            FloatingRateCoupon underlying = new IborCoupon(paymentDate, vars.nominal,startDate, endDate,
               vars.fixingDays, vars.index,gearing, spread);
            // Floating Rate Coupon - Deep-in-the-money Call Digital option
            double strike = 0.001;
            DigitalCoupon digitalCappedCoupon = new DigitalCoupon(underlying,strike, Position.Type.Short, false, 
               cashRate,nullstrike, Position.Type.Short, false, nullstrike,replication);
            IborCouponPricer pricer = new BlackIborCouponPricer(volatility);
            digitalCappedCoupon.setPricer(pricer);

            // Check price vs its target
            double accrualPeriod = underlying.accrualPeriod();
            double discount = vars.termStructure.link.discount(endDate);

            double targetOptionPrice = cashRate * vars.nominal * accrualPeriod * discount;
            double targetPrice = underlying.price(vars.termStructure) - targetOptionPrice;
            double digitalPrice = digitalCappedCoupon.price(vars.termStructure);

            double error = Math.Abs(targetPrice - digitalPrice);
            double tolerance = 1e-07;
            if (error>tolerance)
               QAssert.Fail("\nFloating Coupon - Digital Call Coupon:" +
                           "\nVolatility = " + (capletVolatility) +
                           "\nStrike = " + (strike) +
                           "\nExercise = " + k+1 + " years" +
                           "\nCoupon price = "  + digitalPrice +
                           "\nTarget price = " + targetPrice +
                           "\nError " + error );

            // Check digital option price
            double replicationOptionPrice = digitalCappedCoupon.callOptionRate() *
                                          vars.nominal * accrualPeriod * discount;
            error = Math.Abs(targetOptionPrice - replicationOptionPrice);
            double optionTolerance = 1e-07;
            if (error>optionTolerance)
               QAssert.Fail("\nDigital Call Option:" +
                           "\nVolatility = " + (capletVolatility) +
                           "\nStrike = " + (strike) +
                           "\nExercise = " + k+1 + " years" +
                           "\nPrice by replication = " + replicationOptionPrice +
                           "\nTarget price = " + targetOptionPrice +
                           "\nError = " + error);

            // Floating Rate Coupon + Deep-in-the-money Put Digital option
            strike = 0.99;
            DigitalCoupon digitalFlooredCoupon = new DigitalCoupon(underlying,nullstrike, Position.Type.Long, false, 
               nullstrike,strike, Position.Type.Long, false, cashRate,replication);
            digitalFlooredCoupon.setPricer(pricer);

            // Check price vs its target
            targetPrice = underlying.price(vars.termStructure) + targetOptionPrice;
            digitalPrice = digitalFlooredCoupon.price(vars.termStructure);
            error = Math.Abs(targetPrice - digitalPrice);
            if (error>tolerance)
               QAssert.Fail("\nFloating Coupon + Digital Put Option:" +
                           "\nVolatility = " + (capletVolatility) +
                           "\nStrike = " + (strike) +
                           "\nExercise = " + k+1 + " years" +
                           "\nCoupon price = "  + digitalPrice +
                           "\nTarget price  = " + targetPrice +
                           "\nError = " + error );

            // Check digital option
            replicationOptionPrice = digitalFlooredCoupon.putOptionRate() *
                                    vars.nominal * accrualPeriod * discount;
            error = Math.Abs(targetOptionPrice - replicationOptionPrice);
            if (error>optionTolerance)
               QAssert.Fail("\nDigital Put Coupon:" +
                           "\nVolatility = " + (capletVolatility) +
                           "\nStrike = " + +(strike) +
                           "\nExercise = " + k+1 + " years" +
                           "\nPrice by replication = " + replicationOptionPrice +
                           "\nTarget price = " + targetOptionPrice +
                           "\nError = " + error );
         }
      }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
      public void testCashOrNothingDeepOutTheMoney() 
      {
         // Testing European deep out-the-money cash-or-nothing digital coupon
         CommonVars vars = new CommonVars();

         double gearing = 1.0;
         double spread = 0.0;

         double capletVolatility = 0.0001;
         RelinkableHandle<OptionletVolatilityStructure> volatility = new RelinkableHandle<OptionletVolatilityStructure>();
         volatility.linkTo(new ConstantOptionletVolatility(vars.today, vars.calendar, BusinessDayConvention.Following, 
            capletVolatility, new Actual360()));

         for (int k = 0; k<10; k++) 
         { 
            // loop on start and end dates
            Date startDate = vars.calendar.advance(vars.settlement,new Period(k+1,TimeUnit.Years));
            Date endDate = vars.calendar.advance(vars.settlement,new Period(k+2,TimeUnit.Years));
            double? nullstrike = null;
            double cashRate = 0.01;
            double gap = 1e-4;
            DigitalReplication replication = new DigitalReplication(Replication.Type.Central, gap);
            Date paymentDate = endDate;

            FloatingRateCoupon underlying = new IborCoupon(paymentDate, vars.nominal,startDate, endDate,
               vars.fixingDays, vars.index,gearing, spread);
            // Deep out-of-the-money Capped Digital Coupon
            double strike = 0.99;
            DigitalCoupon digitalCappedCoupon = new DigitalCoupon(underlying,strike, Position.Type.Short, false, 
               cashRate,nullstrike, Position.Type.Short, false, nullstrike,replication);

            IborCouponPricer pricer = new BlackIborCouponPricer(volatility);
            digitalCappedCoupon.setPricer(pricer);

            // Check price vs its target
            double accrualPeriod = underlying.accrualPeriod();
            double discount = vars.termStructure.link.discount(endDate);

            double targetPrice = underlying.price(vars.termStructure);
            double digitalPrice = digitalCappedCoupon.price(vars.termStructure);
            double error = Math.Abs(targetPrice - digitalPrice);
            double tolerance = 1e-10;
            if (error>tolerance)
               QAssert.Fail("\nFloating Coupon + Digital Call Option:" +
                           "\nVolatility = " + +(capletVolatility) +
                           "\nStrike = " + +(strike) +
                           "\nExercise = " + k+1 + " years" +
                           "\nCoupon price = "  + digitalPrice +
                           "\nTarget price  = " + targetPrice +
                           "\nError = " + error );

            // Check digital option price
            double targetOptionPrice = 0.0;
            double replicationOptionPrice = digitalCappedCoupon.callOptionRate() *
                                          vars.nominal * accrualPeriod * discount;
            error = Math.Abs(targetOptionPrice - replicationOptionPrice);
            double optionTolerance = 1e-10;
            if (error>optionTolerance)
               QAssert.Fail("\nDigital Call Option:" +
                           "\nVolatility = " + +(capletVolatility) +
                           "\nStrike = " + +(strike) +
                           "\nExercise = " + k+1 + " years" +
                           "\nPrice by replication = "  + replicationOptionPrice +
                           "\nTarget price = " + targetOptionPrice +
                           "\nError = " + error );

            // Deep out-of-the-money Floored Digital Coupon
            strike = 0.01;
            DigitalCoupon digitalFlooredCoupon = new DigitalCoupon(underlying,nullstrike, Position.Type.Long, false, 
               nullstrike,strike, Position.Type.Long, false, cashRate,replication);
            digitalFlooredCoupon.setPricer(pricer);

            // Check price vs its target
            targetPrice = underlying.price(vars.termStructure);
            digitalPrice = digitalFlooredCoupon.price(vars.termStructure);
            tolerance = 1e-09;
            error = Math.Abs(targetPrice - digitalPrice);
            if (error>tolerance)
               QAssert.Fail("\nDigital Floored Coupon:" +
                           "\nVolatility = " + +(capletVolatility) +
                           "\nStrike = " + +(strike) +
                           "\nExercise = " + k+1 + " years" +
                           "\nCoupon price = "  + digitalPrice +
                           "\nTarget price  = " + targetPrice +
                           "\nError = " + error );

            // Check digital option
            targetOptionPrice = 0.0;
            replicationOptionPrice = digitalFlooredCoupon.putOptionRate() *
                                    vars.nominal * accrualPeriod * discount;
            error = Math.Abs(targetOptionPrice - replicationOptionPrice);
            if (error>optionTolerance)
               QAssert.Fail("\nDigital Put Option:" +
                           "\nVolatility = " + +(capletVolatility) +
                           "\nStrike = " + +(strike) +
                           "\nExercise = " + k+1 + " years" +
                           "\nPrice by replication " + replicationOptionPrice +
                           "\nTarget price " + targetOptionPrice +
                           "\nError " + error );
         }
      }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
      public void testCallPutParity() 
      {
         // Testing call/put parity for European digital coupon
         CommonVars vars = new CommonVars();

         double[] vols = { 0.05, 0.15, 0.30 };
         double[] strikes = { 0.01, 0.02, 0.03, 0.04, 0.05, 0.06, 0.07 };

         double gearing = 1.0;
         double spread = 0.0;

         double gap = 1e-04;
         DigitalReplication replication = new DigitalReplication(Replication.Type.Central, gap);

         for (int i = 0; i< vols.Length; i++) 
         {
            double capletVolatility = vols[i];
            RelinkableHandle<OptionletVolatilityStructure> volatility = new RelinkableHandle<OptionletVolatilityStructure>();
            volatility.linkTo(new ConstantOptionletVolatility(vars.today, vars.calendar, BusinessDayConvention.Following, 
               capletVolatility, new Actual360()));
            for (int j = 0; j< strikes.Length; j++) 
            {
               double strike = strikes[j];
               for (int k = 0; k<10; k++) 
               {
                  Date startDate = vars.calendar.advance(vars.settlement,new Period(k+1,TimeUnit.Years));
                  Date endDate = vars.calendar.advance(vars.settlement,new Period(k+2,TimeUnit.Years));
                  double? nullstrike = null;

                  Date paymentDate = endDate;

                  FloatingRateCoupon underlying = new IborCoupon(paymentDate, vars.nominal,startDate, endDate,
                     vars.fixingDays, vars.index,gearing, spread);
                  // Cash-or-Nothing
                  double cashRate = 0.01;
                  // Floating Rate Coupon + Call Digital option
                  DigitalCoupon cash_digitalCallCoupon = new DigitalCoupon(underlying,strike, Position.Type.Long, false, 
                     cashRate,nullstrike, Position.Type.Long, false, nullstrike,replication);
                  IborCouponPricer pricer = new BlackIborCouponPricer(volatility);
                  cash_digitalCallCoupon.setPricer(pricer);
                  // Floating Rate Coupon - Put Digital option
                  DigitalCoupon cash_digitalPutCoupon = new DigitalCoupon(underlying,nullstrike, Position.Type.Long, 
                     false, nullstrike,strike, Position.Type.Short, false, cashRate,replication);

                  cash_digitalPutCoupon.setPricer(pricer);
                  double digitalPrice = cash_digitalCallCoupon.price(vars.termStructure) -
                                        cash_digitalPutCoupon.price(vars.termStructure);
                  // Target price
                  double accrualPeriod = underlying.accrualPeriod();
                  double discount = vars.termStructure.link.discount(endDate);
                  double targetPrice = vars.nominal * accrualPeriod *  discount * cashRate;

                  double error = Math.Abs(targetPrice - digitalPrice);
                  double tolerance = 1.0e-08;
                  if (error>tolerance)
                     QAssert.Fail("\nCash-or-nothing:" +
                                 "\nVolatility = " + +(capletVolatility) +
                                 "\nStrike = " + +(strike) +
                                 "\nExercise = " + k+1 + " years" +
                                 "\nPrice = "  + digitalPrice +
                                 "\nTarget Price  = " + targetPrice +
                                 "\nError = " + error );

                  // Asset-or-Nothing
                  // Floating Rate Coupon + Call Digital option
                  DigitalCoupon asset_digitalCallCoupon = new DigitalCoupon(underlying,strike, Position.Type.Long, false, 
                     nullstrike,nullstrike, Position.Type.Long, false, nullstrike,replication);
                  asset_digitalCallCoupon.setPricer(pricer);
                  // Floating Rate Coupon - Put Digital option
                  DigitalCoupon asset_digitalPutCoupon = new DigitalCoupon(underlying,nullstrike, Position.Type.Long, 
                     false, nullstrike,strike, Position.Type.Short, false, nullstrike,replication);
                  asset_digitalPutCoupon.setPricer(pricer);
                  digitalPrice = asset_digitalCallCoupon.price(vars.termStructure) -
                                 asset_digitalPutCoupon.price(vars.termStructure);
                  // Target price
                  targetPrice = vars.nominal *  accrualPeriod *  discount * underlying.rate();
                  error = Math.Abs(targetPrice - digitalPrice);
                  tolerance = 1.0e-07;
                  if (error>tolerance)
                     QAssert.Fail("\nAsset-or-nothing:" +
                                 "\nVolatility = " + (capletVolatility) +
                                 "\nStrike = " + (strike) +
                                 "\nExercise = " + k+1 + " years" +
                                 "\nPrice = "  + digitalPrice +
                                 "\nTarget Price  = " + targetPrice +
                                 "\nError = " + error );
               }
            }
         }
      }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
      public void testReplicationType() 
      {
         // Testing replication type for European digital coupon
         CommonVars vars = new CommonVars();

         double[] vols = { 0.05, 0.15, 0.30 };
         double[] strikes = { 0.01, 0.02, 0.03, 0.04, 0.05, 0.06, 0.07 };

         double gearing = 1.0;
         double spread = 0.0;

         double gap = 1e-04;
         DigitalReplication subReplication = new DigitalReplication(Replication.Type.Sub, gap);
         DigitalReplication centralReplication = new DigitalReplication(Replication.Type.Central, gap);
         DigitalReplication superReplication = new DigitalReplication(Replication.Type.Super, gap);

         for (int i = 0; i< vols.Length; i++) 
         {
            double capletVolatility = vols[i];
            RelinkableHandle<OptionletVolatilityStructure> volatility = new RelinkableHandle<OptionletVolatilityStructure>();
            volatility.linkTo(new ConstantOptionletVolatility(vars.today, vars.calendar, BusinessDayConvention.Following, 
               capletVolatility, new Actual360()));
            for (int j = 0; j< strikes.Length; j++) 
            {
               double strike = strikes[j];
               for (int k = 0; k<10; k++) 
               {
                  Date startDate = vars.calendar.advance(vars.settlement,new Period(k+1,TimeUnit.Years));
                  Date endDate = vars.calendar.advance(vars.settlement,new Period(k+2,TimeUnit.Years));
                  double? nullstrike = null;

                  Date paymentDate = endDate;

                  FloatingRateCoupon underlying = new IborCoupon(paymentDate, vars.nominal,startDate, endDate,
                     vars.fixingDays, vars.index,gearing, spread);
                  // Cash-or-Nothing
                  double cashRate = 0.005;
                  // Floating Rate Coupon + Call Digital option
                  DigitalCoupon sub_cash_longDigitalCallCoupon = new DigitalCoupon(underlying,strike, Position.Type.Long, 
                     false, cashRate,nullstrike, Position.Type.Long, false, nullstrike,subReplication);
                  DigitalCoupon central_cash_longDigitalCallCoupon = new DigitalCoupon(underlying,strike, 
                     Position.Type.Long, false, cashRate,nullstrike, Position.Type.Long, false, nullstrike,
                     centralReplication);
                  DigitalCoupon over_cash_longDigitalCallCoupon = new DigitalCoupon(underlying,strike, Position.Type.Long, 
                     false, cashRate,nullstrike, Position.Type.Long, false, nullstrike,superReplication);
                  IborCouponPricer pricer = new BlackIborCouponPricer(volatility);
                  sub_cash_longDigitalCallCoupon.setPricer(pricer);
                  central_cash_longDigitalCallCoupon.setPricer(pricer);
                  over_cash_longDigitalCallCoupon.setPricer(pricer);
                  double sub_digitalPrice = sub_cash_longDigitalCallCoupon.price(vars.termStructure);
                  double central_digitalPrice = central_cash_longDigitalCallCoupon.price(vars.termStructure);
                  double over_digitalPrice = over_cash_longDigitalCallCoupon.price(vars.termStructure);
                  double tolerance = 1.0e-09;
                  if ( ( (sub_digitalPrice > central_digitalPrice) &&
                        Math.Abs(central_digitalPrice - sub_digitalPrice)>tolerance ) ||
                     ( (central_digitalPrice>over_digitalPrice)  &&
                        Math.Abs(central_digitalPrice - over_digitalPrice)>tolerance ) )  
                  {
                     QAssert.Fail("\nCash-or-nothing: Floating Rate Coupon + Call Digital option" +
                                 "\nVolatility = " + +(capletVolatility) +
                                 "\nStrike = " + +(strike) +
                                 "\nExercise = " + k+1 + " years" +
                                 "\nSub-Replication Price = "  + sub_digitalPrice +
                                 "\nCentral-Replication Price = "  + central_digitalPrice +
                                 "\nOver-Replication Price = "  + over_digitalPrice);
                  }

                  // Floating Rate Coupon - Call Digital option
                  DigitalCoupon sub_cash_shortDigitalCallCoupon = new DigitalCoupon(underlying,strike, Position.Type.Short,
                     false, cashRate,nullstrike, Position.Type.Long, false, nullstrike,subReplication);
                  DigitalCoupon central_cash_shortDigitalCallCoupon = new DigitalCoupon(underlying,strike, 
                     Position.Type.Short, false, cashRate,nullstrike, Position.Type.Long, false, nullstrike,
                     centralReplication);
                  DigitalCoupon over_cash_shortDigitalCallCoupon = new DigitalCoupon(underlying,strike, 
                     Position.Type.Short, false, cashRate,nullstrike, Position.Type.Long, false, nullstrike,
                     superReplication);
                  sub_cash_shortDigitalCallCoupon.setPricer(pricer);
                  central_cash_shortDigitalCallCoupon.setPricer(pricer);
                  over_cash_shortDigitalCallCoupon.setPricer(pricer);
                  sub_digitalPrice = sub_cash_shortDigitalCallCoupon.price(vars.termStructure);
                  central_digitalPrice = central_cash_shortDigitalCallCoupon.price(vars.termStructure);
                  over_digitalPrice = over_cash_shortDigitalCallCoupon.price(vars.termStructure);
                  if ( ( (sub_digitalPrice > central_digitalPrice) &&
                        Math.Abs(central_digitalPrice - sub_digitalPrice)>tolerance ) ||
                     ( (central_digitalPrice>over_digitalPrice)  &&
                        Math.Abs(central_digitalPrice - over_digitalPrice)>tolerance ) )
                  {
                     QAssert.Fail("\nCash-or-nothing: Floating Rate Coupon - Call Digital option" +
                                 "\nVolatility = " + +(capletVolatility) +
                                 "\nStrike = " + +(strike) +
                                 "\nExercise = " + k+1 + " years" +
                                 "\nSub-Replication Price = "  + sub_digitalPrice +
                                 "\nCentral-Replication Price = "  + central_digitalPrice +
                                 "\nOver-Replication Price = "  + over_digitalPrice);
                  }
                  // Floating Rate Coupon + Put Digital option
                  DigitalCoupon sub_cash_longDigitalPutCoupon = new DigitalCoupon(underlying,nullstrike, 
                     Position.Type.Long, false, nullstrike,strike, Position.Type.Long, false, cashRate,subReplication);
                  DigitalCoupon central_cash_longDigitalPutCoupon = new DigitalCoupon(underlying,nullstrike, 
                     Position.Type.Long, false, nullstrike,strike, Position.Type.Long, false, cashRate,centralReplication);
                  DigitalCoupon over_cash_longDigitalPutCoupon= new DigitalCoupon(underlying,nullstrike, 
                     Position.Type.Long, false, nullstrike,strike, Position.Type.Long, false, cashRate,superReplication);
                  sub_cash_longDigitalPutCoupon.setPricer(pricer);
                  central_cash_longDigitalPutCoupon.setPricer(pricer);
                  over_cash_longDigitalPutCoupon.setPricer(pricer);
                  sub_digitalPrice = sub_cash_longDigitalPutCoupon.price(vars.termStructure);
                  central_digitalPrice = central_cash_longDigitalPutCoupon.price(vars.termStructure);
                  over_digitalPrice = over_cash_longDigitalPutCoupon.price(vars.termStructure);
                  if ( ( (sub_digitalPrice > central_digitalPrice) &&
                        Math.Abs(central_digitalPrice - sub_digitalPrice)>tolerance ) ||
                     ( (central_digitalPrice>over_digitalPrice)  &&
                        Math.Abs(central_digitalPrice - over_digitalPrice)>tolerance ) )
                  {
                     QAssert.Fail("\nCash-or-nothing: Floating Rate Coupon + Put Digital option" +
                                 "\nVolatility = " + (capletVolatility) +
                                 "\nStrike = " + (strike) +
                                 "\nExercise = " + k+1 + " years" +
                                 "\nSub-Replication Price = "  + sub_digitalPrice +
                                 "\nCentral-Replication Price = "  + central_digitalPrice +
                                 "\nOver-Replication Price = "  + over_digitalPrice);
                  }

                  // Floating Rate Coupon - Put Digital option
                  DigitalCoupon sub_cash_shortDigitalPutCoupon = new DigitalCoupon(underlying,nullstrike, 
                     Position.Type.Long, false, nullstrike,strike, Position.Type.Short, false, cashRate,subReplication);
                  DigitalCoupon central_cash_shortDigitalPutCoupon= new DigitalCoupon(underlying,nullstrike, 
                     Position.Type.Long, false, nullstrike,strike, Position.Type.Short, false, cashRate,centralReplication);
                  DigitalCoupon over_cash_shortDigitalPutCoupon = new DigitalCoupon(underlying,nullstrike, 
                     Position.Type.Long, false, nullstrike,strike, Position.Type.Short, false, cashRate,superReplication);
                  sub_cash_shortDigitalPutCoupon.setPricer(pricer);
                  central_cash_shortDigitalPutCoupon.setPricer(pricer);
                  over_cash_shortDigitalPutCoupon.setPricer(pricer);
                  sub_digitalPrice = sub_cash_shortDigitalPutCoupon.price(vars.termStructure);
                  central_digitalPrice = central_cash_shortDigitalPutCoupon.price(vars.termStructure);
                  over_digitalPrice = over_cash_shortDigitalPutCoupon.price(vars.termStructure);
                  if ( ( (sub_digitalPrice > central_digitalPrice) &&
                        Math.Abs(central_digitalPrice - sub_digitalPrice)>tolerance ) ||
                     ( (central_digitalPrice>over_digitalPrice)  &&
                        Math.Abs(central_digitalPrice - over_digitalPrice)>tolerance ) )
                  {
                     QAssert.Fail("\nCash-or-nothing: Floating Rate Coupon + Call Digital option" +
                                 "\nVolatility = " + (capletVolatility) +
                                 "\nStrike = " + (strike) +
                                 "\nExercise = " + k+1 + " years" +
                                 "\nSub-Replication Price = "  + sub_digitalPrice +
                                 "\nCentral-Replication Price = "  + central_digitalPrice +
                                 "\nOver-Replication Price = "  + over_digitalPrice);
                  }
               }
            }
         }
      }
   }
}
