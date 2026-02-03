# Quick Start: Bond Yield Optimization

## 🎯 Goal
Improve long-term bond yield calculation from **354 μs → <50 μs** (7x faster)

## 📊 Current Baseline
- **Short-term bonds** (1-10 years): ~9 μs ✅ Good
- **Long-term bonds** (20-30 years): ~354 μs ⚠️ **40x slower!**
- **Premium bonds**: ~20 μs ⚠️ 2x slower than par

## 🚀 Getting Started (5 minutes)

### Step 1: Create Optimization Branch
```bash
cd D:\Git\QLNet\Performance
git checkout feature/GH-310
git checkout -b feature/GH-310-optimize-longterm
```

### Step 2: Record Current Baseline
```bash
cd tests/QLNet.Benchmarks
dotnet run -c Release -- --filter "*LongTerm*" > baseline_before.txt
```

### Step 3: Start with Task 1.1 - Profile Solver

**Goal**: Understand why long-term bonds are 40x slower

**Files to examine**:
- `src/QLNet/Cashflows/CashFlows.cs` (lines 936-947) - Main yield calculation
- `src/QLNet/Pricingengines/Bond/BondFunctions.cs` - Public API

**What to measure**:
1. How many solver iterations for long-term vs short-term bonds?
2. How much time in NPV calculation vs solver overhead?
3. Does solver converge faster with better initial guess?

## 📋 Phase 1 Tasks (Priority Order)

### Task 1.1: Add Instrumentation (0.5 days)
**File**: `src/QLNet/Cashflows/CashFlows.cs:936-947`

Add counters to track:
```csharp
// Add before solver.solve()
int iterationCount = 0;
var stopwatch = Stopwatch.StartNew();

// In solver callback, increment iterationCount

// After solver completes
stopwatch.Stop();
Console.WriteLine($"Iterations: {iterationCount}, Time: {stopwatch.ElapsedMicroseconds}μs");
```

**Test**: Run benchmark and capture iteration counts

### Task 1.2: Optimize Cashflow Calculation (1 day)
**Goal**: Reduce NPV calculation overhead

**Ideas to try**:
1. Cache discount factors between iterations
2. Use incremental NPV updates
3. Reduce unnecessary precision in intermediate steps

### Task 1.3: Improve Initial Guess (0.5 days)
**File**: `src/QLNet/Pricingengines/Bond/BondFunctions.cs`

**Current**: Fixed guess of 0.05 (5%)
**Better**:
```csharp
double initialGuess = couponRate + (100 - price) / (maturity * 100);
```

### Task 1.4: Tune Solver Parameters (1 day)
**File**: `src/QLNet/Cashflows/CashFlows.cs`

**Test accuracy levels**:
- Current: 1.0e-10
- Try: 1.0e-8, 1.0e-6
- Measure: Speed vs accuracy trade-off

## 🔬 Testing Each Change

### Quick Test (30 seconds)
```bash
cd tests/QLNet.Benchmarks
dotnet run -c Release -- --filter "*LongTerm*" --job Dry
```

### Full Benchmark (1 minute)
```bash
dotnet run -c Release -- --filter "*LongTerm*"
```

### Validation
```bash
cd ../QLNet.Tests
dotnet test --filter "Bond"
```

## 📈 Success Criteria

### Must Achieve
- [ ] Long-term bonds: <50 μs (currently 354 μs)
- [ ] Short-term bonds: No regression (stay ~9 μs)
- [ ] Convergence: Maintain ≥60%
- [ ] All tests: Pass

### Nice to Have
- [ ] Premium bonds: <10 μs (currently 20 μs)
- [ ] Convergence: Improve to ≥70%

## 🔍 Key Code Locations

### Main Implementation
```
src/QLNet/Cashflows/CashFlows.cs:936-947
└─ yield() method - Newton-Raphson solver
   └─ Uses IrrFinder objective function
      └─ Calculates NPV in each iteration
```

### Newton-Raphson Solver
```
src/QLNet/Math/
├─ Check for NewtonSafe.cs
├─ Or Solver1D.cs
└─ Look for solve() method
```

### Bond Functions API
```
src/QLNet/Pricingengines/Bond/BondFunctions.cs
└─ yield() public methods
   └─ Entry point for yield calculations
```

## 💡 Quick Wins to Try First

### 1. Better Initial Guess (Easiest)
**Impact**: Potentially 20-30% faster
**Risk**: Very low
**Time**: 1 hour

Change in `BondFunctions.cs`:
```csharp
// Instead of:
double guess = 0.05;

// Try:
double guess = couponRate + (100 - price) / (maturity * 100);
guess = Math.Max(0.001, Math.Min(guess, 0.20)); // Clamp to reasonable range
```

### 2. Relaxed Accuracy for Speed (Medium)
**Impact**: Potentially 2-3x faster
**Risk**: Medium (must validate results)
**Time**: 2 hours

Change in `CashFlows.cs`:
```csharp
// Instead of:
double accuracy = 1.0e-10;

// Try:
double accuracy = 1.0e-8;  // Still very accurate for bond yields
```

**MUST TEST**: Compare results to ensure <0.01% deviation

### 3. Profile Before Optimizing (Smartest)
**Impact**: Directs effort to right place
**Risk**: Zero
**Time**: 2 hours

Add diagnostics to understand bottleneck:
- Is it iteration count?
- Is it NPV calculation per iteration?
- Is it discount factor computation?

## 📊 Benchmark Comparison Template

After each change, record results:

```
=== Optimization: [Name] ===
Date: [Date]
Commit: [Hash]

Long-Term Bonds:
  Before: 354 μs
  After:  [X] μs
  Improvement: [Y]x

Short-Term Bonds:
  Before: 9 μs
  After:  [Z] μs
  Change: [W]%

Convergence:
  Before: 65%
  After:  [N]%

Tests: [Pass/Fail]
```

## 🎓 Learning Resources

### Understanding Newton-Raphson for Yield
The solver tries to find yield `y` where:
```
NPV(y) = price
```

In each iteration:
1. Calculate NPV for current yield guess
2. Calculate derivative (sensitivity)
3. Adjust yield guess: `y_new = y_old - NPV(y_old) / derivative`
4. Repeat until NPV ≈ price (within tolerance)

**For long-term bonds**: Each iteration evaluates 40-60 cashflows
**For short-term bonds**: Each iteration evaluates 2-6 cashflows
**Result**: Long-term is much slower per iteration

### Optimization Strategies
1. **Reduce iterations**: Better initial guess
2. **Faster iterations**: Optimize NPV calculation
3. **Both**: Best approach

## 🐛 Troubleshooting

### Benchmark shows no improvement
- Clear build: `dotnet clean && dotnet build -c Release`
- Check you're in Release mode (not Debug)
- Verify changes are in the code path being executed

### Tests failing after changes
- Revert to last working commit
- Make smaller incremental changes
- Add unit tests for new behavior

### Convergence rate drops
- Your optimization may be too aggressive
- Adjust solver tolerance
- Improve initial guess bounds

## 📞 Next Steps

1. **Read the full plan**: `OPTIMIZATION_PLAN.md`
2. **Start with Task 1.1**: Add instrumentation
3. **Run benchmarks**: Before and after each change
4. **Document results**: Keep track of what works
5. **Commit frequently**: Small commits with measurements

---

**Ready to optimize?** Start with Task 1.1 in `OPTIMIZATION_PLAN.md`
**Questions?** Review the detailed plan and baseline results
**Stuck?** Check the troubleshooting section above
