/*
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
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
   //! Analytic formula for American exercise payoff at-expiry options
   //! \todo calculate greeks 
   public class AmericanPayoffAtExpiry
   {
      public AmericanPayoffAtExpiry( double spot, double discount, double dividendDiscount, double variance,
                                     StrikedTypePayoff payoff, bool knock_in = true )
      {
         spot_ = spot;
         discount_ = discount;
         dividendDiscount_ = dividendDiscount;
         variance_ = variance;
         knock_in_ = knock_in;

         Utils.QL_REQUIRE( spot_ > 0.0,()=> "positive spot value required" );
         Utils.QL_REQUIRE( discount_ > 0.0, () => "positive discount required" );
         Utils.QL_REQUIRE( dividendDiscount_ > 0.0, () => "positive dividend discount required" );
         Utils.QL_REQUIRE( variance_ >= 0.0, () => "negative variance not allowed" );

         stdDev_ = Math.Sqrt( variance_ );
         Option.Type type = payoff.optionType();
         strike_ = payoff.strike();
         forward_ = spot_ * dividendDiscount_ / discount_;

         mu_ = Math.Log( dividendDiscount_ / discount_ ) / variance_ - 0.5;

         // binary cash-or-nothing payoff?
         CashOrNothingPayoff coo = payoff as CashOrNothingPayoff;
         if ( coo != null )
         {
            K_ = coo.cashPayoff();
         }

         // binary asset-or-nothing payoff?
         AssetOrNothingPayoff aoo = payoff as AssetOrNothingPayoff;
         if ( aoo != null )
         {
            K_ = forward_;
            mu_ += 1.0;
         }


         log_H_S_ = Math.Log( strike_ / spot_ );
         double log_S_H_ = Math.Log(spot_/strike_);

         double eta = 0.0;
         double phi = 0.0;
                 
         switch (type) 
         {
            case Option.Type.Call:
               if (knock_in_) 
               {
                  // up-and-in cash-(at-expiry)-or-nothing option
                  // a.k.a. american call with cash-or-nothing payoff
                  eta = -1.0;
                  phi =  1.0;
               } 
               else 
               {
                  // up-and-out cash-(at-expiry)-or-nothing option
				      eta = -1.0;
				      phi = -1.0;
               }
               break;
            case Option.Type.Put:
               if (knock_in_) 
               {
                  // down-and-in cash-(at-expiry)-or-nothing option
                  // a.k.a. american put with cash-or-nothing payoff
                  eta =  1.0;
                  phi = -1.0;
               } 
               else 
               {
                  // down-and-out cash-(at-expiry)-or-nothing option
				      eta =  1.0;
				      phi =  1.0;
               }
               break;
            default:
               Utils.QL_FAIL("invalid option type");
               break;
         }


         if ( variance_ >= Const.QL_EPSILON )
         {
            D1_ = phi * ( log_S_H_ / stdDev_ + mu_ * stdDev_ );
            D2_ = eta * ( log_H_S_ / stdDev_ + mu_ * stdDev_ );
            CumulativeNormalDistribution f = new CumulativeNormalDistribution();
            cum_d1_ = f.value( D1_ );
            cum_d2_ = f.value( D2_ );
            n_d1_ = f.derivative( D1_ );
            n_d2_ = f.derivative( D2_ );
         }
         else
         {
            if ( log_S_H_ * phi > 0 )
               cum_d1_ = 1.0;
            else
               cum_d1_ = 0.0;

            if ( log_H_S_ * eta > 0 )
               cum_d2_ = 1.0;
            else
               cum_d2_ = 0.0;

            n_d1_ = 0.0;
            n_d2_ = 0.0;
         }
        
         switch (type) 
         {
            case Option.Type.Call:
               if (strike_<=spot_) 
               {
                  if (knock_in_) 
                  {
                     // up-and-in cash-(at-expiry)-or-nothing option
                     // a.k.a. american call with cash-or-nothing payoff
                     cum_d1_     = 0.5;
                     cum_d2_     = 0.5;
                  } 
                  else 
                  {
                     // up-and-out cash-(at-expiry)-or-nothing option
                     // already knocked out
                     cum_d1_     = 0.0;
                     cum_d2_     = 0.0;
                  }
                  n_d1_       = 0.0;
                  n_d2_       = 0.0;
               }
               break;
            case Option.Type.Put:
   				if (strike_>=spot_) 
               {
                  if (knock_in_) 
                  {
					      // down-and-in cash-(at-expiry)-or-nothing option
					      // a.k.a. american put with cash-or-nothing payoff
                     cum_d1_ = 0.5;
                     cum_d2_ = 0.5;
                  } 
                  else 
                  {
					      // down-and-out cash-(at-expiry)-or-nothing option
                     // already knocked out
                     cum_d1_ = 0.0;
                     cum_d2_ = 0.0;
                  }
                  n_d1_       = 0.0;
                  n_d2_       = 0.0;
               }
               break;
            default:
               Utils.QL_FAIL("invalid option type");
               break;
         }


         inTheMoney_ = ( type == Option.Type.Call && strike_ < spot_ ) || 
                       ( type == Option.Type.Put && strike_ > spot_ );
         if ( inTheMoney_ )
         {
            X_ = 1.0;
            Y_ = 1.0;
         }
         else
         {
            X_ = 1.0;
            if (cum_d2_ == 0.0)
                Y_ = 0.0; // check needed on some extreme cases
            else
                Y_ = Math.Pow((strike_/spot_),(2.0*mu_));
         }
         if ( !knock_in_ )
            Y_ *= -1.0; 
      }

      public double value()
      {
         return discount_ * K_ * ( X_ * cum_d1_ + Y_ * cum_d2_ );
      }


      private double spot_;
      private double discount_;
      private double dividendDiscount_;
      private double variance_;

      private double forward_;
      private double stdDev_;

      private double strike_;
      private double K_;
      //private double DKDstrike_;

      private double mu_;
      private double log_H_S_;

      private double D1_;
      private double D2_;

      private double cum_d1_;
      private double cum_d2_;
      private double n_d1_;
      private double n_d2_;

      private bool inTheMoney_;
      private double Y_;
      private double X_;
      bool knock_in_;

   }
}