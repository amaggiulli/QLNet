/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
  
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
    public static partial class MatrixUtilities {
        public static Matrix CholeskyDecomposition(Matrix S, bool flexible) {
            int i, j, size = S.rows();

            if(size != S.columns())
                throw new ApplicationException("input matrix is not a square matrix");
            #if QL_EXTRA_SAFETY_CHECKS
            for (i=0; i<S.rows(); i++)
                for (j=0; j<i; j++)
                    QL_REQUIRE(S[i][j] == S[j][i],() =>
                               "input matrix is not symmetric");
            #endif

            Matrix result = new Matrix(size, size, 0.0);
            double sum;
            for (i=0; i<size; i++) {
                for (j=i; j<size; j++) {
                    sum = S[i,j];
                    for (int k=0; k<=i-1; k++) {
                        sum -= result[i,k]*result[j,k];
                    }
                    if (i == j) {
                        if (!(flexible || sum > 0.0))
                            throw new ApplicationException("input matrix is not positive definite");
                        // To handle positive semi-definite matrices take the
                        // square root of sum if positive, else zero.
                        result[i,i] = Math.Sqrt(Math.Max(sum, 0.0));
                    } else {
                        // With positive semi-definite matrices is possible
                        // to have result[i][i]==0.0
                        // In this case sum happens to be zero as well
                        result[j,i] = (sum==0.0 ? 0.0 : sum/result[i,i]);
                    }
                }
            }
            return result;
        }
    }
}
