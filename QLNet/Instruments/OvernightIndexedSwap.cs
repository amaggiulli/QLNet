/*
 Copyright (C) 2008, 2009 , 2010  Andrea Maggiulli (a.maggiulli@gmail.com)
 * 
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

namespace QLNet
{
   //! Overnight indexed swap: fix vs compounded overnight rate
   public class OvernightIndexedSwap : Swap 
   {
      private Type type_;
      private double nominal_;
      private Frequency paymentFrequency_;
      private double fixedRate_;
      private DayCounter fixedDC_;
      private OvernightIndex overnightIndex_;
      private double spread_;

      public enum Type { Receiver = -1, Payer = 1 };
      
      public OvernightIndexedSwap(Type type,
                                  double nominal,
                                  Schedule schedule,
                                  double fixedRate,
                                  DayCounter fixedDC,
                                  OvernightIndex overnightIndex,
                                  double spread) : 
      base(2)
      {
      
         type_= type;
         nominal_ = nominal;
         paymentFrequency_ = schedule.tenor().frequency();
         fixedRate_ = fixedRate;
         fixedDC_ = fixedDC;
         overnightIndex_ = overnightIndex;
         spread_ = spread;

         if (fixedDC_== null)
            fixedDC_ = overnightIndex_.dayCounter();

         legs_[0] = new FixedRateLeg(schedule)
            .withCouponRates(fixedRate_, fixedDC_)
            .withNotionals(nominal_);

        legs_[1] = new OvernightLeg(schedule, overnightIndex_)
            .withNotionals(nominal_)
            .withSpreads(spread_);

         for (int j = 0; j < 2; ++j)
         {
            for (int i = 0; i < legs_[j].Count; i++)
               legs_[j][i].registerWith(update);
         }

         switch (type_) 
         {
            case Type.Payer:
               payer_[0] = -1.0;
               payer_[1] = +1.0;
               break;
            case Type.Receiver:
               payer_[0] = +1.0;
               payer_[1] = -1.0;
               break;
            default:
               throw new ApplicationException("Unknown overnight-swap type"); 
         
         }
      }

      public Type type() { return type_; }
      public double nominal() { return nominal_; }
      public Frequency paymentFrequency() { return paymentFrequency_; }
      public double fixedRate() { return fixedRate_; }
      public DayCounter fixedDayCount() { return fixedDC_; }
      //OvernightIndex overnightIndex();
      double spread() { return spread_; }

      List<CashFlow> fixedLeg() { return legs_[0]; }
      List<CashFlow> overnightLeg() { return legs_[1]; }


      public double? fairRate() 
      {
         const double basisPoint = 1.0e-4;
         calculate();
         return fixedRate_ - NPV_/(fixedLegBPS()/basisPoint);
      }

      public double? fairSpread()
      {
        const double basisPoint = 1.0e-4;
        calculate();
        return spread_ - NPV_/(overnightLegBPS()/basisPoint);
      }

      public double? fixedLegBPS() 
      {
         calculate();
         if (legBPS_[0] == null)
            throw new ApplicationException("result not available");
         return legBPS_[0];
      }

      public double? overnightLegBPS()
      {
        calculate();
        if (legBPS_[1] == null)
           throw new ApplicationException("result not available");
        return legBPS_[1];
      }

      public double? fixedLegNPV() 
      {
         calculate();
         if (legNPV_[0] == null)
            throw new ApplicationException("result not available");
         return legNPV_[0];
      }

      public double? overnightLegNPV()
      {
         calculate();
         if (legNPV_[1] == null)
            throw new ApplicationException("result not available");
         return legNPV_[1];
      }
   }
}
