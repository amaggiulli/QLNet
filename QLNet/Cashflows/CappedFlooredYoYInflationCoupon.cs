/*
 Copyright (C) 2008, 2009 , 2010 Andrea Maggiulli (a.maggiulli@gmail.com)
  
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
   //! Capped or floored inflation coupon.
   /*! Essentially a copy of the nominal version but taking a
       different index and a set of pricers (not just one).

       The payoff \f$ P \f$ of a capped inflation-rate coupon
       with paysWithin = true is:

       \f[ P = N \times T \times \min(a L + b, C). \f]

       where \f$ N \f$ is the notional, \f$ T \f$ is the accrual
       time, \f$ L \f$ is the inflation rate, \f$ a \f$ is its
       gearing, \f$ b \f$ is the spread, and \f$ C \f$ and \f$ F \f$
       the strikes.

       The payoff of a floored inflation-rate coupon is:

       \f[ P = N \times T \times \max(a L + b, F). \f]

       The payoff of a collared inflation-rate coupon is:

       \f[ P = N \times T \times \min(\max(a L + b, F), C). \f]

       If paysWithin = false then the inverse is returned
       (this provides for instrument cap and caplet prices).

       They can be decomposed in the following manner.  Decomposition
       of a capped floating rate coupon when paysWithin = true:
       \f[
       R = \min(a L + b, C) = (a L + b) + \min(C - b - \xi |a| L, 0)
       \f]
       where \f$ \xi = sgn(a) \f$. Then:
       \f[
       R = (a L + b) + |a| \min(\frac{C - b}{|a|} - \xi L, 0)
       \f]
    */
   public class CappedFlooredYoYInflationCoupon : YoYInflationCoupon
   {
      // we may watch an underlying coupon ...
      public CappedFlooredYoYInflationCoupon(YoYInflationCoupon underlying,
                                             double? cap = null,
                                             double? floor = null)
         :base(underlying.date(),
               underlying.nominal(),
               underlying.accrualStartDate(),
               underlying.accrualEndDate(),
               underlying.fixingDays(),
               underlying.yoyIndex(),
               underlying.observationLag(),
               underlying.dayCounter(),
               underlying.gearing(),
               underlying.spread(),
               underlying.refPeriodStart,
               underlying.refPeriodEnd)
   
      {
         underlying_ = underlying;
         isFloored_ = false;
         isCapped_ = false;
         setCommon(cap, floor);
         underlying.registerWith(update);
      }

      
      // ... or not
      public CappedFlooredYoYInflationCoupon(Date paymentDate,
                                             double nominal,
                                             Date startDate,
                                             Date endDate,
                                             int fixingDays,
                                             YoYInflationIndex index,
                                             Period observationLag,
                                             DayCounter dayCounter,
                                             double gearing = 1.0,
                                             double spread = 0.0,
                                             double? cap = null,
                                             double? floor = null,
                                             Date refPeriodStart = null,
                                             Date refPeriodEnd = null)
        : base(paymentDate, nominal, startDate, endDate,
               fixingDays, index, observationLag,  dayCounter,
               gearing, spread, refPeriodStart, refPeriodEnd)
      {
         isFloored_ = false;
         isCapped_ = false; 
         setCommon(cap, floor);
      }

      //! \name augmented Coupon interface
      //@{
      //! swap(let) rate
      public override double rate()
      {
         double swapletRate = underlying_ != null ? underlying_.rate() : base.rate();

         if(isFloored_ || isCapped_) 
         {
            if (underlying_ != null) 
            {
               Utils.QL_REQUIRE( underlying_.pricer() != null, () => "pricer not set" );
            } 
            else 
            {
               Utils.QL_REQUIRE( pricer_ != null, () => "pricer not set" );
            }
        }

        double floorletRate = 0.0;
        if(isFloored_) 
        {
           floorletRate =
            underlying_ != null?
            underlying_.pricer().floorletRate(effectiveFloor()) :
            pricer().floorletRate(effectiveFloor())
            ;
        }
        double capletRate = 0.0;
        if(isCapped_) {
            capletRate =
            underlying_ != null ?
            underlying_.pricer().capletRate(effectiveCap()) :
            pricer().capletRate(effectiveCap())
            ;
        }

        return swapletRate + floorletRate - capletRate;

      }
      //! cap
      public double? cap() 
      {
         if ( (gearing_ > 0) && isCapped_)
            return cap_;

         if ( (gearing_ < 0) && isFloored_)
            return floor_;

         return null;
      }
      //! floor
      public double? floor() 
      {
         if ( (gearing_ > 0) && isFloored_)
            return floor_;

         if ( (gearing_ < 0) && isCapped_)
            return cap_;

         return null;
      }
      //! effective cap of fixing
      public double effectiveCap() 
      {
         return  (cap_ - spread())/gearing();
      }
      //! effective floor of fixing
      public double effectiveFloor()
      {
         return (floor_ - spread()) / gearing();
      }
      //@}

      public bool isCapped() { return isCapped_; }
      public bool isFloored() { return isFloored_; }

    
      public void setPricer(YoYInflationCouponPricer pricer) 
      {
         base.setPricer(pricer);
         if (underlying_ != null ) underlying_.setPricer(pricer);
      }

      protected virtual void setCommon(double? cap, double? floor)
      {
         isCapped_ = false;
         isFloored_ = false;

         if (gearing_ > 0)
         {
            if (cap != null)
            {
               isCapped_ = true;
               cap_ = cap.Value;
            }
            if (floor != null)
            {
               floor_ = floor.Value;
               isFloored_ = true;
            }
         }
         else
         {
            if (cap != null)
            {
               floor_ = cap.Value;
               isFloored_ = true;
            }
            if (floor != null)
            {
               isCapped_ = true;
               cap_ = floor.Value;
            }
         }

         if (isCapped_ && isFloored_)
         {
            if ( cap < floor )
               throw new ApplicationException( "cap level (" + cap +
                                               ") less than floor level (" + floor + ")");
         }

      }

      // data, we only use underlying_ if it was constructed that way,
      // generally we use the shared_ptr conversion to boolean to test
      protected YoYInflationCoupon underlying_;
      protected bool isFloored_, isCapped_;
      protected double cap_, floor_;
   }
}
