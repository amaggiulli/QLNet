/*
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
 Copyright (C) 2008-2016  Andrea Maggiulli (a.maggiulli@gmail.com)

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
using System.Collections.Generic;

namespace QLNet
{

   //! Continuous-averaging Asian option
//    ! \todo add running average
//
//        \ingroup instruments
//
   public class ContinuousAveragingAsianOption : OneAssetOption
   {
      public new class Arguments : OneAssetOption.Arguments
      {
         public Arguments()
         {
            averageType = Average.Type.NULL;
         }
         public override void validate()
         {
            base.validate();
            Utils.QL_REQUIRE(averageType != Average.Type.NULL, () => "unspecified average type");
         }
         public Average.Type averageType { get; set; }
      }

      public new class Engine: GenericEngine<ContinuousAveragingAsianOption.Arguments, ContinuousAveragingAsianOption.Results>
      {
      }

      public ContinuousAveragingAsianOption(Average.Type averageType, StrikedTypePayoff payoff, Exercise exercise) : base(payoff, exercise)
      {
         averageType_ = averageType;
      }
      public override void setupArguments(IPricingEngineArguments args)
      {

         base.setupArguments(args);

         ContinuousAveragingAsianOption.Arguments moreArgs = args as ContinuousAveragingAsianOption.Arguments;
         Utils.QL_REQUIRE(moreArgs != null, () => "wrong argument type");
         moreArgs.averageType = averageType_;
      }
      protected Average.Type averageType_;
   }

   //! Discrete-averaging Asian option
   //! \ingroup instruments
   public class DiscreteAveragingAsianOption : OneAssetOption
   {
      public new class Arguments : OneAssetOption.Arguments
      {
         public Arguments()
         {
            averageType = Average.Type.NULL;
            runningAccumulator = null;
            pastFixings = null;
         }
         public override void validate()
         {
            base.validate();

            Utils.QL_REQUIRE(averageType != Average.Type.NULL, () => "unspecified average type");
            Utils.QL_REQUIRE(pastFixings != null, () => "null past-fixing number");
            Utils.QL_REQUIRE(runningAccumulator != null, () => "null running product");

            switch (averageType)
            {
               case Average.Type.Arithmetic:
                  Utils.QL_REQUIRE(runningAccumulator >= 0.0, () =>
                                   "non negative running sum required: " + runningAccumulator + " not allowed");
                  break;
               case Average.Type.Geometric:
                  Utils.QL_REQUIRE(runningAccumulator > 0.0, () =>
                                   "positive running product required: " + runningAccumulator + " not allowed");
                  break;
               default:
                  Utils.QL_FAIL("invalid average type");
                  break;
            }

            // check fixingTimes_ here
         }
         public Average.Type averageType { get; set; }
         public double? runningAccumulator { get; set; }
         public int? pastFixings { get; set; }
         public List<Date> fixingDates { get; set; }
      }

      public new class Engine: GenericEngine<DiscreteAveragingAsianOption.Arguments, DiscreteAveragingAsianOption.Results>
      {
      }

      public DiscreteAveragingAsianOption(Average.Type averageType, double? runningAccumulator, int? pastFixings, List<Date> fixingDates, StrikedTypePayoff payoff, Exercise exercise)
      : base(payoff, exercise)
      {
         averageType_ = averageType;
         runningAccumulator_ = runningAccumulator;
         pastFixings_ = pastFixings;
         fixingDates_ = fixingDates;

         fixingDates_.Sort();
      }

      public override void setupArguments(IPricingEngineArguments args)
      {

         base.setupArguments(args);

         DiscreteAveragingAsianOption.Arguments moreArgs = args as DiscreteAveragingAsianOption.Arguments;
         Utils.QL_REQUIRE(moreArgs != null, () => "wrong argument type");

         moreArgs.averageType = averageType_;
         moreArgs.runningAccumulator = runningAccumulator_;
         moreArgs.pastFixings = pastFixings_;
         moreArgs.fixingDates = fixingDates_;
      }
      protected Average.Type averageType_;
      protected double? runningAccumulator_;
      protected int? pastFixings_;
      protected List<Date> fixingDates_;
   }
}
