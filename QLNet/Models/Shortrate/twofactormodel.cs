﻿/*
 Copyright (C) 2010 Philippe Real (ph_real@hotmail.com)
  
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

namespace QLNet
{
   public abstract class TwoFactorModel : ShortRateModel
   {
      protected TwoFactorModel(int nArguments)
         : base(nArguments)
      {}

      public abstract ShortRateDynamics dynamics();

      public override Lattice tree(TimeGrid grid)
      {
         ShortRateDynamics dyn = dynamics();
         TrinomialTree tree1 = new TrinomialTree(dyn.xProcess(), grid);
         TrinomialTree tree2 = new TrinomialTree(dyn.yProcess(), grid);
         return (Lattice) (new ShortRateTree(tree1, tree2, dyn));
      }

      //! Class describing the dynamics of the two state variables
      /*! We assume here that the short-rate is a function of two state
          variables x and y.
      */

      public abstract class ShortRateDynamics
      {
         StochasticProcess1D xProcess_, yProcess_;
         public double correlation_;

         protected ShortRateDynamics(StochasticProcess1D xProcess,
            StochasticProcess1D yProcess,
            double correlation)
         {
            xProcess_ = xProcess;
            yProcess_ = yProcess;
            correlation_ = correlation;
         }

         public abstract double shortRate(double t, double x, double y);

         //! Risk-neutral dynamics of the first state variable x
         public StochasticProcess1D xProcess()
         {
            return xProcess_;
         }

         //! Risk-neutral dynamics of the second state variable y
         public StochasticProcess1D yProcess()
         {
            return yProcess_;
         }

         //! Correlation \f$ \rho \f$ between the two brownian motions.
         public double correlation()
         {
            return correlation_;
         }

         //! Joint process of the two variables
         public abstract StochasticProcess process();
      }

      //! Recombining two-dimensional tree discretizing the state variable
      public class ShortRateTree : TreeLattice2D<ShortRateTree, TrinomialTree>, IGenericLattice
      {
         protected override ShortRateTree impl()
         {
            return this;
         }

         ShortRateDynamics dynamics_;

         //! Plain tree build-up from short-rate dynamics
         public ShortRateTree(TrinomialTree tree1,
            TrinomialTree tree2,
            ShortRateDynamics dynamics)
            : base(tree1, tree2, dynamics.correlation())
         {
            dynamics_ = dynamics;
         }

         public double discount(int i, int index)
         {
            int modulo = tree1_.size(i);
            int index1 = index % modulo;
            int index2 = index / modulo;

            double x = tree1_.underlying(i, index1);
            double y = tree2_.underlying(i, index2);

            double r = dynamics_.shortRate(timeGrid()[i], x, y);
            return Math.Exp(-r * timeGrid().dt(i));
         }

         #region Interface

         public double underlying(int i, int index)
         {
            throw new NotImplementedException();
         }

         #endregion
      }
   }

}

