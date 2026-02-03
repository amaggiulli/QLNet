# QLNet Bond Yield Calculation Benchmarks

This project provides comprehensive performance benchmarks for QLNet Bond yield calculations using [BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet).

## Overview

The benchmark suite measures the performance of `BondFunctions.yield()` across 1000+ different bond scenarios, organized into 8 thematic batches. This establishes a performance baseline for:
- Future optimization work
- Regression detection
- Performance comparisons across different configurations

## Quick Start

### Build the Project

```bash
cd tests/QLNet.Benchmarks
dotnet build -c Release
```

### Run All Benchmarks

```bash
dotnet run -c Release
```

**Important**: Always run benchmarks in Release mode for accurate results.

### Run Specific Batches

Run a single batch:
```bash
dotnet run -c Release -- --filter "*ShortTerm*"
```

Run multiple batches:
```bash
dotnet run -c Release -- --filter "*ShortTerm* *MediumTerm*"
```

### Quick Dry Run (for testing)

```bash
dotnet run -c Release -- --job Dry
```

## Benchmark Batches

The suite includes 8 scenario batches, each containing 100-150 realistic bond configurations:

| Batch Name | Description | Focus |
|------------|-------------|-------|
| **ShortTerm_Mixed** | 1-3 year bonds | Short-dated bonds, all varieties |
| **MediumTerm_ParBonds** | 5-7 year bonds at par | Near-par pricing (96-104) |
| **MediumTerm_Discount** | 5-10 year discount bonds | Below par pricing (< 96) |
| **MediumTerm_Premium** | 5-10 year premium bonds | Above par pricing (> 104) |
| **LongTerm_Mixed** | 20-30 year bonds | Long-dated bonds |
| **HighCoupon_AllMaturities** | High coupon bonds (≥ 4%) | High yield scenarios |
| **LowCoupon_AllMaturities** | Low coupon bonds (≤ 1.5%) | Low yield scenarios |
| **HighFrequency_Bonds** | Monthly/Quarterly payments | Frequent payment schedules |

## Scenario Generation

Each scenario varies across multiple dimensions:

- **Maturities**: 1, 2, 3, 5, 7, 10, 20, 30 years
- **Coupon Rates**: 0.25%, 0.50%, 1.50%, 2.50%, 4.00%, 5.00%, 7.50%
- **Prices**: 85, 92, 96, 100, 104, 108, 115
- **Frequencies**: Annual, Semiannual, Quarterly, Monthly
- **Day Counters**: Thirty360 (BondBasis), ActualActual (Bond), Actual365Fixed
- **Compounding**: Simple, Compounded, Continuous

Unrealistic combinations (e.g., monthly frequency for 30-year bonds, low coupons at deep premium) are filtered out.

## Architecture

### Performance Optimization

The benchmark is designed to measure only the `BondFunctions.yield()` calculation:

1. **GlobalSetup**: Generates all scenario batches once
2. **IterationSetup**: Pre-constructs all Bond and Schedule objects for the current batch
3. **Benchmark**: Measures only the yield calculation loop

This ensures that bond construction overhead doesn't skew the yield calculation performance metrics.

### What's Measured

- **Mean execution time** per batch of yield calculations
- **Standard deviation** and confidence intervals
- **Memory allocations** per operation
- **GC pressure** (Gen0/Gen1/Gen2 collections)
- **Min/Max/Median** execution times

## Output

Benchmark results are saved to:
```
tests/QLNet.Benchmarks/bin/Release/net9.0/BenchmarkDotNet.Artifacts/results/
```

Generated reports:
- **Markdown** (GitHub format): `*-report-github.md`
- **CSV**: `*-report.csv` (for data analysis)
- **HTML**: `*-report.html` (visual report)

## Example Results

```
| Method            | BatchName       | Mean     | StdDev   | Allocated |
|------------------ |---------------- |---------:|---------:|----------:|
| YieldCalculations | ShortTerm_Mixed | 874.8 us | 197.5 us | 133.14 KB |
```

This shows:
- Average of ~875 microseconds for ~98 yield calculations
- ~9 microseconds per individual yield calculation
- Minimal memory allocation (133 KB total for the batch)

## Expected Performance

Based on initial measurements:

- **Simple bonds** (3-5 years, near par): ~5-10 μs per calculation
- **Complex bonds** (30 years, deep discount/premium): ~15-30 μs per calculation
- **Total suite runtime**: 5-10 minutes for all batches with full iterations

## BenchmarkDotNet Options

Common command-line options:

```bash
# Run specific batches
--filter "*ShortTerm*"

# Use a specific job (Dry, Short, Medium, Long)
--job Short

# Export results to a specific directory
--artifacts ./my-results

# List all available benchmarks
--list flat

# Run with memory profiling
--profiler ETW

# Display help
--help
```

## Configuration

The benchmark uses a custom configuration (`BenchmarkConfig`) with:

- **Runtime**: .NET 9.0
- **Platform**: x64
- **JIT**: RyuJit
- **Diagnostics**: Memory profiling enabled
- **Exporters**: Markdown (GitHub), CSV, HTML

To modify the configuration, edit `BenchmarkConfig.cs`.

## Troubleshooting

### Build Errors

Ensure you're building in Release mode:
```bash
dotnet build -c Release
```

### Non-converging Scenarios

Some bond/yield combinations may not converge (the Newton-Raphson solver fails). These are handled gracefully with try-catch blocks and excluded from results.

### Performance Variance

For consistent results:
1. Close other applications
2. Run on AC power (not battery)
3. Disable CPU throttling
4. Run multiple times and average results

## Implementation Details

### Key Classes

- **`BondYieldBenchmarks`**: Main benchmark class with `[Benchmark]` methods
- **`BondScenarioGenerator`**: Generates realistic bond scenarios
- **`BondScenarioBatch`**: Groups scenarios into thematic batches
- **`BenchmarkConfig`**: Custom BenchmarkDotNet configuration

### Core Algorithm

The yield calculation uses:
- `CashFlows.yield()` in `src/QLNet/Cashflows/CashFlows.cs`
- Newton-Raphson solver (`NewtonSafe`)
- Default accuracy: 1.0e-10
- Max iterations: 100

## Future Enhancements

Potential optimization opportunities once baseline is established:

1. Optimize Newton-Raphson solver convergence
2. Compare alternative solving methods (Brent, Bisection)
3. Reduce memory allocations in hot paths
4. Implement parallel yield calculation batching
5. Fine-tune accuracy/iteration trade-offs

## Contributing

To add new benchmark scenarios:

1. Edit `BondScenarioGenerator.cs` to add new parameter combinations
2. Create new batch definitions in `CreateBatches()`
3. Rebuild and run to verify scenarios

To add new benchmark methods:

1. Add a new method with `[Benchmark]` attribute to `BondYieldBenchmarks`
2. Follow the existing pattern for setup and iteration

## License

This benchmark project follows the same license as QLNet: see the main QLNet LICENSE file.
