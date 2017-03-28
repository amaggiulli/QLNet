/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2015  Andrea Maggiulli (a.maggiulli@gmail.com)
  
 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

 QLNet is free software: you can redistribute it and/or modify it
 under the terms of the QLNet license.  You should have received a
 copy of the license along with this program; if not, license is  
 available online at <https://github.com/amaggiulli/qlnetLicense.html>.
  
 QLNet is a based on QuantLib, a free-software/open-source library
 for financial quantitative analysts and developers - http://quantlib.org/
 The QuantLib license is available online at http://quantlib.org/license.shtml.
 
 This program is distributed in the hope that it will be useful, but WITHOUT
 ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 FOR A PARTICULAR PURPOSE.  See the license for more details.
*/
using System;
using System.Collections.Generic;
#if QL_DOTNET_FRAMEWORK
using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
   using Xunit;
#endif
using QLNet;

namespace TestSuite {
#if QL_DOTNET_FRAMEWORK
   [TestClass()]
#endif
    public class T_Matrices {

        int N;
        Matrix M1, M2, M3, M4, M5, M6, M7, I;

        double norm(Vector v) {
            return Math.Sqrt(Vector.DotProduct(v,v));
        }

        double norm(Matrix m) {
            double sum = 0.0;
            for (int i=0; i<m.rows(); i++)
                for (int j=0; j<m.columns(); j++)
                    sum += m[i,j]*m[i,j];
            return Math.Sqrt(sum);
        }

        void setup() {

            N = 3;
            M1 = new Matrix(N,N); M2 = new Matrix(N,N); I = new Matrix(N,N);
            M3 = new Matrix(3, 4);
            M4 = new Matrix(4, 3);
            M5 = new Matrix(4, 4);
            M6 = new Matrix(4, 4);

            M1[0,0] = 1.0;  M1[0,1] = 0.9;  M1[0,2] = 0.7;
            M1[1,0] = 0.9;  M1[1,1] = 1.0;  M1[1,2] = 0.4;
            M1[2,0] = 0.7;  M1[2,1] = 0.4;  M1[2,2] = 1.0;

            M2[0,0] = 1.0;  M2[0,1] = 0.9;  M2[0,2] = 0.7;
            M2[1,0] = 0.9;  M2[1,1] = 1.0;  M2[1,2] = 0.3;
            M2[2,0] = 0.7;  M2[2,1] = 0.3;  M2[2,2] = 1.0;

            I[0,0] = 1.0;  I[0,1] = 0.0;  I[0,2] = 0.0;
            I[1,0] = 0.0;  I[1,1] = 1.0;  I[1,2] = 0.0;
            I[2,0] = 0.0;  I[2,1] = 0.0;  I[2,2] = 1.0;

            M3[0,0] = 1; M3[0,1] = 2; M3[0,2] = 3; M3[0,3] = 4;
            M3[1,0] = 2; M3[1,1] = 0; M3[1,2] = 2; M3[1,3] = 1;
            M3[2,0] = 0; M3[2,1] = 1; M3[2,2] = 0; M3[2,3] = 0;

            M4[0,0] = 1;  M4[0,1] = 2;  M4[0,2] = 400;
            M4[1,0] = 2;  M4[1,1] = 0;  M4[1,2] = 1;
            M4[2,0] = 30; M4[2,1] = 2;  M4[2,2] = 0;
            M4[3,0] = 2;  M4[3,1] = 0;  M4[3,2] = 1.05;

            // from Higham - nearest correlation matrix
            M5[0,0] = 2;   M5[0,1] = -1;  M5[0,2] = 0.0; M5[0,3] = 0.0;
            M5[1,0] = M5[0,1];  M5[1,1] = 2;   M5[1,2] = -1;  M5[1,3] = 0.0;
            M5[2,0] = M5[0,2]; M5[2,1] = M5[1,2];  M5[2,2] = 2;   M5[2,3] = -1;
            M5[3,0] = M5[0,3]; M5[3,1] = M5[1,3]; M5[3,2] = M5[2,3];  M5[3,3] = 2;

            // from Higham - nearest correlation matrix to M5
            M6[0,0] = 1;        M6[0,1] = -0.8084124981;  M6[0,2] = 0.1915875019;   M6[0,3] = 0.106775049;
            M6[1,0] = M6[0,1]; M6[1,1] = 1;        M6[1,2] = -0.6562326948;  M6[1,3] = M6[0,2];
            M6[2,0] = M6[0,2]; M6[2,1] = M6[1,2]; M6[2,2] = 1;        M6[2,3] = M6[0,1];
            M6[3,0] = M6[0,3]; M6[3,1] = M6[1,3]; M6[3,2] = M6[2,3]; M6[3,3] = 1;

            M7 = new Matrix(M1);
            M7[0,1] = 0.3; M7[0,2] = 0.2; M7[2,1] = 1.2;
        }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
        public void testEigenvectors() {
            //("Testing eigenvalues and eigenvectors calculation...");

            setup();

            Matrix[] testMatrices = { M1, M2 };

            for (int k=0; k<testMatrices.Length; k++) {

                Matrix M = testMatrices[k];
                SymmetricSchurDecomposition dec = new SymmetricSchurDecomposition(M);
                Vector eigenValues = dec.eigenvalues();
                Matrix eigenVectors = dec.eigenvectors();
                double minHolder = double.MaxValue;

                for (int i=0; i<N; i++) {
                    Vector v = new Vector(N);
                    for (int j = 0; j < N; j++)
                        v[j] = eigenVectors[j,i];
                    // check definition
                    Vector a = M * v;
                    Vector b = eigenValues[i] * v;
                    if (norm(a-b) > 1.0e-15)
                        QAssert.Fail("Eigenvector definition not satisfied");
                    // check decreasing ordering
                    if (eigenValues[i] >= minHolder) {
                        QAssert.Fail("Eigenvalues not ordered: " + eigenValues);
                    } else
                        minHolder = eigenValues[i];
                }

                // check normalization
                Matrix m = eigenVectors * Matrix.transpose(eigenVectors);
                if (norm(m-I) > 1.0e-15)
                    QAssert.Fail("Eigenvector not normalized");
            }
        }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
        public void testSqrt() {

            //BOOST_MESSAGE("Testing matricial square root...");

            setup();

            Matrix m = MatrixUtilitites.pseudoSqrt(M1, MatrixUtilitites.SalvagingAlgorithm.None);
            Matrix temp = m*Matrix.transpose(m);
            double error = norm(temp - M1);
            double tolerance = 1.0e-12;
            if (error>tolerance) {
                QAssert.Fail("Matrix square root calculation failed\n"
                           + "original matrix:\n" + M1
                           + "pseudoSqrt:\n" + m
                           + "pseudoSqrt*pseudoSqrt:\n" + temp
                           + "\nerror:     " + error
                           + "\ntolerance: " + tolerance);
            }
        }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
        public void testHighamSqrt() {
            //BOOST_MESSAGE("Testing Higham matricial square root...");

            setup();

            Matrix tempSqrt = MatrixUtilitites.pseudoSqrt(M5, MatrixUtilitites.SalvagingAlgorithm.Higham);
            Matrix ansSqrt = MatrixUtilitites.pseudoSqrt(M6, MatrixUtilitites.SalvagingAlgorithm.None);
            double error = norm(ansSqrt - tempSqrt);
            double tolerance = 1.0e-4;
            if (error>tolerance) {
                QAssert.Fail("Higham matrix correction failed\n"
                           + "original matrix:\n" + M5
                           + "pseudoSqrt:\n" + tempSqrt
                           + "should be:\n" + ansSqrt
                           + "\nerror:     " + error
                           + "\ntolerance: " + tolerance);
            }
        }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
        public void testSVD() {

            //BOOST_MESSAGE("Testing singular value decomposition...");

            setup();

            double tol = 1.0e-12;
            Matrix[] testMatrices = { M1, M2, M3, M4 };

            for (int j = 0; j < testMatrices.Length; j++) {
                // m >= n required (rows >= columns)
                Matrix A = testMatrices[j];
                SVD svd = new SVD(A);
                // U is m x n
                Matrix U = svd.U();
                // s is n long
                Vector s = svd.singularValues();
                // S is n x n
                Matrix S = svd.S();
                // V is n x n
                Matrix V = svd.V();

                for (int i=0; i < S.rows(); i++) {
                    if (S[i,i] != s[i])
                        QAssert.Fail("S not consistent with s");
                }

                // tests
                Matrix U_Utranspose = Matrix.transpose(U)*U;
                if (norm(U_Utranspose-I) > tol)
                    QAssert.Fail("U not orthogonal (norm of U^T*U-I = " + norm(U_Utranspose-I) + ")");

                Matrix V_Vtranspose = Matrix.transpose(V) * V;
                if (norm(V_Vtranspose-I) > tol)
                    QAssert.Fail("V not orthogonal (norm of V^T*V-I = " + norm(V_Vtranspose-I) + ")");

                Matrix A_reconstructed = U * S * Matrix.transpose(V);
                if (norm(A_reconstructed-A) > tol)
                    QAssert.Fail("Product does not recover A: (norm of U*S*V^T-A = " + norm(A_reconstructed-A) + ")");
            }
        }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
        public void testQRDecomposition() 
        {

         // Testing QR decomposition...

         setup();

         double tol = 1.0e-12;
         Matrix[] testMatrices = { M1, M2, I,
                                    M3, Matrix.transpose(M3), M4, Matrix.transpose(M4), M5 };

         for (int j = 0; j < testMatrices.Length; j++) {
               Matrix Q = new Matrix(), R = new Matrix();
               bool pivot = true;
               Matrix A = testMatrices[j];
               List<int> ipvt = MatrixUtilities.qrDecomposition(A, ref Q, ref R, pivot);

               Matrix P = new Matrix(A.columns(), A.columns(), 0.0);

               // reverse column pivoting
               for (int i=0; i < P.columns(); ++i) {
                  P[ipvt[i],i] = 1.0;
               }

               if (norm(Q*R - A*P) > tol)
                  QAssert.Fail("Q*R does not match matrix A*P (norm = "
                              + norm(Q*R-A*P) + ")");

               pivot = false;
               MatrixUtilities.qrDecomposition(A, ref Q, ref R, pivot);

               if (norm(Q*R - A) > tol)
                  QAssert.Fail("Q*R does not match matrix A (norm = "
                              + norm(Q*R-A) + ")");
    
         }
        }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
        public void testQRSolve()
        {
           // Testing QR solve...
           setup();

           double tol = 1.0e-12;
           MersenneTwisterUniformRng rng = new MersenneTwisterUniformRng( 1234 );
           Matrix bigM = new Matrix( 50, 100, 0.0 );
           for ( int i = 0; i < Math.Min( bigM.rows(), bigM.columns() ); ++i )
           {
              bigM[i, i] = i + 1.0;
           }
           Matrix[] testMatrices = { M1, M2, M3, Matrix.transpose(M3),
                                    M4, Matrix.transpose(M4), M5, I, M7, bigM, Matrix.transpose(bigM) };

           for ( int j = 0; j < testMatrices.Length; j++ )
           {
              Matrix A = testMatrices[j];
              Vector b = new Vector( A.rows() );

              for ( int k = 0; k < 10; ++k )
              {
                 for ( int i = 0; i < b.Count; ++i )
                 {
                    b[i] = rng.next().value;
                 }
                 Vector x = MatrixUtilities.qrSolve( A, b, true );

                 if ( A.columns() >= A.rows() )
                 {
                    if ( norm( A * x - b ) > tol )
                       QAssert.Fail( "A*x does not match vector b (norm = "
                                  + norm( A * x - b ) + ")" );
                 }
                 else
                 {
                    // use the SVD to calculate the reference values
                    int n = A.columns();
                    Vector xr = new Vector( n, 0.0 );

                    SVD svd = new SVD( A );
                    Matrix V = svd.V();
                    Matrix U = svd.U();
                    Vector w = svd.singularValues();
                    double threshold = n * Const.QL_EPSILON;

                    for ( int i = 0; i < n; ++i )
                    {
                       if ( w[i] > threshold )
                       {
                          double u = 0;
                          int zero = 0;
                          for ( int kk = 0; kk < U.rows(); kk++ )
                             u += ( U[kk, i] * b[zero++] ) / w[i];

                          for ( int jj = 0; jj < n; ++jj )
                          {
                             xr[jj] += u * V[jj, i];
                          }
                       }
                    }

                    if ( norm( xr - x ) > tol )
                    {
                       QAssert.Fail( "least square solution does not match (norm = "
                                  + norm( x - xr ) + ")" );

                    }
                 }
              }
           }
        }

#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
      public void testInverse() 
      {

         // Testing LU inverse calculation
         setup();

         double tol = 1.0e-12;
         Matrix[] testMatrices = { M1, M2, I, M5 };

         for (int j = 0; j < testMatrices.Length; j++) 
         {
            Matrix A = testMatrices[j];
            Matrix invA = Matrix.inverse(A);

            Matrix I1 = invA*A;
            Matrix I2 = A*invA;

            Matrix eins = new Matrix(A.rows(), A.rows(), 0.0);
            for (int i=0; i < A.rows(); ++i) eins[i,i] = 1.0;

            if (norm(I1 - eins) > tol)
               QAssert.Fail("inverse(A)*A does not recover unit matrix (norm = "
                           + norm(I1-eins) + ")");

            if (norm(I2 - eins) > tol)
               QAssert.Fail("A*inverse(A) does not recover unit matrix (norm = "
                           + norm(I1-eins) + ")");
         }
      }
   }
}
