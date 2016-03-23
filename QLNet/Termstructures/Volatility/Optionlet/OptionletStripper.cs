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

using System.Collections.Generic;
using System.Linq;

namespace QLNet
{
   /*! StrippedOptionletBase specialization. It's up to derived
        classes to implement LazyObject::performCalculations
    */
   public enum VolatilityType { ShiftedLognormal, Normal };
   public class OptionletStripper : StrippedOptionletBase
   {
      //! \name StrippedOptionletBase interface
      //@{
      public override List<double> optionletStrikes(int i)
      {
         calculate();
         Utils.QL_REQUIRE( i < optionletStrikes_.Count,()=>
                    "index (" + i + ") must be less than optionletStrikes size (" + optionletStrikes_.Count + ")" );
         return optionletStrikes_[i];
      }

      public override List<double> optionletVolatilities(int i)
      {
         calculate();
         Utils.QL_REQUIRE( i < optionletVolatilities_.Count,()=>
                    "index (" + i + ") must be less than optionletVolatilities size (" +
                    optionletVolatilities_.Count + ")" );
         return optionletVolatilities_[i];
      }

      public override List<Date> optionletFixingDates()
      {
         calculate();
         return optionletDates_;
      }
      public override List<double> optionletFixingTimes()
      {
         calculate();
         return optionletTimes_;
      }
      public override int optionletMaturities() { return optionletTenors_.Count; }

      public override List<double> atmOptionletRates()
      {
         calculate();
         return atmOptionletRate_;
      }

      public override DayCounter dayCounter() { return termVolSurface_.dayCounter(); }
      public override Calendar calendar() { return termVolSurface_.calendar(); }
      public override int settlementDays() { return termVolSurface_.settlementDays(); }
      public override BusinessDayConvention businessDayConvention() { return termVolSurface_.businessDayConvention(); }
      //@}

      public List<Period> optionletFixingTenors() { return optionletTenors_; }
      public List<Date> optionletPaymentDates()
      {
         calculate();
         return optionletPaymentDates_;
      }
      public List<double> optionletAccrualPeriods()
      {
         calculate();
         return optionletAccrualPeriods_;
      }
      public CapFloorTermVolSurface termVolSurface() { return termVolSurface_; }
      public IborIndex iborIndex() { return iborIndex_; }
      public double displacement() { return displacement_; }
      public VolatilityType volatilityType() { return volatilityType_; }

      protected OptionletStripper( CapFloorTermVolSurface termVolSurface, IborIndex iborIndex,
                                   Handle<YieldTermStructure> discount = null,
                                   VolatilityType type = VolatilityType.ShiftedLognormal,
                                   double displacement = 0.0)
      {
         termVolSurface_ = termVolSurface;
         iborIndex_ = iborIndex;
         discount_ = discount ?? new Handle<YieldTermStructure>();
         nStrikes_ = termVolSurface.strikes().Count; 
         volatilityType_ = type;
         displacement_ = displacement;

                 
         if (volatilityType_ ==  VolatilityType.Normal) 
         {
            Utils.QL_REQUIRE(displacement_ == 0.0,()=>
                       "non-null displacement is not allowed with Normal model");
        }

        termVolSurface.registerWith(update);
        iborIndex_.registerWith( update );
        discount_.registerWith( update );
        Settings.registerWith(update);

        Period indexTenor = iborIndex_.tenor();
        Period maxCapFloorTenor = termVolSurface.optionTenors().Last();

        // optionlet tenors and capFloor lengths
        optionletTenors_.Add(indexTenor);
        capFloorLengths_.Add(optionletTenors_.Last()+indexTenor);
        Utils.QL_REQUIRE(maxCapFloorTenor>=capFloorLengths_.Last(),()=>
                         "too short (" + maxCapFloorTenor + ") capfloor term vol termVolSurface");
        Period nextCapFloorLength = capFloorLengths_.Last()+indexTenor;
        while (nextCapFloorLength<=maxCapFloorTenor) 
        {
           optionletTenors_.Add(capFloorLengths_.Last());
           capFloorLengths_.Add(nextCapFloorLength);
           nextCapFloorLength += indexTenor;
        }
        nOptionletTenors_ = optionletTenors_.Count;

        optionletVolatilities_ = new InitializedList<List<double>>( nOptionletTenors_) ; //, new InitializedList<double>( nStrikes_ ) );
        for ( int x = 0 ; x < nOptionletTenors_; x++)
        {
           optionletVolatilities_[x] = new InitializedList<double>(nStrikes_);
        }
        optionletStrikes_ = new InitializedList<List<double>>( nOptionletTenors_ ) ; //,termVolSurface.strikes());
        for ( int x = 0; x < nOptionletTenors_; x++ )
        {
           optionletStrikes_[x] = new List<double>( termVolSurface.strikes() );
        }

        optionletDates_ = new InitializedList<Date>(nOptionletTenors_);
        optionletTimes_ = new InitializedList<double>(nOptionletTenors_);
        atmOptionletRate_ = new InitializedList<double>(nOptionletTenors_);
        optionletPaymentDates_ = new InitializedList<Date>(nOptionletTenors_);
        optionletAccrualPeriods_ = new InitializedList<double>(nOptionletTenors_);

      }
      protected CapFloorTermVolSurface termVolSurface_;
      protected IborIndex iborIndex_;
      protected Handle<YieldTermStructure> discount_;
      protected int nStrikes_;
      protected int nOptionletTenors_;

      protected List<List<double> > optionletStrikes_;
      protected List<List<double> > optionletVolatilities_;

      protected List<double> optionletTimes_;
      protected List<Date> optionletDates_;
      protected List<Period> optionletTenors_ = new List<Period>();
      protected List<double> atmOptionletRate_;
      protected List<Date> optionletPaymentDates_;
      protected List<double> optionletAccrualPeriods_;

      protected List<Period> capFloorLengths_ = new List<Period>();
      protected VolatilityType volatilityType_;
      protected double displacement_;

   }
}
