/*
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

namespace QLNet
{
   //! exponential correlation model
   /*! This class describes a exponential correlation model

       References:

       Damiano Brigo, Fabio Mercurio, Massimo Morini, 2003,
       Different Covariance Parameterizations of Libor Market Model and Joint
       Caps/Swaptions Calibration,
       (<http://www.business.uts.edu.au/qfrc/conferences/qmf2001/Brigo_D.pdf>)
   */

   public class LmExponentialCorrelationModel : LmCorrelationModel
   {
      public LmExponentialCorrelationModel(int size, double rho)
         : base(size, 1)
      {
         corrMatrix_ = new Matrix(size, size);
         pseudoSqrt_ = new Matrix(size, size);
         arguments_[0] = new ConstantParameter(rho, new PositiveConstraint());
         generateArguments();
      }

      public override Matrix correlation(double t, Vector x = null)
      {
         Matrix tmp = new Matrix(corrMatrix_);
         return tmp;
      }

      public override Matrix pseudoSqrt(double t, Vector x = null)
      {
         Matrix tmp = new Matrix(pseudoSqrt_);
         return tmp;
      }


      public override double correlation(int i, int j, double t, Vector x = null)
      {
         return corrMatrix_[i, j];
      }

      public override bool isTimeIndependent()
      {
         return true;
      }

      protected override void generateArguments()
      {
         double rho = arguments_[0].value(0.0);

         for (int i = 0; i < size_; ++i)
         {
            for (int j = i; j < size_; ++j)
            {
               corrMatrix_[i, j] = corrMatrix_[j, i] = Math.Exp(-rho * Math.Abs((double) i - (double) j));
            }
         }
         pseudoSqrt_ = MatrixUtilitites.pseudoSqrt(corrMatrix_, MatrixUtilitites.SalvagingAlgorithm.Spectral);
      }

      private Matrix corrMatrix_, pseudoSqrt_;
   }
}
