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
using System.Numerics;

namespace QLNet
{
   //! analytic Heston-model engine based on Fourier transform

   /*! Integration detail:
       Two algebraically equivalent formulations of the complex
       logarithm of the Heston model exist. Gatherals [2005]
       (also Duffie, Pan and Singleton [2000], and Schoutens,
       Simons and Tistaert[2004]) version does not cause
       discoutinuities whereas the original version (e.g. Heston [1993])
       needs some sort of "branch correction" to work properly.
       Gatheral's version does also work with adaptive integration
       routines and should be preferred over the original Heston version.
   */

   /*! References:

       Heston, Steven L., 1993. A Closed-Form Solution for Options
       with Stochastic Volatility with Applications to Bond and
       Currency Options.  The review of Financial Studies, Volume 6,
       Issue 2, 327-343.

       A. Sepp, Pricing European-Style Options under Jump Diffusion
       Processes with Stochastic Volatility: Applications of Fourier
       Transform (<http://math.ut.ee/~spartak/papers/stochjumpvols.pdf>)

       R. Lord and C. Kahl, Why the rotation count algorithm works,
       http://papers.ssrn.com/sol3/papers.cfm?abstract_id=921335

       H. Albrecher, P. Mayer, W.Schoutens and J. Tistaert,
       The Little Heston Trap, http://www.schoutens.be/HestonTrap.pdf

       J. Gatheral, The Volatility Surface: A Practitioner's Guide,
       Wiley Finance

       \ingroup vanillaengines

       \test the correctness of the returned value is tested by
             reproducing results available in web/literature
             and comparison with Black pricing.
   */
   public class AnalyticHestonEngine : GenericModelEngine<HestonModel, VanillaOption.Arguments,VanillaOption.Results>
   {
      private class integrand1 
      {
         private double c_inf;
         private Func<double,double> f;
          
         public integrand1(double _c_inf, Func<double,double> _f)
         {
            c_inf = _c_inf;
            f = _f;
         }
            
         public double value(double x) 
         {
            if ((x + 1.0)*c_inf > Const.QL_EPSILON)
            {
               return f(-Math.Log(0.5*x + 0.5)/c_inf)/((x + 1.0)*c_inf);
            }
            else
            {
               return 0.0;
            }
            
         }
      }
      private class integrand2
      {
         private double c_inf;
         private Func<double, double> f;

         public integrand2( double _c_inf, Func<double, double> _f )
         {
            c_inf = _c_inf;
            f = _f;
         }

         public double value( double x )
         {
            if (x*c_inf > Const.QL_EPSILON)
            {
               return f(-Math.Log(x)/c_inf)/(x*c_inf);
            }
            else
            {
               return 0.0;
            }

         }
      }
      public class Integration
      {
         // non adaptive integration algorithms based on Gaussian quadrature
         public static Integration gaussLaguerre    (int intOrder = 128)
         {
            Utils.QL_REQUIRE(intOrder <= 192,()=> "maximum integraton order (192) exceeded");
            return new Integration(Algorithm.GaussLaguerre, new GaussLaguerreIntegration(intOrder));
         }

         public static Integration gaussLegendre  (int intOrder = 128)
         {
            return new Integration(Algorithm.GaussLegendre, new GaussLegendreIntegration(intOrder));   
         }

         public static Integration gaussChebyshev( int intOrder = 128 )
         {
            return new Integration(Algorithm.GaussChebyshev, new GaussChebyshevIntegration(intOrder));   
         }

         public static Integration gaussChebyshev2nd( int intOrder = 128 )
         {
            return new Integration(Algorithm.GaussChebyshev2nd, new GaussChebyshev2ndIntegration(intOrder));
         }

         // for an adaptive integration algorithm Gatheral's version has to
         // be used.Be aware: using a too large number for maxEvaluations might
         // result in a stack overflow as the these integrations are based on
         // recursive algorithms.
         public static Integration gaussLobatto(double relTolerance, double? absTolerance,int maxEvaluations = 1000)
         {
            return new Integration(Algorithm.GaussLobatto, new GaussLobattoIntegral( maxEvaluations, 
               absTolerance,relTolerance,false));
         }

         // usually these routines have a poor convergence behavior.
         public static Integration gaussKronrod(double absTolerance, int maxEvaluations = 1000)
         {
            return new Integration(Algorithm.GaussKronrod, new GaussKronrodAdaptive(absTolerance,maxEvaluations));
         }

         public static Integration simpson(double absTolerance, int maxEvaluations = 1000)
         {
             return new Integration(Algorithm.Simpson, new SimpsonIntegral(absTolerance,maxEvaluations));   
         }

         public static Integration trapezoid(double absTolerance, int maxEvaluations = 1000)
         {
             return new Integration(Algorithm.Trapezoid, new TrapezoidIntegral<Default>(absTolerance,maxEvaluations));
         }

         public double calculate(double c_inf, Func<double, double> f)
         {
            double retVal = 0;

            switch ( intAlgo_ )
            {
               case Algorithm.GaussLaguerre:
                  retVal = gaussianQuadrature_.value( f );
                  break;
               case Algorithm.GaussLegendre:
               case Algorithm.GaussChebyshev:
               case Algorithm.GaussChebyshev2nd:
                  retVal = gaussianQuadrature_ .value( new integrand1( c_inf, f ).value );
                  break;
               case Algorithm.Simpson:
               case Algorithm.Trapezoid:
               case Algorithm.GaussLobatto:
               case Algorithm.GaussKronrod:
                  retVal = integrator_.value(new integrand2( c_inf, f ).value,0.0, 1.0 );
                  break;
               default:
                  Utils.QL_FAIL( "unknwon integration algorithm" );
                  break;
            }

            return retVal;
         }

         public int numberOfEvaluations()
         {
            if ( integrator_ != null )
            {
               return integrator_.numberOfEvaluations();
            }
            else if ( gaussianQuadrature_ != null  )
            {
               return gaussianQuadrature_.order();
            }
            else
            {
               Utils.QL_FAIL( "neither Integrator nor GaussianQuadrature given" );
            }
            return 0; // jfc
         }

         public bool isAdaptiveIntegration()
         {
            return intAlgo_ == Algorithm.GaussLobatto
                || intAlgo_ == Algorithm.GaussKronrod
                || intAlgo_ == Algorithm.Simpson
                || intAlgo_ == Algorithm.Trapezoid;
         }

         private enum Algorithm
         { 
            GaussLobatto, 
            GaussKronrod, 
            Simpson, 
            Trapezoid,
            GaussLaguerre, 
            GaussLegendre,
            GaussChebyshev, 
            GaussChebyshev2nd 
         }

         private Integration( Algorithm intAlgo, GaussianQuadrature gaussianQuadrature )
         {
            intAlgo_ = intAlgo;
            gaussianQuadrature_ = gaussianQuadrature;
         }

         private Integration(Algorithm intAlgo, Integrator integrator)
         {
            intAlgo_ = intAlgo;
            integrator_ = integrator;
         }

         private Algorithm intAlgo_;
         private Integrator integrator_;
         private GaussianQuadrature gaussianQuadrature_;

      }

      public enum ComplexLogFormula { Gatheral, BranchCorrection };

      // Simple to use constructor: Using adaptive
      // Gauss-Lobatto integration and Gatheral's version of complex log.
      // Be aware: using a too large number for maxEvaluations might result
      // in a stack overflow as the Lobatto integration is a recursive
      // algorithm.
      public AnalyticHestonEngine( HestonModel model, double relTolerance, int maxEvaluations )
         :base(model)
      {
         evaluations_ = 0;
         cpxLog_ = ComplexLogFormula.Gatheral;
         integration_ = Integration.gaussLobatto(relTolerance, null, maxEvaluations);
      }

      // Constructor using Laguerre integration
      // and Gatheral's version of complex log.
      public AnalyticHestonEngine(HestonModel model, int integrationOrder = 144)
         :base(model)
      {
         evaluations_ = 0;
         cpxLog_ = ComplexLogFormula.Gatheral;
         integration_ = Integration.gaussLaguerre( integrationOrder );
      }

      // Constructor giving full control
      // over the Fourier integration algorithm
      public AnalyticHestonEngine(HestonModel model, ComplexLogFormula cpxLog, Integration integration)
         :base(model)
      {
         evaluations_ = 0;
         cpxLog_ = cpxLog;
         integration_ = integration; // TODO check
 
         Utils.QL_REQUIRE(   cpxLog_ != ComplexLogFormula.BranchCorrection
                             || !integration.isAdaptiveIntegration(),()=>
                             "Branch correction does not work in conjunction with adaptive integration methods");
      }


      public override void calculate()
      {
         // this is a european option pricer
         Utils.QL_REQUIRE(arguments_.exercise.type() == Exercise.Type.European,()=>"not an European option");

         // plain vanilla
         PlainVanillaPayoff payoff = arguments_.payoff as PlainVanillaPayoff;
         Utils.QL_REQUIRE(payoff != null ,()=> "non plain vanilla payoff given");

         HestonProcess process = model_.link.process();

         double riskFreeDiscount = process.riskFreeRate().link.discount(arguments_.exercise.lastDate());
         double dividendDiscount = process.dividendYield().link.discount(arguments_.exercise.lastDate());

         double spotPrice = process.s0().link.value();
         Utils.QL_REQUIRE(spotPrice > 0.0,()=> "negative or null underlying given");

         double strikePrice = payoff.strike();
         double term = process.time(arguments_.exercise.lastDate());

         doCalculation( riskFreeDiscount,
                        dividendDiscount,
                        spotPrice,
                        strikePrice,
                        term,
                        model_.link.kappa(),
                        model_.link.theta(),
                        model_.link.sigma(),
                        model_.link.v0(),
                        model_.link.rho(),
                        payoff,
                        integration_,
                        cpxLog_,
                        this,
                        ref results_.value,
                        ref evaluations_);
   
      }

      public int numberOfEvaluations() { return evaluations_; }

      public static void doCalculation( double riskFreeDiscount,
                                        double dividendDiscount,
                                        double spotPrice,
                                        double strikePrice,
                                        double term,
                                        double kappa, double theta, double sigma, double v0, double rho,
                                        TypePayoff type,
                                        Integration integration,
                                        ComplexLogFormula cpxLog,
                                        AnalyticHestonEngine  enginePtr,
                                        ref double? value,
                                        ref int evaluations)
      {
                 
         double ratio = riskFreeDiscount/dividendDiscount;

         double c_inf = Math.Min(10.0, Math.Max(0.0001,
                        Math.Sqrt( 1.0 - ( Math.Pow(rho,2) ) ) / sigma ) )
                        *(v0 + kappa*theta*term);

         evaluations = 0;
         double p1 = integration.calculate(c_inf,
            new Fj_Helper(kappa, theta, sigma, v0, spotPrice, rho, enginePtr,
                      cpxLog, term, strikePrice, ratio, 1).value)/Const.M_PI;
         evaluations+= integration.numberOfEvaluations();

         double p2 = integration.calculate(c_inf,
            new Fj_Helper(kappa, theta, sigma, v0, spotPrice, rho, enginePtr,
                      cpxLog, term, strikePrice, ratio, 2).value)/Const.M_PI;
         
         evaluations+= integration.numberOfEvaluations();

         switch (type.optionType())
         {
            case Option.Type.Call:
               value = spotPrice*dividendDiscount*(p1+0.5) - strikePrice*riskFreeDiscount*(p2+0.5);
               break;
            case Option.Type.Put:
               value = spotPrice*dividendDiscount*(p1-0.5) - strikePrice*riskFreeDiscount*(p2-0.5);
               break;
            default:
               Utils.QL_FAIL("unknown option type");
               break;
         }

      }

      
      // call back for extended stochastic volatility
      // plus jump diffusion engines like bates model
      protected virtual Complex addOnTerm( double phi, double t, int j) { return new Complex(0,0); }

      private class Fj_Helper 
      {
         public Fj_Helper( VanillaOption.Arguments arguments,
                           HestonModel model,
                           AnalyticHestonEngine engine,
                           ComplexLogFormula cpxLog,
                           double term, double ratio, int j)
         {
            j_ = j; //arg_(arguments),
            kappa_ = model.kappa();
            theta_ = model.theta();
            sigma_ = model.sigma();
            v0_ = model.v0();
            cpxLog_ = cpxLog;
            term_ = term;
            x_ = Math.Log(model.process().s0().link.value());
            sx_ = Math.Log(  ((StrikedTypePayoff)(arguments.payoff)).strike());
            dd_ = x_ - Math.Log(ratio);
            sigma2_ = sigma_*sigma_;
            rsigma_ = model.rho()*sigma_;
            t0_ = kappa_ - ((j_ == 1) ? model.rho()*sigma_ : 0);
            b_ = 0;
            g_km1_ = 0;
            engine_ = engine;
         }

         public Fj_Helper(double kappa, double theta, double sigma,
                          double v0, double s0, double rho,
                          AnalyticHestonEngine engine,
                          ComplexLogFormula cpxLog,
                          double term,
                          double strike,
                          double ratio,
                          int j)
         {
            j_ = j;
            kappa_ = kappa;
            theta_ = theta;
            sigma_ = sigma;
            v0_ = v0;
            cpxLog_ = cpxLog;
            term_ = term;
            x_ = Math.Log(s0);
            sx_ = Math.Log(strike);
            dd_ = x_-Math.Log(ratio);
            sigma2_ = sigma_*sigma_;
            rsigma_ = rho*sigma_;
            t0_ = kappa - ((j== 1)? rho*sigma : 0);
            b_ = 0;
            g_km1_ = 0;
            engine_ = engine;
         }

         public Fj_Helper(double kappa, double theta, double sigma,
                          double v0, double s0, double rho,
                          ComplexLogFormula cpxLog,
                          double term,
                          double strike,
                          double ratio,
                          int j)
         {
            j_ = j;
            kappa_ = kappa;
            theta_ = theta;
            sigma_ = sigma;
            v0_ = v0;
            cpxLog_ = cpxLog;
            term_ = term;
            x_ = Math.Log(s0);
            sx_ = Math.Log(strike);
            dd_ = x_-Math.Log(ratio);
            sigma2_ = sigma_*sigma_;
            rsigma_ = rho*sigma_;
            t0_ = kappa - ((j== 1)? rho*sigma : 0);
            b_ = 0;
            g_km1_ = 0;
            engine_ = null;
         }

         public double value(double phi)
         {
            double rpsig = rsigma_*phi;

            Complex t1 = t0_+ new Complex(0, -rpsig);
            Complex d = Complex.Sqrt(t1*t1 - sigma2_*phi * new Complex(-phi, (j_== 1)? 1 : -1));
            Complex ex = Complex.Exp(-d*term_);
            Complex addOnTerm = engine_ != null ? engine_.addOnTerm(phi, term_, j_) : 0.0;

            if (cpxLog_ == ComplexLogFormula.Gatheral) 
            {
               if (phi != 0.0) 
               {
                  if (sigma_ > 1e-5) 
                  {
                     Complex p = (t1-d)/(t1+d);
                     Complex g = Complex.Log((1.0 - p*ex)/(1.0 - p));

                     return Complex.Exp(v0_*(t1-d)*(1.0-ex)/(sigma2_*(1.0-ex*p))
                                        + (kappa_*theta_)/sigma2_*((t1-d)*term_-2.0*g)
                                        + new Complex(0.0, phi*(dd_-sx_))
                                        + addOnTerm
                                        ).Imaginary/phi;
                  }
                  else 
                  {
                     Complex td = phi/(2.0*t1) * new Complex(-phi, (j_== 1)? 1 : -1);
                     Complex p = td*sigma2_/(t1+d);
                     Complex g = p*(1.0-ex);

                     return Complex.Exp(v0_*td*(1.0-ex)/(1.0-p*ex)
                                        + (kappa_*theta_)*(td*term_-2.0*g/sigma2_)
                                        + new Complex(0.0, phi*(dd_-sx_))
                                        + addOnTerm
                                        ).Imaginary/phi;
                  }
               }
               else 
               {
                  // use l'Hospital's rule to get lim_{phi->0}
                  if (j_ == 1) 
                  {
                     double kmr = rsigma_-kappa_;
                     if (Math.Abs(kmr) > 1e-7) 
                     {
                        return dd_-sx_
                              + (Math.Exp(kmr*term_)*kappa_*theta_
                                 -kappa_*theta_*(kmr*term_+1.0) ) / (2*kmr*kmr)
                              - v0_*(1.0-Math.Exp(kmr*term_)) / (2.0*kmr);
                     }
                     else
                        // \kappa = \rho * \sigma
                        return dd_-sx_ + 0.25*kappa_*theta_*term_*term_
                                       + 0.5*v0_*term_;
                  }
                  else 
                  {
                     return dd_-sx_
                        - (Math.Exp(-kappa_*term_)*kappa_*theta_
                           +kappa_*theta_*(kappa_*term_-1.0))/(2*kappa_*kappa_)
                        - v0_*(1.0-Math.Exp(-kappa_*term_))/(2*kappa_);
                  }
               }
            }
            else if (cpxLog_ == ComplexLogFormula.BranchCorrection) 
            {
               Complex p  = (t1+d)/(t1 - d);

               // next term: g = std::log((1.0 - p*std::exp(d*term_))/(1.0 - p))
               Complex g = new Complex();

               // the exp of the following expression is needed.
               Complex e = Complex.Log(p)+d*term_;

               // does it fit to the machine precision?
               if (Math.Exp(-e.Real) > Const.QL_EPSILON) 
               {
                  g = Complex.Log((1.0 - p/ex)/(1.0 - p));
               } 
               else 
               {
                  // use a "big phi" approximation
                  g = d*term_ + Complex.Log(p/(p - 1.0));

                  if (g.Imaginary > Const.M_PI || g.Imaginary <= -Const.M_PI) 
                  {
                     // get back to principal branch of the complex logarithm
                     double im = g.Imaginary - (2*Const.M_PI)* Math.Floor(g.Imaginary/2*Const.M_PI);  // TODO Check std::fmod(g.Imaginary, 2*Const.M_PI);
                     if (im > Const.M_PI)
                        im -= 2*Const.M_PI;
                     else if (im <= -Const.M_PI)
                        im += 2*Const.M_PI;

                     g = new Complex(g.Real, im);
                     }
               }

               // be careful here as we have to use a log branch correction
               // to deal with the discontinuities of the complex logarithm.
               // the principal branch is not always the correct one.
               // (s. A. Sepp, chapter 4)
               // remark: there is still the change that we miss a branch
               // if the order of the integration is not high enough.
               double tmp = g.Imaginary - g_km1_;
               if (tmp <= -Const.M_PI)
                  ++b_;
               else if (tmp > Const.M_PI)
                  --b_;

               g_km1_ = g.Imaginary;
               g += new Complex(0, 2*b_*Const.M_PI);

               return Complex.Exp(v0_*(t1+d)*(ex-1.0)/(sigma2_*(ex-p))
                                  + (kappa_*theta_)/sigma2_*((t1+d)*term_-2.0*g)
                                  + new Complex(0,phi*(dd_-sx_))
                                  + addOnTerm
                                  ).Imaginary/phi;
            
            }
            else 
            {
               Utils.QL_FAIL("unknown complex logarithm formula");
            }
            return 0;
         }

         private int j_;
         //      VanillaOption::arguments& arg_;
         private double kappa_, theta_, sigma_, v0_;
         private ComplexLogFormula cpxLog_;

         // helper variables
         private double term_;
         private double x_, sx_, dd_;
         private double sigma2_, rsigma_;
         private double t0_;

         // log branch counter
         private int  b_;     // log branch counter
         private double g_km1_; // imag part of last log value

         private AnalyticHestonEngine engine_;

      }

      private int evaluations_;
      private ComplexLogFormula cpxLog_;
      private Integration integration_;

   }
}
