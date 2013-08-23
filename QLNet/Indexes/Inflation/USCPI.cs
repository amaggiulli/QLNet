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
   //! US CPI index
   public class USCPI : ZeroInflationIndex 
   {
      public USCPI(bool interpolated)
         :this(interpolated,new Handle<ZeroInflationTermStructure>()) {}

      public USCPI(bool interpolated,
                   Handle<ZeroInflationTermStructure> ts)
        : base("CPI",
               new USRegion(),
               false,
               interpolated,
               Frequency.Monthly,
               new Period(1, TimeUnit.Months), // availability
               new USDCurrency(),
               ts) {}
   }

   //! Genuine year-on-year US CPI (i.e. not a ratio of US CPI)
   public class YYUSCPI : YoYInflationIndex 
   {
      public YYUSCPI(bool interpolated)
         :this(interpolated,new Handle<YoYInflationTermStructure>()) {}

      public YYUSCPI(bool interpolated,
                     Handle<YoYInflationTermStructure> ts)
        : base("YY_CPI",
               new USRegion(),
               false,
               interpolated,
               false,
               Frequency.Monthly,
               new Period(1, TimeUnit.Months),
               new USDCurrency(),
               ts) {}
   }

   //! Fake year-on-year US CPI (i.e. a ratio of US CPI)
   public class YYUSCPIr : YoYInflationIndex 
   {
      public YYUSCPIr(bool interpolated)
         : this(interpolated, new Handle<YoYInflationTermStructure>()) { }

      public YYUSCPIr(bool interpolated,
                      Handle<YoYInflationTermStructure> ts)
        : base("YYR_CPI",
               new USRegion(),
               false,
               interpolated,
               true,
               Frequency.Monthly,
               new Period(1, TimeUnit.Months),
               new USDCurrency(),
               ts) {}
    }
}
