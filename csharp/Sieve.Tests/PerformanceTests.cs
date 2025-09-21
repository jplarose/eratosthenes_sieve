using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ConsoleTables;

namespace Sieve.Tests
{
    /// <summary>
    /// Performance and correctness tests for prime sieve implementations.
    /// These tests measure timing characteristics, validate mathematical correctness,
    /// and help determine optimal configuration parameters.
    /// </summary>
    [TestClass]
    public class PerformanceTests
    {
        // Using the optimized SieveImplementation for all performance measurements
        private readonly ISieve sieve = new SieveImplementation();

        // ═══════════════════════════════════════════════════════════════════════════════════
        // CORRECTNESS VERIFICATION TESTS
        // These tests verify that both sieve methods produce mathematically correct results
        // across a range of prime indices, ensuring reliability before performance analysis.
        // ═══════════════════════════════════════════════════════════════════════════════════
        /// <summary>
        /// Verifies correctness of both Regular and Segmented sieve methods at key prime indices.
        /// This ensures mathematical accuracy before running performance comparisons.
        /// </summary>
        /// <param name="primeIndex">Zero-based index of the prime to find (0 → 2nd prime, 1 → 3rd prime, etc.)</param>
        /// <param name="expectedPrime">The mathematically correct prime value at this index</param>
        [DataTestMethod]
        [DataRow(0L, 2L)]       // First prime: index 0 → 2
        [DataRow(1L, 3L)]       // Second prime: index 1 → 3
        [DataRow(5L, 13L)]      // Sixth prime: index 5 → 13
        [DataRow(10_000L, 104_743L)]    // 10,001st prime
        [DataRow(100_000L, 1_299_721L)] // 100,001st prime
        public void VerifyCorrectness_AtKnownPrimeIndices(long primeIndex, long expectedPrime)
        {
            Title("CORRECTNESS • MATHEMATICAL VERIFICATION");
            Console.WriteLine($"Verifying prime at index {primeIndex:N0} → expected value {expectedPrime:N0}");

            // Test default mode (Auto) - should pick optimal method automatically
            Assert.AreEqual(expectedPrime, sieve.NthPrime(primeIndex));

            // Test Regular sieve method explicitly
            Assert.AreEqual(expectedPrime, sieve.NthPrime(primeIndex, new SieveOptions { Method = SieveMethod.Regular }));

            // Test Segmented sieve method explicitly
            Assert.AreEqual(expectedPrime, sieve.NthPrime(primeIndex, new SieveOptions { Method = SieveMethod.Segmented }));

            Console.WriteLine("  ✓ All methods produced correct result\n");
        }

        // ═══════════════════════════════════════════════════════════════════════════════════
        // PERFORMANCE COMPARISON TESTS
        // These tests measure and compare the execution time of Regular vs Segmented methods
        // using statistical analysis (median times and interquartile ranges for reliability).
        // ═══════════════════════════════════════════════════════════════════════════════════
        /// <summary>
        /// Compares Regular vs Segmented sieve performance at specific prime indices.
        /// Uses robust statistical measurement (median of multiple trials) to account for
        /// system noise and provides interquartile range to assess timing consistency.
        /// </summary>
        /// <param name="primeIndex">Zero-based index of the prime to find</param>
        /// <param name="expectedPrime">Expected prime value (for correctness verification)</param>
        [DataTestMethod]
        [DataRow(19L, 71L)]           // Small index - Regular should win
        [DataRow(500L, 3_581L)]       // Small-medium index
        [DataRow(2_000L, 17_393L)]    // Medium index - approaching crossover
        [DataRow(100_000L, 1_299_721L)] // Large index - Segmented may become competitive
        public void CompareMethodPerformance_AtSpecificIndices(long primeIndex, long expectedPrime)
        {
            Title("PERFORMANCE COMPARISON • REGULAR VS SEGMENTED");
            Console.WriteLine($"Prime index {primeIndex:N0} (expected value: {expectedPrime:N0})");
            Console.WriteLine("Lower median times indicate faster performance. Lower interquartile range indicates more consistent timing.\n");

            // Warm up JIT compilation and cache for both methods before timing
            WarmupSieveMethod(() => sieve.NthPrime(primeIndex, new SieveOptions { Method = SieveMethod.Regular }), 3);
            WarmupSieveMethod(() => sieve.NthPrime(primeIndex, new SieveOptions { Method = SieveMethod.Segmented }), 3);

            // Measure performance using statistical analysis (21 trials, use median to reduce noise)
            var (regularMedian, regularIQR) = MeasureExecutionTimeStatistics(21, () =>
                sieve.NthPrime(primeIndex, new SieveOptions { Method = SieveMethod.Regular }));
            var (segmentedMedian, segmentedIQR) = MeasureExecutionTimeStatistics(21, () =>
                sieve.NthPrime(primeIndex, new SieveOptions { Method = SieveMethod.Segmented }));

            // Determine winner and calculate performance ratio
            var fasterMethod = regularMedian <= segmentedMedian ? "Regular" : "Segmented";
            var performanceRatio = segmentedMedian / Math.Max(1e-9, regularMedian);

            // Display results in formatted table
            var table = new ConsoleTable("Method", "Median Time (ms)", "Interquartile Range (ms)");
            table.AddRow("Regular", $"{regularMedian:F1}", $"{regularIQR:F1}");
            table.AddRow("Segmented", $"{segmentedMedian:F1}", $"{segmentedIQR:F1}");
            table.Write();
            Console.WriteLine($"Faster method: {fasterMethod}  |  Speed ratio (segmented/regular): {performanceRatio:F2}\n");

            // Verify both methods produce correct mathematical results
            Assert.AreEqual(expectedPrime, sieve.NthPrime(primeIndex, new SieveOptions { Method = SieveMethod.Regular }),
                "Regular method produced incorrect result");
            Assert.AreEqual(expectedPrime, sieve.NthPrime(primeIndex, new SieveOptions { Method = SieveMethod.Segmented }),
                "Segmented method produced incorrect result");
        }

        // ═══════════════════════════════════════════════════════════════════════════════════
        // SEGMENT SIZE OPTIMIZATION TESTS
        // These tests measure how different segment sizes affect performance of the segmented
        // sieve method. Helps identify optimal memory/cache tradeoffs for the target system.
        // ═══════════════════════════════════════════════════════════════════════════════════
        /// <summary>
        /// Tests segmented sieve performance across different segment sizes to find optimal
        /// memory/cache configuration. Segment size affects both memory usage and cache locality.
        /// </summary>
        /// <param name="segmentSize">Number of integers to process in each memory segment</param>
        [DataTestMethod]
        [DataRow(50_000)]     // Small segments - less memory, more overhead
        [DataRow(100_000)]    //
        [DataRow(500_000)]    // Medium segments - balanced approach
        [DataRow(1_000_000)]  // Large segments - our current default
        [DataRow(2_000_000)]  // Very large segments - may exceed cache
        public void MeasureSegmentedSieve_PerformanceBySegmentSize(int segmentSize)
        {
            // Test against the 10 millionth prime (large enough to show segment size effects)
            const long targetPrimeIndex = 10_000_000;
            const long expectedPrimeValue = 179_424_691;

            Title("SEGMENT SIZE OPTIMIZATION • PERFORMANCE ANALYSIS");
            Console.WriteLine($"Finding prime at index {targetPrimeIndex:N0} using segment size {segmentSize:N0}");
            Console.WriteLine("Optimal segment sizes balance memory usage and cache efficiency. Lower median times indicate better performance.\n");

            // Configure segmented sieve with specific segment size
            var sieveOptions = new SieveOptions { Method = SieveMethod.Segmented, SegmentSize = segmentSize };

            // Warm up to ensure JIT compilation and cache loading
            WarmupSieveMethod(() => sieve.NthPrime(targetPrimeIndex, sieveOptions), 2);

            // Measure performance (11 trials for statistical reliability)
            var (medianTime, timeVariability) = MeasureExecutionTimeStatistics(11, () =>
                sieve.NthPrime(targetPrimeIndex, sieveOptions));

            // Verify correctness
            var computedResult = sieve.NthPrime(targetPrimeIndex, sieveOptions);
            var isCorrect = computedResult == expectedPrimeValue;

            // Display results
            var table = new ConsoleTable("Segment Size", "Median Time (ms)", "Interquartile Range (ms)", "Correctness");
            table.AddRow(segmentSize.ToString("N0"), $"{medianTime:F1}", $"{timeVariability:F1}",
                (isCorrect ? "✓" : "✗"));
            table.Write();
            Console.WriteLine();

            Assert.AreEqual(expectedPrimeValue, computedResult,
                $"Segmented sieve with segment size {segmentSize:N0} produced incorrect result");
        }

        // ═══════════════════════════════════════════════════════════════════════════════════
        // AUTO-SELECTION THRESHOLD VALIDATION TESTS
        // These tests verify that the automatic method selection (Auto mode) produces identical
        // results to manually forced methods at the threshold boundary points.
        // ═══════════════════════════════════════════════════════════════════════════════════
        /// <summary>
        /// Validates that Auto mode correctly selects Regular vs Segmented methods at threshold boundaries.
        /// This ensures the automatic selection logic matches manual method forcing for consistency.
        /// </summary>
        /// <param name="thresholdValue">The RegularSieveThreshold value to test boundary behavior</param>
        [DataTestMethod]
        [DataRow(10_000L)]   // Small threshold
        [DataRow(50_000L)]   // Medium threshold
        [DataRow(100_000L)]  // Large threshold
        [DataRow(500_000L)]  // Very large threshold
        public void ValidateAutoMethodSelection_AtThresholdBoundaries(long thresholdValue)
        {
            Title("AUTO THRESHOLD VALIDATION • BOUNDARY TESTING");
            Console.WriteLine($"Testing threshold boundary at {thresholdValue:N0}");
            Console.WriteLine("Auto mode should produce identical results to manually forced methods at the threshold boundary.\n");

            // Test index just below threshold (should use Regular method)
            long indexBelowThreshold = Math.Max(0, thresholdValue - 1);
            var autoResultBelow = sieve.NthPrime(indexBelowThreshold,
                new SieveOptions { RegularSieveThreshold = thresholdValue });
            var forcedResultBelow = sieve.NthPrime(indexBelowThreshold, new SieveOptions
            {
                Method = indexBelowThreshold < thresholdValue ? SieveMethod.Regular : SieveMethod.Segmented
            });

            // Test index at threshold (should use Segmented method)
            long indexAtThreshold = thresholdValue;
            var autoResultAt = sieve.NthPrime(indexAtThreshold,
                new SieveOptions { RegularSieveThreshold = thresholdValue });
            var forcedResultAt = sieve.NthPrime(indexAtThreshold, new SieveOptions
            {
                Method = indexAtThreshold < thresholdValue ? SieveMethod.Regular : SieveMethod.Segmented
            });

            // Display comparison results
            var table = new ConsoleTable("Test Case", "Auto Mode Result", "Forced Method Result", "Results Match");
            table.AddRow($"Index {indexBelowThreshold:N0} (below threshold)",
                autoResultBelow.ToString("N0"), forcedResultBelow.ToString("N0"),
                ResultsMatch(autoResultBelow, forcedResultBelow));
            table.AddRow($"Index {indexAtThreshold:N0} (at threshold)",
                autoResultAt.ToString("N0"), forcedResultAt.ToString("N0"),
                ResultsMatch(autoResultAt, forcedResultAt));
            table.Write();
            Console.WriteLine();

            // Verify that auto selection matches forced selection
            Assert.AreEqual(forcedResultBelow, autoResultBelow,
                $"Auto mode result below threshold ({indexBelowThreshold:N0}) doesn't match forced Regular method");
            Assert.AreEqual(forcedResultAt, autoResultAt,
                $"Auto mode result at threshold ({indexAtThreshold:N0}) doesn't match forced Segmented method");
        }

        // ═══════════════════════════════════════════════════════════════════════════════════
        // CROSSOVER POINT ANALYSIS
        // Advanced performance analysis to find the prime index where Segmented method
        // becomes consistently faster than Regular method. Uses statistical techniques
        // including outlier resistance and persistence requirements for reliable results.
        // ═══════════════════════════════════════════════════════════════════════════════════
        /// <summary>
        /// Performs comprehensive analysis to find the crossover point where Segmented sieve
        /// becomes persistently faster than Regular sieve. Uses multiple measurement ranges,
        /// statistical smoothing, and persistence requirements to ensure reliable results.
        /// </summary>
        [TestMethod]
        public void AnalyzeCrossoverPoint_WithStatisticalRobustness()
        {
            Title("CROSSOVER POINT • ROBUST ANALYSIS (Median/IQR + smoothing + persistence)");
            Console.WriteLine("We search coarse→fine, compute medians, smooth ratios, and require a persistent win for Segmented.\n");

            var ranges = new (long start, long end, long step)[] {
                (50_000, 150_000, 10_000),
                (80_000, 120_000,  5_000),
                (90_000, 110_000,  2_000)
            };

            const int Warmups = 4;
            const int Trials = 33;

            // Smoothing + decision parameters
            const int SmoothWindow = 5;      // moving median window over ratio
            const double Margin = 0.98;      // require seg/reg ratio < 0.98 to count as “clearly faster” (~2%+ faster)
            const int PersistQ = 5;          // lookahead window size
            const int PersistK = 3;          // need at least K of next Q to be wins (smoothed ratio < Margin)

            // Collect all measurement points across all ranges for analysis
            var allMeasurements = new List<PerformanceMeasurement>();

            // Execute measurements across all defined ranges
            foreach (var (start, end, step) in measurementRanges)
            {
                Subtitle($"Searching {start:N0}..{end:N0} (step {step:N0})");
                Console.WriteLine("Lower ratios indicate segmented method is becoming more competitive relative to regular method.\n");
                var table = new ConsoleTable("Prime Index", "Regular Median (ms)", "Regular Interquartile Range (ms)", "Segmented Median (ms)", "Segmented Interquartile Range (ms)", "Speed Ratio", "Faster Method");
                for (long n0 = start; n0 <= end; n0 += step)
                {
                    // Warmups outside timing regions
                    for (int i = 0; i < Warmups; i++)
                    {
                        sieve.NthPrime(n0, new SieveOptions { Method = SieveMethod.Regular });
                        sieve.NthPrime(n0, new SieveOptions { Method = SieveMethod.Segmented });
                    }

                    var (regMed, regIqr) = MeasureMedianIqrMs(Trials, () => sieve.NthPrime(n0, new SieveOptions { Method = SieveMethod.Regular }));
                    var (segMed, segIqr) = MeasureMedianIqrMs(Trials, () => sieve.NthPrime(n0, new SieveOptions { Method = SieveMethod.Segmented }));

                    double ratio = segMed / Math.Max(1e-9, regMed);
                    var winner = regMed <= segMed ? "Regular" : "Segmented";

                    table.AddRow(n0.ToString("N0"),
                        $"{regMed:F1}", $"{regIqr:F1}",
                        $"{segMed:F1}", $"{segIqr:F1}",
                        $"{ratio:F2}", winner);

                    all.Add(new Point(n0, regMed, regIqr, segMed, segIqr, ratio));
                }
                table.Write();
                Console.WriteLine();
            }

            // Smooth the ratio curve with moving median (outlier-resistant)
            var smoothed = MovingMedian(all.Select(p => p.Ratio).ToArray(), SmoothWindow);
            for (int i = 0; i < all.Count; i++) all[i] = all[i] with { SmoothRatio = smoothed[i] };

            // Find the first “persistent crossing” point:
            // smoothed ratio < Margin AND at least K of next Q points also < Margin
            long? persistent = null;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].SmoothRatio < Margin)
                {
                    int wins = 0;
                    for (int j = i; j < Math.Min(all.Count, i + PersistQ); j++)
                        if (all[j].SmoothRatio < Margin) wins++;

                    if (wins >= PersistK)
                    {
                        persistent = all[i].N;
                        break;
                    }
                }
            }

            // Fallback: if no persistent crossing, pick the global minimum of the smoothed ratio
            var argmin = all.OrderBy(p => p.SmoothRatio).First();
            long recommended = persistent ?? argmin.N;

            // Pretty summary block
            Subtitle("Summary • Crossover Decision");
            Console.WriteLine($"  Smoothing: moving median window = {SmoothWindow}");
            Console.WriteLine($"  Persistence: need {PersistK} of next {PersistQ} points to keep Segmented faster (ratio < {Margin:F2})");
            Console.WriteLine($"  First persistent crossing: {(persistent.HasValue ? persistent.Value.ToString("N0") : "none")}");
            Console.WriteLine($"  Global best (smoothed): n={argmin.N:N0}, smooth ratio={argmin.SmoothRatio:F3}");
            Console.WriteLine($"\n  ⇒ Recommended threshold: {recommended:N0}\n");

            Assert.IsTrue(all.Any(), "No data points were measured.");
            // Do not force presence of crossing; some machines may keep Regular faster in this narrow band.
        }

        /// <summary>
        /// Validates the first 10,000 primes against a reference dataset to ensure
        /// mathematical correctness across a comprehensive range. Shows only failures
        /// to keep output manageable while providing complete verification coverage.
        /// </summary>
        [TestMethod]
        public void ValidateFirst10000Primes_AgainstReferenceData()
        {
            Title("FIXTURES • FIRST 10,000 PRIMES");
            var path = Path.Combine("fixtures", "primes_10k.csv");
            Assert.IsTrue(File.Exists(path), $"Missing fixture: {path}");

            var rows = File.ReadAllLines(path)
                           .Skip(1)
                           .Select(l => l.Split(','))
                           .Select(a => (idx: int.Parse(a[0]), prime: long.Parse(a[1])))
                           .ToArray();

            Subtitle($"Loaded {rows.Length:N0} fixture primes from {path}");
            Console.WriteLine("Verifying all 10,000 primes against reference data (showing only failures)...\n");

            bool allMatch = true;
            var failures = new List<(int index, long expected, long actual)>();

            for (int i = 0; i < rows.Length; i++)
            {
                var lib = sieve.NthPrime(i);
                bool ok = lib == rows[i].prime;
                if (!ok)
                {
                    allMatch = false;
                    failures.Add((i, rows[i].prime, lib));
                }
            }

            if (failures.Any())
            {
                var table = new ConsoleTable("Index (0-based)", "Expected Prime", "Computed Prime", "Status");
                foreach (var (index, expected, actual) in failures)
                {
                    table.AddRow(index.ToString("N0"), expected.ToString("N0"), actual.ToString("N0"), "✗ FAIL");
                }
                table.Write();
            }
            else
            {
                Console.WriteLine("✓ All individual verifications passed - no failures to display.");
            }

            Console.WriteLine();
            Console.WriteLine(allMatch ? "✓ All first 10,000 primes match the fixture.\n"
                                       : "✗ Mismatch detected in first 10,000 primes.\n");

            Assert.IsTrue(allMatch, "First 10,000 primes did not match the fixture exactly.");
        }

        /// <summary>
        /// Validates mathematical correctness using checkpoint data for both:
        /// 1. Prime counting function π(x) - count of primes ≤ x
        /// 2. Prime sequence pₙ - the nth prime number
        /// These tests verify against established mathematical references.
        /// </summary>
        [TestMethod]
        public void ValidateMathematicalCorrectness_PrimeCountingAndSequence()
        {
            Title("FIXTURES • CHECKPOINT SWEEP: π(x) and pₙ");
            var path = Path.Combine("fixtures", "prime_checkpoints.json");
            Assert.IsTrue(File.Exists(path), $"Missing fixture: {path}");

            var json = File.ReadAllText(path);
            var ck = System.Text.Json.JsonSerializer.Deserialize<Checkpoints>(json, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            })!;
            Assert.IsNotNull(ck, "Could not parse prime_checkpoints.json");

            // Prime counting function verification — do a small in-test sieve up to max X
            int maxX = ck.pi_checkpoints.Max(p => p.x);
            var primesUpToMax = LocalGeneratePrimesUpTo(maxX); // fast & local to the test
            Subtitle($"Prime counting function π(x) verification (x ≤ {maxX:N0})");
            Console.WriteLine("Prime counting function π(x) returns the number of primes less than or equal to x.\n");
            var piTable = new ConsoleTable("Upper Limit (x)", "Expected Count π(x)", "Computed Count π(x)", "Results Match");
            bool allPiOk = true;
            foreach (var p in ck.pi_checkpoints.OrderBy(p => p.x))
            {
                int computed = CountPrimesLE(primesUpToMax, p.x);
                bool ok = (computed == p.pi_x);
                if (!ok) allPiOk = false;
                piTable.AddRow(p.x.ToString("N0"), p.pi_x.ToString("N0"), computed.ToString("N0"), ok ? "✓" : "✗");
            }
            piTable.Write();
            Console.WriteLine(allPiOk ? "  ✓ Prime counting function checkpoints matched\n" : "  ✗ Prime counting function mismatch\n");

            // Prime sequence verification — use the library API for nth prime (0-based)
            Subtitle("Prime sequence value checks (1-based n; API uses 0-based index)");
            Console.WriteLine("Verifying that the nth prime in sequence matches expected values from mathematical references.\n");
            var pnTable = new ConsoleTable("Position (1-based)", "Expected Prime Value", "Library Prime Value", "API Index (0-based)", "Results Match");
            bool allPnOk = true;
            foreach (var p in ck.p_n_checkpoints.OrderBy(p => p.n_1_based))
            {
                var lib = sieve.NthPrime(p.index_0_based);
                bool ok = lib == p.p_n;
                if (!ok) allPnOk = false;
                pnTable.AddRow(p.n_1_based.ToString("N0"), p.p_n.ToString("N0"), lib.ToString("N0"),
                    p.index_0_based.ToString("N0"), ok ? "✓" : "✗");
            }
            pnTable.Write();
            Console.WriteLine(allPnOk ? "  ✓ Prime sequence checkpoints matched\n" : "  ✗ Prime sequence mismatch\n");

            Assert.IsTrue(allPiOk && allPnOk, "One or more checkpoint validations failed.");
        }

        // ===== Local helpers for the checkpoint test =====

        private static List<int> LocalGeneratePrimesUpTo(int n)
        {
            if (n < 2) return new List<int>();
            var isPrime = new bool[n + 1];
            Array.Fill(isPrime, true);
            isPrime[0] = isPrime[1] = false;
            int r = (int)Math.Sqrt(n);
            for (int p = 2; p <= r; p++)
                if (isPrime[p])
                    for (int m = p * p; m <= n; m += p)
                        isPrime[m] = false;
            var list = new List<int>();
            for (int i = 2; i <= n; i++) if (isPrime[i]) list.Add(i);
            return list;
        }

        private static int CountPrimesLE(List<int> sortedPrimes, int x)
        {
            // Binary search upper bound
            int lo = 0, hi = sortedPrimes.Count; // [lo, hi)
            while (lo < hi)
            {
                int mid = (lo + hi) >> 1;
                if (sortedPrimes[mid] <= x) lo = mid + 1;
                else hi = mid;
            }
            return lo;
        }

        private record Checkpoints(PiPoint[] pi_checkpoints, PnPoint[] p_n_checkpoints);
        private record PiPoint(int x, int pi_x);
        private record PnPoint(int n_1_based, int index_0_based, long p_n);


        // ═══════════════════════════════════════════════════════════════════════════════════
        // DATA STRUCTURES AND HELPER METHODS
        // Supporting types and utility methods for performance measurement and analysis
        // ═══════════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Represents a single performance measurement comparing Regular vs Segmented sieve methods.
        /// </summary>
        private record struct PerformanceMeasurement(
            long PrimeIndex,
            double RegularMedianMs,
            double RegularInterquartileRangeMs,
            double SegmentedMedianMs,
            double SegmentedInterquartileRangeMs,
            double PerformanceRatio)
        {
            /// <summary>
            /// Performance ratio after statistical smoothing to reduce measurement noise.
            /// </summary>
            public double SmoothedRatio { get; set; }
        }

        private record struct Point(long N, double RegMed, double RegIqr, double SegMed, double SegIqr, double Ratio)
        {
            public double SmoothRatio { get; set; }
        }

        /// <summary>
        /// Warms up a sieve method by executing it multiple times to ensure JIT compilation
        /// and cache loading before performance measurement.
        /// </summary>
        /// <param name="sieveOperation">The sieve operation to warm up</param>
        /// <param name="iterations">Number of warmup iterations (default: 1)</param>
        private static void WarmupSieveMethod(Func<long> sieveOperation, int iterations = 1)
        {
            for (int i = 0; i < iterations; i++)
            {
                sieveOperation();
            }
        }

        private static void Warmup(Func<long> action, int iters = 1)
        {
            for (int i = 0; i < iters; i++) action();
        }

        /// <summary>
        /// Measures execution time statistics for a sieve operation using multiple trials.
        /// Returns median time and interquartile range for robust performance analysis.
        /// Uses high-precision timing and excludes I/O from measurement region.
        /// </summary>
        /// <param name="trials">Number of measurement trials (should be odd for clean median)</param>
        /// <param name="sieveOperation">The sieve operation to measure</param>
        /// <returns>Tuple of (median time in ms, interquartile range in ms)</returns>
        private static (double medianMs, double interquartileRangeMs) MeasureExecutionTimeStatistics(int trials, Func<long> sieveOperation)
        {
            if (trials <= 0)
                throw new ArgumentOutOfRangeException(nameof(trials), "Must have at least 1 trial");

            // Perform garbage collection to minimize GC interference during timing
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Execute one untimed call to ensure JIT compilation and tiering are complete
            sieveOperation();

            // Collect timing measurements
            var timingResults = new double[trials];
            double stopwatchFrequency = Stopwatch.Frequency;

            for (int trial = 0; trial < trials; trial++)
            {
                long startTimestamp = Stopwatch.GetTimestamp();
                sieveOperation();
                long endTimestamp = Stopwatch.GetTimestamp();

                // Convert to milliseconds
                timingResults[trial] = (endTimestamp - startTimestamp) * 1000.0 / stopwatchFrequency;
            }

            // Sort for percentile calculations
            Array.Sort(timingResults);

            // Calculate robust statistics (median and IQR)
            var medianTime = CalculatePercentile(timingResults, 50);
            var q25 = CalculatePercentile(timingResults, 25);
            var q75 = CalculatePercentile(timingResults, 75);
            var interquartileRange = q75 - q25;

            return (medianTime, interquartileRange);
        }

        /// Measures action 'trials' times, returning (medianMs, iqrMs). No I/O inside.
        private static (double medianMs, double iqrMs) MeasureMedianIqrMs(int trials, Func<long> action)
        {
            if (trials <= 0) throw new ArgumentOutOfRangeException(nameof(trials));

            // Perform garbage collection to minimize GC interference during timing
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            // Execute one untimed call to ensure JIT compilation and tiering are complete
            sieveOperation();

            // Collect timing measurements
            var timingResults = new double[trials];
            double stopwatchFrequency = Stopwatch.Frequency;

            for (int trial = 0; trial < trials; trial++)
            {
                long startTimestamp = Stopwatch.GetTimestamp();
                sieveOperation();
                long endTimestamp = Stopwatch.GetTimestamp();

                // Convert to milliseconds
                timingResults[trial] = (endTimestamp - startTimestamp) * 1000.0 / stopwatchFrequency;
            }

            // Sort for percentile calculations
            Array.Sort(timingResults);

            // Calculate robust statistics (median and IQR)
            var medianTime = CalculatePercentile(timingResults, 50);
            var q25 = CalculatePercentile(timingResults, 25);
            var q75 = CalculatePercentile(timingResults, 75);
            var interquartileRange = q75 - q25;

            return (medianTime, interquartileRange);
        }

        /// <summary>
        /// Calculates the specified percentile from a sorted array of values.
        /// Uses linear interpolation for percentiles that fall between array indices.
        /// </summary>
        /// <param name="sortedValues">Array of values sorted in ascending order</param>
        /// <param name="percentile">Percentile to calculate (0-100)</param>
        /// <returns>The calculated percentile value</returns>
        private static double CalculatePercentile(double[] sortedValues, double percentile)
        {
            if (sortedValues.Length == 0)
                return double.NaN;
            if (percentile <= 0)
                return sortedValues[0];
            if (percentile >= 100)
                return sortedValues[^1];

            // Calculate the rank (position) in the sorted array
            double rank = (percentile / 100.0) * (sortedValues.Length - 1);
            int lowerIndex = (int)Math.Floor(rank);
            int upperIndex = (int)Math.Ceiling(rank);

            // If rank falls exactly on an index, return that value
            if (lowerIndex == upperIndex)
                return sortedValues[lowerIndex];

            // Otherwise, linearly interpolate between the two surrounding values
            double fraction = rank - lowerIndex;
            return sortedValues[lowerIndex] + (sortedValues[upperIndex] - sortedValues[lowerIndex]) * fraction;
        }

        /// <summary>
        /// Applies moving median smoothing to reduce noise in performance ratio measurements.
        /// Each point is replaced by the median of surrounding points within the window.
        /// This provides outlier-resistant smoothing for trend identification.
        /// </summary>
        /// <param name="data">Array of performance ratios to smooth</param>
        /// <param name="windowSize">Size of the moving window (should be odd for symmetric smoothing)</param>
        /// <returns>Array of smoothed values</returns>
        private static double[] ApplyMovingMedianSmoothing(double[] data, int windowSize)
        {
            if (windowSize <= 1)
                return data.ToArray(); // No smoothing needed

            int dataLength = data.Length;
            var smoothedData = new double[dataLength];
            int halfWindow = windowSize / 2;

            // Create reusable buffer for window values
            var windowValues = new List<double>(windowSize);

            for (int centerIndex = 0; centerIndex < dataLength; centerIndex++)
            {
                // Clear buffer and collect values within window around center point
                windowValues.Clear();

                int windowStart = Math.Max(0, centerIndex - halfWindow);
                int windowEnd = Math.Min(dataLength - 1, centerIndex + halfWindow);

                for (int windowIndex = windowStart; windowIndex <= windowEnd; windowIndex++)
                {
                    windowValues.Add(data[windowIndex]);
                }

                // Sort and take median
                windowValues.Sort();
                smoothedData[centerIndex] = windowValues[windowValues.Count / 2];
            }

            return smoothedData;
        }

        private static double[] MovingMedian(double[] data, int window)
        {
            if (window <= 1) return data.ToArray();

            int dataLength = data.Length;
            var smoothedData = new double[dataLength];
            int halfWindow = windowSize / 2;

            // Create reusable buffer for window values
            var windowValues = new List<double>(windowSize);

            for (int centerIndex = 0; centerIndex < dataLength; centerIndex++)
            {
                // Clear buffer and collect values within window around center point
                windowValues.Clear();

                int windowStart = Math.Max(0, centerIndex - halfWindow);
                int windowEnd = Math.Min(dataLength - 1, centerIndex + halfWindow);

                for (int windowIndex = windowStart; windowIndex <= windowEnd; windowIndex++)
                {
                    windowValues.Add(data[windowIndex]);
                }

                // Sort and take median
                windowValues.Sort();
                smoothedData[centerIndex] = windowValues[windowValues.Count / 2];
            }

            return smoothedData;
        }

        // ─────────────────────── Console formatting helpers ───────────────────
        private static void Title(string text)
        {
            Console.WriteLine();
            Console.WriteLine("╔══════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine($"║ {text.PadRight(68)} ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════════════╝");
        }

        private static void Subtitle(string text)
        {
            Console.WriteLine($"\n— {text} —");
        }


        /// <summary>
        /// Compares two results and returns a visual indicator of whether they match.
        /// </summary>
        /// <param name="result1">First result to compare</param>
        /// <param name="result2">Second result to compare</param>
        /// <returns>"✓" if results match, "✗" if they differ</returns>
        private static string ResultsMatch(long result1, long result2) => result1 == result2 ? "✓" : "✗";

        private static string Eq(long a, long b) => a == b ? "✓" : "✗";
    }
}
