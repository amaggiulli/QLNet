//  Copyright (C) 2008-2018 Andrea Maggiulli (a.maggiulli@gmail.com)
//
//  This file is part of QLNet Project https://github.com/amaggiulli/qlnet
//  QLNet is free software: you can redistribute it and/or modify it
//  under the terms of the QLNet license.  You should have received a
//  copy of the license along with this program; if not, license is
//  available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.
//
//  QLNet is a based on QuantLib, a free-software/open-source library
//  for financial quantitative analysts and developers - http://quantlib.org/
//  The QuantLib license is available online at http://quantlib.org/license.shtml.
//
//  This program is distributed in the hope that it will be useful, but WITHOUT
//  ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
//  FOR A PARTICULAR PURPOSE.  See the license for more details.

using System.Collections.Generic;

namespace QLNet
{
   /// <summary>
   /// Catastrophe Bond - CAT
   /// <remarks>
   /// A catastrophe bond (CAT) is a high-yield debt instrument that is usually
   /// insurance-linked and meant to raise money in case of a catastrophe such
   /// as a hurricane or earthquake.
   /// </remarks>
   /// </summary>
   public class CatBond : Bond
   {
      public CatBond(int settlementDays,
                     Calendar calendar,
                     Date issueDate,
                     NotionalRisk notionalRisk)
         : base(settlementDays, calendar, issueDate)
      {
         notionalRisk_ = notionalRisk;
      }

      public override void setupArguments(IPricingEngineArguments args)
      {
         CatBond.Arguments arguments = args as CatBond.Arguments;
         Utils.QL_REQUIRE(arguments != null, () => "wrong arguments type");

         base.setupArguments(args);

         arguments.notionalRisk = notionalRisk_;
         arguments.startDate = issueDate();
      }

      public override void fetchResults(IPricingEngineResults r)
      {
         base.fetchResults(r);
         CatBond.Results results = r as CatBond.Results;
         Utils.QL_REQUIRE(results != null, () => "wrong result type");

         lossProbability_ = results.lossProbability;
         expectedLoss_ = results.expectedLoss;
         exhaustionProbability_ = results.exhaustionProbability;
      }

      public double lossProbability()
      {
         return lossProbability_;
      }

      public double expectedLoss()
      {
         return expectedLoss_;
      }

      public double exhaustionProbability()
      {
         return exhaustionProbability_;
      }

      protected NotionalRisk notionalRisk_;
      protected double lossProbability_;
      protected double exhaustionProbability_;
      protected double expectedLoss_;

      public new class Arguments : Bond.Arguments
      {
         public Date startDate;
         public NotionalRisk notionalRisk;

         public override void validate()
         {
            base.validate();
            Utils.QL_REQUIRE(notionalRisk != null, () => "null notionalRisk");
         }
      }

      public new class Results : Bond.Results
      {
         public double lossProbability;
         public double exhaustionProbability;
         public double expectedLoss;
      }

      public new class Engine : GenericEngine<CatBond.Arguments, CatBond.Results>
      { }
   }

   /// <summary>
   /// floating-rate cat bond (possibly capped and/or floored)
   /// </summary>
   public class FloatingCatBond : CatBond
   {
      public FloatingCatBond(int settlementDays,
                             double faceAmount,
                             Schedule schedule,
                             IborIndex iborIndex,
                             DayCounter paymentDayCounter,
                             NotionalRisk notionalRisk,
                             BusinessDayConvention paymentConvention = QLNet.BusinessDayConvention.Following,
                             int fixingDays = 0,
                             List<double> gearings = null,
                             List<double> spreads = null,
                             List < double? > caps = null,
                             List < double? > floors = null,
                             bool inArrears = false,
                             double redemption = 100.0,
                             Date issueDate = null)
      : base(settlementDays, schedule.calendar(), issueDate, notionalRisk)
      {
         maturityDate_ = schedule.endDate();

         cashflows_ = new IborLeg(schedule, iborIndex)
         .withFixingDays(fixingDays)
         .withGearings(gearings)
         .withSpreads(spreads)
         .withCaps(caps)
         .withFloors(floors)
         .inArrears(inArrears)
         .withPaymentDayCounter(paymentDayCounter)
         .withNotionals(faceAmount)
         .withPaymentAdjustment(paymentConvention);

         addRedemptionsToCashflows(new InitializedList<double>(1, redemption));

         Utils.QL_REQUIRE(!cashflows().empty(), () => "bond with no cashflows!");
         Utils.QL_REQUIRE(redemptions_.Count == 1, () => "multiple redemptions created");

         iborIndex.registerWith(update);
      }

      public FloatingCatBond(int settlementDays,
                             double faceAmount,
                             Date startDate,
                             Date maturityDate,
                             Frequency couponFrequency,
                             Calendar calendar,
                             IborIndex iborIndex,
                             DayCounter accrualDayCounter,
                             NotionalRisk notionalRisk,
                             BusinessDayConvention accrualConvention = BusinessDayConvention.Following,
                             BusinessDayConvention paymentConvention = BusinessDayConvention.Following,
                             int fixingDays = 0,
                             List<double> gearings = null,
                             List<double> spreads = null,
                             List < double? > caps = null,
                             List < double? > floors = null,
                             bool inArrears = false,
                             double redemption = 100.0,
                             Date issueDate = null,
                             Date stubDate = null,
                             DateGeneration.Rule rule = DateGeneration.Rule.Backward,
                             bool endOfMonth = false)
      : base(settlementDays, calendar, issueDate, notionalRisk)
      {
         maturityDate_ = maturityDate;

         Date firstDate = null, nextToLastDate = null;
         switch (rule)
         {
            case DateGeneration.Rule.Backward:
               firstDate = new Date();
               nextToLastDate = stubDate;
               break;
            case DateGeneration.Rule.Forward:
               firstDate = stubDate;
               nextToLastDate = new Date();
               break;
            case DateGeneration.Rule.Zero:
            case DateGeneration.Rule.ThirdWednesday:
            case DateGeneration.Rule.Twentieth:
            case DateGeneration.Rule.TwentiethIMM:
               Utils.QL_FAIL("stub date (" + stubDate + ") not allowed with " +
                             rule + " DateGeneration.Rule");
               break;
            default:
               Utils.QL_FAIL("unknown DateGeneration::Rule (" + rule + ")");
               break;
         }

         Schedule schedule = new Schedule(startDate, maturityDate_, new Period(couponFrequency),
                                          calendar_, accrualConvention, accrualConvention,
                                          rule, endOfMonth, firstDate, nextToLastDate);

         cashflows_ = new IborLeg(schedule, iborIndex)
         .withFixingDays(fixingDays)
         .withGearings(gearings)
         .withSpreads(spreads)
         .withCaps(caps)
         .withFloors(floors)
         .inArrears(inArrears)
         .withPaymentDayCounter(accrualDayCounter)
         .withPaymentAdjustment(paymentConvention)
         .withNotionals(faceAmount);

         addRedemptionsToCashflows(new InitializedList<double>(1, redemption));

         Utils.QL_REQUIRE(!cashflows().empty(), () => "bond with no cashflows!");
         Utils.QL_REQUIRE(redemptions_.Count == 1, () => "multiple redemptions created");

         iborIndex.registerWith(update);
      }
   }
}
