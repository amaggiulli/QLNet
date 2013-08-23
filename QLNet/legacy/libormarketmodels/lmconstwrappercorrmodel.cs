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
    public class LmConstWrapperCorrelationModel : LmCorrelationModel {
      
        public LmConstWrapperCorrelationModel(LmCorrelationModel corrModel)
        : base(corrModel.size(), 0){
            corrModel_=corrModel;
        }

        public new int factors(){
            return corrModel_.factors();
        }

        public override Matrix correlation(double t, Vector x) {
            return corrModel_.correlation(t, x);
        }

        public override Matrix correlation(double t){
            return correlation(t, null);
        }

        public override Matrix pseudoSqrt(double t, Vector x) {
            return corrModel_.pseudoSqrt(t, x);
        }

        public override double correlation(int i, int j, double t, Vector x){
            return corrModel_.correlation(i, j, t, x);
        }

        public override double correlation(int i, int j, double t){
            return correlation(i, j, t, null);
        }

        public new bool isTimeIndependent() {
            return corrModel_.isTimeIndependent();
        }

        protected override void generateArguments() { }

        protected LmCorrelationModel corrModel_;
    }
}
