using BenchmarkDotNet.Running;

namespace QLNet.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        var config = args.Length > 0 ? null : new BenchmarkConfig();
        var summary = BenchmarkRunner.Run<BondYieldBenchmarks>(config, args);
    }
}
