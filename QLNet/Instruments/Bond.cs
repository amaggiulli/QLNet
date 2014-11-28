/*
 Copyright (C) 2008, 2009 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2013 Andrea Maggiulli (a.maggiulli@gmail.com)
 
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

namespace QLNet
{
   //! Base bond class
   /*! Derived classes must fill the unitialized data members.

       \warning Most methods assume that the cashflows are stored sorted by date, the redemption being the last one.

       \test
       - price/yield calculations are cross-checked for consistency.
       - price/yield calculations are checked against known good values. */
   public class Bond : Instrument
   {
      #region Constructors
      //! constructor for amortizing or non-amortizing bonds.
      /*! Redemptions and maturity are calculated from the coupon
          data, if available.  Therefore, redemptions must not be
          included in the passed cash flows.
      */
      public Bond(int settlementDays, Calendar calendar, Date issueDate = null, List<CashFlow> coupons = null)
      {
         settlementDays_ = settlementDays;
         calendar_ = calendar;
         if (coupons == null)
            cashflows_ = new List<CashFlow>();
         else
            cashflows_ = coupons;
         issueDate_ = issueDate;

         if (cashflows_.Count != 0)
         {
            cashflows_.Sort();
            if (issueDate_ != null)
            {
               Utils.QL_REQUIRE( issueDate_ < cashflows_[0].date(), () =>
                           "issue date (" + issueDate_ +
                           ") must be earlier than first payment date (" +
                           cashflows_[0].date() + ")");
            }
            maturityDate_ = coupons.Last().date();
            addRedemptionsToCashflows();
         }

         Settings.registerWith(update);
      }

      //! old constructor for non amortizing bonds.
      /*! \warning The last passed cash flow must be the bond
                   redemption. No other cash flow can have a date
                   later than the redemption date.
      */
      public Bond(int settlementDays, Calendar calendar, double faceAmount, Date maturityDate, Date issueDate =null,
                  List<CashFlow> cashflows = null)
      {
         settlementDays_ = settlementDays;
         calendar_ = calendar;
         if (cashflows == null)
            cashflows_ = new List<CashFlow>();
         else
            cashflows_ = cashflows; 
         maturityDate_ = maturityDate;
         issueDate_ = issueDate;

         if (cashflows.Count != 0)
         {
            cashflows_.Sort(0, cashflows_.Count - 1, null);

            if (maturityDate_ ==null)
                maturityDate_ = CashFlows.maturityDate(cashflows);

            if (issueDate_ != null)
            {
               Utils.QL_REQUIRE( issueDate_ < cashflows_[0].date(), () =>
                          "issue date (" + issueDate_ +
                          ") must be earlier than first payment date (" +
                          cashflows_[0].date() + ")");
            }

            notionalSchedule_.Add(new Date());
            notionals_.Add(faceAmount);

            notionalSchedule_.Add(maturityDate_);
            notionals_.Add(0.0);

            redemptions_.Add(cashflows.Last());

            
         }

         Settings.registerWith(update);
      }
      #endregion

      #region Instrument interface

      public override bool isExpired()
      {
         // this is the Instrument interface, so it doesn't use
         // BondFunctions, and includeSettlementDateFlows is true
         // (unless QL_TODAY_PAYMENTS will set it to false later on)
         return CashFlows.isExpired(cashflows_, true, Settings.evaluationDate());
      }

      #endregion

      #region Inspectors
      
      public int settlementDays() { return settlementDays_; }
      public Calendar calendar() { return calendar_; }
      public List<double> notionals() { return notionals_; }
      public virtual double notional(Date d = null)
      {
         if (d == null)
            d = settlementDate();

         if (d > notionalSchedule_.Last())
            // after maturity
            return 0.0;

         // After the check above, d is between the schedule
         // boundaries.  We search starting from the second notional
         // date, since the first is null.  After the call to
         // lower_bound, *i is the earliest date which is greater or
         // equal than d.  Its index is greater or equal to 1.
         int index = notionalSchedule_.FindIndex(x => d <= x);

         if (d < notionalSchedule_[index])
         {
            // no doubt about what to return
            return notionals_[index - 1];
         }
         else
         {
            // d is equal to a redemption date.
            // As per bond conventions, the payment has occurred;
            // the bond already changed notional.
            return notionals_[index];
         }
      }
      // \note returns all the cashflows, including the redemptions.
      public List<CashFlow> cashflows() { return cashflows_; }
      //! returns just the redemption flows (not interest payments)
      public List<CashFlow> redemptions() { return redemptions_; }
      // returns the redemption, if only one is defined 
      public CashFlow redemption()
      {
         Utils.QL_REQUIRE( redemptions_.Count == 1, () => "multiple redemption cash flows given" );
         return redemptions_.Last();
      }
      public Date startDate() { return BondFunctions.startDate(this);}
      public Date maturityDate() { return (maturityDate_ != null) ? maturityDate_ : BondFunctions.maturityDate(this); }
      public Date issueDate() { return issueDate_; }
      public bool isTradable(Date d = null)  { return BondFunctions.isTradable(this, d); }
      public Date settlementDate(Date date = null)
      {
         Date d = (date == null ? Settings.evaluationDate() : date);

         // usually, the settlement is at T+n...
         Date settlement = calendar_.advance(d, settlementDays_, TimeUnit.Days);
         // ...but the bond won't be traded until the issue date (if given.)
         if (issueDate_ == null)
            return settlement;
         else
            return Date.Max(settlement, issueDate_);
      }

      #endregion

      #region Calculations
  
      //! theoretical clean price
      /*! The default bond settlement is used for calculation.

          \warning the theoretical price calculated from a flat term structure might differ slightly from the price
                   calculated from the corresponding yield by means of the other overload of this function. If the
                   price from a constant yield is desired, it is advisable to use such other overload. */
      public double cleanPrice() { return dirtyPrice() - accruedAmount(settlementDate()); }

      //! theoretical dirty price
      /*! The default bond settlement is used for calculation.

          \warning the theoretical price calculated from a flat term structure might differ slightly from the price
                   calculated from the corresponding yield by means of the other overload of this function. If the
                   price from a constant yield is desired, it is advisable to use such other overload.
      */
      public double dirtyPrice()
      {
         double currentNotional = notional(settlementDate());
         if (currentNotional == 0.0)
            return 0.0;
         else
            return settlementValue() * 100 / currentNotional ;
      }

      public double settlementValue()
      {
         calculate();
         Utils.QL_REQUIRE( settlementValue_ != null, () => "settlement value not provided" );
         return settlementValue_.Value;
      }

      public double settlementValue(double cleanPrice)
      {
         double dirtyPrice = cleanPrice + accruedAmount(settlementDate());
         return dirtyPrice / 100.0 * notional(settlementDate());
      }

      //! theoretical bond yield
      /*! The default bond settlement and theoretical price are used for calculation. */
      public double yield(DayCounter dc, Compounding comp, Frequency freq, double accuracy = 1.0e-8, int maxEvaluations = 100)
      {
         double currentNotional = notional(settlementDate());
         
         if (currentNotional == 0.0)
            return 0.0;

         return BondFunctions.yield(this, cleanPrice(), dc, comp, freq,settlementDate(), accuracy, maxEvaluations);
      }

      //! clean price given a yield and settlement date
      /*! The default bond settlement is used if no date is given. */
      public double cleanPrice(double yield, DayCounter dc, Compounding comp, Frequency freq, Date settlement = null)
      {
         return BondFunctions.cleanPrice(this, yield, dc, comp, freq, settlement);
      }

      //! dirty price given a yield and settlement date
      /*! The default bond settlement is used if no date is given. */
      public double dirtyPrice(double yield, DayCounter dc, Compounding comp, Frequency freq, Date settlement = null)
      {
         double currentNotional = notional(settlement);
         if (currentNotional == 0.0)
            return 0.0;

         return BondFunctions.cleanPrice(this, yield, dc, comp, freq, settlement) + accruedAmount(settlement);
      }

      //! yield given a (clean) price and settlement date
      /*! The default bond settlement is used if no date is given. */
      public double yield(double cleanPrice, DayCounter dc, Compounding comp, Frequency freq, Date settlement = null,
                          double accuracy = 1.0e-8, int maxEvaluations=100)
      {
         double currentNotional = notional(settlement);
         if (currentNotional == 0.0)
            return 0.0;

         return BondFunctions.yield(this, cleanPrice, dc, comp, freq, settlement, accuracy, maxEvaluations);
      }

      //! accrued amount at a given date
      /*! The default bond settlement is used if no date is given. */
      public virtual double accruedAmount(Date settlement = null)
      {
         double currentNotional = notional(settlement);
         
         if (currentNotional == 0.0)
            return 0.0;

         return BondFunctions.accruedAmount(this, settlement);

      }

      #endregion

      /*! Expected next coupon: depending on (the bond and) the given date
          the coupon can be historic, deterministic or expected in a
          stochastic sense. When the bond settlement date is used the coupon
          is the already-fixed not-yet-paid one.

          The current bond settlement is used if no date is given.
      */
      public virtual double nextCouponRate(Date settlement = null)
      {
         return BondFunctions.nextCouponRate(this, settlement);
      }

      //! Previous coupon already paid at a given date
      /*! Expected previous coupon: depending on (the bond and) the given
          date the coupon can be historic, deterministic or expected in a
          stochastic sense. When the bond settlement date is used the coupon
          is the last paid one.

          The current bond settlement is used if no date is given.
      */
      public double previousCouponRate(Date settlement = null)
      {
         return BondFunctions.previousCouponRate(this, settlement);
      }

      public Date nextCashFlowDate(Date settlement = null)
      {
         return BondFunctions.nextCashFlowDate(this, settlement);
      }

      public Date previousCashFlowDate(Date settlement = null)
      {
         return BondFunctions.previousCashFlowDate(this, settlement);
      }

      protected override void setupExpired()
      {
         base.setupExpired();
         settlementValue_ = 0.0;
      }

      public override void setupArguments(IPricingEngineArguments args)
      {
         Bond.Arguments arguments = args as Bond.Arguments;
         Utils.QL_REQUIRE( arguments != null, () => "wrong argument type" );

         arguments.settlementDate = settlementDate();
         arguments.cashflows = cashflows_;
         arguments.calendar = calendar_;
      }

      public override void fetchResults(IPricingEngineResults r)
      {
         base.fetchResults(r);

         Bond.Results results = r as Bond.Results;
         Utils.QL_REQUIRE( results != null, () => "wrong result type" );

         settlementValue_ = results.settlementValue;
      }

      /*! This method can be called by derived classes in order to
          build redemption payments from the existing cash flows.
          It must be called after setting up the cashflows_ vector
          and will fill the notionalSchedule_, notionals_, and
          redemptions_ data members.

          If given, the elements of the redemptions vector will
          multiply the amount of the redemption cash flow.  The
          elements will be taken in base 100, i.e., a redemption
          equal to 100 does not modify the amount.

          \pre The cashflows_ vector must contain at least one
               coupon and must be sorted by date.
      */
      protected void addRedemptionsToCashflows(List<double> redemptions = null)
      {
         if (redemptions == null)
            redemptions = new List<double>();

         // First, we gather the notional information from the cashflows
         calculateNotionalsFromCashflows();
         // Then, we create the redemptions based on the notional
         // information and we add them to the cashflows vector after
         // the coupons.
         redemptions_.Clear();
         for (int i = 1; i < notionalSchedule_.Count; ++i)
         {
            double R = i < redemptions.Count ? redemptions[i] :
                       !redemptions.empty() ? redemptions.Last() :
                                              100.0;
            double amount = (R / 100.0) * (notionals_[i - 1] - notionals_[i]);
            CashFlow payment;
            if (i < notionalSchedule_.Count - 1)
               payment = new AmortizingPayment(amount, notionalSchedule_[i]);
            else
               payment = new Redemption(amount, notionalSchedule_[i]);

            cashflows_.Add(payment);
            redemptions_.Add(payment);
         }
         // stable_sort now moves the redemptions to the right places
         // while ensuring that they follow coupons with the same date.
         cashflows_.Sort();
      }

      /*! This method can be called by derived classes in order to
          build a bond with a single redemption payment.  It will
          fill the notionalSchedule_, notionals_, and redemptions_
          data members.
      */
      protected void setSingleRedemption(double notional, double redemption, Date date)
      {
         CashFlow redemptionCashflow = new Redemption(notional * redemption / 100.0, date);
         setSingleRedemption(notional, redemptionCashflow);
      }

      protected void setSingleRedemption(double notional, CashFlow redemption)
      {
         notionals_.Clear();
         notionalSchedule_.Clear();
         redemptions_.Clear();

         notionalSchedule_.Add(new Date());
         notionals_.Add(notional);

         notionalSchedule_.Add(redemption.date());
         notionals_.Add(0.0);

         cashflows_.Add(redemption);
         redemptions_.Add(redemption);
      }

      /*! used internally to collect notional information from the
          coupons. It should not be called by derived classes,
          unless they already provide redemption cash flows (in
          which case they must set up the redemptions_ data member
          independently).  It will fill the notionalSchedule_ and
          notionals_ data members.
      */
      protected void calculateNotionalsFromCashflows()
      {
         notionalSchedule_.Clear();
         notionals_.Clear();

         Date lastPaymentDate = new Date();
         notionalSchedule_.Add(new Date());
         for (int i=0; i<cashflows_.Count; ++i) 
         {
            Coupon coupon = cashflows_[i] as Coupon;
            if (coupon == null)
                continue;

            double notional = coupon.nominal();
            // we add the notional only if it is the first one...
            if (notionals_.empty()) 
            {
               notionals_.Add(coupon.nominal());
               lastPaymentDate = coupon.date();
            } 
            else if (!Utils.close(notional, notionals_.Last())) 
            {
                // ...or if it has changed.
               Utils.QL_REQUIRE( notional < notionals_.Last(), () => "increasing coupon notionals" );
                notionals_.Add(coupon.nominal());
                // in this case, we also add the last valid date for
                // the previous one...
                notionalSchedule_.Add(lastPaymentDate);
                // ...and store the candidate for this one.
                lastPaymentDate = coupon.date();
            } 
            else 
            {
                // otherwise, we just extend the valid range of dates
                // for the current notional.
                lastPaymentDate = coupon.date();
            }
        
         }
         Utils.QL_REQUIRE( !notionals_.empty(), () => "no coupons provided" );
         notionals_.Add(0.0);
         notionalSchedule_.Add(lastPaymentDate);
      }

      #region properties

      protected int settlementDays_;
      protected Calendar calendar_;
      protected List<Date> notionalSchedule_ = new List<Date>();
      protected List<double> notionals_ = new List<double>();
      protected List<CashFlow> cashflows_; // all cashflows
      protected List<CashFlow> redemptions_ = new List<CashFlow>(); // the redemptions
      protected Date maturityDate_, issueDate_;
      protected double? settlementValue_;
      //public double faceAmount() { return notionals_.First(); }

      #endregion

      public class Engine : GenericEngine<Arguments, Results> { }

      public new class Results : Instrument.Results
      {
         public double? settlementValue;
         public override void reset()
         {
            settlementValue = null;
            base.reset();
         }
      }

      public class Arguments : IPricingEngineArguments
      {
         public Date settlementDate;
         public List<CashFlow> cashflows;
         public Calendar calendar;

         public virtual void validate()
         {
            Utils.QL_REQUIRE( settlementDate != null, () => "no settlement date provided" );
            Utils.QL_REQUIRE( !cashflows.empty(), () => "no cash flow provided" );
            for (int i = 0; i < cashflows.Count; ++i)
               Utils.QL_REQUIRE( cashflows[i] != null, () => "null cash flow provided" );
         }
      }
   }
}
