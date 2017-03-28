/*
 Copyright (C) 2008, 2015  Andrea Maggiulli (a.maggiulli@gmail.com)
  
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
#if QL_DOTNET_FRAMEWORK
using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
   using Xunit;
#endif
using QLNet;
using System.Numerics;

namespace TestSuite
{
#if QL_DOTNET_FRAMEWORK
   [TestClass()]
#endif
   public class T_Functions
   {
#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
      public void testFactorial() 
      {
         // Testing factorial numbers

         double expected = 1.0;
         double calculated = Factorial.get(0);
         if (calculated!=expected)
            QAssert.Fail("Factorial(0) = " + calculated);

         for (uint i=1; i<171; ++i) 
         {
            expected *= i;
            calculated = Factorial.get(i);
            if (Math.Abs(calculated-expected)/expected > 1.0e-9)
               QAssert.Fail("Factorial(" + i + ")" +
                           "\n calculated: " + calculated +
                           "\n   expected: " + expected +
                           "\n rel. error: " +
                           Math.Abs(calculated-expected)/expected);
         }
      }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
      public void testGammaFunction() 
      {
         // Testing Gamma function

         double expected = 0.0;
         double calculated = GammaFunction.logValue(1);
         if (Math.Abs(calculated) > 1.0e-15)
            QAssert.Fail("GammaFunction(1)\n"
                        + "    calculated: " + calculated + "\n"
                        + "    expected:   " + expected);

         for (int i=2; i<9000; i++) 
         {
            expected  += Math.Log(i);
            calculated = GammaFunction.logValue((i+1));
            if (Math.Abs(calculated-expected)/expected > 1.0e-9)
               QAssert.Fail("GammaFunction(" + i + ")\n"
                           + "    calculated: " + calculated + "\n"
                           + "    expected:   " + expected + "\n"
                           + "    rel. error: "
                           + Math.Abs(calculated-expected)/expected);
         }
      }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
      public void testGammaValues() 
      {
         // Testing Gamma values

         // reference results are calculated with R
         double[][] tasks = {
               new double[3] { 0.0001, 9999.422883231624, 1e3},
               new double[3] { 1.2, 0.9181687423997607, 1e3},
               new double[3] { 7.3, 1271.4236336639089586, 1e3},
               new double[3] {-1.1, 9.7148063829028946, 1e3},
               new double[3] {-4.001,-41.6040228304425312, 1e3},
               new double[3] {-4.999, -8.347576090315059, 1e3},
               new double[3] {-19.000001, 8.220610833201313e-12, 1e8},
               new double[3] {-19.5, 5.811045977502255e-18, 1e3},
               new double[3] {-21.000001, 1.957288098276488e-14, 1e8},
               new double[3] {-21.5, 1.318444918321553e-20, 1e6}
         };

         for (int i=0; i < tasks.Length; ++i) 
         {
            double x = tasks[i][0];
            double expected = tasks[i][1];
            double calculated = GammaFunction.value(x);
            double tol = tasks[i][2] * Const.QL_EPSILON*Math.Abs(expected);

            if (Math.Abs(calculated - expected) > tol) 
            {
               QAssert.Fail("GammaFunction(" + x + ")\n"
                           + "    calculated: " + calculated + "\n"
                           + "    expected:   " + expected + "\n"
                           + "    rel. error: "
                           + Math.Abs(calculated-expected)/expected);
           
            }
         }
      }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
      public void testModifiedBesselFunctions() 
      {
         // Testing modified Bessel function of first and second kind

         /* reference values are computed with R and the additional package Bessel
         * http://cran.r-project.org/web/packages/Bessel
         */

         double[][] r = {
            new double[4] {-1.3, 2.0, 1.2079888436539505, 0.1608243636110430},
            new double[4] { 1.3, 2.0, 1.2908192151358788, 0.1608243636110430},
            new double[4] { 0.001, 2.0, 2.2794705965773794, 0.1138938963603362},
            new double[4] { 1.2, 0.5,   0.1768918783499572, 2.1086579232338192},
            new double[4] { 2.3, 0.1, 0.00037954958988425198, 572.096866928290183},
            new double[4] {-2.3, 1.1, 1.07222017902746969, 1.88152553684107371},
            new double[4] {-10.0001, 1.1, 13857.7715614282552, 69288858.9474423379}
         };

         for (int i=0; i < r.Length; ++i) 
         {
            double nu = r[i][0];
            double x  = r[i][1];
            double expected_i = r[i][2];
            double expected_k = r[i][3];
            double tol_i = 5e4 * Const.QL_EPSILON*Math.Abs(expected_i);
            double tol_k = 5e4 * Const.QL_EPSILON*Math.Abs(expected_k);

            double calculated_i = Utils.modifiedBesselFunction_i(nu, x);
            double calculated_k = Utils.modifiedBesselFunction_k(nu, x);

            if (Math.Abs(expected_i - calculated_i) > tol_i) {
               QAssert.Fail("failed to reproduce modified Bessel "
                           + "function of first kind"
                           + "\n order     : " + nu
                           + "\n argument  : " + x
                           + "\n calculated: " + calculated_i
                           + "\n expected  : " + expected_i);
            }
            if (Math.Abs(expected_k - calculated_k) > tol_k) {
               QAssert.Fail("failed to reproduce modified Bessel "
                           + "function of second kind"
                           + "\n order     : " + nu
                           + "\n argument  : " + x
                           + "\n calculated: " + calculated_k
                           + "\n expected  : " + expected_k);
            }
         }

         double[][] c = {
            new double[7] {-1.3, 2.0, 0.0, 1.2079888436539505, 0.0, 0.1608243636110430, 0.0},
            new double[7] { 1.2, 1.5, 0.3, 0.7891550871263575, 0.2721408731632123, 0.275126507673411, -0.1316314405663727},
            new double[7] { 1.2, -1.5,0.0,-0.6650597524355781, -0.4831941938091643, -0.251112360556051, -2.400130904230102},
            new double[7] {-11.2, 1.5, 0.3,12780719.20252659, 16401053.26770633, -34155172.65672453, -43830147.36759921},
            new double[7] { 1.2, -1.5,2.0,-0.3869803778520574, 0.9756701796853728, -3.111629716783005, 0.6307859871879062},
            new double[7] { 1.2, 0.0, 9.9999,-0.03507838078252647, 0.1079601550451466, -0.05979939995451453, 0.3929814473878203},
            new double[7] { 1.2, 0.0, 10.1, -0.02782046891519293, 0.08562259917678558, -0.02035685034691133, 0.3949834389686676},
            new double[7] { 1.2, 0.0, 12.1, 0.07092110620741207, -0.2182727210128104, 0.3368505862966958, -0.1299038064313366},
            new double[7] { 1.2, 0.0, 14.1,-0.03014378676768797, 0.09277303628303372, -0.237531022649052, -0.2351923034581644},
            new double[7] { 1.2, 0.0, 16.1,-0.03823210284792657, 0.1176663135266562, -0.1091239402448228, 0.2930535651966139},
            new double[7] { 1.2, 0.0, 18.1,0.05626742394733754, -0.173173324361983, 0.2941636588154642, -0.02023355577954348},
            new double[7] { 1.2, 0.0, 180.1,-0.001230682086826484, 0.003787649998122361,0.02284509628723454, 0.09055419580980778},
            new double[7] { 1.2, 0.0, 21.0,-0.04746415965014021, 0.1460796627610969,-0.2693825171336859, -0.04830804448126782},
            new double[7] { 1.2, 10.0, 0.0, 2609.784936867044, 0, 1.904394919838336e-05, 0},
            new double[7] { 1.2, 14.0, 0.0, 122690.4873454286, 0, 2.902060692576643e-07, 0},
            new double[7] { 1.2, 20.0, 10.0, -37452017.91168936, -13917587.22151363, -3.821534367487143e-10, 4.083211255351664e-10},
            new double[7] { 1.2, 9.0, 9.0, -621.7335051293694,  618.1455736670332, -4.480795479964915e-05, -3.489034389148745e-08}
         };

         for (int i=0; i < c.Length; ++i) 
         {
            double nu = c[i][0];
            Complex z  = new Complex(c[i][1], c[i][2]);
            Complex expected_i = new Complex(c[i][3],c[i][4]);
            Complex expected_k = new Complex(c[i][5],c[i][6]);

            double tol_i = 5e4*Const.QL_EPSILON*Complex.Abs(expected_i);
            double tol_k = 1e6*Const.QL_EPSILON*Complex.Abs(expected_k);

            Complex calculated_i= Utils.modifiedBesselFunction_i(nu, z);
            Complex calculated_k= Utils.modifiedBesselFunction_k(nu, z);

            if (Complex.Abs(expected_i - calculated_i) > tol_i) 
            {
               QAssert.Fail("failed to reproduce modified Bessel "
                           + "function of first kind"
                           + "\n order     : " + nu
                           + "\n argument  : " + z
                           + "\n calculated: " + calculated_i
                           + "\n expected  : " + expected_i);
            }
            
            if (   Complex.Abs(expected_k) > 1e-4 // do not check small values
               && Complex.Abs(expected_k - calculated_k) > tol_k) 
            {
               QAssert.Fail("failed to reproduce modified Bessel "
                           + "function of second kind"
                           + "\n order     : " + nu
                           + "\n argument  : " + z
                           + "\n diff      : " + (calculated_k-expected_k)
                           + "\n calculated: " + calculated_k
                           + "\n expected  : " + expected_k);
            }
         }
         }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
      public void testWeightedModifiedBesselFunctions() 
      {
         // Testing weighted modified Bessel functions
         double nu = -5.0;
         while (nu <= 5.0) 
         {
            double x = 0.1;
            while (x <= 15.0) 
            {
               double vi = Utils.modifiedBesselFunction_i_exponentiallyWeighted(nu, x);
               double wi = Utils.modifiedBesselFunction_i(nu, x) * Math.Exp(-x);
               double vk = Utils.modifiedBesselFunction_k_exponentiallyWeighted(nu, x);
               double wk = Const.M_PI_2 * (Utils.modifiedBesselFunction_i(-nu,x)*Math.Exp(-x)-
                                    Utils.modifiedBesselFunction_i(nu,x)*Math.Exp(-x)) / Math.Sin(Const.M_PI*nu);
               if (Math.Abs((vi - wi) / (Math.Max(Math.Exp(x), 1.0) * vi)) > 1E3 * Const.QL_EPSILON)
                     QAssert.Fail("failed to verify exponentially weighted"
                                 + "modified Bessel function of first kind"
                                 + "\n order      : " + nu + "\n argument   : "
                                 + x + "\n calcuated  : " + vi
                                 + "\n expecetd   : " + wi);

               if (Math.Abs((vk - wk) / (Math.Max(Math.Exp(x), 1.0) * vk)) > 1E3 * Const.QL_EPSILON)
                     QAssert.Fail("failed to verify exponentially weighted"
                                 + "modified Bessel function of second kind"
                                 + "\n order      : " + nu + "\n argument   : "
                                 + x + "\n calcuated  : " + vk
                                 + "\n expecetd   : " + wk);
               x += 0.5;
            }
            nu += 0.5;
         }
         nu = -5.0;
         while (nu <= 5.0) 
         {
            double x = -5.0;
            while (x <= 5.0) {
               double y = -5.0;
               while (y <= 5.0) 
               {
                  Complex z = new Complex(x, y);
                  Complex vi = Utils.modifiedBesselFunction_i_exponentiallyWeighted(nu, z);
                  Complex wi = Utils.modifiedBesselFunction_i(nu, z) * Complex.Exp(-z);
                  Complex vk = Utils.modifiedBesselFunction_k_exponentiallyWeighted(nu, z);
                  Complex wk = Const.M_PI_2 * (Utils.modifiedBesselFunction_i(-nu, z) * Complex.Exp(-z) -
                                               Utils.modifiedBesselFunction_i(nu, z) * Complex.Exp(-z)) /
                               Math.Sin(Const.M_PI * nu);
                  if (Complex.Abs((vi - wi) / vi) > 1E3 * Const.QL_EPSILON)
                     QAssert.Fail("failed to verify exponentially weighted"
                                 + "modified Bessel function of first kind"
                                 + "\n order      : " + nu
                                 + "\n argument   : " + z +
                                 "\n calcuated: "
                                 + vi + "\n expecetd   : " + wi);
                  if (Complex.Abs((vk - wk) / vk) > 1E3 * Const.QL_EPSILON)
                     QAssert.Fail("failed to verify exponentially weighted"
                                 + "modified Bessel function of second kind"
                                 + "\n order      : " + nu
                                 + "\n argument   : " + z +
                                 "\n calcuated: "
                                 + vk + "\n expecetd   : " + wk);
                  y += 0.5;
               }
               x += 0.5;
            }
            nu += 0.5;
         }
         
      }

   }
}
