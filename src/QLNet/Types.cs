/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2017 Andrea Maggiulli (a.maggiulli@gmail.com)

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

using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace QLNet
{
   // interface for all value methods
   public interface IValue
   {
      double value(double v);
   }

   public struct Const
   {
      public const double QL_EPSILON = 2.2204460492503131e-016;

      public const double M_SQRT2    = 1.41421356237309504880;
      public const double M_SQRT_2   = 0.7071067811865475244008443621048490392848359376887;
      public const double M_SQRTPI   = 1.77245385090551602792981;
      public const double M_1_SQRTPI = 0.564189583547756286948;

      public const double M_LN2 = 0.693147180559945309417;
      public const double M_PI = 3.141592653589793238462643383280;
      public const double M_PI_2 = 1.57079632679489661923;
      public const double M_2_PI = 0.636619772367581343076;

      public static double BASIS_POINT = 1.0e-4;
   }

   public class TimeSeries<T> : IDictionary<Date, T>
   {
      private Dictionary<Date, T> backingDictionary_;

      // constructors
      public TimeSeries()
      {
         backingDictionary_ = new Dictionary<Date, T>();
      }

      public TimeSeries(int size)
      {
         backingDictionary_ = new Dictionary<Date, T>(size);
      }

      IEnumerator IEnumerable.GetEnumerator()
      {
         return backingDictionary_.GetEnumerator();
      }

      public IEnumerator<KeyValuePair<Date, T>> GetEnumerator()
      {
         return backingDictionary_.GetEnumerator();
      }

      public void Add(KeyValuePair<Date, T> item)
      {
         backingDictionary_.Add(item.Key, item.Value);
      }

      public void Clear()
      {
         backingDictionary_.Clear();
      }

      public bool Contains(KeyValuePair<Date, T> item)
      {
         return backingDictionary_.Contains(item);
      }

      public void CopyTo(KeyValuePair<Date, T>[] array, int arrayIndex)
      {
         throw new System.NotImplementedException();
      }

      public bool Remove(KeyValuePair<Date, T> item)
      {
         return backingDictionary_.Remove(item.Key);
      }

      public int Count { get { return backingDictionary_.Count; } }
      public bool IsReadOnly
      {
         get
         {
            return false;
         }
      }

      public bool ContainsKey(Date key)
      {
         return backingDictionary_.ContainsKey(key);
      }

      public void Add(Date key, T value)
      {
         backingDictionary_.Add(key, value);
      }

      public bool Remove(Date key)
      {
         return backingDictionary_.Remove(key);
      }

      public bool TryGetValue(Date key, out T value)
      {
         return backingDictionary_.TryGetValue(key, out value);
      }

      public T this[Date key]
      {
         get
         {
            if (backingDictionary_.ContainsKey(key))
            {
               return backingDictionary_[key];
            }
            return default(T);
         }
         set
         {
            backingDictionary_[key] = value;
         }
      }

      public ICollection<Date> Keys { get { return backingDictionary_.Keys; } }
      public ICollection<T> Values { get { return backingDictionary_.Values; } }
   }

   public struct Duration
   {
      public enum Type { Simple, Macaulay, Modified }
   }

   public struct Position
   {
      public enum Type { Long, Short }
   }

   public enum InterestRateType { Fixed, Floating }
   //! Interest rate coumpounding rule
   public enum Compounding
   {
      Simple = 0,          //!< \f$ 1+rt \f$
      Compounded = 1,      //!< \f$ (1+r)^t \f$
      Continuous = 2,      //!< \f$ e^{rt} \f$
      SimpleThenCompounded //!< Simple up to the first period then Compounded
   }

   public enum Month
   {
      January   = 1,
      February  = 2,
      March     = 3,
      April     = 4,
      May       = 5,
      June      = 6,
      July      = 7,
      August    = 8,
      September = 9,
      October   = 10,
      November  = 11,
      December  = 12,
      Jan = 1,
      Feb = 2,
      Mar = 3,
      Apr = 4,
      Jun = 6,
      Jul = 7,
      Aug = 8,
      Sep = 9,
      Oct = 10,
      Nov = 11,
      Dec = 12
   }

   public enum BusinessDayConvention
   {
      // ISDA
      Following,          /*!< Choose the first business day after
                              the given holiday. */
      ModifiedFollowing,  /*!< Choose the first business day after
                              the given holiday unless it belongs
                              to a different month, in which case
                              choose the first business day before
                              the holiday. */
      Preceding,          /*!< Choose the first business day before
                              the given holiday. */
      // NON ISDA
      ModifiedPreceding,  /*!< Choose the first business day before
                              the given holiday unless it belongs
                              to a different month, in which case
                              choose the first business day after
                              the holiday. */
      Unadjusted,          /*!< Do not adjust. */
      HalfMonthModifiedFollowing,   /*!< Choose the first business day after
                                       the given holiday unless that day
                                       crosses the mid-month (15th) or the
                                       end of month, in which case choose
                                       the first business day before the
                                       holiday. */
      Nearest                      /*!< Choose the nearest business day
                                       to the given holiday. If both the
                                       preceding and following business
                                       days are equally far away, default
                                       to following business day. */
   }

   //! Units used to describe time periods
   public enum TimeUnit
   {
      Days,
      Weeks,
      Months,
      Years
   }

   public enum Frequency
   {
      NoFrequency = -1,     //!< null frequency
      Once = 0,             //!< only once, e.g., a zero-coupon
      Annual = 1,           //!< once a year
      Semiannual = 2,       //!< twice a year
      EveryFourthMonth = 3, //!< every fourth month
      Quarterly = 4,        //!< every third month
      Bimonthly = 6,        //!< every second month
      Monthly = 12,         //!< once a month
      EveryFourthWeek = 13, //!< every fourth week
      Biweekly = 26,        //!< every second week
      Weekly = 52,          //!< once a week
      Daily = 365,          //!< once a day
      OtherFrequency = 999  //!< some other unknown frequency
   }

   // These conventions specify the rule used to generate dates in a Schedule.
   public struct DateGeneration
   {
      public enum Rule
      {
         Backward,      /*!< Backward from termination date to effective date. */
         Forward,       /*!< Forward from effective date to termination date. */
         Zero,          /*!< No intermediate dates between effective date and termination date. */
         ThirdWednesday,/*!< All dates but effective date and termination date are taken to be on the third wednesday of their month*/
         Twentieth,     /*!< All dates but the effective date are taken to be the twentieth of their
                              month (used for CDS schedules in emerging markets.)  The termination
                              date is also modified. */
         TwentiethIMM,   /*!< All dates but the effective date are taken to be the twentieth of an IMM
                              month (used for CDS schedules.)  The termination date is also modified. */
         OldCDS,         /*!< Same as TwentiethIMM with unrestricted date ends and log/short stub
                              coupon period (old CDS convention). */
         CDS,            /*!< Credit derivatives standard rule since 'Big Bang' changes in 2009.  */
         CDS2015         /*!< Credit derivatives standard rule since December 20th, 2015.  */
      }
   }

   public enum CapFloorType { Cap, Floor, Collar }

}
