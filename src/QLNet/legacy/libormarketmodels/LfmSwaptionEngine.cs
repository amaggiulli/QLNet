/*
 Copyright (C) 2009 Philippe Real (ph_real@hotmail.com)
 Copyright (C) 2019 Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)

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
using System;
using System.Linq;

namespace QLNet
{
   //! %Libor forward model swaption engine based on Black formula
   /*! \ingroup swaptionengines */
   public class LfmSwaptionEngine : GenericModelEngine<LiborForwardModel,
      Swaption.Arguments,
      Swaption.Results>
   {
      private Handle<YieldTermStructure> discountCurve_;

      public LfmSwaptionEngine(LiborForwardModel model,
                               Handle<YieldTermStructure> discountCurve)
         : base(model)
      {
         discountCurve_ = discountCurve;
         discountCurve_.registerWith(update);
      }

      public override void calculate()
      {
         Utils.QL_REQUIRE(arguments_.settlementMethod != Settlement.Method.ParYieldCurve, () =>
                          "cash-settled (ParYieldCurve) swaptions not priced with Lfm engine");         

         VanillaSwap swap = arguments_.swap;
         IPricingEngine pe = new DiscountingSwapEngine(discountCurve_);
         swap.setPricingEngine(pe);

         double correction = swap.spread *
                             Math.Abs(swap.floatingLegBPS() / swap.fixedLegBPS());
         double fixedRate = swap.fixedRate - correction;
         double fairRate = swap.fairRate() - correction;

         SwaptionVolatilityMatrix volatility =
            model_.link.getSwaptionVolatilityMatrix();

         Date referenceDate = volatility.referenceDate();
         DayCounter dayCounter = volatility.dayCounter();

         double exercise = dayCounter.yearFraction(referenceDate,
                                                   arguments_.exercise.date(0));
         double swapLength =
            dayCounter.yearFraction(referenceDate,
                                    arguments_.fixedPayDates.Last())
            - dayCounter.yearFraction(referenceDate,
                                      arguments_.fixedResetDates[0]);

         Option.Type w = arguments_.type == VanillaSwap.Type.Payer ?
                         Option.Type.Call : Option.Type.Put;
         double vol = volatility.volatility(exercise, swapLength,
                                            fairRate, true);
         results_.value = (swap.fixedLegBPS() / Const.BASIS_POINT) *
                          Utils.blackFormula(w, fixedRate, fairRate, vol * Math.Sqrt(exercise));
      }
   }
}
