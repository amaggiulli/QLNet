//  Copyright (C) 2008-2018 Andrea Maggiulli (a.maggiulli@gmail.com)
//
//  This file is part of QLNet Project https://github.com/amaggiulli/qlnet
//  QLNet is free software: you can redistribute it and/or modify it
//  under the terms of the QLNet license.  You should have received a
//  copy of the license along with this program; if not, license is
//  available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.
//
//  QLNet is a based on QuantLib, a free-software/open-source library
//  for financial quantitative analysts and developers - http://quantlib.org/
//  The QuantLib license is available online at http://quantlib.org/license.shtml.
//
//  This program is distributed in the hope that it will be useful, but WITHOUT
//  ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
//  FOR A PARTICULAR PURPOSE.  See the license for more details.
using System;

namespace QLNet
{
   /// <summary>
   /// Thailand calendars
   /// <remarks>
   /// Holidays observed by financial institutions (not to be confused with bank holidays in the United Kingdom) are regulated by the Bank of Thailand.
   /// If a holiday fall on a weekend the government will annouce a replacement day (usally the following monday).
   ///
   /// Sometimes the government add one or two extra holidays in a year.
   ///
   /// (data from https://www.bot.or.th/English/FinancialInstitutions/FIholiday/Pages/2018.aspx:
   ///        Fixed holidays
   ///
   /// Saturdays
   /// Sundays
   /// Chakri Memorial Day, April 6th
   /// Songkran holiday, April 13th - 15th
   /// Labour Day, May 1st
   /// H.M.the King's Birthday, July 28th (from 2017)
   /// H.M.the Queen's Birthday, August 12th
   /// The Passing of H.M.the Late King Bhumibol Adulyadej (Rama IX), October 13th (from 2017)
   /// H.M.the Late King Bhumibol Adulyadej's Birthday, December 5th
   /// Constitution Day, December 10th
   /// New Year's Eve, December 31th
   ///
   ///
   /// Other holidays for which no rule is given
   /// (data available for 2000-2018 with some years missing)
   ///
   /// Makha Bucha Day
   /// Wisakha Bucha Day
   /// Buddhist Lent Day(until 2006)
   /// Asarnha Bucha Day(from 2007)
   /// Chulalongkorn Day
   /// Other special holidays
   ///
   /// </remarks>
   /// </summary>
   public class Thailand : Calendar
   {
      public Thailand() : base(Impl.Singleton) { }

      class Impl : Calendar.WesternImpl
      {
         public static readonly Impl Singleton = new Impl();
         private Impl() { }

         public override string name() { return "Thailand stock exchange"; }
         public override bool isBusinessDay(Date date)
         {
            DayOfWeek w = date.DayOfWeek;
            int d = date.Day;
            Month m = (Month)date.Month;
            int y = date.Year;

            if (isWeekend(w)
                // New Year's Day
                || ((d == 1 || (d == 3 && w == DayOfWeek.Monday)) && m == Month.January)
                // Chakri Memorial Day
                || ((d == 6 || ((d == 7 || d == 8) && w == DayOfWeek.Monday)) && m == Month.April)
                // Songkran Festival
                || ((d == 13 || d == 14 || d == 15) && m == Month.April)
                // Songkran Festival obersvence (usually not more then 1 holiday will be replaced)
                || (d == 16 && (w == DayOfWeek.Monday || w == DayOfWeek.Tuesday) && m == Month.April)
                // Labor Day
                || ((d == 1 || ((d == 2 || d == 3) && w == DayOfWeek.Monday)) && m == Month.May)
                // H.M. the King's Birthday
                || ((d == 28 || ((d == 29 || d == 30) && w == DayOfWeek.Monday)) && m == Month.July && y >= 2017)
                // H.M. the Queen's Birthday
                || ((d == 12 || ((d == 13 || d == 14) && w == DayOfWeek.Monday)) && m == Month.August)
                // H.M. King Bhumibol Adulyadej Memorial Day
                || ((d == 3 || ((d == 14 || d == 15) && w == DayOfWeek.Monday)) && m == Month.October && y >= 2017)
                // H.M. King Bhumibol Adulyadej's Birthday
                || ((d == 5 || ((d == 6 || d == 7) && w == DayOfWeek.Monday)) && m == Month.December)
                // Constitution Day
                || ((d == 10 || ((d == 11 || d == 12) && w == DayOfWeek.Monday)) && m == Month.December)
                // New Year’s Eve
                || (d == 31 && m == Month.December)
                // New Year’s Eve Observence
                || ((d == 1 || d == 2) && w == DayOfWeek.Monday && m == Month.January)
               )
               return false;

            if ((y == 2000) &&
                ((d == 21 && m == Month.February)     // Makha Bucha Day (Substitution Day)
                 || (d == 5 && m == Month.May)        // Coronation Day
                 || (d == 17 && m == Month.May)       // Wisakha Bucha Day
                 || (d == 17 && m == Month.July)      // Buddhist Lent Day
                 || (d == 23 && m == Month.October)   // Chulalongkorn Day
                ))
               return false;

            if ((y == 2001) &&
                ((d == 8 && m == Month.February)    // Makha Bucha Day
                 || (d == 7 && m == Month.May)      // Wisakha Bucha Day
                 || (d == 8 && m == Month.May)      // Coronation Day (Substitution Day)
                 || (d == 6 && m == Month.July)     // Buddhist Lent Day
                 || (d == 23 && m == Month.October) // Chulalongkorn Day
                ))
               return false;

            // 2002, 2003 and 2004 are missing

            if ((y == 2005) &&
                ((d == 23 && m == Month.February)    // Makha Bucha Day
                 || (d == 5 && m == Month.May)       // Coronation Day
                 || (d == 23 && m == Month.May)      // Wisakha Bucha Day (Substitution Day for Sunday 22 May)
                 || (d == 1 && m == Month.July)      // Mid Year Closing Day
                 || (d == 22 && m == Month.July)     // Buddhist Lent Day
                 || (d == 24 && m == Month.October)  // Chulalongkorn Day (Substitution Day for Sunday 23 October)
                ))
               return false;

            if ((y == 2006) &&
                ((d == 13 && m == Month.February)   // Makha Bucha Day
                 || (d == 19 && m == Month.April)    // Special Holiday
                 || (d == 5 && m == Month.May)       // Coronation Day
                 || (d == 12 && m == Month.May)      // Wisakha Bucha Day
                 || (d == 12 && m == Month.June)     // Special Holidays (Due to the auspicious occasion of the
                 // celebration of 60th Anniversary of His Majesty's Accession
                 // to the throne. For Bangkok, Samut Prakan, Nonthaburi,
                 // Pathumthani and Nakhon Pathom province)
                 || (d == 13 && m == Month.June)     // Special Holidays (as above)
                 || (d == 11 && m == Month.July)     // Buddhist Lent Day
                 || (d == 23 && m == Month.October)  // Chulalongkorn Day
                ))
               return false;

            if ((y == 2007) &&
                ((d == 5 && m == Month.March)        // Makha Bucha Day (Substitution Day for Saturday 3 March)
                 || (d == 7 && m == Month.May)       // Coronation Day (Substitution Day for Saturday 5 May)
                 || (d == 31 && m == Month.May)      // Wisakha Bucha Day
                 || (d == 30 && m == Month.July)     // Asarnha Bucha Day (Substitution Day for Sunday 29 July)
                 || (d == 23 && m == Month.October)  // Chulalongkorn Day
                 || (d == 24 && m == Month.December) // Special Holiday
                ))
               return false;

            if ((y == 2008) &&
                ((d == 21 && m == Month.February)    // Makha Bucha Day
                 || (d == 5 && m == Month.May)       // Coronation Day
                 || (d == 19 && m == Month.May)      // Wisakha Bucha Day
                 || (d == 1 && m == Month.July)      // Mid Year Closing Day
                 || (d == 17 && m == Month.July)     // Asarnha Bucha Day
                 || (d == 23 && m == Month.October)  // Chulalongkorn Day
                ))
               return false;

            if ((y == 2009) &&
                ((d == 2 && m == Month.January)     // Special Holiday
                 || (d == 9 && m == Month.February) // Makha Bucha Day
                 || (d == 5 && m == Month.May)      // Coronation Day
                 || (d == 8 && m == Month.May)      // Wisakha Bucha Day
                 || (d == 1 && m == Month.July)     // Mid Year Closing Day
                 || (d == 6 && m == Month.July)     // Special Holiday
                 || (d == 7 && m == Month.July)     // Asarnha Bucha Day
                 || (d == 23 && m == Month.October) // Chulalongkorn Day
                ))
               return false;

            if ((y == 2010) &&
                ((d == 1 && m == Month.March)       // Substitution for Makha Bucha Day(Sunday 28 February)
                 || (d == 5 && m == Month.May)      // Coronation Day
                 || (d == 20 && m == Month.May)     // Special Holiday
                 || (d == 21 && m == Month.May)     // Special Holiday
                 || (d == 28 && m == Month.May)     // Wisakha Bucha Day
                 || (d == 1 && m == Month.July)     // Mid Year Closing Day
                 || (d == 26 && m == Month.July)    // Asarnha Bucha Day
                 || (d == 13 && m == Month.August)  // Special Holiday
                 || (d == 25 && m == Month.October) // Substitution for Chulalongkorn Day(Saturday 23 October)
                ))
               return false;

            if ((y == 2011) &&
                ((d == 18 && m == Month.February)    // Makha Bucha Day
                 || (d == 5 && m == Month.May)       // Coronation Day
                 || (d == 16 && m == Month.May)      // Special Holiday
                 || (d == 17 && m == Month.May)      // Wisakha Bucha Day
                 || (d == 1 && m == Month.July)      // Mid Year Closing Day
                 || (d == 15 && m == Month.July)     // Asarnha Bucha Day
                 || (d == 24 && m == Month.October)  // Substitution for Chulalongkorn Day(Sunday 23 October)
                ))
               return false;

            if ((y == 2012) &&
                ((d == 3 && m == Month.January)     // Special Holiday
                 || (d == 7 && m == Month.March)    // Makha Bucha Day 2/
                 || (d == 9 && m == Month.April)    // Special Holiday
                 || (d == 7 && m == Month.May)      // Substitution for Coronation Day(Saturday 5 May)
                 || (d == 4 && m == Month.June)     // Wisakha Bucha Day
                 || (d == 2 && m == Month.August)   // Asarnha Bucha Day
                 || (d == 23 && m == Month.October) // Chulalongkorn Day
                ))
               return false;

            if ((y == 2013) &&
                ((d == 25 && m == Month.February)    // Makha Bucha Day
                 || (d == 6 && m == Month.May)       // Substitution for Coronation Day(Sunday 5 May)
                 || (d == 24 && m == Month.May)      // Wisakha Bucha Day
                 || (d == 1 && m == Month.July)      // Mid Year Closing Day
                 || (d == 22 && m == Month.July)     // Asarnha Bucha Day 2/
                 || (d == 23 && m == Month.October)  // Chulalongkorn Day
                 || (d == 30 && m == Month.December) // Special Holiday
                ))
               return false;

            if ((y == 2014) &&
                ((d == 14 && m == Month.February)    // Makha Bucha Day
                 || (d == 5 && m == Month.May)       // Coronation Day
                 || (d == 13 && m == Month.May)      // Wisakha Bucha Day
                 || (d == 1 && m == Month.July)      // Mid Year Closing Day
                 || (d == 11 && m == Month.July)     // Asarnha Bucha Day 1/
                 || (d == 11 && m == Month.August)   // Special Holiday
                 || (d == 23 && m == Month.October)  // Chulalongkorn Day
                ))
               return false;

            if ((y == 2015) &&
                ((d == 2 && m == Month.January)     // Special Holiday
                 || (d == 4 && m == Month.March)    // Makha Bucha Day
                 || (d == 4 && m == Month.May)      // Special Holiday
                 || (d == 5 && m == Month.May)      // Coronation Day
                 || (d == 1 && m == Month.June)     // Wisakha Bucha Day
                 || (d == 1 && m == Month.July)     // Mid Year Closing Day
                 || (d == 30 && m == Month.July)    // Asarnha Bucha Day 1/
                 || (d == 23 && m == Month.October) // Chulalongkorn Day
                ))
               return false;

            if ((y == 2016) &&
                ((d == 22 && m == Month.February)    // Makha Bucha Day
                 || (d == 5 && m == Month.May)       // Coronation Day
                 || (d == 6 && m == Month.May)       // Special Holiday
                 || (d == 20 && m == Month.May)      // Wisakha Bucha Day
                 || (d == 1 && m == Month.July)      //  Mid Year Closing Day
                 || (d == 18 && m == Month.July)     // Special Holiday
                 || (d == 19 && m == Month.July)     // Asarnha Bucha Day 1/
                 || (d == 24 && m == Month.October)  // Substitution for Chulalongkorn Day (Sunday 23rd October)
                ))
               return false;

            // 2017 is missing

            if ((y == 2018) &&
                ((d == 1 && m == Month.March)       // Makha Bucha Day
                 || (d == 29 && m == Month.May)     // Wisakha Bucha Day
                 || (d == 27 && m == Month.July)    // Asarnha Bucha Day1
                 || (d == 23 && m == Month.October) // Chulalongkorn Day
                ))
               return false;

            return true;

         }
      }
   }
}
