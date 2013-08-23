/*
 Copyright (C) 2008, 2009 , 2010, 2011, 2012  Andrea Maggiulli (a.maggiulli@gmail.com)
  
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
   public class PSACurve
   {

      public PSACurve(Date startdate)
         :this(startdate, 1 ) {}

      public PSACurve(Date startdate, double multiplier)
      {
         _startDate = startdate;
         _multi = multiplier;
      }

      public double getCPR(Date valDate)
      {
         Thirty360 dayCounter = new Thirty360();
         int d = dayCounter.dayCount(_startDate,valDate)/30 + 1;

         return (d <= 30 ? 0.06 * (d / 30d) : 0.06) * _multi;
      }

      public double getSMM(Date valDate)
      {
         return 1 - Math.Pow((1 - getCPR(valDate)), (1 / 12d));
      }

      private Date _startDate;
      private double _multi;
   }
}
