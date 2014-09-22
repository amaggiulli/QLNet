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
	//! South African CPI index
	public class ZACPI : ZeroInflationIndex
	{
		public ZACPI( bool interpolated )
			: this( interpolated, new Handle<ZeroInflationTermStructure>() ) { }

		public ZACPI( bool interpolated,
						 Handle<ZeroInflationTermStructure> ts )
			: base( "CPI",
					 new ZARegion(),
					 false,
					 interpolated,
					 Frequency.Monthly,
					 new Period( 1, TimeUnit.Months ), // availability
					 new ZARCurrency(),
					 ts ) { }
	}

	//! Genuine year-on-year South African CPI (i.e. not a ratio of South African CPI)
	public class YYZACPI : YoYInflationIndex
	{
		public YYZACPI( bool interpolated )
			: this( interpolated, new Handle<YoYInflationTermStructure>() ) { }

		public YYZACPI( bool interpolated,
							Handle<YoYInflationTermStructure> ts )
			: base( "YY_CPI",
					 new ZARegion(),
					 false,
					 interpolated,
					 false,
					 Frequency.Monthly,
					 new Period( 1, TimeUnit.Months ),
					 new ZARCurrency(),
					 ts ) { }
	}

	//! Fake year-on-year South African CPI (i.e. a ratio of South African CPI)
	public class YYZACPIr : YoYInflationIndex
	{
		public YYZACPIr( bool interpolated )
			: this( interpolated, new Handle<YoYInflationTermStructure>() ) { }

		public YYZACPIr( bool interpolated,
							 Handle<YoYInflationTermStructure> ts )
			: base( "YYR_CPI",
					 new ZARegion(),
					 false,
					 interpolated,
					 true,
					 Frequency.Monthly,
					 new Period( 1, TimeUnit.Months ),
					 new ZARCurrency(),
					 ts ) { }
	}
}