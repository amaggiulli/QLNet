using BenchmarkDotNet.Running;

namespace QLNet.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        // Use simple performance test instead of BenchmarkDotNet (faster, more reliable)
        // SimplePerformanceTest.Run(args);

        // Use BenchmarkDotNet framework for detailed statistics:
        var config = args.Length > 0 ? null : new BenchmarkConfig();
        var summary = BenchmarkRunner.Run<CallableBondYieldBenchmarks>(config, args);
    }
}
