/*
 Copyright (C) 2020 Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)

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
using System.Linq;

namespace QLNet
{
   //! Quanto term structure
   /*! Quanto term structure for modelling quanto effect in
       option pricing.

       \note This term structure will remain linked to the original
             structures, i.e., any changes in the latters will be
             reflected in this structure as well.
   */
   public class QuantoTermStructure : ZeroYieldStructure
   {

      public QuantoTermStructure(
         Handle<YieldTermStructure> underlyingDividendTS,
         Handle<YieldTermStructure> riskFreeTS,
         Handle<YieldTermStructure> foreignRiskFreeTS,
         Handle<BlackVolTermStructure> underlyingBlackVolTS,
         double strike,
         Handle<BlackVolTermStructure> exchRateBlackVolTS,
         double exchRateATMlevel,
         double underlyingExchRateCorrelation)
         : base(underlyingDividendTS.currentLink().dayCounter())
      {
         underlyingDividendTS_ = underlyingDividendTS;
         riskFreeTS_ = riskFreeTS;
         foreignRiskFreeTS_ = foreignRiskFreeTS;
         underlyingBlackVolTS_ = underlyingBlackVolTS;
         exchRateBlackVolTS_ = exchRateBlackVolTS;
         underlyingExchRateCorrelation_ = underlyingExchRateCorrelation;
         strike_ = strike;
         exchRateATMlevel_ = exchRateATMlevel;
         underlyingDividendTS_.registerWith(update);
         riskFreeTS_.registerWith(update);
         foreignRiskFreeTS_.registerWith(update);
         underlyingBlackVolTS_.registerWith(update);
         exchRateBlackVolTS_.registerWith(update);
      }

      public override DayCounter dayCounter()
      {
         return underlyingDividendTS_.currentLink().dayCounter();
      }

      public override Calendar calendar()
      {
         return underlyingDividendTS_.currentLink().calendar();
      }

      public override int settlementDays()
      {
         return underlyingDividendTS_.currentLink().settlementDays();
      }

      public override Date referenceDate()
      {
         return underlyingDividendTS_.currentLink().referenceDate();
      }

      public override Date maxDate()
      {
         Date maxDate = Date.Min(underlyingDividendTS_.currentLink().maxDate(),
                                 riskFreeTS_.currentLink().maxDate());
         maxDate = Date.Min(maxDate, foreignRiskFreeTS_.currentLink().maxDate());
         maxDate = Date.Min(maxDate, underlyingBlackVolTS_.currentLink().maxDate());
         maxDate = Date.Min(maxDate, exchRateBlackVolTS_.currentLink().maxDate());
         return maxDate;
      }

      protected override double zeroYieldImpl(double t)
      {
         // warning: here it is assumed that all TS have the same daycount.
         //          It should be QL_REQUIREd
         return underlyingDividendTS_.currentLink().zeroRate(t, Compounding.Continuous, Frequency.NoFrequency, true).value()
                + riskFreeTS_.currentLink().zeroRate(t, Compounding.Continuous, Frequency.NoFrequency, true).value()
                - foreignRiskFreeTS_.currentLink().zeroRate(t, Compounding.Continuous, Frequency.NoFrequency, true).value()
                + underlyingExchRateCorrelation_
                * underlyingBlackVolTS_.currentLink().blackVol(t, strike_, true)
                * exchRateBlackVolTS_.currentLink().blackVol(t, exchRateATMlevel_, true);
      }

      protected Handle<YieldTermStructure> underlyingDividendTS_, riskFreeTS_, foreignRiskFreeTS_;
      protected Handle<BlackVolTermStructure> underlyingBlackVolTS_, exchRateBlackVolTS_;
      protected double underlyingExchRateCorrelation_, strike_, exchRateATMlevel_;
   }
}
