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
   //! %principal payment over a fixed period
   //! This class implements part of the CashFlow interface but it is
   //  still abstract and provides derived classes with methods for accrual period calculations.
   public class Principal : CashFlow
   {
      protected double nominal_;
      protected double amount_;
      protected DayCounter dayCounter_;
      protected Date paymentDate_, accrualStartDate_, accrualEndDate_, refPeriodStart_, refPeriodEnd_;

      // access to properties
      public double nominal() { return nominal_; }
      public override Date date() { return paymentDate_; }
      public Date accrualStartDate() { return accrualStartDate_; }
      public Date accrualEndDate() { return accrualEndDate_; }
      public Date refPeriodStart { get { return refPeriodStart_; } }
      public Date refPeriodEnd { get { return refPeriodEnd_; } }
      public override double amount() { return amount_; }
      public void setAmount( double amount ) { amount_ = amount; }
      public DayCounter dayCounter() { return dayCounter_; }

      // Constructors
      public Principal() { }       // default constructor
      public Principal(double amount, 
                       double nominal, 
                       Date paymentDate, 
                       Date accrualStartDate, 
                       Date accrualEndDate, 
                       DayCounter dayCounter, 
                       Date refPeriodStart = null , 
                       Date refPeriodEnd = null )
      {
         amount_ = amount;
         nominal_ = nominal;
         paymentDate_ = paymentDate;
         accrualStartDate_ = accrualStartDate;
         accrualEndDate_ = accrualEndDate;
         refPeriodStart_ = refPeriodStart;
         refPeriodEnd_ = refPeriodEnd;
         dayCounter_ = dayCounter;
         if (refPeriodStart_ == null) refPeriodStart_ = accrualStartDate_;
         if (refPeriodEnd_ == null) refPeriodEnd_ = accrualEndDate_;
      }
   }
}
