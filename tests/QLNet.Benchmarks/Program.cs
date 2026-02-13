using BenchmarkDotNet.Running;

namespace QLNet.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        // Use simple performance test instead of BenchmarkDotNet (faster, more reliable)
        // SimplePerformanceTest.Run(args);

        // Use BenchmarkDotNet framework for detailed statistics:
        // Don't pass config to avoid duplicate jobs (benchmark class already has job attributes)
        var summary = BenchmarkRunner.Run<CallableBondYieldBenchmarks>(args: args);
    }
}
