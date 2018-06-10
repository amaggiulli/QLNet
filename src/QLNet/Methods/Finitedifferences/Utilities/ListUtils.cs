/*
 Copyright (C) 2017 Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)

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
using System.Text;

namespace QLNet
{
   public static class GenericListUtils
   {
      public static T accumulate<T>(this IList<T> list, int first, int last, T init, Func<T, T, T> func)
      {
         T result = init;
         for (int i = first; i < last; i++)
         {
            result = func(result, list[i]);
         }

         return result;
      }

      public static int distance<T>(this IList<T> list, T first, T last)
      {
         int iFirst = list.IndexOf(first);
         int iLast = list.IndexOf(last);

         return (Math.Abs(iLast - iFirst + 1) * (iLast < iFirst ? -1 : 1));
      }

      public static void copy<T>(this IList<T> input1, int first1, int last1, int first2, IList<T> output)
      {
         int index = first2;
         for (int i = first1; i < last1; i++)
         {
            output[index++] = input1[i];
         }
      }

      public static double inner_product(this IList<double> input1, int first1, int last1, int first2, IList<double> v, double init)
      {
         double sum = init;
         int index = first2;
         for (int i = first1; i < last1; i++)
         {
            sum += v[index++] * input1[i];
         }
         return sum;
      }

      public static int inner_product(this IList<int> input1, int first1, int last1, int first2, IList<int> v, int init)
      {
         int sum = init;
         int index = first2;
         for (int i = first1; i < last1; i++)
         {
            sum += v[index++] * input1[i];
         }
         return sum;
      }

      static readonly Random generator = new Random(1);

      public static void Shuffle<T>(this IList<T> sequence)
      {
         // The simplest and most efficient way to return a random subet is to perform
         // an in-place, partial Fisher-Yates shuffle of the sequence. While we could do
         // a full shuffle, it would be wasteful in the cases where subsetSize is shorter
         // than the length of the sequence.
         // See: http://en.wikipedia.org/wiki/Fisher%E2%80%93Yates_shuffle

         var m = 0;                // keeps track of count items shuffled
         var w = sequence.Count;  // upper bound of shrinking swap range
         var g = w - 1;            // used to compute the second swap index

         // perform in-place, partial Fisher-Yates shuffle
         while (m < w)
         {
            var k = g - generator.Next(w);
            var tmp = sequence[k];
            sequence[k] = sequence[m];
            sequence[m] = tmp;
            ++m;
            --w;
         }
      }
      public static IList<T> Clone<T>(this IList<T> input) where T : ICloneable, new ()
      {
         IList<T> c = new InitializedList<T>(input.Count);
         c.ForEach((ii, vv) => c[ii] = (T)input[ii].Clone());
         return c;
      }
   }
}
