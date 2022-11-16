/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
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

namespace QLNet
{
    //! 30/360 day count convention
    /*! The 30/360 day count can be calculated according to a
        number of conventions.

        US convention: if the starting date is the 31st of a month or
        the last day of February, it becomes equal to the 30th of the
        same month.  If the ending date is the 31st of a month and the
        starting date is the 30th or 31th of a month, the ending date
        becomes equal to the 30th.  If the ending date is the last of
        February and the starting date is also the last of February,
        the ending date becomes equal to the 30th.
        Also known as "30/360" or "360/360".

        Bond Basis convention: if the starting date is the 31st of a
        month, it becomes equal to the 30th of the same month.
        If the ending date is the 31st of a month and the starting
        date is the 30th or 31th of a month, the ending date
        also becomes equal to the 30th of the month.
        Also known as "US (ISMA)".

        European convention: starting dates or ending dates that
        occur on the 31st of a month become equal to the 30th of the
        same month.
        Also known as "30E/360", or "Eurobond Basis".

        Italian convention: starting dates or ending dates that
        occur on February and are greater than 27 become equal to 30
        for computational sake.

        ISDA convention: starting or ending dates on the 31st of the
        month become equal to 30; starting dates or ending dates that
        occur on the last day of February also become equal to 30,
        except for the termination date.  Also known as "30E/360
        ISDA", "30/360 ISDA", or "30/360 German".

        NASD convention: if the starting date is the 31st of a
        month, it becomes equal to the 30th of the same month.
        If the ending date is the 31st of a month and the starting
        date is earlier than the 30th of a month, the ending date
        becomes equal to the 1st of the next month, otherwise the
        ending date becomes equal to the 30th of the same month.

    */
   public class Thirty360 : DayCounter
   {
      private static bool IsLastOfFebruary(int d, int m, int y)
      {
         return m == 2 && d == 28 + (Date.IsLeapYear(y) ? 1 : 0);
      }

      public enum Thirty360Convention { USA, BondBasis, European, EurobondBasis, Italian, German, ISMA, ISDA, NASD }

      public Thirty360(Thirty360Convention c, Date terminationDate = null) : base(Implementation(c, terminationDate)) { }

      private static DayCounterImpl Implementation(Thirty360Convention c, Date terminationDate)
      {
         switch (c)
         {
            case Thirty360Convention.USA:
               return new UsImpl();
            case Thirty360Convention.European:
            case Thirty360Convention.EurobondBasis:
               return new EuImpl();
            case Thirty360Convention.Italian:
               return new ItImpl();
            case Thirty360Convention.ISMA:
            case Thirty360Convention.BondBasis:
               return new IsmaImpl();
            case Thirty360Convention.ISDA:
            case Thirty360Convention.German:
               return new IsdaImpl(terminationDate);
            case Thirty360Convention.NASD:
               return new NasdImpl();
            default:
               throw new ArgumentException("Unknown 30/360 convention: " + c);
         }
      }

      private class UsImpl : DayCounterImpl
      {
         public override string name() { return "30/360 (US)"; }
         public override int dayCount(Date d1, Date d2)
         {
            int dd1 = d1.Day, dd2 = d2.Day;
            int mm1 = d1.Month, mm2 = d2.Month;
            int yy1 = d1.Year, yy2 = d2.Year;

            if (dd1 == 31) { dd1 = 30; }
            if (dd2 == 31 && dd1 >= 30) { dd2 = 30; }

            if (IsLastOfFebruary(dd2, mm2, yy2) && IsLastOfFebruary(dd1, mm1, yy1)) { dd2 = 30; }
            if (IsLastOfFebruary(dd1, mm1, yy1)) { dd1 = 30; }

            return 360*(yy2-yy1) + 30*(mm2-mm1) + (dd2-dd1);
         }
         public override double yearFraction(Date d1, Date d2, Date d3, Date d4) { return dayCount(d1,d2)/360.0; }
      }

      private class IsmaImpl : DayCounterImpl
      {
         public override string name() { return "30/360 (Bond Basis)"; }
         public override int dayCount(Date d1, Date d2)
         {
            int dd1 = d1.Day, dd2 = d2.Day;
            int mm1 = d1.month(), mm2 = d2.month();
            int yy1 = d1.year(), yy2 = d2.year();

            if (dd1 == 31) { dd1 = 30; }
            if (dd2 == 31 && dd1 == 30) { dd2 = 30; }

            return 360*(yy2-yy1) + 30*(mm2-mm1) + (dd2-dd1);
         }
         public override double yearFraction(Date d1, Date d2, Date d3, Date d4) { return dayCount(d1,d2)/360.0; }
      }

      private class EuImpl : DayCounterImpl
      {
         public override string name() { return "30E/360 (Eurobond Basis)"; }
         public override int dayCount(Date d1, Date d2)
         {
            int dd1 = d1.Day, dd2 = d2.Day;
            int mm1 = d1.month(), mm2 = d2.month();
            int yy1 = d1.year(), yy2 = d2.year();

            if (dd1 == 31) { dd1 = 30; }
            if (dd2 == 31) { dd2 = 30; }

            return 360*(yy2-yy1) + 30*(mm2-mm1) + (dd2-dd1);
         }

         public override double yearFraction(Date d1, Date d2, Date d3, Date d4) { return dayCount(d1, d2) / 360.0; }
      }

      private class ItImpl : DayCounterImpl
      {
         public override string name() { return "30/360 (Italian)"; }
         public override int dayCount(Date d1, Date d2)
         {
            int dd1 = d1.Day, dd2 = d2.Day;
            int mm1 = d1.month(), mm2 = d2.month();
            int yy1 = d1.year(), yy2 = d2.year();

            if (dd1 == 31) { dd1 = 30; }
            if (dd2 == 31) { dd2 = 30; }

            if (mm1 == 2 && dd1 > 27) { dd1 = 30; }
            if (mm2 == 2 && dd2 > 27) { dd2 = 30; }

            return 360*(yy2-yy1) + 30*(mm2-mm1) + (dd2-dd1);
         }

         public override double yearFraction(Date d1, Date d2, Date d3, Date d4) { return dayCount(d1, d2) / 360.0; }
      }

      private class IsdaImpl : DayCounterImpl
      {
         public IsdaImpl(Date terminationDate)
         {
            terminationDate_ = terminationDate;
         }
         private readonly Date terminationDate_;
         public override string name() { return "30E/360 (ISDA)"; }
         public override int dayCount(Date d1, Date d2)
         {
            int dd1 = d1.Day, dd2 = d2.Day;
            int mm1 = d1.month(), mm2 = d2.month();
            int yy1 = d1.year(), yy2 = d2.year();

            if (dd1 == 31) { dd1 = 30; }
            if (dd2 == 31) { dd2 = 30; }

            if (IsLastOfFebruary(dd1, mm1, yy1)) { dd1 = 30; }

            if (d2 != terminationDate_ && IsLastOfFebruary(dd2, mm2, yy2)) { dd2 = 30; }

            return 360*(yy2-yy1) + 30*(mm2-mm1) + (dd2-dd1);
         }
         public override double yearFraction(Date d1, Date d2, Date d3, Date d4) { return dayCount(d1, d2) / 360.0; }
      }

      private class NasdImpl : DayCounterImpl
      {
         public override string name() { return "30/360 (NASD)"; }
         public override int dayCount(Date d1, Date d2)
         {
            int dd1 = d1.Day, dd2 = d2.Day;
            int mm1 = d1.month(), mm2 = d2.month();
            int yy1 = d1.year(), yy2 = d2.year();

            if (dd1 == 31) { dd1 = 30; }
            if (dd2 == 31 && dd1 >= 30) { dd2 = 30; }
            if (dd2 == 31 && dd1 < 30) { dd2 = 1; mm2++; }

            return 360*(yy2-yy1) + 30*(mm2-mm1) + (dd2-dd1);
         }

         public override double yearFraction(Date d1, Date d2, Date d3, Date d4) { return dayCount(d1, d2) / 360.0; }
      }
   }
}
