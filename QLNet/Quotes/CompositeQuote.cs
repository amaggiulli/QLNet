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
   public class CompositeQuote : Quote
   {
      //! market element whose value depends on two other market element
      /*! \test the correctness of the returned values is tested by
                checking them against numerical calculations.
      */
      private Handle<Quote> element1_;
      private Handle<Quote> element2_;
      private Func<double, double, double> f_;

      public CompositeQuote(Handle<Quote> element1, Handle<Quote> element2, Func<double, double, double> f)
      {
         element1_ = element1;
         element2_ = element2;
         f_ = f;

         element1_.registerWith(this.update);
         element2_.registerWith(this.update);
      }

      //! \name inspectors
      //@{
      double value1() { return element1_.link.value(); }
      double value2() { return element2_.link.value(); }
      //@}

      public void update()
      {
         notifyObservers();
      }

      //! Quote interface
      public override double value()
      {
         if (!isValid()) throw new ArgumentException("invalid DerivedQuote");
         return f_(element1_.link.value(), element2_.link.value());
      }

      public override bool isValid()
      {
         return (element1_.link.isValid() && element2_.link.isValid());
      }

   }
}
