﻿/*
 Copyright (C) 2008-2014  Andrea Maggiulli (a.maggiulli@gmail.com)
  
 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

 QLNet is free software: you can redistribute it and/or modify it
 under the terms of the QLNet license.  You should have received a
 copy of the license along with this program; if not, license is  
 available online at <https://github.com/amaggiulli/qlnetLicense.html>.
  
 QLNet is a based on QuantLib, a free-software/open-source library
 for financial quantitative analysts and developers - http://quantlib.org/
 The QuantLib license is available online at http://quantlib.org/license.shtml.
 
 This program is distributed in the hope that it will be useful, but WITHOUT
 ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 FOR A PARTICULAR PURPOSE.  See the license for more details.
*/
using System;
#if QL_DOTNET_FRAMEWORK
using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
   using Xunit;
#endif
using QLNet;

namespace TestSuite
{
#if QL_DOTNET_FRAMEWORK
   [TestClass()]
#endif
	public class T_BlackFormula
	{
#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
		public void testBachelierImpliedVol()
		{
			// Testing Bachelier implied vol...

			double forward = 1.0;
			double bpvol = 0.01;
			double tte = 10.0;
			double stdDev = bpvol*Math.Sqrt(tte);
			Option.Type optionType = Option.Type.Call;
			double discount = 0.95;

			double[] d = {-3.0, -2.0, -1.0, -0.5, 0.0, 0.5, 1.0, 2.0, 3.0};
			for(int i=0;i<d.Length;++i)
			{
				double strike = forward - d[i] * bpvol * Math.Sqrt(tte);
				double callPrem = Utils.bachelierBlackFormula(optionType, strike, forward, stdDev, discount);
				double impliedBpVol = Utils.bachelierBlackFormulaImpliedVol(optionType,strike, forward, tte, callPrem, discount);

				if (Math.Abs(bpvol-impliedBpVol)>1.0e-12)
				{
					QAssert.Fail("Failed, expected " + bpvol + " realised " + impliedBpVol );
				}
			}
			return;
		}
	}
}
