/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
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
   //! Predetermined cash flow
   /*! This cash flow pays a predetermined amount at a given date. */ 
   public class SimpleCashFlow : CashFlow
   {
      private double amount_;
      public override double amount() { return amount_; }
      private Date date_;
      public override Date date() { return date_; }

      public SimpleCashFlow(double amount, Date date)
      {
         if (date == null) throw new ApplicationException("null date SimpleCashFlow");
         amount_ = amount;
         date_ = date;
      }
   }

   //! Bond redemption
    /*! This class specializes SimpleCashFlow so that visitors
        can perform more detailed cash-flow analysis.
    */
    public class Redemption : SimpleCashFlow 
    {
       public Redemption(double amount, Date date) : base(amount, date) { }
    }

    //! Amortizing payment
    /*! This class specializes SimpleCashFlow so that visitors
        can perform more detailed cash-flow analysis.
    */
    public class AmortizingPayment : SimpleCashFlow 
    {
       public AmortizingPayment(double amount, Date date) : base(amount, date) { }
    }

    //! Voluntary Prepay
    /*! This class specializes SimpleCashFlow so that visitors
        can perform more detailed cash-flow analysis.
    */
    public class VoluntaryPrepay : SimpleCashFlow
    {
       public VoluntaryPrepay(double amount, Date date) : base(amount, date) { }
    }
}
