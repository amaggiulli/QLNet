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

    //! Multi-dimensional Conjugate Gradient class.
    //    ! Fletcher-Reeves-Polak-Ribiere algorithm
    //        adapted from Numerical Recipes in C, 2nd edition.
    //        
    //        User has to provide line-search method and optimization end criteria.
    //        Search direction \f$ d_i = - f'(x_i) + c_i*d_{i-1} \f$
    //        where \f$ c_i = ||f'(x_i)||^2/||f'(x_{i-1})||^2 \f$
    //        and \f$ d_1 = - f'(x_1) \f$
    //    
    public class ConjugateGradient : LineSearchBasedMethod
    {
        public ConjugateGradient()
            : this(null)
        {
        }
        public ConjugateGradient(LineSearch lineSearch)
            : base(lineSearch)
        {
        }
        //! solve the optimization problem P
        public override EndCriteria.Type minimize(Problem P, EndCriteria endCriteria)
		{
			// Initializations
			double ftol = endCriteria.functionEpsilon();
			int maxStationaryStateIterations_ = endCriteria.maxStationaryStateIterations();
			EndCriteria.Type ecType = EndCriteria.Type.None; // reset end criteria
			P.reset(); // reset problem
			Vector x_ = P.currentValue(); // store the starting point
			int iterationNumber_ =0; // stationaryStateIterationNumber_=0
			lineSearch_.searchDirection = new Vector(x_.Count); // dimension line search
			bool done = false;
	
			// function and squared norm of gradient values;
			double fnew;
			double fold;
			double gold2;
			double c;
			double fdiff;
			double normdiff;
			// classical initial value for line-search step
			double t = 1.0;
			// Set gradient g at the size of the optimization problem search direction
			int sz = lineSearch_.searchDirection.Count;
			Vector g = new Vector(sz);
			Vector d = new Vector(sz);
			Vector sddiff = new Vector(sz);
			// Initialize cost function, gradient g and search direction
			P.setFunctionValue(P.valueAndGradient(g, x_));
            P.setGradientNormValue(Vector.DotProduct(g, g));
			lineSearch_.searchDirection = g * -1.0;
			// Loop over iterations
			do
			{
				// Linesearch
				t = lineSearch_.value(P, ref ecType, endCriteria, t);
				// don't throw: it can fail just because maxIterations exceeded
				//QL_REQUIRE(lineSearch_->succeed(), "line-search failed!");
				if (lineSearch_.succeed())
				{
					// Updates
					d = lineSearch_.searchDirection;
					// New point
					x_ = lineSearch_.lastX();
					// New function value
					fold = P.functionValue();
					P.setFunctionValue(lineSearch_.lastFunctionValue());
					// New gradient and search direction vectors
					g = lineSearch_.lastGradient();
					// orthogonalization coef
					gold2 = P.gradientNormValue();
					P.setGradientNormValue(lineSearch_.lastGradientNorm2());
					c = P.gradientNormValue() / gold2;
					// conjugate gradient search direction
					sddiff = ((g*-1.0) + c * d) - lineSearch_.searchDirection;
                    normdiff = Math.Sqrt(Vector.DotProduct(sddiff, sddiff));
					lineSearch_.searchDirection = (g*-1.0) + c * d;
					// Now compute accuracy and check end criteria
					// Numerical Recipes exit strategy on fx (see NR in C++, p.423)
					fnew = P.functionValue();
					fdiff = 2.0 *Math.Abs(fnew-fold) / (Math.Abs(fnew) + Math.Abs(fold) + Double.Epsilon);
					if (fdiff < ftol || endCriteria.checkMaxIterations(iterationNumber_, ref ecType))
					{
						endCriteria.checkStationaryFunctionValue(0.0, 0.0, ref maxStationaryStateIterations_, ref ecType);
						endCriteria.checkMaxIterations(iterationNumber_, ref ecType);
						return ecType;
					}
					//done = endCriteria(iterationNumber_,
					//                   stationaryStateIterationNumber_,
					//                   true,  //FIXME: it should be in the problem
					//                   fold,
					//                   std::sqrt(gold2),
					//                   P.functionValue(),
					//                   std::sqrt(P.gradientNormValue()),
					//                   ecType);
					P.setCurrentValue(x_); // update problem current value
					++iterationNumber_; // Increase iteration number
					}
				else
				{
					done =true;
				}
			} while (!done);
			P.setCurrentValue(x_);
			return ecType;
		}
    }
}