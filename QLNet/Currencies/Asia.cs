/*
 Copyright (C) 2008 Andrea Maggiulli
  
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

namespace QLNet
{
    /// <summary>
    /// Japanese yen
    /// The ISO three-letter code is JPY; the numeric code is 392.
    /// It is divided into 100 sen.
    /// </summary>
    public class JPYCurrency : Currency
    {
        public JPYCurrency() : base("Japanese yen", "JPY", 392, "\xA5", "", 100, new Rounding(), "%3% %1$.0f") { }
    }
}
