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
using QLNet;

namespace CVAIRS
{
   class CVAIRS
   {
      static void Main( string[] args )
      {
         try 
         {

            DateTime timer = DateTime.Now;
            Calendar calendar = new TARGET();
            Date todaysDate = new Date(10,Month.March, 2004);
            // must be a business day
            todaysDate = calendar.adjust(todaysDate);

            Settings.setEvaluationDate(todaysDate);

            IborIndex yieldIndx = new Euribor3M();
            int[] tenorsSwapMkt = {5,10,15,20,25,30};

            // rates ignoring counterparty risk:
            double[] ratesSwapmkt = {.03249,.04074,.04463,.04675,.04775,.04811};

            List<RateHelper> swapHelpers = new List<RateHelper>();
            for (int i = 0; i < tenorsSwapMkt.Length; i++)
               swapHelpers.Add(new SwapRateHelper(new Handle<Quote>(new SimpleQuote(ratesSwapmkt[i])),
                  new Period(tenorsSwapMkt[i],TimeUnit.Years),
                  new TARGET(),
                  Frequency.Quarterly, 
                  BusinessDayConvention.ModifiedFollowing, 
                  new ActualActual(ActualActual.Convention.ISDA),
                  yieldIndx));

            YieldTermStructure swapTS = new PiecewiseYieldCurve<Discount, LogLinear>(2, new TARGET(), swapHelpers, 
               new ActualActual(ActualActual.Convention.ISDA));
            swapTS.enableExtrapolation();

            IPricingEngine riskFreeEngine = new DiscountingSwapEngine(new Handle<YieldTermStructure>(swapTS));

            List<Handle<DefaultProbabilityTermStructure>> defaultIntensityTS = 
               new List<Handle<DefaultProbabilityTermStructure>>();

            int[] defaultTenors = {0,12,36,60,84,120,180,240,300,360}; // months
            // Three risk levels:
            double[] intensitiesLow = {0.0036,0.0036,0.0065,0.0099,0.0111,0.0177,0.0177,0.0177,0.0177,0.0177,0.0177};
            double[] intensitiesMedium = {0.0202,0.0202,0.0231,0.0266,0.0278,0.0349,0.0349,0.0349,0.0349,0.0349,0.0349};
            double[] intensitiesHigh = {0.0534,0.0534,0.0564,0.06,0.0614,0.0696,0.0696,0.0696,0.0696,0.0696,0.0696};
            // Recovery rates:
            double ctptyRRLow = 0.4, ctptyRRMedium = 0.35, ctptyRRHigh = 0.3;

            List<Date> defaultTSDates = new List<Date>();
            List<double> intesitiesVLow = new List<double>(), 
                         intesitiesVMedium = new List<double>(), 
                         intesitiesVHigh = new List<double>();

            for (int i = 0; i < defaultTenors.Length; i++)
            {
               defaultTSDates.Add(new TARGET().advance(todaysDate,new Period(defaultTenors[i], TimeUnit.Months)));
               intesitiesVLow.Add(intensitiesLow[i]);
               intesitiesVMedium.Add(intensitiesMedium[i]);
               intesitiesVHigh.Add(intensitiesHigh[i]);
            }

            defaultIntensityTS.Add(new Handle<DefaultProbabilityTermStructure>(
                  new InterpolatedHazardRateCurve<BackwardFlat>(
                     defaultTSDates,
                     intesitiesVLow,
                     new Actual360(),
                     new TARGET())));
            defaultIntensityTS.Add(new Handle<DefaultProbabilityTermStructure>(
                  new InterpolatedHazardRateCurve<BackwardFlat>(
                     defaultTSDates,
                     intesitiesVMedium,
                     new Actual360(),
                     new TARGET())));
            defaultIntensityTS.Add(new Handle<DefaultProbabilityTermStructure>(
                  new InterpolatedHazardRateCurve<BackwardFlat>(
                     defaultTSDates,
                     intesitiesVHigh,
                     new Actual360(),
                     new TARGET())));

            double blackVol = 0.15;
            IPricingEngine ctptySwapCvaLow = new CounterpartyAdjSwapEngine(new Handle<YieldTermStructure>(swapTS),
                  blackVol,defaultIntensityTS[0],ctptyRRLow);

            IPricingEngine ctptySwapCvaMedium = new CounterpartyAdjSwapEngine(new Handle<YieldTermStructure>(swapTS),
                  blackVol,defaultIntensityTS[1],ctptyRRMedium);

            IPricingEngine ctptySwapCvaHigh = new CounterpartyAdjSwapEngine(new Handle<YieldTermStructure>(swapTS),
                  blackVol,defaultIntensityTS[2],ctptyRRHigh);

            defaultIntensityTS[0].link.enableExtrapolation();
            defaultIntensityTS[1].link.enableExtrapolation();
            defaultIntensityTS[2].link.enableExtrapolation();

            // SWAP RISKY REPRICE----------------------------------------------

            // fixed leg
            Frequency fixedLegFrequency = Frequency.Quarterly;
            BusinessDayConvention fixedLegConvention = BusinessDayConvention.ModifiedFollowing;
            DayCounter fixedLegDayCounter = new ActualActual(ActualActual.Convention.ISDA);
            DayCounter floatingLegDayCounter = new ActualActual(ActualActual.Convention.ISDA);

            VanillaSwap.Type swapType =
               //VanillaSwap::Receiver ;
               VanillaSwap.Type.Payer;
            IborIndex yieldIndxS = new Euribor3M(new Handle<YieldTermStructure>(swapTS));
            List<VanillaSwap> riskySwaps = new List<VanillaSwap>();
            for (int i = 0; i < tenorsSwapMkt.Length; i++)
               riskySwaps.Add(new MakeVanillaSwap(new Period(tenorsSwapMkt[i],TimeUnit.Years),
                  yieldIndxS,
                  ratesSwapmkt[i],
                  new Period(0,TimeUnit.Days))
                  .withSettlementDays(2)
                  .withFixedLegDayCount(fixedLegDayCounter)
                  .withFixedLegTenor(new Period(fixedLegFrequency))
                  .withFixedLegConvention(fixedLegConvention)
                  .withFixedLegTerminationDateConvention(fixedLegConvention)
                  .withFixedLegCalendar(calendar)
                  .withFloatingLegCalendar(calendar)
                  .withNominal(100.0)
                  .withType(swapType).value());

            Console.WriteLine("-- Correction in the contract fix rate in bp --" );
            /* The paper plots correction to be substracted, here is printed
               with its sign 
            */
            for (int i = 0; i < riskySwaps.Count; i++)
            {
               riskySwaps[i].setPricingEngine(riskFreeEngine);
               // should recover the input here:
               double nonRiskyFair = riskySwaps[i].fairRate();
               Console.Write( (tenorsSwapMkt[i]).ToString( "0" ).PadLeft( 6 ) );
               Console.Write( " | " + nonRiskyFair.ToString( "P3" ).PadLeft( 6 ) );
               // Low Risk:
               riskySwaps[i].setPricingEngine(ctptySwapCvaLow);
               Console.Write( " | " + ( 10000.0 * ( riskySwaps[i].fairRate() - nonRiskyFair ) ).ToString( "#0.00" ).PadLeft( 6 ) );
               //cout << " | " << setw(6) << riskySwaps[i].NPV() ;

               // Medium Risk:
               riskySwaps[i].setPricingEngine(ctptySwapCvaMedium);
               Console.Write( " | " + ( 10000.0 * ( riskySwaps[i].fairRate() - nonRiskyFair ) ).ToString( "#0.00" ).PadLeft( 6 ) );
               //cout << " | " << setw(6) << riskySwaps[i].NPV() ;

               riskySwaps[i].setPricingEngine(ctptySwapCvaHigh);
               Console.Write( " | " + ( 10000.0 * ( riskySwaps[i].fairRate() - nonRiskyFair ) ).ToString( "#0.00" ).PadLeft( 6 ) );
               //cout << " | " << setw(6) << riskySwaps[i].NPV() ;

               Console.WriteLine();
            }

           Console.WriteLine();

            Console.WriteLine(" \nRun completed in {0}", DateTime.Now - timer);
            Console.WriteLine();

            Console.Write("Press any key to continue ...");
            Console.ReadKey();
    
         } 
         catch (Exception e)
         {
            Console.Write(e.Message);
            Console.Write( "Press any key to continue ..." );
            Console.ReadKey();
         } 
      }
   }
}
