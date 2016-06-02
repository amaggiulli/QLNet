/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2016  Andrea Maggiulli (a.maggiulli@gmail.com) 
  
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
   //! Bond Market Association index
   /*! The BMA index is the short-term tax-exempt reference index of
       the Bond Market Association.  It has tenor one week, is fixed
       weekly on Wednesdays and is applied with a one-day's fixing
       gap from Thursdays on for one week.  It is the tax-exempt
       correspondent of the 1M USD-Libor.
   */
   public class BMAIndex : InterestRateIndex
   {
      public BMAIndex( Handle<YieldTermStructure> h = null)
         : base( "BMA", new Period( 1, TimeUnit.Weeks ), 1, new USDCurrency(),
                new UnitedStates( UnitedStates.Market.NYSE ), new ActualActual( ActualActual.Convention.ISDA ) )
      {
         termStructure_ = h ?? new Handle<YieldTermStructure>();
         termStructure_.registerWith( update );
      }

      // Index interface
      // BMA is fixed weekly on Wednesdays.
      public override string name() { return "BMA"; }
      public override bool isValidFixingDate( Date fixingDate )
      {
         Calendar cal = fixingCalendar();
         // either the fixing date is last Wednesday, or all days
         // between last Wednesday included and the fixing date are
         // holidays
         for ( Date d = Utils.previousWednesday( fixingDate ); d < fixingDate; ++d )
         {
            if ( cal.isBusinessDay( d ) )
               return false;
         }
         // also, the fixing date itself must be a business day
         return cal.isBusinessDay( fixingDate );
      }
      // Inspectors
      public Handle<YieldTermStructure> forwardingTermStructure() { return termStructure_; }
      // Date calculations
      public override Date maturityDate( Date valueDate )
      {
         Calendar cal = fixingCalendar();
         Date fixingDate = cal.advance( valueDate, -1, TimeUnit.Days );
         Date nextWednesday = Utils.previousWednesday( fixingDate + 7 );
         return cal.advance( nextWednesday, 1, TimeUnit.Days );
      }
      // This method returns a schedule of fixing dates between start and end.
      public Schedule fixingSchedule( Date start, Date end )
      {
         return new MakeSchedule().from( Utils.previousWednesday( start ) )
                           .to( Utils.nextWednesday( end ) )
                           .withFrequency( Frequency.Weekly )
                           .withCalendar( fixingCalendar() )
                           .withConvention( BusinessDayConvention.Following )
                           .forwards()
                           .value();
      }

      public override double forecastFixing( Date fixingDate )
      {
         Utils.QL_REQUIRE( !termStructure_.empty(),()=> "null term structure set to this instance of " + name() );
         Date start = fixingCalendar().advance( fixingDate, 1, TimeUnit.Days );
         Date end = maturityDate( start );
         return termStructure_.link.forwardRate( start, end, dayCounter_, Compounding.Simple ).rate();
      }

      protected Handle<YieldTermStructure> termStructure_;
   }

   public partial class Utils
   {
      public static Date previousWednesday( Date date )
      {
         int w = date.weekday();
         if ( w >= 4 ) // roll back w-4 days
            return date - new Period( ( w - 4 ), TimeUnit.Days );
         else // roll forward 4-w days and back one week
            return date + new Period( ( 4 - w - 7 ), TimeUnit.Days );
      }

      public static Date nextWednesday( Date date )
      {
         return previousWednesday( date + 7 );
      }
   }
}
