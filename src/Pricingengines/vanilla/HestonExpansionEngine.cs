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

namespace QLNet
{
   //! Heston-model engine for European options based on analytic expansions
   /*! References:

       M Forde, A Jacquier, R Lee, The small-time smile and term
       structure of implied volatility under the Heston model
       SIAM Journal on Financial Mathematics, 2012 - SIAM

       M Lorig, S Pagliarani, A Pascucci, Explicit implied vols for
       multifactor local-stochastic vol models
       arXiv preprint arXiv:1306.5447v3, 2014 - arxiv.org

       \ingroup vanillaengines
   */
   public class HestonExpansionEngine : GenericModelEngine<HestonModel,VanillaOption.Arguments,
         VanillaOption.Results>
   {
      public enum HestonExpansionFormula { LPP2, LPP3, Forde };

      public HestonExpansionEngine(HestonModel model,HestonExpansionFormula formula)
         :base(model)
      {
         formula_ = formula;
      }

      public override void calculate()
      {
         // this is a european option pricer
         Utils.QL_REQUIRE(arguments_.exercise.type() == Exercise.Type.European,()=>"not an European option");

         // plain vanilla
         PlainVanillaPayoff payoff = arguments_.payoff as PlainVanillaPayoff;
         Utils.QL_REQUIRE(payoff != null, () => "non plain vanilla payoff given");

         HestonProcess process = model_.link.process();

         double riskFreeDiscount = process.riskFreeRate().link.discount(arguments_.exercise.lastDate());
         double dividendDiscount = process.dividendYield().link.discount(arguments_.exercise.lastDate());

         double spotPrice = process.s0().link.value();
         Utils.QL_REQUIRE(spotPrice > 0.0, () => "negative or null underlying given");

         double strikePrice = payoff.strike();
         double term = process.time(arguments_.exercise.lastDate());

         //possible optimization:
         //  if term=lastTerm & model=lastModel & formula=lastApprox, reuse approx.
         double forward = spotPrice*dividendDiscount/riskFreeDiscount;
         double vol = 0;
         switch(formula_) 
         {
            case HestonExpansionFormula.LPP2: 
            {
               LPP2HestonExpansion expansion = new LPP2HestonExpansion(model_.link.kappa(), model_.link.theta(),
                  model_.link.sigma(), model_.link.v0(),model_.link.rho(), term);
               vol = expansion.impliedVolatility(strikePrice, forward);
               break;
            }
            case HestonExpansionFormula.LPP3: 
            {
               LPP3HestonExpansion expansion = new LPP3HestonExpansion(model_.link.kappa(), model_.link.theta(),
                  model_.link.sigma(), model_.link.v0(),model_.link.rho(), term);
               vol = expansion.impliedVolatility(strikePrice, forward);
               break;
            }
            case HestonExpansionFormula.Forde: 
            {
               FordeHestonExpansion expansion = new FordeHestonExpansion(model_.link.kappa(), model_.link.theta(),
                  model_.link.sigma(), model_.link.v0(),model_.link.rho(), term);
               vol = expansion.impliedVolatility(strikePrice, forward);
               break;
            }
            default:
               Utils.QL_FAIL("unknown expansion formula");
               break;
         }
         double price = Utils.blackFormula(payoff, forward, vol*Math.Sqrt(term),riskFreeDiscount, 0);
         results_.value = price;
      }


      private HestonExpansionFormula formula_;
   }

   /*! Interface to represent some Heston expansion formula.
        During calibration, it would typically be initialized once per
        implied volatility surface slice, then calls for each surface
        strike to impliedVolatility(strike, forward) would be
        performed.
    */
    
   public abstract class HestonExpansion 
   {
      ~HestonExpansion() {}
      public abstract double impliedVolatility(double strike,double forward) ;

   }


   /*! Lorig Pagliarani Pascucci expansion of order-2 for the Heston model.
       During calibration, it can be initialized once per expiry, and
       called many times with different strikes.  The formula is also
       available in the Mathematica notebook from the authors at
       http://explicitsolutions.wordpress.com/
    */

   public class LPP2HestonExpansion : HestonExpansion
   {
      public LPP2HestonExpansion(double kappa, double theta, double sigma, double v0, double rho, double term)
      {
         ekt  = Math.Exp(kappa*term);
         e2kt = ekt*ekt;
         e3kt = e2kt*ekt;
         e4kt = e2kt*e2kt;
         coeffs[0] = z0(term, kappa, theta, sigma, v0, rho);
         coeffs[1] = z1(term, kappa, theta, sigma, v0, rho);
         coeffs[2] = z2(term, kappa, theta, sigma, v0, rho);
      }
      public override double impliedVolatility(double strike, double forward)
      {
         double x = Math.Log(strike/forward);
         double vol = coeffs[0]+x*(coeffs[1]+(x*coeffs[2]));
         return Math.Max(1e-8,vol);
      }

      private double[] coeffs = new double[3];
      private double ekt, e2kt, e3kt, e4kt;
      private double z0(double t, double kappa, double theta, double delta, double y, double rho)
      {
         return (4*Math.Pow(delta,2)*kappa*(-theta - 4*ekt*(theta + kappa*t*(theta - y)) +
            e2kt*((5 - 2*kappa*t)*theta - 2*y) + 2*y)*
            ((1 + ekt*(-1 + kappa*t))*theta + (-1 + ekt)*y) +
            128*ekt*Math.Pow(kappa,3)*
            Math.Pow((1 + ekt*(-1 + kappa*t))*theta + (-1 + ekt)*y,2) +
            32*delta*ekt*Math.Pow(kappa,2)*rho*
            ((1 + ekt*(-1 + kappa*t))*theta + (-1 + ekt)*y)*
            ((2 + kappa*t + ekt*(-2 + kappa*t))*theta +
                (-1 + ekt - kappa*t)*y) +
                Math.Pow(delta,2)*ekt*Math.Pow(rho,2)*
                (-theta + kappa*t*theta + (theta - y)/ekt + y)*
                Math.Pow((2 + kappa*t + ekt*(-2 + kappa*t))*theta +
                    (-1 + ekt - kappa*t)*y,2) +
                    (48*Math.Pow(delta,2)*e2kt*Math.Pow(kappa,2)*Math.Pow(rho,2)*
                        Math.Pow((2 + kappa*t + ekt*(-2 + kappa*t))*theta +
                            (-1 + ekt - kappa*t)*y,2))/
                            ((1 + ekt*(-1 + kappa*t))*theta + (-1 + ekt)*y) -
                            Math.Pow(delta,2)*Math.Pow(rho,2)*((1 + ekt*(-1 + kappa*t))*theta +
                                (-1 + ekt)*y)*Math.Pow((2 + kappa*t + ekt*(-2 + kappa*t))*
                                    theta + (-1 + ekt - kappa*t)*y,2) +
                                    2*Math.Pow(delta,2)*kappa*((1 + ekt*(-1 + kappa*t))*theta +
                                        (-1 + ekt)*y)*(theta - 2*y +
                                            e2kt*(-5*theta + 2*kappa*t*theta + 2*y +
                                                8*Math.Pow(rho,2)*((-3 + kappa*t)*theta + y)) +
                                                4*ekt*(theta + kappa*t*theta - kappa*t*y +
                                                    Math.Pow(rho,2)*((6 + kappa*t*(4 + kappa*t))*theta - (2 + kappa*t*(2 + kappa*t))*y))) -
                                                    (8*Math.Pow(delta,2)*Math.Pow(kappa,2)*((1 + ekt*(-1 + kappa*t))*theta +
                                                        (-1 + ekt)*y)*(theta - 2*y +
                                                            e2kt*(-5*theta + 2*kappa*t*theta + 2*y +
                                                                8*Math.Pow(rho,2)*((-3 + kappa*t)*theta + y)) +
                                                                4*ekt*(theta + kappa*t*theta - kappa*t*y +
                                                                    Math.Pow(rho,2)*((6 + kappa*t*(4 + kappa*t))*theta - (2 + kappa*t*(2 + kappa*t))*y))))
                                                                    /(-theta + kappa*t*theta + (theta - y)/ekt + y))/
                                                                    (128.0*e3kt*Math.Pow(kappa,5)*Math.Pow(t,2)*
                                                                        Math.Pow((-theta + kappa*t*theta + (theta - y)/ekt + y)/(kappa*t),1.5));

      }
      private double z1(double t, double kappa, double theta, double delta, double y, double rho)
      {
         return (delta*rho*(-(delta*Math.Pow(-1 + ekt,2)*rho*(4*theta - y)*y) +
            2*ekt*Math.Pow(kappa,3)*Math.Pow(t,2)*theta*
            ((2 + 2*ekt + delta*rho*t)*theta - (2 + delta*rho*t)*y) -
            2*(-1 + ekt)*kappa*(2*theta - y)*
            ((-1 + ekt)*(-2 + delta*rho*t)*theta +
                (-2 + 2*ekt + delta*rho*t)*y) +
                Math.Pow(kappa,2)*t*((-1 + ekt)*
                    (-4 + delta*rho*t + ekt*(-12 + delta*rho*t))*Math.Pow(theta,2) +
                    2*(-4 + 4*e2kt + delta*rho*t + 3*delta*ekt*rho*t)*theta*
                    y - (-4 + delta*rho*t + 2*ekt*(2 + delta*rho*t))*Math.Pow(y,2))))/
                    (8.0*Math.Pow(kappa,2)*t*Math.Sqrt((-theta + kappa*t*theta + (theta - y)/ekt + y)/
                        (kappa*t))*Math.Pow((1 + ekt*(-1 + kappa*t))*theta + (-1 + ekt)*y,
                            2));
      }
      private double z2(double t, double kappa, double theta, double delta, double y, double rho)
      {
         return (Math.Pow(delta,2)*Math.Sqrt((-theta + kappa*t*theta + (theta - y)/ekt + y)/(kappa*t))*
            (-12*Math.Pow(rho,2)*Math.Pow((2 + kappa*t + ekt*(-2 + kappa*t))*theta +
                (-1 + ekt - kappa*t)*y,2) +
                (-theta + kappa*t*theta + (theta - y)/ekt + y)*
                (theta - 2*y + e2kt*
                    (-5*theta + 2*kappa*t*theta + 2*y + 8*Math.Pow(rho,2)*((-3 + kappa*t)*theta + y)) +
                    4*ekt*(theta + kappa*t*theta - kappa*t*y +
                        Math.Pow(rho,2)*((6 + kappa*t*(4 + kappa*t))*theta - (2 + kappa*t*(2 + kappa*t))*y))))
            )/(16.0*e2kt*Math.Pow(-theta + kappa*t*theta + (theta - y)/ekt + y,
                4));
      }
   }

      
   /*! Lorig Pagliarani Pascucci expansion of order-3 for the Heston model.
       During calibration, it can be initialized once per expiry, and
       called many times with different strikes.  The formula is also
       available in the Mathematica notebook from the authors at
       http://explicitsolutions.wordpress.com/
   */
   public class LPP3HestonExpansion : HestonExpansion
   {
      public LPP3HestonExpansion(double kappa, double theta,double sigma, double v0,double rho, double term)
      {
         ekt = Math.Exp( kappa * term );
         e2kt = ekt * ekt;
         e3kt = e2kt * ekt;
         e4kt = e2kt * e2kt;
         coeffs[0] = z0( term, kappa, theta, sigma, v0, rho );
         coeffs[1] = z1( term, kappa, theta, sigma, v0, rho );
         coeffs[2] = z2( term, kappa, theta, sigma, v0, rho );
         coeffs[3] = z3( term, kappa, theta, sigma, v0, rho );
      }
      public override double impliedVolatility(double strike,double forward)
      {
         double x = Math.Log(strike/forward);
         double vol = coeffs[0]+x*(coeffs[1]+x*(coeffs[2]+x*(coeffs[3])));
         return Math.Max(1e-8,vol);
      }
      
      private double[] coeffs = new double[4];
      private double ekt, e2kt, e3kt, e4kt;
      private double z0(double t, double kappa, double theta,double delta, double y, double rho)
      {
         return (96*Math.Pow(delta, 2)*ekt*Math.Pow(kappa, 3)*
                 (-theta - 4*ekt*(theta + kappa*t*(theta - y)) +
                  e2kt*((5 - 2*kappa*t)*theta - 2*y) + 2*y)*
                 ((1 + ekt*(-1 + kappa*t))*theta + (-1 + ekt)*y) +
                 3072*e2kt*Math.Pow(kappa, 5)*
                 Math.Pow((1 + ekt*(-1 + kappa*t))*theta + (-1 + ekt)*y, 2) +
                 96*Math.Pow(delta, 3)*ekt*Math.Pow(kappa, 2)*rho*
                 ((1 + ekt*(-1 + kappa*t))*theta + (-1 + ekt)*y)*
                 (-2*theta - kappa*t*theta - 2*ekt*(2 + kappa*t)*
                  (2*theta + kappa*t*(theta - y)) + e2kt*((10 - 3*kappa*t)*theta - 3*y) +
                  3*y + 2*kappa*t*y) + 768*delta*e2kt*Math.Pow(kappa, 4)*rho*
                 ((1 + ekt*(-1 + kappa*t))*theta + (-1 + ekt)*y)*
                 ((2 + kappa*t + ekt*(-2 + kappa*t))*theta +
                  (-1 + ekt - kappa*t)*y) +
                 6*Math.Pow(delta, 3)*kappa*rho*(-theta - 4*ekt*(theta + kappa*t*(theta - y)) +
                                            e2kt*((5 - 2*kappa*t)*theta - 2*y) + 2*y)*
                 ((1 + ekt*(-1 + kappa*t))*theta + (-1 + ekt)*y)*
                 ((2 + kappa*t + ekt*(-2 + kappa*t))*theta +
                  (-1 + ekt - kappa*t)*y) +
                 24*Math.Pow(delta, 2)*e2kt*Math.Pow(kappa, 2)*Math.Pow(rho, 2)*
                 (-theta + kappa*t*theta + (theta - y)/ekt + y)*
                 Math.Pow((2 + kappa*t + ekt*(-2 + kappa*t))*theta +
                     (-1 + ekt - kappa*t)*y, 2) +
                 (1152*Math.Pow(delta, 2)*e3kt*Math.Pow(kappa, 4)*Math.Pow(rho, 2)*
                  Math.Pow((2 + kappa*t + ekt*(-2 + kappa*t))*theta +
                      (-1 + ekt - kappa*t)*y, 2))/
                 ((1 + ekt*(-1 + kappa*t))*theta + (-1 + ekt)*y) -
                 24*Math.Pow(delta, 2)*ekt*Math.Pow(kappa, 2)*Math.Pow(rho, 2)*
                 ((1 + ekt*(-1 + kappa*t))*theta + (-1 + ekt)*y)*
                 Math.Pow((2 + kappa*t + ekt*(-2 + kappa*t))*theta +
                     (-1 + ekt - kappa*t)*y, 2) +
                 80*Math.Pow(delta, 3)*ekt*kappa*Math.Pow(rho, 3)*
                 Math.Pow((2 + kappa*t + ekt*(-2 + kappa*t))*theta +
                     (-1 + ekt - kappa*t)*y, 3) +
                 Math.Pow(delta, 3)*ekt*Math.Pow(rho, 3)*
                 (-theta + kappa*t*theta + (theta - y)/ekt + y)*
                 Math.Pow((2 + kappa*t + ekt*(-2 + kappa*t))*theta +
                     (-1 + ekt - kappa*t)*y, 3) -
                 (1440*Math.Pow(delta, 3)*e3kt*Math.Pow(kappa, 3)*Math.Pow(rho, 3)*
                  Math.Pow((2 + kappa*t + ekt*(-2 + kappa*t))*theta +
                      (-1 + ekt - kappa*t)*y, 3))/
                 Math.Pow((1 + ekt*(-1 + kappa*t))*theta + (-1 + ekt)*y, 2) -
                 (528*Math.Pow(delta, 3)*e2kt*Math.Pow(kappa, 2)*Math.Pow(rho, 3)*
                  Math.Pow((2 + kappa*t + ekt*(-2 + kappa*t))*theta +
                      (-1 + ekt - kappa*t)*y, 3))/
                 ((1 + ekt*(-1 + kappa*t))*theta + (-1 + ekt)*y) -
                 3*Math.Pow(delta, 3)*Math.Pow(rho, 3)*((1 + ekt*(-1 + kappa*t))*theta +
                                              (-1 + ekt)*y)*Math.Pow((2 + kappa*t + ekt*(-2 + kappa*t))*
                                                                theta + (-1 + ekt - kappa*t)*y, 3) +
                 384*Math.Pow(delta, 3)*e2kt*Math.Pow(kappa, 3)*rho*
                 ((2 + kappa*t + 2*ekt*Math.Pow(2 + kappa*t, 2) +
                   e2kt*(-10 + 3*kappa*t))*theta +
                  (-3 + 3*e2kt - 2*kappa*t - 2*ekt*kappa*t*(2 + kappa*t))*y) -
                 (576*Math.Pow(delta, 3)*e2kt*Math.Pow(kappa, 3)*rho*
                  ((2 + kappa*t + ekt*(-2 + kappa*t))*theta +
                   (-1 + ekt - kappa*t)*y)*
                  ((1 + e2kt*(-5 + 2*kappa*t + 4*Math.Pow(rho, 2)*(-3 + kappa*t)) +
                    2*ekt*(2 + 2*kappa*t +
                           Math.Pow(rho, 2)*(6 + 4*kappa*t + Math.Pow(kappa, 2)*Math.Pow(t, 2))))*theta +
                   2*(-1 + e2kt*(1 + 2*Math.Pow(rho, 2)) -
                      ekt*(2*kappa*t +
                           Math.Pow(rho, 2)*(2 + 2*kappa*t + Math.Pow(kappa, 2)*Math.Pow(t, 2))))*y))/
                 ((1 + ekt*(-1 + kappa*t))*theta + (-1 + ekt)*y) +
                 Math.Pow(delta, 3)*rho*((1 + ekt*(-1 + kappa*t))*theta +
                                    (-1 + ekt)*y)*((2 + kappa*t + ekt*(-2 + kappa*t))*theta +
                                                   (-1 + ekt - kappa*t)*y)*
                 (theta*(12*ekt*Math.Pow(kappa, 3)*Math.Pow(rho, 2)*Math.Pow(t, 2) +
                         8*Math.Pow(-1 + ekt, 2)*Math.Pow(rho, 2)*theta -
                         (-1 + ekt)*kappa*
                         (3 + 8*Math.Pow(rho, 2)*t*theta + ekt*(15 + 8*Math.Pow(rho, 2)*(9 + t*theta)))
                         + 2*Math.Pow(kappa, 2)*t*(Math.Pow(rho, 2)*t*theta +
                                              2*ekt*(3 + Math.Pow(rho, 2)*(12 + t*theta)) +
                                              e2kt*(3 + Math.Pow(rho, 2)*(12 + t*theta)))) -
                  2*(6*ekt*Math.Pow(kappa, 3)*Math.Pow(rho, 2)*Math.Pow(t, 2) +
                     4*Math.Pow(-1 + ekt, 2)*Math.Pow(rho, 2)*theta +
                     2*Math.Pow(kappa, 2)*t*(Math.Pow(rho, 2)*t*theta +
                                        ekt*(3 + Math.Pow(rho, 2)*(6 + t*theta))) -
                     (-1 + ekt)*kappa*
                     (3 + 6*Math.Pow(rho, 2)*t*theta + ekt*(3 + 2*Math.Pow(rho, 2)*(6 + t*theta))))*
                  y + 2*Math.Pow(rho, 2)*Math.Pow(1 - ekt + kappa*t, 2)*Math.Pow(y, 2)) -
                 (40*Math.Pow(delta, 3)*kappa*rho*((1 + ekt*(-1 + kappa*t))*theta +
                                              (-1 + ekt)*y)*((2 + kappa*t + ekt*(-2 + kappa*t))*theta +
                                                             (-1 + ekt - kappa*t)*y)*
                  (theta*(12*ekt*Math.Pow(kappa, 3)*Math.Pow(rho, 2)*Math.Pow(t, 2) +
                          8*Math.Pow(-1 + ekt, 2)*Math.Pow(rho, 2)*theta -
                          (-1 + ekt)*kappa*
                          (3 + 8*Math.Pow(rho, 2)*t*theta +
                           ekt*(15 + 8*Math.Pow(rho, 2)*(9 + t*theta))) +
                          2*Math.Pow(kappa, 2)*t*(Math.Pow(rho, 2)*t*theta +
                                             2*ekt*(3 + Math.Pow(rho, 2)*(12 + t*theta)) +
                                             e2kt*(3 + Math.Pow(rho, 2)*(12 + t*theta)))) -
                   2*(6*ekt*Math.Pow(kappa, 3)*Math.Pow(rho, 2)*Math.Pow(t, 2) +
                      4*Math.Pow(-1 + ekt, 2)*Math.Pow(rho, 2)*theta +
                      2*Math.Pow(kappa, 2)*t*(Math.Pow(rho, 2)*t*theta +
                                         ekt*(3 + Math.Pow(rho, 2)*(6 + t*theta))) -
                      (-1 + ekt)*kappa*
                      (3 + 6*Math.Pow(rho, 2)*t*theta + ekt*(3 + 2*Math.Pow(rho, 2)*(6 + t*theta)))
                      )*y + 2*Math.Pow(rho, 2)*Math.Pow(1 - ekt + kappa*t, 2)*Math.Pow(y, 2)))/
                 (-theta + kappa*t*theta + (theta - y)/ekt + y) -
                 12*Math.Pow(delta, 3)*kappa*rho*((1 + ekt*(-1 + kappa*t))*theta +
                                             (-1 + ekt)*y)*(2*theta + kappa*t*theta - y - kappa*t*y +
                                                            ekt*((-2 + kappa*t)*theta + y))*
                 (theta - 2*y + e2kt*
                  (-5*theta + 2*kappa*t*theta + 2*y + 4*Math.Pow(rho, 2)*((-3 + kappa*t)*theta + y)) +
                  2*ekt*(2*(theta + kappa*t*(theta - y)) +
                         Math.Pow(rho, 2)*((6 + kappa*t*(4 + kappa*t))*theta - (2 + kappa*t*(2 + kappa*t))*y))) +
                 (288*Math.Pow(delta, 3)*Math.Pow(kappa, 2)*rho*
                  ((1 + ekt*(-1 + kappa*t))*theta + (-1 + ekt)*y)*
                  (2*theta + kappa*t*theta - y - kappa*t*y + ekt*((-2 + kappa*t)*theta + y))*
                  (theta - 2*y + e2kt*
                   (-5*theta + 2*kappa*t*theta + 2*y + 4*Math.Pow(rho, 2)*((-3 + kappa*t)*theta + y)) +
                   2*ekt*(2*(theta + kappa*t*(theta - y)) +
                          Math.Pow(rho, 2)*((6 + kappa*t*(4 + kappa*t))*theta - (2 + kappa*t*(2 + kappa*t))*y))))
                 /(-theta + kappa*t*theta + (theta - y)/ekt + y) +
                 48*Math.Pow(delta, 2)*ekt*Math.Pow(kappa, 3)*
                 ((1 + ekt*(-1 + kappa*t))*theta + (-1 + ekt)*y)*
                 (theta - 2*y + e2kt*
                  (-5*theta + 2*kappa*t*theta + 2*y + 8*Math.Pow(rho, 2)*((-3 + kappa*t)*theta + y)) +
                  4*ekt*(theta + kappa*t*theta - kappa*t*y +
                         Math.Pow(rho, 2)*((6 + kappa*t*(4 + kappa*t))*theta - (2 + kappa*t*(2 + kappa*t))*y))) -
                 (192*Math.Pow(delta, 2)*ekt*Math.Pow(kappa, 4)*
                  ((1 + ekt*(-1 + kappa*t))*theta + (-1 + ekt)*y)*
                  (theta - 2*y + e2kt*
                   (-5*theta + 2*kappa*t*theta + 2*y + 8*Math.Pow(rho, 2)*((-3 + kappa*t)*theta + y)) +
                   4*ekt*(theta + kappa*t*theta - kappa*t*y +
                          Math.Pow(rho, 2)*((6 + kappa*t*(4 + kappa*t))*theta - (2 + kappa*t*(2 + kappa*t))*y))))
                 /(-theta + kappa*t*theta + (theta - y)/ekt + y) +
                 3*Math.Pow(delta, 3)*kappa*rho*((1 + ekt*(-1 + kappa*t))*theta +
                                            (-1 + ekt)*y)*((2 + kappa*t + ekt*(-2 + kappa*t))*theta +
                                                           (-1 + ekt - kappa*t)*y)*
                 (theta - 2*y + e2kt*
                  (-5*theta + 2*kappa*t*theta + 2*y + 8*Math.Pow(rho, 2)*((-3 + kappa*t)*theta + y)) +
                  4*ekt*(theta + kappa*t*theta - kappa*t*y +
                         Math.Pow(rho, 2)*((6 + kappa*t*(4 + kappa*t))*theta - (2 + kappa*t*(2 + kappa*t))*y))) -
                 (12*Math.Pow(delta, 3)*Math.Pow(kappa, 2)*rho*
                  ((1 + ekt*(-1 + kappa*t))*theta + (-1 + ekt)*y)*
                  ((2 + kappa*t + ekt*(-2 + kappa*t))*theta +
                   (-1 + ekt - kappa*t)*y)*
                  (theta - 2*y + e2kt*
                   (-5*theta + 2*kappa*t*theta + 2*y + 8*Math.Pow(rho, 2)*((-3 + kappa*t)*theta + y)) +
                   4*ekt*(theta + kappa*t*theta - kappa*t*y +
                          Math.Pow(rho, 2)*((6 + kappa*t*(4 + kappa*t))*theta - (2 + kappa*t*(2 + kappa*t))*y))))
                 /(-theta + kappa*t*theta + (theta - y)/ekt + y) +
                 4*Math.Pow(delta, 3)*kappa*rho*((1 + ekt*(-1 + kappa*t))*theta +
                                            (-1 + ekt)*y)*(3*(theta - 2*y)*((2 + kappa*t)*theta - (1 + kappa*t)*y) +
                                                           3*ekt*(6*Math.Pow(theta, 2) + theta*y - 2*Math.Pow(y, 2) +
                                                                  kappa*
                                                                  (13*t*Math.Pow(theta, 2) + theta*(8 - 18*t*y) +
                                                                   4*y*(-3 + t*y)) +
                                                                  4*Math.Pow(kappa, 2)*t*
                                                                  (theta + t*Math.Pow(theta, 2) - 2*t*theta*y + y*(-2 + t*y))) +
                                                           3*e3kt*(10*Math.Pow(theta, 2) +
                                                                   2*Math.Pow(kappa, 2)*t*theta*(6 + 8*Math.Pow(rho, 2) + t*theta) -
                                                                   9*theta*y + 2*Math.Pow(y, 2) +
                                                                   kappa*(-9*t*Math.Pow(theta, 2) + 4*(3 + 4*Math.Pow(rho, 2))*y +
                                                                          theta*(-40 - 64*Math.Pow(rho, 2) + 4*t*y))) +
                                                           e2kt*(-54*Math.Pow(theta, 2) +
                                                                 8*Math.Pow(kappa, 4)*Math.Pow(rho, 2)*Math.Pow(t, 3)*(theta - y) +
                                                                 39*theta*y - 6*Math.Pow(y, 2) +
                                                                 24*Math.Pow(kappa, 3)*Math.Pow(t, 2)*
                                                                 (theta + 2*Math.Pow(rho, 2)*theta - (1 + Math.Pow(rho, 2))*y) +
                                                                 6*Math.Pow(kappa, 2)*t*
                                                                 (3*t*Math.Pow(theta, 2) - 8*(1 + Math.Pow(rho, 2))*y +
                                                                  theta*(16 + 24*Math.Pow(rho, 2) - 3*t*y)) -
                                                                 3*kappa*
                                                                 (5*t*Math.Pow(theta, 2) + 2*y*(8*Math.Pow(rho, 2) + 3*t*y) -
                                                                  theta*(32 + 64*Math.Pow(rho, 2) + 17*t*y)))) -
                 (48*Math.Pow(delta, 3)*Math.Pow(kappa, 2)*rho*
                  ((1 + ekt*(-1 + kappa*t))*theta + (-1 + ekt)*y)*
                  (3*(theta - 2*y)*((2 + kappa*t)*theta - (1 + kappa*t)*y) +
                   3*ekt*(6*Math.Pow(theta, 2) + theta*y - 2*Math.Pow(y, 2) +
                          kappa*(13*t*Math.Pow(theta, 2) + theta*(8 - 18*t*y) + 4*y*(-3 + t*y)) +
                          4*Math.Pow(kappa, 2)*t*(theta + t*Math.Pow(theta, 2) - 2*t*theta*y + y*(-2 + t*y))) +
                   3*e3kt*(10*Math.Pow(theta, 2) +
                           2*Math.Pow(kappa, 2)*t*theta*(6 + 8*Math.Pow(rho, 2) + t*theta) - 9*theta*y +
                           2*Math.Pow(y, 2) + kappa*(-9*t*Math.Pow(theta, 2) + 4*(3 + 4*Math.Pow(rho, 2))*y +
                                                theta*(-40 - 64*Math.Pow(rho, 2) + 4*t*y))) +
                   e2kt*(-54*Math.Pow(theta, 2) +
                         8*Math.Pow(kappa, 4)*Math.Pow(rho, 2)*Math.Pow(t, 3)*(theta - y) + 39*theta*y - 6*Math.Pow(y, 2) +
                         24*Math.Pow(kappa, 3)*Math.Pow(t, 2)*
                         (theta + 2*Math.Pow(rho, 2)*theta - (1 + Math.Pow(rho, 2))*y) +
                         6*Math.Pow(kappa, 2)*t*(3*t*Math.Pow(theta, 2) - 8*(1 + Math.Pow(rho, 2))*y +
                                            theta*(16 + 24*Math.Pow(rho, 2) - 3*t*y)) -
                         3*kappa*(5*t*Math.Pow(theta, 2) + 2*y*(8*Math.Pow(rho, 2) + 3*t*y) -
                                  theta*(32 + 64*Math.Pow(rho, 2) + 17*t*y)))))/
                 (-theta + kappa*t*theta + (theta - y)/ekt + y) +
                 (240*Math.Pow(delta, 3)*e2kt*Math.Pow(kappa, 2)*rho*
                  ((2 + kappa*t + ekt*(-2 + kappa*t))*theta +
                   (-1 + ekt - kappa*t)*y)*
                  (12*ekt*Math.Pow(kappa, 3)*Math.Pow(rho, 2)*Math.Pow(t, 2)*(theta - y) +
                   2*Math.Pow(-1 + ekt, 2)*Math.Pow(rho, 2)*Math.Pow(-2*theta + y, 2) -
                   (-1 + ekt)*kappa*
                   (8*(1 + ekt)*Math.Pow(rho, 2)*t*Math.Pow(theta, 2) +
                    2*y*(-3 - 3*ekt*(1 + 4*Math.Pow(rho, 2)) + 2*Math.Pow(rho, 2)*t*y) +
                    theta*(3 - 12*Math.Pow(rho, 2)*t*y + ekt*(15 + Math.Pow(rho, 2)*(72 - 4*t*y)))
                      ) + 2*Math.Pow(kappa, 2)*t*(e2kt*theta*
                                             (3 + Math.Pow(rho, 2)*(12 + t*theta)) + Math.Pow(rho, 2)*t*Math.Pow(theta - y, 2) +
                                             2*ekt*(Math.Pow(rho, 2)*t*Math.Pow(theta, 2) - 3*(y + 2*Math.Pow(rho, 2)*y) +
                                                    theta*(3 + Math.Pow(rho, 2)*(12 - t*y))))))/
                 ((1 + ekt*(-1 + kappa*t))*theta + (-1 + ekt)*y))/
                (3072.0*e4kt*Math.Pow(kappa, 7)*Math.Pow(t, 2)*
         Math.Pow((-theta + kappa*t*theta + (theta - y)/ekt + y)/(kappa*t), 1.5));
      }
      private double z1(double t, double kappa, double theta,double delta, double y, double rho)
      {
         return (delta*(768*e2kt*Math.Pow(kappa, 4)*rho*
                        ((2 + kappa*t + ekt*(-2 + kappa*t))*theta +
                         (-1 + ekt - kappa*t)*y) -
                        (576*delta*e2kt*Math.Pow(kappa, 3)*Math.Pow(rho, 2)*
                         Math.Pow((2 + kappa*t + ekt*(-2 + kappa*t))*theta +
                             (-1 + ekt - kappa*t)*y, 2))/
                        ((1 + ekt*(-1 + kappa*t))*theta + (-1 + ekt)*y) -
                        10*Math.Pow(delta, 2)*Math.Pow(rho, 3)*Math.Pow((2 + kappa*t + ekt*(-2 + kappa*t))*
                                                         theta + (-1 + ekt - kappa*t)*y, 3) +
                        (6*Math.Pow(delta, 2)*kappa*Math.Pow(rho, 3)*
                         Math.Pow((2 + kappa*t + ekt*(-2 + kappa*t))*theta +
                             (-1 + ekt - kappa*t)*y, 3))/
                        (-theta + kappa*t*theta + (theta - y)/ekt + y) -
                        (3360*Math.Pow(delta, 2)*e3kt*Math.Pow(kappa, 3)*Math.Pow(rho, 3)*
                         Math.Pow((2 + kappa*t + ekt*(-2 + kappa*t))*theta +
                             (-1 + ekt - kappa*t)*y, 3))/
                        Math.Pow((1 + ekt*(-1 + kappa*t))*theta + (-1 + ekt)*y, 3) -
                        (288*Math.Pow(delta, 2)*e2kt*Math.Pow(kappa, 2)*Math.Pow(rho, 3)*
                         Math.Pow((2 + kappa*t + ekt*(-2 + kappa*t))*theta +
                             (-1 + ekt - kappa*t)*y, 3))/
                        Math.Pow((1 + ekt*(-1 + kappa*t))*theta + (-1 + ekt)*y, 2) +
                        (234*Math.Pow(delta, 2)*ekt*kappa*Math.Pow(rho, 3)*
                         Math.Pow((2 + kappa*t + ekt*(-2 + kappa*t))*theta +
                             (-1 + ekt - kappa*t)*y, 3))/
                        ((1 + ekt*(-1 + kappa*t))*theta + (-1 + ekt)*y) -
                        96*delta*ekt*Math.Pow(kappa, 3)*
                        ((1 + 4*ekt*(1 + kappa*t) + e2kt*(-5 + 2*kappa*t))*theta +
                         2*(-1 + e2kt - 2*ekt*kappa*t)*y) -
                        12*Math.Pow(delta, 2)*kappa*rho*((2 + kappa*t + ekt*(-2 + kappa*t))*theta +
                                                    (-1 + ekt - kappa*t)*y)*
                        ((1 + 4*ekt*(1 + kappa*t) + e2kt*(-5 + 2*kappa*t))*theta +
                         2*(-1 + e2kt - 2*ekt*kappa*t)*y) -
                        192*Math.Pow(delta, 2)*ekt*Math.Pow(kappa, 2)*rho*
                        ((2 + kappa*t + 2*ekt*Math.Pow(2 + kappa*t, 2) +
                          e2kt*(-10 + 3*kappa*t))*theta +
                         (-3 + 3*e2kt - 2*kappa*t - 2*ekt*kappa*t*(2 + kappa*t))*y)
                        - (12*Math.Pow(delta, 2)*ekt*Math.Pow(kappa, 2)*rho*
                           ((2 + kappa*t + ekt*(-2 + kappa*t))*theta +
                            (-1 + ekt - kappa*t)*y)*
                           ((1 + e2kt*(-5 + 2*kappa*t + 8*Math.Pow(rho, 2)*(-3 + kappa*t)) +
                             4*ekt*(1 + kappa*t +
                                    Math.Pow(rho, 2)*(6 + 4*kappa*t + Math.Pow(kappa, 2)*Math.Pow(t, 2))))*theta +
                            2*(-1 + e2kt*(1 + 4*Math.Pow(rho, 2)) -
                               2*ekt*(kappa*t +
                                      Math.Pow(rho, 2)*(2 + 2*kappa*t + Math.Pow(kappa, 2)*Math.Pow(t, 2))))*y))/
                        ((1 + ekt*(-1 + kappa*t))*theta + (-1 + ekt)*y) +
                        (576*Math.Pow(delta, 2)*ekt*Math.Pow(kappa, 2)*rho*
                         ((2 + kappa*t + ekt*(-2 + kappa*t))*theta +
                          (-1 + ekt - kappa*t)*y)*
                         ((1 + e2kt*(-5 + 2*kappa*t + 4*Math.Pow(rho, 2)*(-3 + kappa*t)) +
                           2*ekt*(2 + 2*kappa*t +
                                  Math.Pow(rho, 2)*(6 + 4*kappa*t + Math.Pow(kappa, 2)*Math.Pow(t, 2))))*theta +
                          2*(-1 + e2kt*(1 + 2*Math.Pow(rho, 2)) -
                             ekt*(2*kappa*t +
                                  Math.Pow(rho, 2)*(2 + 2*kappa*t + Math.Pow(kappa, 2)*Math.Pow(t, 2))))*y))/
                        ((1 + ekt*(-1 + kappa*t))*theta + (-1 + ekt)*y) +
                        (5*Math.Pow(delta, 2)*rho*((1 + ekt*(-1 + kappa*t))*theta +
                                              (-1 + ekt)*y)*
                         ((2 + kappa*t + ekt*(-2 + kappa*t))*theta +
                          (-1 + ekt - kappa*t)*y)*
                         (theta*(12*ekt*Math.Pow(kappa, 3)*Math.Pow(rho, 2)*Math.Pow(t, 2) +
                                 8*Math.Pow(-1 + ekt, 2)*Math.Pow(rho, 2)*theta -
                                 (-1 + ekt)*kappa*
                                 (3 + 8*Math.Pow(rho, 2)*t*theta +
                                  ekt*(15 + 8*Math.Pow(rho, 2)*(9 + t*theta))) +
                                 2*Math.Pow(kappa, 2)*t*(Math.Pow(rho, 2)*t*theta +
                                                    2*ekt*(3 + Math.Pow(rho, 2)*(12 + t*theta)) +
                                                    e2kt*(3 + Math.Pow(rho, 2)*(12 + t*theta)))) -
                          2*(6*ekt*Math.Pow(kappa, 3)*Math.Pow(rho, 2)*Math.Pow(t, 2) +
                             4*Math.Pow(-1 + ekt, 2)*Math.Pow(rho, 2)*theta +
                             2*Math.Pow(kappa, 2)*t*(Math.Pow(rho, 2)*t*theta +
                                                ekt*(3 + Math.Pow(rho, 2)*(6 + t*theta))) -
                             (-1 + ekt)*kappa*
                             (3 + 6*Math.Pow(rho, 2)*t*theta +
                              ekt*(3 + 2*Math.Pow(rho, 2)*(6 + t*theta))))*y +
                          2*Math.Pow(rho, 2)*Math.Pow(1 - ekt + kappa*t, 2)*Math.Pow(y, 2)))/
                        (ekt*(-theta + kappa*t*theta + (theta - y)/ekt + y)) -
                        (48*Math.Pow(delta, 2)*kappa*rho*((1 + ekt*(-1 + kappa*t))*theta +
                                                     (-1 + ekt)*y)*
                         (2*theta + kappa*t*theta - y - kappa*t*y +
                          ekt*((-2 + kappa*t)*theta + y))*
                         (theta - 2*y + e2kt*
                          (-5*theta + 2*kappa*t*theta + 2*y + 4*Math.Pow(rho, 2)*((-3 + kappa*t)*theta + y)) +
                          2*ekt*(2*(theta + kappa*t*(theta - y)) +
                                 Math.Pow(rho, 2)*((6 + kappa*t*(4 + kappa*t))*theta - (2 + kappa*t*(2 + kappa*t))*y))
                            ))/(ekt*(-theta + kappa*t*theta + (theta - y)/ekt + y)) +
                        (96*delta*Math.Pow(kappa, 3)*((1 + ekt*(-1 + kappa*t))*theta +
                                                 (-1 + ekt)*y)*
                         (theta - 2*y + e2kt*
                          (-5*theta + 2*kappa*t*theta + 2*y + 8*Math.Pow(rho, 2)*((-3 + kappa*t)*theta + y)) +
                          4*ekt*(theta + kappa*t*theta - kappa*t*y +
                                 Math.Pow(rho, 2)*((6 + kappa*t*(4 + kappa*t))*theta - (2 + kappa*t*(2 + kappa*t))*y))
                            ))/(-theta + kappa*t*theta + (theta - y)/ekt + y) +
                        (9*Math.Pow(delta, 2)*kappa*rho*((1 + ekt*(-1 + kappa*t))*theta +
                                                    (-1 + ekt)*y)*
                         ((2 + kappa*t + ekt*(-2 + kappa*t))*theta +
                          (-1 + ekt - kappa*t)*y)*
                         (theta - 2*y + e2kt*
                          (-5*theta + 2*kappa*t*theta + 2*y + 8*Math.Pow(rho, 2)*((-3 + kappa*t)*theta + y)) +
                          4*ekt*(theta + kappa*t*theta - kappa*t*y +
                                 Math.Pow(rho, 2)*((6 + kappa*t*(4 + kappa*t))*theta - (2 + kappa*t*(2 + kappa*t))*y))
                            ))/(ekt*(-theta + kappa*t*theta + (theta - y)/ekt + y)) -
                        (48*Math.Pow(delta, 2)*ekt*Math.Pow(kappa, 2)*rho*
                         (3*(theta - 2*y)*((2 + kappa*t)*theta - (1 + kappa*t)*y) +
                          3*ekt*(6*Math.Pow(theta, 2) + theta*y - 2*Math.Pow(y, 2) +
                                 kappa*(13*t*Math.Pow(theta, 2) + theta*(8 - 18*t*y) + 4*y*(-3 + t*y)) +
                                 4*Math.Pow(kappa, 2)*t*(theta + t*Math.Pow(theta, 2) - 2*t*theta*y + y*(-2 + t*y))) +
                          3*e3kt*(10*Math.Pow(theta, 2) +
                                  2*Math.Pow(kappa, 2)*t*theta*(6 + 8*Math.Pow(rho, 2) + t*theta) - 9*theta*y +
                                  2*Math.Pow(y, 2) + kappa*(-9*t*Math.Pow(theta, 2) + 4*(3 + 4*Math.Pow(rho, 2))*y +
                                                       theta*(-40 - 64*Math.Pow(rho, 2) + 4*t*y))) +
                          e2kt*(-54*Math.Pow(theta, 2) +
                                8*Math.Pow(kappa, 4)*Math.Pow(rho, 2)*Math.Pow(t, 3)*(theta - y) + 39*theta*y -
                                6*Math.Pow(y, 2) + 24*Math.Pow(kappa, 3)*Math.Pow(t, 2)*
                                (theta + 2*Math.Pow(rho, 2)*theta - (1 + Math.Pow(rho, 2))*y) +
                                6*Math.Pow(kappa, 2)*t*(3*t*Math.Pow(theta, 2) - 8*(1 + Math.Pow(rho, 2))*y +
                                                   theta*(16 + 24*Math.Pow(rho, 2) - 3*t*y)) -
                                3*kappa*(5*t*Math.Pow(theta, 2) + 2*y*(8*Math.Pow(rho, 2) + 3*t*y) -
                                         theta*(32 + 64*Math.Pow(rho, 2) + 17*t*y)))))/
                        ((1 + ekt*(-1 + kappa*t))*theta + (-1 + ekt)*y) +
                        (12*Math.Pow(delta, 2)*kappa*rho*((1 + ekt*(-1 + kappa*t))*theta +
                                                     (-1 + ekt)*y)*
                         (3*(theta - 2*y)*((2 + kappa*t)*theta - (1 + kappa*t)*y) +
                          3*ekt*(6*Math.Pow(theta, 2) + theta*y - 2*Math.Pow(y, 2) +
                                 kappa*(13*t*Math.Pow(theta, 2) + theta*(8 - 18*t*y) + 4*y*(-3 + t*y)) +
                                 4*Math.Pow(kappa, 2)*t*(theta + t*Math.Pow(theta, 2) - 2*t*theta*y + y*(-2 + t*y))) +
                          3*e3kt*(10*Math.Pow(theta, 2) +
                                  2*Math.Pow(kappa, 2)*t*theta*(6 + 8*Math.Pow(rho, 2) + t*theta) - 9*theta*y +
                                  2*Math.Pow(y, 2) + kappa*(-9*t*Math.Pow(theta, 2) + 4*(3 + 4*Math.Pow(rho, 2))*y +
                                                       theta*(-40 - 64*Math.Pow(rho, 2) + 4*t*y))) +
                          e2kt*(-54*Math.Pow(theta, 2) +
                                8*Math.Pow(kappa, 4)*Math.Pow(rho, 2)*Math.Pow(t, 3)*(theta - y) + 39*theta*y -
                                6*Math.Pow(y, 2) + 24*Math.Pow(kappa, 3)*Math.Pow(t, 2)*
                                (theta + 2*Math.Pow(rho, 2)*theta - (1 + Math.Pow(rho, 2))*y) +
                                6*Math.Pow(kappa, 2)*t*(3*t*Math.Pow(theta, 2) - 8*(1 + Math.Pow(rho, 2))*y +
                                                   theta*(16 + 24*Math.Pow(rho, 2) - 3*t*y)) -
                                3*kappa*(5*t*Math.Pow(theta, 2) + 2*y*(8*Math.Pow(rho, 2) + 3*t*y) -
                                         theta*(32 + 64*Math.Pow(rho, 2) + 17*t*y)))))/
                        (ekt*(-theta + kappa*t*theta + (theta - y)/ekt + y)) +
                        (240*Math.Pow(delta, 2)*e2kt*Math.Pow(kappa, 2)*rho*
                         ((2 + kappa*t + ekt*(-2 + kappa*t))*theta +
                          (-1 + ekt - kappa*t)*y)*
                         (12*ekt*Math.Pow(kappa, 3)*Math.Pow(rho, 2)*Math.Pow(t, 2)*(theta - y) +
                          2*Math.Pow(-1 + ekt, 2)*Math.Pow(rho, 2)*Math.Pow(-2*theta + y, 2) -
                          (-1 + ekt)*kappa*
                          (8*(1 + ekt)*Math.Pow(rho, 2)*t*Math.Pow(theta, 2) +
                           2*y*(-3 - 3*ekt*(1 + 4*Math.Pow(rho, 2)) + 2*Math.Pow(rho, 2)*t*y) +
                           theta*(3 - 12*Math.Pow(rho, 2)*t*y +
                                  ekt*(15 + Math.Pow(rho, 2)*(72 - 4*t*y)))) +
                          2*Math.Pow(kappa, 2)*t*(e2kt*theta*(3 + Math.Pow(rho, 2)*(12 + t*theta)) +
                                             Math.Pow(rho, 2)*t*Math.Pow(theta - y, 2) +
                                             2*ekt*(Math.Pow(rho, 2)*t*Math.Pow(theta, 2) - 3*(y + 2*Math.Pow(rho, 2)*y) +
                                                    theta*(3 + Math.Pow(rho, 2)*(12 - t*y))))))/
                        Math.Pow((1 + ekt*(-1 + kappa*t))*theta + (-1 + ekt)*y, 2) -
                        (120*Math.Pow(delta, 2)*ekt*kappa*rho*
                         ((2 + kappa*t + ekt*(-2 + kappa*t))*theta +
                          (-1 + ekt - kappa*t)*y)*
                         (12*ekt*Math.Pow(kappa, 3)*Math.Pow(rho, 2)*Math.Pow(t, 2)*(theta - y) +
                          2*Math.Pow(-1 + ekt, 2)*Math.Pow(rho, 2)*Math.Pow(-2*theta + y, 2) -
                          (-1 + ekt)*kappa*
                          (8*(1 + ekt)*Math.Pow(rho, 2)*t*Math.Pow(theta, 2) +
                           2*y*(-3 - 3*ekt*(1 + 4*Math.Pow(rho, 2)) + 2*Math.Pow(rho, 2)*t*y) +
                           theta*(3 - 12*Math.Pow(rho, 2)*t*y +
                                  ekt*(15 + Math.Pow(rho, 2)*(72 - 4*t*y)))) +
                          2*Math.Pow(kappa, 2)*t*(e2kt*theta*(3 + Math.Pow(rho, 2)*(12 + t*theta)) +
                                             Math.Pow(rho, 2)*t*Math.Pow(theta - y, 2) +
                                             2*ekt*(Math.Pow(rho, 2)*t*Math.Pow(theta, 2) - 3*(y + 2*Math.Pow(rho, 2)*y) +
                                                    theta*(3 + Math.Pow(rho, 2)*(12 - t*y))))))/
                        ((1 + ekt*(-1 + kappa*t))*theta + (-1 + ekt)*y)))/
                (1536.0*e3kt*Math.Pow(kappa, 6)*Math.Pow(t, 2)*
         Math.Pow((-theta + kappa*t*theta + (theta - y)/ekt + y)/(kappa*t), 1.5));
      }
      private double z2(double t, double kappa, double theta,double delta, double y, double rho)
      {
         return (Math.Pow(delta, 2)*(8*e3kt*Math.Pow(kappa, 5)*Math.Pow(rho, 2)*Math.Pow(t, 4)*(2 + delta*rho*t)*
                                Math.Pow(theta, 2)*(theta - y) - delta*Math.Pow(-1 + ekt, 3)*rho*
                                (2*(-1 + ekt*(-5 + 24*Math.Pow(rho, 2)))*Math.Pow(theta, 3) +
                                 (7 + ekt*(3 + 56*Math.Pow(rho, 2)))*Math.Pow(theta, 2)*y -
                                 3*(1 + ekt*(-3 + 8*Math.Pow(rho, 2)))*theta*Math.Pow(y, 2) +
                                 2*(-1 + ekt*(-1 + 2*Math.Pow(rho, 2)))*Math.Pow(y, 3)) -
                                Math.Pow(-1 + ekt, 2)*kappa*
                                ((-4 + delta*rho*t - 8*ekt*
                                  (2 - 12*Math.Pow(rho, 2) - 4*delta*rho*t + 25*delta*Math.Pow(rho, 3)*t) +
                                  e2kt*(20 - 96*Math.Pow(rho, 2) + 3*delta*rho*t + 56*delta*Math.Pow(rho, 3)*t)
                                   )*Math.Pow(theta, 3) - 2*(-8 + 2*delta*rho*t +
                                                        e2kt*(24 - 80*Math.Pow(rho, 2) - 9*delta*rho*t +
                                                              24*delta*Math.Pow(rho, 3)*t) -
                                                        4*ekt*
                                                        (4 - 20*Math.Pow(rho, 2) - 10*delta*rho*t + 39*delta*Math.Pow(rho, 3)*t)
                                      )*Math.Pow(theta, 2)*y + (5*(-4 + delta*rho*t) +
                                                           ekt*(-16 + 80*Math.Pow(rho, 2) + 57*delta*rho*t -
                                                                140*delta*Math.Pow(rho, 3)*t) +
                                                           2*e2kt*(18 - 40*Math.Pow(rho, 2) - 3*delta*rho*t +
                                                                   6*delta*Math.Pow(rho, 3)*t))*theta*Math.Pow(y, 2) +
                                 2*(4 + e2kt*(-4 + 8*Math.Pow(rho, 2)) - delta*rho*t +
                                    ekt*rho*(-8*rho - 7*delta*t + 14*delta*Math.Pow(rho, 2)*t))*Math.Pow(y, 3)) +
                                ekt*(-1 + ekt)*Math.Pow(kappa, 2)*t*
                                ((-24 + 128*Math.Pow(rho, 2) + 9*delta*rho*t - 144*delta*Math.Pow(rho, 3)*t -
                                  4*ekt*(6 - 8*Math.Pow(rho, 2) - 9*delta*rho*t + 6*delta*Math.Pow(rho, 3)*t) +
                                  e2kt*(48 - 160*Math.Pow(rho, 2) - 9*delta*rho*t +
                                        24*delta*Math.Pow(rho, 3)*t))*Math.Pow(theta, 3) -
                                 (-72 + 320*Math.Pow(rho, 2) + 27*delta*rho*t - 360*delta*Math.Pow(rho, 3)*t -
                                  ekt*rho*(160*rho - 81*delta*t + 348*delta*Math.Pow(rho, 2)*t) +
                                  2*e2kt*(36 - 80*Math.Pow(rho, 2) - 3*delta*rho*t +
                                          6*delta*Math.Pow(rho, 3)*t))*Math.Pow(theta, 2)*y -
                                 2*(32 - 128*Math.Pow(rho, 2) + 12*e2kt*(-1 + 2*Math.Pow(rho, 2)) -
                                    15*delta*rho*t + 144*delta*Math.Pow(rho, 3)*t +
                                    2*ekt*(-10 + 52*Math.Pow(rho, 2) - 13*delta*rho*t +
                                           58*delta*Math.Pow(rho, 3)*t))*theta*Math.Pow(y, 2) +
                                 4*(4 - 16*Math.Pow(rho, 2) - 3*delta*rho*t + 18*delta*Math.Pow(rho, 3)*t +
                                    ekt*(-4 + 16*Math.Pow(rho, 2) - 2*delta*rho*t + 11*delta*Math.Pow(rho, 3)*t))*
                                 Math.Pow(y, 3)) - 4*e2kt*Math.Pow(kappa, 4)*Math.Pow(t, 3)*theta*
                                (2*e2kt*(-1 + 2*Math.Pow(rho, 2))*Math.Pow(theta, 2) +
                                 Math.Pow(rho, 2)*(4 + 13*delta*rho*t)*Math.Pow(theta - y, 2) +
                                 ekt*((-4 + 16*Math.Pow(rho, 2) - 2*delta*rho*t + 9*delta*Math.Pow(rho, 3)*t)*
                                      Math.Pow(theta, 2) + (4 - 32*Math.Pow(rho, 2) + 2*delta*rho*t - 19*delta*Math.Pow(rho, 3)*t)*
                                      theta*y + 4*Math.Pow(rho, 2)*(2 + delta*rho*t)*Math.Pow(y, 2))) -
                                2*ekt*Math.Pow(kappa, 3)*Math.Pow(t, 2)*
                                (-4*Math.Pow(rho, 2)*(-4 + 3*delta*rho*t)*Math.Pow(theta - y, 3) +
                                 e3kt*Math.Pow(theta, 2)*
                                 ((18 - 40*Math.Pow(rho, 2) - delta*rho*t + 2*delta*Math.Pow(rho, 3)*t)*theta +
                                  12*(-1 + 2*Math.Pow(rho, 2))*y) +
                                 2*ekt*((-9 + 36*Math.Pow(rho, 2) + 19*delta*Math.Pow(rho, 3)*t)*Math.Pow(theta, 3) +
                                        2*(9 - 30*Math.Pow(rho, 2) + 7*delta*Math.Pow(rho, 3)*t)*Math.Pow(theta, 2)*y +
                                        (-8 + 20*Math.Pow(rho, 2) + delta*rho*t - 46*delta*Math.Pow(rho, 3)*t)*theta*Math.Pow(y, 2) +
                                        Math.Pow(rho, 2)*(4 + 13*delta*rho*t)*Math.Pow(y, 3)) +
                                 e2kt*(8*theta*y*(-3*theta + 2*y) +
                                       delta*rho*t*theta*(7*Math.Pow(theta, 2) - 23*theta*y + 8*Math.Pow(y, 2)) -
                                       8*Math.Pow(rho, 2)*(6*Math.Pow(theta, 3) - 18*Math.Pow(theta, 2)*y + 11*theta*Math.Pow(y, 2) -
                                                      Math.Pow(y, 3)) + 4*delta*Math.Pow(rho, 3)*t*
                                       (-13*Math.Pow(theta, 3) + 31*Math.Pow(theta, 2)*y - 14*theta*Math.Pow(y, 2) + Math.Pow(y, 3))))))/
                (64.0*Math.Pow(kappa, 2)*t*Math.Sqrt((-theta + kappa*t*theta + (theta - y)/ekt + y)/
                               (kappa*t))*Math.Pow((1 + ekt*(-1 + kappa*t))*theta + (-1 + ekt)*y,4));
      }
      private double z3(double t, double kappa, double theta,double delta, double y, double rho)
      {
         return (Math.Pow(delta, 3)*ekt*rho*((-15*(2 + kappa*t) +
                                         3*e4kt*(50 - 79*kappa*t + 35*Math.Pow(kappa, 2)*Math.Pow(t, 2) -
                                                 6*Math.Pow(kappa, 3)*Math.Pow(t, 3) +
                                                 8*Math.Pow(rho, 2)*(-18 + 15*kappa*t - 6*Math.Pow(kappa, 2)*Math.Pow(t, 2) +
                                                                Math.Pow(kappa, 3)*Math.Pow(t, 3))) +
                                         ekt*(-3*(20 + 86*kappa*t + 29*Math.Pow(kappa, 2)*Math.Pow(t, 2)) +
                                              Math.Pow(rho, 2)*(432 + 936*kappa*t + 552*Math.Pow(kappa, 2)*Math.Pow(t, 2) +
                                                           92*Math.Pow(kappa, 3)*Math.Pow(t, 3))) +
                                         e2kt*(360 + 324*kappa*t - 261*Math.Pow(kappa, 2)*Math.Pow(t, 2) -
                                               48*Math.Pow(kappa, 3)*Math.Pow(t, 3) -
                                               4*Math.Pow(rho, 2)*(324 + 378*kappa*t - 12*Math.Pow(kappa, 2)*Math.Pow(t, 2) -
                                                              2*Math.Pow(kappa, 3)*Math.Pow(t, 3) + 23*Math.Pow(kappa, 4)*Math.Pow(t, 4))) +
                                         e3kt*(3*(-140 + 62*kappa*t + 81*Math.Pow(kappa, 2)*Math.Pow(t, 2) -
                                                  38*Math.Pow(kappa, 3)*Math.Pow(t, 3) + 8*Math.Pow(kappa, 4)*Math.Pow(t, 4)) +
                                               4*Math.Pow(rho, 2)*(324 + 54*kappa*t - 114*Math.Pow(kappa, 2)*Math.Pow(t, 2) +
                                                              77*Math.Pow(kappa, 3)*Math.Pow(t, 3) - 19*Math.Pow(kappa, 4)*Math.Pow(t, 4) +
                                                              2*Math.Pow(kappa, 5)*Math.Pow(t, 5))))*Math.Pow(theta, 3) +
                                        (15*(7 + 4*kappa*t) + 3*e4kt*
                                         (-79 + 70*kappa*t - 18*Math.Pow(kappa, 2)*Math.Pow(t, 2) +
                                          24*Math.Pow(rho, 2)*(5 - 4*kappa*t + Math.Pow(kappa, 2)*Math.Pow(t, 2))) -
                                         3*ekt*(26 - 200*kappa*t - 87*Math.Pow(kappa, 2)*Math.Pow(t, 2) +
                                                4*Math.Pow(rho, 2)*(30 + 142*kappa*t + 115*Math.Pow(kappa, 2)*Math.Pow(t, 2) +
                                                               23*Math.Pow(kappa, 3)*Math.Pow(t, 3))) +
                                         2*e2kt*(3*(-66 - 195*kappa*t + 63*Math.Pow(kappa, 2)*Math.Pow(t, 2) +
                                                    16*Math.Pow(kappa, 3)*Math.Pow(t, 3)) +
                                                 4*Math.Pow(rho, 2)*(135 + 390*kappa*t - 9*Math.Pow(kappa, 2)*Math.Pow(t, 2) -
                                                                48*Math.Pow(kappa, 3)*Math.Pow(t, 3) + 23*Math.Pow(kappa, 4)*Math.Pow(t, 4))) +
                                         e3kt*(606 + 300*kappa*t - 585*Math.Pow(kappa, 2)*Math.Pow(t, 2) +
                                               210*Math.Pow(kappa, 3)*Math.Pow(t, 3) - 24*Math.Pow(kappa, 4)*Math.Pow(t, 4) -
                                               4*Math.Pow(rho, 2)*(270 + 282*kappa*t - 345*Math.Pow(kappa, 2)*Math.Pow(t, 2) +
                                                              153*Math.Pow(kappa, 3)*Math.Pow(t, 3) - 29*Math.Pow(kappa, 4)*Math.Pow(t, 4) +
                                                              2*Math.Pow(kappa, 5)*Math.Pow(t, 5))))*Math.Pow(theta, 2)*y +
                                        (-93 - 75*kappa*t + 3*e4kt*
                                         (35 - 18*kappa*t + 24*Math.Pow(rho, 2)*(-2 + kappa*t)) +
                                         3*ekt*(58 - 123*kappa*t - 86*Math.Pow(kappa, 2)*Math.Pow(t, 2) +
                                                4*Math.Pow(rho, 2)*(12 + 80*kappa*t + 92*Math.Pow(kappa, 2)*Math.Pow(t, 2) +
                                                               23*Math.Pow(kappa, 3)*Math.Pow(t, 3))) +
                                         e3kt*(-3*(74 + 137*kappa*t - 100*Math.Pow(kappa, 2)*Math.Pow(t, 2) +
                                                   16*Math.Pow(kappa, 3)*Math.Pow(t, 3)) -
                                               16*Math.Pow(rho, 2)*(-27 - 51*kappa*t + 45*Math.Pow(kappa, 2)*Math.Pow(t, 2) -
                                                               12*Math.Pow(kappa, 3)*Math.Pow(t, 3) + Math.Pow(kappa, 4)*Math.Pow(t, 4))) +
                                         e2kt*(36 + 909*kappa*t - 42*Math.Pow(kappa, 2)*Math.Pow(t, 2) -
                                               60*Math.Pow(kappa, 3)*Math.Pow(t, 3) -
                                               4*Math.Pow(rho, 2)*(108 + 462*kappa*t + 96*Math.Pow(kappa, 2)*Math.Pow(t, 2) -
                                                              117*Math.Pow(kappa, 3)*Math.Pow(t, 3) + 23*Math.Pow(kappa, 4)*Math.Pow(t, 4))))*
                                        theta*Math.Pow(y, 2)
                                        + 2*(9 + 3*e4kt*(-3 + 4*Math.Pow(rho, 2)) + 15*kappa*t +
                                             e2kt*(-3*kappa*t*(33 + 10*kappa*t) +
                                                   Math.Pow(rho, 2)*(36 + 192*kappa*t + 96*Math.Pow(kappa, 2)*Math.Pow(t, 2) -
                                                                46*Math.Pow(kappa, 3)*Math.Pow(t, 3))) +
                                             e3kt*(18 + 57*kappa*t - 12*Math.Pow(kappa, 2)*Math.Pow(t, 2) -
                                                   2*Math.Pow(rho, 2)*(18 + 48*kappa*t - 21*Math.Pow(kappa, 2)*Math.Pow(t, 2) +
                                                                  2*Math.Pow(kappa, 3)*Math.Pow(t, 3))) +
                                             ekt*(3*(-6 + 9*kappa*t + 14*Math.Pow(kappa, 2)*Math.Pow(t, 2)) -
                                                  2*Math.Pow(rho, 2)*(6 + 48*kappa*t + 69*Math.Pow(kappa, 2)*Math.Pow(t, 2) +
                                                                 23*Math.Pow(kappa, 3)*Math.Pow(t, 3))))*Math.Pow(y, 3)))/
                (96.0*kappa*t*Math.Sqrt((-theta + kappa*t*theta + (theta - y)/ekt + y)/(kappa*t))*
         Math.Pow((1 + ekt*(-1 + kappa*t))*theta + (-1 + ekt)*y, 5));
      }
   }

   /*! Small-time expansion from
       "The small-time smile and term structure of implied volatility
       under the Heston model" M Forde, A Jacquier, R Lee - SIAM
       Journal on Financial Mathematics, 2012 - SIAM
   */
   public class FordeHestonExpansion : HestonExpansion 
   {
      public FordeHestonExpansion(double kappa, double theta,double sigma, double v0,double rho, double term)
      {
         double v0Sqrt = Math.Sqrt(v0);
         double rhoBarSquare = 1 - rho * rho;
         double sigma00 = v0Sqrt;
         double sigma01 = v0Sqrt * (rho * sigma / (4 * v0)); //term in x
         double sigma02 = v0Sqrt * ((1 - 5 * rho * rho / 2) / 24 * sigma * sigma/ (v0 * v0)); //term in x*x
         double a00 = -sigma * sigma / 12 * (1 - rho * rho / 4) + v0 * rho * sigma / 4 + kappa / 2 * (theta - v0);
         double a01 = rho * sigma / (24 * v0) * (sigma * sigma * rhoBarSquare - 2 * kappa * (theta + v0) + v0 * rho * sigma); //term in x
         double a02 = (176 * sigma * sigma - 480 * kappa * theta - 712 * rho * rho * sigma * sigma + 521 * rho * rho * rho * rho * sigma * sigma + 40 * sigma * rho * rho * rho * v0 + 1040 * kappa * theta * rho * rho - 80 * v0 * kappa * rho * rho) * sigma * sigma / (v0 * v0 * 7680);
         coeffs[0] = sigma00*sigma00+a00*term;
         coeffs[1] = sigma00*sigma01*2+a01*term;
         coeffs[2] = sigma00*sigma02*2+sigma01*sigma01+a02*term;
         coeffs[3] = sigma01*sigma02*2;
         coeffs[4] = sigma02*sigma02;
      }

      public override double impliedVolatility(double strike,double forward)
      {
         double x = Math.Log(strike/forward);
         double var = coeffs[0]+x*(coeffs[1]+(x*(coeffs[2]+x*(coeffs[3]+(x*coeffs[4])))));
         var = Math.Max(1e-8,var);
         return Math.Sqrt(var);
      }

      private double[] coeffs = new double[5];
   }

}
