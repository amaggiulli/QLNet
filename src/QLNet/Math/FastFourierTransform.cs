/*
 Copyright (C) 2018 Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)

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
using System.Numerics;

namespace QLNet
{
   // FFT implementation
   public class FastFourierTransform
   {
      // the minimum order required for the given input size
      public static int min_order(int inputSize)
      {
         return (int)(Math.Ceiling(Math.Log(Convert.ToDouble(inputSize)) / Math.Log(2.0)));
      }

      public FastFourierTransform(int order)
      {
         int m = 1 << order;
         cs_ = new Vector(order);
         sn_ = new Vector(order);
         cs_[order - 1] = Math.Cos(2.0 * Math.PI / m);
         sn_[order - 1] = Math.Sin(2.0 * Math.PI / m);

         for (int i = order - 1; i > 0; --i)
         {
            cs_[i - 1] = cs_[i] * cs_[i] - sn_[i] * sn_[i];
            sn_[i - 1] = 2.0 * sn_[i] * cs_[i];
         }
      }

      // The required size for the output vector
      public int output_size() { return 1 << cs_.size(); }

      // FFT transform.
      /* The output sequence must be allocated by the user */
      public void transform(List<Complex> input,
                            int inputBeg,
                            int inputEnd,
                            List<Complex> output)
      {
         transform_impl(input, inputBeg, inputEnd, output, false);
      }

      // Inverse FFT transform.
      /* The output sequence must be allocated by the user. */
      public void inverse_transform(List<Complex> input,
                                    int inputBeg,
                                    int inputEnd,
                                    List<Complex> output)
      {
         transform_impl(input, inputBeg, inputEnd, output, true);
      }

      protected void transform_impl(List<Complex> input,
                                    int inputBeg,
                                    int inputEnd,
                                    List<Complex> output,
                                    bool inverse)
      {
         int order = cs_.size();
         int N = 1 << order;

         int i;
         for (i = inputBeg; i < inputEnd; ++i)
         {
            output[bit_reverse(i, order)] = new Complex(input[i].Real, input[i].Imaginary);
         }
         Utils.QL_REQUIRE(i <= N, () => "FFT order is too small");
         for (int s = 1; s <= order; ++s)
         {
            int m = 1 << s;
            Complex w = new Complex(1.0, 0.0);
            Complex wm = new Complex(cs_[s - 1], inverse ? sn_[s - 1] : -sn_[s - 1]);
            for (int j = 0; j < m / 2; ++j)
            {
               for (int k = j; k < N; k += m)
               {
                  Complex t = w * (output[k + m / 2]);
                  Complex u = new Complex(output[k].Real, output[k].Imaginary);
                  output[k] = u + t;
                  output[k + m / 2] = u - t;
               }
               w *= wm;
            }
         }
      }

      public static int bit_reverse(int x, int order)
      {
         int n = 0;
         for (int i = 0; i < order; ++i)
         {
            n <<= 1;
            n |= (x & 1);
            x >>= 1;
         }
         return n;
      }

      protected Vector cs_, sn_;
   }
}
