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

namespace QLNet
{
   /// <summary>
   /// Runge-Kutta ODE integration
   /// <remarks>
   /// Runge Kutta method with adaptive stepsize as described in
   /// Numerical Recipes in C, Chapter 16.2
   /// </remarks>
   /// </summary>
   public class AdaptiveRungeKutta
   {
      public delegate List<double> OdeFct(double x, List<double> l);

      public delegate double OdeFct1d(double x, double y);

      /*! The class is constructed with the following inputs:
          - eps       prescribed error for the solution
          - h1        start step size
          - hmin      smallest step size allowed
      */

      public AdaptiveRungeKutta(double eps = 1.0e-6,
                                double h1 = 1.0e-4,
                                double hmin = 0.0)
      {
         eps_ = eps;
         h1_ = h1;
         hmin_ = hmin;

         a2 = 0.2;
         a3 = 0.3;
         a4 = 0.6;
         a5 = 1.0;
         a6 = 0.875;
         b21 = 0.2;
         b31 = 3.0 / 40.0;
         b32 = 9.0 / 40.0;
         b41 = 0.3;
         b42 = -0.9;
         b43 = 1.2;
         b51 = -11.0 / 54.0;
         b52 = 2.5;
         b53 = -70.0 / 27.0;
         b54 = 35.0 / 27.0;
         b61 = 1631.0 / 55296.0;
         b62 = 175.0 / 512.0;
         b63 = 575.0 / 13824.0;
         b64 = 44275.0 / 110592.0;
         b65 = 253.0 / 4096.0;
         c1 = 37.0 / 378.0;
         c3 = 250.0 / 621.0;
         c4 = 125.0 / 594.0;
         c6 = 512.0 / 1771.0;
         dc1 = c1 - 2825.0 / 27648.0;
         dc3 = c3 - 18575.0 / 48384.0;
         dc4 = c4 - 13525.0 / 55296.0;
         dc5 = -277.0 / 14336.0;
         dc6 = c6 - 0.25;
      }

      /*! Integrate the ode from \f$ x1 \f$ to \f$ x2 \f$ with
          initial value condition \f$ f(x1)=y1 \f$.

          The ode is given by a function \f$ F: R \times K^n
          \rightarrow K^n \f$ as \f$ f'(x) = F(x,f(x)) \f$, $K=R,
          C$ */
      public List<double> value(OdeFct ode,
                                List<double> y1,
                                double x1,
                                double x2)
      {
         int n = y1.Count;
         List<double> y = new List<double>(y1);
         List<double> yScale = new InitializedList<double>(n);
         double x = x1;
         double h = h1_ * (x1 <= x2 ? 1 : -1);
         double hnext = 0, hdid = 0;

         for (int nstp = 1; nstp <= ADAPTIVERK_MAXSTP; nstp++)
         {
            List<double> dydx = ode(x, y);
            for (int i = 0; i < n; i++)
               yScale[i] = Math.Abs(y[i]) + Math.Abs(dydx[i] * h) + ADAPTIVERK_TINY;
            if ((x + h - x2) * (x + h - x1) > 0.0)
               h = x2 - x;
            rkqs(y, dydx, ref x, h, eps_, yScale, ref hdid, ref hnext, ode);

            if ((x - x2) * (x2 - x1) >= 0.0)
               return y;

            if (Math.Abs(hnext) <= hmin_)
               Utils.QL_FAIL("Step size (" + hnext + ") too small ("
                             + hmin_ + " min) in AdaptiveRungeKutta");
            h = hnext;
         }
         Utils.QL_FAIL("Too many steps (" + ADAPTIVERK_MAXSTP
                       + ") in AdaptiveRungeKutta");
         return null;
      }

      public double value(OdeFct1d ode,
                          double y1,
                          double x1,
                          double x2)
      {
         return value(new OdeFctWrapper(ode).value,
                      new InitializedList<double>(1, y1), x1, x2)[0];
      }

      protected void rkqs(List<double> y,
                          List<double> dydx,
                          ref double x,
                          double htry,
                          double eps,
                          List<double> yScale,
                          ref double hdid,
                          ref double hnext,
                          OdeFct derivs)
      {
         int n = y.Count;
         double errmax, xnew;
         List<double> yerr = new InitializedList<double>(n), ytemp = new InitializedList<double>(n);

         double h = htry;

         for (;;)
         {
            rkck(y, dydx, ref x, h, ref ytemp, ref yerr, derivs);
            errmax = 0.0;
            for (int i = 0; i < n; i++)
               errmax = Math.Max(errmax, Math.Abs(yerr[i] / yScale[i]));
            errmax /= eps;
            if (errmax > 1.0)
            {
               double htemp1 = ADAPTIVERK_SAFETY * h * Math.Pow(errmax, ADAPTIVERK_PSHRINK);
               double htemp2 = h / 10;
               // These would be std::min and std::max, of course,
               // but VC++14 had problems inlining them and caused
               // the wrong results to be calculated.  The problem
               // seems to be fixed in update 3, but let's keep this
               // implementation for compatibility.
               double max_positive = htemp1 > htemp2 ? htemp1 : htemp2;
               double max_negative = htemp1 < htemp2 ? htemp1 : htemp2;
               h = ((h >= 0.0) ? max_positive : max_negative);
               xnew = x + h;
               if (xnew.IsEqual(x))
                  Utils.QL_FAIL("Stepsize underflow (" + h + " at x = " + x
                                + ") in AdaptiveRungeKutta::rkqs");
               continue;
            }
            else
            {
               if (errmax > ADAPTIVERK_ERRCON)
                  hnext = ADAPTIVERK_SAFETY * h * Math.Pow(errmax, ADAPTIVERK_PGROW);
               else
                  hnext = 5.0 * h;
               x += (hdid = h);
               for (int i = 0; i < n; i++)
                  y[i] = ytemp[i];
               break;
            }
         }
      }

      protected void rkck(List<double> y,
                          List<double> dydx,
                          ref double x,
                          double h,
                          ref List<double> yout,
                          ref List<double> yerr,
                          OdeFct derivs)
      {
         int n = y.Count;
         List<double> ak2 = new InitializedList<double>(n),
         ak3 = new InitializedList<double>(n),
         ak4 = new InitializedList<double>(n),
         ak5 = new InitializedList<double>(n),
         ak6 = new InitializedList<double>(n),
         ytemp = new InitializedList<double>(n);

         // first step
         for (int i = 0; i < n; i++)
            ytemp[i] = y[i] + b21 * h * dydx[i];

         // second step
         ak2 = derivs(x + a2 * h, ytemp);
         for (int i = 0; i < n; i++)
            ytemp[i] = y[i] + h * (b31 * dydx[i] + b32 * ak2[i]);

         // third step
         ak3 = derivs(x + a3 * h, ytemp);
         for (int i = 0; i < n; i++)
            ytemp[i] = y[i] + h * (b41 * dydx[i] + b42 * ak2[i] + b43 * ak3[i]);

         // fourth step
         ak4 = derivs(x + a4 * h, ytemp);
         for (int i = 0; i < n; i++)
            ytemp[i] = y[i] + h * (b51 * dydx[i] + b52 * ak2[i] + b53 * ak3[i] + b54 * ak4[i]);

         // fifth step
         ak5 = derivs(x + a5 * h, ytemp);
         for (int i = 0; i < n; i++)
            ytemp[i] = y[i] + h * (b61 * dydx[i] + b62 * ak2[i] + b63 * ak3[i] + b64 * ak4[i] + b65 * ak5[i]);

         // sixth step
         ak6 = derivs(x + a6 * h, ytemp);
         for (int i = 0; i < n; i++)
         {
            yout[i] = y[i] + h * (c1 * dydx[i] + c3 * ak3[i] + c4 * ak4[i] + c6 * ak6[i]);
            yerr[i] = h * (dc1 * dydx[i] + dc3 * ak3[i] + dc4 * ak4[i] + dc5 * ak5[i] + dc6 * ak6[i]);
         }
      }

      protected List<double> yStart_;
      protected double eps_, h1_, hmin_;

      protected double a2,
                a3,
                a4,
                a5,
                a6,
                b21,
                b31,
                b32,
                b41,
                b42,
                b43,
                b51,
                b52,
                b53,
                b54,
                b61,
                b62,
                b63,
                b64,
                b65,
                c1,
                c3,
                c4,
                c6,
                dc1,
                dc3,
                dc4,
                dc5,
                dc6;

      protected const double ADAPTIVERK_MAXSTP = 10000.0,
                             ADAPTIVERK_TINY = 1.0E-30,
                             ADAPTIVERK_SAFETY = 0.9,
                             ADAPTIVERK_PGROW = -0.2,
                             ADAPTIVERK_PSHRINK = -0.25,
                             ADAPTIVERK_ERRCON = 1.89E-4;

      public class OdeFctWrapper
      {
         public OdeFctWrapper(OdeFct1d ode1d)
         {
            ode1d_ = ode1d;
         }

         public List<double> value(double x, List<double> y)
         {
            List<double> res = new InitializedList<double>(1, ode1d_(x, y[0]));
            return res;
         }

         protected OdeFct1d ode1d_;
      }
   }
}
