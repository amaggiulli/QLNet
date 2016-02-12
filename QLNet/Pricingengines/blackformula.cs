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

	public partial class Utils
	{
      /*! Black 1976 formula
        \warning instead of volatility it uses standard deviation,
                 i.e. volatility*sqrt(timeToMaturity)
      */
      public static double blackFormula( Option.Type optionType,
                                         double strike,
                                         double forward,
                                         double stdDev,
                                         double discount = 1.0,
                                         double displacement = 0.0 )
      {
         checkParameters(strike, forward, displacement);
         Utils.QL_REQUIRE(stdDev>=0.0,()=> "stdDev (" + stdDev + ") must be non-negative");
         Utils.QL_REQUIRE(discount>0.0,()=> "discount (" + discount + ") must be positive");

         if (stdDev==0.0)
            return Math.Max((forward-strike)*(int)optionType, 0.0)*discount;

         forward = forward + displacement;
         strike = strike + displacement;

         // since displacement is non-negative strike==0 iff displacement==0
         // so returning forward*discount is OK
         if (strike==0.0)
            return (optionType==Option.Type.Call ? forward*discount : 0.0);

         double d1 = Math.Log(forward/strike)/stdDev + 0.5*stdDev;
         double d2 = d1 - stdDev;
         CumulativeNormalDistribution phi = new CumulativeNormalDistribution();
         double nd1 = phi.value( (int)optionType * d1 );
         double nd2 = phi.value( (int)optionType * d2 );
         double result = discount * (int)optionType * ( forward * nd1 - strike * nd2 );
         Utils.QL_REQUIRE(result>=0.0,()=>
                  "negative value (" + result + ") for " +
                  stdDev + " stdDev, " +
                  optionType + " option, " +
                  strike + " strike , " +
                  forward + " forward");
        return result;
      }

      public static double blackFormula( PlainVanillaPayoff payoff,
                                         double forward,
                                         double stdDev,
                                         double discount = 1.0,
                                         double displacement = 0.0)
      {
         return blackFormula( payoff.optionType(),payoff.strike(), forward, stdDev, discount, displacement );   
      }

      /*! Approximated Black 1976 implied standard deviation,
          i.e. volatility*sqrt(timeToMaturity).

          It is calculated using Brenner and Subrahmanyan (1988) and Feinstein
          (1988) approximation for at-the-money forward option, with the
          extended moneyness approximation by Corrado and Miller (1996)
      */
      public static double blackFormulaImpliedStdDevApproximation( Option.Type optionType,
                                                                   double strike,
                                                                   double forward,
                                                                   double blackPrice,
                                                                   double discount = 1.0,
                                                                   double displacement = 0.0 )
      {
         checkParameters(strike, forward, displacement);
         Utils.QL_REQUIRE(blackPrice>=0.0,()=>
                   "blackPrice (" + blackPrice + ") must be non-negative");
         Utils.QL_REQUIRE(discount>0.0,()=>
                   "discount (" + discount + ") must be positive");

         double stdDev;
         forward = forward + displacement;
         strike = strike + displacement;
         if (strike==forward)
            // Brenner-Subrahmanyan (1988) and Feinstein (1988) ATM approx.
            stdDev = blackPrice/discount*Math.Sqrt(2.0 * Const.M_PI)/forward;
         else 
         {
            // Corrado and Miller extended moneyness approximation
            double moneynessDelta = (int)optionType*(forward-strike);
            double moneynessDelta_2 = moneynessDelta/2.0;
            double temp = blackPrice/discount - moneynessDelta_2;
            double moneynessDelta_PI = moneynessDelta*moneynessDelta/Const.M_PI;
            double temp2 = temp*temp-moneynessDelta_PI;
            if (temp2<0.0) // approximation breaks down, 2 alternatives:
                // 1. zero it
                temp2=0.0;
                // 2. Manaster-Koehler (1982) efficient Newton-Raphson seed
                //return std::fabs(std::log(forward/strike))*std::sqrt(2.0);
            temp2 = Math.Sqrt(temp2);
            temp += temp2;
            temp *= Math.Sqrt(2.0 * Const.M_PI);
            stdDev = temp/(forward+strike);
         }

         Utils.QL_REQUIRE(stdDev>=0.0,()=> "stdDev (" + stdDev + ") must be non-negative");
         return stdDev;
      }

      public static double blackFormulaImpliedStdDevApproximation( PlainVanillaPayoff payoff,
                                                                   double forward,
                                                                   double blackPrice,
                                                                   double discount,
                                                                   double displacement) 
      {
         return blackFormulaImpliedStdDevApproximation(payoff.optionType(),
            payoff.strike(), forward, blackPrice, discount, displacement);
      }


      /*! Approximated Black 1976 implied standard deviation,
          i.e. volatility*sqrt(timeToMaturity).

          It is calculated following "An improved approach to computing
          implied volatility", Chambers, Nawalkha, The Financial Review,
          2001, 89-100. The atm option price must be known to use this
          method.
      */
      public static double blackFormulaImpliedStdDevChambers( Option.Type optionType,
                                                              double strike,
                                                              double forward,
                                                              double blackPrice,
                                                              double blackAtmPrice,
                                                              double discount = 1.0,
                                                              double displacement = 0.0 )
      {
         checkParameters(strike, forward, displacement);
         QL_REQUIRE(blackPrice >= 0.0,()=>
            "blackPrice (" + blackPrice + ") must be non-negative");
         QL_REQUIRE(blackAtmPrice >= 0.0, ()=>
            "blackAtmPrice ("+ blackAtmPrice + ") must be non-negative");
         QL_REQUIRE(discount > 0.0,()=> 
            "discount (" + discount + ") must be positive");

         double stdDev;

         forward = forward + displacement;
         strike = strike + displacement;
         blackPrice /= discount;
         blackAtmPrice /= discount;

         double s0 = Const.M_SQRT2 * Const.M_SQRTPI * blackAtmPrice /
                  forward; // Brenner-Subrahmanyam formula
         double priceAtmVol = blackFormula(optionType, strike, forward, s0, 1.0, 0.0);
         double dc = blackPrice - priceAtmVol;

         if (close(dc, 0.0)) 
         {
            stdDev = s0;
         } 
         else 
         {
            double d1 = blackFormulaStdDevDerivative(strike, forward, s0, 1.0, 0.0);
            double d2 = blackFormulaStdDevSecondDerivative(strike, forward, s0,1.0, 0.0);
            double ds = 0.0;
            double tmp = d1 * d1 + 2.0 * d2 * dc;
            if (Math.Abs(d2) > 1E-10 && tmp >= 0.0)
                ds = (-d1 + Math.Sqrt(tmp)) / d2; // second order approximation
            else
                if(Math.Abs(d1) > 1E-10)
                    ds = dc / d1; // first order approximation
            stdDev = s0 + ds;
         }

         QL_REQUIRE(stdDev >= 0.0,()=> "stdDev (" + stdDev + ") must be non-negative");
         return stdDev;
      }
      
      public static double blackFormulaImpliedStdDevChambers( PlainVanillaPayoff payoff,
                                                              double forward,
                                                              double blackPrice,
                                                              double blackAtmPrice,
                                                              double discount,
                                                              double displacement) 
      {
        return blackFormulaImpliedStdDevChambers(payoff.optionType(), payoff.strike(), forward, blackPrice,
            blackAtmPrice, discount, displacement);
      }


      /*! Black 1976 implied standard deviation,
            i.e. volatility*sqrt(timeToMaturity)
      */
      public static double blackFormulaImpliedStdDev( Option.Type optionType,
                                                      double strike,
                                                      double forward,
                                                      double blackPrice,
                                                      double discount = 1.0,
                                                      double displacement = 0.0,
                                                      double? guess =null,
                                                      double accuracy = 1.0e-6,
                                                      int maxIterations = 100 )
      {
         checkParameters(strike, forward, displacement);

         QL_REQUIRE(discount>0.0,()=>
                   "discount (" + discount + ") must be positive");

         QL_REQUIRE(blackPrice>=0.0,()=>
                   "option price (" + blackPrice + ") must be non-negative");
         // check the price of the "other" option implied by put-call paity
         double otherOptionPrice = blackPrice - (int)optionType*(forward-strike)*discount;
         QL_REQUIRE(otherOptionPrice>=0.0,()=>
                   "negative " + (-1*(int)optionType) +
                   " price (" + otherOptionPrice +
                   ") implied by put-call parity. No solution exists for " +
                   optionType + " strike " + strike +
                   ", forward " + forward +
                   ", price " + blackPrice +
                   ", deflator " + discount);

         // solve for the out-of-the-money option which has
         // greater vega/price ratio, i.e.
         // it is numerically more robust for implied vol calculations
         if (optionType==Option.Type.Put && strike>forward) 
         {
            optionType = Option.Type.Call;
            blackPrice = otherOptionPrice;
         }
         if (optionType==Option.Type.Call && strike<forward) 
         {
            optionType = Option.Type.Put;
            blackPrice = otherOptionPrice;
         }

         strike = strike + displacement;
         forward = forward + displacement;

         if (guess==null)
            guess = blackFormulaImpliedStdDevApproximation(optionType, strike, forward, blackPrice, discount, displacement);
         else
            QL_REQUIRE(guess>=0.0,()=> "stdDev guess (" + guess + ") must be non-negative");
         
         BlackImpliedStdDevHelper f = new BlackImpliedStdDevHelper(optionType, strike, forward,blackPrice/discount);
         NewtonSafe solver = new NewtonSafe();
         solver.setMaxEvaluations(maxIterations);
         double minSdtDev = 0.0, maxStdDev = 24.0; // 24 = 300% * sqrt(60)
         double stdDev = solver.solve(f, accuracy, guess.Value, minSdtDev, maxStdDev);
         QL_REQUIRE(stdDev>=0.0,()=> "stdDev (" + stdDev + ") must be non-negative");
         return stdDev;
      }
      
      public static double blackFormulaImpliedStdDev( PlainVanillaPayoff payoff,
                                                      double forward,
                                                      double blackPrice,
                                                      double discount,
                                                      double displacement,
                                                      double guess,
                                                      double accuracy,
                                                      int maxIterations = 100) 
      {
         return blackFormulaImpliedStdDev(payoff.optionType(), payoff.strike(),
            forward, blackPrice, discount, displacement, guess, accuracy, maxIterations);
      }


      /*! Black 1976 probability of being in the money (in the bond martingale measure), i.e. N(d2).
            It is a risk-neutral probability, not the real world one.
             \warning instead of volatility it uses standard deviation, i.e. volatility*sqrt(timeToMaturity)
      */
      public static double blackFormulaCashItmProbability( Option.Type optionType,
                                                           double strike,
                                                           double forward,
                                                           double stdDev,
                                                           double displacement = 0.0 )
      {
         checkParameters(strike, forward, displacement);
         if (stdDev==0.0)
            return (forward*(int)optionType > strike*(int)optionType ? 1.0 : 0.0);

         forward = forward + displacement;
         strike = strike + displacement;
         if (strike==0.0)
            return (optionType==Option.Type.Call ? 1.0 : 0.0);
         double d2 = Math.Log(forward/strike)/stdDev - 0.5*stdDev;
         CumulativeNormalDistribution phi = new CumulativeNormalDistribution();
         return phi.value((int)optionType*d2);
      }

      public static double blackFormulaCashItmProbability( PlainVanillaPayoff payoff,
                                                           double forward,
                                                           double stdDev,
                                                           double displacement = 0.0)
      {
         return blackFormulaCashItmProbability( payoff.optionType(),
            payoff.strike(), forward, stdDev, displacement );
      }

      /*! Black 1976 formula for standard deviation derivative
          \warning instead of volatility it uses standard deviation, i.e.
                   volatility*sqrt(timeToMaturity), and it returns the
                   derivative with respect to the standard deviation.
                   If T is the time to maturity Black vega would be
                   blackStdDevDerivative(strike, forward, stdDev)*sqrt(T)
      */
      public static double blackFormulaStdDevDerivative( double strike,
                                                         double forward,
                                                         double stdDev,
                                                         double discount = 1.0,
                                                         double displacement = 0.0 )
      {
         checkParameters(strike, forward, displacement);
         QL_REQUIRE(stdDev>=0.0,()=>
                   "stdDev (" + stdDev + ") must be non-negative");
         QL_REQUIRE(discount>0.0,()=>
                   "discount (" + discount + ") must be positive");

         forward = forward + displacement;
         strike = strike + displacement;

         if (stdDev==0.0 || strike==0.0)
            return 0.0;

         double d1 = Math.Log(forward/strike)/stdDev + .5*stdDev;
         return discount * forward *
            new CumulativeNormalDistribution().derivative(d1);
      }
      /*! Black 1976 formula for  derivative with respect to implied vol, this
        is basically the vega, but if you want 1% change multiply by 1%
      */
      public static double blackFormulaVolDerivative( double strike,
                                                      double forward,
                                                      double stdDev,
                                                      double expiry,
                                                      double discount = 1.0,
                                                      double displacement = 0.0 )
      {
         return  blackFormulaStdDevDerivative(strike,forward,stdDev,discount,displacement)*Math.Sqrt(expiry);
      }

      public static double blackFormulaStdDevDerivative( PlainVanillaPayoff payoff,
                                                         double forward,
                                                         double stdDev,
                                                         double discount = 1.0,
                                                         double displacement = 0.0)
      {
         return blackFormulaStdDevDerivative( payoff.strike(), forward,stdDev, discount, displacement );
      }

      /*! Black 1976 formula for second derivative by standard deviation
            \warning instead of volatility it uses standard deviation, i.e.
             volatility*sqrt(timeToMaturity), and it returns the
             derivative with respect to the standard deviation.
      */
      public static double blackFormulaStdDevSecondDerivative( double strike,
                                                               double forward,
                                                               double stdDev,
                                                               double discount,
                                                               double displacement )
      {
         checkParameters(strike, forward, displacement);
         QL_REQUIRE(stdDev>=0.0,()=>
                   "stdDev (" + stdDev + ") must be non-negative");
         QL_REQUIRE(discount>0.0,()=>
                   "discount (" + discount + ") must be positive");

         forward = forward + displacement;
         strike = strike + displacement;

         if (stdDev==0.0 || strike==0.0)
            return 0.0;

         double d1 = Math.Log(forward/strike)/stdDev + .5*stdDev;
         double d1p = -Math.Log(forward/strike)/(stdDev*stdDev) + .5;
         return discount * forward *
            new NormalDistribution().derivative(d1) * d1p;
      }

      public static double blackFormulaStdDevSecondDerivative( PlainVanillaPayoff payoff,
                                                               double forward,
                                                               double stdDev,
                                                               double discount = 1.0,
                                                               double displacement = 0.0)
      {
         return blackFormulaStdDevSecondDerivative( payoff.strike(), forward,stdDev, discount, displacement );
      }

      /*! Black style formula when forward is normal rather than
         log-normal. This is essentially the model of Bachelier.

          \warning Bachelier model needs absolute volatility, not
             percentage volatility. Standard deviation is
             absoluteVolatility*sqrt(timeToMaturity)
      */
      public static double bachelierBlackFormula( Option.Type optionType,
                                                  double strike,
                                                  double forward,
                                                  double stdDev,
                                                  double discount = 1.0 )
      {
         QL_REQUIRE(stdDev>=0.0,()=>
                   "stdDev (" + stdDev + ") must be non-negative");
         QL_REQUIRE(discount>0.0,()=>
                   "discount (" + discount + ") must be positive");
         double d = (forward-strike)*(int)optionType, h = d/stdDev;
         if (stdDev==0.0)
            return discount*Math.Max(d, 0.0);
         CumulativeNormalDistribution phi = new CumulativeNormalDistribution();
         double result = discount*(stdDev*phi.derivative(h) + d*phi.value(h));
         QL_REQUIRE(result>=0.0,()=>
                  "negative value (" + result + ") for " +
                  stdDev + " stdDev, " +
                  optionType + " option, " +
                  strike + " strike , " +
                  forward + " forward");
         return result;
      }

      public static double bachelierBlackFormula( PlainVanillaPayoff payoff,
                                                  double forward,
                                                  double stdDev,
                                                  double discount = 1.0)
      {
         return bachelierBlackFormula( payoff.optionType(),payoff.strike(), forward, stdDev, discount );
      }

      /*! Approximated Bachelier implied volatility

         It is calculated using  the analytic implied volatility approximation
         of J. Choi, K Kim and M. Kwak (2009), “Numerical Approximation of the
         Implied Volatility Under Arithmetic Brownian Motion”,
         Applied Math. Finance, 16(3), pp. 261-268.
      */
      public static double bachelierBlackFormulaImpliedVol( Option.Type optionType,
                                                            double strike,
                                                            double forward,
                                                            double tte,
                                                            double bachelierPrice,
                                                            double discount = 1.0 )
      {
         double SQRT_QL_EPSILON = Math.Sqrt(Const.QL_EPSILON);

         QL_REQUIRE(tte>0.0,()=> "tte (" + tte + ") must be positive");

         double forwardPremium = bachelierPrice/discount;

         double straddlePremium;
         if (optionType==Option.Type.Call)
         {
            straddlePremium = 2.0 * forwardPremium - (forward - strike);
         } 
         else 
         {
            straddlePremium = 2.0 * forwardPremium + (forward - strike);
         }

         double nu = (forward - strike) / straddlePremium;
         QL_REQUIRE(nu<=1.0,()=> "nu (" + nu + ") must be <= 1.0");
         QL_REQUIRE(nu>=-1.0,()=> "nu (" + nu + ") must be >= -1.0");

         nu = Math.Max(-1.0 + Const.QL_EPSILON, Math.Min(nu,1.0 - Const.QL_EPSILON));

         // nu / arctanh(nu) -> 1 as nu -> 0
         double eta = (Math.Abs(nu) < SQRT_QL_EPSILON) ? 1.0 : nu / ((Math.Log(1 + nu) - Math.Log(1 - nu))/2);

         double heta = h(eta);

         double impliedBpvol = Math.Sqrt(Const.M_PI / (2 * tte)) * straddlePremium * heta;

         return impliedBpvol;
      }
          
      public static double h(double eta) 
      {

         const double  A0 = 3.994961687345134e-1;
         const double  A1 = 2.100960795068497e+1;
         const double  A2 = 4.980340217855084e+1;
         const double  A3 = 5.988761102690991e+2;
         const double  A4 = 1.848489695437094e+3;
         const double  A5 = 6.106322407867059e+3;
         const double  A6 = 2.493415285349361e+4;
         const double  A7 = 1.266458051348246e+4;
             
         const double  B0 = 1.000000000000000e+0;
         const double  B1 = 4.990534153589422e+1;
         const double  B2 = 3.093573936743112e+1;
         const double  B3 = 1.495105008310999e+3;
         const double  B4 = 1.323614537899738e+3;
         const double  B5 = 1.598919697679745e+4;
         const double  B6 = 2.392008891720782e+4;
         const double  B7 = 3.608817108375034e+3;
         const double  B8 = -2.067719486400926e+2;
         const double  B9 = 1.174240599306013e+1;

         QL_REQUIRE(eta>=0.0,()=>
                       "eta (" + eta + ") must be non-negative");

         double num = A0 + eta * (A1 + eta * (A2 + eta * (A3 + eta * (A4 + eta
                    * (A5 + eta * (A6 + eta * A7))))));

         double den = B0 + eta * (B1 + eta * (B2 + eta * (B3 + eta * (B4 + eta
                    * (B5 + eta * (B6 + eta * (B7 + eta * (B8 + eta * B9))))))));

         return Math.Sqrt(eta) * (num / den);

    }

      /*! Bachelier formula for standard deviation derivative
            \warning instead of volatility it uses standard deviation, i.e.
             volatility*sqrt(timeToMaturity), and it returns the
             derivative with respect to the standard deviation.
             If T is the time to maturity Black vega would be
             blackStdDevDerivative(strike, forward, stdDev)*sqrt(T)
      */

      public static double bachelierBlackFormulaStdDevDerivative( double strike,
                                                                  double forward,
                                                                  double stdDev,
                                                                  double discount = 1.0 )
      {
         QL_REQUIRE( stdDev >= 0.0,()=>
           "stdDev (" + stdDev + ") must be non-negative" );
         QL_REQUIRE( discount > 0.0,()=>
                    "discount (" + discount + ") must be positive" );

         if ( stdDev == 0.0 )
            return 0.0;

         double d1 = ( forward - strike ) / stdDev;
         return discount *
             new CumulativeNormalDistribution().derivative( d1 );
      }

      public static double bachelierBlackFormulaStdDevDerivative( PlainVanillaPayoff payoff,
                                                                  double forward,
                                                                  double stdDev,
                                                                  double discount = 1.0)
      {
         return bachelierBlackFormulaStdDevDerivative( payoff.strike(), forward, stdDev, discount );
      }

      public static void checkParameters( double strike,double forward,double displacement )
      {
         Utils.QL_REQUIRE( displacement >= 0.0,()=> 
            "displacement (" + displacement + ") must be non-negative" );
         Utils.QL_REQUIRE( strike + displacement >= 0.0,()=>
            "strike + displacement (" + strike + " + " + displacement + ") must be non-negative" );
         Utils.QL_REQUIRE( forward + displacement > 0.0, () => 
            "forward + displacement (" + forward + " + " + displacement + ") must be positive" );
      }

      class BlackImpliedStdDevHelper : ISolver1d
      {
         public BlackImpliedStdDevHelper( Option.Type optionType,
                                         double strike,
                                         double forward,
                                         double undiscountedBlackPrice,
                                         double displacement = 0.0 )
         {
            halfOptionType_ = 0.5 * (int)optionType;
            signedStrike_ = (int)optionType * ( strike + displacement );
            signedForward_ = (int)optionType * ( forward + displacement );
            undiscountedBlackPrice_ = undiscountedBlackPrice;
            N_ = new CumulativeNormalDistribution();
            checkParameters( strike, forward, displacement );
            Utils.QL_REQUIRE( undiscountedBlackPrice >= 0.0, () =>
                       "undiscounted Black price (" +
                       undiscountedBlackPrice + ") must be non-negative" );
            signedMoneyness_ = (int)optionType * Math.Log( ( forward + displacement ) / ( strike + displacement ) );
         }
         public override double value( double stdDev )
         {
#if QL_EXTRA_SAFETY_CHECKS
               Utils.QL_REQUIRE(stdDev>=0.0,()=> "stdDev (" + stdDev + ") must be non-negative");
#endif
            if ( stdDev == 0.0 )
               return Math.Max( signedForward_ - signedStrike_, 0.0 )
                                                  - undiscountedBlackPrice_;
            double temp = halfOptionType_ * stdDev;
            double d = signedMoneyness_ / stdDev;
            double signedD1 = d + temp;
            double signedD2 = d - temp;
            double result = signedForward_ * N_.value( signedD1 )
                - signedStrike_ * N_.value( signedD2 );
            // numerical inaccuracies can yield a negative answer
            return Math.Max( 0.0, result ) - undiscountedBlackPrice_;
         }

         public override double derivative( double stdDev )
         {
#if QL_EXTRA_SAFETY_CHECKS
            QL_REQUIRE(stdDev>=0.0,
                       "stdDev (" << stdDev << ") must be non-negative");
#endif
            double signedD1 = signedMoneyness_ / stdDev + halfOptionType_ * stdDev;
            return signedForward_ * N_.derivative( signedD1 );
         }


         private double halfOptionType_;
         private double signedStrike_, signedForward_;
         private double undiscountedBlackPrice_, signedMoneyness_;
         private CumulativeNormalDistribution N_;
      }
   }
}