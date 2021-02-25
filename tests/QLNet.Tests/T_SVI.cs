/*
 Copyright (C) 2008-2009 Andrea Maggiulli

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

#if NET452
using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
using Xunit;
#endif
using QLNet;

namespace TestSuite
{

#if NET452
   [TestClass()]
#endif
   public class T_SVI
   {
      double add10(double x) { return x + 10; }
      double mul10(double x) { return x * 10; }
      double sub10(double x) { return x - 10; }

      double add
         (double x, double y) { return x + y; }
      double mul(double x, double y) { return x * y; }
      double sub(double x, double y) { return x - y; }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testCalibration()
      {
         double forward = 0.03;
         double tau = 1.0;

         //Real a = 0.04;
         //Real b = 0.1;
         //Real rho = -0.5;
         //Real sigma = 0.1;
         //Real m  = 0.0;
         double a = 0.1;
         double b = 0.06;
         double rho = -0.9;
         double m = 0.24;
         double sigma = 0.06;

         List<double> strikes = new List<double>();
         strikes.Add(0.01);
         strikes.Add(0.015);
         strikes.Add(0.02);
         strikes.Add(0.025);
         strikes.Add(0.03);
         strikes.Add(0.035);
         strikes.Add(0.04);
         strikes.Add(0.045);
         strikes.Add(0.05);

         List<double> vols = new InitializedList<double>(strikes.Count, 0.20); //dummy vols (we do not calibrate here)

         SviInterpolation svi = new SviInterpolation(strikes, strikes.Count, vols, tau,
                                                     forward, a, b, sigma, rho, m, true, true, true,
                                                     true, true);

         svi.enableExtrapolation();

         List<double> sviVols = new InitializedList<double>(strikes.Count, 0.0);
         for (int i = 0; i < strikes.Count; ++i)
            sviVols[i] = svi.value(strikes[i]);

         SviInterpolation svi2 = new SviInterpolation(strikes, strikes.Count, sviVols, tau,
                                                      forward, null, null, null,
                                                      null, null, false, false, false,
                                                      false, false, false, null,
                                                      null, 1E-8, false,
                                                      0); //don't allow for random start values

         svi2.enableExtrapolation();
         svi2.update();

         Console.WriteLine("a=" + svi2.a());
         if (!Utils.close_enough(a, svi2.a(), 100))
            QAssert.Fail("error in a coefficient estimation");

         Console.WriteLine("b=" + svi2.b());
         if (!Utils.close_enough(b, svi2.b(), 100))
            QAssert.Fail("error in b coefficient estimation");

         Console.WriteLine("sigma=" + svi2.sigma());
         if (!Utils.close_enough(sigma, svi2.sigma(), 100))
            QAssert.Fail("error in sigma coefficient estimation");

         Console.WriteLine("rho=" + svi2.rho());
         if (!Utils.close_enough(rho, svi2.rho(), 100))
            QAssert.Fail("error in rho coefficient estimation");

         Console.WriteLine("m=" + svi2.m());
         if (!Utils.close_enough(m, svi2.m(), 100))
            QAssert.Fail("error in m coefficient estimation");

         Console.WriteLine("error=" + svi2.rmsError());
      }

   }
}
