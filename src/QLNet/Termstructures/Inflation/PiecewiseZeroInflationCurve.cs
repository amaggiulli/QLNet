/*
 Copyright (C) 2008-2016  Andrea Maggiulli (a.maggiulli@gmail.com)
 Copyright (C) 2014  Edem Dawui (edawui@gmail.com)
 Copyright (C) 2018 Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)

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

   public class PiecewiseZeroInflationCurve : ZeroInflationTermStructure, Curve<ZeroInflationTermStructure>
   {
      #region InflationTraits

      public Date initialDate(ZeroInflationTermStructure c) { return traits_.initialDate(c); }
      public double initialValue(ZeroInflationTermStructure c) { return traits_.initialValue(c); }
      public double guess<C>(int i, C c, bool validData, int first) where C : Curve<ZeroInflationTermStructure>
      { return traits_.guess(i, c, validData, first); }
      public double minValueAfter<C>(int i, C c, bool validData, int first) where C : Curve<ZeroInflationTermStructure>
      { return traits_.minValueAfter(i, c, validData, first); }
      public double maxValueAfter<C>(int i, C c, bool validData, int first) where C : Curve<ZeroInflationTermStructure>
      { return traits_.maxValueAfter(i, c, validData, first); }
      public void updateGuess(List<double> data, double discount, int i) { traits_.updateGuess(data, discount, i); }
      public int maxIterations() { return traits_.maxIterations(); }

      #endregion

      #region InterpolatedCurve
      protected ZeroInflationTermStructure base_curve;

      public List<double> times_
      {
         get
         {
            return (base_curve as InterpolatedCurve).times();
         }
         set
         {
            (base_curve as InterpolatedCurve).times_ = value;
         }
      }
      public List<double> times() { calculate(); return this.times_; }

      public List<Date> dates_
      {
         get
         {
            return (base_curve as InterpolatedCurve).dates();
         }
         set
         {
            (base_curve as InterpolatedCurve).dates_ = value;
         }
      }
      public List<Date> dates() { calculate(); return dates_; }
      public Date maxDate_
      {
         get
         {
            return (base_curve as InterpolatedCurve).maxDate();
         }
         set
         {
            (base_curve as InterpolatedCurve).maxDate_ = value;
         }
      }
      public override Date maxDate()
      {
         calculate();
         return this.maxDate_;
      }

      public List<double> data_
      {
         get
         {
            return (base_curve as InterpolatedCurve).data();
         }
         set
         {
            (base_curve as InterpolatedCurve).data_ = value;
         }
      }
      public List<double> forwards() { return this.data_; }
      public List<double> data() { return forwards(); }

      public Interpolation interpolation_
      {
         get
         {
            return (base_curve as InterpolatedCurve).interpolation_;
         }
         set
         {
            (base_curve as InterpolatedCurve).interpolation_ = value;
         }
      }
      public IInterpolationFactory interpolator_
      {
         get
         {
            if (base_curve != null)
               return (base_curve as InterpolatedCurve).interpolator_;

            else
               return null;
         }
         set
         {
            if (base_curve != null)
               (base_curve as InterpolatedCurve).interpolator_ = value;
         }
      }

      public Dictionary<Date, double> nodes()
      {
         calculate();
         return (base_curve as InterpolatedCurve).nodes();
      }

      public void setupInterpolation()
      {
         (base_curve as InterpolatedCurve).setupInterpolation();
      }

      public object Clone()
      {
         InterpolatedCurve copy = this.MemberwiseClone() as InterpolatedCurve;
         copy.times_ = new List<double>(times_);
         copy.data_ = new List<double>(data_);
         copy.interpolator_ = interpolator_;
         copy.setupInterpolation();
         (copy as PiecewiseZeroInflationCurve).base_curve = (base_curve as InterpolatedCurve).Clone() as ZeroInflationTermStructure;
         return copy;
      }

      #endregion

      public List<double> rates()
      {
         return this.data_;
      }

      protected internal override double zeroRateImpl(double t)
      {
         calculate();
         return base_curve.zeroRateImpl(t);
      }

      # region new fields: Curve

      public double initialValue() { return _traits_.initialValue(this); }
      public Date initialDate() { return _traits_.initialDate(this); }

      public void registerWith(BootstrapHelper<ZeroInflationTermStructure> helper)
      {
         helper.registerWith(this.update);
      }

      //public new bool moving_
      public new bool moving_
      {
         get
         {
            return base.moving_;
         }
         set
         {
            base.moving_ = value;
         }
      }

      public void setTermStructure(BootstrapHelper<ZeroInflationTermStructure> helper)
      {
         helper.setTermStructure(this);
      }


      protected ITraits<ZeroInflationTermStructure> _traits_ = null;//todo define with the trait for yield curve
      public ITraits<ZeroInflationTermStructure> traits_
      {
         get
         {
            return _traits_;
         }
      }

      protected List<BootstrapHelper<ZeroInflationTermStructure>> _instruments_ = new List<BootstrapHelper<ZeroInflationTermStructure>>();

      public List<BootstrapHelper<ZeroInflationTermStructure>> instruments_
      {
         get
         {
            //todo edem
            List<BootstrapHelper<ZeroInflationTermStructure>> instruments = new List<BootstrapHelper<ZeroInflationTermStructure>>();
            _instruments_.ForEach((i, x) => instruments.Add(x));
            return instruments;
         }
      }

      protected IBootStrap<PiecewiseZeroInflationCurve> bootstrap_;

      protected double _accuracy_;
      public double accuracy_
      {
         get
         {
            return _accuracy_;
         }
         set
         {
            _accuracy_ = value;
         }
      }

      public override Date baseDate()
      {
         calculate();
         // if indexIsInterpolated we fixed the dates in the constructor
         return base_curve.baseDate();
      }

      # endregion

      public PiecewiseZeroInflationCurve(DayCounter dayCounter, double baseZeroRate, Period observationLag, Frequency frequency,
                                         bool indexIsInterpolated, Handle<YieldTermStructure> yTS)
         : base(dayCounter, baseZeroRate, observationLag, frequency, indexIsInterpolated, yTS) { }

      public PiecewiseZeroInflationCurve(Date referenceDate, Calendar calendar, DayCounter dayCounter, double baseZeroRate,
                                         Period observationLag, Frequency frequency, bool indexIsInterpolated,
                                         Handle<YieldTermStructure> yTS)
         : base(referenceDate, calendar, dayCounter, baseZeroRate, observationLag, frequency, indexIsInterpolated, yTS) { }

      public PiecewiseZeroInflationCurve(int settlementDays, Calendar calendar, DayCounter dayCounter, double baseZeroRate,
                                         Period observationLag, Frequency frequency, bool indexIsInterpolated,
                                         Handle<YieldTermStructure> yTS)
         : base(settlementDays, calendar, dayCounter, baseZeroRate, observationLag, frequency, indexIsInterpolated, yTS) { }


      public PiecewiseZeroInflationCurve()
         : base()
      { }
   }


   public class PiecewiseZeroInflationCurve<Interpolator, Bootstrap, Traits> : PiecewiseZeroInflationCurve
      where Traits : ITraits<ZeroInflationTermStructure>, new ()
      where Interpolator : class, IInterpolationFactory, new ()
         where Bootstrap : IBootStrap<PiecewiseZeroInflationCurve>, new ()
   {

      public PiecewiseZeroInflationCurve(Date referenceDate,
                                         Calendar calendar,
                                         DayCounter dayCounter,
                                         Period lag,
                                         Frequency frequency,
                                         bool indexIsInterpolated,
                                         double baseZeroRate,
                                         Handle<YieldTermStructure> nominalTS,
                                         List<BootstrapHelper<ZeroInflationTermStructure>> instruments,
                                         double accuracy = 1.0e-12,
                                         Interpolator i = default(Interpolator),
                                         Bootstrap bootstrap = default(Bootstrap))
         : base(referenceDate, calendar, dayCounter, baseZeroRate, lag, frequency, indexIsInterpolated, nominalTS)
      {
         _instruments_ = instruments;
         _traits_ = FastActivator<Traits>.Create();
         base_curve = _traits_.factory(referenceDate, calendar, dayCounter, lag, frequency,
                                       indexIsInterpolated, baseZeroRate, nominalTS, i);
         accuracy_ = accuracy;

         if (bootstrap == null)
            bootstrap_ = FastActivator<Bootstrap>.Create();
         else
            bootstrap_ = bootstrap;

         if (i == null)
            interpolator_ = FastActivator<Interpolator>.Create();
         else
            interpolator_ = i;

         bootstrap_.setup(this);
      }

      // observer interface
      public override void update()
      {
         base_curve.update();
         base.update();
         // LazyObject::update();        // we do it in the TermStructure
         if (this.moving_)
            this.moving_ = false;
      }

      // methods
      protected override void performCalculations() { bootstrap_.calculate(); }
   }

   // Allows for optional 3rd generic parameter defaulted to IterativeBootstrap
   public class PiecewiseZeroInflationCurve<Interpolator> : PiecewiseZeroInflationCurve<Interpolator, IterativeBootstrapForInflation, ZeroInflationTraits>
      where Interpolator : class, IInterpolationFactory, new ()
   {
      public PiecewiseZeroInflationCurve(Date referenceDate,
                                         Calendar calendar,
                                         DayCounter dayCounter,
                                         Period lag,
                                         Frequency frequency,
                                         bool indexIsInterpolated,
                                         double baseZeroRate,
                                         Handle<YieldTermStructure> nominalTS,
                                         List<BootstrapHelper<ZeroInflationTermStructure>> instruments,
                                         double accuracy = 1.0e-12,
                                         Interpolator i = default(Interpolator))
         : base(referenceDate, calendar, dayCounter, lag, frequency, indexIsInterpolated, baseZeroRate, nominalTS,
                instruments, accuracy, i) { }
   }


}
