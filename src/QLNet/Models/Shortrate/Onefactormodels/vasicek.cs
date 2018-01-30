﻿/*
 Copyright (C) 2010 Philippe Real (ph_real@hotmail.com)
 Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)

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

namespace QLNet
{
   /// <summary>
   /// Vasicek model class
   /// </summary>
   public class Vasicek : OneFactorAffineModel
   {
      public Vasicek(double r0, double a, double b = 0.05, double sigma = 0.01, double lambda = 0.0)
         : base(4)
      {
         r0_ = r0;
         a_ = arguments_[0];
         b_ = arguments_[1];
         sigma_ = arguments_[2];
         lambda_ = arguments_[3];
         a_ = arguments_[0] = new ConstantParameter(a, new PositiveConstraint());
         b_ = arguments_[1] = new ConstantParameter(b, new NoConstraint());
         sigma_ = arguments_[2] = new ConstantParameter(sigma, new PositiveConstraint());
         lambda_ = arguments_[3] = new ConstantParameter(lambda, new NoConstraint());
      }

      public override double discountBondOption(Option.Type type, double strike, double maturity,
         double bondMaturity)
      {
         double v;
         double _a = a();
         if (Math.Abs(maturity) < Const.QL_EPSILON)
         {
            v = 0.0;
         }
         else if (_a < Math.Sqrt(Const.QL_EPSILON))
         {
            v = sigma() * B(maturity, bondMaturity) * Math.Sqrt(maturity);
         }
         else
         {
            v = sigma() * B(maturity, bondMaturity) *
                Math.Sqrt(0.5 * (1.0 - Math.Exp(-2.0 * _a * maturity)) / _a);
         }
         double f = discountBond(0.0, bondMaturity, r0_);
         double k = discountBond(0.0, maturity, r0_) * strike;

         return Utils.blackFormula(type, k, f, v);
      }

      public override ShortRateDynamics dynamics()
      {
         return new Dynamics(a(), b(), sigma(), r0_);
      }

      protected override double A(double t, double T)
      {
         double _a = a();
         if (_a < Math.Sqrt(Const.QL_EPSILON))
         {
            return 0.0;
         }
         else
         {
            double sigma2 = sigma() * sigma();
            double bt = B(t, T);
            return Math.Exp((b() + lambda() * sigma() / _a
                             - 0.5 * sigma2 / (_a * _a)) * (bt - (T - t))
                            - 0.25 * sigma2 * bt * bt / _a);
         }
      }

      protected override double B(double t, double T)
      {
         double _a = a();
         if (_a < Math.Sqrt(Const.QL_EPSILON))
            return (T - t);
         else
            return (1.0 - Math.Exp(-_a * (T - t))) / _a;
      }

      public double a()
      {
         return a_.value(0.0);
      }

      public double b()
      {
         return b_.value(0.0);
      }

      public double lambda()
      {
         return lambda_.value(0.0);
      }

      public double sigma()
      {
         return sigma_.value(0.0);
      }

      protected double r0_;
      protected Parameter a_;
      protected Parameter b_;
      protected Parameter sigma_;
      protected Parameter lambda_;

      //! Short-rate dynamics in the %Vasicek model
      /*! The short-rate follows an Ornstein-Uhlenbeck process with mean
          \f$ b \f$.
      */

      public class Dynamics : ShortRateDynamics
      {
         public Dynamics(double a, double b, double sigma, double r0)
            : base(new OrnsteinUhlenbeckProcess(a, sigma, r0 - b))
         {
            a_ = a;
            b_ = b;
            r0_ = r0;
         }

         public override double variable(double t, double r)
         {
            return r - b_;
         }

         public override double shortRate(double t, double x)
         {
            return x + b_;
         }

         private double a_, b_, r0_;
      }
   }
}
