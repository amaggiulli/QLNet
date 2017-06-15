/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2017 Andrea Maggiulli (a.maggiulli@gmail.com)
  
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

namespace QLNet
{
   //! Flat interest-rate curve
   public class FlatForward : YieldTermStructure, ILazyObject
   {
      // constructors
      public FlatForward( Date referenceDate, 
                          Handle<Quote> forward, 
                          DayCounter dayCounter, 
                          Compounding compounding = Compounding.Continuous,
                          Frequency frequency = Frequency.Annual)
         : base(referenceDate, new Calendar(), dayCounter)
      {
         forward_ = forward;
         compounding_ = compounding;
         frequency_ = frequency;

         forward_.registerWith(update);
      }

      public FlatForward( Date referenceDate, 
                          double forward, 
                          DayCounter dayCounter, 
                          Compounding compounding = Compounding.Continuous,
                          Frequency frequency = Frequency.Annual) :
         base(referenceDate, new Calendar(), dayCounter)
      {
         forward_ = new Handle<Quote>(new SimpleQuote(forward));
         compounding_ = compounding;
         frequency_ = frequency;
      }

      public FlatForward( int settlementDays, 
                          Calendar calendar, 
                          Handle<Quote> forward, 
                          DayCounter dayCounter,
                          Compounding compounding = Compounding.Continuous, 
                          Frequency frequency = Frequency.Annual) :
         base(settlementDays, calendar, dayCounter)
      {
         forward_ = forward;
         compounding_ = compounding;
         frequency_ = frequency;

         forward_.registerWith(update);
      }

      public FlatForward( int settlementDays, 
                          Calendar calendar, 
                          double forward, 
                          DayCounter dayCounter,
                          Compounding compounding = Compounding.Continuous, 
                          Frequency frequency = Frequency.Annual) :
         base(settlementDays, calendar, dayCounter)
      {
         forward_ = forward_ = new Handle<Quote>(new SimpleQuote(forward)); 
         compounding_ = compounding;
         frequency_ = frequency;
      }

      // TermStructure interface
      public override Date maxDate()
      {
         return Date.maxDate();
      }

      protected override double discountImpl(double t)
      {
         this.calculate();
         return rate_.discountFactor(t);
      }

      public void performCalculations()
      {
         rate_ = new InterestRate(forward_.link.value(), dayCounter(), compounding_, frequency_);
      }

      public override void update()
      {
         ((ILazyObject)this).update();
         base.update();
      }

      private Handle<Quote> forward_;
      private Compounding compounding_;
      private Frequency frequency_;
      private InterestRate rate_;

   }
}
