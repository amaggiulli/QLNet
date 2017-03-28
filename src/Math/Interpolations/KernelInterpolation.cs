/*
 Copyright (C) 2008-2015  Andrea Maggiulli (a.maggiulli@gmail.com)

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
using System.Collections.Generic;

namespace QLNet
{
        
   public class KernelInterpolationImpl<Kernel> : Interpolation.templateImpl where Kernel: IKernelFunction
   {
      public KernelInterpolationImpl(List<double> xBegin, int size, List<double> yBegin,Kernel kernel)
         :base( xBegin, size, yBegin )
      {
         xSize_ = size;
         invPrec_ = 1.0e-7;
         M_ = new Matrix(xSize_,xSize_);
         alphaVec_ = new Vector(xSize_); 
         yVec_ = new Vector(xSize_);
         kernel_ = kernel;
      }

      public override void update() 
      {
         updateAlphaVec();
      }

      public override double value(double x) 
      {
         double res=0.0;
         for( int i=0; i< xSize_;++i)
         {
            res+=alphaVec_[i]*kernelAbs(x,this.xBegin_[i]);
         }
         return res/gammaFunc(x);
      }

      public override double primitive(double d)  
      {
         Utils.QL_FAIL("Primitive calculation not implemented for kernel interpolation");
         return 0;
      }

      public override double derivative(double d)  
      {
         Utils.QL_FAIL("First derivative calculation not implemented for kernel interpolation");
         return 0;
      }

      public override double secondDerivative(double d)  
      {
         Utils.QL_FAIL("Second derivative calculation not implemented for kernel interpolation");
         return 0;
      }

      // the calculation will solve y=M*a for a.  Due to
      // singularity or rounding errors the recalculation
      // M*a may not give y. Here, a failure will be thrown if
      // |M*a-y|>=invPrec_

      public void setInverseResultPrecision(double invPrec) { invPrec_=invPrec; }

      private double kernelAbs(double x1, double x2){ return kernel_.value(Math.Abs(x1-x2)); }

      private double gammaFunc(double x)
      {
         double res=0.0;

         for(int i=0; i< xSize_;++i)
         {
            res+=kernelAbs(x,this.xBegin_[i]);
         }
         return res;
      }

      private void updateAlphaVec()
      {
         // Function calculates the alpha vector with given
         // fixed pillars+values

         // Write Matrix M
         double tmp=0.0;

         for(int rowIt=0; rowIt<xSize_;++rowIt)
         {

            yVec_[rowIt]=this.yBegin_[rowIt];
            tmp=1.0/gammaFunc(this.xBegin_[rowIt]);

            for(int colIt=0; colIt<xSize_;++colIt)
            {
               M_[rowIt,colIt]=kernelAbs(this.xBegin_[rowIt],
                                          this.xBegin_[colIt])*tmp;
            }
         }

         // Solve y=M*\alpha for \alpha
         alphaVec_ = MatrixUtilities.qrSolve( M_, yVec_ );

         // check if inversion worked up to a reasonable precision.
         // I've chosen not to check determinant(M_)!=0 before solving
         Vector test = M_ * alphaVec_;
         Vector diffVec=Vector.Abs((M_*alphaVec_) - yVec_);

         for (int i=0; i<diffVec.size(); ++i) {
            Utils.QL_REQUIRE(diffVec[i] < invPrec_,()=>
                        "Inversion failed in 1d kernel interpolation");
         }
            
      }

      private int xSize_;
      private double invPrec_;
      private Matrix M_;
      private Vector alphaVec_, yVec_;
      private Kernel kernel_;
        
   }

   //! Kernel interpolation between discrete points
   /*! Implementation of the kernel interpolation approach, which can
      be found in "Foreign Exchange Risk" by Hakala, Wystup page
      256.

      The kernel in the implementation is kept general, although a Gaussian
      is considered in the cited text.
   */
   public class KernelInterpolation : Interpolation 
   {
   
      /*! \pre the \f$ x \f$ values must be sorted.
         \pre kernel needs a Real operator()(Real x) implementation
      */
      public KernelInterpolation(List<double> xBegin, int size, List<double> yBegin,IKernelFunction kernel) 
      {
         impl_ = new KernelInterpolationImpl<IKernelFunction>( xBegin, size, yBegin, kernel );
         impl_.update();
      }
   };

    
} 
