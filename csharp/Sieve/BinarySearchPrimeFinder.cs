namespace Sieve
{
    /// <summary>
    /// Binary search with Lucy-Hedgehog counting for finding very large primes.
    /// </summary>
    public static class BinarySearchPrimeFinder
    {
        /// <summary>
        /// Finds nth prime using binary search with Lucy-Hedgehog counting.
        /// </summary>
        public static long FindNthPrime(long n, SieveOptions options)
        {
            long target = n + 1;

            long lo = PrimeBounds.EstimateLowerBound(target);
            long hi = PrimeBounds.EstimateUpperBound(target);

            // Precompute base primes for counting
            int rootHi = checked((int)Math.Floor(Math.Sqrt(hi)));
            var smallPrimes = SieveImplementation.SieveOddsOnly(rootHi);

            int iters = 0;
            while (lo < hi && iters++ < 50)
            {
                long mid = lo + ((hi - lo) >> 1);
                long count = LucyHedgehog.PrimeCount(mid, smallPrimes);

                if (count < target) lo = mid + 1; else hi = mid;
            }

            return FindExactPrimeNear(lo, n, options);
        }

        /// <summary>
        /// Finds exact nth prime using local segmented search around estimate.
        /// </summary>
        private static long FindExactPrimeNear(long estimate, long n, SieveOptions options)
        {
            // Create targeted search window around estimate
            long windowSize = Math.Min(1_000_000, Math.Max(10_000, estimate / 10000));
            long start = Math.Max(2, estimate - windowSize / 4);
            long end = estimate + windowSize;

            if (options.Logger != null)
            {
                options.Logger($"Local search window: [{start:N0}, {end:N0}] (size: {windowSize:N0})");
            }

            // Generate base primes for segmented sieve
            long sqrtEnd = (long)Math.Sqrt(end);
            var basePrimes = SieveImplementation.SieveOddsOnly(checked((int)Math.Min(sqrtEnd, int.MaxValue - 1)));

            long primeCount = 0;

            // Count primes before search window
            if (start > 2)
            {
                primeCount = LucyHedgehog.PrimeCount(start - 1);
            }

            if (options.Logger != null)
            {
                options.Logger($"Estimated primes before window: {primeCount:N0}");
            }

            // Scan through window in segments
            long segmentStart = start;
            int segmentSize = Math.Min(options.SegmentSize ?? 100_000, 100_000);

            while (segmentStart <= end)
            {
                long segmentEnd = Math.Min(segmentStart + segmentSize - 1, end);
                var primes = SegmentedSieve.SieveOddsOnly(segmentStart, segmentEnd, basePrimes);

                foreach (long prime in primes)
                {
                    if (primeCount == n)
                    {
                        if (options.Logger != null)
                        {
                            options.Logger($"Found {n}th prime: {prime:N0}");
                        }
                        return prime;
                    }
                    primeCount++;
                }

                segmentStart = segmentEnd + 1;
            }

            // Expand search if not found in initial window
            if (options.Logger != null)
            {
                options.Logger($"Prime not found in window, expanding search...");
            }

            // Fallback with larger window
            return FindExactPrimeNearExpanded(estimate, n, options, primeCount);
        }

        /// <summary>
        /// Fallback search with larger window.
        /// </summary>
        private static long FindExactPrimeNearExpanded(long estimate, long n, SieveOptions options, long currentCount)
        {
            // Create much larger search window
            long windowSize = Math.Max(10_000_000, estimate / 100);
            long start = Math.Max(2, estimate - windowSize / 2);
            long end = estimate + windowSize;

            if (options.Logger != null)
            {
                options.Logger($"Expanded search window: [{start:N0}, {end:N0}]");
            }

            // Recalculate starting count for expanded window
            long primeCount = start > 2 ? LucyHedgehog.PrimeCount(start - 1) : 0;

            long sqrtEnd = (long)Math.Sqrt(end);
            var basePrimes = SieveImplementation.SieveOddsOnly(checked((int)Math.Min(sqrtEnd, int.MaxValue - 1)));

            long segmentStart = start;
            int segmentSize = options.SegmentSize ?? 1_000_000;

            while (segmentStart <= end)
            {
                long segmentEnd = Math.Min(segmentStart + segmentSize - 1, end);
                var primes = SegmentedSieve.SieveOddsOnly(segmentStart, segmentEnd, basePrimes);

                foreach (long prime in primes)
                {
                    if (primeCount == n) return prime;
                    primeCount++;
                }

                segmentStart = segmentEnd + 1;
            }

            throw new InvalidOperationException($"Could not find {n}th prime near estimate {estimate}. Search range was [{start:N0}, {end:N0}].");
        }
    }
}