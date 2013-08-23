/*
 Copyright (C) 2009 Philippe Real (ph_real@hotmail.com)
  
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

namespace QLNet
{
    public class LmLinearExponentialCorrelationModel : LmCorrelationModel {
        
        private Matrix corrMatrix_, pseudoSqrt_;
        private int factors_;

        public LmLinearExponentialCorrelationModel(int size, double rho, double beta,
                                                    int factors)
        : base(size, 2)
        {
             corrMatrix_=new Matrix(size, size);
             factors_=factors;
             arguments_[0] = new ConstantParameter(rho, new BoundaryConstraint(-1.0, 1.0));
             arguments_[1] = new ConstantParameter(beta, new PositiveConstraint());
             generateArguments();
        }

        public LmLinearExponentialCorrelationModel(int size, double rho, double beta)
        : this(size, rho, beta,size){}

        public override Matrix correlation(double t, Vector x){
            Matrix tmp = new Matrix(corrMatrix_);
            return tmp;
        }

        public override Matrix correlation(double t){
            return correlation(t, null);
        }

        public override Matrix pseudoSqrt(double t, Vector x){
            Matrix tmp = new Matrix(pseudoSqrt_);
            return tmp;
        }

        public override Matrix pseudoSqrt(double t) {
            return pseudoSqrt(t, null);
        }


        public override double correlation(int i, int j, double t, Vector x) {
            return corrMatrix_[i,j];   
        }

        public override double correlation(int i, int j,double t){
            return correlation(i,j,t);
        }

        protected override void generateArguments(){
            double rho = arguments_[0].value (0.0);
            double beta= arguments_[1].value (0.0);

            for (int i=0; i<size_; ++i) {
                for (int j=i; j<size_; ++j) {
                    corrMatrix_[i,j] = corrMatrix_[j,i]
                        = rho + (1-rho)*Math.Exp (-beta*Math.Abs((double)i-(double)j));
                }
            }
           
            pseudoSqrt_ = MatrixUtilitites.rankReducedSqrt(corrMatrix_, factors_,
                1.0, MatrixUtilitites.SalvagingAlgorithm.None);

            corrMatrix_ = pseudoSqrt_ * Matrix.transpose(pseudoSqrt_);
        }
    }   
}   
