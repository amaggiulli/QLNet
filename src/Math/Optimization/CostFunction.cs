/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
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
   //!  Cost function abstract class for optimization problem
   public abstract class CostFunction
   {
      //! method to overload to compute the cost function value in x
      public abstract double value( Vector x );
      //! method to overload to compute the cost function values in x
      public abstract Vector values( Vector x );

      //! method to overload to compute grad_f, the first derivative of
      //  the cost function with respect to x
      public virtual void gradient( Vector grad, Vector x )
      {
         double eps = finiteDifferenceEpsilon(), fp, fm;
         Vector xx = new Vector( x );
         for ( int i = 0; i < x.Count; i++ )
         {
            xx[i] += eps;
            fp = value( xx );
            xx[i] -= 2.0 * eps;
            fm = value( xx );
            grad[i] = 0.5 * ( fp - fm ) / eps;
            xx[i] = x[i];
         }
      }

      //! method to overload to compute grad_f, the first derivative of
      //  the cost function with respect to x and also the cost function
      public virtual double valueAndGradient( Vector grad, Vector x )
      {
         gradient( grad, x );
         return value( x );
      }
       
      //! method to overload to compute J_f, the jacobian of
      // the cost function with respect to x
      public virtual void jacobian(Matrix jac, Vector x) 
      {
         double eps = finiteDifferenceEpsilon();
         Vector xx = new Vector(x) ;
         Vector fp = new Vector() ;
         Vector fm = new Vector() ;
         for(int i=0; i<x.size(); ++i) 
         {
            xx[i] += eps;
            fp = values(xx);
            xx[i] -= 2.0*eps;
            fm = values(xx);
            for(int j=0; j<fp.size(); ++j) 
            {
               jac[j,i] = 0.5*(fp[j]-fm[j])/eps;
            }
            xx[i] = x[i];
         }
      }

      //! method to overload to compute J_f, the jacobian of
      // the cost function with respect to x and also the cost function
      public virtual Vector valuesAndJacobian(Matrix jac, Vector x) 
      {
         jacobian(jac,x);
         return values(x);
      }

      //! Default epsilon for finite difference method :
      public virtual double finiteDifferenceEpsilon() { return 1e-8; }
   }

   public interface IParametersTransformation 
   {
      Vector direct(Vector x) ;
      Vector inverse(Vector x);
   }
}
