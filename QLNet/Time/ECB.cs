/*
 Copyright (C) 2008-2016  Andrea Maggiulli (a.maggiulli@gmail.com) 
  
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
using System.Collections.Generic;
using System.Linq;

namespace QLNet
{
   //! European Central Bank reserve maintenance dates
   public struct ECB 
   {
      static List<Date> knownDateSet = new List<Date>();
      public static List<Date> knownDates()
      {
         // one-off inizialization
         int[] knownDatesArray = 
         {
              38371, 38391, 38420, 38455, 38483, 38511, 38546, 38574, 38602, 38637, 38665, 38692 // 2005
            , 38735, 38756, 38784, 38819, 38847, 38883, 38910, 38938, 38966, 39001, 39029, 39064 // 2006
            , 39099, 39127, 39155, 39190, 39217, 39246, 39274, 39302, 39337, 39365, 39400, 39428 // 2007
            , 39463, 39491, 39519, 39554, 39582, 39610, 39638, 39673, 39701, 39729, 39764, 39792 // 2008
            , 39834, 39855, 39883, 39911, 39946, 39974, 40002, 40037, 40065, 40100, 40128, 40155 // 2009
            , 40198, 40219, 40247, 40282, 40310, 40345, 40373, 40401, 40429, 40464, 40492, 40520 // 2010
            , 40562, 40583, 40611, 40646, 40674, 40709, 40737, 40765, 40800, 40828, 40856, 40891 // 2011
            // http://www.ecb.europa.eu/press/pr/date/2011/html/pr110520.en.html
            , 40926, 40954, 40982, 41010, 41038, 41073, 41101, 41129, 41164, 41192, 41227, 41255 // 2012
            , 41290, 41318, 41346, 41374, 41402, 41437, 41465, 41493, 41528, 41556, 41591, 41619 // 2013
            // http://www.ecb.europa.eu/press/pr/date/2013/html/pr130610.en.html
            , 41654, 41682, 41710, 41738, 41773, 41801, 41829, 41864, 41892, 41920, 41955, 41983 // 2014
            // http://www.ecb.europa.eu/press/pr/date/2014/html/pr140717_1.en.html
            , 42032, 42074, 42116, 42165, 42207, 42256, 42305, 42347// 2015
            // https://www.ecb.europa.eu/press/pr/date/2015/html/pr150622.en.html
            , 42396, 42445, 42487, 42529, 42578, 42627, 42669, 42718 // 2016
            , 42760 , /*source ICAP */ 42802, 42844, 42893, 42942 // 2017
         };
         
         if (knownDateSet.empty()) 
         {
            for ( int i = 0; i < knownDatesArray.Length; ++i )
            {
                knownDateSet.Add(new Date(knownDatesArray[i]));
            }
        }

        return knownDateSet;

      }

      public static void addDate( Date d )
      {
         knownDates(); // just to ensure inizialization
         knownDateSet.Add( d );
         knownDateSet.Sort();
      }

      public static void removeDate( Date d )
      {
         knownDates(); // just to ensure inizialization
         knownDateSet.Remove( d );
      }

      //! maintenance period start date in the given month/year
      public static Date date( Month m, int y ) { return nextDate( new Date( 1, m, y ) - 1 ); }

      /*! returns the ECB date for the given ECB code
         (e.g. March xxth, 2013 for MAR10).

         \warning It raises an exception if the input
                  string is not an ECB code
      */
      public static Date date( string ecbCode, Date refDate = null )
      {
         Utils.QL_REQUIRE(isECBcode(ecbCode),() => ecbCode + " is not a valid ECB code");

         string code = ecbCode.ToUpper();
         string monthString = code.Substring(0, 3);
         Month m = Month.Jan;
         if (monthString=="JAN")     m = Month.January;
        else if (monthString=="FEB") m = Month.February;
        else if (monthString=="MAR") m = Month.March;
        else if (monthString=="APR") m = Month.April;
        else if (monthString=="MAY") m = Month.May;
        else if (monthString=="JUN") m = Month.June;
        else if (monthString=="JUL") m = Month.July;
        else if (monthString=="AUG") m = Month.August;
        else if (monthString=="SEP") m = Month.September;
        else if (monthString=="OCT") m = Month.October;
        else if (monthString=="NOV") m = Month.November;
        else if (monthString=="DEC") m = Month.December;
        else Utils.QL_FAIL("not an ECB month (and it should have been)");

        // lexical_cast causes compilation errors with x64
        //Year y = boost::lexical_cast<Year>(code.substr(3, 2));

        int y = int.Parse(code.Substring(3, 2));
        Date referenceDate = (refDate ?? new Date(Settings.evaluationDate()));
        int referenceYear = (referenceDate.year() % 100);
        y += referenceDate.year() - referenceYear;
        if (y<Date.minDate().year())
            return ECB.nextDate(Date.minDate());

        return ECB.nextDate(new Date(1, m, y));
      }

      /*! returns the ECB code for the given date
         (e.g. MAR10 for March xxth, 2010).

         \warning It raises an exception if the input
                  date is not an ECB date
      */
      public static string code( Date ecbDate )
      {
         Utils.QL_REQUIRE(isECBdate(ecbDate),() => ecbDate + " is not a valid ECB date");

        string ECBcode = string.Empty;
        int y = ecbDate.year() % 100;
        string padding = string.Empty;
        if (y < 10)
            padding = "0";
        switch(ecbDate.month()) {
          case (int)Month.January:
            ECBcode += "JAN" + padding + y;
            break;
          case (int)Month.February:
            ECBcode += "FEB" + padding + y;
            break;
          case (int)Month.March:
            ECBcode += "MAR" + padding + y;
            break;
          case (int)Month.April:
            ECBcode += "APR" + padding + y;
            break;
          case (int)Month.May:
            ECBcode += "MAY" + padding + y;
            break;
          case (int)Month.June:
            ECBcode += "JUN" + padding + y;
            break;
          case (int)Month.July:
            ECBcode += "JUL" + padding + y;
            break;
          case (int)Month.August:
            ECBcode += "AUG" + padding + y;
            break;
          case (int)Month.September:
            ECBcode += "SEP" + padding + y;
            break;
          case (int)Month.October:
            ECBcode += "OCT" + padding + y;
            break;
          case (int)Month.November:
            ECBcode += "NOV" + padding + y;
            break;
          case (int)Month.December:
            ECBcode += "DEC" + padding + y;
            break;
          default:
            Utils.QL_FAIL("not an ECB month (and it should have been)");
            break;
        }

        #if(QL_EXTRA_SAFETY_CHECKS)
        QL_ENSURE(isECBcode(ECBcode.str()),
                  "the result " << ECBcode.str() <<
                  " is an invalid ECB code");
        #endif
        return ECBcode;
      }

      //! next maintenance period start date following the given date
      public static Date nextDate( Date date = null )
      {
         Date d = (date ?? Settings.evaluationDate());

         int i = knownDates().FindIndex( x => x > d );

        Utils.QL_REQUIRE(i!=-1,() =>
                   "ECB dates after " + knownDates().Last() + " are unknown");
        return knownDates()[i];
      }

      //! next maintenance period start date following the given ECB code
      public static Date nextDate( string ecbCode, Date referenceDate = null )
      {
         return nextDate(date(ecbCode, referenceDate));
      }

      //! next maintenance period start dates following the given date
      public static List<Date> nextDates( Date date = null )
      {
         Date d = (date ?? Settings.evaluationDate());

        int i = knownDates(). FindIndex(x => x > d );

        Utils.QL_REQUIRE(i != -1 ,() =>
                   "ECB dates after " + knownDates().Last() + " are unknown");
        
         return new List<Date>(knownDates().GetRange(i, knownDates().Count - i ));
      }

      //! next maintenance period start dates following the given code
      public static List<Date> nextDates( string ecbCode, Date referenceDate = null )
      {
         return nextDates(date(ecbCode, referenceDate));
      }

      /*! returns whether or not the given date is
         a maintenance period start date */
      public static bool isECBdate( Date d ) 
      {
         Date date = nextDate(d-1);
         return d==date;
      }

      //! returns whether or not the given string is an ECB code
      public static bool isECBcode( String ecbCode )
      {
         if (ecbCode.Length != 5)
            return false;

        String code = ecbCode.ToUpper();

        String str1 = "0123456789";
        if ( !str1.Contains(code.Substring(3, 1)))
            return false;
        if (!str1.Contains(code.Substring(4, 1)))
            return false;

        string monthString = code.Substring(0, 3);
        if (monthString=="JAN")      return true;
        else if (monthString=="FEB") return true;
        else if (monthString=="MAR") return true;
        else if (monthString=="APR") return true;
        else if (monthString=="MAY") return true;
        else if (monthString=="JUN") return true;
        else if (monthString=="JUL") return true;
        else if (monthString=="AUG") return true;
        else if (monthString=="SEP") return true;
        else if (monthString=="OCT") return true;
        else if (monthString=="NOV") return true;
        else if (monthString=="DEC") return true;
        else return false;
      }

      //! next ECB code following the given date
      static string nextCode(Date d = null) {
         return code(nextDate(d));
      }

      //! next ECB code following the given code
      public static string nextCode( String ecbCode ) 
      {
         Utils.QL_REQUIRE(isECBcode(ecbCode),() =>
                   ecbCode + " is not a valid ECB code");

        String code = ecbCode.ToUpper();
        String result = String.Empty;

        string monthString = code.Substring(0, 3);
        if (monthString=="JAN")      result += "FEB" + code.Substring(3, 2);
        else if (monthString=="FEB") result += "MAR" + code.Substring(3, 2);
        else if (monthString=="MAR") result += "APR" + code.Substring(3, 2);
        else if (monthString=="APR") result += "MAY" + code.Substring(3, 2);
        else if (monthString=="MAY") result += "JUN" + code.Substring(3, 2);
        else if (monthString=="JUN") result += "JUL" + code.Substring(3, 2);
        else if (monthString=="JUL") result += "AUG" + code.Substring(3, 2);
        else if (monthString=="AUG") result += "SEP" + code.Substring(3, 2);
        else if (monthString=="SEP") result += "OCT" + code.Substring(3, 2);
        else if (monthString=="OCT") result += "NOV" + code.Substring(3, 2);
        else if (monthString=="NOV") result += "DEC" + code.Substring(3, 2);
        else if (monthString=="DEC") {
            // lexical_cast causes compilation errors with x64
            //Year y = boost::lexical_cast<Year>(code.substr(3, 2));
            int y = (int.Parse(code.Substring(3, 2)) + 1) % 100;
            String padding = String.Empty;
            if (y < 10)
                padding = "0";

            result += "JAN" + padding + y;
        } else Utils.QL_FAIL("not an ECB month (and it should have been)");


        #if QL_EXTRA_SAFETY_CHECKS
        QL_ENSURE(isECBcode(result.str()),
                  "the result " << result.str() <<
                  " is an invalid ECB code");
        #endif
        return result;
      }

   };
}
