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
   public class VannaVolgaInterpolationImpl : Interpolation.templateImpl
   {
      public VannaVolgaInterpolationImpl(List<double> xBegin, int size, List<double> yBegin,
         double spot, double dDiscount, double fDiscount, double T)
            : base(xBegin, size, yBegin, VannaVolga.requiredPoints)
      {
         spot_ = spot;
         dDiscount_ = dDiscount;
         fDiscount_ = fDiscount;
         T_ = T;

         premiaBS = new List<double>();
         premiaMKT = new List<double>();
         vegas = new List<double>();
         
         Utils.QL_REQUIRE(size == 3, ()=> "Vanna Volga Interpolator only interpolates 3 volatilities in strike space");
      }

      public override void update() 
      {
         //atmVol should be the second vol
         atmVol_ = this.yBegin_[1];
         fwd_ = spot_*fDiscount_/dDiscount_;
         for(int i = 0; i < 3; i++)
         {
            premiaBS.Add(Utils.blackFormula(Option.Type.Call, xBegin_[i], fwd_, atmVol_ * Math.Sqrt(T_), dDiscount_));
            premiaMKT.Add(Utils.blackFormula(Option.Type.Call, xBegin_[i], fwd_, yBegin_[i] * Math.Sqrt(T_), dDiscount_));
            vegas.Add(vega(xBegin_[i]));
         }
      }
      
      public override double value(double k) 
      {
         double x1 = vega(k)/vegas[0]
            * (Math.Log(xBegin_[1]/k) * Math.Log(xBegin_[2]/k))
            / (Math.Log(xBegin_[1]/xBegin_[0]) * Math.Log(xBegin_[2]/xBegin_[0]));
         double x2 = vega(k)/vegas[1]
            * (Math.Log(k/xBegin_[0]) * Math.Log(xBegin_[2]/k))
            / (Math.Log(xBegin_[1]/xBegin_[0]) * Math.Log(xBegin_[2]/xBegin_[1]));
         double x3 = vega(k)/vegas[2]
            * (Math.Log(k/xBegin_[0]) * Math.Log(k/xBegin_[1]))
            / (Math.Log(xBegin_[2]/xBegin_[0]) * Math.Log(xBegin_[2]/xBegin_[1]));

         double cBS = Utils.blackFormula(Option.Type.Call, k, fwd_, atmVol_ * Math.Sqrt(T_), dDiscount_);
         double c = cBS + x1*(premiaMKT[0] - premiaBS[0]) + x2*(premiaMKT[1] - premiaBS[1]) + x3*(premiaMKT[2] - premiaBS[2]);
         double std = Utils.blackFormulaImpliedStdDev(Option.Type.Call, k, fwd_, c, dDiscount_);
         return std / Math.Sqrt(T_);
      }

      public override double primitive(double x) 
      {
         Utils.QL_FAIL("Vanna Volga primitive not implemented");
         return 0;
      }

      public override double derivative(double x) 
      {
         Utils.QL_FAIL("Vanna Volga derivative not implemented");
         return 0;
      }
            
      public override double secondDerivative(double x) 
      {
         Utils.QL_FAIL("Vanna Volga secondDerivative not implemented");
         return 0;
      }

          
      private List<double> premiaBS;
      private List<double> premiaMKT;
      private List<double> vegas;
      private double atmVol_;
      private double spot_;
      private double fwd_;
      private double dDiscount_;
      private double fDiscount_;
      private double T_;

      private double vega(double k) 
      {
         double d1 = (Math.Log(fwd_/k) + 0.5 * Math.Pow(atmVol_, 2.0) * T_)/(atmVol_ * Math.Sqrt(T_));
         NormalDistribution norm = new NormalDistribution();
         return spot_ * dDiscount_ * Math.Sqrt(T_) * norm.value(d1);
      }
   
   }
   public class VannaVolgaInterpolation : Interpolation
   {
      /*! \pre the \f$ x \f$ values must be sorted. */
      public VannaVolgaInterpolation(List<double> xBegin, int size, List<double> yBegin,
         double spot,double dDiscount,double fDiscount,double T) 
      {
         impl_ = new VannaVolgaInterpolationImpl( xBegin, size, yBegin, spot, dDiscount, fDiscount, T);
         impl_.update();
      }

   }

   //! %VannaVolga-interpolation factory and traits
   public class VannaVolga 
   {
      public VannaVolga(double spot,double dDiscount,double fDiscount,double T)
      {
         spot_ = spot;
         dDiscount_ = dDiscount;
         fDiscount_ = fDiscount;
         T_ = T;
      }

      public Interpolation interpolate(List<double> xBegin, int size,List<double> yBegin) 
      {
         return new VannaVolgaInterpolation(xBegin, size, yBegin, spot_, dDiscount_, fDiscount_, T_);
      }
        
      public static int requiredPoints = 3;
      
      private double spot_;
      private double dDiscount_;
      private double fDiscount_;
      private double T_;
    }
}
