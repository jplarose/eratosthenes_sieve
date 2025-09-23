namespace Sieve
{
    /// <summary>
    /// Segmented sieve with bounded memory usage and dynamic base prime extension.
    /// </summary>
    public static class SegmentedSieve
    {
        /// <summary>
        /// Finds nth prime using segmented sieve with dynamic base prime extension.
        /// </summary>
        public static long FindNthPrime(long n, SieveOptions options)
        {
            int segmentCount = options.SegmentSize ?? 1_000_000; // default: 1M integers per segment
            long segmentStart = 2;
            long produced = 0;

            // Start with modest base primes; extend as needed
            int baseLimit = 1024;
            List<int> basePrimes = SieveImplementation.SieveOddsOnly(baseLimit);

            while (true)
            {
                long segmentEnd = segmentStart + segmentCount - 1;
                // Ensure base primes cover √(segmentEnd) for correctness
                long needBase = (long)Math.Floor(Math.Sqrt(Math.Max(4, segmentEnd)));

                if (baseLimit < needBase)
                {
                    // Extend base primes with padding to avoid frequent re-computation
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
        /// Odds-only sieve for range [start, end] using base primes ≤ √end.
        /// </summary>
        public static List<long> SieveOddsOnly(long start, long end, List<int> basePrimes)
        {
            var result = new List<long>();

            if (start <= 2 && 2 <= end)
                result.Add(2);

            // Map odds in [start, end] to compact boolean array
            long firstOdd = (start <= 2) ? 3 : ((start % 2 == 0) ? start + 1 : start);
            if (firstOdd > end) return result;

            int countOdds = (int)((end - firstOdd) / 2 + 1);
            var isComposite = new bool[countOdds]; // index i = value firstOdd + 2*i

            foreach (int p in basePrimes)
            {
                if (p == 2) continue; // skip 2, only mark odd multiples
                long pp = (long)p * p;
                if (pp > end) break;

                // Find first odd multiple of p in range
                long first = Math.Max(pp, CeilDiv(firstOdd, p) * (long)p);
                if ((first & 1) == 0) first += p; // ensure odd

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
        /// Ceiling division: ⌈a / b⌉.
        /// </summary>
        private static long CeilDiv(long a, int b)
        {
            // Assumes b > 0
            return (a + b - 1) / b;
        }
    }
}