/*
 Copyright (C) 2008, 2009 , 2010, 2011, 2012  Andrea Maggiulli (a.maggiulli@gmail.com) 
  
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
   //! Constant callable-bond volatility, no time-strike dependence
   public class CallableBondConstantVolatility : CallableBondVolatilityStructure
   {
      public CallableBondConstantVolatility(Date referenceDate, double volatility, DayCounter dayCounter)
         :base(referenceDate)
      {
         volatility_ = new Handle<Quote>(new SimpleQuote(volatility));
         dayCounter_ = dayCounter;
         maxBondTenor_ = new Period(100,TimeUnit.Years);
      }

      public CallableBondConstantVolatility(Date referenceDate, Handle<Quote> volatility, DayCounter dayCounter)
         :base(referenceDate)
      {
         volatility_ = volatility;
         dayCounter_ = dayCounter;
         maxBondTenor_ = new Period(100,TimeUnit.Years);
         volatility_.registerWith(update);
      }

      public CallableBondConstantVolatility(int settlementDays, Calendar calendar, double volatility, DayCounter dayCounter)
         :base(settlementDays, calendar)
      {
         volatility_ = new Handle<Quote>(new SimpleQuote(volatility));
         dayCounter_ = dayCounter;
         maxBondTenor_ = new Period(100,TimeUnit.Years);
      }

      public CallableBondConstantVolatility(int settlementDays, Calendar calendar, Handle<Quote> volatility,DayCounter dayCounter)
          :base(settlementDays, calendar)
      {
         volatility_ = volatility;
         dayCounter_ = dayCounter;
         maxBondTenor_ = new Period(100,TimeUnit.Years);
         volatility_.registerWith(update);
      }

      //! \name TermStructure interface
      //@{
      public override DayCounter dayCounter() { return dayCounter_; }
      public override Date maxDate() { return Date.maxDate(); }
      //@}
      //! \name CallableBondConstantVolatility interface
      //@{
      public override Period maxBondTenor() {return maxBondTenor_;}
      public override double maxBondLength() {return double.MaxValue;}
      public override double minStrike() {return double.MinValue;}
      public override double maxStrike()  {return double.MaxValue;}

      protected override double volatilityImpl(double d1, double d2, double d3)
      {
         return volatility_.link.value();
      }
      protected override SmileSection smileSectionImpl(double optionTime, double bondLength)
      {
         double atmVol = volatility_.link.value();
         return new FlatSmileSection(optionTime, atmVol, dayCounter_);
      }
      protected override double volatilityImpl( Date d , Period p, double d1)
      {
         return volatility_.link.value();
      }
      //@}
      private Handle<Quote> volatility_;
      private DayCounter dayCounter_;
      private Period maxBondTenor_;
   }
}
