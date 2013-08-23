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
    //! extended linear exponential volatility model
    /*! This class describes an extended linear-exponential volatility model

        \f[
        \sigma_i(t)=k_i*((a*(T_{i}-t)+d)*e^{-b(T_{i}-t)}+c)
        \f]

        References:

        Damiano Brigo, Fabio Mercurio, Massimo Morini, 2003,
        Different Covariance Parameterizations of Libor Market Model and Joint
        Caps/Swaptions Calibration,
        (<http://www.business.uts.edu.au/qfrc/conferences/qmf2001/Brigo_D.pdf>)
    */

    public class LmExtLinearExponentialVolModel
        :  LmLinearExponentialVolatilityModel {
      
        public  LmExtLinearExponentialVolModel(List<double> fixingTimes,
                                       double a, double b, double c, double d)
        : base(fixingTimes, a, b, c, d) 
        {
            arguments_.Capacity+= size_;
            for (int i=0; i <size_; ++i) {
                arguments_.Add(new ConstantParameter(1.0, new PositiveConstraint()));
            }
        }
        
        public override Vector volatility(double t){
            return volatility(t, null); 
        }

        public override Vector volatility(double t, Vector x) {
            Vector tmp = base.volatility(t, x);
            for (int i=0; i<size_; ++i) {
                tmp[i]*=arguments_[i+4].value (0.0);
            }
            return tmp;
        }

        public override double volatility(int i, double t){
            return volatility(i,t,null);
        }

        public override double volatility(int i, double t, Vector x) {
            return arguments_[i+4].value (0.0)
                    * base.volatility(i, t, x);
        }

        public override double integratedVariance(int i, int j, double u){
            return integratedVariance(i, j,u);
        }

        public override double integratedVariance(int i, int j, double u,Vector x) {
            return arguments_[i+4].value(0.0)*arguments_[j+4].value(0.0)
                   * base.integratedVariance(i, j, u, x);
        }
    }
}
