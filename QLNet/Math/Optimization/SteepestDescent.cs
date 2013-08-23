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

    //! Multi-dimensional steepest-descent class
    //    ! User has to provide line-search method and optimization end criteria
    //
    //        search direction \f$ = - f'(x) \f$
    //    
    public class SteepestDescent : LineSearchBasedMethod
    {
        public SteepestDescent()
            : this(null)
        {
        }
        public SteepestDescent(LineSearch lineSearch)
            : base(lineSearch)
        {
        }
        //! minimize the optimization problem P
        public override EndCriteria.Type minimize(Problem P, EndCriteria endCriteria)
        {
            EndCriteria.Type ecType = EndCriteria.Type.None;
            P.reset();
            Vector x_ = P.currentValue();
            int iterationNumber_ = 0;
            int stationaryStateIterationNumber_ = 0;
            lineSearch_.searchDirection = new Vector(x_.Count);
            bool end;

            // function and squared norm of gradient values;
            double normdiff;
            // classical initial value for line-search step
            double t = 1.0;
            // Set gold at the size of the optimization problem search direction
            Vector gold = new Vector(lineSearch_.searchDirection.Count);
            Vector gdiff = new Vector(lineSearch_.searchDirection.Count);

            P.setFunctionValue(P.valueAndGradient(gold, x_));
            lineSearch_.searchDirection = gold*-1.0;
            P.setGradientNormValue(Vector.DotProduct(gold, gold));
            normdiff = Math.Sqrt(P.gradientNormValue());

            do
            {
                // Linesearch
                t = lineSearch_.value(P, ref ecType, endCriteria, t);

                if (!(lineSearch_.succeed()))
                    throw new ApplicationException("line-search failed!");

                // End criteria
                // FIXME: it's never been used! ???
                // , normdiff
                end = endCriteria.value(iterationNumber_, ref stationaryStateIterationNumber_, true, P.functionValue(), Math.Sqrt(P.gradientNormValue()), lineSearch_.lastFunctionValue(), Math.Sqrt(lineSearch_.lastGradientNorm2()), ref ecType);

                // Updates
                // New point
                x_ = lineSearch_.lastX();
                // New function value
                P.setFunctionValue(lineSearch_.lastFunctionValue());
                // New gradient and search direction vectors
                gdiff = gold - lineSearch_.lastGradient();
                normdiff = Math.Sqrt(Vector.DotProduct(gdiff, gdiff));
                gold = lineSearch_.lastGradient();
                lineSearch_.searchDirection = gold*-1.0;
                // New gradient squared norm
                P.setGradientNormValue(lineSearch_.lastGradientNorm2());

                // Increase interation number
                ++iterationNumber_;
            } while (end == false);

            P.setCurrentValue(x_);
            return ecType;

        }
    }
}