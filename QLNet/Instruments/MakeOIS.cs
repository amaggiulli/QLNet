/*
 Copyright (C) 2008-2016  Andrea Maggiulli (a.maggiulli@gmail.com)
  
 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

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

namespace QLNet
{
   //! helper class
   /*! This class provides a more comfortable way
       to instantiate overnight indexed swaps.
   */
   public class MakeOIS
   {
      private Period swapTenor_;
      private OvernightIndex overnightIndex_;
      private double? fixedRate_;
      private Period forwardStart_;

      private int settlementDays_;
      private Date effectiveDate_, terminationDate_;
      private Calendar calendar_;

      private Frequency paymentFrequency_;
      DateGeneration.Rule rule_;
      private bool endOfMonth_, isDefaultEOM_;

      private OvernightIndexedSwap.Type type_;
      private double nominal_;

      private double overnightSpread_;
      private DayCounter fixedDayCount_;

      private IPricingEngine engine_;

      public MakeOIS(Period swapTenor, OvernightIndex overnightIndex,double? fixedRate = null , Period fwdStart = null )
      {
         swapTenor_=swapTenor;
         overnightIndex_ = overnightIndex;
         fixedRate_= fixedRate;
         forwardStart_= fwdStart?? new Period(0,TimeUnit.Days);
         settlementDays_ = 2;
         calendar_ = overnightIndex.fixingCalendar();
         paymentFrequency_ = Frequency.Annual;
         rule_ = DateGeneration.Rule.Backward;
         // any value here for endOfMonth_ would not be actually used
         isDefaultEOM_ = true;
         //endOfMonth_ = (new Period(1,TimeUnit.Months)<=swapTenor && swapTenor<=new Period(2,TimeUnit.Years) ? true : false);
         type_ = OvernightIndexedSwap.Type.Payer;
         nominal_ = 1.0;
         overnightSpread_ = 0.0;
         fixedDayCount_ = overnightIndex.dayCounter();
         //engine_ = new DiscountingSwapEngine(overnightIndex_.forwardingTermStructure());

      }

      public MakeOIS receiveFixed(bool flag = true) 
      {
        type_ = flag ? OvernightIndexedSwap.Type.Receiver : OvernightIndexedSwap.Type.Payer ;
        return this;
      }

      public MakeOIS withType(OvernightIndexedSwap.Type type) 
      {
         type_ = type;
         return this;
      }

      public MakeOIS withSettlementDays( int settlementDays ) 
      {
         settlementDays_ = settlementDays;
         effectiveDate_ = null; 
         return this;
      }

      public MakeOIS withEffectiveDate(Date effectiveDate)
      {
         effectiveDate_ = effectiveDate;
         return this;
      }

      public MakeOIS withTerminationDate(Date terminationDate) 
      {
         terminationDate_ = terminationDate;
         swapTenor_ = new Period();
         return this;
      }

      public MakeOIS withPaymentFrequency(Frequency f)
      {
         paymentFrequency_ = f;
         if (paymentFrequency_== Frequency.Once)
            rule_ = DateGeneration.Rule.Zero;
         return this;
      }

      public MakeOIS withRule(DateGeneration.Rule r) 
      {
         rule_ = r;
         if (r==DateGeneration.Rule.Zero)
            paymentFrequency_ = Frequency.Once;
         return this;
      }

      public MakeOIS withOvernightLegSpread(double sp)
      {
         overnightSpread_ = sp;
         return this;
      }

      public MakeOIS withNominal(double n)
      {
         nominal_ = n;
         return this;
      }

      public MakeOIS withDiscountingTermStructure(Handle<YieldTermStructure> discountingTermStructure)
      {

         engine_ = (IPricingEngine) new DiscountingSwapEngine(discountingTermStructure,false);

         return this;
      }

      public MakeOIS withFixedLegDayCount(DayCounter dc) 
      {
         fixedDayCount_ = dc;
         return this;
      }

      public MakeOIS withEndOfMonth(bool flag = true) 
      {
         endOfMonth_ = flag;
         isDefaultEOM_ = false;
         return this;
      }

      public MakeOIS withPricingEngine( IPricingEngine engine )
      {
         engine_ = engine;
         return this;
      }
      
      // OIswap creator
      public static implicit operator OvernightIndexedSwap(MakeOIS o) { return o.value(); }

      public OvernightIndexedSwap value()
      {
         Date startDate;

         if ( effectiveDate_ != null )
            startDate = effectiveDate_;
         else
         {
            Date refDate = Settings.evaluationDate();
            // if the evaluation date is not a business day
            // then move to the next business day
            refDate = calendar_.adjust( refDate );
            Date spotDate = calendar_.advance( refDate, new Period( settlementDays_, TimeUnit.Days ) );
            startDate = spotDate + forwardStart_;
            if ( forwardStart_.length() < 0 )
               startDate = calendar_.adjust( startDate, BusinessDayConvention.Preceding );
            else
               startDate = calendar_.adjust( startDate, BusinessDayConvention.Following );
         }

         // OIS end of month default
         bool usedEndOfMonth =
             isDefaultEOM_ ? calendar_.isEndOfMonth( startDate ) : endOfMonth_;

         Date endDate = terminationDate_;
         if ( endDate == null )
            if ( usedEndOfMonth )
               endDate = calendar_.advance( startDate,
                                           swapTenor_,
                                           BusinessDayConvention.ModifiedFollowing,
                                           usedEndOfMonth );
            else
               endDate = startDate + swapTenor_;



         Schedule schedule = new Schedule( startDate, endDate,
                          new Period( paymentFrequency_ ),
                          calendar_,
                          BusinessDayConvention.ModifiedFollowing,
                          BusinessDayConvention.ModifiedFollowing,
                          rule_,
                          usedEndOfMonth );

         double? usedFixedRate = fixedRate_;
         if ( fixedRate_ == null )
         {
            OvernightIndexedSwap temp = new OvernightIndexedSwap( type_, nominal_,
                                            schedule,
                                            0.0, // fixed rate
                                            fixedDayCount_,
                                            overnightIndex_, overnightSpread_ );
            if ( engine_ == null )
            {
               Handle<YieldTermStructure> disc = overnightIndex_.forwardingTermStructure();
               Utils.QL_REQUIRE( !disc.empty(), () => "null term structure set to this instance of " +
                  overnightIndex_.name() );
               bool includeSettlementDateFlows = false;
               IPricingEngine engine = new DiscountingSwapEngine( disc, includeSettlementDateFlows );
               temp.setPricingEngine( engine );
            }
            else
               temp.setPricingEngine( engine_ );

            usedFixedRate = temp.fairRate();
         }

         OvernightIndexedSwap ois = new OvernightIndexedSwap( type_, nominal_,
                                  schedule,
                                  usedFixedRate.Value, fixedDayCount_,
                                  overnightIndex_, overnightSpread_ );

         if ( engine_ == null )
         {
            Handle<YieldTermStructure> disc = overnightIndex_.forwardingTermStructure();
            bool includeSettlementDateFlows = false;
            IPricingEngine engine = new DiscountingSwapEngine( disc, includeSettlementDateFlows );
            ois.setPricingEngine( engine );
         }
         else
            ois.setPricingEngine( engine_ );

         return ois;
      }
   }
}
