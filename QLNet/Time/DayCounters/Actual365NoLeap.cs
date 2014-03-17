/*
 Copyright (C) 2008-2014  Andrea Maggiulli (a.maggiulli@gmail.com)
  
 This file is part of QLNet Project http://qlnet.sourceforge.net/

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
	//! Actual/365 (No Leap) day count convention
   /*! "Actual/365 (No Leap)" day count convention, also known as
      "Act/365 (NL)", "NL/365", or "Actual/365 (JGB)".

      \ingroup daycounters
   */
   public class Actual365NoLeap : DayCounter 
	{
		public Actual365NoLeap() : base(Impl.Singleton) { }
   
		 class Impl : DayCounter
        {
            public static readonly Impl Singleton = new Impl();
				private static int[] MonthOffset = { 0,  31,  59,  90, 120, 151,  // Jan - Jun
                                                 181, 212, 243, 273, 304, 334   // Jun - Dec 
															  };
            private Impl() { }

            public override string name() { return "Actual/365 (NL)"; }
            public override int dayCount(Date d1, Date d2) 
				{ 
                
                int s1, s2;

                s1 = d1.Day + MonthOffset[d1.month()-1] + (d1.year() * 365);
                s2 = d2.Day + MonthOffset[d2.month()-1] + (d2.year() * 365);

                if (d1.month() == (int)Month.Feb && d1.Day == 29)
                {
                    --s1;
                }

                if (d2.month() == (int)Month.Feb && d2.Day == 29)
                {
                    --s2;
                }

                return s2 - s1;
				}
            public override double yearFraction(Date d1, Date d2, Date refPeriodStart, Date refPeriodEnd)
            {
                return dayCount(d1, d2)/365.0;
            }
		 }
	}
}
