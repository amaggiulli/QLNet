/*
 Copyright (C) 2008 Toyin Akin (toyin_akin@hotmail.com)
  
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

namespace QLNet {

	//! %JIBAR rate
//    ! Johannesburg Interbank Agreed Rate
//
//        \todo check settlement days and day-count convention.
//    
	public class Jibar : IborIndex
	{
        public Jibar(Period tenor)
            : base("Jibar", tenor, 0, new ZARCurrency(), new SouthAfrica(), BusinessDayConvention.ModifiedFollowing, false, new Actual365Fixed(), new Handle<YieldTermStructure>())
        {
        }
        public Jibar(Period tenor, Handle<YieldTermStructure> h)
            : base("Jibar", tenor, 0, new ZARCurrency(), new SouthAfrica(), BusinessDayConvention.ModifiedFollowing, false, new Actual365Fixed(), h)
		{
		}
	}

}
