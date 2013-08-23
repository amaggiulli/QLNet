/*
 Copyright (C) 2008,2009 Andrea Maggiulli 
  
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
   //! Quote adapter for the last fixing available of a given Index
   class LastFixingQuote : Quote, IObserver
   {
      protected Index index_;

      public LastFixingQuote(Index index) 
      {
         index_ = index;
         index_.registerWith(update);
      }

      //! Quote interface
      public override double value()
      {
         if (!isValid()) throw new ArgumentException(index_.name() + " has no fixing");
         return index_.fixing(referenceDate());
      }

      public override bool isValid()
      {
        return index_.timeSeries().value().Count() > 0;
      }

      public Date referenceDate()  
      {
         return index_.timeSeries().value().Keys.Last(); // must be tested
      }

      public void update()
      {
         notifyObservers();
      }

   }
}
