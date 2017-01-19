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
   public class SpreadedSwaptionVolatility : SwaptionVolatilityStructure
   {
      public SpreadedSwaptionVolatility(Handle<SwaptionVolatilityStructure> baseVol,Handle<Quote> spread)
         :base(baseVol.link.businessDayConvention(),baseVol.link.dayCounter())
      {
         baseVol_ = baseVol;
         spread_ = spread;

         enableExtrapolation( baseVol.link.allowsExtrapolation() );
         baseVol_.registerWith(update);
         spread_.registerWith(update);
      }
      // All virtual methods of base classes must be forwarded
      //! \name TermStructure interface
      //@{
      public override DayCounter dayCounter() { return baseVol_.link.dayCounter(); }
      public override Date maxDate() { return baseVol_.link.maxDate(); }
      public override double maxTime() { return baseVol_.link.maxTime(); }
      public override Date referenceDate() { return baseVol_.link.referenceDate(); }
      public override Calendar calendar() { return baseVol_.link.calendar(); }
      public override int settlementDays() { return baseVol_.link.settlementDays(); }
      //@}
      //! \name VolatilityTermStructure interface
      //@{
      public override double minStrike() { return baseVol_.link.minStrike(); }
      public override double maxStrike() { return baseVol_.link.maxStrike(); }
      //@}
      //! \name SwaptionVolatilityStructure interface
      //@{
      public override Period maxSwapTenor() { return baseVol_.link.maxSwapTenor(); }
      //@}
      public override VolatilityType volatilityType() { return baseVol_.link.volatilityType(); }
      
      
      //! \name SwaptionVolatilityStructure interface
      //@{
      protected override SmileSection smileSectionImpl(Date optionDate,Period swapTenor)
      {
         SmileSection baseSmile = baseVol_.link.smileSection(optionDate, swapTenor, true);
         return new SpreadedSmileSection(baseSmile, spread_);
      }
      protected override SmileSection smileSectionImpl(double optionTime,double swapLength)
      {
         SmileSection baseSmile = baseVol_.link.smileSection(optionTime, swapLength, true);
         return new SpreadedSmileSection(baseSmile, spread_);
      }
      protected override double volatilityImpl(Date optionDate,Period swapTenor,double strike)
      {
         return baseVol_.link.volatility( optionDate, swapTenor, strike, true ) + spread_.link.value();
      }
      protected override double volatilityImpl(double optionTime,double swapLength,double strike)
      {
         return baseVol_.link.volatility( optionTime, swapLength, strike, true ) + spread_.link.value();
      }
      protected override double shiftImpl( double optionTime, double swapLength )
      {
         return baseVol_.link.shift( optionTime, swapLength, true );
      }
      //@}
      private Handle<SwaptionVolatilityStructure> baseVol_;
      private Handle<Quote> spread_;

   }
}
