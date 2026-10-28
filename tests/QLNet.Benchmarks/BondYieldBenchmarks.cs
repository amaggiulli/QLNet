using BenchmarkDotNet.Attributes;

namespace QLNet.Benchmarks;

[Config(typeof(BenchmarkConfig))]
public class BondYieldBenchmarks
{
    private List<BondScenarioBatch>? _batches;
    private List<(Bond bond, double price, DayCounter dc, Compounding comp, Frequency freq, Date settlement)> _bonds = null!;

    [ParamsSource(nameof(BatchNames))]
    public string BatchName { get; set; } = string.Empty;

    public IEnumerable<string> BatchNames
    {
        get
        {
            _batches ??= BondScenarioGenerator.GenerateBatches();
            return _batches.Select(b => b.BatchName);
        }
    }

    [GlobalSetup]
    public void Setup()
    {
        // Generate all scenario batches if not already done
        _batches ??= BondScenarioGenerator.GenerateBatches();
    }

    [IterationSetup]
    public void IterationSetup()
    {
        // Find the current batch
        var currentBatch = _batches!.First(b => b.BatchName == BatchName);

        // Pre-construct all bonds for this batch
        _bonds = new List<(Bond, double, DayCounter, Compounding, Frequency, Date)>();

        var calendar = new TARGET();
        var convention = BusinessDayConvention.Unadjusted;

        foreach (var scenario in currentBatch.Scenarios)
        {
            try
            {
                // Calculate maturity date
                var maturityDate = scenario.IssueDate + new Period(scenario.MaturityYears, TimeUnit.Years);

                // Create schedule - use the second constructor with all required parameters
                var schedule = new Schedule(
                    scenario.IssueDate,              // effectiveDate
                    maturityDate,                     // terminationDate
                    new Period(scenario.Frequency),   // tenor
                    calendar,                         // calendar
                    convention,                       // convention
                    convention,                       // terminationDateConvention
                    DateGeneration.Rule.Backward,     // rule
                    false);                           // endOfMonth

                // Create fixed rate bond
                var bond = new FixedRateBond(
                    settlementDays: 0,
                    faceAmount: 100.0,
                    schedule: schedule,
                    coupons: new List<double> { scenario.CouponRate },
                    accrualDayCounter: scenario.DayCounter,
                    paymentConvention: convention,
                    redemption: 100.0,
                    issueDate: scenario.IssueDate);

                _bonds.Add((bond, scenario.Price, scenario.DayCounter, scenario.Compounding, scenario.Frequency, scenario.SettlementDate));
            }
            catch
            {
                // Skip bonds that fail to construct
                continue;
            }
        }
    }

    [Benchmark]
    public void YieldCalculations()
    {
        foreach (var (bond, price, dc, comp, freq, settlement) in _bonds)
        {
            try
            {
                var yieldValue = BondFunctions.yield(bond, price, dc, comp, freq, settlement);
            }
            catch
            {
                // Handle non-converging scenarios gracefully
                continue;
            }
        }
    }
}
