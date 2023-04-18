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

using System.Collections.Generic;
using Cache = System.Collections.Generic.SortedDictionary<int, System.Collections.Generic.SortedDictionary<QLNet.Month,int>>;
using OuterCache = System.Collections.Generic.SortedDictionary<int, int>;

namespace QLNet
{
   /// <summary>
   /// Business/252 day count convention
   /// </summary>
   public class Business252 : DayCounter
   {
      public Business252(Calendar c = null) :
         base(new Impl(c))
      {}

      private class Impl : DayCounterImpl
      {
         private readonly Calendar calendar_;
         readonly SortedDictionary<string, Cache> monthlyFigures_ = new SortedDictionary<string, Cache>();
         readonly SortedDictionary<string, OuterCache> yearlyFigures_ = new SortedDictionary<string, OuterCache>();

         public Impl(Calendar c)
         {
            calendar_ = c ?? new Brazil();
         }
         public override string name() { return "Business/252(" + calendar_.name() + ")"; }

         private static bool SameMonth(Date d1, Date d2)
         {
            return d1.year() == d2.year() && d1.month() == d2.month();
         }

         private static bool SameYear(Date d1, Date d2)
         {
            return d1.year() == d2.year();
         }

         private static int BusinessDays(Cache cache, Calendar calendar, Month month, int year)
         {
            if (!cache.ContainsKey(year))
               cache.Add(year, new SortedDictionary<Month, int>());
            if (!cache[year].ContainsKey(month))
            {
               // calculate and store.
               Date d1 = new Date(1,month,year);
               Date d2 = d1 + new Period(1,TimeUnit.Months);
               cache[year].Add(month, calendar.businessDaysBetween(d1, d2));
            }
            return cache[year][month];
         }

         private static int BusinessDays(OuterCache outerCache, Cache cache, Calendar calendar, int year)
         {
            if (!outerCache.ContainsKey(year))
            {
               // calculate and store.
               var total = 0;
               for (var i=1; i<=12; ++i)
               {
                  total += BusinessDays(cache, calendar, (Month)i, year);
               }
               outerCache.Add(year, total);
            }
            return outerCache[year];
         }

         public override int dayCount(Date d1, Date d2)
         {
            if (SameMonth(d1, d2) || d1 >= d2)
            {
               // we treat the case of d1 > d2 here, since we'd need a
               // second cache to get it right (our cached figures are
               // for first included, last excluded and might have to be
               // changed going the other way.)
               return calendar_.businessDaysBetween(d1, d2);
            }
            else if (SameYear(d1, d2))
            {
               if (!monthlyFigures_.ContainsKey(calendar_.name()))
                  monthlyFigures_.Add(calendar_.name(),new Cache());

               var cache = monthlyFigures_[calendar_.name()];
               var total = 0;
               // first, we get to the beginning of next month.
               var d = new Date(1, d1.month(), d1.year()) + new Period(1, TimeUnit.Months);
               total += calendar_.businessDaysBetween(d1, d);
               // then, we add any whole months (whose figures might be
               // cached already) in the middle of our period.
               while (!SameMonth(d, d2))
               {
                  total += BusinessDays(cache, calendar_, (Month)d.month(), d.year());
                  d += new Period(1, TimeUnit.Months);
               }

               // finally, we get to the end of the period.
               total += calendar_.businessDaysBetween(d, d2);
               return total;
            }
            else
            {
               if (!monthlyFigures_.ContainsKey(calendar_.name()))
                  monthlyFigures_.Add(calendar_.name(),new Cache());
               var cache = monthlyFigures_[calendar_.name()];
               if (!yearlyFigures_.ContainsKey(calendar_.name()))
                  yearlyFigures_.Add(calendar_.name(),new OuterCache());
               var outerCache = yearlyFigures_[calendar_.name()];
               var total = 0;
               // first, we get to the beginning of next year.
               // The first bit gets us to the end of this month...
               var d = new Date(1, d1.month(), d1.year()) + new Period(1, TimeUnit.Months);
               total += calendar_.businessDaysBetween(d1, d);
               // ...then we add any remaining months, possibly cached
               for (var m = (d1.month()) + 1; m <= 12; ++m)
               {
                  total += BusinessDays(cache, calendar_, (Month)m, d.year());
               }

               // then, we add any whole year in the middle of our period.
               d = new Date(1, Month.January, d1.year() + 1);
               while (!SameYear(d, d2))
               {
                  total += BusinessDays(outerCache, cache, calendar_, d.year());
                  d += new Period(1, TimeUnit.Years);
               }

               // finally, we get to the end of the period.
               // First, we add whole months...
               for (var m = 1; m < (d2.month()); ++m)
               {
                  total += BusinessDays(cache, calendar_, (Month)m, d2.year());
               }

               // ...then the last bit.
               d = new Date(1, d2.month(), d2.year());
               total += calendar_.businessDaysBetween(d, d2);
               return total;
            }
         }

         public override double yearFraction(Date d1, Date d2, Date d3, Date d4)
         {
            return dayCount(d1, d2) / 252.0;
         }
      }
   }
}
