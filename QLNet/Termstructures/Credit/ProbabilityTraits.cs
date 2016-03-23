/*
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

namespace QLNet
{
	public class SurvivalProbability : ITraits<YieldTermStructure>
   {
      const double avgRate = 0.05;
      public Date initialDate(YieldTermStructure c) { return c.referenceDate(); }   // start of curve data
      public double initialValue(YieldTermStructure c) { return 1; }    // value at reference date
      public bool dummyInitialValue() { return false; }   // true if the initialValue is just a dummy value
      public double initialGuess() { return 1.0 / (1.0 + avgRate * 0.25); }   // initial guess
      public double guess(YieldTermStructure c, Date d) { return c.discount(d, true); }  // further guesses
      // possible constraints based on previous values
      public double minValueAfter(int s, List<double> l)
      {
         // replace with Epsilon
         return Const.QL_EPSILON;
      }
      public double maxValueAfter(int i, List<double> data)
      {
         // discount are not required to be decreasing--all bets are off.
         // We choose as max a value very unlikely to be exceeded.
         return 3.0;

         // discounts cannot increase
         //return data[i - 1]; 
      }
      // update with new guess
      public void updateGuess(List<double> data, double discount, int i) { data[i] = discount; }
      public int maxIterations() { return 50; }   // upper bound for convergence loop

      public double discountImpl(Interpolation i, double t) { return i.value(t, true); }
      public double zeroYieldImpl(Interpolation i, double t) { throw new NotSupportedException(); }
      public double forwardImpl(Interpolation i, double t) { throw new NotSupportedException(); }
		
      public double guess(int i, InterpolatedCurve c, bool validData, int f)
      { throw new NotSupportedException(); }

      public double minValueAfter(int i, InterpolatedCurve c, bool validData, int f)
      {throw new NotSupportedException(); }

      public double maxValueAfter(int i, InterpolatedCurve c, bool validData, int f)
      {throw new NotSupportedException(); }
  
   }

}
