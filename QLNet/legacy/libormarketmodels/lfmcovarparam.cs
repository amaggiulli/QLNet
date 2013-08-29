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
    internal class Var_Helper
    {
        public LfmCovarianceParameterization param_;
        public int i_;
        public int j_;

        public Var_Helper(LfmCovarianceParameterization param, int i, int j) {
            param_ = param; 
            i_ = i; 
            j_ = j; 
        }

        public virtual double value(double t) {
            Matrix m = param_.diffusion(t, new Vector());
            double u = 0;
            m.row(i_).ForEach((ii, vv) => u += vv * m.row(j_)[ii]);
            return u;
        }
    }

    public abstract class LfmCovarianceParameterization
    {
        protected int size_;
        protected int factors_;

        public LfmCovarianceParameterization(int size, int factors)
        { size_ = size; factors_ = factors; }

        public int size() { return size_; }
        
        public int factors() { return factors_; }

        public abstract Matrix diffusion(double t);

        public abstract Matrix diffusion(double t, Vector x);

        public virtual Matrix covariance(double t){
            return covariance(t, null);
        }

        public virtual Matrix covariance(double t, Vector x) {
            Matrix sigma = this.diffusion(t, x);
            Matrix result = sigma * Matrix.transpose(sigma);
            return result;
        }

        public virtual Matrix integratedCovariance(double t){
            return integratedCovariance(t, null); 
        }

        public virtual Matrix integratedCovariance(double t, Vector x) {
            // this implementation is not intended for production.
            // because it is too slow and too inefficient.
            // This method is useful for testing and R&D.
            // Please overload the method within derived classes.
            
            //QL_REQUIRE(x.empty(), "can not handle given x here");
            try {
                if (!(x.empty()))
                    throw new ApplicationException("can not handle given x here");
            }
            catch { } //x is empty or null
            
            Matrix tmp= new Matrix(size_, size_,0.0);

            for (int i=0; i<size_; ++i) {
                for (int j=0; j<=i;++j) {
                    Var_Helper helper = new Var_Helper(this, i, j);
                    GaussKronrodAdaptive integrator=new GaussKronrodAdaptive(1e-10, 10000);
                    for (int k=0; k < 64; ++k) {
                        tmp[i,j] +=integrator.value(helper.value, k * t / 64.0, (k + 1) * t / 64.0);
                    }
                    tmp[j,i]=tmp[i,j];
                }
            }
            return tmp;
        }

        //private Var_Helper varHelper_;
    }

}
