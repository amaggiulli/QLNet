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
namespace QLNet
{
   public class DurationRequest
   {
      public int Id { get; set; }
      public Bond Bond { get; set; }
      public double Yield { get; set; }
      public DayCounter DayCounter { get; set; }
      public Compounding Compounding { get; set; }
      public Frequency Frequency { get; set; }
      public Duration.Type Type { get; set; }
      public Date SettlementDate { get; set; }
   }
}
