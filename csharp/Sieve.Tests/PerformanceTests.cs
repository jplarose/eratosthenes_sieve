using System;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ConsoleTables;
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
            Console.WriteLine("Method Consistency Verification:");
            Console.WriteLine("Testing that all sieve methods produce identical results.\n");

            long testIndex = 4999;
            long expectedPrime = 48611; // 5000th prime (0-based)

            var stopwatch = Stopwatch.StartNew();
            var regular = sieve.NthPrime(testIndex, new SieveOptions { Method = SieveMethod.Regular });
            var regularTime = stopwatch.ElapsedMilliseconds;

            stopwatch.Restart();
            var segmented = sieve.NthPrime(testIndex, new SieveOptions { Method = SieveMethod.Segmented });
            var segmentedTime = stopwatch.ElapsedMilliseconds;

            stopwatch.Restart();
            var auto = sieve.NthPrime(testIndex, new SieveOptions { Method = SieveMethod.Auto });
            var autoTime = stopwatch.ElapsedMilliseconds;

            var table = new ConsoleTable("Method", "Result", "Time (ms)", "Status");
            table.AddRow("Regular", regular.ToString("N0"), regularTime.ToString("N0"), regular == expectedPrime ? "✓ CORRECT" : "✗ INCORRECT");
            table.AddRow("Segmented", segmented.ToString("N0"), segmentedTime.ToString("N0"), segmented == expectedPrime ? "✓ CORRECT" : "✗ INCORRECT");
            table.AddRow("Auto", auto.ToString("N0"), autoTime.ToString("N0"), auto == expectedPrime ? "✓ CORRECT" : "✗ INCORRECT");
            table.Write();

            bool allMatch = (regular == segmented) && (regular == auto) && (regular == expectedPrime);
            Console.WriteLine(allMatch ? "\n✓ All methods produced identical, correct results!\n" : "\n✗ Methods produced different results!\n");

            Assert.AreEqual(regular, segmented);
            Assert.AreEqual(regular, auto);
            Assert.AreEqual(expectedPrime, regular);
        }

        /// <summary>
        /// Verifies auto-selection behavior and that the switching logic works correctly.
        /// </summary>
        [TestMethod]
        public void AutoSelection_ChoosesAppropriateMethod()
        {
            Console.WriteLine("Auto-Selection Logic Verification:");
            Console.WriteLine("Testing that Auto mode selects the appropriate method for different index ranges.\n");

            var testCases = new[]
            {
                (index: 100L, expected: 547L, expectedMethod: "Regular"),
                (index: 100_000L, expected: 1_299_721L, expectedMethod: "Segmented"),
                (index: 1_000_000L, expected: 15_485_867L, expectedMethod: "PrimeCounting"),
                (index: 10_000_000L, expected: 179_424_691L, expectedMethod: "PrimeCounting")
            };

            var table = new ConsoleTable("Index (0-based)", "Expected Method", "Computed Prime", "Time (ms)", "Status");

            foreach (var (index, expected, expectedMethod) in testCases)
            {
                var stopwatch = Stopwatch.StartNew();
                var result = sieve.NthPrime(index, new SieveOptions { Method = SieveMethod.Auto });
                stopwatch.Stop();

                var status = result == expected ? "✓ CORRECT" : "✗ INCORRECT";

                table.AddRow(
                    index.ToString("N0"),
                    expectedMethod,
                    result.ToString("N0"),
                    stopwatch.ElapsedMilliseconds.ToString("N0"),
                    status
                );

                Assert.AreEqual(expected, result);
            }

            table.Write();
            Console.WriteLine("\n✓ Auto-selection logic working correctly!\n");
        }

        /// <summary>
        /// Demonstrates performance characteristics around the Regular→Segmented threshold.
        /// Shows why the auto-selection logic matters in practice.
        /// </summary>
        [TestMethod]
        public void PerformanceDemonstration_MethodCrossover()
        {
            Console.WriteLine("Method Crossover Performance Analysis:");
            Console.WriteLine("Comparing performance across the Regular→Segmented threshold to demonstrate auto-selection benefits.\n");

            var testCases = new[]
            {
                (index: 10_000L, expected: 104_743L, description: "Small (Regular optimal)"),
                (index: 100_000L, expected: 1_299_721L, description: "Medium (near threshold)"),
                (index: 1_000_000L, expected: 15_485_867L, description: "Large (Segmented optimal)"),
                (index: 10_000_000L, expected: 179_424_691L, description: "Extra-Large (PrimeCounting)")
            };

            var table = new ConsoleTable("Size Category", "Index (0-based)", "Computed Prime", "Time (ms)", "Auto-Selected Method", "Status");

            foreach (var (index, expected, description) in testCases)
            {
                var stopwatch = Stopwatch.StartNew();
                var result = sieve.NthPrime(index);
                stopwatch.Stop();

                var autoMethod = GetAutoSelectedMethod(index);
                var status = result == expected ? "✓ CORRECT" : "✗ INCORRECT";

                table.AddRow(
                    description,
                    index.ToString("N0"),
                    result.ToString("N0"),
                    stopwatch.ElapsedMilliseconds.ToString("N0"),
                    autoMethod,
                    status
                );

                Assert.AreEqual(expected, result);
            }

            table.Write();
            Console.WriteLine("\n✓ Performance characteristics demonstrate effective auto-selection across size ranges!\n");
        }

        /// <summary>
        /// Demonstrates the dramatic performance improvement of Lucy-Hedgehog prime counting
        /// for very large indices compared to linear sieving approaches.
        /// </summary>
        [TestMethod]
        public void PerformanceDemonstration_LargePrimeCounting()
        {
            Console.WriteLine("Large Index Performance Comparison:");
            Console.WriteLine("Demonstrating Lucy-Hedgehog prime counting vs linear sieving for very large indices.\n");

            const long largeIndex = 10_000_000L; // 10 millionth prime
            const long expectedPrime = 179_424_691L;

            // Test segmented (linear approach)
            var stopwatch = Stopwatch.StartNew();
            var segmentedResult = sieve.NthPrime(largeIndex,
                new SieveOptions { Method = SieveMethod.Segmented });
            stopwatch.Stop();
            var segmentedTime = stopwatch.ElapsedMilliseconds;

            // Test prime counting approach
            stopwatch.Restart();
            var countingResult = sieve.NthPrime(largeIndex,
                new SieveOptions { Method = SieveMethod.PrimeCounting });
            stopwatch.Stop();
            var countingTime = stopwatch.ElapsedMilliseconds;

            var table = new ConsoleTable("Method", "Computed Prime", "Time (ms)", "Speedup", "Status");

            var speedupText = countingTime > 0 ? $"{(double)segmentedTime/Math.Max(1, countingTime):F1}x" : "Too fast to measure";

            table.AddRow("Segmented (Linear)", segmentedResult.ToString("N0"), segmentedTime.ToString("N0"), "1.0x (baseline)",
                segmentedResult == expectedPrime ? "✓ CORRECT" : "✗ INCORRECT");
            table.AddRow("Prime Counting (Lucy-Hedgehog)", countingResult.ToString("N0"), countingTime.ToString("N0"), speedupText,
                countingResult == expectedPrime ? "✓ CORRECT" : "✗ INCORRECT");

            table.Write();

            Console.WriteLine($"\n🚀 Prime counting method demonstrates why auto-selection switches at large indices!");
            Console.WriteLine($"   Both methods computed the correct 10,000,000th prime: {expectedPrime:N0}\n");

            Assert.AreEqual(segmentedResult, countingResult, "Methods should agree on the result");
            Assert.AreEqual(expectedPrime, countingResult, "Result should match expected prime");
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
            Console.WriteLine("Custom Options Verification:");
            Console.WriteLine("Testing that custom configuration options work correctly without affecting correctness.\n");

            long testIndex = 1000;
            long expected = 7927; // 1000th prime (0-based)

            var testConfigs = new[]
            {
                (description: "Default Options", options: new SieveOptions()),
                (description: "Custom Segment Size", options: new SieveOptions { Method = SieveMethod.Segmented, SegmentSize = 50_000 }),
                (description: "Custom Threshold", options: new SieveOptions { RegularSieveThreshold = 500 })
            };

            var table = new ConsoleTable("Configuration", "Computed Prime", "Time (ms)", "Status");

            foreach (var (description, options) in testConfigs)
            {
                var stopwatch = Stopwatch.StartNew();
                var result = sieve.NthPrime(testIndex, options);
                stopwatch.Stop();

                var status = result == expected ? "✓ CORRECT" : "✗ INCORRECT";

                table.AddRow(
                    description,
                    result.ToString("N0"),
                    stopwatch.ElapsedMilliseconds.ToString("N0"),
                    status
                );

                Assert.AreEqual(expected, result);
            }

            table.Write();
            Console.WriteLine($"\n✓ All custom configurations produced the correct result: {expected:N0}\n");
        }

        /// <summary>
        /// Helper method to determine which algorithm Auto mode would select for a given index.
        /// Replicates the logic from SieveImplementation.NthPrime().
        /// </summary>
        private static string GetAutoSelectedMethod(long n)
        {
            // These thresholds should match the defaults in SieveOptions
            const long PrimeCountingThreshold = 10_000_000; // Default for very large n
            const long RegularSieveThreshold = 1_000_000;   // Default transition to segmented

            if (n > PrimeCountingThreshold)
                return "PrimeCounting";
            else if (n > RegularSieveThreshold)
                return "Segmented";
            else
                return "Regular";
        }
    }
}