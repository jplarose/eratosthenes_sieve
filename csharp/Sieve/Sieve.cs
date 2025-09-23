using System;
using System.Collections.Generic;

namespace Sieve
{

    /// <summary>
    /// Interface for finding the nth prime number (0-based indexing).
    /// </summary>
    public interface ISieve
    {
        /// <summary>
        /// Returns the nth prime using default options (0 =&gt; 2, 1 =&gt; 3).
        /// </summary>
        long NthPrime(long n);

        /// <summary>
        /// Returns the nth prime using specified options.
        /// </summary>
        long NthPrime(long n, SieveOptions options);
    }

    /// <summary>
    /// Three-tier prime sieve: Regular (small n), Segmented (medium n), Lucy-Hedgehog (large n).
    /// Uses odds-only optimization and automatic algorithm selection.
    /// </summary>
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

            // Log advisory warnings for potentially suboptimal forced methods
            if (forced && options.Logger is not null)
            {
                if (method == SieveMethod.Segmented && n < options.RegularSieveThreshold)
                {
                    options.Logger($"Advisory: Segmented forced for small n={n}. Regular typically faster.");
                }
                else if (method == SieveMethod.Regular && n >= options.RegularSieveThreshold)
                {
                    options.Logger($"Advisory: Regular forced for large n={n}. Segmented may be more efficient.");
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
        /// Regular sieve with gentle growth on underestimates (×1.25).
        /// </summary>
        private static long FindNthPrimeRegular(long n)
        {
            long k = n + 1; // k-th prime in 1-based terms
            long ub = PrimeBounds.EstimateUpperBound(k);

            List<int> primes = SieveIntLimitOrThrow(ub);
            while (primes.Count <= n)
            {
                // Gentle growth to avoid expensive re-sieving
                ub = (long)Math.Min(int.MaxValue - 1L, (long)(ub * 1.25));
                primes = SieveIntLimitOrThrow(ub);
            }
            return primes[(int)n];
        }

        /// <summary>
        /// Enforces int limit for regular sieve.
        /// </summary>
        private static List<int> SieveIntLimitOrThrow(long limit64)
        {
            if (limit64 > int.MaxValue - 1)
                throw new ArgumentOutOfRangeException(nameof(limit64),
                    "Limit too large for regular sieve; use segmented.");
            return SieveOddsOnly((int)limit64);
        }

        /// <summary>
        /// Odds-only sieve: index i maps to value 2*i + 1.
        /// Seeds result with 2, then collects odd primes ≥ 3.
        /// </summary>
        internal static List<int> SieveOddsOnly(int limit)
        {
            if (limit < 2) return new List<int>();

            int m = (limit - 1) / 2;            // count of odds up to limit
            var isComposite = new bool[m + 1];  // isComposite[i] = (2*i + 1) is composite

            int sqrt = (int)Math.Sqrt(limit);
            // Start at i=1 (value 3), skip i=0 (value 1)
            for (int i = 1; (2 * i + 1) <= sqrt; i++)
            {
                if (!isComposite[i])
                {
                    int p = 2 * i + 1;                  // odd prime
                    int start = (p * p - 1) / 2;        // index of p*p
                    for (int j = start; j <= m; j += p) // mark odd multiples
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