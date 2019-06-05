/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
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

namespace QLNet
{
   //! Rate helper for bootstrapping over interest-rate futures prices
   public class FuturesRateHelper : RateHelper
   {

      public FuturesRateHelper(Handle<Quote> price,
                               Date iborStartDate,
                               int lengthInMonths,
                               Calendar calendar,
                               BusinessDayConvention convention,
                               bool endOfMonth,
                               DayCounter dayCounter,
                               Handle<Quote> convAdj = null,
                               Futures.Type type = Futures.Type.IMM)
         : base(price)
      {
         convAdj_ = convAdj ?? new Handle<Quote>();

         switch (type)
         {
            case QLNet.Futures.Type.IMM:
               Utils.QL_REQUIRE(QLNet.IMM.isIMMdate(iborStartDate, false), () =>
                                iborStartDate + " is not a valid IMM date");
               break;
            case QLNet.Futures.Type.ASX:
               Utils.QL_REQUIRE(ASX.isASXdate(iborStartDate, false), () =>
                                iborStartDate + " is not a valid ASX date");
               break;
            default:
               Utils.QL_FAIL("unknown futures type (" + type + ")");
               break;
         }
         earliestDate_ = iborStartDate;
         maturityDate_ = calendar.advance(iborStartDate, new Period(lengthInMonths, TimeUnit.Months), convention, endOfMonth);
         yearFraction_ = dayCounter.yearFraction(earliestDate_, maturityDate_);
         pillarDate_ = latestDate_ = latestRelevantDate_ = maturityDate_;

         convAdj_.registerWith(update);
      }


      public FuturesRateHelper(double price,
                               Date iborStartDate,
                               int lengthInMonths,
                               Calendar calendar,
                               BusinessDayConvention convention,
                               bool endOfMonth,
                               DayCounter dayCounter,
                               double convexityAdjustment = 0.0,
                               Futures.Type type = Futures.Type.IMM)
         : base(price)
      {
         convAdj_ = new Handle<Quote>(new SimpleQuote(convexityAdjustment));

         switch (type)
         {
            case Futures.Type.IMM:
               Utils.QL_REQUIRE(IMM.isIMMdate(iborStartDate, false), () =>
                                iborStartDate + " is not a valid IMM date");
               break;
            case Futures.Type.ASX:
               Utils.QL_REQUIRE(ASX.isASXdate(iborStartDate, false), () =>
                                iborStartDate + " is not a valid ASX date");
               break;
            default:
               Utils.QL_FAIL("unknown futures type (" + type + ")");
               break;
         }
         earliestDate_ = iborStartDate;
         maturityDate_ = calendar.advance(iborStartDate, new Period(lengthInMonths, TimeUnit.Months), convention, endOfMonth);
         yearFraction_ = dayCounter.yearFraction(earliestDate_, maturityDate_);
         pillarDate_ = latestDate_ = latestRelevantDate_ = maturityDate_;
      }

      public FuturesRateHelper(Handle<Quote> price,
                               Date iborStartDate,
                               Date iborEndDate,
                               DayCounter dayCounter,
                               Handle<Quote> convAdj = null,
                               Futures.Type type = Futures.Type.IMM)
         : base(price)
      {
         convAdj_ = convAdj ?? new Handle<Quote>();

         switch (type)
         {
            case Futures.Type.IMM:
               Utils.QL_REQUIRE(IMM.isIMMdate(iborStartDate, false), () =>
                                iborStartDate + " is not a valid IMM date");
               if (iborEndDate == null)
               {
                  // advance 3 months
                  maturityDate_ = IMM.nextDate(iborStartDate, false);
                  maturityDate_ = IMM.nextDate(maturityDate_, false);
                  maturityDate_ = IMM.nextDate(maturityDate_, false);
               }
               else
               {
                  Utils.QL_REQUIRE(iborEndDate > iborStartDate, () =>
                                   "end date (" + iborEndDate +
                                   ") must be greater than start date (" +
                                   iborStartDate + ")");
                  maturityDate_ = iborEndDate;
               }
               break;
            case QLNet.Futures.Type.ASX:
               Utils.QL_REQUIRE(ASX.isASXdate(iborStartDate, false), () =>
                                iborStartDate + " is not a valid ASX date");
               if (iborEndDate == null)
               {
                  // advance 3 months
                  maturityDate_ = ASX.nextDate(iborStartDate, false);
                  maturityDate_ = ASX.nextDate(maturityDate_, false);
                  maturityDate_ = ASX.nextDate(maturityDate_, false);
               }
               else
               {
                  Utils.QL_REQUIRE(iborEndDate > iborStartDate, () =>
                                   "end date (" + iborEndDate +
                                   ") must be greater than start date (" +
                                   iborStartDate + ")");
                  maturityDate_ = iborEndDate;
               }
               break;
            default:
               Utils.QL_FAIL("unknown futures type (" + type + ")");
               break;
         }
         earliestDate_ = iborStartDate;
         yearFraction_ = dayCounter.yearFraction(earliestDate_, maturityDate_);
         pillarDate_ = latestDate_ = latestRelevantDate_ = maturityDate_;

         convAdj_.registerWith(update);
      }

      public FuturesRateHelper(double price,
                               Date iborStartDate,
                               Date iborEndDate,
                               DayCounter dayCounter,
                               double convAdj = 0,
                               Futures.Type type = Futures.Type.IMM)
         : base(price)
      {
         convAdj_ = new Handle<Quote>(new SimpleQuote(convAdj));

         switch (type)
         {
            case Futures.Type.IMM:
               Utils.QL_REQUIRE(IMM.isIMMdate(iborStartDate, false), () =>
                                iborStartDate + " is not a valid IMM date");
               if (iborEndDate == null)
               {
                  // advance 3 months
                  maturityDate_ = IMM.nextDate(iborStartDate, false);
                  maturityDate_ = IMM.nextDate(maturityDate_, false);
                  maturityDate_ = IMM.nextDate(maturityDate_, false);
               }
               else
               {
                  Utils.QL_REQUIRE(iborEndDate > iborStartDate, () =>
                                   "end date (" + iborEndDate +
                                   ") must be greater than start date (" +
                                   iborStartDate + ")");
                  maturityDate_ = iborEndDate;
               }
               break;
            case Futures.Type.ASX:
               Utils.QL_REQUIRE(ASX.isASXdate(iborStartDate, false), () =>
                                iborStartDate + " is not a valid ASX date");
               if (iborEndDate == null)
               {
                  // advance 3 months
                  maturityDate_ = ASX.nextDate(iborStartDate, false);
                  maturityDate_ = ASX.nextDate(maturityDate_, false);
                  maturityDate_ = ASX.nextDate(maturityDate_, false);
               }
               else
               {
                  Utils.QL_REQUIRE(iborEndDate > iborStartDate, () =>
                                   "end date (" + iborEndDate +
                                   ") must be greater than start date (" +
                                   iborStartDate + ")");
                  maturityDate_ = iborEndDate;
               }
               break;
            default:
               Utils.QL_FAIL("unknown futures type (" + type + ")");
               break;
         }
         earliestDate_ = iborStartDate;
         yearFraction_ = dayCounter.yearFraction(earliestDate_, maturityDate_);
         pillarDate_ = latestDate_ = latestRelevantDate_ = maturityDate_;
      }

      public FuturesRateHelper(Handle<Quote> price,
                               Date iborStartDate,
                               IborIndex i,
                               Handle<Quote> convAdj = null,
                               Futures.Type type = Futures.Type.IMM)
         : base(price)
      {
         convAdj_ = convAdj ?? new Handle<Quote>();

         switch (type)
         {
            case Futures.Type.IMM:
               Utils.QL_REQUIRE(IMM.isIMMdate(iborStartDate, false), () =>
                                iborStartDate + " is not a valid IMM date");
               break;
            case Futures.Type.ASX:
               Utils.QL_REQUIRE(ASX.isASXdate(iborStartDate, false), () =>
                                iborStartDate + " is not a valid ASX date");
               break;
            default:
               Utils.QL_FAIL("unknown futures type (" + type + ")");
               break;
         }
         earliestDate_ = iborStartDate;
         Calendar cal = i.fixingCalendar();
         maturityDate_ = cal.advance(iborStartDate, i.tenor(), i.businessDayConvention());
         yearFraction_ = i.dayCounter().yearFraction(earliestDate_, maturityDate_);
         pillarDate_ = latestDate_ = latestRelevantDate_ = maturityDate_;
         convAdj_.registerWith(update);
      }

      public FuturesRateHelper(double price,
                               Date iborStartDate,
                               IborIndex i,
                               double convAdj = 0.0,
                               Futures.Type type = Futures.Type.IMM)
         : base(price)
      {
         convAdj_ = new Handle<Quote>(new SimpleQuote(convAdj));

         switch (type)
         {
            case Futures.Type.IMM:
               Utils.QL_REQUIRE(IMM.isIMMdate(iborStartDate, false), () =>
                                iborStartDate + " is not a valid IMM date");
               break;
            case Futures.Type.ASX:
               Utils.QL_REQUIRE(ASX.isASXdate(iborStartDate, false), () =>
                                iborStartDate + " is not a valid ASX date");
               break;
            default:
               Utils.QL_FAIL("unknown futures type (" + type + ")");
               break;
         }
         earliestDate_ = iborStartDate;
         Calendar cal = i.fixingCalendar();
         maturityDate_ = cal.advance(iborStartDate, i.tenor(), i.businessDayConvention());
         yearFraction_ = i.dayCounter().yearFraction(earliestDate_, maturityDate_);
         pillarDate_ = latestDate_ = latestRelevantDate_ = maturityDate_;
      }

      //! RateHelper interface
      public override double impliedQuote()
      {
         Utils.QL_REQUIRE(termStructure_ != null, () => "term structure not set");

         double forwardRate = (termStructure_.discount(earliestDate_) /
                               termStructure_.discount(maturityDate_) - 1) / yearFraction_;
         double convAdj = convAdj_.empty() ? 0 : convAdj_.link.value();
         // Convexity, as FRA/futures adjustment, has been used in the
         // past to take into account futures margining vs FRA.
         // Therefore, there's no requirement for it to be non-negative.
         double futureRate = forwardRate + convAdj;
         return 100.0 * (1.0 - futureRate);
      }

      //! FuturesRateHelper inspectors
      public double convexityAdjustment()
      {
         return convAdj_.empty() ? 0.0 : convAdj_.link.value();
      }

      private double yearFraction_;
      private Handle<Quote> convAdj_;

   }

   // Rate helper with date schedule relative to the global evaluation date
   // This class takes care of rebuilding the date schedule when the global evaluation date changes
   public abstract class RelativeDateRateHelper : RateHelper
   {
      protected Date evaluationDate_;

      ///////////////////////////////////////////
      // constructors
      protected RelativeDateRateHelper(Handle<Quote> quote)
         : base(quote)
      {
         Settings.registerWith(update);
         evaluationDate_ = Settings.evaluationDate();
      }

      protected RelativeDateRateHelper(double quote)
         : base(quote)
      {
         Settings.registerWith(update);
         evaluationDate_ = Settings.evaluationDate();
      }


      //////////////////////////////////////
      //! Observer interface
      public override void update()
      {
         if (evaluationDate_ != Settings.evaluationDate())
         {
            evaluationDate_ = Settings.evaluationDate();
            initializeDates();
         }
         base.update();
      }

      ///////////////////////////////////////////
      protected abstract void initializeDates();
   }

   // Rate helper for bootstrapping over deposit rates
   public class DepositRateHelper : RelativeDateRateHelper
   {
      public DepositRateHelper(Handle<Quote> rate,
                               Period tenor,
                               int fixingDays,
                               Calendar calendar,
                               BusinessDayConvention convention,
                               bool endOfMonth,
                               DayCounter dayCounter)
         : base(rate)
      {
         iborIndex_ = new IborIndex("no-fix", tenor, fixingDays, new Currency(), calendar, convention,
                                    endOfMonth, dayCounter, termStructureHandle_);
         initializeDates();
      }

      public DepositRateHelper(double rate,
                               Period tenor,
                               int fixingDays,
                               Calendar calendar,
                               BusinessDayConvention convention,
                               bool endOfMonth,
                               DayCounter dayCounter) :
         base(rate)
      {
         iborIndex_ = new IborIndex("no-fix", tenor, fixingDays, new Currency(), calendar, convention,
                                    endOfMonth, dayCounter, termStructureHandle_);
         initializeDates();
      }

      public DepositRateHelper(Handle<Quote> rate, IborIndex i)
         : base(rate)
      {
         iborIndex_ = i.clone(termStructureHandle_);
         initializeDates();
      }
      public DepositRateHelper(double rate, IborIndex i)
         : base(rate)
      {
         iborIndex_ = i.clone(termStructureHandle_);
         initializeDates();
      }


      /////////////////////////////////////////
      //! RateHelper interface
      public override double impliedQuote()
      {
         Utils.QL_REQUIRE(termStructure_ != null, () => "term structure not set");
         // the forecast fixing flag is set to true because
         // we do not want to take fixing into account
         return iborIndex_.fixing(fixingDate_, true);
      }

      public override void setTermStructure(YieldTermStructure t)
      {
         // no need to register---the index is not lazy
         termStructureHandle_.linkTo(t, false);
         base.setTermStructure(t);
      }

      protected override void initializeDates()
      {
         // if the evaluation date is not a business day
         // then move to the next business day
         Date referenceDate = iborIndex_.fixingCalendar().adjust(evaluationDate_);
         earliestDate_ = iborIndex_.valueDate(referenceDate);
         fixingDate_ = iborIndex_.fixingDate(earliestDate_);
         maturityDate_ = iborIndex_.maturityDate(earliestDate_);
         pillarDate_ = latestDate_ = latestRelevantDate_ = maturityDate_;
      }

      private Date fixingDate_;
      private IborIndex iborIndex_;
      // need to init this because it is used before the handle has any link, i.e. setTermStructure will be used after ctor
      private RelinkableHandle<YieldTermStructure> termStructureHandle_ = new RelinkableHandle<YieldTermStructure>();

   }

   //! Rate helper for bootstrapping over %FRA rates
   public class FraRateHelper : RelativeDateRateHelper
   {

      public FraRateHelper(Handle<Quote> rate,
                           int monthsToStart,
                           int monthsToEnd,
                           int fixingDays,
                           Calendar calendar,
                           BusinessDayConvention convention,
                           bool endOfMonth,
                           DayCounter dayCounter,
                           Pillar.Choice pillarChoice = Pillar.Choice.LastRelevantDate,
                           Date customPillarDate = null) :
         base(rate)
      {
         periodToStart_ = new Period(monthsToStart, TimeUnit.Months);
         pillarChoice_ = pillarChoice;

         Utils.QL_REQUIRE(monthsToEnd > monthsToStart, () =>
                          "monthsToEnd (" + monthsToEnd + ") must be grater than monthsToStart (" + monthsToStart + ")");

         iborIndex_ = new IborIndex("no-fix", new Period(monthsToEnd - monthsToStart, TimeUnit.Months), fixingDays,
                                    new Currency(), calendar, convention, endOfMonth, dayCounter, termStructureHandle_);
         pillarDate_ = customPillarDate;
         initializeDates();
      }

      public FraRateHelper(double rate,
                           int monthsToStart,
                           int monthsToEnd,
                           int fixingDays,
                           Calendar calendar,
                           BusinessDayConvention convention,
                           bool endOfMonth,
                           DayCounter dayCounter,
                           Pillar.Choice pillarChoice = Pillar.Choice.LastRelevantDate,
                           Date customPillarDate = null)
         : base(rate)
      {
         periodToStart_ = new Period(monthsToStart, TimeUnit.Months);
         pillarChoice_ = pillarChoice;

         Utils.QL_REQUIRE(monthsToEnd > monthsToStart, () =>
                          "monthsToEnd (" + monthsToEnd + ") must be grater than monthsToStart (" + monthsToStart + ")");

         iborIndex_ = new IborIndex("no-fix", new Period(monthsToEnd - monthsToStart, TimeUnit.Months), fixingDays,
                                    new Currency(), calendar, convention, endOfMonth, dayCounter, termStructureHandle_);
         pillarDate_ = customPillarDate;
         initializeDates();
      }

      public FraRateHelper(Handle<Quote> rate,
                           int monthsToStart, IborIndex i,
                           Pillar.Choice pillarChoice = Pillar.Choice.LastRelevantDate,
                           Date customPillarDate = null)
         : base(rate)
      {
         periodToStart_ = new Period(monthsToStart, TimeUnit.Months);
         pillarChoice_ = pillarChoice;
         iborIndex_ = i.clone(termStructureHandle_);

         // We want to be notified of changes of fixings, but we don't
         // want notifications from termStructureHandle_ (they would
         // interfere with bootstrapping.)
         iborIndex_.registerWith(update);
         pillarDate_ = customPillarDate;
         initializeDates();
      }

      public FraRateHelper(double rate,
                           int monthsToStart,
                           IborIndex i,
                           Pillar.Choice pillarChoice = Pillar.Choice.LastRelevantDate,
                           Date customPillarDate = null)
         : base(rate)
      {
         periodToStart_ = new Period(monthsToStart, TimeUnit.Months);
         pillarChoice_ = pillarChoice;

         iborIndex_ = i.clone(termStructureHandle_);
         iborIndex_.registerWith(update);
         pillarDate_ = customPillarDate;

         initializeDates();
      }

      public FraRateHelper(Handle<Quote> rate,
                           Period periodToStart,
                           int lengthInMonths,
                           int fixingDays,
                           Calendar calendar,
                           BusinessDayConvention convention,
                           bool endOfMonth,
                           DayCounter dayCounter,
                           Pillar.Choice pillarChoice = Pillar.Choice.LastRelevantDate,
                           Date customPillarDate = null)
         : base(rate)
      {
         periodToStart_ = periodToStart;
         pillarChoice_ = pillarChoice;
         // no way to take fixing into account,
         // even if we would like to for FRA over today
         iborIndex_ = new IborIndex("no-fix", // correct family name would be needed
                                    new Period(lengthInMonths, TimeUnit.Months),
                                    fixingDays,
                                    new Currency(), calendar, convention,
                                    endOfMonth, dayCounter, termStructureHandle_);
         pillarDate_ = customPillarDate;
         initializeDates();
      }

      public FraRateHelper(double rate,
                           Period periodToStart,
                           int lengthInMonths,
                           int fixingDays,
                           Calendar calendar,
                           BusinessDayConvention convention,
                           bool endOfMonth,
                           DayCounter dayCounter,
                           Pillar.Choice pillarChoice = Pillar.Choice.LastRelevantDate,
                           Date customPillarDate = null)
         : base(rate)
      {
         periodToStart_ = periodToStart;
         pillarChoice_ = pillarChoice;
         // no way to take fixing into account,
         // even if we would like to for FRA over today
         iborIndex_ = new IborIndex("no-fix",  // correct family name would be needed
                                    new Period(lengthInMonths, TimeUnit.Months),
                                    fixingDays,
                                    new Currency(), calendar, convention,
                                    endOfMonth, dayCounter, termStructureHandle_);
         pillarDate_ = customPillarDate;
         initializeDates();
      }

      public FraRateHelper(Handle<Quote> rate,
                           Period periodToStart,
                           IborIndex iborIndex,
                           Pillar.Choice pillarChoice = Pillar.Choice.LastRelevantDate,
                           Date customPillarDate = null)
         : base(rate)
      {
         periodToStart_ = periodToStart;
         pillarChoice_ = pillarChoice;
         // no way to take fixing into account,
         // even if we would like to for FRA over today
         iborIndex_ = iborIndex.clone(termStructureHandle_);
         iborIndex_.registerWith(update);
         pillarDate_ = customPillarDate;
         initializeDates();
      }

      public FraRateHelper(double rate,
                           Period periodToStart,
                           IborIndex iborIndex,
                           Pillar.Choice pillarChoice = Pillar.Choice.LastRelevantDate,
                           Date customPillarDate = null)
         : base(rate)
      {
         periodToStart_ = periodToStart;
         pillarChoice_ = pillarChoice;
         // no way to take fixing into account,
         // even if we would like to for FRA over today
         iborIndex_ = iborIndex.clone(termStructureHandle_);
         iborIndex_.registerWith(update);
         pillarDate_ = customPillarDate;
         initializeDates();
      }

      public override void setTermStructure(YieldTermStructure t)
      {
         // no need to register---the index is not lazy
         termStructureHandle_.linkTo(t, false);
         base.setTermStructure(t);
      }

      public override double impliedQuote()
      {
         Utils.QL_REQUIRE(termStructure_ != null, () => "term structure not set");
         return iborIndex_.fixing(fixingDate_, true);
      }

      protected override void initializeDates()
      {
         // if the evaluation date is not a business day
         // then move to the next business day
         Date referenceDate = iborIndex_.fixingCalendar().adjust(evaluationDate_);
         Date spotDate = iborIndex_.fixingCalendar().advance(referenceDate, new Period(iborIndex_.fixingDays(), TimeUnit.Days));
         earliestDate_ = iborIndex_.fixingCalendar().advance(spotDate,
                                                             periodToStart_,
                                                             iborIndex_.businessDayConvention(),
                                                             iborIndex_.endOfMonth());
         // maturity date is calculated from spot date
         maturityDate_ = iborIndex_.fixingCalendar().advance(spotDate,
                                                             periodToStart_ + iborIndex_.tenor(),
                                                             iborIndex_.businessDayConvention(),
                                                             iborIndex_.endOfMonth());

         // latest relevant date is calculated from earliestDate_ instead
         latestRelevantDate_ = iborIndex_.maturityDate(earliestDate_);

         switch (pillarChoice_)
         {
            case Pillar.Choice.MaturityDate:
               pillarDate_ = maturityDate_;
               break;
            case Pillar.Choice.LastRelevantDate:
               pillarDate_ = latestRelevantDate_;
               break;
            case Pillar.Choice.CustomDate:
               // pillarDate_ already assigned at construction time
               Utils.QL_REQUIRE(pillarDate_ >= earliestDate_, () =>
                                "pillar date (" + pillarDate_ + ") must be later than or equal to the instrument's earliest date (" +
                                earliestDate_ + ")");
               Utils.QL_REQUIRE(pillarDate_ <= latestRelevantDate_, () =>
                                "pillar date (" + pillarDate_ + ") must be before or equal to the instrument's latest relevant date (" +
                                latestRelevantDate_ + ")");
               break;
            default:
               Utils.QL_FAIL("unknown Pillar::Choice(" + pillarChoice_ + ")");
               break;
         }

         latestDate_ = pillarDate_; // backward compatibility
         fixingDate_ = iborIndex_.fixingDate(earliestDate_);
      }

      private Date fixingDate_;
      private Period periodToStart_;
      private Pillar.Choice pillarChoice_;
      private IborIndex iborIndex_;
      // need to init this because it is used before the handle has any link, i.e. setTermStructure will be used after ctor
      RelinkableHandle<YieldTermStructure> termStructureHandle_ = new RelinkableHandle<YieldTermStructure>();

   }

   // Rate helper for bootstrapping over swap rates
   public class SwapRateHelper : RelativeDateRateHelper
   {
      public SwapRateHelper(Handle<Quote> rate,
                            SwapIndex swapIndex,
                            Handle<Quote> spread = null,
                            Period fwdStart = null,
                            // exogenous discounting curve
                            Handle<YieldTermStructure> discount = null,
                            Pillar.Choice pillarChoice = Pillar.Choice.LastRelevantDate,
                            Date customPillarDate = null)
         : base(rate)
      {
         spread_ = spread ?? new Handle<Quote>();
         fwdStart_ = fwdStart ?? new Period(0, TimeUnit.Days);

         settlementDays_ = swapIndex.fixingDays();
         tenor_ = swapIndex.tenor();
         pillarChoice_ = pillarChoice;
         calendar_ = swapIndex.fixingCalendar();
         fixedConvention_ = swapIndex.fixedLegConvention();
         fixedFrequency_ = swapIndex.fixedLegTenor().frequency();
         fixedDayCount_ = swapIndex.dayCounter();
         iborIndex_ = swapIndex.iborIndex();
         fwdStart_ = fwdStart;
         discountHandle_ = discount ?? new Handle<YieldTermStructure>();

         // take fixing into account
         iborIndex_ = swapIndex.iborIndex().clone(termStructureHandle_);
         // We want to be notified of changes of fixings, but we don't
         // want notifications from termStructureHandle_ (they would
         // interfere with bootstrapping.)
         iborIndex_.registerWith(update) ;
         spread_.registerWith(update);
         discountHandle_.registerWith(update);
         pillarDate_ = customPillarDate;

         initializeDates();
      }


      public SwapRateHelper(Handle<Quote> rate,
                            Period tenor,
                            Calendar calendar,
                            Frequency fixedFrequency,
                            BusinessDayConvention fixedConvention,
                            DayCounter fixedDayCount,
                            IborIndex iborIndex,
                            Handle<Quote> spread = null,
                            Period fwdStart = null,
                            // exogenous discounting curve
                            Handle<YieldTermStructure> discount = null,
                            int? settlementDays = null,
                            Pillar.Choice pillarChoice = Pillar.Choice.LastRelevantDate,
                            Date customPillarDate = null)
      : base(rate)
      {
         settlementDays_ = settlementDays;
         tenor_ = tenor;
         pillarChoice_ = pillarChoice;
         calendar_ = calendar;
         fixedConvention_ = fixedConvention;
         fixedFrequency_ = fixedFrequency;
         fixedDayCount_ = fixedDayCount;
         spread_ = spread ?? new Handle<Quote>();
         fwdStart_ = fwdStart ?? new Period(0, TimeUnit.Days);
         discountHandle_ = discount ?? new Handle<YieldTermStructure>();

         if (settlementDays_ == null)
            settlementDays_ = iborIndex.fixingDays();

         // take fixing into account
         iborIndex_ = iborIndex.clone(termStructureHandle_);
         // We want to be notified of changes of fixings, but we don't
         // want notifications from termStructureHandle_ (they would
         // interfere with bootstrapping.)
         iborIndex_.registerWith(update) ;
         spread_.registerWith(update);
         discountHandle_.registerWith(update);

         pillarDate_ = customPillarDate;
         initializeDates();
      }

      public SwapRateHelper(double rate,
                            SwapIndex swapIndex,
                            Handle<Quote> spread = null,
                            Period fwdStart = null,
                            // exogenous discounting curve
                            Handle<YieldTermStructure> discount = null,
                            Pillar.Choice pillarChoice = Pillar.Choice.LastRelevantDate,
                            Date customPillarDate = null)
         : base(rate)
      {
         settlementDays_ = swapIndex.fixingDays();
         tenor_ = swapIndex.tenor();
         pillarChoice_ = pillarChoice;
         calendar_ = swapIndex.fixingCalendar();
         fixedConvention_ = swapIndex.fixedLegConvention();
         fixedFrequency_ = swapIndex.fixedLegTenor().frequency();
         fixedDayCount_ = swapIndex.dayCounter();
         spread_ = spread ?? new Handle<Quote>();
         fwdStart_ = fwdStart ?? new Period(0, TimeUnit.Days);
         discountHandle_ = discount ?? new Handle<YieldTermStructure>();

         // take fixing into account
         iborIndex_ = swapIndex.iborIndex().clone(termStructureHandle_);
         // We want to be notified of changes of fixings, but we don't
         // want notifications from termStructureHandle_ (they would
         // interfere with bootstrapping.)
         iborIndex_.registerWith(update);
         spread_.registerWith(update);
         discountHandle_.registerWith(update);

         pillarDate_ = customPillarDate;
         initializeDates();
      }


      public SwapRateHelper(double rate,
                            Period tenor,
                            Calendar calendar,
                            Frequency fixedFrequency,
                            BusinessDayConvention fixedConvention,
                            DayCounter fixedDayCount,
                            IborIndex iborIndex,
                            Handle<Quote> spread = null,
                            Period fwdStart = null,
                            // exogenous discounting curve
                            Handle<YieldTermStructure> discount = null,
                            int? settlementDays = null,
                            Pillar.Choice pillarChoice = Pillar.Choice.LastRelevantDate,
                            Date customPillarDate = null)
      : base(rate)
      {
         settlementDays_ = settlementDays;
         tenor_ = tenor;
         pillarChoice_ = pillarChoice;
         calendar_ = calendar;
         fixedConvention_ = fixedConvention;
         fixedFrequency_ = fixedFrequency;
         fixedDayCount_ = fixedDayCount;
         spread_ = spread ?? new Handle<Quote>();
         fwdStart_ = fwdStart ?? new Period(0, TimeUnit.Days);
         discountHandle_ = discount ?? new Handle<YieldTermStructure>();

         if (settlementDays_ == null)
            settlementDays_ = iborIndex.fixingDays();

         // take fixing into account
         iborIndex_ = iborIndex.clone(termStructureHandle_);
         // We want to be notified of changes of fixings, but we don't
         // want notifications from termStructureHandle_ (they would
         // interfere with bootstrapping.)
         iborIndex_.registerWith(update);
         spread_.registerWith(update);
         discountHandle_.registerWith(update);

         pillarDate_ = customPillarDate;
         initializeDates();
      }



      protected override void initializeDates()
      {
         // do not pass the spread here, as it might be a Quote i.e. it can dinamically change
         // input discount curve Handle might be empty now but it could be assigned a curve later;
         // use a RelinkableHandle here
         swap_ = new MakeVanillaSwap(tenor_, iborIndex_, 0.0, fwdStart_)
         .withSettlementDays(settlementDays_.Value)
         .withDiscountingTermStructure(discountRelinkableHandle_)
         .withFixedLegDayCount(fixedDayCount_)
         .withFixedLegTenor(new Period(fixedFrequency_))
         .withFixedLegConvention(fixedConvention_)
         .withFixedLegTerminationDateConvention(fixedConvention_)
         .withFixedLegCalendar(calendar_)
         .withFloatingLegCalendar(calendar_);

         earliestDate_ = swap_.startDate();

         // Usually...
         maturityDate_ = latestRelevantDate_ = swap_.maturityDate();

         // ...but due to adjustments, the last floating coupon might
         // need a later date for fixing
#if QL_USE_INDEXED_COUPON
         FloatingRateCoupon lastCoupon = (FloatingRateCoupon)swap_.floatingLeg()[swap_.floatingLeg().Count - 1];
         Date fixingValueDate = iborIndex_.valueDate(lastFloating.fixingDate());
         Date endValueDate = iborIndex_.maturityDate(fixingValueDate);
         latestDate_ = Date.Max(latestDate_, endValueDate);
#endif

         switch (pillarChoice_)
         {
            case Pillar.Choice.MaturityDate:
               pillarDate_ = maturityDate_;
               break;
            case Pillar.Choice.LastRelevantDate:
               pillarDate_ = latestRelevantDate_;
               break;
            case Pillar.Choice.CustomDate:
               // pillarDate_ already assigned at construction time
               Utils.QL_REQUIRE(pillarDate_ >= earliestDate_, () =>
                                "pillar date (" + pillarDate_ + ") must be later " +
                                "than or equal to the instrument's earliest date (" +
                                earliestDate_ + ")");
               Utils.QL_REQUIRE(pillarDate_ <= latestRelevantDate_, () =>
                                "pillar date (" + pillarDate_ + ") must be before " +
                                "or equal to the instrument's latest relevant date (" +
                                latestRelevantDate_ + ")");
               break;
            default:
               Utils.QL_FAIL("unknown Pillar::Choice(" + pillarChoice_ + ")");
               break;
         }

         latestDate_ = pillarDate_; // backward compatibility

      }

      public override void setTermStructure(YieldTermStructure t)
      {
         // do not set the relinkable handle as an observer -
         // force recalculation when needed
         termStructureHandle_.linkTo(t, false);
         base.setTermStructure(t);
         discountRelinkableHandle_.linkTo(discountHandle_.empty() ? t : discountHandle_, false);
      }

      public override double impliedQuote()
      {
         Utils.QL_REQUIRE(termStructure_ != null, () => "term structure not set");
         // we didn't register as observers - force calculation
         swap_.recalculate();                // it is from lazy objects
         // weak implementation... to be improved         
         double floatingLegNPV = swap_.floatingLegNPV();
         double spread = this.spread();
         double spreadNPV = swap_.floatingLegBPS() / Const.BASIS_POINT * spread;
         double totNPV = -(floatingLegNPV + spreadNPV);
         double result = totNPV / (swap_.fixedLegBPS() / Const.BASIS_POINT);
         return result;
      }

      // SwapRateHelper inspectors
      public double spread() { return spread_.empty() ? 0.0 : spread_.link.value(); }
      public VanillaSwap swap() { return swap_; }
      public Period forwardStart() { return fwdStart_; }

      protected int? settlementDays_;
      protected Period tenor_;
      protected Pillar.Choice pillarChoice_;
      protected Calendar calendar_;
      protected BusinessDayConvention fixedConvention_;
      protected Frequency fixedFrequency_;
      protected DayCounter fixedDayCount_;
      protected IborIndex iborIndex_;
      protected VanillaSwap swap_;
      // need to init this because it is used before the handle has any link, i.e. setTermStructure will be used after ctor
      protected RelinkableHandle<YieldTermStructure> termStructureHandle_ = new RelinkableHandle<YieldTermStructure>();
      protected Handle<Quote> spread_;
      protected Period fwdStart_;
      protected Handle<YieldTermStructure> discountHandle_;
      protected RelinkableHandle<YieldTermStructure> discountRelinkableHandle_ = new RelinkableHandle<YieldTermStructure>();

   }

   //! Rate helper for bootstrapping over BMA swap rates
   public class BMASwapRateHelper : RelativeDateRateHelper
   {
      public BMASwapRateHelper(Handle<Quote> liborFraction,
                               Period tenor,
                               int settlementDays,
                               Calendar calendar,
                               Period bmaPeriod,
                               BusinessDayConvention bmaConvention,
                               DayCounter bmaDayCount,
                               BMAIndex bmaIndex,
                               IborIndex iborIndex)
         : base(liborFraction)
      {
         tenor_ = tenor;
         settlementDays_ = settlementDays;
         calendar_ = calendar;
         bmaPeriod_ = bmaPeriod;
         bmaConvention_ = bmaConvention;
         bmaDayCount_ = bmaDayCount;
         bmaIndex_ = bmaIndex;
         iborIndex_ = iborIndex;

         iborIndex_.registerWith(update);
         bmaIndex_.registerWith(update);

         initializeDates();
      }

      // RateHelper interface
      public override double impliedQuote()
      {
         Utils.QL_REQUIRE(termStructure_ != null, () => "term structure not set");
         // we didn't register as observers - force calculation
         swap_.recalculate();
         return swap_.fairLiborFraction();
      }

      public override void setTermStructure(YieldTermStructure t)
      {
         // do not set the relinkable handle as an observer -
         // force recalculation when needed
         termStructureHandle_.linkTo(t, false);
         base.setTermStructure(t);
      }

      protected override void initializeDates()
      {
         // if the evaluation date is not a business day
         // then move to the next business day
         JointCalendar jc = new JointCalendar(calendar_, iborIndex_.fixingCalendar());
         Date referenceDate = jc.adjust(evaluationDate_);
         earliestDate_ = calendar_.advance(referenceDate, new Period(settlementDays_, TimeUnit.Days), BusinessDayConvention.Following);

         Date maturity = earliestDate_ + tenor_;

         // dummy BMA index with curve/swap arguments
         BMAIndex clonedIndex = new BMAIndex(termStructureHandle_);

         Schedule bmaSchedule = new MakeSchedule().from(earliestDate_).to(maturity)
         .withTenor(bmaPeriod_)
         .withCalendar(bmaIndex_.fixingCalendar())
         .withConvention(bmaConvention_)
         .backwards().value();

         Schedule liborSchedule = new MakeSchedule().from(earliestDate_).to(maturity)
         .withTenor(iborIndex_.tenor())
         .withCalendar(iborIndex_.fixingCalendar())
         .withConvention(iborIndex_.businessDayConvention())
         .endOfMonth(iborIndex_.endOfMonth())
         .backwards().value();

         swap_ =  new BMASwap(BMASwap.Type.Payer,
                              100.0, liborSchedule, 0.75, // arbitrary
                              0.0, iborIndex_, iborIndex_.dayCounter(), bmaSchedule, clonedIndex, bmaDayCount_);

         swap_.setPricingEngine(new DiscountingSwapEngine(iborIndex_.forwardingTermStructure()));

         Date d = calendar_.adjust(swap_.maturityDate(), BusinessDayConvention.Following);
         int w = d.weekday();
         Date nextWednesday = (w >= 4) ?
                              d + new Period((11 - w), TimeUnit.Days) :
                              d + new Period((4 - w), TimeUnit.Days);
         latestDate_ = clonedIndex.valueDate(clonedIndex.fixingCalendar().adjust(nextWednesday));

      }

      protected Period tenor_;
      protected int settlementDays_;
      protected Calendar calendar_;
      protected Period bmaPeriod_;
      protected BusinessDayConvention bmaConvention_;
      protected DayCounter bmaDayCount_;
      protected BMAIndex bmaIndex_;
      protected IborIndex iborIndex_;

      protected BMASwap swap_;
      protected RelinkableHandle<YieldTermStructure> termStructureHandle_ = new RelinkableHandle<YieldTermStructure>();

   }

   //! Rate helper for bootstrapping over Fx Swap rates
   /*! fwdFx = spotFx + fwdPoint
      isFxBaseCurrencyCollateralCurrency indicates if the base currency
      of the fx currency pair is the one used as collateral
   */
   public class FxSwapRateHelper : RelativeDateRateHelper
   {
      public FxSwapRateHelper(Handle<Quote> fwdPoint,
                              Handle<Quote> spotFx,
                              Period tenor,
                              int fixingDays,
                              Calendar calendar,
                              BusinessDayConvention convention,
                              bool endOfMonth,
                              bool isFxBaseCurrencyCollateralCurrency,
                              Handle<YieldTermStructure> coll)
         : base(fwdPoint)
      {
         spot_ = spotFx;
         tenor_ = tenor;
         fixingDays_ = fixingDays;
         cal_ = calendar;
         conv_ = convention;
         eom_ = endOfMonth;
         isFxBaseCurrencyCollateralCurrency_ = isFxBaseCurrencyCollateralCurrency;
         collHandle_ = coll;

         spot_.registerWith(update);
         collHandle_.registerWith(update);
         initializeDates();
      }

      // RateHelper interface
      public override double impliedQuote()
      {
         Utils.QL_REQUIRE(termStructure_ != null, () => "term structure not set");

         Utils.QL_REQUIRE(!collHandle_.empty(), () => "collateral term structure not set");

         double d1 = collHandle_.link.discount(earliestDate_);
         double d2 = collHandle_.link.discount(latestDate_);
         double collRatio = d1 / d2;
         d1 = termStructureHandle_.link.discount(earliestDate_);
         d2 = termStructureHandle_.link.discount(latestDate_);
         double ratio = d1 / d2;
         double spot = spot_.link.value();
         if (isFxBaseCurrencyCollateralCurrency_)
         {
            return (ratio / collRatio - 1) * spot;
         }
         return (collRatio / ratio - 1) * spot;
      }
      public override void setTermStructure(YieldTermStructure t)
      {
         // do not set the relinkable handle as an observer -
         // force recalculation when needed

         termStructureHandle_.linkTo(t, false);
         collRelinkableHandle_.linkTo(collHandle_, false);
         base.setTermStructure(t);

      }

      // FxSwapRateHelper inspectors
      public double spot()  { return spot_.link.value(); }
      public Period tenor()  { return tenor_; }
      public int fixingDays()  { return fixingDays_; }
      public Calendar calendar()  { return cal_; }
      public BusinessDayConvention businessDayConvention()  { return conv_; }
      public bool endOfMonth()  { return eom_; }
      public bool isFxBaseCurrencyCollateralCurrency()  { return isFxBaseCurrencyCollateralCurrency_; }

      protected override void initializeDates()
      {
         // if the evaluation date is not a business day
         // then move to the next business day
         Date refDate = cal_.adjust(evaluationDate_);
         earliestDate_ = cal_.advance(refDate, new Period(fixingDays_, TimeUnit.Days));
         latestDate_ = cal_.advance(earliestDate_, tenor_, conv_, eom_);
      }
      private Handle<Quote> spot_;
      private Period tenor_;
      private int fixingDays_;
      private Calendar cal_;
      private BusinessDayConvention conv_;
      private bool eom_;
      private bool isFxBaseCurrencyCollateralCurrency_;

      private RelinkableHandle<YieldTermStructure> termStructureHandle_ = new RelinkableHandle<YieldTermStructure>();

      private Handle<YieldTermStructure> collHandle_;
      private RelinkableHandle<YieldTermStructure> collRelinkableHandle_ = new RelinkableHandle<YieldTermStructure>();
   }
}
