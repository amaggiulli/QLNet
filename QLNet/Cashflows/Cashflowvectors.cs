/*
 Copyright (C) 2008, 2009 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
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

namespace QLNet {

    public static partial class Utils {
        public static double? toNullable(double val) {
            if (val == double.MinValue)
                return null;
            else
                return val;
        }
    }

    public static class CashFlowVectors {
        public static List<CashFlow> FloatingLeg<InterestRateIndexType, FloatingCouponType, CappedFlooredCouponType>(List<double> nominals,
                                                Schedule schedule,
                                                InterestRateIndexType index,
                                                DayCounter paymentDayCounter,
                                                BusinessDayConvention paymentAdj,
                                                List<int> fixingDays,
                                                List<double> gearings,
                                                List<double> spreads,
                                                List<double> caps,
                                                List<double> floors,
                                                bool isInArrears,
                                                bool isZero)
            where InterestRateIndexType : InterestRateIndex, new()
            where FloatingCouponType : FloatingRateCoupon, new()
            where CappedFlooredCouponType : CappedFlooredCoupon, new() {

            int n = schedule.Count;
            if (nominals.Count == 0) throw new ArgumentException("no notional given");
            if (nominals.Count > n) throw new ArgumentException(
                       "too many nominals (" + nominals.Count + "), only " + n + " required");
            if (gearings != null && gearings.Count > n) throw new ArgumentException(
                       "too many gearings (" + gearings.Count + "), only " + n + " required");
            if (spreads != null && spreads.Count > n) throw new ArgumentException(
                       "too many spreads (" + spreads.Count + "), only " + n + " required");
            if (caps != null && caps.Count > n) throw new ArgumentException(
                       "too many caps (" + caps.Count + "), only " + n + " required");
            if (floors != null && floors.Count > n) throw new ArgumentException(
                       "too many floors (" + floors.Count + "), only " + n + " required");
            if (isZero && isInArrears) throw new ArgumentException("in-arrears and zero features are not compatible");

            List<CashFlow> leg = new List<CashFlow>();

            // the following is not always correct
            Calendar calendar = schedule.calendar();

            Date refStart, start, refEnd, end;
            Date lastPaymentDate = calendar.adjust(schedule[n - 1], paymentAdj);

            for (int i = 0; i < n - 1; ++i) {
                refStart = start = schedule[i];
                refEnd = end = schedule[i + 1];
                Date paymentDate = isZero ? lastPaymentDate : calendar.adjust(end, paymentAdj);
                if (i == 0 && !schedule.isRegular(i + 1))
                    refStart = calendar.adjust(end - schedule.tenor(), schedule.businessDayConvention());
                if (i == n - 1 && !schedule.isRegular(i + 1))
                    refEnd = calendar.adjust(start + schedule.tenor(), schedule.businessDayConvention());

                if (Utils.Get(gearings, i, 1) == 0) {                               // fixed coupon
                    leg.Add(new FixedRateCoupon(Utils.Get(nominals, i),
                                                paymentDate,
                                                Utils.effectiveFixedRate(spreads, caps, floors, i),
                                                paymentDayCounter,
                                                start, end, refStart, refEnd));
                } else {
                    if (Utils.noOption(caps, floors, i)) {
                        leg.Add(new FloatingCouponType().factory(Utils.Get(nominals, i),
                            paymentDate, start, end,
                            Utils.Get(fixingDays, i, 2),
                            index,
                            Utils.Get(gearings, i, 1),
                            Utils.Get(spreads, i),
                            refStart, refEnd, paymentDayCounter,
                            isInArrears));
                    } else {
                        leg.Add(new CappedFlooredCouponType().factory(Utils.Get(nominals, i),
                            paymentDate, start, end,
                            Utils.Get(fixingDays, i, 2),
                            index,
                            Utils.Get(gearings, i, 1),
                            Utils.Get(spreads, i),
                            Utils.toNullable(Utils.Get(caps, i, Double.MinValue)),
                            Utils.toNullable(Utils.Get(floors, i, Double.MinValue)),
                            refStart, refEnd, paymentDayCounter,
                            isInArrears));
                    }
                }
            }
            return leg;
        }

        public static List<CashFlow> FloatingDigitalLeg<InterestRateIndexType, FloatingCouponType, DigitalCouponType>(List<double> nominals,
                                                Schedule schedule,
                                                InterestRateIndexType index,
                                                DayCounter paymentDayCounter,
                                                BusinessDayConvention paymentAdj,
                                                List<int> fixingDays,
                                                List<double> gearings,
                                                List<double> spreads,
                                                bool isInArrears,
                                                List<double> callStrikes,
                                                Position.Type callPosition,
                                                bool isCallATMIncluded,
                                                List<double> callDigitalPayoffs,
                                                List<double> putStrikes,
                                                Position.Type putPosition,
                                                bool isPutATMIncluded,
                                                List<double> putDigitalPayoffs,
                                                DigitalReplication replication)
            where InterestRateIndexType : InterestRateIndex, new()
            where FloatingCouponType : FloatingRateCoupon, new()
            where DigitalCouponType : DigitalCoupon, new() {

            int n = schedule.Count;
            if (nominals.Count == 0) throw new ArgumentException("no nominal given");
            if (nominals.Count > n) throw new ArgumentException(
                       "too many nominals (" + nominals.Count + "), only " + n + " required");
            if (gearings != null && gearings.Count > n) throw new ArgumentException(
                       "too many gearings (" + gearings.Count + "), only " + n + " required");
            if (spreads != null && spreads.Count > n) throw new ArgumentException(
                       "too many spreads (" + spreads.Count + "), only " + n + " required");
            if (callStrikes.Count > n) throw new ArgumentException(
                       "too many nominals (" + callStrikes.Count + "), only " + n + " required");
            if (putStrikes.Count > n) throw new ArgumentException(
                       "too many nominals (" + putStrikes.Count + "), only " + n + " required");


            List<CashFlow> leg = new List<CashFlow>();

            // the following is not always correct
            Calendar calendar = schedule.calendar();

            Date refStart, start, refEnd, end;
            Date paymentDate;

            for (int i = 0; i < n; ++i) {
                refStart = start = schedule.date(i);
                refEnd = end = schedule.date(i + 1);
                paymentDate = calendar.adjust(end, paymentAdj);
                if (i == 0 && !schedule.isRegular(i + 1)) {
                    BusinessDayConvention bdc = schedule.businessDayConvention();
                    refStart = calendar.adjust(end - schedule.tenor(), bdc);
                }
                if (i == n - 1 && !schedule.isRegular(i + 1)) {
                    BusinessDayConvention bdc = schedule.businessDayConvention();
                    refEnd = calendar.adjust(start + schedule.tenor(), bdc);
                }
                if (Utils.Get(gearings, i, 1.0) == 0.0) { // fixed coupon
                    leg.Add(new
                        FixedRateCoupon(Utils.Get(nominals, i, 1.0),
                                        paymentDate,
                                        Utils.Get(spreads, i, 1.0),
                                        paymentDayCounter,
                                        start, end, refStart, refEnd));

                } else { // floating digital coupon
                    FloatingCouponType underlying = new FloatingCouponType().factory(
                       Utils.Get(nominals, i, 1.0),
                       paymentDate, start, end,
                       Utils.Get(fixingDays, i, index.fixingDays()),
                       index,
                       Utils.Get(gearings, i, 1.0),
                       Utils.Get(spreads, i, 0.0),
                       refStart, refEnd,
                       paymentDayCounter, isInArrears) as FloatingCouponType;

                    DigitalCouponType digitalCoupon = new DigitalCouponType().factory(
                     underlying,
                     Utils.toNullable(Utils.Get(callStrikes, i, Double.MinValue)),
                     callPosition,
                     isCallATMIncluded,
                     Utils.toNullable(Utils.Get(callDigitalPayoffs, i, Double.MinValue)),
                     Utils.toNullable(Utils.Get(putStrikes, i, Double.MinValue)),
                     putPosition,
                     isPutATMIncluded,
                     Utils.toNullable(Utils.Get(putDigitalPayoffs, i, Double.MinValue)),
                     replication) as DigitalCouponType;

                    leg.Add(digitalCoupon);
                }
            }
            return leg;
        }

        
         public static List<CashFlow> OvernightLeg(List<double> nominals,
                                                   Schedule schedule,
                                                   BusinessDayConvention paymentAdjustment,
                                                   OvernightIndex overnightIndex,
                                                   List<double> gearings,
                                                   List<double> spreads,
                                                   DayCounter paymentDayCounter      
                                                  )
        
       
         {
            if (nominals.Count == 0) throw new ArgumentException("no nominal given");

            List<CashFlow> leg = new List<CashFlow>();

            // the following is not always correct
            Calendar calendar = schedule.calendar();

            Date refStart, start, refEnd, end;
            Date paymentDate;

            int n = schedule.Count;
            for (int i=0; i<n-1; ++i) 
            {
               refStart = start = schedule.date(i);
               refEnd   =   end = schedule.date(i+1);
               paymentDate = calendar.adjust(end, paymentAdjustment);
               if (i == 0 && !schedule.isRegular(i+1))
                   refStart = calendar.adjust(end - schedule.tenor(),
                                              paymentAdjustment);
               if (i == n-1 && !schedule.isRegular(i+1))
                   refEnd = calendar.adjust(start + schedule.tenor(),
                                            paymentAdjustment);

               leg.Add( new OvernightIndexedCoupon(paymentDate,
                                                   Utils.Get(nominals, i),
                                                   start, end,
                                                   overnightIndex,
                                                   Utils.Get(gearings, i, 1.0),
                                                   Utils.Get(spreads, i, 0.0),
                                                   refStart, refEnd,
                                                   paymentDayCounter));
           }
           return leg;
        }

         public static List<CashFlow> yoyInflationLeg(List<double> notionals_, 
                                                      Schedule schedule_, 
                                                      BusinessDayConvention paymentAdjustment_, 
                                                      YoYInflationIndex index_, 
                                                      List<double> gearings_, 
                                                      List<double> spreads_, 
                                                      DayCounter paymentDayCounter_,
                                                      List<double> caps_, 
                                                      List<double> floors_ ,
                                                      Calendar paymentCalendar_,
                                                      List<int> fixingDays_,
                                                      Period observationLag_)
         {
            int n = schedule_.Count -1 ;

            if (notionals_.empty())
               throw new ApplicationException("no notional given");

            if (notionals_.Count > n)
               throw new ApplicationException("too many nominals (" + notionals_.Count +
                                              "), only " + n + " required");
				if ( gearings_ != null && gearings_.Count > n )
               throw new ApplicationException("too many gearings (" + gearings_.Count +
                                              "), only " + n + " required");

				if ( spreads_ != null && spreads_.Count > n )
               throw new ApplicationException("too many spreads (" + spreads_.Count +
                                              "), only " + n + " required");

				if ( caps_ != null && caps_.Count > n )
               throw new ApplicationException("too many caps (" + caps_.Count +
                                              "), only " + n + " required");

				if ( floors_ != null && floors_.Count > n )
               throw new ApplicationException("too many floors (" + floors_.Count +
                                              "), only " + n + " required");


            List<CashFlow> leg = new List<CashFlow>(n);

            Calendar calendar = paymentCalendar_;

            Date refStart, start, refEnd, end;
            //Date lastPaymentDate = calendar.adjust(schedule_.date(n), paymentAdjustment_);

            for (int i = 0; i < n; ++i)
            {
               refStart = start = schedule_.date(i);
               refEnd = end = schedule_.date(i + 1);
               Date paymentDate = calendar.adjust(end, paymentAdjustment_);
               if (i == 0 && !schedule_.isRegular(i + 1))
               {
                  BusinessDayConvention bdc = schedule_.businessDayConvention();
                  refStart = schedule_.calendar().adjust(end - schedule_.tenor(), bdc);
               }
               if (i == n - 1 && !schedule_.isRegular(i + 1))
               {
                  BusinessDayConvention bdc = schedule_.businessDayConvention();
                  refEnd = schedule_.calendar().adjust(start + schedule_.tenor(), bdc);
               }
               if (Utils.Get(gearings_, i, 1.0) == 0.0)
               {
                  // fixed coupon
                  leg.Add(new FixedRateCoupon(Utils.Get(notionals_, i, 1.0),
                                              paymentDate,
                                              Utils.effectiveFixedRate(spreads_, caps_,
                                                                         floors_, i),
                                              paymentDayCounter_,
                                              start, end, refStart, refEnd));
               }
               else
               {
                  // yoy inflation coupon
                  if (Utils.noOption(caps_, floors_, i))
                  {
                     // just swaplet
                     YoYInflationCoupon coup = new YoYInflationCoupon(paymentDate,
                                                                     Utils.Get(notionals_, i, 1.0),
                                                                     start, end,
                                                                     Utils.Get(fixingDays_, i, 0),
                                                                     index_,
                                                                     observationLag_,
                                                                     paymentDayCounter_,
                                                                     Utils.Get(gearings_, i, 1.0),
                                                                     Utils.Get(spreads_, i, 0.0),
                                                                     refStart, refEnd);

                     // in this case you can set a pricer
                     // straight away because it only provides computation - not data
                     YoYInflationCouponPricer pricer = new YoYInflationCouponPricer();
                     coup.setPricer(pricer);
                     leg.Add(coup);
                  }
                  else
                  {
                     // cap/floorlet
                     leg.Add(new CappedFlooredYoYInflationCoupon(
                                   paymentDate,
                                   Utils.Get(notionals_, i, 1.0),
                                   start, end,
                                   Utils.Get(fixingDays_, i, 0),
                                   index_,
                                   observationLag_,
                                   paymentDayCounter_,
                                   Utils.Get(gearings_, i, 1.0),
                                   Utils.Get(spreads_, i, 0.0),
                                   Utils.toNullable(Utils.Get(caps_, i, Double.MinValue)),
                                   Utils.toNullable(Utils.Get(floors_, i, Double.MinValue)),
                                   refStart, refEnd));
                  }
               }
            }

            return leg;

         }


    }
}
