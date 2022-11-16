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
namespace QLNet
{
   /// <summary>
   ///  Actual/366 day count convention
   /// </summary>
   public class Actual366 : DayCounter
   {
      public Actual366( bool includeLastDay = false) : base(new Impl(includeLastDay)) { }

      private class Impl : DayCounterImpl
      {
         private readonly bool includeLastDay;

         public Impl(bool includeLastDay)
         {
            this.includeLastDay = includeLastDay;
         }

         public override string name()
         {
            return includeLastDay ? "Actual/366 (inc)" : "Actual/366";
         }

         public override int dayCount(Date d1, Date d2)
         {
            return (d2 - d1) + (includeLastDay ? 1 : 0);
         }

         public override double yearFraction(Date d1, Date d2, Date refPeriodStart, Date refPeriodEnd)
         {
            return dayCount(d1,d2)/366.0;
         }
      }
   }
}
