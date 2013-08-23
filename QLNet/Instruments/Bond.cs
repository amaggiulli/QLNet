/*
 Copyright (C) 2008, 2009 Siarhei Novik (snovik@gmail.com)
  
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
using System.Collections.Generic;
using System.Linq;

namespace QLNet {
    public partial class Utils {
        public static double dirtyPriceFromYield(double faceAmount, List<CashFlow> cashflows, double yield, DayCounter dayCounter,
                             Compounding compounding, Frequency frequency, Date settlement) {

            if (frequency == Frequency.NoFrequency || frequency == Frequency.Once)
                frequency = Frequency.Annual;

            InterestRate y = new InterestRate(yield, dayCounter, compounding, frequency);

            double price = 0.0;
            double discount = 1.0;
            Date lastDate = null;

            for (int i = 0; i < cashflows.Count - 1; ++i) {
                if (cashflows[i].hasOccurred(settlement))
                    continue;

                Date couponDate = cashflows[i].date();
                double amount = cashflows[i].amount();
                if (lastDate == null) {
                    // first not-expired coupon
                    if (i > 0) {
                        lastDate = cashflows[i - 1].date();
                    } else {
                        if (cashflows[i].GetType().IsSubclassOf(typeof(Coupon)))
                            lastDate = ((Coupon)cashflows[i]).accrualStartDate();
                        else
                            lastDate = couponDate - new Period(1, TimeUnit.Years);
                    }
                    discount *= y.discountFactor(settlement, couponDate, lastDate, couponDate);
                } else {
                    discount *= y.discountFactor(lastDate, couponDate);
                }
                lastDate = couponDate;

                price += amount * discount;
            }

            CashFlow redemption = cashflows.Last();
            if (!redemption.hasOccurred(settlement)) {
                Date redemptionDate = redemption.date();
                double amount = redemption.amount();
                if (lastDate == null) {
                    // no coupons
                    lastDate = redemptionDate - new Period(1, TimeUnit.Years);
                    discount *= y.discountFactor(settlement, redemptionDate, lastDate, redemptionDate);
                } else {
                    discount *= y.discountFactor(lastDate, redemptionDate);
                }

                price += amount * discount;
            }

            return price / faceAmount * 100.0;
        }
        
        public static double dirtyPriceFromZSpreadFunction(double faceAmount, List<CashFlow> cashflows, double zSpread,
                                                           DayCounter dc, Compounding comp, Frequency freq, Date settlement,
                                                           Handle<YieldTermStructure> discountCurve) {

            if (!(freq != Frequency.NoFrequency && freq != Frequency.Once))
                throw new ApplicationException("invalid frequency:" + freq);

            Quote zSpreadQuoteHandle = new SimpleQuote(zSpread);

            var spreadedCurve = new ZeroSpreadedTermStructure(discountCurve, zSpreadQuoteHandle, comp, freq, dc);
            
            double price = 0.0;
            foreach (CashFlow cf in cashflows.FindAll(x => !x.hasOccurred(settlement))) {
                Date couponDate = cf.date();
                double amount = cf.amount();
                price += amount * spreadedCurve.discount(couponDate);
            }
            price /= spreadedCurve.discount(settlement);
            return price/faceAmount*100.0;
        }
    }


    public class YieldFinder : ISolver1d {
        private double faceAmount_;
        private List<CashFlow> cashflows_;
        private double dirtyPrice_;
        private Compounding compounding_;
        private DayCounter dayCounter_;
        private Frequency frequency_;
        private Date settlement_;

        public YieldFinder(double faceAmount, List<CashFlow> cashflows, double dirtyPrice, DayCounter dayCounter,
                           Compounding compounding, Frequency frequency, Date settlement) {
            faceAmount_ = faceAmount;
            cashflows_ = cashflows;
            dirtyPrice_ = dirtyPrice;
            compounding_ = compounding;
            dayCounter_ = dayCounter;
            frequency_ = frequency;
            settlement_ = settlement;
        }

        public override double value(double yield) {
            return dirtyPrice_
                 - Utils.dirtyPriceFromYield(faceAmount_, cashflows_, yield, dayCounter_, compounding_, frequency_, settlement_);
        }
    }


    //! Base bond class
    /*! Derived classes must fill the unitialized data members.

        \warning Most methods assume that the cashflows are stored sorted by date, the redemption being the last one.

        \test
        - price/yield calculations are cross-checked for consistency.
        - price/yield calculations are checked against known good values. */
    public class Bond : Instrument {
        #region properties
        protected int settlementDays_;
        protected Calendar calendar_;
        protected List<Date> notionalSchedule_ = new List<Date>();
        protected List<double> notionals_ = new List<double>();
        protected List<CashFlow> cashflows_; // all cashflows
        protected List<CashFlow> redemptions_ = new List<CashFlow>(); // the redemptions
        protected Date maturityDate_, issueDate_;

        protected double? settlementValue_;

        public int settlementDays() { return settlementDays_; }
        public Calendar calendar() { return calendar_; }
        public double faceAmount() { return notionals_.First(); }
        public List<double> notionals() { return notionals_; }
        public List<CashFlow> cashflows() { return cashflows_; }
        public List<CashFlow> redemptions() { return redemptions_; }
        public Date maturityDate() { return (maturityDate_ != null) ? maturityDate_ : cashflows_.Last().date(); }
        public Date issueDate() { return issueDate_; }
        #endregion

        #region Constructors
        //! constructor for amortizing or non-amortizing bonds.
        /*! Redemptions and maturity are calculated from the coupon
            data, if available.  Therefore, redemptions must not be
            included in the passed cash flows.
        */
        //public Bond(int settlementDays, Calendar calendar, Date issueDate = Date(), List<CashFlow> coupons = Leg());
        public Bond(int settlementDays, Calendar calendar, Date issueDate) 
            : this(settlementDays, calendar, issueDate, new List<CashFlow>()) { }
        public Bond(int settlementDays, Calendar calendar, Date issueDate, List<CashFlow> coupons) {
            settlementDays_ = settlementDays;
            calendar_ = calendar;
            cashflows_ = coupons;
            issueDate_ = issueDate;

            if (coupons.Count != 0) {
                cashflows_.Sort();
                maturityDate_ = coupons.Last().date();
                addRedemptionsToCashflows();
            }

            Settings.registerWith(update);
        }

        //! old constructor for non amortizing bonds.
        /*! \warning The last passed cash flow must be the bond
                     redemption. No other cash flow can have a date
                     later than the redemption date.
        */
        public Bond(int settlementDays, Calendar calendar, double faceAmount, Date maturityDate, Date issueDate) 
            : this(settlementDays, calendar, faceAmount, maturityDate, issueDate, new List<CashFlow>()) { }
        public Bond(int settlementDays, Calendar calendar, double faceAmount, Date maturityDate, Date issueDate,
                    List<CashFlow> cashflows) {
            settlementDays_ = settlementDays;
            calendar_ = calendar;
            cashflows_ = cashflows;
            maturityDate_ = maturityDate;
            issueDate_ = issueDate;

            if (cashflows.Count != 0) {
                notionalSchedule_.Add(new Date());
                notionals_.Add(faceAmount);

                notionalSchedule_.Add(maturityDate);
                notionals_.Add(0.0);

                redemptions_.Add(cashflows.Last());

                cashflows_.Sort(0, cashflows_.Count - 2, null);
            }

            Settings.registerWith(update);
        } 
	    #endregion


        public double notional(Date d) {
            if (d == null)
                d = settlementDate();

            if (d > notionalSchedule_.Last())
                // after maturity
                return 0.0;

            // After the check above, d is between the schedule
            // boundaries.  We search starting from the second notional
            // date, since the first is null.  After the call to
            // lower_bound, *i is the earliest date which is greater or
            // equal than d.  Its index is greater or equal to 1.
            int index = notionalSchedule_.FindIndex(x => d <= x);

            if (d < notionalSchedule_[index]) {
                // no doubt about what to return
                return notionals_[index-1];
            } else {
                // d is equal to a redemption date.
                #if QL_TODAYS_PAYMENTS
                // We consider today's payment as pending; the bond still
                // has the previous notional
                return notionals_[index-1];
                #else
                // today's payment has occurred; the bond already changed
                // notional.
                return notionals_[index];
                #endif
            }
        }

        public CashFlow redemption() { 
            if (redemptions_.Count != 1)
                throw new ApplicationException("multiple redemption cash flows given");
            return redemptions_.Last();
        }


        public Date settlementDate() { return settlementDate(null); }
        public Date settlementDate(Date date) {
            Date d = (date==null ? Settings.evaluationDate() : date);

            // usually, the settlement is at T+n...
            Date settlement = calendar_.advance(d, settlementDays_, TimeUnit.Days);
            // ...but the bond won't be traded until the issue date (if given.)
            if (issueDate_ == null)
                return settlement;
            else
                return Date.Max(settlement, issueDate_);
        }

        //@}
        //! \name Calculations
        //@{
        //! theoretical clean price
        /*! The default bond settlement is used for calculation.

            \warning the theoretical price calculated from a flat term structure might differ slightly from the price
                     calculated from the corresponding yield by means of the other overload of this function. If the
                     price from a constant yield is desired, it is advisable to use such other overload. */
        public double cleanPrice() { return dirtyPrice() - accruedAmount(settlementDate()); }

        //! theoretical dirty price
        /*! The default bond settlement is used for calculation.

            \warning the theoretical price calculated from a flat term structure might differ slightly from the price
                     calculated from the corresponding yield by means of the other overload of this function. If the
                     price from a constant yield is desired, it is advisable to use such other overload.
        */
         public double dirtyPrice() 
         {
            double currentNotional = notional(settlementDate());
            if (currentNotional == 0.0)
               return 0.0;
            else
               return settlementValue()/notional(settlementDate())*100.0; 
        }

        public double settlementValue() {
            calculate();
            if (settlementValue_ == null)
                throw new ApplicationException("settlement value not provided");
            return settlementValue_.Value;
        }

        public double settlementValue(double cleanPrice) {
            double dirtyPrice = cleanPrice + accruedAmount(settlementDate());
            return dirtyPrice/100.0 * notional(settlementDate());
        }

        //! clean price given a yield and settlement date
        /*! The default bond settlement is used if no date is given. */
        public double cleanPrice(double yield, DayCounter dc, Compounding comp, Frequency freq) {
            return cleanPrice(yield, dc, comp, freq, null); }
        public double cleanPrice(double yield, DayCounter dc, Compounding comp, Frequency freq, Date settlement) {
            if (settlement == null)
                settlement = settlementDate();
            return dirtyPrice(yield, dc, comp, freq, settlement) - accruedAmount(settlement);
        }

        //! dirty price given a yield and settlement date
        /*! The default bond settlement is used if no date is given. */
        public double dirtyPrice(double yield, DayCounter dc, Compounding comp, Frequency freq, Date settlement) {
            if (settlement == null)
                settlement = settlementDate();
            return Utils.dirtyPriceFromYield(notional(settlement), cashflows_, yield, dc, comp, freq, settlement);
        }


        //! theoretical bond yield
        /*! The default bond settlement and theoretical price are used for calculation. */
        public double yield(DayCounter dc, Compounding comp, Frequency freq) { return yield(dc, comp, freq, 1.0e-8); }
        public double yield(DayCounter dc, Compounding comp, Frequency freq, double accuracy) {
            return yield(dc, comp, freq, accuracy, 100);
        }
        public double yield(DayCounter dc, Compounding comp, Frequency freq, double accuracy, int maxEvaluations) {
            Brent solver = new Brent();
            solver.setMaxEvaluations(maxEvaluations);
            YieldFinder objective = new YieldFinder(notional(settlementDate()), cashflows_, dirtyPrice(), dc, comp, freq, settlementDate());
            return solver.solve(objective, accuracy, 0.02, 0.0, 1.0);
        }

        public double yield(double cleanPrice, DayCounter dc, Compounding comp, Frequency freq) {
            return yield(cleanPrice, dc, comp, freq, null, 1.0e-8, 100);
        }
        public double yield(double cleanPrice, DayCounter dc, Compounding comp, Frequency freq, Date settlement) {
            return yield(cleanPrice, dc, comp, freq, settlement, 1.0e-8, 100);
        }
        public double yield(double cleanPrice, DayCounter dc, Compounding comp, Frequency freq, Date settlement,
                            double accuracy, int maxEvaluations) {
            if (settlement == null)
                settlement = settlementDate();
            Brent solver = new Brent();
            solver.setMaxEvaluations(maxEvaluations);
            double dirtyPrice = cleanPrice + accruedAmount(settlement);
            YieldFinder objective = new YieldFinder(notional(settlement), cashflows_, dirtyPrice, dc, comp, freq, settlement);
            return solver.solve(objective, accuracy, 0.02, 0.0, 1.0);
        }

        //! clean price given Z-spread
        /*! Z-spread compounding, frequency, daycount are taken into account
            The default bond settlement is used if no date is given.
            For details on Z-spread refer to:
            "Credit Spreads Explained", Lehman Brothers European Fixed Income Research - March 2004, D. O'Kane*/
        public double cleanPriceFromZSpread(double zSpread, DayCounter dc, Compounding comp, Frequency freq, Date settlement) {
            double p = dirtyPriceFromZSpread(zSpread, dc, comp, freq, settlement);
            return p - accruedAmount(settlement);
        }

        //! dirty price given Z-spread
        /*! Z-spread compounding, frequency, daycount are taken into account
            The default bond settlement is used if no date is given.
            For details on Z-spread refer to:
            "Credit Spreads Explained", Lehman Brothers European Fixed Income Research - March 2004, D. O'Kane*/
        public double dirtyPriceFromZSpread(double zSpread, DayCounter dc, Compounding comp, Frequency freq, Date settlement) {
            if (settlement == null)
                 settlement = settlementDate();

            if (engine_ == null)
                throw new ApplicationException("null pricing engine");

            if (!engine_.GetType().IsSubclassOf(typeof(DiscountingBondEngine)))
                throw new ApplicationException("engine not compatible with calculation");

             return Utils.dirtyPriceFromZSpreadFunction(notional(settlement), cashflows_, zSpread, dc, comp, freq,
                                                  settlement, ((DiscountingBondEngine)engine_).discountCurve());
        }

        //! accrued amount at a given date
        /*! The default bond settlement is used if no date is given. */
        public double accruedAmount() { return accruedAmount(null); }
        public double accruedAmount(Date settlement) {
            if (settlement==null)
                settlement = settlementDate();

            CashFlow cf = CashFlows.nextCashFlow(cashflows_,false, settlement);
            if (cf==cashflows_.Last()) return 0.0;

            Date paymentDate = cf.date();
            bool firstCouponFound = false;
            double nominal = 0;
            double accrualPeriod = 0;
            DayCounter dc = null;
            double result = 0.0;
            foreach(CashFlow x in cashflows_.FindAll(x => x.date()==paymentDate && x.GetType().IsSubclassOf(typeof(Coupon)))) {
                Coupon cp = (Coupon)x;
                if (firstCouponFound) {
                    if (!(nominal == cp.nominal() && accrualPeriod == cp.accrualPeriod() && dc == cp.dayCounter()))
                        throw new ApplicationException("cannot aggregate accrued amount of two different coupons on " + paymentDate);
                } else {
                    firstCouponFound = true;
                    nominal = cp.nominal();
                    accrualPeriod = cp.accrualPeriod();
                    dc = cp.dayCounter();
                }
                result += cp.accruedAmount(settlement);
            }
            return result/notional(settlement)*100.0;
        }

        public override bool isExpired() {
            return cashflows_.Last().hasOccurred(settlementDate());
        }

        /*! Expected next coupon: depending on (the bond and) the given date the coupon can be historic, deterministic
            or expected in a stochastic sense. When the bond settlement date is used the coupon
            is the already-fixed not-yet-paid one.

            The current bond settlement is used if no date is given. */
        public double nextCoupon() { return nextCoupon(null); }
        public double nextCoupon(Date settlement) {
            if (settlement == null)
                settlement = settlementDate();
            return CashFlows.nextCouponRate(cashflows_,false, settlement);
        }

        //! Previous coupon already paid at a given date
        /*! Expected previous coupon: depending on (the bond and) the given date the coupon can be historic, deterministic
            or expected in a stochastic sense. When the bond settlement date is used the coupon is the last paid one.

            The current bond settlement is used if no date is given. */
        public double previousCoupon() { return previousCoupon(null); }
        public double previousCoupon(Date settlement) {
            if (settlement == null)
                settlement = settlementDate();
            return CashFlows.previousCouponRate(cashflows_, false, settlement);
        }

        public Date nextCouponDate() { return nextCouponDate(null); }
        public Date nextCouponDate(Date settlement) {
            if (settlement == null)
                settlement = settlementDate();
            return CashFlows.nextCouponDate(cashflows_, false, settlement);
        }

        public Date previousCouponDate() { return previousCouponDate(null); }
        public Date previousCouponDate(Date settlement) {
            if (settlement == null)
                settlement = settlementDate();
            return CashFlows.previousCouponDate(cashflows_, false, settlement);
        }

        protected override void setupExpired() {
            base.setupExpired();
            settlementValue_ = null;
        }

        public override void setupArguments(IPricingEngineArguments args) {
            Bond.Arguments arguments = args as Bond.Arguments;
            if (args == null)
                throw new ApplicationException("wrong argument type");

            arguments.settlementDate = settlementDate();
            arguments.cashflows = cashflows_;
            arguments.calendar = calendar_;
        }

        public override void fetchResults(IPricingEngineResults r) {
            base.fetchResults(r);

            Bond.Results results = r as Bond.Results;
            if (results ==  null)
                throw new ApplicationException("wrong result type");

            settlementValue_ = results.settlementValue;
        }

        /*! This method can be called by derived classes in order to
            build redemption payments from the existing cash flows.
            It must be called after setting up the cashflows_ vector
            and will fill the notionalSchedule_, notionals_, and
            redemptions_ data members.

            If given, the elements of the redemptions vector will
            multiply the amount of the redemption cash flow.  The
            elements will be taken in base 100, i.e., a redemption
            equal to 100 does not modify the amount.

            \pre The cashflows_ vector must contain at least one
                 coupon and must be sorted by date.
        */
        protected void addRedemptionsToCashflows() { addRedemptionsToCashflows(new List<double>()); }
        protected void addRedemptionsToCashflows(List<double> redemptions) {
            // First, we gather the notional information from the cashflows
            calculateNotionalsFromCashflows();
            // Then, we create the redemptions based on the notional
            // information and we add them to the cashflows vector after
            // the coupons.
            redemptions_.Clear();
            for (int i=1; i<notionalSchedule_.Count; ++i) {
                double R = i < redemptions.Count ? redemptions[i] :
                           !redemptions.empty()  ? redemptions.Last() :
                                                  100.0;
                double amount = (R/100.0)*(notionals_[i-1]-notionals_[i]);
                CashFlow redemption;
                if (i < notionalSchedule_.Count - 1)
                   //payment.reset(new AmortizingPayment(amount,notionalSchedule_[i]));
                   redemption = new AmortizingPayment(amount, notionalSchedule_[i]);
                else
                   //payment.reset(new Redemption(amount, notionalSchedule_[i]));
                   redemption = new Redemption(amount, notionalSchedule_[i]);

                //CashFlow redemption = new SimpleCashFlow(amount, notionalSchedule_[i]);
                cashflows_.Add(redemption);
                redemptions_.Add(redemption);
            }
            // stable_sort now moves the redemptions to the right places
            // while ensuring that they follow coupons with the same date.
            cashflows_.Sort();
        }


        /*! This method can be called by derived classes in order to
            build a bond with a single redemption payment.  It will
            fill the notionalSchedule_, notionals_, and redemptions_
            data members.
        */
        protected void setSingleRedemption(double notional, double redemption, Date date) {
            CashFlow redemptionCashflow = new Redemption(notional*redemption/100.0, date);
            setSingleRedemption(notional, redemptionCashflow);
        }

        protected void setSingleRedemption(double notional, CashFlow redemption) {
            notionals_.Clear();
            notionalSchedule_.Clear();
            redemptions_.Clear();

            notionalSchedule_.Add(new Date());
            notionals_.Add(notional);

            notionalSchedule_.Add(redemption.date());
            notionals_.Add(0.0);

            cashflows_.Add(redemption);
            redemptions_.Add(redemption);
        }


        protected void calculateNotionalsFromCashflows() {
            notionalSchedule_.Clear();
            notionals_.Clear();

            Date lastPaymentDate = new Date();
            //notionalSchedule_.Add((Coupon)(cashflows_[0])accrualStartDate());
            for (int i=0; i<cashflows_.Count; ++i) {
                Coupon coupon = cashflows_[i] as Coupon;
                if (coupon == null)
                    continue;
                if (i == 0)
                   notionalSchedule_.Add(coupon.accrualStartDate());
                double notional = coupon.nominal();
                // we add the notional only if it is the first one...
                if (notionals_.empty()) {
                    notionals_.Add(coupon.nominal());
                    lastPaymentDate = coupon.date();
                } else if (!Utils.close(notional, notionals_.Last())) {
                    // ...or if it has changed.
                    if (!(notional < notionals_.Last()))
                        throw new ApplicationException("increasing coupon notionals");
                    notionals_.Add(coupon.nominal());
                    // in this case, we also add the last valid date for
                    // the previous one...
                    notionalSchedule_.Add(lastPaymentDate);
                    // ...and store the candidate for this one.
                    lastPaymentDate = coupon.date();
                } else {
                    // otherwise, we just extend the valid range of dates
                    // for the current notional.
                    lastPaymentDate = coupon.date();
                }
            }
            if (notionals_.empty())
                throw new ApplicationException("no coupons provided");
            notionals_.Add(0.0);
            notionalSchedule_.Add(lastPaymentDate);
        }


        public class Engine : GenericEngine<Arguments, Results> { }

        public new class Results : Instrument.Results { 
            public double? settlementValue;
            public override void reset() {
                settlementValue = null;
                base.reset();
            }
        }

        public class Arguments : IPricingEngineArguments {
            public Date settlementDate;
            public List<CashFlow> cashflows;
            public Calendar calendar;

            public void validate() {
                if (settlementDate == null) throw new ApplicationException("no settlement date provided");
                if (cashflows.Count == 0) throw new ApplicationException("no cash flow provided");
                foreach(CashFlow cf in cashflows)
                    if (cf == null)
                        throw new ApplicationException("null coupon provided");
            }
        };
    }
}
