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
   public partial class Utils
   {
      public static Date time2Date(Date referenceDate, DayCounter dc, double t)
      {
         t -= 1e4 * Const.QL_EPSILON; // add a small buffer for rounding errors
         Date d = new Date(referenceDate);
         while (dc.yearFraction(referenceDate, d += new Period(1, TimeUnit.Years)) < t);
         d -= new Period(1, TimeUnit.Years);
         while (dc.yearFraction(referenceDate, d += new Period(1, TimeUnit.Months)) < t);
         d -= new Period(1, TimeUnit.Months);
         while (dc.yearFraction(referenceDate, d++) < t);
         return d;
      }
   }

   public class FixedLocalVolSurface : LocalVolTermStructure
   {
      public enum Extrapolation
      {
         ConstantExtrapolation,
         InterpolatorDefaultExtrapolation
      }

      public FixedLocalVolSurface(Date referenceDate,
                                  List<Date> dates,
                                  List<double> strikes,
                                  Matrix localVolMatrix,
                                  DayCounter dayCounter,
                                  Extrapolation lowerExtrapolation =
                                     Extrapolation.ConstantExtrapolation,
                                  Extrapolation upperExtrapolation =
                                     Extrapolation.ConstantExtrapolation)
         : base(referenceDate, null, BusinessDayConvention.Following, dayCounter)
      {
         maxDate_ = dates.Last();
         localVolMatrix_ = localVolMatrix;
         strikes_ = new InitializedList<List<double>>(dates.Count, new List<double>(strikes));
         localVolInterpol_ = new List<Interpolation>();
         lowerExtrapolation_ = lowerExtrapolation;
         upperExtrapolation_ = upperExtrapolation;

         Utils.QL_REQUIRE(dates[0] >= referenceDate,
                          () => "cannot have dates[0] < referenceDate");

         times_ = new InitializedList<double>(dates.Count);
         for (int j = 0; j < times_.Count; j++)
            times_[j] = timeFromReference(dates[j]);

         checkSurface();
         setInterpolation<Linear>();
      }

      public FixedLocalVolSurface(Date referenceDate,
                                  List<double> times,
                                  List<double> strikes,
                                  Matrix localVolMatrix,
                                  DayCounter dayCounter,
                                  Extrapolation lowerExtrapolation =
                                     Extrapolation.ConstantExtrapolation,
                                  Extrapolation upperExtrapolation =
                                     Extrapolation.ConstantExtrapolation)
         : base(referenceDate, null, BusinessDayConvention.Following, dayCounter)
      {
         maxDate_ = Utils.time2Date(referenceDate, dayCounter, times.Last());
         times_ = times;
         localVolMatrix_ = localVolMatrix;
         strikes_ = new InitializedList<List<double>>(times.Count, new List<double>(strikes));
         localVolInterpol_ = new List<Interpolation>();
         lowerExtrapolation_ = lowerExtrapolation;
         upperExtrapolation_ = upperExtrapolation;

         Utils.QL_REQUIRE(times[0] >= 0.0,
                          () => "cannot have times[0] < 0");

         checkSurface();
         setInterpolation<Linear>();
      }

      public FixedLocalVolSurface(Date referenceDate,
                                  List<double> times,
                                  List<List<double>> strikes,
                                  Matrix localVolMatrix,
                                  DayCounter dayCounter,
                                  Extrapolation lowerExtrapolation =
                                     Extrapolation.ConstantExtrapolation,
                                  Extrapolation upperExtrapolation =
                                     Extrapolation.ConstantExtrapolation)
         : base(referenceDate, null, BusinessDayConvention.Following, dayCounter)
      {
         maxDate_ = Utils.time2Date(referenceDate, dayCounter, times.Last());
         times_ = times;
         localVolMatrix_ = localVolMatrix;
         strikes_ = strikes;
         localVolInterpol_ = new List<Interpolation>();
         lowerExtrapolation_ = lowerExtrapolation;
         upperExtrapolation_ = upperExtrapolation;

         Utils.QL_REQUIRE(times[0] >= 0.0,
                          () => "cannot have times[0] < 0");

         Utils.QL_REQUIRE(times.Count == strikes.Count,
                          () => "need strikes for every time step");

         checkSurface();
         setInterpolation<Linear>();
      }


      public override Date maxDate() { return maxDate_; }
      public override double maxTime() { return times_.Last(); }
      public override double minStrike() { return strikes_.Min().Min(); }
      public override double maxStrike() { return strikes_.Max().Max(); }

      public void setInterpolation<Interpolator>(Interpolator i = default(Interpolator))
      where Interpolator : class, IInterpolationFactory, new ()
      {
         localVolInterpol_.Clear();
         Interpolator i_ = i ?? FastActivator<Interpolator>.Create();
         for (int j = 0; j < times_.Count; ++j)
         {
            localVolInterpol_.Add(i_.interpolate(
                                     strikes_[j], strikes_[j].Count,
                                     localVolMatrix_.column(j)));
         }
         notifyObservers();
      }

      protected override double localVolImpl(double t, double strike)
      {
         t = Math.Min(times_.Last(), Math.Max(t, times_.First()));

         int idx = times_.BinarySearch(t);
         if (idx < 0)
            idx = ~idx;

         if (idx == times_.Count)
            idx--;
         if (Utils.close_enough(t, times_[idx]))
         {
            if (strikes_[idx].First() < strikes_[idx].Last())
               return localVolInterpol_[idx].value(strike, true);
            else
               return localVolMatrix_[localVolMatrix_.rows() / 2, idx];
         }
         else
         {
            double earlierStrike = strike, laterStrike = strike;
            if (lowerExtrapolation_ == Extrapolation.ConstantExtrapolation)
            {
               if (strike < strikes_[idx - 1].First())
                  earlierStrike = strikes_[idx - 1].First();
               if (strike < strikes_[idx].First())
                  laterStrike = strikes_[idx].First();
            }

            if (upperExtrapolation_ == Extrapolation.ConstantExtrapolation)
            {
               if (strike > strikes_[idx - 1].Last())
                  earlierStrike = strikes_[idx - 1].Last();
               if (strike > strikes_[idx].Last())
                  laterStrike = strikes_[idx].Last();
            }

            double earlyVol =
               (strikes_[idx - 1].First() < strikes_[idx - 1].Last())
               ? localVolInterpol_[idx - 1].value(earlierStrike, true)
               : localVolMatrix_[localVolMatrix_.rows() / 2, idx - 1];
            double laterVol = localVolInterpol_[idx].value(laterStrike, true);

            return earlyVol
                   + (laterVol - earlyVol) / (times_[idx] - times_[idx - 1])
                   * (t - times_[idx - 1]);
         }
      }

      protected Date maxDate_;
      protected List<double> times_;
      protected Matrix localVolMatrix_;
      protected List<List<double>> strikes_;

      protected List<Interpolation> localVolInterpol_;
      protected Extrapolation lowerExtrapolation_, upperExtrapolation_;

      private void checkSurface()
      {
         Utils.QL_REQUIRE(times_.Count == localVolMatrix_.columns(),
                          () => "mismatch between date vector and vol matrix colums");
         for (int i = 0; i < strikes_.Count; ++i)
         {
            Utils.QL_REQUIRE(strikes_[i].Count == localVolMatrix_.rows(),
                             () => "mismatch between money-strike vector and "
                             + "vol matrix rows");
         }

         for (int j = 1; j < times_.Count; j++)
         {
            Utils.QL_REQUIRE(times_[j] > times_[j - 1],
                             () => "dates must be sorted unique!");
         }

         for (int i = 0; i < strikes_.Count; ++i)
            for (int j = 1; j < strikes_[i].Count; j++)
            {
               Utils.QL_REQUIRE(strikes_[i][j] >= strikes_[i][j - 1],
                                () => "strikes must be sorted");
            }
      }
   }
}
