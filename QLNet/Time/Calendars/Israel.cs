//  Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)
//  
//  This file is part of QLNet Project https://github.com/amaggiulli/qlnet
//  QLNet is free software: you can redistribute it and/or modify it
//  under the terms of the QLNet license.  You should have received a
//  copy of the license along with this program; if not, license is  
//  available online at <http://qlnet.sourceforge.net/License.html>.
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
   //! Israel calendar
   /*! Due to the lack of reliable sources, the settlement calendar
       has the same holidays as the Tel Aviv stock-exchange.

       Holidays for the Tel-Aviv Stock Exchange
       (data from <http://www.tase.co.il>):
       <ul>
       <li>Friday</li>
       <li>Saturday</li>
       </ul>
       Other holidays for wich no rule is given
       (data available for 2013-2044 only:)
       <ul>
       <li>Purim, Adar 14th (between Feb 24th & Mar 26th)</li>
       <li>Passover I, Nisan 15th (between Mar 26th & Apr 25th)</li>
       <li>Passover VII, Nisan 21st (between Apr 1st & May 1st)</li>
       <li>Memorial Day, Nisan 27th (between Apr 7th & May 7th)</li>
       <li>Indipendence Day, Iyar 5th (between Apr 15th & May 15th)</li>
       <li>Pentecost (Shavuot), Sivan 6th (between May 15th & June 14th)</li>
       <li>Fast Day</li>
       <li>Jewish New Year, Tishrei 1st & 2nd (between Sep 5th & Oct 5th)</li>
       <li>Yom Kippur, Tishrei 10th (between Sep 14th & Oct 14th)</li>
       <li>Sukkoth, Tishrei 15th (between Sep 19th & Oct 19th)</li>
       <li>Simchat Tora, Tishrei 22nd (between Sep 26th & Oct 26th)</li>
       </ul>


       \ingroup calendars
   */
   public class Israel : Calendar
   {
      public enum Market
      {
         Settlement,  //!< generic settlement calendar
         TASE         //!< Tel-Aviv stock exchange calendar
      }

      public Israel( Market m = Market.Settlement )
         : base()
      {
         // all calendar instances on the same market share the same
         // implementation instance
         switch ( m )
         {
            case Market.Settlement:
               calendar_ = TelAvivImpl.Singleton;
               break;
            case Market.TASE:
               calendar_ = TelAvivImpl.Singleton;
               break;
            default:
               throw new ArgumentException( "Unknown market: " + m );
         }
      }

      class TelAvivImpl : Calendar
      {
         public static readonly TelAvivImpl Singleton = new TelAvivImpl();
         private TelAvivImpl() { }

         public override string name() { return "Tel Aviv stock exchange"; }
         public override bool isWeekend( DayOfWeek w )
         {
            return w == DayOfWeek.Friday || w == DayOfWeek.Saturday;
         }
         public override bool isBusinessDay( Date date )
         {
            DayOfWeek w = date.DayOfWeek;
            int d = date.Day;
            Month m = (Month)date.Month;
            int y = date.Year;

            if ( isWeekend( w )
               //Purim
                || ( d == 24 && m == Month.February && y == 2013 )
                || ( d == 16 && m == Month.March && y == 2014 )
                || ( d == 05 && m == Month.March && y == 2015 )
                || ( d == 24 && m == Month.March && y == 2016 )
                || ( d == 12 && m == Month.March && y == 2017 )
                || ( d == 1 && m == Month.March && y == 2018 )
                || ( d == 21 && m == Month.March && y == 2019 )
                || ( d == 10 && m == Month.March && y == 2020 )
                || ( d == 26 && m == Month.February && y == 2021 )
                || ( d == 17 && m == Month.March && y == 2022 )
                || ( d == 7 && m == Month.March && y == 2023 )
                || ( d == 24 && m == Month.March && y == 2024 )
                || ( d == 14 && m == Month.March && y == 2025 )
                || ( d == 3 && m == Month.March && y == 2026 )
                || ( d == 23 && m == Month.March && y == 2027 )
                || ( d == 12 && m == Month.March && y == 2028 )
                || ( d == 1 && m == Month.March && y == 2029 )
                || ( d == 19 && m == Month.March && y == 2030 )
                || ( d == 9 && m == Month.March && y == 2031 )
                || ( d == 26 && m == Month.February && y == 2032 )
                || ( d == 15 && m == Month.March && y == 2033 )
                || ( d == 5 && m == Month.March && y == 2034 )
                || ( d == 25 && m == Month.March && y == 2035 )
                || ( d == 13 && m == Month.March && y == 2036 )
                || ( d == 1 && m == Month.March && y == 2037 )
                || ( d == 21 && m == Month.March && y == 2038 )
                || ( d == 10 && m == Month.March && y == 2039 )
                || ( d == 28 && m == Month.February && y == 2040 )
                || ( d == 17 && m == Month.March && y == 2041 )
                || ( d == 6 && m == Month.March && y == 2042 )
                || ( d == 26 && m == Month.March && y == 2043 )
                || ( d == 13 && m == Month.March && y == 2044 )
               //Passover I and Passover VII
                || ( ( ( ( d == 25 || d == 26 || d == 31 ) && m == Month.March ) || ( d == 1 && m == Month.April ) ) && y == 2013 )
                || ( ( d == 14 || d == 15 || d == 20 || d == 21 ) && m == Month.April && y == 2014 )
                || ( ( d == 3 || d == 4 || d == 9 || d == 10 ) && m == Month.April && y == 2015 )
                || ( ( d == 22 || d == 23 || d == 28 || d == 29 ) && m == Month.April && y == 2016 )
                || ( ( d == 10 || d == 11 || d == 16 || d == 17 ) && m == Month.April && y == 2017 )
                || ( ( ( d == 31 && m == Month.March ) || ( ( d == 5 || d == 6 ) && m == Month.April ) ) && y == 2018 )
                || ( ( d == 20 || d == 25 || d == 26 ) && m == Month.April && y == 2019 )
                || ( ( d == 8 || d == 9 || d == 14 || d == 15 ) && m == Month.April && y == 2020 )
                || ( ( ( d == 28 && m == Month.March ) || ( d == 3 && m == Month.April ) ) && y == 2021 )
                || ( ( d == 16 || d == 22 ) && m == Month.April && y == 2022 )
                || ( ( d == 6 || d == 12 ) && m == Month.April && y == 2023 )
                || ( ( d == 23 || d == 29 ) && m == Month.April && y == 2024 )
                || ( ( d == 13 || d == 19 ) && m == Month.April && y == 2025 )
                || ( ( d == 2 || d == 8 ) && m == Month.April && y == 2026 )
                || ( ( d == 22 || d == 28 ) && m == Month.April && y == 2027 )
                || ( ( d == 11 || d == 17 ) && m == Month.April && y == 2028 )
                || ( ( ( d == 31 && m == Month.March ) || ( d == 6 && m == Month.April ) ) && y == 2029 )
                || ( ( d == 18 || d == 24 ) && m == Month.April && y == 2030 )
                || ( ( d == 8 || d == 14 ) && m == Month.April && y == 2031 )
                || ( ( ( d == 27 && m == Month.March ) || ( d == 2 && m == Month.April ) ) && y == 2032 )
                || ( ( d == 14 || d == 20 ) && m == Month.April && y == 2033 )
                || ( ( d == 4 || d == 10 ) && m == Month.April && y == 2034 )
                || ( ( d == 24 || d == 30 ) && m == Month.April && y == 2035 )
                || ( ( d == 12 || d == 18 ) && m == Month.April && y == 2036 )
                || ( ( ( d == 31 && m == Month.March ) || ( d == 6 && m == Month.April ) ) && y == 2037 )
                || ( ( d == 20 || d == 26 ) && m == Month.April && y == 2038 )
                || ( ( d == 9 || d == 15 ) && m == Month.April && y == 2039 )
                || ( ( ( d == 29 && m == Month.March ) || ( d == 4 && m == Month.April ) ) && y == 2040 )
                || ( ( d == 16 || d == 22 ) && m == Month.April && y == 2041 )
                || ( ( d == 5 || d == 11 ) && m == Month.April && y == 2042 )
                || ( ( ( d == 25 && m == Month.April ) || ( d == 1 && m == Month.May ) ) && y == 2043 )
                || ( ( d == 12 || d == 18 ) && m == Month.April && y == 2044 )
               //Memorial and Indipendence Day
                || ( ( d == 15 || d == 16 ) && m == Month.April && y == 2013 )
                || ( ( d == 5 || d == 6 ) && m == Month.May && y == 2014 )
                || ( ( d == 22 || d == 23 ) && m == Month.April && y == 2015 )
                || ( ( d == 11 || d == 12 ) && m == Month.May && y == 2016 )
                || ( ( d == 1 || d == 2 ) && m == Month.May && y == 2017 )
                || ( ( d == 18 || d == 19 ) && m == Month.April && y == 2018 )
                || ( ( d == 8 || d == 9 ) && m == Month.May && y == 2019 )
                || ( ( d == 28 || d == 29 ) && m == Month.April && y == 2020 )
                || ( ( d == 14 || d == 15 ) && m == Month.April && y == 2021 )
                || ( ( d == 4 || d == 5 ) && m == Month.May && y == 2022 )
                || ( ( d == 25 || d == 26 ) && m == Month.April && y == 2023 )
                || ( ( d == 13 || d == 14 ) && m == Month.May && y == 2024 )
                || ( ( ( d == 30 && m == Month.April ) || ( d == 1 && m == Month.May ) ) && y == 2025 )
                || ( ( d == 21 || d == 22 ) && m == Month.April && y == 2026 )
                || ( ( d == 11 || d == 12 ) && m == Month.May && y == 2027 )
                || ( ( d == 1 || d == 2 ) && m == Month.May && y == 2028 )
                || ( ( d == 18 || d == 19 ) && m == Month.April && y == 2029 )
                || ( ( d == 7 || d == 8 ) && m == Month.May && y == 2030 )
                || ( ( d == 28 || d == 29 ) && m == Month.April && y == 2031 )
                || ( ( d == 14 || d == 15 ) && m == Month.April && y == 2032 )
                || ( ( d == 3 || d == 4 ) && m == Month.May && y == 2033 )
                || ( ( d == 24 || d == 25 ) && m == Month.April && y == 2034 )
                || ( ( d == 14 || d == 15 ) && m == Month.May && y == 2035 )
                || ( ( ( d == 30 && m == Month.April ) || ( d == 1 && m == Month.May ) ) && y == 2036 )
                || ( ( d == 20 || d == 21 ) && m == Month.April && y == 2037 )
                || ( ( d == 9 || d == 10 ) && m == Month.May && y == 2038 )
                || ( ( d == 27 || d == 28 ) && m == Month.April && y == 2039 )
                || ( ( d == 17 || d == 18 ) && m == Month.April && y == 2040 )
                || ( ( d == 6 || d == 7 ) && m == Month.May && y == 2041 )
                || ( ( d == 23 || d == 24 ) && m == Month.April && y == 2042 )
                || ( ( d == 13 || d == 14 ) && m == Month.May && y == 2043 )
                || ( ( d == 2 || d == 3 ) && m == Month.May && y == 2044 )
               //Pentecost (Shavuot)
                || ( ( d == 14 || d == 15 ) && m == Month.May && y == 2013 )
                || ( ( d == 3 || d == 4 ) && m == Month.June && y == 2014 )
                || ( ( d == 23 || d == 24 ) && m == Month.May && y == 2015 )
                || ( ( d == 11 || d == 12 ) && m == Month.June && y == 2016 )
                || ( ( d == 30 || d == 31 ) && m == Month.May && y == 2017 )
                || ( ( d == 19 || d == 20 ) && m == Month.May && y == 2018 )
                || ( ( d == 8 || d == 9 ) && m == Month.June && y == 2019 )
                || ( ( d == 28 || d == 29 ) && m == Month.May && y == 2020 )
                || ( d == 17 && m == Month.May && y == 2021 )
                || ( d == 5 && m == Month.June && y == 2022 )
                || ( d == 26 && m == Month.May && y == 2023 )
                || ( d == 12 && m == Month.June && y == 2024 )
                || ( d == 2 && m == Month.June && y == 2025 )
                || ( d == 22 && m == Month.May && y == 2026 )
                || ( d == 11 && m == Month.June && y == 2027 )
                || ( d == 31 && m == Month.May && y == 2028 )
                || ( d == 20 && m == Month.May && y == 2029 )
                || ( d == 7 && m == Month.June && y == 2030 )
                || ( d == 28 && m == Month.May && y == 2031 )
                || ( d == 16 && m == Month.May && y == 2032 )
                || ( d == 3 && m == Month.June && y == 2033 )
                || ( d == 24 && m == Month.May && y == 2034 )
                || ( d == 13 && m == Month.June && y == 2035 )
                || ( d == 1 && m == Month.June && y == 2036 )
                || ( d == 20 && m == Month.May && y == 2037 )
                || ( d == 9 && m == Month.June && y == 2038 )
                || ( d == 29 && m == Month.May && y == 2039 )
                || ( d == 18 && m == Month.May && y == 2040 )
                || ( d == 5 && m == Month.June && y == 2041 )
                || ( d == 25 && m == Month.May && y == 2042 )
                || ( d == 14 && m == Month.June && y == 2043 )
                || ( d == 1 && m == Month.June && y == 2044 )
               //Fast Day
                || ( d == 16 && m == Month.July && y == 2013 )
                || ( d == 5 && m == Month.August && y == 2014 )
                || ( d == 26 && m == Month.July && y == 2015 )
                || ( d == 14 && m == Month.August && y == 2016 )
                || ( d == 1 && m == Month.August && y == 2017 )
                || ( d == 22 && m == Month.July && y == 2018 )
                || ( d == 11 && m == Month.August && y == 2019 )
                || ( d == 30 && m == Month.July && y == 2020 )
                || ( d == 18 && m == Month.July && y == 2021 )
                || ( d == 7 && m == Month.August && y == 2022 )
                || ( d == 27 && m == Month.July && y == 2023 )
                || ( d == 13 && m == Month.August && y == 2024 )
                || ( d == 3 && m == Month.August && y == 2025 )
                || ( d == 23 && m == Month.July && y == 2026 )
                || ( d == 12 && m == Month.August && y == 2027 )
                || ( d == 1 && m == Month.August && y == 2028 )
                || ( d == 22 && m == Month.July && y == 2029 )
                || ( d == 8 && m == Month.August && y == 2030 )
                || ( d == 29 && m == Month.July && y == 2031 )
                || ( d == 18 && m == Month.July && y == 2032 )
                || ( d == 4 && m == Month.August && y == 2033 )
                || ( d == 25 && m == Month.July && y == 2034 )
                || ( d == 14 && m == Month.August && y == 2035 )
                || ( d == 3 && m == Month.August && y == 2036 )
                || ( d == 21 && m == Month.July && y == 2037 )
                || ( d == 10 && m == Month.August && y == 2038 )
                || ( d == 31 && m == Month.July && y == 2039 )
                || ( d == 19 && m == Month.July && y == 2040 )
                || ( d == 6 && m == Month.August && y == 2041 )
                || ( d == 27 && m == Month.July && y == 2042 )
                || ( d == 16 && m == Month.August && y == 2043 )
                || ( d == 2 && m == Month.August && y == 2044 )
               //Jewish New Year
                || ( ( d == 4 || d == 5 || d == 6 ) && m == Month.September && y == 2013 )
                || ( ( d == 24 || d == 25 || d == 26 ) && m == Month.September && y == 2014 )
                || ( ( d == 13 || d == 14 || d == 15 ) && m == Month.September && y == 2015 )
                || ( ( d == 2 || d == 3 || d == 4 ) && m == Month.October && y == 2016 )
                || ( ( d == 20 || d == 21 || d == 22 ) && m == Month.September && y == 2017 )
                || ( ( d == 9 || d == 10 || d == 11 ) && m == Month.September && y == 2018 )
                || ( ( ( ( d == 29 || d == 30 ) && m == Month.September ) || ( d == 1 && m == Month.October ) ) && y == 2019 )
                || ( ( d == 19 || d == 20 ) && m == Month.September && y == 2020 )
                || ( ( d == 7 || d == 8 ) && m == Month.September && y == 2021 )
                || ( ( d == 26 || d == 27 ) && m == Month.September && y == 2022 )
                || ( ( d == 16 || d == 17 ) && m == Month.September && y == 2023 )
                || ( ( d == 3 || d == 4 ) && m == Month.October && y == 2024 )
                || ( ( d == 23 || d == 24 ) && m == Month.September && y == 2025 )
                || ( ( d == 12 || d == 13 ) && m == Month.September && y == 2026 )
                || ( ( d == 2 || d == 3 ) && m == Month.October && y == 2027 )
                || ( ( d == 21 || d == 22 ) && m == Month.September && y == 2028 )
                || ( ( d == 10 || d == 11 ) && m == Month.September && y == 2029 )
                || ( ( d == 28 || d == 29 ) && m == Month.September && y == 2030 )
                || ( ( d == 18 || d == 19 ) && m == Month.September && y == 2031 )
                || ( ( d == 6 || d == 7 ) && m == Month.September && y == 2032 )
                || ( ( d == 24 || d == 25 ) && m == Month.September && y == 2033 )
                || ( ( d == 14 || d == 15 ) && m == Month.September && y == 2034 )
                || ( ( d == 4 || d == 5 ) && m == Month.October && y == 2035 )
                || ( ( d == 22 || d == 23 ) && m == Month.September && y == 2036 )
                || ( ( d == 10 || d == 11 ) && m == Month.September && y == 2037 )
                || ( ( ( d == 30 && m == Month.September ) || ( d == 01 && m == Month.October ) ) && y == 2038 )
                || ( ( d == 19 || d == 20 ) && m == Month.September && y == 2039 )
                || ( ( d == 8 || d == 9 ) && m == Month.September && y == 2040 )
                || ( ( d == 26 || d == 27 ) && m == Month.September && y == 2041 )
                || ( ( d == 15 || d == 16 ) && m == Month.September && y == 2042 )
                || ( ( d == 5 || d == 6 ) && m == Month.October && y == 2043 )
                || ( ( d == 22 || d == 23 ) && m == Month.September && y == 2044 )
               //Yom Kippur
                || ( ( d == 13 || d == 14 ) && m == Month.September && y == 2013 )
                || ( ( d == 3 || d == 4 ) && m == Month.October && y == 2014 )
                || ( ( d == 22 || d == 23 ) && m == Month.September && y == 2015 )
                || ( ( d == 11 || d == 12 ) && m == Month.October && y == 2016 )
                || ( ( d == 29 || d == 30 ) && m == Month.September && y == 2017 )
                || ( ( d == 18 || d == 19 ) && m == Month.September && y == 2018 )
                || ( ( d == 8 || d == 9 ) && m == Month.October && y == 2019 )
                || ( ( d == 27 || d == 28 ) && m == Month.September && y == 2020 )
                || ( ( d == 15 || d == 16 ) && m == Month.September && y == 2021 )
                || ( ( d == 4 || d == 5 ) && m == Month.October && y == 2022 )
                || ( ( d == 24 || d == 25 ) && m == Month.September && y == 2023 )
                || ( ( d == 11 || d == 12 ) && m == Month.October && y == 2024 )
                || ( ( d == 1 || d == 2 ) && m == Month.October && y == 2025 )
                || ( ( d == 20 || d == 21 ) && m == Month.September && y == 2026 )
                || ( ( d == 10 || d == 11 ) && m == Month.October && y == 2027 )
                || ( ( d == 29 || d == 30 ) && m == Month.September && y == 2028 )
                || ( ( d == 18 || d == 19 ) && m == Month.September && y == 2029 )
                || ( ( d == 6 || d == 7 ) && m == Month.October && y == 2030 )
                || ( ( d == 26 || d == 27 ) && m == Month.September && y == 2031 )
                || ( ( d == 14 || d == 15 ) && m == Month.September && y == 2032 )
                || ( ( d == 2 || d == 3 ) && m == Month.October && y == 2033 )
                || ( ( d == 22 || d == 23 ) && m == Month.September && y == 2034 )
                || ( ( d == 12 || d == 13 ) && m == Month.October && y == 2035 )
                || ( ( ( d == 30 && m == Month.September ) || ( d == 01 && m == Month.October ) ) && y == 2036 )
                || ( ( d == 18 || d == 19 ) && m == Month.September && y == 2037 )
                || ( ( d == 8 || d == 9 ) && m == Month.October && y == 2038 )
                || ( ( d == 27 || d == 28 ) && m == Month.September && y == 2039 )
                || ( ( d == 16 || d == 17 ) && m == Month.September && y == 2040 )
                || ( ( d == 4 || d == 5 ) && m == Month.October && y == 2041 )
                || ( ( d == 23 || d == 24 ) && m == Month.September && y == 2042 )
                || ( ( d == 13 || d == 14 ) && m == Month.October && y == 2043 )
                || ( ( ( d == 30 && m == Month.September ) || ( d == 01 && m == Month.October ) ) && y == 2044 )
               //Sukkoth
                || ( ( d == 18 || d == 19 ) && m == Month.September && y == 2013 )
                || ( ( d == 8 || d == 9 ) && m == Month.October && y == 2014 )
                || ( ( d == 27 || d == 28 ) && m == Month.September && y == 2015 )
                || ( ( d == 16 || d == 17 ) && m == Month.October && y == 2016 )
                || ( ( d == 4 || d == 5 ) && m == Month.October && y == 2017 )
                || ( ( d == 23 || d == 24 ) && m == Month.September && y == 2018 )
                || ( ( d == 13 || d == 14 ) && m == Month.October && y == 2019 )
                || ( ( d == 2 || d == 3 ) && m == Month.October && y == 2020 )
                || ( ( d == 20 || d == 21 ) && m == Month.September && y == 2021 )
                || ( ( d == 9 || d == 10 ) && m == Month.October && y == 2022 )
                || ( ( d == 29 || d == 30 ) && m == Month.September && y == 2023 )
                || ( ( d == 16 || d == 17 ) && m == Month.October && y == 2024 )
                || ( ( d == 6 || d == 7 ) && m == Month.October && y == 2025 )
                || ( ( d == 25 || d == 26 ) && m == Month.September && y == 2026 )
                || ( ( d == 15 || d == 16 ) && m == Month.October && y == 2027 )
                || ( ( d == 4 || d == 5 ) && m == Month.October && y == 2028 )
                || ( ( d == 23 || d == 24 ) && m == Month.September && y == 2029 )
                || ( ( d == 11 || d == 12 ) && m == Month.October && y == 2030 )
                || ( ( d == 1 || d == 2 ) && m == Month.October && y == 2031 )
                || ( ( d == 19 || d == 20 ) && m == Month.September && y == 2032 )
                || ( ( d == 7 || d == 8 ) && m == Month.October && y == 2033 )
                || ( ( d == 27 || d == 28 ) && m == Month.September && y == 2034 )
                || ( ( d == 17 || d == 18 ) && m == Month.October && y == 2035 )
                || ( ( d == 5 || d == 6 ) && m == Month.October && y == 2036 )
                || ( ( d == 23 || d == 24 ) && m == Month.September && y == 2037 )
                || ( ( d == 13 || d == 14 ) && m == Month.October && y == 2038 )
                || ( ( d == 2 || d == 3 ) && m == Month.October && y == 2039 )
                || ( ( d == 21 || d == 22 ) && m == Month.September && y == 2040 )
                || ( ( d == 9 || d == 10 ) && m == Month.October && y == 2041 )
                || ( ( d == 28 || d == 29 ) && m == Month.September && y == 2042 )
                || ( ( d == 18 || d == 19 ) && m == Month.October && y == 2043 )
                || ( ( d == 5 || d == 6 ) && m == Month.October && y == 2044 )
               //Simchat Tora
                || ( ( d == 25 || d == 26 ) && m == Month.September && y == 2013 )
                || ( ( d == 15 || d == 16 ) && m == Month.October && y == 2014 )
                || ( ( d == 4 || d == 5 ) && m == Month.October && y == 2015 )
                || ( ( d == 23 || d == 24 ) && m == Month.October && y == 2016 )
                || ( ( d == 11 || d == 12 ) && m == Month.October && y == 2017 )
                || ( ( ( d == 30 && m == Month.September ) || ( d == 1 && m == Month.October ) ) && y == 2018 )
                || ( ( d == 20 || d == 21 ) && m == Month.October && y == 2019 )
                || ( ( d == 9 || d == 10 ) && m == Month.October && y == 2020 )
                || ( ( d == 27 || d == 28 ) && m == Month.September && y == 2021 )
                || ( ( d == 16 || d == 17 ) && m == Month.October && y == 2022 )
                || ( ( d == 6 || d == 7 ) && m == Month.October && y == 2023 )
                || ( ( d == 23 || d == 24 ) && m == Month.October && y == 2024 )
                || ( ( d == 13 || d == 14 ) && m == Month.October && y == 2025 )
                || ( ( d == 2 || d == 3 ) && m == Month.October && y == 2026 )
                || ( ( d == 22 || d == 23 ) && m == Month.October && y == 2027 )
                || ( ( d == 11 || d == 12 ) && m == Month.October && y == 2028 )
                || ( ( ( d == 30 && m == Month.September ) || ( d == 1 && m == Month.October ) ) && y == 2029 )
                || ( ( d == 18 || d == 19 ) && m == Month.October && y == 2030 )
                || ( ( d == 8 || d == 9 ) && m == Month.October && y == 2031 )
                || ( ( d == 26 || d == 27 ) && m == Month.September && y == 2032 )
                || ( ( d == 14 || d == 15 ) && m == Month.October && y == 2033 )
                || ( ( d == 4 || d == 5 ) && m == Month.October && y == 2034 )
                || ( ( d == 24 || d == 25 ) && m == Month.October && y == 2035 )
                || ( ( d == 12 || d == 13 ) && m == Month.October && y == 2036 )
                || ( ( ( d == 30 && m == Month.September ) || ( d == 1 && m == Month.October ) ) && y == 2037 )
                || ( ( d == 20 || d == 21 ) && m == Month.October && y == 2038 )
                || ( ( d == 9 || d == 10 ) && m == Month.October && y == 2039 )
                || ( ( d == 28 || d == 29 ) && m == Month.September && y == 2040 )
                || ( ( d == 16 || d == 17 ) && m == Month.October && y == 2041 )
                || ( ( d == 5 || d == 6 ) && m == Month.October && y == 2042 )
                || ( ( d == 25 || d == 26 ) && m == Month.October && y == 2043 )
                || ( ( d == 12 || d == 13 ) && m == Month.October && y == 2044 ) )
               return false;

            return true;
         }
   
      }

   }


}


