/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008 Andrea Maggiulli
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
 * 
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
    //! 1-D array used in linear algebra.
    /*! This class implements the concept of vector as used in linear algebra.
        As such, it is <b>not</b> meant to be used as a container -
        <tt>std::vector</tt> should be used instead.

        \test construction of arrays is checked in a number of cases
    */
    public class Vector : InitializedList<double>, ICloneable {
        //! \name Constructors, and assignment
        //! creates the array with the given dimension
        public Vector() : this(0) { }
        public Vector(int size) : base(size) { }

        //! creates the array and fills it with <tt>value</tt>
        public Vector(int size, double value) : base(size, value) { }

        /*! \brief creates the array and fills it according to \f$ a_{0} = value, a_{i}=a_{i-1}+increment \f$ */
        public Vector(int size, double value, double increment) : this(size) {
            for (int i = 0; i < this.Count; i++, value += increment)
                this[i] = value;
        }

        public Vector(Vector from) : base(from.Count) {
            for (int i = 0; i < this.Count; i++)
                this[i] = from[i];
        }

        //! creates the array as a copy of a given stl vector
        public Vector(List<double> from) : this(from.Count) {
            for (int i = 0; i < this.Count; i++)
                this[i] = from[i];
        }

        //public Vector(const Disposable<Vector>&);

        // these can not be overloaded
        //Array& operator=(const Array&);
        //Array& operator=(const Disposable<Vector>&);
        public Object Clone() { return this.MemberwiseClone(); }

        public static bool operator ==(Vector to, Vector from) {

			  if ( System.Object.ReferenceEquals( to, from ) ) return true;
			  else if ( (object)to == null || (object)from == null ) return false;
			  else
			  {
				  if ( from.Count != to.Count ) return false;

				  for ( int i = 0; i < from.Count; i++ )
					  if ( from[i] != to[i] ) return false;

				  return true;
			  }
        }
        public static bool operator !=(Vector to, Vector from) { return (!(to == from)); }
        public override bool Equals(object o) { return (this == (Vector)o); }
        public override int GetHashCode() { return 0; }

        public int size() { return this.Count; }
        public bool empty() { return this.Count == 0; }

        #region Vector algebra
        //    <tt>v += x</tt> and similar operation involving a scalar value
        //    are shortcuts for \f$ \forall i : v_i = v_i + x \f$

        //    <tt>v *= w</tt> and similar operation involving two vectors are
        //    shortcuts for \f$ \forall i : v_i = v_i \times w_i \f$

        //    \pre all arrays involved in an algebraic expression must have the same size.
        ////@{
        public static Vector operator +(Vector v1, Vector v2) { return operVector(v1, v2, (x, y) => x + y); }
        public static Vector operator -(Vector v1, Vector v2) { return operVector(v1, v2, (x, y) => x - y); }

        public static Vector operator +(Vector v1, double value) { return operValue(v1, value, (x, y) => x + y); }
        public static Vector operator -(Vector v1, double value) { return operValue(v1, value, (x, y) => x - y); }
        public static Vector operator *(double value, Vector v1) { return operValue(v1, value, (x, y) => x * y); }
        public static Vector operator *(Vector v1, double value) { return operValue(v1, value, (x, y) => x * y); }
        public static Vector operator /(Vector v1, double value) { return operValue(v1, value, (x, y) => x / y); }

        internal static Vector operVector(Vector v1, Vector v2, Func<double, double, double> func) {
            if (v1.Count != v2.Count)
                throw new ApplicationException("operation on vectors with different sizes (" + v1.Count + ", " + v2.Count);

            Vector temp = new Vector(v1.Count);
            for (int i = 0; i < v1.Count; i++)
                temp[i] = func(v1[i], v2[i]);
            return temp;
        }
        private static Vector operValue(Vector v1, double value, Func<double, double, double> func) {
            Vector temp = new Vector(v1.Count);
            for (int i = 0; i < v1.Count; i++)
                temp[i] = func(v1[i], value);
            return temp;
        }

        public static double operator *(Vector v1, Vector v2) {
            if (v1.Count != v2.Count)
                throw new ApplicationException("operation on vectors with different sizes (" + v1.Count + ", " + v2.Count);

            double result = 0;
            for (int i = 0; i < v1.Count; i++)
                result += v1[i] * v2[i];
            return result;
        }
        //public static double operator /(Vector v1, Vector v2) { return operVector(ref v1, ref v2, (x, y) => x / y); }
        #endregion


        #region Vector utils
        // dot product. It is already overloaded in the vector. Thus for compatibility only
        public static double DotProduct(Vector v1, Vector v2) {
            return v1 * v2;
        }

        public static Vector DirectMultiply(Vector v1, Vector v2) {
            return Vector.operVector(v1, v2, (x, y) => x * y);
        }

        public static Vector Sqrt(Vector v) {
            Vector result = new Vector(v.size());
            result.ForEach((i, x) => result[i] = Math.Sqrt(v[i]));
            return result;
        }

        public void swap(int i1, int i2) {
            double t = this[i2];
            this[i2] = this[i1];
            this[i1] = t;
        }
        #endregion
    }
}
