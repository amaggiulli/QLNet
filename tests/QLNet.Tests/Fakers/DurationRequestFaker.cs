/*
 Copyright (C) 2008-2022 Andrea Maggiulli (a.maggiulli@gmail.com)

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

using System.Collections.Generic;
using AutoBogus;

namespace QLNet.Tests.Fakers
{
   public class DurationRequestFaker
   {
      public static DurationRequest[] CreateDurationRequest(int count = 1)
      {
         var list = new List<DurationRequest>();

         for (var i = 0; i < count; i++)
         {
            list.Add(new AutoFaker<DurationRequest>()
               .RuleFor(x => x.Id, i)
               .RuleFor(x => x.Bond, FixedRateBondFaker.CreateFixedRateBond)
               .RuleFor(x => x.Yield, x => x.Random.Double(0, 100))
               .RuleFor(x => x.DayCounter, x => new Thirty360(Thirty360.Thirty360Convention.BondBasis))
               .RuleFor(x => x.Compounding, x => Compounding.Compounded)
               .RuleFor(x => x.Frequency, x => Frequency.Semiannual)
               .RuleFor(x => x.SettlementDate, x => null)
               .Generate());
         }

         return list.ToArray();
      }
   }
}
