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
   public class ProjectedConstraint : Constraint
   {
      private class Impl : IConstraint 
      {
         public Impl( Constraint constraint,
                      Vector parameterValues,
                      List<bool>fixParameters)

         {
            constraint_ = constraint;
            projection_ = new Projection(parameterValues, fixParameters);
         }

         public Impl( Constraint constraint, Projection projection)
         {
            constraint_ = constraint;
            projection_ = projection;
         }
            
         public bool test(Vector parameters) 
         {
            return constraint_.test(projection_.include(parameters));
         }
            
         public Vector upperBound(Vector parameters) 
         {
            return constraint_.upperBound(projection_.include(parameters));
         }
            
         public Vector lowerBound(Vector parameters) 
         {
            return constraint_.lowerBound(projection_.include(parameters));
         }

          private Constraint constraint_;
          private Projection projection_;
      }

      public ProjectedConstraint( Constraint constraint,
                                  Vector parameterValues,
                                  List<bool> fixParameters)
         : base( new Impl(constraint, parameterValues,fixParameters)) 
      {}

      public ProjectedConstraint( Constraint constraint, Projection projection)
            : base(new Impl(constraint, projection)) 
      {}

   }
}
