/*
 Copyright (C) 2008-2013  Andrea Maggiulli (a.maggiulli@gmail.com)

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
   //! Integral of a 1-dimensional function using the Gauss quadratures method
   /*! References:
      Gauss quadratures and orthogonal polynomials

      G.H. Gloub and J.H. Welsch: Calculation of Gauss quadrature rule.
      Math. Comput. 23 (1986), 221-230

      "Numerical Recipes in C", 2nd edition,
      Press, Teukolsky, Vetterling, Flannery,

      \test the correctness of the result is tested by checking it
            against known good values.
   */
   public class GaussianQuadrature 
   {
      public GaussianQuadrature(int n, GaussianOrthogonalPolynomial orthPoly)
      {
         x_ = new Vector(n);
         w_ = new Vector(n);

        // set-up matrix to compute the roots and the weights
        Vector e = new Vector(n-1);

        int i;
        for (i=1; i < n; ++i) 
        {
            x_[i] = orthPoly.alpha(i);
            e[i-1] = Math.Sqrt(orthPoly.beta(i));
        }
        x_[0] = orthPoly.alpha(0);

        TqrEigenDecomposition tqr = new TqrEigenDecomposition( x_, e,
                               TqrEigenDecomposition.EigenVectorCalculation.OnlyFirstRowEigenVector,
                               TqrEigenDecomposition.ShiftStrategy.Overrelaxation);

        x_ = tqr.eigenvalues();
        Matrix ev = tqr.eigenvectors();

        double mu_0 = orthPoly.mu_0();
        for (i=0; i<n; ++i) {
            w_[i] = mu_0*ev[0,i]*ev[0,i] / orthPoly.w(x_[i]);
        }
      }

   
      public double value(Func<double,double> f) 
      {
            double sum = 0.0;
            for (int i = order()-1; i >= 0; --i) 
            {
                sum += w_[i] * f(x_[i]);
            }
            return sum;
        }

        int order()  { return x_.size(); }
        Vector weights() { return w_; }
        Vector x()       { return x_; }
        
      
      private Vector x_, w_;
   }

   //! generalized Gauss-Laguerre integration
   /*! This class performs a 1-dimensional Gauss-Laguerre integration.
       \f[
       \int_{0}^{\inf} f(x) \mathrm{d}x
       \f]
       The weighting function is
       \f[
           w(x;s)=x^s \exp{-x}
       \f]
       and \f[ s > -1 \f]
   */
   public class GaussLaguerreIntegration : GaussianQuadrature 
   {
      public GaussLaguerreIntegration(int n, double s = 0.0)
        : base(n, new GaussLaguerrePolynomial(s)) {}
   }

   //! generalized Gauss-Hermite integration
   /*! This class performs a 1-dimensional Gauss-Hermite integration.
       \f[
       \int_{-\inf}^{\inf} f(x) \mathrm{d}x
       \f]
       The weighting function is
       \f[
           w(x;\mu)=|x|^{2\mu} \exp{-x*x}
       \f]
       and \f[ \mu > -0.5 \f]
   */
   public class GaussHermiteIntegration : GaussianQuadrature 
   {
     public GaussHermiteIntegration(int n, double mu = 0.0)
       : base(n, new GaussHermitePolynomial(mu)) {}
    }

   //! Gauss-Jacobi integration
   /*! This class performs a 1-dimensional Gauss-Jacobi integration.
       \f[
       \int_{-1}^{1} f(x) \mathrm{d}x
       \f]
       The weighting function is
       \f[
           w(x;\alpha,\beta)=(1-x)^\alpha (1+x)^\beta
       \f]
   */
   public class GaussJacobiIntegration : GaussianQuadrature 
   {
     public GaussJacobiIntegration(int n, double alpha, double beta)
       : base(n, new GaussJacobiPolynomial(alpha, beta)) {}
   }

   //! Gauss-Hyperbolic integration
   /*! This class performs a 1-dimensional Gauss-Hyperbolic integration.
       \f[
       \int_{-\inf}^{\inf} f(x) \mathrm{d}x
       \f]
       The weighting function is
       \f[
           w(x)=1/cosh(x)
       \f]
   */
   public class GaussHyperbolicIntegration : GaussianQuadrature 
   {
     public GaussHyperbolicIntegration(int n)
       : base(n, new GaussHyperbolicPolynomial()) {}
   }

   //! Gauss-Legendre integration
   /*! This class performs a 1-dimensional Gauss-Legendre integration.
       \f[
       \int_{-1}^{1} f(x) \mathrm{d}x
       \f]
       The weighting function is
       \f[
           w(x)=1
       \f]
   */
   public class GaussLegendreIntegration : GaussianQuadrature 
   {
     public GaussLegendreIntegration(int n)
       : base(n, new GaussJacobiPolynomial(0.0, 0.0)) {}
   }

   //! Gauss-Chebyshev integration
   /*! This class performs a 1-dimensional Gauss-Chebyshev integration.
       \f[
       \int_{-1}^{1} f(x) \mathrm{d}x
       \f]
       The weighting function is
       \f[
           w(x)=(1-x^2)^{-1/2}
       \f]
   */
   public class GaussChebyshevIntegration : GaussianQuadrature 
   {
     public GaussChebyshevIntegration(int n)
       : base(n, new GaussJacobiPolynomial(-0.5, -0.5)) {}
   }

   //! Gauss-Chebyshev integration (second kind)
   /*! This class performs a 1-dimensional Gauss-Chebyshev integration.
       \f[
       \int_{-1}^{1} f(x) \mathrm{d}x
       \f]
       The weighting function is
       \f[
           w(x)=(1-x^2)^{1/2}
       \f]
   */
   public class GaussChebyshev2ndIntegration : GaussianQuadrature 
   {
     public GaussChebyshev2ndIntegration(int n)
     : base(n, new GaussJacobiPolynomial(0.5, 0.5)) {}
   }

   //! Gauss-Gegenbauer integration
   /*! This class performs a 1-dimensional Gauss-Gegenbauer integration.
       \f[
       \int_{-1}^{1} f(x) \mathrm{d}x
       \f]
       The weighting function is
       \f[
           w(x)=(1-x^2)^{\lambda-1/2}
       \f]
   */
   public class GaussGegenbauerIntegration : GaussianQuadrature 
   {
     public GaussGegenbauerIntegration(int n, double lambda)
       : base(n, new GaussJacobiPolynomial(lambda-0.5, lambda-0.5)) {}
   }

   //! tabulated Gauss-Legendre quadratures
   public class TabulatedGaussLegendre 
   {
      public TabulatedGaussLegendre(int n = 20) { order(n); }
        //template <class F>
        //Real operator() (const F& f) const {
        //    QL_ASSERT(w_!=0, "Null weights" );
        //    QL_ASSERT(x_!=0, "Null abscissas");
        //    Size startIdx;
        //    Real val;

        //    const Size isOrderOdd = order_ & 1;

        //    if (isOrderOdd) {
        //      QL_ASSERT((n_>0), "assume at least 1 point in quadrature");
        //      val = w_[0]*f(x_[0]);
        //      startIdx=1;
        //    } else {
        //      val = 0.0;
        //      startIdx=0;
        //    }

        //    for (Size i=startIdx; i<n_; ++i) {
        //        val += w_[i]*f( x_[i]);
        //        val += w_[i]*f(-x_[i]);
        //    }
        //    return val;
        //}
      
      public double value (Func<double,double> f) 
      {
         Utils.QL_REQUIRE( w_ != null, () => "Null weights" );
         Utils.QL_REQUIRE( x_ != null, () => "Null abscissas" );
          int startIdx;
          double val;

          int isOrderOdd = order_ & 1;

          if (isOrderOdd > 0) {
             Utils.QL_REQUIRE( ( n_ > 0 ), () => "assume at least 1 point in quadrature" );
            val = w_[0]*f(x_[0]);
            startIdx=1;
          } else {
            val = 0.0;
            startIdx=0;
          }

          for (int i=startIdx; i<n_; ++i) {
              val += w_[i]*f( x_[i]);
              val += w_[i]*f(-x_[i]);
          }
          return val;
      }

        public void order(int order)
        {
           switch(order) 
           {
              case(6):
                 order_=order; x_= x6.ToList(); w_= w6.ToList(); n_=n6;
                 break;
              case(7):
                 order_=order; x_=x7.ToList(); w_=w7.ToList(); n_=n7;
                 break;
              case(12):
                 order_=order; x_=x12.ToList(); w_=w12.ToList(); n_=n12;
                 break;
              case(20):
                 order_=order; x_=x20.ToList(); w_=w20.ToList(); n_=n20;
                 break;
              default:
                 Utils.QL_FAIL("order " + order + " not supported");
                 break;
            }
        }

        public int order() { return order_; }

      
        private int order_;

        private List<double> w_;
        private List<double> x_;
        private int  n_;

        private static double[] w6 = { 0.467913934572691,0.360761573048139,0.171324492379170 };
        private static double[] x6 = { 0.238619186083197, 0.661209386466265, 0.932469514203152 };
        private static int n6 = 3;

        private static double[] w7 = { 0.417959183673469,0.381830050505119,0.279705391489277,0.129484966168870 };
        private static  double[] x7 = { 0.000000000000000,0.405845151377397,0.741531185599394,0.949107912342759 };
        private static  int n7 = 4;

        private static  double[] w12 = { 0.249147045813403,0.233492536538355,0.203167426723066,0.160078328543346,
                                         0.106939325995318,0.047175336386512 };
        private static  double[] x12 = { 0.125233408511469,0.367831498998180,0.587317954286617,0.769902674194305,
                                         0.904117256370475,0.981560634246719 };
        private static  int n12 = 6;

        private static  double[] w20 = { 0.152753387130726,0.149172986472604,0.142096109318382,0.131688638449177,
                                         0.118194531961518,0.101930119817240,0.083276741576704,0.062672048334109,
                                         0.040601429800387,0.017614007139152 };
        private static  double[] x20 = { 0.076526521133497,0.227785851141645,0.373706088715420,0.510867001950827,
                                         0.636053680726515,0.746331906460151,0.839116971822219,0.912234428251326,
                                         0.963971927277914,0.993128599185095 };
        private static  int n20 = 10;
    }
}
