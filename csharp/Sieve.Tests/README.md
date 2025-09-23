# Prime Sieve Library — Test Suite

This test suite validates correctness and demonstrates the performance characteristics of the sieve implementation with clear, readable console output.

## Core API

```csharp
long NthPrime(long n0Based);
```

Where **`prime[0] = 2`** (0-based indexing).

The implementation automatically chooses between three strategies:
- **Regular Sieve**: Single contiguous bitset (fastest for small indices)
- **Segmented Sieve**: Bounded working set (memory-efficient for medium indices)
- **Prime Counting**: Lucy-Hedgehog algorithm (fastest for very large indices)

---

## Test Classes

### UnitTest1.cs
Comprehensive correctness verification with performance insights:
- **TestNthPrime()**: Validates correctness from small to billion-scale indices
- **TestBillionthPrime()**: Demonstrates Lucy-Hedgehog performance for the billionth prime

### PerformanceTests.cs
Simplified performance demonstrations with clean table output:
- **NthPrime_ReturnsCorrectValue**: Basic correctness across the usage spectrum
- **AllMethods_ProduceSameResults**: Verifies all three methods produce identical results
- **AutoSelection_ChoosesAppropriateMethod**: Tests smart method selection logic
- **PerformanceDemonstration_MethodCrossover**: Shows timing differences across size categories
- **PerformanceDemonstration_LargePrimeCounting**: Demonstrates Lucy-Hedgehog speedup
- **NthPrime_HandlesEdgeCases**: Edge cases and error conditions
- **CustomOptions_WorkCorrectly**: Validates configuration options

---

## Sample Console Output

The tests produce formatted tables showing performance characteristics:

```
Prime Number Verification Test Results:
+--------------------+-------------------+-------------------+-----------+--------------+--------+
| Index (0-based)    | Expected Prime    | Computed Prime    | Time (ms) | Method Used  | Status |
+--------------------+-------------------+-------------------+-----------+--------------+--------+
| 0                  | 2                 | 2                 | 0         | Regular      | ✓ PASS |
| 1,000,000          | 15,485,867        | 15,485,867        | 45        | Segmented    | ✓ PASS |
| 10,000,000         | 179,424,691       | 179,424,691       | 120       | PrimeCounting| ✓ PASS |
+--------------------+-------------------+-------------------+-----------+--------------+--------+

Large Index Performance Comparison:
+--------------------------------+-------------------+-----------+---------+--------+
| Method                         | Computed Prime    | Time (ms) | Speedup | Status |
+--------------------------------+-------------------+-----------+---------+--------+
| Segmented (Linear)             | 179,424,691       | 15,234    | 1.0x    | ✓ CORRECT |
| Prime Counting (Lucy-Hedgehog) | 179,424,691       | 127       | 120x    | ✓ CORRECT |
+--------------------------------+-------------------+-----------+---------+--------+
```

---

## Key Performance Insights

### Method Selection (Auto Mode)
- **Regular**: Best for indices < 1,000,000 (low overhead, cache-friendly)
- **Segmented**: Best for indices 1M - 10M (memory-efficient scaling)
- **Prime Counting**: Best for indices > 10M (Lucy-Hedgehog algorithm)

### Performance Expectations

| Prime Index | Expected Time | Memory Usage | Auto-Selected Method |
|-------------|---------------|--------------|---------------------|
| 1,000       | < 10ms        | < 1MB        | Regular             |
| 100,000     | < 100ms       | < 50MB       | Regular             |
| 1,000,000   | < 1s          | < 100MB      | Segmented           |
| 10,000,000  | < 10s         | < 200MB      | Prime Counting      |
| 1,000,000,000 | < 60s       | < 50MB       | Prime Counting      |

*Performance varies by hardware and system configuration.*

---

## Running the Tests

```bash
# Run all tests with detailed console output
dotnet test --logger:"console;verbosity=detailed"

# Run only performance demonstrations
dotnet test --filter FullyQualifiedName~PerformanceTests --logger:"console;verbosity=detailed"

# Run specific correctness validation
dotnet test --filter FullyQualifiedName~UnitTest1 --logger:"console;verbosity=detailed"
```

The `--logger:"console;verbosity=detailed"` flag ensures you see the formatted table output and performance insights directly in the console.

---

## Configuration Examples

```csharp
var sieve = new SieveImplementation();

// Auto mode (recommended) - smart method selection
var result = sieve.NthPrime(1_000_000);

// Force specific method for testing
var options = new SieveOptions
{
    Method = SieveMethod.Segmented,
    SegmentSize = 500_000
};
result = sieve.NthPrime(1_000_000, options);

// Custom threshold for auto-selection
var customOptions = new SieveOptions
{
    RegularSieveThreshold = 500_000,
    PrimeCountingThreshold = 5_000_000
};
result = sieve.NthPrime(1_000_000, customOptions);
```

---

## Algorithm Summary

- **API**: `NthPrime(long n0Based)` where `prime[0] = 2`
- **Smart Selection**: Automatically chooses optimal method based on index size
- **Scalability**: Handles indices from 0 to 1+ billion efficiently
- **Lucy-Hedgehog**: Enables billion-scale prime finding without linear scanning

The test suite demonstrates these capabilities with clear, table-formatted output that shows both correctness and performance characteristics.