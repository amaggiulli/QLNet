/*
 Copyright (C) 2008, 2009 , 2010  Andrea Maggiulli (a.maggiulli@gmail.com)
  
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
   public class UsdLiborSwapIsdaFixAm : SwapIndex
   {
      public UsdLiborSwapIsdaFixAm(Period tenor)
         : this(tenor, new Handle<YieldTermStructure>()) { }

      public UsdLiborSwapIsdaFixAm(Period tenor, Handle<YieldTermStructure> h)
         : base("UsdLiborSwapIsdaFixAm", // familyName
                tenor,
                2, // settlementDays
                new USDCurrency(),
                new TARGET(),
                new Period(6, TimeUnit.Months), // fixedLegTenor
                BusinessDayConvention.ModifiedFollowing, // fixedLegConvention
                new Thirty360(Thirty360.Thirty360Convention.BondBasis), // fixedLegDaycounter
                new USDLibor(new Period(3, TimeUnit.Months), h)) { }
   }

   public class UsdLiborSwapIsdaFixPm : SwapIndex
   {
      public UsdLiborSwapIsdaFixPm(Period tenor)
         : this(tenor, new Handle<YieldTermStructure>()) { }

      public UsdLiborSwapIsdaFixPm(Period tenor, Handle<YieldTermStructure> h)
         : base("UsdLiborSwapIsdaFixPm", // familyName
                tenor,
                2, // settlementDays
                new USDCurrency(),
                new TARGET(),
                new Period(6, TimeUnit.Months), // fixedLegTenor
                BusinessDayConvention.ModifiedFollowing, // fixedLegConvention
                new Thirty360(Thirty360.Thirty360Convention.BondBasis), // fixedLegDaycounter
                new USDLibor(new Period(3, TimeUnit.Months), h)) { }
   }
}
