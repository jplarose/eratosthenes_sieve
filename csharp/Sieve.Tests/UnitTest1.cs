using ConsoleTables;

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
                (index: 0L, expected: 2L),
                (index: 19L, expected: 71L),
                (index: 99L, expected: 541L),
                (index: 500L, expected: 3581L),
                (index: 986L, expected: 7793L),
                (index: 2000L, expected: 17393L),
                (index: 1000000L, expected: 15485867L),
                (index: 10000000L, expected: 179424691L),
                (index: 100000000L, expected: 2038074751L) //not required, just a fun challenge
            };

            Console.WriteLine("Prime Number Verification Test Results:");
            var table = new ConsoleTable("Index (0-based)", "Expected Prime", "Computed Prime", "Status");

            foreach (var (index, expected) in testCases)
            {
                var actual = sieve.NthPrime(index);
                var status = actual == expected ? "✓ PASS" : "✗ FAIL";
                table.AddRow(index.ToString("N0"), expected.ToString("N0"), actual.ToString("N0"), status);

                Assert.AreEqual(expected, actual);
            }

            table.Write();
            Console.WriteLine($"\nAll {testCases.Length} test cases passed successfully!");
        }
    }
}