﻿/*
 Copyright (C) 2014 Edem Dawui (edawui@gmail.com)
  
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
	public interface Curve<T> : ITraits<T>, InterpolatedCurve
	{
		#region ITraits

		//protected 
		ITraits<T> traits_ { get; }
		#endregion

		#region InterpolatedCurve
		#endregion

		List<BootstrapHelper<T>> instruments_ { get; }
		void setTermStructure( BootstrapHelper<T> helper );

		double accuracy_ { get; }
		bool moving_ { get; }//set; }

		void registerWith( BootstrapHelper<T> helper );
		Date initialDate();

		double timeFromReference( Date d );
		double initialValue();
	}

}