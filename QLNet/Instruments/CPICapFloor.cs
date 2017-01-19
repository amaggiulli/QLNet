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

namespace QLNet
{
   //! CPI cap or floor
   /*! Quoted as a fixed strike rate \f$ K \f$.  Payoff:
    \f[
    P_n(0,T) \max(y (N [(1+K)^{T}-1] -
                N \left[ \frac{I(T)}{I(0)} -1 \right]), 0)
    \f]
    where \f$ T \f$ is the maturity time, \f$ P_n(0,t) \f$ is the
    nominal discount factor at time \f$ t \f$, \f$ N \f$ is the
    notional, and \f$ I(t) \f$ is the inflation index value at
    time \f$ t \f$.

    Inflation is generally available on every day, including
    holidays and weekends.  Hence there is a variable to state
    whether the observe/fix dates for inflation are adjusted or
    not.  The default is not to adjust.

    N.B. a cpi cap or floor is an option, not a cap or floor on a coupon.
    Thus this is very similar to a ZCIIS and has a single flow, this is
    as usual for cpi because it is cumulative up to option maturity from base
    date.

    We do not inherit from Option, although this would be reasonable,
    because we do not have that degree of generality.

   */
   public class CPICapFloor : Instrument
   {
      public class Arguments : IPricingEngineArguments
      {
         public Option.Type type;
         public double nominal;
         public Date startDate, fixDate, payDate;
         public double baseCPI;
         public Date maturity;
         public Calendar fixCalendar, payCalendar;
         public BusinessDayConvention fixConvention, payConvention;
         public double strike;
         public Handle<ZeroInflationIndex> infIndex;
         public Period observationLag;
         public InterpolationType observationInterpolation;

         public void validate() {}
      }

      public new class Results : Instrument.Results 
      {
         public override void reset() { base.reset();}
      }

      public class Engine : GenericEngine<CPICapFloor.Arguments,CPICapFloor.Results> {}

      public CPICapFloor(Option.Type type,
                         double nominal,
                         Date startDate,   // start date of contract (only)
                         double baseCPI,
                         Date maturity,    // this is pre-adjustment!
                         Calendar fixCalendar,
                         BusinessDayConvention fixConvention,
                         Calendar payCalendar,
                         BusinessDayConvention payConvention,
                         double strike,
                         Handle<ZeroInflationIndex> infIndex,
                         Period observationLag,
                         InterpolationType observationInterpolation = InterpolationType.AsIndex)
      {
         type_ = type; 
         nominal_ = nominal; 
         startDate_ = startDate; 
         baseCPI_ = baseCPI;
         maturity_ = maturity; 
         fixCalendar_ = fixCalendar; 
         fixConvention_ = fixConvention;
         payCalendar_ = payCalendar; 
         payConvention_ = payConvention;
         strike_ = strike; 
         infIndex_ = infIndex; 
         observationLag_ = observationLag;
         observationInterpolation_ = observationInterpolation;

         Utils.QL_REQUIRE(fixCalendar_ != null, ()=> "CPICapFloor: fixing calendar may not be null.");
         Utils.QL_REQUIRE(payCalendar_ != null, ()=> "CPICapFloor: payment calendar may not be null.");

         if (observationInterpolation_ == InterpolationType.Flat  ||
             observationInterpolation_ == InterpolationType.AsIndex && !infIndex_.link.interpolated()) 
         {
            Utils.QL_REQUIRE(observationLag_ >= infIndex_.link.availabilityLag(),()=>
                       "CPIcapfloor's observationLag must be at least availabilityLag of inflation index: "
                       +"when the observation is effectively flat"
                       + observationLag_ + " vs " + infIndex_.link.availabilityLag());
         }
         if (observationInterpolation_ == InterpolationType.Linear ||
            (observationInterpolation_ == InterpolationType.AsIndex && infIndex_.link.interpolated())) 
         {
            Utils.QL_REQUIRE(observationLag_ > infIndex_.link.availabilityLag(),()=>
                       "CPIcapfloor's observationLag must be greater then availabilityLag of inflation index: "
                       +"when the observation is effectively linear"
                       + observationLag_ + " vs " + infIndex_.link.availabilityLag());
         }
      }

      //! \name Inspectors
      //@{
      public Option.Type type()  { return type_; }
      public double nominal() { return nominal_; }
      //! \f$ K \f$ in the above formula.
      public double strike() { return strike_; }
      //! when you fix - but remember that there is an observation interpolation factor as well
      public Date fixingDate() { return fixCalendar_.adjust( maturity_ - observationLag_, fixConvention_ ); }
      public Date payDate() { return payCalendar_.adjust( maturity_, payConvention_ ); }
      public Handle<ZeroInflationIndex> inflationIndex() { return infIndex_; }
      public Period observationLag() { return observationLag_; }
      //@}

      //! \name Instrument interface
      //@{
      public override bool isExpired() {return (Settings.evaluationDate() > maturity_);}
      public override void setupArguments(IPricingEngineArguments args)
      {
         // correct PricingEngine?
         CPICapFloor.Arguments arguments = args as CPICapFloor.Arguments;
         Utils.QL_REQUIRE( arguments != null,()=> "wrong argument type, not CPICapFloor.Arguments" );

         // data move
         arguments.type = type_;
         arguments.nominal = nominal_;
         arguments.startDate = startDate_;
         arguments.baseCPI = baseCPI_;
         arguments.maturity = maturity_;
         arguments.fixCalendar = fixCalendar_;
         arguments.fixConvention = fixConvention_;
         arguments.payCalendar = fixCalendar_;
         arguments.payConvention = payConvention_;
         arguments.fixDate = fixingDate();
         arguments.payDate = payDate();
         arguments.strike = strike_;
         arguments.infIndex = infIndex_;
         arguments.observationLag = observationLag_;
         arguments.observationInterpolation = observationInterpolation_;

      }

      //@}

      protected Option.Type type_;
      protected double nominal_;
      protected Date startDate_, fixDate_, payDate_;
      protected double baseCPI_;
      protected Date maturity_;
      protected Calendar fixCalendar_;
      protected BusinessDayConvention fixConvention_;
      protected Calendar payCalendar_;
      protected BusinessDayConvention payConvention_;
      protected double strike_;
      protected Handle<ZeroInflationIndex> infIndex_;
      protected Period observationLag_;
      protected InterpolationType observationInterpolation_;
   }


}
