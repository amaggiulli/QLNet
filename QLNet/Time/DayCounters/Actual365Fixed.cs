/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
  
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

namespace QLNet
{
    /* "Actual/365 (Fixed)" day count convention, also know as "Act/365 (Fixed)", "A/365 (Fixed)", or "A/365F".
	   According to ISDA, "Actual/365" (without "Fixed") is an alias for "Actual/Actual (ISDA)" (see ActualActual.)
     * If Actual/365 is not explicitly specified as fixed in an instrument specification, 
     * you might want to double-check its meaning.   */
    public class Actual365Fixed : DayCounter
    {
        public Actual365Fixed() : base(Impl.Singleton) { }

        class Impl : DayCounter
        {
            public static readonly Impl Singleton = new Impl();
            private Impl() { }

            public override string name() { return "Actual/365 (Fixed)"; }
            public override int dayCount(Date d1, Date d2) { return (d2 - d1); }
            public override double yearFraction(Date d1, Date d2, Date refPeriodStart, Date refPeriodEnd)
            {
                return dayCount(d1, d2) / 365.0;
            }

        }
    }
}