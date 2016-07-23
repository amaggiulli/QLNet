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
using System;
using System.Collections.Generic;

namespace QLNet
{
   //! Levenberg-Marquardt optimization method
   /*! This implementation is based on MINPACK
       (<http://www.netlib.org/minpack>,
       <http://www.netlib.org/cephes/linalg.tgz>)
       It has a built in fd scheme to compute
       the jacobian, which is used by default.
       If useCostFunctionsJacobian is true the
       corresponding method in the cost function
       of the problem is used instead. Note that
       the default implementation of the jacobian
       in CostFunction uses a central difference
       (oder 2, but requiring more function
       evaluations) compared to the forward
       difference implemented here (order 1).
   */
   public class LevenbergMarquardt : OptimizationMethod
   {
      private Problem currentProblem_;
      private Vector initCostValues_;
      private Matrix initJacobian_;
      private bool useCostFunctionsJacobian_;

      private int info_;
      public int getInfo() { return info_; }

      private double epsfcn_, xtol_, gtol_;

      public LevenbergMarquardt() : this( 1.0e-8, 1.0e-8, 1.0e-8 ) { }
      public LevenbergMarquardt( double epsfcn, double xtol, double gtol, bool useCostFunctionsJacobian = false )
      {
         info_ = 0;
         epsfcn_ = epsfcn;
         xtol_ = xtol;
         gtol_ = gtol;
         useCostFunctionsJacobian_ = useCostFunctionsJacobian;
      }

      public override EndCriteria.Type minimize( Problem P, EndCriteria endCriteria )
      {
         EndCriteria.Type ecType = EndCriteria.Type.None;
         P.reset();
         Vector x_ = P.currentValue();
         currentProblem_ = P;
         initCostValues_ = P.costFunction().values( x_ );
         int m = initCostValues_.size();
         int n = x_.size();
         if ( useCostFunctionsJacobian_ )
         {
            initJacobian_ = new Matrix( m, n );
            P.costFunction().jacobian( initJacobian_, x_ );
         }

         Vector xx = new Vector( x_ );
         Vector fvec = new Vector( m ), diag = new Vector( n );

         int mode = 1;
         double factor = 1;
         int nprint = 0;
         int info = 0;
         int nfev = 0;

         Matrix fjac = new Matrix( m, n );

         int ldfjac = m;

         List<int> ipvt = new InitializedList<int>( n );
         Vector qtf = new Vector( n ), wa1 = new Vector( n ), wa2 = new Vector( n ), wa3 = new Vector( n ), wa4 = new Vector( m );

         // call lmdif to minimize the sum of the squares of m functions
         // in n variables by the Levenberg-Marquardt algorithm.
         Func<int, int, Vector, int, Matrix> j = null;
         if ( useCostFunctionsJacobian_ ) 
            j = jacFcn;

         MINPACK.lmdif( m, n, xx, ref fvec,
                                  endCriteria.functionEpsilon(),
                                  xtol_,
                                  gtol_,
                                  endCriteria.maxIterations(),
                                  epsfcn_,
                                  diag, mode, factor,
                                  nprint, ref info, ref nfev, ref fjac,
                                  ldfjac, ref ipvt, ref qtf,
                                  wa1, wa2, wa3, wa4,
                                  fcn, j);
         info_ = info;
         // check requirements & endCriteria evaluation
         if ( info == 0 ) throw new Exception( "MINPACK: improper input parameters" );
         //if(info == 6) throw new Exception("MINPACK: ftol is too small. no further " +
         //                                             "reduction in the sum of squares is possible.");

         if ( info != 6 ) ecType = EndCriteria.Type.StationaryFunctionValue;
         //QL_REQUIRE(info != 5, "MINPACK: number of calls to fcn has reached or exceeded maxfev.");
         endCriteria.checkMaxIterations( nfev, ref ecType );
         if ( info == 7 ) throw new Exception( "MINPACK: xtol is too small. no further " +
                                           "improvement in the approximate " +
                                           "solution x is possible." );
         if ( info == 8 ) throw new Exception( "MINPACK: gtol is too small. fvec is " +
                                           "orthogonal to the columns of the " +
                                           "jacobian to machine precision." );
         // set problem
         x_ = new Vector( xx.GetRange( 0, n ) );
         P.setCurrentValue( x_ );
         P.setFunctionValue( P.costFunction().value( x_ ) );

         return ecType;
      }

      public Vector fcn( int m, int n, Vector x, int iflag )
      {
         Vector xt = new Vector( x );
         Vector fvec;
         // constraint handling needs some improvement in the future:
         // starting point should not be close to a constraint violation
         if ( currentProblem_.constraint().test( xt ) )
         {
            fvec = new Vector( currentProblem_.values( xt ) );
         }
         else
         {
            fvec = new Vector( initCostValues_ );
         }
         return fvec;
      }

      public Matrix jacFcn( int m, int n, Vector x, int iflag )
      {
         Vector xt = new Vector(x);
         Matrix fjac;
         //std::copy(x, x+n, xt.begin());
         // constraint handling needs some improvement in the future:
         // starting point should not be close to a constraint violation
         if (currentProblem_.constraint().test(xt)) 
         {
            Matrix tmp = new Matrix(m,n);
            currentProblem_.costFunction().jacobian(tmp, xt);
            Matrix tmpT = Matrix.transpose(tmp);
            fjac = new Matrix( tmpT );
         } 
         else 
         {
            Matrix tmpT = Matrix.transpose(initJacobian_);
            fjac = new Matrix( tmpT );
         }
         return fjac;
      }
   }
}
