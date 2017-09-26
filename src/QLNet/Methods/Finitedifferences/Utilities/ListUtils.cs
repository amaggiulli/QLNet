/*
 Copyright (C) 2017 Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)
 
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
using System.Text;

namespace QLNet
{
    public static class GenericListUtils
    {
        public static T accumulate<T>(this List<T> list, int first, int last, T init, Func<T, T, T> func)
        {
            T result = init;
            for (int i = first; i < last; i++)
            {
                result = func(result, list[i]);
            }

            return result;
        }

        public static int distance<T>(this List<T> list, T first, T last)
        {
            int iFirst = list.IndexOf(first);
            int iLast = list.IndexOf(last);

            return (Math.Abs(iLast - iFirst + 1) * (iLast < iFirst ? -1 : 1));
        }

        public static void copy<T>(this List<T> input1, int first1, int last1, int first2, List<T> output)
        {
            int index = first2;
            for (int i = first1; i < last1; i++)
            {
                output[index++] = input1[i];
            }
        }

        public static double inner_product(this List<double> input1, int first1, int last1, int first2, List<double> v, double init)
        {
            double sum = init;
            int index = first2;
            for (int i = first1; i < last1; i++)
            {
                sum += v[index++] * input1[i];
            }
            return sum;
        }

        public static int inner_product(this List<int> input1, int first1, int last1, int first2, List<int> v, int init)
        {
            int sum = init;
            int index = first2;
            for (int i = first1; i < last1; i++)
            {
                sum += v[index++] * input1[i];
            }
            return sum;
        }
    }
}
