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
   //! UK Retail Price Inflation Index
   public class UKRPI : ZeroInflationIndex
   {
      public UKRPI(bool interpolated)
         : this(interpolated,new Handle<ZeroInflationTermStructure>()) { }

      public UKRPI(bool interpolated,Handle<ZeroInflationTermStructure> ts)
        : base("RPI",new UKRegion(),false,interpolated,Frequency.Monthly,
               new Period(1, TimeUnit.Months),new GBPCurrency(),ts) {}
   }

   //! Genuine year-on-year UK RPI (i.e. not a ratio of UK RPI)
   public class YYUKRPI : YoYInflationIndex 
   {
      public YYUKRPI(bool interpolated)
         : this(interpolated,new Handle<YoYInflationTermStructure>()) { }

      public YYUKRPI(bool interpolated,Handle<YoYInflationTermStructure> ts)
        : base("YY_RPI",new UKRegion(),false,interpolated,false,Frequency.Monthly,
               new Period(1, TimeUnit.Months),new GBPCurrency(), ts) {}
   }

   //! Fake year-on-year UK RPI (i.e. a ratio of UK RPI)
   public class YYUKRPIr : YoYInflationIndex 
   {
      public YYUKRPIr(bool interpolated)
         : this(interpolated,new Handle<YoYInflationTermStructure>()) { }

      public YYUKRPIr(bool interpolated,Handle<YoYInflationTermStructure> ts)
        : base("YYR_RPI",new UKRegion(),false,interpolated,true,Frequency.Monthly,
               new Period(1, TimeUnit.Months),new GBPCurrency(),ts) {}
    };
}
