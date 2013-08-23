/*
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
 * 
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

namespace QLNet
{
    public static partial class Utils {
        // Computes the size of the simplex
        public static double computeSimplexSize(InitializedList<Vector> vertices)
        {
            Vector center = new Vector(vertices[0].Count, 0);
            for (int i = 0; i < vertices.Count; ++i)
                center += vertices[i];
            center *= 1 / (double)(vertices.Count);
            double result = 0;
            for (int i = 0; i < vertices.Count; ++i)
            {
                Vector temp = vertices[i] - center;
                result += Math.Sqrt(Vector.DotProduct(temp, temp));
            }
            return result / (double)(vertices.Count);
        }

    }

    //! Multi-dimensional simplex class
    public class Simplex : OptimizationMethod
    {
        //! Constructor taking as input the characteristic length 
        public Simplex(double lambda)
        {
            lambda_ = lambda;
        }
        public override EndCriteria.Type minimize(Problem P, EndCriteria endCriteria)
        {
            // set up of the problem
            //double ftol = endCriteria.functionEpsilon();    // end criteria on f(x) (see Numerical Recipes in C++, p.410)
            double xtol = endCriteria.rootEpsilon(); // end criteria on x (see GSL v. 1.9, http://www.gnu.org/software/gsl/)
            int maxStationaryStateIterations_ = endCriteria.maxStationaryStateIterations();
            EndCriteria.Type ecType = EndCriteria.Type.None;
            P.reset();
            Vector x_ = P.currentValue();
            int iterationNumber_ = 0;

            // Initialize vertices of the simplex
            bool end = false;
            int n = x_.Count;
            vertices_ = new InitializedList<Vector>(n + 1, x_);
            for (int i = 0; i < n; i++)
            {
                Vector direction = new Vector(n, 0.0);
                direction[i] = 1.0;
                P.constraint().update(vertices_[i + 1], direction, lambda_);
            }
            // Initialize function values at the vertices of the simplex
            values_ = new Vector(n + 1, 0.0);
            for (int i = 0; i <= n; i++)
                values_[i] = P.value(vertices_[i]);
            // Loop looking for minimum
            do
            {
                sum_ = new Vector(n, 0.0);
                for (int i = 0; i <= n; i++)
                    sum_ += vertices_[i];
                // Determine the best (iLowest), worst (iHighest)
                // and 2nd worst (iNextHighest) vertices
                int iLowest = 0;
                int iHighest;
                int iNextHighest;
                if (values_[0] < values_[1])
                {
                    iHighest = 1;
                    iNextHighest = 0;
                }
                else
                {
                    iHighest = 0;
                    iNextHighest = 1;
                }
                for (int i = 1; i <= n; i++)
                {
                    if (values_[i] > values_[iHighest])
                    {
                        iNextHighest = iHighest;
                        iHighest = i;
                    }
                    else
                    {
                        if ((values_[i] > values_[iNextHighest]) && i != iHighest)
                            iNextHighest = i;
                    }
                    if (values_[i] < values_[iLowest])
                        iLowest = i;
                }
                // Now compute accuracy, update iteration number and check end criteria
                //// Numerical Recipes exit strategy on fx (see NR in C++, p.410)
                //double low = values_[iLowest];
                //double high = values_[iHighest];
                //double rtol = 2.0*std::fabs(high - low)/
                //    (std::fabs(high) + std::fabs(low) + QL_EPSILON);
                //++iterationNumber_;
                //if (rtol < ftol ||
                //    endCriteria.checkMaxIterations(iterationNumber_, ecType)) {
                // GSL exit strategy on x (see GSL v. 1.9, http://www.gnu.org/software/gsl
                double simplexSize = Utils.computeSimplexSize(vertices_);
                ++iterationNumber_;
                if (simplexSize < xtol || endCriteria.checkMaxIterations(iterationNumber_, ref ecType))
                {
                    endCriteria.checkStationaryPoint(0.0, 0.0, ref maxStationaryStateIterations_, ref ecType);
                    endCriteria.checkMaxIterations(iterationNumber_, ref ecType);
                    x_ = vertices_[iLowest];
                    double low = values_[iLowest];
                    P.setFunctionValue(low);
                    P.setCurrentValue(x_);
                    return ecType;
                }
                // If end criteria is not met, continue
                double factor = -1.0;
                double vTry = extrapolate(ref P, iHighest, ref factor);
                if ((vTry <= values_[iLowest]) && (factor == -1.0))
                {
                    factor = 2.0;
                    extrapolate(ref P, iHighest, ref factor);
                } else if (Math.Abs(factor) > Const.QL_Epsilon) {
                    if (vTry >= values_[iNextHighest])
                    {
                        double vSave = values_[iHighest];
                        factor = 0.5;
                        vTry = extrapolate(ref P, iHighest, ref factor);
                        if (vTry >= vSave && Math.Abs(factor) > Const.QL_Epsilon) {
                            for (int i = 0; i <= n; i++)
                            {
                                if (i != iLowest)
                                {
                                    #if QL_ARRAY_EXPRESSIONS
                                    vertices_[i] = 0.5 * (vertices_[i] + vertices_[iLowest]);
                                    #else
                                    vertices_[i] += vertices_[iLowest];
                                    vertices_[i] *= 0.5;
                                    #endif
                                    values_[i] = P.value(vertices_[i]);
                                }
                            }
                        }
                    }
                }
                // If can't extrapolate given the constraints, exit
                if (Math.Abs(factor) <= Const.QL_Epsilon) {
                    x_ = vertices_[iLowest];
                    double low = values_[iLowest];
                    P.setFunctionValue(low);
                    P.setCurrentValue(x_);
                    return EndCriteria.Type.StationaryFunctionValue;
                }
            } while (end == false);
            throw new ApplicationException("optimization failed: unexpected behaviour");
        }

        private double extrapolate(ref Problem P, int iHighest, ref double factor)
        {

            Vector pTry;
            do
            {
                int dimensions = values_.Count - 1;
                double factor1 = (1.0 - factor) / dimensions;
                double factor2 = factor1 - factor;
                // #if QL_ARRAY_EXPRESSIONS
                pTry = sum_ * factor1 - vertices_[iHighest] * factor2;
                //#else
                //                    // composite expressions fail to compile with gcc 3.4 on windows
                //                    pTry = sum_ * factor1;
                //                    pTry -= vertices_[iHighest] * factor2;
                //#endif
                factor *= 0.5;
            } while (!P.constraint().test(pTry) && Math.Abs(factor) > Const.QL_Epsilon);
            if (Math.Abs(factor) <= Const.QL_Epsilon) {
        	    return values_[iHighest];
            }
            factor *= 2.0;
            double vTry = P.value(pTry);
            if (vTry < values_[iHighest])
            {
                values_[iHighest] = vTry;
                //#if QL_ARRAY_EXPRESSIONS
                sum_ += pTry - vertices_[iHighest];
                //#else
                //                    sum_ += pTry;
                //                    sum_ -= vertices_[iHighest];
                //#endif
                vertices_[iHighest] = pTry;
            }
            return vTry;

        }
        private double lambda_;
        private InitializedList<Vector> vertices_;
        private Vector values_;
        private Vector sum_;
    }
}