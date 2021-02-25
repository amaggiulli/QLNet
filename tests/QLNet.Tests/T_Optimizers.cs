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
   public class T_Optimizers
   {
      List<CostFunction> costFunctions_ = new List<CostFunction>();
      List<Constraint> constraints_ = new List<Constraint>();
      List<Vector> initialValues_ = new List<Vector>();
      List<int> maxIterations_ = new List<int>(), maxStationaryStateIterations_ = new List<int>();
      List<double> rootEpsilons_ = new List<double>(),
      functionEpsilons_ = new List<double>(),
      gradientNormEpsilons_ = new List<double>();
      List<EndCriteria> endCriterias_ = new List<EndCriteria>();
      List<List<NamedOptimizationMethod>> optimizationMethods_ = new List<List<NamedOptimizationMethod>>();
      List<Vector> xMinExpected_ = new List<Vector>(), yMinExpected_ = new List<Vector>();

      struct NamedOptimizationMethod
      {
         public OptimizationMethod optimizationMethod;
         public string name;
      }

      enum OptimizationMethodType
      {
         simplex,
         levenbergMarquardt,
         levenbergMarquardt2,
         conjugateGradient,
         conjugateGradient_goldstein,
         steepestDescent,
         steepestDescent_goldstein,
         bfgs,
         bfgs_goldstein
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void OptimizersTest()
      {
         //("Testing optimizers...");

         setup();

         // Loop over problems (currently there is only 1 problem)
         for (int i = 0; i < costFunctions_.Count; ++i)
         {
            Problem problem = new Problem(costFunctions_[i], constraints_[i], initialValues_[i]);
            Vector initialValues = problem.currentValue();
            // Loop over optimizers
            for (int j = 0; j < (optimizationMethods_[i]).Count; ++j)
            {
               double rootEpsilon = endCriterias_[i].rootEpsilon();
               int endCriteriaTests = 1;
               // Loop over rootEpsilon
               for (int k = 0; k < endCriteriaTests; ++k)
               {
                  problem.setCurrentValue(initialValues);
                  EndCriteria endCriteria = new EndCriteria(endCriterias_[i].maxIterations(),
                                                            endCriterias_[i].maxStationaryStateIterations(),
                                                            rootEpsilon,
                                                            endCriterias_[i].functionEpsilon(),
                                                            endCriterias_[i].gradientNormEpsilon());
                  rootEpsilon *= .1;
                  EndCriteria.Type endCriteriaResult =
                     optimizationMethods_[i][j].optimizationMethod.minimize(problem, endCriteria);
                  Vector xMinCalculated = problem.currentValue();
                  Vector yMinCalculated = problem.values(xMinCalculated);
                  // Check optimization results vs known solution
                  if (endCriteriaResult == EndCriteria.Type.None ||
                      endCriteriaResult == EndCriteria.Type.MaxIterations ||
                      endCriteriaResult == EndCriteria.Type.Unknown)
                     QAssert.Fail("function evaluations: " + problem.functionEvaluation()  +
                                  " gradient evaluations: " + problem.gradientEvaluation() +
                                  " x expected:           " + xMinExpected_[i] +
                                  " x calculated:         " + xMinCalculated +
                                  " x difference:         " + (xMinExpected_[i] - xMinCalculated) +
                                  " rootEpsilon:          " + endCriteria.rootEpsilon() +
                                  " y expected:           " + yMinExpected_[i] +
                                  " y calculated:         " + yMinCalculated +
                                  " y difference:         " + (yMinExpected_[i] - yMinCalculated) +
                                  " functionEpsilon:      " + endCriteria.functionEpsilon() +
                                  " endCriteriaResult:    " + endCriteriaResult);
               }
            }
         }
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void nestedOptimizationTest()
      {
         //("Testing nested optimizations...");
         OptimizationBasedCostFunction optimizationBasedCostFunction = new OptimizationBasedCostFunction();
         NoConstraint constraint = new NoConstraint();
         Vector initialValues = new Vector(1, 0.0);
         Problem problem = new Problem(optimizationBasedCostFunction, constraint, initialValues);
         LevenbergMarquardt optimizationMethod = new LevenbergMarquardt();
         //Simplex optimizationMethod(0.1);
         //ConjugateGradient optimizationMethod;
         //SteepestDescent optimizationMethod;
         EndCriteria endCriteria = new EndCriteria(1000, 100, 1e-5, 1e-5, 1e-5);
         optimizationMethod.minimize(problem, endCriteria);
      }

#if NET452
      [TestMethod()]
#else
      [Fact]
#endif
      public void testDifferentialEvolution()
      {
         //BOOST_TEST_MESSAGE("Testing differential evolution...");

         /* Note:
         *
         * The "ModFourthDeJong" doesn't have a well defined optimum because
         * of its noisy part. It just has to be <= 15 in our example.
         * The concrete value might differ for a different input and
         * different random numbers.
         *
         * The "Griewangk" function is an example where the adaptive
         * version of DifferentialEvolution turns out to be more successful.
         */

         DifferentialEvolution.Configuration conf =
            new DifferentialEvolution.Configuration()
         .withStepsizeWeight(0.4)
         .withBounds()
         .withCrossoverProbability(0.35)
         .withPopulationMembers(500)
         .withStrategy(DifferentialEvolution.Strategy.BestMemberWithJitter)
         .withCrossoverType(DifferentialEvolution.CrossoverType.Normal)
         .withAdaptiveCrossover()
         .withSeed(3242);

         DifferentialEvolution.Configuration conf2 =
            new DifferentialEvolution.Configuration()
         .withStepsizeWeight(1.8)
         .withBounds()
         .withCrossoverProbability(0.9)
         .withPopulationMembers(1000)
         .withStrategy(DifferentialEvolution.Strategy.Rand1SelfadaptiveWithRotation)
         .withCrossoverType(DifferentialEvolution.CrossoverType.Normal)
         .withAdaptiveCrossover()
         .withSeed(3242);
         DifferentialEvolution deOptim2 = new DifferentialEvolution(conf2);

         List<DifferentialEvolution> diffEvolOptimisers = new List<DifferentialEvolution>();
         diffEvolOptimisers.Add(new DifferentialEvolution(conf));
         diffEvolOptimisers.Add(new DifferentialEvolution(conf));
         diffEvolOptimisers.Add(new DifferentialEvolution(conf));
         diffEvolOptimisers.Add(new DifferentialEvolution(conf));
         diffEvolOptimisers.Add(deOptim2);

         List<CostFunction> costFunctions = new List<CostFunction>();
         costFunctions.Add(new FirstDeJong());
         costFunctions.Add(new SecondDeJong());
         costFunctions.Add(new ModThirdDeJong());
         costFunctions.Add(new ModFourthDeJong());
         costFunctions.Add(new Griewangk());

         List<BoundaryConstraint> constraints = new List<BoundaryConstraint>();
         constraints.Add(new BoundaryConstraint(-10.0, 10.0));
         constraints.Add(new BoundaryConstraint(-10.0, 10.0));
         constraints.Add(new BoundaryConstraint(-10.0, 10.0));
         constraints.Add(new BoundaryConstraint(-10.0, 10.0));
         constraints.Add(new BoundaryConstraint(-600.0, 600.0));

         List<Vector> initialValues = new List<Vector>();
         initialValues.Add(new Vector(3, 5.0));
         initialValues.Add(new Vector(2, 5.0));
         initialValues.Add(new Vector(5, 5.0));
         initialValues.Add(new Vector(30, 5.0));
         initialValues.Add(new Vector(10, 100.0));

         List<EndCriteria> endCriteria = new List<EndCriteria>();
         endCriteria.Add(new EndCriteria(100, 10, 1e-10, 1e-8, null));
         endCriteria.Add(new EndCriteria(100, 10, 1e-10, 1e-8, null));
         endCriteria.Add(new EndCriteria(100, 10, 1e-10, 1e-8, null));
         endCriteria.Add(new EndCriteria(500, 100, 1e-10, 1e-8, null));
         endCriteria.Add(new EndCriteria(1000, 800, 1e-12, 1e-10, null));

         List<double> minima = new List<double>();
         minima.Add(0.0);
         minima.Add(0.0);
         minima.Add(0.0);
         minima.Add(10.9639796558);
         minima.Add(0.0);

         for (int i = 0; i < costFunctions.Count; ++i)
         {
            Problem problem = new Problem(costFunctions[i], constraints[i], initialValues[i]);
            diffEvolOptimisers[i].minimize(problem, endCriteria[i]);

            if (i != 3)
            {
               // stable
               if (Math.Abs(problem.functionValue() - minima[i]) > 1e-8)
               {
                  QAssert.Fail("costFunction # " + i
                               + "\ncalculated: " + problem.functionValue()
                               + "\nexpected:   " + minima[i]);
               }
            }
            else
            {
               // this case is unstable due to randomness; we're good as
               // long as the result is below 15
               if (problem.functionValue() > 15)
               {
                  QAssert.Fail("costFunction # " + i
                               + "\ncalculated: " + problem.functionValue()
                               + "\nexpected:   " + "less than 15");
               }
            }
         }
      }


      // Set up, for each cost function, all the ingredients for optimization:
      // constraint, initial guess, end criteria, optimization methods.
      void setup()
      {

         // Cost function n. 1: 1D polynomial of degree 2 (parabolic function y=a*x^2+b*x+c)
         const double a = 1;   // required a > 0
         const double b = 1;
         const double c = 1;
         Vector coefficients = new Vector() { c, b, a };

         costFunctions_.Add(new OneDimensionalPolynomialDegreeN(coefficients));
         // Set constraint for optimizers: unconstrained problem
         constraints_.Add(new NoConstraint());
         // Set initial guess for optimizer
         Vector initialValue = new Vector(1);
         initialValue[0] = -100;
         initialValues_.Add(initialValue);
         // Set end criteria for optimizer
         maxIterations_.Add(10000);                // maxIterations
         maxStationaryStateIterations_.Add(100);   // MaxStationaryStateIterations
         rootEpsilons_.Add(1e-8);                  // rootEpsilon
         functionEpsilons_.Add(1e-8);              // functionEpsilon
         gradientNormEpsilons_.Add(1e-8);          // gradientNormEpsilon
         endCriterias_.Add(new EndCriteria(maxIterations_.Last(), maxStationaryStateIterations_.Last(),
                                           rootEpsilons_.Last(), functionEpsilons_.Last(),
                                           gradientNormEpsilons_.Last()));

         // Set optimization methods for optimizer
         OptimizationMethodType[] optimizationMethodTypes =
         {
            OptimizationMethodType.simplex,
            OptimizationMethodType.levenbergMarquardt,
            OptimizationMethodType.levenbergMarquardt2,
            OptimizationMethodType.conjugateGradient/*, steepestDescent*/,
            OptimizationMethodType.conjugateGradient_goldstein,
            OptimizationMethodType.steepestDescent_goldstein,
            OptimizationMethodType.bfgs,
            OptimizationMethodType.bfgs_goldstein
         };

         double simplexLambda = 0.1;                   // characteristic search length for simplex
         double levenbergMarquardtEpsfcn = 1.0e-8;     // parameters specific for Levenberg-Marquardt
         double levenbergMarquardtXtol   = 1.0e-8;     //
         double levenbergMarquardtGtol   = 1.0e-8;     //
         optimizationMethods_.Add(makeOptimizationMethods(
                                     optimizationMethodTypes, optimizationMethodTypes.Length,
                                     simplexLambda, levenbergMarquardtEpsfcn, levenbergMarquardtXtol,
                                     levenbergMarquardtGtol));
         // Set expected results for optimizer
         Vector xMinExpected = new Vector(1), yMinExpected = new Vector(1);
         xMinExpected[0] = -b / (2.0 * a);
         yMinExpected[0] = -(b * b - 4.0 * a * c) / (4.0 * a);
         xMinExpected_.Add(xMinExpected);
         yMinExpected_.Add(yMinExpected);
      }


      OptimizationMethod makeOptimizationMethod(OptimizationMethodType optimizationMethodType,
                                                double simplexLambda,
                                                double levenbergMarquardtEpsfcn,
                                                double levenbergMarquardtXtol,
                                                double levenbergMarquardtGtol)
      {
         switch (optimizationMethodType)
         {
            case OptimizationMethodType.simplex:
               return new Simplex(simplexLambda);
            case OptimizationMethodType.levenbergMarquardt:
               return new LevenbergMarquardt(levenbergMarquardtEpsfcn, levenbergMarquardtXtol, levenbergMarquardtGtol);
            case OptimizationMethodType.levenbergMarquardt2:
               return new LevenbergMarquardt(levenbergMarquardtEpsfcn, levenbergMarquardtXtol, levenbergMarquardtGtol, true);
            case OptimizationMethodType.conjugateGradient:
               return new ConjugateGradient();
            case OptimizationMethodType.steepestDescent:
               return new SteepestDescent();
            case OptimizationMethodType.bfgs:
               return new BFGS();
            case OptimizationMethodType.conjugateGradient_goldstein:
               return new ConjugateGradient(new GoldsteinLineSearch());
            case OptimizationMethodType.steepestDescent_goldstein:
               return new SteepestDescent(new GoldsteinLineSearch());
            case OptimizationMethodType.bfgs_goldstein:
               return new BFGS(new GoldsteinLineSearch());
            default:
               throw new Exception("unknown OptimizationMethod type");
         }
      }

      List<NamedOptimizationMethod> makeOptimizationMethods(OptimizationMethodType[] optimizationMethodTypes,
                                                            int optimizationMethodNb,
                                                            double simplexLambda,
                                                            double levenbergMarquardtEpsfcn,
                                                            double levenbergMarquardtXtol,
                                                            double levenbergMarquardtGtol)
      {
         List<NamedOptimizationMethod> results = new List<NamedOptimizationMethod>(optimizationMethodNb);
         for (int i = 0; i < optimizationMethodNb; ++i)
         {
            NamedOptimizationMethod namedOptimizationMethod;
            namedOptimizationMethod.optimizationMethod = makeOptimizationMethod(optimizationMethodTypes[i],
                                                                                simplexLambda,
                                                                                levenbergMarquardtEpsfcn,
                                                                                levenbergMarquardtXtol,
                                                                                levenbergMarquardtGtol);
            namedOptimizationMethod.name = optimizationMethodTypeToString(optimizationMethodTypes[i]);
            results.Add(namedOptimizationMethod);
         }
         return results;
      }

      string optimizationMethodTypeToString(OptimizationMethodType type)
      {
         switch (type)
         {
            case OptimizationMethodType.simplex:
               return "Simplex";
            case OptimizationMethodType.levenbergMarquardt:
               return "Levenberg Marquardt";
            case OptimizationMethodType.levenbergMarquardt2:
               return "Levenberg Marquardt (cost function's jacbobian)";
            case OptimizationMethodType.conjugateGradient:
               return "Conjugate Gradient";
            case OptimizationMethodType.steepestDescent:
               return "Steepest Descent";
            case OptimizationMethodType.bfgs:
               return "BFGS";
            case OptimizationMethodType.conjugateGradient_goldstein:
               return "Conjugate Gradient (Goldstein line search)";
            case OptimizationMethodType.steepestDescent_goldstein:
               return "Steepest Descent (Goldstein line search)";
            case OptimizationMethodType.bfgs_goldstein:
               return "BFGS (Goldstein line search)";
            default:
               throw new Exception("unknown OptimizationMethod type");
         }
      }
   }

   class OneDimensionalPolynomialDegreeN : CostFunction
   {
      private Vector coefficients_;
      private int polynomialDegree_;

      public OneDimensionalPolynomialDegreeN(Vector coefficients)
      {
         coefficients_ = new Vector(coefficients);
         polynomialDegree_ = coefficients.size() - 1;
      }

      public override double value(Vector x)
      {
         if (x.size() != 1)
            throw new Exception("independent variable must be 1 dimensional");
         double y = 0;
         for (int i = 0; i <= polynomialDegree_; ++i)
            y += coefficients_[i] * Utils.Pow(x[0], i);
         return y;
      }

      public override Vector values(Vector x)
      {
         if (x.size() != 1)
            throw new Exception("independent variable must be 1 dimensional");
         Vector y = new Vector(1);
         y[0] = value(x);
         return y;
      }
   }

   // The goal of this cost function is simply to call another optimization inside
   // in order to test nested optimizations
   class OptimizationBasedCostFunction : CostFunction
   {
      public override double value(Vector x) { return 1.0; }

      public override Vector values(Vector x)
      {
         // dummy nested optimization
         Vector coefficients = new Vector(3, 1.0);
         OneDimensionalPolynomialDegreeN oneDimensionalPolynomialDegreeN = new OneDimensionalPolynomialDegreeN(coefficients);
         NoConstraint constraint = new NoConstraint();
         Vector initialValues = new Vector(1, 100.0);
         Problem problem = new Problem(oneDimensionalPolynomialDegreeN, constraint, initialValues);
         LevenbergMarquardt optimizationMethod = new LevenbergMarquardt();
         //Simplex optimizationMethod(0.1);
         //ConjugateGradient optimizationMethod;
         //SteepestDescent optimizationMethod;
         EndCriteria endCriteria = new EndCriteria(1000, 100, 1e-5, 1e-5, 1e-5);
         optimizationMethod.minimize(problem, endCriteria);
         // return dummy result
         Vector dummy = new Vector(1, 0);
         return dummy;
      }
   }

   class FirstDeJong : CostFunction
   {
      public override Vector values(Vector x)
      {
         Vector retVal = new Vector(x.size(), value(x));
         return retVal;
      }
      public override double value(Vector x)
      {
         return Vector.DotProduct(x, x);
      }
   }

   class SecondDeJong : CostFunction
   {
      public override Vector values(Vector x)
      {
         Vector retVal = new Vector(x.size(), value(x));
         return retVal;
      }
      public override double value(Vector x)
      {
         return  100.0 * (x[0] * x[0] - x[1]) * (x[0] * x[0] - x[1])
                 + (1.0 - x[0]) * (1.0 - x[0]);
      }
   }

   class ModThirdDeJong : CostFunction
   {
      public override Vector values(Vector x)
      {
         Vector retVal = new Vector(x.size(), value(x));
         return retVal;
      }
      public override double value(Vector x)
      {
         double fx = 0.0;
         for (int i = 0; i < x.size(); ++i)
         {
            fx += Math.Floor(x[i]) * Math.Floor(x[i]);
         }
         return fx;
      }
   }

   class ModFourthDeJong : CostFunction
   {
      public ModFourthDeJong()
      {
         uniformRng_ = new MersenneTwisterUniformRng(4711);
      }

      public override Vector values(Vector x)
      {
         Vector retVal = new Vector(x.size(), value(x));
         return retVal;
      }
      public override double value(Vector x)
      {
         double fx = 0.0;
         for (int i = 0; i < x.size(); ++i)
         {
            fx += (i + 1.0) * Math.Pow(x[i], 4.0) + uniformRng_.nextReal();
         }
         return fx;
      }
      MersenneTwisterUniformRng uniformRng_;
   }

   class Griewangk : CostFunction
   {
      public override Vector values(Vector x)
      {
         Vector retVal = new Vector(x.size(), value(x));
         return retVal;
      }
      public override double value(Vector x)
      {
         double fx = 0.0;
         for (int i = 0; i < x.size(); ++i)
         {
            fx += x[i] * x[i] / 4000.0;
         }
         double p = 1.0;
         for (int i = 0; i < x.size(); ++i)
         {
            p *= Math.Cos(x[i] / Math.Sqrt(i + 1.0));
         }
         return fx - p + 1.0;
      }
   }
}
