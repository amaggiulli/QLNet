/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
  
 This file is part of QLNet Project http://qlnet.sourceforge.net/

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
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QLNet;

namespace Test2008
{
   [TestClass()]
   public class T_SampledCurve
   {

      class FSquared
      {
         public double value(double x) { return x * x; }
      }

      [TestMethod()]
      public void testConstruction()
      {
         //("Testing sampled curve construction...");

         SampledCurve curve = new SampledCurve(Utils.BoundedGrid(-10.0, 10.0, 100));
         FSquared f2 = new FSquared();
         curve.sample(f2.value);
         double expected = 100.0;
         if (Math.Abs(curve.value(0) - expected) > 1e-5)
         {
            Assert.Fail("function sampling failed");
         }

         curve.setValue(0, 2.0);
         if (Math.Abs(curve.value(0) - 2.0) > 1e-5)
         {
            Assert.Fail("curve value setting failed");
         }

         Vector value = curve.values();
         value[1] = 3.0;
         if (Math.Abs(curve.value(1) - 3.0) > 1e-5)
         {
            Assert.Fail("curve value grid failed");
         }

         curve.shiftGrid(10.0);
         if (Math.Abs(curve.gridValue(0) - 0.0) > 1e-5)
         {
            Assert.Fail("sample curve shift grid failed");
         }
         if (Math.Abs(curve.value(0) - 2.0) > 1e-5)
         {
            Assert.Fail("sample curve shift grid - value failed");
         }

         curve.sample(f2.value);
         curve.regrid(Utils.BoundedGrid(0.0, 20.0, 200));
         double tolerance = 1.0e-2;
         for (int i = 0; i < curve.size(); i++)
         {
            double grid = curve.gridValue(i);
            double v = curve.value(i);
            double exp = f2.value(grid);
            if (Math.Abs(v - exp) > tolerance)
            {
               Assert.Fail("sample curve regriding failed" +
                           "\n    at " + (i + 1) + " point " + "(x = " + grid + ")" +
                           "\n    grid value: " + v +
                           "\n    expected:   " + exp);
            }
         }
      }

   }
}
