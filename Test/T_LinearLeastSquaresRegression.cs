/*
 Copyright (C) 2008-2016  Andrea Maggiulli (a.maggiulli@gmail.com) 
  
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
#if QL_DOTNET_FRAMEWORK
using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
   using Xunit;
#endif
using QLNet;

namespace TestSuite {
    /// <summary>
    /// Summary description for LinearLeastSquaresRegression
    /// </summary>
   #if QL_DOTNET_FRAMEWORK
   [TestClass]
   #endif
   public class T_LinearLeastSquaresRegression : IDisposable
    {
       
       #region Initialize&Cleanup
       private SavedSettings backup;
       #if QL_DOTNET_FRAMEWORK
       [TestInitialize]
       public void testInitialize()
       {
       #else
       public T_LinearLeastSquaresRegression()
       {
       #endif

          backup = new SavedSettings();
       }
       #if QL_DOTNET_FRAMEWORK
       [TestCleanup]
       #endif
       public void testCleanup()
       {
          Dispose();
       }
       public void Dispose()
       {
          backup.Dispose();
       }
       #endregion

        const double tolerance = 0.025;

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
        public void testRegression() 
        {
            // Testing linear least-squares regression
            const int nr=100000;
            
            var rng = new InverseCumulativeRng<MersenneTwisterUniformRng,InverseCumulativeNormal>(
                            new MersenneTwisterUniformRng(1234u));

            List<Func<double, double>> v = new List<Func<double,double>>();
            v.Add(x => 1.0);
            v.Add(x => x);
            v.Add(x => x*x);
            v.Add(Math.Sin);

            List<Func<double, double>> w = new List<Func<double,double>>(v);
            w.Add(x => x*x);

            for (int k=0; k<3; ++k) {
                int i;
                double[] a = { rng.next().value,
                               rng.next().value,
                               rng.next().value,
                               rng.next().value};

                List<double> x = new InitializedList<double>(nr), y = new InitializedList<double>(nr);
                for (i=0; i<nr; ++i) {
                    x[i] = rng.next().value;

                    // regression in y = a_1 + a_2*x + a_3*x^2 + a_4*sin(x) + eps
                    y[i] =  a[0]*v[0](x[i]) + a[1]*v[1](x[i]) + a[2]*v[2](x[i])
                          + a[3]*v[3](x[i]) + rng.next().value;
                }

                LinearLeastSquaresRegression m = new LinearLeastSquaresRegression(x, y, v);

                for (i=0; i<v.Count; ++i) {
                    if (m.standardErrors()[i] > tolerance) {
                        QAssert.Fail("Failed to reproduce linear regression coef."
                                    + "\n    error:     " + m.standardErrors()[i]
                                    + "\n    tolerance: " + tolerance);
                    }
                    if (Math.Abs(m.coefficients()[i]-a[i]) > 3*m.error()[i]) {
                        QAssert.Fail("Failed to reproduce linear regression coef."
                                    + "\n    calculated: " + m.coefficients()[i]
                                    + "\n    error:      " + m.standardErrors()[i]
                                    + "\n    expected:   " + a[i]);
                    }
                }

                m = new LinearLeastSquaresRegression(x, y, w);

                double[] ma = { m.coefficients()[0], m.coefficients()[1], m.coefficients()[2] + m.coefficients()[4], m.coefficients()[3] };
                double[] err = {m.standardErrors()[0], m.standardErrors()[1],
                                    Math.Sqrt( m.standardErrors()[2]*m.standardErrors()[2]
                                              +m.standardErrors()[4]*m.standardErrors()[4]),
                                    m.standardErrors()[3]};
                for (i = 0; i < v.Count; ++i) {
                    if (Math.Abs(ma[i] - a[i]) > 3 * err[i]) {
                        QAssert.Fail("Failed to reproduce linear regression coef."
                                    + "\n    calculated: " + ma[i]
                                    + "\n    error:      " + err[i]
                                    + "\n    expected:   " + a[i]);
                    }
                }
            }

        }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
        public void test1dLinearRegression() 
        {
            // Testing 1d simple linear least-squares regression

            /* Example taken from the QuantLib-User list, see posting
             * Multiple linear regression/weighted regression, Boris Skorodumov */

            List<double> x = new InitializedList<double>(9),
                         y = new InitializedList<double>(9);
            x[0] = 2.4; x[1] = 1.8; x[2] = 2.5; x[3] = 3.0;
            x[4] = 2.1; x[5] = 1.2; x[6] = 2.0; x[7] = 2.7; x[8] = 3.6;

            y[0] = 7.8; y[1] = 5.5; y[2] = 8.0; y[3] = 9.0;
            y[4] = 6.5; y[5] = 4.0; y[6] = 6.3; y[7] = 8.4; y[8] = 10.2;

            List<Func<double, double>> v = new List<Func<double, double>>();
            v.Add(a => 1.0);
            v.Add(a => a);

            LinearRegression m = new LinearRegression(x, y);

            const double tol = 0.0002;
            double[] coeffExpected = new double[] { 0.9448, 2.6853 };
            double[] errorsExpected = new double[] { 0.3654, 0.1487 };

            for (int i = 0; i < 2; ++i) {
                if (Math.Abs(m.standardErrors()[i] - errorsExpected[i]) > tol) {
                    QAssert.Fail("Failed to reproduce linear regression standard errors"
                                + "\n    calculated: " + m.standardErrors()[i]
                                + "\n    expected:   " + errorsExpected[i]
                                + "\n    tolerance:  " + tol);
                }

                if (Math.Abs(m.coefficients()[i] - coeffExpected[i]) > tol) {
                    QAssert.Fail("Failed to reproduce linear regression coef."
                                + "\n    calculated: " + m.coefficients()[i]
                                + "\n    expected:   " + coeffExpected[i]
                                + "\n    tolerance:  " + tol);
                }
            }
        }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
        public void testMultiDimRegression() 
        {
            // Testing linear least-squares regression
            const int nr=100000;
            const int dims = 4;
            const double tolerance = 0.01;

            var rng = new InverseCumulativeRng<MersenneTwisterUniformRng,InverseCumulativeNormal>(
                            new MersenneTwisterUniformRng(1234u));

            List<Func<Vector, double>> v = new List<Func<Vector,double>>();
            v.Add(xx => 1.0);
            for (int i = 0; i < dims; ++i) {
                int jj = i;     // c# delegate work-around vs. boost bind; jj has to be evaluted before add delegate to the list
                v.Add(vv => vv[jj]);
            }

            Vector coeff = new Vector(v.Count);
            for (int i=0; i < v.Count; ++i)
                coeff[i] = rng.next().value;
            
            List<double> y = new InitializedList<double>(nr, 0.0);
            List<Vector> x = new InitializedList<Vector>(nr);
            for (int i=0; i < nr; ++i) {
                x[i] = new Vector(dims);
                for (int j = 0; j < dims; ++j) {
                    x[i][j] = rng.next().value;
                }
                
                for (int j=0; j < v.Count; ++j) {
                    y[i] += coeff[j]*v[j](x[i]);
                }
                y[i] += rng.next().value;
            }
            
            LinearLeastSquaresRegression<Vector> m = new LinearLeastSquaresRegression<Vector>(x, y, v);
            
            for (int i=0; i < v.Count; ++i) {
                if (m.standardErrors()[i] > tolerance) {
                    QAssert.Fail("Failed to reproduce linear regression coef."
                                + "\n    error:     " + m.standardErrors()[i]
                                + "\n    tolerance: " + tolerance);
                }
                
                if (Math.Abs(m.coefficients()[i]-coeff[i]) > 3*tolerance) {
                    QAssert.Fail("Failed to reproduce linear regression coef."
                                + "\n    calculated: " + m.coefficients()[i]
                                + "\n    error:      " + m.standardErrors()[i]
                                + "\n    expected:   " + coeff[i]);
                }
            }
        }

    }
}
