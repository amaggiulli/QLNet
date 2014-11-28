/*
 Copyright (C) 2008, 2009 , 2010, 2011 , 2012  Andrea Maggiulli (a.maggiulli@gmail.com)
  
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
   //! base pricer for capped/floored CPI coupons N.B. vol-dependent parts are a TODO
   /*! \note this pricer can already do swaplets but to get
             volatility-dependent coupons you need to implement the descendents.
   */
   public class CPICouponPricer : InflationCouponPricer
   {
      public CPICouponPricer(Handle<CPIVolatilitySurface> capletVol = null)
      {
         if ( capletVol == null ) 
            capletVol = new Handle<CPIVolatilitySurface>();
        
        capletVol_ = capletVol;

        if( !capletVol_.empty() ) capletVol_.registerWith(update);
      }

      public virtual Handle<CPIVolatilitySurface> capletVolatility() 
      {
         return capletVol_;
      }

      public virtual void setCapletVolatility(Handle<CPIVolatilitySurface> capletVol)
      {
         Utils.QL_REQUIRE( !capletVol.empty(), () => "empty capletVol handle" );
         capletVol_ = capletVol;
         capletVol_.registerWith(update);
      }


      //! \name InflationCouponPricer interface
      //@{
      public override double swapletPrice()
      {
         double swapletPrice = adjustedFixing() * coupon_.accrualPeriod() * discount_;
         return gearing_ * swapletPrice + spreadLegValue_;
      }
      public override double swapletRate()
      {
         // This way we do not require the index to have
         // a yield curve, i.e. we do not get the problem
         // that a discounting-instrument-pricer is used
         // with a different yield curve
         //std::cout << (gearing_ * adjustedFixing() + spread_) << " SWAPLET rate" << gearing_ << " " << spread_ << std::endl;
         return gearing_ * adjustedFixing() + spread_;
      }
      public override double capletPrice(double effectiveCap)
      {
         double capletPrice = optionletPrice(Option.Type.Call, effectiveCap);
         return gearing_ * capletPrice;
      }
      public override double capletRate(double effectiveCap)
      {
         return capletPrice(effectiveCap)/(coupon_.accrualPeriod()*discount_);
      }
      public override double floorletPrice(double effectiveFloor)
      {
         double floorletPrice = optionletPrice(Option.Type.Put, effectiveFloor);
         return gearing_ * floorletPrice;
      }
      public override double floorletRate(double effectiveFloor)
      {
         return floorletPrice(effectiveFloor) / (coupon_.accrualPeriod()*discount_);
      }
      public override void initialize( InflationCoupon coupon)
      {
         coupon_ = coupon as CPICoupon;
         gearing_ = coupon_.fixedRate();
         spread_ = coupon_.spread();
         paymentDate_ = coupon_.date();
         rateCurve_ = ((ZeroInflationIndex)coupon.index())
            .zeroInflationTermStructure().link
            .nominalTermStructure();

         // past or future fixing is managed in YoYInflationIndex::fixing()
         // use yield curve from index (which sets discount)

         discount_ = 1.0;
         if (paymentDate_ > rateCurve_.link.referenceDate())
            discount_ = rateCurve_.link.discount(paymentDate_);

         spreadLegValue_ = spread_ * coupon_.accrualPeriod()* discount_;
      }
      //@}


      //! can replace this if really required
      protected virtual double optionletPrice(Option.Type optionType, double effStrike)
      {
         Date fixingDate = coupon_.fixingDate();
         if (fixingDate <= Settings.evaluationDate()) 
         {
            // the amount is determined
            double a, b;
            if (optionType==Option.Type.Call) 
            {
                  a = coupon_.indexFixing();
                  b = effStrike;
            } 
            else 
            {
                  a = effStrike;
                  b = coupon_.indexFixing();
            }
            return Math.Max(a - b, 0.0)* coupon_.accrualPeriod()*discount_;
         } 
         else 
         {
            // not yet determined, use Black/DD1/Bachelier/whatever from Impl
            Utils.QL_REQUIRE( !capletVolatility().empty(), () => "missing optionlet volatility" );
            double stdDev = Math.Sqrt(capletVolatility().link.totalVariance(fixingDate, effStrike));
            double fixing = optionletPriceImp(optionType,
                                              effStrike,
                                              adjustedFixing(),
                                              stdDev);
            return fixing * coupon_.accrualPeriod() * discount_;
         }
      }

      //! usually only need implement this (of course they may need
      //! to re-implement initialize too ...)
      protected virtual double optionletPriceImp(Option.Type optionType, double strike, double forward, double stdDev)
      {
         Utils.QL_FAIL("you must implement this to get a vol-dependent price");
         return strike * forward * stdDev * (int)optionType;
      }
      protected virtual double adjustedFixing(double? fixing = null)
      {
         if (fixing == null)
            fixing = coupon_.indexFixing() / coupon_.baseCPI();
         
         // no adjustment
         return fixing.Value;
      }

      //! data
      protected Handle<CPIVolatilitySurface> capletVol_;
      protected CPICoupon coupon_;
      protected double gearing_;
      protected double spread_;
      protected double discount_;
      protected double spreadLegValue_;
   }
}
