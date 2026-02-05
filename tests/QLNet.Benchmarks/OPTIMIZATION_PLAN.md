# Bond Yield Calculation Optimization Plan

**Created**: February 3, 2026
**Baseline Established**: ✅ Complete
**Target Branch**: `feature/GH-310`

## Executive Summary

Based on comprehensive benchmark analysis of 1000+ bond scenarios, we have identified critical performance bottlenecks in `BondFunctions.yield()` calculations. This plan outlines a phased approach to achieve significant performance improvements.

## Baseline Performance Summary

| Scenario Type | Current Performance | Target Performance | Improvement Potential |
|---------------|--------------------|--------------------|----------------------|
| **Long-Term Bonds (20-30y)** | 354 μs/calc | <50 μs/calc | **7x improvement** |
| **Premium Bonds** | 19.8 μs/calc | ~10 μs/calc | **2x improvement** |
| **Short-Term Bonds (1-10y)** | 7-10 μs/calc | Maintain | Baseline |

### Critical Finding

Long-term bonds are **40x slower** than short-term bonds due to 40-60 coupon payments requiring extensive NPV calculations in each Newton-Raphson solver iteration.

## Phase 1: Long-Term Bond Yield Calculation (PRIORITY 1)

**Impact**: Highest - 7x potential improvement
**Risk**: Low - Optimization of existing algorithm
**Estimated Effort**: 2-3 days

### Root Cause Analysis

1. **Problem**: Newton-Raphson solver evaluates NPV for all cashflows in each iteration
2. **Long-term bonds**: 40-60 coupon payments × multiple solver iterations = expensive
3. **Short-term bonds**: 2-6 coupon payments × same iterations = fast

### Investigation Tasks

#### Task 1.1: Profile Solver Iterations
**File**: `src/QLNet/Cashflows/CashFlows.cs` (lines 936-947)

```csharp
// Current implementation uses NewtonSafe solver
// Need to track: iteration count, NPV calculation frequency
```

**Actions**:
1. Add instrumentation to `CashFlows.yield()` to track:
   - Number of solver iterations per calculation
   - Time spent in NPV evaluation vs solver overhead
   - Convergence rate by bond maturity
2. Run instrumented benchmarks on LongTerm_Mixed batch
3. Compare iteration counts: long-term vs short-term bonds

**Success Criteria**: Understand why long-term bonds require more iterations (if they do) or confirm NPV calculation is the bottleneck.

#### Task 1.2: Optimize Cashflow Calculations
**File**: `src/QLNet/Cashflows/CashFlows.cs`

**Current bottleneck locations**:
- NPV calculation in `IrrFinder` objective function
- Repeated discount factor calculations
- Potential for caching within solver loop

**Optimization strategies to test**:
1. **Incremental NPV calculation**:
   - Cache discount factors between iterations
   - Reuse calculations when yield guess changes slightly

2. **Reduce NPV calculation precision**:
   - Test if slightly less precision in intermediate steps maintains final accuracy
   - Current accuracy: 1.0e-10 (may be overkill for intermediate iterations)

3. **Vectorization opportunities**:
   - Batch discount factor calculations
   - Use SIMD operations if available

**Success Criteria**: Achieve <100 μs per calculation for long-term bonds (3.5x improvement).

#### Task 1.3: Improve Initial Yield Guess
**File**: `src/QLNet/Pricingengines/Bond/BondFunctions.cs`

**Current approach**: Fixed initial guess of 0.05 (5%)

**Better strategies**:
1. **Price-based heuristic**:
   - Discount bonds (price < 100): Start with higher yield guess
   - Premium bonds (price > 100): Start with lower yield guess
   - Formula: `initialGuess = couponRate + (100 - price) / maturity`

2. **Coupon-based adjustment**:
   - High coupon bonds: Adjust guess upward
   - Low coupon bonds: Adjust guess downward

3. **Maturity-aware guessing**:
   - Long-term bonds: More conservative guess adjustments
   - Short-term bonds: Can use more aggressive guesses

**Success Criteria**: Reduce average iterations by 20-30% through better initial guesses.

#### Task 1.4: Tune Solver Parameters
**File**: `src/QLNet/Cashflows/CashFlows.cs`

**Current parameters**:
```csharp
accuracy = 1.0e-10
maxIterations = 100
```

**Experiments to run**:
1. Test accuracy levels: 1.0e-10, 1.0e-8, 1.0e-6
2. Measure impact on:
   - Performance (speed improvement)
   - Result precision (acceptable deviation)
   - Convergence rate (% of scenarios that converge)

3. Consider adaptive accuracy:
   - Start with lower accuracy, refine if needed
   - Use higher accuracy only for critical scenarios

**Success Criteria**: Find optimal accuracy/performance trade-off. Target: 1.0e-8 with <5% performance loss and no convergence issues.

### Phase 1 Deliverables

1. **Instrumented benchmark results** showing iteration counts and bottlenecks
2. **Optimized cashflow calculation** with performance improvements
3. **Smart initial guess implementation** reducing iteration count
4. **Tuned solver parameters** balancing accuracy and speed
5. **Updated benchmarks** showing 5-7x improvement for long-term bonds

### Phase 1 Success Criteria

- [ ] Long-term bond yield calculation: 354 μs → <50 μs (7x improvement)
- [ ] No regression in short-term bond performance (<5% acceptable)
- [ ] Maintain convergence rate: ≥60% of scenarios converge
- [ ] All existing tests pass
- [ ] New benchmark results documented

## Phase 2: Premium Bond Pricing (PRIORITY 2)

**Impact**: Medium - 2x potential improvement
**Risk**: Low
**Estimated Effort**: 1-2 days

### Investigation Tasks

#### Task 2.1: Analyze Premium Bond Behavior
**Current**: Premium bonds (price > 104) take 2x longer than par bonds

**Investigation**:
1. Compare solver iteration counts:
   - Premium bonds vs par bonds
   - Premium bonds vs discount bonds
2. Analyze yield ranges:
   - Premium bonds typically have lower yields
   - Does solver struggle with low yield values?
3. Check for numerical instabilities:
   - Very low yields near zero
   - Precision issues in discount factor calculations

**Success Criteria**: Understand root cause of 2x slowdown.

#### Task 2.2: Implement Premium-Aware Optimizations

**Strategies**:
1. **Better initial guess for premium bonds**:
   ```csharp
   if (price > 100)
       initialGuess = Math.Max(0.001, couponRate * (100.0 / price) * 0.9);
   ```

2. **Adaptive solver tolerance**:
   - Lower yields may need tighter convergence criteria
   - Or: Lower yields may allow looser criteria (test both)

3. **Special handling for deep premium** (price > 110):
   - Use alternative solving approach
   - Or: Adjust solver bounds

**Success Criteria**: Premium bonds achieve ~10 μs per calculation (match par bond performance).

### Phase 2 Deliverables

1. **Analysis report** on premium bond behavior
2. **Premium-aware initial guess** implementation
3. **Updated benchmarks** showing 2x improvement
4. **Regression tests** ensuring accuracy maintained

## Phase 3: Solver Algorithm Comparison (PRIORITY 3)

**Impact**: Variable - Depends on alternative solver performance
**Risk**: Medium - Requires thorough testing
**Estimated Effort**: 3-5 days

### Investigation Tasks

#### Task 3.1: Benchmark Alternative Solvers

**Solvers to test** (if available in QLNet):
1. **Brent's method** - Bracket-based, guaranteed convergence
2. **Bisection** - Simple, reliable, potentially slower
3. **Secant method** - Similar to Newton-Raphson, no derivative needed
4. **Hybrid approach** - Start with Brent, switch to Newton-Raphson

**Comparison metrics**:
- Speed (μs per calculation)
- Convergence rate (% scenarios)
- Accuracy (deviation from Newton-Raphson baseline)
- Robustness (handles edge cases)

#### Task 3.2: Implement Adaptive Solver Selection

**Strategy**: Choose solver based on bond characteristics

```csharp
ISolver ChooseSolver(Bond bond, double price)
{
    if (bond.maturity() > 20 years)
        return new BrentSolver();  // More robust for long-term
    else if (price > 110 || price < 90)
        return new HybridSolver();  // Better for extreme pricing
    else
        return new NewtonSafeSolver();  // Fast for normal cases
}
```

**Success Criteria**: Match or exceed Newton-Raphson performance while improving convergence rate by 10-15%.

### Phase 3 Deliverables

1. **Solver comparison report** with benchmark results
2. **Adaptive solver selection** implementation (if beneficial)
3. **Updated benchmarks** showing improvements
4. **Decision document** on solver strategy going forward

## Implementation Strategy

### Step-by-Step Approach

1. **Create optimization branch**:
   ```bash
   git checkout feature/GH-310
   git checkout -b feature/GH-310-optimize-longterm
   ```

2. **For each optimization task**:
   - Implement changes
   - Run specific benchmark: `dotnet run -c Release -- --filter "*LongTerm*"`
   - Document results
   - Commit with measurements

3. **Incremental validation**:
   - Run full benchmark suite after each major change
   - Compare to baseline results
   - Ensure no regressions

4. **Testing protocol**:
   - All existing unit tests must pass
   - Add new tests for edge cases
   - Validate convergence rate maintained or improved

### Benchmarking Protocol

**Before starting**: Record baseline
```bash
cd tests/QLNet.Benchmarks
dotnet run -c Release > baseline_pre_optimization.txt
```

**After each change**: Quick check
```bash
dotnet run -c Release -- --filter "*LongTerm*" --job Short
```

**After completing a phase**: Full benchmark
```bash
dotnet run -c Release
# Compare results to BASELINE_RESULTS.md
```

### Documentation Requirements

For each optimization:
1. **Code comments**: Explain optimization strategy
2. **Commit message**: Include before/after measurements
3. **Update BASELINE_RESULTS.md**: Add new section with improvements
4. **Create performance log**: Track all measurements

## Risk Mitigation

### Potential Risks

1. **Accuracy degradation**
   - Mitigation: Test against known bond yield values
   - Acceptance: <0.01% deviation from baseline results
   - Validation: Run existing test suite

2. **Regression in short-term performance**
   - Mitigation: Run full benchmark suite after each change
   - Acceptance: <5% performance loss
   - Action: Rollback if regression detected

3. **Reduced convergence rate**
   - Mitigation: Track convergence in every benchmark run
   - Acceptance: Must maintain ≥60% convergence
   - Action: Investigate scenarios that stop converging

4. **Numerical instability**
   - Mitigation: Test extreme scenarios (very high/low yields)
   - Validation: Edge case test suite
   - Action: Add bounds checking if needed

### Testing Strategy

**Unit Tests**: Validate correctness
```csharp
[Fact]
public void OptimizedYieldMatchesOriginal()
{
    // Test on known scenarios
    // Ensure optimized version matches within tolerance
}
```

**Benchmark Tests**: Validate performance
```bash
# Run before optimization
dotnet run -c Release -- --filter "*LongTerm*" > before.txt

# Run after optimization
dotnet run -c Release -- --filter "*LongTerm*" > after.txt

# Compare results
diff before.txt after.txt
```

**Regression Tests**: Ensure no breaking changes
```bash
cd tests/QLNet.Tests
dotnet test --filter "Category=BondYield"
```

## Success Metrics

### Phase 1 Success (Long-Term Bonds)
- [x] Baseline established: 354 μs per calculation
- [ ] Target achieved: <50 μs per calculation (7x improvement)
- [ ] No regression: Short-term performance maintained
- [ ] Convergence maintained: ≥60% scenarios converge
- [ ] All tests pass

### Phase 2 Success (Premium Bonds)
- [x] Baseline established: 19.8 μs per calculation
- [ ] Target achieved: ~10 μs per calculation (2x improvement)
- [ ] Convergence improved: ≥70% scenarios converge
- [ ] All tests pass

### Phase 3 Success (Solver Comparison)
- [ ] Alternative solvers benchmarked
- [ ] Best solver strategy identified
- [ ] Implementation complete (if beneficial)
- [ ] Performance improved or maintained
- [ ] Convergence rate improved

### Overall Success
- [ ] **Total speedup**: 5-10x for long-term bonds
- [ ] **Maintained quality**: All tests pass
- [ ] **Documented**: Complete performance analysis
- [ ] **Reproducible**: Benchmarks show consistent improvements

## Timeline

| Phase | Tasks | Estimated Time | Dependencies |
|-------|-------|----------------|--------------|
| Phase 1.1 | Profile solver iterations | 0.5 days | None |
| Phase 1.2 | Optimize cashflow calcs | 1 day | Phase 1.1 |
| Phase 1.3 | Improve initial guess | 0.5 days | Phase 1.1 |
| Phase 1.4 | Tune solver parameters | 1 day | Phase 1.2 |
| **Phase 1 Total** | | **3 days** | |
| Phase 2.1 | Analyze premium bonds | 0.5 days | Phase 1 complete |
| Phase 2.2 | Implement optimizations | 1 day | Phase 2.1 |
| **Phase 2 Total** | | **1.5 days** | |
| Phase 3.1 | Benchmark alt solvers | 2 days | Phase 2 complete |
| Phase 3.2 | Adaptive solver | 2 days | Phase 3.1 |
| **Phase 3 Total** | | **4 days** | |
| **Total Estimated** | | **8.5 days** | |

## Key Files to Modify

### Primary Implementation Files
1. **`src/QLNet/Cashflows/CashFlows.cs`** (lines 936-947)
   - Main yield calculation implementation
   - Newton-Raphson solver usage
   - NPV calculation loop

2. **`src/QLNet/Pricingengines/Bond/BondFunctions.cs`**
   - Public API for yield calculations
   - Initial guess logic
   - Parameter handling

3. **`src/QLNet/Math/NewtonSafe.cs`** (if exists)
   - Solver implementation
   - May need modifications for caching

### Supporting Files
4. **`src/QLNet/Instruments/Bond.cs`**
   - Base bond class
   - Cashflow access

5. **`src/QLNet/Instruments/Bonds/FixedRateBond.cs`**
   - Fixed rate bond specifics
   - Used in benchmarks

### Test Files
6. **`tests/QLNet.Benchmarks/BondYieldBenchmarks.cs`**
   - Add instrumentation benchmarks
   - Track iteration counts

7. **`tests/QLNet.Tests/T_Bonds.cs`** (if exists)
   - Validate accuracy
   - Add regression tests

## Monitoring and Validation

### Performance Tracking

Create a performance log for each optimization:

```markdown
## Optimization: [Name]
Date: [Date]
Branch: [Branch name]

### Before
- Long-term bonds: 354 μs
- Short-term bonds: 9 μs
- Convergence: 65%

### After
- Long-term bonds: [X] μs ([Y]x improvement)
- Short-term bonds: [Z] μs ([W]% change)
- Convergence: [N]%

### Changes Made
- [List changes]

### Test Results
- Unit tests: [Pass/Fail]
- Benchmarks: [Results]
- Regression: [None/Describe]
```

### Continuous Validation

After each commit:
```bash
# 1. Build
dotnet build -c Release

# 2. Run tests
cd tests/QLNet.Tests
dotnet test

# 3. Quick benchmark
cd ../QLNet.Benchmarks
dotnet run -c Release -- --filter "*LongTerm*" --job Short

# 4. Record results
echo "Commit: $(git rev-parse --short HEAD)" >> optimization_log.txt
# Copy relevant metrics
```

## Next Steps

1. **Review this plan** with team/maintainer
2. **Set up instrumentation** for profiling
3. **Create optimization branch**
4. **Begin Phase 1.1**: Profile solver iterations
5. **Document findings** as you go

## References

- **Baseline Results**: `BASELINE_RESULTS.md`
- **Benchmark Usage**: `README.md`
- **Implementation Details**: `src/QLNet/Cashflows/CashFlows.cs:936-947`
- **Newton-Raphson Solver**: `src/QLNet/Math/` (check for solver implementations)

---

**Plan Status**: Ready for implementation
**Last Updated**: February 3, 2026
**Next Review**: After Phase 1 completion
