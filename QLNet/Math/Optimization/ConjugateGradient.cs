/*
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
 Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)
  
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

namespace QLNet
{
   //! Multi-dimensional Conjugate Gradient class.
   /*! Fletcher-Reeves-Polak-Ribiere algorithm
       adapted from Numerical Recipes in C, 2nd edition.

       User has to provide line-search method and optimization end criteria.
       Search direction \f$ d_i = - f'(x_i) + c_i*d_{i-1} \f$
       where \f$ c_i = ||f'(x_i)||^2/||f'(x_{i-1})||^2 \f$
       and \f$ d_1 = - f'(x_1) \f$

       This optimization method requires the knowledge of
       the gradient of the cost function.

       \ingroup optimizers
   */
   public class ConjugateGradient : LineSearchBasedMethod
   {
      public ConjugateGradient(LineSearch lineSearch = null)
         : base(lineSearch)
      {}

      protected override Vector getUpdatedDirection( Problem P, double gold2, Vector gradient )
      {
         return lineSearch_.lastGradient()*-1 +
         ( P.gradientNormValue() / gold2 ) * lineSearch_.searchDirection;
      }
   }
}