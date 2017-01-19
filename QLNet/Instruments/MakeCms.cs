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
   //! helper class for instantiating CMS
   /*! This class provides a more comfortable way
       to instantiate standard market constant maturity swap.
   */
   public class MakeCms
   {
      public MakeCms( Period swapTenor,
                      SwapIndex swapIndex,
                      IborIndex iborIndex,
                      double iborSpread = 0.0,
                      Period forwardStart = null)
      {
         swapTenor_ = swapTenor; 
         swapIndex_ = swapIndex;
         iborIndex_ = iborIndex; 
         iborSpread_ = iborSpread;
         useAtmSpread_ = false; 
         forwardStart_ = forwardStart ?? new Period(0,TimeUnit.Days);
         cmsSpread_ = 0.0; 
         cmsGearing_ = 1.0;
         cmsCap_ = 0; 
         cmsFloor_ = 0;
         effectiveDate_ = null;
         cmsCalendar_ = swapIndex.fixingCalendar();
         floatCalendar_ = iborIndex.fixingCalendar();
         payCms_ = true; nominal_ = 1.0;
         cmsTenor_ = new Period(3,TimeUnit.Months); 
         floatTenor_ = iborIndex.tenor();
         cmsConvention_ = BusinessDayConvention.ModifiedFollowing;
         cmsTerminationDateConvention_ = BusinessDayConvention.ModifiedFollowing;
         floatConvention_ = iborIndex.businessDayConvention();
         floatTerminationDateConvention_ = iborIndex.businessDayConvention();
         cmsRule_ = DateGeneration.Rule.Backward; 
         floatRule_ = DateGeneration.Rule.Backward;
         cmsEndOfMonth_ = false; 
         floatEndOfMonth_ = false;
         cmsFirstDate_ = null; 
         cmsNextToLastDate_ = null;
         floatFirstDate_ = null; 
         floatNextToLastDate_ = null;
         cmsDayCount_ = new Actual360();
         floatDayCount_ = iborIndex.dayCounter();
         // arbitrary choice:
         //engine_ = new DiscountingSwapEngine(iborIndex->termStructure());
         engine_ = new DiscountingSwapEngine(swapIndex.forwardingTermStructure());
      }

      public MakeCms( Period swapTenor,
                      SwapIndex swapIndex,
                      double iborSpread = 0.0,
                      Period forwardStart = null)
      {
         swapTenor_ = swapTenor; 
         swapIndex_ = swapIndex;
         iborIndex_ = swapIndex.iborIndex(); 
         iborSpread_ = iborSpread;
         useAtmSpread_ = false; 
         forwardStart_ = forwardStart ?? new Period(0,TimeUnit.Days);
         cmsSpread_ = 0.0; 
         cmsGearing_ = 1.0;
         cmsCap_ = 0; 
         cmsFloor_ = 0;
         effectiveDate_ = null;
         cmsCalendar_ = swapIndex.fixingCalendar();
         floatCalendar_ = iborIndex_.fixingCalendar();
         payCms_ = true; 
         nominal_ = 1.0;
         cmsTenor_ = new Period(3,TimeUnit.Months); 
         floatTenor_ = iborIndex_.tenor();
         cmsConvention_ = BusinessDayConvention.ModifiedFollowing;
         cmsTerminationDateConvention_ = BusinessDayConvention.ModifiedFollowing;
         floatConvention_ = iborIndex_.businessDayConvention();
         floatTerminationDateConvention_ = iborIndex_.businessDayConvention();
         cmsRule_ = DateGeneration.Rule.Backward; 
         floatRule_ = DateGeneration.Rule.Backward;
         cmsEndOfMonth_ = false; 
         floatEndOfMonth_ = false;
         cmsFirstDate_ = null; 
         cmsNextToLastDate_ = null;
         floatFirstDate_ = null; 
         floatNextToLastDate_ = null;
         cmsDayCount_ = new Actual360();
         floatDayCount_ = iborIndex_.dayCounter();
         engine_ = new DiscountingSwapEngine(swapIndex.forwardingTermStructure());
      }

      public Swap value()
      {
         Date startDate;
         if (effectiveDate_ != null)
            startDate = effectiveDate_;
         else 
         {
            int fixingDays = iborIndex_.fixingDays();
            Date refDate = Settings.evaluationDate();
            // if the evaluation date is not a business day
            // then move to the next business day
            refDate = floatCalendar_.adjust(refDate);
            Date spotDate = floatCalendar_.advance(refDate, new Period(fixingDays,TimeUnit.Days));
            startDate = spotDate+forwardStart_;
        }

        Date terminationDate = startDate+swapTenor_;

        Schedule cmsSchedule = new Schedule(startDate, terminationDate,
                                            cmsTenor_, cmsCalendar_,
                                            cmsConvention_,
                                            cmsTerminationDateConvention_,
                                            cmsRule_, cmsEndOfMonth_,
                                            cmsFirstDate_, cmsNextToLastDate_);

        Schedule floatSchedule = new Schedule(startDate, terminationDate,
                                              floatTenor_, floatCalendar_,
                                              floatConvention_,
                                              floatTerminationDateConvention_,
                                              floatRule_ , floatEndOfMonth_,
                                              floatFirstDate_, floatNextToLastDate_);

         List<CashFlow> cmsLeg = new CmsLeg(cmsSchedule, swapIndex_)
            .withPaymentDayCounter(cmsDayCount_)
            .withFixingDays(swapIndex_.fixingDays())
            .withGearings(cmsGearing_)
            .withSpreads(cmsSpread_)
            .withCaps(cmsCap_)
            .withFloors(cmsFloor_)
            .withNotionals(nominal_)
            .withPaymentAdjustment(cmsConvention_);

         if (couponPricer_ != null)
            Utils.setCouponPricer(cmsLeg, couponPricer_);

         double? usedSpread = iborSpread_;
         if (useAtmSpread_) 
         {
            Utils.QL_REQUIRE(!iborIndex_.forwardingTermStructure().empty(),()=>
                       "null term structure set to this instance of " + iborIndex_.name());
            Utils.QL_REQUIRE(!swapIndex_.forwardingTermStructure().empty(),()=>
                       "null term structure set to this instance of " + swapIndex_.name());
            Utils.QL_REQUIRE(couponPricer_ != null,()=> "no CmsCouponPricer set (yet)");
            List<CashFlow> fLeg = new IborLeg(floatSchedule, iborIndex_)
               .withPaymentDayCounter(floatDayCount_)
               .withFixingDays(iborIndex_.fixingDays())
               .withNotionals(nominal_)
               .withPaymentAdjustment(floatConvention_);

            Swap temp = new Swap(cmsLeg, fLeg);
            temp.setPricingEngine(engine_);

            double? npv = temp.legNPV(0)+temp.legNPV(1);

            usedSpread = -npv/temp.legBPS(1)*1e-4;
         } 
         else 
         {
            Utils.QL_REQUIRE(usedSpread.HasValue,()=>"null spread set");
         }

         List<CashFlow> floatLeg = new IborLeg(floatSchedule, iborIndex_)
            .withSpreads(usedSpread.Value)
            .withPaymentDayCounter(floatDayCount_)
            .withFixingDays(iborIndex_.fixingDays())
            .withPaymentAdjustment(floatConvention_)
            .withNotionals(nominal_);

         Swap swap;
         if (payCms_)
            swap = new Swap(cmsLeg, floatLeg);
         else
            swap = new Swap(floatLeg, cmsLeg);
         swap.setPricingEngine(engine_);
         return swap;
      }

      public MakeCms receiveCms(bool flag = true)
      {
         payCms_ = !flag;
         return this;
      }
      public MakeCms withNominal(double n)
      {
         nominal_ = n;
         return this;
      }
      public MakeCms withEffectiveDate( Date effectiveDate )
      {
         effectiveDate_ = effectiveDate;
         return this;
      }

      public MakeCms withCmsLegTenor(Period t)
      {
         cmsTenor_ = t;
         return this;
      }
      public MakeCms withCmsLegCalendar( Calendar cal)
      {
         cmsCalendar_ = cal;
         return this;
      }
      public MakeCms withCmsLegConvention(BusinessDayConvention bdc)
      {
         cmsConvention_ = bdc;
         return this;
      }
      public MakeCms withCmsLegTerminationDateConvention( BusinessDayConvention bdc )
      {
         cmsTerminationDateConvention_ = bdc;
         return this;
      }
      public MakeCms withCmsLegRule(DateGeneration.Rule r)
      {
         cmsRule_ = r;
         return this;
      }
      public MakeCms withCmsLegEndOfMonth(bool flag = true)
      {
         cmsEndOfMonth_ = flag;
         return this;
      }
      public MakeCms withCmsLegFirstDate( Date d)
      {
         cmsFirstDate_ = d;
         return this;
      }
      public MakeCms withCmsLegNextToLastDate( Date d)
      {
         cmsNextToLastDate_ = d;
         return this;
      }
      public MakeCms withCmsLegDayCount( DayCounter dc)
      {
         cmsDayCount_ = dc;
         return this;
      }

      public MakeCms withFloatingLegTenor( Period t)
      {
         floatTenor_ = t;
         return this;
      }
      public MakeCms withFloatingLegCalendar( Calendar cal)
      {
         floatCalendar_ = cal;
         return this;
      }
      public MakeCms withFloatingLegConvention(BusinessDayConvention bdc)
      {
         floatConvention_ = bdc;
         return this;
      }
      public MakeCms withFloatingLegTerminationDateConvention( BusinessDayConvention bdc)
      {
         floatTerminationDateConvention_ = bdc;
         return this;
      }
      public MakeCms withFloatingLegRule(DateGeneration.Rule r)
      {
         floatRule_ = r;
         return this;
      }
      public MakeCms withFloatingLegEndOfMonth(bool flag = true)
      {
         floatEndOfMonth_ = flag;
         return this;
      }
      public MakeCms withFloatingLegFirstDate( Date d)
      {
         floatFirstDate_ = d;
         return this;
      }
      public MakeCms withFloatingLegNextToLastDate( Date d)
      {
         floatNextToLastDate_ = d;
         return this;
      }
      public MakeCms withFloatingLegDayCount( DayCounter dc)
      {
         floatDayCount_ = dc;
         return this;
      }

      public MakeCms withAtmSpread(bool flag = true)
      {
         useAtmSpread_ = flag;
         return this;
      }

      public MakeCms withDiscountingTermStructure( Handle<YieldTermStructure> discountingTermStructure)
      {
         engine_ = new DiscountingSwapEngine(discountingTermStructure);
         return this;
      }
      public MakeCms withCmsCouponPricer( CmsCouponPricer couponPricer)
      {
         couponPricer_ = couponPricer;
         return this;
      }

      private Period swapTenor_;
      private SwapIndex swapIndex_;
      private IborIndex iborIndex_;
      private double iborSpread_;
      private bool useAtmSpread_;
      private Period forwardStart_;

      private double cmsSpread_;
      private double cmsGearing_;
      private double cmsCap_, cmsFloor_;

      private Date effectiveDate_;
      private Calendar cmsCalendar_, floatCalendar_;

      private bool payCms_;
      private double nominal_;
      private Period cmsTenor_, floatTenor_;
      private BusinessDayConvention cmsConvention_, cmsTerminationDateConvention_;
      private BusinessDayConvention floatConvention_, floatTerminationDateConvention_;
      private DateGeneration.Rule cmsRule_, floatRule_;
      private bool cmsEndOfMonth_, floatEndOfMonth_;
      private Date cmsFirstDate_, cmsNextToLastDate_;
      private Date floatFirstDate_, floatNextToLastDate_;
      private DayCounter cmsDayCount_, floatDayCount_;

      private IPricingEngine engine_;
      private CmsCouponPricer couponPricer_;

   }
}
