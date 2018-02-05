/*
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)

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
   //! %Barrier option on a single asset.
   //    ! The analytic pricing Engine will be used if none if passed.
   //
   //        \ingroup instruments
   //
   public class BarrierOption : OneAssetOption
   {
      public new class Arguments : OneAssetOption.Arguments
      {
         public Arguments()
         {
            barrierType = Barrier.Type.NULL;
            barrier = null;
            rebate = null;
         }
         public Barrier.Type barrierType { get; set; }
         public double? barrier { get; set; }
         public double? rebate { get; set; }

         public override void validate()
         {
            base.validate();

            switch (barrierType)
            {
               case Barrier.Type.DownIn:
               case Barrier.Type.UpIn:
               case Barrier.Type.DownOut:
               case Barrier.Type.UpOut:
                  break;
               default:
                  Utils.QL_FAIL("unknown type");
                  break;
            }

            Utils.QL_REQUIRE(barrier != null, () => "no barrier given");
            Utils.QL_REQUIRE(rebate != null, () => "no rebate given");
         }
      }

      public new class Engine : GenericEngine<BarrierOption.Arguments, BarrierOption.Results>
      {
         protected bool triggered(double underlying)
         {
            switch (arguments_.barrierType)
            {
               case Barrier.Type.DownIn:
               case Barrier.Type.DownOut:
                  return underlying < arguments_.barrier;
               case Barrier.Type.UpIn:
               case Barrier.Type.UpOut:
                  return underlying > arguments_.barrier;
               default:
                  Utils.QL_FAIL("unknown type");
                  return false;
            }
         }
      }
      public BarrierOption(Barrier.Type barrierType, double barrier, double rebate, StrikedTypePayoff payoff, Exercise exercise) : base(payoff, exercise)
      {
         barrierType_ = barrierType;
         barrier_ = barrier;
         rebate_ = rebate;
      }

      public override void setupArguments(IPricingEngineArguments args)
      {
         base.setupArguments(args);

         BarrierOption.Arguments moreArgs = args as BarrierOption.Arguments;
         Utils.QL_REQUIRE(moreArgs != null, () => "wrong argument type");

         moreArgs.barrierType = barrierType_;
         moreArgs.barrier = barrier_;
         moreArgs.rebate = rebate_;
      }
      //        ! \warning see VanillaOption for notes on implied-volatility
      //                     calculation.
      //
      public double impliedVolatility(double targetValue, GeneralizedBlackScholesProcess process, double accuracy = 1.0e-4,
                                      int maxEvaluations = 100, double minVol = 1.0e-7, double maxVol = 4.0)
      {
         Utils.QL_REQUIRE(!isExpired(), () => "option expired");

         SimpleQuote volQuote = new SimpleQuote();

         GeneralizedBlackScholesProcess newProcess = ImpliedVolatilityHelper.clone(process, volQuote);

         // engines are built-in for the time being
         IPricingEngine engine = null;
         switch (exercise_.type())
         {
            case Exercise.Type.European:
               engine = new AnalyticBarrierEngine(newProcess);
               break;
            case Exercise.Type.American:
            case Exercise.Type.Bermudan:
               Utils.QL_FAIL("Engine not available for non-European barrier option");
               break;
            default:
               Utils.QL_FAIL("unknown exercise type");
               break;
         }

         return ImpliedVolatilityHelper.calculate(this, engine, volQuote, targetValue, accuracy, maxEvaluations, minVol, maxVol);
      }

      // Arguments
      protected Barrier.Type barrierType_;
      protected double? barrier_;
      protected double? rebate_;
   }
}
