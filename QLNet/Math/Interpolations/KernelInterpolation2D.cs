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
   /*
     Grid Explanation:

     Grid=[  (x1,y1) (x1,y2) (x1,y3)... (x1,yM);
             (x2,y1) (x2,y2) (x2,y3)... (x2,yM);
             .
             .
             .
             (xN,y1) (xN,y2) (xN,y3)... (xN,yM);
          ]

     The Passed variables are:
     - x which is N dimensional
     - y which is M dimensional
     - zData which is NxM dimensional and has the z values
       corresponding to the grid above.
     - kernel is a template which needs a Real operator()(Real x) implementation
   */
   public class KernelInterpolation2DImpl<Kernel> : Interpolation2D.templateImpl where Kernel: IKernelFunction
   {
      public KernelInterpolation2DImpl(List<double> xBegin, int size, List<double> yBegin,int ySize,
                                      Matrix zData, Kernel kernel)
         :base( xBegin, size, yBegin,ySize,zData )
      {
         xSize_ = size;
         ySize_ = yBegin.Count;
         xySize_ = xSize_*ySize_; 
         invPrec_ = 1.0e-10;
         alphaVec_ = new Vector(xySize_); 
         yVec_ = new Vector(xySize_);
         M_ = new Matrix(xySize_,xySize_);
         kernel_ = kernel; 

         Utils.QL_REQUIRE(zData.rows()==xSize_,()=>
                     "Z value matrix has wrong number of rows");
         Utils.QL_REQUIRE( zData.columns() == ySize_, () =>
                     "Z value matrix has wrong number of columns");
      }

      public override void calculate() { updateAlphaVec(); }

      public override double value(double x1, double x2) 
      {
         double res=0.0;

         Vector X = new Vector(2),Xn = new Vector(2);
         X[0]=x1;X[1]=x2;

         int cnt=0; // counter

         for( int j=0; j< ySize_;++j){
            for( int i=0; i< xSize_;++i)
            {
               Xn[0]=this.xBegin_[i];
               Xn[1]=this.yBegin_[j];
               res+=alphaVec_[cnt]*kernelAbs(X,Xn);
               cnt++;
            }
         }
         return res/gammaFunc(X);
      }

      // the calculation will solve y=M*a for a.  Due to
      // singularity or rounding errors the recalculation
      // M*a may not give y. Here, a failure will be thrown if
      // |M*a-y|>=invPrec_
      void setInverseResultPrecision(double invPrec){invPrec_=invPrec;}

   
      // returns K(||X-Y||) where X,Y are vectors
      private double kernelAbs(Vector X, Vector Y)
      {
         return kernel_.value( vecNorm( X - Y ) );
      }

      private double vecNorm(Vector X)
      {
         return Math.Sqrt(Vector.DotProduct(X,X));
      }

      private double gammaFunc(Vector X)
      {
         double res=0.0;
         Vector Xn = new Vector(X.size());

         for(int j=0; j< ySize_;++j)
         {
            for(int i=0; i< xSize_;++i)
            {
               Xn[0]=this.xBegin_[i];
               Xn[1]=this.yBegin_[j];
               res+=kernelAbs(X,Xn);
            }
         }

         return res;
      }

      private void updateAlphaVec()
      {
         // Function calculates the alpha vector with given
         // fixed pillars+values

         Vector Xk = new Vector(2),Xn = new Vector(2);

         int rowCnt=0,colCnt=0;
         double tmpVar=0.0;

         // write y-vector and M-Matrix
         for(int j=0; j< ySize_;++j)
         {
            for(int i=0; i< xSize_;++i)
            {
               yVec_[rowCnt]=this.zData_[i,j];
               // calculate X_k
               Xk[0]=this.xBegin_[i];
               Xk[1]=this.yBegin_[j];

               tmpVar=1/gammaFunc(Xk);
               colCnt=0;

               for(int jM=0; jM< ySize_;++jM)
               {
                  for(int iM=0; iM< xSize_;++iM)
                  {
                     Xn[0]=this.xBegin_[iM];
                     Xn[1]=this.yBegin_[jM];
                     M_[rowCnt,colCnt]=kernelAbs(Xk,Xn)*tmpVar;
                     colCnt++; // increase column counter
                  }// end iM
               }// end jM
               rowCnt++; // increase row counter
            } // end i
         }// end j

         alphaVec_= MatrixUtilities.qrSolve(M_, yVec_);

         // check if inversion worked up to a reasonable precision.
         // I've chosen not to check determinant(M_)!=0 before solving

         Vector diffVec=Vector.Abs(M_*alphaVec_ - yVec_);
         for (int i=0; i<diffVec.size(); ++i) {
            Utils.QL_REQUIRE(diffVec[i]<invPrec_,()=>
                        "inversion failed in 2d kernel interpolation");
         }
      }

      private int xySize_; // xSize_,ySize_,
      private double invPrec_;
      private Vector alphaVec_, yVec_;
      private Matrix M_;
      private Kernel kernel_;
      
}

   /*! Implementation of the 2D kernel interpolation approach, which
         can be found in "Foreign Exchange Risk" by Hakala, Wystup page
         256.

         The kernel in the implementation is kept general, although a
         Gaussian is considered in the cited text.
   */
   public class KernelInterpolation2D : Interpolation2D
   {

      /*! \pre the \f$ x \f$ values must be sorted.
            \pre kernel needs a Real operator()(Real x) implementation
      
       */


      public KernelInterpolation2D(List<double> xBegin, int size, List<double> yBegin,int ySize,
                                      Matrix zData, IKernelFunction kernel) 
      {

         impl_ = new KernelInterpolation2DImpl<IKernelFunction>( xBegin, size, yBegin, ySize, zData, kernel );
         this.update();
      }
   }
}
