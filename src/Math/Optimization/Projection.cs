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

using System.Collections.Generic;

namespace QLNet
{
   public class Projection
   {
      public Projection(Vector parameterValues,List<bool> fixParameters = null)
      {
         numberOfFreeParameters_ = 0;
         fixedParameters_ = parameterValues;
         actualParameters_ = parameterValues;
         fixParameters_ = fixParameters ?? new InitializedList<bool>(actualParameters_.size(), false);

         Utils.QL_REQUIRE( fixedParameters_.size() == fixParameters_.Count,()=>
                   "fixedParameters_.size()!=parametersFreedoms_.size()" );
         for ( int i = 0; i < fixParameters_.Count; i++ )
            if ( !fixParameters_[i] )
               numberOfFreeParameters_++;

         Utils.QL_REQUIRE( numberOfFreeParameters_ > 0,()=> "numberOfFreeParameters==0" );
      }

      //! returns the subset of free parameters corresponding
      // to set of parameters
      public virtual Vector project(Vector parameters)
      {
         Utils.QL_REQUIRE(parameters.size() == fixParameters_.Count,()=> "parameters.size()!=parametersFreedoms_.size()");
         Vector projectedParameters = new Vector(numberOfFreeParameters_);
         int i = 0;
         for (int j = 0; j < fixParameters_.Count; j++)
            if (!fixParameters_[j])
               projectedParameters[i++] = parameters[j];
         return projectedParameters;
      }

      //! returns whole set of parameters corresponding to the set
      // of projected parameters
      public virtual Vector include(Vector projectedParameters)
      {
         Utils.QL_REQUIRE(projectedParameters.size() == numberOfFreeParameters_,()=>
                   "projectedParameters.size()!=numberOfFreeParameters");
         Vector y = new Vector(fixedParameters_);
         int i = 0;
         for (int j = 0; j < y.size(); j++)
            if (!fixParameters_[j])
                y[j] = projectedParameters[i++];
         return y;
      }

      protected void mapFreeParameters(Vector parameterValues)
      {
         Utils.QL_REQUIRE( parameterValues.size() == numberOfFreeParameters_,()=>
                   "parameterValues.size()!=numberOfFreeParameters" );
         int i = 0;
         for ( int j = 0; j < actualParameters_.size(); j++ )
            if ( !fixParameters_[j] )
               actualParameters_[j] = parameterValues[i++];
      }
      protected int numberOfFreeParameters_;
      protected Vector fixedParameters_;
      protected Vector actualParameters_;
      protected List<bool> fixParameters_;
   }
}
