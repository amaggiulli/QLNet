/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008, 2009 , 2010  Andrea Maggiulli (a.maggiulli@gmail.com)
  
 This file is part of QLNet Project http://qlnet.sourceforge.net/

 QLNet is free software: you can redistribute it and/or modify it
 under the terms of the QLNet license.  You should have received a
 copy of the license along with this program; if not, license is  
 available online at <http://qlnet.sourceforge.net/License.html>.
  
 QLNet is a based on QuantLib, a free-software/open-source library
 for financial quantitative analysts and developers - http://quantlib.org/
 The QuantLib license is available online at http://quantlib.org/license.shtml.
 
 This program is distributed in the hope that it will be useful, but WITHOUT
 ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 FOR A PARTICULAR PURPOSE.  See the license for more details.
*/
using System;

namespace QLNet {
    //! Rate helper for bootstrapping over interest-rate futures prices
   public class FuturesRateHelper : RateHelper
   {
        private double yearFraction_;
        private Handle<Quote> convAdj_;

        // constructors. special case when convexityAdjustment is really delivered as Quote
        public FuturesRateHelper(Handle<Quote> price, Date immDate, int lengthInMonths, Calendar calendar,
                                 BusinessDayConvention convention, bool endOfMonth, DayCounter dayCounter)
            : this(price, immDate, lengthInMonths, calendar, convention, endOfMonth, dayCounter, new Handle<Quote>()) { }
        public FuturesRateHelper(Handle<Quote> price, Date immDate, int nMonths, Calendar calendar,
                                 BusinessDayConvention convention, bool endOfMonth, DayCounter dayCounter,
                                 Handle<Quote> convexityAdjustment)
            : base(price) {
            convAdj_ = convexityAdjustment;

            if (!IMM.isIMMdate(immDate, false)) throw new ArgumentException(immDate + "is not a valid IMM date");
            earliestDate_ = immDate;

            latestDate_ = calendar.advance(immDate, new Period(nMonths, TimeUnit.Months), convention, endOfMonth);
            yearFraction_ = dayCounter.yearFraction(earliestDate_, latestDate_);

            convAdj_.registerWith(update);
        }

        // overloaded constructors
        public FuturesRateHelper(double price, Date immDate, int nMonths, Calendar calendar, BusinessDayConvention convention,
                                 bool endOfMonth, DayCounter dayCounter, double convAdj)
            : base(price) {
            convAdj_ = new Handle<Quote>(new SimpleQuote(convAdj));

            if (!IMM.isIMMdate(immDate, false)) throw new ArgumentException(immDate + "is not a valid IMM date");
            earliestDate_ = immDate;

            latestDate_ = calendar.advance(immDate, new Period(nMonths, TimeUnit.Months), convention, endOfMonth);
            yearFraction_ = dayCounter.yearFraction(earliestDate_, latestDate_);
        }

        public FuturesRateHelper(Handle<Quote> price, Date immDate, IborIndex i, Handle<Quote> convAdj)
            : base(price) {
            convAdj_ = convAdj;

            if (!IMM.isIMMdate(immDate, false)) throw new ArgumentException(immDate + "is not a valid IMM date");
            earliestDate_ = immDate;

            Calendar cal = i.fixingCalendar();
            latestDate_ = cal.advance(immDate, i.tenor(), i.businessDayConvention());
            yearFraction_ = i.dayCounter().yearFraction(earliestDate_, latestDate_);

            convAdj_.registerWith(update);
        }

        public FuturesRateHelper(double price, Date immDate, IborIndex i, double convAdj)
            : base(price) {
            convAdj_ = new Handle<Quote>(new SimpleQuote(convAdj));

            if (!IMM.isIMMdate(immDate, false)) throw new ArgumentException(immDate + "is not a valid IMM date");
            earliestDate_ = immDate;

            Calendar cal = i.fixingCalendar();
            latestDate_ = cal.advance(immDate, i.tenor(), i.businessDayConvention());
            yearFraction_ = i.dayCounter().yearFraction(earliestDate_, latestDate_);
        }


        /////////////////////////////////////////////////////
        //! RateHelper interface
        public override double impliedQuote() {
            if (termStructure_ == null) throw new ArgumentException("term structure not set");

            double forwardRate = (termStructure_.discount(earliestDate_) /
                                  termStructure_.discount(latestDate_) - 1) / yearFraction_;
            double convAdj = convAdj_.link.value();

            if (convAdj < 0) throw new ArgumentException("Negative (" + convAdj + ") futures convexity adjustment");
            double futureRate = forwardRate + convAdj;
            return 100.0 * (1.0 - futureRate);
        }


        /////////////////////////////////////////////////////
        //! FuturesRateHelper inspectors
        public double convexityAdjustment() {
            return convAdj_.empty() ? 0.0 : convAdj_.link.value();
        }
    }

    // Rate helper with date schedule relative to the global evaluation date
    // This class takes care of rebuilding the date schedule when the global evaluation date changes
    public abstract class RelativeDateRateHelper : RateHelper {
        protected Date evaluationDate_;

        ///////////////////////////////////////////
        // constructors
        public RelativeDateRateHelper(Handle<Quote> quote) : base(quote) {
            Settings.registerWith(update);
            evaluationDate_ = Settings.evaluationDate();
        }

        public RelativeDateRateHelper(double quote) : base(quote) {
            Settings.registerWith(update);
            evaluationDate_ = Settings.evaluationDate();
        }


        //////////////////////////////////////
        //! Observer interface
        public override void update() {
            if (evaluationDate_ != Settings.evaluationDate()) {
                evaluationDate_ = Settings.evaluationDate();
                initializeDates();
            }
            base.update();
        }

        ///////////////////////////////////////////
        protected abstract void initializeDates();
    }

    // Rate helper for bootstrapping over deposit rates
    public class DepositRateHelper : RelativeDateRateHelper {
        private Date fixingDate_;
        IborIndex iborIndex_;
        // need to init this because it is used before the handle has any link, i.e. setTermStructure will be used after ctor
        RelinkableHandle<YieldTermStructure> termStructureHandle_ = new RelinkableHandle<YieldTermStructure>();

        ///////////////////////////////////////////
        // constructors
        public DepositRateHelper(Handle<Quote> rate, Period tenor, int fixingDays, Calendar calendar,
                          BusinessDayConvention convention, bool endOfMonth, DayCounter dayCounter) :
            base(rate) {
            iborIndex_ = new IborIndex("no-fix", tenor, fixingDays, new Currency(), calendar, convention,
                                       endOfMonth, dayCounter, termStructureHandle_);
            initializeDates();
        }

        public DepositRateHelper(double rate, Period tenor, int fixingDays, Calendar calendar,
                          BusinessDayConvention convention, bool endOfMonth, DayCounter dayCounter) :
            base(rate)
        {
            iborIndex_ = new IborIndex("no-fix", tenor, fixingDays, new Currency(), calendar, convention,
                                       endOfMonth, dayCounter, termStructureHandle_);
            initializeDates();
        }

        public DepositRateHelper(Handle<Quote> rate, IborIndex i)
                : base(rate) {
            iborIndex_ = new IborIndex("no-fix", // never take fixing into account
                                      i.tenor(), i.fixingDays(), new Currency(),
                                      i.fixingCalendar(), i.businessDayConvention(),
                                      i.endOfMonth(), i.dayCounter(), termStructureHandle_);
            initializeDates();
        }
        public DepositRateHelper(double rate, IborIndex i)
            : base(rate) {
            iborIndex_ = new IborIndex("no-fix", // never take fixing into account
                      i.tenor(), i.fixingDays(), new Currency(),
                      i.fixingCalendar(), i.businessDayConvention(),
                      i.endOfMonth(), i.dayCounter(), termStructureHandle_);
            initializeDates();
        }


        /////////////////////////////////////////
        //! RateHelper interface
        public override double impliedQuote() {
            if (termStructure_ == null) throw new ArgumentException("term structure not set");
            return iborIndex_.fixing(fixingDate_, true);
        }

        public override void setTermStructure(YieldTermStructure t) {
            // no need to register---the index is not lazy
            termStructureHandle_.linkTo(t, false);
            base.setTermStructure(t);
        }

        protected override void initializeDates() {
            earliestDate_ = iborIndex_.fixingCalendar().advance(evaluationDate_, iborIndex_.fixingDays(), TimeUnit.Days);
            latestDate_ = iborIndex_.maturityDate(earliestDate_);
            fixingDate_ = iborIndex_.fixingCalendar().advance(earliestDate_, -iborIndex_.fixingDays(), TimeUnit.Days);
        }
    }

    //! Rate helper for bootstrapping over %FRA rates
    public class FraRateHelper : RelativeDateRateHelper {
        private Date fixingDate_;
        private Period periodToStart_;
        private IborIndex iborIndex_;
        // need to init this because it is used before the handle has any link, i.e. setTermStructure will be used after ctor
        RelinkableHandle<YieldTermStructure> termStructureHandle_ = new RelinkableHandle<YieldTermStructure>();


        public FraRateHelper(Handle<Quote> rate, int monthsToStart, int monthsToEnd, int fixingDays,
                             Calendar calendar, BusinessDayConvention convention, bool endOfMonth,
                             DayCounter dayCounter) :
            base(rate) {
            periodToStart_ = new Period(monthsToStart, TimeUnit.Months);

            if (!(monthsToEnd>monthsToStart)) throw new ArgumentException("monthsToEnd must be grater than monthsToStart");
            iborIndex_ = new IborIndex("no-fix", new Period(monthsToEnd - monthsToStart, TimeUnit.Months), fixingDays,
                                    new Currency(), calendar, convention, endOfMonth, dayCounter, termStructureHandle_);
            initializeDates();
        }

        public FraRateHelper(double rate, int monthsToStart, int monthsToEnd, int fixingDays, Calendar calendar,
                             BusinessDayConvention convention, bool endOfMonth, DayCounter dayCounter)
            : base(rate) {
            periodToStart_ = new Period(monthsToStart, TimeUnit.Months);

            if (!(monthsToEnd>monthsToStart)) throw new ArgumentException("monthsToEnd must be grater than monthsToStart");
            iborIndex_ = new IborIndex("no-fix", new Period(monthsToEnd - monthsToStart, TimeUnit.Months), fixingDays,
                                    new Currency(), calendar, convention, endOfMonth, dayCounter, termStructureHandle_);
            initializeDates();
        }

        public FraRateHelper(Handle<Quote> rate, int monthsToStart, IborIndex i) : base(rate) {
            periodToStart_ = new Period(monthsToStart, TimeUnit.Months);

            iborIndex_ = new IborIndex("no-fix",  // never take fixing into account
                                       i.tenor(), i.fixingDays(), new Currency(),
                                       i.fixingCalendar(), i.businessDayConvention(),
                                       i.endOfMonth(), i.dayCounter(), termStructureHandle_);

            initializeDates();
        }

        public FraRateHelper(double rate, int monthsToStart, IborIndex i) : base(rate) {
            periodToStart_ = new Period(monthsToStart, TimeUnit.Months);

            iborIndex_ = new IborIndex("no-fix",  // never take fixing into account
                                       i.tenor(), i.fixingDays(), new Currency(),
                                       i.fixingCalendar(), i.businessDayConvention(),
                                       i.endOfMonth(), i.dayCounter(), termStructureHandle_);

            initializeDates();
        }

        public override void setTermStructure(YieldTermStructure t) {
            // no need to register---the index is not lazy
            termStructureHandle_.linkTo(t, false);
            base.setTermStructure(t);
        }


        /////////////////////////////////////////////////////////
        //! RateHelper interface
        public override double impliedQuote() {
            if (termStructure_ == null) throw new ArgumentException("term structure not set");
            return iborIndex_.fixing(fixingDate_, true);
        }
        
        protected override void initializeDates() {
            // why not using index_->fixingDays instead of settlementDays_
            Date settlement = iborIndex_.fixingCalendar().advance(evaluationDate_, iborIndex_.fixingDays(), TimeUnit.Days);
            earliestDate_ = iborIndex_.fixingCalendar().advance(settlement, periodToStart_,
                                                                iborIndex_.businessDayConvention(), iborIndex_.endOfMonth());
            latestDate_ = iborIndex_.maturityDate(earliestDate_);
            fixingDate_ = iborIndex_.fixingDate(earliestDate_);
        }
    }

    // Rate helper for bootstrapping over swap rates
    public class SwapRateHelper : RelativeDateRateHelper {
        protected Period tenor_;
        protected Calendar calendar_;
        protected BusinessDayConvention fixedConvention_;
        protected Frequency fixedFrequency_;
        protected DayCounter fixedDayCount_;
        protected IborIndex iborIndex_;
        protected VanillaSwap swap_;
        // need to init this because it is used before the handle has any link, i.e. setTermStructure will be used after ctor
        RelinkableHandle<YieldTermStructure> termStructureHandle_ = new RelinkableHandle<YieldTermStructure>();
        protected Handle<Quote> spread_;
        protected Period fwdStart_;


        #region ctors
        //public SwapRateHelper(Quote rate, SwapIndex swapIndex) :
        //    this(rate, swapIndex, new SimpleQuote(), new Period(0, TimeUnit.Days)) { }
        //public SwapRateHelper(Quote rate, SwapIndex swapIndex, Quote spread) :
        //    this(rate, swapIndex, spread, new Period(0, TimeUnit.Days)) { }
        public SwapRateHelper(Handle<Quote> rate, SwapIndex swapIndex, Handle<Quote> spread, Period fwdStart)
            : base(rate) {
            tenor_ = swapIndex.tenor();
            calendar_ = swapIndex.fixingCalendar();
            fixedConvention_ = swapIndex.fixedLegConvention();
            fixedFrequency_ = swapIndex.fixedLegTenor().frequency();
            fixedDayCount_ = swapIndex.dayCounter();
            iborIndex_ = swapIndex.iborIndex();
            spread_ = spread;
            fwdStart_ = fwdStart;

            // add observers
            iborIndex_.registerWith(update);
            spread_.registerWith(update);

            initializeDates();
        }

        public SwapRateHelper(Handle<Quote> rate, Period tenor, Calendar calendar,
               Frequency fixedFrequency, BusinessDayConvention fixedConvention, DayCounter fixedDayCount,
               IborIndex iborIndex) :
            this(rate, tenor, calendar, fixedFrequency, fixedConvention, fixedDayCount, iborIndex,
                 new Handle<Quote>(), new Period(0, TimeUnit.Days)) { }

        public SwapRateHelper(double rate, Period tenor, Calendar calendar,
               Frequency fixedFrequency, BusinessDayConvention fixedConvention, DayCounter fixedDayCount,
               IborIndex iborIndex) :
            this(rate, tenor, calendar, fixedFrequency, fixedConvention, fixedDayCount, iborIndex,
                 new Handle<Quote>(), new Period(0, TimeUnit.Days)) { }

        //public SwapRateHelper(Quote rate, Period tenor, Calendar calendar,
        //               Frequency fixedFrequency, BusinessDayConvention fixedConvention, DayCounter fixedDayCount,
        //               IborIndex iborIndex, Quote spread) :
        //    this(rate, tenor, calendar, fixedFrequency, fixedConvention, fixedDayCount, iborIndex,
        //         spread, new Period(0, TimeUnit.Days)) { }
        public SwapRateHelper(Handle<Quote> rate, Period tenor, Calendar calendar,
            // fixed leg
                       Frequency fixedFrequency, BusinessDayConvention fixedConvention, DayCounter fixedDayCount,
            // floating leg
                       IborIndex iborIndex, Handle<Quote> spread, Period fwdStart)
            : base(rate) {
            tenor_ = tenor;
            calendar_ = calendar;
            fixedConvention_ = fixedConvention;
            fixedFrequency_ = fixedFrequency;
            fixedDayCount_ = fixedDayCount;
            iborIndex_ = iborIndex;
            spread_ = spread;
            fwdStart_ = fwdStart;

            // add observers
            iborIndex_.registerWith(update);
            spread_.registerWith(update);

            initializeDates();
        }


        //public SwapRateHelper(double rate, Period tenor, Calendar calendar,
        //               Frequency fixedFrequency, BusinessDayConvention fixedConvention, DayCounter fixedDayCount,
        //               IborIndex iborIndex) :
        //    this(rate, tenor, calendar, fixedFrequency, fixedConvention, fixedDayCount, iborIndex,
        //         new SimpleQuote(), new Period(0, TimeUnit.Days)) { }
        //public SwapRateHelper(double rate, Period tenor, Calendar calendar,
        //               Frequency fixedFrequency, BusinessDayConvention fixedConvention, DayCounter fixedDayCount,
        //               IborIndex iborIndex, Quote spread) :
        //    this(rate, tenor, calendar, fixedFrequency, fixedConvention, fixedDayCount, iborIndex,
        //         spread, new Period(0, TimeUnit.Days)) { }
        public SwapRateHelper(double rate, Period tenor, Calendar calendar,
            // fixed leg
                       Frequency fixedFrequency, BusinessDayConvention fixedConvention, DayCounter fixedDayCount,
            // floating leg
                       IborIndex iborIndex, Handle<Quote> spread, Period fwdStart)
            : base(rate) {
            tenor_ = tenor;
            calendar_ = calendar;
            fixedConvention_ = fixedConvention;
            fixedFrequency_ = fixedFrequency;
            fixedDayCount_ = fixedDayCount;
            iborIndex_ = iborIndex;
            spread_ = spread;
            fwdStart_ = fwdStart;

            // add observers
            iborIndex_.registerWith(update);
            spread_.registerWith(update);

            initializeDates();
        }

        //public SwapRateHelper(double rate, SwapIndex swapIndex)
        //    : this(rate, swapIndex, new SimpleQuote()) { }
        //public SwapRateHelper(double rate, SwapIndex swapIndex, Quote spread)
        //    : this(rate, swapIndex, spread, new Period(0, TimeUnit.Days)) { }
        public SwapRateHelper(double rate, SwapIndex swapIndex, Handle<Quote> spread, Period fwdStart)
            : base(rate) {
            tenor_ = swapIndex.tenor();
            calendar_ = swapIndex.fixingCalendar();
            fixedConvention_ = swapIndex.fixedLegConvention();
            fixedFrequency_ = swapIndex.fixedLegTenor().frequency();
            fixedDayCount_ = swapIndex.dayCounter();
            iborIndex_ = swapIndex.iborIndex();
            spread_ = spread;
            fwdStart_ = fwdStart;

            // add observers
            iborIndex_.registerWith(update);
            spread_.registerWith(update);

            initializeDates();
        }
        #endregion
        

        protected override void initializeDates() {
            // dummy ibor index with curve/swap arguments
            IborIndex clonedIborIndex = iborIndex_.clone(termStructureHandle_);

            // do not pass the spread here, as it might be a Quote i.e. it can dinamically change
            swap_ = new MakeVanillaSwap(tenor_, clonedIborIndex, 0.0, fwdStart_)
                                        .withFixedLegDayCount(fixedDayCount_)
                                        .withFixedLegTenor(new Period(fixedFrequency_))
                                        .withFixedLegConvention(fixedConvention_)
                                        .withFixedLegTerminationDateConvention(fixedConvention_)
                                        .withFixedLegCalendar(calendar_)
                                        .withFloatingLegCalendar(calendar_);

            earliestDate_ = swap_.startDate();

            // Usually...
            latestDate_ = swap_.maturityDate();
            // ...but due to adjustments, the last floating coupon might
            // need a later date for fixing
            #if QL_USE_INDEXED_COUPON
            FloatingRateCoupon lastFloating = (FloatingRateCoupon)swap_.floatingLeg()[swap_.floatingLeg().Count - 1];
            Date fixingValueDate = iborIndex_.valueDate(lastFloating.fixingDate());
            Date endValueDate = iborIndex_.maturityDate(fixingValueDate);
            latestDate_ = Date.Max(latestDate_, endValueDate);
            #endif
        }

        public override void setTermStructure(YieldTermStructure t) {
            // do not set the relinkable handle as an observer -
            // force recalculation when needed
            termStructureHandle_.linkTo(t, false);
            base.setTermStructure(t);
        }

        ////////////////////////////////////////////////////
        //! RateHelper interface
        public override double impliedQuote() {
            if (termStructure_ == null) throw new ArgumentException("term structure not set");
            // we didn't register as observers - force calculation
            swap_.recalculate();                // it is from lazy objects
            // weak implementation... to be improved
            const double basisPoint = 1.0e-4;
            double floatingLegNPV = swap_.floatingLegNPV();
            double spread = this.spread();
            double spreadNPV = swap_.floatingLegBPS() / basisPoint * spread;
            double totNPV = -(floatingLegNPV + spreadNPV);
            double result = totNPV / (swap_.fixedLegBPS() / basisPoint);
            return result;
        }

        //! \name SwapRateHelper inspectors
        public double spread() { return spread_.empty() ? 0.0 : spread_.link.value(); }
        public VanillaSwap swap() { return swap_; }
        public Period forwardStart() { return fwdStart_; }
    };

    //! Rate helper for bootstrapping over BMA swap rates
    public class BMASwapRateHelper : RelativeDateRateHelper {
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

        public BMASwapRateHelper(Handle<Quote> liborFraction, Period tenor,  int settlementDays, Calendar calendar,
                          // bma leg
                          Period bmaPeriod, BusinessDayConvention bmaConvention, DayCounter bmaDayCount, BMAIndex bmaIndex,
                          // ibor leg
                          IborIndex iborIndex)    
            : base(liborFraction) {
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

        //! \name RateHelper interface
        public override double impliedQuote() {
            if (termStructure_ == null)
                throw new ApplicationException("term structure not set");
            // we didn't register as observers - force calculation
            swap_.recalculate();
            return swap_.fairLiborFraction();
        }

        public override void setTermStructure(YieldTermStructure t) {
            // do not set the relinkable handle as an observer -
            // force recalculation when needed
            termStructureHandle_.linkTo(t, false);
            base.setTermStructure(t);
        }

        protected override void initializeDates() {
            earliestDate_ = calendar_.advance(evaluationDate_, new Period(settlementDays_, TimeUnit.Days),
                                              BusinessDayConvention.Following);

            Date maturity = earliestDate_ + tenor_;

            // dummy BMA index with curve/swap arguments
            BMAIndex clonedIndex = new BMAIndex(termStructureHandle_);

            Schedule bmaSchedule = new MakeSchedule().from(earliestDate_).to(maturity)
                          .withTenor(bmaPeriod_)
                          .withCalendar(bmaIndex_.fixingCalendar())
                          .withConvention(bmaConvention_)
                          .backwards()
                          .value();

            Schedule liborSchedule = new MakeSchedule().from(earliestDate_).to(maturity)
                          .withTenor(iborIndex_.tenor())
                          .withCalendar(iborIndex_.fixingCalendar())
                          .withConvention(iborIndex_.businessDayConvention())
                          .endOfMonth(iborIndex_.endOfMonth())
                          .backwards()
                          .value();

            swap_ = new BMASwap(BMASwap.Type.Payer, 100.0, liborSchedule, 0.75, // arbitrary
                                0.0, iborIndex_, iborIndex_.dayCounter(), bmaSchedule, clonedIndex, bmaDayCount_);
            swap_.setPricingEngine(new DiscountingSwapEngine(iborIndex_.forwardingTermStructure()));

            Date d = calendar_.adjust(swap_.maturityDate(), BusinessDayConvention.Following);
            int w = d.weekday();
            Date nextWednesday = (w >= 4) ? d + new Period((11 - w), TimeUnit.Days) :
                                            d + new Period((4 - w), TimeUnit.Days);
            latestDate_ = clonedIndex.valueDate(clonedIndex.fixingCalendar().adjust(nextWednesday));
        }
    }
}
