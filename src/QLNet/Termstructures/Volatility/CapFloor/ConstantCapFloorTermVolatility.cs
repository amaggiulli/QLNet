/*
 Copyright (C) 2008 Andrea Maggiulli

 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

 QLNet is free software: you can redistribute it and/or modify it
 under the terms of the QLNet license.  You should have received a
 copy of the license along with this program; if not, license is
 available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.

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
   public class ConstantCapFloorTermVolatility : CapFloorTermVolatilityStructure
   {
      private Handle<Quote> volatility_;

      /// <summary>
      /// Initializes the structure with a floating reference date and floating market data.
      /// </summary>
      public ConstantCapFloorTermVolatility(int settlementDays,
                                            Calendar cal,
                                            BusinessDayConvention bdc,
                                            Handle<Quote> volatility,
                                            DayCounter dc)
         : base(settlementDays, cal, bdc, dc)
      {
         volatility_ = volatility;
         volatility_.registerWith(update);
      }

      /// <summary>
      /// Initializes the structure with a fixed reference date and floating market data.
      /// </summary>
      public ConstantCapFloorTermVolatility(Date referenceDate,
                                            Calendar cal,
                                            BusinessDayConvention bdc,
                                            Handle<Quote> volatility,
                                            DayCounter dc)
         : base(referenceDate, cal, bdc, dc)
      {
         volatility_ = volatility;
         volatility_.registerWith(update);
      }

      /// <summary>
      /// Initializes the structure with a floating reference date and fixed market data.
      /// </summary>
      public ConstantCapFloorTermVolatility(int settlementDays,
                                            Calendar cal,
                                            BusinessDayConvention bdc,
                                            double volatility,
                                            DayCounter dc)
         : base(settlementDays, cal, bdc, dc)
      {
         volatility_ = new Handle<Quote>(new SimpleQuote(volatility));
      }

      /// <summary>
      /// Initializes the structure with a fixed reference date and fixed market data.
      /// </summary>
      public ConstantCapFloorTermVolatility(Date referenceDate,
                                            Calendar cal,
                                            BusinessDayConvention bdc,
                                            double volatility,
                                            DayCounter dc)
         : base(referenceDate, cal, bdc, dc)
      {
         volatility_ = new Handle<Quote>(new SimpleQuote(volatility));
      }

      #region TermStructure interface

      public override Date maxDate()
      {
         return Date.maxDate();
      }

      #endregion


      #region VolatilityTermStructure interface
      public override double minStrike()
      {
         return Double.MinValue;
      }

      public override double maxStrike()
      {
         return Double.MaxValue;
      }

      #endregion


      protected override double volatilityImpl(double t, double rate)
      {
         return volatility_.link.value();
      }

   }
}
