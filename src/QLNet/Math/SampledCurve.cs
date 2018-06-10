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
   //! This class contains a sampled curve.
   /*! Initially the class will contain one indexed curve */

   public class SampledCurve : ICloneable
   {
      private Vector grid_;

      public Vector grid()
      {
         return grid_;
      }

      private Vector values_;

      public Vector values()
      {
         return values_;
      }

      public SampledCurve(int gridSize)
      {
         grid_ = new Vector(gridSize);
         values_ = new Vector(gridSize);
      }

      public SampledCurve(Vector grid)
      {
         grid_ = grid.Clone();
         values_ = new Vector(grid.Count);
      }

      // instead of "=" overload
      public object Clone()
      {
         return MemberwiseClone();
      }

      public double gridValue(int i)
      {
         return grid_[i];
      }

      public double value(int i)
      {
         return values_[i];
      }

      public void setValue(int i, double v)
      {
         values_[i] = v;
      }

      public int size()
      {
         return grid_.Count;
      }

      public bool empty()
      {
         return grid_.Count == 0;
      }

      // modifiers
      public void setGrid(Vector g)
      {
         grid_ = g.Clone();
      }

      public void setValues(Vector g)
      {
         values_ = g.Clone();
      }

      public void sample(Func<double, double> f)
      {
         for (int i = 0; i < grid_.Count; i++)
            values_[i] = f(grid_[i]);
      }

      // calculations
      /*! \todo replace or complement with a more general function valueAt(spot) */

      public double valueAtCenter()
      {
         Utils.QL_REQUIRE(!empty(), () => "empty sampled curve");

         int jmid = size() / 2;
         if (size() % 2 == 1)
            return values_[jmid];

         return (values_[jmid] + values_[jmid - 1]) / 2.0;
      }

      /*! \todo replace or complement with a more general function firstDerivativeAt(spot) */

      public double firstDerivativeAtCenter()
      {
         Utils.QL_REQUIRE(size() >= 3, () => "the size of the curve must be at least 3");

         int jmid = size() / 2;
         if (size() % 2 == 1)
         {
            return (values_[jmid + 1] - values_[jmid - 1]) / (grid_[jmid + 1] - grid_[jmid - 1]);
         }
         return (values_[jmid] - values_[jmid - 1]) / (grid_[jmid] - grid_[jmid - 1]);
      }

      /*! \todo replace or complement with a more general function secondDerivativeAt(spot) */

      public double secondDerivativeAtCenter()
      {
         Utils.QL_REQUIRE(size() >= 4, () => "the size of the curve must be at least 4");
         int jmid = size() / 2;
         if (size() % 2 == 1)
         {
            double deltaPlus = (values_[jmid + 1] - values_[jmid]) / (grid_[jmid + 1] - grid_[jmid]);
            double deltaMinus = (values_[jmid] - values_[jmid - 1]) / (grid_[jmid] - grid_[jmid - 1]);
            double dS = (grid_[jmid + 1] - grid_[jmid - 1]) / 2.0;
            return (deltaPlus - deltaMinus) / dS;
         }
         else
         {
            double deltaPlus = (values_[jmid + 1] - values_[jmid - 1]) / (grid_[jmid + 1] - grid_[jmid - 1]);
            double deltaMinus = (values_[jmid] - values_[jmid - 2]) / (grid_[jmid] - grid_[jmid - 2]);
            return (deltaPlus - deltaMinus) / (grid_[jmid] - grid_[jmid - 1]);
         }
      }

      // utilities
      public void setLogGrid(double min, double max)
      {
         setGrid(Utils.BoundedLogGrid(min, max, size() - 1));
      }

      public void regridLogGrid(double min, double max)
      {
         regrid(Utils.BoundedLogGrid(min, max, size() - 1), Math.Log);
      }

      public void shiftGrid(double s)
      {
         grid_ += s;
      }

      public void scaleGrid(double s)
      {
         grid_ *= s;
      }

      public void regrid(Vector new_grid)
      {
         CubicInterpolation priceSpline = new CubicInterpolation(grid_, grid_.Count, values_,
                                                                 CubicInterpolation.DerivativeApprox.Spline, false,
                                                                 CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0,
                                                                 CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0);
         priceSpline.update();
         Vector newValues = new Vector(new_grid.Count);

         for (int i = 0; i < new_grid.Count; i++)
            newValues[i] = priceSpline.value(new_grid[i], true);

         values_ = newValues;
         grid_ = new_grid.Clone();
      }

      public void regrid(Vector new_grid, Func<double, double> func)
      {
         Vector transformed_grid = new Vector(grid_.Count);

         for (int i = 0; i < grid_.Count; i++)
            transformed_grid[i] = func(grid_[i]);

         CubicInterpolation priceSpline = new CubicInterpolation(transformed_grid, transformed_grid.Count, values_,
                                                                 CubicInterpolation.DerivativeApprox.Spline, false,
                                                                 CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0,
                                                                 CubicInterpolation.BoundaryCondition.SecondDerivative, 0.0);
         priceSpline.update();

         Vector newValues = new_grid.Clone();

         for (int i = 0; i < grid_.Count; i++)
            newValues[i] = func(newValues[i]);

         for (int j = 0; j < grid_.Count; j++)
            newValues[j] = priceSpline.value(newValues[j], true);

         values_ = newValues;
         grid_ = new_grid.Clone();
      }

      public SampledCurve transform(Func<double, double> x)
      {
         for (int i = 0; i < values_.Count; i++)
            values_[i] = x(values_[i]);
         return this;
      }

      public SampledCurve transformGrid(Func<double, double> x)
      {
         for (int i = 0; i < grid_.Count; i++)
            grid_[i] = x(grid_[i]);
         return this;
      }
   }
}
