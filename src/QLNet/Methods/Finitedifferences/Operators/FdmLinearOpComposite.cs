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
using System.Collections.Generic;
using System.Linq;

namespace QLNet
{
   public abstract class FdmLinearOpComposite : FdmLinearOp
   {
      //! Time \f$t1 <= t2\f$ is required
      public abstract void setTime(double t1, double t2);

      public abstract Vector apply_mixed(Vector r);

      public abstract Vector apply_direction(int direction, Vector r);
      public abstract Vector solve_splitting(int direction, Vector r, double s);
      public abstract Vector preconditioner(Vector r, double s);

      public virtual List<SparseMatrix> toMatrixDecomp()
      {
         return null;
      }

      public override SparseMatrix toMatrix()
      {
         List<SparseMatrix> dcmp = toMatrixDecomp();
         SparseMatrix retVal = dcmp.accumulate(1, dcmp.Count, dcmp.First(), (a, b) => a + b);
         return retVal;
      }
   }
}
