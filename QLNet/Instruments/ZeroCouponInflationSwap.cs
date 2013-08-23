/*
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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QLNet
{
   public class ZeroCouponInflationSwap : Swap
   {
      public enum Type { Receiver = -1, Payer = 1 };

      public ZeroCouponInflationSwap(Type type,
                                     double nominal,
                                     Date startDate,   // start date of contract (only)
                                     Date maturity,    // this is pre-adjustment!
                                     Calendar fixCalendar,
                                     BusinessDayConvention fixConvention,
                                     DayCounter dayCounter,
                                     double fixedRate,
                                     ZeroInflationIndex infIndex,
                                     Period observationLag)
         :this(type,nominal,startDate,maturity,fixCalendar,fixConvention,dayCounter,fixedRate,
               infIndex,observationLag,false,new Calendar(),new BusinessDayConvention()) {}

      public ZeroCouponInflationSwap(Type type,
                                     double nominal,
                                     Date startDate,   // start date of contract (only)
                                     Date maturity,    // this is pre-adjustment!
                                     Calendar fixCalendar,
                                     BusinessDayConvention fixConvention,
                                     DayCounter dayCounter,
                                     double fixedRate,
                                     ZeroInflationIndex infIndex,
                                     Period observationLag,
                                     bool adjustInfObsDates)
         :this(type,nominal,startDate,maturity,fixCalendar,fixConvention,dayCounter,fixedRate,
               infIndex,observationLag,adjustInfObsDates,null,new BusinessDayConvention()) {}

      public ZeroCouponInflationSwap(Type type,
                                     double nominal,
                                     Date startDate,   // start date of contract (only)
                                     Date maturity,    // this is pre-adjustment!
                                     Calendar fixCalendar,
                                     BusinessDayConvention fixConvention,
                                     DayCounter dayCounter,
                                     double fixedRate,
                                     ZeroInflationIndex infIndex,
                                     Period observationLag,
                                     bool adjustInfObsDates,
                                     Calendar infCalendar)
         :this(type,nominal,startDate,maturity,fixCalendar,fixConvention,dayCounter,fixedRate,
               infIndex,observationLag,adjustInfObsDates,infCalendar,new BusinessDayConvention()) {}

      public ZeroCouponInflationSwap(Type type,
                                     double nominal,
                                     Date startDate,   // start date of contract (only)
                                     Date maturity,    // this is pre-adjustment!
                                     Calendar fixCalendar,
                                     BusinessDayConvention fixConvention,
                                     DayCounter dayCounter,
                                     double fixedRate,
                                     ZeroInflationIndex infIndex,
                                     Period observationLag,
                                     bool adjustInfObsDates,
                                     Calendar infCalendar,
                                     BusinessDayConvention infConvention)
         :base(2)
      {
         type_ = type;
         nominal_ = nominal;
         fixedRate_ = fixedRate;
         infIndex_ = infIndex;
         observationLag_ = observationLag;
         dayCounter_ = dayCounter;

         // first check compatibility of index and swap definitions
         if (infIndex_.interpolated()) 
         {
            Period pShift = new Period(infIndex_.frequency());
            if ((observationLag_ - pShift) <= infIndex_.availabilityLag())
               throw new ApplicationException(
                       "inconsistency between swap observation of index " + observationLag_ +
                       " index availability " + infIndex_.availabilityLag() +
                       " interpolated index period " + pShift +
                       " and index availability " + infIndex_.availabilityLag() +
                       " need (obsLag-index period) > availLag");
         } 
         else 
         {
            if (infIndex_.availabilityLag() >= observationLag_)
               throw new  ApplicationException(
                       "index tries to observe inflation fixings that do not yet exist: "
                       + " availability lag " + infIndex_.availabilityLag()
                       + " versus obs lag = " + observationLag_);
         }

         if (infCalendar == null) infCalendar = fixCalendar;
         if (infConvention == new BusinessDayConvention()) infConvention = fixConvention;

         if (adjustInfObsDates) 
         {
            baseDate_ = infCalendar.adjust(startDate - observationLag_, infConvention);
            obsDate_ = infCalendar.adjust(maturity - observationLag_, infConvention);
         } 
         else 
         {
            baseDate_ = startDate - observationLag_;
            obsDate_ = maturity - observationLag_;
         }

         Date infPayDate = infCalendar.adjust(maturity, infConvention);
         Date fixedPayDate = fixCalendar.adjust(maturity, fixConvention);

         // At this point the index may not be able to forecast
         // i.e. do not want to force the existence of an inflation
         // term structure before allowing users to create instruments.
         double T = Utils.inflationYearFraction(infIndex_.frequency(), infIndex_.interpolated(),
                                       dayCounter_, baseDate_, obsDate_);
         // N.B. the -1.0 is because swaps only exchange growth, not notionals as well
         double fixedAmount = nominal * ( Math.Pow(1.0 + fixedRate, T) - 1.0 );

         legs_[0].Add(new SimpleCashFlow(fixedAmount, fixedPayDate));
         bool growthOnly = true;
         legs_[1].Add(new IndexedCashFlow(nominal,infIndex,baseDate_,obsDate_,infPayDate,growthOnly));

         for (int j=0; j<2; ++j) 
         {
            for (int i = 0; i < legs_[j].Count; i++)
               legs_[j][i].registerWith(update);
         }

        switch (type_) {
            case Type.Payer:
                payer_[0] = +1.0;
                payer_[1] = -1.0;
                break;
            case Type.Receiver:
                payer_[0] = -1.0;
                payer_[1] = +1.0;
                break;
            default:
                throw new ApplicationException("Unknown zero-inflation-swap type");

        }
      }


      //! \name Inspectors
      //@{
      //! "payer" or "receiver" refer to the inflation-indexed leg
      public Type type() {return type_; }
      public double nominal() { return nominal_; }
      //! \f$ K \f$ in the above formula.
      public double fixedRate() { return fixedRate_; }
      public ZeroInflationIndex inflationIndex() { return infIndex_; }
      public Period observationLag() { return observationLag_; }
      public DayCounter dayCounter() { return dayCounter_; }
      //! just one cashflow (that is not a coupon) in each leg
      //public Leg  fixedLeg() {}
      //! just one cashflow (that is not a coupon) in each leg
      //public Leg inflationLeg() { }
      //@}

      //! \name Instrument interface
      //@{
      public override void setupArguments(IPricingEngineArguments args)
      {
         // you don't actually need to do anything else because it is so simple
         base.setupArguments(args);
      }
      public override void fetchResults(IPricingEngineResults r)
      {
         // you don't actually need to do anything else because it is so simple
         base.fetchResults(r);
      }
      //@}


      //! \name Results
      //@{
      public double fixedLegNPV() 
      {
         calculate();
         if ( legNPV_[0] == null )
            throw new ApplicationException("result not available");
         return legNPV_[0].Value;
      }
      public double inflationLegNPV() 
      {
         calculate();
         if ( legNPV_[0] == null )
            throw new ApplicationException("result not available");
        return legNPV_[1].Value;
      }
      public double fairRate()
      {

         // What does this mean before or after trade date?
         // Always means that NPV is zero for _this_ instrument
         // if it was created with _this_ rate
         // _knowing_ the time from base to obs (etc).

         IndexedCashFlow icf = legs_[1][0] as IndexedCashFlow;
         if ( icf == null )
            throw new ApplicationException("failed to downcast to IndexedCashFlow in ::fairRate()");

         // +1 because the IndexedCashFlow has growthOnly=true
         double growth = icf.amount() / icf.notional() + 1.0;
         double T = Utils.inflationYearFraction(infIndex_.frequency(),
                                                infIndex_.interpolated(),
                                                dayCounter_, baseDate_, obsDate_);

         return Math.Pow(growth,1.0/T) - 1.0;

         // we cannot use this simple definition because
         // it does not work for already-issued instruments
         // return infIndex_->zeroInflationTermStructure()->zeroRate(
         //      maturityDate(), observationLag(), infIndex_->interpolated());
      }
      //@}

      protected Type type_;
      protected double nominal_;
      protected double fixedRate_;
      protected ZeroInflationIndex infIndex_;
      protected Period observationLag_;
      protected DayCounter dayCounter_;
      protected Date baseDate_, obsDate_;

   }

   public class Arguments : Swap.Arguments
   {
      double fixedRate;
      public override void validate()
      {
        // you don't actually need to do anything else because it is so simple
         base.validate();
      }
   }

   public class engine : GenericEngine<ZeroCouponInflationSwap.Arguments,
                                       ZeroCouponInflationSwap.Results> {};

}
