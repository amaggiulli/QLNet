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
    public class LfmCovarianceProxy : LfmCovarianceParameterization
    {
        public LmVolatilityModel volaModel;
        public LmCorrelationModel corrModel;
        public LmVolatilityModel volaModel_;
        public LmCorrelationModel corrModel_;

        public LfmCovarianceProxy(LmVolatilityModel volaModel,
                           LmCorrelationModel corrModel)
            : base(corrModel.size(), corrModel.factors()){
            volaModel_=volaModel;
            corrModel_=corrModel ;

            if(!(volaModel_.size() == corrModel_.size()))
                throw new ArgumentException("different size for the volatility (" + volaModel_.size() +
                                            ") and correlation (" + corrModel_.size() +
                                            ") models");
        }

        public LmVolatilityModel volatilityModel() {
            return volaModel_;
        }

        public LmCorrelationModel correlationModel() {
            return corrModel_;
        }

        public override Matrix diffusion(double t){
            return diffusion(t, null);
        }

        public override Matrix diffusion(double t, Vector x) {
            Matrix pca = corrModel_.pseudoSqrt(t, x);
            Vector  vol = volaModel_.volatility(t, x);
            for (int i=0; i<size_; ++i) {
                /*transform(pca.row_begin(i), pca.row_end(i),
                           pca.row_begin(i),
                           bind2nd(multiplies<double>(), vol[i]));*/
                for (int j = 0; j < size_; ++j)
                    pca[i,j] =   pca[i,j] * vol[i];
                //pca.column(i).ForEach((ii, v) => v *= vol[ii]);   
            }
            return pca;
        }

        public override Matrix covariance(double t, Vector x) {
            Vector  volatility  = volaModel_.volatility(t, x);
            Matrix correlation = corrModel_.correlation(t, x);

            Matrix tmp=new Matrix(size_, size_);
            for (int i=0; i<size_; ++i) {
                for (int j = 0; j < size_; ++j){
                    tmp[i,j] = volatility[i]*correlation[i,j]*volatility[j];
                }
            }
            return tmp;
        }

        public double integratedCovariance(int i, int j, double t) { 
            return integratedCovariance(i, j, t, new Vector()); }

        public double integratedCovariance(int i, int j, double t,Vector x)  {
            if (corrModel_.isTimeIndependent()) {
            try {
                // if all objects support these methods
                // thats by far the fastest way to get the
                // integrated covariance
                return corrModel_.correlation(i, j, 0.0, x)
                       *volaModel_.integratedVariance(j, i, t, x);
                //verifier la methode integratedVariance, qui bdoit etre implémenté
                }
                catch (System.Exception) {
                // okay proceed with the
                // slow numerical integration routine
                }
            }
            
            //QL_REQUIRE(x.empty(), "can not handle given x here");
            try {
                if (x.empty() == false)
                    throw new ApplicationException("can not handle given x here");
            }
            catch { //OK x empty
            }

            double tmp=0.0;
            VarProxy_Helper helper = new VarProxy_Helper(this, i, j);

            GaussKronrodAdaptive integrator=new GaussKronrodAdaptive(1e-10, 10000);
            for (int k=0; k<64; ++k) {
                tmp+=integrator.value(helper.value, k*t/64.0, (k+1)*t/64.0);
            }
            return tmp;
        }
    }

    public class VarProxy_Helper
    {
        private int i_, j_;
        public LmVolatilityModel volaModel_;
        public LmCorrelationModel corrModel_;

        public VarProxy_Helper(LfmCovarianceProxy proxy, int i, int j) {
            i_=i;
            j_=j;
            volaModel_=proxy.volaModel_;
            corrModel_=proxy.corrModel_;
        }
        
        public double value(double t) {
            double v1, v2;
            if (i_ == j_) {
                v1 = v2 = volaModel_.volatility(i_, t,null);
            } 
            else {
                v1 = volaModel_.volatility(i_, t, null);
                v2 = volaModel_.volatility(j_, t, null);
            }
            return v1 * corrModel_.correlation(i_, j_, t, null) * v2;
        }
    }
}
