namespace Sieve
{
    /// <summary>
    /// Segmented sieve implementation that ensures base primes cover sqrt(end) for each segment.
    /// Optimized for memory usage and cache performance when finding primes in large ranges.
    /// </summary>
    public static class SegmentedSieve
    {
        /// <summary>
        /// Segmented sieve that ensures base primes cover sqrt(end) for each segment.
        /// </summary>
        /// <remarks>
        /// SegmentSize is interpreted as "count of integers per segment." Odds-only marking is used inside segments.
        /// </remarks>
        public static long FindNthPrime(long n, SieveOptions options)
        {
            int segmentCount = options.SegmentSize ?? 1_000_000; // use provided value or cache-optimized default
            long segmentStart = 2;
            long produced = 0;

            // Start with a modest base bound; will be extended on demand.
            int baseLimit = 1024;
            List<int> basePrimes = SieveImplementation.SieveOddsOnly(baseLimit);

            while (true)
            {
                long segmentEnd = segmentStart + segmentCount - 1;
                // before each segment:
                long needBase = (long)Math.Floor(Math.Sqrt(Math.Max(4, segmentEnd)));

                // Correct coverage check: ensure we've sieved primes through needBase.
                if (baseLimit < needBase)
                {
                    // grow to at least needBase (pad a little to avoid repeated bumps)
                    baseLimit = (int)Math.Min(
                        int.MaxValue - 1,
                        Math.Max((int)needBase + 1024, baseLimit * 2)
                    );
                    basePrimes = SieveImplementation.SieveOddsOnly(baseLimit);
                }

                var primes = SieveOddsOnly(segmentStart, segmentEnd, basePrimes);

                foreach (var p in primes)
                {
                    if (produced == n) return p;
                    produced++;
                }

                segmentStart = segmentEnd + 1;
            }
        }

        /// <summary>
        /// Segmented odds-only sieve for [start, end]. Requires base primes ≤ ⌊√end⌋.
        /// </summary>
        /// <remarks>
        /// Invariants:
        /// <list type="bullet">
        /// <item><description><c>2 &lt;= start &lt;= end</c></description></item>
        /// <item><description><paramref name="basePrimes"/> contains all primes ≤ ⌊√end⌋</description></item>
        /// <item><description>Marks only odds in the segment; 2 is handled explicitly.</description></item>
        /// </list>
        /// </remarks>
        public static List<long> SieveOddsOnly(long start, long end, List<int> basePrimes)
        {
            var result = new List<long>();

            if (start <= 2 && 2 <= end)
                result.Add(2);

            // Map odds in [start, end] into a compact boolean window.
            long firstOdd = (start <= 2) ? 3 : ((start % 2 == 0) ? start + 1 : start);
            if (firstOdd > end) return result; // nothing odd to do

            int countOdds = (int)((end - firstOdd) / 2 + 1);
            var isComposite = new bool[countOdds]; // index i -> value v = firstOdd + 2*i

            foreach (int p in basePrimes)
            {
                if (p == 2) continue; // we only mark odds; even multiples are irrelevant here
                long pp = (long)p * p;
                if (pp > end) break;

                // first multiple of p within [firstOdd, end]
                long first = Math.Max(pp, CeilDiv(firstOdd, p) * (long)p);
                if ((first & 1) == 0) first += p; // align to odd multiple

                for (long v = first; v <= end; v += 2L * p)
                {
                    int idx = (int)((v - firstOdd) / 2);
                    if ((uint)idx < (uint)isComposite.Length)
                        isComposite[idx] = true;
                }
            }

            for (int i = 0; i < isComposite.Length; i++)
            {
                if (!isComposite[i])
                {
                    long candidate = firstOdd + 2L * i;
                    if (candidate >= 3) result.Add(candidate);
                }
            }

            return result;
        }

        /// <summary>
        /// Integer ceil division: ⌈a / b⌉ for positive <paramref name="b"/>.
        /// </summary>
        private static long CeilDiv(long a, int b)
        {
            // Assumes b > 0
            return (a + b - 1) / b;
        }
    }
}