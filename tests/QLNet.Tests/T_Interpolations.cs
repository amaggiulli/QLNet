/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2015  Andrea Maggiulli (a.maggiulli@gmail.com)

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
   public class T_Interpolations
   {

      /* See J. M. Hyman, "Accurate monotonicity preserving cubic interpolation"
         SIAM J. of Scientific and Statistical Computing, v. 4, 1983, pp. 645-654.
         http://math.lanl.gov/~mac/papers/numerics/H83.pdf
      */
#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testSplineErrorOnGaussianValues()
      {
         //("Testing spline approximation on Gaussian data sets...");

         int[] points                = {      5,      9,     17,     33 };

         // complete spline data from the original 1983 Hyman paper
         double[] tabulatedErrors     = { 3.5e-2, 2.0e-3, 4.0e-5, 1.8e-6 };
         double[] toleranceOnTabErr   = { 0.1e-2, 0.1e-3, 0.1e-5, 0.1e-6 };

         // (complete) MC spline data from the original 1983 Hyman paper
         // NB: with the improved Hyman filter from the Dougherty, Edelman, and
         //     Hyman 1989 paper the n=17 nonmonotonicity is not filtered anymore
         //     so the error agrees with the non MC method.
         double[] tabulatedMCErrors   = { 1.7e-2, 2.0e-3, 4.0e-5, 1.8e-6 };
         double[] toleranceOnTabMCErr = { 0.1e-2, 0.1e-3, 0.1e-5, 0.1e-6 };

         SimpsonIntegral integral = new SimpsonIntegral(1e-12, 10000);

         // still unexplained scale factor needed to obtain the numerical
         // results from the paper
         double scaleFactor = 1.9;

         for (int i = 0; i < points.Length; i++)
         {
            int n = points[i];
            List<double> x = xRange(-1.7, 1.9, n);
            List<double> y = gaussian(x);

            // Not-a-knot
            CubicInterpolation f = new CubicInterpolation(x, x.Count, y,
                                                          CubicInterpolation.DerivativeApprox.Spline, false,
                                                          CubicInterpolation.BoundaryCondition.NotAKnot, 0,
                                                          CubicInterpolation.BoundaryCondition.NotAKnot, 0);
            f.update();
            double result = Math.Sqrt(integral.value(make_error_function(f).value, -1.7, 1.9));
            result /= scaleFactor;
            if (Math.Abs(result - tabulatedErrors[i]) > toleranceOnTabErr[i])
               QAssert.Fail("Not-a-knot spline interpolation "
                            + "\n    sample points:      " + n
                            + "\n    norm of difference: " + result
                            + "\n    it should be:       " + tabulatedErrors[i]);

            // MC not-a-knot
            f = new CubicInterpolation(x, x.Count, y,
                                       CubicInterpolation.DerivativeApprox.Spline, true,
                                       CubicInterpolation.BoundaryCondition.NotAKnot, 0,
                                       CubicInterpolation.BoundaryCondition.NotAKnot, 0);
            f.update();
            result = Math.Sqrt(integral.value(make_error_function(f).value, -1.7, 1.9));
            result /= scaleFactor;
            if (Math.Abs(result - tabulatedMCErrors[i]) > toleranceOnTabMCErr[i])
               QAssert.Fail("MC Not-a-knot spline interpolation "
                            + "\n    sample points:      " + n
                            + "\n    norm of difference: " + result
                            + "\n    it should be:       "
                            + tabulatedMCErrors[i]);
         }
      }

      /* See J. M. Hyman, "Accurate monotonicity preserving cubic interpolation"
         SIAM J. of Scientific and Statistical Computing, v. 4, 1983, pp. 645-654.
         http://math.lanl.gov/~mac/papers/numerics/H83.pdf
      */
#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testSplineOnGaussianValues()
      {

         //("Testing spline interpolation on a Gaussian data set...");

         double interpolated, interpolated2;
         int n = 5;

         List<double> x = new InitializedList<double>(n), y = new InitializedList<double>(n);
         double x1_bad = -1.7, x2_bad = 1.7;

         for (double start = -1.9, j = 0; j < 2; start += 0.2, j++)
         {
            x = xRange(start, start + 3.6, n);
            y = gaussian(x);

            // Not-a-knot spline
            CubicInterpolation f = new CubicInterpolation(x, x.Count, y,
                                                          CubicInterpolation.DerivativeApprox.Spline, false,
                                                          CubicInterpolation.BoundaryCondition.NotAKnot, 0,
                                                          CubicInterpolation.BoundaryCondition.NotAKnot, 0);
            f.update();
            checkValues("Not-a-knot spline", f, x, y);
            checkNotAKnotCondition("Not-a-knot spline", f);
            // bad performance
            interpolated = f.value(x1_bad);
            interpolated2 = f.value(x2_bad);
            if (interpolated > 0.0 && interpolated2 > 0.0)
            {
               QAssert.Fail("Not-a-knot spline interpolation "
                            + "bad performance unverified"
                            + "\nat x = " + x1_bad
                            + " interpolated value: " + interpolated
                            + "\nat x = " + x2_bad
                            + " interpolated value: " + interpolated
                            + "\n at least one of them was expected to be < 0.0");
            }

            // MC not-a-knot spline
            f = new CubicInterpolation(x, x.Count, y,
                                       CubicInterpolation.DerivativeApprox.Spline, true,
                                       CubicInterpolation.BoundaryCondition.NotAKnot, 0,
                                       CubicInterpolation.BoundaryCondition.NotAKnot, 0);
            f.update();
            checkValues("MC not-a-knot spline", f, x, y);
            // good performance
            interpolated = f.value(x1_bad);
            if (interpolated < 0.0)
            {
               QAssert.Fail("MC not-a-knot spline interpolation "
                            + "good performance unverified\n"
                            + "at x = " + x1_bad
                            + "\ninterpolated value: " + interpolated
                            + "\nexpected value > 0.0");
            }
            interpolated = f.value(x2_bad);
            if (interpolated < 0.0)
            {
               QAssert.Fail("MC not-a-knot spline interpolation "
                            + "good performance unverified\n"
                            + "at x = " + x2_bad
                            + "\ninterpolated value: " + interpolated
                            + "\nexpected value > 0.0");
            }
         }
      }

      /* See J. M. Hyman, "Accurate monotonicity preserving cubic interpolation"
         SIAM J. of Scientific and Statistical Computing, v. 4, 1983, pp. 645-654.
         http://math.lanl.gov/~mac/papers/numerics/H83.pdf
      */
#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testSplineOnRPN15AValues()
      {

         //("Testing spline interpolation on RPN15A data set...");

         List<double> RPN15A_x = new List<double>()
         {
            7.99,       8.09,       8.19,      8.7,
                        9.2,     10.0,     12.0,     15.0,     20.0
         };
         List<double> RPN15A_y = new List<double>()
         {
            0.0, 2.76429e-5, 4.37498e-5, 0.169183,
                 0.469428, 0.943740, 0.998636, 0.999919, 0.999994
         };

         double interpolated;

         // Natural spline
         CubicInterpolation f = new CubicInterpolation(RPN15A_x, RPN15A_x.Count, RPN15A_y,
                                                       CubicInterpolation.DerivativeApprox.Spline, false,
                                                       CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0,
                                                       CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0);
         f.update();
         checkValues("Natural spline", f, RPN15A_x, RPN15A_y);
         check2ndDerivativeValue("Natural spline", f, RPN15A_x.First(), 0.0);
         check2ndDerivativeValue("Natural spline", f, RPN15A_x.Last(), 0.0);
         // poor performance
         double x_bad = 11.0;
         interpolated = f.value(x_bad);
         if (interpolated < 1.0)
         {
            QAssert.Fail("Natural spline interpolation "
                         + "poor performance unverified\n"
                         + "at x = " + x_bad
                         + "\ninterpolated value: " + interpolated
                         + "\nexpected value > 1.0");
         }


         // Clamped spline
         f = new CubicInterpolation(RPN15A_x, RPN15A_x.Count, RPN15A_y,
                                    CubicInterpolation.DerivativeApprox.Spline, false,
                                    CubicInterpolation.BoundaryCondition.FirstDerivative, 0.0,
                                    CubicInterpolation.BoundaryCondition.FirstDerivative, 0.0);
         f.update();
         checkValues("Clamped spline", f, RPN15A_x, RPN15A_y);
         check1stDerivativeValue("Clamped spline", f, RPN15A_x.First(), 0.0);
         check1stDerivativeValue("Clamped spline", f, RPN15A_x.Last(), 0.0);
         // poor performance
         interpolated = f.value(x_bad);
         if (interpolated < 1.0)
         {
            QAssert.Fail("Clamped spline interpolation "
                         + "poor performance unverified\n"
                         + "at x = " + x_bad
                         + "\ninterpolated value: " + interpolated
                         + "\nexpected value > 1.0");
         }


         // Not-a-knot spline
         f = new CubicInterpolation(RPN15A_x, RPN15A_x.Count, RPN15A_y,
                                    CubicInterpolation.DerivativeApprox.Spline, false,
                                    CubicInterpolation.BoundaryCondition.NotAKnot, 0.0,
                                    CubicInterpolation.BoundaryCondition.NotAKnot, 0.0);

         f.update();
         checkValues("Not-a-knot spline", f, RPN15A_x, RPN15A_y);
         checkNotAKnotCondition("Not-a-knot spline", f);
         // poor performance
         interpolated = f.value(x_bad);
         if (interpolated < 1.0)
         {
            QAssert.Fail("Not-a-knot spline interpolation "
                         + "poor performance unverified\n"
                         + "at x = " + x_bad
                         + "\ninterpolated value: " + interpolated
                         + "\nexpected value > 1.0");
         }


         // MC natural spline values
         f = new CubicInterpolation(RPN15A_x, RPN15A_x.Count, RPN15A_y,
                                    CubicInterpolation.DerivativeApprox.Spline, true,
                                    CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0,
                                    CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0);
         f.update();
         checkValues("MC natural spline", f, RPN15A_x, RPN15A_y);
         // good performance
         interpolated = f.value(x_bad);
         if (interpolated > 1.0)
         {
            QAssert.Fail("MC natural spline interpolation "
                         + "good performance unverified\n"
                         + "at x = " + x_bad
                         + "\ninterpolated value: " + interpolated
                         + "\nexpected value < 1.0");
         }


         // MC clamped spline values
         f = new CubicInterpolation(RPN15A_x, RPN15A_x.Count, RPN15A_y,
                                    CubicInterpolation.DerivativeApprox.Spline, true,
                                    CubicInterpolation.BoundaryCondition.FirstDerivative, 0.0,
                                    CubicInterpolation.BoundaryCondition.FirstDerivative, 0.0);
         f.update();
         checkValues("MC clamped spline", f, RPN15A_x, RPN15A_y);
         check1stDerivativeValue("MC clamped spline", f, RPN15A_x.First(), 0.0);
         check1stDerivativeValue("MC clamped spline", f, RPN15A_x.Last(), 0.0);

         // good performance
         interpolated = f.value(x_bad);
         if (interpolated > 1.0)
         {
            QAssert.Fail("MC clamped spline interpolation "
                         + "good performance unverified\n"
                         + "at x = " + x_bad
                         + "\ninterpolated value: " + interpolated
                         + "\nexpected value < 1.0");
         }


         // MC not-a-knot spline values
         f = new CubicInterpolation(RPN15A_x, RPN15A_x.Count, RPN15A_y,
                                    CubicInterpolation.DerivativeApprox.Spline, true,
                                    CubicInterpolation.BoundaryCondition.NotAKnot, 0.0,
                                    CubicInterpolation.BoundaryCondition.NotAKnot, 0.0);
         f.update();
         checkValues("MC not-a-knot spline", f, RPN15A_x, RPN15A_y);
         // good performance
         interpolated = f.value(x_bad);
         if (interpolated > 1.0)
         {
            QAssert.Fail("MC clamped spline interpolation "
                         + "good performance unverified\n"
                         + "at x = " + x_bad
                         + "\ninterpolated value: " + interpolated
                         + "\nexpected value < 1.0");
         }
      }

      /* Blossey, Frigyik, Farnum "A Note On CubicSpline Splines"
         Applied Linear Algebra and Numerical Analysis AMATH 352 Lecture Notes
         http://www.amath.washington.edu/courses/352-winter-2002/spline_note.pdf
      */
#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testSplineOnGenericValues()
      {

         //("Testing spline interpolation on generic values...");

         List<double> generic_x = new List<double>() { 0.0, 1.0, 3.0, 4.0 };
         List<double> generic_y = new List<double>() { 0.0, 0.0, 2.0, 2.0 };
         List<double> generic_natural_y2 = new List<double>() { 0.0, 1.5, -1.5, 0.0 };

         double interpolated, error;
         int i, n = generic_x.Count;
         List<double> x35 = new InitializedList<double>(3);

         // Natural spline
         CubicInterpolation f = new CubicInterpolation(generic_x, generic_x.Count, generic_y,
                                                       CubicInterpolation.DerivativeApprox.Spline, false,
                                                       CubicInterpolation.BoundaryCondition.SecondDerivative,
                                                       generic_natural_y2[0],
                                                       CubicInterpolation.BoundaryCondition.SecondDerivative,
                                                       generic_natural_y2[n - 1]);
         f.update();
         checkValues("Natural spline", f, generic_x, generic_y);
         // cached second derivative
         for (i = 0; i < n; i++)
         {
            interpolated = f.secondDerivative(generic_x[i]);
            error = interpolated - generic_natural_y2[i];
            if (Math.Abs(error) > 3e-16)
            {
               QAssert.Fail("Natural spline interpolation "
                            + "second derivative failed at x=" + generic_x[i]
                            + "\ninterpolated value: " + interpolated
                            + "\nexpected value:     " + generic_natural_y2[i]
                            + "\nerror:              " + error);
            }
         }
         x35[1] = f.value(3.5);


         // Clamped spline
         double y1a = 0.0, y1b = 0.0;
         f = new CubicInterpolation(generic_x, generic_x.Count, generic_y,
                                    CubicInterpolation.DerivativeApprox.Spline, false,
                                    CubicInterpolation.BoundaryCondition.FirstDerivative, y1a,
                                    CubicInterpolation.BoundaryCondition.FirstDerivative, y1b);
         f.update();
         checkValues("Clamped spline", f, generic_x, generic_y);
         check1stDerivativeValue("Clamped spline", f, generic_x.First(), 0.0);
         check1stDerivativeValue("Clamped spline", f, generic_x.Last(), 0.0);
         x35[0] = f.value(3.5);


         // Not-a-knot spline
         f = new CubicInterpolation(generic_x, generic_x.Count, generic_y,
                                    CubicInterpolation.DerivativeApprox.Spline, false,
                                    CubicInterpolation.BoundaryCondition.NotAKnot, 0,
                                    CubicInterpolation.BoundaryCondition.NotAKnot, 0);
         f.update();
         checkValues("Not-a-knot spline", f, generic_x, generic_y);
         checkNotAKnotCondition("Not-a-knot spline", f);

         x35[2] = f.value(3.5);

         if (x35[0] > x35[1] || x35[1] > x35[2])
         {
            QAssert.Fail("Spline interpolation failure"
                         + "\nat x = " + 3.5
                         + "\nclamped spline    " + x35[0]
                         + "\nnatural spline    " + x35[1]
                         + "\nnot-a-knot spline " + x35[2]
                         + "\nvalues should be in increasing order");
         }
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testSimmetricEndConditions()
      {

         //("Testing symmetry of spline interpolation end-conditions...");

         int n = 9;

         List<double> x, y;
         x = xRange(-1.8, 1.8, n);
         y = gaussian(x);

         // Not-a-knot spline
         CubicInterpolation f = new CubicInterpolation(x, x.Count, y,
                                                       CubicInterpolation.DerivativeApprox.Spline, false,
                                                       CubicInterpolation.BoundaryCondition.NotAKnot, 0,
                                                       CubicInterpolation.BoundaryCondition.NotAKnot, 0);
         f.update();
         checkValues("Not-a-knot spline", f, x, y);
         checkNotAKnotCondition("Not-a-knot spline", f);
         checkSymmetry("Not-a-knot spline", f, x[0]);


         // MC not-a-knot spline
         f = new CubicInterpolation(x, x.Count, y,
                                    CubicInterpolation.DerivativeApprox.Spline, true,
                                    CubicInterpolation.BoundaryCondition.NotAKnot, 0,
                                    CubicInterpolation.BoundaryCondition.NotAKnot, 0);
         f.update();
         checkValues("MC not-a-knot spline", f, x, y);
         checkSymmetry("MC not-a-knot spline", f, x[0]);
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testDerivativeEndConditions()
      {

         //("Testing derivative end-conditions for spline interpolation...");

         int n = 4;

         List<double> x, y;
         x = xRange(-2.0, 2.0, n);
         y = parabolic(x);

         // Not-a-knot spline
         CubicInterpolation f = new CubicInterpolation(x, x.Count, y,
                                                       CubicInterpolation.DerivativeApprox.Spline, false,
                                                       CubicInterpolation.BoundaryCondition.NotAKnot, 0,
                                                       CubicInterpolation.BoundaryCondition.NotAKnot, 0);
         f.update();
         checkValues("Not-a-knot spline", f, x, y);
         check1stDerivativeValue("Not-a-knot spline", f, x[0], 4.0);
         check1stDerivativeValue("Not-a-knot spline", f, x[n - 1], -4.0);
         check2ndDerivativeValue("Not-a-knot spline", f, x[0], -2.0);
         check2ndDerivativeValue("Not-a-knot spline", f, x[n - 1], -2.0);


         // Clamped spline
         f = new CubicInterpolation(x, x.Count, y,
                                    CubicInterpolation.DerivativeApprox.Spline, false,
                                    CubicInterpolation.BoundaryCondition.FirstDerivative,  4.0,
                                    CubicInterpolation.BoundaryCondition.FirstDerivative, -4.0);
         f.update();
         checkValues("Clamped spline", f, x, y);
         check1stDerivativeValue("Clamped spline", f, x[0], 4.0);
         check1stDerivativeValue("Clamped spline", f, x[n - 1], -4.0);
         check2ndDerivativeValue("Clamped spline", f, x[0], -2.0);
         check2ndDerivativeValue("Clamped spline", f, x[n - 1], -2.0);


         // SecondDerivative spline
         f = new CubicInterpolation(x, x.Count, y,
                                    CubicInterpolation.DerivativeApprox.Spline, false,
                                    CubicInterpolation.BoundaryCondition.SecondDerivative, -2.0,
                                    CubicInterpolation.BoundaryCondition.SecondDerivative, -2.0);

         f.update();
         checkValues("SecondDerivative spline", f, x, y);
         check1stDerivativeValue("SecondDerivative spline", f, x[0], 4.0);
         check1stDerivativeValue("SecondDerivative spline", f, x[n - 1], -4.0);
         check2ndDerivativeValue("SecondDerivative spline", f, x[0], -2.0);
         check2ndDerivativeValue("SecondDerivative spline", f, x[n - 1], -2.0);

         // MC Not-a-knot spline
         f = new CubicInterpolation(x, x.Count, y,
                                    CubicInterpolation.DerivativeApprox.Spline, true,
                                    CubicInterpolation.BoundaryCondition.NotAKnot, 0,
                                    CubicInterpolation.BoundaryCondition.NotAKnot, 0);

         f.update();
         checkValues("MC Not-a-knot spline", f, x, y);
         check1stDerivativeValue("MC Not-a-knot spline", f, x[0], 4.0);
         check1stDerivativeValue("MC Not-a-knot spline", f, x[n - 1], -4.0);
         check2ndDerivativeValue("MC Not-a-knot spline", f, x[0], -2.0);
         check2ndDerivativeValue("MC Not-a-knot spline", f, x[n - 1], -2.0);


         // MC Clamped spline
         f = new CubicInterpolation(x, x.Count, y,
                                    CubicInterpolation.DerivativeApprox.Spline, true,
                                    CubicInterpolation.BoundaryCondition.FirstDerivative, 4.0,
                                    CubicInterpolation.BoundaryCondition.FirstDerivative, -4.0);

         f.update();
         checkValues("MC Clamped spline", f, x, y);
         check1stDerivativeValue("MC Clamped spline", f, x[0], 4.0);
         check1stDerivativeValue("MC Clamped spline", f, x[n - 1], -4.0);
         check2ndDerivativeValue("MC Clamped spline", f, x[0], -2.0);
         check2ndDerivativeValue("MC Clamped spline", f, x[n - 1], -2.0);


         // MC SecondDerivative spline
         f = new CubicInterpolation(x, x.Count, y,
                                    CubicInterpolation.DerivativeApprox.Spline, true,
                                    CubicInterpolation.BoundaryCondition.SecondDerivative, -2.0,
                                    CubicInterpolation.BoundaryCondition.SecondDerivative, -2.0);

         f.update();
         checkValues("MC SecondDerivative spline", f, x, y);
         check1stDerivativeValue("MC SecondDerivative spline", f, x[0], 4.0);
         check1stDerivativeValue("MC SecondDerivative spline", f, x[n - 1], -4.0);
         check2ndDerivativeValue("MC SecondDerivative spline", f, x[0], -2.0);
         check2ndDerivativeValue("MC SecondDerivative spline", f, x[n - 1], -2.0);
      }

      /* See R. L. Dougherty, A. Edelman, J. M. Hyman,
         "Nonnegativity-, Monotonicity-, or Convexity-Preserving CubicSpline and Quintic
         Hermite Interpolation"
         Mathematics Of Computation, v. 52, n. 186, April 1989, pp. 471-494.
      */
#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testNonRestrictiveHymanFilter()
      {

         //("Testing non-restrictive Hyman filter...");

         int n = 4;

         List<double> x, y;
         x = xRange(-2.0, 2.0, n);
         y = parabolic(x);
         double zero = 0.0, interpolated, expected = 0.0;

         // MC Not-a-knot spline
         CubicInterpolation f = new CubicInterpolation(x, x.Count, y,
                                                       CubicInterpolation.DerivativeApprox.Spline, true,
                                                       CubicInterpolation.BoundaryCondition.NotAKnot, 0,
                                                       CubicInterpolation.BoundaryCondition.NotAKnot, 0);
         f.update();
         interpolated = f.value(zero);
         if (Math.Abs(interpolated - expected) > 1e-15)
         {
            QAssert.Fail("MC not-a-knot spline"
                         + " interpolation failed at x = " + zero
                         + "\n    interpolated value: " + interpolated
                         + "\n    expected value:     " + expected
                         + "\n    error:              "
                         + Math.Abs(interpolated - expected));
         }


         // MC Clamped spline
         f = new CubicInterpolation(x, x.Count, y,
                                    CubicInterpolation.DerivativeApprox.Spline, true,
                                    CubicInterpolation.BoundaryCondition.FirstDerivative,  4.0,
                                    CubicInterpolation.BoundaryCondition.FirstDerivative, -4.0);
         f.update();
         interpolated = f.value(zero);
         if (Math.Abs(interpolated - expected) > 1e-15)
         {
            QAssert.Fail("MC clamped spline"
                         + " interpolation failed at x = " + zero
                         + "\n    interpolated value: " + interpolated
                         + "\n    expected value:     " + expected
                         + "\n    error:              "
                         + Math.Abs(interpolated - expected));
         }


         // MC SecondDerivative spline
         f = new CubicInterpolation(x, x.Count, y,
                                    CubicInterpolation.DerivativeApprox.Spline, true,
                                    CubicInterpolation.BoundaryCondition.SecondDerivative, -2.0,
                                    CubicInterpolation.BoundaryCondition.SecondDerivative, -2.0);
         f.update();
         interpolated = f.value(zero);
         if (Math.Abs(interpolated - expected) > 1e-15)
         {
            QAssert.Fail("MC SecondDerivative spline"
                         + " interpolation failed at x = " + zero
                         + "\n    interpolated value: " + interpolated
                         + "\n    expected value:     " + expected
                         + "\n    error:              "
                         + Math.Abs(interpolated - expected));
         }

      }


      //[TestMethod()]
      //public void testMultiSpline()
      //{
      //   // Testing N-dimensional cubic spline...

      //   List<int> dim = new List<int>() { 6, 5, 5, 6, 4 };

      //   List<double> args = new InitializedList<double>(5),
      //         offsets = new List<double>() { 1.005, 14.0, 33.005, 35.025, 19.025 };

      //   double s = args[0] = offsets[0],
      //         t = args[1] = offsets[1],
      //         u = args[2] = offsets[2],
      //         v = args[3] = offsets[3],
      //         w = args[4] = offsets[4];

      //   int i, j, k, l, m;

      //   SplineGrid grid = new SplineGrid(5);

      //   double r = 0.15;

      //   for (i = 0; i < 5; ++i) {
      //         double temp = offsets[i];
      //         for (j = 0; j < dim[i]; temp += r, ++j)
      //            grid[i].Add(temp);
      //   }

      //   r = 0.01;

      //   MultiCubicSpline<5>::data_table y5(dim);

      //   for (i = 0; i < dim[0]; ++i)
      //         for (j = 0; j < dim[1]; ++j)
      //            for (k = 0; k < dim[2]; ++k)
      //               for (l = 0; l < dim[3]; ++l)
      //                     for (m = 0; m < dim[4]; ++m)
      //                        y5[i][j][k][l][m] =
      //                           multif(grid[0][i], grid[1][j], grid[2][k],
      //                                    grid[3][l], grid[4][m]);

      //   MultiCubicSpline<5> cs(grid, y5);
      //   /* it would fail with
      //   for (i = 0; i < dim[0]; ++i)
      //         for (j = 0; j < dim[1]; ++j)
      //            for (k = 0; k < dim[2]; ++k)
      //               for (l = 0; l < dim[3]; ++l)
      //                     for (m = 0; m < dim[4]; ++m) {
      //   */
      //   for (i = 1; i < dim[0]-1; ++i)
      //         for (j = 1; j < dim[1]-1; ++j)
      //            for (k = 1; k < dim[2]-1; ++k)
      //               for (l = 1; l < dim[3]-1; ++l)
      //                     for (m = 1; m < dim[4]-1; ++m) {
      //                        s = grid[0][i];
      //                        t = grid[1][j];
      //                        u = grid[2][k];
      //                        v = grid[3][l];
      //                        w = grid[4][m];
      //                        double interpolated = cs(args);
      //                        double expected = y5[i][j][k][l][m];
      //                        double error = Math.Abs(interpolated-expected);
      //                        double tolerance = 1e-16;
      //                        if (error > tolerance) {
      //                           QAssert.Fail(
      //                                 "\n  At ("
      //                                 + s + "," + t + "," + u + ","
      //                                             + v + "," + w + "):"
      //                                 + "\n    interpolated: " + interpolated
      //                                 + "\n    actual value: " + expected
      //                                 + "\n       error: " + error
      //                                 + "\n    tolerance: " + tolerance);
      //                        }
      //                     }


      //   ulong seed = 42;
      //   SobolRsg rsg = new SobolRsg(5, seed);

      //   double tolerance = 1.7e-4;
      //   // actually tested up to 2^21-1=2097151 Sobol draws
      //   for (i = 0; i < 1023; ++i) {
      //         List<double> next = rsg.nextSequence().value;
      //         s = grid[0].front() + next[0]*(grid[0].back()-grid[0].front());
      //         t = grid[1].front() + next[1]*(grid[1].back()-grid[1].front());
      //         u = grid[2].front() + next[2]*(grid[2].back()-grid[2].front());
      //         v = grid[3].front() + next[3]*(grid[3].back()-grid[3].front());
      //         w = grid[4].front() + next[4]*(grid[4].back()-grid[4].front());
      //         double interpolated = cs(args), expected = multif(s, t, u, v, w);
      //         double error = Math.Abs(interpolated-expected);
      //         if (error > tolerance) {
      //            QAssert.Fail(
      //               "\n  At ("
      //               + s + "," + t + "," + u + "," + v + "," + w + "):"
      //               + "\n    interpolated: " + interpolated
      //               + "\n    actual value: " + expected
      //               + "\n    error:        " + error
      //               + "\n    tolerance:    " + tolerance);
      //         }
      //   }
      //}

      class NotThrown : Exception { }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testAsFunctor()
      {

         //("Testing use of interpolations as functors...");

         List<double> x = new List<double>() { 0.0, 1.0, 2.0, 3.0, 4.0 };
         List<double> y = new List<double>() { 5.0, 4.0, 3.0, 2.0, 1.0 };

         Interpolation f = new LinearInterpolation(x, x.Count, y);
         f.update();

         List<double> x2 = new List<double>() { -2.0, -1.0, 0.0, 1.0, 3.0, 4.0, 5.0, 6.0, 7.0 };
         int N = x2.Count;
         List<double> y2 = new InitializedList<double>(N);
         double tolerance = 1.0e-12;

         // case 1: extrapolation not allowed
         try
         {
            for (int xx = 0; xx < x2.Count; xx++)
            {
               y2[xx] = f.value(x2[xx]);
            }
            //y2 = x2.ConvertAll<double>(f.value);
            throw new NotThrown();
         }
         catch (NotThrown)
         {
            throw new Exception("failed to throw exception when trying to extrapolate");
         }
         catch { }

         // case 2: enable extrapolation
         f.enableExtrapolation();
         y2 = new InitializedList<double>(N);
         for (int xx = 0; xx < x2.Count; xx++)
         {
            y2[xx] = f.value(x2[xx]);
         }
         //y2 = x2.ConvertAll<double>(f.value);
         for (int i = 0; i < N; i++)
         {
            double expected = 5.0 - x2[i];
            if (Math.Abs(y2[i] - expected) > tolerance)
               QAssert.Fail(
                  "failed to reproduce " + (i + 1) + " expected datum"
                  + "\n    expected:   " + expected
                  + "\n    calculated: " + y2[i]
                  + "\n    error:      " + Math.Abs(y2[i] - expected));
         }
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testBackwardFlat()
      {

         //("Testing backward-flat interpolation...");

         List<double> x = new List<double>() { 0.0, 1.0, 2.0, 3.0, 4.0 };
         List<double> y = new List<double>() { 5.0, 4.0, 3.0, 2.0, 1.0 };

         Interpolation f = new BackwardFlatInterpolation(x, x.Count, y);
         f.update();

         int N = x.Count;
         int i;
         double tolerance = 1.0e-12, p, calculated, expected;

         // at original points
         for (i = 0; i < N; i++)
         {
            p = x[i];
            calculated = f.value(p);
            expected = y[i];
            if (Math.Abs(expected - calculated) > tolerance)
               QAssert.Fail(
                  "failed to reproduce " + (i + 1) + " datum"
                  + "\n    expected:   " + expected
                  + "\n    calculated: " + calculated
                  + "\n    error:      " + Math.Abs(calculated - expected));
         }

         // at middle points
         for (i = 0; i < N - 1; i++)
         {
            p = (x[i] + x[i + 1]) / 2;
            calculated = f.value(p);
            expected = y[i + 1];
            if (Math.Abs(expected - calculated) > tolerance)
               QAssert.Fail(
                  "failed to interpolate correctly at " + p
                  + "\n    expected:   " + expected
                  + "\n    calculated: " + calculated
                  + "\n    error:      " + Math.Abs(calculated - expected));
         }

         // outside the original range
         f.enableExtrapolation();

         p = x[0] - 0.5;
         calculated = f.value(p);
         expected = y[0];
         if (Math.Abs(expected - calculated) > tolerance)
            QAssert.Fail(
               "failed to extrapolate correctly at " + p
               + "\n    expected:   " + expected
               + "\n    calculated: " + calculated
               + "\n    error:      " + Math.Abs(calculated - expected));

         p = x[N - 1] + 0.5;
         calculated = f.value(p);
         expected = y[N - 1];
         if (Math.Abs(expected - calculated) > tolerance)
            QAssert.Fail(
               "failed to extrapolate correctly at " + p
               + "\n    expected:   " + expected
               + "\n    calculated: " + calculated
               + "\n    error:      " + Math.Abs(calculated - expected));

         // primitive at original points
         calculated = f.primitive(x[0]);
         expected = 0.0;
         if (Math.Abs(expected - calculated) > tolerance)
            QAssert.Fail(
               "failed to calculate primitive at " + x[0]
               + "\n    expected:   " + expected
               + "\n    calculated: " + calculated
               + "\n    error:      " + Math.Abs(calculated - expected));

         double sum = 0.0;
         for (i = 1; i < N; i++)
         {
            sum += (x[i] - x[i - 1]) * y[i];
            calculated = f.primitive(x[i]);
            expected = sum;
            if (Math.Abs(expected - calculated) > tolerance)
               QAssert.Fail(
                  "failed to calculate primitive at " + x[i]
                  + "\n    expected:   " + expected
                  + "\n    calculated: " + calculated
                  + "\n    error:      " + Math.Abs(calculated - expected));
         }

         // primitive at middle points
         sum = 0.0;
         for (i = 0; i < N - 1; i++)
         {
            p = (x[i] + x[i + 1]) / 2;
            sum += (x[i + 1] - x[i]) * y[i + 1] / 2;
            calculated = f.primitive(p);
            expected = sum;
            sum += (x[i + 1] - x[i]) * y[i + 1] / 2;
            if (Math.Abs(expected - calculated) > tolerance)
               QAssert.Fail(
                  "failed to calculate primitive at " + x[i]
                  + "\n    expected:   " + expected
                  + "\n    calculated: " + calculated
                  + "\n    error:      " + Math.Abs(calculated - expected));
         }

      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testForwardFlat()
      {

         //("Testing forward-flat interpolation...");

         List<double> x = new List<double>() { 0.0, 1.0, 2.0, 3.0, 4.0 };
         List<double> y = new List<double>() { 5.0, 4.0, 3.0, 2.0, 1.0 };

         Interpolation f = new ForwardFlatInterpolation(x, x.Count, y);
         f.update();

         int N = x.Count;
         int i;
         double tolerance = 1.0e-12, p, calculated, expected;

         // at original points
         for (i = 0; i < N; i++)
         {
            p = x[i];
            calculated = f.value(p);
            expected = y[i];
            if (Math.Abs(expected - calculated) > tolerance)
               QAssert.Fail(
                  "failed to reproduce " + (i + 1) + " datum"
                  + "\n    expected:   " + expected
                  + "\n    calculated: " + calculated
                  + "\n    error:      " + Math.Abs(calculated - expected));
         }

         // at middle points
         for (i = 0; i < N - 1; i++)
         {
            p = (x[i] + x[i + 1]) / 2;
            calculated = f.value(p);
            expected = y[i];
            if (Math.Abs(expected - calculated) > tolerance)
               QAssert.Fail(
                  "failed to interpolate correctly at " + p
                  + "\n    expected:   " + expected
                  + "\n    calculated: " + calculated
                  + "\n    error:      " + Math.Abs(calculated - expected));
         }

         // outside the original range
         f.enableExtrapolation();

         p = x[0] - 0.5;
         calculated = f.value(p);
         expected = y[0];
         if (Math.Abs(expected - calculated) > tolerance)
            QAssert.Fail(
               "failed to extrapolate correctly at " + p
               + "\n    expected:   " + expected
               + "\n    calculated: " + calculated
               + "\n    error:      " + Math.Abs(calculated - expected));

         p = x[N - 1] + 0.5;
         calculated = f.value(p);
         expected = y[N - 1];
         if (Math.Abs(expected - calculated) > tolerance)
            QAssert.Fail(
               "failed to extrapolate correctly at " + p
               + "\n    expected:   " + expected
               + "\n    calculated: " + calculated
               + "\n    error:      " + Math.Abs(calculated - expected));

         // primitive at original points
         calculated = f.primitive(x[0]);
         expected = 0.0;
         if (Math.Abs(expected - calculated) > tolerance)
            QAssert.Fail(
               "failed to calculate primitive at " + x[0]
               + "\n    expected:   " + expected
               + "\n    calculated: " + calculated
               + "\n    error:      " + Math.Abs(calculated - expected));

         double sum = 0.0;
         for (i = 1; i < N; i++)
         {
            sum += (x[i] - x[i - 1]) * y[i - 1];
            calculated = f.primitive(x[i]);
            expected = sum;
            if (Math.Abs(expected - calculated) > tolerance)
               QAssert.Fail(
                  "failed to calculate primitive at " + x[i]
                  + "\n    expected:   " + expected
                  + "\n    calculated: " + calculated
                  + "\n    error:      " + Math.Abs(calculated - expected));
         }

         // primitive at middle points
         sum = 0.0;
         for (i = 0; i < N - 1; i++)
         {
            p = (x[i] + x[i + 1]) / 2;
            sum += (x[i + 1] - x[i]) * y[i] / 2;
            calculated = f.primitive(p);
            expected = sum;
            sum += (x[i + 1] - x[i]) * y[i] / 2;
            if (Math.Abs(expected - calculated) > tolerance)
               QAssert.Fail(
                  "failed to calculate primitive at " + p
                  + "\n    expected:   " + expected
                  + "\n    calculated: " + calculated
                  + "\n    error:      " + Math.Abs(calculated - expected));
         }
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testSabrInterpolation()
      {
         // Testing Sabr interpolation...

         // Test SABR function against input volatilities
         double tolerance = 1.0e-12;
         List<double> strikes = new InitializedList<double>(31);
         List<double> volatilities = new InitializedList<double>(31);
         // input strikes
         strikes[0] = 0.03 ; strikes[1] = 0.032 ; strikes[2] = 0.034 ;
         strikes[3] = 0.036 ; strikes[4] = 0.038 ; strikes[5] = 0.04 ;
         strikes[6] = 0.042 ; strikes[7] = 0.044 ; strikes[8] = 0.046 ;
         strikes[9] = 0.048 ; strikes[10] = 0.05 ; strikes[11] = 0.052 ;
         strikes[12] = 0.054 ; strikes[13] = 0.056 ; strikes[14] = 0.058 ;
         strikes[15] = 0.06 ; strikes[16] = 0.062 ; strikes[17] = 0.064 ;
         strikes[18] = 0.066 ; strikes[19] = 0.068 ; strikes[20] = 0.07 ;
         strikes[21] = 0.072 ; strikes[22] = 0.074 ; strikes[23] = 0.076 ;
         strikes[24] = 0.078 ; strikes[25] = 0.08 ; strikes[26] = 0.082 ;
         strikes[27] = 0.084 ; strikes[28] = 0.086 ; strikes[29] = 0.088;
         strikes[30] = 0.09;
         // input volatilities
         volatilities[0] = 1.16725837321531 ; volatilities[1] = 1.15226075991385 ; volatilities[2] = 1.13829711098834 ;
         volatilities[3] = 1.12524190877505 ; volatilities[4] = 1.11299079244474 ; volatilities[5] = 1.10145609357162 ;
         volatilities[6] = 1.09056348513411 ; volatilities[7] = 1.08024942745106 ; volatilities[8] = 1.07045919457758 ;
         volatilities[9] = 1.06114533019077 ; volatilities[10] = 1.05226642581503 ; volatilities[11] = 1.04378614411707 ;
         volatilities[12] = 1.03567243073732 ; volatilities[13] = 1.0278968727451 ; volatilities[14] = 1.02043417226345 ;
         volatilities[15] = 1.01326171139321 ; volatilities[16] = 1.00635919013311 ; volatilities[17] = 0.999708323124949 ;
         volatilities[18] = 0.993292584155381 ; volatilities[19] = 0.987096989695393 ; volatilities[20] = 0.98110791455717 ;
         volatilities[21] = 0.975312934134512 ; volatilities[22] = 0.969700688771689 ; volatilities[23] = 0.964260766651027;
         volatilities[24] = 0.958983602256592 ; volatilities[25] = 0.953860388001395 ; volatilities[26] = 0.948882997029509 ;
         volatilities[27] = 0.944043915545469 ; volatilities[28] = 0.939336183299237 ; volatilities[29] = 0.934753341079515 ;
         volatilities[30] = 0.930289384251337;

         double expiry = 1.0;
         double forward = 0.039;
         // input SABR coefficients (corresponding to the vols above)
         double initialAlpha = 0.3;
         double initialBeta = 0.6;
         double initialNu = 0.02;
         double initialRho = 0.01;
         // calculate SABR vols and compare with input vols
         for (int i = 0; i < strikes.Count; i++)
         {
            double calculatedVol = Utils.sabrVolatility(strikes[i], forward, expiry,
                                                        initialAlpha, initialBeta,
                                                        initialNu, initialRho);
            if (Math.Abs(volatilities[i] - calculatedVol) > tolerance)
               QAssert.Fail("failed to calculate Sabr function at strike " + strikes[i]
                            + "\n    expected:   " + volatilities[i]
                            + "\n    calculated: " + calculatedVol
                            + "\n    error:      " + Math.Abs(calculatedVol - volatilities[i]));
         }

         // Test SABR calibration against input parameters
         // Use default values (but not null, since then parameters
         // will then not be fixed during optimization, see the
         // interpolation constructor, thus rendering the test cases
         // with fixed parameters non-sensical)
         double alphaGuess = Math.Sqrt(0.2);
         double betaGuess = 0.5;
         double nuGuess = Math.Sqrt(0.4);
         double rhoGuess = 0.0;

         bool[] vegaWeighted = {true, false};
         bool[] isAlphaFixed = {true, false};
         bool[] isBetaFixed = {true, false};
         bool[] isNuFixed = {true, false};
         bool[] isRhoFixed = {true, false};

         double calibrationTolerance = 5.0e-8;
         // initialize optimization methods
         List<OptimizationMethod>  methods_ = new List<OptimizationMethod>();
         methods_.Add(new Simplex(0.01));
         methods_.Add(new LevenbergMarquardt(1e-8, 1e-8, 1e-8));
         // Initialize end criteria
         EndCriteria endCriteria = new EndCriteria(100000, 100, 1e-8, 1e-8, 1e-8);
         // Test looping over all possibilities
         for (int j = 0; j < methods_.Count; ++j)
         {
            for (int i = 0; i < vegaWeighted.Length; ++i)
            {
               for (int k_a = 0; k_a < isAlphaFixed.Length; ++k_a)
               {
                  for (int k_b = 0; k_b < isBetaFixed.Length ; ++k_b)
                  {
                     for (int k_n = 0; k_n < isNuFixed.Length ; ++k_n)
                     {
                        for (int k_r = 0; k_r < isRhoFixed.Length; ++k_r)
                        {
                           // to meet the tough calibration tolerance we need to lower the default
                           // error threshold for accepting a calibration (to be more specific, some
                           // of the new test cases arising from fixing a subset of the model's
                           // parameters do not calibrate with the desired error using the initial
                           // guess (i.e. optimization runs into a local minimum) - then a series of
                           // random start values for optimization is chosen until our tight custom
                           // error threshold is satisfied.
                           SABRInterpolation sabrInterpolation = new SABRInterpolation(
                              strikes, strikes.Count, volatilities,
                              expiry, forward, isAlphaFixed[k_a] ? initialAlpha : alphaGuess,
                              isBetaFixed[k_b] ? initialBeta : betaGuess,
                              isNuFixed[k_n] ? initialNu : nuGuess,
                              isRhoFixed[k_r] ? initialRho : rhoGuess, isAlphaFixed[k_a],
                              isBetaFixed[k_b], isNuFixed[k_n], isRhoFixed[k_r],
                              vegaWeighted[i], endCriteria, methods_[j], 1E-10);
                           sabrInterpolation.update();

                           // Recover SABR calibration parameters
                           bool failed = false;
                           double calibratedAlpha = sabrInterpolation.alpha();
                           double calibratedBeta = sabrInterpolation.beta();
                           double calibratedNu = sabrInterpolation.nu();
                           double calibratedRho = sabrInterpolation.rho();
                           double error;

                           // compare results: alpha
                           error = Math.Abs(initialAlpha - calibratedAlpha);
                           if (error > calibrationTolerance)
                           {
                              QAssert.Fail("\nfailed to calibrate alpha Sabr parameter:" +
                                           "\n    expected:        " + initialAlpha +
                                           "\n    calibrated:      " + calibratedAlpha +
                                           "\n    error:           " + error);
                              failed = true;
                           }
                           // Beta
                           error = Math.Abs(initialBeta - calibratedBeta);
                           if (error > calibrationTolerance)
                           {
                              QAssert.Fail("\nfailed to calibrate beta Sabr parameter:" +
                                           "\n    expected:        " + initialBeta +
                                           "\n    calibrated:      " + calibratedBeta +
                                           "\n    error:           " + error);
                              failed = true;
                           }
                           // Nu
                           error = Math.Abs(initialNu - calibratedNu);
                           if (error > calibrationTolerance)
                           {
                              QAssert.Fail("\nfailed to calibrate nu Sabr parameter:" +
                                           "\n    expected:        " + initialNu +
                                           "\n    calibrated:      " + calibratedNu +
                                           "\n    error:           " + error);
                              failed = true;
                           }
                           // Rho
                           error = Math.Abs(initialRho - calibratedRho);
                           if (error > calibrationTolerance)
                           {
                              QAssert.Fail("\nfailed to calibrate rho Sabr parameter:" +
                                           "\n    expected:        " + initialRho +
                                           "\n    calibrated:      " + calibratedRho +
                                           "\n    error:           " + error);
                              failed = true;
                           }

                           if (failed)
                              QAssert.Fail("\nSabr calibration failure:" +
                                           "\n    isAlphaFixed:    " + isAlphaFixed[k_a] +
                                           "\n    isBetaFixed:     " + isBetaFixed[k_b] +
                                           "\n    isNuFixed:       " + isNuFixed[k_n] +
                                           "\n    isRhoFixed:      " + isRhoFixed[k_r] +
                                           "\n    vegaWeighted[i]: " + vegaWeighted[i]);
                        }
                     }
                  }
               }
            }
         }
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testNormalSabrInterpolation()
      {
         // Testing Sabr interpolation...

         // Test SABR function against input volatilities
         double tolerance = 1.0e-12;
         List<double> strikes = new InitializedList<double>(31);
         List<double> volatilities = new InitializedList<double>(31);
         // input strikes
         strikes[0] = 0.03; strikes[1] = 0.032; strikes[2] = 0.034;
         strikes[3] = 0.036; strikes[4] = 0.038; strikes[5] = 0.04;
         strikes[6] = 0.042; strikes[7] = 0.044; strikes[8] = 0.046;
         strikes[9] = 0.048; strikes[10] = 0.05; strikes[11] = 0.052;
         strikes[12] = 0.054; strikes[13] = 0.056; strikes[14] = 0.058;
         strikes[15] = 0.06; strikes[16] = 0.062; strikes[17] = 0.064;
         strikes[18] = 0.066; strikes[19] = 0.068; strikes[20] = 0.07;
         strikes[21] = 0.072; strikes[22] = 0.074; strikes[23] = 0.076;
         strikes[24] = 0.078; strikes[25] = 0.08; strikes[26] = 0.082;
         strikes[27] = 0.084; strikes[28] = 0.086; strikes[29] = 0.088;
         strikes[30] = 0.09;
         // input volatilities
         volatilities[0] = 0.00302585323005545; volatilities[1] = 0.00294383987488083; volatilities[2] = 0.00288298622420075;
         volatilities[3] = 0.00285179755813925; volatilities[4] = 0.00285792878198394; volatilities[5] = 0.00290512184396261;
         volatilities[6] = 0.00299136112388108; volatilities[7] = 0.00311009972637092; volatilities[8] = 0.00325327416236817;
         volatilities[9] = 0.00341363771488595; volatilities[10] = 0.0035856197690038; volatilities[11] = 0.00376525972074818;
         volatilities[12] = 0.00394983924954725; volatilities[13] = 0.00413751556423761; volatilities[14] = 0.0043270404491738;
         volatilities[15] = 0.00451756485802441; volatilities[16] = 0.00470850804875522; volatilities[17] = 0.00489947094428383;
         volatilities[18] = 0.00509017869762345; volatilities[19] = 0.00528044232532999; volatilities[20] = 0.00547013280521446;
         volatilities[21] = 0.0056591633819362; volatilities[22] = 0.00584747733448256; volatilities[23] = 0.0060350394214727;
         volatilities[24] = 0.00622182983342147; volatilities[25] = 0.00640783987460733; volatilities[26] = 0.00659306885217061;
         volatilities[27] = 0.0067775218171438; volatilities[28] = 0.00696120791288998; volatilities[29] = 0.00714413916074818;
         volatilities[30] = 0.00732632956313855;


         double expiry = 1.0;
         double forward = 0.039;
         // input SABR coefficients (corresponding to the vols above)
         double initialAlpha = 0.3;
         double initialBeta = 0.6;
         double initialNu = 0.02;
         double initialRho = 0.01;
         // calculate SABR vols and compare with input vols
         for (int i = 0; i < strikes.Count; i++)
         {
            double calculatedVol = Utils.shiftedSabrNormalVolatility(strikes[i], forward, expiry,
                                                                     initialAlpha, initialBeta,
                                                                     initialNu, initialRho);
            if (Math.Abs(volatilities[i] - calculatedVol) > tolerance)
               QAssert.Fail("failed to calculate Sabr function at strike " + strikes[i]
                            + "\n    expected:   " + volatilities[i]
                            + "\n    calculated: " + calculatedVol
                            + "\n    error:      " + Math.Abs(calculatedVol - volatilities[i]));
         }

         // Test SABR calibration against input parameters
         // Use default values (but not null, since then parameters
         // will then not be fixed during optimization, see the
         // interpolation constructor, thus rendering the test cases
         // with fixed parameters non-sensical)
         double alphaGuess = Math.Sqrt(0.2);
         double betaGuess = 0.5;
         double nuGuess = Math.Sqrt(0.4);
         double rhoGuess = 0.0;

         bool[] vegaWeighted = { true, false };
         bool[] isAlphaFixed = { true, false };
         bool[] isBetaFixed = { true, false };
         bool[] isNuFixed = { true, false };
         bool[] isRhoFixed = { true, false };

         double calibrationTolerance = 5.0e-8;
         // initialize optimization methods
         List<OptimizationMethod> methods_ = new List<OptimizationMethod>();
         methods_.Add(new Simplex(0.01));
         methods_.Add(new LevenbergMarquardt(1e-8, 1e-8, 1e-8));
         // Initialize end criteria
         EndCriteria endCriteria = new EndCriteria(100000, 100, 1e-8, 1e-8, 1e-8);
         // Test looping over all possibilities
         for (int j = 0; j < methods_.Count; ++j)
         {
            for (int i = 0; i < vegaWeighted.Length; ++i)
            {
               for (int k_a = 0; k_a < isAlphaFixed.Length; ++k_a)
               {
                  for (int k_b = 0; k_b < isBetaFixed.Length; ++k_b)
                  {
                     for (int k_n = 0; k_n < isNuFixed.Length; ++k_n)
                     {
                        for (int k_r = 0; k_r < isRhoFixed.Length; ++k_r)
                        {
                           // to meet the tough calibration tolerance we need to lower the default
                           // error threshold for accepting a calibration (to be more specific, some
                           // of the new test cases arising from fixing a subset of the model's
                           // parameters do not calibrate with the desired error using the initial
                           // guess (i.e. optimization runs into a local minimum) - then a series of
                           // random start values for optimization is chosen until our tight custom
                           // error threshold is satisfied.
                           SABRInterpolation sabrInterpolation = new SABRInterpolation(
                              strikes, strikes.Count, volatilities,
                              expiry, forward, isAlphaFixed[k_a] ? initialAlpha : alphaGuess,
                              isBetaFixed[k_b] ? initialBeta : betaGuess,
                              isNuFixed[k_n] ? initialNu : nuGuess,
                              isRhoFixed[k_r] ? initialRho : rhoGuess, isAlphaFixed[k_a],
                              isBetaFixed[k_b], isNuFixed[k_n], isRhoFixed[k_r],
                              vegaWeighted[i], endCriteria, methods_[j], 1E-10, false, 50, 0.0, VolatilityType.Normal);
                           sabrInterpolation.update();

                           // Recover SABR calibration parameters
                           bool failed = false;
                           double calibratedAlpha = sabrInterpolation.alpha();
                           double calibratedBeta = sabrInterpolation.beta();
                           double calibratedNu = sabrInterpolation.nu();
                           double calibratedRho = sabrInterpolation.rho();
                           double error;

                           // compare results: alpha
                           error = Math.Abs(initialAlpha - calibratedAlpha);
                           if (error > calibrationTolerance)
                           {
                              QAssert.Fail("\nfailed to calibrate alpha Sabr parameter:" +
                                           "\n    expected:        " + initialAlpha +
                                           "\n    calibrated:      " + calibratedAlpha +
                                           "\n    error:           " + error);
                              failed = true;
                           }
                           // Beta
                           error = Math.Abs(initialBeta - calibratedBeta);
                           if (error > calibrationTolerance)
                           {
                              QAssert.Fail("\nfailed to calibrate beta Sabr parameter:" +
                                           "\n    expected:        " + initialBeta +
                                           "\n    calibrated:      " + calibratedBeta +
                                           "\n    error:           " + error);
                              failed = true;
                           }
                           // Nu
                           error = Math.Abs(initialNu - calibratedNu);
                           if (error > calibrationTolerance)
                           {
                              QAssert.Fail("\nfailed to calibrate nu Sabr parameter:" +
                                           "\n    expected:        " + initialNu +
                                           "\n    calibrated:      " + calibratedNu +
                                           "\n    error:           " + error);
                              failed = true;
                           }
                           // Rho
                           error = Math.Abs(initialRho - calibratedRho);
                           if (error > calibrationTolerance)
                           {
                              QAssert.Fail("\nfailed to calibrate rho Sabr parameter:" +
                                           "\n    expected:        " + initialRho +
                                           "\n    calibrated:      " + calibratedRho +
                                           "\n    error:           " + error);
                              failed = true;
                           }

                           if (failed)
                              QAssert.Fail("\nSabr calibration failure:" +
                                           "\n    isAlphaFixed:    " + isAlphaFixed[k_a] +
                                           "\n    isBetaFixed:     " + isBetaFixed[k_b] +
                                           "\n    isNuFixed:       " + isNuFixed[k_n] +
                                           "\n    isRhoFixed:      " + isRhoFixed[k_r] +
                                           "\n    vegaWeighted[i]: " + vegaWeighted[i]);
                        }
                     }
                  }
               }
            }
         }
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testKernelInterpolation()
      {

         // Testing kernel 1D interpolation

         List<double> deltaGrid = new InitializedList<double>(5); // x-values, here delta in FX
         deltaGrid[0] = 0.10; deltaGrid[1] = 0.25; deltaGrid[2] = 0.50;
         deltaGrid[3] = 0.75; deltaGrid[4] = 0.90;

         List<double> yd1 = new InitializedList<double>(deltaGrid.Count);   // test y-values 1
         yd1[0] = 11.275; yd1[1] = 11.125; yd1[2] = 11.250;
         yd1[3] = 11.825; yd1[4] = 12.625;

         List<double> yd2 = new InitializedList<double>(deltaGrid.Count);   // test y-values 2
         yd2[0] = 16.025; yd2[1] = 13.450; yd2[2] = 11.350;
         yd2[3] = 10.150; yd2[4] = 10.075;

         List<double> yd3 = new InitializedList<double>(deltaGrid.Count);   // test y-values 3
         yd3[0] = 10.3000; yd3[1] = 9.6375; yd3[2] = 9.2000;
         yd3[3] = 9.1125; yd3[4] = 9.4000;

         List<List<double> > yd = new List<List<double>>();
         yd.Add(yd1);
         yd.Add(yd2);
         yd.Add(yd3);

         List<double> lambdaVec = new InitializedList<double>(5);
         lambdaVec[0] = 0.05; lambdaVec[1] = 0.50; lambdaVec[2] = 0.75;
         lambdaVec[3] = 1.65; lambdaVec[4] = 2.55;

         double tolerance = 2.0e-5;

         double expectedVal;
         double calcVal;

         // Check that y-values at knots are exactly the feeded y-values,
         // irrespective of kernel parameters
         for (int i = 0; i < lambdaVec.Count; ++i)
         {
            GaussianKernel myKernel = new GaussianKernel(0, lambdaVec[i]);

            for (int j = 0; j < yd.Count; ++j)
            {
               List<double> currY = yd[j];
               KernelInterpolation f = new KernelInterpolation(deltaGrid, deltaGrid.Count, currY, myKernel);
               f.update();

               for (int dIt = 0; dIt < deltaGrid.Count; ++dIt)
               {
                  expectedVal = currY[dIt];
                  calcVal = f.value(deltaGrid[dIt]);

                  if (Math.Abs(expectedVal - calcVal) > tolerance)
                  {

                     QAssert.Fail("Kernel interpolation failed at x = "
                                  + deltaGrid[dIt]
                                  + "\n    interpolated value: " + calcVal
                                  + "\n    expected value:     " + expectedVal
                                  + "\n    error:              "
                                  + Math.Abs(expectedVal - calcVal));

                  }
               }
            }
         }

         List<double> testDeltaGrid = new InitializedList<double>(deltaGrid.Count);
         testDeltaGrid[0] = 0.121; testDeltaGrid[1] = 0.279; testDeltaGrid[2] = 0.678;
         testDeltaGrid[3] = 0.790; testDeltaGrid[4] = 0.980;

         // Gaussian Kernel values for testDeltaGrid with a standard
         // deviation of 2.05 (the value is arbitrary.)  Source: parrallel
         // implementation in R, no literature sources found

         List<double> ytd1 = new InitializedList<double>(testDeltaGrid.Count);
         ytd1[0] = 11.23847; ytd1[1] = 11.12003; ytd1[2] = 11.58932;
         ytd1[3] = 11.99168; ytd1[4] = 13.29650;

         List<double> ytd2 = new InitializedList<double>(testDeltaGrid.Count);
         ytd2[0] = 15.55922; ytd2[1] = 13.11088; ytd2[2] = 10.41615;
         ytd2[3] = 10.05153; ytd2[4] = 10.50741;

         List<double> ytd3 = new InitializedList<double>(testDeltaGrid.Count);
         ytd3[0] = 10.17473; ytd3[1] = 9.557842; ytd3[2] = 9.09339;
         ytd3[3] = 9.149687; ytd3[4] = 9.779971;

         List<List<double> > ytd = new List<List<double>>();
         ytd.Add(ytd1);
         ytd.Add(ytd2);
         ytd.Add(ytd3);

         GaussianKernel myKernel2 = new GaussianKernel(0, 2.05);

         for (int j = 0; j < ytd.Count; ++j)
         {
            List<double> currY = yd[j];
            List<double> currTY = ytd[j];

            // Build interpolation according to original grid + y-values
            KernelInterpolation f = new KernelInterpolation(deltaGrid, deltaGrid.Count, currY, myKernel2);
            f.update();

            // test values at test Grid
            for (int dIt = 0; dIt < testDeltaGrid.Count; ++dIt)
            {
               expectedVal = currTY[dIt];
               f.enableExtrapolation();// allow extrapolation

               calcVal = f.value(testDeltaGrid[dIt]);
               if (Math.Abs(expectedVal - calcVal) > tolerance)
               {

                  QAssert.Fail("Kernel interpolation failed at x = "
                               + deltaGrid[dIt]
                               + "\n    interpolated value: " + calcVal
                               + "\n    expected value:     " + expectedVal
                               + "\n    error:              "
                               + Math.Abs(expectedVal - calcVal));
               }
            }

         }

      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testKernelInterpolation2D()
      {
         // No test values known from the literature.
         // Testing for consistency of input output data
         // at the nodes

         // Testing kernel 2D interpolation...

         double mean = 0.0, var = 0.18;
         GaussianKernel myKernel = new GaussianKernel(mean, var);

         List<double> xVec = new InitializedList<double>(10);
         xVec[0] = 0.10; xVec[1] = 0.20; xVec[2] = 0.30; xVec[3] = 0.40;
         xVec[4] = 0.50; xVec[5] = 0.60; xVec[6] = 0.70; xVec[7] = 0.80;
         xVec[8] = 0.90; xVec[9] = 1.00;

         List<double> yVec = new InitializedList<double>(3);
         yVec[0] = 1.0; yVec[1] = 2.0; yVec[2] = 3.5;

         Matrix M = new Matrix(xVec.Count, yVec.Count);

         M[0, 0] = 0.25; M[1, 0] = 0.24; M[2, 0] = 0.23; M[3, 0] = 0.20; M[4, 0] = 0.19;
         M[5, 0] = 0.20; M[6, 0] = 0.21; M[7, 0] = 0.22; M[8, 0] = 0.26; M[9, 0] = 0.29;

         M[0, 1] = 0.27; M[1, 1] = 0.26; M[2, 1] = 0.25; M[3, 1] = 0.22; M[4, 1] = 0.21;
         M[5, 1] = 0.22; M[6, 1] = 0.23; M[7, 1] = 0.24; M[8, 1] = 0.28; M[9, 1] = 0.31;

         M[0, 2] = 0.21; M[1, 2] = 0.22; M[2, 2] = 0.27; M[3, 2] = 0.29; M[4, 2] = 0.24;
         M[5, 2] = 0.28; M[6, 2] = 0.25; M[7, 2] = 0.22; M[8, 2] = 0.29; M[9, 2] = 0.30;

         KernelInterpolation2D kernel2D = new KernelInterpolation2D(xVec, xVec.Count, yVec, yVec.Count, M, myKernel);

         double calcVal, expectedVal;
         double tolerance = 1.0e-10;

         for (int i = 0; i < M.rows(); ++i)
         {
            for (int j = 0; j < M.columns(); ++j)
            {

               calcVal = kernel2D.value(xVec[i], yVec[j]);
               expectedVal = M[i, j];

               if (Math.Abs(expectedVal - calcVal) > tolerance)
               {

                  QAssert.Fail("2D Kernel interpolation failed at x = " + xVec[i]
                               + ", y = " + yVec[j]
                               + "\n    interpolated value: " + calcVal
                               + "\n    expected value:     " + expectedVal
                               + "\n    error:              "
                               + Math.Abs(expectedVal - calcVal));
               }
            }
         }

         // alternative data set
         List<double> xVec1 = new InitializedList<double>(4);
         xVec1[0] = 80.0; xVec1[1] = 90.0; xVec1[2] = 100.0; xVec1[3] = 110.0;

         List<double> yVec1 = new InitializedList<double>(8);
         yVec1[0] = 0.5; yVec1[1] = 0.7; yVec1[2] = 1.0; yVec1[3] = 2.0;
         yVec1[4] = 3.5; yVec1[5] = 4.5; yVec1[6] = 5.5; yVec1[7] = 6.5;

         Matrix M1 = new Matrix(xVec1.Count, yVec1.Count);
         M1[0, 0] = 10.25; M1[1, 0] = 12.24; M1[2, 0] = 14.23; M1[3, 0] = 17.20;
         M1[0, 1] = 12.25; M1[1, 1] = 15.24; M1[2, 1] = 16.23; M1[3, 1] = 16.20;
         M1[0, 2] = 12.25; M1[1, 2] = 13.24; M1[2, 2] = 13.23; M1[3, 2] = 17.20;
         M1[0, 3] = 13.25; M1[1, 3] = 15.24; M1[2, 3] = 12.23; M1[3, 3] = 19.20;
         M1[0, 4] = 14.25; M1[1, 4] = 16.24; M1[2, 4] = 13.23; M1[3, 4] = 12.20;
         M1[0, 5] = 15.25; M1[1, 5] = 17.24; M1[2, 5] = 14.23; M1[3, 5] = 12.20;
         M1[0, 6] = 16.25; M1[1, 6] = 13.24; M1[2, 6] = 15.23; M1[3, 6] = 10.20;
         M1[0, 7] = 14.25; M1[1, 7] = 14.24; M1[2, 7] = 16.23; M1[3, 7] = 19.20;

         // test with another kernel
         KernelInterpolation2D kernel2DEp = new KernelInterpolation2D(xVec1, xVec1.Count, yVec1, yVec1.Count, M1, new epanechnikovKernel());
         for (int i = 0; i < M1.rows(); ++i)
         {
            for (int j = 0; j < M1.columns(); ++j)
            {

               calcVal = kernel2DEp.value(xVec1[i], yVec1[j]);
               expectedVal = M1[i, j];

               if (Math.Abs(expectedVal - calcVal) > tolerance)
               {

                  QAssert.Fail("2D Epanechnkikov Kernel interpolation failed at x = " + xVec1[i]
                               + ", y = " + yVec1[j]
                               + "\n    interpolated value: " + calcVal
                               + "\n    expected value:     " + expectedVal
                               + "\n    error:              "
                               + Math.Abs(expectedVal - calcVal));
               }
            }
         }

         // test updating mechanism by changing initial variables
         xVec1[0] = 60.0; xVec1[1] = 95.0; xVec1[2] = 105.0; xVec1[3] = 135.0;

         yVec1[0] = 12.5; yVec1[1] = 13.7; yVec1[2] = 15.0; yVec1[3] = 19.0;
         yVec1[4] = 26.5; yVec1[5] = 27.5; yVec1[6] = 29.2; yVec1[7] = 36.5;

         kernel2DEp.update();

         for (int i = 0; i < M1.rows(); ++i)
         {
            for (int j = 0; j < M1.columns(); ++j)
            {

               calcVal = kernel2DEp.value(xVec1[i], yVec1[j]);
               expectedVal = M1[i, j];

               if (Math.Abs(expectedVal - calcVal) > tolerance)
               {

                  QAssert.Fail("2D Epanechnkikov Kernel updated interpolation failed at x = " + xVec1[i]
                               + ", y = " + yVec1[j]
                               + "\n    interpolated value: " + calcVal
                               + "\n    expected value:     " + expectedVal
                               + "\n    error:              "
                               + Math.Abs(expectedVal - calcVal));
               }
            }
         }
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testBicubicDerivatives()
      {
         // Testing bicubic spline derivatives...

         List<double> x = new InitializedList<double>(100), y = new InitializedList<double>(100);
         for (int i = 0; i < 100; ++i)
         {
            x[i] = y[i] = i / 20.0;
         }

         Matrix f = new Matrix(100, 100);
         for (int i = 0; i < 100; ++i)
            for (int j = 0; j < 100; ++j)
               f[i, j] = y[i] / 10 * Math.Sin(x[j]) + Math.Cos(y[i]);

         double tol = 0.005;
         BicubicSpline spline = new BicubicSpline(x, x.Count, y, y.Count, f);

         for (int i = 5; i < 95; i += 10)
         {
            for (int j = 5; j < 95; j += 10)
            {
               double f_x  = spline.derivativeX(x[j], y[i]);
               double f_xx = spline.secondDerivativeX(x[j], y[i]);
               double f_y  = spline.derivativeY(x[j], y[i]);
               double f_yy = spline.secondDerivativeY(x[j], y[i]);
               double f_xy = spline.derivativeXY(x[j], y[i]);

               if (Math.Abs(f_x - y[i] / 10 * Math.Cos(x[j])) > tol)
               {
                  QAssert.Fail("Failed to reproduce f_x");
               }
               if (Math.Abs(f_xx + y[i] / 10 * Math.Sin(x[j])) > tol)
               {
                  QAssert.Fail("Failed to reproduce f_xx");
               }
               if (Math.Abs(f_y - (Math.Sin(x[j]) / 10 - Math.Sin(y[i]))) > tol)
               {
                  QAssert.Fail("Failed to reproduce f_y");
               }
               if (Math.Abs(f_yy + Math.Cos(y[i])) > tol)
               {
                  QAssert.Fail("Failed to reproduce f_yy");
               }
               if (Math.Abs(f_xy - Math.Cos(x[j]) / 10) > tol)
               {
                  QAssert.Fail("Failed to reproduce f_xy");
               }
            }
         }
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testBicubicUpdate()
      {
         // Testing that bicubic splines actually update...
         int N = 6;
         List<double> x = new InitializedList<double>(N), y = new InitializedList<double>(N);
         for (int i = 0; i < N; ++i)
         {
            x[i] = y[i] = i * 0.2;
         }

         Matrix f = new Matrix(N, N);
         for (int i = 0; i < N; ++i)
            for (int j = 0; j < N; ++j)
               f[i, j] = x[j] * (x[j] + y[i]);

         BicubicSpline spline = new BicubicSpline(x, x.Count, y, y.Count, f);

         double old_result = spline.value(x[2] + 0.1, y[4]);

         // modify input matrix and update.
         f[4, 3] += 1.0;
         spline.update();

         double new_result = spline.value(x[2] + 0.1, y[4]);
         if (Math.Abs(old_result - new_result) < 0.5)
            QAssert.Fail("Failed to update bicubic spline");
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testRichardsonExtrapolation()
      {
         // Testing Richardson extrapolation...

         /* example taken from
         * http://www.ipvs.uni-stuttgart.de/abteilungen/bv/lehre/
         *      lehrveranstaltungen/vorlesungen/WS0910/
         *      NSG_termine/dateien/Richardson.pdf
         */

         double stepSize = 0.1;
         double orderOfConvergence = 1.0;

         RichardsonExtrapolation.function f = delegate(double h)
         {
            return Math.Pow(1.0 + h, 1 / h);
         };

         RichardsonExtrapolation extrap = new RichardsonExtrapolation(f, stepSize, orderOfConvergence);

         double tol = 0.00002;
         double expected = 2.71285;

         double scalingFactor = 2.0;
         double calculated = extrap.value(scalingFactor);

         if (Math.Abs(expected - calculated) > tol)
         {
            QAssert.Fail("failed to reproduce Richardson extrapolation");
         }

         calculated = extrap.value();
         if (Math.Abs(expected - calculated) > tol)
         {
            QAssert.Fail("failed to reproduce Richardson extrapolation");
         }

         expected = 2.721376;
         const double scalingFactor2 = 4.0;
         calculated = extrap.value(scalingFactor2, scalingFactor);

         if (Math.Abs(expected - calculated) > tol)
         {
            QAssert.Fail("failed to reproduce Richardson extrapolation");
         }
      }


#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testSabrSingleCases()
      {
         // Testing Sabr calibration single cases...

         List<double> strikes = new List<double>() { 0.01, 0.01125, 0.0125, 0.01375, 0.0150};
         List<double> vols = new List<double>() {0.1667, 0.2020, 0.2785, 0.3279, 0.3727};


         double tte = 0.3833;
         double forward = 0.011025;

         SABRInterpolation s0 = new SABRInterpolation(strikes, strikes.Count, vols, tte, forward,
                                                      null, 0.25, null, null,
                                                      false, true, false, false);
         s0.update();

         if (s0.maxError() > 0.01 || s0.rmsError() > 0.01)
         {
            QAssert.Fail("Sabr case #1 failed with max error ("
                         + s0.maxError() + ") and rms error (" + s0.rmsError()
                         + "), both should be < 0.01");

         }
      }


      #region Functions
      List<double> xRange(double start, double finish, int points)
      {
         List<double> x = new InitializedList<double>(points);
         double dx = (finish - start) / (points - 1);
         for (int i = 0; i < points - 1; i++)
            x[i] = start + i * dx;
         x[points - 1] = finish;
         return x;
      }

      List<double> gaussian(List<double> x)
      {
         List<double> y = new InitializedList<double>(x.Count);
         for (int i = 0; i < x.Count; i++)
            y[i] = Math.Exp(-x[i] * x[i]);
         return y;
      }

      List<double> parabolic(List<double> x)
      {
         List<double> y = new InitializedList<double>(x.Count);
         for (int i = 0; i < x.Count; i++)
            y[i] = -x[i] * x[i];
         return y;
      }

      void checkValues(string type, CubicInterpolation cubic, List<double> xBegin, List<double> yBegin)
      {
         double tolerance = 2.0e-15;
         for (int i = 0; i < xBegin.Count; i++)
         {
            double interpolated = cubic.value(xBegin[i]);
            if (Math.Abs(interpolated - yBegin[i]) > tolerance)
            {
               QAssert.Fail(type + " interpolation failed at x = " + xBegin[i]
                            + "\n    interpolated value: " + interpolated
                            + "\n    expected value:     " + yBegin[i]
                            + "\n    error:              "
                            + Math.Abs(interpolated - yBegin[i]));
            }
         }
      }

      void check1stDerivativeValue(string type, CubicInterpolation cubic, double x, double value)
      {
         double tolerance = 1.0e-14;
         double interpolated = cubic.derivative(x);
         double error = Math.Abs(interpolated - value);
         if (error > tolerance)
         {
            QAssert.Fail(type + " interpolation first derivative failure\n"
                         + "at x = " + x
                         + "\n    interpolated value: " + interpolated
                         + "\n    expected value:     " + value
                         + "\n    error:              " + error);
         }
      }

      void check2ndDerivativeValue(string type, CubicInterpolation cubic, double x, double value)
      {
         double tolerance = 1.0e-13;
         double interpolated = cubic.secondDerivative(x);
         double error = Math.Abs(interpolated - value);
         if (error > tolerance)
         {
            QAssert.Fail(type + " interpolation second derivative failure\n"
                         + "at x = " + x
                         + "\n    interpolated value: " + interpolated
                         + "\n    expected value:     " + value
                         + "\n    error:              " + error);
         }
      }

      void checkNotAKnotCondition(string type, CubicInterpolation cubic)
      {
         double tolerance = 1.0e-14;
         List<double> c = cubic.cCoefficients();
         if (Math.Abs(c[0] - c[1]) > tolerance)
         {
            QAssert.Fail(type + " interpolation failure"
                         + "\n    cubic coefficient of the first"
                         + " polinomial is " + c[0]
                         + "\n    cubic coefficient of the second"
                         + " polinomial is " + c[1]);
         }
         int n = c.Count;
         if (Math.Abs(c[n - 2] - c[n - 1]) > tolerance)
         {
            QAssert.Fail(type + " interpolation failure"
                         + "\n    cubic coefficient of the 2nd to last"
                         + " polinomial is " + c[n - 2]
                         + "\n    cubic coefficient of the last"
                         + " polinomial is " + c[n - 1]);
         }
      }

      void checkSymmetry(string type, CubicInterpolation cubic, double xMin)
      {
         double tolerance = 1.0e-15;
         for (double x = xMin; x < 0.0; x += 0.1)
         {
            double y1 = cubic.value(x), y2 = cubic.value(-x);
            if (Math.Abs(y1 - y2) > tolerance)
            {
               QAssert.Fail(type + " interpolation not symmetric"
                            + "\n    x = " + x
                            + "\n    g(x)  = " + y1
                            + "\n    g(-x) = " + y2
                            + "\n    error:  " + Math.Abs(y1 - y2));
            }
         }
      }

      class errorFunction<F> : IValue where F : IValue
      {
         private IValue f_;

         public errorFunction(IValue f)
         {
            f_ = f;
         }
         public double value(double x)
         {
            double temp = f_.value(x) - Math.Exp(-x * x);
            return temp * temp;
         }
      }

      errorFunction<IValue> make_error_function(IValue f)
      {
         return new errorFunction<IValue>(f);
      }

      double multif(double s, double t, double u, double v, double w)
      {
         return Math.Sqrt(s * Math.Sinh(Math.Log(t)) +
                          Math.Exp(Math.Sin(u) * Math.Sin(3 * v)) +
                          Math.Sinh(Math.Log(v * w)));
      }

      // Note : a better solution will be an anonymous type casted to IKernelFunction
      class epanechnikovKernel : IKernelFunction
      {
         public double value(double u)
         {
            if (Math.Abs(u) <= 1)
            {
               return (3.0 / 4.0) * (1 - u * u);
            }
            else
            {
               return 0.0;
            }
         }
      }


      #endregion

   }
}
