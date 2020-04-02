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

namespace QLNet
{
   public class FdmBlackScholesOp : FdmLinearOpComposite
   {
      public FdmBlackScholesOp(FdmMesher mesher,
                               GeneralizedBlackScholesProcess bsProcess,
                               double strike,
                               bool localVol = false,
                               double? illegalLocalVolOverwrite = null,
                               int direction = 0,
                               FdmQuantoHelper quantoHelper = null)
      {
         mesher_ = mesher;
         rTS_   = bsProcess.riskFreeRate().currentLink();
         qTS_   = bsProcess.dividendYield().currentLink();
         volTS_ = bsProcess.blackVolatility().currentLink();
         localVol_ = (localVol) ? bsProcess.localVolatility().currentLink()
                     : null;
         x_      = (localVol) ? new Vector(Vector.Exp(mesher.locations(direction))) : null;
         dxMap_ = new FirstDerivativeOp(direction, mesher);
         dxxMap_ = new SecondDerivativeOp(direction, mesher);
         mapT_  = new TripleBandLinearOp(direction, mesher);
         strike_ = strike;
         illegalLocalVolOverwrite_ = illegalLocalVolOverwrite;
         direction_ = direction;
         quantoHelper_ = quantoHelper;
      }

      public override int size() { return 1; }

      //! Time \f$t1 <= t2\f$ is required
      public override void setTime(double t1, double t2)
      {
         double r = rTS_.forwardRate(t1, t2, Compounding.Continuous).rate();
         double q = qTS_.forwardRate(t1, t2, Compounding.Continuous).rate();

         if (localVol_ != null)
         {
            FdmLinearOpLayout layout = mesher_.layout();
            FdmLinearOpIterator endIter = layout.end();

            Vector v = new Vector(layout.size());
            for (FdmLinearOpIterator iter = layout.begin();
                 iter != endIter; ++iter)
            {
               int i = iter.index();

               if (illegalLocalVolOverwrite_ == null)
               {
                  double t = localVol_.localVol(0.5 * (t1 + t2), x_[i], true);
                  v[i] = t * t;
               }
               else
               {
                  try
                  {
                     double t = localVol_.localVol(0.5 * (t1 + t2), x_[i], true);
                     v[i] = t * t;
                  }
                  catch
                  {
                     v[i] = illegalLocalVolOverwrite_.Value * illegalLocalVolOverwrite_.Value;
                  }

               }
            }
            if (quantoHelper_ != null)
            {
               mapT_.axpyb(r - q - 0.5 * v
                           - quantoHelper_.quantoAdjustment(Vector.Sqrt(v), t1, t2),
                           dxMap_,
                           dxxMap_.mult(0.5 * v), new Vector(1, -r));
            }
            else
            {
               mapT_.axpyb(r - q - 0.5 * v, dxMap_,
                           dxxMap_.mult(0.5 * v), new Vector(1, -r));
            }
         }
         else
         {
            double vv = volTS_.blackForwardVariance(t1, t2, strike_) / (t2 - t1);

            if (quantoHelper_ != null)
            {
               mapT_.axpyb(new Vector(1, r - q - 0.5 * vv)
                           - quantoHelper_.quantoAdjustment(new Vector(1, Math.Sqrt(vv)), t1, t2),
                           dxMap_,
                           dxxMap_.mult(0.5 * new Vector(mesher_.layout().size(), vv)),
                           new Vector(1, -r));
            }
            else
            {
               mapT_.axpyb(new Vector(1, r - q - 0.5 * vv), dxMap_,
                           dxxMap_.mult(0.5 * new Vector(mesher_.layout().size(), vv)),
                           new Vector(1, -r));
            }
         }
      }

      public override Vector apply(Vector r)
      {
         return mapT_.apply(r);
      }

      public override Vector apply_mixed(Vector r)
      {
         Vector retVal = new Vector(r.size(), 0.0);
         return retVal;
      }

      public override Vector apply_direction(int direction, Vector r)
      {
         if (direction == direction_)
            return mapT_.apply(r);
         else
         {
            Vector retVal = new Vector(r.size(), 0.0);
            return retVal;
         }
      }
      public override Vector solve_splitting(int direction, Vector r, double dt)
      {
         if (direction == direction_)
            return mapT_.solve_splitting(r, dt, 1.0);
         else
         {
            Vector retVal = new Vector(r);
            return retVal;
         }
      }
      public override Vector preconditioner(Vector r, double dt) { return solve_splitting(direction_, r, dt); }

      public override List<SparseMatrix> toMatrixDecomp()
      {
         List<SparseMatrix> retVal = new InitializedList<SparseMatrix>(1, mapT_.toMatrix());
         return retVal;
      }

      #region IOperator interface
      public override IOperator identity(int size) { return null; }
      public override Vector applyTo(Vector v) { return null; }
      public override Vector solveFor(Vector rhs) { return null; }

      public override IOperator multiply(double a, IOperator D) { return null; }
      public override IOperator add
         (IOperator A, IOperator B) { return null; }
      public override IOperator subtract(IOperator A, IOperator B) { return null; }

      public override bool isTimeDependent() { return false; }
      public override void setTime(double t) { }
      public override object Clone() { return this.MemberwiseClone(); }
      #endregion

      protected FdmMesher mesher_;
      protected YieldTermStructure rTS_, qTS_;
      protected BlackVolTermStructure volTS_;
      protected LocalVolTermStructure localVol_;
      protected Vector x_;
      protected FirstDerivativeOp dxMap_;
      protected TripleBandLinearOp dxxMap_;
      protected TripleBandLinearOp mapT_;
      protected double strike_;
      protected double? illegalLocalVolOverwrite_;
      protected int direction_;
      protected FdmQuantoHelper quantoHelper_;
   }
}
