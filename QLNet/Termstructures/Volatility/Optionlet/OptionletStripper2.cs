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

using System.Collections.Generic;

namespace QLNet
{
   /*! Helper class to extend an OptionletStripper1 object stripping
        additional optionlet (i.e. caplet/floorlet) volatilities (a.k.a.
        forward-forward volatilities) from the (cap/floor) At-The-Money
        term volatilities of a CapFloorTermVolCurve.
   */
   public class OptionletStripper2 : OptionletStripper
   {
       
      public OptionletStripper2(OptionletStripper1 optionletStripper1, Handle<CapFloorTermVolCurve> atmCapFloorTermVolCurve)
         :base(optionletStripper1.termVolSurface(),
                        optionletStripper1.iborIndex(),
                        new Handle<YieldTermStructure>(),
                        optionletStripper1.volatilityType(),
                        optionletStripper1.displacement())
      {
         stripper1_ = optionletStripper1;
         atmCapFloorTermVolCurve_ = atmCapFloorTermVolCurve;
         dc_ = stripper1_.termVolSurface().dayCounter();
         nOptionExpiries_ = atmCapFloorTermVolCurve.link.optionTenors().Count;
         atmCapFloorStrikes_ = new InitializedList<double>(nOptionExpiries_,0.0);
         atmCapFloorPrices_ = new InitializedList<double>(nOptionExpiries_,0.0);
         spreadsVolImplied_ = new InitializedList<double>(nOptionExpiries_,0.0);
         caps_ = new List<CapFloor>();
         maxEvaluations_ = 10000;
         accuracy_ = 1E-6;

         stripper1_.registerWith(update);
         atmCapFloorTermVolCurve_.registerWith(update); 

         Utils.QL_REQUIRE( dc_ == atmCapFloorTermVolCurve.link.dayCounter(),()=> "different day counters provided" );
      }

      public List<double> atmCapFloorStrikes()
      {
         calculate();
         return atmCapFloorStrikes_;
      }
      public List<double> atmCapFloorPrices()
      {
         calculate();
         return atmCapFloorPrices_;
      }

      public List<double> spreadsVol()
      {
         calculate();
         return spreadsVolImplied_;
      }

      //! \name LazyObject interface
      //@{
      protected override void performCalculations()
      {
         //// optionletStripper data
         optionletDates_ = new List<Date>(stripper1_.optionletFixingDates());
         optionletPaymentDates_ = new List<Date>(stripper1_.optionletPaymentDates());
         optionletAccrualPeriods_ = new List<double>(stripper1_.optionletAccrualPeriods());
         optionletTimes_ = new List<double>(stripper1_.optionletFixingTimes());
         atmOptionletRate_ = new List<double>(stripper1_.atmOptionletRates());
         for (int i=0; i<optionletTimes_.Count; ++i) 
         {
            optionletStrikes_[i] = new List<double>(stripper1_.optionletStrikes(i));
            optionletVolatilities_[i] = new List<double>(stripper1_.optionletVolatilities(i));
         }

         // atmCapFloorTermVolCurve data
         List<Period> optionExpiriesTenors = new List<Period>(atmCapFloorTermVolCurve_.link.optionTenors());
         List<double> optionExpiriesTimes = new List<double>(atmCapFloorTermVolCurve_.link.optionTimes());

         for (int j=0; j<nOptionExpiries_; ++j) 
         {
            double atmOptionVol = atmCapFloorTermVolCurve_.link.volatility(optionExpiriesTimes[j], 33.3333); // dummy strike
            BlackCapFloorEngine engine = new BlackCapFloorEngine(iborIndex_.forwardingTermStructure(),atmOptionVol, dc_);
            CapFloor test = new MakeCapFloor(CapFloorType.Cap, optionExpiriesTenors[j], iborIndex_, null,
               new Period(0, TimeUnit.Days)).withPricingEngine(engine).value();
            caps_.Add(test);
            atmCapFloorStrikes_[j] = caps_[j].atmRate(iborIndex_.forwardingTermStructure());
            atmCapFloorPrices_[j] = caps_[j].NPV();
         }

         spreadsVolImplied_ = spreadsVolImplied();

         StrippedOptionletAdapter adapter = new StrippedOptionletAdapter(stripper1_);

         double unadjustedVol, adjustedVol;
         for (int j=0; j<nOptionExpiries_; ++j) 
         {
            for (int i=0; i<optionletVolatilities_.Count; ++i) 
            {
               if (i<=caps_[j].floatingLeg().Count) 
               {
                  unadjustedVol = adapter.volatility(optionletTimes_[i],atmCapFloorStrikes_[j]);
                  adjustedVol = unadjustedVol + spreadsVolImplied_[j];

                  // insert adjusted volatility
                  //List<Rate>::const_iterator previous =
                  //      std::lower_bound(optionletStrikes_[i].begin(),
                  //                        optionletStrikes_[i].end(),
                  //                        atmCapFloorStrikes_[j]);
                  var previous = optionletStrikes_[i].FindIndex(x => x >= atmCapFloorStrikes_[j]);
                  int insertIndex = (int) previous ;  // (int)optionletStrikes_[i].First();

                  optionletStrikes_[i].Insert(insertIndex,atmCapFloorStrikes_[j]);
                  optionletVolatilities_[i].Insert(insertIndex, adjustedVol);
                  //optionletStrikes_[i].Insert((int)atmCapFloorStrikes_[j], optionletStrikes_[i][insertIndex]);
                  //optionletVolatilities_[i].Insert((int)adjustedVol,optionletVolatilities_[i][insertIndex]);
               }
            }
         }
      }
      //@}

            
      private List<double> spreadsVolImplied()
      {
        Brent solver = new Brent();
        List<double> result = new InitializedList<double>(nOptionExpiries_,0.0);
        double guess = 0.0001, minSpread = -0.1, maxSpread = 0.1;
        for (int j=0; j<nOptionExpiries_; ++j) 
        {
            ObjectiveFunction f = new ObjectiveFunction(stripper1_, caps_[j], atmCapFloorPrices_[j]);
            solver.setMaxEvaluations(maxEvaluations_);
            double root = solver.solve(f, accuracy_, guess,minSpread, maxSpread);
            result[j] = root;
        }
        return result;
      }

      private class ObjectiveFunction : ISolver1d
      {
         public ObjectiveFunction(OptionletStripper1 optionletStripper1,CapFloor cap,double targetValue)
         {
            cap_ = cap;
            targetValue_ = targetValue;
                    
            OptionletVolatilityStructure adapter = new StrippedOptionletAdapter(optionletStripper1);

            // set an implausible value, so that calculation is forced
            // at first operator()(Volatility x) call
            spreadQuote_ = new SimpleQuote(-1.0);

            OptionletVolatilityStructure spreadedAdapter = new SpreadedOptionletVolatility(
               new Handle<OptionletVolatilityStructure>(adapter), new Handle<Quote>(spreadQuote_));

            BlackCapFloorEngine engine = new BlackCapFloorEngine(optionletStripper1.iborIndex().forwardingTermStructure(),
                   new Handle<OptionletVolatilityStructure>(spreadedAdapter));

            cap_.setPricingEngine(engine);

         }
         public override double value(double s)
         {
            if ( s != spreadQuote_.value() )
               spreadQuote_.setValue( s );
            return cap_.NPV() - targetValue_;
         }
         
         private SimpleQuote spreadQuote_;
         private CapFloor cap_;
         private double targetValue_;
      }

      private OptionletStripper1 stripper1_;
      private Handle<CapFloorTermVolCurve> atmCapFloorTermVolCurve_;
      private DayCounter dc_;
      private int nOptionExpiries_;
      private List<double> atmCapFloorStrikes_;
      private List<double> atmCapFloorPrices_;
      private List<double> spreadsVolImplied_;
      private List<CapFloor> caps_;
      private int maxEvaluations_;
      private double accuracy_;

   }
}
