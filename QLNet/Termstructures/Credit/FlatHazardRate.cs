/*
 Copyright (C) 2008-2013  Andrea Maggiulli (a.maggiulli@gmail.com)

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
   //! Flat hazard-rate curve
   /*! \ingroup defaultprobabilitytermstructures */
   public class FlatHazardRate : HazardRateStructure
   {
      #region Constructors

      public FlatHazardRate(Date referenceDate, Handle<Quote> hazardRate, DayCounter dc)
         : base(referenceDate, new Calendar(), dc)
      {
         hazardRate_ = hazardRate;
         hazardRate_.registerWith(update);
      }

      public FlatHazardRate(Date referenceDate, double hazardRate, DayCounter dc)
         : base(referenceDate, new Calendar(), dc)
      {
         hazardRate_ = new Handle<Quote>( new SimpleQuote(hazardRate));
      }

      public FlatHazardRate(int settlementDays,Calendar calendar,Handle<Quote> hazardRate,DayCounter dc)
         : base(settlementDays, calendar, dc)
      {
         hazardRate_ = hazardRate;
         hazardRate_.registerWith(update);
      }

      public FlatHazardRate(int settlementDays, Calendar calendar, double hazardRate, DayCounter dc)
         : base(settlementDays, calendar, dc)
      {
         hazardRate_ = new Handle<Quote>(new SimpleQuote(hazardRate));
      }

      #endregion


      #region TermStructure interface

      public override Date maxDate()  { return Date.maxDate(); }

      #endregion

      
      #region HazardRateStructure interface
        
      protected override double hazardRateImpl(double t) { return hazardRate_.link.value(); }

      #endregion

      #region DefaultProbabilityTermStructure interface

      protected override double survivalProbabilityImpl(double t)
      {
         return Math.Exp(-hazardRate_.link.value()*t);
      }
      
      #endregion

      private Handle<Quote> hazardRate_;
    
   }
}
