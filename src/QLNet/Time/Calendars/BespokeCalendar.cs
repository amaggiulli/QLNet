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

namespace QLNet
{
   //! Bespoke calendar
   /*! This calendar has no predefined set of business days. Holidays
       and weekdays can be defined by means of the provided
       interface. Instances constructed by copying remain linked to
       the original one; adding a new holiday or weekday will affect
       all linked instances.

       \ingroup calendars
   */
   public class BespokeCalendar : Calendar
   {
      // here implementation does not follow a singleton pattern
      private class Impl : CalendarImpl
      {
         private readonly string _Name;
         private readonly SortedSet<DayOfWeek> _Weekend = new();

         public Impl(string name)
         {
            _Name = name;
         }
         public override string name() { return _Name; }
         public override bool isWeekend(DayOfWeek w) { return (_Weekend.Contains(w)); }
         public override bool isBusinessDay(Date date) { return !isWeekend(date.DayOfWeek); }
         public void addWeekend(DayOfWeek w) { _Weekend.Add(w); }
      }

      private Impl _BespokeImpl;

      /*! \warning different bespoke calendars created with the same
                   name (or different bespoke calendars created with
                   no name) will compare as equal.
      */
      public BespokeCalendar(string name = "") : base()
      {
         _BespokeImpl = new Impl(name);
         _impl = _BespokeImpl;
      }

      //! marks the passed day as part of the weekend
      public void addWeekend(DayOfWeek w)
      {
            _BespokeImpl.addWeekend(w);
      }
   }
}
