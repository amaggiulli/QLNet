using System;
using System.Collections.Generic;
using System.Diagnostics;
using QLNet;

namespace QLNet.Benchmarks
{
    public class SimplePerformanceTest
    {
        public static void Run(string[] args)
        {
            Console.WriteLine("Simple Performance Test - Callable Bond YieldToCalls");
            Console.WriteLine("=".PadRight(60, '='));

            // Set evaluation date
            Date today = new Date(15, Month.January, 2024);
            Settings.setEvaluationDate(today);
            Date settlementDate = today;

            // Create a 30-year semiannual callable bond with quarterly call schedule
            Date issueDate = new Date(15, Month.January, 2024);
            Date maturityDate = new Date(15, Month.January, 2054);

            Calendar calendar = new UnitedStates(UnitedStates.Market.GovernmentBond);
            int settlementDays = 3;
            double faceAmount = 100.0;

            Schedule schedule = new Schedule(issueDate, maturityDate,
                new Period(Frequency.Semiannual), calendar,
                BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                DateGeneration.Rule.Backward, false);

            List<double> coupons = new List<double> { 0.05 };
            DayCounter dayCounter = new ActualActual(ActualActual.Convention.Bond);

            CallabilitySchedule callSchedule = new CallabilitySchedule();
            Date callStartDate = new Date(15, Month.January, 2025);
            Date callDate = callStartDate;

            while (callDate <= maturityDate)
            {
                double yearsFromStart = (callDate - callStartDate) / 365.25;
                double callPrice = Math.Max(100.0, 105.0 - (yearsFromStart * 0.2));

                Bond.Price callabilityPrice = new Bond.Price(callPrice, Bond.Price.Type.Clean);
                callSchedule.Add(new Callability(callabilityPrice, Callability.Type.Call, callDate));

                callDate = calendar.advance(callDate, new Period(3, TimeUnit.Months));
            }

            CallableFixedRateBond callableBond = new CallableFixedRateBond(
                settlementDays, faceAmount, schedule, coupons,
                dayCounter, BusinessDayConvention.Unadjusted,
                100.0, issueDate, callSchedule);

            double price = 108.50;
            int iterations = 1000;

            Console.WriteLine($"Bond with {callSchedule.Count} call dates");
            Console.WriteLine($"Iterations: {iterations}");
            Console.WriteLine($"Total calculations: {callSchedule.Count * iterations}");
            Console.WriteLine();

            // Warmup
            Console.WriteLine("Warming up...");
            for (int i = 0; i < 5; i++)
            {
                callableBond.yieldToCalls(settlementDate, price, Frequency.Semiannual, 1.0e-8);
                callableBond.yieldToCallsOptimized(settlementDate, price, Frequency.Semiannual, 1.0e-8);
            }
            Console.WriteLine("Warmup complete.");
            Console.WriteLine();

            // Test Original Method
            Console.WriteLine("Testing ORIGINAL method (creates Bond objects)...");
            Stopwatch sw1 = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                var results = callableBond.yieldToCalls(settlementDate, price, Frequency.Semiannual, 1.0e-8);
            }
            sw1.Stop();
            double originalMs = sw1.Elapsed.TotalMilliseconds;
            Console.WriteLine($"Time: {originalMs:F2} ms");
            Console.WriteLine();

            // Test Optimized Method
            Console.WriteLine("Testing OPTIMIZED method (no Bond creation)...");
            Stopwatch sw2 = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++)
            {
                var results = callableBond.yieldToCallsOptimized(settlementDate, price, Frequency.Semiannual, 1.0e-8);
            }
            sw2.Stop();
            double optimizedMs = sw2.Elapsed.TotalMilliseconds;
            Console.WriteLine($"Time: {optimizedMs:F2} ms");
            Console.WriteLine();

            // Results
            Console.WriteLine("=".PadRight(60, '='));
            Console.WriteLine("RESULTS:");
            Console.WriteLine($"Original:  {originalMs:F2} ms");
            Console.WriteLine($"Optimized: {optimizedMs:F2} ms");
            Console.WriteLine($"Speedup:   {(originalMs / optimizedMs):F2}x");
            Console.WriteLine($"Time saved: {(originalMs - optimizedMs):F2} ms ({((originalMs - optimizedMs) / originalMs * 100):F1}%)");
            Console.WriteLine("=".PadRight(60, '='));
        }
    }
}
