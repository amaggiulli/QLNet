/*
 Copyright (C) 2017, 2020 Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)

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

/*! \file fdmblackscholesmesher.cpp
    \brief 1-d mesher for the Black-Scholes process (in ln(S))
*/

namespace QLNet
{
   public class Pair<TFirst, TSecond>
   {
      protected KeyValuePair<TFirst, TSecond> pair;

      public Pair() { }

      public Pair(TFirst first, TSecond second)
      {
         pair = new KeyValuePair<TFirst, TSecond>(first, second);
      }

      public void set(TFirst first, TSecond second)
      {
         pair = new KeyValuePair<TFirst, TSecond>(first, second);
      }

      public TFirst first
      {
         get
         {
            return pair.Key;
         }
      }

      public TSecond second
      {
         get
         {
            return pair.Value;
         }
      }
   }

   public class pair_double : Pair<double, double>, IComparable<Pair<double, double>>
   {
      public pair_double(double first, double second)
         : base(first, second)
      { }

      public int CompareTo(Pair<double, double> other)
      {
         return first.CompareTo(other.first);
      }
   }

   public class equal_on_first : IEqualityComparer < Pair < double?, double? >>
   {
      public bool Equals(Pair < double?, double? > p1,
                         Pair < double?, double? > p2)
      {
         return Utils.close_enough(p1.first.Value, p2.first.Value, 1000);
      }

      public int GetHashCode(Pair < double?, double? > p)
      {
         return Convert.ToInt32(p.first.Value * p.second.Value);
      }

   }

   public class FdmBlackScholesMesher : Fdm1dMesher
   {
      public FdmBlackScholesMesher(int size,
                                   GeneralizedBlackScholesProcess process,
                                   double maturity, double strike,
                                   double? xMinConstraint = null,
                                   double? xMaxConstraint = null,
                                   double eps = 0.0001,
                                   double scaleFactor = 1.5,
                                   Pair < double?, double? > cPoint
                                   = null,
                                   DividendSchedule dividendSchedule = null,
                                   FdmQuantoHelper fdmQuantoHelper = null,
                                   double spotAdjustment = 0.0)
      : base(size)
      {
         double S = process.x0();
         Utils.QL_REQUIRE(S > 0.0, () => "negative or null underlying given");

         dividendSchedule = dividendSchedule == null ? new DividendSchedule() : dividendSchedule;
         List<pair_double> intermediateSteps = new List<pair_double>();
         for (int i = 0; i < dividendSchedule.Count
              && process.time(dividendSchedule[i].date()) <= maturity; ++i)
            intermediateSteps.Add(
               new pair_double(
                  process.time(dividendSchedule[i].date()),
                  dividendSchedule[i].amount()
               ));

         int intermediateTimeSteps = (int)Math.Max(2, 24.0 * maturity);
         for (int i = 0; i < intermediateTimeSteps; ++i)
            intermediateSteps.Add(
               new pair_double((i + 1) * (maturity / intermediateTimeSteps), 0.0));

         intermediateSteps.Sort();

         Handle<YieldTermStructure> rTS = process.riskFreeRate();
         Handle<YieldTermStructure> qTS = fdmQuantoHelper != null
                                          ? new Handle<YieldTermStructure>(
                                             new QuantoTermStructure(process.dividendYield(),
                                                                     process.riskFreeRate(),
                                                                     new Handle<YieldTermStructure>(fdmQuantoHelper.foreignTermStructure()),
                                                                     process.blackVolatility(),
                                                                     strike,
                                                                     new Handle<BlackVolTermStructure>(fdmQuantoHelper.fxVolatilityTermStructure()),
                                                                     fdmQuantoHelper.exchRateATMlevel(),
                                                                     fdmQuantoHelper.equityFxCorrelation()))
                                          : process.dividendYield();

         double lastDivTime = 0.0;
         double fwd = S + spotAdjustment;
         double mi = fwd, ma = fwd;

         for (int i = 0; i < intermediateSteps.Count; ++i)
         {
            double divTime = intermediateSteps[i].first;
            double divAmount = intermediateSteps[i].second;

            fwd = fwd / rTS.currentLink().discount(divTime) * rTS.currentLink().discount(lastDivTime)
                  * qTS.currentLink().discount(divTime) / qTS.currentLink().discount(lastDivTime);

            mi = Math.Min(mi, fwd); ma = Math.Max(ma, fwd);

            fwd -= divAmount;

            mi = Math.Min(mi, fwd); ma = Math.Max(ma, fwd);

            lastDivTime = divTime;
         }

         // Set the grid boundaries
         double normInvEps = new InverseCumulativeNormal().value(1 - eps);
         double sigmaSqrtT
            = process.blackVolatility().currentLink().blackVol(maturity, strike)
              * Math.Sqrt(maturity);

         double? xMin = Math.Log(mi) - sigmaSqrtT * normInvEps * scaleFactor;
         double? xMax = Math.Log(ma) + sigmaSqrtT * normInvEps * scaleFactor;

         if (xMinConstraint != null)
         {
            xMin = xMinConstraint;
         }
         if (xMaxConstraint != null)
         {
            xMax = xMaxConstraint;
         }

         Fdm1dMesher helper;
         if (cPoint != null
             && cPoint.first != null
             && Math.Log(cPoint.first.Value) >= xMin && Math.Log(cPoint.first.Value) <= xMax)
         {

            helper = new Concentrating1dMesher(xMin.Value, xMax.Value, size,
                                               new Pair < double?, double? >(Math.Log(cPoint.first.Value), cPoint.second));
         }
         else
         {
            helper = new Uniform1dMesher(xMin.Value, xMax.Value, size);
         }

         locations_ = helper.locations();
         for (int i = 0; i < locations_.Count; ++i)
         {
            dplus_[i]  = helper.dplus(i);
            dminus_[i] = helper.dminus(i);
         }
      }

      public static GeneralizedBlackScholesProcess processHelper(Handle<Quote> s0,
                                                                 Handle<YieldTermStructure> rTS,
                                                                 Handle<YieldTermStructure> qTS,
                                                                 double vol)
      {
         return new GeneralizedBlackScholesProcess(
                   s0, qTS, rTS,
                   new Handle<BlackVolTermStructure>(
                      new BlackConstantVol(rTS.currentLink().referenceDate(),
                                           new Calendar(),
                                           vol,
                                           rTS.currentLink().dayCounter())));
      }
   }
}
