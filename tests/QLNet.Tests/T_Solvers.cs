/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)

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
using Xunit;
using QLNet;

namespace TestSuite
{
   [Collection("QLNet CI Tests")]
   public class T_Solvers
   {
      class Foo : ISolver1d
      {
         public override double value(double x) { return x * x - 1.0; }
         public override double derivative(double x) { return 2.0 * x; }
      }

      internal void test(Solver1D solver, string name)
      {
         double[] accuracy = new double[] { 1.0e-4, 1.0e-6, 1.0e-8 };
         double expected = 1.0;
         for (int i = 0; i < accuracy.Length; i++)
         {
            double root = solver.solve(new Foo(), accuracy[i], 1.5, 0.1);
            if (Math.Abs(root - expected) > accuracy[i])
            {
               QAssert.Fail(name + " solver:\n"
                            + "    expected:   " + expected + "\n"
                            + "    calculated: " + root + "\n"
                            + "    accuracy:   " + accuracy[i]);
            }
            root = solver.solve(new Foo(), accuracy[i], 1.5, 0.0, 1.0);
            if (Math.Abs(root - expected) > accuracy[i])
            {
               QAssert.Fail(name + " solver (bracketed):\n"
                            + "    expected:   " + expected + "\n"
                            + "    calculated: " + root + "\n"
                            + "    accuracy:   " + accuracy[i]);
            }
         }
      }

      [Fact]
      public void testBrent()
      {
         test(new Brent(), "Brent");
      }
      [Fact]
      public void testNewton()
      {
         test(new Newton(), "Newton");
      }
      [Fact]
      public void testFalsePosition()
      {
         test(new FalsePosition(), "FalsePosition");
      }
      [Fact]
      public void testBisection()
      {
         test(new Bisection(), "Bisection");
      }
      [Fact]
      public void testRidder()
      {
         test(new Ridder(), "Ridder");
      }
      [Fact]
      public void testSecant()
      {
         test(new Secant(), "Secant");
      }

      class F1 : ISolver1d
      {
         public override double value(double x) { return x * x - 1.0; }
         public override double derivative(double x) { return 2.0 * x; }
         public override double secondDerivative(double x) { return 2.0; }
      }

      class F2 : ISolver1d
      {
         public override double value(double x) { return 1.0 - x * x; }
         public override double derivative(double x) { return -2.0 * x; }
         public override double secondDerivative(double x) { return -2.0; }
      }

      class F3 : ISolver1d
      {
         public override double value(double x) { return Math.Atan(x - 1.0); }
         public override double derivative(double x) { return 1.0 / (1.0 + (x - 1.0) * (x - 1.0)); }
         public override double secondDerivative(double x)
         {
            double u = x - 1.0;
            return -2.0 * u / ((1.0 + u * u) * (1.0 + u * u));
         }
      }

      // records the last argument the function was evaluated at
      class Probe : ISolver1d
      {
         private readonly double previous_, offset_;
         public double last;

         public Probe(double last, double offset)
         {
            this.last = last;
            previous_ = last;
            offset_ = offset;
         }

         public override double value(double x)
         {
            last = x;
            return previous_ + offset_ - x * x;
         }
         public override double derivative(double x) { return -2.0 * x; }
         public override double secondDerivative(double x) { return -2.0; }
      }

      private void testNotBracketed(Solver1D solver, string name, ISolver1d f, double guess)
      {
         double[] accuracy = { 1.0e-4, 1.0e-6, 1.0e-8 };
         double expected = 1.0;
         foreach (double a in accuracy)
         {
            double root = solver.solve(f, a, guess, 0.1);
            if (Math.Abs(root - expected) > a)
               QAssert.Fail(name + " solver (not bracketed):\n"
                            + "    expected:   " + expected + "\n"
                            + "    calculated: " + root + "\n"
                            + "    accuracy:   " + a);
         }
      }

      private void testBracketed(Solver1D solver, string name, ISolver1d f, double guess)
      {
         double[] accuracy = { 1.0e-4, 1.0e-6, 1.0e-8 };
         double expected = 1.0;
         foreach (double a in accuracy)
         {
            double root = solver.solve(f, a, guess, 0.0, 2.0);
            if (Math.Abs(root - expected) > a)
               QAssert.Fail(name + " solver (bracketed):\n"
                            + "    expected:   " + expected + "\n"
                            + "    calculated: " + root + "\n"
                            + "    accuracy:   " + a);
         }
      }

      private void testLastCallWithRoot(Solver1D solver, string name, bool bracketed, double accuracy)
      {
         double[] mins = { 3.0, 2.25, 1.5, 1.0 };
         double[] maxs = { 7.0, 5.75, 4.5, 3.0 };
         double[] steps = { 0.2, 0.2, 0.1, 0.1 };
         double[] offsets = { 25.0, 11.0, 5.0, 1.0 };
         double[] guesses = { 4.5, 4.5, 2.5, 2.5 };

         // the last argument of each solve is the starting point of the next probe, as in the upstream test
         double argument = 0.0;
         for (int i = 0; i < 4; ++i)
         {
            var probe = new Probe(argument, offsets[i]);
            double result = bracketed
               ? solver.solve(probe, accuracy, guesses[i], mins[i], maxs[i])
               : solver.solve(probe, accuracy, guesses[i], steps[i]);

            // the solver should have called the function with the very same value it's returning
            argument = probe.last;
            double error = Math.Abs(result - argument);
            if (error > 2 * Const.QL_EPSILON)
               QAssert.Fail(name + " solver (" + (bracketed ? "" : "not ") + "bracketed):\n"
                            + "    index:   " + i + "\n"
                            + "    expected:   " + result + "\n"
                            + "    calculated: " + probe.last + "\n"
                            + "    error: " + error);
         }
      }

      // accuracy is null when the solver does not call the function with the returned root
      private void testSolver(Solver1D solver, string name, double? accuracy)
      {
         // guess on the left side of the root, increasing function
         testNotBracketed(solver, name, new F1(), 0.5);
         testBracketed(solver, name, new F1(), 0.5);
         // guess on the right side of the root, increasing function
         testNotBracketed(solver, name, new F1(), 1.5);
         testBracketed(solver, name, new F1(), 1.5);
         // guess on the left side of the root, decreasing function
         testNotBracketed(solver, name, new F2(), 0.5);
         testBracketed(solver, name, new F2(), 0.5);
         // guess on the right side of the root, decreasing function
         testNotBracketed(solver, name, new F2(), 1.5);
         testBracketed(solver, name, new F2(), 1.5);
         // situation where bisection is used in the finite difference newton solver as the first step and where the
         // initial guess is equal to the next estimate (which causes an infinite derivative if we do not handle
         // this case with special care)
         testNotBracketed(solver, name, new F3(), 1.00001);
         // check that the last function call is made with the root value
         if (accuracy.HasValue)
         {
            testLastCallWithRoot(solver, name, false, accuracy.Value);
            testLastCallWithRoot(solver, name, true, accuracy.Value);
         }
      }

      [Fact]
      public void testBrentLastCall() { testSolver(new Brent(), "Brent", 1.0e-6); }

      [Fact]
      public void testBisectionLastCall() { testSolver(new Bisection(), "Bisection", 1.0e-6); }

      [Fact]
      public void testFalsePositionLastCall() { testSolver(new FalsePosition(), "FalsePosition", 1.0e-6); }

      [Fact]
      public void testNewtonLastCall() { testSolver(new Newton(), "Newton", 1.0e-12); }

      [Fact]
      public void testNewtonSafe() { testSolver(new NewtonSafe(), "NewtonSafe", 1.0e-9); }

      [Fact]
      public void testFiniteDifferenceNewtonSafe()
      {
         testSolver(new FiniteDifferenceNewtonSafe(), "FiniteDifferenceNewtonSafe", null);
      }

      [Fact]
      public void testRidderLastCall() { testSolver(new Ridder(), "Ridder", 1.0e-6); }

      [Fact]
      public void testSecantLastCall() { testSolver(new Secant(), "Secant", 1.0e-6); }

      [Fact]
      public void testHalley() { testSolver(new Halley(), "Halley", 1.0e-6); }

      // f(x) = x^3 - 8 has the root 2 and a derivative that vanishes at 0
      class CubicMinusEight : ISolver1d
      {
         public override double value(double x) { return x * x * x - 8.0; }
         public override double derivative(double x) { return 3.0 * x * x; }
         public override double secondDerivative(double x) { return 6.0 * x; }
      }

      // f(x) = tanh(x - 2) has root 2; at 25, tanh(x - 2) rounds to 1,
      // so the computed derivative 1 - tanh(x - 2)^2 is zero.
      class ShiftedTanh : ISolver1d
      {
         public override double value(double x) { return Math.Tanh(x - 2.0); }
         public override double derivative(double x)
         {
            double t = Math.Tanh(x - 2.0);
            return 1.0 - t * t;
         }
         public override double secondDerivative(double x)
         {
            double t = Math.Tanh(x - 2.0);
            return -2.0 * t * (1.0 - t * t);
         }
      }

      // records the arguments of the function calls
      class Recorder : ISolver1d
      {
         private readonly ISolver1d f_;
         public readonly System.Collections.Generic.List<double> arguments = new System.Collections.Generic.List<double>();
         public Recorder(ISolver1d f) { f_ = f; }
         public override double value(double x)
         {
            arguments.Add(x);
            return f_.value(x);
         }
         public override double derivative(double x) { return f_.derivative(x); }
         public override double secondDerivative(double x) { return f_.secondDerivative(x); }
      }

      private void testFallback(Solver1D solver, string name, ISolver1d f,
                                double expected, double guess, double xMin, double xMax)
      {
         double accuracy = 1.0e-10;
         double root = solver.solve(f, accuracy, guess, xMin, xMax);
         if (Math.Abs(root - expected) > accuracy)
            QAssert.Fail(name + " solver, guess " + guess + ":\n"
                         + "    expected:   " + expected + "\n"
                         + "    calculated: " + root + "\n"
                         + "    accuracy:   " + accuracy);
      }

      [Fact]
      public void testNewtonAndHalleyFallback()
      {
         // Unlike QuantLib, where the fallback to NewtonSafe finds the root, QLNet treats a zero derivative
         // as a function that does not implement derivative(): Newton rejects it at the guess and Halley,
         // whose step is then not a finite number, falls back to NewtonSafe, which rejects it as well.
         Assert.Throws<ArgumentException>(() => testFallback(new Newton(), "Newton", new CubicMinusEight(), 2.0, 0.0, -1.0, 5.0));
         Assert.Throws<ArgumentException>(() => testFallback(new Halley(), "Halley", new CubicMinusEight(), 2.0, 0.0, -1.0, 5.0));
         Assert.Throws<ArgumentException>(() => testFallback(new Newton(), "Newton", new ShiftedTanh(), 2.0, 25.0, -20.0, 30.0));
         Assert.Throws<ArgumentException>(() => testFallback(new Halley(), "Halley", new ShiftedTanh(), 2.0, 25.0, -20.0, 30.0));

         // The derivative is tiny, so the step is finite but takes the iterate far
         // out of the bracket. The Newton-safe solver must start again from the
         // guess, not from an iterate reconstructed by adding the step back.
         double guess = 1.0e-9;
         var f = new Recorder(new CubicMinusEight());
         testFallback(new Newton(), "Newton", f, 2.0, guess, -1.0, 5.0);
         // the first call is made by the Newton solver, the second by the other one
         int calls = f.arguments.FindAll(x => x == guess).Count;
         if (calls < 2)
            QAssert.Fail("Newton solver: the guess was passed " + calls + " times, expected at least 2");
      }

      class NanSolver : ISolver1d
      {
         public override double value(double x) { return (x < 1.25) ? double.NaN : x * x - 1.60; }
         public override double derivative(double x) { return 2.0 * x; }
      }

      internal void testNan(Solver1D solver, string name)
      {
         double[] accuracy = new double[] { 1.0e-3, 1.0e-4, 1.0e-6 };
         double expected = 1.2649110986579275;
         for (int i = 0; i < accuracy.Length; i++)
         {
            double root = solver.solve(new NanSolver(), accuracy[i], 1.5, 0.1);
            //double root = solver.solve(new NanSolver(), accuracy[i], 0.5, 0.1);
            if (Math.Abs(root - expected) > accuracy[i])
            {
               QAssert.Fail(name + " solver:\n"
                            + "    expected:   " + expected + "\n"
                            + "    calculated: " + root + "\n"
                            + "    accuracy:   " + accuracy[i]);
            }
            root = solver.solve(new NanSolver(), accuracy[i], 1.5, 1.25, 2.0);
            if (Math.Abs(root - expected) > accuracy[i])
            {
               QAssert.Fail(name + " solver (bracketed):\n"
                            + "    expected:   " + expected + "\n"
                            + "    calculated: " + root + "\n"
                            + "    accuracy:   " + accuracy[i]);
            }
         }
      }

      internal void testNanExceptionByStep(Solver1D solver, string name)
      {
         double accuracy = 1.0e-6;

         try
         {
            double root = solver.solve(new NanSolver(), accuracy, 0.5, 0.1);
         }
         catch (RootNotBracketException)
         {
            return;
         }
         catch (Exception)
         {
            QAssert.Fail("Failed to handle QLNet exception");

            throw;
         }

         QAssert.Fail("Failed to capture QLNet exception");
      }

      internal void testNanExceptionByRange(Solver1D solver, string name)
      {
         double accuracy = 1.0e-6;

         try
         {
            double root = solver.solve(new NanSolver(), accuracy, 0.5, 0.0, 1.0);
         }
         catch (ArgumentException)
         {
            return;
         }
         catch (Exception)
         {
            QAssert.Fail("Failed to handle QLNet exception");

            throw;
         }

         QAssert.Fail("Failed to capture QLNet exception");
      }

      [Fact]
      public void testNanBrent()
      {
         testNan(new Brent(), "Brent");
      }

      [Fact]
      public void testNanExceptionByStepBrent()
      {
         testNanExceptionByStep(new Brent(), "Brent");
      }

      [Fact]
      public void testNanExceptionByRangeBrent()
      {
         testNanExceptionByRange(new Brent(), "Brent");
      }

      [Fact]
      public void testNanNewton()
      {
         testNan(new Newton(), "Newton");
      }

      [Fact]
      public void testNanExceptionByStepNewton()
      {
         testNanExceptionByStep(new Newton(), "Newton");
      }

      [Fact]
      public void testNanExceptionByRangeNewton()
      {
         testNanExceptionByRange(new Newton(), "Newton");
      }
   }
}
