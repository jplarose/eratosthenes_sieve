# Performance Testing Framework

I include a small performance harness to benchmark and tune this Sieve of Eratosthenes implementation.

## Key Features

### 1. Configurable Implementation

The `SieveImplementation` class supports multiple algorithms and configuration options:

```csharp
// Auto mode (default) - chooses a method based on RegularSieveThreshold
var sieve = new SieveImplementation();
var result = sieve.NthPrime(1_000_000);

// Force a specific method
var options = new SieveOptions
{
    Method = SieveMethod.Regular,   // or SieveMethod.Segmented
    SegmentSize = 500_000,          // segment size for segmented method
    RegularSieveThreshold = 100_000 // threshold used by Auto
};
result = sieve.NthPrime(1_000_000, options);

// Optional: surface advisory messages without changing behavior
options.Logger = Console.WriteLine; // e.g., warns if a forced method is likely suboptimal
result = sieve.NthPrime(5, options);
```

### 2. Available Sieve Methods

- **Regular Sieve**: Traditional Sieve of Eratosthenes that generates all primes up to an estimated upper bound
- **Segmented Sieve**: Memory-efficient approach that processes numbers in segments
- **Auto Mode**: Automatically selects the method based on `RegularSieveThreshold`

### 3. Performance Test Suites

#### PerformanceTests.cs
- `TestOptimalThresholdAnalysis()`: Compares regular vs segmented performance across different prime indices
- `TestSegmentSizeOptimization()`: Finds optimal segment sizes for segmented sieve
- `TestMethodPerformanceComparison()`: Validates performance expectations with time limits
- `TestLargeScalePerformance()`: Tests extreme cases (100M+ primes)
- `TestAutoModeThresholdValidation()`: Optimizes automatic method selection thresholds

#### BenchmarkTests.cs
- `BenchmarkRegularSieveMemoryLimits()`: Finds breaking points where regular sieve fails
- `BenchmarkSegmentSizeImpact()`: Detailed analysis of segment size impact on performance
- `BenchmarkCrossoverPoint()`: Pinpoints exact threshold where segmented becomes faster
- `BenchmarkExtremeCase()`: Stress test with 100 millionth prime
- `BenchmarkMemoryEfficiency()`: Memory usage comparison between methods

#### DemoTests.cs
- `DemoConfigurableImplementation()`: Shows all configuration options
- `DemoPerformanceComparison()`: Live comparison of method performance
- `DemoThresholdFinding()`: Interactive threshold optimization

## Running Performance Tests

All commands below include `--logger:"console;verbosity=detailed"` so you see test output directly in the console.

### Run All Tests
```bash
dotnet test --logger:"console;verbosity=detailed"
```

### Run Specific Test Categories
```bash
# Performance analysis
dotnet test --filter FullyQualifiedName~PerformanceTests --logger:"console;verbosity=detailed"

# Benchmarking
dotnet test --filter FullyQualifiedName~BenchmarkTests --logger:"console;verbosity=detailed"

# Demos
dotnet test --filter FullyQualifiedName~DemoTests --logger:"console;verbosity=detailed"
```

### Run Individual Tests
```bash
# Threshold analysis
dotnet test --filter FullyQualifiedName~TestOptimalThresholdAnalysis --logger:"console;verbosity=detailed"

# Memory limit testing
dotnet test --filter FullyQualifiedName~BenchmarkRegularSieveMemoryLimits --logger:"console;verbosity=detailed"

# Configuration demo
dotnet test --filter FullyQualifiedName~DemoConfigurableImplementation --logger:"console;verbosity=detailed"
```

**Note:** The `--logger:"console;verbosity=detailed"` flag is used to display detailed test output in the console. This flag can be removed or modified to change the verbosity level.

## Key Performance Insights

### Memory Constraints
- Regular sieve hits memory limits around 1–5 million primes (depends on your system)
- Segmented sieve can handle 100+ million primes with constant memory usage

### Performance Crossover
- Regular sieve is faster for indices < ~100,000
- Segmented sieve becomes faster for larger indices due to better memory locality
- Crossover point varies by system but typically lands around 50,000–200,000

### Optimal Configurations
- **Segment Size**: 1–2 million is a good balance of memory and speed
- **Auto Threshold**: 100,000–500,000 works well on most systems
- **Large Computations**: Use larger segments (2–5 million) for very large prime indices

## Example Usage for Optimization

```csharp
// Try different configurations to find what runs best on your machine
var sieve = new SieveImplementation();

// Find crossover point for your system
for (long n = 50_000; n <= 200_000; n += 10_000)
{
    var regularTime = MeasureTime(() =>
        sieve.NthPrime(n, new SieveOptions { Method = SieveMethod.Regular }));

    var segmentedTime = MeasureTime(() =>
        sieve.NthPrime(n, new SieveOptions { Method = SieveMethod.Segmented }));

    Console.WriteLine($"{n}: Regular={regularTime}ms, Segmented={segmentedTime}ms");
}

// Test segment sizes for your typical workload
var testSizes = new[] { 100_000, 500_000, 1_000_000, 2_000_000 };
foreach (var size in testSizes)
{
    var time = MeasureTime(() => sieve.NthPrime(1_000_000,
        new SieveOptions { Method = SieveMethod.Segmented, SegmentSize = size }));
    Console.WriteLine($"Segment {size}: {time}ms");
}
```

## Performance Expectations

Based on testing, here’s what I generally see:

| Prime Index | Regular Method | Segmented Method | Memory Usage |
|-------------|----------------|------------------|--------------|
| 10,000      | < 50ms         | < 100ms          | < 10MB       |
| 100,000     | < 500ms        | < 200ms          | < 100MB      |
| 1,000,000   | < 10s          | < 2s             | < 500MB      |
| 10,000,000  | OOM/Very Slow  | < 30s            | < 100MB      |
| 100,000,000 | Not Feasible   | < 10min          | < 200MB      |

*Your results will vary based on hardware and OS configuration.*