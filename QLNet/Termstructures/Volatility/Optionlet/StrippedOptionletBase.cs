//  Copyright (C) 2008-2016 Andrea Maggiulli (a.maggiulli@gmail.com)
//  
//  This file is part of QLNet Project https://github.com/amaggiulli/qlnet
//  QLNet is free software: you can redistribute it and/or modify it
//  under the terms of the QLNet license.  You should have received a
//  copy of the license along with this program; if not, license is  
//  available online at <http://qlnet.sourceforge.net/License.html>.
//   
//  QLNet is a based on QuantLib, a free-software/open-source library
//  for financial quantitative analysts and developers - http://quantlib.org/
//  The QuantLib license is available online at http://quantlib.org/license.shtml.
//  
//  This program is distributed in the hope that it will be useful, but WITHOUT
//  ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
//  FOR A PARTICULAR PURPOSE.  See the license for more details.

using System.Collections.Generic;

namespace QLNet
{
   /*! Abstract base class interface for a (double indexed) vector of (strike
       indexed) optionlet (i.e. caplet/floorlet) volatilities.
   */
   public abstract class StrippedOptionletBase : LazyObject
   {
      public abstract List<double> optionletStrikes(int i) ;
      public abstract List<double> optionletVolatilities(int i) ;

      public abstract List<Date> optionletFixingDates() ;
      public abstract List<double> optionletFixingTimes() ;
      public abstract int optionletMaturities() ;

      public abstract List<double> atmOptionletRates() ;

      public abstract DayCounter dayCounter() ;
      public abstract Calendar calendar() ;
      public abstract int settlementDays() ;
      public abstract BusinessDayConvention businessDayConvention() ;
   }
}
