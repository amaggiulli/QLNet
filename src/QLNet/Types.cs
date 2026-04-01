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

   /// <summary>
   /// Duration calculation types
   /// </summary>
   public struct Duration
   {
      /// <summary>
      /// Duration type
      /// </summary>
      public enum Type
      {
         /// <summary>
         /// Simple duration
         /// </summary>
         Simple,

         /// <summary>
         /// Macaulay duration
         /// </summary>
         Macaulay,

         /// <summary>
         /// Modified duration
         /// </summary>
         Modified
      }
   }

   /// <summary>
   /// Position in a financial instrument
   /// </summary>
   public struct Position
   {
      /// <summary>
      /// Position type
      /// </summary>
      public enum Type
      {
         /// <summary>
         /// Long position
         /// </summary>
         Long,

         /// <summary>
         /// Short position
         /// </summary>
         Short
      }
   }

   /// <summary>
   /// Interest rate type
   /// </summary>
   public enum InterestRateType
   {
      /// <summary>
      /// Fixed interest rate
      /// </summary>
      Fixed,

      /// <summary>
      /// Floating interest rate
      /// </summary>
      Floating
   }

   /// <summary>
   /// Interest rate compounding rule
   /// </summary>
   public enum Compounding
   {
      /// <summary>
      /// Simple compounding: 1+rt
      /// </summary>
      Simple = 0,

      /// <summary>
      /// Compounded: (1+r)^t
      /// </summary>
      Compounded = 1,

      /// <summary>
      /// Continuous compounding: e^(rt)
      /// </summary>
      Continuous = 2,

      /// <summary>
      /// Simple up to the first period then Compounded
      /// </summary>
      SimpleThenCompounded,

      /// <summary>
      /// Compounded up to the first period then Simple
      /// </summary>
      CompoundedThenSimple
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

   /// <summary>
   /// Business day convention rules
   /// </summary>
   public enum BusinessDayConvention
   {
      /// <summary>
      /// Choose the first business day after the given holiday.
      /// </summary>
      Following,
      
      /// <summary>
      /// Choose the first business day after the given holiday unless it belongs
      /// to a different month, in which case choose the first business day before
      /// the holiday.
      /// </summary>
      ModifiedFollowing,
      
      /// <summary>
      /// Choose the first business day before the given holiday.
      /// </summary>
      Preceding,
      
      /// <summary>
      /// Choose the first business day before the given holiday unless it belongs
      /// to a different month, in which case choose the first business day after
      /// the holiday.
      /// </summary>
      ModifiedPreceding,
      
      /// <summary>
      /// Do not adjust.
      /// </summary>
      Unadjusted,
      
      /// <summary>
      /// Choose the first business day after the given holiday unless that day
      /// crosses the mid-month (15th) or the end of month, in which case choose
      /// the first business day before the holiday.
      /// </summary>
      HalfMonthModifiedFollowing,
      
      /// <summary>
      /// Choose the nearest business day to the given holiday. If both the
      /// preceding and following business days are equally far away, default
      /// to following business day.
      /// </summary>
      Nearest
   }

   /// <summary>
   /// Units used to describe time periods
   /// </summary>
   public enum TimeUnit
   {
      /// <summary>
      /// Days
      /// </summary>
      Days,

      /// <summary>
      /// Weeks
      /// </summary>
      Weeks,

      /// <summary>
      /// Months
      /// </summary>
      Months,

      /// <summary>
      /// Years
      /// </summary>
      Years
   }

   /// <summary>
   /// Payment frequency enumeration
   /// </summary>
   public enum Frequency
   {
      /// <summary>
      /// Null frequency
      /// </summary>
      NoFrequency = -1,

      /// <summary>
      /// Only once, e.g., a zero-coupon
      /// </summary>
      Once = 0,

      /// <summary>
      /// Once a year
      /// </summary>
      Annual = 1,

      /// <summary>
      /// Twice a year
      /// </summary>
      Semiannual = 2,

      /// <summary>
      /// Every fourth month
      /// </summary>
      EveryFourthMonth = 3,

      /// <summary>
      /// Every third month
      /// </summary>
      Quarterly = 4,

      /// <summary>
      /// Every second month
      /// </summary>
      Bimonthly = 6,

      /// <summary>
      /// Once a month
      /// </summary>
      Monthly = 12,

      /// <summary>
      /// Every fourth week
      /// </summary>
      EveryFourthWeek = 13,

      /// <summary>
      /// Every second week
      /// </summary>
      Biweekly = 26,

      /// <summary>
      /// Once a week
      /// </summary>
      Weekly = 52,

      /// <summary>
      /// Once a day
      /// </summary>
      Daily = 365,

      /// <summary>
      /// Some other unknown frequency
      /// </summary>
      OtherFrequency = 999
   }

   /// <summary>
   /// Conventions used to generate dates in a Schedule
   /// </summary>
   public struct DateGeneration
   {
      /// <summary>
      /// Date generation rule
      /// </summary>
      public enum Rule
      {
         /// <summary>
         /// Backward from termination date to effective date
         /// </summary>
         Backward,
         
         /// <summary>
         /// Forward from effective date to termination date
         /// </summary>
         Forward,
         
         /// <summary>
         /// No intermediate dates between effective date and termination date
         /// </summary>
         Zero,
         
         /// <summary>
         /// All dates but effective date and termination date are taken to be on the third Wednesday of their month
         /// </summary>
         ThirdWednesday,
         
         /// <summary>
         /// All dates including effective date and termination date are taken to be on the third Wednesday of their month (with forward calculation)
         /// </summary>
         ThirdWednesdayInclusive,
         
         /// <summary>
         /// All dates but the effective date are taken to be the twentieth of their month (used for CDS schedules in emerging markets). The termination date is also modified.
         /// </summary>
         Twentieth,
         
         /// <summary>
         /// All dates but the effective date are taken to be the twentieth of an IMM month (used for CDS schedules). The termination date is also modified.
         /// </summary>
         TwentiethIMM,
         
         /// <summary>
         /// Same as TwentiethIMM with unrestricted date ends and long/short stub coupon period (old CDS convention)
         /// </summary>
         OldCDS,
         
         /// <summary>
         /// Credit derivatives standard rule since 'Big Bang' changes in 2009
         /// </summary>
         CDS,
         
         /// <summary>
         /// Credit derivatives standard rule since December 20th, 2015
         /// </summary>
         CDS2015
      }
   }

   /// <summary>
   /// Cap/Floor type
   /// </summary>
   public enum CapFloorType
   {
      /// <summary>
      /// Interest rate cap
      /// </summary>
      Cap,

      /// <summary>
      /// Interest rate floor
      /// </summary>
      Floor,

      /// <summary>
      /// Interest rate collar
      /// </summary>
      Collar
   }

}
