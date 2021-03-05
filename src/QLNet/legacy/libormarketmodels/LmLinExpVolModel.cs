﻿/*
 Copyright (C) 2009 Philippe Real (ph_real@hotmail.com)
 Copyright (C) 2008-2017 Andrea Maggiulli (a.maggiulli@gmail.com)

 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

 QLNet is free software: you can redistribute it and/or modify it
 under the terms of the QLNet license.  You should have received a
 copy of the license along with this program; if not, license is
 available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.

 QLNet is a based on QuantLib, a free-software/open-source library
 for financial quantitative analysts and developers - http://quantlib.org/
 The QuantLib license is available online at http://quantlib.org/license.shtml.

 This program is distributed in the hope that it will be useful, but WITHOUT
 ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 FOR A PARTICULAR PURPOSE.  See the license for more details.
*/
using System;
using System.Collections.Generic;

namespace QLNet
{
   //! %linear exponential volatility model
   /*! This class describes a linear-exponential volatility model

       \f[
       \sigma_i(t)=(a*(T_{i}-t)+d)*e^{-b(T_{i}-t)}+c
       \f]

       References:

       Damiano Brigo, Fabio Mercurio, Massimo Morini, 2003,
       Different Covariance Parameterizations of Libor Market Model and Joint
       Caps/Swaptions Calibration,
       (<http://www.business.uts.edu.au/qfrc/conferences/qmf2001/Brigo_D.pdf>)
   */
   public class LmLinearExponentialVolatilityModel : LmVolatilityModel
   {
      public LmLinearExponentialVolatilityModel(List<double> fixingTimes, double a, double b, double c, double d)
         : base(fixingTimes.Count, 4)
      {
         fixingTimes_ = fixingTimes;
         arguments_[0] = new ConstantParameter(a, new PositiveConstraint());
         arguments_[1] = new ConstantParameter(b, new PositiveConstraint());
         arguments_[2] = new ConstantParameter(c, new PositiveConstraint());
         arguments_[3] = new ConstantParameter(d, new PositiveConstraint());
      }

      public override Vector volatility(double t, Vector x = null)
      {
         double a = arguments_[0].value(0.0);
         double b = arguments_[1].value(0.0);
         double c = arguments_[2].value(0.0);
         double d = arguments_[3].value(0.0);

         Vector tmp = new Vector(size_, 0.0);

         for (int i = 0; i < size_; ++i)
         {
            double T = fixingTimes_[i];
            if (T > t)
            {
               tmp[i] = (a * (T - t) + d) * Math.Exp(-b * (T - t)) + c;
            }
         }
         return tmp;
      }

      public override double volatility(int i, double t, Vector x = null)
      {
         double a = arguments_[0].value(0.0);
         double b = arguments_[1].value(0.0);
         double c = arguments_[2].value(0.0);
         double d = arguments_[3].value(0.0);

         double T = fixingTimes_[i];

         return (T > t) ? (a * (T - t) + d) * Math.Exp(-b * (T - t)) + c : 0.0;
      }


      public override double integratedVariance(int i, int j, double u, Vector x = null)
      {
         double a = arguments_[0].value(0.0);
         double b = arguments_[1].value(0.0);
         double c = arguments_[2].value(0.0);
         double d = arguments_[3].value(0.0);

         double T = fixingTimes_[i];
         double S = fixingTimes_[j];

         double k1 = Math.Exp(b * u);
         double k2 = Math.Exp(b * S);
         double k3 = Math.Exp(b * T);

         return (a * a * (-1 - 2 * b * b * S * T - b * (S + T)
                          + k1 * k1 * (1 + b * (S + T - 2 * u) + 2 * b * b * (S - u) * (T - u)))
                 + 2 * b * b * (2 * c * d * (k2 + k3) * (k1 - 1)
                                + d * d * (k1 * k1 - 1) + 2 * b * c * c * k2 * k3 * u)
                 + 2 * a * b * (d * (-1 - b * (S + T) + k1 * k1 * (1 + b * (S + T - 2 * u)))
                                - 2 * c * (k3 * (1 + b * S) + k2 * (1 + b * T)
                                           - k1 * k3 * (1 + b * (S - u))
                                           - k1 * k2 * (1 + b * (T - u)))
                               )
                ) / (4 * b * b * b * k2 * k3);
      }

      public override void generateArguments()
      { }

      private List<double> fixingTimes_;
   }
}
