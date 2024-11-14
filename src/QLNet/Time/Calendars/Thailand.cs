/*
 Copyright (C) 2008-2024 Andrea Maggiulli (a.maggiulli@gmail.com)

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

      private class Impl : WesternImpl
      {
         public static readonly Impl Singleton = new();
         private Impl() { }

         public override string name() { return "Thailand stock exchange"; }
         public override bool isBusinessDay(Date date)
         {
            var w = date.DayOfWeek;
            var d = date.Day;
            var m = (Month)date.Month;
            var y = date.Year;

            if (isWeekend(w)
                // New Year's Day
                || ((d == 1 || (d == 3 && w == DayOfWeek.Monday)) && m == Month.January)
                // Chakri Memorial Day
                || ((d == 6 || ((d == 7 || d == 8) && w == DayOfWeek.Monday)) && m == Month.April)
                // Songkran Festival (was cancelled in 2020 due to the Covid-19 Pandamic)
                || ((d == 13 || d == 14 || d == 15) && m == Month.April && y != 2020)
                // Substitution Songkran Festival, usually not more than 5 days in total (was cancelled
                // in 2020 due to the Covid-19 Pandamic)
                || (d == 16 && (w == DayOfWeek.Monday || w == DayOfWeek.Tuesday) && m == Month.April && y != 2020)
                // Labor Day
                || ((d == 1 || ((d == 2 || d == 3) && w == DayOfWeek.Monday)) && m == Month.May)
                // Coronation Day
                || ((d == 4 || ((d == 5 || d == 6) && w == DayOfWeek.Monday)) && m == Month.May && y >= 2019)
                // H.M.Queen Suthida Bajrasudhabimalalakshana’s Birthday
                || ((d == 03 || ((d == 04 || d == 05) && w == DayOfWeek.Monday)) && m == Month.June && y >= 2019)
                // H.M. King Maha Vajiralongkorn Phra Vajiraklaochaoyuhua’s Birthday
                || ((d == 28 || ((d == 29 || d == 30) && w == DayOfWeek.Monday)) && m == Month.July && y >= 2017)
                // 	​H.M. Queen Sirikit The Queen Mother’s Birthday / Mother’s Day
                || ((d == 12 || ((d == 13 || d == 14) && w == DayOfWeek.Monday)) && m == Month.August)
                // H.M. King Bhumibol Adulyadej The Great Memorial Day
                || ((d == 13 || ((d == 14 || d == 15) && w == DayOfWeek.Monday)) && m == Month.October && y >= 2017)
                // Chulalongkorn Day
                || ((d == 23 || ((d == 24 || d == 25) && w == DayOfWeek.Monday)) && m == Month.October && y != 2021)  // Moved 2021, see below
                // H.M. King Bhumibol Adulyadej The Great’s Birthday/ National Day / Father’s Day
                || ((d == 5 || ((d == 6 || d == 7) && w == DayOfWeek.Monday)) && m == Month.December)
                // Constitution Day
                || ((d == 10 || ((d == 11 || d == 12) && w == DayOfWeek.Monday)) && m == Month.December)
                // New Year’s Eve
                || ((d == 31 && m == Month.December) || (d == 2 && w == DayOfWeek.Monday && m == Month.January && y != 2024))  // Moved 2024
                )
               return false;

            if ((y == 2000) &&
                ((d == 21 && m == Month.February)  // Makha Bucha Day (Substitution Day)
                 || (d == 5 && m == Month.May)       // Coronation Day
                 || (d == 17 && m == Month.May)       // Wisakha Bucha Day
                 || (d == 17 && m == Month.July)      // Buddhist Lent Day
                 || (d == 23 && m == Month.October)   // Chulalongkorn Day
                    ))
               return false;

            if ((y == 2001) &&
                ((d == 8 && m == Month.February) // Makha Bucha Day
                 || (d == 7 && m == Month.May)      // Wisakha Bucha Day
                 || (d == 8 && m == Month.May)      // Coronation Day (Substitution Day)
                 || (d == 6 && m == Month.July)     // Buddhist Lent Day
                 || (d == 23 && m == Month.October) // Chulalongkorn Day
                    ))
               return false;

            // 2002, 2003 and 2004 are missing

            if ((y == 2005) &&
                ((d == 23 && m == Month.February) // Makha Bucha Day
                 || (d == 5 && m == Month.May)       // Coronation Day
                 || (d == 23 && m == Month.May)      // Wisakha Bucha Day (Substitution Day for Sunday 22 May)
                 || (d == 1 && m == Month.July)      // Mid Year Closing Day
                 || (d == 22 && m == Month.July)     // Buddhist Lent Day
                 || (d == 24 && m == Month.October)  // Chulalongkorn Day (Substitution Day for Sunday 23 October)
                    ))
               return false;

            if ((y == 2006) &&
                ((d == 13 && m == Month.February) // Makha Bucha Day
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
                ((d == 5 && m == Month.March)     // Makha Bucha Day (Substitution Day for Saturday 3 March)
                 || (d == 7 && m == Month.May)       // Coronation Day (Substitution Day for Saturday 5 May)
                 || (d == 31 && m == Month.May)      // Wisakha Bucha Day
                 || (d == 30 && m == Month.July)     // Asarnha Bucha Day (Substitution Day for Sunday 29 July)
                 || (d == 23 && m == Month.October)  // Chulalongkorn Day
                 || (d == 24 && m == Month.December) // Special Holiday
                    ))
               return false;

            if ((y == 2008) &&
                ((d == 21 && m == Month.February) // Makha Bucha Day
                 || (d == 5 && m == Month.May)       // Coronation Day
                 || (d == 19 && m == Month.May)      // Wisakha Bucha Day
                 || (d == 1 && m == Month.July)      // Mid Year Closing Day
                 || (d == 17 && m == Month.July)     // Asarnha Bucha Day
                 || (d == 23 && m == Month.October)  // Chulalongkorn Day
                   ))
               return false;

            if ((y == 2009) &&
                ((d == 2 && m == Month.January)  // Special Holiday
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
                ((d == 1 && m == Month.March)    // Substitution for Makha Bucha Day(Sunday 28 February)
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
                ((d == 18 && m == Month.February) // Makha Bucha Day
                 || (d == 5 && m == Month.May)       // Coronation Day
                 || (d == 16 && m == Month.May)      // Special Holiday
                 || (d == 17 && m == Month.May)      // Wisakha Bucha Day
                 || (d == 1 && m == Month.July)      // Mid Year Closing Day
                 || (d == 15 && m == Month.July)     // Asarnha Bucha Day
                 || (d == 24 && m == Month.October)  // Substitution for Chulalongkorn Day(Sunday 23 October)
                   ))
               return false;

            if ((y == 2012) &&
                ((d == 3 && m == Month.January)  // Special Holiday
                 || (d == 7 && m == Month.March)    // Makha Bucha Day 2/
                 || (d == 9 && m == Month.April)    // Special Holiday
                 || (d == 7 && m == Month.May)      // Substitution for Coronation Day(Saturday 5 May)
                 || (d == 4 && m == Month.June)     // Wisakha Bucha Day
                 || (d == 2 && m == Month.August)   // Asarnha Bucha Day
                 || (d == 23 && m == Month.October) // Chulalongkorn Day
                    ))
               return false;

            if ((y == 2013) &&
                ((d == 25 && m == Month.February) // Makha Bucha Day
                 || (d == 6 && m == Month.May)       // Substitution for Coronation Day(Sunday 5 May)
                 || (d == 24 && m == Month.May)      // Wisakha Bucha Day
                 || (d == 1 && m == Month.July)      // Mid Year Closing Day
                 || (d == 22 && m == Month.July)     // Asarnha Bucha Day 2/
                 || (d == 23 && m == Month.October)  // Chulalongkorn Day
                 || (d == 30 && m == Month.December) // Special Holiday
                    ))
               return false;

            if ((y == 2014) &&
                ((d == 14 && m == Month.February) // Makha Bucha Day
                 || (d == 5 && m == Month.May)       // Coronation Day
                 || (d == 13 && m == Month.May)      // Wisakha Bucha Day
                 || (d == 1 && m == Month.July)      // Mid Year Closing Day
                 || (d == 11 && m == Month.July)     // Asarnha Bucha Day 1/
                 || (d == 11 && m == Month.August)   // Special Holiday
                 || (d == 23 && m == Month.October)  // Chulalongkorn Day
                    ))
               return false;

            if ((y == 2015) &&
                ((d == 2 && m == Month.January)  // Special Holiday
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
                ((d == 22 && m == Month.February) // Makha Bucha Day
                 || (d == 5 && m == Month.May)       // Coronation Day
                 || (d == 6 && m == Month.May)       // Special Holiday
                 || (d == 20 && m == Month.May)      // Wisakha Bucha Day
                 || (d == 1 && m == Month.July)      //  Mid Year Closing Day
                 || (d == 18 && m == Month.July)     // Special Holiday
                 || (d == 19 && m == Month.July)     // Asarnha Bucha Day 1/
                 || (d == 24 && m == Month.October)  // Substitution for Chulalongkorn Day (Sunday 23rd October)
                    ))
               return false;

            if ((y == 2017) &&
                ((d == 13 && m == Month.February)  // Makha Bucha Day
                    || (d == 10 && m == Month.May)       // Wisakha Bucha Day
                    || (d == 10 && m == Month.July)      // Asarnha Bucha Day
                    || (d == 23 && m == Month.October)   // Chulalongkorn Day
                    || (d == 26 && m == Month.October)   // Special Holiday
                    ))
               return false;

            if ((y == 2018) &&
                ((d == 1 && m == Month.March)    // Makha Bucha Day
                 || (d == 29 && m == Month.May)     // Wisakha Bucha Day
                 || (d == 27 && m == Month.July)    // Asarnha Bucha Day1
                 || (d == 23 && m == Month.October) // Chulalongkorn Day
                    ))

            if ((y == 2019) && ((d == 19 && m == Month.February) // Makha Bucha Day
                                || (d == 6 && m == Month.May)    // Special Holiday
                                || (d == 20 && m == Month.May)   // Wisakha Bucha Day
                                || (d == 16 && m == Month.July)  // Asarnha Bucha Day
                                ))
            return false;

            if ((y == 2020) && ((d == 10 && m == Month.February)    // Makha Bucha Day
                                || (d == 6 && m == Month.May)       // Wisakha Bucha Day
                                || (d == 6 && m == Month.July)      // Asarnha Bucha Day
                                || (d == 27 && m == Month.July)     // Substitution for Songkran Festival
                                || (d == 4 && m == Month.September) // Substitution for Songkran Festival
                                || (d == 7 && m == Month.September) // Substitution for Songkran Festival
                                || (d == 11 && m == Month.December) // Special Holiday
                                ))
            return false;

            if ((y == 2021) && ((d == 12 && m == Month.February)     // Special Holiday
                                || (d == 26 && m == Month.February)  // Makha Bucha Day
                                || (d == 26 && m == Month.May)       // Wisakha Bucha Day
                                || (d == 26 && m == Month.July)      // Substitution for Asarnha Bucha Day (Saturday 24th July 2021)
                                || (d == 24 && m == Month.September) // Special Holiday
                                || (d == 22 && m == Month.October)   // ​Substitution for Chulalongkorn Day
                                ))
            return false;

            if ((y == 2022) && ((d == 16 && m == Month.February)   // Makha Bucha Day
                                || (d == 16 && m == Month.May)     // Substitution for Wisakha Bucha Day (Sunday 15th May 2022)
                                || (d == 13 && m == Month.July)    // Asarnha Bucha Day
                                || (d == 29 && m == Month.July)    // Additional special holiday (added)
                                || (d == 14 && m == Month.October) // Additional special holiday (added)
                                || (d == 24 && m == Month.October) // ​Substitution for Chulalongkorn Day (Sunday 23rd October 2022)
            ))
               return false;

            if ((y == 2023) && ((d == 6 && m == Month.March)        // Makha Bucha Day
                                || (d == 5 && m == Month.May)       // Additional special holiday (added)
                                || (d == 5 && m == Month.June)      // Substitution for H.M. Queen's birthday and Wisakha Bucha Day (Saturday 3rd June 2022)
                                || (d == 1 && m == Month.August)    // Asarnha Bucha Day
                                || (d == 23 && m == Month.October)  // Chulalongkorn Day
                                || (d == 29 && m == Month.December) // Substitution for New Year’s Eve (Sunday 31st December 2023) (added)
            ))
            return false;

            if ((y == 2024) && ((d == 26 && m == Month.February)    // Substitution for Makha Bucha Day (Saturday 24th February 2024)
                                || (d == 8 && m == Month.April)     // Substitution for Chakri Memorial Day (Saturday 6th April 2024)
                                || (d == 12 && m == Month.April)    // Additional holiday in relation to the Songkran festival
                                || (d == 6 && m == Month.May)       // Substitution for Coronation Day (Saturday 4th May 2024)
                                || (d == 22 && m == Month.May)      // Wisakha Bucha Day
                                || (d == 22 && m == Month.July)     // Substitution for Asarnha Bucha Day (Saturday 20th July 2024)
                                || (d == 23 && m == Month.October)  // Chulalongkorn Day
            ))
               return false;

            return true;
         }
      }
   }
}
