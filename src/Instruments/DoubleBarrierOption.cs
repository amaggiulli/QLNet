//  Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)
//  
//  This file is part of QLNet Project https://github.com/amaggiulli/qlnet
//  QLNet is free software: you can redistribute it and/or modify it
//  under the terms of the QLNet license.  You should have received a
//  copy of the license along with this program; if not, license is  
//  available online at <http://qlnet.sourceforge.net/License.html>.
//   
//  QLNet is a based on QuantLib, a free-software/open-source library
//  for financial quantitative analysts and developers - http://quantlib.org/
//  The QuantLib license is available online at http://quantlib.org/license.shtml.
//  
//  This program is distributed in the hope that it will be useful, but WITHOUT
//  ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
//  FOR A PARTICULAR PURPOSE.  See the license for more details.

namespace QLNet
{
   public struct DoubleBarrier 
   {
      public enum Type 
      {
         KnockIn,
         KnockOut,
         KIKO,     //! lower barrier KI, upper KO
         KOKI      //! lower barrier KO, upper KI
      }
    }

   //! %Double Barrier option on a single asset.
   /*! The analytic pricing engine will be used if none if passed.

       \ingroup instruments
   */
   public class DoubleBarrierOption : OneAssetOption
   {
            
      public DoubleBarrierOption(DoubleBarrier.Type barrierType,
                                 double barrier_lo,
                                 double barrier_hi,
                                 double rebate,
                                 StrikedTypePayoff payoff,
                                 Exercise exercise)
         :base(payoff, exercise)
      {
         barrierType_ = barrierType;
         barrier_lo_ = barrier_lo;
         barrier_hi_ = barrier_hi;
         rebate_ = rebate;
      }
        
      public override void setupArguments(IPricingEngineArguments args)
      {
         base.setupArguments(args);

         DoubleBarrierOption.Arguments moreArgs = args as DoubleBarrierOption.Arguments;
         Utils.QL_REQUIRE(moreArgs != null,()=> "wrong argument type");
         moreArgs.barrierType = barrierType_;
         moreArgs.barrier_lo = barrier_lo_;
         moreArgs.barrier_hi = barrier_hi_;
         moreArgs.rebate = rebate_;
      }

      /*! \warning see VanillaOption for notes on implied-volatility
                  calculation.
      */
      public double impliedVolatility( double targetValue,
                                       GeneralizedBlackScholesProcess process,
                                       double accuracy = 1.0e-4,
                                       int maxEvaluations = 100,
                                       double minVol = 1.0e-7,
                                       double maxVol = 4.0)
      {
         Utils.QL_REQUIRE(!isExpired(),()=> "option expired");

         SimpleQuote volQuote=new SimpleQuote();

         GeneralizedBlackScholesProcess newProcess = ImpliedVolatilityHelper.clone(process, volQuote);

         // engines are built-in for the time being
         IPricingEngine engine = null;
         
         switch (exercise_.type()) 
         {
            case Exercise.Type.European:
               engine = new AnalyticDoubleBarrierEngine(newProcess);
               break;
            case Exercise.Type.American:
            case Exercise.Type.Bermudan:
               Utils.QL_FAIL("engine not available for non-European barrier option");
               break;
            default:
               Utils.QL_FAIL("unknown exercise type");
               break;
         }

        return ImpliedVolatilityHelper.calculate(this,engine,volQuote,targetValue,accuracy,maxEvaluations,minVol, maxVol);

      }
      
      // arguments
      protected DoubleBarrier.Type barrierType_;
      protected double barrier_lo_;
      protected double barrier_hi_;
      protected double rebate_;

      //! %Arguments for double barrier option calculation
      public new class Arguments : OneAssetOption.Arguments 
      {
         public Arguments()
         {
            barrier_lo = null;
            barrier_hi = null;
            rebate = null;
         }
         public DoubleBarrier.Type barrierType;
         public double? barrier_lo;
         public double? barrier_hi;
         public double? rebate;
         public override void validate()
         {
            base.validate();

            Utils.QL_REQUIRE(barrierType == DoubleBarrier.Type.KnockIn ||
                             barrierType == DoubleBarrier.Type.KnockOut ||
                             barrierType == DoubleBarrier.Type.KIKO ||
                             barrierType == DoubleBarrier.Type.KOKI,()=>
                             "Invalid barrier type");

            Utils.QL_REQUIRE(barrier_lo != null,()=> "no low barrier given");
            Utils.QL_REQUIRE(barrier_hi !=null,()=> "no high barrier given");
            Utils.QL_REQUIRE(rebate != null,()=> "no rebate given");
         }
      }

      //! %Double-Barrier-option %engine base class
      public new class Engine : GenericEngine<DoubleBarrierOption.Arguments,DoubleBarrierOption.Results> 
      {
         protected bool triggered(double underlying)
         {
            return underlying <= arguments_.barrier_lo || underlying >= arguments_.barrier_hi;
         }
      }

   }
}
