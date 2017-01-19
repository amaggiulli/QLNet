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
using System.Collections.Generic;

namespace QLNet
{
   //! This engine only adds timing functionality (e.g. different lag)
   //! w.r.t. an existing interpolated price surface.
   public class InterpolatingCPICapFloorEngine : CPICapFloor.Engine
   {
      public InterpolatingCPICapFloorEngine(Handle<CPICapFloorTermPriceSurface> priceSurf)
      {
         priceSurf_ = priceSurf;

         priceSurf_.registerWith(update);
      }

      public override void calculate()
      {
         double npv = 0.0;

         // what is the difference between the observationLag of the surface
         // and the observationLag of the cap/floor?
         // TODO next line will fail if units are different
         Period lagDiff = arguments_.observationLag - priceSurf_.link.observationLag();
         // next line will fail if units are different if Period() is not well written
         Utils.QL_REQUIRE(lagDiff >= new Period(0, TimeUnit.Months),()=> "InterpolatingCPICapFloorEngine: " +
            "lag difference must be non-negative: " + lagDiff);

         // we now need an effective maturity to use in the price surface because this uses
         // maturity of calibration instruments as its time axis, N.B. this must also
         // use the roll because the surface does
         Date effectiveMaturity = arguments_.payDate - lagDiff;


         // what interpolation do we use? Index / flat / linear
         if (arguments_.observationInterpolation == InterpolationType.AsIndex) 
         {
            // same as index means we can just use the price surface
            // since this uses the index
            if (arguments_.type == Option.Type.Call) 
            {
               npv = priceSurf_.link.capPrice(effectiveMaturity, arguments_.strike);
            } 
            else 
            {
               npv = priceSurf_.link.floorPrice(effectiveMaturity, arguments_.strike);
            }


         } 
         else 
         {
            KeyValuePair<Date,Date> dd = Utils.inflationPeriod(effectiveMaturity, arguments_.infIndex.link.frequency());
            double priceStart = 0.0;

            if (arguments_.type == Option.Type.Call) 
            {
               priceStart = priceSurf_.link.capPrice(dd.Key, arguments_.strike);
            } 
            else 
            {
               priceStart = priceSurf_.link.floorPrice(dd.Key, arguments_.strike);
            }

            // if we use a flat index vs the interpolated one ...
            if (arguments_.observationInterpolation == InterpolationType.Flat) 
            {
               // then use the price for the first day in the period because the value cannot change after then
               npv = priceStart;
            } 
            else 
            {
               // linear interpolation will be very close
               double priceEnd = 0.0;
               if (arguments_.type == Option.Type.Call) 
               {
                  priceEnd = priceSurf_.link.capPrice((dd.Value + new Period(1,TimeUnit.Days)), arguments_.strike);
               } 
               else 
               {
                  priceEnd = priceSurf_.link.floorPrice((dd.Value + new Period(1,TimeUnit.Days)), arguments_.strike);
               }

               npv = priceStart + (priceEnd - priceStart) * (effectiveMaturity - dd.Key)
                     / ( (dd.Value + new Period(1,TimeUnit.Days)) - dd.Key); // can't get to next period'
            }
         }

         results_.value = npv;
      }

      public  virtual String name() { return "InterpolatingCPICapFloorEngine"; }

      protected Handle<CPICapFloorTermPriceSurface> priceSurf_;
   }
}
