//  Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)
//  
//  This file is part of QLNet Project https://github.com/amaggiulli/qlnet
//  QLNet is free software: you can redistribute it and/or modify it
//  under the terms of the QLNet license.  You should have received a
//  copy of the license along with this program; if not, license is  
//  available online at <http://qlnet.sourceforge.net/License.html>.
//   
//  QLNet is a based on QuantLib, a free-software/open-source library
//  for financial quantitative analysts and developers - http://quantlib.org/
//  The QuantLib license is available online at http://quantlib.org/license.shtml.
//  
//  This program is distributed in the hope that it will be useful, but WITHOUT
//  ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
//  FOR A PARTICULAR PURPOSE.  See the license for more details.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QLNet
{
   //! Broyden-Fletcher-Goldfarb-Shanno algorithm
   /*! See <http://en.wikipedia.org/wiki/BFGS_method>.

       Adapted from Numerical Recipes in C, 2nd edition.

       User has to provide line-search method and optimization end criteria.
   */
   public class BFGS : LineSearchBasedMethod
   {
      public BFGS( LineSearch lineSearch = null)
        : base(lineSearch)
      {
         inverseHessian_ = new Matrix();
      }
      
      //! \name LineSearchBasedMethod interface
      //@{
      protected override Vector getUpdatedDirection(Problem P,double gold2,Vector oldGradient)
      {
         if (inverseHessian_.rows() == 0)
         {
            // first time in this update, we create needed structures
            inverseHessian_ = new Matrix(P.currentValue().size(),P.currentValue().size(), 0.0);
            for (int i = 0; i < P.currentValue().size(); ++i)
                inverseHessian_[i,i] = 1.0;
         }

         Vector diffGradient = new Vector();
         Vector diffGradientWithHessianApplied = new Vector(P.currentValue().size(), 0.0);

         diffGradient = lineSearch_.lastGradient() - oldGradient;
         for (int i = 0; i < P.currentValue().size(); ++i)
            for (int j = 0; j < P.currentValue().size(); ++j)
               diffGradientWithHessianApplied[i] += inverseHessian_[i,j] * diffGradient[j];

         double fac, fae, fad;
         double sumdg, sumxi;

         fac = fae = sumdg = sumxi = 0.0;
         for (int i = 0; i < P.currentValue().size(); ++i)
         {
            fac += diffGradient[i] * lineSearch_.searchDirection[i];
            fae += diffGradient[i] * diffGradientWithHessianApplied[i];
            sumdg += Math.Pow(diffGradient[i], 2.0);
            sumxi += Math.Pow(lineSearch_.searchDirection[i], 2.0);
         }

         if (fac > Math.Sqrt(1e-8 * sumdg * sumxi))  // skip update if fac not sufficiently positive
         {
            fac = 1.0 / fac;
            fad = 1.0 / fae;

            for (int i = 0; i < P.currentValue().size(); ++i)
               diffGradient[i] = fac * lineSearch_.searchDirection[i] - fad * diffGradientWithHessianApplied[i];

            for (int i = 0; i < P.currentValue().size(); ++i)
               for (int j = 0; j < P.currentValue().size(); ++j)
               {
                  inverseHessian_[i,j] += fac * lineSearch_.searchDirection[i] * lineSearch_.searchDirection[j];
                  inverseHessian_[i,j] -= fad * diffGradientWithHessianApplied[i] * diffGradientWithHessianApplied[j];
                  inverseHessian_[i,j] += fae * diffGradient[i] * diffGradient[j];
               }
         }
         //else
         //  throw "BFGS: FAC not sufficiently positive";


         Vector direction = new Vector(P.currentValue().size());
         for (int i = 0; i < P.currentValue().size(); ++i)
         {
            direction[i] = 0.0;
            for (int j = 0; j < P.currentValue().size(); ++j)
               direction[i] -= inverseHessian_[i,j] * lineSearch_.lastGradient()[j];
         }

        return direction;

      }
      //@}
      //! inverse of hessian matrix
      private Matrix inverseHessian_;
   }
}
