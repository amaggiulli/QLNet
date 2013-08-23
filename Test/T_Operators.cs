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
   public class T_Operators
   {
      public const double average = 0.0, sigma = 1.0;

      [TestMethod()]
      public void testOperatorConsistency()
      {

         //("Testing differential operators...");

         NormalDistribution normal = new NormalDistribution(average, sigma);
         CumulativeNormalDistribution cum = new CumulativeNormalDistribution(average, sigma);

         double xMin = average - 4 * sigma,
              xMax = average + 4 * sigma;
         int N = 10001;
         double h = (xMax - xMin) / (N - 1);

         Vector x = new Vector(N),
             y = new Vector(N),
             yi = new Vector(N),
             yd = new Vector(N),
             temp = new Vector(N),
             diff = new Vector(N);

         for (int i = 0; i < N; i++)
            x[i] = xMin + h * i;

         for (int i = 0; i < x.Count; i++)
            y[i] = normal.value(x[i]);
         for (int i = 0; i < x.Count; i++)
            yi[i] = cum.value(x[i]);

         for (int i = 0; i < x.size(); i++)
            yd[i] = normal.derivative(x[i]);

         // define the differential operators
         DZero D = new DZero(N, h);
         DPlusDMinus D2 = new DPlusDMinus(N, h);

         // check that the derivative of cum is Gaussian
         temp = D.applyTo(yi);

         for (int i = 0; i < y.Count; i++)
            diff[i] = y[i] - temp[i];
         double e = Utilities.norm(diff, diff.size(), h);
         if (e > 1.0e-6)
         {
            Assert.Fail("norm of 1st derivative of cum minus Gaussian: " + e + "\ntolerance exceeded");
         }

         // check that the second derivative of cum is normal.derivative
         temp = D2.applyTo(yi);

         for (int i = 0; i < yd.Count; i++)
            diff[i] = yd[i] - temp[i];

         e = Utilities.norm(diff, diff.size(), h);
         if (e > 1.0e-4)
         {
            Assert.Fail("norm of 2nd derivative of cum minus Gaussian derivative: " + e + "\ntolerance exceeded");
         }
      }

      [TestMethod()]
      public void testBSMOperatorConsistency()
      {
         //("Testing consistency of BSM operators...");

         Vector grid = new Vector(10);
         double price = 20.0;
         double factor = 1.1;
         for (int i = 0; i < grid.size(); i++)
         {
            grid[i] = price;
            price *= factor;
         }

         double dx = Math.Log(factor);
         double r = 0.05;
         double q = 0.01;
         double sigma = 0.5;

         BSMOperator refer = new BSMOperator(grid.size(), dx, r, q, sigma);

         DayCounter dc = new Actual360();
         Date today = Date.Today;
         Date exercise = today + new Period(2, TimeUnit.Years);
         double residualTime = dc.yearFraction(today, exercise);

         SimpleQuote spot = new SimpleQuote(0.0);
         YieldTermStructure qTS = Utilities.flatRate(today, q, dc);
         YieldTermStructure rTS = Utilities.flatRate(today, r, dc);
         BlackVolTermStructure volTS = Utilities.flatVol(today, sigma, dc);
         GeneralizedBlackScholesProcess stochProcess = new GeneralizedBlackScholesProcess(
                                                        new Handle<Quote>(spot),
                                                        new Handle<YieldTermStructure>(qTS),
                                                        new Handle<YieldTermStructure>(rTS),
                                                        new Handle<BlackVolTermStructure>(volTS));
         BSMOperator op1 = new BSMOperator(grid, stochProcess, residualTime);
         PdeOperator<PdeBSM> op2 = new PdeOperator<PdeBSM>(grid, stochProcess, residualTime);

         double tolerance = 1.0e-6;
         Vector lderror = refer.lowerDiagonal() - op1.lowerDiagonal();
         Vector derror = refer.diagonal() - op1.diagonal();
         Vector uderror = refer.upperDiagonal() - op1.upperDiagonal();

         for (int i = 2; i < grid.size() - 2; i++)
         {
            if (Math.Abs(lderror[i]) > tolerance ||
                Math.Abs(derror[i]) > tolerance ||
                Math.Abs(uderror[i]) > tolerance)
            {
               Assert.Fail("inconsistency between BSM operators:\n"
                          + i + " row:\n"
                          + "expected:   "
                          + refer.lowerDiagonal()[i] + ", "
                          + refer.diagonal()[i] + ", "
                          + refer.upperDiagonal()[i] + "\n"
                          + "calculated: "
                          + op1.lowerDiagonal()[i] + ", "
                          + op1.diagonal()[i] + ", "
                          + op1.upperDiagonal()[i]);
            }
         }
         lderror = refer.lowerDiagonal() - op2.lowerDiagonal();
         derror = refer.diagonal() - op2.diagonal();
         uderror = refer.upperDiagonal() - op2.upperDiagonal();

         for (int i = 2; i < grid.size() - 2; i++)
         {
            if (Math.Abs(lderror[i]) > tolerance ||
                Math.Abs(derror[i]) > tolerance ||
                Math.Abs(uderror[i]) > tolerance)
            {
               Assert.Fail("inconsistency between BSM operators:\n"
                          + i + " row:\n"
                          + "expected:   "
                          + refer.lowerDiagonal()[i] + ", "
                          + refer.diagonal()[i] + ", "
                          + refer.upperDiagonal()[i] + "\n"
                          + "calculated: "
                          + op2.lowerDiagonal()[i] + ", "
                          + op2.diagonal()[i] + ", "
                          + op2.upperDiagonal()[i]);
            }
         }
      }

   }
}
