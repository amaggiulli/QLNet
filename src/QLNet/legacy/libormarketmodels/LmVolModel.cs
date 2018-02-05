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
using System.Collections.Generic;

namespace QLNet
{
   //! caplet volatility model
   public abstract class LmVolatilityModel
   {
      protected LmVolatilityModel(int size, int nArguments)
      {
         size_ = size;
         arguments_ = new InitializedList<Parameter>(nArguments);
      }

      public int size()
      {
         return size_;
      }

      public abstract void generateArguments();

      public abstract Vector volatility(double t, Vector x = null);

      public virtual double volatility(int i, double t, Vector x = null)
      {
         // inefficient implementation, please overload in derived classes
         return volatility(t, x)[i];
      }

      public virtual double integratedVariance(int i, int j, double u, Vector x = null)
      {
         Utils.QL_FAIL("integratedVariance() method is not supported");
         return 0;
      }

      public List<Parameter> parameters()
      {
         return arguments_;
      }

      public void setParams(List<Parameter> arguments)
      {
         arguments_ = arguments;
         generateArguments();
      }

      protected int size_;
      protected List<Parameter> arguments_;
   }
}
