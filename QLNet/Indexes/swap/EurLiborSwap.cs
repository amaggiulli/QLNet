/*
 Copyright (C) 2008, 2009 , 2010 Andrea Maggiulli (a.maggiulli@gmail.com)

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
   //! %EurLiborSwapIsdaFixA index base class
   /*! %EUR %Libor %Swap indexes fixed by ISDA in cooperation with
       Reuters and Intercapital Brokers at 10am London.
       Annual 30/360 vs 6M Libor, 1Y vs 3M Libor.
       Reuters page ISDAFIX2 or EURSFIXLA=.

       Further info can be found at <http://www.isda.org/fix/isdafix.html> or
       Reuters page ISDAFIX.

   */
   public class EurLiborSwapIsdaFixA : SwapIndex 
   {
      public EurLiborSwapIsdaFixA(Period tenor)
         :this(tenor,new Handle<YieldTermStructure>() ) {}

      public EurLiborSwapIsdaFixA(Period tenor,Handle<YieldTermStructure> h)
         : base("EurLiborSwapIsdaFixA", // familyName
                tenor,
                2, // settlementDays
                new EURCurrency(),
                new TARGET(),
                new Period(1,TimeUnit.Years), // fixedLegTenor
                BusinessDayConvention.ModifiedFollowing, // fixedLegConvention
                new Thirty360(Thirty360.Thirty360Convention.BondBasis), // fixedLegDaycounter
                tenor > new Period(1,TimeUnit.Years) ?
                    new EURLibor(new Period(6,TimeUnit.Months), h) :
                    new EURLibor(new Period(3,TimeUnit.Months), h)) {}

      public EurLiborSwapIsdaFixA(Period tenor,
                                  Handle<YieldTermStructure> forwarding,
                                  Handle<YieldTermStructure> discounting)
         : base("EurLiborSwapIsdaFixA", // familyName
                tenor,
                2, // settlementDays
                new EURCurrency(),
                new TARGET(),
                new Period(1,TimeUnit.Years), // fixedLegTenor
                BusinessDayConvention.ModifiedFollowing, // fixedLegConvention
                new Thirty360(Thirty360.Thirty360Convention.BondBasis), // fixedLegDaycounter
                tenor > new Period(1,TimeUnit.Years) ?
                    new EURLibor(new Period(6,TimeUnit.Months), forwarding) :
                    new EURLibor(new Period(3,TimeUnit.Months), forwarding),
                discounting) {}
   }

   //! %EurLiborSwapIsdaFixB index base class
   /*! %EUR %Libor %Swap indexes fixed by ISDA in cooperation with
       Reuters and Intercapital Brokers at 11am London.
       Annual 30/360 vs 6M Libor, 1Y vs 3M Libor.
       Reuters page ISDAFIX2 or EURSFIXLB=.

       Further info can be found at <http://www.isda.org/fix/isdafix.html> or
       Reuters page ISDAFIX.

   */
   public class EurLiborSwapIsdaFixB : SwapIndex 
   {
      public EurLiborSwapIsdaFixB(Period tenor)
         :this(tenor,new Handle<YieldTermStructure>() ) {}

      public EurLiborSwapIsdaFixB(Period tenor,Handle<YieldTermStructure> h)
         : base("EurLiborSwapIsdaFixB", // familyName
                tenor,
                2, // settlementDays
                new EURCurrency(),
                new TARGET(),
                new Period(1,TimeUnit.Years), // fixedLegTenor
                BusinessDayConvention.ModifiedFollowing, // fixedLegConvention
                new Thirty360(Thirty360.Thirty360Convention.BondBasis), // fixedLegDaycounter
                tenor > new Period(1,TimeUnit.Years) ?
                    new EURLibor(new Period(6,TimeUnit.Months), h) :
                    new EURLibor(new Period(3,TimeUnit.Months), h)) {}

      public EurLiborSwapIsdaFixB(Period tenor,
                                  Handle<YieldTermStructure> forwarding,
                                  Handle<YieldTermStructure> discounting)
         : base("EurLiborSwapIsdaFixB", // familyName
                tenor,
                2, // settlementDays
                new EURCurrency(),
                new TARGET(),
                new Period(1,TimeUnit.Years), // fixedLegTenor
                BusinessDayConvention.ModifiedFollowing, // fixedLegConvention
                new Thirty360(Thirty360.Thirty360Convention.BondBasis), // fixedLegDaycounter
                tenor > new Period(1,TimeUnit.Years) ?
                    new EURLibor(new Period(6,TimeUnit.Months), forwarding) :
                    new EURLibor(new Period(3,TimeUnit.Months), forwarding),
                discounting) {}
   }

   //! %EurLiborSwapIfrFix index base class
   /*! %EUR %Libor %Swap indexes published by IFR Markets and
       distributed by Reuters page TGM42281 and by Telerate.
       Annual 30/360 vs 6M Libor, 1Y vs 3M Libor.
       For more info see <http://www.ifrmarkets.com>.

   */
   public class EurLiborSwapIfrFix : SwapIndex 
   {
      public EurLiborSwapIfrFix(Period tenor)
         :this(tenor,new Handle<YieldTermStructure>() ) {}

      public EurLiborSwapIfrFix(Period tenor,Handle<YieldTermStructure> h)
         : base("EurLiborSwapIfrFix", // familyName
                tenor,
                2, // settlementDays
                new EURCurrency(),
                new TARGET(),
                new Period(1,TimeUnit.Years), // fixedLegTenor
                BusinessDayConvention.ModifiedFollowing, // fixedLegConvention
                new Thirty360(Thirty360.Thirty360Convention.BondBasis), // fixedLegDaycounter
                tenor > new Period(1,TimeUnit.Years) ?
                    new EURLibor(new Period(6,TimeUnit.Months), h) :
                    new EURLibor(new Period(3,TimeUnit.Months), h)) {}

     
      public EurLiborSwapIfrFix(Period tenor,
                               Handle<YieldTermStructure> forwarding,
                               Handle<YieldTermStructure> discounting)
         : base("EurLiborSwapIfrFix", // familyName
                tenor,
                2, // settlementDays
                new EURCurrency(),
                new TARGET(),
                new Period(1, TimeUnit.Years), // fixedLegTenor
                BusinessDayConvention.ModifiedFollowing, // fixedLegConvention
                new Thirty360(Thirty360.Thirty360Convention.BondBasis), // fixedLegDaycounter
                tenor > new Period(1, TimeUnit.Years) ?
                    new EURLibor(new Period(6, TimeUnit.Months), forwarding) :
                    new EURLibor(new Period(3, TimeUnit.Months), forwarding),
                discounting) {}

    }
}
