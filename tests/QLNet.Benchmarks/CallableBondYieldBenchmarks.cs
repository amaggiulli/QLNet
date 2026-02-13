using BenchmarkDotNet.Attributes;

namespace QLNet.Benchmarks
{
    [Config(typeof(BenchmarkConfig))]
    public class CallableBondYieldBenchmarks
    {
        private CallableFixedRateBond callableBond = null!;
        private Date settlementDate = null!;
        private double price;
        private const int Iterations = 100; // Calculate yields 100 times to amplify the difference

        [GlobalSetup]
        public void Setup()
        {
            // Set evaluation date
            var today = new Date(15, Month.January, 2024);
            Settings.setEvaluationDate(today);
            settlementDate = today;

            // Create a 30-year semiannual callable bond with quarterly call schedule
            var issueDate = new Date(15, Month.January, 2024);
            var maturityDate = new Date(15, Month.January, 2054); // 30 years

            Calendar calendar = new UnitedStates(UnitedStates.Market.GovernmentBond);
            var settlementDays = 3;
            var faceAmount = 100.0;

            // Semiannual coupon schedule
            var schedule = new Schedule(issueDate, maturityDate,
                new Period(Frequency.Semiannual), calendar,
                BusinessDayConvention.Unadjusted, BusinessDayConvention.Unadjusted,
                DateGeneration.Rule.Backward, false);

            // 5% coupon rate
            var coupons = new List<double> { 0.05 };
            DayCounter dayCounter = new ActualActual(ActualActual.Convention.Bond);

            // Create callable schedule - quarterly calls starting after 1 year
            var callSchedule = new CallabilitySchedule();
            var callStartDate = new Date(15, Month.January, 2025); // Callable after 1 year

            // Add quarterly call dates from year 1 to year 30
            // This will create approximately 116 call dates
            var callDate = callStartDate;
            while (callDate <= maturityDate)
            {
                // Call price starts at 105 and decreases to 100 over time
                var yearsFromStart = (callDate - callStartDate) / 365.25;
                var callPrice = Math.Max(100.0, 105.0 - (yearsFromStart * 0.2)); // Decreases 0.2 per year

                var callabilityPrice = new Bond.Price(callPrice, Bond.Price.Type.Clean);
                callSchedule.Add(new Callability(callabilityPrice, Callability.Type.Call, callDate));

                callDate = calendar.advance(callDate, new Period(3, TimeUnit.Months));
            }

            // Create the callable bond
            callableBond = new CallableFixedRateBond(
                settlementDays, faceAmount, schedule, coupons,
                dayCounter, BusinessDayConvention.Unadjusted,
                100.0, issueDate, callSchedule);

            // Use a premium price (bond trading above par)
            price = 108.50;

            Console.WriteLine($"[SETUP] Created callable bond with {callSchedule.Count} call dates");
            Console.WriteLine($"[SETUP] Will calculate yields {Iterations} times per benchmark");
            Console.WriteLine($"[SETUP] Total yield calculations per run: {callSchedule.Count * Iterations}");
        }

        [Benchmark(Baseline = true, Description = "YieldToCalls with bond creation (CURRENT)")]
        public int YieldToCallsCurrent()
        {
            var totalCalculations = 0;
            // Calculate yields 100 times to show real-world impact
            for (var i = 0; i < Iterations; i++)
            {
                var results = callableBond.yieldToCalls(settlementDate, price, Frequency.Semiannual, 1.0e-8);
                totalCalculations += results.Length;
            }
            return totalCalculations;
        }

        // [Benchmark(Description = "YieldToCalls optimized - no bond creation")]
        // public int YieldToCallsOptimized()
        // {
        //     var totalCalculations = 0;
        //     // Calculate yields 100 times to show real-world impact
        //     for (var i = 0; i < Iterations; i++)
        //     {
        //         var results = callableBond.yieldToCallsOptimized(settlementDate, price, Frequency.Semiannual, 1.0e-8);
        //         totalCalculations += results.Length;
        //     }
        //     return totalCalculations;
        // }
    }
}
