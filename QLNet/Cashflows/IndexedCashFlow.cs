/*
 Copyright (C) 2008, 2009 , 2010  Andrea Maggiulli (a.maggiulli@gmail.com)
 * 
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
   //! Cash flow dependent on an index ratio.

   /*! This cash flow is not a coupon, i.e., there's no accrual.  The
       amount is either i(T)/i(0) or i(T)/i(0) - 1, depending on the
       growthOnly parameter.

       We expect this to be used inside an instrument that does all the date
       adjustment etc., so this takes just dates and does not change them.
       growthOnly = false means i(T)/i(0), which is a bond-type setting.
       growthOnly = true means i(T)/i(0) - 1, which is a swap-type setting.
   */
   public class IndexedCashFlow : CashFlow
   {
      public IndexedCashFlow(double notional,
                             Index index,
                             Date baseDate,
                             Date fixingDate,
                             Date paymentDate,
                             bool growthOnly = false)
      {
         notional_ = notional;
         index_=index;
         baseDate_ = baseDate;
         fixingDate_ = fixingDate;
         paymentDate_= paymentDate;
         growthOnly_ = growthOnly;
      }

      public override Date date() { return paymentDate_; }
      public virtual double notional()  { return notional_; }
      public virtual Date baseDate()  { return baseDate_; }
      public virtual Date fixingDate() { return fixingDate_; }
      public virtual Index index() { return index_; }
      public virtual bool growthOnly() { return growthOnly_; }

      public override double amount() 
      {
        double I0 = index_.fixing(baseDate_);
        double I1 = index_.fixing(fixingDate_);

        if (growthOnly_)
            return notional_ * (I1 / I0 - 1.0);
        else
            return notional_ * (I1 / I0);
    
      }
      private double notional_;
      private Index index_;
      private Date baseDate_, fixingDate_, paymentDate_;
      private bool growthOnly_;
   }
}
