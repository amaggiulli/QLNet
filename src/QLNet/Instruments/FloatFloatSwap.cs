//  Copyright (C) 2008-2017 Andrea Maggiulli (a.maggiulli@gmail.com)
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
using System;
using System.Collections.Generic;
using System.Linq;

namespace QLNet
{
   /// <summary>
   /// float float swap
   /// </summary>
   public class FloatFloatSwap : Swap
   {
      public FloatFloatSwap(VanillaSwap.Type type,
                            double nominal1,
                            double nominal2,
                            Schedule schedule1,
                            InterestRateIndex index1,
                            DayCounter dayCount1,
                            Schedule schedule2,
                            InterestRateIndex index2,
                            DayCounter dayCount2,
                            bool intermediateCapitalExchange = false,
                            bool finalCapitalExchange = false,
                            double gearing1 = 1.0,
                            double spread1 = 0.0,
                            double? cappedRate1 = null,
                            double? flooredRate1 = null,
                            double gearing2 = 1.0,
                            double spread2 = 0.0,
                            double? cappedRate2 = null,
                            double? flooredRate2 = null,
                            BusinessDayConvention? paymentConvention1 = null,
                            BusinessDayConvention? paymentConvention2 = null)
      : base(2)
      {
         type_ = type;
         nominal1_ = new InitializedList<double> (schedule1.size()  - 1, nominal1);
         nominal2_ = new InitializedList<double> (schedule2.size() - 1, nominal2);
         schedule1_ = schedule1;
         schedule2_ = schedule2;
         index1_ = index1;
         index2_ = index2;
         gearing1_ = new InitializedList<double>(schedule1.size() - 1, gearing1);
         gearing2_ = new InitializedList<double>(schedule2.size() - 1, gearing2);
         spread1_ = new InitializedList<double>(schedule1.size() - 1, spread1);
         spread2_ = new InitializedList<double>(schedule2.size() - 1, spread2);
         cappedRate1_ = new InitializedList < double? >(schedule1.size() - 1, cappedRate1);
         flooredRate1_ = new InitializedList < double? >(schedule1.size() - 1, flooredRate1);
         cappedRate2_ = new InitializedList < double? >(schedule2.size() - 1, cappedRate2);
         flooredRate2_ = new InitializedList < double? >(schedule2.size() - 1, flooredRate2);
         dayCount1_ = dayCount1; dayCount2_ = dayCount2;
         intermediateCapitalExchange_ = intermediateCapitalExchange;
         finalCapitalExchange_ = finalCapitalExchange;

         init(paymentConvention1, paymentConvention2);
      }

      public FloatFloatSwap(VanillaSwap.Type type,
                            List<double> nominal1,
                            List<double> nominal2,
                            Schedule schedule1,
                            InterestRateIndex index1,
                            DayCounter dayCount1,
                            Schedule schedule2,
                            InterestRateIndex index2,
                            DayCounter dayCount2,
                            bool intermediateCapitalExchange = false,
                            bool finalCapitalExchange = false,
                            List<double> gearing1 = null,
                            List<double> spread1 = null,
                            List < double? > cappedRate1 = null,
                            List < double? > flooredRate1 = null,
                            List<double> gearing2 = null,
                            List<double> spread2 = null,
                            List < double? > cappedRate2 = null,
                            List < double? > flooredRate2 = null,
                            BusinessDayConvention? paymentConvention1 = null,
                            BusinessDayConvention? paymentConvention2 = null)
      : base(2)
      {
         type_ = type;
         nominal1_ = nominal1;
         nominal2_ = nominal2;
         schedule1_ = schedule1;
         schedule2_ = schedule2;
         index1_ = index1;
         index2_ = index2;
         gearing1_ = gearing1;
         gearing2_ = gearing2;
         spread1_ = spread1;
         spread2_ = spread2;
         cappedRate1_ = cappedRate1;
         flooredRate1_ = flooredRate1;
         cappedRate2_ = cappedRate2;
         flooredRate2_ = flooredRate2;
         dayCount1_ = dayCount1;
         dayCount2_ = dayCount2;
         intermediateCapitalExchange_ = intermediateCapitalExchange;
         finalCapitalExchange_ = finalCapitalExchange;

         init(paymentConvention1, paymentConvention2);
      }

      //! \name Inspectors
      public VanillaSwap.Type type() { return type_; }
      public List<double> nominal1() { return nominal1_; }
      public List<double> nominal2() { return nominal2_; }

      public Schedule schedule1() { return schedule1_; }
      public Schedule schedule2() { return schedule2_; }

      public InterestRateIndex index1() { return index1_; }
      public InterestRateIndex index2() { return index2_; }

      public List<double> spread1() { return spread1_; }
      public List<double> spread2() { return spread2_; }

      public List<double> gearing1() { return gearing1_; }
      public List<double> gearing2() { return gearing2_; }

      public List < double? > cappedRate1() { return cappedRate1_; }
      public List < double? > flooredRate1() { return flooredRate1_; }
      public List < double? > cappedRate2() { return cappedRate2_; }
      public List < double? > flooredRate2() { return flooredRate2_; }

      public DayCounter dayCount1() { return dayCount1_; }
      public DayCounter dayCount2() { return dayCount2_; }

      public BusinessDayConvention paymentConvention1() { return paymentConvention1_; }
      public BusinessDayConvention paymentConvention2() { return paymentConvention2_; }

      public List<CashFlow> leg1() { return legs_[0]; }
      public List<CashFlow> leg2() { return legs_[1]; }

      // other
      public override void setupArguments(IPricingEngineArguments args)
      {
         base.setupArguments(args);

         Arguments arguments = args as Arguments;

         Utils.QL_REQUIRE(arguments != null, () => "argument type does not match");

         arguments.type = type_;
         arguments.nominal1 = nominal1_;
         arguments.nominal2 = nominal2_;
         arguments.index1 = index1_;
         arguments.index2 = index2_;

         List<CashFlow> leg1Coupons = leg1();
         List<CashFlow> leg2Coupons = leg2();

         arguments.leg1ResetDates = arguments.leg1PayDates =
                                       arguments.leg1FixingDates = new InitializedList<Date>(leg1Coupons.Count);
         arguments.leg2ResetDates = arguments.leg2PayDates =
                                       arguments.leg2FixingDates = new InitializedList<Date>(leg2Coupons.Count);

         arguments.leg1Spreads = arguments.leg1AccrualTimes =
                                    arguments.leg1Gearings = new InitializedList<double>(leg1Coupons.Count);
         arguments.leg2Spreads = arguments.leg2AccrualTimes =
                                    arguments.leg2Gearings = new InitializedList<double>(leg2Coupons.Count);

         arguments.leg1Coupons = new InitializedList < double? >(leg1Coupons.Count, null);
         arguments.leg2Coupons = new InitializedList < double? >(leg2Coupons.Count, null);

         arguments.leg1IsRedemptionFlow = new InitializedList<bool>(leg1Coupons.Count, false);
         arguments.leg2IsRedemptionFlow = new InitializedList<bool>(leg2Coupons.Count, false);

         arguments.leg1CappedRates = arguments.leg1FlooredRates =
                                        new InitializedList < double? >(leg1Coupons.Count, null);
         arguments.leg2CappedRates = arguments.leg2FlooredRates =
                                        new InitializedList < double? >(leg2Coupons.Count, null);

         for (int i = 0; i < leg1Coupons.Count; ++i)
         {
            FloatingRateCoupon coupon = leg1Coupons[i] as FloatingRateCoupon;
            if (coupon != null)
            {
               arguments.leg1AccrualTimes[i] = coupon.accrualPeriod();
               arguments.leg1PayDates[i] = coupon.date();
               arguments.leg1ResetDates[i] = coupon.accrualStartDate();
               arguments.leg1FixingDates[i] = coupon.fixingDate();
               arguments.leg1Spreads[i] = coupon.spread();
               arguments.leg1Gearings[i] = coupon.gearing();
               try
               {
                  arguments.leg1Coupons[i] = coupon.amount();
               }
               catch (Exception)
               {
                  arguments.leg1Coupons[i] = null;
               }
               CappedFlooredCoupon cfcoupon = leg1Coupons[i] as CappedFlooredCoupon;
               if (cfcoupon != null)
               {
                  arguments.leg1CappedRates[i] = cfcoupon.cap();
                  arguments.leg1FlooredRates[i] = cfcoupon.floor();
               }
            }
            else
            {
               CashFlow cashflow = leg1Coupons[i] as CashFlow;
               int j = arguments.leg1PayDates.FindIndex(x => x == cashflow.date());
               Utils.QL_REQUIRE(j != -1, () =>
                                "nominal redemption on " + cashflow.date() + "has no corresponding coupon");
               int jIdx = j; // Size jIdx = j - arguments->leg1PayDates.begin();
               arguments.leg1IsRedemptionFlow[i] = true;
               arguments.leg1Coupons[i] = cashflow.amount();
               arguments.leg1ResetDates[i] = arguments.leg1ResetDates[jIdx];
               arguments.leg1FixingDates[i] = arguments.leg1FixingDates[jIdx];
               arguments.leg1AccrualTimes[i] = 0.0;
               arguments.leg1Spreads[i] = 0.0;
               arguments.leg1Gearings[i] = 1.0;
               arguments.leg1PayDates[i] = cashflow.date();
            }
         }

         for (int i = 0; i < leg2Coupons.Count; ++i)
         {
            FloatingRateCoupon coupon = leg2Coupons[i] as FloatingRateCoupon;
            if (coupon != null)
            {
               arguments.leg2AccrualTimes[i] = coupon.accrualPeriod();
               arguments.leg2PayDates[i] = coupon.date();
               arguments.leg2ResetDates[i] = coupon.accrualStartDate();
               arguments.leg2FixingDates[i] = coupon.fixingDate();
               arguments.leg2Spreads[i] = coupon.spread();
               arguments.leg2Gearings[i] = coupon.gearing();
               try
               {
                  arguments.leg2Coupons[i] = coupon.amount();
               }
               catch (Exception)
               {
                  arguments.leg2Coupons[i] = null;
               }
               CappedFlooredCoupon cfcoupon = leg2Coupons[i] as CappedFlooredCoupon;
               if (cfcoupon != null)
               {
                  arguments.leg2CappedRates[i] = cfcoupon.cap();
                  arguments.leg2FlooredRates[i] = cfcoupon.floor();
               }
            }
            else
            {
               CashFlow cashflow = leg2Coupons[i] as CashFlow;
               int j = arguments.leg2PayDates.FindIndex(x => x == cashflow.date());
               Utils.QL_REQUIRE(j != -1, () =>
                                "nominal redemption on " + cashflow.date() + "has no corresponding coupon");
               int jIdx = j; // j - arguments->leg2PayDates.begin();
               arguments.leg2IsRedemptionFlow[i] = true;
               arguments.leg2Coupons[i] = cashflow.amount();
               arguments.leg2ResetDates[i] = arguments.leg2ResetDates[jIdx];
               arguments.leg2FixingDates[i] =
                  arguments.leg2FixingDates[jIdx];
               arguments.leg2AccrualTimes[i] = 0.0;
               arguments.leg2Spreads[i] = 0.0;
               arguments.leg2Gearings[i] = 1.0;
               arguments.leg2PayDates[i] = cashflow.date();
            }
         }
      }


      public override void fetchResults(IPricingEngineResults res) { base.fetchResults(res);}

      private void init(BusinessDayConvention? paymentConvention1, BusinessDayConvention? paymentConvention2)
      {
         Utils.QL_REQUIRE(nominal1_.Count == schedule1_.Count - 1, () =>
                          "nominal1 size (" + nominal1_.Count +
                          ") does not match schedule1 size (" + schedule1_.size() + ")");
         Utils.QL_REQUIRE(nominal2_.Count == schedule2_.Count - 1, () =>
                          "nominal2 size (" + nominal2_.Count + ") does not match schedule2 size ("
                          + nominal2_.Count + ")");
         Utils.QL_REQUIRE(gearing1_.Count == 0 || gearing1_.Count == nominal1_.Count, () =>
                          "nominal1 size (" + nominal1_.Count + ") does not match gearing1 size ("
                          + gearing1_.Count + ")");
         Utils.QL_REQUIRE(gearing2_.Count == 0 || gearing2_.Count == nominal2_.Count, () =>
                          "nominal2 size (" + nominal2_.Count + ") does not match gearing2 size ("
                          + gearing2_.Count + ")");
         Utils.QL_REQUIRE(cappedRate1_.Count == 0 || cappedRate1_.Count == nominal1_.Count, () =>
                          "nominal1 size (" + nominal1_.Count + ") does not match cappedRate1 size ("
                          + cappedRate1_.Count + ")");
         Utils.QL_REQUIRE(cappedRate2_.Count == 0 || cappedRate2_.Count == nominal2_.Count, () =>
                          "nominal2 size (" + nominal2_.Count + ") does not match cappedRate2 size ("
                          + cappedRate2_.Count + ")");
         Utils.QL_REQUIRE(flooredRate1_.Count == 0 || flooredRate1_.Count == nominal1_.Count, () =>
                          "nominal1 size (" + nominal1_.Count + ") does not match flooredRate1 size ("
                          + flooredRate1_.Count + ")");
         Utils.QL_REQUIRE(flooredRate2_.Count == 0 || flooredRate2_.Count == nominal2_.Count, () =>
                          "nominal2 size (" + nominal2_.Count + ") does not match flooredRate2 size ("
                          + flooredRate2_.Count + ")");

         if (paymentConvention1 != null)
            paymentConvention1_ = paymentConvention1.Value;
         else
            paymentConvention1_ = schedule1_.businessDayConvention();

         if (paymentConvention2 != null)
            paymentConvention2_ = paymentConvention2.Value;
         else
            paymentConvention2_ = schedule2_.businessDayConvention();

         if (gearing1_.Count == 0)
            gearing1_ = new InitializedList<double>(nominal1_.Count, 1.0);
         if (gearing2_.Count == 0)
            gearing2_ = new InitializedList<double>(nominal2_.Count, 1.0);
         if (spread1_.Count == 0)
            spread1_ = new InitializedList<double>(nominal1_.Count, 0.0);
         if (spread2_.Count == 0)
            spread2_ = new InitializedList<double>(nominal2_.Count, 0.0);
         if (cappedRate1_.Count == 0)
            cappedRate1_ = new InitializedList < double? >(nominal1_.Count, null);
         if (cappedRate2_.Count == 0)
            cappedRate2_ = new InitializedList < double? >(nominal2_.Count, null);
         if (flooredRate1_.Count == 0)
            flooredRate1_ = new InitializedList < double? >(nominal1_.Count, null);
         if (flooredRate2_.Count == 0)
            flooredRate2_ = new InitializedList < double? >(nominal2_.Count, null);

         bool isNull = cappedRate1_[0] == null;
         for (int i = 0; i < cappedRate1_.Count; i++)
         {
            if (isNull)
               Utils.QL_REQUIRE(cappedRate1_[i] == null, () =>
                                "cappedRate1 must be null for all or none entry (" + (i + 1)
                                + "th is " + cappedRate1_[i] + ")");
            else
               Utils.QL_REQUIRE(cappedRate1_[i] != null, () =>
                                "cappedRate 1 must be null for all or none entry ("
                                + "1st is " + cappedRate1_[0] + ")");
         }
         isNull = cappedRate2_[0] == null;
         for (int i = 0; i < cappedRate2_.Count; i++)
         {
            if (isNull)
               Utils.QL_REQUIRE(cappedRate2_[i] == null, () =>
                                "cappedRate2 must be null for all or none entry ("
                                + (i + 1) + "th is " + cappedRate2_[i] + ")");
            else
               Utils.QL_REQUIRE(cappedRate2_[i] != null, () =>
                                "cappedRate2 must be null for all or none entry ("
                                + "1st is " + cappedRate2_[0] + ")");
         }
         isNull = flooredRate1_[0] == null;
         for (int i = 0; i < flooredRate1_.Count; i++)
         {
            if (isNull)
               Utils.QL_REQUIRE(flooredRate1_[i] == null, () =>
                                "flooredRate1 must be null for all or none entry ("
                                + (i + 1) + "th is " + flooredRate1_[i]
                                + ")");
            else
               Utils.QL_REQUIRE(flooredRate1_[i] != null, () =>
                                "flooredRate 1 must be null for all or none entry ("
                                + "1st is " + flooredRate1_[0] + ")");
         }
         isNull = flooredRate2_[0] == null;
         for (int i = 0; i < flooredRate2_.Count; i++)
         {
            if (isNull)
               Utils.QL_REQUIRE(flooredRate2_[i] == null, () =>
                                "flooredRate2 must be null for all or none entry ("
                                + (i + 1) + "th is " + flooredRate2_[i]
                                + ")");
            else
               Utils.QL_REQUIRE(flooredRate2_[i] != null, () =>
                                "flooredRate2 must be null for all or none entry ("
                                + "1st is " + flooredRate2_[0] + ")");
         }

         // if the gearing is zero then the ibor / cms leg will be set up with
         // fixed coupons which makes trouble here in this context. We therefore
         // use a dirty trick and enforce the gearing to be non zero.
         for (int i = 0; i < gearing1_.Count; i++)
            if (Utils.close(gearing1_[i], 0.0))
               gearing1_[i] = Const.QL_EPSILON;
         for (int i = 0; i < gearing2_.Count; i++)
            if (Utils.close(gearing2_[i], 0.0))
               gearing2_[i] = Const.QL_EPSILON;

         IborIndex ibor1 = index1_ as IborIndex;
         IborIndex ibor2 = index2_ as IborIndex;
         SwapIndex cms1 =  index1_ as SwapIndex;
         SwapIndex cms2 =  index2_ as SwapIndex;
         SwapSpreadIndex cmsspread1 = index1_ as SwapSpreadIndex;
         SwapSpreadIndex cmsspread2 = index2_ as SwapSpreadIndex;

         Utils.QL_REQUIRE(ibor1 != null || cms1 != null || cmsspread1 != null, () =>
                          "index1 must be ibor or cms or cms spread");
         Utils.QL_REQUIRE(ibor2 != null || cms2 != null || cmsspread2 != null, () =>
                          "index2 must be ibor or cms");

         if (ibor1 != null)
         {
            IborLeg leg = new IborLeg(schedule1_, ibor1);
            leg = (IborLeg)leg.withPaymentDayCounter(dayCount1_)
                  .withSpreads(spread1_)
                  .withGearings(gearing1_)
                  .withPaymentAdjustment(paymentConvention1_)
                  .withNotionals(nominal1_);

            if (cappedRate1_[0] != null)
               leg = (IborLeg) leg.withCaps(cappedRate1_);
            if (flooredRate1_[0] != null)
               leg = (IborLeg) leg.withFloors(flooredRate1_);
            legs_[0] = leg;
         }

         if (ibor2 != null)
         {
            IborLeg leg = new IborLeg(schedule2_, ibor2);
            leg = (IborLeg) leg.withPaymentDayCounter(dayCount2_)
                  .withSpreads(spread2_)
                  .withGearings(gearing2_)
                  .withPaymentAdjustment(paymentConvention2_)
                  .withNotionals(nominal2_);

            if (cappedRate2_[0] != null)
               leg = (IborLeg) leg.withCaps(cappedRate2_);
            if (flooredRate2_[0] != null)
               leg = (IborLeg) leg.withFloors(flooredRate2_);
            legs_[1] = leg;
         }

         if (cms1 != null)
         {
            CmsLeg leg = new CmsLeg(schedule1_, cms1);
            leg = (CmsLeg) leg.withPaymentDayCounter(dayCount1_)
                  .withSpreads(spread1_)
                  .withGearings(gearing1_)
                  .withNotionals(nominal1_)
                  .withPaymentAdjustment(paymentConvention1_);

            if (cappedRate1_[0] != null)
               leg = (CmsLeg) leg.withCaps(cappedRate1_);
            if (flooredRate1_[0] != null)
               leg = (CmsLeg) leg.withFloors(flooredRate1_);
            legs_[0] = leg;
         }

         if (cms2 != null)
         {
            CmsLeg leg = new CmsLeg(schedule2_, cms2);
            leg = (CmsLeg) leg.withPaymentDayCounter(dayCount2_)
                  .withSpreads(spread2_)
                  .withGearings(gearing2_)
                  .withNotionals(nominal2_)
                  .withPaymentAdjustment(paymentConvention2_);

            if (cappedRate2_[0] != null)
               leg = (CmsLeg) leg.withCaps(cappedRate2_);
            if (flooredRate2_[0] != null)
               leg = (CmsLeg) leg.withFloors(flooredRate2_);
            legs_[1] = leg;
         }

         if (cmsspread1 != null)
         {
            CmsSpreadLeg leg = new CmsSpreadLeg(schedule1_, cmsspread1);
            leg = (CmsSpreadLeg) leg.withPaymentDayCounter(dayCount1_)
                  .withSpreads(spread1_)
                  .withGearings(gearing1_)
                  .withNotionals(nominal1_)
                  .withPaymentAdjustment(paymentConvention1_);
            if (cappedRate1_[0] != null)
               leg = (CmsSpreadLeg) leg.withCaps(cappedRate1_);
            if (flooredRate1_[0] != null)
               leg = (CmsSpreadLeg) leg.withFloors(flooredRate1_);
            legs_[0] = leg;
         }

         if (cmsspread2 != null)
         {
            CmsSpreadLeg leg = new CmsSpreadLeg(schedule2_, cmsspread2);
            leg = (CmsSpreadLeg) leg.withPaymentDayCounter(dayCount2_)
                  .withSpreads(spread2_)
                  .withGearings(gearing2_)
                  .withNotionals(nominal2_)
                  .withPaymentAdjustment(paymentConvention2_);

            if (cappedRate2_[0] != null)
               leg = (CmsSpreadLeg) leg.withCaps(cappedRate2_);
            if (flooredRate2_[0] != null)
               leg = (CmsSpreadLeg) leg.withFloors(flooredRate2_);
            legs_[1] = leg;
         }

         if (intermediateCapitalExchange_)
         {
            for (int i = 0; i < legs_[0].Count - 1; i++)
            {
               double cap = nominal1_[i] - nominal1_[i + 1];
               if (!Utils.close(cap, 0.0))
               {
                  legs_[0].Insert(i + 1, new Redemption(cap, legs_[0][i].date()));
                  nominal1_.Insert(i + 1, nominal1_[i]);
                  i++;
               }
            }
            for (int i = 0; i < legs_[1].Count - 1; i++)
            {
               double cap = nominal2_[i] - nominal2_[i + 1];
               if (!Utils.close(cap, 0.0))
               {
                  legs_[1].Insert(i + 1, new Redemption(cap, legs_[1][i].date()));
                  nominal2_.Insert(i + 1, nominal2_[i]);
                  i++;
               }
            }
         }

         if (finalCapitalExchange_)
         {
            legs_[0].Add(new Redemption(nominal1_.Last(), legs_[0].Last().date()));
            nominal1_.Add(nominal1_.Last());
            legs_[1].Add(new Redemption(nominal2_.Last(), legs_[1].Last().date()));
            nominal2_.Add(nominal2_.Last());
         }

         foreach (var c in legs_[0])
            c.registerWith(update);

         foreach (var c in legs_[1])
            c.registerWith(update);

         switch (type_)
         {
            case VanillaSwap.Type.Payer:
               payer_[0] = -1.0;
               payer_[1] = +1.0;
               break;
            case VanillaSwap.Type.Receiver:
               payer_[0] = +1.0;
               payer_[1] = -1.0;
               break;
            default:
               Utils.QL_FAIL("Unknown float float - swap type");
               break;
         }
      }

      private VanillaSwap.Type type_;
      private List<double> nominal1_, nominal2_;
      private Schedule schedule1_, schedule2_;
      private InterestRateIndex index1_, index2_;
      private List<double> gearing1_, gearing2_, spread1_, spread2_;
      private List < double? > cappedRate1_, flooredRate1_, cappedRate2_, flooredRate2_;
      private DayCounter dayCount1_, dayCount2_;
      //private List<bool> isRedemptionFlow1_, isRedemptionFlow2_;
      private BusinessDayConvention paymentConvention1_, paymentConvention2_;
      private bool intermediateCapitalExchange_, finalCapitalExchange_;

      /// <summary>
      /// Arguments for float float swap calculation
      /// </summary>
      public new class Arguments : Swap.Arguments
      {
         public Arguments()  { type = VanillaSwap.Type.Receiver;}
         public VanillaSwap.Type type { get; set; }
         public List<double> nominal1 { get; set; }
         public List<double> nominal2 { get; set; }

         public List<Date> leg1ResetDates { get; set; }
         public List<Date> leg1FixingDates { get; set; }
         public List<Date> leg1PayDates { get; set; }

         public List<Date> leg2ResetDates { get; set; }
         public List<Date> leg2FixingDates { get; set; }
         public List<Date> leg2PayDates { get; set; }

         public List<double> leg1Spreads { get; set; }
         public List<double> leg2Spreads { get; set; }
         public List<double> leg1Gearings { get; set; }
         public List<double> leg2Gearings { get; set; }

         public List < double? > leg1CappedRates { get; set; }
         public List < double? > leg1FlooredRates { get; set; }
         public List < double? > leg2CappedRates { get; set; }
         public List < double? > leg2FlooredRates { get; set; }

         public List < double? > leg1Coupons { get; set; }
         public List < double? > leg2Coupons { get; set; }

         public List<double> leg1AccrualTimes { get; set; }
         public List<double> leg2AccrualTimes { get; set; }

         public InterestRateIndex index1 { get; set; }
         public InterestRateIndex index2 { get; set; }

         public List<bool> leg1IsRedemptionFlow { get; set; }
         public List<bool> leg2IsRedemptionFlow { get; set; }

         public override void validate()
         {
            base.validate();

            Utils.QL_REQUIRE(nominal1.Count ==  leg1ResetDates.Count, () =>
                             "nominal1 size is different from resetDates1 size");
            Utils.QL_REQUIRE(nominal1.Count ==  leg1FixingDates.Count, () =>
                             "nominal1 size is different from fixingDates1 size");
            Utils.QL_REQUIRE(nominal1.Count ==  leg1PayDates.Count, () =>
                             "nominal1 size is different from payDates1 size");
            Utils.QL_REQUIRE(nominal1.Count ==  leg1Spreads.Count, () =>
                             "nominal1 size is different from spreads1 size");
            Utils.QL_REQUIRE(nominal1.Count ==  leg1Gearings.Count, () =>
                             "nominal1 size is different from gearings1 size");
            Utils.QL_REQUIRE(nominal1.Count ==  leg1CappedRates.Count, () =>
                             "nominal1 size is different from cappedRates1 size");
            Utils.QL_REQUIRE(nominal1.Count ==  leg1FlooredRates.Count, () =>
                             "nominal1 size is different from flooredRates1 size");
            Utils.QL_REQUIRE(nominal1.Count ==  leg1Coupons.Count, () =>
                             "nominal1 size is different from coupons1 size");
            Utils.QL_REQUIRE(nominal1.Count ==  leg1AccrualTimes.Count, () =>
                             "nominal1 size is different from accrualTimes1 size");
            Utils.QL_REQUIRE(nominal1.Count ==  leg1IsRedemptionFlow.Count, () =>
                             "nominal1 size is different from redemption1 size");

            Utils.QL_REQUIRE(nominal2.Count ==  leg2ResetDates.Count, () =>
                             "nominal2 size is different from resetDates2 size");
            Utils.QL_REQUIRE(nominal2.Count ==  leg2FixingDates.Count, () =>
                             "nominal2 size is different from fixingDates2 size");
            Utils.QL_REQUIRE(nominal2.Count ==  leg2PayDates.Count, () =>
                             "nominal2 size is different from payDates2 size");
            Utils.QL_REQUIRE(nominal2.Count ==  leg2Spreads.Count, () =>
                             "nominal2 size is different from spreads2 size");
            Utils.QL_REQUIRE(nominal2.Count ==  leg2Gearings.Count, () =>
                             "nominal2 size is different from gearings2 size");
            Utils.QL_REQUIRE(nominal2.Count ==  leg2CappedRates.Count, () =>
                             "nominal2 size is different from cappedRates2 size");
            Utils.QL_REQUIRE(nominal2.Count ==  leg2FlooredRates.Count, () =>
                             "nominal2 size is different from flooredRates2 size");
            Utils.QL_REQUIRE(nominal2.Count ==  leg2Coupons.Count, () =>
                             "nominal2 size is different from coupons2 size");
            Utils.QL_REQUIRE(nominal2.Count ==  leg2AccrualTimes.Count, () =>
                             "nominal2 size is different from accrualTimes2 size");
            Utils.QL_REQUIRE(nominal2.Count ==  leg2IsRedemptionFlow.Count, () =>
                             "nominal2 size is different from redemption2 size");

            Utils.QL_REQUIRE(index1 != null, () => "index1 is null");
            Utils.QL_REQUIRE(index2 != null, () => "index2 is null");

         }

      }

      //! %Results from float float swap calculation
      public new class Results : Swap.Results
      {}

      public class Engine : GenericEngine<Arguments, Results>
      {}
   }
}



