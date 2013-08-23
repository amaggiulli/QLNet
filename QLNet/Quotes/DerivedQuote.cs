/*
 Copyright (C) 2008-2009 Andrea Maggiulli
  
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
using System.Text;
using QLNet;

namespace QLNet
{
   public class DerivedQuote : Quote
   {
      //! market quote whose value depends on another quote
      /*! \test the correctness of the returned values is tested by
                checking them against numerical calculations.
      */
      private Handle<Quote> element_;
      private Func<double, double> f_;

      public DerivedQuote(Handle<Quote> element, Func<double, double> f)
      {
         element_ = element;
         f_ = f;

         element_.registerWith(this.update);
      }

      //! Quote interface
      public override double value()
      {
         if (!isValid()) throw new ArgumentException("invalid DerivedQuote");
         return f_(element_.link.value());
      }

      public override bool isValid() 
      { 
         return element_.link.isValid(); 
      }

      public void update() 
      {
        notifyObservers();
      }

   }
}
