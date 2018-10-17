/*
  Copyright (C) 2018 Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)
  Copyright (C) 2008 Chris Kenyon
  Copyright (C) 2008 Roland Lichters
  Copyright (C) 2008, 2009 StatPro Italia srl

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
   /// <summary>
   /// DefaultProbabilityTermStructure based on interpolation of default densities
   /// </summary>
   /// <typeparam name="Interpolator"></typeparam>
   public class InterpolatedDefaultDensityCurve<Interpolator> : DefaultDensityStructure, InterpolatedCurve
      where Interpolator : IInterpolationFactory, new ()
   {
      public InterpolatedDefaultDensityCurve(
         List<Date> dates,
         List<double> densities,
         DayCounter dayCounter,
         Calendar calendar = null,
         List<Handle<Quote> > jumps = null,
         List<Date> jumpDates = null,
         Interpolator interpolator = default(Interpolator))
         : base(dates[0], calendar, dayCounter, jumps, jumpDates)
      {
         dates_ = new List<Date>(dates);
         times_ = new List<double>();
         data_ = new List<double>(densities);

         if (interpolator == null)
            interpolator_ = FastActivator<Interpolator>.Create();
         else
            interpolator_ = interpolator;

         initialize(dates, densities, dayCounter);
      }
      public InterpolatedDefaultDensityCurve(
         List<Date> dates,
         List<double> densities,
         DayCounter dayCounter,
         Calendar calendar,
         Interpolator interpolator = default(Interpolator))
         : base(dates[0], calendar, dayCounter)
      {
         dates_ = new List<Date>(dates);
         times_ = new List<double>();
         data_ = new List<double>(densities);

         if (interpolator == null)
            interpolator_ = FastActivator<Interpolator>.Create();
         else
            interpolator_ = interpolator;

         initialize(dates, densities, dayCounter);
      }
      public InterpolatedDefaultDensityCurve(
         List<Date> dates,
         List<double> densities,
         DayCounter dayCounter,
         Interpolator interpolator = default(Interpolator))
         : base(dates[0], null, dayCounter)
      {
         dates_ = new List<Date>(dates);
         times_ = new List<double>();
         data_ = new List<double>(densities);

         if (interpolator == null)
            interpolator_ = FastActivator<Interpolator>.Create();
         else
            interpolator_ = interpolator;

         initialize(dates, densities, dayCounter);
      }
      /// <summary>
      /// TermStructure interface
      /// </summary>
      /// <returns></returns>
      public override Date maxDate() { return dates_.Last(); }

      // other inspectors
      public List<double> times() { return this.times_; }
      public List<Date> dates() { return dates_; }
      public List<double> data() { return this.data_; }
      public List<double> defaultDensities() { return this.data_; }
      public Dictionary<Date, double> nodes()
      {
         Dictionary<Date, double> results = new Dictionary<Date, double>();
         dates_.ForEach((i, x) => results.Add(x, data_[i]));
         return results;
      }

      public InterpolatedDefaultDensityCurve(
         DayCounter dayCounter,
         List<Handle<Quote> > jumps = null,
         List<Date> jumpDates = null,
         Interpolator interpolator = default(Interpolator))
         : base(dayCounter, jumps, jumpDates)
      {
         if (interpolator == null)
            interpolator_ = FastActivator<Interpolator>.Create();
         else
            interpolator_ = interpolator;
      }
      public InterpolatedDefaultDensityCurve(
         Date referenceDate,
         DayCounter dayCounter,
         List<Handle<Quote> > jumps = null,
         List<Date> jumpDates = null,
         Interpolator interpolator = default(Interpolator))
         : base(referenceDate, null, dayCounter, jumps, jumpDates)
      {
         if (interpolator == null)
            interpolator_ = FastActivator<Interpolator>.Create();
         else
            interpolator_ = interpolator;
      }
      public InterpolatedDefaultDensityCurve(
         int settlementDays,
         Calendar cal,
         DayCounter dayCounter,
         List<Handle<Quote> > jumps = null,
         List<Date> jumpDates = null,
         Interpolator interpolator = default(Interpolator))
         : base(settlementDays, cal, dayCounter, jumps, jumpDates)
      {
         if (interpolator == null)
            interpolator_ = FastActivator<Interpolator>.Create();
         else
            interpolator_ = interpolator;
      }

      /// <summary>
      /// DefaultProbabilityTermStructure implementation
      /// </summary>
      /// <param name="t"></param>
      /// <returns></returns>
      protected internal override double survivalProbabilityImpl(double t)
      {
         if (t == 0.0)
            return 1.0;

         double integral = 0.0;
         if (t <= this.times_.Last())
         {
            integral = this.interpolation_.primitive(t, true);
         }
         else
         {
            // flat default density extrapolation
            integral = this.interpolation_.primitive(this.times_.Last(), true)
                       + this.data_.Last() * (t - this.times_.Last());
         }
         double P = 1.0 - integral;
         return Math.Max(P, 0.0);
      }

      protected internal override double defaultDensityImpl(double t)
      {
         if (t <= this.times_.Last())
            return -this.interpolation_.value(t, true);

         return this.data_.Last();
      }

      protected void initialize(List<Date> dates,
                                List<double> densities,
                                DayCounter dayCounter)
      {
         Utils.QL_REQUIRE(dates_.Count >= interpolator_.requiredPoints, () => "not enough input dates given");
         Utils.QL_REQUIRE(this.data_.Count == dates_.Count, () => "dates/data count mismatch");

         this.times_.Add(0.0);
         for (int i = 1; i < dates_.Count; ++i)
         {
            Utils.QL_REQUIRE(dates_[i] > dates_[i - 1], () => "invalid date (" + dates_[i] + ", vs " + dates_[i - 1] + ")");
            this.times_.Add(dayCounter.yearFraction(dates_[0], dates_[i]));
            Utils.QL_REQUIRE(!Utils.close(this.times_[i], this.times_[i - 1]), () => "two dates correspond to the same time " +
                             "under this curve's day count convention");
            Utils.QL_REQUIRE(this.data_[i] >= 0.0, () => "negative hazard rate");
         }

         setupInterpolation();
         this.interpolation_.update();
      }

      #region InterpolatedCurve

      public List<double> times_ { get; set; }

      public List<Date> dates_ { get; set; }
      public Date maxDate_ { get; set; }

      public List<double> data_ { get; set; }
      public List<double> discounts() { return this.data_; }

      public Interpolation interpolation_ { get; set; }
      public IInterpolationFactory interpolator_ { get; set; }

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
   }
}
