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

namespace QLNet
{
   public class FdmSchemeDesc
   {
      public enum FdmSchemeType
      {
         HundsdorferType, DouglasType,
         CraigSneydType, ModifiedCraigSneydType,
         ImplicitEulerType, ExplicitEulerType,
         MethodOfLinesType, TrBDF2Type,
         CrankNicolsonType
      }

      public FdmSchemeDesc() { }
      public FdmSchemeDesc(FdmSchemeType type, double theta, double mu)
      {
         type_ = type;
         theta_ = theta;
         mu_ = mu;
      }

      public FdmSchemeType type
      {
         get
         {
            return type_;
         }
      }

      public double theta
      {
         get
         {
            return theta_;
         }
      }

      public double mu
      {
         get
         {
            return mu_;
         }
      }

      protected FdmSchemeType type_;
      private double theta_, mu_;

      // some default scheme descriptions
      public FdmSchemeDesc Douglas() { return new FdmSchemeDesc(FdmSchemeType.DouglasType, 0.5, 0.0); }
      public FdmSchemeDesc CrankNicolson() { return new FdmSchemeDesc(FdmSchemeType.CrankNicolsonType, 0.5, 0.0); }
      public FdmSchemeDesc ImplicitEuler() { return new FdmSchemeDesc(FdmSchemeType.ImplicitEulerType, 0.0, 0.0); }
      public FdmSchemeDesc ExplicitEuler() { return new FdmSchemeDesc(FdmSchemeType.ExplicitEulerType, 0.0, 0.0); }
      public FdmSchemeDesc CraigSneyd() { return new FdmSchemeDesc(FdmSchemeType.CraigSneydType, 0.5, 0.5); }
      public FdmSchemeDesc ModifiedCraigSneyd() { return new FdmSchemeDesc(FdmSchemeType.ModifiedCraigSneydType, 1.0 / 3.0, 1.0 / 3.0); }
      public FdmSchemeDesc Hundsdorfer() { return new FdmSchemeDesc(FdmSchemeType.HundsdorferType, 0.5 + Math.Sqrt(3.0) / 6.0, 0.5); }
      public FdmSchemeDesc ModifiedHundsdorfer() { return new FdmSchemeDesc(FdmSchemeType.HundsdorferType, 1.0 - Math.Sqrt(2.0) / 2.0, 0.5); }
      public FdmSchemeDesc MethodOfLines(double eps = 0.001, double relInitStepSize = 0.01) { return new FdmSchemeDesc(FdmSchemeType.MethodOfLinesType, eps, relInitStepSize); }
      public FdmSchemeDesc TrBDF2() { return new FdmSchemeDesc(FdmSchemeType.TrBDF2Type, 2.0 - Math.Sqrt(2.0), 1E-8); }
   }

   public class FdmBackwardSolver
   {
      public FdmBackwardSolver(FdmLinearOpComposite map,
                               FdmBoundaryConditionSet bcSet,
                               FdmStepConditionComposite condition,
                               FdmSchemeDesc schemeDesc)
      {
         map_ = map;
         bcSet_ = bcSet;
         condition_ = condition;
         schemeDesc_ = schemeDesc;
      }

      public void rollback(ref object a,
                           double from, double to,
                           int steps, int dampingSteps)
      {
         double deltaT = from - to;
         int allSteps = steps + dampingSteps;
         double dampingTo = from - (deltaT * dampingSteps) / allSteps;

         if (dampingSteps > 0
             && schemeDesc_.type != FdmSchemeDesc.FdmSchemeType.ImplicitEulerType)
         {
            ImplicitEulerScheme implicitEvolver = new ImplicitEulerScheme(map_, bcSet_);
            FiniteDifferenceModel<ImplicitEulerScheme> dampingModel
               = new FiniteDifferenceModel<ImplicitEulerScheme>(implicitEvolver, condition_.stoppingTimes());

            dampingModel.rollback(ref a, from, dampingTo,
                                  dampingSteps, condition_);
         }

         switch (schemeDesc_.type)
         {
            case FdmSchemeDesc.FdmSchemeType.HundsdorferType:
            {
               HundsdorferScheme hsEvolver = new HundsdorferScheme(schemeDesc_.theta, schemeDesc_.mu,
                                                                   map_, bcSet_);
               FiniteDifferenceModel<HundsdorferScheme>
               hsModel = new FiniteDifferenceModel<HundsdorferScheme>(hsEvolver, condition_.stoppingTimes());
               hsModel.rollback(ref a, dampingTo, to, steps, condition_);
            }
            break;
            case FdmSchemeDesc.FdmSchemeType.DouglasType:
            {
               DouglasScheme dsEvolver = new DouglasScheme(schemeDesc_.theta, map_, bcSet_);
               FiniteDifferenceModel<DouglasScheme>
               dsModel = new FiniteDifferenceModel<DouglasScheme>(dsEvolver, condition_.stoppingTimes());
               dsModel.rollback(ref a, dampingTo, to, steps, condition_);
            }
            break;
            case FdmSchemeDesc.FdmSchemeType.CrankNicolsonType:
            {
               CrankNicolsonScheme cnEvolver = new CrankNicolsonScheme(schemeDesc_.theta, map_, bcSet_);
               FiniteDifferenceModel<CrankNicolsonScheme>
               cnModel = new FiniteDifferenceModel<CrankNicolsonScheme>(cnEvolver, condition_.stoppingTimes());
               cnModel.rollback(ref a, dampingTo, to, steps, condition_);
            }
            break;
            case FdmSchemeDesc.FdmSchemeType.CraigSneydType:
            {
               CraigSneydScheme csEvolver = new CraigSneydScheme(schemeDesc_.theta, schemeDesc_.mu,
                                                                 map_, bcSet_);
               FiniteDifferenceModel<CraigSneydScheme>
               csModel = new FiniteDifferenceModel<CraigSneydScheme>(csEvolver, condition_.stoppingTimes());
               csModel.rollback(ref a, dampingTo, to, steps, condition_);
            }
            break;
            case FdmSchemeDesc.FdmSchemeType.ModifiedCraigSneydType:
            {
               ModifiedCraigSneydScheme csEvolver = new ModifiedCraigSneydScheme(schemeDesc_.theta,
                                                                                 schemeDesc_.mu,
                                                                                 map_, bcSet_);
               FiniteDifferenceModel<ModifiedCraigSneydScheme>
               mcsModel = new FiniteDifferenceModel<ModifiedCraigSneydScheme>(csEvolver, condition_.stoppingTimes());
               mcsModel.rollback(ref a, dampingTo, to, steps, condition_);
            }
            break;
            case FdmSchemeDesc.FdmSchemeType.ImplicitEulerType:
            {
               ImplicitEulerScheme implicitEvolver = new ImplicitEulerScheme(map_, bcSet_);
               FiniteDifferenceModel<ImplicitEulerScheme>
               implicitModel = new FiniteDifferenceModel<ImplicitEulerScheme>(implicitEvolver, condition_.stoppingTimes());
               implicitModel.rollback(ref a, from, to, allSteps, condition_);
            }
            break;
            case FdmSchemeDesc.FdmSchemeType.ExplicitEulerType:
            {
               ExplicitEulerScheme explicitEvolver = new ExplicitEulerScheme(map_, bcSet_);
               FiniteDifferenceModel<ExplicitEulerScheme>
               explicitModel = new FiniteDifferenceModel<ExplicitEulerScheme>(explicitEvolver, condition_.stoppingTimes());
               explicitModel.rollback(ref a, dampingTo, to, steps, condition_);
            }
            break;
            case FdmSchemeDesc.FdmSchemeType.MethodOfLinesType:
            {
               MethodOfLinesScheme methodOfLines = new MethodOfLinesScheme(schemeDesc_.theta, schemeDesc_.mu, map_, bcSet_);
               FiniteDifferenceModel<MethodOfLinesScheme>
               molModel = new FiniteDifferenceModel<MethodOfLinesScheme>(methodOfLines, condition_.stoppingTimes());
               molModel.rollback(ref a, dampingTo, to, steps, condition_);
            }
            break;
            case FdmSchemeDesc.FdmSchemeType.TrBDF2Type:
            {
               FdmSchemeDesc trDesc = new FdmSchemeDesc().CraigSneyd();
               CraigSneydScheme hsEvolver = new CraigSneydScheme(trDesc.theta, trDesc.mu, map_, bcSet_);

               TrBDF2Scheme<CraigSneydScheme> trBDF2 = new TrBDF2Scheme<CraigSneydScheme>(
                  schemeDesc_.theta, map_, hsEvolver, bcSet_, schemeDesc_.mu);

               FiniteDifferenceModel<TrBDF2Scheme<CraigSneydScheme>>
               trBDF2Model = new FiniteDifferenceModel<TrBDF2Scheme<CraigSneydScheme>>(trBDF2, condition_.stoppingTimes());
               trBDF2Model.rollback(ref a, dampingTo, to, steps, condition_);
            }
            break;
            default:
               Utils.QL_FAIL("Unknown scheme type");
               break;
         }
      }

      protected FdmLinearOpComposite map_;
      protected FdmBoundaryConditionSet bcSet_;
      protected FdmStepConditionComposite condition_;
      protected FdmSchemeDesc schemeDesc_;
   }
}
