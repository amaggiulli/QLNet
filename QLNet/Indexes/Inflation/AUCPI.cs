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
   //! AU CPI index (either quarterly or annual)
   public class AUCPI : ZeroInflationIndex 
   {
      public AUCPI(Frequency frequency,
                   bool revised,
                   bool interpolated)
         :this(frequency,revised,interpolated,new Handle<ZeroInflationTermStructure>()) {} 

      public AUCPI(Frequency frequency,
                   bool revised,
                   bool interpolated,
                   Handle<ZeroInflationTermStructure> ts)
        : base("CPI",
               new AustraliaRegion(),
               revised,
               interpolated,
               frequency,
               new Period(2, TimeUnit.Months),
               new AUDCurrency(),
               ts) {}

   }
    
   //! Genuine year-on-year AU CPI (i.e. not a ratio)
   public class YYAUCPI : YoYInflationIndex 
   {
      public YYAUCPI(Frequency frequency,
                     bool revised,
                     bool interpolated)
         :this(frequency,revised,interpolated,new Handle<YoYInflationTermStructure>()){}

      public YYAUCPI(Frequency frequency,
                     bool revised,
                     bool interpolated,
                     Handle<YoYInflationTermStructure> ts)
        : base("YY_CPI",
               new AustraliaRegion(),
               revised,
               interpolated,
               false,
               frequency,
               new Period(2, TimeUnit.Months),
               new AUDCurrency(),
               ts) {}
   }

 
   //! Fake year-on-year AUCPI (i.e. a ratio)
   public class YYAUCPIr : YoYInflationIndex 
   {
      public YYAUCPIr(Frequency frequency,
                      bool revised,
                      bool interpolated)
         : this(frequency, revised, interpolated, new Handle<YoYInflationTermStructure>()) { }

      public YYAUCPIr(Frequency frequency,
                      bool revised,
                      bool interpolated,
                      Handle<YoYInflationTermStructure> ts )
        : base("YYR_CPI",
               new AustraliaRegion(),
               revised,
               interpolated,
               true,
               frequency,
               new Period(2, TimeUnit.Months),
               new AUDCurrency(),
               ts) {}
   }
}
