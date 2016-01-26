/*
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

namespace QLNet
{
   //! Cumulative bivariate normal distribution function
   /*! Drezner (1978) algorithm, six decimal places accuracy.

       For this implementation see
      "Option pricing formulas", E.G. Haug, McGraw-Hill 1998

       \todo check accuracy of this algorithm and compare with:
             1) Drezner, Z, (1978),
                Computation of the bivariate normal integral,
                Mathematics of Computation 32, pp. 277-279.
             2) Drezner, Z. and Wesolowsky, G. O. (1990)
                `On the Computation of the Bivariate Normal Integral',
                Journal of Statistical Computation and Simulation 35,
                pp. 101-107.
             3) Drezner, Z (1992)
                Computation of the Multivariate Normal Integral,
                ACM Transactions on Mathematics Software 18, pp. 450-460.
             4) Drezner, Z (1994)
                Computation of the Trivariate Normal Integral,
                Mathematics of Computation 62, pp. 289-294.
             5) Genz, A. (1992)
               `Numerical Computation of the Multivariate Normal
                Probabilities', J. Comput. Graph. Stat. 1, pp. 141-150.

       \test the correctness of the returned value is tested by
             checking it against known good results.
   */

   public class BivariateCumulativeNormalDistributionDr78
   {
      public BivariateCumulativeNormalDistributionDr78(double rho)
      {
         rho_ = rho;
         rho2_ = rho*rho;

         Utils.QL_REQUIRE(rho>=-1.0, ()=> "rho must be >= -1.0 (" + rho + " not allowed)");
         Utils.QL_REQUIRE(rho<=1.0,()=> "rho must be <= 1.0 (" + rho + " not allowed)");
      }

      // function
      public double value(double a, double b)
      {
         CumulativeNormalDistribution cumNormalDist = new CumulativeNormalDistribution();
         double CumNormDistA = cumNormalDist.value(a);
         double CumNormDistB = cumNormalDist.value(b);
         double MaxCumNormDistAB = Math.Max(CumNormDistA, CumNormDistB);
         double MinCumNormDistAB = Math.Min(CumNormDistA, CumNormDistB);

         if (1.0-MaxCumNormDistAB<1e-15)
            return MinCumNormDistAB;

         if (MinCumNormDistAB<1e-15)
            return MinCumNormDistAB;

         double a1 = a / Math.Sqrt(2.0 * (1.0 - rho2_));
         double b1 = b / Math.Sqrt(2.0 * (1.0 - rho2_));

         double result=-1.0;

         if (a<=0.0 && b<=0 && rho_<=0) 
         {
            double sum=0.0;
            for (int i=0; i<5; i++) 
            {
               for (int j=0;j<5; j++) 
               {
                  sum += x_[i]*x_[j]* Math.Exp(a1*(2.0*y_[i]-a1)+b1*(2.0*y_[j]-b1) + 2.0*rho_*(y_[i]-a1)*(y_[j]-b1));
                }
            }
            result = Math.Sqrt(1.0 - rho2_)/Const.M_PI*sum;
         } 
         else if (a<=0 && b>=0 && rho_>=0) 
         {
            BivariateCumulativeNormalDistributionDr78 bivCumNormalDist = new BivariateCumulativeNormalDistributionDr78(-rho_);
            result= CumNormDistA - bivCumNormalDist.value(a, -b);
         } 
         else if (a>=0.0 && b<=0.0 && rho_>=0.0) 
         {
            BivariateCumulativeNormalDistributionDr78 bivCumNormalDist = new BivariateCumulativeNormalDistributionDr78(-rho_);
            result= CumNormDistB - bivCumNormalDist.value(-a, b);
         } 
         else if (a>=0.0 && b>=0.0 && rho_<=0.0) 
         {
            result= CumNormDistA + CumNormDistB -1.0 + (this.value(-a, -b));
         } 
         else if (a*b*rho_>0.0) 
         {
            double rho1 = (rho_*a-b)*(a>0.0 ? 1.0: -1.0) / Math.Sqrt(a*a-2.0*rho_*a*b+b*b);
            BivariateCumulativeNormalDistributionDr78 bivCumNormalDist = new BivariateCumulativeNormalDistributionDr78(rho1);

            double rho2 = (rho_*b-a)*(b>0.0 ? 1.0: -1.0) / Math.Sqrt(a*a-2.0*rho_*a*b+b*b);
            BivariateCumulativeNormalDistributionDr78 CBND2 = new BivariateCumulativeNormalDistributionDr78(rho2);

            double delta = (1.0-(a>0.0 ? 1.0: -1.0)*(b>0.0 ? 1.0: -1.0))/4.0;

            result= bivCumNormalDist.value(a, 0.0) + CBND2.value(b, 0.0) - delta;
         } 
         else 
         {
            Utils.QL_FAIL("case not handled");
         }

         return result;
      }

      private double rho_, rho2_;
      private static double[] x_ = { 0.24840615,
                                     0.39233107,
                                     0.21141819,
                                     0.03324666,
                                     0.00082485334};

       private static double[] y_ = { 0.10024215,
                                      0.48281397,
                                      1.06094980,
                                      1.77972940,
                                      2.66976040000};
      
}

   //! Cumulative bivariate normal distibution function (West 2004)
   /*! The implementation derives from the article "Better
      Approximations To Cumulative Normal Distibutions", Graeme
      West, Dec 2004 available at www.finmod.co.za. Also available
      in Wilmott Magazine, 2005, (May), 70-76, The main code is a
      port of the C++ code at www.finmod.co.za/cumfunctions.zip.

      The algorithm is based on the near double-precision algorithm
      described in "Numerical Computation of Rectangular Bivariate
      an Trivariate Normal and t Probabilities", Genz (2004),
      Statistics and Computing 14, 151-160. (available at
      www.sci.wsu.edu/math/faculty/henz/homepage)

      The QuantLib implementation mainly differs from the original
      code in two regards;
      - The implementation of the cumulative normal distribution is
         QuantLib::CumulativeNormalDistribution
      - The arrays XX and W are zero-based

      \test the correctness of the returned value is tested by
            checking it against known good results.
   */
   public class BivariateCumulativeNormalDistributionWe04DP 
   {
      public BivariateCumulativeNormalDistributionWe04DP(double rho)
      {
         correlation_ = rho;
         Utils.QL_REQUIRE( rho >= -1.0,()=> "rho must be >= -1.0 (" + rho + " not allowed)" );
         Utils.QL_REQUIRE( rho <= 1.0,()=> "rho must be <= 1.0 (" + rho + " not allowed)" );
      }
        
      // function
      public double value(double x, double y)
      {
         /* The implementation is described at section 2.4 "Hybrid
            Numerical Integration Algorithms" of "Numerical Computation
            of Rectangular Bivariate an Trivariate Normal and t
            Probabilities", Genz (2004), Statistics and Computing 14,
            151-160. (available at
            www.sci.wsu.edu/math/faculty/henz/homepage)

            The Gauss-Legendre quadrature have been extracted to
            TabulatedGaussLegendre (x,w zero-based)

            Tthe functions ot be integrated numerically have been moved
            to classes eqn3 and eqn6

            Change some magic numbers to M_PI */

         TabulatedGaussLegendre gaussLegendreQuad = new TabulatedGaussLegendre(20);
         if (Math.Abs(correlation_) < 0.3) 
         {
            gaussLegendreQuad.order(6);
         } 
         else if (Math.Abs(correlation_) < 0.75) 
         {
            gaussLegendreQuad.order(12);
         }

        double h = -x;
        double k = -y;
        double hk = h * k;
        double BVN = 0.0;

        if (Math.Abs(correlation_) < 0.925)
        {
            if (Math.Abs(correlation_) > 0)
            {
                double asr = Math.Asin(correlation_);
                eqn3 f = new eqn3(h,k,asr);
                BVN = gaussLegendreQuad.value(f.value);
                BVN *= asr * (0.25 / Const.M_PI);
            }
            BVN += cumnorm_.value(-h) * cumnorm_.value(-k);
        }
        else
        {
            if (correlation_ < 0)
            {
                k *= -1;
                hk *= -1;
            }
            if (Math.Abs(correlation_) < 1)
            {
                double Ass = (1 - correlation_) * (1 + correlation_);
                double a = Math.Sqrt(Ass);
                double bs = (h-k)*(h-k);
                double c = (4 - hk) / 8;
                double d = (12 - hk) / 16;
                double asr = -(bs / Ass + hk) / 2;
                if (asr > -100)
                {
                    BVN = a * Math.Exp(asr) *
                        (1 - c * (bs - Ass) * (1 - d * bs / 5) / 3 +
                         c * d * Ass * Ass / 5);
                }
                if (-hk < 100)
                {
                    double B = Math.Sqrt(bs);
                    BVN -= Math.Exp(-hk / 2) * 2.506628274631 *
                        cumnorm_.value(-B / a) * B *
                        (1 - c * bs * (1 - d * bs / 5) / 3);
                }
                a /= 2;
                eqn6 f = new eqn6(a,c,d,bs,hk);
                BVN += gaussLegendreQuad.value(f.value);
                BVN /= (-2.0 * Const.M_PI);
            }

            if (correlation_ > 0) {
                BVN += cumnorm_.value(-Math.Max(h, k));
            } else {
                BVN *= -1;
                if (k > h) {
                    // evaluate cumnorm where it is most precise, that
                    // is in the lower tail because of double accuracy
                    // around 0.0 vs around 1.0
                    if (h >= 0) {
                        BVN += cumnorm_.value(-h) - cumnorm_.value(-k);
                    } else {
                        BVN += cumnorm_.value(k) - cumnorm_.value(h);
                    }
                }
            }
        }
        return BVN;

      }
      private double correlation_;
      private  CumulativeNormalDistribution cumnorm_ = new CumulativeNormalDistribution();
   }

   public class eqn3 
   { 
      /* Relates to eqn3 Genz 2004 */
      public eqn3(double h, double k, double asr) 
      {
         hk_ = h * k;
         hs_  = (h * h + k * k) / 2;
         asr_ = asr;
      }
      public double value(double x) 
      {
         double sn = Math.Sin(asr_ * (-x + 1) * 0.5);
         return Math.Exp((sn * hk_ - hs_) / (1.0 - sn * sn));
      }
      
      private double hk_, asr_, hs_;
        
   }

   public class eqn6 
   { 
      /* Relates to eqn6 Genz 2004 */
      public eqn6(double a, double c, double d, double bs, double hk)
      {
         a_ = a;
         c_ = c;
         d_ = d;
         bs_ = bs;
         hk_ = hk;
      }
            
      public double value(double x) 
      {
         double xs = a_ * (-x + 1);
         xs = Math.Abs(xs*xs);
         double rs = Math.Sqrt(1 - xs);
         double asr = -(bs_ / xs + hk_) / 2;
         if (asr > -100.0) 
         {
            return (a_ * Math.Exp(asr) *
                     (Math.Exp(-hk_ * (1 - rs) / (2 * (1 + rs))) / rs -
                     (1 + c_ * xs * (1 + d_ * xs))));
         } 
         else 
         {
            return 0.0;
         }
            
      }
      
      private double a_, c_, d_, bs_, hk_;
        
   }


}
