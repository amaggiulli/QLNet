/*
 Copyright (C) 2017 Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)

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
   public abstract class FdmLinearOp : IOperator
   {
      public abstract Vector apply(Vector r);
      public abstract SparseMatrix toMatrix();

      //IOperator interface
      public abstract int size();
      public abstract IOperator identity(int size);
      public abstract Vector applyTo(Vector v);
      public abstract Vector solveFor(Vector rhs);

      public abstract IOperator multiply(double a, IOperator D);
      public abstract IOperator add
         (IOperator A, IOperator B);
      public abstract IOperator subtract(IOperator A, IOperator B);

      public abstract bool isTimeDependent();
      public abstract void setTime(double t);

      public abstract object Clone();
   }
}
