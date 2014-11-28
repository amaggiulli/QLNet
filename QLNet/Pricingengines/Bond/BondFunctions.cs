/*
 Copyright (C) 2008-2013  Andrea Maggiulli (a.maggiulli@gmail.com)

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
   //! Bond adapters of CashFlows functions
   /*! See CashFlows for functions' documentation.

       These adapters calls into CashFlows functions passing as input the
       Bond cashflows, the dirty price (i.e. npv) calculated from clean
       price, the bond settlementDate date (unless another date is given), zero
       ex-dividend days, and excluding any cashflow on the settlementDate date.

       Prices are always clean, as per market convention.
   */
   public class BondFunctions
   {
      #region Date inspectors

      public static Date startDate(Bond bond)
      {
         return CashFlows.startDate(bond.cashflows());
      }
      public static Date maturityDate(Bond bond)
      {
         return CashFlows.maturityDate(bond.cashflows());
      }
      public static bool isTradable(Bond bond, Date settlementDate = null)
      {
         if (settlementDate == null)
            settlementDate = bond.settlementDate();

         return bond.notional(settlementDate) != 0.0;
      }

      #endregion

      #region CashFlow inspectors

      public static CashFlow previousCashFlow(Bond bond, Date refDate = null)
      {
         if (refDate == null)
            refDate = bond.settlementDate();

         return CashFlows.previousCashFlow(bond.cashflows(), false, refDate);
      }
      public static CashFlow nextCashFlow(Bond bond, Date refDate = null)
      {
         if (refDate == null)
            refDate = bond.settlementDate();

         return CashFlows.nextCashFlow(bond.cashflows(), false, refDate);
      }
      public static Date previousCashFlowDate(Bond bond, Date refDate = null)
      {
         if (refDate == null)
            refDate = bond.settlementDate();

         return CashFlows.previousCashFlowDate(bond.cashflows(), false, refDate);
      }
      public static Date nextCashFlowDate(Bond bond, Date refDate = null)
      {
         if (refDate == null)
            refDate = bond.settlementDate();

         return CashFlows.nextCashFlowDate(bond.cashflows(), false, refDate);
      }
      public static double? previousCashFlowAmount(Bond bond, Date refDate = null)
      {
         if (refDate == null)
            refDate = bond.settlementDate();

         return CashFlows.previousCashFlowAmount(bond.cashflows(), false, refDate);
      }
      public static double? nextCashFlowAmount(Bond bond, Date refDate = null)
      {
         if (refDate == null)
            refDate = bond.settlementDate();

         return CashFlows.nextCashFlowAmount(bond.cashflows(), false, refDate);
      }

      #endregion

      #region Coupon inspectors

      public static double previousCouponRate(Bond bond, Date settlementDate = null)
      {
         if (settlementDate == null)
            settlementDate = bond.settlementDate();

         return CashFlows.previousCouponRate(bond.cashflows(), false, settlementDate);
      }
      public static double nextCouponRate(Bond bond, Date settlementDate = null)
      {
         if (settlementDate == null)
            settlementDate = bond.settlementDate();

         return CashFlows.nextCouponRate(bond.cashflows(), false, settlementDate);
      }
      public static Date accrualStartDate(Bond bond, Date settlementDate = null)
      {
         if (settlementDate == null)
            settlementDate = bond.settlementDate();

         Utils.QL_REQUIRE( BondFunctions.isTradable( bond, settlementDate ), () =>
                   "non tradable at " + settlementDate +
                   " (maturity being " + bond.maturityDate() + ")");

         return CashFlows.accrualStartDate(bond.cashflows(), false, settlementDate);
      }
      public static Date accrualEndDate(Bond bond, Date settlementDate = null)
      {
         if (settlementDate == null)
            settlementDate = bond.settlementDate();

         Utils.QL_REQUIRE( BondFunctions.isTradable( bond, settlementDate ), () =>
                   "non tradable at " + settlementDate +
                   " (maturity being " + bond.maturityDate() + ")");

         return CashFlows.accrualEndDate(bond.cashflows(), false, settlementDate);
      }
      public static Date referencePeriodStart(Bond bond, Date settlementDate = null)
      {
         if (settlementDate == null)
            settlementDate = bond.settlementDate();

         Utils.QL_REQUIRE( BondFunctions.isTradable( bond, settlementDate ), () =>
                   "non tradable at " + settlementDate +
                   " (maturity being " + bond.maturityDate() + ")");

         return CashFlows.referencePeriodStart(bond.cashflows(), false, settlementDate);
      }
      public static Date referencePeriodEnd(Bond bond, Date settlementDate = null)
      {
         if (settlementDate == null)
            settlementDate = bond.settlementDate();

         Utils.QL_REQUIRE( BondFunctions.isTradable( bond, settlementDate ), () =>
                   "non tradable at " + settlementDate +
                   " (maturity being " + bond.maturityDate() + ")");

         return CashFlows.referencePeriodEnd(bond.cashflows(), false, settlementDate);
      }
      public static double accrualPeriod(Bond bond, Date settlementDate = null)
      {
         if (settlementDate == null)
            settlementDate = bond.settlementDate();

         Utils.QL_REQUIRE( BondFunctions.isTradable( bond, settlementDate ), () =>
                   "non tradable at " + settlementDate +
                   " (maturity being " + bond.maturityDate() + ")");

         return CashFlows.accrualPeriod(bond.cashflows(), false, settlementDate);
      }
      public static int accrualDays(Bond bond, Date settlementDate = null)
      {
         if (settlementDate == null)
            settlementDate = bond.settlementDate();

         Utils.QL_REQUIRE( BondFunctions.isTradable( bond, settlementDate ), () =>
                   "non tradable at " + settlementDate +
                   " (maturity being " + bond.maturityDate() + ")");

         return CashFlows.accrualDays(bond.cashflows(), false, settlementDate);
      }
      public static double accruedPeriod(Bond bond, Date settlementDate = null)
      {
         if (settlementDate == null)
            settlementDate = bond.settlementDate();

         Utils.QL_REQUIRE( BondFunctions.isTradable( bond, settlementDate ), () =>
                   "non tradable at " + settlementDate +
                   " (maturity being " + bond.maturityDate() + ")");

         return CashFlows.accruedPeriod(bond.cashflows(), false, settlementDate);
      }
      public static double accruedDays(Bond bond, Date settlementDate = null)
      {
         if (settlementDate == null)
            settlementDate = bond.settlementDate();

         Utils.QL_REQUIRE( BondFunctions.isTradable( bond, settlementDate ), () =>
                   "non tradable at " + settlementDate +
                   " (maturity being " + bond.maturityDate() + ")");

         return CashFlows.accruedDays(bond.cashflows(), false, settlementDate);
      }
      public static double accruedAmount(Bond bond, Date settlementDate = null)
      {
         if (settlementDate == null)
            settlementDate = bond.settlementDate();

         Utils.QL_REQUIRE( BondFunctions.isTradable( bond, settlementDate ), () =>
                   "non tradable at " + settlementDate +
                   " (maturity being " + bond.maturityDate() + ")");

         return CashFlows.accruedAmount(bond.cashflows(), false, settlementDate) * 100.0 / bond.notional(settlementDate);
      }

      #endregion

      #region YieldTermStructure functions

      public static double cleanPrice(Bond bond, YieldTermStructure discountCurve, Date settlementDate = null)
      {
         if (settlementDate == null)
            settlementDate = bond.settlementDate();

         Utils.QL_REQUIRE( BondFunctions.isTradable( bond, settlementDate ), () =>
                   "non tradable at " + settlementDate +
                   " settlementDate date (maturity being " +
                   bond.maturityDate() + ")");

         double dirtyPrice = CashFlows.npv(bond.cashflows(), discountCurve, false, settlementDate) *
                             100.0 / bond.notional(settlementDate);
         return dirtyPrice - bond.accruedAmount(settlementDate);
      }
      public static double bps(Bond bond, YieldTermStructure discountCurve, Date settlementDate = null)
      {
         if (settlementDate == null)
            settlementDate = bond.settlementDate();

         Utils.QL_REQUIRE( BondFunctions.isTradable( bond, settlementDate ), () =>
                   "non tradable at " + settlementDate +
                   " (maturity being " + bond.maturityDate() + ")");

         return CashFlows.bps(bond.cashflows(), discountCurve, false, settlementDate) * 100.0 / bond.notional(settlementDate);
      }
      public static double atmRate(Bond bond, YieldTermStructure discountCurve, Date settlementDate = null, double? cleanPrice = null)
      {
         if (settlementDate == null)
            settlementDate = bond.settlementDate();

         Utils.QL_REQUIRE( BondFunctions.isTradable( bond, settlementDate ), () =>
                   "non tradable at " + settlementDate +
                   " (maturity being " + bond.maturityDate() + ")");

         double? dirtyPrice = cleanPrice == null ? null : cleanPrice + bond.accruedAmount(settlementDate);
         double currentNotional = bond.notional(settlementDate);
         double? npv = dirtyPrice / 100.0 * currentNotional;

         return CashFlows.atmRate(bond.cashflows(), discountCurve, false, settlementDate, settlementDate, npv);
      }

      #endregion

      #region Yield (a.k.a. Internal Rate of Return, i.e. IRR) functions

      public static double cleanPrice(Bond bond, InterestRate yield, Date settlementDate = null)
      {
        return dirtyPrice(bond, yield, settlementDate) - bond.accruedAmount(settlementDate);
      }
      public static double cleanPrice(Bond bond, double yield, DayCounter dayCounter, Compounding compounding, Frequency frequency,
                                Date settlementDate = null)
      {
         return cleanPrice(bond, new InterestRate(yield, dayCounter, compounding, frequency), settlementDate);
      }
      public static double dirtyPrice(Bond bond, InterestRate yield, Date settlementDate = null)
      {
          if (settlementDate == null)
              settlementDate = bond.settlementDate();

          Utils.QL_REQUIRE( BondFunctions.isTradable( bond, settlementDate ), () =>
                    "non tradable at " + settlementDate +
                    " (maturity being " + bond.maturityDate() + ")");

          double dirtyPrice = CashFlows.npv(bond.cashflows(), yield, false, settlementDate) *
                              100.0 / bond.notional(settlementDate);
          return dirtyPrice;
      }
      public static double dirtyPrice(Bond bond, double yield, DayCounter dayCounter, Compounding compounding, Frequency frequency,
                                Date settlementDate = null)
      {
          return dirtyPrice(bond, new InterestRate(yield, dayCounter, compounding, frequency), settlementDate);
      }
      public static double bps(Bond bond, InterestRate yield, Date settlementDate = null)
      {
         if (settlementDate == null)
            settlementDate = bond.settlementDate();

         Utils.QL_REQUIRE( BondFunctions.isTradable( bond, settlementDate ), () =>
                   "non tradable at " + settlementDate +
                   " (maturity being " + bond.maturityDate() + ")");

         return CashFlows.bps(bond.cashflows(), yield, false, settlementDate) *
                              100.0 / bond.notional(settlementDate);
      }
      public static double bps(Bond bond, double yield, DayCounter dayCounter, Compounding compounding, Frequency frequency,
                         Date settlementDate = null)
      {
         return bps(bond, new InterestRate(yield, dayCounter, compounding, frequency), settlementDate);
      }
      public static double yield(Bond bond, double cleanPrice, DayCounter dayCounter, Compounding compounding, Frequency frequency,
                           Date settlementDate = null, double accuracy = 1.0e-10, int maxIterations = 100, double guess = 0.05)
      {
         if (settlementDate == null)
            settlementDate = bond.settlementDate();

         Utils.QL_REQUIRE( BondFunctions.isTradable( bond, settlementDate ), () =>
                   "non tradable at " + settlementDate +
                   " (maturity being " + bond.maturityDate() + ")");

         double dirtyPrice = cleanPrice + bond.accruedAmount(settlementDate);
         dirtyPrice /= 100.0 / bond.notional(settlementDate);

         return CashFlows.yield(bond.cashflows(), dirtyPrice,
                                dayCounter, compounding, frequency,
                                false, settlementDate, settlementDate,
                                accuracy, maxIterations, guess);
      }
      public static double duration(Bond bond, InterestRate yield, Duration.Type type = Duration.Type.Modified,
                                     Date settlementDate = null)
      {
         if (settlementDate == null)
            settlementDate = bond.settlementDate();

         Utils.QL_REQUIRE( BondFunctions.isTradable( bond, settlementDate ), () =>
                   "non tradable at " + settlementDate +
                   " (maturity being " + bond.maturityDate() + ")");

         return CashFlows.duration(bond.cashflows(), yield, type, false, settlementDate);
      }
      public static double duration(Bond bond, double yield, DayCounter dayCounter, Compounding compounding, Frequency frequency,
                              Duration.Type type = Duration.Type.Modified, Date settlementDate = null)
      {
         return duration(bond, new InterestRate(yield, dayCounter, compounding, frequency), type, settlementDate);
      }
      public static double convexity(Bond bond, InterestRate yield, Date settlementDate = null)
      {
         if (settlementDate == null)
            settlementDate = bond.settlementDate();

         Utils.QL_REQUIRE( BondFunctions.isTradable( bond, settlementDate ), () =>
                   "non tradable at " + settlementDate +
                   " (maturity being " + bond.maturityDate() + ")");

         return CashFlows.convexity(bond.cashflows(), yield, false, settlementDate);
      }
      public static double convexity(Bond bond, double yield, DayCounter dayCounter, Compounding compounding, Frequency frequency,
                               Date settlementDate = null)
      {
         return convexity(bond, new InterestRate(yield, dayCounter, compounding, frequency), settlementDate);
      }
      public static double basisPointValue(Bond bond, InterestRate yield, Date settlementDate = null)
      {
         if (settlementDate == null)
            settlementDate = bond.settlementDate();

         Utils.QL_REQUIRE( BondFunctions.isTradable( bond, settlementDate ), () =>
                   "non tradable at " + settlementDate +
                   " (maturity being " + bond.maturityDate() + ")");

         return CashFlows.basisPointValue(bond.cashflows(), yield,
                                          false, settlementDate);
      }
      public static double basisPointValue(Bond bond, double yield, DayCounter dayCounter, Compounding compounding, Frequency frequency,
                                     Date settlementDate = null)
      {
         return CashFlows.basisPointValue(bond.cashflows(), new InterestRate(yield, dayCounter, compounding, frequency), false, settlementDate);
      }
      public static double yieldValueBasisPoint(Bond bond, InterestRate yield, Date settlementDate = null)
      {
         if (settlementDate == null)
            settlementDate = bond.settlementDate();

         Utils.QL_REQUIRE( BondFunctions.isTradable( bond, settlementDate ), () =>
                   "non tradable at " + settlementDate +
                   " (maturity being " + bond.maturityDate() + ")");

         return CashFlows.yieldValueBasisPoint(bond.cashflows(), yield,
                                               false, settlementDate);
      }
      public static double yieldValueBasisPoint(Bond bond, double yield, DayCounter dayCounter, Compounding compounding,
                                          Frequency frequency, Date settlementDate = null)
      {
         return CashFlows.yieldValueBasisPoint(bond.cashflows(), new InterestRate(yield, dayCounter, compounding, frequency), false, settlementDate);
      }
      #endregion

      #region Z-spread functions

      public static double cleanPrice(Bond bond, YieldTermStructure discount, double zSpread, DayCounter dayCounter, Compounding compounding,
                                Frequency frequency, Date settlementDate = null)
      {
         if (settlementDate == null)
            settlementDate = bond.settlementDate();

         Utils.QL_REQUIRE( BondFunctions.isTradable( bond, settlementDate ), () =>
                   "non tradable at " + settlementDate +
                   " (maturity being " + bond.maturityDate() + ")");

         double dirtyPrice = CashFlows.npv(bond.cashflows(), discount, zSpread, dayCounter, compounding, frequency, false, settlementDate) *
                             100.0 / bond.notional(settlementDate);
         return dirtyPrice - bond.accruedAmount(settlementDate);
      }
      public static double zSpread(Bond bond, double cleanPrice, YieldTermStructure discount, DayCounter dayCounter, Compounding compounding,
                             Frequency frequency, Date settlementDate = null, double accuracy = 1.0e-10, int maxIterations = 100,
                             double guess = 0.0)
      {
         if (settlementDate == null)
            settlementDate = bond.settlementDate();

         Utils.QL_REQUIRE( BondFunctions.isTradable( bond, settlementDate ), () =>
                   "non tradable at " + settlementDate +
                   " (maturity being " + bond.maturityDate() + ")");

         double dirtyPrice = cleanPrice + bond.accruedAmount(settlementDate);
         dirtyPrice /= 100.0 / bond.notional(settlementDate);

         return CashFlows.zSpread(bond.cashflows(),
                                  discount,
                                  dirtyPrice,
                                  dayCounter, compounding, frequency,
                                  false, settlementDate, settlementDate,
                                  accuracy, maxIterations, guess);
      }
      #endregion

   }
}
