﻿/*
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
using Bogus;

namespace QLNet.Tests.Fakers
{
   public class FixedRateBondFaker
   {
      public static FixedRateBond createFixedRateBond()
      {
         var faker = new Faker<FixedRateBond>()
            .CustomInstantiator(f => new FixedRateBond(0, 1000, ScheduleFaker.createSchedule(), new InitializedList<double>(1, f.Random.Double(0.1)),
               new Thirty360(Thirty360.Thirty360Convention.BondBasis),BusinessDayConvention.Unadjusted));

         return faker.Generate();
      }
   }
}
