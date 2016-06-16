using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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


