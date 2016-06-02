/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2016  Andrea Maggiulli (a.maggiulli@gmail.com)
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
 
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

namespace QLNet
{
   //! base class for Inter-Bank-Offered-Rate indexes (e.g. %Libor, etc.)
   public class IborIndex : InterestRateIndex
   {
      public IborIndex( string familyName, 
                        Period tenor, 
                        int settlementDays, 
                        Currency currency,
                        Calendar fixingCalendar, 
                        BusinessDayConvention convention, 
                        bool endOfMonth,
                        DayCounter dayCounter, 
                        Handle<YieldTermStructure> h = null) 
         :base( familyName, tenor, settlementDays, currency, fixingCalendar, dayCounter )
      {
         
         convention_ = convention;
         termStructure_ = h ?? new Handle<YieldTermStructure>();
         endOfMonth_ = endOfMonth;

         // observer interface
         if ( !termStructure_.empty() )
            termStructure_.registerWith( update );
      }

      // InterestRateIndex interface
      public override Date maturityDate( Date valueDate )
      {
         return fixingCalendar().advance( valueDate, tenor_, convention_, endOfMonth_ );
      }
      public override double forecastFixing( Date fixingDate )
      {
         Date d1 = valueDate( fixingDate );
         Date d2 = maturityDate( d1 );
         double t = dayCounter_.yearFraction( d1, d2 );
         Utils.QL_REQUIRE( t > 0.0,()=>
                    "\n cannot calculate forward rate between " +
                    d1 + " and " + d2 +
                    ":\n non positive time (" + t +
                    ") using " + dayCounter_.name() + " daycounter" );
         return forecastFixing( d1, d2, t );
      }
      // Inspectors
      public BusinessDayConvention businessDayConvention() { return convention_; }
      public bool endOfMonth() { return endOfMonth_; }
      // the curve used to forecast fixings
      public Handle<YieldTermStructure> forwardingTermStructure() { return termStructure_; }
      // Other methods
      // returns a copy of itself linked to a different forwarding curve
      public virtual IborIndex clone( Handle<YieldTermStructure> forwarding )
      {
         return new IborIndex( familyName(), tenor(), fixingDays(), currency(), fixingCalendar(),
                              businessDayConvention(), endOfMonth(), dayCounter(), forwarding );
      }

      protected BusinessDayConvention convention_;
      protected Handle<YieldTermStructure> termStructure_;
      protected bool endOfMonth_;


      public double forecastFixing(Date d1,Date d2, double t)
      {
         Utils.QL_REQUIRE( !termStructure_.empty(),()=> "null term structure set to this instance of " + name() );
         double disc1 = termStructure_.link.discount( d1 );
         double disc2 = termStructure_.link.discount( d2 );
         return ( disc1 / disc2 - 1.0 ) / t;
      }


      // need by CashFlowVectors
      public IborIndex() { }
     
   }

   public class OvernightIndex : IborIndex
   {
      public OvernightIndex( string familyName,
                             int settlementDays,
                             Currency currency,
                             Calendar fixingCalendar,
                             DayCounter dayCounter,
                             Handle<YieldTermStructure> h = null) :

         base( familyName, new Period( 1, TimeUnit.Days ), settlementDays,
                     currency, fixingCalendar, BusinessDayConvention.Following, false, dayCounter, h )
      {}

      //! returns a copy of itself linked to a different forwarding curve
      public new IborIndex clone( Handle<YieldTermStructure> h )
      {
         return new OvernightIndex( familyName(), fixingDays(), currency(), fixingCalendar(),
                                   dayCounter(), h );

      }
   }
}
