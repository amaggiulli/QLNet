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

namespace QLNet
{
   //! Class for the quotation of delta vs vol.
   /*! It includes the various delta quotation types
       in FX markets as well as ATM types.
   */
   public class DeltaVolQuote : Quote, IObserver
   {
      public enum DeltaType 
      {
         Spot,        // Spot Delta, e.g. usual Black Scholes delta
         Fwd,         // Forward Delta
         PaSpot,      // Premium Adjusted Spot Delta
         PaFwd        // Premium Adjusted Forward Delta
      }
      
      public enum AtmType
      {
         AtmNull,         // Default, if not an atm quote
         AtmSpot,         // K=S_0
         AtmFwd,          // K=F
         AtmDeltaNeutral, // Call Delta = Put Delta
         AtmVegaMax,      // K such that Vega is Maximum
         AtmGammaMax,     // K such that Gamma is Maximum
         AtmPutCall50     // K such that Call Delta=0.50 (only for Fwd Delta)
      }

      // Standard constructor delta vs vol.
      public DeltaVolQuote(double delta, Handle<Quote> vol,double maturity,DeltaType deltaType)
      {
         delta_ = delta;
         vol_ = vol;
         deltaType_ = deltaType;
         maturity_ = maturity;
         atmType_ = DeltaVolQuote.AtmType.AtmNull;

         vol_.registerWith(update);
      }

      // Additional constructor, if special atm quote is used
      public DeltaVolQuote( Handle<Quote> vol,DeltaType deltaType,double maturity,AtmType atmType)
      {
         vol_ = vol;
         deltaType_ = deltaType;
         maturity_ = maturity;
         atmType_ = atmType;

         vol_.registerWith( update );
      }

      public void update()  { notifyObservers(); }

      public override double value() { return vol_.link.value(); }
      public double delta() { return delta_; }
      public double maturity() { return maturity_; }

      public AtmType atmType() { return atmType_; }
      public DeltaType deltaType() { return deltaType_; }

      public override bool isValid() { return !vol_.empty() && vol_.link.isValid(); }

      private double delta_;
      private Handle<Quote> vol_;
      private DeltaType deltaType_;
      private double maturity_;
      private AtmType atmType_;

   }
}
