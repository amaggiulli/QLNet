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

using System.Collections.Generic;

namespace QLNet
{
           
   public class AbcdCoeffHolder 
   {
      public AbcdCoeffHolder(double? a,
                             double? b,
                             double? c,
                             double? d,
                             bool aIsFixed,
                             bool bIsFixed,
                             bool cIsFixed,
                             bool dIsFixed)
      {
         a_ = a; 
         b_ = b; 
         c_ = c; 
         d_ = d;
         aIsFixed_ = false;
         bIsFixed_ = false;
         cIsFixed_ = false;
         dIsFixed_ = false;
         k_ = new List<double>();
         error_ = null;
         maxError_ = null;
         abcdEndCriteria_ = EndCriteria.Type.None;

         if (a_ != null)
            aIsFixed_ = aIsFixed;
         else a_ = -0.06;
         if (b_ != null)
            bIsFixed_ = bIsFixed;
         else b_ = 0.17;
         if (c_ != null)
            cIsFixed_ = cIsFixed;
         else c_ = 0.54;
         if (d_ != null)
            dIsFixed_ = dIsFixed;
         else d_ = 0.17;

         AbcdMathFunction.validate(a.Value, b.Value, c.Value, d.Value);

      }

      public double? a_, b_, c_, d_;
      public bool aIsFixed_, bIsFixed_, cIsFixed_, dIsFixed_;
      public List<double> k_;
      public double? error_, maxError_;
      public EndCriteria.Type abcdEndCriteria_;
   }
           
   public class AbcdInterpolationImpl : Interpolation.templateImpl   
   {
      public AbcdInterpolationImpl( List<double> xBegin, int size, List<double> yBegin,
                                    double a, double b, double c, double d,
                                    bool aIsFixed,
                                    bool bIsFixed,
                                    bool cIsFixed,
                                    bool dIsFixed,
                                    bool vegaWeighted,
                                    EndCriteria endCriteria,
                                    OptimizationMethod optMethod)
            : base(xBegin, size, yBegin)
      {
         abcdCoeffHolder_ = new AbcdCoeffHolder(a, b, c, d, aIsFixed, bIsFixed, cIsFixed, dIsFixed);
         endCriteria_ = endCriteria;
         optMethod_ = optMethod;
         vegaWeighted_ = vegaWeighted;
      }

            
      public override void update() 
      {
         List<double> times = new List<double>(), blackVols = new List<double>();
         for ( int i = 0; i < xBegin_.Count; ++i) 
         {
            times.Add(xBegin_[i]);
            blackVols.Add(yBegin_[i]);
         }
         
         abcdCalibrator_ = new AbcdCalibration(times, blackVols,
                                               abcdCoeffHolder_.a_.Value, 
                                               abcdCoeffHolder_.b_.Value, 
                                               abcdCoeffHolder_.c_.Value, 
                                               abcdCoeffHolder_.d_.Value,
                                               abcdCoeffHolder_.aIsFixed_, 
                                               abcdCoeffHolder_.bIsFixed_,
                                               abcdCoeffHolder_.cIsFixed_, 
                                               abcdCoeffHolder_.dIsFixed_,
                                               vegaWeighted_,
                                               endCriteria_,
                                               optMethod_);
  
         abcdCalibrator_.compute();
         abcdCoeffHolder_.a_ = abcdCalibrator_.a();
         abcdCoeffHolder_.b_ = abcdCalibrator_.b();
         abcdCoeffHolder_.c_ = abcdCalibrator_.c();
         abcdCoeffHolder_.d_ = abcdCalibrator_.d();
         abcdCoeffHolder_.k_ = abcdCalibrator_.k(times, blackVols);
         abcdCoeffHolder_.error_ = abcdCalibrator_.error();
         abcdCoeffHolder_.maxError_ = abcdCalibrator_.maxError();
         abcdCoeffHolder_.abcdEndCriteria_ = abcdCalibrator_.endCriteria();
      }
      
      public override double value(double x) 
      {
         Utils.QL_REQUIRE(x>=0.0,()=> "time must be non negative: " + x + " not allowed");
         return abcdCalibrator_.value(x);
      }

      public override double primitive(double x) 
      {
         Utils.QL_FAIL("Abcd primitive not implemented");
         return 0;
      }
      
      public override double derivative(double x)  
      {
         Utils.QL_FAIL("Abcd derivative not implemented");
         return 0;
      }
      
      public override double secondDerivative(double x) 
      {
         Utils.QL_FAIL("Abcd secondDerivative not implemented");
         return 0;
      }
      
      public double k(double t) 
      {
         LinearInterpolation li = new LinearInterpolation(this.xBegin_, this.size_, this.yBegin_);
         return li.value(t);
      }

      public AbcdCoeffHolder AbcdCoeffHolder() {return abcdCoeffHolder_;}
      private EndCriteria endCriteria_;
      private OptimizationMethod optMethod_;
      private bool vegaWeighted_;
      private AbcdCalibration abcdCalibrator_;
      private AbcdCoeffHolder abcdCoeffHolder_;

   }
    
   //! %Abcd interpolation between discrete points.
   /*! \ingroup interpolations */
   public class AbcdInterpolation : Interpolation 
   {
      /*! Constructor */
      public AbcdInterpolation(List<double> xBegin, int size, List<double> yBegin,
                               double a = -0.06,
                               double b =  0.17,
                               double c =  0.54,
                               double d =  0.17,
                               bool aIsFixed = false,
                               bool bIsFixed = false,
                               bool cIsFixed = false,
                               bool dIsFixed = false,
                               bool vegaWeighted = false,
                               EndCriteria endCriteria = null,
                               OptimizationMethod optMethod = null) 
   {
         impl_ = new AbcdInterpolationImpl(xBegin, size, yBegin,
                                           a, b, c, d,
                                           aIsFixed, bIsFixed,
                                           cIsFixed, dIsFixed,
                                           vegaWeighted,
                                           endCriteria,
                                           optMethod);
         impl_.update();
         coeffs_ = ((AbcdInterpolationImpl)impl_).AbcdCoeffHolder();
        }

      //! \name Inspectors
      //@{
      public double? a() { return coeffs_.a_; }
      public double? b() { return coeffs_.b_; }
      public double? c() { return coeffs_.c_; }
      public double? d() { return coeffs_.d_; }
      public List<double> k() { return coeffs_.k_; }
      public double? rmsError()  { return coeffs_.error_; }
      public double? maxError()  { return coeffs_.maxError_; }
      public EndCriteria.Type endCriteria(){ return coeffs_.abcdEndCriteria_; }
      public double k(double t, List<double> xBegin, int size) 
      {
         LinearInterpolation li = new LinearInterpolation(xBegin, size, coeffs_.k_);
         return li.value(t);
      }

      private AbcdCoeffHolder coeffs_;
    
   }

   //! %Abcd interpolation factory and traits
   /*! \ingroup interpolations */
   public class Abcd 
   {
      public Abcd(double a, double b, double c, double d,
                  bool aIsFixed, bool bIsFixed,
                  bool cIsFixed, bool dIsFixed,
                  bool vegaWeighted = false,
                  EndCriteria endCriteria = null,
                  OptimizationMethod optMethod = null)
      {
         a_ = a; 
         b_ = b; 
         c_ = c; 
         d_ = d;
         aIsFixed_  = aIsFixed; 
         bIsFixed_  = bIsFixed;
         cIsFixed_  = cIsFixed; 
         dIsFixed_  = dIsFixed;
         vegaWeighted_ = vegaWeighted;
         endCriteria_ = endCriteria;
         optMethod_ = optMethod;
      }
        
      public Interpolation interpolate(List<double> xBegin, int size,List<double> yBegin) 
      {
            return new AbcdInterpolation(xBegin, size, yBegin,
                                         a_, b_, c_, d_,
                                         aIsFixed_, bIsFixed_,
                                         cIsFixed_, dIsFixed_,
                                         vegaWeighted_,
                                         endCriteria_, optMethod_);
      }
        
      public  bool global = true;

      private double a_, b_, c_, d_;
      private bool aIsFixed_, bIsFixed_, cIsFixed_, dIsFixed_;
      private bool vegaWeighted_;
      private EndCriteria endCriteria_;
      private OptimizationMethod optMethod_;
   }
}
