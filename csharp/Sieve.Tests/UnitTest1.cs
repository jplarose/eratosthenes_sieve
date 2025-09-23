using ConsoleTables;
using Sieve;

namespace Sieve.Tests

{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestNthPrime()
        {
            ISieve sieve = new SieveImplementation();

            var testCases = new[]
            {
                (index: 0, expected: 2),
                (index: 19, expected: 71),
                (index: 99, expected: 541),
                (index: 500, expected: 3581),
                (index: 986, expected: 7793),
                (index: 2000, expected: 17393),
                (index: 1000000, expected: 15485867),
                (index: 10000000, expected: 179424691),
                (index: 100000000, expected: 2038074751), //not required, just a fun challenge

                // Only adds about 30-40 seconds to the test run
                (index: 999_999_999, expected: 22_801_763_489),

                // Adds about 3-6 minutes to the test run depending on your machine, but runnable under the current implementation
                //(index: 10_000_000_000, expected: 252_097_800_629)
            };

            Console.WriteLine("Prime Number Verification Test Results:");
            var table = new ConsoleTable("Index (0-based)", "Expected Prime", "Computed Prime", "Time (ms)", "Method Used", "Status");

            foreach (var (index, expected) in testCases)
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var actual = sieve.NthPrime(index);
                stopwatch.Stop();

                var status = actual == expected ? "✓ PASS" : "✗ FAIL";
                var timeMs = stopwatch.ElapsedMilliseconds;

                // Determine which method Auto would select for this index
                var methodUsed = GetAutoSelectedMethod(index);

                table.AddRow(
                    index.ToString("N0"),
                    expected.ToString("N0"),
                    actual.ToString("N0"),
                    timeMs.ToString("N0"),
                    methodUsed,
                    status
                );

                Assert.AreEqual(expected, actual);
            }

            table.Write();
            Console.WriteLine($"\nAll {testCases.Length} test cases passed successfully!");
        }

        [TestMethod]
        public void TestBillionthPrime()
        {
            Console.WriteLine("Testing Billionth Prime with Lucy-Hedgehog Prime Counting:");
            Console.WriteLine("This test demonstrates the dramatic performance improvement for very large n.\n");

            ISieve sieve = new SieveImplementation();
            
            // Test the billionth prime (0-based index: 999,999,999)
            const long billionthIndex = 999_999_999;
            const long expectedBillionthPrime = 22_801_763_489L;

            var options = new SieveOptions 
            { 
                Method = SieveMethod.PrimeCounting,
                Logger = msg => Console.WriteLine($"[LOG] {msg}")
            };

            Console.WriteLine($"Finding prime at index {billionthIndex:N0} (the billionth prime)...");
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = sieve.NthPrime(billionthIndex, options);
            stopwatch.Stop();

            var table = new ConsoleTable("Metric", "Value");
            table.AddRow("Target Index (0-based)", billionthIndex.ToString("N0"));
            table.AddRow("Expected Prime (Source below)", expectedBillionthPrime.ToString("N0"));
            table.AddRow("Computed Prime", result.ToString("N0"));
            table.AddRow("Execution Time", $"{stopwatch.ElapsedMilliseconds:N0} ms ({stopwatch.Elapsed.TotalSeconds:F1} seconds)");
            table.AddRow("Method Used", "Lucy-Hedgehog Prime Counting");
            table.AddRow("Result", result == expectedBillionthPrime ? "✓ CORRECT" : "✗ INCORRECT");

            table.Write();

            Console.WriteLine("\nBillionth Prime Source: https://t5k.org/curios/page.php/22801763489.html");

            if (stopwatch.ElapsedMilliseconds < 60_000) // Less than 1 minute
            {
                Console.WriteLine($"\n🎉 SUCCESS! Found billionth prime in {stopwatch.Elapsed.TotalSeconds:F1} seconds!");
            }
            else
            {
                Console.WriteLine($"\n⚠️  Took {stopwatch.Elapsed.TotalMinutes:F1} minutes. Still room for optimization.");
            }

            Assert.AreEqual(expectedBillionthPrime, result, "Billionth prime calculation was incorrect");
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