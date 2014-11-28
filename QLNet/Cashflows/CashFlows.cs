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
using System.Reflection;
using Leg = System.Collections.Generic.List<QLNet.CashFlow>;


namespace QLNet 
{
   //! %cashflow-analysis functions
   public class CashFlows 
   {
      const double basisPoint_ = 1.0e-4;
      
      #region utility functions

      private static double aggregateRate(Leg leg,CashFlow cf)
      {
         if ( cf == null) return 0.0;

         Date paymentDate = cf.date();
         bool firstCouponFound = false;
         double nominal = 0.0;
         double accrualPeriod = 0.0;
         DayCounter dc = null;
         double result = 0.0;

         foreach (CashFlow x in leg.Where(x => x.date() == paymentDate))
         {
            Coupon cp = x as Coupon;
            if (cp != null)
            {
               if (firstCouponFound)
               {
                  Utils.QL_REQUIRE(nominal == cp.nominal() &&
                                   accrualPeriod == cp.accrualPeriod() &&
                                   dc == cp.dayCounter(), () =>
                                   "cannot aggregate two different coupons on "
                                   + paymentDate);
               }
               else
               {
                  firstCouponFound = true;
                  nominal = cp.nominal();
                  accrualPeriod = cp.accrualPeriod();
                  dc = cp.dayCounter();
               }
               result += cp.rate();
            }
         }

         Utils.QL_REQUIRE( firstCouponFound, () => "no coupon paid at cashflow date " + paymentDate );
         return result;
        }
      public static double simpleDuration(Leg leg,InterestRate y, bool includeSettlementDateFlows,
                                          Date settlementDate,Date npvDate) 
      {
         if (leg.empty())                
            return 0.0;

         if (settlementDate == null)
            settlementDate = Settings.evaluationDate();

         if (npvDate == null)
            npvDate = settlementDate;

         double P = 0.0;
         double dPdy = 0.0;
         double t = 0.0;
         Date lastDate = npvDate;
         Date refStartDate, refEndDate;

         DayCounter dc = y.dayCounter();
         for (int i=0; i<leg.Count; ++i) 
         {
            if (leg[i].hasOccurred(settlementDate, includeSettlementDateFlows))
               continue;

            double c = leg[i].amount();
				if (leg[i].tradingExCoupon(settlementDate))
				{
					c = 0.0;
				}
            Date couponDate = leg[i].date();
            Coupon coupon = leg[i] as Coupon;
            if (coupon != null) 
            {
               refStartDate = coupon.refPeriodStart;
               refEndDate = coupon.refPeriodEnd;
            } 
            else 
            {
               if (lastDate == npvDate) 
               {
                  // we don't have a previous coupon date,
                  // so we fake it
                  refStartDate = couponDate - new Period(1,TimeUnit.Years);
               } 
               else  
               {
                  refStartDate = lastDate;
               }
               refEndDate = couponDate;
            }

            t += dc.yearFraction(lastDate, couponDate, refStartDate, refEndDate);
            double B = y.discountFactor(t);
            P += c * B;
            dPdy += t * c * B;
                
            lastDate = couponDate;
         }
            
         if (P == 0.0) // no cashflows
            return 0.0;
         return dPdy/P;
      }
      public static double modifiedDuration(Leg leg,InterestRate y, bool includeSettlementDateFlows,
                                            Date settlementDate,Date npvDate) 
      {
         if (leg.empty())
            return 0.0;

         if (settlementDate == null)
            settlementDate = Settings.evaluationDate();

         if (npvDate == null)
            npvDate = settlementDate;

         double P = 0.0;
         double t = 0.0;
         double dPdy = 0.0;
         double r = y.rate();
         int N = (int)y.frequency();
         Date lastDate = npvDate;
         Date refStartDate, refEndDate;
         DayCounter dc = y.dayCounter();

         for (int i=0; i<leg.Count; ++i) 
         {
            if (leg[i].hasOccurred(settlementDate, includeSettlementDateFlows))
               continue;

            double c = leg[i].amount();
				if (leg[i].tradingExCoupon(settlementDate))
				{
					c = 0.0;
				}
            Date couponDate = leg[i].date();
            Coupon coupon = leg[i] as Coupon;
            if (coupon != null) 
            {
               refStartDate = coupon.refPeriodStart;
               refEndDate = coupon.refPeriodEnd;
            } 
            else 
            {
               if (lastDate == npvDate) 
               {
                  // we don't have a previous coupon date,
                  // so we fake it
                  refStartDate = couponDate - new Period(1,TimeUnit.Years);
               } 
               else  
               {
                  refStartDate = lastDate;
               }
               refEndDate = couponDate;
            }
                
            t += dc.yearFraction(lastDate, couponDate, refStartDate, refEndDate);
                
            double B = y.discountFactor(t);
            P += c * B;
            switch (y.compounding()) 
            {
               case Compounding.Simple:
                  dPdy -= c * B*B * t;
                  break;
               case Compounding.Compounded:
                  dPdy -= c * t * B/(1+r/N);
                  break;
               case Compounding.Continuous:
                  dPdy -= c * B * t;
                  break;
               case Compounding.SimpleThenCompounded:
                  if (t<=1.0/N)
                     dPdy -= c * B*B * t;
                  else
                     dPdy -= c * t * B/(1+r/N);
                  break;
               default:
                  Utils.QL_FAIL("unknown compounding convention (" + y.compounding() + ")");
                  break;
            }
            lastDate = couponDate;
         }

         if (P == 0.0) // no cashflows
            return 0.0;
         return -dPdy/P; // reverse derivative sign
      }

      public static double macaulayDuration(Leg leg,InterestRate y, bool includeSettlementDateFlows,
                                            Date settlementDate, Date npvDate) 
      {
         Utils.QL_REQUIRE( y.compounding() == Compounding.Compounded, () => "compounded rate required" );

         return (1.0+y.rate()/(int)y.frequency()) *
               modifiedDuration(leg, y, includeSettlementDateFlows, settlementDate, npvDate);
      }

      #endregion

      #region Helper Classes
        
      class IrrFinder : ISolver1d 
      {
         private Leg leg_;
         private double npv_;
         private DayCounter dayCounter_;
         private Compounding compounding_;
         private Frequency frequency_;
         private bool includeSettlementDateFlows_;
         private Date settlementDate_, npvDate_;

         public IrrFinder(Leg leg, double npv,DayCounter dayCounter,Compounding comp,Frequency freq,
                          bool includeSettlementDateFlows,Date settlementDate,Date npvDate)
         {
            leg_ = leg; 
            npv_ = npv;
            dayCounter_ = dayCounter;
            compounding_= comp;
            frequency_ = freq;
            includeSettlementDateFlows_ = includeSettlementDateFlows;
            settlementDate_=settlementDate;
            npvDate_=npvDate;

            if (settlementDate == null)
               settlementDate = Settings.evaluationDate();

            if (npvDate == null)
               npvDate = settlementDate;

            checkSign();
         }
      
         public override double value(double y) 
         {
            InterestRate yield = new InterestRate(y, dayCounter_, compounding_, frequency_);
            double NPV = CashFlows.npv(leg_, yield, includeSettlementDateFlows_, settlementDate_, npvDate_);
            return npv_ - NPV;
         }

         public override double derivative(double y) 
         {
            InterestRate yield = new InterestRate(y, dayCounter_, compounding_, frequency_);
                return modifiedDuration(leg_, yield,includeSettlementDateFlows_,settlementDate_, npvDate_);
         }

         private void checkSign() 
         {
            // depending on the sign of the market price, check that cash
            // flows of the opposite sign have been specified (otherwise
            // IRR is nonsensical.)

            int lastSign = Math.Sign(-npv_), signChanges = 0;
            for (int i = 0; i < leg_.Count; ++i) 
            {
					if (!leg_[i].hasOccurred(settlementDate_, includeSettlementDateFlows_) &&
						 !leg_[i].tradingExCoupon(settlementDate_)) 
               {
                  int thisSign = Math.Sign(leg_[i].amount());
                  if (lastSign * thisSign < 0) // sign change
                        signChanges++;

                  if (thisSign != 0)
                        lastSign = thisSign;
               }
            }
            Utils.QL_REQUIRE( signChanges > 0, () =>
                     "the given cash flows cannot result in the given market " +
                     "price due to their sign");
         }
      }
      class ZSpreadFinder : ISolver1d
      {
         private Leg leg_;
         private double npv_;
         private SimpleQuote zSpread_;
         ZeroSpreadedTermStructure curve_;
         private bool includeSettlementDateFlows_;
         private Date settlementDate_, npvDate_;

         public ZSpreadFinder(Leg leg,YieldTermStructure discountCurve,double npv,DayCounter dc,Compounding comp,Frequency freq,
                              bool includeSettlementDateFlows, Date settlementDate, Date npvDate)
         {
            leg_ = leg;
            npv_ = npv;
            zSpread_ = new SimpleQuote(0.0);
            curve_ = new ZeroSpreadedTermStructure(new Handle<YieldTermStructure>(discountCurve),
                                                   new Handle<Quote>(zSpread_), comp, freq, dc);
            includeSettlementDateFlows_ = includeSettlementDateFlows;
            settlementDate_ = settlementDate;
            npvDate_ = npvDate;

            if (settlementDate == null)
               settlementDate = Settings.evaluationDate();

            if (npvDate == null)
               npvDate = settlementDate;

            // if the discount curve allows extrapolation, let's
            // the spreaded curve do too.
            curve_.enableExtrapolation(discountCurve.allowsExtrapolation());
         }

         public override double value(double zSpread)
         {
            zSpread_.setValue(zSpread);
            double NPV = CashFlows.npv(leg_, curve_, includeSettlementDateFlows_, settlementDate_, npvDate_);
                return npv_ - NPV;
         }

        
      }
      class BPSCalculator : IAcyclicVisitor 
      {
         private YieldTermStructure discountCurve_;
         double bps_, nonSensNPV_;

         public BPSCalculator(YieldTermStructure discountCurve)
         {
            discountCurve_ = discountCurve;
            nonSensNPV_ = 0.0;
            bps_ = 0.0;
         }

         #region IAcyclicVisitor pattern
         // visitor classes should implement the generic visit method in the following form
         public void visit(object o) 
         {
               Type[] types = new Type[] { o.GetType() };
               MethodInfo methodInfo = this.GetType().GetMethod("visit", types);
               if (methodInfo != null) {
                  methodInfo.Invoke(this, new object[] { o });
               }
         }
         public void visit(Coupon c) 
         {
            double bps = c.nominal() *
                           c.accrualPeriod() *
                           discountCurve_.discount(c.date());
            bps_ += bps;
         }
         public void visit(CashFlow cf) 
         {
            nonSensNPV_ += cf.amount() * discountCurve_.discount(cf.date());
         }
         #endregion

         public double bps() { return bps_; }
         public double nonSensNPV() { return nonSensNPV_; }
        }
      #endregion

      #region Date functions
      public static Date startDate(Leg leg)
      {
         Utils.QL_REQUIRE( !leg.empty(), () => "empty leg" );
         Date d = Date.maxDate();
         for (int i=0; i<leg.Count; ++i) 
         {
            Coupon c = leg[i] as Coupon;
            if (c != null)
                d = Date.Min(d, c.accrualStartDate());
            else
                d = Date.Min(d, leg[i].date());
         }
         return d;
      }
      public static Date maturityDate(Leg leg)
      {
         Utils.QL_REQUIRE( !leg.empty(), () => "empty leg" );
         Date d = Date.minDate();
         for (int i = 0; i < leg.Count; ++i)
         {
            Coupon c = leg[i] as Coupon;
            if (c != null)
               d = Date.Max(d, c.accrualEndDate());
            else
               d = Date.Max(d, leg[i].date());
         }
         return d;
      }
      public static bool isExpired(Leg leg, bool includeSettlementDateFlows,Date settlementDate = null)
      {
         if (leg.empty())
            return true;

         if (settlementDate == null)
            settlementDate = Settings.evaluationDate();

         for (int i = leg.Count; i > 0; --i)
            if (!leg[i - 1].hasOccurred(settlementDate,includeSettlementDateFlows))
               return false;
         return true;
      }
      #endregion

      #region CashFlow functions
      //! the last cashflow paying before or at the given date
      public static CashFlow previousCashFlow(Leg leg, bool includeSettlementDateFlows,Date settlementDate = null) 
      {
         if (leg.empty()) return null;

         Date d = (settlementDate == null ? Settings.evaluationDate() : settlementDate);
         return  leg.LastOrDefault(x => x.hasOccurred(d, includeSettlementDateFlows));
      }
      //! the first cashflow paying after the given date
      public static CashFlow nextCashFlow(Leg leg, bool includeSettlementDateFlows, Date settlementDate = null)
      {
         if (leg.empty()) return null;

         Date d = (settlementDate == null ? Settings.evaluationDate() : settlementDate);

         // the first coupon paying after d is the one we're after
         return leg.FirstOrDefault(x => !x.hasOccurred(d, includeSettlementDateFlows));
      }
      public static Date previousCashFlowDate(Leg leg, bool includeSettlementDateFlows,Date settlementDate = null) 
      {
        CashFlow cf = previousCashFlow(leg, includeSettlementDateFlows, settlementDate);

        if (cf == null)
            return null;

        return cf.date();
      }
      public static Date nextCashFlowDate(Leg leg,bool includeSettlementDateFlows,Date settlementDate = null) 
      {
         CashFlow cf = nextCashFlow(leg, includeSettlementDateFlows, settlementDate);
         if (cf == null) return null;
         return cf.date();
      }
      public static double? previousCashFlowAmount(Leg leg,bool includeSettlementDateFlows,Date settlementDate = null) 
      {
        
         CashFlow cf = previousCashFlow(leg, includeSettlementDateFlows, settlementDate);

         if (cf==null) return null;

         Date paymentDate = cf.date();
         double? result = 0.0;
         result = leg.Where(cf1 => cf1.date() == paymentDate).Sum(cf1 => cf1.amount());
         return result;

      }
      public static double? nextCashFlowAmount(Leg leg,bool includeSettlementDateFlows,Date settlementDate = null) 
      {
         CashFlow cf = nextCashFlow(leg, includeSettlementDateFlows, settlementDate);

         if (cf == null) return null;

         Date paymentDate = cf.date();
         double result = 0.0;
         result = leg.Where(cf1 => cf1.date() == paymentDate).Sum(cf1 => cf1.amount());
         return result;
      }
      #endregion

      #region Coupon inspectors
     
      public static double previousCouponRate(List<CashFlow> leg, bool includeSettlementDateFlows, Date settlementDate = null)
      {
         CashFlow cf = previousCashFlow(leg, includeSettlementDateFlows, settlementDate);
         return aggregateRate(leg, cf);
      }
      public static double nextCouponRate(List<CashFlow> leg, bool includeSettlementDateFlows, Date settlementDate = null)
      {
         CashFlow cf = nextCashFlow(leg, includeSettlementDateFlows, settlementDate);
         return aggregateRate(leg, cf);
      }
      public static double nominal(Leg leg, bool includeSettlementDateFlows,  Date settlementDate = null) 
      {
         CashFlow cf = nextCashFlow(leg, includeSettlementDateFlows, settlementDate);
         if (cf == null) return 0.0;

         Date paymentDate = cf.date();

         foreach (CashFlow x in leg.Where(x => x.date() == paymentDate))
         {
            Coupon cp = x as Coupon;
            if (cp != null)
               return cp.nominal();
         }
        return 0.0;
    }
      public static Date accrualStartDate(Leg leg,bool includeSettlementDateFlows,Date settlementDate = null) 
      {
        CashFlow cf = nextCashFlow(leg,includeSettlementDateFlows,settlementDate);
        if ( cf == null) return null;

        Date paymentDate = cf.date();

         foreach (CashFlow x in leg.Where(x => x.date() == paymentDate))
         {
            Coupon cp = x as Coupon;
            if (cp != null)
               return cp.accrualStartDate();
         }
         return null;
    }
      public static Date accrualEndDate(Leg leg,bool includeSettlementDateFlows,Date settlementDate = null) 
      {
         CashFlow cf = nextCashFlow(leg,includeSettlementDateFlows,settlementDate);
         if (cf == null) return null;

         Date paymentDate = cf.date();

         foreach (CashFlow x in leg.Where(x => x.date() == paymentDate))
         {
            Coupon cp = x as Coupon;
            if (cp != null)
               return cp.accrualEndDate();
         }
         return null;
      }
      public static Date referencePeriodStart(Leg leg, bool includeSettlementDateFlows,Date settlementDate= null) 
      {
         CashFlow cf = nextCashFlow(leg,includeSettlementDateFlows,settlementDate);
         if (cf == null) return null;
         Date paymentDate = cf.date();

         foreach (CashFlow x in leg.Where(x => x.date() == paymentDate))
         {
            Coupon cp = x as Coupon;
            if (cp != null)
               return cp.refPeriodStart;
         }
         return null;
      }
      public static Date referencePeriodEnd(Leg leg, bool includeSettlementDateFlows,Date settlementDate = null)
      {
         CashFlow cf = nextCashFlow(leg, includeSettlementDateFlows, settlementDate);
         if (cf == null) return null;
         Date paymentDate = cf.date();

         foreach (CashFlow x in leg.Where(x => x.date() == paymentDate))
         {
            Coupon cp = x as Coupon;
            if (cp != null)
               return cp.refPeriodEnd;
         }
         return null;
      }
      public static double accrualPeriod(Leg leg, bool includeSettlementDateFlows,Date settlementDate = null)
      {
         CashFlow cf = nextCashFlow(leg, includeSettlementDateFlows, settlementDate);
         if (cf == null) return 0;
         Date paymentDate = cf.date();

         foreach (CashFlow x in leg.Where(x => x.date() == paymentDate))
         {
            Coupon cp = x as Coupon;
            if (cp != null)
               return cp.accrualPeriod();
         }
         return 0;
      }
      public static int accrualDays(Leg leg, bool includeSettlementDateFlows,Date settlementDate = null)
      {
         CashFlow cf = nextCashFlow(leg, includeSettlementDateFlows, settlementDate);
         if (cf == null) return 0;
         Date paymentDate = cf.date();

         foreach (CashFlow x in leg.Where(x => x.date() == paymentDate))
         {
            Coupon cp = x as Coupon;
            if (cp != null)
               return cp.accrualDays();
         }
         return 0;
      }
      public static double accruedPeriod(Leg leg, bool includeSettlementDateFlows, Date settlementDate = null) 
      {
         if (settlementDate == null)
            settlementDate = Settings.evaluationDate();

         CashFlow cf = nextCashFlow(leg, includeSettlementDateFlows,  settlementDate);
         if (cf == null) return 0;

         Date paymentDate = cf.date();
         foreach (CashFlow x in leg.Where(x => x.date() == paymentDate))
         {
            Coupon cp = x as Coupon;
            if (cp != null)
               return cp.accruedPeriod(settlementDate);
         }
         return 0;
    }
      public static int accruedDays(Leg leg, bool includeSettlementDateFlows,Date settlementDate = null)
      {
			if ( settlementDate == null )
				settlementDate = Settings.evaluationDate();

         CashFlow cf = nextCashFlow(leg, includeSettlementDateFlows, settlementDate);
         if (cf == null) return 0;
         Date paymentDate = cf.date();

         foreach (CashFlow x in leg.Where(x => x.date() == paymentDate))
         {
            Coupon cp = x as Coupon;
            if (cp != null)
               return cp.accruedDays(settlementDate);
         }
         return 0;
      }
      public static double accruedAmount(Leg leg, bool includeSettlementDateFlows,Date settlementDate = null)
      {
			if ( settlementDate == null )
				settlementDate = Settings.evaluationDate();

         CashFlow cf = nextCashFlow(leg, includeSettlementDateFlows, settlementDate);
         if (cf == null) return 0;
         Date paymentDate = cf.date();
         double result = 0.0;

         foreach (CashFlow x in leg.Where(x => x.date() == paymentDate))
         {
            Coupon cp = x as Coupon;
            if (cp != null)
               result += cp.accruedAmount(settlementDate);
         }
         return result;
      }
      #endregion


      //// need to refactor the bond classes and remove the following  
      //public static Date previousCouponDate(List<CashFlow> leg, bool includeSettlementDateFlows,Date refDate)
      //{
      //   var cf = previousCashFlow(leg,includeSettlementDateFlows, refDate);
      //   if (cf == leg.Last()) return null;
      //   return cf.date();
      //}
      //public static Date nextCouponDate(List<CashFlow> leg, bool includeSettlementDateFlows,Date refDate)
      //{
      //   var cf = nextCashFlow(leg,includeSettlementDateFlows, refDate);
      //   if (cf == leg.Last()) return null;
      //   return cf.date();
      //}
      ////@}

      #region YieldTermStructure functions

      //! NPV of the cash flows. The NPV is the sum of the cash flows, each discounted according to the given term structure.
      public static double npv(Leg leg,YieldTermStructure discountCurve, bool includeSettlementDateFlows,
                               Date settlementDate = null, Date npvDate = null) 
      {

         if (leg.empty())
            return 0.0;

         if (settlementDate == null)
            settlementDate = Settings.evaluationDate();

         if (npvDate == null)
            npvDate = settlementDate;

         double totalNPV = 0.0;
         for (int i=0; i<leg.Count; ++i) 
         {
            if (!leg[i].hasOccurred(settlementDate, includeSettlementDateFlows) && !leg[i].tradingExCoupon(settlementDate))
               totalNPV += leg[i].amount() * discountCurve.discount(leg[i].date());
         }

         return totalNPV/discountCurve.discount(npvDate);
    }

      // Basis-point sensitivity of the cash flows.
      // The result is the change in NPV due to a uniform 1-basis-point change in the rate paid by the cash flows. The change for each coupon is discounted according to the given term structure.
      public static double bps(Leg leg, YieldTermStructure discountCurve, bool includeSettlementDateFlows,
                               Date settlementDate = null, Date npvDate = null)
      {
         if (leg.empty())
            return 0.0;

         if (settlementDate == null)
            settlementDate = Settings.evaluationDate();

         if (npvDate == null)
            npvDate = settlementDate;

         BPSCalculator calc = new BPSCalculator(discountCurve);
         for (int i = 0; i < leg.Count; ++i)
         {
				if (!leg[i].hasOccurred(settlementDate, includeSettlementDateFlows) &&
					 !leg[i].tradingExCoupon(settlementDate))
               leg[i].accept(calc);
         }
         return basisPoint_ * calc.bps() / discountCurve.discount(npvDate);
      }
      //! NPV and BPS of the cash flows.
      // The NPV and BPS of the cash flows calculated together for performance reason
      public static void npvbps(Leg leg,YieldTermStructure discountCurve, bool includeSettlementDateFlows,
                                Date settlementDate, Date npvDate, out double npv,out double bps) 
      {
         npv = 0.0;
         if (leg.empty())
         {
            bps = 0.0;
            return;
         }

         BPSCalculator calc = new BPSCalculator(discountCurve);
         for (int i=0; i<leg.Count; ++i) 
         {
            CashFlow cf = leg[i];
				if (!cf.hasOccurred(settlementDate, includeSettlementDateFlows) &&
					 !cf.tradingExCoupon(settlementDate)) 
            {
               npv += cf.amount() * discountCurve.discount(cf.date());
               cf.accept(calc);
            }
         }
         double d = discountCurve.discount(npvDate);
         npv /= d;
         bps = basisPoint_ * calc.bps() / d;
      }

      // At-the-money rate of the cash flows.
      // The result is the fixed rate for which a fixed rate cash flow  vector, equivalent to the input vector, has the required NPV according to the given term structure. If the required NPV is
      //  not given, the input cash flow vector's NPV is used instead.
      public static double atmRate(Leg leg,YieldTermStructure discountCurve, bool includeSettlementDateFlows,
                                   Date settlementDate = null, Date npvDate = null,double? targetNpv = null) 
      {

         if (settlementDate == null)
            settlementDate = Settings.evaluationDate();

         if (npvDate == null)
            npvDate = settlementDate;

         double npv = 0.0;
         BPSCalculator calc = new BPSCalculator(discountCurve);
         for (int i=0; i<leg.Count; ++i) 
         {
            CashFlow cf = leg[i];
				if (!cf.hasOccurred(settlementDate, includeSettlementDateFlows) &&
					 !cf.tradingExCoupon(settlementDate)) 
            {
               npv += cf.amount() * discountCurve.discount(cf.date());
               cf.accept(calc);
            }
         }

         if (targetNpv==null)
            targetNpv = npv - calc.nonSensNPV();
         else 
         {
            targetNpv *= discountCurve.discount(npvDate);
            targetNpv -= calc.nonSensNPV();
         }

         if (targetNpv==0.0)
            return 0.0;

         double bps = calc.bps();
         Utils.QL_REQUIRE( bps != 0.0, () => "null bps: impossible atm rate" );

         return targetNpv.Value/bps;
    }

      // NPV of the cash flows.
      // The NPV is the sum of the cash flows, each discounted
      // according to the given constant interest rate.  The result
      // is affected by the choice of the interest-rate compounding
      // and the relative frequency and day counter.
      public static double npv(Leg leg, InterestRate yield, bool includeSettlementDateFlows,
                               Date settlementDate = null, Date npvDate = null)
      {
         if (leg.empty())
            return 0.0;

         if (settlementDate == null)
            settlementDate = Settings.evaluationDate();

         if (npvDate == null)
            npvDate = settlementDate;

         double npv = 0.0;
         double discount = 1.0;
         Date lastDate = npvDate;
         Date refStartDate, refEndDate;

         for (int i=0; i<leg.Count; ++i) 
         {
            if (leg[i].hasOccurred(settlementDate, includeSettlementDateFlows))
                continue;

            Date couponDate = leg[i].date();
            double amount = leg[i].amount();
				if (leg[i].tradingExCoupon(settlementDate))
				{
					amount = 0.0;
				}
            Coupon coupon = leg[i] as Coupon;
            if (coupon != null ) 
            {
                refStartDate = coupon.refPeriodStart;
                refEndDate = coupon.refPeriodEnd;
            } 
            else 
            {
               if (lastDate == npvDate) 
               {
                  // we don't have a previous coupon date,
                  // so we fake it
                  refStartDate = couponDate - new Period(1,TimeUnit.Years);
                } 
               else  
               {
                  refStartDate = lastDate;
               }
               refEndDate = couponDate;
            }
            
            double b = yield.discountFactor(lastDate, couponDate, refStartDate, refEndDate);
            discount *= b;
            lastDate = couponDate;

            npv += amount * discount;
        }
        return npv;
      }
      public static double npv(Leg leg, double yield, DayCounter dayCounter, Compounding compounding, Frequency frequency,
                               bool includeSettlementDateFlows, Date settlementDate = null, Date npvDate = null)
      {
         return npv(leg, new InterestRate(yield, dayCounter, compounding, frequency),
                    includeSettlementDateFlows, settlementDate, npvDate);
      }

      //! Basis-point sensitivity of the cash flows.
      // The result is the change in NPV due to a uniform
      // 1-basis-point change in the rate paid by the cash
      // flows. The change for each coupon is discounted according
      // to the given constant interest rate.  The result is
      // affected by the choice of the interest-rate compounding
      // and the relative frequency and day counter.

      public static double bps(Leg leg, InterestRate yield, bool includeSettlementDateFlows,
                               Date settlementDate = null, Date npvDate = null)
      {
         if (leg.empty())
            return 0.0;

         if (settlementDate == null)
            settlementDate = Settings.evaluationDate();

         if (npvDate == null)
            npvDate = settlementDate;

         FlatForward flatRate = new FlatForward(settlementDate, yield.rate(), yield.dayCounter(),
                                               yield.compounding(), yield.frequency());
         return bps(leg, flatRate, includeSettlementDateFlows, settlementDate, npvDate);
      }

      public static double bps(Leg leg, double yield, DayCounter dayCounter, Compounding compounding, Frequency frequency,
                               bool includeSettlementDateFlows, Date settlementDate = null, Date npvDate = null)
      {
         return bps(leg, new InterestRate(yield, dayCounter, compounding, frequency),
                    includeSettlementDateFlows,settlementDate, npvDate);
      }

      //! NPV of a single cash flows 
      public static double npv(CashFlow cashflow, YieldTermStructure discountCurve,
                               Date settlementDate = null, Date npvDate = null, int exDividendDays = 0)
      {
         double NPV = 0.0;

         if (cashflow == null)
            return 0.0;

         if (settlementDate == null)
            settlementDate = Settings.evaluationDate();

         if (npvDate == null)
            npvDate = settlementDate;

         if (!cashflow.hasOccurred(settlementDate + exDividendDays))
            NPV = cashflow.amount() * discountCurve.discount(cashflow.date());

         
         return NPV / discountCurve.discount(npvDate);
      }


      //! CASH of the cash flows. The CASH is the sum of the cash flows.
      public static double cash(List<CashFlow> cashflows, Date settlementDate = null, int exDividendDays = 0 )
      {
         if (cashflows.Count == 0)
            return 0.0;

         if (settlementDate == null)
            settlementDate = Settings.evaluationDate();

         double totalCASH = cashflows.Where(x => !x.hasOccurred(settlementDate + exDividendDays)).
            Sum(c => c.amount());

         return totalCASH;
      }

      //! Implied internal rate of return.
      // The function verifies
      // the theoretical existance of an IRR and numerically
      // establishes the IRR to the desired precision.
      public static double yield(Leg leg, double npv, DayCounter dayCounter, Compounding compounding, Frequency frequency,
                                 bool includeSettlementDateFlows, Date settlementDate = null, Date npvDate = null,
                                 double accuracy = 1.0e-10, int maxIterations = 100, double guess = 0.05)
      {
        NewtonSafe solver = new NewtonSafe();
        solver.setMaxEvaluations(maxIterations);
        IrrFinder objFunction = new IrrFinder(leg, npv,
                              dayCounter, compounding, frequency,
                              includeSettlementDateFlows,
                              settlementDate, npvDate);
        return solver.solve(objFunction, accuracy, guess, guess/10.0);
      }

      //! Cash-flow duration.
      /*! The simple duration of a string of cash flows is defined as
         \f[
         D_{\mathrm{simple}} = \frac{\sum t_i c_i B(t_i)}{\sum c_i B(t_i)}
         \f]
         where \f$ c_i \f$ is the amount of the \f$ i \f$-th cash
         flow, \f$ t_i \f$ is its payment time, and \f$ B(t_i) \f$ is the corresponding discount according to the passed yield.

         The modified duration is defined as
         \f[
         D_{\mathrm{modified}} = -\frac{1}{P} \frac{\partial P}{\partial y}
         \f]
         where \f$ P \f$ is the present value of the cash flows according to the given IRR \f$ y \f$.

         The Macaulay duration is defined for a compounded IRR as
         \f[
         D_{\mathrm{Macaulay}} = \left( 1 + \frac{y}{N} \right)
                                 D_{\mathrm{modified}}
         \f]
         where \f$ y \f$ is the IRR and \f$ N \f$ is the number of cash flows per year.
   
      */
      public static double duration(Leg leg, InterestRate rate, Duration.Type type, bool includeSettlementDateFlows,
                                    Date settlementDate = null, Date npvDate = null)
      {
         if (leg.empty())
            return 0.0;

         if (settlementDate == null)
            settlementDate = Settings.evaluationDate();

         if (npvDate == null)
            npvDate = settlementDate;

        switch (type) 
        {
            case Duration.Type.Simple:
               return simpleDuration(leg, rate, includeSettlementDateFlows, settlementDate, npvDate);
            case Duration.Type.Modified:
               return modifiedDuration(leg, rate, includeSettlementDateFlows, settlementDate, npvDate);
            case Duration.Type.Macaulay:
               return macaulayDuration(leg, rate, includeSettlementDateFlows, settlementDate, npvDate);
            default:
               Utils.QL_FAIL("unknown duration type");
               break;
         }
         return 0.0;
      }

      public static double duration(Leg leg, double yield, DayCounter dayCounter, Compounding compounding, Frequency frequency,
                                    Duration.Type type, bool includeSettlementDateFlows, Date settlementDate = null,
                                    Date npvDate = null)
      {
         return duration(leg, new InterestRate(yield, dayCounter, compounding, frequency),
                         type, includeSettlementDateFlows,   settlementDate, npvDate);
      }

      //! Cash-flow convexity
      /*! The convexity of a string of cash flows is defined as
          \f[
          C = \frac{1}{P} \frac{\partial^2 P}{\partial y^2}
          \f]
          where \f$ P \f$ is the present value of the cash flows
          according to the given IRR \f$ y \f$.
      */
      public static double convexity(Leg leg, InterestRate yield, bool includeSettlementDateFlows,
                                     Date settlementDate = null, Date npvDate = null)
      {
         if (leg.empty())
            return 0.0;

         if (settlementDate == null)
            settlementDate = Settings.evaluationDate();

         if (npvDate == null)
            npvDate = settlementDate;

         DayCounter dc = yield.dayCounter();

         double P = 0.0;
         double t = 0.0;
         double d2Pdy2 = 0.0;
         double r = yield.rate();
         int N = (int)yield.frequency();
         Date lastDate = npvDate;
         Date refStartDate, refEndDate;

         for (int i=0; i<leg.Count; ++i) 
         {
            if (leg[i].hasOccurred(settlementDate,includeSettlementDateFlows))
               continue;
            
            double c = leg[i].amount();
				if (leg[i].tradingExCoupon(settlementDate))
				{
					c = 0.0;
				}
            Date couponDate = leg[i].date();
            Coupon coupon = leg[i] as Coupon;
            if (coupon != null ) 
            {
                refStartDate = coupon.refPeriodStart;
                refEndDate = coupon.refPeriodEnd;
            } 
            else 
            {
               if (lastDate == npvDate) 
               {
                  // we don't have a previous coupon date,
                  // so we fake it
                  refStartDate = couponDate - new Period(1,TimeUnit.Years);
               } 
               else  
               {
                   refStartDate = lastDate;
               }
               refEndDate = couponDate;
            }
            
            t += dc.yearFraction(lastDate, couponDate,
                                 refStartDate, refEndDate);
            
            double B = yield.discountFactor(t);
            P += c * B;
            switch (yield.compounding()) 
            {
              case  Compounding.Simple:
                d2Pdy2 += c * 2.0*B*B*B*t*t;
                break;
              case Compounding.Compounded:
                d2Pdy2 += c * B*t*(N*t+1)/(N*(1+r/N)*(1+r/N));
                break;
              case Compounding.Continuous:
                d2Pdy2 += c * B*t*t;
                break;
              case Compounding.SimpleThenCompounded:
                if (t<=1.0/N)
                    d2Pdy2 += c * 2.0*B*B*B*t*t;
                else
                    d2Pdy2 += c * B*t*(N*t+1)/(N*(1+r/N)*(1+r/N));
                break;
              default:
                Utils.QL_FAIL("unknown compounding convention (" + yield.compounding() + ")");
                break;
            }
            lastDate = couponDate;
        }

        if (P == 0.0)
            // no cashflows
            return 0.0;

        return d2Pdy2/P;
      }

      public static double convexity(Leg leg, double yield, DayCounter dayCounter, Compounding compounding, Frequency frequency,
                                     bool includeSettlementDateFlows, Date settlementDate = null, Date npvDate = null)
      {
         return convexity(leg, new InterestRate(yield, dayCounter, compounding, frequency),
                          includeSettlementDateFlows, settlementDate, npvDate);
      }
      
      //! Basis-point value
      /*! Obtained by setting dy = 0.0001 in the 2nd-order Taylor
          series expansion.
      */
      public static double basisPointValue(Leg leg,InterestRate yield,bool includeSettlementDateFlows,
                                           Date settlementDate = null,Date npvDate = null)
      {
         if (leg.empty())
            return 0.0;

         if (settlementDate == null)
            settlementDate = Settings.evaluationDate();

         if (npvDate == null)
            npvDate = settlementDate;

         double npv = CashFlows.npv(leg, yield,includeSettlementDateFlows,settlementDate, npvDate);
         double modifiedDuration = CashFlows.duration(leg, yield, Duration.Type.Modified,includeSettlementDateFlows,
                                                      settlementDate, npvDate);
         double convexity = CashFlows.convexity(leg, yield, includeSettlementDateFlows, settlementDate, npvDate);
         double delta = -modifiedDuration*npv;
         double gamma = (convexity/100.0)*npv;

         double shift = 0.0001;
         delta *= shift;
         gamma *= shift*shift;

         return delta + 0.5*gamma;
      }
      public static double basisPointValue(Leg leg, double yield, DayCounter dayCounter, Compounding compounding, Frequency frequency,
                                           bool includeSettlementDateFlows, Date settlementDate = null, Date npvDate = null)
      {
         return basisPointValue(leg, new InterestRate(yield, dayCounter, compounding, frequency),
                                includeSettlementDateFlows, settlementDate, npvDate);
      }
      
      //! Yield value of a basis point
      /*! The yield value of a one basis point change in price is
          the derivative of the yield with respect to the price
          multiplied by 0.01
      */
      public static double yieldValueBasisPoint(Leg leg, InterestRate yield, bool includeSettlementDateFlows,
                                                Date settlementDate = null, Date npvDate = null)
      {
         if (leg.empty())
            return 0.0;

         if (settlementDate == null)
            settlementDate = Settings.evaluationDate();

         if (npvDate == null)
            npvDate = settlementDate;

         double npv = CashFlows.npv(leg, yield, includeSettlementDateFlows, settlementDate, npvDate);
         double modifiedDuration = CashFlows.duration(leg, yield, Duration.Type.Modified, includeSettlementDateFlows,
                                                      settlementDate, npvDate);

         double shift = 0.01;
         return (1.0/(-npv*modifiedDuration))*shift;
      }

      public static double yieldValueBasisPoint(Leg leg, double yield, DayCounter dayCounter, Compounding compounding,
                                                Frequency frequency, bool includeSettlementDateFlows, Date settlementDate = null,
                                                Date npvDate = null)
      {
         return yieldValueBasisPoint(leg, new InterestRate(yield, dayCounter, compounding, frequency),
                                     includeSettlementDateFlows, settlementDate, npvDate);
      }
      #endregion

      #region  Z-spread utility functions

      // NPV of the cash flows.
      //  The NPV is the sum of the cash flows, each discounted
      //  according to the z-spreaded term structure.  The result
      //  is affected by the choice of the z-spread compounding
      //  and the relative frequency and day counter.
      public static double npv(Leg leg,YieldTermStructure discountCurve,double zSpread,DayCounter dc,Compounding comp,
                               Frequency freq,bool includeSettlementDateFlows,
                               Date settlementDate = null,Date npvDate = null) 
      {
         if (leg.empty())
            return 0.0;

         if (settlementDate == null)
            settlementDate = Settings.evaluationDate();

         if (npvDate == null)
            npvDate = settlementDate;

         Handle<YieldTermStructure> discountCurveHandle = new Handle<YieldTermStructure>(discountCurve);
         Handle<Quote> zSpreadQuoteHandle = new Handle<Quote>(new SimpleQuote(zSpread));

         ZeroSpreadedTermStructure spreadedCurve = new ZeroSpreadedTermStructure(discountCurveHandle,zSpreadQuoteHandle,
            comp, freq, dc);

         spreadedCurve.enableExtrapolation(discountCurveHandle.link.allowsExtrapolation());

         return npv(leg, spreadedCurve, includeSettlementDateFlows, settlementDate, npvDate);
      }
      //! implied Z-spread.
      public static double zSpread(Leg leg, double npv, YieldTermStructure discount, DayCounter dayCounter, Compounding compounding,
                                   Frequency frequency, bool includeSettlementDateFlows, Date settlementDate = null,
                                   Date npvDate = null, double accuracy = 1.0e-10, int maxIterations = 100, double guess = 0.0)
      {
         if (settlementDate == null)
            settlementDate = Settings.evaluationDate();

         if (npvDate == null)
            npvDate = settlementDate;

         Brent solver = new Brent();
         solver.setMaxEvaluations(maxIterations);
         ZSpreadFinder objFunction = new ZSpreadFinder(leg,discount,npv,dayCounter, compounding, frequency, 
            includeSettlementDateFlows, settlementDate, npvDate);
         double step = 0.01;
         return solver.solve(objFunction, accuracy, guess, step);
      }
      //! deprecated implied Z-spread.
      public static double zSpread(Leg leg, YieldTermStructure d, double npv, DayCounter dayCounter, Compounding compounding,
                                   Frequency frequency, bool includeSettlementDateFlows, Date settlementDate = null,
                                   Date npvDate = null, double accuracy = 1.0e-10, int maxIterations = 100,
                                   double guess = 0.0)
      {
         return zSpread(leg, npv, d, dayCounter, compounding, frequency,
                        includeSettlementDateFlows, settlementDate, npvDate,
                        accuracy, maxIterations, guess);
      }
      #endregion
   }


}
