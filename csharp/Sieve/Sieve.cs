using System;
using System.Collections.Generic;

namespace Sieve
{

    /// <summary>
    /// Interface for classes that can find the nth prime number.
    /// </summary>
    public interface ISieve
    {
        /// <summary>
        /// Returns the nth prime number using default options. Index is 0-based: 0 =&gt; 2, 1 =&gt; 3.
        /// </summary>
        /// <param name="n">Zero-based index of the prime (0 =&gt; 2).</param>
        /// <returns>The nth prime.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="n"/> is negative.</exception>
        long NthPrime(long n);

        /// <summary>
        /// Returns the nth prime number using the provided options. Honors the caller's choices
        /// without overriding them, even if they are suboptimal.
        /// </summary>
        /// <param name="n">Zero-based index of the prime (0 =&gt; 2).</param>
        /// <param name="options">Configuration for the sieve. Must not be null.</param>
        /// <returns>The nth prime.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="n"/> is negative.</exception>
        long NthPrime(long n, SieveOptions options);
    }

    /// <summary>
    /// Odds-only, cache-friendly implementation with auto/regular/segmented strategies.
    /// For very large n, uses Lucy-Hedgehog prime counting with binary search.
    /// </summary>
    /// <remarks>
    /// Design notes:
    /// <list type="bullet">
    /// <item>
    /// <description>Regular sieve uses an <em>odds-only</em> layout (halve memory/touches).</description>
    /// </item>
    /// <item>
    /// <description>Segmented sieve ensures <c>basePrimes</c> always cover ≤ ⌊√segmentEnd⌋ (correctness).</description>
    /// </item>
    /// <item>
    /// <description>Lucy-Hedgehog counting used for n ≥ 10M to avoid linear scanning.</description>
    /// </item>
    /// <item>
    /// <description>Growth after an underestimate is gentle (×1.25) to avoid large re-sieves.</description>
    /// </item>
    /// </list>
    /// References:
    /// <para>
    /// Prime bounds (Dusart): <see href="https://doi.org/10.4153/CJM-1999-066-8">Dusart, 1999</see>.
    /// </para>
    /// <para>
    /// Lucy-Hedgehog algorithm: <see href="https://projecteuler.net/thread=10;page=5#111677">Project Euler discussion</see>.
    /// </para>
    /// </remarks>
    public class SieveImplementation : ISieve
    {
        /// <inheritdoc />
        public long NthPrime(long n)
        {
            return NthPrime(n, new SieveOptions());
        }

        /// <inheritdoc />
        public long NthPrime(long n, SieveOptions options)
        {
            if (n < 0)
                throw new ArgumentOutOfRangeException(nameof(n), "n must be non-negative (0-based index).");
            if (options is null)
                throw new ArgumentNullException(nameof(options));

            var method = options.Method;
            bool forced = method != SieveMethod.Auto;

            // Advisory diagnostics: inform (but do not override) if forced options are likely suboptimal.
            if (forced && options.Logger is not null)
            {
                if (method == SieveMethod.Segmented && n < options.RegularSieveThreshold)
                {
                    options.Logger(
                        $"Advisory: Segmented was forced for n={n} with SegmentSize={options.SegmentSize:N0}. " +
                        $"For small n, Regular is typically faster. Consider Auto or Regular.");
                }
                else if (method == SieveMethod.Regular && n >= options.RegularSieveThreshold)
                {
                    options.Logger(
                        $"Advisory: Regular was forced for n={n}. For large n, Segmented may use less memory/fewer cache misses. " +
                        $"Consider Auto or Segmented.");
                }
            }

            if (method == SieveMethod.Auto)
            {
                if (n > options.PrimeCountingThreshold)
                    method = SieveMethod.PrimeCounting;
                else if (n > options.RegularSieveThreshold)
                    method = SieveMethod.Segmented;
                else
                    method = SieveMethod.Regular;
            }

            return method switch
            {
                SieveMethod.Regular => FindNthPrimeRegular(n),
                SieveMethod.Segmented => FindNthPrimeSegmented(n, options),
                SieveMethod.PrimeCounting => FindNthPrimeBinarySearch(n, options),
                _ => throw new ArgumentOutOfRangeException(nameof(method), $"Unknown sieve method: {method}")
            };
        }

        /// <summary>
        /// Regular odds-only sieve approach. Prefers readability + cache behavior.
        /// </summary>
        /// <remarks>
        /// If the upper bound undershoots, we grow it gently (×1.25) and retry.
        /// Uses <see cref="SieveOddsOnly"/> internally.
        /// </remarks>
        private static long FindNthPrimeRegular(long n)
        {
            long k = n + 1; // k-th prime in 1-based terms
            long ub = EstimateUpperBound(k);

            List<int> primes = SieveIntLimitOrThrow(ub);
            while (primes.Count <= n)
            {
                // Gentle growth to reduce wasted work on near-misses
                ub = (long)Math.Min(int.MaxValue - 1L, (long)(ub * 1.25));
                primes = SieveIntLimitOrThrow(ub);
            }
            return primes[(int)n];
        }

        /// <summary>
        /// Segmented sieve that ensures base primes cover sqrt(end) for each segment.
        /// </summary>
        /// <remarks>
        /// SegmentSize is interpreted as "count of integers per segment." Odds-only marking is used inside segments.
        /// </remarks>
        private static long FindNthPrimeSegmented(long n, SieveOptions options)
        {
            int segmentCount = options.SegmentSize ?? 1_000_000; // use provided value or cache-optimized default
            long segmentStart = 2;
            long produced = 0;

            // Start with a modest base bound; will be extended on demand.
            int baseLimit = 1024;
            List<int> basePrimes = SieveOddsOnly(baseLimit);

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
                    basePrimes = SieveOddsOnly(baseLimit);
                }


                var primes = SegmentedSieveOddsOnly(segmentStart, segmentEnd, basePrimes);

                foreach (var p in primes)
                {
                    if (produced == n) return p;
                    produced++;
                }

                segmentStart = segmentEnd + 1;
            }
        }

        /// <summary>
        /// Estimates an upper bound for the k-th prime (k = n+1) with a modest safety factor.
        /// </summary>
        /// <remarks>
        /// For small k, returns a conservative constant or linear fudge to avoid undershoot.
        /// For k ≥ 6, uses n (log n + log log n - 1 + (log log n - 2)/log n) with a 1.25× cushion.
        /// See <see href="https://doi.org/10.4153/CJM-1999-066-8">Dusart 1999</see> for bounds.
        /// </remarks>
        private static long EstimateUpperBound(long k)
        {
            if (k < 6) return 30;
            if (k < 100) return k * 15;

            double n = k;
            double logN = Math.Log(n);
            double logLogN = Math.Log(logN);

            double estimate = n * (logN + logLogN - 1 + (logLogN - 2) / logN);
            return (long)(estimate * 1.25); // small cushion to reduce resieves
        }

        /// <summary>
        /// Wrapper that enforces an <c>int</c> limit for odds-only regular sieve.
        /// </summary>
        private static List<int> SieveIntLimitOrThrow(long limit64)
        {
            if (limit64 > int.MaxValue - 1)
                throw new ArgumentOutOfRangeException(nameof(limit64),
                    "Limit too large for regular sieve; use segmented.");
            return SieveOddsOnly((int)limit64);
        }

        /// <summary>
        /// Odds-only sieve up to <paramref name="limit"/> (inclusive).
        /// </summary>
        /// <remarks>
        /// Layout maps index <c>i</c> to value <c>v = 2*i + 1</c>. Index 0 =&gt; 1 (ignored), index 1 =&gt; 3, etc.
        /// We seed result with 2 and then collect all odd non-composites ≥ 3.
        /// </remarks>
        private static List<int> SieveOddsOnly(int limit)
        {
            if (limit < 2) return new List<int>();

            int m = (limit - 1) / 2;            // count of odds up to limit (1,3,5,...,limit)
            var isComposite = new bool[m + 1];  // isComposite[i] corresponds to (2*i + 1)

            int sqrt = (int)Math.Sqrt(limit);
            // i = 1 => value 3; skip i=0 (value 1)
            for (int i = 1; (2 * i + 1) <= sqrt; i++)
            {
                if (!isComposite[i])
                {
                    int p = 2 * i + 1;                  // odd prime candidate
                    int start = (p * p - 1) / 2;        // index of p*p in the odds array
                    for (int j = start; j <= m; j += p) // step by p across odd multiples
                        isComposite[j] = true;
                }
            }

            var primes = new List<int>(m / 2 + 1) { 2 };
            for (int i = 1; i <= m; i++)
                if (!isComposite[i]) primes.Add(2 * i + 1);
            return primes;
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
        private static List<long> SegmentedSieveOddsOnly(long start, long end, List<int> basePrimes)
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

        /// <summary>
        /// Uses binary search with Lucy-Hedgehog prime counting to find the nth prime efficiently.
        /// This approach avoids linear scanning for very large n values.
        /// </summary>
private static long FindNthPrimeBinarySearch(long n, SieveOptions options)
{
    long target = n + 1;

    long lo = EstimateLowerBound(target);
    long hi = EstimateUpperBound(target);

    // Precompute primes up to sqrt(hi) for prime counting
    int rootHi = checked((int)Math.Floor(Math.Sqrt(hi)));
    var smallPrimes = SieveOddsOnly(rootHi);

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
            var basePrimes = SieveOddsOnly(checked((int)Math.Min(sqrtEnd, int.MaxValue - 1))); // no cap

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
                var primes = SegmentedSieveOddsOnly(segmentStart, segmentEnd, basePrimes);
                
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
            var basePrimes = SieveOddsOnly(checked((int)Math.Min(sqrtEnd, int.MaxValue - 1))); // no cap

            
            long segmentStart = start;
            int segmentSize = options.SegmentSize ?? 1_000_000;
            
            while (segmentStart <= end)
            {
                long segmentEnd = Math.Min(segmentStart + segmentSize - 1, end);
                var primes = SegmentedSieveOddsOnly(segmentStart, segmentEnd, basePrimes);
                
                foreach (long prime in primes)
                {
                    if (primeCount == n) return prime;
                    primeCount++;
                }
                
                segmentStart = segmentEnd + 1;
            }

            throw new InvalidOperationException($"Could not find {n}th prime near estimate {estimate}. Search range was [{start:N0}, {end:N0}].");
        }





        /// <summary>
        /// Estimates a lower bound for the kth prime using improved bounds.
        /// </summary>
        private static long EstimateLowerBound(long k)
        {
            if (k < 6) return 2;
            
            double n = k;
            double logN = Math.Log(n);
            double logLogN = Math.Log(logN);
            
            // Dusart's lower bound: p_n > n(ln n + ln ln n - 1)
            double lowerEstimate = n * (logN + logLogN - 1.0);
            
            return Math.Max(2, (long)(lowerEstimate * 0.95)); // 5% safety margin
        }
    }

}