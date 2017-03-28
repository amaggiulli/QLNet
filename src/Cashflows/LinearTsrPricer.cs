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
using System;
using System.Linq;

namespace QLNet
{
   //! CMS-coupon pricer
   /*! Prices a cms coupon using a linear terminal swap rate model
      The slope parameter is linked to a gaussian short rate model.
      Reference: Andersen, Piterbarg, Interest Rate Modeling, 16.3.2

      The cut off point for integration can be set
      - by explicitly specifying the lower and upper bound
      - by defining the lower and upper bound to be the strike where
         a vanilla swaption has 1% or less vega of the atm swaption
      - by defining the lower and upper bound to be the strike where
         undeflated (!) payer resp. receiver prices are below a given
         threshold
      - by specificying a number of standard deviations to cover
         using a Black Scholes process with an atm volatility as
         a benchmark
      In every case the lower and upper bound are applied though.
      In case the smile section is shifted lognormal, the specified
      lower and upper bound are applied to strike + shift so that
      e.g. a zero lower bound always refers to the lower bound of
      the rates in the shifted lognormal model.
      Note that for normal volatility input the lower rate bound
      should probably be adjusted to an appropriate negative value,
      there is no automatic adjustment in this case.
   */
   public class LinearTsrPricer : CmsCouponPricer, IMeanRevertingPricer
   {
      public class Settings 
      {
         public Settings()
         {
            strategy_ = Strategy.RateBound;
            vegaRatio_ = 0.01;
            priceThreshold_ = 1.0E-8;
            stdDevs_ = 3.0;
            lowerRateBound_ = 0.0001;
            upperRateBound_ = 2.0000;
         }

         public Settings withRateBound( double lowerRateBound = 0.0001, double upperRateBound = 2.0000 ) 
         {
            strategy_ = Strategy.RateBound;
            lowerRateBound_ = lowerRateBound;
            upperRateBound_ = upperRateBound;
            return this;
         }

         public Settings withVegaRatio( double vegaRatio = 0.01, double lowerRateBound = 0.0001, double upperRateBound = 2.0000 ) 
         {
            strategy_ = Strategy.VegaRatio;
            vegaRatio_ = vegaRatio;
            lowerRateBound_ = lowerRateBound;
            upperRateBound_ = upperRateBound;
            return this;
         }

         public Settings withPriceThreshold( double priceThreshold = 1.0E-8, double lowerRateBound = 0.0001,
            double upperRateBound = 2.0000) 
         {
            strategy_ = Strategy.PriceThreshold;
            priceThreshold_ = priceThreshold;
            lowerRateBound_ = lowerRateBound;
            upperRateBound_ = upperRateBound;
            return this;
         }

         public Settings withBSStdDevs( double stdDevs = 3.0,
                                double lowerRateBound = 0.0001,
                                double upperRateBound = 2.0000) 
         {
            strategy_ = Strategy.BSStdDevs;
            stdDevs_ = stdDevs;
            lowerRateBound_ = lowerRateBound;
            upperRateBound_ = upperRateBound;
            return this;
         }

         public enum Strategy
         {
                RateBound,
                VegaRatio,
                PriceThreshold,
                BSStdDevs
         }

         public Strategy strategy_;
         public double vegaRatio_;
         public double priceThreshold_;
         public double stdDevs_;
         public double lowerRateBound_, upperRateBound_;
        
      }

      public LinearTsrPricer( Handle<SwaptionVolatilityStructure> swaptionVol,
                              Handle<Quote> meanReversion,
                              Handle<YieldTermStructure> couponDiscountCurve = null,
                              Settings settings = null,
                              Integrator integrator = null)
         :base(swaptionVol)
      {
         meanReversion_ = meanReversion;
         couponDiscountCurve_ = couponDiscountCurve ?? new Handle<YieldTermStructure>();
         settings_ = settings ?? new Settings();
         volDayCounter_ = swaptionVol.link.dayCounter();
         integrator_ = integrator;

          if (!couponDiscountCurve_.empty())
            couponDiscountCurve_.registerWith(update);

        if (integrator_ == null)
            integrator_ = new  GaussKronrodNonAdaptive(1E-10, 5000, 1E-10);
      }

      /* */
      public override double swapletPrice()
      {
         if (fixingDate_ <= today_) 
         {
            // the fixing is determined
            double Rs = coupon_.swapIndex().fixing(fixingDate_);
            double price =
                (gearing_ * Rs + spread_) *
                (coupon_.accrualPeriod() *
                 discountCurve_.link.discount(paymentDate_) * couponDiscountRatio_);
            return price;
         } 
         else 
         {
            double atmCapletPrice = optionletPrice(Option.Type.Call, swapRateValue_);
            double atmFloorletPrice = optionletPrice(Option.Type.Put, swapRateValue_);
            return gearing_ * (coupon_.accrualPeriod() *
                                   discountCurve_.link.discount(paymentDate_) *
                                   swapRateValue_ * couponDiscountRatio_ +
                                   atmCapletPrice - atmFloorletPrice) +
                                   spreadLegValue_;
         }
      }
      public override double swapletRate()
      {
         return swapletPrice() /
               ( coupon_.accrualPeriod() * discountCurve_.link.discount( paymentDate_ ) * couponDiscountRatio_ );
      }
      public override double capletPrice(double effectiveCap)
      {
         // caplet is equivalent to call option on fixing
         if (fixingDate_ <= today_) 
         {
            // the fixing is determined
            double Rs = Math.Max(coupon_.swapIndex().fixing(fixingDate_) - effectiveCap, 0.0);
            double price =
                (gearing_ * Rs) *
                (coupon_.accrualPeriod() *
                 discountCurve_.link.discount(paymentDate_) * couponDiscountRatio_);
            return price;
         } 
         else 
         {
            double capletPrice = optionletPrice(Option.Type.Call, effectiveCap);
            return gearing_ * capletPrice;
         }
      }
      public override double capletRate(double effectiveCap)
      {
         return capletPrice( effectiveCap ) /
               ( coupon_.accrualPeriod() *
                discountCurve_.link.discount( paymentDate_ ) * couponDiscountRatio_ );
      }
      public override double floorletPrice(double effectiveFloor)
      {
         // floorlet is equivalent to put option on fixing
         if (fixingDate_ <= today_) 
         {
            // the fixing is determined
            double Rs = Math.Max(effectiveFloor - coupon_.swapIndex().fixing(fixingDate_), 0.0);
            double price =
                (gearing_ * Rs) *
                (coupon_.accrualPeriod() *
                 discountCurve_.link.discount(paymentDate_) * couponDiscountRatio_);
            return price;
         } 
         else 
         {
            double floorletPrice = optionletPrice(Option.Type.Put, effectiveFloor);
            return gearing_ * floorletPrice;
         }
      }
      public override double floorletRate(double effectiveFloor)
      {
         return floorletPrice( effectiveFloor ) /
               ( coupon_.accrualPeriod() *
                discountCurve_.link.discount( paymentDate_ ) * couponDiscountRatio_ );
      }
      /* */
      public double meanReversion() { return meanReversion_.link.value(); }
      public void setMeanReversion( Handle<Quote> meanReversion) 
      {
         meanReversion_.unregisterWith(update);
         meanReversion_ = meanReversion;
         meanReversion_.registerWith(update);
         update();
      }


      private double GsrG( Date d)
      {
         double yf = volDayCounter_.yearFraction(fixingDate_, d);
         if (Math.Abs(meanReversion_.link.value()) < 1.0E-4)
            return yf;
         else
            return (1.0 - Math.Exp(-meanReversion_.link.value() * yf)) /
                                    meanReversion_.link.value();
      }
      private double singularTerms( Option.Type type, double strike)
      {
         double omega = (type == Option.Type.Call ? 1.0 : -1.0);
         double s1 = Math.Max(omega * (swapRateValue_ - strike), 0.0) *
                  (a_ * swapRateValue_ + b_);
         double s2 = (a_ * strike + b_) *
                     smileSection_.optionPrice(strike, strike < swapRateValue_ ? Option.Type.Put : Option.Type.Call);
        return s1 + s2;
      }
      private double integrand(double strike)
      {
          return 2.0 * a_ * smileSection_.optionPrice( strike, strike < swapRateValue_ ? Option.Type.Put : Option.Type.Call);
      }
      private double a_, b_;

      private class VegaRatioHelper : ISolver1d
      {
         public VegaRatioHelper( SmileSection section,  double targetVega)
         {
            section_ = section;
            targetVega_ = targetVega;
         }
         
         public override double value(double strike) 
         {
            return section_.vega(strike) - targetVega_;
         }
         
         SmileSection section_;
         double targetVega_;
      }

      private class PriceHelper  : ISolver1d
      {
         public PriceHelper( SmileSection section, Option.Type type,double targetPrice)
         {
            section_ = section;
            targetPrice_ = targetPrice;
            type_ = type;
         }
            
         public override double value(double strike) 
         {
            return section_.optionPrice(strike, type_) - targetPrice_;
         }

         private SmileSection section_;
         private double targetPrice_;
         private Option.Type type_;
      }

      public override void initialize( FloatingRateCoupon coupon)
      {
         coupon_ = coupon as CmsCoupon;
         Utils.QL_REQUIRE(coupon_ != null,()=> "CMS coupon needed");
         gearing_ = coupon_.gearing();
         spread_ = coupon_.spread();

         fixingDate_ = coupon_.fixingDate();
         paymentDate_ = coupon_.date();
         swapIndex_ = coupon_.swapIndex();

         forwardCurve_ = swapIndex_.forwardingTermStructure();
         if (swapIndex_.exogenousDiscount())
            discountCurve_ = swapIndex_.discountingTermStructure();
         else
            discountCurve_ = forwardCurve_;

         // if no coupon discount curve is given just use the discounting curve
         // from the swap index. for rate calculation this curve cancels out in
         // the computation, so e.g. the discounting swap engine will produce
         // correct results, even if the couponDiscountCurve is not set here.
         // only the price member function in this class will be dependent on the
         // coupon discount curve.

         today_ = QLNet.Settings.evaluationDate();

         if (paymentDate_ > today_ && !couponDiscountCurve_.empty())
            couponDiscountRatio_ = couponDiscountCurve_.link.discount(paymentDate_) /
                                   discountCurve_.link.discount(paymentDate_);
         else
            couponDiscountRatio_ = 1.0;

         spreadLegValue_ = spread_ * coupon_.accrualPeriod() *
                           discountCurve_.link.discount(paymentDate_) *
                           couponDiscountRatio_;

         if (fixingDate_ > today_) 
         {
            swapTenor_ = swapIndex_.tenor();
            swap_ = swapIndex_.underlyingSwap(fixingDate_);

            swapRateValue_ = swap_.fairRate();
            annuity_ = 1.0E4 * Math.Abs(swap_.fixedLegBPS());

            SmileSection sectionTmp = swaptionVolatility().link.smileSection(fixingDate_, swapTenor_);

            // adjust bounds by section's shift
            shiftedLowerBound_ = settings_.lowerRateBound_ - sectionTmp.shift();
            shiftedUpperBound_ = settings_.upperRateBound_ - sectionTmp.shift();

            // if the section does not provide an atm level, we enhance it to
            // have one, no need to exit with an exception ...

            if (sectionTmp.atmLevel() ==null)
                  smileSection_ = new AtmSmileSection(sectionTmp, swapRateValue_);
            else
                  smileSection_ = sectionTmp;

            // compute linear model's parameters

            double gx = 0.0, gy = 0.0;
            for (int i = 0; i < swap_.fixedLeg().Count; i++) 
            {
               Coupon c = swap_.fixedLeg()[i] as Coupon;
               double yf = c.accrualPeriod();
               Date d = c.date();
               double pv = yf * discountCurve_.link.discount(d);
               gx += pv * GsrG(d);
               gy += pv;
            }

            double gamma = gx / gy;
            Date lastd = swap_.fixedLeg().Last().date();

            a_ = discountCurve_.link.discount(paymentDate_) *
                  (gamma - GsrG(paymentDate_)) /
                  (discountCurve_.link.discount(lastd) * GsrG(lastd) +
                  swapRateValue_ * gy * gamma);

            b_ = discountCurve_.link.discount(paymentDate_) / gy -
                  a_ * swapRateValue_;
        }

      }

      private double optionletPrice(Option.Type optionType, double strike)
      {
         if (optionType == Option.Type.Call && strike >= shiftedUpperBound_)
            return 0.0;
         if (optionType == Option.Type.Put && strike <= shiftedLowerBound_)
            return 0.0;

         // determine lower or upper integration bound (depending on option type)

         double lower = strike, upper = strike;

         switch (settings_.strategy_) 
         {

            case Settings.Strategy.RateBound: 
            {
               if (optionType == Option.Type.Call)
                  upper = shiftedUpperBound_;
               else
                  lower = shiftedLowerBound_;
               break;
            }

            case Settings.Strategy.VegaRatio: 
            {
               // strikeFromVegaRatio ensures that returned strike is on the
               // expected side of strike
               double bound = strikeFromVegaRatio(settings_.vegaRatio_, optionType, strike);
               if (optionType == Option.Type.Call)
                  upper = Math.Min(bound, shiftedUpperBound_);
               else
                  lower = Math.Max(bound, shiftedLowerBound_);
               break;
            }

            case Settings.Strategy.PriceThreshold: 
            {
               // strikeFromPrice ensures that returned strike is on the expected
               // side of strike
               double bound = strikeFromPrice(settings_.vegaRatio_, optionType, strike);
               if (optionType == Option.Type.Call)
                  upper = Math.Min(bound, shiftedUpperBound_);
               else
                  lower = Math.Max(bound, shiftedLowerBound_);
               break;
            }

            case Settings.Strategy.BSStdDevs : 
            {
               double? atm = smileSection_.atmLevel();
               double atmVol = smileSection_.volatility(atm.GetValueOrDefault());
               double shift = smileSection_.shift();
               double lowerTmp, upperTmp;
               if (smileSection_.volatilityType() == VolatilityType.ShiftedLognormal) 
               {
                  upperTmp = (atm.GetValueOrDefault() + shift) *
                              Math.Exp(settings_.stdDevs_ * atmVol -
                                        0.5 * atmVol * atmVol *
                                            smileSection_.exerciseTime()) - shift;
                  lowerTmp = (atm.GetValueOrDefault() + shift) *
                               Math.Exp(-settings_.stdDevs_ * atmVol -
                                        0.5 * atmVol * atmVol *
                                            smileSection_.exerciseTime()) - shift;
               } 
               else 
               {
                  double tmp = settings_.stdDevs_ * atmVol * Math.Sqrt(smileSection_.exerciseTime());
                  upperTmp = atm.GetValueOrDefault() + tmp;
                  lowerTmp = atm.GetValueOrDefault() - tmp;
               }
               upper = Math.Min(upperTmp - shift, shiftedUpperBound_);
               lower = Math.Max(lowerTmp - shift, shiftedLowerBound_);
               break;
            }

            default:
               Utils.QL_FAIL("Unknown strategy (" + settings_.strategy_ + ")");
               break;
         }

         // compute the relevant integral

         double result = 0.0;
         double tmpBound;
         if (upper > lower) 
         {
            tmpBound = Math.Min(upper, swapRateValue_);
            if (tmpBound > lower) 
            {
               result += integrator_.value( integrand,lower, tmpBound);
            }
            tmpBound = Math.Max(lower, swapRateValue_);
            if (upper > tmpBound) 
            {
                result += integrator_.value(integrand,tmpBound, upper);
            }
            result *= (optionType == Option.Type.Call ? 1.0 : -1.0);
         }

         result += singularTerms(optionType, strike);

         return annuity_ * result * couponDiscountRatio_ * coupon_.accrualPeriod();
      }

      private double strikeFromVegaRatio(double ratio, Option.Type optionType,double referenceStrike)
      {
         double a, b, min, max, k;
         if (optionType == Option.Type.Call) 
         {
            a = swapRateValue_;
            min = referenceStrike;
            b = max = k = Math.Min(smileSection_.maxStrike(), shiftedUpperBound_);
         } 
         else 
         {
            a = min = k = Math.Max(smileSection_.minStrike(), shiftedLowerBound_);
            b = swapRateValue_;
            max = referenceStrike;
         }

         VegaRatioHelper h = new VegaRatioHelper(smileSection_,smileSection_.vega(swapRateValue_) * ratio);
         Brent solver = new Brent();

         try 
         {
            k = solver.solve(h, 1.0E-5, (a + b) / 2.0, a, b);
         }
         catch (Exception) 
         {
            // use default value set above
         }

         return Math.Min(Math.Max(k, min), max);
      }

      private double strikeFromPrice(double price, Option.Type optionType,double referenceStrike)
      {
         double a, b, min, max, k;
         if (optionType == Option.Type.Call) 
         {
            a = swapRateValue_;
            min = referenceStrike;
            b = max = k = Math.Min(smileSection_.maxStrike(), shiftedUpperBound_);
         } 
         else 
         {
            a = min = k = Math.Max(smileSection_.minStrike(), shiftedLowerBound_);
            b = swapRateValue_;
            max = referenceStrike;
         }

         PriceHelper h = new PriceHelper(smileSection_, optionType, price);
         Brent solver = new Brent();

         try 
         {
            k = solver.solve(h, 1.0E-5, swapRateValue_, a, b);
         }
         catch (Exception) 
         {
            // use default value set above
         }

        return Math.Min(Math.Max(k, min), max);
   
      }

      private   Handle<Quote> meanReversion_;
      private   Handle<YieldTermStructure> forwardCurve_, discountCurve_;
      private   Handle<YieldTermStructure> couponDiscountCurve_;
      private   CmsCoupon coupon_;
      private   Date today_, paymentDate_, fixingDate_;
      private   double gearing_, spread_;

      private   Period swapTenor_;
      private   double spreadLegValue_, swapRateValue_, couponDiscountRatio_, annuity_;

      private   SwapIndex swapIndex_;
      private   VanillaSwap swap_;
      private   SmileSection smileSection_;

      private   Settings settings_;
      private   DayCounter volDayCounter_;
      private   Integrator integrator_;

      private   double shiftedLowerBound_, shiftedUpperBound_;

   }
}
