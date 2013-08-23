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

namespace TestSuite {
    [TestClass()]
    public class T_Optimizers {
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

        struct NamedOptimizationMethod {
            public OptimizationMethod optimizationMethod;
            public string name;
        }

        enum OptimizationMethodType {
            simplex,
            levenbergMarquardt,
            conjugateGradient,
            steepestDescent
        }

        [TestMethod()]
        public void OptimizersTest() {
            //("Testing optimizers...");

            setup();

            // Loop over problems (currently there is only 1 problem)
            for (int i=0; i<costFunctions_.Count; ++i) {
                Problem problem = new Problem(costFunctions_[i], constraints_[i], initialValues_[i]);
                Vector initialValues = problem.currentValue();
                // Loop over optimizers
                for (int j = 0; j < (optimizationMethods_[i]).Count; ++j) {
                    double rootEpsilon = endCriterias_[i].rootEpsilon();
                    int endCriteriaTests = 1;
                   // Loop over rootEpsilon
                    for(int k=0; k<endCriteriaTests; ++k) {
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
                        if (endCriteriaResult==EndCriteria.Type.None ||
                            endCriteriaResult==EndCriteria.Type.MaxIterations ||
                            endCriteriaResult==EndCriteria.Type.Unknown)
                            Assert.Fail("function evaluations: " + problem.functionEvaluation()  +
                                      " gradient evaluations: " + problem.gradientEvaluation() +
                                      " x expected:           " + xMinExpected_[i] +
                                      " x calculated:         " + xMinCalculated +
                                      " x difference:         " + (xMinExpected_[i]- xMinCalculated) +
                                      " rootEpsilon:          " + endCriteria.rootEpsilon() +
                                      " y expected:           " + yMinExpected_[i] +
                                      " y calculated:         " + yMinCalculated +
                                      " y difference:         " + (yMinExpected_[i]- yMinCalculated) +
                                      " functionEpsilon:      " + endCriteria.functionEpsilon() +
                                      " endCriteriaResult:    " + endCriteriaResult);
                    }
                }
            }
        }

        [TestMethod()]
        public void nestedOptimizationTest() {
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


        // Set up, for each cost function, all the ingredients for optimization:
        // constraint, initial guess, end criteria, optimization methods.
        void setup() {

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
            OptimizationMethodType[] optimizationMethodTypes = {
                OptimizationMethodType.simplex, 
                OptimizationMethodType.levenbergMarquardt, 
                OptimizationMethodType.conjugateGradient/*, steepestDescent*/};

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
            xMinExpected[0] = -b/(2.0*a);
            yMinExpected[0] = -(b*b-4.0*a*c)/(4.0*a);
            xMinExpected_.Add(xMinExpected);
            yMinExpected_.Add(yMinExpected);
        }


        OptimizationMethod makeOptimizationMethod(OptimizationMethodType optimizationMethodType,
                                                double simplexLambda,
                                                double levenbergMarquardtEpsfcn,
                                                double levenbergMarquardtXtol,
                                                double levenbergMarquardtGtol) {
            switch (optimizationMethodType) {
                case OptimizationMethodType.simplex:
                    return new Simplex(simplexLambda);
                case OptimizationMethodType.levenbergMarquardt:
                    return new LevenbergMarquardt(levenbergMarquardtEpsfcn, levenbergMarquardtXtol, levenbergMarquardtGtol);
                case OptimizationMethodType.conjugateGradient:
                    return new ConjugateGradient();
                case OptimizationMethodType.steepestDescent:
                    return new SteepestDescent();
                default:
                    throw new ApplicationException("unknown OptimizationMethod type");
            }
        }      

        List<NamedOptimizationMethod> makeOptimizationMethods(OptimizationMethodType[] optimizationMethodTypes,
                                                             int optimizationMethodNb,
                                                             double simplexLambda,
                                                             double levenbergMarquardtEpsfcn,
                                                             double levenbergMarquardtXtol,
                                                             double levenbergMarquardtGtol) {
            List<NamedOptimizationMethod> results = new List<NamedOptimizationMethod>(optimizationMethodNb);
            for (int i = 0; i < optimizationMethodNb; ++i) {
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

        string optimizationMethodTypeToString(OptimizationMethodType type) {
            switch (type) {
                case OptimizationMethodType.simplex:
                    return "Simplex";
                case OptimizationMethodType.levenbergMarquardt:
                    return "Levenberg Marquardt";
                case OptimizationMethodType.conjugateGradient:
                    return "Conjugate Gradient";
                case OptimizationMethodType.steepestDescent:
                    return "Steepest Descent";
                default:
                    throw new ApplicationException("unknown OptimizationMethod type");
            }
        }
    }

    class OneDimensionalPolynomialDegreeN : CostFunction {
        private Vector coefficients_;
        private int polynomialDegree_;

        public OneDimensionalPolynomialDegreeN(Vector coefficients) {
            coefficients_ = new Vector(coefficients);
            polynomialDegree_ = coefficients.size()-1;
        }

        public override double value(Vector x) {
            if(x.size()!=1) throw new ApplicationException("independent variable must be 1 dimensional");
            double y = 0;
            for (int i=0; i<=polynomialDegree_; ++i)
                y += coefficients_[i]*Utils.Pow(x[0],i);
            return y;
        }

        public override Vector values(Vector x) {
            if(x.size()!=1) throw new ApplicationException("independent variable must be 1 dimensional");
            Vector y = new Vector(1);
            y[0] = value(x);
            return y;
        }
    }

    // The goal of this cost function is simply to call another optimization inside
    // in order to test nested optimizations
    class OptimizationBasedCostFunction : CostFunction {
        public override double value(Vector x) { return 1.0; }

        public override Vector values(Vector x) {
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
            Vector dummy = new Vector(1,0);
            return dummy;
        }
    }


}
