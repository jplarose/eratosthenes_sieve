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
                SieveMethod.Segmented => SegmentedSieve.FindNthPrime(n, options),
                SieveMethod.PrimeCounting => BinarySearchPrimeFinder.FindNthPrime(n, options),
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
            long ub = PrimeBounds.EstimateUpperBound(k);

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
        internal static List<int> SieveOddsOnly(int limit)
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
    }
}