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
   //! Callable bond base class
   /*! Base callable bond class for fixed and zero coupon bonds.
       Defines commonalities between fixed and zero coupon callable
       bonds. At present, only European and Bermudan put/call schedules
       supported (no American optionality), as defined by the Callability
       class.

       \todo models/shortrate/calibrationHelpers
       \todo OAS/OAD
       \todo floating rate callable bonds ?

       \ingroup instruments
   */
   public class CallableBond : Bond
   {
      public new class Arguments : Bond.Arguments 
      {
         public Arguments() {}
         public List<Date> couponDates;
         public List<double> couponAmounts;
         //! redemption = face amount * redemption / 100.
         public double redemption;
         public Date redemptionDate;
         public DayCounter paymentDayCounter;
         public Frequency frequency;
         public CallabilitySchedule putCallSchedule;
         //! bond full/dirty/cash prices
         public List<double> callabilityPrices;
         public List<Date> callabilityDates;
         public override void validate()
         {
            Utils.QL_REQUIRE( settlementDate != null, () => "null settlement date" );
            
            //Utils.QL_REQUIRE(redemption != null, "null redemption");
            Utils.QL_REQUIRE( redemption >= 0.0, () => "positive redemption required: " + redemption + " not allowed" );

            Utils.QL_REQUIRE( callabilityDates.Count == callabilityPrices.Count, () => "different number of callability dates and prices" );
            Utils.QL_REQUIRE( couponDates.Count == couponAmounts.Count, () => "different number of coupon dates and amounts" );
         }
    }
      //! results for a callable bond calculation
      public new class Results : Bond.Results 
      {
         // no extra results set yet
      }
      //! base class for callable fixed rate bond engine
      public new class Engine :  GenericEngine<CallableBond.Arguments,CallableBond.Results> {};

      //! \name Inspectors
      //@{
      //! return the bond's put/call schedule
      public CallabilitySchedule callability() 
      {
         return putCallSchedule_;
      }
      //@}
      //! \name Calculations
      //@{
      //! returns the Black implied forward yield volatility
      /*! the forward yield volatility, see Hull, Fourth Edition,
         Chapter 20, pg 536). Relevant only to European put/call
         schedules
      */
      public double impliedVolatility( double targetValue,
                                       Handle<YieldTermStructure> discountCurve,
                                       double accuracy,
                                       int maxEvaluations,
                                       double minVol,
                                       double maxVol) 
      {
         calculate();
         Utils.QL_REQUIRE( !isExpired(), () => "instrument expired" );
         double guess = 0.5*(minVol + maxVol);
         blackDiscountCurve_.linkTo(discountCurve, false);
         ImpliedVolHelper f = new ImpliedVolHelper(this,targetValue);
         Brent solver = new Brent();
         solver.setMaxEvaluations(maxEvaluations);
         return solver.solve(f, accuracy, guess, minVol, maxVol);
      }
      //@}
      public override void setupArguments(IPricingEngineArguments args) 
      {
         base.setupArguments(args);
      }

      protected CallableBond( int settlementDays,
                              Schedule schedule,
                              DayCounter paymentDayCounter,
                              Date issueDate = null,
                              CallabilitySchedule putCallSchedule = null)
         : base(settlementDays, schedule.calendar(), issueDate)
      {
         if (putCallSchedule == null)
            putCallSchedule = new CallabilitySchedule();

         paymentDayCounter_ = paymentDayCounter;
         putCallSchedule_ =putCallSchedule;
         maturityDate_ = schedule.dates().Last();

        if (!putCallSchedule_.empty()) 
        {
            Date finalOptionDate = Date.minDate();
            for (int i=0; i<putCallSchedule_.Count;++i) 
            {
                finalOptionDate=Date.Max(finalOptionDate,
                                         putCallSchedule_[i].date());
            }
            Utils.QL_REQUIRE( finalOptionDate <= maturityDate_, () => "Bond cannot mature before last call/put date" );
        }

        // derived classes must set cashflows_ and frequency_
      }

      protected DayCounter paymentDayCounter_;
      protected Frequency frequency_;
      protected CallabilitySchedule putCallSchedule_;
      //! must be set by derived classes for impliedVolatility() to work
      protected IPricingEngine blackEngine_;
      //! Black fwd yield volatility quote handle to internal blackEngine_
      protected RelinkableHandle<Quote> blackVolQuote_ = new RelinkableHandle<Quote>();
      //! Black fwd yield volatility quote handle to internal blackEngine_
      protected RelinkableHandle<YieldTermStructure> blackDiscountCurve_ = new RelinkableHandle<YieldTermStructure>();
      //! helper class for Black implied volatility calculation
      protected class ImpliedVolHelper : ISolver1d
      {
         public ImpliedVolHelper(CallableBond bond, double targetValue)
         {
            targetValue_ = targetValue;
            vol_ = new SimpleQuote(0.0);
            bond.blackVolQuote_.linkTo(vol_);

            Utils.QL_REQUIRE( bond.blackEngine_ != null, () => "Must set blackEngine_ to use impliedVolatility" );

           engine_ = bond.blackEngine_;
           bond.setupArguments(engine_.getArguments());
           results_ = engine_.getResults() as Instrument.Results;
         }
         //double operator()(double x);
         public override double value(double x)
         {
            vol_.setValue(x);
            engine_.calculate(); // get the Black NPV based on vol x
            return results_.value.Value - targetValue_;
         }
         private IPricingEngine engine_;
         private double targetValue_;
         private SimpleQuote vol_;
         private Instrument.Results results_;
      }
   }

   //! callable/puttable fixed rate bond
   /*! Callable fixed rate bond class.

      \ingroup instruments

      <b> Example: </b>
      \link CallableBonds.cpp
      \endlink
   */
   public class CallableFixedRateBond : CallableBond
   {
      public CallableFixedRateBond( int settlementDays,
                                    double faceAmount,
                                    Schedule schedule,
                                    List<double> coupons,
                                    DayCounter accrualDayCounter,
                                    BusinessDayConvention paymentConvention = BusinessDayConvention.Following,
                                    double redemption = 100.0,
                                    Date issueDate = null,
                                    CallabilitySchedule putCallSchedule = null)
         :base(settlementDays, schedule, accrualDayCounter, issueDate, putCallSchedule)
      {
         
         if ( putCallSchedule == null )
            putCallSchedule = new CallabilitySchedule();
          
         frequency_ = schedule.tenor().frequency();

         bool isZeroCouponBond = (coupons.Count == 1 && Utils.close(coupons[0], 0.0));

        if (!isZeroCouponBond) 
        {
            cashflows_ = new FixedRateLeg(schedule)
                            .withCouponRates(coupons, accrualDayCounter)
                            .withNotionals(faceAmount)
                            .withPaymentAdjustment(paymentConvention);

            addRedemptionsToCashflows(new List<double>(){redemption});
        } 
        else 
        {
            Date redemptionDate = calendar_.adjust(maturityDate_, paymentConvention);
            setSingleRedemption(faceAmount, redemption, redemptionDate);
        }

        // used for impliedVolatility() calculation
        SimpleQuote dummyVolQuote = new SimpleQuote(0.0);
        blackVolQuote_.linkTo(dummyVolQuote);
        blackEngine_ = new BlackCallableFixedRateBondEngine(blackVolQuote_, blackDiscountCurve_);
      }

      public override void setupArguments(IPricingEngineArguments args)
      {
         base.setupArguments(args);
         CallableBond.Arguments arguments = args as CallableBond.Arguments;

         Utils.QL_REQUIRE( arguments != null, () => "no arguments given" );

         Date settlement = arguments.settlementDate;

         arguments.redemption = redemption().amount();
         arguments.redemptionDate = redemption().date();

         List<CashFlow> cfs = cashflows();

         arguments.couponDates = new List<Date>(cfs.Count - 1);
         //arguments.couponDates.Capacity = ;
         arguments.couponAmounts = new List<double>(cfs.Count - 1);
         //arguments.couponAmounts.Capacity = cfs.Count - 1;

         for (int i = 0; i < cfs.Count ; i++)
         {
            if (!cfs[i].hasOccurred(settlement, false))
            {
               if (cfs[i] is QLNet.FixedRateCoupon)
               {
                  arguments.couponDates.Add(cfs[i].date());
                  arguments.couponAmounts.Add(cfs[i].amount());
               }
            }
         }

         arguments.callabilityPrices = new List<double>(putCallSchedule_.Count);
         arguments.callabilityDates = new List<Date>(putCallSchedule_.Count);
         //arguments.callabilityPrices.Capacity = putCallSchedule_.Count;
         //arguments.callabilityDates.Capacity = putCallSchedule_.Count;

         arguments.paymentDayCounter = paymentDayCounter_;
         arguments.frequency = frequency_;

         arguments.putCallSchedule = putCallSchedule_;
         for (int i = 0; i < putCallSchedule_.Count; i++)
         {
            if (!putCallSchedule_[i].hasOccurred(settlement, false))
            {
               arguments.callabilityDates.Add(putCallSchedule_[i].date());
               arguments.callabilityPrices.Add(putCallSchedule_[i].price().amount());

               if (putCallSchedule_[i].price().type() == Callability.Price.Type.Clean)
               {
                  /* calling accrued() forces accrued interest to be zero
                     if future option date is also coupon date, so that dirty
                     price = clean price. Use here because callability is
                     always applied before coupon in the tree engine.
                  */
                  arguments.callabilityPrices[arguments.callabilityPrices.Count - 1] += this.accrued(putCallSchedule_[i].date());
               }
            }
         }
      }
      //! accrued interest used internally, where includeToday = false
      /*! same as Bond::accruedAmount() but with enable early
         payments true.  Forces accrued to be calculated in a
         consistent way for future put/ call dates, which can be
         problematic in lattice engines when option dates are also
         coupon dates.
      */
      private double accrued(Date settlement)
      {
         if (settlement == null) settlement = settlementDate();

         bool IncludeToday = false;
         for (int i = 0; i<cashflows_.Count; ++i) 
         {
            // the first coupon paying after d is the one we're after
            if (!cashflows_[i].hasOccurred(settlement,IncludeToday)) 
            {
               Coupon coupon = cashflows_[i] as Coupon;
               if (coupon != null)
                  // !!!
                  return coupon.accruedAmount(settlement) /
                     notional(settlement) * 100.0;
               else
                  return 0.0;
            }
         }
         return 0.0;
      }
   }

   //! callable/puttable zero coupon bond
   /*! Callable zero coupon bond class.

       \ingroup instruments
   */
   public class CallableZeroCouponBond : CallableFixedRateBond
   {
      public CallableZeroCouponBond(int settlementDays,
                                    double faceAmount,
                                    Calendar calendar,
                                    Date maturityDate,
                                    DayCounter dayCounter,
                                    BusinessDayConvention paymentConvention = BusinessDayConvention.Following,
                                    double redemption = 100.0,
                                    Date issueDate = null,
                                    CallabilitySchedule putCallSchedule = null)
         :base(settlementDays,faceAmount, new Schedule(issueDate, maturityDate,
                                                       new Period(Frequency.Once),
                                                       calendar,
                                                       paymentConvention,
                                                       paymentConvention,
                                                       DateGeneration.Rule.Backward,
                                                       false), 
               new List<double>(){0.0}, dayCounter, paymentConvention, redemption, issueDate, putCallSchedule)
      {
         if (putCallSchedule == null)
            putCallSchedule = new CallabilitySchedule();
      }
   }
}
