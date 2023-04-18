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
using System.Collections.Generic;
using System.Linq;

namespace QLNet
{
   /// <summary>
   /// Actual/Actual day count
   /// <remarks>
   /// The day count can be calculated according to:
   ///    - the ISDA convention, also known as "Actual/Actual (Historical)", "Actual/Actual", "Act/Act",
   ///      and according to ISDA also "Actual/365", "Act/365", and "A/365"
   ///    - the ISMA and US Treasury convention, also known as "Actual/Actual (Bond)";
   ///    - the AFB convention, also known as "Actual/Actual (Euro)".
   /// For more details, refer to https://www.isda.org/a/pIJEE/The-Actual-Actual-Day-Count-Fraction-1999.pdf
   /// </remarks>
   /// </summary>
   public class ActualActual : DayCounter
   {
      public enum Convention { ISMA, Bond, ISDA, Historical, Actual365, AFB, Euro }

      public ActualActual(Convention c = Convention.ISDA, Schedule schedule = null) : base(Conventions(c, schedule)) { }

      private static DayCounterImpl Conventions(Convention c, Schedule schedule)
      {
         switch (c)
         {
            case Convention.ISMA:
            case Convention.Bond:
               if (schedule != null && !schedule.empty())
                  return new ISMA_Impl(schedule);
               return new Old_ISMA_Impl();
            case Convention.ISDA:
            case Convention.Historical:
            case Convention.Actual365:
               return new ISDA_Impl();
            case Convention.AFB:
            case Convention.Euro:
               return new AFB_Impl();
            default:
               throw new ArgumentException("Unknown act/act convention: " + c);
         }
      }


      private class ISMA_Impl : DayCounterImpl
      {
         private Schedule schedule_;

         public ISMA_Impl(Schedule schedule)
         {
            schedule_ = schedule;
         }

         public override string name() { return "Actual/Actual (ISMA)"; }

         public override double yearFraction(Date d1, Date d2, Date d3, Date d4)
         {
            if (d1 == d2)
               return 0.0;

            if (d2 < d1)
               return -yearFraction(d2, d1, d3, d4);

            var couponDates = getListOfPeriodDatesIncludingQuasiPayments(schedule_);

            var firstDate = couponDates.Min();
            var lastDate = couponDates.Max();

            Utils.QL_REQUIRE(d1 >= firstDate && d2 <= lastDate, ()=>"Dates out of range of schedule: "
                                                          + "date 1: " + d1 + ", date 2: " + d2 + ", first date: "
                                                          + firstDate + ", last date: " + lastDate);
            var yearFractionSum = 0.0;
            for (var i = 0; i < couponDates.Count - 1; i++)
            {
               var startReferencePeriod = couponDates[i];
               var endReferencePeriod = couponDates[i + 1];
               if (d1 < endReferencePeriod && d2 > startReferencePeriod)
               {
                  yearFractionSum +=
                     yearFractionWithReferenceDates(this,
                                                    Date.Max(d1, startReferencePeriod),
                                                    Date.Min(d2, endReferencePeriod),
                                                    startReferencePeriod,
                                                    endReferencePeriod);
               }
            }
            return yearFractionSum;
         }

         private List<Date> getListOfPeriodDatesIncludingQuasiPayments(Schedule schedule)
         {
            // Process the schedule into an array of dates.
            Date issueDate = schedule.date(0);
            Date firstCoupon = schedule.date(1);
            Date notionalFirstCoupon =
               schedule.calendar().advance(firstCoupon,
                                           -schedule.tenor(),
                                           schedule.businessDayConvention(),
                                           schedule.endOfMonth());

            List<Date> newDates = schedule.dates().ToList();
            newDates[0] = notionalFirstCoupon ;

            // The handling of the last coupon is is needed for odd final periods
            var notionalLastCoupon =
               schedule.calendar().advance(
                  schedule.date(schedule.Count - 2),
                  schedule.tenor(),
                  schedule.businessDayConvention(),
                  schedule.endOfMonth());

            newDates[schedule.Count - 1] = notionalLastCoupon;

            //long first coupon
            if (notionalFirstCoupon  > issueDate)
            {
               Date priorNotionalCoupon =
                  schedule.calendar().advance(notionalFirstCoupon ,
                                              -schedule.tenor(),
                                              schedule.businessDayConvention(),
                                              schedule.endOfMonth());
               newDates.Insert(0, priorNotionalCoupon);
            }

            // long last coupon
            if (notionalLastCoupon < schedule.endDate())
            {
               Date nextNotionalCoupon =
                  schedule.calendar().advance(
                     notionalLastCoupon,
                     schedule.tenor(),
                     schedule.businessDayConvention(),
                     schedule.endOfMonth());

               newDates.Add(nextNotionalCoupon);
            }

            return newDates;
         }

         private double yearFractionWithReferenceDates<T>(T impl,
                                                          Date d1, Date d2, Date d3, Date d4) where T: DayCounterImpl
         {
            Utils.QL_REQUIRE(d1 <= d2, () =>
                             "This function is only correct if d1 <= d2\n" +
                             "d1: " + d1 + " d2: " + d2);

            double referenceDayCount = impl.dayCount(d3, d4);
            //guess how many coupon periods per year:
            int couponsPerYear;
            if (referenceDayCount < 16)
            {
               couponsPerYear = 1;
               referenceDayCount = impl.dayCount(d1, d1 + new Period(1, TimeUnit.Years));
            }
            else
            {
               couponsPerYear = findCouponsPerYear(impl, d3, d4);
            }
            return impl.dayCount(d1, d2) / (referenceDayCount * couponsPerYear);
         }

         private int findCouponsPerYear<T>(T impl, Date refStart, Date refEnd) where T : DayCounterImpl
         {
            // This will only work for day counts longer than 15 days.
            int months = (int)(0.5 + 12 * (double)(impl.dayCount(refStart, refEnd)) / 365.0);
            return (int)(0.5 + 12.0 / months);
         }
      }

      private class Old_ISMA_Impl : DayCounterImpl
      {
         public override string name() { return "Actual/Actual (ISMA)"; }
         public override double yearFraction(Date d1, Date d2, Date d3, Date d4)
         {
            if (d1 == d2)
               return 0;
            if (d1 > d2)
               return -yearFraction(d2, d1, d3, d4);

            // when the reference period is not specified, try taking it equal to (d1,d2)
            var refPeriodStart = (d3 ?? d1);
            var refPeriodEnd = (d4 ?? d2);

            Utils.QL_REQUIRE(refPeriodEnd > refPeriodStart && refPeriodEnd > d1, () =>
                             "Invalid reference period: date 1: " + d1 + ", date 2: " + d2 +
                             ", reference period start: " + refPeriodStart + ", reference period end: " + refPeriodEnd);

            // estimate roughly the length in months of a period
            var months = (int)Math.Round(12 * (double)(refPeriodEnd - refPeriodStart) / 365);

            // for short periods...
            if (months == 0)
            {
               // ...take the reference period as 1 year from d1
               refPeriodStart = d1;
               refPeriodEnd = d1 + new Period(1, TimeUnit.Years);
               months = 12;
            }

            var period = months / 12.0;

            if (d2 <= refPeriodEnd)
            {
               // here refPeriodEnd is a future (notional?) payment date
               if (d1 >= refPeriodStart)
               {
                  // here refPeriodStart is the last (maybe notional) payment date.
                  // refPeriodStart <= d1 <= d2 <= refPeriodEnd
                  // [maybe the equality should be enforced, since   refPeriodStart < d1 <= d2 < refPeriodEnd  could give wrong results] ???
                  return period * Date.daysBetween(d1, d2) / Date.daysBetween(refPeriodStart, refPeriodEnd);
               }
               else
               {
                  // here refPeriodStart is the next (maybe notional) payment date and refPeriodEnd is the second next (maybe notional) payment date.
                  // d1 < refPeriodStart < refPeriodEnd
                  // AND d2 <= refPeriodEnd
                  // this case is long first coupon

                  // the last notional payment date
                  var previousRef = refPeriodStart - new Period(months, TimeUnit.Months);
                  if (d2 > refPeriodStart)
                     return yearFraction(d1, refPeriodStart, previousRef, refPeriodStart) +
                            yearFraction(refPeriodStart, d2, refPeriodStart, refPeriodEnd);
                  else
                     return yearFraction(d1, d2, previousRef, refPeriodStart);
               }
            }
            else
            {
               // here refPeriodEnd is the last (notional?) payment date
               // d1 < refPeriodEnd < d2 AND refPeriodStart < refPeriodEnd
               Utils.QL_REQUIRE(refPeriodStart <= d1, () => "invalid dates: d1 < refPeriodStart < refPeriodEnd < d2");

               // now it is: refPeriodStart <= d1 < refPeriodEnd < d2

               // the part from d1 to refPeriodEnd
               double sum = yearFraction(d1, refPeriodEnd, refPeriodStart, refPeriodEnd);

               // the part from refPeriodEnd to d2
               // count how many regular periods are in [refPeriodEnd, d2], then add the remaining time
               int i = 0;
               Date newRefStart, newRefEnd;
               for (; ;)
               {
                  newRefStart = refPeriodEnd + new Period(months * i, TimeUnit.Months);
                  newRefEnd = refPeriodEnd + new Period(months * (i + 1), TimeUnit.Months);
                  if (d2 < newRefEnd)
                  {
                     break;
                  }
                  else
                  {
                     sum += period;
                     i++;
                  }
               }
               sum += yearFraction(newRefStart, d2, newRefStart, newRefEnd);
               return sum;
            }
         }
      }

      private class ISDA_Impl : DayCounterImpl
      {
         public override string name() { return "Actual/Actual (ISDA)"; }

         public override double yearFraction(Date d1, Date d2, Date refPeriodStart, Date refPeriodEnd)
         {
            if (d1 == d2)
               return 0;
            if (d1 > d2)
               return -yearFraction(d2, d1, null, null);

            int y1 = d1.Year, y2 = d2.Year;
            double dib1 = (Date.IsLeapYear(y1) ? 366 : 365),
                   dib2 = (Date.IsLeapYear(y2) ? 366 : 365);

            double sum = y2 - y1 - 1;
            sum += Date.daysBetween(d1, new Date(1, Month.January, y1 + 1)) / dib1;
            sum += Date.daysBetween(new Date(1, Month.January, y2), d2) / dib2;
            return sum;
         }
      }

      private class AFB_Impl : DayCounterImpl
      {
         public override string name() { return "Actual/Actual (AFB)"; }
         public override double yearFraction(Date d1, Date d2, Date refPeriodStart, Date refPeriodEnd)
         {
            if (d1 == d2)
               return 0;
            if (d1 > d2)
               return -yearFraction(d2, d1, null, null);

            Date newD2 = d2, temp = d2;
            double sum = 0;
            while (temp > d1)
            {
               temp = newD2 - TimeUnit.Years;
               if (temp.Day == 28 && temp.Month == 2 && Date.IsLeapYear(temp.Year))
                  temp += 1;
               if (temp >= d1)
               {
                  sum += 1;
                  newD2 = temp;
               }
            }

            double den = 365;

            if (Date.IsLeapYear(newD2.Year))
            {
               temp = new Date(29, Month.February, newD2.Year);
               if (newD2 > temp && d1 <= temp)
                  den += 1;
            }
            else if (Date.IsLeapYear(d1.Year))
            {
               temp = new Date(29, Month.February, d1.Year);
               if (newD2 > temp && d1 <= temp)
                  den += 1;
            }

            return sum + Date.daysBetween(d1, newD2) / den;
         }
      }
   }
}
