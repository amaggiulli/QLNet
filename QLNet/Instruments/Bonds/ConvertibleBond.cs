/*
 Copyright (C) 2008, 2009 , 2010, 2011, 2012  Andrea Maggiulli (a.maggiulli@gmail.com) 
  
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
    //! %callability leaving to the holder the possibility to convert
    class SoftCallability : Callability 
    {
       public SoftCallability(Callability.Price price,Date date,double trigger)
        : base(price, Callability.Type.Call, date) 
       {
          trigger_ = trigger;
       }
       public double trigger() { return trigger_; }
       private double trigger_;
    }

   //! base class for convertible bonds
   public class ConvertibleBond : Bond
   {
      //class option;
      public class option : OneAssetOption 
      {
         public class arguments : OneAssetOption.Arguments
         {
            public arguments()
            {
               conversionRatio = null;
               settlementDays = null;
               redemption= null;
            }

            public double? conversionRatio;
            public Handle<Quote> creditSpread;
            public DividendSchedule dividends;
            public List<Date> dividendDates;
            public List<Date> callabilityDates;
            public List<Callability.Type> callabilityTypes;
            public List<double> callabilityPrices;
            public List<double?> callabilityTriggers;
            public List<Date> couponDates;
            public List<double> couponAmounts;
            public Date issueDate;
            public Date settlementDate;
  
            public int? settlementDays;
            public double? redemption;
            public override void validate()
            {
               base.validate();

               Utils.QL_REQUIRE( conversionRatio != null, () => "null conversion ratio" );
               Utils.QL_REQUIRE( conversionRatio > 0.0, () => "positive conversion ratio required: " + conversionRatio + " not allowed" );

               Utils.QL_REQUIRE( redemption != null, () => "null redemption" );
               Utils.QL_REQUIRE( redemption >= 0.0, () => "positive redemption required: " + redemption + " not allowed" );

               Utils.QL_REQUIRE( settlementDate != null, () => "null settlement date" );

               Utils.QL_REQUIRE( settlementDays != null, () => "null settlement days" );

               Utils.QL_REQUIRE( callabilityDates.Count == callabilityTypes.Count, () => "different number of callability dates and types" );
               Utils.QL_REQUIRE( callabilityDates.Count == callabilityPrices.Count, () => "different number of callability dates and prices" );
               Utils.QL_REQUIRE( callabilityDates.Count == callabilityTriggers.Count, () => "different number of callability dates and triggers" );

               Utils.QL_REQUIRE( couponDates.Count == couponAmounts.Count, () => "different number of coupon dates and amounts" );
            }
         
         }

        //public class engine;
        public option( ConvertibleBond bond,
                             Exercise exercise,
                             double conversionRatio,
                             DividendSchedule dividends,
                             CallabilitySchedule callability,
                             Handle<Quote> creditSpread,
                             List<CashFlow> cashflows,
                             DayCounter dayCounter,
                             Schedule schedule,
                             Date issueDate,
                             int settlementDays,
                             double redemption)
           :base(new PlainVanillaPayoff(Option.Type.Call, (bond.notionals()[0])/100.0 *redemption/conversionRatio), exercise)
        {
            bond_ = bond; 
            conversionRatio_ = conversionRatio;
            callability_ = callability; 
            dividends_ = dividends;
            creditSpread_ = creditSpread; 
            cashflows_ = cashflows;
            dayCounter_ = dayCounter; 
            issueDate_ = issueDate; 
            schedule_ = schedule;
            settlementDays_ = settlementDays; 
            redemption_ = redemption;
        }

        public override void setupArguments(IPricingEngineArguments args)
        {
           base.setupArguments(args);

           ConvertibleBond.option.arguments moreArgs = args as ConvertibleBond.option.arguments;
           Utils.QL_REQUIRE( moreArgs != null, () => "wrong argument type" );

           moreArgs.conversionRatio = conversionRatio_;

           Date settlement = bond_.settlementDate();

           int n = callability_.Count;
           moreArgs.callabilityDates.Clear();
           moreArgs.callabilityTypes.Clear();
           moreArgs.callabilityPrices.Clear();
           moreArgs.callabilityTriggers.Clear();
           moreArgs.callabilityDates.Capacity = n;
           moreArgs.callabilityTypes.Capacity = n;
           moreArgs.callabilityPrices.Capacity = n;
           moreArgs.callabilityTriggers.Capacity = n;
           for (int i=0; i<n; i++) 
           {
               if (!callability_[i].hasOccurred(settlement, false)) 
               {
                  moreArgs.callabilityTypes.Add(callability_[i].type());
                  moreArgs.callabilityDates.Add(callability_[i].date());
                  
                  if (callability_[i].price().type() == Callability.Price.Type.Clean)
                     moreArgs.callabilityPrices.Add(callability_[i].price().amount() + bond_.accruedAmount(callability_[i].date()));
                  else
                     moreArgs.callabilityPrices.Add(callability_[i].price().amount());

                   SoftCallability softCall = callability_[i] as SoftCallability;
                   if (softCall != null )
                       moreArgs.callabilityTriggers.Add(softCall.trigger());
                   else
                       moreArgs.callabilityTriggers.Add(null);
               }
           }

           List<CashFlow> cashflows = bond_.cashflows();

           moreArgs.couponDates.Clear();
           moreArgs.couponAmounts.Clear();
           for (int i=0; i<cashflows.Count-1; i++) 
           {
               if (!cashflows[i].hasOccurred(settlement, false)) 
               {
                   moreArgs.couponDates.Add(cashflows[i].date());
                   moreArgs.couponAmounts.Add(cashflows[i].amount());
               }
           }

           moreArgs.dividends.Clear();
           moreArgs.dividendDates.Clear();
           for (int i=0; i<dividends_.Count; i++) 
           {
               if (!dividends_[i].hasOccurred(settlement, false)) 
               {
                   moreArgs.dividends.Add(dividends_[i]);
                   moreArgs.dividendDates.Add(dividends_[i].date());
               }
           }

           moreArgs.creditSpread = creditSpread_;
           moreArgs.issueDate = issueDate_;
           moreArgs.settlementDate = settlement;
           moreArgs.settlementDays = settlementDays_;
           moreArgs.redemption = redemption_;
        }
      
        private ConvertibleBond bond_;
        private double conversionRatio_;
        private CallabilitySchedule callability_;
        private DividendSchedule  dividends_;
        private Handle<Quote> creditSpread_;
        private List<CashFlow> cashflows_;
        private DayCounter dayCounter_;
        private Date issueDate_;
        private Schedule schedule_;
        private int settlementDays_;
        private double redemption_;
      }

      public double conversionRatio()  { return conversionRatio_; }
      public DividendSchedule dividends()  { return dividends_; }
      public CallabilitySchedule callability()  { return callability_; }
      public Handle<Quote> creditSpread()  { return creditSpread_; }
      
      protected ConvertibleBond( Exercise exercise,
                                 double conversionRatio,
                                 DividendSchedule dividends,
                                 CallabilitySchedule callability,
                                 Handle<Quote> creditSpread,
                                 Date issueDate,
                                 int settlementDays,
                                 Schedule schedule,
                                 double redemption)
         : base(settlementDays, schedule.calendar(), issueDate)
      {
         conversionRatio_ = conversionRatio;
         callability_ = callability;
         dividends_ = dividends;
         creditSpread_ = creditSpread;

         maturityDate_ = schedule.endDate();

         if (!callability.empty()) 
         {
            Utils.QL_REQUIRE( callability.Last().date() <= maturityDate_, () =>
                              "last callability date (" 
                              + callability.Last().date()
                              + ") later than maturity ("
                              + maturityDate_.ToShortDateString() + ")");
        }

         creditSpread.registerWith(update);
      }
      protected override void performCalculations()
      {
         option_.setPricingEngine(engine_);
         NPV_ = settlementValue_ = option_.NPV();
         errorEstimate_ = null;
      }

      protected double conversionRatio_;
      protected CallabilitySchedule callability_;
      protected DividendSchedule dividends_;
      protected Handle<Quote> creditSpread_;
      protected option option_;
   }

   //! convertible zero-coupon bond
   /*! \warning Most methods inherited from Bond (such as yield or
               the yield-based dirtyPrice and cleanPrice) refer to
               the underlying plain-vanilla bond and do not take
               convertibility and callability into account.
   */
   public class ConvertibleZeroCouponBond : ConvertibleBond 
   {
      public ConvertibleZeroCouponBond(Exercise exercise,
                                        double conversionRatio,
                                        DividendSchedule dividends,
                                        CallabilitySchedule callability,
                                        Handle<Quote> creditSpread,
                                        Date issueDate,
                                        int settlementDays,
                                        DayCounter dayCounter,
                                        Schedule schedule,
                                        double redemption = 100)
         : base(exercise, conversionRatio, dividends, callability, creditSpread, issueDate, settlementDays,schedule, redemption) 
      {

         cashflows_ = new List<CashFlow>();

        // !!! notional forcibly set to 100
        setSingleRedemption(100.0, redemption, maturityDate_);

        option_ = new option(this, exercise, conversionRatio, dividends, callability, creditSpread, cashflows_, dayCounter, schedule,
                             issueDate, settlementDays, redemption);
    }
    };  

   //! convertible fixed-coupon bond
    /*! \warning Most methods inherited from Bond (such as yield or
                 the yield-based dirtyPrice and cleanPrice) refer to
                 the underlying plain-vanilla bond and do not take
                 convertibility and callability into account.
    */
   public class ConvertibleFixedCouponBond : ConvertibleBond 
   {
      public ConvertibleFixedCouponBond( Exercise exercise,
                                         double conversionRatio,
                                         DividendSchedule dividends,
                                         CallabilitySchedule callability,
                                         Handle<Quote> creditSpread,
                                         Date issueDate,
                                         int settlementDays,
                                         List<double> coupons,
                                         DayCounter dayCounter,
                                         Schedule schedule,
                                         double redemption = 100)
         : base(exercise, conversionRatio, dividends, callability, creditSpread, issueDate, settlementDays, schedule, redemption) 
      {

        // !!! notional forcibly set to 100
        cashflows_ = new FixedRateLeg(schedule)
                           .withCouponRates(coupons, dayCounter)
                           .withNotionals(100.0)
                           .withPaymentAdjustment(schedule.businessDayConvention());

        addRedemptionsToCashflows(new List<double>(){redemption});

        Utils.QL_REQUIRE( redemptions_.Count == 1, () => "multiple redemptions created" );

        option_ = new option(this, exercise, conversionRatio, dividends, callability, creditSpread, cashflows_, dayCounter, schedule,
                             issueDate, settlementDays, redemption);
    }
    };

   //! convertible floating-rate bond
    /*! \warning Most methods inherited from Bond (such as yield or
                 the yield-based dirtyPrice and cleanPrice) refer to
                 the underlying plain-vanilla bond and do not take
                 convertibility and callability into account.
    */
    public class ConvertibleFloatingRateBond : ConvertibleBond 
    {
      public ConvertibleFloatingRateBond( Exercise exercise,
                                          double conversionRatio,
                                          DividendSchedule dividends,
                                          CallabilitySchedule callability,
                                          Handle<Quote> creditSpread,
                                          Date issueDate,
                                          int settlementDays,
                                          IborIndex index,
                                          int fixingDays,
                                          List<double> spreads,
                                          DayCounter dayCounter,
                                          Schedule schedule,
                                          double redemption = 100)
         : base(exercise, conversionRatio, dividends, callability, creditSpread, issueDate, settlementDays, schedule, redemption) 

      {
        // !!! notional forcibly set to 100
        cashflows_ = new IborLeg(schedule, index)
                        .withPaymentDayCounter(dayCounter)
                        .withFixingDays(fixingDays)
                        .withSpreads(spreads)
                        .withNotionals(100.0)
                        .withPaymentAdjustment(schedule.businessDayConvention());

        addRedemptionsToCashflows(new List<double>{redemption});

        Utils.QL_REQUIRE( redemptions_.Count == 1, () => "multiple redemptions created" );

        option_ = new option(this, exercise, conversionRatio, dividends, callability, creditSpread, cashflows_, dayCounter, schedule,
                             issueDate, settlementDays, redemption);
    
      }
    }
}
