/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)

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
   //! %Lattice (tree, finite-differences) base class
   public abstract class Lattice
   {
      protected TimeGrid t_;

      public TimeGrid timeGrid()
      {
         return t_;
      }

      protected Lattice(TimeGrid timeGrid)
      {
         t_ = timeGrid;
      }

      /* Numerical method interface

          These methods are to be used by discretized assets and
          must be overridden by developers implementing numerical
          methods. Users are advised to use the corresponding
          methods of DiscretizedAsset instead.
      */

      //! initialize an asset at the given time.
      public abstract void initialize(DiscretizedAsset a, double time);

      /*! Roll back an asset until the given time, performing any needed adjustment. */
      public abstract void rollback(DiscretizedAsset a, double to);

      /*! Roll back an asset until the given time, but do not perform
          the final adjustment.
      */
      public abstract void partialRollback(DiscretizedAsset a, double to);

      //! computes the present value of an asset.
      public abstract double presentValue(DiscretizedAsset a);

      // this is a smell, but we need it. We'll rethink it later.
      public virtual Vector grid(double t)
      {
         throw new NotImplementedException();
      }
   }
}
