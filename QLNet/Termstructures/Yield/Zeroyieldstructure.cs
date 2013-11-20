/*
 Copyright (C) 2008, 2009 Siarhei Novik (snovik@gmail.com)
  
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
   //! Zero-yield term structure
   /*! This abstract class acts as an adapter to YieldTermStructure
      allowing the programmer to implement only the
      <tt>zeroYieldImpl(Time)</tt> method in derived classes.

      Discount and forward are calculated from zero yields.

      Zero rates are assumed to be annual continuous compounding.

      \ingroup yieldtermstructures
   */
   public class ZeroYieldStructure : YieldTermStructure 
   {
      #region Constructors

      public ZeroYieldStructure(DayCounter dc = null,List<Handle<Quote>> jumps = null, List<Date> jumpDates = null)
         : base(dc, jumps, jumpDates) {}

      public ZeroYieldStructure(Date referenceDate,Calendar calendar = null,DayCounter dc = null,
          List<Handle<Quote>> jumps = null, List<Date> jumpDates = null)
         : base(referenceDate, calendar, dc, jumps, jumpDates) { }

      public ZeroYieldStructure(int settlementDays,Calendar calendar, DayCounter dc = null,
          List<Handle<Quote>> jumps = null, List<Date> jumpDates = null)
         : base(settlementDays, calendar, dc, jumps, jumpDates) { }

      #endregion
      
      #region Calculations

      // This method must be implemented in derived classes to
      // perform the actual calculations. When it is called,
      // range check has already been performed; therefore, it
      // must assume that extrapolation is required.

      //! zero-yield calculation
      protected virtual double zeroYieldImpl(double t) { throw new NotSupportedException(); }
   
      #endregion

      #region YieldTermStructure implementation
      
      /*! Returns the discount factor for the given date calculating it
          from the zero yield.
      */
      protected override double discountImpl(double t)
      {
         if (t == 0.0)     // this acts as a safe guard in cases where
            return 1.0;   // zeroYieldImpl(0.0) would throw.

         double r = zeroYieldImpl(t);
         return Math.Exp(-r*t);
      }

      #endregion
   }
    ////! Zero-yield term structure
    ///*! This abstract class acts as an adapter to YieldTermStructure
    //    allowing the programmer to implement only the
    //    <tt>zeroYieldImpl(Time, bool)</tt> method in derived classes.
    //    Discount and forward are calculated from zero yields.
    //    Rates are assumed to be annual continuous compounding.
    //*/
    //public abstract class ZeroYieldStructure : YieldTermStructure {

    //    #region ctors
    //    public ZeroYieldStructure() : this(new DayCounter()) { }
    //    public ZeroYieldStructure(DayCounter dc) : base(dc) { }

    //    //public ZeroYieldStructure(Date referenceDate) :
    //    //    this(referenceDate, new Calendar(), new DayCounter()) { }
    //    //public ZeroYieldStructure(Date referenceDate, Calendar calendar) :
    //    //    this(referenceDate, calendar, new DayCounter()) { }
    //    public ZeroYieldStructure(Date referenceDate, Calendar calendar, DayCounter dc) :
    //        base(referenceDate, calendar, dc) { }

    //    //public ZeroYieldStructure(int settlementDays, Calendar c) :
    //    //    this(settlementDays, c, new DayCounter()) { }
    //    public ZeroYieldStructure(int settlementDays, Calendar c, DayCounter dc) :
    //        base(settlementDays, c, dc) { } 
    //    #endregion

    //    // zero-yield calculation
    //    protected abstract double zeroYieldImpl(double t);

    //    protected override double discountImpl(double t) {
    //        if (t == 0.0)     // this acts as a safe guard in cases where
    //            return 1.0;   // zeroYieldImpl(0.0) would throw.

    //        double r = zeroYieldImpl(t);
    //        return System.Math.Exp(-r * t);
    //    }
    //}
}
