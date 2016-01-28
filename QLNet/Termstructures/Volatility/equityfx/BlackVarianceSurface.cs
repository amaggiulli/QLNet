/*
 Copyright (C) 2016 Francois Botha (igitur@gmail.com)

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
using System.Linq;

namespace QLNet
{
   //! Black volatility surface modelled as variance surface
   /*! This class calculates time/strike dependent Black volatilities
       using as input a matrix of Black volatilities observed in the
       market.

       The calculation is performed interpolating on the variance
       surface.  Bilinear interpolation is used as default; this can
       be changed by the setInterpolation() method.

       \todo check time extrapolation

   */

   public class BlackVarianceSurface : BlackVarianceTermStructure
   {
      public enum Extrapolation
      {
         ConstantExtrapolation,
         InterpolatorDefaultExtrapolation
      }

      private DayCounter dayCounter_;
      private Date maxDate_;
      private List<double> strikes_;
      private List<double> times_;
      private Matrix variances_;
      private Interpolation2D varianceSurface_;
      private Extrapolation lowerExtrapolation_, upperExtrapolation_;

      //! \name TermStructure interface
      //@{
      public override DayCounter dayCounter() { return dayCounter_; }

      public override Date maxDate()
      {
         return maxDate_;
      }

      //@}
      //! \name VolatilityTermStructure interface
      //@{
      public override double minStrike() { return strikes_.First(); }

      public override double maxStrike()
      {
         return strikes_.Last();
      }

      //@}

      // required for Handle
      public BlackVarianceSurface() { }

      public BlackVarianceSurface(Date referenceDate,
                                  Calendar calendar,
                                  List<Date> dates,
                                  List<double> strikes,
                                  Matrix blackVolMatrix,
                                  DayCounter dayCounter,
                                  Extrapolation lowerExtrapolation = Extrapolation.InterpolatorDefaultExtrapolation,
                                  Extrapolation upperExtrapolation = Extrapolation.InterpolatorDefaultExtrapolation)
         : base(referenceDate, calendar)
      {
         dayCounter_ = dayCounter;
         maxDate_ = dates.Last();
         strikes_ = strikes;
         lowerExtrapolation_ = lowerExtrapolation;
         upperExtrapolation_ = upperExtrapolation;

         Utils.QL_REQUIRE(dates.Count == blackVolMatrix.columns(), () =>
           "mismatch between date vector and vol matrix colums");
         Utils.QL_REQUIRE(strikes_.Count == blackVolMatrix.rows(), () =>
                    "mismatch between money-strike vector and vol matrix rows");
         Utils.QL_REQUIRE(dates[0] >= referenceDate, () =>
                    "cannot have dates[0] < referenceDate");

         int i, j;
         times_ = new InitializedList<double>(dates.Count + 1);
         times_[0] = 0.0;
         variances_ = new Matrix(strikes_.Count, dates.Count + 1);
         for (i = 0; i < blackVolMatrix.rows(); i++)
         {
            variances_[i, 0] = 0.0;
         }
         for (j = 1; j <= blackVolMatrix.columns(); j++)
         {
            times_[j] = timeFromReference(dates[j - 1]);
            Utils.QL_REQUIRE(times_[j] > times_[j - 1],
                       () => "dates must be sorted unique!");
            for (i = 0; i < blackVolMatrix.rows(); i++)
            {
               variances_[i, j] = times_[j] * blackVolMatrix[i, j - 1] * blackVolMatrix[i, j - 1];
            }
         }

         // default: bilinear interpolation
         setInterpolation<Bilinear>();
      }

      protected override double blackVarianceImpl(double t, double strike)
      {
         if (t == 0.0) return 0.0;
         // enforce constant extrapolation when required
         if (strike < strikes_.First() && lowerExtrapolation_ == Extrapolation.ConstantExtrapolation)
            strike = strikes_.First();
         if (strike > strikes_.Last() && upperExtrapolation_ == Extrapolation.ConstantExtrapolation)
            strike = strikes_.Last();

         if (t <= times_.Last())
            return varianceSurface_.value(t, strike, true);
         else // t>times_.Last() || extrapolate
            return varianceSurface_.value(times_.Last(), strike, true) * t / times_.Last();
      }

      public void setInterpolation<Interpolator>() where Interpolator : IInterpolationFactory2D, new()
      {
         setInterpolation<Interpolator>(new Interpolator());
      }

      public void setInterpolation<Interpolator>(Interpolator i) where Interpolator : IInterpolationFactory2D, new()
      {
         varianceSurface_ = i.interpolate(times_, times_.Count, strikes_, strikes_.Count, variances_);
         varianceSurface_.update();
         notifyObservers();
      }
   }
}
