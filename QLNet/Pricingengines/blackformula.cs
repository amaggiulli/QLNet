/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008, 2009 , 2010  Andrea Maggiulli (a.maggiulli@gmail.com) 
  
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

namespace QLNet {
    /*! Black 1976 formula
    \warning instead of volatility it uses standard deviation,
             i.e. volatility*sqrt(timeToMaturity)
    */
   public partial class Utils
   {
      private static void checkParameters(double strike, double forward, double displacement)
      {
         if (!(strike >= 0.0))
            throw new ApplicationException("strike (" + strike + ") must be non-negative");
         if (!(forward > 0.0))
            throw new ApplicationException("forward (" + forward + ") must be positive");
         if (!(displacement >= 0.0))
            throw new ApplicationException("displacement (" + displacement + ") must be non-negative");
      }

      public static double blackFormula(Option.Type optionType, double strike, double forward, double stdDev)
      {
         return blackFormula(optionType, strike, forward, stdDev, 1.0, 0.0);
      }
      public static double blackFormula(Option.Type optionType, double strike, double forward, double stdDev, double discount)
      {
         return blackFormula(optionType, strike, forward, stdDev, discount, 0.0);
      }
      public static double blackFormula(Option.Type optionType, double strike, double forward, double stdDev,
                                        double discount, double displacement)
      {
         checkParameters(strike, forward, displacement);
         if (!(stdDev >= 0.0))
            throw new ApplicationException("stdDev (" + stdDev + ") must be non-negative");
         if (!(discount > 0.0))
            throw new ApplicationException("discount (" + discount + ") must be positive");

         if (stdDev == 0.0)
            return Math.Max((forward - strike) * (int)optionType, 0.0) * discount;

         forward = forward + displacement;
         strike = strike + displacement;

         // since displacement is non-negative strike==0 iff displacement==0
         // so returning forward*discount is OK 
         if (strike == 0.0)
            return (optionType == Option.Type.Call ? forward * discount : 0.0);

         double d1 = Math.Log(forward / strike) / stdDev + 0.5 * stdDev;
         double d2 = d1 - stdDev;
         CumulativeNormalDistribution phi = new CumulativeNormalDistribution();
         double nd1 = phi.value((int)optionType * d1);
         double nd2 = phi.value((int)optionType * d2);
         double result = discount * (int)optionType * (forward * nd1 - strike * nd2);
         if (!(result >= 0.0))
            throw new ApplicationException("negative value (" + result + ") for " +
                  stdDev + " stdDev, " +
                  optionType + " option, " +
                  strike + " strike , " +
                  forward + " forward");
         return result;
      }

      /// <summary>
      /// Black 1976 formula for standard deviation derivative
      /// \warning instead of volatility it uses standard deviation, i.e.
      /// volatility*sqrt(timeToMaturity), and it returns the
      /// derivative with respect to the standard deviation.
      /// If T is the time to maturity Black vega would be
      /// blackStdDevDerivative(strike, forward, stdDev)*sqrt(T)
      /// </summary>
      public static double blackFormulaStdDevDerivative(double strike, double forward, double stdDev)
      { return blackFormulaStdDevDerivative(strike, forward, stdDev, 1.0, 0.0); }
      public static double blackFormulaStdDevDerivative(double strike, double forward, double stdDev, double discount)
      { return blackFormulaStdDevDerivative(strike, forward, stdDev, discount, 0.0); }


      public static double blackFormulaStdDevDerivative(double strike,
                                          double forward,
                                          double stdDev,
                                          double discount,
                                          double displacement)
      {

         checkParameters(strike, forward, displacement);

         if (stdDev < 0.0)
            throw new ArgumentException("stdDev (" + stdDev + ") must be non-negative");

         if (discount <= 0.0)
            throw new ArgumentException("discount (" + discount + ") must be positive");

         forward = forward + displacement;
         strike = strike + displacement;

         if (stdDev == 0.0)
         {
            if (forward > strike)
               return discount * forward;
            else
               return 0.0;
         }

         double d1 = Math.Log(forward / strike) / stdDev + .5 * stdDev;
         CumulativeNormalDistribution phi = new CumulativeNormalDistribution();
         return discount * forward * phi.derivative(d1);
      }

      /*! Black style formula when forward is normal rather than
              log-normal. This is essentially the model of Bachelier.

              \warning Bachelier model needs absolute volatility, not
                       percentage volatility. Standard deviation is
                       absoluteVolatility*sqrt(timeToMaturity)
      */
      public static double bachelierBlackFormula(Option.Type optionType,
                                                 double strike,
                                                 double forward,
                                                 double stdDev
                                                 )
      {
         return bachelierBlackFormula(optionType, strike, forward, stdDev, 1);
      }


      public static double bachelierBlackFormula(Option.Type optionType,
                                                 double strike,
                                                 double forward,
                                                 double stdDev,
                                                 double discount)
      {
         if (stdDev < 0.0)
            throw new ArgumentException("stdDev (" + stdDev + ") must be non-negative");

         if (discount <= 0.0)
            throw new ArgumentException("discount (" + discount + ") must be positive");

         double d = (forward - strike) * (int)optionType;
         double h = d / stdDev;

         if (stdDev == 0.0)
            return discount * Math.Max(d, 0.0);

         CumulativeNormalDistribution phi = new CumulativeNormalDistribution();
         double result = discount * (stdDev * phi.derivative(h) + d * phi.value(h));

         if (!(result >= 0.0))
            throw new ApplicationException("negative value (" + result + ") for " +
                  stdDev + " stdDev, " +
                  optionType + " option, " +
                  strike + " strike , " +
                  forward + " forward");

         return result;
      }

      public static double bachelierBlackFormula(PlainVanillaPayoff payoff,
                                                 double forward,
                                                 double stdDev,
                                                 double discount)
      {

         return bachelierBlackFormula(payoff.optionType(),
           payoff.strike(), forward, stdDev, discount);

      }
   }
}
