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

namespace QLNet
{
   public class BachelierCapFloorEngine : CapFloorEngine
   {
      public BachelierCapFloorEngine(Handle<YieldTermStructure> discountCurve,double vol,DayCounter dc = null)  // new Actual365Fixed()
      {
         
         discountCurve_ = discountCurve;
         vol_ = new Handle<OptionletVolatilityStructure>(
            new ConstantOptionletVolatility(0, new NullCalendar(), BusinessDayConvention.Following, vol, 
               dc ?? new Actual365Fixed())) ;
         discountCurve_.registerWith(update);
      }
      public BachelierCapFloorEngine(Handle<YieldTermStructure> discountCurve,Handle<Quote> vol,DayCounter dc = null)
      {
         discountCurve_ = discountCurve;
         vol_ = new Handle<OptionletVolatilityStructure>(
            new ConstantOptionletVolatility( 0, new NullCalendar(), BusinessDayConvention.Following, vol,
               dc ?? new Actual365Fixed() ) );
         discountCurve_.registerWith( update );
         vol_.registerWith(update);
      }
      public BachelierCapFloorEngine(Handle<YieldTermStructure> discountCurve,Handle<OptionletVolatilityStructure> vol)
      {
         discountCurve_ = discountCurve;
         vol_ = vol;
         discountCurve_.registerWith( update );
         vol_.registerWith( update );
      }

      public override void calculate()
      {
         double value = 0.0;
         double vega = 0.0;
         int optionlets = arguments_.startDates.Count;
         List<double> values = new InitializedList<double>(optionlets, 0.0);
         List<double> vegas = new InitializedList<double>(optionlets, 0.0);
         List<double> stdDevs = new InitializedList<double>(optionlets, 0.0);
         CapFloorType type = arguments_.type;
         Date today = vol_.link.referenceDate();
         Date settlement = discountCurve_.link.referenceDate();

         for (int i = 0; i < optionlets; ++i)
         {
            Date paymentDate = arguments_.endDates[i];
            // handling of settlementDate, npvDate and includeSettlementFlows
            // should be implemented.
            // For the double being just discard expired caplets
            if (paymentDate > settlement)
            {
               double d = arguments_.nominals[i]*
                          arguments_.gearings[i]*
                          discountCurve_.link.discount(paymentDate)*
                          arguments_.accrualTimes[i];

               double forward = arguments_.forwards[i].Value;

               Date fixingDate = arguments_.fixingDates[i];
               double sqrtTime = 0.0;
               if (fixingDate > today)
                  sqrtTime = Math.Sqrt(vol_.link.timeFromReference(fixingDate));

               if (type == CapFloorType.Cap || type == CapFloorType.Collar)
               {
                  double strike = arguments_.capRates[i].Value;
                  if (sqrtTime > 0.0)
                  {
                     stdDevs[i] = Math.Sqrt(vol_.link.blackVariance(fixingDate, strike));
                     vegas[i] = Utils.bachelierBlackFormulaStdDevDerivative(strike, forward, stdDevs[i], d)*sqrtTime;
                  }
                  // include caplets with past fixing date
                  values[i] = Utils.bachelierBlackFormula(Option.Type.Call, strike, forward, stdDevs[i], d);
               }
               if (type == CapFloorType.Floor || type == CapFloorType.Collar)
               {
                  double strike = arguments_.floorRates[i].Value;
                  double floorletVega = 0.0;
                  if (sqrtTime > 0.0)
                  {
                     stdDevs[i] = Math.Sqrt(vol_.link.blackVariance(fixingDate, strike));
                     floorletVega = Utils.bachelierBlackFormulaStdDevDerivative(strike, forward, stdDevs[i], d)*sqrtTime;
                  }
                  double floorlet = Utils.bachelierBlackFormula(Option.Type.Put, strike, forward, stdDevs[i], d);
                  if (type == CapFloorType.Floor)
                  {
                     values[i] = floorlet;
                     vegas[i] = floorletVega;
                  }
                  else
                  {
                     // a collar is long a cap and short a floor
                     values[i] -= floorlet;
                     vegas[i] -= floorletVega;
                  }
               }
               value += values[i];
               vega += vegas[i];
            }
         }
         results_.value = value;
         results_.additionalResults["vega"] = vega;

         results_.additionalResults["optionletsPrice"] = values;
         results_.additionalResults["optionletsVega"] = vegas;
         results_.additionalResults["optionletsAtmForward"] = arguments_.forwards;
         if (type != CapFloorType.Collar)
            results_.additionalResults["optionletsStdDev"] = stdDevs;
      }
   

      public Handle<YieldTermStructure> termStructure() { return discountCurve_; }
      public Handle<OptionletVolatilityStructure> volatility() { return vol_; }
      
      private Handle<YieldTermStructure> discountCurve_;
      private  Handle<OptionletVolatilityStructure> vol_;
   }
}
