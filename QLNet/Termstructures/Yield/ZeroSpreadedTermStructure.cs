/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
  
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
   //! Term structure with an added spread on the zero yield rate
   /*! \note This term structure will remain linked to the original
         structure, i.e., any changes in the latter will be
         reflected in this structure as well.

   \ingroup yieldtermstructures

   \test
   - the correctness of the returned values is tested by
      checking them against numerical calculations.
   - observability against changes in the underlying term
      structure and in the added spread is checked.
   */
   public class ZeroSpreadedTermStructure : ZeroYieldStructure 
   {
      public ZeroSpreadedTermStructure(Handle<YieldTermStructure> h,
                                       Handle<Quote> spread,
                                       Compounding comp = Compounding.Continuous,
                                       Frequency freq = Frequency.NoFrequency,
                                       DayCounter dc = null)
      {
         originalCurve_ = h;
         spread_ = spread;
         comp_ = comp;
         freq_ = freq;
         dc_ = dc;

         originalCurve_.registerWith(update);
         spread_.registerWith(update);
      }

        
      #region YieldTermStructure interface

      public override DayCounter dayCounter() {return originalCurve_.link.dayCounter();}
      public override Calendar calendar() { return originalCurve_.link.calendar(); }
      public override int settlementDays() { return originalCurve_.link.settlementDays(); }
      public override Date referenceDate() { return originalCurve_.link.referenceDate(); }
      public override Date maxDate() { return originalCurve_.link.maxDate(); }
      public override double maxTime() { return originalCurve_.link.maxTime(); }

      #endregion

      
        //! returns the spreaded zero yield rate
      protected override double zeroYieldImpl(double t) 
      {
         // to be fixed: user-defined daycounter should be used
        InterestRate zeroRate =
            originalCurve_.link.zeroRate(t, comp_, freq_, true);
        InterestRate spreadedRate = new InterestRate(zeroRate.value() + spread_.link.value(),
                                  zeroRate.dayCounter(),
                                  zeroRate.compounding(),
                                  zeroRate.frequency());
        return spreadedRate.equivalentRate(Compounding.Continuous, Frequency.NoFrequency, t).value();
      }
        //! returns the spreaded forward rate
        /* This method must disappear should the spread become a curve */
      protected   double forwardImpl(double t) 
      {
         return originalCurve_.link.forwardRate(t, t, comp_, freq_, true).value()
            + spread_.link.value();
      }
      
      private   Handle<YieldTermStructure> originalCurve_;
      private   Handle<Quote> spread_;
      private   Compounding comp_;
      private   Frequency freq_;
      private   DayCounter dc_;
    }

}
