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

namespace QLNet
{
   public class LmConstWrapperCorrelationModel : LmCorrelationModel
   {
      public LmConstWrapperCorrelationModel(LmCorrelationModel corrModel)
         : base(corrModel.size(), 0)
      {
         corrModel_ = corrModel;
      }

      public override int factors()
      {
         return corrModel_.factors();
      }

      public override Matrix correlation(double t, Vector x = null)
      {
         return corrModel_.correlation(t, x);
      }

      public override Matrix pseudoSqrt(double t, Vector x = null)
      {
         return corrModel_.pseudoSqrt(t, x);
      }

      public override double correlation(int i, int j, double t, Vector x = null)
      {
         return corrModel_.correlation(i, j, t, x);
      }

      public new bool isTimeIndependent()
      {
         return corrModel_.isTimeIndependent();
      }

      protected override void generateArguments()
      {}

      protected LmCorrelationModel corrModel_;
   }
}
