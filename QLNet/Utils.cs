/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
  
 This file is part of QLNet Project http://qlnet.sourceforge.net/

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
using System.Text;

namespace QLNet 
{
    // here are extensions to IList to accomodate some QL functionality as well as have useful things for .net
    public static partial class Utils {
        public static bool empty<T>(this IList<T> items) { return items.Count == 0; }

        // equivalent of ForEach but with the index
        public static void ForEach<T>(this IList<T> items, Action<int, T> action) {
            if (items != null && action != null)
                for (int idx = 0; idx < items.Count; idx++)
                    action(idx, items[idx]);
        }

        // this is a version of element retrieval with some logic for default values
        public static T Get<T>(this List<T> v, int i) { return Get(v, i, default(T)); }
        public static T Get<T>(this List<T> v, int i, T defval) {
            if (v == null || v.Count == 0) return defval;
            else if (i >= v.Count) return v.Last();
            else return v[i];
        }
    }


    public static partial class Utils {
        public static double effectiveFixedRate(List<double> spreads, List<double> caps, List<double> floors, int i) {
            double result = Get(spreads, i);
            double floor = Get(floors, i);
            double cap = Get(caps, i);
            if (floor != default(double)) result = Math.Max(floor, result);
            if (cap != default(double)) result = Math.Min(cap, result);
            return result;
        }

        public static bool noOption(List<double> caps, List<double> floors, int i) {
            return (Get(caps, i) == default(double)) && (Get(floors, i) == default(double));
        }

        public static void swap(ref double a1, ref double a2) { swap<double>(ref a1, ref a2); }
        public static void swap<T>(ref T a1, ref T a2) {
            T t = a2;
            a2 = a1;
            a1 = t;
        }

        // this is the overload for Pow with int power: much faster and more precise
        public static double Pow(double x, int y) {
            int n = Math.Abs(y);
            double retval = 1;
            for (; ; x *= x) {
                if ((n & 1) != 0) retval *= x;
                if ((n >>= 1) == 0) return y < 0 ? 1 / retval : retval;
            }
        }
        
       public static void QL_REQUIRE( bool condition, Func<string> message )
       {
          if ( !condition )
            throw new ApplicationException( message.Invoke() );
       }
       public static void QL_FAIL(string message)
       {
          throw new ApplicationException(message);
       }

		 public static bool is_QL_NEGATIVE_RATES()
		 {
			 #if QL_NEGATIVE_RATES
				return true;
			 #else
			    return false;
		    #endif
		 }
    }

    // this is a redefined collection class to emulate array-type behaviour at initialisation
    // if T is a class then the list is initilized with default constructors instead of null
    public class InitializedList<T> : List<T> where T : new() {
        public InitializedList() : base() { }
        public InitializedList(int size) : base(size) {
            for (int i = 0; i < this.Capacity; i++)
                this.Add(default(T) == null ? new T() : default(T));
        }
        public InitializedList(int size, T value) : base(size) {
            for (int i = 0; i < this.Capacity; i++)
                this.Add(value);
        }

        // erases the contents without changing the size
        public void Erase() {
            for (int i = 0; i < this.Count; i++)
                this[i] = default(T);       // do we need to use "new T()" instead of default(T) when T is class?
        }
    }
}
