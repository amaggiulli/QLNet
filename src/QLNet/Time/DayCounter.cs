/*
 Copyright (C) 2008-2022 Andrea Maggiulli (a.maggiulli@gmail.com)

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
   /// <summary>
   /// Bridge Implementor
   /// </summary>
   public abstract class DayCounterImpl
   {
      public abstract string name();
      public virtual int dayCount(Date d1,Date d2)
      {
         return d2-d1;
      }
     
      public abstract double yearFraction(Date d1,Date d2,Date refPeriodStart,Date refPeriodEnd);
   }

   // This class provides methods for determining the length of a time period according to given market convention,
   // both as a number of days and as a year fraction.
   /// <summary>
   /// This class provides methods for determining the length of a time period according to given market convention,
   /// both as a number of days and as a year fraction.
   /// 
   /// The Bridge pattern is used to provide the base behavior of the day counter.
   /// </summary>
   public class DayCounter
   {
      protected DayCounterImpl _impl;
      protected DayCounter(DayCounterImpl impl) {_impl = impl;}

      public DayCounter()
      {}

      public bool empty()
      {
         return _impl == null;
      }

      public string name()
      {
         Utils.QL_REQUIRE(_impl!= null,()=> "no day counter implementation provided");
         return _impl!.name();
      }

      public int dayCount(Date d1,Date d2)
      {
         Utils.QL_REQUIRE(_impl != null,()=> "no day counter implementation provided");
         return _impl!.dayCount(d1,d2);
      }

      public double yearFraction(Date d1, Date d2) { return yearFraction(d1, d2, d1, d2); }
      public double yearFraction(Date d1, Date d2, Date refPeriodStart, Date refPeriodEnd)
      {
         Utils.QL_REQUIRE(_impl!=null,()=> "no day counter implementation provided");
         return _impl!.yearFraction(d1,d2,refPeriodStart,refPeriodEnd);
      }

      public static bool operator ==(DayCounter d1, DayCounter d2)
      {
         return d1 is null || d2 is null ?
                d1 is null && d2 is null :
                (d1.empty() && d2.empty()) || (!d1.empty() && !d2.empty() && d1.name() == d2.name());
      }
      public static bool operator!=(DayCounter d1, DayCounter d2)
      {
         return !(d1 == d2);
      }

      public override string ToString() { return this.name(); }
      public override bool Equals(object o) { return this == (DayCounter)o; }
      public override int GetHashCode() { return 0; }
   }
}
