/*
 Copyright (C) 2018 Jean-Camille Tournier (jean-camille.tournier@avivainvestors.com)

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
using System.Text;

namespace QLNet
{
   public class t_remapper
   {
      public Func<double, double> f;
      public double T;
      public t_remapper(Func<double, double> f, double T)
      {
         this.f = f;
         this.T = T;
      }
      public double value(double x)
      {
         double arg = (x + 1.0) * T / 2.0;
         return f(arg);
      }
   }
   public class DefaultDensityStructure : DefaultProbabilityTermStructure
   {
      public DefaultDensityStructure(
         DayCounter dayCounter = null,
         List<Handle<Quote>> jumps = null,
         List<Date> jumpDates = null)
         : base(dayCounter, jumps, jumpDates)
      {}
      public DefaultDensityStructure(
         Date referenceDate,
         Calendar cal = null,
         DayCounter dayCounter = null,
         List<Handle<Quote>> jumps = null,
         List<Date> jumpDates = null)
         : base(referenceDate, cal, dayCounter, jumps, jumpDates)
      { }
      public DefaultDensityStructure(
         int settlementDays,
         Calendar cal,
         DayCounter dayCounter = null,
         List<Handle<Quote> > jumps = null,
         List<Date> jumpDates = null)
         : base(settlementDays, cal, dayCounter, jumps, jumpDates)
      { }
      protected internal override double survivalProbabilityImpl(double t)
      {
         GaussChebyshevIntegration integral = new GaussChebyshevIntegration(48);
         t_remapper remap_t = new t_remapper(this.defaultDensityImpl, t);
         // the Gauss-Chebyshev quadratures integrate over [-1,1],
         // hence the remapping (and the Jacobian term t/2)
         double P = 1.0 - integral.value(remap_t.value) * t / 2.0;
         return Math.Max(P, 0.0);
      }
      protected internal override double defaultDensityImpl(double t)
      {
         throw new NotImplementedException();
      }
      protected internal override double hazardRateImpl(double t)
      {
         throw new NotImplementedException();
      }
      public override Date maxDate() { return null; }
   }
}
