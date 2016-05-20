/*
 Copyright (C) 2008, 2009 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2016  Andrea Maggiulli (a.maggiulli@gmail.com)

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
using System.Linq;

namespace QLNet
{
   //! Term structure based on interpolation of discount factors
   /*! \ingroup yieldtermstructures */
   public class InterpolatedDiscountCurve<Interpolator> : YieldTermStructure, InterpolatedCurve
       where Interpolator : class, IInterpolationFactory, new()
   {

      #region InterpolatedCurve
      public List<double> times_ { get; set; }
      public List<double> times() { return this.times_; }

      public List<Date> dates_ { get; set; }
      public List<Date> dates() { return dates_; }
      public Date maxDate_ { get; set; }
      public override Date maxDate()
      {
         if ( maxDate_ != null )
            return maxDate_;

         return dates_.Last();
      }


      public List<double> data_ { get; set; }
      public List<double> discounts() { return this.data_; }
      public List<double> data() { return discounts(); }

      public Interpolation interpolation_ { get; set; }
      public IInterpolationFactory interpolator_ { get; set; }

      public Dictionary<Date, double> nodes()
      {
         Dictionary<Date, double> results = new Dictionary<Date, double>();
         dates_.ForEach((i, x) => results.Add(x, data_[i]));
         return results;
      }

      public void setupInterpolation()
      {
         interpolation_ = interpolator_.interpolate(times_, times_.Count, data_);
      }

      public object Clone()
      {
         InterpolatedCurve copy = this.MemberwiseClone() as InterpolatedCurve;
         copy.times_ = new List<double>(times_);
         copy.data_ = new List<double>(data_);
         copy.interpolator_ = interpolator_;
         copy.setupInterpolation();
         return copy;
      }
      #endregion


      public InterpolatedDiscountCurve(DayCounter dayCounter,
                                       List<Handle<Quote>> jumps = null,
                                       List<Date> jumpDates = null,
                                       Interpolator interpolator = default(Interpolator))
         : base(dayCounter, jumps, jumpDates)
      {
         interpolator_ = interpolator ?? new Interpolator();
      }

      public InterpolatedDiscountCurve(Date referenceDate,
                                       DayCounter dayCounter,
                                       List<Handle<Quote>> jumps = null,
                                       List<Date> jumpDates = null,
                                       Interpolator interpolator = default(Interpolator))
         : base(referenceDate, null, dayCounter, jumps, jumpDates)
      {
         interpolator_ = interpolator ?? new Interpolator();
      }

      public InterpolatedDiscountCurve(int settlementDays,
                                       Calendar calendar,
                                       DayCounter dayCounter,
                                       List<Handle<Quote>> jumps = null,
                                       List<Date> jumpDates = null,
                                       Interpolator interpolator = default(Interpolator))
         : base(settlementDays, calendar, dayCounter, jumps, jumpDates)
      {
         interpolator_ = interpolator ?? new Interpolator();
      }

      public InterpolatedDiscountCurve(List<Date> dates,
                                       List<double> discounts,
                                       DayCounter dayCounter,
                                       Calendar calendar = null,
                                       List<Handle<Quote>> jumps = null,
                                       List<Date> jumpDates = null,
                                       Interpolator interpolator = default(Interpolator))
         : base(dates[0], calendar, dayCounter, jumps, jumpDates)
      {
         times_ = new List<double>();
         dates_ = dates;
         data_ = discounts;
         interpolator_ = interpolator ?? new Interpolator();
         initialize();
      }

      public InterpolatedDiscountCurve(List<Date> dates,
                                       List<double> discounts,
                                       DayCounter dayCounter,
                                       Calendar calendar,
                                       Interpolator interpolator)
         : base(dates[0], calendar, dayCounter)
      {
         times_ = new List<double>();
         dates_ = dates;
         data_ = discounts;
         interpolator_ = interpolator;
         initialize();
      }

      public InterpolatedDiscountCurve(List<Date> dates,
                                       List<double> discounts,
                                       DayCounter dayCounter,
                                       Interpolator interpolator)
         : base(dates[0], null, dayCounter)
      {
         times_ = new List<double>();
         dates_ = dates;
         data_ = discounts;
         interpolator_ = interpolator;
         initialize();
      }

      public InterpolatedDiscountCurve(List<Date> dates,
                                      List<double> discounts,
                                      DayCounter dayCounter,
                                      List<Handle<Quote>> jumps,
                                      List<Date> jumpDates,
                                      Interpolator interpolator = default(Interpolator))
         : base(dates[0], null, dayCounter, jumps, jumpDates)
      {
         times_ = new List<double>();
         dates_ = dates;
         data_ = discounts;
         interpolator_ = interpolator ?? new Interpolator();
         initialize();
      }

      private void initialize()
      {
         Utils.QL_REQUIRE(dates_.Count >= interpolator_.requiredPoints,
            () => "not enough input dates givesn");
         Utils.QL_REQUIRE(this.data_.Count == this.dates_.Count,
            () => "dates/data count mismatch");
         Utils.QL_REQUIRE(this.data_[0] == 1.0, () => "the first discount must be == 1.0 " +
                                                      "to flag the corrsponding date as settlement date");

         times_ = new InitializedList<double>(dates_.Count - 1);
         times_.Add(0.0);
         for (int i = 1; i < dates_.Count; i++)
         {
            Utils.QL_REQUIRE(dates_[i] > dates_[i - 1],
               () => "invalid date (" + dates_[i] + ", vs " + dates_[i - 1] + ")");
            times_[i] = dayCounter().yearFraction(dates_[0], dates_[i]);
            Utils.QL_REQUIRE(!Utils.close(this.times_[i], this.times_[i - 1]),
               () => "two dates correspond to the same time " +
                     "under this curve's day count convention");
            Utils.QL_REQUIRE(this.data_[i] > 0.0, () => "negative discount");

#if !QL_NEGATIVE_RATES
            Utils.QL_REQUIRE(this.data_[i] <= this.data_[i-1],
               () => "negative forward rate implied by the discount " +
                     this.data_[i] + " at " + dates_[i] +
                     " (t=" + this.times_[i] + ") after the discount " +
                     this.data_[i-1] + " at " + dates_[i-1] +
                     " (t=" + this.times_[i-1] + ")");
#endif

         }

         setupInterpolation();
         interpolation_.update();
      }

      protected override double discountImpl(double t)
      {
         if (t <= this.times_.Last())
            return interpolation_.value(t, true);

         // flat fwd extrapolation
         double tMax = this.times_.Last();
         double dMax = this.data_.Last();
         double instFwdMax = -this.interpolation_.derivative(tMax) / dMax;
         return dMax * Math.Exp(-instFwdMax * (t - tMax));
      }
   }
}
