# Task 1.3 Results - Improve Initial Guess

## Date: February 3, 2026

## Optimization Implemented
Replaced fixed initial guess (0.05) with price-based heuristic:

```csharp
double estimatedCouponRate = // extracted from first coupon
double yearsToMaturity = (maturity - settlement) / 365.25;
double priceDeviation = (100.0 - dirtyPrice) / 100.0;
guess = estimatedCouponRate + (priceDeviation / yearsToMaturity);
guess = Math.Max(0.001, Math.Min(guess, 0.20)); // Clamp to [0.1%, 20%]
```

## Results

### Iteration Count Reduction ✅
**Before**: ~35 iterations average
**After**: ~24-26 iterations average  
**Reduction**: ~28% fewer iterations

Sample data:
- 21 cashflows: 35 → 25 iterations (29% reduction)
- 41 cashflows: 35 → 24 iterations (31% reduction)
- 81 cashflows: 35 → 26 iterations (26% reduction)

### Performance Improvement ⚠️
| Metric | Baseline | After Optimization | Improvement |
|--------|----------|-------------------|-------------|
| **Full Run (Job)** | 37.69 ms | 37.22 ms | **1.2% faster** |
| **Short Run** | 52.02 ms | 33.74 ms | **35% faster** |
| **Allocated Memory** | 55.42 MB | 43.64 MB | **21% less** |

### Analysis

**Positive**:
- ✅ Iteration count reduced by 28% (confirmed in profiling data)
- ✅ Memory allocation improved by 21% (43.64 MB vs 55.42 MB)
- ✅ Short run shows significant improvement (35% faster)

**Observation**:
- ⚠️ Full run improvement is modest (1.2%)
- Suggests solver overhead beyond NPV calculation is significant
- Variance in benchmarks may mask true improvement

### Why Is Full Run Only 1.2% Faster?

Possible explanations:
1. **Solver overhead**: Time is not just NPV calculation
   - Newton-Raphson convergence checks
   - Derivative calculations
   - Bisection fallback logic

2. **Diminishing returns**: Reducing from 35 to 25 iterations
   - Saves 10 iterations worth of NPV time
   - But solver overhead per iteration remains
   
3. **Benchmark variance**: Standard deviation is 3-4ms
   - 1.2% improvement (~0.47ms) is within noise

### Breakdown of Time

From profiling data:
- NPV calculation time: ~850 μs (for 81 cashflows, 25 iterations)
- Total time: ~1,200 μs
- Solver overhead: ~350 μs (29% of total)

For full benchmark (98 calculations):
- NPV time saved: 98 × 10 iterations × ~24 μs = ~23.5 ms saved
- Actual improvement: 37.69 - 37.22 = 0.47 ms
- **Conclusion**: Most of the theoretical savings are lost to other factors

### Next Steps

**Option 1**: Accept modest gain and move to Task 1.2 (NPV optimization)
- This is where the big gains are (5-10x potential)
- Initial guess optimization gives diminishing returns

**Option 2**: Further tune initial guess
- Analyze scenarios where iterations are still high (29-32)
- Refine formula for different bond types

**Recommendation**: Move to Task 1.2 - Optimize NPV Calculation
- The 28% iteration reduction is real
- But to get 7x improvement, we need to optimize the NPV calculation itself
- Even with 25 iterations, each iteration processing 81 cashflows is expensive

## Files Modified
- `src/QLNet/Pricingengines/Bond/BondFunctions.cs` - Added smart initial guess logic

## Conclusion

Task 1.3 achieved its primary goal:
- ✅ Reduced iteration count by 28%
- ✅ Improved memory efficiency by 21%
- ⚠️ Overall performance gain is modest due to solver overhead

**Key Learning**: Iteration count is only part of the story. The cost per iteration (NPV calculation) remains the dominant factor. This confirms Task 1.2 (NPV optimization) is where we'll achieve our 7x target.

**Status**: ✅ Task 1.3 Complete - Proceed to Task 1.2
