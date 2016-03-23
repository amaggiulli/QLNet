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
   public class SpreadedSmileSection : SmileSection
   {
      public SpreadedSmileSection(SmileSection underlyingSection,Handle<Quote> spread)
      {
         underlyingSection_ = underlyingSection;
         spread_ = spread;

         underlyingSection_.registerWith(update);
         spread_.registerWith(update);
      }
      //! \name SmileSection interface
      //@{
      public override double minStrike() { return underlyingSection_.minStrike(); }
      public override double maxStrike() { return underlyingSection_.maxStrike(); }
      public override double? atmLevel() { return underlyingSection_.atmLevel(); }
      public override Date exerciseDate() { return underlyingSection_.exerciseDate(); }
      public override double exerciseTime() { return underlyingSection_.exerciseTime(); }
      public override DayCounter dayCounter() { return underlyingSection_.dayCounter(); }
      public override Date referenceDate() { return underlyingSection_.referenceDate(); }
      public override VolatilityType volatilityType() { return underlyingSection_.volatilityType(); }
      public override double shift() { return underlyingSection_.shift(); }
      //@}
      //! \name LazyObject interface
      //@{
      public override void update() { notifyObservers(); }
      //@}

      protected override double volatilityImpl( double k )
      {
         return underlyingSection_.volatility( k ) + spread_.link.value();
      }
      private SmileSection underlyingSection_;
      private Handle<Quote> spread_;
   }
}
