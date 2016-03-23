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
   public class StrippedOptionletAdapter : OptionletVolatilityStructure
   {
       /*! Adapter class for turning a StrippedOptionletBase object into an
        OptionletVolatilityStructure.
      */
      public StrippedOptionletAdapter(StrippedOptionletBase s)
         :base(s.settlementDays(),s.calendar(),s.businessDayConvention(),s.dayCounter())
      {
         optionletStripper_ = s;
         nInterpolations_ = s.optionletMaturities();
         strikeInterpolations_ = new List<Interpolation>(nInterpolations_);

         optionletStripper_.registerWith(update);
      }

      //! \name TermStructure interface
      //@{
      public override Date maxDate() { return optionletStripper_.optionletFixingDates().Last(); }
      //@}
      //! \name VolatilityTermStructure interface
      //@{
      public override double minStrike() { return optionletStripper_.optionletStrikes(0).First(); }
      public override double maxStrike() { return optionletStripper_.optionletStrikes( 0 ).Last(); }
      //@} 
      //! \name LazyObject interface
      //@{
      public override void update() { base.update(); }

      protected override void performCalculations()
      {
         for (int i=0; i<nInterpolations_; ++i) 
         {
            List<double> optionletStrikes = new List<double>(optionletStripper_.optionletStrikes(i));
            List<double> optionletVolatilities = new List<double>(optionletStripper_.optionletVolatilities(i));
            strikeInterpolations_.Add(new LinearInterpolation(optionletStrikes,optionletStrikes.Count,optionletVolatilities));
         }
      }
      //@}

      
      //! \name OptionletVolatilityStructure interface
      //@{
      protected override SmileSection smileSectionImpl(double t)
      {
         List<double> optionletStrikes = new List<double>(optionletStripper_.optionletStrikes(0)); // strikes are the same for all times ?!
         List<double> stddevs = new List<double>();
         for(int i=0;i<optionletStrikes.Count;i++) 
         {
            stddevs.Add(volatilityImpl(t,optionletStrikes[i])*Math.Sqrt(t));
         }
         // Extrapolation may be a problem with splines, but since minStrike() and maxStrike() are set, we assume that no one will use stddevs for strikes outside these strikes
         CubicInterpolation.BoundaryCondition bc = optionletStrikes.Count>=4 ? CubicInterpolation.BoundaryCondition.Lagrange : CubicInterpolation.BoundaryCondition.SecondDerivative;
         return new InterpolatedSmileSection<Cubic>(t,optionletStrikes,stddevs,0,
                  new Cubic(CubicInterpolation.DerivativeApprox.Spline,false,bc,0.0,bc,0.0));
      }
      protected override double volatilityImpl(double length,double strike)
      {
         calculate();

         List<double> vol = new InitializedList<double>(nInterpolations_);
         for (int i=0; i<nInterpolations_; ++i)
            vol[i] = strikeInterpolations_[i].value(strike, true);

         List<double> optionletTimes = new List<double>(optionletStripper_.optionletFixingTimes());
         LinearInterpolation timeInterpolator = new LinearInterpolation(optionletTimes, optionletTimes.Count,vol);
         return timeInterpolator.value(length, true);
      }
      //@} 
    
      private StrippedOptionletBase optionletStripper_;
      private int nInterpolations_;
      private List<Interpolation> strikeInterpolations_;

   }
}
