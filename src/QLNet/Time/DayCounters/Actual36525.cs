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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLNet
{
   /// <summary>
   /// Actual/365.25 day count convention
   /// </summary>
   public class Actual36525 : DayCounter
   {
      public Actual36525(bool includeLastDay = false)
         :base(new Impl(includeLastDay))
      {}

      private class Impl : DayCounterImpl
      {
         private readonly bool includeLastDay_;

         public Impl(bool includeLastDay)
         {
            includeLastDay_ = includeLastDay;
         }

         public override string name()
         {
            return includeLastDay_ ? "Actual/365.25 (inc)" : "Actual/365.25";
         }

         public override int dayCount(Date d1, Date d2)
         {
            return (d2 - d1) + (includeLastDay_ ? 1 : 0);
         }

         public override double yearFraction(Date d1, Date d2, Date refPeriodStart, Date refPeriodEnd)
         {
            return dayCount(d1,d2)/365.25;
         }
      }
   }
}
