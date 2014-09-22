/*
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
using System.Text;

namespace QLNet
{
   public enum AmortizingMethod
   {
       EffectiveInterestRate
   }

   public class AmortizingBond : Bond
   {
         
      public AmortizingBond(double FaceValue,
                            double MarketValue,
                            double CouponRate,
                            Date IssueDate,
                            Date MaturityDate,
                            Date TradeDate,
                            Frequency payFrequency,
                            DayCounter dCounter,
                            AmortizingMethod Method,
                            Calendar calendar,
                            double gYield = 0) :
         base(0, new TARGET(), IssueDate)
      {
         _faceValue = FaceValue;
         _marketValue = MarketValue;
         _couponRate = CouponRate;
         _issueDate = IssueDate;
         _maturityDate = MaturityDate;
         _tradeDate = TradeDate;
         _payFrequency = payFrequency;
         _dCounter = dCounter;
         _method = Method;
         _calendar = calendar;
         _isPremium = _marketValue > _faceValue;

         // Store regular payment of faceValue * couponRate for later calculation
         _originalPayment = (_faceValue * _couponRate) / (double)_payFrequency;

         if (gYield == 0)
            _yield = calculateYield();
         else
            _yield = gYield;

         // We can have several method here 
         //  Straight-Line Amortization , Effective Interest Rate, Rule 78
         // for now we start with Effective Interest Rate.
         switch ( _method )
         {
            case AmortizingMethod.EffectiveInterestRate:
               addEffectiveInterestRateAmortizing();
               break;
            default:
               break;
         }

      }

      public bool isPremium()
      {
         return _isPremium;
      }

      void addEffectiveInterestRateAmortizing()
      {

         // Amortizing Schedule
         Schedule schedule = new Schedule(_tradeDate, _maturityDate, new Period(_payFrequency),
                                           _calendar, BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                           DateGeneration.Rule.Backward, false);
         double currentNominal = _marketValue;
         Date prevDate = _tradeDate;
         Date actualDate = _tradeDate;

         for (int i = 1; i < schedule.Count; ++i)
         {

            actualDate = schedule[i];
            InterestRate rate = new InterestRate(_yield, _dCounter, Compounding.Simple, Frequency.Annual);
            InterestRate rate2 = new InterestRate(_couponRate, _dCounter, Compounding.Simple, Frequency.Annual);
            FixedRateCoupon r,r2;
            if (i > 1)
            {
               r = new FixedRateCoupon(currentNominal, actualDate, rate, prevDate, actualDate, prevDate, actualDate);
               r2 = new FixedRateCoupon(currentNominal, actualDate, rate2, prevDate, actualDate, prevDate, actualDate, null,_originalPayment);
            }

            else
            {
               Calendar nullCalendar = new NullCalendar();
               Period p1 = new Period(_payFrequency);
               Date testDate = nullCalendar.advance(actualDate, -1 * p1);

               r = new FixedRateCoupon(currentNominal, actualDate, rate, testDate, actualDate, prevDate, actualDate);
               r2 = new FixedRateCoupon(currentNominal, actualDate, rate2, testDate, actualDate, prevDate, actualDate, null,_originalPayment);
            }

            double amort = Math.Round(Math.Abs(_originalPayment - r.amount()),2);
          
            AmortizingPayment p = new AmortizingPayment(amort, actualDate);
            if (_isPremium)
               currentNominal -= Math.Abs(amort);
            else
               currentNominal += Math.Abs(amort);


            cashflows_.Add(r2);
            cashflows_.Add(p);
            prevDate = actualDate;
         }

         // Add single redemption for yield calculation
         setSingleRedemption(_faceValue, 100, _maturityDate);

      }

      public double AmortizationValue(Date d)
      {
         // Check Date
         if (d < _tradeDate || d > _maturityDate)
            return 0;

         double totAmortized = 0;
         Date lastDate = _tradeDate;
         foreach (CashFlow c in cashflows_)
         {
            if ( c.date() <= d )
            {
               lastDate = c.date();
               if ( c is QLNet.AmortizingPayment )
                  totAmortized += (c as QLNet.AmortizingPayment).amount();
            }
            else
               break;
         }


         if (lastDate < d )
         {
            // lastDate < d let calculate last interest

            // Base Interest
            InterestRate r1 = new InterestRate(_couponRate,_dCounter,Compounding.Simple,_payFrequency);
            FixedRateCoupon c1 = new FixedRateCoupon(_faceValue,d,r1,lastDate,d);
            double baseInterest = c1.amount();

            // 
            InterestRate r2 = new InterestRate(_yield,_dCounter,Compounding.Simple,_payFrequency);
            FixedRateCoupon c2 = new FixedRateCoupon(_marketValue,d,r2,lastDate,d);
            double yieldInterest = c2.amount();

            totAmortized += Math.Abs(baseInterest - yieldInterest);
         }


         if (_isPremium)
               return (_marketValue - totAmortized);
            else
               return (_marketValue  + totAmortized);

      }

      public double Yield() { return _yield; }

      private double calculateYield()
      {
         // We create a bond cashflow from issue to maturity just
         // to calculate effective rate ( the rate that discount _marketValue )
         Schedule schedule = new Schedule(_issueDate, _maturityDate, new Period(_payFrequency),
                                           _calendar, BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                                           DateGeneration.Rule.Backward, false);


         List<CashFlow> cashflows = new FixedRateLeg(schedule)
            .withCouponRates(_couponRate, _dCounter)
            .withPaymentCalendar(_calendar)
            .withNotionals(_faceValue)
            .withPaymentAdjustment(BusinessDayConvention.Unadjusted);

         // Add single redemption for yield calculation
         Redemption r = new Redemption(_faceValue , _maturityDate);
         cashflows.Add( r );

         // Calculate Amortizing Yield ( Effective Rate )
         Date testDate = CashFlows.previousCashFlowDate(cashflows, false, _tradeDate);
         return CashFlows.yield(cashflows, _marketValue, _dCounter, Compounding.Simple, _payFrequency,
                                  false, testDate);
      }

      // temporary testing function
      private double calculateYield2()
      {
         double CapitalGain = _faceValue - _marketValue;
         double YearToMaturity = _maturityDate.year() - _tradeDate.year();
         double AnnualizedCapitalGain = CapitalGain / YearToMaturity;
         double AnnualInterest = _couponRate * _faceValue;
         double TotalAnnualizedReturn = AnnualizedCapitalGain + AnnualInterest;
         double yieldA = TotalAnnualizedReturn / _marketValue;
         double yieldB = TotalAnnualizedReturn / (_faceValue - AnnualizedCapitalGain);
         return (yieldA + yieldB) / 2;
      }

      protected double _faceValue;
      protected double _marketValue;
      protected double _couponRate;
      protected Date _issueDate;
      protected Date _maturityDate;
      protected Date _tradeDate;
      protected Frequency _payFrequency;
      protected DayCounter _dCounter;
      protected AmortizingMethod _method;
      protected Calendar _calendar;
      protected double _yield;
      protected double _originalPayment;
      protected bool _isPremium;

   }
}
