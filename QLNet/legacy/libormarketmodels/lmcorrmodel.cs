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
    public abstract class LmCorrelationModel
    {
        protected int size_;
        protected List<Parameter> arguments_;

        public LmCorrelationModel(int size, int nArguments) {
            size_ = size;
            arguments_ = new InitializedList<Parameter>( nArguments);
        }

        public virtual int size() {return size_;}

        public virtual int factors() {return size_;}

        public List<Parameter> parameters() { return arguments_; }

        public void setParams(List<Parameter> arguments){
            arguments_ = arguments;
            generateArguments();
        }

        public abstract Matrix correlation(double t, Vector x);

        public abstract Matrix correlation(double t);

        public virtual double correlation(int i, int j, double t, Vector x){
            // inefficient implementation, please overload in derived classes
            return correlation(t, x)[i, j];
        }

        public virtual double correlation(int i, int j, double t){
            return correlation(i, j, t, null);
        }

        public virtual Matrix pseudoSqrt(double t, Vector x) {
            return MatrixUtilitites.pseudoSqrt(this.correlation(t, x),
                                                MatrixUtilitites.SalvagingAlgorithm.Spectral);
        }

        public virtual Matrix pseudoSqrt(double t){
            return pseudoSqrt(t, null);
        }

        protected abstract void generateArguments();

        public virtual bool isTimeIndependent() { return false; }
       
    }
}
