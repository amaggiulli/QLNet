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
   // backflat interpolation in first component, linear in second component
   public class BackwardflatLinearInterpolationImpl : Interpolation2D.templateImpl
   {
      public BackwardflatLinearInterpolationImpl(List<double> xBegin, int xEnd,List<double> yBegin, int yEnd,
         Matrix zData)
            : base(xBegin,xEnd,yBegin,yEnd,zData) 
      {
         calculate();
      }

      public override void calculate() {}
            
      public override double value(double x, double y) 
      {
         int j = locateY(y);
         double z1, z2;
         if (x <= xBegin_[0])
         {
            z1 = zData_[j,0];
            z2 = zData_[j + 1,0];
         }
         else
         {
            int i = locateX(x);
            if (x == xBegin_[i])
            {
               z1 = zData_[j,i];
               z2 = zData_[j + 1,i];
            }
            else
            {
               z1 = zData_[j,i + 1];
               z2 = zData_[j + 1,i + 1];
            }
         }

         double u = (y - yBegin_[j])/ (yBegin_[j + 1] - yBegin_[j]);

         return (1.0 - u)*z1 + u*z2;
            
      }
      
   }

   public class BackwardflatLinearInterpolation : Interpolation2D 
   {
      /*! \pre the \f$ x \f$ and \f$ y \f$ values must be sorted. */
      public BackwardflatLinearInterpolation(List<double> xBegin, int xEnd,List<double> yBegin, int yEnd,Matrix zData) 
      {
         impl_ = new BackwardflatLinearInterpolationImpl(xBegin, xEnd,yBegin, yEnd,zData);      
      }
   }

   public class BackwardflatLinear 
   {
      public Interpolation2D interpolate(List<double> xBegin, int xEnd,List<double> yBegin, int yEnd,Matrix z) 
      {
         return new BackwardflatLinearInterpolation(xBegin,xEnd,yBegin,yEnd,z);
      }
   }

   
}


