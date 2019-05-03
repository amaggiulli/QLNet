/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)

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
using System.Reflection;

namespace QLNet
{
   // here are extensions to IList to accomodate some QL functionality as well as have useful things for .net
   public static partial class Utils
   {
      public static bool empty<T>(this IList<T> items) { return items.Count == 0; }

      // equivalent of ForEach but with the index
      public static void ForEach<T>(this IList<T> items, Action<int, T> action)
      {
         if (items != null && action != null)
            for (int idx = 0; idx < items.Count; idx++)
               action(idx, items[idx]);
      }

      // this is a version of element retrieval with some logic for default values
      public static T Get<T>(this List<T> v, int i)
      {
         return Get(v, i, default(T));
      }
      public static T Get<T>(this List<T> v, int i, T defval)
      {
         if (v == null || v.Count == 0)
            return defval;
         else if (i >= v.Count)
            return v.Last();
         else
            return v[i];
      }
      public static Nullable<T> Get<T>(this List<Nullable<T>> v, int i)
      where T : struct
      {
         return Get(v, i, null);
      }
      public static Nullable<T> Get<T>(this List<Nullable<T>> v, int i, Nullable<T> defval)
      where T : struct
      {
         if (v == null || v.Count == 0)
            return defval;
         else if (i >= v.Count)
            return v.Last();
         else
            return v[i];
      }
   }


   public static partial class Utils
   {
      public static double effectiveFixedRate(List<double> spreads, List < double? > caps, List < double? > floors, int i)
      {
         double result = Get(spreads, i);
         double? floor = Get(floors, i);
         double? cap = Get(caps, i);
         if (floor != null)
            result = Math.Max(Convert.ToDouble(floor), result);
         if (cap != null)
            result = Math.Min(Convert.ToDouble(cap), result);
         return result;
      }

      public static bool noOption(List < double? > caps, List < double? > floors, int i)
      {
         return Get(caps, i) == null && Get(floors, i) == null;
      }

      public static void swap(ref double a1, ref double a2) { swap<double>(ref a1, ref a2); }
      public static void swap<T>(ref T a1, ref T a2)
      {
         T t = a2;
         a2 = a1;
         a1 = t;
      }

      // this is the overload for Pow with int power: much faster and more precise
      public static double Pow(double x, int y)
      {
         int n = Math.Abs(y);
         double retval = 1;
         for (; ; x *= x)
         {
            if ((n & 1) != 0)
               retval *= x;
            if ((n >>= 1) == 0)
               return y < 0 ? 1 / retval : retval;
         }
      }

      public static void QL_REQUIRE(bool condition, Func<string> message, QLNetExceptionEnum exEnum = QLNetExceptionEnum.ArgumentException)
      {
         if (!condition)
            switch (exEnum)
            {
               case QLNetExceptionEnum.ArgumentException:
                  throw new ArgumentException(message.Invoke());
               case QLNetExceptionEnum.NotTradableException:
                  throw new NotTradableException(message.Invoke());
               case QLNetExceptionEnum.RootNotBracketException:
                  throw new RootNotBracketException(message.Invoke());

            }
      }

      public static void QL_FAIL(string message, QLNetExceptionEnum exEnum = QLNetExceptionEnum.ArgumentException)
      {
         switch (exEnum)
         {
            case QLNetExceptionEnum.ArgumentException:
               throw new ArgumentException(message);
            case QLNetExceptionEnum.NotTradableException:
               throw new NotTradableException(message);
            case QLNetExceptionEnum.RootNotBracketException:
               throw new RootNotBracketException(message);
         }
      }

      public static bool is_QL_NEGATIVE_RATES()
      {
#if QL_NEGATIVE_RATES
         return true;
#else
         return false;
#endif
      }

      public static MethodInfo GetMethodInfo(Object t, String function,  Type[] types = null)
      {
         MethodInfo methodInfo;
         if (types == null)
            types = new Type[0];
#if NET452
         methodInfo =  t.GetType().GetMethod(function, types);
#else
         methodInfo = t.GetType().GetRuntimeMethod(function, types);
#endif

         return methodInfo;
      }
   }

   // this is a redefined collection class to emulate array-type behaviour at initialisation
   // if T is a class then the list is initilized with default constructors instead of null
   public class InitializedList<T> : List<T> where T : new ()
   {
      public InitializedList() : base() { }
      public InitializedList(int size) : base(size)
      {
         for (int i = 0; i < this.Capacity; i++)
            this.Add(default(T) == null ? FastActivator<T>.Create() : default(T));
      }
      public InitializedList(int size, T value) : base(size)
      {
         for (int i = 0; i < this.Capacity; i++)
            this.Add(value);
      }

      // erases the contents without changing the size
      public void Erase()
      {
         for (int i = 0; i < this.Count; i++)
            this[i] = default(T);       // do we need to use "new T()" instead of default(T) when T is class?
      }
   }

#if !(NET40 || NET45)
   public interface ICloneable
   {
      object Clone();
   }
#endif
}
