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
   public class AtmSmileSection : SmileSection
   {
      public AtmSmileSection( SmileSection source,double? atm = null)
      {
         source_ = source;
         f_ = atm;
         if ( f_ == null )
            f_ = source_.atmLevel();
      }

      public override double minStrike() { return source_.minStrike(); }
      public override double maxStrike() { return source_.maxStrike(); }
      public override double? atmLevel() { return f_; }
      public override Date exerciseDate() { return source_.exerciseDate(); }
      public override double exerciseTime() { return source_.exerciseTime(); }
      public override DayCounter dayCounter() { return source_.dayCounter(); }
      public override Date referenceDate()  { return source_.referenceDate(); }
      public override VolatilityType volatilityType()  { return source_.volatilityType();}
      public override double shift() { return source_.shift(); }

      protected override double volatilityImpl(double strike) { return source_.volatility(strike);}
      protected override double varianceImpl(double strike) { return source_.variance(strike);}

      private SmileSection source_;
      private double? f_;
   }
}
