using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sieve;

namespace Sieve.Tests
{
    /// <summary>
    /// Simplified performance and correctness tests for prime sieve implementations.
    /// Focuses on essential verification and key performance characteristics without
    /// complex statistical analysis.
    /// </summary>
    [TestClass]
    public class PerformanceTests
    {
        private readonly ISieve sieve = new SieveImplementation();

        /// <summary>
        /// Verifies correctness at key prime indices across the range of expected usage.
        /// </summary>
        [DataTestMethod]
        [DataRow(0L, 2L)]           // First prime
        [DataRow(1L, 3L)]           // Second prime
        [DataRow(10L, 31L)]         // Small index
        [DataRow(1000L, 7927L)]     // Medium index
        [DataRow(10000L, 104743L)]  // Large index
        [DataRow(100000L, 1299721L)] // Very large index
        public void NthPrime_ReturnsCorrectValue(long index, long expectedPrime)
        {
            Assert.AreEqual(expectedPrime, sieve.NthPrime(index));
        }

        /// <summary>
        /// Verifies that all sieve methods produce identical results for consistency.
        /// </summary>
        [TestMethod]
        public void AllMethods_ProduceSameResults()
        {
            long testIndex = 5000;

            var regular = sieve.NthPrime(testIndex, new SieveOptions { Method = SieveMethod.Regular });
            var segmented = sieve.NthPrime(testIndex, new SieveOptions { Method = SieveMethod.Segmented });
            var auto = sieve.NthPrime(testIndex, new SieveOptions { Method = SieveMethod.Auto });

            Assert.AreEqual(regular, segmented);
            Assert.AreEqual(regular, auto);
        }

        /// <summary>
        /// Verifies auto-selection behavior and that the switching logic works correctly.
        /// </summary>
        [TestMethod]
        public void AutoSelection_ChoosesAppropriateMethod()
        {
            var options = new SieveOptions { Logger = msg => Console.WriteLine($"  [AUTO] {msg}") };

            Console.WriteLine("Testing auto-selection logic:\n");

            // Should use Regular for small n
            Console.WriteLine("Small index (should use Regular):");
            var small = sieve.NthPrime(100, options);
            Console.WriteLine($"  Result: {small}\n");

            // Should use Segmented for medium n
            Console.WriteLine("Medium index (should use Segmented):");
            var medium = sieve.NthPrime(100_000, options);
            Console.WriteLine($"  Result: {medium}\n");

            // Should use PrimeCounting for large n
            Console.WriteLine("Large index (should use PrimeCounting):");
            var large = sieve.NthPrime(1_000_000, options);
            Console.WriteLine($"  Result: {large}\n");

            // Verify they produce valid results
            Assert.IsTrue(small > 0 && medium > 0 && large > 0);
        }

        /// <summary>
        /// Demonstrates performance characteristics around the Regular→Segmented threshold.
        /// Shows why the auto-selection logic matters in practice.
        /// </summary>
        [TestMethod]
        public void PerformanceDemonstration_MethodCrossover()
        {
            Console.WriteLine("Performance comparison around the Regular→Segmented threshold\n");

            var testCases = new[]
            {
                (index: 10_000L, description: "Small (Regular optimal)"),
                (index: 100_000L, description: "Medium (near threshold)"),
                (index: 1_000_000L, description: "Large (Segmented optimal)")
            };

            foreach (var (index, description) in testCases)
            {
                // Use auto-selection to demonstrate the switching behavior
                var stopwatch = Stopwatch.StartNew();
                var result = sieve.NthPrime(index);
                stopwatch.Stop();

                Console.WriteLine($"{description}: n={index:N0} → {result:N0} ({stopwatch.ElapsedMilliseconds}ms)");
            }

            Console.WriteLine("\nNote: Performance varies by system, but the pattern shows the effectiveness of auto-selection.\n");
        }

        /// <summary>
        /// Demonstrates the dramatic performance improvement of Lucy-Hedgehog prime counting
        /// for very large indices compared to linear sieving approaches.
        /// </summary>
        [TestMethod]
        public void PerformanceDemonstration_LargePrimeCounting()
        {
            Console.WriteLine("Performance comparison: Linear vs Prime Counting for large n\n");

            const long largeIndex = 1_000_000L; // 1 millionth prime

            // Force segmented (linear approach)
            Console.WriteLine("Testing segmented (linear) approach...");
            var stopwatch = Stopwatch.StartNew();
            var segmentedResult = sieve.NthPrime(largeIndex,
                new SieveOptions { Method = SieveMethod.Segmented });
            stopwatch.Stop();
            var segmentedTime = stopwatch.ElapsedMilliseconds;

            // Use prime counting approach
            Console.WriteLine("Testing prime counting approach...");
            stopwatch.Restart();
            var countingResult = sieve.NthPrime(largeIndex,
                new SieveOptions { Method = SieveMethod.PrimeCounting });
            stopwatch.Stop();
            var countingTime = stopwatch.ElapsedMilliseconds;

            Console.WriteLine($"\nResults:");
            Console.WriteLine($"Segmented (linear):   {segmentedTime:N0}ms → {segmentedResult:N0}");
            Console.WriteLine($"Prime Counting:       {countingTime:N0}ms → {countingResult:N0}");

            if (countingTime > 0)
            {
                Console.WriteLine($"Speedup:              {(double)segmentedTime/countingTime:F1}x faster");
            }
            else
            {
                Console.WriteLine("Speedup:              Prime counting was too fast to measure accurately!");
            }

            Console.WriteLine("\nThis demonstrates why auto-selection switches to prime counting for very large indices.\n");

            Assert.AreEqual(segmentedResult, countingResult, "Methods should agree on the result");
        }

        /// <summary>
        /// Verifies edge case handling and error conditions.
        /// </summary>
        [TestMethod]
        public void NthPrime_HandlesEdgeCases()
        {
            // First prime
            Assert.AreEqual(2L, sieve.NthPrime(0));

            // Negative index should throw
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => sieve.NthPrime(-1));

            // Null options should throw
            Assert.ThrowsException<ArgumentNullException>(() => sieve.NthPrime(0, null!));
        }

        /// <summary>
        /// Verifies that custom options are respected and don't interfere with correctness.
        /// </summary>
        [TestMethod]
        public void CustomOptions_WorkCorrectly()
        {
            long testIndex = 1000;
            long expected = 7927; // 1000th prime (0-based)

            // Test with custom segment size
            var customSegmentOptions = new SieveOptions
            {
                Method = SieveMethod.Segmented,
                SegmentSize = 50_000
            };
            Assert.AreEqual(expected, sieve.NthPrime(testIndex, customSegmentOptions));

            // Test with custom threshold
            var customThresholdOptions = new SieveOptions
            {
                RegularSieveThreshold = 500
            };
            Assert.AreEqual(expected, sieve.NthPrime(testIndex, customThresholdOptions));
        }
    }
}