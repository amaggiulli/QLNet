using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using QLNet;
using System;
using System.Collections.Generic;

namespace QLNet.Benchmarks
{
   // Use ShortRunJob for faster benchmarks (3 warmup, 3 iterations instead of ~15-100)
   [ShortRunJob(RuntimeMoniker.Net90)]
   [MemoryDiagnoser]
   public class ScheduleUntilBenchmark(
       Schedule schedule10Years,
       Schedule schedule30Years,
       Date truncationDate5Years,
       Date truncationDate15Years)
   {
      private Schedule schedule10Years = schedule10Years;
      private Schedule schedule30Years = schedule30Years;
      private Date truncationDate5Years = truncationDate5Years;
      private Date truncationDate15Years = truncationDate15Years;

      [GlobalSetup]
      public void Setup()
      {
         var startDate = new Date(15, Month.January, 2020);
         var endDate10 = new Date(15, Month.January, 2030);
         var endDate30 = new Date(15, Month.January, 2050);
         var tenor = new Period(6, TimeUnit.Months);
         var calendar = new TARGET();
         var convention = BusinessDayConvention.ModifiedFollowing;
         var terminationDateConvention = BusinessDayConvention.ModifiedFollowing;
         var rule = DateGeneration.Rule.Backward;

         // Create schedules with different sizes
         schedule10Years = new Schedule(startDate, endDate10, tenor, calendar,
                                       convention, terminationDateConvention, rule, false);

         schedule30Years = new Schedule(startDate, endDate30, tenor, calendar,
                                       convention, terminationDateConvention, rule, false);

         // Truncation dates at 5 and 15 years
         truncationDate5Years = new Date(15, Month.January, 2025);
         truncationDate15Years = new Date(15, Month.January, 2035);
      }

      [Benchmark(Description = "Until - 10yr schedule, truncate at 5yr (remove ~10 dates)")]
      public Schedule Until_10Y_Truncate_5Y()
      {
         return schedule10Years.until(truncationDate5Years);
      }

      [Benchmark(Description = "Until - 30yr schedule, truncate at 5yr (remove ~50 dates)")]
      public Schedule Until_30Y_Truncate_5Y()
      {
         return schedule30Years.until(truncationDate5Years);
      }

      [Benchmark(Description = "Until - 30yr schedule, truncate at 15yr (remove ~30 dates)")]
      public Schedule Until_30Y_Truncate_15Y()
      {
         return schedule30Years.until(truncationDate15Years);
      }

      [Benchmark(Baseline = true, Description = "Until - No truncation (just clone)")]
      public Schedule Until_NoTruncation()
      {
         // Truncate at a date after the schedule end - should just clone
         var farFutureDate = new Date(15, Month.January, 2060);
         return schedule30Years.until(farFutureDate);
      }
   }
}
