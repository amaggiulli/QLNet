﻿/*
 Copyright (C) 2009 Philippe Real (ph_real@hotmail.com)
 Copyright (C) 2008-2017 Andrea Maggiulli (a.maggiulli@gmail.com)
  
 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

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
using System.Collections.Generic;

namespace QLNet
{
   // libor forward correlation model
   public abstract class LmCorrelationModel
   {
      protected LmCorrelationModel(int size, int nArguments)
      {
         size_ = size;
         arguments_ = new InitializedList<Parameter>(nArguments);
      }

      public virtual int size()
      {
         return size_;
      }

      public virtual int factors()
      {
         return size_;
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

      public abstract Matrix correlation(double t, Vector x = null);

      public virtual double correlation(int i, int j, double t, Vector x = null )
      {
         // inefficient implementation, please overload in derived classes
         return correlation(t, x)[i, j];
      }

      public virtual Matrix pseudoSqrt(double t, Vector x = null)
      {
         return MatrixUtilitites.pseudoSqrt(this.correlation(t, x),
            MatrixUtilitites.SalvagingAlgorithm.Spectral);
      }

      public virtual bool isTimeIndependent()
      {
         return false;
      }

      protected abstract void generateArguments();

      protected int size_;
      protected List<Parameter> arguments_;


   }
}
