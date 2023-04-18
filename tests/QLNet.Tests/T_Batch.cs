/*
 Copyright (C) 2008-2022 Andrea Maggiulli (a.maggiulli@gmail.com)

 This file is part of QLNet Project https://github.com/amaggiulli/qlnet

 QLNet is free software: you can redistribute it and/or modify it
 under the terms of the QLNet license.  You should have received a
 copy of the license along with this program; if not, license is
 available at <https://github.com/amaggiulli/QLNet/blob/develop/LICENSE>.

 QLNet is a based on QuantLib, a free-software/open-source library
 for financial quantitative analysts and developers - http://quantlib.org/
 The QuantLib license is available online at http://quantlib.org/license.shtml.

 This program is distributed in the hope that it will be useful, but WITHOUT
 ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 FOR A PARTICULAR PURPOSE.  See the license for more details.
*/

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using QLNet;
using QLNet.Tests.Fakers;
using Xunit;
using Xunit.Abstractions;

namespace TestSuite
{
   [Collection("QLNet CI Tests")]
   public class T_Batch
   {
      private readonly ITestOutputHelper output;

      public T_Batch(ITestOutputHelper output)
      {
         this.output = output;
      }

      [Fact]
      public async Task testBatchYieldCalculation()
      {
         const int size = 2; // increase size to benchmark yield calculation
         var request = YieldRequestFaker.CreateYieldRequest(size);

         var stopwatch = Stopwatch.StartNew();
         var response = await BondFunctions.calculateYieldsAsync(request);
         output.WriteLine($"Calculated {size} yields in {stopwatch.ElapsedMilliseconds}ms");

         Assert.NotEmpty(response);
         Assert.Equal(size, response.Length);
         Assert.All(response, item => Assert.NotEqual(0,item.Yield));
      }

      [Fact]
      public async Task testBatchWalCalculation()
      {
         const int size = 2; // increase size to benchmark wal calculation
         var request = WalRequestFaker.CreateWalRequest(size);

         var stopwatch = Stopwatch.StartNew();
         var response = await BondFunctions.calculateWalAsync(request);
         output.WriteLine($"Calculated {size} wal in {stopwatch.ElapsedMilliseconds}ms");

         Assert.NotEmpty(response);
         Assert.Equal(size, response.Length);
         Assert.All(response, item => Assert.NotEqual(DateTime.MinValue, item.Wal));
      }

      [Fact]
      public async Task testBatchDurationCalculation()
      {
         const int size = 2; // increase size to benchmark duration calculation
         var request = DurationRequestFaker.CreateDurationRequest(size);

         var stopwatch = Stopwatch.StartNew();
         var response = await BondFunctions.calculateDurationAsync(request);
         output.WriteLine($"Calculated {size} duration in {stopwatch.ElapsedMilliseconds}ms");

         Assert.NotEmpty(response);
         Assert.Equal(size, response.Length);
         Assert.All(response, item => Assert.NotEqual(0, item.Duration));
      }

      [Fact]
      public async Task testBatchAccruedDaysAndAmountCalculation()
      {
         const int size = 2; // increase size to benchmark duration calculation
         var request = AccruedRequestFaker.CreateAccruedRequest(size);

         var stopwatch = Stopwatch.StartNew();
         var response = await BondFunctions.calculateAccruedDaysAndAmountAsync(request);
         output.WriteLine($"Calculated {size} accrued days and amount in {stopwatch.ElapsedMilliseconds}ms");

         Assert.NotEmpty(response);
         Assert.Equal(size, response.Length);
      }

      [Fact]
      public async Task testBatchAllBondFunctionsCalculation()
      {
         const int size = 2; // increase size to benchmark bond functions calculation
         var request = BondFunctionRequestFaker.CreateBondFunctions(size);

         var stopwatch = Stopwatch.StartNew();
         var response = await BondFunctions.getAllBondFunctions(request);
         output.WriteLine($"Calculated {size} bond functions in {stopwatch.ElapsedMilliseconds}ms");

         Assert.NotEmpty(response);
         Assert.Equal(size, response.Length);
      }
   }
}
