/*
 Copyright (C) 2008, 2009 Siarhei Novik (snovik@gmail.com)
  
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

namespace QLNet {
    //! general linear least squares regression
    /*! References:
       "Numerical Recipes in C", 2nd edition,
        Press, Teukolsky, Vetterling, Flannery,

        \test the correctness of the returned values is tested by
              checking their properties.
    */
    public class LinearLeastSquaresRegression : LinearLeastSquaresRegression<double> {
        public LinearLeastSquaresRegression(List<double> x, List<double> y, List<Func<double, double>> v)
            : base(x,y,v) { }
    }
    
    public class LinearLeastSquaresRegression<ArgumentType> {
        private Vector a_, err_, residuals_, standardErrors_;

        public Vector coefficients() { return a_; }
        public Vector residuals() { return residuals_; }

        //! standard parameter errors as given by Excel, R etc.
        public Vector standardErrors() { return standardErrors_; }
        //! modeling uncertainty as definied in Numerical Recipes

        public Vector error() { return err_; }


        public LinearLeastSquaresRegression(List<ArgumentType> x, List<double> y, List<Func<ArgumentType, double>> v) {
            a_ = new Vector(v.Count, 0);
            err_ = new Vector(v.Count, 0);
            residuals_ = new Vector(x.Count, 0);
            standardErrors_ = new Vector(v.Count, 0);

            if (x.Count != y.Count) throw new ApplicationException("sample set need to be of the same size");
            if (!(x.Count >= v.Count)) throw new ApplicationException("sample set is too small");

            int i;
            int n = x.Count;
            int m = v.Count;

            Matrix A = new Matrix(n, m);
            for (i = 0; i < m; ++i)
                x.ForEach((jj, xx) => A[jj, i] = v[i](xx));

            SVD svd = new SVD(A);
            Matrix V = svd.V();
            Matrix U = svd.U();
            Vector w = svd.singularValues();
            double threshold = n*Const.QL_Epsilon;

            for (i=0; i<m; ++i) {
                if (w[i] > threshold) {
                    double u = 0;
                    U.column(i).ForEach((ii, vv) => u += vv * y[ii]);
                    u /= w[i];

                    for (int j=0; j<m; ++j) {
                        a_[j]  +=u*V[j,i];
                        err_[j]+=V[j,i]*V[j,i]/(w[i]*w[i]);
                    }
                }
            }
            err_ = Vector.Sqrt(err_);
            residuals_ = A * a_ - new Vector(y);

            double chiSq = residuals_.Sum(r => r * r);
            err_.ForEach((ii, vv) => standardErrors_[ii] = vv * Math.Sqrt(chiSq / (n - 2)));
        }
    }

    //! linear regression y_i = a_0 + a_1*x_0 +..+a_n*x_{n-1} + eps
    public class LinearRegression {
        private LinearLeastSquaresRegression<List<double>> reg_;


        //! one dimensional linear regression
        public LinearRegression(List<double> x, List<double> y) {
            reg_ = new LinearLeastSquaresRegression<List<double>>(argumentWrapper(x), y, linearFcts(1));
        }    

        //! multi dimensional linear regression
        public LinearRegression(List<List<double>> x, List<double> y) {
            reg_ = new LinearLeastSquaresRegression<List<double>>(x, y, linearFcts(x.Count));
        }

        //! returns paramters {a_0, a_1, ..., a_n}
        public Vector coefficients()   { return reg_.coefficients(); }

        public Vector residuals()      { return reg_.residuals(); }
        public Vector standardErrors()  { return reg_.standardErrors(); }


        class LinearFct {
            private int i_;  

            public LinearFct(int i) { 
                i_ = i;
            }
            
            public double value(List<double> x) {
                return x[i_]; 
            }
        }

        private List<Func<List<double>, double>> linearFcts(int dims) {
            List<Func<List<double>, double>> retVal = new List<Func<List<double>, double>>();
            retVal.Add(x => 1.0);
            
            for (int i=0; i < dims; ++i) {
                retVal.Add(new LinearFct(i).value);
            }
            
            return retVal;
        }

        private List<List<double>> argumentWrapper(List<double> x) {
            List<List<double>> retVal = new List<List<double>>();

            foreach(var v in x)
                retVal.Add(new List<double>() { v });
            
            return retVal;
        }
    };
}
