//  Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)
//
//  This file is part of QLNet Project https://github.com/amaggiulli/qlnet
//  QLNet is free software: you can redistribute it and/or modify it
//  under the terms of the QLNet license.  You should have received a
//  copy of the license along with this program; if not, license is
//  available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.
//
//  QLNet is a based on QuantLib, a free-software/open-source library
//  for financial quantitative analysts and developers - http://quantlib.org/
//  The QuantLib license is available online at http://quantlib.org/license.shtml.
//
//  This program is distributed in the hope that it will be useful, but WITHOUT
//  ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
//  FOR A PARTICULAR PURPOSE.  See the license for more details.

using System.Collections.Generic;

namespace QLNet
{
    /// <summary>
    /// Cubic functional form
    /// </summary>
    /// <remarks>
    /// The polynomial is defined as <c>f(t) = sum(c_i * t^i)</c>.
    /// </remarks>
    public class PolynomialFunction
   {
      public PolynomialFunction(List<double> coeff)
      {
         Utils.QL_REQUIRE(!coeff.empty(), () => "empty coefficient vector");
         order_ = coeff.Count;
         c_ = coeff;
         derC_ = new InitializedList<double>(order_ - 1);
         prC_ = new InitializedList<double>(order_);
         K_ = 0.0;
         eqs_ = new Matrix(order_, order_, 0.0);

         int i;
         for (i = 0; i < order_ - 1; ++i)
         {
            prC_[i] = c_[i] / (i + 1);
            derC_[i] = c_[i + 1] * (i + 1);
         }
         prC_[i] = c_[i] / (i + 1);
      }

      /// <summary>
      /// Returns the function value at time <paramref name="t"/>.
      /// </summary>
      public double value(double t)
      {
         double result = 0.0, tPower = 1.0;
         for (int i = 0; i < order_; ++i)
         {
            result += c_[i] * tPower;
            tPower *= t;
         }
         return result;
      }

      /// <summary>
      /// Returns the first derivative of the function at time <paramref name="t"/>.
      /// </summary>
      public double derivative(double t)
      {
         double result = 0.0, tPower = 1.0;
         for (int i = 0; i < order_ - 1; ++i)
         {
            result += derC_[i] * tPower;
            tPower *= t;
         }
         return result;
      }

      /// <summary>
      /// Returns the primitive of the function evaluated at time <paramref name="t"/>.
      /// </summary>
      public double primitive(double t)
      {
         double result = K_, tPower = t;
         for (int i = 0; i < order_; ++i)
         {
            result += prC_[i] * tPower;
            tPower *= t;
         }
         return result;
      }

      /// <summary>
      /// Returns the definite integral of the function between <paramref name="t1"/> and <paramref name="t2"/>.
      /// </summary>
      public double definiteIntegral(double t1, double t2)
      {
         return primitive(t2) - primitive(t1);
      }

      // Inspectors.
      public int order()  { return order_; }
      public List<double> coefficients() { return c_; }
      public List<double> derivativeCoefficients() { return derC_; }
      public List<double> primitiveCoefficients() { return prC_; }

      /// <summary>
      /// Returns the coefficients of the polynomial defined by the rolling-window definite integral on <c>[t, t2]</c>.
      /// </summary>
      public List<double> definiteIntegralCoefficients(double t, double t2)
      {
         Vector k = new Vector(c_);
         initializeEqs_(t, t2);
         Vector coeff = eqs_ * k;
         List<double> result = new List<double>(coeff);
         return result;
      }

      /// <summary>
      /// Returns the coefficients of the polynomial defined by the rolling-window derivative on <c>[t, t2]</c>.
      /// </summary>
      public List<double> definiteDerivativeCoefficients(double t, double t2)
      {
         Vector k = new Vector(c_);
         initializeEqs_(t, t2);
         Vector coeff = Matrix.transpose(eqs_) * k;
         List<double> result = new Vector(coeff);
         return result;
      }

      private int order_;
      private List<double> c_, derC_, prC_;
      private double K_;
      private Matrix eqs_;
      private void initializeEqs_(double t, double t2)
      {
         double dt = t2 - t;
         double tau;
         for (int i = 0; i < order_; ++i)
         {
            tau = 1.0;
            for (int j = i; j < order_; ++j)
            {
               tau *= dt;
               eqs_[i, j] = (tau * PascalTriangle.get(j + 1)[i]) / (j + 1);
            }
         }
      }

   }
}
