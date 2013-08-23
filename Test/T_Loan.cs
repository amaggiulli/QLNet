/*
 Copyright (C) 2008, 2009 , 2010 Andrea Maggiulli (a.maggiulli@gmail.com)

 This file is part of QLNet Project http://www.qlnet.org

 QLNet is free software: you can redistribute it and/or modify it
 under the terms of the QLNet license.  You should have received a
 copy of the license along with this program; if not, license is  
 available online at <http://trac2.assembla.com/QLNet/wiki/License>.
  
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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QLNet;

namespace TestSuite
{
   [TestClass()]
   public class T_Loan
   {
      [TestMethod()]
      public void testFairRate()
      {
         Calendar calendar = new TARGET();

         Date settlementDate = new Date(10, Month.Mar, 2010);

         /*********************
         * LOAN TO BE PRICED *
         **********************/

         // constant nominal 1,000,000 Euro
         double nominal = 1000000.0;
         // fixed leg
         Frequency fixedLegFrequency = Frequency.Monthly;
         BusinessDayConvention fixedLegConvention = BusinessDayConvention.Unadjusted;
         BusinessDayConvention principalLegConvention = BusinessDayConvention.ModifiedFollowing;
         DayCounter fixedLegDayCounter = new Thirty360(Thirty360.Thirty360Convention.European);
         double fixedRate = 0.04;

         // Principal leg
         Frequency pricipalLegFrequency = Frequency.Annual;

         int lenghtInMonths = 3;
         Loan.Type loanType = Loan.Type.Payer;

         Date maturity = settlementDate + new Period(lenghtInMonths, TimeUnit.Years);
         Schedule fixedSchedule = new Schedule(settlementDate, maturity, new Period(fixedLegFrequency),
                                  calendar, fixedLegConvention, fixedLegConvention, DateGeneration.Rule.Forward, false);
         Schedule principalSchedule = new Schedule(settlementDate, maturity, new Period(pricipalLegFrequency),
                                  calendar, principalLegConvention, principalLegConvention, DateGeneration.Rule.Forward, false);
         Loan testLoan = new FixedLoan(loanType, nominal, fixedSchedule, fixedRate, fixedLegDayCounter,
                                     principalSchedule, principalLegConvention);


      }
   }
}
