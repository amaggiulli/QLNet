/*
 Copyright (C) 2008-2022 Andrea Maggiulli (a.maggiulli@gmail.com)
 Copyright (C) 2008 Alessandro Duci
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)

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
   //! Turkish calendar
   /*! Holidays for the Istanbul Stock Exchange:
       (data from <https://borsaistanbul.com/en/sayfa/3631/official-holidays>) and
       <https://feiertagskalender.ch/index.php?geo=3539&hl=en>
       <ul>
       <li>Saturdays</li>
       <li>Sundays</li>
       <li>New Year's Day, January 1st</li>
       <li>National Sovereignty and Children’s Day, April 23rd</li>
       <li>Youth and Sports Day, May 19th</li>
       <li>Victory Day, August 30th</li>
       <li>Republic Day, October 29th</li>
       <li>Local Holidays (Kurban, Ramadan - dates need further validation for >= 2024) </li>
       </ul>

       \ingroup calendars
   */
   public class Turkey :  Calendar
   {
      public Turkey() : base(Impl.Singleton) { }

      private class Impl : CalendarImpl
      {
         public static readonly Impl Singleton = new();
         private Impl() { }

         public override string name() { return "Turkey"; }
         public override bool isWeekend(DayOfWeek w)
         {
            return w is DayOfWeek.Saturday or DayOfWeek.Sunday;
         }

         public override bool isBusinessDay(Date date)
         {
            var w = date.DayOfWeek;
            int d = date.Day, dd = date.DayOfYear;
            var m = (Month)date.Month;
            var y = date.Year;

            if (isWeekend(w)
                // New Year's Day
                || (d == 1 && m == Month.January)
                // 23 nisan / National Holiday
                || (d == 23 && m == Month.April)
                // 19 may/ National Holiday
                || (d == 19 && m == Month.May)
                // 15 july / National Holiday (since 2017)
                || (d == 15 && m == Month.July && y >= 2017)
                // 30 aug/ National Holiday
                || (d == 30 && m == Month.August)
                // 29 ekim  National Holiday
                || (d == 29 && m == Month.October))
               return false;

            // Local Holidays
            if (y == 2004)
            {
               // Kurban
               if ((m == Month.February && d <= 4)
                   // Ramadan
                   || (m == Month.November && d >= 14 && d <= 16))
                  return false;
            }
            else if (y == 2005)
            {
               // Kurban
               if ((m == Month.January && d >= 19 && d <= 21)
                   // Ramadan
                   || (m == Month.November && d >= 2 && d <= 5))
                  return false;
            }
            else if (y == 2006)
            {
               // Kurban
               if ((m == Month.January && d >= 10 && d <= 13)
                   // Ramadan
                   || (m == Month.October && d >= 23 && d <= 25)
                   // Kurban
                   || (m == Month.December && d == 31))
                  return false;
            }
            else if (y == 2007)
            {
               // Kurban
               if ((m == Month.January && d <= 3)
                   // Ramadan
                   || (m == Month.October && d >= 12 && d <= 14)
                   // Kurban
                   || (m == Month.December && d >= 20 && d <= 23))
                  return false;
            }
            else if (y == 2008)
            {
               // Ramadan
               if ((m == Month.September && d == 30)
                   || (m == Month.October && d <= 2)
                   // Kurban
                   || (m == Month.December && d >= 8 && d <= 11))
                  return false;
            }
            else if (y == 2009)
            {
               // Ramadan
               if ((m == Month.September && d >= 20 && d <= 22)
                   // Kurban
                   || (m == Month.November && d >= 27 && d <= 30))
                  return false;
            }
            else if (y == 2010)
            {
               // Ramadan
               if ((m == Month.September && d >= 9 && d <= 11)
                   // Kurban
                   || (m == Month.November && d >= 16 && d <= 19))
                  return false;
            }
            else if (y == 2011)
            {
               // not clear from borsainstanbul.com
               if ((m == Month.October && d == 1)
                   || (m == Month.November && d >= 9 && d <= 13))
                  return false;
            }
            else if (y == 2012)
            {
               // Ramadan
               if ((m == Month.August && d >= 18 && d <= 21)
                   // Kurban
                   || (m == Month.October && d >= 24 && d <= 28))
                  return false;
            }
            else if (y == 2013)
            {
               // Ramadan
               if ((m == Month.August && d >= 7 && d <= 10)
                   // Kurban
                   || (m == Month.October && d >= 14 && d <= 18)
                   // additional holiday for Republic Day
                   || (m == Month.October && d == 28))
                  return false;
            }
            else if (y == 2014)
            {
               // Ramadan
               if ((m == Month.July && d >= 27 && d <= 30)
                   // Kurban
                   || (m == Month.October && d >= 4 && d <= 7)
                   // additional holiday for Republic Day
                   || (m == Month.October && d == 29))
                  return false;
            }
            else if (y == 2015)
            {
			      // Ramadan
			      if ((m == Month.July && d >= 17 && d <= 19)
				      // Kurban
				      || (m == Month.October && d >= 24 && d <= 27))
				      return false;
		      }
            else if (y == 2016)
            {
			      // Ramadan
			      if ((m == Month.July && d >= 5 && d <= 7)
				      // Kurban
				      || (m == Month.September && d >= 12 && d <= 15))
				      return false;
		      }
            else if (y == 2017)
            {
			      // Ramadan
			      if ((m == Month.June && d >= 25 && d <= 27)
				      // Kurban
				      || (m == Month.September && d >= 1 && d <= 4))
				      return false;
		      }
            else if (y == 2018)
            {
			      // Ramadan
			      if ((m == Month.June && d >= 15 && d <= 17)
				      // Kurban
				      || (m == Month.August && d >= 21 && d <= 24))
				      return false;
		      }
            else if (y == 2019)
            {
			      // Ramadan
			      if ((m == Month.June && d >= 4 && d <= 6)
				      // Kurban
				      || (m == Month.August && d >= 11 && d <= 14))
				      return false;
		      }
            else if (y == 2020)
            {
			      // Ramadan
			      if ((m == Month.May && d >= 24 && d <= 26)
				      // Kurban
				      || (m == Month.July && d == 31) || (m == Month.August && d >= 1 && d <= 3))
				      return false;
		      }
            else if (y == 2021)
            {
			      // Ramadan
			      if ((m == Month.May && d >= 13 && d <= 15)
				      // Kurban
				      || (m == Month.July && d >= 20 && d <= 23))
				      return false;
		      }
            else if (y == 2022)
            {
			      // Ramadan
			      if ((m == Month.May && d >= 2 && d <= 4)
				      // Kurban
				      || (m == Month.July && d >= 9 && d <= 12))
				      return false;
		      }
            else if (y == 2023)
            {
			      // Ramadan
			      if ((m == Month.April && d >= 21 && d <= 23)
				      // Kurban
                      // July 1 is also a holiday but falls on a Saturday which is already flagged
				      || (m == Month.June && d >= 28 && d <= 30))
				      return false;
		      }
            else if (y == 2024)
            {
		         // Note: Holidays >= 2024 are not yet officially anounced by borsaistanbul.com
		         // and need further validation
			      // Ramadan
			      if ((m == Month.April && d >= 10 && d <= 12)
				      // Kurban
				      || (m == Month.June && d >= 17 && d <= 19))
				      return false;
		      }
            else if (y == 2025)
            {
               // Ramadan
      			if ((m == Month.March && d == 31) || (m == Month.April && d >= 1 && d <= 2)
				   // Kurban
				   || (m == Month.June && d >= 6 && d <= 9))
				   return false;
            }
            else if (y == 2026)
            {
               // Ramadan
			      if ((m == Month.March && d >= 20 && d <= 22)
				      // Kurban
				      || (m == Month.May && d >= 26 && d <= 29))
                  return false;
		      }
            else if (y == 2027)
            {
			      // Ramadan
			      if ((m == Month.March && d >= 10 && d <= 12)
				      // Kurban
				      || (m == Month.May && d >= 16 && d <= 19))
				      return false;
		      }
            else if (y == 2028)
            {
			      // Ramadan
			      if ((m == Month.February && d >= 27 && d <= 29)
				      // Kurban
				      || (m == Month.May && d >= 4 && d <= 7))
				      return false;
		      }
            else if (y == 2029)
            {
			      // Ramadan
			      if ((m == Month.February && d >= 15 && d <= 17)
				      // Kurban
				      || (m == Month.April && d >= 23 && d <= 26))
				      return false;
		      }
            else if (y == 2030)
            {
			      // Ramadan
			      if ((m == Month.February && d >= 5 && d <= 7)
				      // Kurban
				      || (m == Month.April && d >= 13 && d <= 16))
				      return false;
		      }
            else if (y == 2031)
            {
			      // Ramadan
			      if ((m == Month.January && d >= 25 && d <= 27)
				      // Kurban
				      || (m == Month.April && d >= 2 && d <= 5))
				      return false;
		      }
            else if (y == 2032)
            {
               // Ramadan
			      if ((m == Month.January && d >= 14 && d <= 16)
				      // Kurban
				      || (m == Month.March && d >= 21 && d <= 24))
				      return false;
		      }
            else if (y == 2033)
            {
               // Ramadan
			      if ((m == Month.January && d >= 3 && d <= 5) || (m == Month.December && d == 23)
				      // Kurban
				      || (m == Month.March && d >= 11 && d <= 14))
				      return false;
            }
            else if (y == 2034)
            {
               // Ramadan
               if ((m == Month.December && d >= 12 && d <= 14)
                   // Kurban
                   || (m == Month.February && d == 28) || (m == Month.March && d >= 1 && d <= 3))
                  return false;
            }
            return true;
         }
      }
   }
}




