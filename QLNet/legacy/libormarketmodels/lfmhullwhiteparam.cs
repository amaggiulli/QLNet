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
    public class LfmHullWhiteParameterization : LfmCovarianceParameterization 
    {
        protected Matrix diffusion_, covariance_;
        protected List<double> fixingTimes_;

        public LfmHullWhiteParameterization(
                LiborForwardModelProcess process,
                OptionletVolatilityStructure capletVol,
                Matrix correlation, int factors)
            : base(process.size(), factors)
        {
            diffusion_  = new Matrix(size_-1, factors_);
            fixingTimes_= process.fixingTimes();

            Matrix sqrtCorr = new Matrix(size_ - 1, factors_, 1.0);
            if (correlation.empty()) {
                if(!(factors_ == 1))
                    throw new ApplicationException("correlation matrix must be given for "+
                                                    "multi factor models");
            } else {
                if(!(correlation.rows() == size_-1
                   && correlation.rows() == correlation.columns()))
                   throw new ApplicationException("wrong dimesion of the correlation matrix");

                if(!(factors_ <= size_-1))
                    throw new ApplicationException("too many factors for given LFM process");

                Matrix tmpSqrtCorr =MatrixUtilitites.pseudoSqrt(correlation,
                                               MatrixUtilitites.SalvagingAlgorithm.Spectral);

                // reduce to n factor model
                // "Reconstructing a valid correlation matrix from invalid data"
                // (<http://www.quarchome.org/correlationmatrix.pdf>)
                for (int i=0; i < size_-1; ++i) {
                    double d = 0;
                    tmpSqrtCorr.row(i).GetRange(0, factors_).ForEach((ii, vv) => d += vv*tmpSqrtCorr.row(i)[ii]);
                    //sqrtCorr.row(i).GetRange(0, factors_).ForEach((ii, vv) => sqrtCorr.row(i)[ii] = tmpSqrtCorr.row(i).GetRange(0, factors_)[ii] / Math.Sqrt(d));
                    for (int k = 0; k < factors_; ++k){
                        sqrtCorr[i, k] = tmpSqrtCorr.row(i).GetRange(0, factors_)[k] / Math.Sqrt(d);
                    }                    
                }
            }
            List<double> lambda=new List<double>();
            DayCounter dayCounter = process.index().dayCounter();
            List<double>  fixingTimes = process.fixingTimes();
            List<Date> fixingDates = process.fixingDates();

            for (int i = 1; i < size_; ++i) {
                double cumVar = 0.0;
                for (int j = 1; j < i; ++j) {
                    cumVar +=  lambda[i-j-1] * lambda[i-j-1]
                             * (fixingTimes[j+1] - fixingTimes[j]);
                }

                double vol =  capletVol.volatility(fixingDates[i], 0.0,false);
                double var = vol * vol
                    * capletVol.dayCounter().yearFraction(fixingDates[0],
                                                      fixingDates[i]);
                lambda.Add(Math.Sqrt(  (var - cumVar)
                                       / (fixingTimes[1] - fixingTimes[0])) );
                for (int q=0; q<factors_; ++q) {
                    diffusion_[i - 1, q]=sqrtCorr[i - 1, q] * lambda.Last() ;
                }
            }
            covariance_ = diffusion_ * Matrix.transpose(diffusion_);
        }

        public LfmHullWhiteParameterization(
                LiborForwardModelProcess process,
                OptionletVolatilityStructure capletVol)
            : this(process, capletVol, new Matrix(), 1) { }

        public override Matrix diffusion (double t){
            return diffusion(t, null);
        }

        public override Matrix diffusion (double t,Vector x) {
            Matrix tmp = new Matrix(size_, factors_, 0.0);
            int m = nextIndexReset(t);
            
            for (int k = m; k < size_; ++k) {
                for (int q = 0; q < factors_; ++q) {
                    tmp[k, q] = diffusion_[k - m, q];
                }
            }
            return tmp;
        }

        public override Matrix covariance(double t){
            return covariance(t,null);
        }

        public override Matrix covariance(double t, Vector x) {
            Matrix tmp = new Matrix(size_, size_, 0.0);
            int m = nextIndexReset(t);

            for (int k=m; k<size_; ++k) {
                for (int i=m; i<size_; ++i) {
                    tmp[k,i] = covariance_[k-m,i-m];
                }
            }
           return tmp;
        }

        public override Matrix integratedCovariance(double t){
            return integratedCovariance(t, null);
        }

        public override Matrix integratedCovariance(double t, Vector x) {
            Matrix tmp=new Matrix(size_, size_, 0.0);
            int last = fixingTimes_.BinarySearch(t);
            if (last < 0)
                //Lower_bound is a version of binary search: it attempts to find the element value in an ordered range [first, last)
                // [1]. Specifically, it returns the first position where value could be inserted without violating the ordering. 
                // [2] The first version of lower_bound uses operator< for comparison, and the second uses the function object comp.
                // lower_bound returns the furthermost iterator i in [first, last) such that, for every iterator j in [first, i), *j < value. 
                last = ~last;
            
            for (int i=0; i<=last; ++i) {
                double dt = ((i<last)? fixingTimes_[i+1] : t )
                    - fixingTimes_[i];

                for (int k=i; k<size_-1; ++k) {
                    for (int l=i; l<size_-1; ++l) {
                        tmp[k+1,l+1]+= covariance_[k-i,l-i]*dt;
                    }
                }
            }
            return tmp;
        }

        protected int nextIndexReset(double t) {
          //return std::upper_bound(fixingTimes_.begin(), fixingTimes_.end(), t)
          //       - fixingTimes_.begin();
            int result = fixingTimes_.BinarySearch(t);
            if (result < 0)
                // The upper_bound() algorithm finds the last position in a sequence that value can occupy 
                // without violating the sequence's ordering
                // if BinarySearch does not find value the value, the index of the next larger item is returned
                result = ~result -1;

            // impose limits. we need the one before last at max or the first at min
            result = Math.Max(Math.Min(result, fixingTimes_.Count - 2), 0);
            return result+1;
        }
    }
}
