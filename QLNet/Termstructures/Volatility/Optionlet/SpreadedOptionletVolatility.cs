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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QLNet
{
   public class SpreadedOptionletVolatility : OptionletVolatilityStructure
   {
      public SpreadedOptionletVolatility(Handle<OptionletVolatilityStructure> baseVol,Handle<Quote> spread)
      {
         baseVol_ = baseVol;
         spread_ = spread;
         enableExtrapolation(baseVol.link.allowsExtrapolation()) ;
         baseVol_.registerWith(update);
         spread_.registerWith(update);
      }
      // All virtual methods of base classes must be forwarded
      //! \name VolatilityTermStructure interface
      //@{
      public override BusinessDayConvention businessDayConvention() { return baseVol_.link.businessDayConvention(); }
      public override double minStrike() { return baseVol_.link.minStrike(); }
      public override double maxStrike() { return baseVol_.link.maxStrike(); }
      //@}
      //! \name TermStructure interface
      //@{
      public override DayCounter dayCounter() { return baseVol_.link.dayCounter(); }
      public override Date maxDate() { return baseVol_.link.maxDate(); }
      public override double maxTime() { return baseVol_.link.maxTime(); }
      public override Date referenceDate() { return baseVol_.link.referenceDate(); }
      public override Calendar calendar() { return baseVol_.link.calendar(); }
      public override int settlementDays() { return baseVol_.link.settlementDays(); }
      //@}


      // All virtual methods of base classes must be forwarded
      //! \name OptionletVolatilityStructure interface
      //@{
      protected override SmileSection smileSectionImpl( Date d )
      {
         SmileSection baseSmile = baseVol_.link.smileSection(d, true);
         return new SpreadedSmileSection(baseSmile, spread_);
      }
      protected override SmileSection smileSectionImpl( double optionTime )
      {
         SmileSection baseSmile = baseVol_.link.smileSection(optionTime, true);
         return new SpreadedSmileSection(baseSmile, spread_);
      }
      protected override double volatilityImpl( double t, double s )
      {
         return baseVol_.link.volatility( t, s, true ) + spread_.link.value();
      }
      //@}

      private Handle<OptionletVolatilityStructure> baseVol_;
      private Handle<Quote> spread_;

   }
}
