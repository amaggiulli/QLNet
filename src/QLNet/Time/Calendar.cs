/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2024 Andrea Maggiulli (a.maggiulli@gmail.com)
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)

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
   /// <summary>
   /// Bridge implementor
   /// </summary>
   public abstract class CalendarImpl
   {
      public abstract string name();
      public abstract bool isBusinessDay(Date date);
      public abstract bool isWeekend(DayOfWeek w);
      public virtual bool isEarlyClose(Date d)
      {
         throw new NotImplementedException("isEarlyClose not implemented for the given calendar");
      }

      public SortedSet<Date> addedHolidays { get; set; } = new SortedSet<Date>();
      public SortedSet<Date> removedHolidays { get; set; } = new SortedSet<Date>();
   }

   /// <summary>
   /// This class provides methods for determining whether a date is a
   /// business day or a holiday for a given market, and for
   /// incrementing/decrementing a date of a given number of business days.
   ///
   /// A calendar should be defined for specific exchange holiday schedule
   /// or for general country holiday schedule. Legacy city holiday schedule
   /// calendars will be moved to the exchange/country convention.
   /// </summary>
   public class Calendar
   {
      protected CalendarImpl _impl;

      // constructors
      /*! The default constructor returns a calendar with a null
          implementation, which is therefore unusable except as a
          placeholder. */
      public Calendar()
      {}

      /// <summary>
      /// Create a calendar from implementation
      /// </summary>
      /// <param name="impl"></param>
      protected Calendar(CalendarImpl impl)
      {
         _impl = impl;
      }

      // other functions
      // <summary>
      // Returns whether or not the calendar is initialized
      // </summary>
      public bool empty() { return _impl == null; }           

      // <summary>
      // Returns the name of the calendar.
      // This method is used for output and comparison between
      // calendars. It is not> meant to be used for writing
      // switch-on-type code.
      // </summary>
      // <returns>   
      // The name of the calendar.
      // </returns>
      public string name()
      {
         Utils.QL_REQUIRE(_impl!=null,()=> "no calendar implementation provided");
         return _impl.name();
      }

      // Returns the set of added holidays for the given calendar
      public SortedSet<Date> addedHolidays()
      {
         Utils.QL_REQUIRE(_impl!=null,()=> "no calendar implementation provided");
         return _impl.addedHolidays;
      }

      // Returns the set of removed holidays for the given calendar 
      public SortedSet<Date> removedHolidays()
      {
         Utils.QL_REQUIRE(_impl!=null,()=> "no calendar implementation provided");
         return _impl.removedHolidays;
      }

      // Clear the set of added and removed holidays 
      public void resetAddedAndRemovedHolidays()
      {
         Utils.QL_REQUIRE(_impl!=null,()=> "no calendar implementation provided");
         _impl.addedHolidays.Clear();
         _impl.removedHolidays.Clear();
      }

      // Returns true iff the date is a holiday for the given market.
      public bool isBusinessDay(Date d)
      {
         Utils.QL_REQUIRE(_impl != null, ()=> "no calendar implementation provided");

         if (_impl != null && _impl.addedHolidays.Count != 0 && _impl.addedHolidays.Contains(d))
            return false;

         if (_impl != null && _impl.removedHolidays.Count != 0 && _impl.removedHolidays.Contains(d))
            return true;

         return _impl!.isBusinessDay(d);
      }

      /// <summary>
      /// Return true if the date is an early close for the given market
      /// </summary>
      /// <param name="d"></param>
      /// <returns></returns>
      public bool isEarlyClose(Date d)
      {
         Utils.QL_REQUIRE(_impl != null, ()=> "no calendar implementation provided");
         return _impl!.isEarlyClose(d);
      }

      /// <summary>
      /// Returns true if the date is a holiday for the given
      /// market.
      /// </summary>
      public bool isHoliday(Date d) { return !isBusinessDay(d); }

      ///<summary>
      /// Returns true if the weekday is part of the
      /// weekend for the given market.
      ///</summary>
      public virtual bool isWeekend(DayOfWeek w)
      {
         Utils.QL_REQUIRE(_impl != null, ()=> "no calendar implementation provided");
         return _impl!.isWeekend(w);
      }

      /// <summary>
      /// Returns true iff the date is last business day for the
      /// month in given market.
      /// </summary>
      public bool isEndOfMonth(Date d)
      {
         return (d.Month != adjust(d + 1).Month);
      }

      /// <summary>
      /// last business day of the month to which the given date belongs
      /// </summary>
      public Date endOfMonth(Date d)
      {
         return adjust(Date.endOfMonth(d), BusinessDayConvention.Preceding);
      }

      /// <summary>
      /// Adds a date to the set of holidays for the given calendar.
      /// </summary>
      public void addHoliday(Date d)
      {
         Utils.QL_REQUIRE(_impl != null, ()=> "no calendar implementation provided");

         // if d was a genuine holiday previously removed, revert the change
         _impl!.removedHolidays.Remove(d);
         // if it's already a holiday, leave the calendar alone.
         // Otherwise, add it.
         if (_impl.isBusinessDay(d))
            _impl.addedHolidays.Add(d);
      }

      /// <summary>
      /// Removes a date from the set of holidays for the given calendar.
      /// </summary>
      public void removeHoliday(Date d)
      {
         Utils.QL_REQUIRE(_impl != null, ()=> "no calendar implementation provided");

         // if d was an artificially-added holiday, revert the change
         _impl!.addedHolidays.Remove(d);
         // if it's already a business day, leave the calendar alone.
         // Otherwise, add it.
         if (!_impl.isBusinessDay(d))
            _impl.removedHolidays.Add(d);
      }

      /// <summary>
      /// Returns the holidays between two dates
      /// </summary>
      public List<Date> holidayList(Date from, Date to, bool includeWeekEnds = false)
      {
         Utils.QL_REQUIRE(to >= from, () => "'from' date (" + from + ") must be equal to or earlier than 'to' date (" + to + ")");
         var result = new List<Date>();

         for (var d = from; d <= to; ++d)
         {
            if (isHoliday(d)
                && (includeWeekEnds || !isWeekend(d.DayOfWeek)))
               result.Add(d);
         }
         return result;
      }

      /// <summary>
      /// Returns the business days between two dates. */
      /// </summary>
      /// <param name="from"></param>
      /// <param name="to"></param>
      /// <returns></returns>
      public List<Date> businessDayList(Date from, Date to)
      {
         Utils.QL_REQUIRE(to >= from, () => "'from' date (" + from + ") must be equal to or earlier than 'to' date (" + to + ")");
         var result = new List<Date>();

         for (var d = from; d <= to; ++d)
         {
            if (isBusinessDay(d))
               result.Add(d);
         }
         return result;
      }

      /// <summary>
      /// Adjusts a non-business day to the appropriate near business day  with respect
      /// to the given convention.
      /// </summary>
      public Date adjust(Date d, BusinessDayConvention c = BusinessDayConvention.Following)
      {
         Utils.QL_REQUIRE(d != null,()=> "null date");
         if (c == BusinessDayConvention.Unadjusted)
            return d;

         var d1 = d;
         if (c is BusinessDayConvention.Following or
             BusinessDayConvention.ModifiedFollowing or
             BusinessDayConvention.HalfMonthModifiedFollowing)
         {
            while (isHoliday(d1))
               d1++;
            if (c is BusinessDayConvention.ModifiedFollowing or BusinessDayConvention.HalfMonthModifiedFollowing)
            {
               if (d1.Month != d.Month)
                  return adjust(d, BusinessDayConvention.Preceding);
               if (c == BusinessDayConvention.HalfMonthModifiedFollowing)
               {
                  if (d.Day <= 15 && d1.Day > 15)
                  {
                     return adjust(d, BusinessDayConvention.Preceding);
                  }
               }
            }
         }
         else if (c is BusinessDayConvention.Preceding or BusinessDayConvention.ModifiedPreceding)
         {
            while (isHoliday(d1))
               d1--;
            if (c == BusinessDayConvention.ModifiedPreceding && d1.Month != d.Month)
               return adjust(d, BusinessDayConvention.Following);
         }
         else if (c == BusinessDayConvention.Nearest)
         {
            Date d2 = d;
            while (isHoliday(d1) && isHoliday(d2))
            {
               d1++;
               d2--;
            }
            if (isHoliday(d1))
               return d2;
            else
               return d1;
         }
         else
            Utils.QL_FAIL("unknown business-day convention");
         return d1;
      }

      /// <summary>
      /// Advances the given date of the given number of business days and
      /// returns the result.
      /// </summary>
      /// <remarks>The input date is not modified</remarks>
      public Date advance(Date d, int n, TimeUnit unit, BusinessDayConvention c = BusinessDayConvention.Following, bool endOfMonth = false)
      {
         Utils.QL_REQUIRE(d!=null,()=> "null date");
         if (n == 0)
            return adjust(d, c);
         else if (unit == TimeUnit.Days)
         {
            Date d1 = d;
            if (n > 0)
            {
               while (n > 0)
               {
                  d1++;
                  while (isHoliday(d1))
                     d1++;
                  n--;
               }
            }
            else
            {
               while (n < 0)
               {
                  d1--;
                  while (isHoliday(d1))
                     d1--;
                  n++;
               }
            }
            return d1;
         }
         else if (unit == TimeUnit.Weeks)
         {
            Date d1 = d + new Period(n, unit);
            return adjust(d1, c);
         }
         else
         {
            Date d1 = d + new Period(n, unit);
            if (endOfMonth && (unit == TimeUnit.Months || unit == TimeUnit.Years) && isEndOfMonth(d))
               return this.endOfMonth(d1);
            return adjust(d1, c);
         }
      }

      /// <summary>
      /// Advances the given date as specified by the given period and
      /// returns the result.
      /// </summary>
      /// <remarks>The input date is not modified.</remarks>
      public Date advance(Date d, Period p, BusinessDayConvention c = BusinessDayConvention.Following, bool endOfMonth = false)
      {
         return advance(d, p.length(), p.units(), c, endOfMonth);
      }

      /// <summary>
      /// Calculates the number of business days between two given
      /// dates and returns the result.
      /// </summary>
      public int businessDaysBetween(Date from, Date to, bool includeFirst = true, bool includeLast = false)
      {
         int wd = 0;
         if (from != to)
         {
            if (from < to)
            {
               // the last one is treated separately to avoid incrementing Date::maxDate()
               for (Date d = from; d < to; ++d)
               {
                  if (isBusinessDay(d))
                     ++wd;
               }
               if (isBusinessDay(to))
                  ++wd;
            }
            else
            {
               for (Date d = to; d < from; ++d)
               {
                  if (isBusinessDay(d))
                     ++wd;
               }
               if (isBusinessDay(from))
                  ++wd;
            }

            if (isBusinessDay(from) && !includeFirst)
               wd--;
            if (isBusinessDay(to) && !includeLast)
               wd--;

            if (from > to)
               wd = -wd;
         }
         else if (includeFirst && includeLast && isBusinessDay(from))
         {
            wd = 1;
         }

         return wd;
      }

      /// <summary>
      /// This class provides the means of determining the Easter
      /// Monday for a given year, as well as specifying Saturdays
      /// and Sundays as weekend days.
      /// </summary>
      public abstract class WesternImpl : CalendarImpl
      {
         private static readonly int[] EasterMonday =
         {
            98,  90, 103,  95, 114, 106,  91, 111, 102,   // 1901-1909
            87, 107,  99,  83, 103,  95, 115,  99,  91, 111,   // 1910-1919
            96,  87, 107,  92, 112, 103,  95, 108, 100,  91,   // 1920-1929
            111,  96,  88, 107,  92, 112, 104,  88, 108, 100,   // 1930-1939
            85, 104,  96, 116, 101,  92, 112,  97,  89, 108,   // 1940-1949
            100,  85, 105,  96, 109, 101,  93, 112,  97,  89,   // 1950-1959
            109,  93, 113, 105,  90, 109, 101,  86, 106,  97,   // 1960-1969
            89, 102,  94, 113, 105,  90, 110, 101,  86, 106,   // 1970-1979
            98, 110, 102,  94, 114,  98,  90, 110,  95,  86,   // 1980-1989
            106,  91, 111, 102,  94, 107,  99,  90, 103,  95,   // 1990-1999
            115, 106,  91, 111, 103,  87, 107,  99,  84, 103,   // 2000-2009
            95, 115, 100,  91, 111,  96,  88, 107,  92, 112,   // 2010-2019
            104,  95, 108, 100,  92, 111,  96,  88, 108,  92,   // 2020-2029
            112, 104,  89, 108, 100,  85, 105,  96, 116, 101,   // 2030-2039
            93, 112,  97,  89, 109, 100,  85, 105,  97, 109,   // 2040-2049
            101,  93, 113,  97,  89, 109,  94, 113, 105,  90,   // 2050-2059
            110, 101,  86, 106,  98,  89, 102,  94, 114, 105,   // 2060-2069
            90, 110, 102,  86, 106,  98, 111, 102,  94, 114,   // 2070-2079
            99,  90, 110,  95,  87, 106,  91, 111, 103,  94,   // 2080-2089
            107,  99,  91, 103,  95, 115, 107,  91, 111, 103,   // 2090-2099
            88, 108, 100,  85, 105,  96, 109, 101,  93, 112,   // 2100-2109
            97,  89, 109,  93, 113, 105,  90, 109, 101,  86,   // 2110-2119
            106,  97,  89, 102,  94, 113, 105,  90, 110, 101,   // 2120-2129
            86, 106,  98, 110, 102,  94, 114,  98,  90, 110,   // 2130-2139
            95,  86, 106,  91, 111, 102,  94, 107,  99,  90,   // 2140-2149
            103,  95, 115, 106,  91, 111, 103,  87, 107,  99,   // 2150-2159
            84, 103,  95, 115, 100,  91, 111,  96,  88, 107,   // 2160-2169
            92, 112, 104,  95, 108, 100,  92, 111,  96,  88,   // 2170-2179
            108,  92, 112, 104,  89, 108, 100,  85, 105,  96,   // 2180-2189
            116, 101,  93, 112,  97,  89, 109, 100,  85, 105    // 2190-2199
         };

         /// <summary>
         /// Returns true if the weekday is part of the
         /// weekend for the given market.
         /// </summary>
         /// <param name="w"></param>
         /// <returns></returns>
         public override bool isWeekend(DayOfWeek w) { return w is DayOfWeek.Saturday or DayOfWeek.Sunday; }

         /// <summary>
         /// Expressed relative to first day of year
         /// </summary>
         /// <param name="y"></param>
         /// <returns></returns>
         public int easterMonday(int y)
         {
            return y is < 1901 or > 2199 ? 0 : EasterMonday[y - 1901];
         }
      }

      /// <summary>
      /// This class provides the means of determining the Orthodox
      /// Easter Monday for a given year, as well as specifying
      /// Saturdays and Sundays as weekend days.
      /// </summary>
      public abstract class OrthodoxImpl : CalendarImpl
      {
         private static readonly int[] EasterMonday =
         {
            105, 118, 110, 102, 121, 106, 126, 118, 102,   // 1901-1909
            122, 114,  99, 118, 110,  95, 115, 106, 126, 111,   // 1910-1919
            103, 122, 107,  99, 119, 110, 123, 115, 107, 126,   // 1920-1929
            111, 103, 123, 107,  99, 119, 104, 123, 115, 100,   // 1930-1939
            120, 111,  96, 116, 108, 127, 112, 104, 124, 115,   // 1940-1949
            100, 120, 112,  96, 116, 108, 128, 112, 104, 124,   // 1950-1959
            109, 100, 120, 105, 125, 116, 101, 121, 113, 104,   // 1960-1969
            117, 109, 101, 120, 105, 125, 117, 101, 121, 113,   // 1970-1979
            98, 117, 109, 129, 114, 105, 125, 110, 102, 121,   // 1980-1989
            106,  98, 118, 109, 122, 114, 106, 118, 110, 102,   // 1990-1999
            122, 106, 126, 118, 103, 122, 114,  99, 119, 110,   // 2000-2009
            95, 115, 107, 126, 111, 103, 123, 107,  99, 119,   // 2010-2019
            111, 123, 115, 107, 127, 111, 103, 123, 108,  99,   // 2020-2029
            119, 104, 124, 115, 100, 120, 112,  96, 116, 108,   // 2030-2039
            128, 112, 104, 124, 116, 100, 120, 112,  97, 116,   // 2040-2049
            108, 128, 113, 104, 124, 109, 101, 120, 105, 125,   // 2050-2059
            117, 101, 121, 113, 105, 117, 109, 101, 121, 105,   // 2060-2069
            125, 110, 102, 121, 113,  98, 118, 109, 129, 114,   // 2070-2079
            106, 125, 110, 102, 122, 106,  98, 118, 110, 122,   // 2080-2089
            114,  99, 119, 110, 102, 115, 107, 126, 118, 103,   // 2090-2099
            123, 115, 100, 120, 112,  96, 116, 108, 128, 112,   // 2100-2109
            104, 124, 109, 100, 120, 105, 125, 116, 108, 121,   // 2110-2119
            113, 104, 124, 109, 101, 120, 105, 125, 117, 101,   // 2120-2129
            121, 113,  98, 117, 109, 129, 114, 105, 125, 110,   // 2130-2139
            102, 121, 113,  98, 118, 109, 129, 114, 106, 125,   // 2140-2149
            110, 102, 122, 106, 126, 118, 103, 122, 114,  99,   // 2150-2159
            119, 110, 102, 115, 107, 126, 111, 103, 123, 114,   // 2160-2169
            99, 119, 111, 130, 115, 107, 127, 111, 103, 123,   // 2170-2179
            108,  99, 119, 104, 124, 115, 100, 120, 112, 103,   // 2180-2189
            116, 108, 128, 119, 104, 124, 116, 100, 120, 112    // 2190-2199
         };

         /// Returns true if the weekday is part of the
         /// weekend for the given market.
         public override bool isWeekend(DayOfWeek w) { return w is DayOfWeek.Saturday or DayOfWeek.Sunday; }

         /// <summary>
         /// expressed relative to first day of year
         /// </summary>
         /// <param name="y"></param>
         /// <returns></returns>
         public int easterMonday(int y)
         {
            return y is < 1901 or > 2199 ? 0 : EasterMonday[y - 1901];
         }
      }

      public bool isStartOfMonth(Date d)
      {
         return d <= startOfMonth(d);
      }

      public Date startOfMonth(Date d)
      {
         return adjust(Date.startOfMonth(d), BusinessDayConvention.Following);
      }

      // Operators
      public static bool operator ==(Calendar c1, Calendar c2)
      {
         // If both are null, or both are same instance, return true.
         if (System.Object.ReferenceEquals(c1, c2))
         {
            return true;
         }

         // If one is null, but not both, return false.
         if (((object)c1 == null) || ((object)c2 == null))
         {
            return false;
         }

         return (c1.empty() && c2.empty())
                || (!c1.empty() && !c2.empty() && c1.name() == c2.name());
      }

      public static bool operator !=(Calendar c1, Calendar c2)
      {
         return !(c1 == c2);
      }
      public override bool Equals(object o) { return (this == (Calendar)o); }
      public override int GetHashCode() { return 0; }
   }
}
