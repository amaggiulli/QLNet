/*
 Copyright (C) 2008, 2009 , 2010, 2011, 2012  Andrea Maggiulli (a.maggiulli@gmail.com)
  
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
using System.Text;

namespace QLNet
{
   //! when you observe an index, how do you interpolate between fixings?
   public enum InterpolationType
   {
      AsIndex,   //!< same interpolation as index
      Flat,      //!< flat from previous fixing
      Linear     //!< linearly between bracketing fixings
   }

   //! %Coupon paying the performance of a CPI (zero inflation) index
   /*! The performance is relative to the index value on the base date.

      The other inflation value is taken from the refPeriodEnd date
      with observation lag, so any roll/calendar etc. will be built
      in by the caller.  By default this is done in the
      InflationCoupon which uses ModifiedPreceding with fixing days
      assumed positive meaning earlier, i.e. always stay in same
      month (relative to referencePeriodEnd).

      This is more sophisticated than an %IndexedCashFlow because it
      does date calculations itself.

      \todo we do not do any convexity adjustment for lags different
            to the natural ZCIIS lag that was used to create the
            forward inflation curve.
   */
   public class CPICoupon : InflationCoupon
   {
      protected double baseCPI_;
      protected double fixedRate_;
      protected double spread_;
      protected InterpolationType observationInterpolation_;

      protected override bool checkPricerImpl(InflationCouponPricer pricer) 
      {
         CPICouponPricer p = pricer as CPICouponPricer;
         return ( p != null );
      }
      
      // use to calculate for fixing date, allows change of
      // interpolation w.r.t. index.  Can also be used ahead of time
      protected double indexFixing(Date d)
      {
         // you may want to modify the interpolation of the index
        // this gives you the chance

        double I1;
        // what interpolation do we use? Index / flat / linear
        if (observationInterpolation() == InterpolationType.AsIndex) 
        {
           I1 = cpiIndex().fixing(d);
        } 
        else 
        {
            // work out what it should be
            KeyValuePair<Date,Date> dd = Utils.inflationPeriod(d, cpiIndex().frequency());
            double indexStart = cpiIndex().fixing(dd.Key);
            if (observationInterpolation() == InterpolationType.Linear) 
            {
                double indexEnd = cpiIndex().fixing(dd.Value + new Period(1,TimeUnit.Days));
                // linear interpolation
                I1 = indexStart + (indexEnd - indexStart) * (d - dd.Key)
                / (double)((dd.Value + new Period(1, TimeUnit.Days)) - dd.Key); // can't get to next period's value within current period
            } else {
                // no interpolation, i.e. flat = constant, so use start-of-period value
                I1 = indexStart;
            }

        }
        return I1;
      }
      
      public CPICoupon(double baseCPI, // user provided, could be arbitrary
                        Date paymentDate,
                        double nominal,
                        Date startDate,
                        Date endDate,
                        int fixingDays,
                        ZeroInflationIndex index,
                        Period observationLag,
                        InterpolationType observationInterpolation,
                        DayCounter dayCounter,
                        double fixedRate, // aka gearing
                        double spread = 0.0,
                        Date refPeriodStart = null,
                        Date refPeriodEnd = null,
                        Date exCouponDate = null)
         :base(paymentDate, nominal, startDate, endDate, fixingDays, index, 
               observationLag, dayCounter, refPeriodStart, refPeriodEnd, exCouponDate)
      {

         baseCPI_ = baseCPI;
         fixedRate_ = fixedRate;
         spread_ = spread;
         observationInterpolation_ = observationInterpolation;
         Utils.QL_REQUIRE( Math.Abs( baseCPI_ ) > 1e-16, () => "|baseCPI_| < 1e-16, future divide-by-zero problem" );
      }

      //! \name Inspectors
      //@{
      //! fixed rate that will be inflated by the index ratio
      public double fixedRate() { return fixedRate_; }
      //! spread paid over the fixing of the underlying index
      public double spread() { return spread_; }

      //! adjusted fixing (already divided by the base fixing)
      public double adjustedFixing() { return (rate() - spread()) / fixedRate(); }
      //! allows for a different interpolation from the index
      public override double indexFixing() { return indexFixing(fixingDate()); }
      //! base value for the CPI index
      /*! \warning make sure that the interpolation used to create
                  this is what you are using for the fixing,
                  i.e. the observationInterpolation.
      */
      public double baseCPI() { return baseCPI_; }
      //! how do you observe the index?  as-is, flat, linear?
      public InterpolationType observationInterpolation() { return observationInterpolation_; }
      //! utility method, calls indexFixing
      public double indexObservation(Date onDate) { return indexFixing(onDate); }
      //! index used
      public ZeroInflationIndex cpiIndex() { return index() as ZeroInflationIndex; }
      //@}
   }

   //! Cash flow paying the performance of a CPI (zero inflation) index
   /*! It is NOT a coupon, i.e. no accruals. */
   public class CPICashFlow : IndexedCashFlow
   {
      public CPICashFlow(double notional,
                         ZeroInflationIndex index,
                         Date baseDate,
                         double baseFixing,
                         Date fixingDate,
                         Date paymentDate,
                         bool growthOnly = false,
                         InterpolationType interpolation = InterpolationType.AsIndex,
                         Frequency frequency = Frequency.NoFrequency)
      : base(notional, index, baseDate, fixingDate,paymentDate, growthOnly)
      {
         baseFixing_= baseFixing;
         interpolation_= interpolation;
         frequency_=frequency;

         if(Math.Abs(baseFixing_) <= 1e-16)
               throw new ApplicationException("|baseFixing|<1e-16, future divide-by-zero error");

         if (interpolation_ != InterpolationType.AsIndex)
         {
               if ( frequency_ == Frequency.NoFrequency)
                  throw new ApplicationException ("non-index interpolation w/o frequency");
         }
        }
        
      //! value used on base date
      /*! This does not have to agree with index on that date. */
      public virtual double baseFixing() {return baseFixing_;}

      //! you may not have a valid date
      public override Date baseDate() {throw new ApplicationException();}

      //! do you want linear/constant/as-index interpolation of future data?
      public virtual InterpolationType interpolation() { return interpolation_; }

      public virtual Frequency frequency() { return frequency_; }

      //! redefined to use baseFixing() and interpolation
      public override double amount()
      { 
         double I0 = baseFixing();
         double I1;

         // what interpolation do we use? Index / flat / linear
         if (interpolation() == InterpolationType.AsIndex ) 
         {
               I1 = index().fixing(fixingDate());
         } 
         else 
         {
               // work out what it should be
               //std::cout << fixingDate() << " and " << frequency() << std::endl;
               //std::pair<Date,Date> dd = inflationPeriod(fixingDate(), frequency());
               //std::cout << fixingDate() << " and " << dd.first << " " << dd.second << std::endl;
               // work out what it should be
               KeyValuePair<Date,Date> dd = Utils.inflationPeriod(fixingDate(), frequency());
               double indexStart = index().fixing(dd.Key);
               if (interpolation() == InterpolationType.Linear)
               {
                  double indexEnd = index().fixing(dd.Value + new Period(1, TimeUnit.Days));
                  // linear interpolation
                  //std::cout << indexStart << " and " << indexEnd << std::endl;
                  I1 = indexStart + (indexEnd - indexStart) * (fixingDate() - dd.Key)
                  / ((dd.Value + new Period(1, TimeUnit.Days)) - dd.Key); // can't get to next period's value within current period
               } 
               else 
               {
                  // no interpolation, i.e. flat = constant, so use start-of-period value
                  I1 = indexStart;
               }

         }

         if (growthOnly())
               return notional() * (I1 / I0 - 1.0);
         else
               return notional() * (I1 / I0);
        }

        protected double baseFixing_;
        protected InterpolationType interpolation_;
        protected Frequency frequency_;
    }

    //! Helper class building a sequence of capped/floored CPI coupons.
    /*! Also allowing for the inflated notional at the end...
        especially if there is only one date in the schedule.
        If a fixedRate is zero you get a FixedRateCoupon, otherwise
        you get a ZeroInflationCoupon.

        payoff is: spread + fixedRate x index
    */
   public class CPILeg : CPILegBase
   {
      public CPILeg(Schedule schedule,
                    ZeroInflationIndex index,
                    double baseCPI,
                    Period observationLag)
      {
         schedule_ = schedule;
         index_ = index;
         baseCPI_ = baseCPI;
         observationLag_ = observationLag;
         paymentDayCounter_ = new Thirty360();
         paymentAdjustment_ = BusinessDayConvention.ModifiedFollowing;
         paymentCalendar_ = schedule.calendar();
         fixingDays_ = new List<int>() { 0 };
         observationInterpolation_ = InterpolationType.AsIndex;
         subtractInflationNominal_ = true;
         spreads_ = new List<double>() { 0 };
      }

      public override List<CashFlow> value()
      {
         if (notionals_.empty())
            throw new ApplicationException("no notional given");

         int n = schedule_.Count - 1;
         List<CashFlow> leg = new List<CashFlow>(n + 1);

         if (n > 0)
         {
            if (fixedRates_.empty() && spreads_.empty())
               throw new ApplicationException("no fixedRates or spreads given");

            Date refStart, start, refEnd, end;

            for (int i = 0; i < n; ++i)
            {
               refStart = start = schedule_.date(i);
               refEnd = end = schedule_.date(i + 1);
               Date paymentDate = paymentCalendar_.adjust(end, paymentAdjustment_);

               Date exCouponDate = null;
               if (exCouponPeriod_ != null)
               {
                  exCouponDate = exCouponCalendar_.advance(paymentDate,
                                                           -exCouponPeriod_,
                                                           exCouponAdjustment_,
                                                           exCouponEndOfMonth_);
               }

               if (i == 0 && !schedule_.isRegular(i + 1))
               {
                  BusinessDayConvention bdc = schedule_.businessDayConvention();
                  refStart = schedule_.calendar().adjust(end - schedule_.tenor(), bdc);
               }
               if (i == n - 1 && !schedule_.isRegular(i + 1))
               {
                  BusinessDayConvention bdc = schedule_.businessDayConvention();
                  refEnd = schedule_.calendar().adjust(start + schedule_.tenor(), bdc);
               }
               if (Utils.Get(fixedRates_, i, 1.0) == 0.0)
               {
                  // fixed coupon
                  leg.Add(new FixedRateCoupon(Utils.Get(notionals_, i, 0.0),
                                              paymentDate,
                                              Utils.effectiveFixedRate(spreads_, caps_, floors_, i),
                                              paymentDayCounter_, start, end, refStart, refEnd, exCouponDate));
               }
               else
               {
                  // zero inflation coupon
                  if (Utils.noOption(caps_, floors_, i))
                  {
                     // just swaplet
                     CPICoupon coup;

                     coup = new CPICoupon(baseCPI_,    // all have same base for ratio
                                          paymentDate,
                                          Utils.Get(notionals_, i, 0.0),
                                          start, end,
                                          Utils.Get(fixingDays_, i, 0),
                                          index_, observationLag_,
                                          observationInterpolation_,
                                          paymentDayCounter_,
                                          Utils.Get(fixedRates_, i, 0.0),
                                          Utils.Get(spreads_, i, 0.0),
                                          refStart, refEnd, exCouponDate);

                     // in this case you can set a pricer
                     // straight away because it only provides computation - not data
                     CPICouponPricer pricer = new CPICouponPricer();
                     coup.setPricer(pricer);
                     leg.Add(coup);

                  }
                  else
                  {
                     // cap/floorlet
                     throw new ApplicationException("caps/floors on CPI coupons not implemented.");
                  }
               }
            }
         }

         // in CPI legs you always have a notional flow of some sort
         Date pDate = paymentCalendar_.adjust(schedule_.date(n), paymentAdjustment_);
         Date fixingDate = pDate - observationLag_;
         CashFlow xnl = new CPICashFlow
                           (Utils.Get(notionals_, n, 0.0), index_,
                            new Date(), // is fake, i.e. you do not have one
                            baseCPI_, fixingDate, pDate,
                            subtractInflationNominal_, observationInterpolation_,
                            index_.frequency());

         leg.Add(xnl);

         return leg;
      }
   }
}
