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
using System.Linq;
using System.Text;

namespace QLNet
{
   /*! Bilateral (CVA and DVA) default adjusted vanilla swap pricing
    engine. Collateral is not considered. No wrong way risk is 
    considered (rates and counterparty default are uncorrelated).
    Based on:
    Sorensen,  E.H.  and  Bollier,  T.F.,  Pricing  swap  default 
    risk. Financial Analysts Journal, 1994, 50, 23–33
    Also see sect. II-5 in: Risk Neutral Pricing of Counterparty Risk
    D. Brigo, M. Masetti, 2004
    or in sections 3 and 4 of "A Formula for Interest Rate Swaps 
      Valuation under Counterparty Risk in presence of Netting Agreements"
    D. Brigo and M. Masetti; May 4, 2005

    to do: Compute fair rate through iteration instead of the 
    current approximation .
    to do: write Issuer based constructors (event type)
    to do: Check consistency between option engine discount and the one given
   */
   public class CounterpartyAdjSwapEngine : VanillaSwap.Engine
   {
      //! \name Constructors
      //@{
      //! 
      /*! Creates the engine from an arbitrary swaption engine.
        If the investor default model is not given a default 
        free one is assumed.
        @param discountCurve Used in pricing.
        @param swaptionEngine Determines the volatility and thus the 
        exposure model.
        @param ctptyDTS Counterparty default curve.
        @param ctptyRecoveryRate Counterparty recovey rate.
        @param invstDTS Investor (swap holder) default curve.
        @param invstRecoveryRate Investor recovery rate.
       */
      public CounterpartyAdjSwapEngine(Handle<YieldTermStructure> discountCurve,
         Handle<IPricingEngine> swaptionEngine,Handle<DefaultProbabilityTermStructure> ctptyDTS,double ctptyRecoveryRate, 
         Handle<DefaultProbabilityTermStructure> invstDTS=null,double invstRecoveryRate = 0.999)
      {
         baseSwapEngine_ = new Handle<IPricingEngine>(new DiscountingSwapEngine(discountCurve));
         swaptionletEngine_ = swaptionEngine;
         discountCurve_ = discountCurve;
         defaultTS_ = ctptyDTS;
         ctptyRecoveryRate_ = ctptyRecoveryRate;
         invstDTS_ = invstDTS ?? new Handle<DefaultProbabilityTermStructure>(
            new FlatHazardRate(0, ctptyDTS.link.calendar(), 1.0E-12, ctptyDTS.link.dayCounter()));
         invstRecoveryRate_ = invstRecoveryRate;

         discountCurve.registerWith(update) ;
         ctptyDTS.registerWith( update ) ;
         invstDTS_.registerWith( update ) ;
         swaptionEngine.registerWith( update ) ;
      }

      /*! Creates an engine with a black volatility model for the 
        exposure.
        If the investor default model is not given a default 
        free one is assumed.
        @param discountCurve Used in pricing.
        @param blackVol Black volatility used in the exposure model.
        @param ctptyDTS Counterparty default curve.
        @param ctptyRecoveryRate Counterparty recovey rate.
        @param invstDTS Investor (swap holder) default curve.
        @param invstRecoveryRate Investor recovery rate.
       */
      public CounterpartyAdjSwapEngine(Handle<YieldTermStructure> discountCurve,double blackVol,
         Handle<DefaultProbabilityTermStructure> ctptyDTS,double ctptyRecoveryRate, 
         Handle<DefaultProbabilityTermStructure> invstDTS=null,double invstRecoveryRate = 0.999)
      {
         baseSwapEngine_ = new Handle<IPricingEngine>(new DiscountingSwapEngine(discountCurve));
         swaptionletEngine_ = new Handle<IPricingEngine>(new BlackSwaptionEngine(discountCurve, blackVol));
         discountCurve_ = discountCurve;
         defaultTS_ = ctptyDTS;
         ctptyRecoveryRate_ = ctptyRecoveryRate;
         invstDTS_ = invstDTS ?? new Handle<DefaultProbabilityTermStructure>(
            new FlatHazardRate(0, ctptyDTS.link.calendar(), 1.0e-12, ctptyDTS.link.dayCounter()));
         invstRecoveryRate_ = invstRecoveryRate;

         discountCurve.registerWith(update) ;
         ctptyDTS.registerWith(update) ;
         invstDTS_.registerWith(update) ;
      }

      /*! Creates an engine with a black volatility model for the 
        exposure. The volatility is given as a quote.
        If the investor default model is not given a default 
        free one is assumed.
        @param discountCurve Used in pricing.
        @param blackVol Black volatility used in the exposure model.
        @param ctptyDTS Counterparty default curve.
        @param ctptyRecoveryRate Counterparty recovey rate.
        @param invstDTS Investor (swap holder) default curve.
        @param invstRecoveryRate Investor recovery rate.
      */
      public CounterpartyAdjSwapEngine(Handle<YieldTermStructure> discountCurve,Handle<Quote> blackVol,
         Handle<DefaultProbabilityTermStructure> ctptyDTS,double ctptyRecoveryRate, 
         Handle<DefaultProbabilityTermStructure> invstDTS=null,double invstRecoveryRate = 0.999)
      {
         baseSwapEngine_ = new Handle<IPricingEngine>(new DiscountingSwapEngine(discountCurve));
         swaptionletEngine_ = new Handle<IPricingEngine>(new BlackSwaptionEngine(discountCurve, blackVol));
         discountCurve_ = discountCurve;
         defaultTS_ = ctptyDTS;
         ctptyRecoveryRate_ = ctptyRecoveryRate;
         invstDTS_ = invstDTS ?? new Handle<DefaultProbabilityTermStructure>(
            new FlatHazardRate(0, ctptyDTS.link.calendar(), 1.0e-12, ctptyDTS.link.dayCounter()));
         invstRecoveryRate_ = invstRecoveryRate;

         discountCurve.registerWith(update) ;
         ctptyDTS.registerWith(update) ;
         invstDTS_.registerWith(update) ;
         blackVol.registerWith(update) ;
      }
      //@}
      public override void calculate()
      {
         /* both DTS, YTS ref dates and pricing date consistency 
         checks? settlement... */
         Utils.QL_REQUIRE(!discountCurve_.empty(),()=> "no discount term structure set");
         Utils.QL_REQUIRE(!defaultTS_.empty(),()=> "no ctpty default term structure set");
         Utils.QL_REQUIRE(!swaptionletEngine_.empty(),()=> "no swap option engine set");

         Date priceDate = defaultTS_.link.referenceDate();

         double cumOptVal = 0.0,cumPutVal = 0.0;
         // Vanilla swap so 0 leg is floater

         int index = 0;
         Date nextFD = arguments_.fixedPayDates[index];
         Date swapletStart = priceDate;
         while (nextFD < priceDate)
         {
            index++;
            nextFD = arguments_.fixedPayDates[index];
         }


         // Compute fair spread for strike value:
         // copy args into the non risky engine
         Swap.Arguments noCVAArgs = baseSwapEngine_.link.getArguments() as Swap.Arguments;

         noCVAArgs.legs = this.arguments_.legs;
         noCVAArgs.payer = this.arguments_.payer;

         baseSwapEngine_.link.calculate();

         double baseSwapRate = ((FixedRateCoupon)arguments_.legs[0][0]).rate();

         Swap.Results vSResults = baseSwapEngine_.link.getResults() as Swap.Results;

         double? baseSwapFairRate = -baseSwapRate*vSResults.legNPV[1]/ vSResults.legNPV[0];
         double? baseSwapNPV = vSResults.value;

         VanillaSwap.Type reversedType = arguments_.type == VanillaSwap.Type.Payer
            ? VanillaSwap.Type.Receiver
            : VanillaSwap.Type.Payer;

         // Swaplet options summatory:
         while (nextFD != arguments_.fixedPayDates.Last())
         {
            // iFD coupon not fixed, create swaptionlet:
            IborIndex swapIndex = ((FloatingRateCoupon)arguments_.legs[1][0]).index() as IborIndex;

            // Alternatively one could cap this period to, say, 1M 
            // Period swapPeriod = boost::dynamic_pointer_cast<FloatingRateCoupon>(
            //   arguments_.legs[1][0])->index()->tenor();

            Period baseSwapsTenor = new Period(arguments_.fixedPayDates.Last().serialNumber()
             - swapletStart.serialNumber(),TimeUnit.Days);
            VanillaSwap swaplet = new MakeVanillaSwap(baseSwapsTenor, swapIndex, baseSwapFairRate)
               .withType(arguments_.type)
               .withNominal(arguments_.nominal)
               .withEffectiveDate(swapletStart)
               .withTerminationDate(arguments_.fixedPayDates.Last()).value();

            VanillaSwap revSwaplet = new MakeVanillaSwap(baseSwapsTenor,swapIndex,baseSwapFairRate)
               .withType(reversedType)
               .withNominal(arguments_.nominal)
               .withEffectiveDate(swapletStart)
               .withTerminationDate(arguments_.fixedPayDates.Last()).value();

            Swaption swaptionlet = new Swaption(swaplet,new EuropeanExercise(swapletStart));
            Swaption putSwaplet = new Swaption(revSwaplet,new EuropeanExercise (swapletStart));
            swaptionlet.setPricingEngine(swaptionletEngine_.currentLink());
            putSwaplet.setPricingEngine(swaptionletEngine_.currentLink());

            // atm underlying swap means that the value of put = value
            // call so this double pricing is not needed
            cumOptVal += swaptionlet.NPV()*defaultTS_.link.defaultProbability(
               swapletStart, nextFD);
            cumPutVal += putSwaplet.NPV()*invstDTS_.link.defaultProbability(swapletStart, nextFD);

            swapletStart = nextFD;
            index++;
            nextFD = arguments_.fixedPayDates[index];
         }

         results_.value = baseSwapNPV - (1.0-ctptyRecoveryRate_)*cumOptVal+ (1.0-invstRecoveryRate_)*cumPutVal;
         results_.fairRate = -baseSwapRate*(vSResults.legNPV[1] - (1.0 - ctptyRecoveryRate_)*cumOptVal +
                             (1.0-invstRecoveryRate_)*cumPutVal )/vSResults.legNPV[0];

      }

      private Handle<IPricingEngine> baseSwapEngine_;
      private Handle<IPricingEngine> swaptionletEngine_;
      private Handle<YieldTermStructure> discountCurve_;
      private Handle<DefaultProbabilityTermStructure> defaultTS_;
      private double ctptyRecoveryRate_;
      private Handle<DefaultProbabilityTermStructure> invstDTS_;
      private double invstRecoveryRate_;

   }
}
