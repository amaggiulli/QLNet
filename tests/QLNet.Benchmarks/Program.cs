using BenchmarkDotNet.Running;

namespace QLNet.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        // Use simple performance test instead of BenchmarkDotNet (faster, more reliable)
        // SimplePerformanceTest.Run(args);

        // Use BenchmarkDotNet framework for detailed statistics:
        // Use BenchmarkSwitcher to allow running any benchmark via command line
        // Example: dotnet run -c Release -- --filter *BondYieldProfile*
        var summary = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
