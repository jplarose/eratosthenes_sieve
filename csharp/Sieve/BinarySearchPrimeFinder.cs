using System;
using System.Collections.Generic;

namespace Sieve
{
    /// <summary>
    /// Binary search-based prime finding using Lucy-Hedgehog prime counting.
    /// Efficient for finding very large prime indices by avoiding linear scanning.
    /// </summary>
    public static class BinarySearchPrimeFinder
    {
        /// <summary>
        /// Uses binary search with Lucy-Hedgehog prime counting to find the nth prime efficiently.
        /// This approach avoids linear scanning for very large n values.
        /// </summary>
        public static long FindNthPrime(long n, SieveOptions options)
        {
            long target = n + 1;

            long lo = PrimeBounds.EstimateLowerBound(target);
            long hi = PrimeBounds.EstimateUpperBound(target);

            // Precompute primes up to sqrt(hi) for prime counting
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
        /// Finds the exact nth prime near the estimated position using local segmented sieve.
        /// </summary>
        private static long FindExactPrimeNear(long estimate, long n, SieveOptions options)
        {
            // Use a much smaller, more targeted window
            long windowSize = Math.Min(1_000_000, Math.Max(10_000, estimate / 10000));
            long start = Math.Max(2, estimate - windowSize / 4); // Start closer to estimate
            long end = estimate + windowSize;

            if (options.Logger != null)
            {
                options.Logger($"Local search window: [{start:N0}, {end:N0}] (size: {windowSize:N0})");
            }

            // Use segmented sieve to find primes in this range
            long sqrtEnd = (long)Math.Sqrt(end);
            var basePrimes = SieveImplementation.SieveOddsOnly(checked((int)Math.Min(sqrtEnd, int.MaxValue - 1))); // no cap

            long primeCount = 0;

            // Get approximate count up to start using our counting function
            if (start > 2)
            {
                primeCount = LucyHedgehog.PrimeCount(start - 1);
            }

            if (options.Logger != null)
            {
                options.Logger($"Estimated primes before window: {primeCount:N0}");
            }

            // Now sieve through our target window in smaller chunks
            long segmentStart = start;
            int segmentSize = Math.Min(options.SegmentSize ?? 100_000, 100_000); // Smaller segments

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

            // If we didn't find it, the estimate was off - expand search
            if (options.Logger != null)
            {
                options.Logger($"Prime not found in window, expanding search...");
            }

            // Try a larger window
            return FindExactPrimeNearExpanded(estimate, n, options, primeCount);
        }

        /// <summary>
        /// Fallback method with expanded search window.
        /// </summary>
        private static long FindExactPrimeNearExpanded(long estimate, long n, SieveOptions options, long currentCount)
        {
            // Expand the search window significantly
            long windowSize = Math.Max(10_000_000, estimate / 100);
            long start = Math.Max(2, estimate - windowSize / 2);
            long end = estimate + windowSize;

            if (options.Logger != null)
            {
                options.Logger($"Expanded search window: [{start:N0}, {end:N0}]");
            }

            // Reset count and search from beginning of expanded window
            long primeCount = start > 2 ? LucyHedgehog.PrimeCount(start - 1) : 0;

            long sqrtEnd = (long)Math.Sqrt(end);
            var basePrimes = SieveImplementation.SieveOddsOnly(checked((int)Math.Min(sqrtEnd, int.MaxValue - 1))); // no cap

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