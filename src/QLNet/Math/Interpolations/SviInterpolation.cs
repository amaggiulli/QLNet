/*
 Copyright (C) 2008-2015  Andrea Maggiulli (a.maggiulli@gmail.com)
               2017       Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)

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

namespace QLNet
{
   public class SVIWrapper : IWrapper
   {
      public SVIWrapper(double t, double forward, List<double?> param, List<double?> addParams)
      {
         t_ = t;
         forward_ = forward;
         params_ = param;
         Utils.checkSviParameters(param[0].Value, param[1].Value, param[2].Value, param[3].Value, param[4].Value);
      }

      public double volatility(double x)
      {
         return Utils.sviVolatility(x, forward_, t_, params_);
      }

      private double t_, forward_;
      private List<double?> params_;
   }

   public struct SVISpecs : IModel
   {
      public int dimension() { return 5; }

      public void defaultValues(List<double?> param, List<bool> paramIsFixed, double forward, double expiryTime, List<double?> addParams)
      {
         if (param[2] == null)
            param[2] = 0.1;
         if (param[3] == null)
            param[3] = -0.4;
         if (param[4] == null)
            param[4] = 0.0;
         if (param[1] == null)
            param[1] = 2.0 / (1.0 + Math.Abs(Convert.ToDouble(param[3])));
         if (param[0] == null)
            param[0] = Math.Max(0.20 * 0.20 * expiryTime -
                                (double)param[1] * ((double)param[3] * (-(double)param[4]) +
                                            Math.Sqrt((-(double)param[4]) * (-(double)param[4]) +
                                                        (double)param[2] * (double)param[2])),
                                -(double)param[1] * (double)param[2] *
                                Math.Sqrt(1.0 - (double)param[3] * (double)param[3]) + eps1());
      }

      public void guess(Vector values, List<bool> paramIsFixed, double forward, double expiryTime, List<double> r, List<double?> addParams)
      {
         int j = 0;
         if (!paramIsFixed[2])
            values[2] = r[j++] + eps1();
         if (!paramIsFixed[3])
            values[3] = (2.0 * r[j++] - 1.0) * eps2();
         if (!paramIsFixed[4])
            values[4] = (2.0 * r[j++] - 1.0);
         if (!paramIsFixed[1])
            values[1] = r[j++] * 4.0 / (1.0 + Math.Abs(values[3])) * eps2();
         if (!paramIsFixed[0])
            values[0] = r[j++] * expiryTime -
                        eps2() * (values[1] * values[2] *
                                  Math.Sqrt(1.0 - values[3] * values[3]));
      }

      public double eps1() { return 0.000001; }

      public double eps2() { return 0.999999; }

      public double dilationFactor() { return 0.001; }

      public Vector inverse(Vector y, List<bool> b, List<double?> c, double d)
      {
         Vector x = new Vector(5);
         x[2] = Math.Sqrt(y[2] - eps1());
         x[3] = Math.Asin(y[3] / eps2());
         x[4] = y[4];
         x[1] = Math.Tan(y[1] / 4.0 * (1.0 + Math.Abs(y[3])) / eps2() * Const.M_PI -
                         Const.M_PI / 2.0);
         x[0] = Math.Sqrt(y[0] - eps1() +
                          y[1] * y[2] * Math.Sqrt(1.0 - y[3] * y[3]));
         return x;
      }

      public Vector direct(Vector x, List<bool> paramIsFixed, List<double?> param, double forward)
      {
         Vector y = new Vector(5);
         y[2] = x[2] * x[2] + eps1();
         y[3] = Math.Sin(x[3]) * eps2();
         y[4] = x[4];
         if (paramIsFixed[1])
            y[1] = Convert.ToDouble(param[1]);
         else
            y[1] = (Math.Atan(x[1]) + Const.M_PI / 2.0) / Const.M_PI * eps2() * 4.0 /
                   (1.0 + Math.Abs(y[3]));
         if (paramIsFixed[0])
            y[0] = Convert.ToDouble(param[0]);
         else
            y[0] = eps1() + x[0] * x[0] -
                   y[1] * y[2] * Math.Sqrt(1.0 - y[3] * y[3]);
         return y;
      }

      public IWrapper instance(double t, double forward, List<double?> param, List<double?> addParams)
      {
         return new SVIWrapper(t, forward, param, addParams);
      }

      public double weight(double strike, double forward, double stdDev, List<double?> addParams)
      {
         return Utils.blackFormulaStdDevDerivative(strike, forward, stdDev, 1.0);
      }

      public SVIWrapper modelInstance_ { get; set; }
   }

   //! %SABR smile interpolation between discrete volatility points.
   public class SviInterpolation : Interpolation
   {
      public SviInterpolation(List<double> xBegin,  // x = strikes
                              int size,
                              List<double> yBegin,  // y = volatilities
                              double t,             // option expiry
                              double forward,
                              double? a,
                              double? b,
                              double? sigma,
                              double? rho,
                              double? m,
                              bool aIsFixed,
                              bool bIsFixed,
                              bool sigmaIsFixed,
                              bool rhoIsFixed,
                              bool mIsFixed, bool vegaWeighted = true,
                              EndCriteria endCriteria = null,
                              OptimizationMethod optMethod = null,
                              double errorAccept = 0.0020,
                              bool useMaxError = false,
                              int maxGuesses = 50,
                              List<double?> addParams = null)
      {
         impl_ = new XABRInterpolationImpl<SVISpecs>(
                 xBegin, size, yBegin, t, forward,
                 new List<double?>() { a, b, sigma, rho, m },
                 new List<bool>() { aIsFixed, bIsFixed, sigmaIsFixed, rhoIsFixed, mIsFixed },
                 vegaWeighted, endCriteria, optMethod, errorAccept, useMaxError,
                 maxGuesses, addParams);
         coeffs_ = (impl_ as XABRInterpolationImpl<SVISpecs>).coeff_;
      }

      public double expiry() { return coeffs_.t_; }

      public double forward() { return coeffs_.forward_; }

      public double a() { return coeffs_.params_[0].Value; }

      public double b() { return coeffs_.params_[1].Value; }

      public double sigma() { return coeffs_.params_[2].Value; }

      public double rho() { return coeffs_.params_[3].Value; }

      public double m() { return coeffs_.params_[4].Value; }

      public double rmsError() { return coeffs_.error_.Value; }

      public double maxError() { return coeffs_.maxError_.Value; }

      public List<double> interpolationWeights() { return coeffs_.weights_; }

      public EndCriteria.Type endCriteria() { return coeffs_.XABREndCriteria_; }

      private XABRCoeffHolder<SVISpecs> coeffs_;
   }

   //! %SABR interpolation factory and traits
   public class SVI
   {
      public SVI(double t, double forward, double a, double b, double sigma, double rho, double m,
                  bool aIsFixed, bool bIsFixed, bool sigmaIsFixed, bool rhoIsFixed, bool mIsFixed,
                  bool vegaWeighted = false,
                  EndCriteria endCriteria = null,
                  OptimizationMethod optMethod = null,
                  double errorAccept = 0.0020, bool useMaxError = false, int maxGuesses = 50, List<double?> addParams = null)
      {
         t_ = t;
         forward_ = forward;
         a_ = a;
         b_ = b;
         sigma_ = sigma;
         rho_ = rho;
         m_ = m;
         aIsFixed_ = aIsFixed;
         bIsFixed_ = bIsFixed;
         sigmaIsFixed_ = sigmaIsFixed;
         rhoIsFixed_ = rhoIsFixed;
         mIsFixed_ = mIsFixed;
         vegaWeighted_ = vegaWeighted;
         endCriteria_ = endCriteria;
         optMethod_ = optMethod;
         errorAccept_ = errorAccept;
         useMaxError_ = useMaxError;
         maxGuesses_ = maxGuesses;
         addParams_ = addParams;
      }

      public Interpolation interpolate(List<double> xBegin, int xEnd, List<double> yBegin)
      {
         return new SviInterpolation(xBegin, xEnd, yBegin, t_, forward_, a_, b_, sigma_, rho_, m_,
                aIsFixed_, bIsFixed_, sigmaIsFixed_, rhoIsFixed_, mIsFixed_, vegaWeighted_,
                endCriteria_, optMethod_, errorAccept_, useMaxError_, maxGuesses_);
      }

      public const bool global = true;

      private double t_;
      private double forward_;
      private double a_, b_, sigma_, rho_, m_;
      private bool aIsFixed_, bIsFixed_, sigmaIsFixed_, rhoIsFixed_, mIsFixed_;
      private bool vegaWeighted_;
      private EndCriteria endCriteria_;
      private OptimizationMethod optMethod_;
      private double errorAccept_;
      private bool useMaxError_;
      private int maxGuesses_;
      private List<double?> addParams_;
   }
}
