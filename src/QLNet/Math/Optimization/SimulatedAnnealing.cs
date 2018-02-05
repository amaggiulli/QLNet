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

namespace QLNet
{
   /// <summary>
   /// Class RNG must implement the following interface:
   /// RNG.sample_type RNG.next();
   /// </summary>
   /// <typeparam name="RNG"></typeparam>
   public class SimulatedAnnealing<RNG> : OptimizationMethod where RNG : class, IRNGTraits, new ()
   {
      public enum Scheme
      {
         ConstantFactor,
         ConstantBudget
      }

      /// <summary>
      /// reduce temperature T by a factor of  (1-\epsilon) after m moves
      /// </summary>
      /// <param name="lambda"></param>
      /// <param name="T0"></param>
      /// <param name="epsilon"></param>
      /// <param name="m"></param>
      /// <param name="rng"></param>
      public SimulatedAnnealing(double lambda, double T0, double epsilon, int m, RNG rng = default(RNG))
      {
         scheme_ = Scheme.ConstantFactor;
         lambda_ = lambda;
         T0_ = T0;
         epsilon_ = epsilon;
         alpha_ = 0.0;
         K_ = 0;
         rng_ = rng;
         m_ = m;
      }

      /// <summary>
      /// budget a total of K moves, set temperature T to the initial
      /// temperature times \f$ ( 1 - k/K )^\alpha \f$ with k being the total number
      /// of moves so far. After K moves the temperature is guaranteed to be
      /// zero, after that the optimization runs like a deterministic simplex
      /// algorithm.
      /// </summary>
      /// <param name="lambda"></param>
      /// <param name="T0"></param>
      /// <param name="K"></param>
      /// <param name="alpha"></param>
      /// <param name="rng"></param>
      public SimulatedAnnealing(double lambda, double T0, int K, double alpha, RNG rng = default(RNG))
      {
         scheme_ = Scheme.ConstantBudget;
         lambda_ = lambda;
         T0_ = T0;
         epsilon_ = 0.0;
         alpha_ = alpha;
         K_ = K;
         rng_ = rng;
      }

      public override EndCriteria.Type minimize(Problem P, EndCriteria endCriteria)
      {
         int stationaryStateIterations_ = 0;
         EndCriteria.Type ecType = EndCriteria.Type.None;
         P.reset();
         Vector x = P.currentValue();
         iteration_ = 0;
         n_ = x.size();
         ptry_ = new Vector(n_, 0.0);

         // build vertices

         vertices_ = new InitializedList<Vector>(n_ + 1, x);
         for (i_ = 0; i_ < n_; i_++)
         {
            Vector direction = new Vector(n_, 0.0);
            direction[i_] = 1.0;
            Vector tmp = vertices_[i_ + 1];
            P.constraint().update(ref tmp, direction, lambda_);
            vertices_[i_ + 1] = tmp;
         }

         values_ = new Vector(n_ + 1, 0.0);
         for (i_ = 0; i_ <= n_; i_++)
         {
            if (!P.constraint().test(vertices_[i_]))
               values_[i_] = Double.MaxValue;
            else
               values_[i_] = P.value(vertices_[i_]);
            if (Double.IsNaN(ytry_))
            {
               // handle NAN
               values_[i_] = Double.MaxValue;
            }
         }

         // minimize

         T_ = T0_;
         yb_ = Double.MaxValue;
         pb_ = new Vector(n_, 0.0);
         do
         {
            iterationT_ = iteration_;
            do
            {
               sum_ = new Vector(n_, 0.0);
               for (i_ = 0; i_ <= n_; i_++)
                  sum_ += vertices_[i_];
               tt_ = -T_;
               ilo_ = 0;
               ihi_ = 1;
               ynhi_ = values_[0] + tt_ * Math.Log(rng_.next().value);
               ylo_ = ynhi_;
               yhi_ = values_[1] + tt_ * Math.Log(rng_.next().value);
               if (ylo_ > yhi_)
               {
                  ihi_ = 0;
                  ilo_ = 1;
                  ynhi_ = yhi_;
                  yhi_ = ylo_;
                  ylo_ = ynhi_;
               }

               for (i_ = 2; i_ < n_ + 1; i_++)
               {
                  yt_ = values_[i_] + tt_ * Math.Log(rng_.next().value);
                  if (yt_ <= ylo_)
                  {
                     ilo_ = i_;
                     ylo_ = yt_;
                  }

                  if (yt_ > yhi_)
                  {
                     ynhi_ = yhi_;
                     ihi_ = i_;
                     yhi_ = yt_;
                  }
                  else
                  {
                     if (yt_ > ynhi_)
                     {
                        ynhi_ = yt_;
                     }
                  }
               }

               // GSL end criterion in x (cf. above)
               if (endCriteria.checkStationaryPoint(simplexSize(), 0.0,
                                                    ref stationaryStateIterations_,
                                                    ref ecType) ||
                   endCriteria.checkMaxIterations(iteration_, ref ecType))
               {
                  // no matter what, we return the best ever point !
                  P.setCurrentValue(pb_);
                  P.setFunctionValue(yb_);
                  return ecType;
               }

               iteration_ += 2;
               amotsa(P, -1.0);
               if (ytry_ <= ylo_)
               {
                  amotsa(P, 2.0);
               }
               else
               {
                  if (ytry_ >= ynhi_)
                  {
                     ysave_ = yhi_;
                     amotsa(P, 0.5);
                     if (ytry_ >= ysave_)
                     {
                        for (i_ = 0; i_ < n_ + 1; i_++)
                        {
                           if (i_ != ilo_)
                           {
                              for (j_ = 0; j_ < n_; j_++)
                              {
                                 sum_[j_] = 0.5 * (vertices_[i_][j_] +
                                                   vertices_[ilo_][j_]);
                                 vertices_[i_][j_] = sum_[j_];
                              }

                              values_[i_] = P.value(sum_);
                           }
                        }

                        iteration_ += n_;
                        for (i_ = 0; i_ < n_; i_++)
                           sum_[i_] = 0.0;
                        for (i_ = 0; i_ <= n_; i_++)
                           sum_ += vertices_[i_];
                     }
                  }
                  else
                  {
                     iteration_ += 1;
                  }
               }
            }
            while (iteration_ <
                   iterationT_ + (scheme_ == Scheme.ConstantFactor ? m_ : 1));

            switch (scheme_)
            {
               case Scheme.ConstantFactor:
                  T_ *= (1.0 - epsilon_);
                  break;
               case Scheme.ConstantBudget:
                  if (iteration_ <= K_)
                     T_ = T0_ *
                          Math.Pow(1.0 - Convert.ToDouble(iteration_) / Convert.ToDouble(K_), alpha_);
                  else
                     T_ = 0.0;
                  break;
            }
         }
         while (true);
      }

      protected Scheme scheme_;
      protected double lambda_, T0_, epsilon_, alpha_;
      protected int K_;
      protected RNG rng_;

      protected double simplexSize()
      {
         Vector center = new Vector(vertices_.First().size(), 0);
         for (int i = 0; i < vertices_.Count; ++i)
            center += vertices_[i];

         center *= 1 / Convert.ToDouble(vertices_.Count);
         double result = 0;
         for (int i = 0; i < vertices_.Count; ++i)
         {
            Vector temp = vertices_[i] - center;
            result += Vector.Norm2(temp);
         }

         return result / Convert.ToDouble(vertices_.Count);
      }

      protected void amotsa(Problem P, double fac)
      {
         fac1_ = (1.0 - fac) / Convert.ToDouble(n_);
         fac2_ = fac1_ - fac;
         for (j_ = 0; j_ < n_; j_++)
         {
            ptry_[j_] = sum_[j_] * fac1_ - vertices_[ihi_][j_] * fac2_;
         }

         if (!P.constraint().test(ptry_))
            ytry_ = Double.MaxValue;
         else
            ytry_ = P.value(ptry_);
         if (Double.IsNaN(ytry_))
         {
            ytry_ = Double.MaxValue;
         }

         if (ytry_ <= yb_)
         {
            yb_ = ytry_;
            pb_ = ptry_;
         }

         yflu_ = ytry_ - tt_ * Math.Log(rng_.next().value);
         if (yflu_ < yhi_)
         {
            values_[ihi_] = ytry_;
            yhi_ = yflu_;
            for (j_ = 0; j_ < n_; j_++)
            {
               sum_[j_] += ptry_[j_] - vertices_[ihi_][j_];
               vertices_[ihi_][j_] = ptry_[j_];
            }
         }

         ytry_ = yflu_;
      }

      protected double T_;
      protected List<Vector> vertices_;
      protected Vector values_, sum_;
      protected int i_, ihi_, ilo_, j_, m_, n_;
      protected double fac1_, fac2_, yflu_;
      protected double rtol_, swap_, yhi_, ylo_, ynhi_, ysave_, yt_, ytry_, yb_, tt_;
      protected Vector pb_, ptry_;
      protected int iteration_, iterationT_;
   }
}
