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
   //! caplet const volatility model
   public class LmConstWrapperVolatilityModel : LmVolatilityModel
   {
      public LmConstWrapperVolatilityModel(LmVolatilityModel volaModel)
         : base(volaModel.size(), 0)
      {
         volaModel_ = volaModel;
      }

      public override Vector volatility(double t, Vector x = null)
      {
         return volaModel_.volatility(t, x);
      }

      public override double volatility(int i, double t, Vector x = null)
      {
         return volaModel_.volatility(i, t, x);
      }

      public override double integratedVariance(int i, int j, double u, Vector x = null)
      {
         return volaModel_.integratedVariance(i, j, u, x);
      }

      protected LmVolatilityModel volaModel_;

      public override void generateArguments()
      {}
   }
}
