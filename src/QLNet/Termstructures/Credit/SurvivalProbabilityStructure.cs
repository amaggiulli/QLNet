//  Copyright (C) 2008-2017 Andrea Maggiulli (a.maggiulli@gmail.com)
//
//  This file is part of QLNet Project https://github.com/amaggiulli/qlnet
//  QLNet is free software: you can redistribute it and/or modify it
//  under the terms of the QLNet license.  You should have received a
//  copy of the license along with this program; if not, license is
//  available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.
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
using System.Text;
using System.Threading.Tasks;

namespace QLNet
{
   /// <summary>
   /// Hazard-rate term structure
   /// This abstract class acts as an adapter to
   /// DefaultProbabilityTermStructure allowing the programmer to implement
   /// only the survivalProbabilityImpl(Time) method in derived classes.
   /// <remarks>
   /// Hazard rates and default densities are calculated from survival probabilities.
   /// </remarks>
   /// </summary>
   public abstract class SurvivalProbabilityStructure : DefaultProbabilityTermStructure
   {
      public SurvivalProbabilityStructure(DayCounter dayCounter = null,
                                          List<Handle<Quote>> jumps = null,
                                          List<Date> jumpDates = null)
         : base(dayCounter, jumps, jumpDates) {}
      public SurvivalProbabilityStructure(Date referenceDate,
                                          Calendar cal = null,
                                          DayCounter dayCounter = null,
                                          List<Handle<Quote>> jumps = null,
                                          List<Date> jumpDates = null)
         : base(referenceDate, cal, dayCounter, jumps, jumpDates) { }

      public SurvivalProbabilityStructure(int settlementDays,
                                          Calendar cal,
                                          DayCounter dayCounter = null,
                                          List<Handle<Quote>> jumps = null,
                                          List<Date> jumpDates = null)
         : base(settlementDays, cal, dayCounter, jumps, jumpDates) { }

      /// <summary>
      /// DefaultProbabilityTermStructure implementation
      /// </summary>
      /// <remarks>
      /// This implementation uses numerical differentiation,which might be inefficient and inaccurate.
      /// Derived classes should override it if a more efficient implementation is available.
      /// </remarks>
      /// <param name="t"></param>
      /// <returns></returns>
      protected override double defaultDensityImpl(double t)
      {
         double dt = 0.0001;
         double t1 = Math.Max(t - dt, 0.0);
         double t2 = t + dt;

         double p1 = survivalProbabilityImpl(t1);
         double p2 = survivalProbabilityImpl(t2);

         return (p1 - p2) / (t2 - t1);
      }
   }
}
