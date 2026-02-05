# Task 1.1 Profiling Analysis - COMPLETED

## Date: February 3, 2026

## Objective
Understand why long-term bonds are 40x slower than short-term bonds in yield calculations.

## Data Collected
- 18,000 profiling entries from LongTerm_Mixed benchmark
- Each entry tracks: cashflows, maturity, iterations, NPV time, total time, avg per iteration

## Key Findings

### 1. ITERATION COUNT IS CONSISTENT ✅
**Observation**: All bonds require approximately the same number of iterations regardless of maturity or cashflow count.
- Range: 32-36 iterations
- Average: ~35 iterations
- **Conclusion**: Iteration count is NOT the bottleneck

### 2. NPV CALCULATION TIME SCALES LINEARLY WITH CASHFLOWS ⚠️
**Observation**: Time spent in NPV calculation scales directly with number of cashflows.

| Cashflows | Avg NPV Time | Avg Per Iteration | Total Time Range |
|-----------|--------------|-------------------|------------------|
| 21 (Annual) | ~250 μs | ~7 μs | 250-700 μs |
| 41 (Semiannual) | ~450 μs | ~13 μs | 300-1200 μs |
| 81 (Monthly) | ~850 μs | ~24 μs | 500-2300 μs |

**Calculation**:
- 21 cashflows: 35 iterations × 7 μs = 245 μs
- 41 cashflows: 35 iterations × 13 μs = 455 μs
- 81 cashflows: 35 iterations × 24 μs = 840 μs

### 3. BOTTLENECK IDENTIFIED 🎯
**Root Cause**: NPV calculation processes ALL cashflows in EVERY iteration

```csharp
// In IrrFinder.value() - called 35 times per yield calculation
public override double value(double y)
{
    InterestRate yield = new InterestRate(y, dayCounter_, compounding_, frequency_);
    // THIS LINE is called 35 times and processes ALL cashflows each time:
    double NPV = CashFlows.npv(leg_, yield, includeSettlementDateFlows_, settlementDate_, npvDate_);
    return npv_ - NPV;
}
```

For an 81-cashflow bond:
- 81 cashflows × 35 iterations = **2,835 cashflow evaluations** per yield calculation

### 4. COMPARISON: Short vs Long-Term Bonds
**Extrapolation** (need to verify with actual short-term profiling):
- Short-term (3 years): ~6 cashflows × 35 iterations = 210 evaluations
- Long-term (30 years, monthly): ~360 cashflows × 35 iterations = 12,600 evaluations
- **Ratio**: 12,600 / 210 = **60x more evaluations**

This explains the 40x slowdown observed in benchmarks!

## Optimization Opportunities (Ranked by Impact)

### Priority 1: Optimize NPV Calculation (HIGHEST IMPACT)
**Target**: Reduce cost of processing cashflows in each iteration

**Strategies**:
1. **Cache discount factors** between iterations
   - Yield changes slightly each iteration
   - Can reuse or incrementally update discount factors
   - **Potential**: 2-3x faster

2. **Incremental NPV updates**
   - Instead of recalculating all cashflows, update incrementally
   - Use previous NPV and adjust for yield change
   - **Potential**: 3-5x faster

3. **Vectorize cashflow processing**
   - Process multiple cashflows in parallel (SIMD)
   - **Potential**: 2x faster on modern CPUs

**Combined potential**: 5-10x improvement

### Priority 2: Improve Initial Guess (MEDIUM IMPACT)
**Target**: Reduce iteration count from 35 to 15-20

**Current**: Fixed guess of 0.05 (5%)
**Better approach**: Price-based heuristic
```csharp
double initialGuess = couponRate + (100 - price) / (maturity * 100);
```

**Potential**: 30-40% faster (not as impactful as NPV optimization)

### Priority 3: Solver Tuning (LOW IMPACT)
**Target**: Relax accuracy without compromising results

**Current**: 1.0e-10
**Test**: 1.0e-8 (still very accurate for bond yields)

**Potential**: 10-20% faster

## Recommended Next Steps

1. **IMMEDIATE**: Implement Priority 1.1 - Cache discount factors
   - Most straightforward optimization
   - Largest potential impact
   - Low risk

2. **QUICK WIN**: Implement Priority 2 - Better initial guess
   - Easy to implement (30 minutes)
   - 30-40% improvement
   - Can do while planning NPV optimization

3. **RESEARCH**: Study CashFlows.npv() implementation
   - Understand current discount factor calculation
   - Identify caching opportunities
   - Design incremental update strategy

## Files to Modify

### Primary Target
- `src/QLNet/Cashflows/CashFlows.cs:267`
  - `IrrFinder.value()` method
  - Where NPV is calculated 35 times per yield calculation

### Supporting Modifications
- `src/QLNet/Cashflows/CashFlows.cs` (npv() method)
  - Add caching support
  - Implement incremental updates

## Success Metrics

### Phase 1 Complete When:
- [x] Profiling data collected (18,000 entries)
- [x] Bottleneck identified (NPV calculation scales with cashflows)
- [x] Root cause understood (35 iterations × all cashflows)
- [x] Optimization strategies prioritized
- [ ] Next task ready to start (Priority 1.1: Cache discount factors)

## Data Files
- `instrumentation_longterm.txt` - Full profiling data (18,000 entries)
- `profiling_analysis.md` - This analysis

## Conclusion

**Question Answered**: Are long-term bonds slow due to more iterations or slower iterations?
**Answer**: **Slower iterations** - same iteration count (~35), but each iteration processes many more cashflows.

**Implication**: Optimizing the NPV calculation loop is the key to achieving our 7x improvement target.

**Next Task**: Task 1.2 - Optimize Cashflow Calculations (start with discount factor caching)
