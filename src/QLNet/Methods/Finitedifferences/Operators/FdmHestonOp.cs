/*
 Copyright (C) 2017 Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)

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
using System;
using System.Collections.Generic;

namespace QLNet
{
   public class FdmHestonEquityPart
   {
      public FdmHestonEquityPart(FdmMesher mesher,
                                 YieldTermStructure rTS,
                                 YieldTermStructure qTS,
                                 FdmQuantoHelper quantoHelper = null,
                                 LocalVolTermStructure leverageFct = null)
      {
         varianceValues_ = new Vector(0.5 * mesher.locations(1));
         dxMap_ = new FirstDerivativeOp(0, mesher);
         dxxMap_ = new SecondDerivativeOp(0, mesher).mult(0.5 * mesher.locations(1));
         mapT_ = new TripleBandLinearOp(0, mesher);
         mesher_ = mesher;
         rTS_ = rTS;
         qTS_ = qTS;
         quantoHelper_ = quantoHelper;
         leverageFct_ = leverageFct;

         // on the boundary s_min and s_max the second derivative
         // d^2V/dS^2 is zero and due to Ito's Lemma the variance term
         // in the drift should vanish.
         FdmLinearOpLayout layout = mesher_.layout();
         FdmLinearOpIterator endIter = layout.end();
         for (FdmLinearOpIterator iter = layout.begin(); iter != endIter;
              ++iter)
         {
            if (iter.coordinates()[0] == 0
                || iter.coordinates()[0] == layout.dim()[0] - 1)
            {
               varianceValues_[iter.index()] = 0.0;
            }
         }
         volatilityValues_ = Vector.Sqrt(2 * varianceValues_);
      }

      public void setTime(double t1, double t2)
      {
         double r = rTS_.forwardRate(t1, t2, Compounding.Continuous).rate();
         double q = qTS_.forwardRate(t1, t2, Compounding.Continuous).rate();

         L_ = getLeverageFctSlice(t1, t2);
         Vector Lsquare = Vector.DirectMultiply(L_, L_);

         if (quantoHelper_ != null)
         {
            Vector tmp = quantoHelper_.quantoAdjustment(Vector.DirectMultiply(volatilityValues_, L_), t1, t2);

            mapT_.axpyb(r - q - Vector.DirectMultiply(varianceValues_, Lsquare)
                        - tmp, dxMap_, dxxMap_.mult(Lsquare), new Vector(1, -0.5 * r));
         }
         else
         {
            mapT_.axpyb(r - q - Vector.DirectMultiply(varianceValues_, Lsquare), dxMap_,
                        dxxMap_.mult(Lsquare), new Vector(1, -0.5 * r));
         }
      }
      public TripleBandLinearOp getMap()
      {
         return mapT_;
      }
      public Vector getL() { return L_; }

      protected Vector getLeverageFctSlice(double t1, double t2)
      {
         FdmLinearOpLayout layout = mesher_.layout();
         Vector v = new Vector(layout.size(), 1.0);

         if (leverageFct_ == null)
         {
            return v;
         }

         double t = 0.5 * (t1 + t2);
         double time = Math.Min(leverageFct_ == null ? 1000.0 : leverageFct_.maxTime(), t);

         FdmLinearOpIterator endIter = layout.end();
         for (FdmLinearOpIterator iter = layout.begin();
              iter != endIter; ++iter)
         {
            int nx = iter.coordinates()[0];

            if (iter.coordinates()[1] == 0)
            {
               double x = Math.Exp(mesher_.location(iter, 0));
               double spot = Math.Min(leverageFct_ == null ? 100000.0 : leverageFct_.maxStrike(),
                                      Math.Max(leverageFct_ == null ? -100000.0 : leverageFct_.minStrike(), x));
               v[nx] = Math.Max(0.01, leverageFct_ == null ? 0.0 : leverageFct_.localVol(time, spot, true));
            }
            else
            {
               v[iter.index()] = v[nx];
            }
         }
         return v;
      }

      protected Vector varianceValues_, volatilityValues_, L_;
      protected FirstDerivativeOp dxMap_;
      protected TripleBandLinearOp dxxMap_;
      protected TripleBandLinearOp mapT_;

      protected FdmMesher mesher_;
      protected YieldTermStructure rTS_, qTS_;
      protected FdmQuantoHelper quantoHelper_;
      protected LocalVolTermStructure leverageFct_;
   }

   public class FdmHestonVariancePart
   {
      public FdmHestonVariancePart(
         FdmMesher mesher,
         YieldTermStructure rTS,
         double sigma, double kappa, double theta)
      {
         dyMap_ = new SecondDerivativeOp(1, mesher)
         .mult(0.5 * sigma * sigma * mesher.locations(1))
         .add(new FirstDerivativeOp(1, mesher)
              .mult(kappa * (theta - mesher.locations(1))));
         mapT_ = new TripleBandLinearOp(1, mesher);
         rTS_ = rTS;
      }

      public void setTime(double t1, double t2)
      {
         double r = rTS_.forwardRate(t1, t2, Compounding.Continuous).rate();
         mapT_.axpyb(new Vector(), dyMap_, dyMap_, new Vector(1, -0.5 * r));
      }
      public TripleBandLinearOp getMap()
      {
         return mapT_;
      }

      protected TripleBandLinearOp dyMap_;
      protected TripleBandLinearOp mapT_;
      protected YieldTermStructure rTS_;
   }

   public class FdmHestonOp : FdmLinearOpComposite
   {
      public FdmHestonOp(FdmMesher mesher,
                         HestonProcess hestonProcess,
                         FdmQuantoHelper quantoHelper = null,
                         LocalVolTermStructure leverageFct = null)
      {
         correlationMap_ = new SecondOrderMixedDerivativeOp(0, 1, mesher)
         .mult(hestonProcess.rho() * hestonProcess.sigma()
               * mesher.locations(1));

         dyMap_ = new FdmHestonVariancePart(mesher,
                                            hestonProcess.riskFreeRate().currentLink(),
                                            hestonProcess.sigma(),
                                            hestonProcess.kappa(),
                                            hestonProcess.theta());
         dxMap_ = new FdmHestonEquityPart(mesher,
                                          hestonProcess.riskFreeRate().currentLink(),
                                          hestonProcess.dividendYield().currentLink(),
                                          quantoHelper,
                                          leverageFct);
      }
      public override int size() { return 2; }

      //! Time \f$t1 <= t2\f$ is required
      public override void setTime(double t1, double t2)
      {
         dxMap_.setTime(t1, t2);
         dyMap_.setTime(t1, t2);
      }

      public override Vector apply(Vector r)
      {
         return dyMap_.getMap().apply(r) + dxMap_.getMap().apply(r)
                + Vector.DirectMultiply(dxMap_.getL(), correlationMap_.apply(r));
      }

      public override Vector apply_mixed(Vector r)
      {
         return Vector.DirectMultiply(dxMap_.getL(), correlationMap_.apply(r));
      }

      public override Vector apply_direction(int direction, Vector r)
      {
         if (direction == 0)
            return dxMap_.getMap().apply(r);
         else if (direction == 1)
            return dyMap_.getMap().apply(r);
         else
         {
            Utils.QL_FAIL("direction too large");
            return new Vector();
         }
      }
      public override Vector solve_splitting(int direction, Vector r, double dt)
      {
         if (direction == 0)
         {
            return dxMap_.getMap().solve_splitting(r, dt, 1.0);
         }
         else if (direction == 1)
         {
            return dyMap_.getMap().solve_splitting(r, dt, 1.0);
         }
         else
         {
            Utils.QL_FAIL("direction too large");
            return new Vector();
         }
      }
      public override Vector preconditioner(Vector r, double dt)
      {
         return solve_splitting(0, r, dt);
      }

      public override List<SparseMatrix> toMatrixDecomp()
      {
         List<SparseMatrix> retVal = new InitializedList<SparseMatrix>(3);
         retVal[0] = dxMap_.getMap().toMatrix();
         retVal[1] = dyMap_.getMap().toMatrix();
         retVal[2] = correlationMap_.toMatrix();
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

      protected FdmHestonVariancePart dyMap_;
      protected FdmHestonEquityPart dxMap_;
      protected NinePointLinearOp correlationMap_;
   }
}
