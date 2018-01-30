/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)

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

using System;

namespace QLNet
{
   //! %Calendar for reproducing theoretical calculations.
   /*! This calendar has no holidays. It ensures that dates at whole-month distances have the same day of month.    */

   public class NullCalendar : Calendar
   {
      public NullCalendar() : base(Impl.Singleton) { }

      private class Impl : Calendar
      {
         public static readonly Impl Singleton = new Impl();

         private Impl() { }

         public override string name() { return "Null calendar"; }

         public override bool isWeekend(DayOfWeek w) { return false; }

         public override bool isBusinessDay(Date d) { return true; }
      }
   }
}
