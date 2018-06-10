/*
 Copyright (C) 2009 Philippe Real (ph_real@hotmail.com)
 Copyright (C) 2008-2017  Andrea Maggiulli (a.maggiulli@gmail.com)

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

namespace QLNet
{
   //! Statistic tool for sequences with discrepancy calculation
   /*! It inherit from SequenceStatistics<Statistics> and adds
       \f$ L^2 \f$ discrepancy calculation
   */

   public sealed class DiscrepancyStatistics : SequenceStatistics
   {
      // constructor
      public DiscrepancyStatistics(int dimension)
         : base(dimension)
      {
         reset(dimension);
      }

      //!  1-dimensional inspectors
      public double discrepancy()
      {
         int N = samples();
         if (N == 0)
            return 0;
         return Math.Sqrt(adiscr_ / (N * N) - bdiscr_ / N * cdiscr_ + ddiscr_);
      }

      public override void add
         (List<double> begin)
      {
         add
            (begin, 1);
      }

      public override void add
         (List<double> begin, double weight)
      {
         base.add(begin, weight);
         int k, m, N;
         N = samples();

         double r_ik, r_jk, temp;
         temp = 1.0;

         for (k = 0; k < dimension_; ++k)
         {
            r_ik = begin[k]; //i=N
            temp *= (1.0 - r_ik * r_ik);
         }
         cdiscr_ += temp;

         for (m = 0; m < N - 1; m++)
         {
            temp = 1.0;
            for (k = 0; k < dimension_; ++k)
            {
               // running i=1..(N-1)
               r_ik = 0;
               // fixed j=N
               r_jk = begin[k];
               temp *= (1.0 - Math.Max(r_ik, r_jk));
            }
            adiscr_ += temp;

            temp = 1.0;
            for (k = 0; k < dimension_; ++k)
            {
               // fixed i=N
               r_ik = begin[k];
               // running j=1..(N-1)
               r_jk = 0;
               temp *= (1.0 - Math.Max(r_ik, r_jk));
            }
            adiscr_ += temp;
         }
         temp = 1.0;
         for (k = 0; k < dimension_; ++k)
         {
            // fixed i=N, j=N
            r_ik = r_jk = begin[k];
            temp *= (1.0 - Math.Max(r_ik, r_jk));
         }
         adiscr_ += temp;
      }

      public override void reset(int dimension)
      {
         if (dimension == 0) // if no size given,
            dimension = dimension_; // keep the current one

         Utils.QL_REQUIRE(dimension != 1, () => "dimension==1 not allowed");

         base.reset(dimension);

         adiscr_ = 0.0;
         bdiscr_ = 1.0 / Math.Pow(2.0, dimension - 1);
         cdiscr_ = 0.0;
         ddiscr_ = 1.0 / Math.Pow(3.0, dimension);
      }

      private double adiscr_, cdiscr_;
      private double bdiscr_, ddiscr_;
   }
}