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
   //! Continuous-floating lookback option
   public class ContinuousFloatingLookbackOption : OneAssetOption 
   {
      //! %Arguments for continuous fixed lookback option calculation
      public new class Arguments :  OneAssetOption.Arguments 
      {
         public double? minmax;
         public override void validate()
         {
            base.validate();

            Utils.QL_REQUIRE(minmax != null,()=> "null prior extremum");
            Utils.QL_REQUIRE(minmax >= 0.0,()=> "nonnegative prior extremum required: " + minmax + " not allowed");
         }
      }

      //! %Continuous floating lookback %engine base class
      public new class Engine : GenericEngine<ContinuousFloatingLookbackOption.Arguments,
                                          ContinuousFloatingLookbackOption.Results> 
      {}
      public ContinuousFloatingLookbackOption( double minmax, TypePayoff payoff, Exercise exercise )
         :base(payoff, exercise)
      {
         minmax_ = minmax;
      }

      public override void setupArguments(IPricingEngineArguments args )
      {
         base.setupArguments(args);

         ContinuousFloatingLookbackOption.Arguments moreArgs = args as ContinuousFloatingLookbackOption.Arguments;
         Utils.QL_REQUIRE(moreArgs != null,()=> "wrong argument type");
         moreArgs.minmax = minmax_;
      }
       
      // arguments
      protected double? minmax_;
    }

   //! Continuous-fixed lookback option
   public class ContinuousFixedLookbackOption : OneAssetOption 
   {
      //! %Arguments for continuous fixed lookback option calculation
      public new class Arguments : OneAssetOption.Arguments 
      {
         public double? minmax;
         public override void validate()
         {
            base.validate();

            Utils.QL_REQUIRE(minmax != null,()=> "null prior extremum");
            Utils.QL_REQUIRE(minmax >= 0.0,()=> "nonnegative prior extremum required: "
                   + minmax + " not allowed");
         }
      }

      //! %Continuous fixed lookback %engine base class
      public new class Engine : GenericEngine<ContinuousFixedLookbackOption.Arguments,
                                          ContinuousFixedLookbackOption.Results> 
      {}
      public ContinuousFixedLookbackOption( double minmax, StrikedTypePayoff payoff, Exercise exercise )
         :base(payoff, exercise)
      {
         minmax_ = minmax;
      }
      public override void setupArguments(IPricingEngineArguments args)
      {
         base.setupArguments(args);

         ContinuousFixedLookbackOption.Arguments moreArgs = args as ContinuousFixedLookbackOption.Arguments;
         Utils.QL_REQUIRE(moreArgs != null,()=> "wrong argument type");
         moreArgs.minmax = minmax_;
      }

      protected double minmax_;
   }

   //! Continuous-partial-floating lookback option
   /*! From http://help.rmetrics.org/fExoticOptions/LookbackOptions.html :

      For a partial-time floating strike lookback option, the
      lookback period starts at time zero and ends at an arbitrary
      date before expiration. Except for the partial lookback
      period, the option is similar to a floating strike lookback
      option. The partial-time floating strike lookback option is
      cheaper than a similar standard floating strike lookback
      option. Partial-time floating strike lookback options can be
      priced analytically using a model introduced by Heynen and Kat
      (1994).

   */
   public class ContinuousPartialFloatingLookbackOption : ContinuousFloatingLookbackOption 
   {
      //! %Arguments for continuous partial floating lookback option calculation
      public new class Arguments: ContinuousFloatingLookbackOption.Arguments 
      {
         public double lambda;
         public Date lookbackPeriodEnd;
         public override void validate()
         {
            base.validate();

            EuropeanExercise europeanExercise = exercise as EuropeanExercise;
            Utils.QL_REQUIRE(lookbackPeriodEnd <= europeanExercise.lastDate(), ()=>
               "lookback start date must be earlier than exercise date");
        
            FloatingTypePayoff floatingTypePayoff = payoff as FloatingTypePayoff;
        
            if (floatingTypePayoff.optionType() == Option.Type.Call) 
            {
               Utils.QL_REQUIRE(lambda >= 1.0,()=>
                       "lambda should be greater than or equal to 1 for calls");
            }
            
            if (floatingTypePayoff.optionType() == Option.Type.Put) 
            {
               Utils.QL_REQUIRE(lambda <= 1.0,()=>
                       "lambda should be smaller than or equal to 1 for puts");
            }
         }
      }

      //! %Continuous partial floating lookback %engine base class
      public new class Engine: GenericEngine<ContinuousPartialFloatingLookbackOption.Arguments,
                                         ContinuousPartialFloatingLookbackOption.Results> 
      {}

      public ContinuousPartialFloatingLookbackOption( double minmax, double lambda,
         Date lookbackPeriodEnd,TypePayoff payoff,Exercise exercise)
         :base(minmax, payoff, exercise)
      {
         lambda_ = lambda;
         lookbackPeriodEnd_ = lookbackPeriodEnd;
      }
      public override void setupArguments(IPricingEngineArguments args)
      {
         base.setupArguments(args);

         ContinuousPartialFloatingLookbackOption.Arguments moreArgs = args as ContinuousPartialFloatingLookbackOption.Arguments;
         Utils.QL_REQUIRE(moreArgs != null,()=> "wrong argument type");
         moreArgs.lambda = lambda_;
         moreArgs.lookbackPeriodEnd = lookbackPeriodEnd_;
      }
   
      protected double lambda_;
      protected Date lookbackPeriodEnd_;
   }

       
   //! Continuous-partial-fixed lookback option
   /*! From http://help.rmetrics.org/fExoticOptions/LookbackOptions.html :

      For a partial-time fixed strike lookback option, the lookback
      period starts at a predetermined date after the initialization
      date of the option.  The partial-time fixed strike lookback
      call option payoff is given by the difference between the
      maximum observed price of the underlying asset during the
      lookback period and the fixed strike price. The partial-time
      fixed strike lookback put option payoff is given by the
      difference between the fixed strike price and the minimum
      observed price of the underlying asset during the lookback
      period. The partial-time fixed strike lookback option is
      cheaper than a similar standard fixed strike lookback
      option. Partial-time fixed strike lookback options can be
      priced analytically using a model introduced by Heynen and Kat
      (1994).

   */
   public class ContinuousPartialFixedLookbackOption : ContinuousFixedLookbackOption 
   {
      //! %Arguments for continuous partial fixed lookback option calculation
      public new class Arguments : ContinuousFixedLookbackOption.Arguments 
      {
         public Date lookbackPeriodStart;
         public override void validate()
         {
            base.validate();

            EuropeanExercise europeanExercise = exercise as EuropeanExercise;
            Utils.QL_REQUIRE(lookbackPeriodStart <= europeanExercise.lastDate(), ()=>
               "lookback start date must be earlier than exercise date");
         }
      }
      //! %Continuous partial fixed lookback %engine base class
      public new class Engine : GenericEngine<ContinuousPartialFixedLookbackOption.Arguments,
                                          ContinuousPartialFixedLookbackOption.Results> 
      {}
      public ContinuousPartialFixedLookbackOption(Date lookbackPeriodStart,StrikedTypePayoff payoff,Exercise exercise)
         :base(0, payoff, exercise)
      {
         lookbackPeriodStart_ = lookbackPeriodStart;
      }
      public override void setupArguments(IPricingEngineArguments args)
      {
         base.setupArguments(args);

         ContinuousPartialFixedLookbackOption.Arguments moreArgs = args as ContinuousPartialFixedLookbackOption.Arguments;
         Utils.QL_REQUIRE(moreArgs != null,()=> "wrong argument type");
         moreArgs.lookbackPeriodStart = lookbackPeriodStart_;
      }
   
      protected Date lookbackPeriodStart_;
   }

}
