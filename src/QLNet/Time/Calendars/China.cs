/*
 Copyright (C) 2008-2022 Andrea Maggiulli (a.maggiulli@gmail.com)
 Copyright (C) 2008 Alessandro Duci
 Copyright (C) 2008, 2009 Siarhei Novik (snovik@gmail.com)

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
   //! Chinese calendar
   /*! Holidays:
       <ul>
       <li>Saturdays</li>
       <li>Sundays</li>
       <li>New Year's day, January 1st (possibly followed by one or
           two more holidays)</li>
       <li>Labour Day, first week in May</li>
       <li>National Day, one week from October 1st</li>
       </ul>

       Other holidays for which no rule is given (data available for
       2004-2019 only):
       <ul>
       <li>Chinese New Year</li>
       <li>Ching Ming Festival</li>
       <li>Tuen Ng Festival</li>
       <li>Mid-Autumn Festival</li>
       <li>70th anniversary of the victory of anti-Japaneses war</li>
       </ul>

       SSE data from <http://www.sse.com.cn/>
       IB data from <http://www.chinamoney.com.cn/>

       \ingroup calendars
   */
   public class China : Calendar
   {
      public enum Market
      {
         SSE,    //!< Shanghai stock exchange
         IB      //!< Interbank calendar
      }

      public China(Market market = Market.SSE)
      {

         // all calendar instances on the same market share the same implementation instance
         switch (market)
         {
            case Market.SSE:
               _impl = SseImpl.Singleton;
               break;
            case Market.IB:
               _impl = IbImpl.Singleton;
               break;
            default:
               Utils.QL_FAIL("unknown market");
               break;
         }
      }

      private class SseImpl : CalendarImpl
      {
         public static readonly SseImpl Singleton = new();
         private SseImpl() { }
         public override string name() { return "Shanghai stock exchange"; }

         public override bool isWeekend(DayOfWeek w)
         {
            return w == DayOfWeek.Saturday || w == DayOfWeek.Sunday;
         }

         public override bool isBusinessDay(Date date)
         {
            var w = date.DayOfWeek;
            var d = date.Day;
            var m = (Month)date.Month;
            var y = date.Year;

            if (isWeekend(w)
                // New Year's Day
                || (d == 1 && m == Month.January)
                || (y == 2005 && d == 3 && m == Month.January)
                || (y == 2006 && (d == 2 || d == 3) && m == Month.January)
                || (y == 2007 && d <= 3 && m == Month.January)
                || (y == 2007 && d == 31 && m == Month.December)
                || (y == 2009 && d == 2 && m == Month.January)
                || (y == 2011 && d == 3 && m == Month.January)
                || (y == 2012 && (d == 2 || d == 3) && m == Month.January)
                || (y == 2013 && d <= 3 && m == Month.January)
                || (y == 2014 && d == 1 && m == Month.January)
                || (y == 2015 && d <= 3 && m == Month.January)
                || (y == 2017 && d == 2 && m == Month.January)
                || (y == 2018 && d == 1 && m == Month.January)
                || (y == 2018 && d == 31 && m == Month.December)
                || (y == 2019 && d == 1 && m == Month.January)
                || (y == 2020 && d == 1 && m == Month.January)
                || (y == 2021 && d == 1 && m == Month.January)
                || (y == 2022 && d == 3 && m == Month.January)
                || (y == 2023 && d == 2 && m == Month.January)
                // Chinese New Year
                || (y == 2004 && d >= 19 && d <= 28 && m == Month.January)
                || (y == 2005 && d >= 7 && d <= 15 && m == Month.February)
                || (y == 2006 && ((d >= 26 && m == Month.January) ||
                                  (d <= 3 && m == Month.February)))
                || (y == 2007 && d >= 17 && d <= 25 && m == Month.February)
                || (y == 2008 && d >= 6 && d <= 12 && m == Month.February)
                || (y == 2009 && d >= 26 && d <= 30 && m == Month.January)
                || (y == 2010 && d >= 15 && d <= 19 && m == Month.February)
                || (y == 2011 && d >= 2 && d <= 8 && m == Month.February)
                || (y == 2012 && d >= 23 && d <= 28 && m == Month.January)
                || (y == 2013 && d >= 11 && d <= 15 && m == Month.February)
                || (y == 2014 && d >= 31 && m == Month.January)
                || (y == 2014 && d <= 6 && m == Month.February)
                || (y == 2015 && d >= 18 && d <= 24 && m == Month.February)
                || (y == 2016 && d >= 8 && d <= 12 && m == Month.February)
                || (y == 2017 && ((d >= 27 && m == Month.January) ||
                                  (d <= 2 && m == Month.February)))
                || (y == 2018 && (d >= 15 && d <= 21 && m == Month.February))
                || (y == 2019 && d >= 4 && d <= 8 && m == Month.February)
                || (y == 2020 && (d == 24 || (d >= 27 && d <= 31)) && m == Month.January)
                || (y == 2021 && (d == 11 || d == 12 || d == 15 || d == 16 || d == 17) && m == Month.February)
                || (y == 2022 && ((d == 31 && m == Month.January) || (d <= 4 && m == Month.February)))
                || (y == 2023 && d >= 23 && d <= 27 && m == Month.January)
                // Ching Ming Festival
                || (y <= 2008 && d == 4 && m == Month.April)
                || (y == 2009 && d == 6 && m == Month.April)
                || (y == 2010 && d == 5 && m == Month.April)
                || (y == 2011 && d >= 3 && d <= 5 && m == Month.April)
                || (y == 2012 && d >= 2 && d <= 4 && m == Month.April)
                || (y == 2013 && d >= 4 && d <= 5 && m == Month.April)
                || (y == 2014 && d == 7 && m == Month.April)
                || (y == 2015 && d >= 5 && d <= 6 && m == Month.April)
                || (y == 2016 && d == 4 && m == Month.April)
                || (y == 2017 && d >= 3 && d <= 4 && m == Month.April)
                || (y == 2018 && d >= 5 && d <= 6 && m == Month.April)
                || (y == 2019 && d == 5 && m == Month.April)
                || (y == 2020 && d == 6 && m == Month.April)
                || (y == 2021 && d == 5 && m == Month.April)
                || (y == 2022 && d >= 4 && d <= 5 && m == Month.April)
                || (y == 2023 && d == 5 && m == Month.April)
                // Labor Day
                || (y <= 2007 && d >= 1 && d <= 7 && m == Month.May)
                || (y == 2008 && d >= 1 && d <= 2 && m == Month.May)
                || (y == 2009 && d == 1 && m == Month.May)
                || (y == 2010 && d == 3 && m == Month.May)
                || (y == 2011 && d == 2 && m == Month.May)
                || (y == 2012 && ((d == 30 && m == Month.April) ||
                                  (d == 1 && m == Month.May)))
                || (y == 2013 && ((d >= 29 && m == Month.April) ||
                                  (d == 1 && m == Month.May)))
                || (y == 2014 && d >= 1 && d <= 3 && m == Month.May)
                || (y == 2015 && d == 1 && m == Month.May)
                || (y == 2016 && d >= 1 && d <= 2 && m == Month.May)
                || (y == 2017 && d == 1 && m == Month.May)
                || (y == 2018 && ((d == 30 && m == Month.April) || (d == 1 && m == Month.May)))
                || (y == 2019 && d >= 1 && d <= 3 && m == Month.May)
                || (y == 2020 && (d == 1 || d == 4 || d == 5) && m == Month.May)
                || (y == 2021 && (d == 3 || d == 4 || d == 5) && m == Month.May)
                || (y == 2022 && d >= 2 && d <= 4 && m == Month.May)
                || (y == 2023 && d >= 1 && d <= 3 && m == Month.May)
                // Tuen Ng Festival
                || (y <= 2008 && d == 9 && m == Month.June)
                || (y == 2009 && (d == 28 || d == 29) && m == Month.May)
                || (y == 2010 && d >= 14 && d <= 16 && m == Month.June)
                || (y == 2011 && d >= 4 && d <= 6 && m == Month.June)
                || (y == 2012 && d >= 22 && d <= 24 && m == Month.June)
                || (y == 2013 && d >= 10 && d <= 12 && m == Month.June)
                || (y == 2014 && d == 2 && m == Month.June)
                || (y == 2015 && d == 22 && m == Month.June)
                || (y == 2016 && d >= 9 && d <= 10 && m == Month.June)
                || (y == 2017 && d >= 29 && d <= 30 && m == Month.May)
                || (y == 2018 && d == 18 && m == Month.June)
                || (y == 2019 && d == 7 && m == Month.June)
                || (y == 2020 && d >= 25 && d <= 26 && m == Month.June)
                || (y == 2021 && d == 14 && m == Month.June)
                || (y == 2022 && d == 3 && m == Month.June)
                || (y == 2023 && d >= 22 && d <= 23 && m == Month.June)
                // Mid-Autumn Festival
                || (y <= 2008 && d == 15 && m == Month.September)
                || (y == 2010 && d >= 22 && d <= 24 && m == Month.September)
                || (y == 2011 && d >= 10 && d <= 12 && m == Month.September)
                || (y == 2012 && d == 30 && m == Month.September)
                || (y == 2013 && d >= 19 && d <= 20 && m == Month.September)
                || (y == 2014 && d == 8 && m == Month.September)
                || (y == 2015 && d == 27 && m == Month.September)
                || (y == 2016 && d >= 15 && d <= 16 && m == Month.September)
                || (y == 2018 && d == 24 && m == Month.September)
                || (y == 2019 && d == 13 && m == Month.September)
                || (y == 2021 && (d == 20 || d == 21) && m == Month.September)
                || (y == 2022 && d == 12 && m == Month.September)
                || (y == 2023 && d == 29 && m == Month.September)
                // National Day
                || (y <= 2007 && d >= 1 && d <= 7 && m == Month.October)
                || (y == 2008 && ((d >= 29 && m == Month.September) ||
                                  (d <= 3 && m == Month.October)))
                || (y == 2009 && d >= 1 && d <= 8 && m == Month.October)
                || (y == 2010 && d >= 1 && d <= 7 && m == Month.October)
                || (y == 2011 && d >= 1 && d <= 7 && m == Month.October)
                || (y == 2012 && d >= 1 && d <= 7 && m == Month.October)
                || (y == 2013 && d >= 1 && d <= 7 && m == Month.October)
                || (y == 2014 && d >= 1 && d <= 7 && m == Month.October)
                || (y == 2015 && d >= 1 && d <= 7 && m == Month.October)
                || (y == 2016 && d >= 3 && d <= 7 && m == Month.October)
                || (y == 2017 && d >= 2 && d <= 6 && m == Month.October)
                || (y == 2018 && d >= 1 && d <= 5 && m == Month.October)
                || (y == 2019 && d >= 1 && d <= 7 && m == Month.October)
                || (y == 2020 && d >= 1 && d <= 2 && m == Month.October)
                || (y == 2020 && d >= 5 && d <= 8 && m == Month.October)
                || (y == 2021 && (d == 1 || d == 4 || d == 5 || d == 6 || d == 7) && m == Month.October)
                || (y == 2022 && d >= 3 && d <= 7 && m == Month.October)
                || (y == 2023 && d >= 2 && d <= 6 && m == Month.October)
                // 70th anniversary of the victory of anti-Japaneses war
                || (y == 2015 && d >= 3 && d <= 4 && m == Month.September)
               )
               return false;
            return true;

         }
      }

      private class IbImpl : CalendarImpl
      {
         public static readonly IbImpl Singleton = new();
         private IbImpl()
         {
            sseImpl = new China(Market.SSE);
         }
         private readonly Calendar sseImpl;
         public override string name() { return "China inter bank market"; }
         public override bool isWeekend(DayOfWeek w) { return w == DayOfWeek.Saturday || w == DayOfWeek.Sunday; }
         public override bool isBusinessDay(Date date)
         {

            var working_weekends = new List<Date>
            {
               // 2005
               new Date(5, Month.February, 2005),
               new Date(6, Month.February, 2005),
               new Date(30, Month.April, 2005),
               new Date(8, Month.May, 2005),
               new Date(8, Month.October, 2005),
               new Date(9, Month.October, 2005),
               new Date(31, Month.December, 2005),
               //2006
               new Date(28, Month.January, 2006),
               new Date(29, Month.April, 2006),
               new Date(30, Month.April, 2006),
               new Date(30, Month.September, 2006),
               new Date(30, Month.December, 2006),
               new Date(31, Month.December, 2006),
               // 2007
               new Date(17, Month.February, 2007),
               new Date(25, Month.February, 2007),
               new Date(28, Month.April, 2007),
               new Date(29, Month.April, 2007),
               new Date(29, Month.September, 2007),
               new Date(30, Month.September, 2007),
               new Date(29, Month.December, 2007),
               // 2008
               new Date(2, Month.February, 2008),
               new Date(3, Month.February, 2008),
               new Date(4, Month.May, 2008),
               new Date(27, Month.September, 2008),
               new Date(28, Month.September, 2008),
               // 2009
               new Date(4, Month.January, 2009),
               new Date(24, Month.January, 2009),
               new Date(1, Month.February, 2009),
               new Date(31, Month.May, 2009),
               new Date(27, Month.September, 2009),
               new Date(10, Month.October, 2009),
               // 2010
               new Date(20, Month.February, 2010),
               new Date(21, Month.February, 2010),
               new Date(12, Month.June, 2010),
               new Date(13, Month.June, 2010),
               new Date(19, Month.September, 2010),
               new Date(25, Month.September, 2010),
               new Date(26, Month.September, 2010),
               new Date(9, Month.October, 2010),
               // 2011
               new Date(30, Month.January, 2011),
               new Date(12, Month.February, 2011),
               new Date(2, Month.April, 2011),
               new Date(8, Month.October, 2011),
               new Date(9, Month.October, 2011),
               new Date(31, Month.December, 2011),
               // 2012
               new Date(21, Month.January, 2012),
               new Date(29, Month.January, 2012),
               new Date(31, Month.March, 2012),
               new Date(1, Month.April, 2012),
               new Date(28, Month.April, 2012),
               new Date(29, Month.September, 2012),
               // 2013
               new Date(5, Month.January, 2013),
               new Date(6, Month.January, 2013),
               new Date(16, Month.February, 2013),
               new Date(17, Month.February, 2013),
               new Date(7, Month.April, 2013),
               new Date(27, Month.April, 2013),
               new Date(28, Month.April, 2013),
               new Date(8, Month.June, 2013),
               new Date(9, Month.June, 2013),
               new Date(22, Month.September, 2013),
               new Date(29, Month.September, 2013),
               new Date(12, Month.October, 2013),
               // 2014
               new Date(26, Month.January, 2014),
               new Date(8, Month.February, 2014),
               new Date(4, Month.May, 2014),
               new Date(28, Month.September, 2014),
               new Date(11, Month.October, 2014),
               // 2015
               new Date(4, Month.January, 2015),
               new Date(15, Month.February, 2015),
               new Date(28, Month.February, 2015),
               new Date(6, Month.September, 2015),
               new Date(10, Month.October, 2015),
               // 2016
               new Date(6, Month.February, 2016),
               new Date(14, Month.February, 2016),
               new Date(12, Month.June, 2016),
               new Date(18, Month.September, 2016),
               new Date(8, Month.October, 2016),
               new Date(9, Month.October, 2016),
               // 2017
               new Date(22, Month.January, 2017),
               new Date(4, Month.February, 2017),
               new Date(1, Month.April, 2017),
               new Date(27, Month.May, 2017),
               new Date(30, Month.September, 2017),
               // 2018
               new Date(11, Month.February, 2018),
               new Date(24, Month.February, 2018),
               new Date(8, Month.April, 2018),
               new Date(28, Month.April, 2018),
               new Date(29, Month.September, 2018),
               new Date(30, Month.September, 2018),
               new Date(29, Month.December, 2018),
               // 2019
               new Date(2, Month.February, 2019),
               new Date(3, Month.February, 2019),
               new Date(28, Month.April, 2019),
               new Date(5, Month.May, 2019),
               new Date(29, Month.September, 2019),
               new Date(12, Month.October, 2019),
               // 2020
               new Date(19, Month.January, 2020),
               new Date(26, Month.April, 2020),
               new Date(9, Month.May, 2020),
               new Date(28, Month.June, 2020),
               new Date(27, Month.September, 2020),
               new Date(10, Month.October, 2020),
               // 2021
               new Date(7, Month.February, 2021),
               new Date(20, Month.February, 2021),
               new Date(25, Month.April, 2021),
               new Date(8, Month.May, 2021),
               new Date(18, Month.September, 2021),
               new Date(26, Month.September, 2021),
               new Date(9, Month.October, 2021),
               // 2022
               new Date(29, Month.January, 2022),
               new Date(30, Month.January, 2022),
               new Date(2, Month.April, 2022),
               new Date(24, Month.April, 2022),
               new Date(7, Month.May, 2022),
               new Date(8, Month.October, 2022),
               new Date(9, Month.October, 2022),
               // 2023
               new Date(28, Month.January, 2023),
               new Date(29, Month.January, 2023),
               new Date(23, Month.April, 2023),
               new Date(6, Month.May, 2023),
               new Date(25, Month.June, 2023),
               new Date(7, Month.October, 2023),
               new Date(8, Month.October, 2023)
            };

            // If it is already a SSE business day, it must be a IB business day
            return sseImpl.isBusinessDay(date) || working_weekends.Contains(date);
         }
      }

   }
}

