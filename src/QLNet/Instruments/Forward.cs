/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)

 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

 QLNet is free software: you can redistribute it and/or modify it
 under the terms of the QLNet license.  You should have received a
 copy of the license along with this program; if not, license is
 available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.

 QLNet is a based on QuantLib, a free-software/open-source library
 for financial quantitative analysts and developers - http://quantlib.org/
 The QuantLib license is available online at http://quantlib.org/license.shtml.

 This program is distributed in the hope that it will be useful, but WITHOUT
 ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 FOR A PARTICULAR PURPOSE.  See the license for more details.
*/

namespace QLNet
{
   /// <summary>
   /// Abstract base forward class
   /// </summary>
   /// <remarks>
   /// Derived classes must implement the virtual functions spotValue() (NPV or spot price) and spotIncome() associated
   /// with the specific relevant underlying (e.g. bond, stock, commodity, loan/deposit). These functions must be used to set the
   /// protected member variables underlyingSpotValue_ and underlyingIncome_ within performCalculations() in the derived
   /// class before the base-class implementation is called.
   ///
   /// spotIncome() refers generically to the present value of coupons, dividends or storage costs.
   ///
   /// discountCurve_ is the curve used to discount forward contract cash flows back to the evaluation day, as well as to obtain
   /// forward values for spot values/prices.
   ///
   /// incomeDiscountCurve_, which for generality is not automatically set to the discountCurve_, is the curve used to
   /// discount future income/dividends/storage-costs etc back to the evaluation date.
   ///
   /// TODO: Add preconditions and tests
   ///
   /// Warning: This class still needs to be rigorously tested
   /// </remarks>
   public abstract class Forward : Instrument
   {
      // Derived classes must set this, typically via spotIncome().
      protected double underlyingIncome_;
      // Derived classes must set this, typically via spotValue().
      protected double underlyingSpotValue_;

      protected DayCounter dayCounter_;
      protected Calendar calendar_;
      protected BusinessDayConvention businessDayConvention_;
      protected int settlementDays_;
      protected Payoff payoff_;
      // valueDate is the settlement date, i.e. the date the forward contract starts accruing.
      protected Date valueDate_;
      // maturityDate of the forward contract or delivery date of the underlying.
      protected Date maturityDate_;
      protected Handle<YieldTermStructure> discountCurve_;
      // Must be set in derived classes based on the particular underlying.
      protected Handle<YieldTermStructure> incomeDiscountCurve_;

      protected Forward(DayCounter dayCounter, Calendar calendar, BusinessDayConvention businessDayConvention,
                        int settlementDays, Payoff payoff, Date valueDate, Date maturityDate,
                        Handle<YieldTermStructure> discountCurve)
      {
         dayCounter_ = dayCounter;
         calendar_ = calendar;
         businessDayConvention_ = businessDayConvention;
         settlementDays_ = settlementDays;
         payoff_ = payoff;
         valueDate_ = valueDate;
         maturityDate_ = maturityDate;
         discountCurve_ = discountCurve;

         maturityDate_ = calendar_.adjust(maturityDate_, businessDayConvention_);

         Settings.registerWith(update);
         discountCurve_.registerWith(update);
      }

      public virtual Date settlementDate()
      {
         Date d = calendar_.advance(Settings.evaluationDate(), settlementDays_, TimeUnit.Days);
         return Date.Max(d, valueDate_);
      }

      public override bool isExpired()
      {
         return new simple_event(maturityDate_).hasOccurred(settlementDate());
      }


      /// <summary>
      /// Returns the spot value or spot price of the underlying financial instrument.
      /// </summary>
      public abstract double spotValue();
      /// <summary>
      /// Returns the present value of income, dividends, storage costs, and similar carry of the underlying instrument.
      /// </summary>
      public abstract double spotIncome(Handle<YieldTermStructure> incomeDiscountCurve);

      // Calculations
      /// <summary>
      /// forward value/price of underlying, discounting income/dividends
      /// </summary>
      /// <remarks>
      /// Note: if this is a bond forward price, is must be a dirty
      /// forward price.
      /// </remarks>
      public virtual double forwardValue()
      {
         calculate();
         return (underlyingSpotValue_ - underlyingIncome_) / discountCurve_.link.discount(maturityDate_);
      }

      /// <summary>
      /// Calculates a simple implied yield from the spot and forward values.
      /// </summary>
      /// <remarks>
      /// The calculation takes the underlying income into account. When <c>t &gt; 0</c>, call with <c>underlyingSpotValue = spotValue(t)</c> and <c>forwardValue = strikePrice</c> to obtain the current yield. For repos with <c>t = 0</c>, this should reproduce the spot repo rate; for FRAs, it should reproduce the relevant zero rate at the FRA maturity.
      /// </remarks>
      public InterestRate impliedYield(double underlyingSpotValue, double forwardValue, Date settlementDate,
                                       Compounding compoundingConvention, DayCounter dayCounter)
      {

         double tenor = dayCounter.yearFraction(settlementDate, maturityDate_) ;
         double compoundingFactor = forwardValue / (underlyingSpotValue - spotIncome(incomeDiscountCurve_)) ;
         return InterestRate.impliedRate(compoundingFactor, dayCounter, compoundingConvention, Frequency.Annual, tenor);
      }

      protected override void performCalculations()
      {
         Utils.QL_REQUIRE(!discountCurve_.empty(), () => "no discounting term structure set to Forward");

         ForwardTypePayoff ftpayoff = payoff_ as ForwardTypePayoff;
         double fwdValue = forwardValue();
         NPV_ = ftpayoff.value(fwdValue) * discountCurve_.link.discount(maturityDate_);
      }
   }

   /// <summary>
   /// Class for forward type payoffs
   /// </summary>
   public class ForwardTypePayoff : Payoff
   {
      protected Position.Type type_;
      public Position.Type forwardType() { return type_; }

      protected double strike_;
      public double strike() { return strike_; }

      public ForwardTypePayoff(Position.Type type, double strike)
      {
         type_ = type;
         strike_ = strike;
         Utils.QL_REQUIRE(strike >= 0.0, () => "negative strike given");
      }

      // Payoff interface
      public override string name() { return "Forward";}
      public override string description()
      {
         string result = name() + ", " + strike() + " strike";
         return result;
      }
      public override double value(double price)
      {
         switch (type_)
         {
            case Position.Type.Long:
               return (price - strike_);
            case Position.Type.Short:
               return (strike_ - price);
            default:
               Utils.QL_FAIL("unknown/illegal position type");
               return 0;
         }
      }
   }
}
