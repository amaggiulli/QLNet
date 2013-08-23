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
using System.Linq;
using System.Collections.Generic;

namespace QLNet {
    //! %Matrix used in linear algebra.
    /*! This class implements the concept of Matrix as used in linear
        algebra. As such, it is <b>not</b> meant to be used as a
        container.
    */
    public struct Matrix {
        #region properties
        private int rows_, columns_;
        public int rows() { return rows_; }
        public int columns() { return columns_; }
        public bool empty() { return rows_ == 0 || columns_ == 0; }

        private double[] data_;
        public double this[int i, int j] { get { return data_[i * columns_ + j]; } set { data_[i * columns_ + j] = value; } }
        public double this[int i] { get { return data_[i]; } set { data_[i] = value; } }
        public Vector row(int r) {
            Vector result = new Vector(columns_);
            for (int i = 0; i < columns_; i++)
                result[i] = this[r, i];
            return result; 
        }
        public Vector column(int c) {
            Vector result = new Vector(rows_);
            for (int i = 0; i < rows_; i++)
                result[i] = this[i, c];
            return result;
        }
        public Vector diagonal() {
            int arraySize = Math.Min(rows(), columns());
            Vector tmp = new Vector(arraySize);
            for(int i = 0; i < arraySize; i++)
                tmp[i] = this[i, i];
            return tmp;
        }
        public Vector GetRange(int start, int length) {
            return new Vector(data_.Skip(start).Take(length).ToList());
        }
        #endregion

        #region Constructors
        //! creates a null matrix
        // public Matrix() : base(0) { rows_ = 0; columns_ = 0; }

        //! creates a matrix with the given dimensions
        public Matrix(int rows, int columns) {
            data_ = new double[rows * columns];
            rows_ = rows;
            columns_ = columns;
        }

        //! creates the matrix and fills it with <tt>value</tt>
        public Matrix(int rows, int columns, double value) {
            data_ = new double[rows * columns];
            for (int i = 0; i < data_.Length; i++)
                data_[i] = value;
            rows_ = rows;
            columns_ = columns;
        }

        public Matrix(Matrix from) {
            data_ = !from.empty() ? (double[])from.data_.Clone() : null;
            rows_ = from.rows_;
            columns_ = from.columns_;
        }
	    #endregion
    
        #region Algebraic operators
        /*! \pre all matrices involved in an algebraic expression must have the same size. */
        public static Matrix operator +(Matrix m1, Matrix m2) { return operMatrix(ref m1, ref m2, (x, y) => x + y); }
        public static Matrix operator -(Matrix m1, Matrix m2) { return operMatrix(ref m1, ref m2, (x, y) => x - y); }
        public static Matrix operator *(double value, Matrix m1) { return operValue(ref m1, value, (x, y) => x * y); }
        public static Matrix operator /(double value, Matrix m1) { return operValue(ref m1, value, (x, y) => x / y); }
        public static Matrix operator *(Matrix m1, double value) { return operValue(ref m1, value, (x, y) => x * y); }
        public static Matrix operator /(Matrix m1, double value) { return operValue(ref m1, value, (x, y) => x / y); }
        private static Matrix operMatrix(ref Matrix m1, ref Matrix m2, Func<double, double, double> func) {
            if (!(m1.rows_ == m2.rows_ && m1.columns_ == m2.columns_))
                throw new ApplicationException("operation on matrices with different sizes (" +
                       m2.rows_ + "x" + m2.columns_ + ", " + m1.rows_ + "x" + m1.columns_ + ")");

            Matrix result = new Matrix(m1.rows_, m1.columns_);
            for (int i = 0; i < m1.rows_; i++)
                for (int j = 0; j < m1.columns_; j++)
                    result[i, j] = func(m1[i, j], m2[i, j]);
            return result;
        }
        private static Matrix operValue(ref Matrix m1, double value, Func<double, double, double> func) {
            Matrix result = new Matrix(m1.rows_, m1.columns_);
            for (int i = 0; i < m1.rows_; i++)
                for (int j = 0; j < m1.columns_; j++)
                    result[i, j] = func(m1[i, j], value);
            return result;
        }

        public static Vector operator *(Vector v, Matrix m) {
            if (!(v.Count == m.rows()))
                throw new ApplicationException("vectors and matrices with different sizes ("
                       + v.Count + ", " + m.rows() + "x" + m.columns() + ") cannot be multiplied");
            Vector result = new Vector(m.columns());
            for (int i = 0; i < result.Count; i++)
                result[i] = v * m.column(i);
            return result;
        }
        /*! \relates Matrix */
        public static Vector operator *(Matrix m, Vector v) {
            if (!(v.Count == m.columns()))
                throw new ApplicationException("vectors and matrices with different sizes ("
                       + v.Count + ", " + m.rows() + "x" + m.columns() + ") cannot be multiplied");
            Vector result = new Vector(m.rows());
            for (int i = 0; i < result.Count; i++)
                result[i] = m.row(i) * v;
            return result;
        }
        /*! \relates Matrix */
        public static Matrix operator *(Matrix m1, Matrix m2) {
            if (!(m1.columns() == m2.rows()))
                throw new ApplicationException("matrices with different sizes (" +
                       m1.rows() + "x" + m1.columns() + ", " +
                       m2.rows() + "x" + m2.columns() + ") cannot be multiplied");
            Matrix result = new Matrix(m1.rows(), m2.columns());
            for (int i = 0; i < result.rows(); i++)
                for (int j = 0; j < result.columns(); j++)
                    result[i, j] = m1.row(i) * m2.column(j);
            return result;
        } 
        #endregion

        public static Matrix transpose(Matrix m) {
            Matrix result = new Matrix(m.columns(),m.rows());
            for (int i=0; i<m.rows(); i++)
                for (int j=0; j<m.columns();j++)
                    result[j,i] = m[i,j];
            return result;
        }

        public static Matrix outerProduct(List<double> v1begin, List<double> v2begin) {

            int size1 = v1begin.Count;
            if (!(size1>0)) throw new ApplicationException("null first vector");

            int size2 = v2begin.Count;
            if(!(size2>0)) throw new ApplicationException("null second vector");

            Matrix result = new Matrix(size1, size2);

            for (int i=0; i<v1begin.Count; i++)
                for(int j=0; j<v2begin.Count; j++)
                    result[i,j] = v1begin[i] * v2begin[j];
            return result;
        }

        public void fill(double value) {
            for (int i = 0; i < data_.Length; i++)
                data_[i] = value;
        }

        public void swap(int i1, int j1, int i2, int j2) {
            double t = this[i2, j2];
            this[i2, j2] = this[i1, j1];
            this[i1, j1] = t;
        }
    }
}
