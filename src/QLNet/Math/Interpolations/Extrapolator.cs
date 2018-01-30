/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2013 Andrea Maggiulli (a.maggiulli@gmail.com)

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

namespace QLNet
{
   //! base class for classes possibly allowing extrapolation
   // LazyObject should not be here but it is because of the InterpolatedYieldCurve
   public abstract class Extrapolator : LazyObject
   {
      private bool extrapolate_;
      public bool extrapolate { get { return extrapolate_; } set { extrapolate_ = value; } }

      // some extra functionality
      public bool allowsExtrapolation() { return extrapolate_; }      //! tells whether extrapolation is enabled

      public void enableExtrapolation(bool b = true) { extrapolate_ = b; }      //! enable extrapolation in subsequent calls

      public void disableExtrapolation(bool b = true) { extrapolate_ = !b; }    //! disable extrapolation in subsequent calls
   }
}
