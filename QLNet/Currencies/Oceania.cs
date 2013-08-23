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

	//! Australian dollar
//    ! The ISO three-letter code is AUD; the numeric code is 36.
//        It is divided into 100 cents.
//
//        \ingroup currencies
//    
	public class AUDCurrency : Currency
	{
        public AUDCurrency() : base("Australian dollar", "AUD", 36, "A$", "", 100, new Rounding(), "%3% %1$.2f") { }
	}

	//! New Zealand dollar
//    ! The ISO three-letter code is NZD; the numeric code is 554.
//        It is divided in 100 cents.
//
//        \ingroup currencies
//    
	public class NZDCurrency : Currency
	{
        public NZDCurrency() : base("New Zealand dollar", "NZD", 554, "NZ$", "", 100, new Rounding(), "%3% %1$.2f") { }
	}

}
