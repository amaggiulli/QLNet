/*
 Copyright (C) 2008, 2009 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008, 2009 , 2010  Andrea Maggiulli (a.maggiulli@gmail.com)
 
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

namespace QLNet 
{
   //! %cashflow-analysis functions
   public class CashFlows 
   {
      const double basisPoint_ = 1.0e-4;
      
      #region utility functions
      
      private static double couponRate(List<CashFlow> leg, CashFlow cf) 
      {
         if (cf == null) return 0.0;

         Date paymentDate = cf.date();
         bool firstCouponFound = false;
         double nominal = 0;
         double accrualPeriod = 0;
         DayCounter dc = null;
         double result = 0.0;

         foreach (CashFlow x in leg.Where(x => x.date() == paymentDate)) {
             Coupon cp = x as Coupon;
             if (cp != null) {
                 if (firstCouponFound) {
                     if (!(nominal == cp.nominal() && accrualPeriod == cp.accrualPeriod() && dc == cp.dayCounter()))
                         throw new ApplicationException("cannot aggregate two different coupons on " + paymentDate);
                 } else {
                     firstCouponFound = true;
                     nominal = cp.nominal();
                     accrualPeriod = cp.accrualPeriod();
                     dc = cp.dayCounter();
                 }
                 result += cp.rate();
             }
         }

         if (!firstCouponFound) throw new ApplicationException("next cashflow (" + paymentDate + ") is not a coupon");
         return result;
      }
      public static double simpleDuration(List<CashFlow> cashflows, InterestRate y, Date settlementDate) 
      {
         if (cashflows.Count == 0)
            return 0.0;

         double P = 0, dPdy = 0;
         DayCounter dc = y.dayCounter();
         foreach (CashFlow cf in cashflows.Where(cf => !cf.hasOccurred(settlementDate))) 
         {
            double t = dc.yearFraction(settlementDate, cf.date());
            double c = cf.amount();
            double B = y.discountFactor(t);
            P += c * B;
            dPdy += t * c * B;
         }


         // no cashflows
         if (P == 0.0) return 0.0;
         return dPdy / P;
      }
      public static double modifiedDuration(List<CashFlow> cashflows, InterestRate y, Date settlementDate) 
      {
         if (cashflows.Count == 0)
            return 0.0;

         double P = 0.0;
         double dPdy = 0.0;
         double r = y.rate();
         int N = (int)y.frequency();
         DayCounter dc = y.dayCounter();

         foreach (CashFlow cf in cashflows.Where(cf => !cf.hasOccurred(settlementDate))) 
         {

            double t = dc.yearFraction(settlementDate, cf.date());
            double c = cf.amount();
            double B = y.discountFactor(t);

            P += c * B;

            switch (y.compounding()) 
            {
               case Compounding.Simple:
                  dPdy -= c * B * B * t;
                  break;
                  
               case Compounding.Compounded:
                  dPdy -= c * t * B / (1 + r / N);
                  break;
                  
               case Compounding.Continuous:
                  dPdy -= c * B * t;
                  break;
                  
               case Compounding.SimpleThenCompounded:
                  if (t <= 1.0 / N)
                     dPdy -= c * B * B * t;
                  else
                     dPdy -= c * t * B / (1 + r / N);
                     break;
                    
               default:
                  throw new ArgumentException("unknown compounding convention (" + y.compounding() + ")");
            }
         }
         
         if (P == 0.0) // no cashflows
            return 0.0;
            
         return -dPdy / P; // reverse derivative sign
      }
      public static double macaulayDuration(List<CashFlow> cashflows, InterestRate y, Date settlementDate) 
      {
         if (y.compounding() != Compounding.Compounded) throw new ArgumentException("compounded rate required");
         return (1.0 + y.rate() / (int)y.frequency()) * modifiedDuration(cashflows, y, settlementDate);
      }
      
      #endregion

      #region helper classes
        class IrrFinder : ISolver1d {
            List<CashFlow> cashflows_;
            double marketPrice_;
            DayCounter dayCounter_;
            Compounding compounding_;
            Frequency frequency_;
            Date settlementDate_,npvDate_;
            bool includeSettlementDateFlows_;

            public IrrFinder(List<CashFlow> cashflows, double marketPrice, DayCounter dayCounter,
                             Compounding compounding, Frequency frequency, bool includeSettlementDateFlows,
                             Date settlementDate, Date npvDate)
            {
                cashflows_ = cashflows;
                marketPrice_ = marketPrice;
                dayCounter_ = dayCounter;
                compounding_ = compounding;
                frequency_ = frequency;
                settlementDate_ = settlementDate;
                npvDate_ = npvDate;
                includeSettlementDateFlows_ = includeSettlementDateFlows;

               if (settlementDate_ == null)
                settlementDate_ = Settings.evaluationDate();

                if (npvDate_ == null)
                    npvDate_ = settlementDate_;
            }

            public override double value(double x) {
                InterestRate y = new InterestRate(x, dayCounter_, compounding_, frequency_);
                double NPV = CashFlows.npv(cashflows_, y,
                                          includeSettlementDateFlows_,
                                          settlementDate_, npvDate_);
                return marketPrice_ - NPV;
            }
            public override double derivative(double x) {
                InterestRate y = new InterestRate(x, dayCounter_, compounding_, frequency_);
                return modifiedDuration(cashflows_, y, settlementDate_);
            }
        };


        class BPSCalculator : IAcyclicVisitor {
            private YieldTermStructure termStructure_;
            private Date npvDate_;
            private double result_;

            public BPSCalculator(YieldTermStructure termStructure, Date npvDate) {
                termStructure_ = termStructure;
                npvDate_ = npvDate;
                result_ = 0;
            }

            #region IAcyclicVisitor pattern
            // visitor classes should implement the generic visit method in the following form
            public void visit(object o) {
                Type[] types = new Type[] { o.GetType() };
                MethodInfo methodInfo = this.GetType().GetMethod("visit", types);
                if (methodInfo != null) {
                    methodInfo.Invoke(this, new object[] { o });
                }
            }
            public void visit(Coupon c) {
                result_ += c.accrualPeriod() * c.nominal() * termStructure_.discount(c.date());
            }
            #endregion

            public double result() {
                if (npvDate_ == null) return result_;
                else return result_ / termStructure_.discount(npvDate_);
            }
        }
        #endregion

      //! \name Date functions
      //@{
      public static Date startDate(List<CashFlow> cashflows)
      {
         if (cashflows.Count == 0) throw new ArgumentException("empty leg");
         return cashflows.Where(cf => cf is Coupon).Min(cf => ((Coupon)cf).accrualStartDate());
      }
      public static Date maturityDate(List<CashFlow> cashflows)
      {
         if (cashflows.Count == 0) throw new ArgumentException("empty leg");
         return cashflows.Where(cf => cf is Coupon).Max(cf => ((Coupon)cf).accrualEndDate());
      }
      public static bool isExpired(List<CashFlow> cashflows, bool includeSettlementDateFlows,Date settlementDate = null)
      {
         if (cashflows.Count == 0)
            return true;

         if (settlementDate == null)
            settlementDate = Settings.evaluationDate();

         for (int i = cashflows.Count; i > 0; --i)
            if (!cashflows[i - 1].hasOccurred(settlementDate,includeSettlementDateFlows))
               return false;
         return true;
      }
      //@}

      //! \name CashFlow functions
      //@{
      //! the last cashflow paying before or at the given date
      public static CashFlow previousCashFlow(List<CashFlow> leg, bool includeSettlementDateFlows,Date settlementDate = null) 
      {
         if (leg.Count == 0) return null;

         Date d = (settlementDate == null ? Settings.evaluationDate() : settlementDate);
         return  leg.FindLast(x => x.hasOccurred(d, includeSettlementDateFlows));
      }
      //! the first cashflow paying after the given date
      public static CashFlow nextCashFlow(List<CashFlow> leg, bool includeSettlementDateFlows, Date settlementDate = null)
      {
         if (leg.Count == 0) return null;

         Date d = (settlementDate == null ? Settings.evaluationDate() : settlementDate);

         // the first coupon paying after d is the one we're after
         return leg.Find(x => !x.hasOccurred(d, includeSettlementDateFlows));
      }

      public static Date previousCashFlowDate(List<CashFlow> leg, bool includeSettlementDateFlows,Date settlementDate = null) 
      {
        CashFlow cf = previousCashFlow(leg, includeSettlementDateFlows, settlementDate);

        if (cf == null)
            return null;

        return cf.date();
      }
      public static Date nextCashFlowDate(List<CashFlow> leg,bool includeSettlementDateFlows,Date settlementDate = null) 
      {
         CashFlow cf = nextCashFlow(leg, includeSettlementDateFlows, settlementDate);
         if (cf == null) return null;
         return cf.date();
      }
      public static double? previousCashFlowAmount(List<CashFlow> leg,bool includeSettlementDateFlows,Date settlementDate = null) 
      {
        
         CashFlow cf = previousCashFlow(leg, includeSettlementDateFlows, settlementDate);

         if (cf==null) return null;

         Date paymentDate = cf.date();
         double? result = 0.0;
         result = leg.Where(cf1 => cf1.date() == paymentDate).Sum(cf1 => cf1.amount());
         return result;

      }
      public static double? nextCashFlowAmount(List<CashFlow>leg,bool includeSettlementDateFlows,Date settlementDate = null) 
      {
         CashFlow cf = nextCashFlow(leg, includeSettlementDateFlows, settlementDate);

         if (cf == null) return null;

         Date paymentDate = cf.date();
         double result = 0.0;
         result = leg.Where(cf1 => cf1.date() == paymentDate).Sum(cf1 => cf1.amount());
         return result;
      }
      //@}

      //! \name Coupon inspectors
      //@{
      public static double previousCouponRate(List<CashFlow> leg, bool includeSettlementDateFlows, Date refDate = null)
      {
         CashFlow cf = previousCashFlow(leg,includeSettlementDateFlows, refDate);
         return couponRate(leg, cf);
      }
      public static double nextCouponRate(List<CashFlow> leg,bool includeSettlementDateFlows, Date refDate = null)
      {
         CashFlow cf = nextCashFlow(leg,includeSettlementDateFlows, refDate);
         return couponRate(leg, cf);
      }
      public static Date accrualStartDate(List<CashFlow> leg,bool includeSettlementDateFlows,Date settlementDate = null) 
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
      public static Date accrualEndDate(List<CashFlow> leg,bool includeSettlementDateFlows,Date settlementDate = null) 
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
      public static Date referencePeriodStart(List<CashFlow> leg, bool includeSettlementDateFlows,Date settlementDate= null) 
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
      public static Date referencePeriodEnd(List<CashFlow> leg, bool includeSettlementDateFlows,Date settlementDate = null)
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
      public static double accrualPeriod(List<CashFlow> leg, bool includeSettlementDateFlows,Date settlementDate = null)
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
      public static int accrualDays(List<CashFlow> leg, bool includeSettlementDateFlows,Date settlementDate = null)
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
      public static int accruedDays(List<CashFlow> leg, bool includeSettlementDateFlows,Date settlementDate = null)
      {
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
      public static double accruedAmount(List<CashFlow> leg, bool includeSettlementDateFlows,Date settlementDate = null)
      {
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
      //@}
      // need to refactor the bond classes and remove the following  
      public static Date previousCouponDate(List<CashFlow> leg, bool includeSettlementDateFlows,Date refDate)
      {
         var cf = previousCashFlow(leg,includeSettlementDateFlows, refDate);
         if (cf == leg.Last()) return null;
         return cf.date();
      }
      public static Date nextCouponDate(List<CashFlow> leg, bool includeSettlementDateFlows,Date refDate)
      {
         var cf = nextCashFlow(leg,includeSettlementDateFlows, refDate);
         if (cf == leg.Last()) return null;
         return cf.date();
      }
      //@}

      //! NPV of the cash flows. The NPV is the sum of the cash flows, each discounted according to the given term structure.
      public static double npv(List<CashFlow> cashflows, YieldTermStructure discountCurve, 
                               Date settlementDate = null,Date npvDate = null, int exDividendDays = 0) 
      {
         if (cashflows.Count == 0)
            return 0.0;

         if (settlementDate == null)
            settlementDate = discountCurve.referenceDate();

         double totalNPV = cashflows.Where(x => !x.hasOccurred(settlementDate + exDividendDays)).
                                        Sum(c => c.amount() * discountCurve.discount(c.date()));

         if (npvDate == null) 
            return totalNPV;
         else 
            return totalNPV / discountCurve.discount(npvDate);
      }

      //! NPV of a single cash flows 
      public static double npv(CashFlow cashflow, YieldTermStructure discountCurve,
                               Date settlementDate = null, Date npvDate = null, int exDividendDays = 0)
      {
         double NPV = 0.0;

         if (cashflow == null)
            return 0.0;

         if (settlementDate == null)
            settlementDate = discountCurve.referenceDate();

         if (!cashflow.hasOccurred(settlementDate + exDividendDays))
            NPV = cashflow.amount() * discountCurve.discount(cashflow.date());

         if (npvDate == null)
            return NPV;
         else
            return NPV / discountCurve.discount(npvDate);
      }

      // NPV of the cash flows.
        // The NPV is the sum of the cash flows, each discounted according to the given constant interest rate.  The result
        // is affected by the choice of the interest-rate compounding and the relative frequency and day counter.
      public static double npv(List<CashFlow> cashflows, InterestRate r, Date settlementDate = null) 
        {
        
           if (settlementDate == null)
              settlementDate = Settings.evaluationDate();

           FlatForward flatRate = new FlatForward(settlementDate, r.rate(), r.dayCounter(), r.compounding(), r.frequency());
           return npv(cashflows, flatRate, settlementDate, settlementDate);
        }

      public static double npv(List<CashFlow> leg,InterestRate y,bool includeSettlementDateFlows, 
         Date settlementDate = null , Date npvDate = null) 
      {

        if (leg == null || leg.empty())
            return 0.0;

        if (settlementDate ==null)
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
            Coupon coupon = leg[i] as Coupon;
            if (coupon != null ) 
            {
                refStartDate = coupon.accrualStartDate();
                refEndDate = coupon.accrualEndDate();
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
            double b = y.discountFactor(lastDate, couponDate, refStartDate, refEndDate);
            discount *= b;
            lastDate = couponDate;

            npv += amount * discount;
        }

        return npv;
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

      // Basis-point sensitivity of the cash flows.
      // The result is the change in NPV due to a uniform 1-basis-point change in the rate paid by the cash flows. The change for each coupon is discounted according to the given term structure.
      public static double bps(List<CashFlow> cashflows, YieldTermStructure discountCurve,
                               Date settlementDate = null, Date npvDate = null, int exDividendDays = 0 ) 
      {
         if (cashflows.Count == 0)
            return 0.0;

         if (settlementDate == null)
            settlementDate = discountCurve.referenceDate();

         BPSCalculator calc = new BPSCalculator(discountCurve, npvDate);
         for (int i = 0; i < cashflows.Count; i++)
            if (!cashflows[i].hasOccurred(settlementDate + exDividendDays))
               cashflows[i].accept(calc);

         return basisPoint_ * calc.result();
      }

      // Basis-point sensitivity of the cash flows.
        // The result is the change in NPV due to a uniform 1-basis-point change in the rate paid by the cash flows. The change for each coupon is discounted according
        //  to the given constant interest rate.  The result is affected by the choice of the interest-rate compounding and the relative frequency and day counter.
      public static double bps(List<CashFlow> cashflows, InterestRate irr, Date settlementDate = null) {
            if (settlementDate == null)
                settlementDate = Settings.evaluationDate();
            var flatRate = new FlatForward(settlementDate, irr.rate(), irr.dayCounter(), irr.compounding(), irr.frequency());
            return bps(cashflows, flatRate, settlementDate, settlementDate);
        }

      // At-the-money rate of the cash flows.
        // The result is the fixed rate for which a fixed rate cash flow  vector, equivalent to the input vector, has the required NPV according to the given term structure. If the required NPV is
        //  not given, the input cash flow vector's NPV is used instead.
      public static double atmRate(List<CashFlow> cashflows, YieldTermStructure discountCurve,
                                   Date settlementDate = null, Date npvDate = null, int exDividendDays = 0 , double? npv = null) 
      {
            double bps = CashFlows.bps(cashflows, discountCurve, settlementDate, npvDate, exDividendDays);
            if (npv == null)
                npv = CashFlows.npv(cashflows, discountCurve, settlementDate, npvDate, exDividendDays);
            return basisPoint_ * npv.Value / bps;
        }


      ////! Internal rate of return.
      ///*! The IRR is the interest rate at which the NPV of the cash flows equals the given market price. The function verifies
      //      the theoretical existance of an IRR and numerically establishes the IRR to the desired precision. */
      //public static double irr(List<CashFlow> cashflows, double marketPrice, DayCounter dayCounter, Compounding compounding,
      //                        Frequency frequency, Date settlementDate, double accuracy, int maxIterations, double guess) {
      //      if (settlementDate == null)
      //          settlementDate = Settings.evaluationDate();

      //      // depending on the sign of the market price, check that cash flows of the opposite sign have been specified (otherwise
      //      // IRR is nonsensical.)

      //      int lastSign = Math.Sign(-marketPrice),
      //          signChanges = 0;

      //      foreach (CashFlow cf in cashflows.Where(cf => !cf.hasOccurred(settlementDate))) {
      //          int thisSign = Math.Sign(cf.amount());
      //          if (lastSign * thisSign < 0) // sign change
      //              signChanges++;

      //          if (thisSign != 0)
      //              lastSign = thisSign;
      //      }
      //      if (!(signChanges > 0))
      //          throw new ApplicationException("the given cash flows cannot result in the given market price due to their sign");

      //      /* The following is commented out due to the lack of a QL_WARN macro
      //      if (signChanges > 1) {    // Danger of non-unique solution
      //                                // Check the aggregate cash flows (Norstrom)
      //          Real aggregateCashFlow = marketPrice;
      //          signChanges = 0;
      //          for (Size i = 0; i < cashflows.size(); ++i) {
      //              Real nextAggregateCashFlow =
      //                  aggregateCashFlow + cashflows[i]->amount();

      //              if (aggregateCashFlow * nextAggregateCashFlow < 0.0)
      //                  signChanges++;

      //              aggregateCashFlow = nextAggregateCashFlow;
      //          }
      //          if (signChanges > 1)
      //              QL_WARN( "danger of non-unique solution");
      //      };
      //      */

      //      //Brent solver;
      //      NewtonSafe solver = new NewtonSafe();
      //      solver.setMaxEvaluations(maxIterations);
      //      return solver.solve(new IrrFinder(cashflows, marketPrice, dayCounter, compounding, frequency, settlementDate),
      //                          accuracy, guess, guess / 10.0);
      //  }

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
      public static double duration(List<CashFlow> cashflows, InterestRate rate, Duration.Type type = Duration.Type.Modified, 
                                    Date settlementDate = null) 
        {

           if (cashflows.Count == 0)
              return 0.0;

           if (settlementDate == null) settlementDate = Settings.evaluationDate();
           
           switch (type) 
           {
              case Duration.Type.Simple:
                 return simpleDuration(cashflows, rate, settlementDate);
                
              case Duration.Type.Modified:
                 return modifiedDuration(cashflows, rate, settlementDate);
                
              case Duration.Type.Macaulay:
                 return macaulayDuration(cashflows, rate, settlementDate);
                
              default:
                 throw new ArgumentException("unknown duration type");
           }
        }

      //! Cash-flow convexity
      /*! The convexity of a string of cash flows is defined as
	            \f[
	            C = \frac{1}{P} \frac{\partial^2 P}{\partial y^2}
	            \f]
	            where \f$ P \f$ is the present value of the cash flows according to the given IRR \f$ y \f$.
	        */
      public static double convexity(List<CashFlow> cashflows, InterestRate rate, Date settlementDate = null) 
        {
           if (cashflows.Count == 0)
              return 0.0;

           if (settlementDate == null) settlementDate = Settings.evaluationDate();

           DayCounter dayCounter = rate.dayCounter();

           double P = 0;
           double d2Pdy2 = 0;
           double y = rate.rate();
           int N = (int)rate.frequency();

           
           foreach (CashFlow cashflow in cashflows.Where(cashflow => !cashflow.hasOccurred(settlementDate))) 
           {
              double t = dayCounter.yearFraction(settlementDate, cashflow.date());
              double c = cashflow.amount();
              double B = rate.discountFactor(t);

              P += c * B;

              switch (rate.compounding()) 
              {
                 case Compounding.Simple:
                    d2Pdy2 += c * 2.0 * B * B * B * t * t;
                    break;
                    
                 case Compounding.Compounded:
                    d2Pdy2 += c * B * t * (N * t + 1) / (N * (1 + y / N) * (1 + y / N));
                    break;
                    
                 case Compounding.Continuous:
                    d2Pdy2 += c * B * t * t;
                    break;
                    
                 case Compounding.SimpleThenCompounded:
                    if (t <= 1.0 / N)
                       d2Pdy2 += c * 2.0 * B * B * B * t * t;
                    else
                       d2Pdy2 += c * B * t * (N * t + 1) / (N * (1 + y / N) * (1 + y / N));
                    break;
                    
                 default:
                    throw new ArgumentException("unknown compounding convention (" + rate.compounding() + ")");
              }
           }

           // no cashflows
           
           if (P == 0) return 0;
           return d2Pdy2 / P;
        }

      //! Basis-point value
      /*! Obtained by setting dy = 0.0001 in the 2nd-order Taylor
            series expansion.
        */
      public static double basisPointValue(List<CashFlow> leg, InterestRate y, Date settlementDate) 
      {
         if (leg.Count == 0)
           return 0.0;


         double shift = 0.0001;
         double dirtyPrice = CashFlows.npv(leg, y, settlementDate);
         double modifiedDuration = CashFlows.duration(leg, y, Duration.Type.Modified, settlementDate);
         double convexity = CashFlows.convexity(leg, y, settlementDate);

         double delta = -modifiedDuration*dirtyPrice;

         double gamma = (convexity/100.0)*dirtyPrice;

         delta *= shift;
         gamma *= shift*shift;

         return delta + 0.5*gamma;
      }

      //! Yield value of a basis point
      /*! The yield value of a one basis point change in price is
            the derivative of the yield with respect to the price
            multiplied by 0.01
        */
      public static double yieldValueBasisPoint(List<CashFlow> leg, InterestRate y, Date settlementDate) 
      {
         if (leg.Count == 0)
            return 0.0;

         double shift = 0.01;
         double dirtyPrice = CashFlows.npv(leg, y, settlementDate);
         double modifiedDuration = CashFlows.duration(leg, y, Duration.Type.Modified, settlementDate);
         
         return (1.0/(-dirtyPrice*modifiedDuration))*shift;
      }

      public static double yield( List<CashFlow> leg,
                          double npv,
                          DayCounter dayCounter,
                          Compounding compounding,
                          Frequency frequency,
                          bool includeSettlementDateFlows,
                          Date settlementDate = null,
                          Date npvDate = null,
                          double accuracy = 1.0e-10,
                          int maxIterations = 100,
                          double guess = 0.05) 
      {
        //Brent solver;
        NewtonSafe solver = new NewtonSafe();
        solver.setMaxEvaluations(maxIterations);

         IrrFinder objFunction = new IrrFinder(leg, npv, dayCounter, compounding, frequency, includeSettlementDateFlows,
            settlementDate,npvDate);
        
         return solver.solve(objFunction, accuracy, guess, guess / 10.0);

    }
    }


}
