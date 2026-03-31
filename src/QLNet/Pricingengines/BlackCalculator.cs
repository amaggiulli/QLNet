/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2025  Andrea Maggiulli (a.maggiulli@gmail.com)

 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

 QLNet is free software: you can redistribute it and/or modify it
 under the terms of the QLNet license.  You should have received a
 copy of the license along with this program; if not, license is
 available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.

 QLNet is a based on QuantLib, a free-software/open-source library
 for financial quantitative analysts and developers - http://quantlib.org/
 The QuantLib license is available online at http://quantlib.org/license.shtml.

 This program is distributed in the hope that it will be useful, but WITHOUT
 ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 FOR A PARTICULAR PURPOSE.  See the license for more details.
*/
using System;
using System.Reflection;

namespace QLNet
{
   /// <summary>
   /// Black 1976 calculator class
   /// </summary>
   /// <remarks>
   /// Bug: When the variance is null, division by zero occur during
   /// the calculation of delta, delta forward, gamma, gamma
   /// forward, rho, dividend rho, vega, and strike sensitivity.
   /// </remarks>
   public class BlackCalculator
   {
      protected double strike_, forward_, stdDev_, discount_, variance_;
      double D1_, D2_, alpha_, beta_, DalphaDd1_, DbetaDd2_;
      double n_d1_, cum_d1_, n_d2_, cum_d2_;
      double X_, DXDs_, DXDstrike_;

      public BlackCalculator(StrikedTypePayoff payoff, double forward, double stdDev, double discount)
      {
         strike_ = payoff.strike();
         forward_ = forward;
         stdDev_ = stdDev;
         discount_ = discount;
         variance_ = (stdDev * stdDev);
         initialize(payoff);
      }

      public BlackCalculator(Option.Type optionType, double strike, double forward, double stdDev, double discount = 1.0)
      {
         strike_ = strike;
         forward_ = forward;
         stdDev_ = stdDev;
         discount_ = discount;
         variance_ = stdDev * stdDev;
         initialize(new PlainVanillaPayoff(optionType, strike));
      }

      public void initialize(StrikedTypePayoff payoff)
      {
         Utils.QL_REQUIRE(forward_ > 0.0, () => "positive forward value required: " + forward_ + " not allowed");
         Utils.QL_REQUIRE(stdDev_ >= 0.0, () => "non-negative standard deviation required: " + stdDev_ + " not allowed");
         Utils.QL_REQUIRE(discount_ > 0.0, () => "positive discount required: " + discount_ + " not allowed");

         if (stdDev_ >= Const.QL_EPSILON)
         {
            if (strike_.IsEqual(0.0))
            {
               n_d1_ = 0.0;
               n_d2_ = 0.0;
               cum_d1_ = 1.0;
               cum_d2_ = 1.0;
            }
            else
            {
               D1_ = Math.Log(forward_ / strike_) / stdDev_ + 0.5 * stdDev_;
               D2_ = D1_ - stdDev_;
               CumulativeNormalDistribution f = new CumulativeNormalDistribution();
               cum_d1_ = f.value(D1_);
               cum_d2_ = f.value(D2_);
               n_d1_ = f.derivative(D1_);
               n_d2_ = f.derivative(D2_);
            }
         }
         else
         {
            if (forward_ > strike_)
            {
               cum_d1_ = 1.0;
               cum_d2_ = 1.0;
            }
            else
            {
               cum_d1_ = 0.0;
               cum_d2_ = 0.0;
            }
            n_d1_ = 0.0;
            n_d2_ = 0.0;
         }

         X_ = strike_;
         DXDstrike_ = 1.0;

         // the following one will probably disappear as soon as
         // super-share will be properly handled
         DXDs_ = 0.0;

         // this part is always executed.
         // in case of plain-vanilla payoffs, it is also the only part
         // which is executed.
         switch (payoff.optionType())
         {
            case Option.Type.Call:
               alpha_     =  cum_d1_;//  N(d1)
               DalphaDd1_ =    n_d1_;//  n(d1)
               beta_      = -cum_d2_;// -N(d2)
               DbetaDd2_  = -  n_d2_;// -n(d2)
               break;
            case Option.Type.Put:
               alpha_     = -1.0 + cum_d1_; // -N(-d1)
               DalphaDd1_ =        n_d1_;//  n( d1)
               beta_      =  1.0 - cum_d2_; //  N(-d2)
               DbetaDd2_  =     -  n_d2_;// -n( d2)
               break;
            default:
               Utils.QL_FAIL("invalid option type");
               break;
         }

         // now dispatch on type.

         Calculator calc = new Calculator(this);
         payoff.accept(calc);
      }

      public double value()
      {
         double result = discount_ * (forward_ * alpha_ + X_ * beta_);
         return result;
      }

      /// <summary>
      /// Returns the sensitivity to a change in the underlying forward price.
      /// </summary>
      public double deltaForward()
      {

         double temp = stdDev_ * forward_;
         double DalphaDforward = DalphaDd1_ / temp;
         double DbetaDforward = DbetaDd2_ / temp;
         double temp2 = DalphaDforward * forward_ + alpha_
                        + DbetaDforward * X_; // DXDforward = 0.0

         return discount_ * temp2;
      }

      /// <summary>
      /// Returns the sensitivity to a change in the underlying spot price.
      /// </summary>
      public virtual double delta(double spot)
      {
         Utils.QL_REQUIRE(spot > 0.0, () => "positive spot value required: " + spot + " not allowed");

         double DforwardDs = forward_ / spot;

         double temp = stdDev_ * spot;
         double DalphaDs = DalphaDd1_ / temp;
         double DbetaDs  = DbetaDd2_ / temp;
         double temp2 = DalphaDs * forward_ + alpha_ * DforwardDs
                        + DbetaDs  * X_       + beta_  * DXDs_;

         return discount_ * temp2;
      }

      /// <summary>
      /// Returns the elasticity with respect to the underlying forward price.
      /// </summary>
      public double elasticityForward()
      {
         double val = value();
         double del = deltaForward();
         if (val > Const.QL_EPSILON)
            return del / val * forward_;
         if (Math.Abs(del) < Const.QL_EPSILON)
            return 0.0;
         if (del > 0.0)
            return double.MaxValue;
         return double.MinValue;
      }

      /// <summary>
      /// Returns the elasticity with respect to the underlying spot price.
      /// </summary>
      public virtual double elasticity(double spot)
      {
         double val = value();
         double del = delta(spot);
         if (val > Const.QL_EPSILON)
            return del / val * spot;
         if (Math.Abs(del) < Const.QL_EPSILON)
            return 0.0;
         if (del > 0.0)
            return double.MaxValue;
         return double.MinValue;
      }

      /// <summary>
      /// Returns the second derivative with respect to the underlying forward price.
      /// </summary>
      public double gammaForward()
      {

         double temp = stdDev_ * forward_;
         double DalphaDforward = DalphaDd1_ / temp;
         double DbetaDforward = DbetaDd2_ / temp;

         double D2alphaDforward2 = -DalphaDforward / forward_ * (1 + D1_ / stdDev_);
         double D2betaDforward2 = -DbetaDforward / forward_ * (1 + D2_ / stdDev_);

         double temp2 = D2alphaDforward2 * forward_ + 2.0 * DalphaDforward
                        + D2betaDforward2 * X_; // DXDforward = 0.0

         return discount_ * temp2;
      }

      /// <summary>
      /// Returns the second derivative with respect to the underlying spot price.
      /// </summary>
      public virtual double gamma(double spot)
      {

         Utils.QL_REQUIRE(spot > 0.0, () => "positive spot value required: " + spot + " not allowed");

         double DforwardDs = forward_ / spot;

         double temp = stdDev_ * spot;
         double DalphaDs = DalphaDd1_ / temp;
         double DbetaDs = DbetaDd2_ / temp;

         double D2alphaDs2 = -DalphaDs / spot * (1 + D1_ / stdDev_);
         double D2betaDs2 = -DbetaDs / spot * (1 + D2_ / stdDev_);

         double temp2 = D2alphaDs2 * forward_ + 2.0 * DalphaDs * DforwardDs
                        + D2betaDs2 * X_ + 2.0 * DbetaDs * DXDs_;

         return discount_ * temp2;
      }

      /// <summary>
      /// Returns the sensitivity to time to maturity.
      /// </summary>
      public virtual double theta(double spot, double maturity)
      {

         if (maturity.IsEqual(0.0))
            return 0.0;
         Utils.QL_REQUIRE(maturity > 0.0, () => "non negative maturity required: " + maturity + " not allowed");

         return -(Math.Log(discount_)            * value()
                  + Math.Log(forward_ / spot) * spot * delta(spot)
                  + 0.5 * variance_ * spot  * spot * gamma(spot)) / maturity;
      }

      /// <summary>
      /// Returns the sensitivity to time to maturity per day, assuming 365 days per year.
      /// </summary>
      public virtual double thetaPerDay(double spot, double maturity)
      {
         return theta(spot, maturity) / 365.0;
      }

      /// <summary>
      /// Returns the sensitivity to volatility.
      /// </summary>
      public double vega(double maturity)
      {
         Utils.QL_REQUIRE(maturity >= 0.0, () => "negative maturity not allowed");

         double temp = Math.Log(strike_ / forward_) / variance_;
         // actually DalphaDsigma / SQRT(T)
         double DalphaDsigma = DalphaDd1_ * (temp + 0.5);
         double DbetaDsigma  = DbetaDd2_ * (temp - 0.5);

         double temp2 = DalphaDsigma * forward_ + DbetaDsigma * X_;

         return discount_ * Math.Sqrt(maturity) * temp2;

      }

      /// <summary>
      /// Returns the sensitivity to the discounting rate.
      /// </summary>
      public double rho(double maturity)
      {
         Utils.QL_REQUIRE(maturity >= 0.0, () => "negative maturity not allowed");

         // actually DalphaDr / T
         double DalphaDr = DalphaDd1_ / stdDev_;
         double DbetaDr = DbetaDd2_ / stdDev_;
         double temp = DalphaDr * forward_ + alpha_ * forward_ + DbetaDr * X_;

         return maturity * (discount_ * temp - value());
      }

      /// <summary>
      /// Returns the sensitivity to the dividend or growth rate.
      /// </summary>
      public double dividendRho(double maturity)
      {
         Utils.QL_REQUIRE(maturity >= 0.0, () => "negative maturity not allowed");

         // actually DalphaDq / T
         double DalphaDq = -DalphaDd1_ / stdDev_;
         double DbetaDq = -DbetaDd2_ / stdDev_;

         double temp = DalphaDq * forward_ - alpha_ * forward_ + DbetaDq * X_;

         return maturity * discount_ * temp;
      }

      /// <summary>
      /// Returns the probability of finishing in the money in the bond martingale measure.
      /// </summary>
      /// <remarks>
      /// This is the risk-neutral probability <c>N(d2)</c>, not a real-world probability.
      /// </remarks>
      public double itmCashProbability()
      {
         return cum_d2_;
      }

      /// <summary>
      /// Returns the probability of finishing in the money in the asset martingale measure.
      /// </summary>
      /// <remarks>
      /// This is the risk-neutral probability <c>N(d1)</c>, not a real-world probability.
      /// </remarks>
      public double itmAssetProbability()
      {
         return cum_d1_;
      }

      /// <summary>
      /// Returns the sensitivity to the strike.
      /// </summary>
      public double strikeSensitivity()
      {

         double temp = stdDev_ * strike_;
         double DalphaDstrike = -DalphaDd1_ / temp;
         double DbetaDstrike = -DbetaDd2_ / temp;

         double temp2 = DalphaDstrike * forward_ + DbetaDstrike * X_ + beta_ * DXDstrike_;

         return discount_ * temp2;
      }

      public double alpha()
      {
         return alpha_;
      }
      public double beta()
      {
         return beta_;
      }


      class Calculator : IAcyclicVisitor
      {
         private BlackCalculator black_;

         public Calculator(BlackCalculator black)
         {
            black_ = black;
         }

         public void visit(object o)
         {
            Type[] types = new Type[] { o.GetType() };
            MethodInfo methodInfo = Utils.GetMethodInfo(this, "visit", types);
            if (methodInfo != null)
            {
               methodInfo.Invoke(this, new object[] { o });
            }
         }

         public void visit(Payoff p)
         {
            Utils.QL_FAIL("unsupported payoff type: " + p.name());
         }

         public void visit(PlainVanillaPayoff p)
         {
            // Nothing to do here
         }

         public void visit(CashOrNothingPayoff payoff)
         {
            black_.alpha_ = black_.DalphaDd1_ = 0.0;
            black_.X_ = payoff.cashPayoff();
            black_.DXDstrike_ = 0.0;
            switch (payoff.optionType())
            {
               case Option.Type.Call:
                  black_.beta_     = black_.cum_d2_;
                  black_.DbetaDd2_ = black_.n_d2_;
                  break;
               case Option.Type.Put:
                  black_.beta_     = 1.0 - black_.cum_d2_;
                  black_.DbetaDd2_ =    -black_.n_d2_;
                  break;
               default:
                  Utils.QL_FAIL("invalid option type");
                  break;
            }
         }

         public void visit(AssetOrNothingPayoff payoff)
         {
            black_.beta_ = black_.DbetaDd2_ = 0.0;
            switch (payoff.optionType())
            {
               case Option.Type.Call:
                  black_.alpha_     = black_.cum_d1_;
                  black_.DalphaDd1_ = black_.n_d1_;
                  break;
               case Option.Type.Put:
                  black_.alpha_     = 1.0 - black_.cum_d1_;
                  black_.DalphaDd1_ = -black_.n_d1_;
                  break;
               default:
                  Utils.QL_FAIL("invalid option type");
                  break;
            }
         }

         public void visit(GapPayoff payoff)
         {
            black_.X_ = payoff.secondStrike();
            black_.DXDstrike_ = 0.0;
         }
      }
   }
}
