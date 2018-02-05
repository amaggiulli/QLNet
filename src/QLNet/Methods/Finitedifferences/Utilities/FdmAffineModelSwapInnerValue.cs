/*
 Copyright (C) 2017 Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)

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
using System.Collections.Generic;
using System.Linq;

namespace QLNet
{
   public class FdmAffineModelSwapInnerValue<ModelType> : FdmInnerValueCalculator where ModelType : ITermStructureConsistentModel, IAffineModel
   {
      public FdmAffineModelSwapInnerValue(
         ModelType disModel,
         ModelType fwdModel,
         VanillaSwap swap,
         Dictionary<double, Date> exerciseDates,
         FdmMesher mesher,
         int direction)
      {
         disModel_ = disModel;
         fwdModel_ = fwdModel;
         mesher_ = mesher;
         direction_ = direction;
         swap_ = new VanillaSwap(swap.swapType,
                                 swap.nominal,
                                 swap.fixedSchedule(),
                                 swap.fixedRate,
                                 swap.fixedDayCount(),
                                 swap.floatingSchedule(),
                                 swap.iborIndex().clone(fwdTs_),
                                 swap.spread,
                                 swap.floatingDayCount(),
                                 null);
         exerciseDates_ = exerciseDates;
      }

      public override double innerValue(FdmLinearOpIterator iter, double t)
      {
         Date iterExerciseDate = exerciseDates_.ContainsKey(t) ? exerciseDates_[t] : exerciseDates_.Last().Value;

         Vector disRate = getState(disModel_, t, iter);
         Vector fwdRate = getState(fwdModel_, t, iter);

         if (disTs_.empty() || iterExerciseDate != disTs_.currentLink().referenceDate())
         {
            Handle<YieldTermStructure> discount
               = disModel_.termStructure();

            disTs_.linkTo(new FdmAffineModelTermStructure(disRate,
                                                          discount.currentLink().calendar(), discount.currentLink().dayCounter(),
                                                          iterExerciseDate, discount.currentLink().referenceDate(),
                                                          disModel_));

            Handle<YieldTermStructure> fwd = fwdModel_.termStructure();

            fwdTs_.linkTo(new FdmAffineModelTermStructure(fwdRate,
                                                          fwd.currentLink().calendar(), fwd.currentLink().dayCounter(),
                                                          iterExerciseDate, fwd.currentLink().referenceDate(),
                                                          fwdModel_));

         }
         else
         {
            (disTs_.currentLink() as FdmAffineModelTermStructure).setVariable(disRate);
            (fwdTs_.currentLink() as FdmAffineModelTermStructure).setVariable(fwdRate);
         }

         double npv = 0.0;
         for (int j = 0; j < 2; j++)
         {
            for (int i = 0; i < swap_.leg(j).Count; ++i)
            {
               npv += (swap_.leg(j)[i] as Coupon).accrualStartDate() >= iterExerciseDate
                      ? swap_.leg(j)[i].amount() * disTs_.currentLink().discount(swap_.leg(j)[i].date())
                      : 0.0;
            }
            if (j == 0)
               npv *= -1.0;
         }
         if (swap_.swapType == VanillaSwap.Type.Receiver)
            npv *= -1.0;

         return Math.Max(0.0, npv);
      }
      public override double avgInnerValue(FdmLinearOpIterator iter, double t)
      {
         return innerValue(iter, t);
      }

      public Vector getState(ModelType model, double t, FdmLinearOpIterator iter)
      {
         if (model.GetType().Equals(typeof(HullWhite)))
         {
            Vector retVal = new Vector(1, (model as HullWhite).dynamics().shortRate(t,
                                                                                    mesher_.location(iter, direction_)));
            return retVal;
         }
         else if (model.GetType().Equals(typeof(G2)))
         {
            Vector retVal = new Vector(2);
            retVal[0] = mesher_.location(iter, direction_);
            retVal[1] = mesher_.location(iter, direction_ + 1);

            return retVal;
         }
         else
            return new Vector();
      }

      protected RelinkableHandle<YieldTermStructure> disTs_ = new RelinkableHandle<YieldTermStructure>(), fwdTs_ = new RelinkableHandle<YieldTermStructure>();
      protected ModelType disModel_, fwdModel_;

      protected IborIndex index_;
      protected VanillaSwap swap_;
      protected Dictionary<double, Date> exerciseDates_;
      protected FdmMesher mesher_;
      protected int direction_;
   }
}
