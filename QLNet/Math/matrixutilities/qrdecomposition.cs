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
        public static List<int> qrDecomposition(Matrix M, Matrix q, Matrix r, bool pivot) {
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
            //    std::fill(r.row_begin(i), r.row_begin(i)+i, 0.0);
                r[i, i] = rdiag[i];
                if (i < m) {
            //        std::copy(mT.column_begin(i)+i+1, mT.column_end(i),
            //                  r.row_begin(i)+i+1);
                }
                else {
            //        std::fill(r.row_begin(i)+i+1, r.row_end(i), 0.0);
                }
            }

            if (q.rows() != m || q.columns() != n)
                q = new Matrix(m, n);

            Vector w = new Vector(m);
            //for (int k=0; k < m; ++k) {
            //    std::fill(w.begin(), w.end(), 0.0);
            //    w[k] = 1.0;

            //    for (int j=0; j < Math.Min(n, m); ++j) {
            //        double t3 = mT[j,j];
            //        if (t3 != 0.0) {
            //            double t
            //                = std::inner_product(mT.row_begin(j)+j, mT.row_end(j),
            //                                     w.begin()+j, 0.0)/t3;
            //            for (int i=j; i<m; ++i) {
            //                w[i]-=mT[j,i]*t;
            //            }
            //        }
            //        q[k,j] = w[j];
            //    }
            //    std::fill(q.row_begin(k) + Math.Min(n, m), q.row_end(k), 0.0);
            //}

            List<int> ipvt = new InitializedList<int>(n);
            //if (pivot) {
            //    std::copy(lipvt.get(), lipvt.get()+n, ipvt.begin());
            //}
            //else {
            //    for (int i=0; i < n; ++i)
            //        ipvt[i] = i;
            //}

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
        //public Vector qrSolve(Matrix a, Vector b, bool pivot = true, Vector d = Array()) {
        //public Vector qrSolve(Matrix a, Vector b, bool pivot, Vector d) {
        //    int m = a.rows();
        //    int n = a.columns();

        //    QL_REQUIRE(b.size() == m, "dimensions of A and b don't match");
        //    QL_REQUIRE(d.size() == n || d.empty(),
        //               "dimensions of A and d don't match");

        //    Matrix q(m, n), r(n, n);

        //    std::vector<Size> lipvt = qrDecomposition(a, q, r, pivot);
        //    boost::scoped_array<int> ipvt(new int[n]);
        //    std::copy(lipvt.begin(), lipvt.end(), ipvt.get());

        //    Matrix aT = Matrix.transpose(a);
        //    Matrix rT = Matrix.transpose(r);

        //    boost::scoped_array<double> sdiag(new double[n]);
        //    boost::scoped_array<double> wa(new double[n]);

        //    Array ld(n, 0.0);
        //    if (!d.empty()) {
        //        std::copy(d.begin(), d.end(), ld.begin());
        //    }
        //    Array x(n);
        //    Array qtb = transpose(q)*b;

        //    MINPACK.qrsolv(n, rT.begin(), n, ipvt.get(),
        //                    ld.begin(), qtb.begin(),
        //                    x.begin(), sdiag.get(), wa.get());

        //    return x;
        //}
    }
}