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
    public class LmLinearExponentialVolatilityModel : LmVolatilityModel
    {
        private List<double> fixingTimes_;

        public LmLinearExponentialVolatilityModel(
                                         List<double> fixingTimes,
                                         double a, double b, double c, double d)
           : base(fixingTimes.Count, 4)
       {
           fixingTimes_=fixingTimes;
           arguments_[0] = new ConstantParameter(a, new PositiveConstraint());
           arguments_[1] = new ConstantParameter(b, new PositiveConstraint());
           arguments_[2] = new ConstantParameter(c, new PositiveConstraint());
           arguments_[3] = new ConstantParameter(d, new PositiveConstraint());
       }

        public override Vector volatility(double t,  Vector x) 
        {
            double a = arguments_[0].value(0.0);
            double b = arguments_[1].value(0.0);
            double c = arguments_[2].value(0.0);
            double d = arguments_[3].value (0.0);

            Vector tmp=new Vector(size_, 0.0);

            for (int i=0; i<size_; ++i) {
                double T = fixingTimes_[i];
                if (T>t) {
                tmp[i] = (a*(T-t)+d)*Math.Exp(-b*(T-t)) + c;
                }
            }
            return tmp;
        }

        public override Vector volatility(double t){
           return volatility(t, null); 
        }

        public override double volatility(int i, double t,  Vector x) 
        {
            double a = arguments_[0].value(0.0);
            double b = arguments_[1].value(0.0);
            double c = arguments_[2].value(0.0);
            double d = arguments_[3].value (0.0);

            double T = fixingTimes_[i];

            return (T > t) ? (a * (T - t) + d) * Math.Exp(-b * (T - t)) + c : 0.0;
        }

        public override double volatility(int i, double t){
            return volatility(i, t, null);
        }

        public override double integratedVariance(int i, int j, double u, Vector x)
        {
            double a = arguments_[0].value(0.0);
            double b = arguments_[1].value(0.0);
            double c = arguments_[2].value(0.0);
            double d = arguments_[3].value(0.0);

            double T = fixingTimes_[i];
            double S = fixingTimes_[j];

            double k1 = Math.Exp(b*u);
            double k2 = Math.Exp(b*S);
            double k3 = Math.Exp(b * T);

            return (a*a*(-1 - 2*b*b*S*T - b*(S + T)
                     + k1*k1*(1 + b*(S + T - 2*u) + 2*b*b*(S - u)*(T - u)))
                    + 2*b*b*(2*c*d*(k2 + k3)*(k1 - 1)
                         +d*d*(k1*k1 - 1)+2*b*c*c*k2*k3*u)
                    + 2*a*b*(d*(-1 - b*(S + T) + k1*k1*(1 + b*(S + T - 2*u)))
                         -2*c*(k3*(1 + b*S) + k2*(1 + b*T)
                               - k1*k3*(1 + b*(S - u))
                               - k1*k2*(1 + b*(T - u)))
                            )
                    ) / (4*b*b*b*k2*k3);
        }

        public override double integratedVariance(int i, int j, double u){
            return integratedVariance(i, j, u,null);
        }

        public override void generateArguments(){}
    }
}
