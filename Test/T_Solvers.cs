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

namespace TestSuite
{
   [TestClass()]
   public class T_Solvers
   {
      class Foo : ISolver1d
      {
         public override double value(double x) { return x * x - 1.0; }
         public override double derivative(double x) { return 2.0 * x; }
      };

      public void test(Solver1D solver, string name)
      {
         double[] accuracy = new double[] { 1.0e-4, 1.0e-6, 1.0e-8 };
         double expected = 1.0;
         for (int i = 0; i < accuracy.Length; i++)
         {
            double root = solver.solve(new Foo(), accuracy[i], 1.5, 0.1);
            if (Math.Abs(root - expected) > accuracy[i])
            {
               Assert.Fail(name + " solver:\n"
                          + "    expected:   " + expected + "\n"
                          + "    calculated: " + root + "\n"
                          + "    accuracy:   " + accuracy[i]);
            }
            root = solver.solve(new Foo(), accuracy[i], 1.5, 0.0, 1.0);
            if (Math.Abs(root - expected) > accuracy[i])
            {
               Assert.Fail(name + " solver (bracketed):\n"
                          + "    expected:   " + expected + "\n"
                          + "    calculated: " + root + "\n"
                          + "    accuracy:   " + accuracy[i]);
            }
         }
      }

      [TestMethod()]
      public void testBrent()
      {
         test(new Brent(), "Brent");
      }
      [TestMethod()]
      public void testNewton()
      {
         test(new Newton(), "Newton");
      }
      [TestMethod()]
      public void testFalsePosition()
      {
         test(new FalsePosition(), "FalsePosition");
      }
      [TestMethod()]
      public void testBisection()
      {
         test(new Bisection(), "Bisection");
      }
      [TestMethod()]
      public void testRidder()
      {
         test(new Ridder(), "Ridder");
      }
      [TestMethod()]
      public void testSecant()
      {
         test(new Secant(), "Secant");
      }

      public void suite()
      {
         testBrent();
         testNewton();
         testFalsePosition();
         testBisection();
         testRidder();
         testSecant();
      }
   }
}
