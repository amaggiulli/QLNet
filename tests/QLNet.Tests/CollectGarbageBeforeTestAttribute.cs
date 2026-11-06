//  Copyright (C) 2008-2026 Andrea Maggiulli (a.maggiulli@gmail.com)
//
//  This file is part of QLNet Project https://github.com/amaggiulli/qlnet
//  QLNet is free software: you can redistribute it and/or modify it
//  under the terms of the QLNet license.  You should have received a
//  copy of the license along with this program; if not, license is
//  available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.
//
//  QLNet is a based on QuantLib, a free-software/open-source library
//  for financial quantitative analysts and developers - http://quantlib.org/
//  The QuantLib license is available online at http://quantlib.org/license.shtml.
//
//  This program is distributed in the hope that it will be useful, but WITHOUT
//  ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
//  FOR A PARTICULAR PURPOSE.  See the license for more details.

using System;
using System.Reflection;
using Xunit.Sdk;

[assembly: TestSuite.CollectGarbageBeforeTest]

namespace TestSuite
{
   /// <summary>
   /// Forces a full garbage collection before every test.
   /// </summary>
   /// <remarks>
   /// Observables (Settings above all) hold their observers through weak references, so the objects
   /// created by a test keep receiving notifications until the GC actually collects them. In QuantLib
   /// they are destroyed, and unregistered, as soon as the test ends. Here, if no full collection happens
   /// between tests, the dead object graph of a previous test stays registered: for instance the
   /// RelativeDateRateHelpers built by T_BasisSwapRateHelpers rebuild their swaps on every evaluation
   /// date change, so each Settings.setEvaluationDate() of a later test (T_CliquetOption calls it about
   /// two thousand times) pays for them. Whether such a collection happens depends on the allocation
   /// pattern of the tests in between, which made the CI build hang randomly for tens of minutes.
   /// The collection runs before the test (not after) because the instance of the previous test
   /// class has been disposed by then and is no longer reachable.
   /// </remarks>
   [AttributeUsage(AttributeTargets.Assembly)]
   public sealed class CollectGarbageBeforeTestAttribute : BeforeAfterTestAttribute
   {
      public override void Before(MethodInfo methodUnderTest)
      {
         GC.Collect();
         GC.WaitForPendingFinalizers();
         GC.Collect();
      }
   }
}
