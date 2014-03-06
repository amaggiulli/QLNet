/*
 Copyright (C) 2008-2013  Andrea Maggiulli (a.maggiulli@gmail.com)

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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QLNet;

namespace TestSuite
{
   [TestClass()]
   public class T_DefaultProbabilityCurves
   {
      [TestMethod()]
      public void testDefaultProbability() 
      {
         // Testing default-probability structure...

         double hazardRate = 0.0100;
         Handle<Quote> hazardRateQuote = new Handle<Quote>(new SimpleQuote(hazardRate));
         DayCounter dayCounter = new Actual360();
         Calendar calendar = new TARGET();
         int n = 20;

         double tolerance = 1.0e-10;
         Date today = Settings.evaluationDate();
         Date startDate = today;
         Date endDate = startDate;

         FlatHazardRate flatHazardRate = new FlatHazardRate(startDate, hazardRateQuote, dayCounter);

         for(int i=0; i<n; i++)
         {
            startDate = endDate;
            endDate = calendar.advance(endDate, 1, TimeUnit.Years);

            double pStart = flatHazardRate.defaultProbability(startDate);
            double pEnd = flatHazardRate.defaultProbability(endDate);

            double pBetweenComputed =
               flatHazardRate.defaultProbability(startDate, endDate);

            double pBetween = pEnd - pStart;

            if (Math.Abs(pBetween - pBetweenComputed) > tolerance)
               Assert.Fail( "Failed to reproduce probability(d1, d2) "
                            + "for default probability structure\n"
                            + "    calculated probability: " + pBetweenComputed + "\n"
                            + "    expected probability:   " + pBetween);

            double t2 = dayCounter.yearFraction(today, endDate);
            double timeProbability = flatHazardRate.defaultProbability(t2);
            double dateProbability =
               flatHazardRate.defaultProbability(endDate);

            if (Math.Abs(timeProbability - dateProbability) > tolerance)
               Assert.Fail( "single-time probability and single-date probability do not match\n"
                           + "    time probability: " + timeProbability + "\n"
                           + "    date probability: " + dateProbability);

            double t1 = dayCounter.yearFraction(today, startDate);
            timeProbability = flatHazardRate.defaultProbability(t1, t2);
            dateProbability = flatHazardRate.defaultProbability(startDate, endDate);

            if (Math.Abs(timeProbability - dateProbability) > tolerance)
               Assert.Fail( "double-time probability and double-date probability do not match\n"
                            + "    time probability: " + timeProbability + "\n"
                            + "    date probability: " + dateProbability);
      
         }
      }

      [TestMethod()]
      public void testFlatHazardRate() 
      {

         // Testing flat hazard rate...

         double hazardRate = 0.0100;
         Handle<Quote> hazardRateQuote = new Handle<Quote>(new SimpleQuote(hazardRate));
         DayCounter dayCounter = new Actual360();
         Calendar calendar = new TARGET();
         int n = 20;

         double tolerance = 1.0e-10;
         Date today = Settings.evaluationDate();
         Date startDate = today;
         Date endDate = startDate;

         FlatHazardRate flatHazardRate = new FlatHazardRate(today, hazardRateQuote, dayCounter);

         for(int i=0; i<n; i++)
         {
            endDate = calendar.advance(endDate, 1, TimeUnit.Years);
            double t = dayCounter.yearFraction(startDate, endDate);
            double probability = 1.0 - Math.Exp(-hazardRate * t);
            double computedProbability = flatHazardRate.defaultProbability(t);

            if (Math.Abs(probability - computedProbability) > tolerance)
               Assert.Fail( "Failed to reproduce probability for flat hazard rate\n"
                            + "    calculated probability: " + computedProbability + "\n"
                            + "    expected probability:   " + probability);
         }
      }

      [TestMethod()]
      public void testFlatHazardConsistency() 
      {
         // Testing piecewise-flat hazard-rate consistency...
         //testBootstrapFromSpread<HazardRate,BackwardFlat>();
         //testBootstrapFromUpfront<HazardRate,BackwardFlat>();
      }
   }
}
