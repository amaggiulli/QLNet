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
namespace QLNet
{
   public class Thirty365 : DayCounter
   {
      public Thirty365()
         : base(new Impl())
      { }

      private class Impl : DayCounterImpl
      {
         public override string name()
         {
            return "30/365";
         }

         public override int dayCount(Date d1, Date d2)
         {
            int dd1 = d1.Day, dd2 = d2.Day;
            int mm1 = d1.month(), mm2 = d2.month();
            int yy1 = d1.year(), yy2 = d2.year();

            return 360*(yy2-yy1) + 30*(mm2-mm1) + (dd2-dd1);
         }

         public override double yearFraction(Date d1, Date d2, Date refPeriodStart, Date refPeriodEnd)
         {
            return dayCount(d1,d2)/365.0;
         }
      }
   }
}
