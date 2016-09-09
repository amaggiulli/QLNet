/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
 Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com) 
 
 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

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
using System.Reflection;

namespace QLNet
{

   //! generic pricer for floating-rate coupons
   public abstract class FloatingRateCouponPricer : IObservable, IObserver
   {
      //! \name required interface
      //@{
      public abstract double swapletPrice();
      public abstract double swapletRate();
      public abstract double capletPrice( double effectiveCap );
      public abstract double capletRate( double effectiveCap );
      public abstract double floorletPrice( double effectiveFloor );
      public abstract double floorletRate( double effectiveFloor );
      public abstract void initialize( FloatingRateCoupon coupon );

      #region Observer & observable
      private readonly WeakEventSource eventSource = new WeakEventSource();
      public event Callback notifyObserversEvent
      {
         add { eventSource.Subscribe( value ); }
         remove { eventSource.Unsubscribe( value ); }
      }

      public void registerWith( Callback handler ) { notifyObserversEvent += handler; }
      public void unregisterWith( Callback handler ) { notifyObserversEvent -= handler; }
      protected void notifyObservers()
      {
         eventSource.Raise();
      }

      // observer interface
      public void update() { notifyObservers(); }
      #endregion
   }

   //! base pricer for capped/floored Ibor coupons
   public abstract class IborCouponPricer : FloatingRateCouponPricer
   {
      public IborCouponPricer( Handle<OptionletVolatilityStructure> v = null )
      {
         capletVol_ = v ?? new Handle<OptionletVolatilityStructure>();
         if ( !capletVol_.empty() )
            capletVol_.registerWith( update );
      }

      public Handle<OptionletVolatilityStructure> capletVolatility()
      {
         return capletVol_;
      }

      public void setCapletVolatility( Handle<OptionletVolatilityStructure> v = null)
      {
         capletVol_.unregisterWith( update );
         capletVol_ = v ?? new Handle<OptionletVolatilityStructure>();
         if ( !capletVol_.empty() )
            capletVol_.registerWith( update );

         update();
      }
      private Handle<OptionletVolatilityStructure> capletVol_;
   }

   /*! Black-formula pricer for capped/floored Ibor coupons
       References for timing adjustments
       Black76             Hull, Options, Futures and other
                           derivatives, 4th ed., page 550
       BivariateLognormal  http://ssrn.com/abstract=2170721
       The bivariate lognormal adjustment implementation is
       still considered experimental */
   public class BlackIborCouponPricer : IborCouponPricer
   {
      public enum TimingAdjustment { Black76, BivariateLognormal };
      public BlackIborCouponPricer( Handle<OptionletVolatilityStructure> v = null,
                                    TimingAdjustment timingAdjustment  = TimingAdjustment.Black76,
                                    Handle<Quote> correlation = null)
         : base( v )
      {
         timingAdjustment_ = timingAdjustment;
         correlation_ = correlation ?? new Handle<Quote>(new SimpleQuote(1.0));

         Utils.QL_REQUIRE( timingAdjustment_ == TimingAdjustment.Black76 ||
                           timingAdjustment_ == TimingAdjustment.BivariateLognormal,()=>
                       "unknown timing adjustment (code " + timingAdjustment_ + ")" );
         correlation_.registerWith(update);
      }


      public override void initialize( FloatingRateCoupon coupon )
      {
         gearing_ = coupon.gearing();
         spread_ = coupon.spread();
         accrualPeriod_ = coupon.accrualPeriod();
         Utils.QL_REQUIRE(accrualPeriod_ != 0.0,()=> "null accrual period");

         index_ = coupon.index() as IborIndex;
         if (index_ == null) 
         {
            // check if the coupon was right
            IborCoupon c = coupon as IborCoupon;
            Utils.QL_REQUIRE(c!=null,()=> "IborCoupon required");
            // coupon was right, index is not
            Utils.QL_FAIL("IborIndex required");
         }

         Handle<YieldTermStructure> rateCurve = index_.forwardingTermStructure();
         Date paymentDate = coupon.date();
         if ( paymentDate > rateCurve.link.referenceDate() )
            discount_ = rateCurve.link.discount( paymentDate );
         else
            discount_ = 1.0;

         spreadLegValue_ = spread_ * accrualPeriod_ * discount_;

         coupon_ = coupon ;
      }     
      public override double swapletPrice()
      {
         // past or future fixing is managed in InterestRateIndex::fixing()

         double swapletPrice = adjustedFixing() * accrualPeriod_ * discount_;
         return gearing_ * swapletPrice + spreadLegValue_;
      }
      public override double swapletRate()
      {
         return swapletPrice() / ( accrualPeriod_ * discount_ );
      }
      public override double capletPrice( double effectiveCap )
      {
         double capletPrice = optionletPrice( Option.Type.Call, effectiveCap );
         return gearing_ * capletPrice;
      }
      public override double capletRate( double effectiveCap )
      {
         return capletPrice( effectiveCap ) / ( accrualPeriod_ * discount_ );
      }
      public override double floorletPrice( double effectiveFloor )
      {
         double floorletPrice = optionletPrice( Option.Type.Put, effectiveFloor );
         return gearing_ * floorletPrice;
      }
      public override double floorletRate( double effectiveFloor )
      {
         return floorletPrice( effectiveFloor ) / ( accrualPeriod_ * discount_ );
      }

      protected double optionletPrice( Option.Type optionType, double effStrike )
      {
         Date fixingDate = coupon_.fixingDate();
         if ( fixingDate <= Settings.evaluationDate() )
         {
            // the amount is determined
            double a;
            double b;
            if ( optionType == Option.Type.Call )
            {
               a = coupon_.indexFixing();
               b = effStrike;
            }
            else
            {
               a = effStrike;
               b = coupon_.indexFixing();
            }
            return Math.Max( a - b, 0.0 ) * accrualPeriod_ * discount_;
         }
         else
         {
            // not yet determined, use Black model
            Utils.QL_REQUIRE( !capletVolatility().empty(), () => "missing optionlet volatility" );

            double stdDev = Math.Sqrt( capletVolatility().link.blackVariance( fixingDate, effStrike ) );
            double shift = capletVolatility().link.displacement();
            bool shiftedLn = capletVolatility().link.volatilityType() == VolatilityType.ShiftedLognormal;
            double fixing =
                shiftedLn
                    ? Utils.blackFormula(optionType, effStrike, adjustedFixing(),stdDev, 1.0, shift)
                    : Utils.bachelierBlackFormula(optionType, effStrike,adjustedFixing(), stdDev, 1.0);
            return fixing * accrualPeriod_ * discount_;
         }
      }
      protected virtual double adjustedFixing( double? fixing = null)
      {
         if (fixing == null)
            fixing = coupon_.indexFixing();

         if (!coupon_.isInArrears() && timingAdjustment_ == TimingAdjustment.Black76)
            return fixing.Value;

         Utils.QL_REQUIRE(!capletVolatility().empty(),()=> "missing optionlet volatility");
         Date d1 = coupon_.fixingDate();
         Date referenceDate = capletVolatility().link.referenceDate();
         if (d1 <= referenceDate)
            return fixing.Value;
         Date d2 = index_.valueDate(d1);
         Date d3 = index_.maturityDate(d2);
         double tau = index_.dayCounter().yearFraction(d2, d3);
         double variance = capletVolatility().link.blackVariance(d1, fixing.Value);

         double shift = capletVolatility().link.displacement();
         bool shiftedLn = capletVolatility().link.volatilityType() == VolatilityType.ShiftedLognormal;

         double adjustment = shiftedLn
            ? (fixing.Value + shift)*(fixing.Value + shift)*variance*tau/(1.0 + fixing.Value*tau)
            : variance*tau/(1.0 + fixing.Value*tau);

         if (timingAdjustment_ == TimingAdjustment.BivariateLognormal)
         {
            Utils.QL_REQUIRE(!correlation_.empty(),()=> "no correlation given");
            Date d4 = coupon_.date();
            Date d5 = d4 >= d3 ? d3 : d2;
            double tau2 = index_.dayCounter().yearFraction(d5, d4);
            if (d4 >= d3)
               adjustment = 0.0;
            // if d4 < d2 (payment before index start) we just apply the
            // Black76 in arrears adjustment
            if (tau2 > 0.0)
            {
               double fixing2 = (index_.forwardingTermStructure().link.discount(d5)/
                                 index_.forwardingTermStructure().link.discount(d4) -
                                 1.0)/ tau2;
               adjustment -= shiftedLn
                  ? correlation_.link.value()*tau2*variance* (fixing.Value + shift)*(fixing2 + shift)/ (1.0 + fixing2*tau2)
                  : correlation_.link.value()*tau2*variance/ (1.0 + fixing2*tau2);
            }
         }
         return fixing.Value + adjustment;
      }

      protected double gearing_;
      protected double spread_;
      protected double accrualPeriod_;
      protected IborIndex index_;
      protected double discount_;
      protected double spreadLegValue_;
      protected FloatingRateCoupon coupon_;
 
      private TimingAdjustment timingAdjustment_;
      private Handle<Quote> correlation_;


   }

   //! base pricer for vanilla CMS coupons
   public abstract class CmsCouponPricer : FloatingRateCouponPricer
   {
      public CmsCouponPricer( Handle<SwaptionVolatilityStructure> v = null )
      {
         swaptionVol_ = v ?? new Handle<SwaptionVolatilityStructure>();
         swaptionVol_.registerWith( update );
      }

      public Handle<SwaptionVolatilityStructure> swaptionVolatility() {return swaptionVol_;}

      public void setSwaptionVolatility( Handle<SwaptionVolatilityStructure> v = null)
      {
         swaptionVol_.unregisterWith( update );
         swaptionVol_ = v ?? new Handle<SwaptionVolatilityStructure>();
         swaptionVol_.registerWith( update );
         update();
      }
      private Handle<SwaptionVolatilityStructure> swaptionVol_;
   }

   /*! (CMS) coupon pricer that has a mean reversion parameter which can be
      used to calibrate to cms market quotes */
    public interface IMeanRevertingPricer 
    {
       double meanReversion() ;
       void setMeanReversion(Handle<Quote> q) ;
    }

   //===========================================================================//
   //                         CouponSelectorToSetPricer                         //
   //===========================================================================//

   public class PricerSetter : IAcyclicVisitor
   {
      private FloatingRateCouponPricer pricer_;
      public PricerSetter( FloatingRateCouponPricer pricer )
      {
         pricer_ = pricer;
      }

      public void visit( object o )
      {
         Type[] types = new Type[] { o.GetType() };
         MethodInfo methodInfo = Utils.GetMethodInfo( this, "visit", types );
         if ( methodInfo != null )
         {
            methodInfo.Invoke( this, new object[] { o } );
         }
      }

      public void visit( CashFlow c )
      {
         // nothing to do
      }
      public void visit( Coupon c )
      {
         // nothing to do
      }
      public void visit(FloatingRateCoupon c) 
      {
         c.setPricer(pricer_);
      }
      public void visit( CappedFlooredCoupon c )
      {
         c.setPricer( pricer_ );
      }
      public void visit( IborCoupon c )
      {
         IborCouponPricer iborCouponPricer = pricer_ as IborCouponPricer;
         Utils.QL_REQUIRE( iborCouponPricer!=null,()=> "pricer not compatible with Ibor coupon" );
         c.setPricer( iborCouponPricer );
      }
      public void visit( DigitalIborCoupon c )
      {
         IborCouponPricer iborCouponPricer = pricer_ as IborCouponPricer;
         Utils.QL_REQUIRE( iborCouponPricer!= null,()=> "pricer not compatible with Ibor coupon" );
         c.setPricer( iborCouponPricer );
      }
      public void visit( CappedFlooredIborCoupon c )
      {
         IborCouponPricer iborCouponPricer = pricer_ as IborCouponPricer;
         Utils.QL_REQUIRE( iborCouponPricer != null, () => "pricer not compatible with Ibor coupon" );
         c.setPricer( iborCouponPricer );
      }
      public void visit( CmsCoupon c )
      {
         CmsCouponPricer cmsCouponPricer = pricer_ as CmsCouponPricer;
         Utils.QL_REQUIRE( cmsCouponPricer!=null,()=> "pricer not compatible with CMS coupon" );
         c.setPricer( cmsCouponPricer );
      }
      //public void visit(CmsSpreadCoupon c) 
      //{
      //   CmsSpreadCouponPricer cmsSpreadCouponPricer = pricer_ as CmsSpreadCouponPricer;
      //   Utils.QL_REQUIRE(cmsSpreadCouponPricer!=null,()=>"pricer not compatible with CMS spread coupon");
      //   c.setPricer(cmsSpreadCouponPricer);
      //}
      public void visit( CappedFlooredCmsCoupon c )
      {
         CmsCouponPricer cmsCouponPricer = pricer_ as CmsCouponPricer;
         Utils.QL_REQUIRE( cmsCouponPricer != null, () => "pricer not compatible with CMS coupon" );
         c.setPricer( cmsCouponPricer );
      }
      //public void visit(CappedFlooredCmsSpreadCoupon c) 
      //{
      //   CmsSpreadCouponPricer cmsSpreadCouponPricer = pricer_ as CmsSpreadCouponPricer;
      //   QL_REQUIRE(cmsSpreadCouponPricer!=null,()=>"pricer not compatible with CMS spread coupon");
      //   c.setPricer(cmsSpreadCouponPricer);
      //}
      public void visit( DigitalCmsCoupon c )
      {
         CmsCouponPricer cmsCouponPricer = pricer_ as CmsCouponPricer;
         Utils.QL_REQUIRE( cmsCouponPricer != null, () => "pricer not compatible with CMS coupon" );
         c.setPricer( cmsCouponPricer );
      }
      //public void visit(DigitalCmsSpreadCoupon c) 
      //{
      //   CmsSpreadCouponPricer cmsSpreadCouponPricer = pricer_ as CmsSpreadCouponPricer;
      //   Utils.QL_REQUIRE(cmsSpreadCouponPricer!=null,()=> "pricer not compatible with CMS spread coupon");
      //   c.setPricer(cmsSpreadCouponPricer);
      //}
      public void visit(RangeAccrualFloatersCoupon c)
      {
         RangeAccrualPricer rangeAccrualPricer = pricer_ as RangeAccrualPricer;
         Utils.QL_REQUIRE(rangeAccrualPricer!=null,()=> "pricer not compatible with range-accrual coupon");
         c.setPricer(rangeAccrualPricer);
      }
      //public void visit(SubPeriodsCoupon c) 
      //{
      //   SubPeriodsPricer subPeriodsPricer = pricer_ as SubPeriodsPricer;
      //   Utils.QL_REQUIRE(subPeriodsPricer!=null,()=> "pricer not compatible with sub-period coupon");
      //   c.setPricer(subPeriodsPricer);
      //}
   }

   partial class Utils
   {
      public static void setCouponPricer( List<CashFlow> leg, FloatingRateCouponPricer pricer )
      {
         PricerSetter setter = new PricerSetter( pricer );
         foreach ( CashFlow cf in leg )
         {
            cf.accept( setter );
         }
      }

      public static void setCouponPricers( List<CashFlow> leg, List<FloatingRateCouponPricer> pricers )
      {
         int nCashFlows = leg.Count;
         Utils.QL_REQUIRE(nCashFlows>0,()=> "no cashflows");

         int nPricers = pricers.Count;
         Utils.QL_REQUIRE(nCashFlows >= nPricers,()=>
                   "mismatch between leg size (" + nCashFlows +
                   ") and number of pricers (" + nPricers + ")");

         for (int i = 0; i < nCashFlows; ++i)
         {
             PricerSetter setter = new PricerSetter(i < nPricers ? pricers[i] : pricers[nPricers - 1]);
             leg[i].accept(setter);
         }
      }
   }
}
