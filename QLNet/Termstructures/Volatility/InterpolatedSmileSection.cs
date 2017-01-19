//  Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)
//  
//  This file is part of QLNet Project https://github.com/amaggiulli/qlnet
//  QLNet is free software: you can redistribute it and/or modify it
//  under the terms of the QLNet license.  You should have received a
//  copy of the license along with this program; if not, license is  
//  available online at <http://qlnet.sourceforge.net/License.html>.
//   
//  QLNet is a based on QuantLib, a free-software/open-source library
//  for financial quantitative analysts and developers - http://quantlib.org/
//  The QuantLib license is available online at http://quantlib.org/license.shtml.
//  
//  This program is distributed in the hope that it will be useful, but WITHOUT
//  ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
//  FOR A PARTICULAR PURPOSE.  See the license for more details.

using System;
using System.Collections.Generic;
using System.Linq;

namespace QLNet
{
   public class InterpolatedSmileSection<Interpolator> : SmileSection, InterpolatedCurve
      where Interpolator : IInterpolationFactory, new()
   {
      public InterpolatedSmileSection( double timeToExpiry,
                                       List<double> strikes,
                                       List<Handle<Quote>> stdDevHandles,
                                       Handle<Quote> atmLevel,
                                       Interpolator interpolator = default(Interpolator),
                                       DayCounter dc = null, //Actual365Fixed()
                                       VolatilityType type = VolatilityType.ShiftedLognormal,
                                       double shift = 0.0)
         : base( timeToExpiry, dc ?? new Actual365Fixed(), type, shift )
      {
         exerciseTimeSquareRoot_ = Math.Sqrt(exerciseTime());
         strikes_ = strikes;
         stdDevHandles_ = stdDevHandles;
         atmLevel_ = atmLevel;
         vols_ = new InitializedList<double>(stdDevHandles.Count);

         for (int i=0; i<stdDevHandles_.Count; ++i)
            stdDevHandles_[i].registerWith(update);
        
         atmLevel_.registerWith(update);
         // check strikes!!!!!!!!!!!!!!!!!!!!
         interpolation_ = interpolator.interpolate(strikes_,strikes_.Count,vols_);
      }

      public InterpolatedSmileSection( double timeToExpiry,
                                       List<double> strikes,
                                       List<double> stdDevs,
                                       double atmLevel,
                                       Interpolator interpolator = default(Interpolator),
                                       DayCounter dc = null , //Actual365Fixed(),
                                        VolatilityType type = VolatilityType.ShiftedLognormal,
                                       double shift = 0.0)
         :base(timeToExpiry, dc?? new Actual365Fixed(), type, shift)
      {
         exerciseTimeSquareRoot_ = Math.Sqrt(exerciseTime());
         strikes_ = strikes;
         stdDevHandles_ = new InitializedList<Handle<Quote>>(stdDevs.Count); 
         vols_= new InitializedList<double>(stdDevs.Count);
                 
         // fill dummy handles to allow generic handle-based
         // computations later on
         for (int i=0; i<stdDevs.Count; ++i)
            stdDevHandles_[i] = new Handle<Quote>(new SimpleQuote(stdDevs[i]));

         atmLevel_ = new Handle<Quote>(new SimpleQuote(atmLevel));
         // check strikes!!!!!!!!!!!!!!!!!!!!
         interpolation_ = interpolator.interpolate(strikes_,strikes_.Count,vols_);
      }

      public InterpolatedSmileSection( Date d,
                                       List<double> strikes,
                                       List<Handle<Quote> > stdDevHandles,
                                       Handle<Quote> atmLevel,
                                       DayCounter dc = null , //Actual365Fixed(),
                                       Interpolator interpolator = default(Interpolator),
                                       Date referenceDate = null,
                                       VolatilityType type = VolatilityType.ShiftedLognormal,
                                       double shift = 0.0)
         : base( d, dc?? new Actual365Fixed(), referenceDate, type, shift )
      {
         exerciseTimeSquareRoot_ = Math.Sqrt(exerciseTime());
         strikes_ = strikes;
         stdDevHandles_ = stdDevHandles;
         atmLevel_ = atmLevel; 
         vols_ = new InitializedList<double>(stdDevHandles.Count);

         for (int i=0; i<stdDevHandles_.Count; ++i)
            stdDevHandles_[i].registerWith(update);
         atmLevel_.registerWith(update);
         // check strikes!!!!!!!!!!!!!!!!!!!!
         interpolation_ = interpolator.interpolate(strikes_,strikes_.Count,vols_);
      }

       public InterpolatedSmileSection( Date d,
                                        List<double> strikes,
                                        List<double> stdDevs,
                                        double atmLevel,
                                        DayCounter dc = null , // Actual365Fixed(),
                                        Interpolator interpolator = default(Interpolator),
                                        Date referenceDate = null,
                                        double shift = 0.0)
         : base( d, dc?? new Actual365Fixed(), referenceDate, VolatilityType.ShiftedLognormal, shift )
       {
          strikes_ = strikes;
          stdDevHandles_ = new InitializedList<Handle<Quote>>(stdDevs.Count);
          vols_ = new InitializedList<double>(stdDevs.Count);

          //fill dummy handles to allow generic handle-based
          // computations later on
          for (int i = 0; i < stdDevs.Count; ++i)
             stdDevHandles_[i] = new Handle<Quote>(new SimpleQuote(stdDevs[i]));
          atmLevel_ = new Handle<Quote>(new SimpleQuote(atmLevel));
          // check strikes!!!!!!!!!!!!!!!!!!!!
          interpolation_ = interpolator.interpolate(strikes_, strikes_.Count, vols_);
       }

      protected override void performCalculations()
      {
         for (int i = 0; i < stdDevHandles_.Count; ++i)
            vols_[i] = stdDevHandles_[i].link.value()/exerciseTimeSquareRoot_;
         interpolation_.update();
      }


      protected override double varianceImpl(double strike)
      {
         calculate();
         double v = interpolation_.value(strike, true);
         return v*v*exerciseTime();
      }

      protected override double volatilityImpl(double strike)
      {
         calculate();
         return interpolation_.value(strike, true);
      }
      public override double minStrike () { return strikes_.First(); }
      public override double maxStrike () { return strikes_.Last(); }
      public override double? atmLevel()  { return atmLevel_.link.value(); }
      public override void update() {base.update();}

      private double exerciseTimeSquareRoot_;
      private List<double> strikes_;
      private List<Handle<Quote>> stdDevHandles_;
      private Handle<Quote> atmLevel_;
      private List<double> vols_;
      //private Interpolation interpolation_;

      #region InterpolatedCurve

      public List<double> times_ { get; set; }
      public List<double> times() { return this.times_; }

      public List<Date> dates_ { get; set; }
      public List<Date> dates() { return dates_; }
      public Date maxDate_ { get; set; }
      public Date maxDate()
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
         dates_.ForEach( ( i, x ) => results.Add( x, data_[i] ) );
         return results;
      }

      public void setupInterpolation()
      {
         interpolation_ = interpolator_.interpolate( times_, times_.Count, data_ );
      }

      public object Clone()
      {
         InterpolatedCurve copy = this.MemberwiseClone() as InterpolatedCurve;
         copy.times_ = new List<double>( times_ );
         copy.data_ = new List<double>( data_ );
         copy.interpolator_ = interpolator_;
         copy.setupInterpolation();
         return copy;
      }
      #endregion
   }
}
