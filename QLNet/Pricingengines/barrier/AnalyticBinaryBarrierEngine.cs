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
   //! Analytic pricing engine for American binary barriers options
   /*! The formulas are taken from "The complete guide to option pricing formulas 2nd Ed",
        E.G. Haug, McGraw-Hill, p.176 and following. 

       \ingroup barrierengines

       \test
       - the correctness of the returned value in case of
         cash-or-nothing at-expiry binary payoff is tested by
         reproducing results available in literature.
       - the correctness of the returned value in case of
         asset-or-nothing at-expiry binary payoff is tested by
         reproducing results available in literature.
   */
   public class AnalyticBinaryBarrierEngine : BarrierOption.Engine
   {
      public AnalyticBinaryBarrierEngine( GeneralizedBlackScholesProcess process)
      {
         process_ = process;
         process_.registerWith(update);
      }
      public override void calculate()
      {
         AmericanExercise ex = arguments_.exercise as AmericanExercise;
         Utils.QL_REQUIRE(ex!=null,()=> "non-American exercise given");
         Utils.QL_REQUIRE(ex.payoffAtExpiry(),()=> "payoff must be at expiry");
         Utils.QL_REQUIRE(ex.dates()[0] <= process_.blackVolatility().link.referenceDate(),()=>
            "American option with window exercise not handled yet");

         StrikedTypePayoff payoff = arguments_.payoff as StrikedTypePayoff;
         Utils.QL_REQUIRE(payoff!=null,()=> "non-striked payoff given");

         double spot = process_.stateVariable().link.value();
         Utils.QL_REQUIRE(spot > 0.0,()=> "negative or null underlying given");

         double variance = process_.blackVolatility().link.blackVariance(ex.lastDate(),payoff.strike());
         double? barrier = arguments_.barrier;
         Utils.QL_REQUIRE(barrier>0.0,()=>"positive barrier value required");
         Barrier.Type barrierType = arguments_.barrierType;

         // KO degenerate cases
         if ( (barrierType == Barrier.Type.DownOut && spot <= barrier) ||
              (barrierType == Barrier.Type.UpOut && spot >= barrier))
         {
            // knocked out, no value
            results_.value = 0;
            results_.delta = 0;
            results_.gamma = 0;
            results_.vega = 0;
            results_.theta = 0;
            results_.rho = 0;
            results_.dividendRho = 0;
            return;
         }

         // KI degenerate cases
         if ((barrierType == Barrier.Type.DownIn && spot <= barrier) ||
             (barrierType == Barrier.Type.UpIn && spot >= barrier)) 
         {
            // knocked in - is a digital european
            Exercise exercise = new EuropeanExercise(arguments_.exercise.lastDate());

            IPricingEngine engine = new AnalyticEuropeanEngine(process_);

            VanillaOption opt = new VanillaOption(payoff, exercise);
            opt.setPricingEngine(engine);
            results_.value = opt.NPV();
            results_.delta = opt.delta();
            results_.gamma = opt.gamma();
            results_.vega = opt.vega();
            results_.theta = opt.theta();
            results_.rho = opt.rho();
            results_.dividendRho = opt.dividendRho();
            return;
         }

         double riskFreeDiscount = process_.riskFreeRate().link.discount(ex.lastDate());

         AnalyticBinaryBarrierEngine_helper helper = new AnalyticBinaryBarrierEngine_helper( 
            process_, payoff, ex, arguments_ );
         results_.value = helper.payoffAtExpiry(spot, variance, riskFreeDiscount);

      }
      
      private GeneralizedBlackScholesProcess process_;
   }

   // calc helper object 
   public class AnalyticBinaryBarrierEngine_helper
   {
    
      public AnalyticBinaryBarrierEngine_helper(
             GeneralizedBlackScholesProcess process,
             StrikedTypePayoff payoff,
             AmericanExercise exercise,
             BarrierOption.Arguments arguments)
      {
         process_ = process;
         payoff_ = payoff;
         exercise_ = exercise;
         arguments_ = arguments;
      }

      public double payoffAtExpiry(double spot, double variance, double discount)
      {
         double dividendDiscount = process_.dividendYield().link.discount(exercise_.lastDate());

         Utils.QL_REQUIRE(spot>0.0,()=> "positive spot value required");
         Utils.QL_REQUIRE(discount>0.0,()=> "positive discount required");
         Utils.QL_REQUIRE(dividendDiscount>0.0,()=> "positive dividend discount required");
         Utils.QL_REQUIRE(variance>=0.0,()=> "negative variance not allowed");

         Option.Type type   = payoff_.optionType();
         double strike = payoff_.strike();
         double? barrier = arguments_.barrier;
         Utils.QL_REQUIRE(barrier>0.0,()=>"positive barrier value required");
         Barrier.Type barrierType = arguments_.barrierType;

         double stdDev = Math.Sqrt(variance);
         double mu = Math.Log(dividendDiscount/discount)/variance - 0.5;
         double K = 0;

         // binary cash-or-nothing payoff?
         CashOrNothingPayoff coo = payoff_ as CashOrNothingPayoff;
         if (coo != null ) 
         {
            K = coo.cashPayoff();
         }

         // binary asset-or-nothing payoff?
         AssetOrNothingPayoff aoo = payoff_ as AssetOrNothingPayoff;
         if (aoo!=null) 
         {
            mu += 1.0; 
            K = spot * dividendDiscount / discount; // forward
         }

         double log_S_X   = Math.Log(spot/strike);
         double log_S_H   = Math.Log(spot/barrier.GetValueOrDefault());
         double log_H_S   = Math.Log(barrier.GetValueOrDefault()/spot);
         double log_H2_SX = Math.Log(barrier.GetValueOrDefault()*barrier.GetValueOrDefault()/(spot*strike));
         double H_S_2mu   = Math.Pow(barrier.GetValueOrDefault()/spot, 2*mu);

         double eta = (barrierType == Barrier.Type.DownIn ||
                       barrierType == Barrier.Type.DownOut ? 1.0 : -1.0);
         double phi = (type == Option.Type.Call ? 1.0 : -1.0);

         double x1, x2, y1, y2;
         double cum_x1, cum_x2, cum_y1, cum_y2;
         if (variance>=Const.QL_EPSILON) 
         {
            // we calculate using mu*stddev instead of (mu+1)*stddev
            // because cash-or-nothing don't need it. asset-or-nothing
            // mu is really mu+1
            x1 = phi*(log_S_X/stdDev + mu*stdDev);
            x2 = phi*(log_S_H/stdDev + mu*stdDev);
            y1 = eta*(log_H2_SX/stdDev + mu*stdDev);
            y2 = eta*(log_H_S/stdDev + mu*stdDev);

            CumulativeNormalDistribution f = new CumulativeNormalDistribution();
            cum_x1 = f.value(x1);
            cum_x2 = f.value(x2);
            cum_y1 = f.value(y1);
            cum_y2 = f.value(y2);
         } 
         else 
         {
            if (log_S_X>0)
                  cum_x1= 1.0;
            else
                  cum_x1= 0.0;
            if (log_S_H>0)
                  cum_x2= 1.0;
            else
                  cum_x2= 0.0;
            if (log_H2_SX>0)
                  cum_y1= 1.0;
            else
                  cum_y1= 0.0;
            if (log_H_S>0)
                  cum_y2= 1.0;
            else
                  cum_y2= 0.0;
         }

         double alpha = 0;

         switch (barrierType) 
         {
            case Barrier.Type.DownIn:
               if (type == Option.Type.Call) 
               {
                  // down-in and call
                  if (strike >= barrier) 
                  {
                     // B3 (eta=1, phi=1)
                     alpha = H_S_2mu * cum_y1;  
                  } 
                  else 
                  {
                     // B1-B2+B4 (eta=1, phi=1)
                     alpha = cum_x1 - cum_x2 + H_S_2mu * cum_y2; 
                  }
               }
               else 
               {
                  // down-in and put 
                  if (strike >= barrier) 
                  {
                     // B2-B3+B4 (eta=1, phi=-1)
                     alpha = cum_x2 + H_S_2mu*(-cum_y1 + cum_y2);
                  } 
                  else 
                  {
                     // B1 (eta=1, phi=-1)
                     alpha = cum_x1;
                  }
               }
               break;

            case Barrier.Type.UpIn:
               if (type == Option.Type.Call) 
               {
                  // up-in and call
                  if (strike >= barrier) 
                  {
                     // B1 (eta=-1, phi=1)
                     alpha = cum_x1;  
                  } 
                  else 
                  {
                     // B2-B3+B4 (eta=-1, phi=1)
                     alpha = cum_x2 + H_S_2mu * (-cum_y1 + cum_y2);
                  }
               }
               else 
               {
                  // up-in and put 
                  if (strike >= barrier) 
                  {
                     // B1-B2+B4 (eta=-1, phi=-1)
                     alpha = cum_x1 - cum_x2 + H_S_2mu * cum_y2;
                  } 
                  else 
                  {
                     // B3 (eta=-1, phi=-1)
                     alpha = H_S_2mu * cum_y1;  
                  }
               }
               break;

            case Barrier.Type.DownOut:
               if (type == Option.Type.Call) 
               {
                  // down-out and call
                  if (strike >= barrier) 
                  {
                     // B1-B3 (eta=1, phi=1)
                     alpha = cum_x1 - H_S_2mu * cum_y1; 
                  } 
                  else 
                  {
                     // B2-B4 (eta=1, phi=1)
                     alpha = cum_x2 - H_S_2mu * cum_y2; 
                  }
               }
               else 
               {
                  // down-out and put 
                  if (strike >= barrier) 
                  {
                     // B1-B2+B3-B4 (eta=1, phi=-1)
                     alpha = cum_x1 - cum_x2 + H_S_2mu * (cum_y1-cum_y2);
                  } 
                  else 
                  {
                     // always 0
                     alpha = 0;  
                  }
               }
               break;
            case Barrier.Type.UpOut:
               if (type == Option.Type.Call) 
               {
                  // up-out and call
                  if (strike >= barrier) 
                  {
                     // always 0
                     alpha = 0;  
                  } 
                  else 
                  {
                     // B1-B2+B3-B4 (eta=-1, phi=1)
                     alpha = cum_x1 - cum_x2 + H_S_2mu * (cum_y1-cum_y2);
                  }
               }
               else 
               {
                  // up-out and put 
                  if (strike >= barrier) 
                  {
                     // B2-B4 (eta=-1, phi=-1)
                     alpha = cum_x2 - H_S_2mu * cum_y2;
                  } 
                  else 
                  {
                     // B1-B3 (eta=-1, phi=-1)
                     alpha = cum_x1 - H_S_2mu * cum_y1;
                  }
               }
               break;
            default:
               Utils.QL_FAIL("invalid barrier type");
               break;
         }

         return discount * K * alpha;
      }
    
      private GeneralizedBlackScholesProcess process_;
      private StrikedTypePayoff payoff_;
      private AmericanExercise exercise_;
      private BarrierOption.Arguments arguments_;
    }
}
