﻿/*
 Copyright (C) 2009 Philippe Real (ph_real@hotmail.com)

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

using System.Collections.Generic;

namespace QLNet
{
   public class CapletVarianceCurve : OptionletVolatilityStructure
   {
      private BlackVarianceCurve blackCurve_;

      public CapletVarianceCurve(Date referenceDate,
                           List<Date> dates,
                           List<double> capletVolCurve,
                           DayCounter dayCounter)
          : base(referenceDate, new Calendar(), BusinessDayConvention.Following, new DayCounter())
      {
         blackCurve_ = new BlackVarianceCurve(referenceDate, dates, capletVolCurve, dayCounter, false);
      }

      public override DayCounter dayCounter()
      {
         return blackCurve_.dayCounter();
      }

      public override Date maxDate()
      {
         return blackCurve_.maxDate();
      }

      public override double minStrike()
      {
         return blackCurve_.minStrike();
      }

      public override double maxStrike()
      {
         return blackCurve_.maxStrike();
      }

      protected override SmileSection smileSectionImpl(double t)
      {
         // dummy strike
         double atmVol = blackCurve_.blackVol(t, 0.05, true);
         return new FlatSmileSection(t, atmVol, dayCounter());
      }

      protected override double volatilityImpl(double t, double r)
      {
         return blackCurve_.blackVol(t, r, true);
      }
   }
}
