namespace QLNet.Benchmarks;

public class BondScenario
{
    public string Name { get; set; } = string.Empty;
    public int MaturityYears { get; set; }
    public double CouponRate { get; set; }
    public double Price { get; set; }
    public Frequency Frequency { get; set; }
    public DayCounter DayCounter { get; set; } = new Thirty360(Thirty360.Thirty360Convention.BondBasis);
    public Compounding Compounding { get; set; }
    public Date IssueDate { get; set; } = new Date();
    public Date SettlementDate { get; set; } = new Date();
}

public class BondScenarioBatch
{
    public string BatchName { get; set; } = string.Empty;
    public List<BondScenario> Scenarios { get; set; } = new List<BondScenario>();
}

public static class BondScenarioGenerator
{
    private static readonly int[] Maturities = { 1, 2, 3, 5, 7, 10, 20, 30 };
    private static readonly double[] CouponRates = { 0.0025, 0.005, 0.015, 0.025, 0.04, 0.05, 0.075 };
    private static readonly double[] Prices = { 85, 92, 96, 100, 104, 108, 115 };
    private static readonly Frequency[] Frequencies = { Frequency.Annual, Frequency.Semiannual, Frequency.Quarterly, Frequency.Monthly };
    private static readonly Compounding[] Compoundings = { Compounding.Simple, Compounding.Compounded, Compounding.Continuous };

    public static List<BondScenarioBatch> GenerateBatches()
    {
        var issueDate = new Date(15, Month.January, 2020);
        var settlementDate = new Date(15, Month.January, 2025);

        var allScenarios = new List<BondScenario>();

        // Generate all combinations
        foreach (var maturity in Maturities)
        {
            foreach (var coupon in CouponRates)
            {
                foreach (var price in Prices)
                {
                    foreach (var frequency in Frequencies)
                    {
                        foreach (var compounding in Compoundings)
                        {
                            // Apply filtering rules
                            if (!IsRealisticScenario(maturity, coupon, price, frequency))
                                continue;

                            // Use different day counters for variety
                            var dayCounter = GetDayCounter(allScenarios.Count % 3);

                            allScenarios.Add(new BondScenario
                            {
                                Name = $"M{maturity}Y_C{coupon:P2}_P{price}_F{frequency}_{compounding}",
                                MaturityYears = maturity,
                                CouponRate = coupon,
                                Price = price,
                                Frequency = frequency,
                                DayCounter = dayCounter,
                                Compounding = compounding,
                                IssueDate = issueDate,
                                SettlementDate = settlementDate
                            });
                        }
                    }
                }
            }
        }

        // Group into batches
        return CreateBatches(allScenarios);
    }

    private static bool IsRealisticScenario(int maturity, double coupon, double price, Frequency frequency)
    {
        // Filter out monthly frequency for bonds longer than 10 years
        if (frequency == Frequency.Monthly && maturity > 10)
            return false;

        // Filter out unrealistic combinations: low coupon with high premium
        if (coupon < 0.01 && price > 110)
            return false;

        // Filter out unrealistic combinations: high coupon with deep discount
        if (coupon > 0.06 && price < 90)
            return false;

        // For very short maturities, limit frequency
        if (maturity == 1 && (frequency == Frequency.Quarterly || frequency == Frequency.Monthly))
            return false;

        return true;
    }

    private static DayCounter GetDayCounter(int index)
    {
        return index switch
        {
            0 => new Thirty360(Thirty360.Thirty360Convention.BondBasis),
            1 => new ActualActual(ActualActual.Convention.Bond),
            2 => new Actual365Fixed(),
            _ => new Thirty360(Thirty360.Thirty360Convention.BondBasis)
        };
    }

    private static List<BondScenarioBatch> CreateBatches(List<BondScenario> allScenarios)
    {
        var batches = new List<BondScenarioBatch>();

        // Batch 1: Short-term bonds (1-3 years)
        batches.Add(new BondScenarioBatch
        {
            BatchName = "ShortTerm_Mixed",
            Scenarios = allScenarios.Where(s => s.MaturityYears <= 3).Take(150).ToList()
        });

        // Batch 2: Medium-term bonds near par (4-7 years, price 96-104)
        batches.Add(new BondScenarioBatch
        {
            BatchName = "MediumTerm_ParBonds",
            Scenarios = allScenarios.Where(s => s.MaturityYears >= 5 && s.MaturityYears <= 7 && s.Price >= 96 && s.Price <= 104).Take(150).ToList()
        });

        // Batch 3: Medium-term discount bonds (5-10 years, price < 96)
        batches.Add(new BondScenarioBatch
        {
            BatchName = "MediumTerm_Discount",
            Scenarios = allScenarios.Where(s => s.MaturityYears >= 5 && s.MaturityYears <= 10 && s.Price < 96).Take(150).ToList()
        });

        // Batch 4: Medium-term premium bonds (5-10 years, price > 104)
        batches.Add(new BondScenarioBatch
        {
            BatchName = "MediumTerm_Premium",
            Scenarios = allScenarios.Where(s => s.MaturityYears >= 5 && s.MaturityYears <= 10 && s.Price > 104).Take(150).ToList()
        });

        // Batch 5: Long-term bonds (20-30 years)
        batches.Add(new BondScenarioBatch
        {
            BatchName = "LongTerm_Mixed",
            Scenarios = allScenarios.Where(s => s.MaturityYears >= 20).Take(150).ToList()
        });

        // Batch 6: High coupon bonds (coupon >= 4%)
        batches.Add(new BondScenarioBatch
        {
            BatchName = "HighCoupon_AllMaturities",
            Scenarios = allScenarios.Where(s => s.CouponRate >= 0.04).Take(150).ToList()
        });

        // Batch 7: Low coupon bonds (coupon <= 1.5%)
        batches.Add(new BondScenarioBatch
        {
            BatchName = "LowCoupon_AllMaturities",
            Scenarios = allScenarios.Where(s => s.CouponRate <= 0.015).Take(150).ToList()
        });

        // Batch 8: Monthly and quarterly frequency bonds
        batches.Add(new BondScenarioBatch
        {
            BatchName = "HighFrequency_Bonds",
            Scenarios = allScenarios.Where(s => s.Frequency == Frequency.Monthly || s.Frequency == Frequency.Quarterly).Take(150).ToList()
        });

        return batches;
    }
}
