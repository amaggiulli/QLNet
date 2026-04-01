/*
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
 Copyright (C) 2008-2025  Andrea Maggiulli (a.maggiulli@gmail.com)

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

   /// <summary>
   /// Continuous-averaging Asian option.
   /// </summary>
   // TODO: add running average.
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

   /// <summary>
   /// Discrete-averaging Asian option.
   /// </summary>
   public class DiscreteAveragingAsianOption : OneAssetOption
   {
      protected Average.Type averageType_;
      protected double? runningAccumulator_;
      protected int? pastFixings_;
      protected List<Date> fixingDates_;
      protected bool allPastFixingsProvided_;
      protected List<double> allPastFixings_;

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

      public DiscreteAveragingAsianOption(Average.Type averageType, double? runningAccumulator, int? pastFixings,
         List<Date> fixingDates, StrikedTypePayoff payoff, Exercise exercise)
      : base(payoff, exercise)
      {
         averageType_ = averageType;
         runningAccumulator_ = runningAccumulator;
         pastFixings_ = pastFixings;
         fixingDates_ = fixingDates;
         allPastFixingsProvided_ = false;

         fixingDates_.Sort();
         // Add a hard override to the runningAccumulator if pastFixings is 0
         // (ie. the option is unseasoned)
         if (pastFixings_ == 0)
         {
            if (averageType == Average.Type.Geometric)
            {
               runningAccumulator_ = 1.0;
            }
            else if (averageType == Average.Type.Arithmetic)
            {
               runningAccumulator_ = 0.0;
            }
            else
            {
               Utils.QL_FAIL("Unrecognised average type, must be Average::Arithmetic or Average::Geometric");
            }
         }
      }

      public DiscreteAveragingAsianOption(Average.Type averageType, List<Date> fixingDates, StrikedTypePayoff payoff,
         Exercise exercise, List<double> allPastFixings) : base(payoff, exercise)
      {
         averageType_ = averageType;
         runningAccumulator_ = 0.0;
         pastFixings_ = 0;
         fixingDates_ = fixingDates;
         allPastFixingsProvided_ = true;
         allPastFixings_ = allPastFixings;
      }
      public override void setupArguments(IPricingEngineArguments args)
      {
         var runningAccumulator = runningAccumulator_;
         var pastFixings = pastFixings_;
         var fixingDates = fixingDates_;

         // If the option was initialised with a list of fixings, before pricing we
         // compare the evaluation date to the fixing dates, and set up the pastFixings,
         // fixingDates, and runningAccumulator accordingly
         if (allPastFixingsProvided_)
         {
            var futureFixingDates = new List<Date>();
            var today = Settings.evaluationDate();

            pastFixings = 0;
            foreach (var fixingDate in fixingDates_)
            {
               if (fixingDate < today)
               {
                  pastFixings += 1;
               }
               else
               {
                  futureFixingDates.Add(fixingDate);
               }
            }
            fixingDates = futureFixingDates;

            if (pastFixings > allPastFixings_.Count)
               Utils.QL_FAIL("Not enough past fixings have been provided for the required historical fixing dates");

            if (averageType_ == Average.Type.Geometric)
            {
               runningAccumulator = 1.0;
               for (var i=0; i<pastFixings; i++)
                  runningAccumulator *= allPastFixings_[i];

            }
            else if (averageType_ == Average.Type.Arithmetic)
            {
               runningAccumulator = 0.0;
               for (var i=0; i<pastFixings; i++)
                  runningAccumulator += allPastFixings_[i];

            }
            else
            {
               Utils.QL_FAIL("Unrecognised average type, must be Average::Arithmetic or Average::Geometric");
            }

         }

         base.setupArguments(args);

         DiscreteAveragingAsianOption.Arguments moreArgs = args as DiscreteAveragingAsianOption.Arguments;
         Utils.QL_REQUIRE(moreArgs != null, () => "wrong argument type");

         moreArgs.averageType = averageType_;
         moreArgs.runningAccumulator = runningAccumulator;
         moreArgs.pastFixings = pastFixings;
         moreArgs.fixingDates = fixingDates;
      }
   }
}
