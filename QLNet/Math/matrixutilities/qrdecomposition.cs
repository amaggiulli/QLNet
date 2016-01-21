/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
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

namespace QLNet {
    public static partial class MatrixUtilities {
        //! QR decompoisition
        /*! This implementation is based on MINPACK
            (<http://www.netlib.org/minpack>,
            <http://www.netlib.org/cephes/linalg.tgz>)

            This subroutine uses householder transformations with column
            pivoting (optional) to compute a qr factorization of the
            m by n matrix A. That is, qrfac determines an orthogonal
            matrix q, a permutation matrix p, and an upper trapezoidal
            matrix r with diagonal elements of nonincreasing magnitude,
            such that A*p = q*r.

            Return value ipvt is an integer array of length n, which
            defines the permutation matrix p such that A*p = q*r.
            Column j of p is column ipvt(j) of the identity matrix.

            See lmdiff.cpp for further details.
        */
        //public static List<int> qrDecomposition(Matrix A, Matrix q, Matrix r, bool pivot = true) {
        public static List<int> qrDecomposition(Matrix M, ref Matrix q, ref Matrix r, bool pivot) {
            Matrix mT = Matrix.transpose(M);
            int m = M.rows();
            int n = M.columns();

            List<int> lipvt = new InitializedList<int>(n);
            Vector rdiag = new Vector(n);
            Vector wa = new Vector(n);

            MINPACK.qrfac(m, n, mT, 0, (pivot)?1:0, ref lipvt, n, ref rdiag, ref rdiag, wa);

            if (r.columns() != n || r.rows() !=n)
                r = new Matrix(n, n);

            for (int i=0; i < n; ++i) {
                r[i, i] = rdiag[i];
                if (i < m) {
                   for ( int j = i; j < mT.rows()-1; j++ )
                      r[i, j + 1] = mT[j+1, i];
                }
            }

            if (q.rows() != m || q.columns() != n)
                q = new Matrix(m, n);

            Vector w = new Vector(m);
            for (int k=0; k < m; ++k) 
            {
                w.Erase();
                w[k] = 1.0;

                for (int j=0; j < Math.Min(n, m); ++j) 
                {
                    double t3 = mT[j,j];
                    if (t3 != 0.0) 
                    {
                       double t = 0;
                       for ( int kk = j ; kk < mT.columns(); kk++ )
                          t += ( mT[j,kk] * w[kk] ) / t3 ; 

                       for (int i=j; i<m; ++i) 
                       {
                          w[i]-=mT[j,i]*t;
                       }
                    }
                    q[k,j] = w[j];
                }
            }

            List<int> ipvt = new InitializedList<int>(n);
            if (pivot) {
               for ( int i = 0; i < n; ++i )
                  ipvt[i] = lipvt[i];
            }
            else {
                for (int i=0; i < n; ++i)
                    ipvt[i] = i;
            }

           return ipvt;
        }

        //! QR Solve
        /*! This implementation is based on MINPACK
            (<http://www.netlib.org/minpack>,
            <http://www.netlib.org/cephes/linalg.tgz>)

            Given an m by n matrix A, an n by n diagonal matrix d,
            and an m-vector b, the problem is to determine an x which
            solves the system

            A*x = b ,     d*x = 0 ,

            in the least squares sense.

            d is an input array of length n which must contain the
            diagonal elements of the matrix d.

            See lmdiff.cpp for further details.
        */
        public static Vector qrSolve( Matrix a, Vector b, bool pivot = true, Vector d = null )
        {
           int m = a.rows();
           int n = a.columns();
           if ( d == null ) d = new Vector();
           Utils.QL_REQUIRE( b.Count == m, () => "dimensions of A and b don't match" );
           Utils.QL_REQUIRE( d.Count == n || d.empty(), () => "dimensions of A and d don't match" );

           Matrix q = new Matrix( m, n ), r = new Matrix( n, n );

           List<int> lipvt = MatrixUtilities.qrDecomposition( a, ref q, ref r, pivot );
           List<int> ipvt = new List<int>( n );
           ipvt = lipvt;

           //std::copy(lipvt.begin(), lipvt.end(), ipvt.get());

           Matrix aT = Matrix.transpose( a );
           Matrix rT = Matrix.transpose( r );

           Vector sdiag = new Vector( n );
           Vector wa = new Vector( n );

           Vector ld = new Vector( n, 0.0 );
           if ( !d.empty() )
           {
              ld = d;
              //std::copy(d.begin(), d.end(), ld.begin());
           }
           Vector x = new Vector( n );
           Vector qtb = Matrix.transpose( q ) * b;

           MINPACK.qrsolv( n, rT, n, ipvt, ld, qtb, x, sdiag, wa );

           return x;
        }
    }
}