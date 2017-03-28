/*
 Copyright (C) 2008, 2009 , 2010, 2011, 2012  Andrea Maggiulli (a.maggiulli@gmail.com)
  
 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

 QLNet is free software: you can redistribute it and/or modify it
 under the terms of the QLNet license.  You should have received a
 copy of the license along with this program; if not, license is  
 available online at <https://github.com/amaggiulli/qlnetLicense.html>.
  
 QLNet is a based on QuantLib, a free-software/open-source library
 for financial quantitative analysts and developers - http://quantlib.org/
 The QuantLib license is available online at http://quantlib.org/license.shtml.
 
 This program is distributed in the hope that it will be useful, but WITHOUT
 ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 FOR A PARTICULAR PURPOSE.  See the license for more details.
*/
#if QL_DOTNET_FRAMEWORK
using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
   using Xunit;
#endif
using QLNet;

namespace TestSuite
{
#if QL_DOTNET_FRAMEWORK
   [TestClass()]
#endif
   public class T_PSACurve
   {
#if QL_DOTNET_FRAMEWORK
        [TestMethod()]
#else
       [Fact]
#endif
      public void testCashedValues()
      {

         Date startDate = new Date(01, 03, 2007);
         Period period = new Period(360, TimeUnit.Months);
         Calendar calendar = new TARGET();
         Date endDate = calendar.advance(startDate,period,BusinessDayConvention.Unadjusted);

         Schedule schedule = new Schedule( startDate, endDate, new Period(1,TimeUnit.Months), calendar,
                                           BusinessDayConvention.Unadjusted,
                                           BusinessDayConvention.Unadjusted,
                                           DateGeneration.Rule.Backward, false);

         // PSA 100%
         PSACurve psa100 = new PSACurve(startDate);
         double[] listCPR = {0.2000,0.4000,0.6000,0.8000,1.0000,1.2000,1.4000,1.6000,1.8000,2.0000,2.2000,2.4000,2.6000,2.8000,
                             3.0000,3.2000,3.4000,3.6000,3.8000,4.0000,4.2000,4.4000,4.6000,4.8000,5.0000,5.2000,5.4000,5.6000,
                             5.8000,6.0000};

         for (int i = 0; i < schedule.Count; i++)
         {
            if ( i <= 29 )
               QAssert.AreEqual(listCPR[i], psa100.getCPR(schedule[i])*100,0.001);
            else
               QAssert.AreEqual(6.0000, psa100.getCPR(schedule[i])*100);
         }


      }
   }
}
