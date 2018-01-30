/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
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

namespace QLNet
{
   //! Business/252 day count convention
   public class Business252 : DayCounter
   {
      public Business252(Calendar c = null)
      {
         calendar_ = c ?? new Brazil();
         dayCounter_ = this;
      }

      public override string name() { return "Business/252(" + calendar_.name() + ")"; }

      public override int dayCount(Date d1, Date d2) { return calendar_.businessDaysBetween(d1, d2); }

      public override double yearFraction(Date d1, Date d2, Date d3, Date d4) { return dayCount(d1, d2) / 252.0; }

      private Calendar calendar_;
   }
}
