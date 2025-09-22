using System;
using System.Collections.Generic;
using System.Linq;

namespace Sieve
{
    /// <summary>
    /// Lucy-Hedgehog algorithm for counting primes up to x.
    /// Time complexity: O(x^(3/4) / log x), efficient for very large values.
    /// </summary>
    public static class LucyHedgehog
    {
        /// <summary>
        /// Counts the number of primes less than or equal to x using the Lucy-Hedgehog algorithm.
        /// </summary>
        /// <param name="x">Upper bound for prime counting</param>
        /// <returns>Number of primes ≤ x</returns>
        public static long PrimeCount(long x)
        {
            if (x < 2) return 0;
            if (x == 2) return 1;

            long sqrtX = (long)Math.Sqrt(x);
            var smallPrimes = SievePrimes((int)Math.Min(sqrtX, int.MaxValue - 1));
            return PrimeCount(x, smallPrimes);
        }

        /// <summary>
        /// Counts the number of primes less than or equal to x using precomputed small primes.
        /// </summary>
        /// <param name="x">Upper bound for prime counting</param>
        /// <param name="smallPrimes">Precomputed primes up to at least sqrt(x)</param>
        /// <returns>Number of primes ≤ x</returns>
        public static long PrimeCount(long x, List<int> smallPrimes)
        {
            if (x < 2) return 0;
            if (x == 2) return 1;

            long sqrt_x = (long)Math.Sqrt(x);

            // Create sorted list of unique values: {1, 2, ..., sqrt_x, x/sqrt_x, x/(sqrt_x-1), ..., x/1}
            var values = new HashSet<long>();

            // Add small values 1..sqrt_x
            for (long i = 1; i <= sqrt_x; i++)
                values.Add(i);

            // Add large values x/i for i = 1..sqrt_x
            for (long i = 1; i <= sqrt_x; i++)
                values.Add(x / i);

            var W = values.OrderByDescending(v => v).ToArray();
            int len = W.Length;

            // S[n] = sum of integers from 1 to n = n*(n+1)/2 - 1 (excluding 1)
            // So count of integers in [2..n] = n - 1
            var S = new long[len];
            for (int i = 0; i < len; i++)
                S[i] = W[i] - 1;

            // Index lookup: value -> index in W
            var valueToIndex = new Dictionary<long, int>();
            for (int i = 0; i < len; i++)
                valueToIndex[W[i]] = i;

            // Process each prime p
            foreach (int p in smallPrimes)
            {
                if (p < 2) continue;
                if ((long)p * p > x) break;

                // Get S[p-1]
                long sp_minus_1 = 0;
                if (p > 1)
                {
                    if (valueToIndex.ContainsKey(p - 1))
                        sp_minus_1 = S[valueToIndex[p - 1]];
                    else
                        sp_minus_1 = (p - 1) - 1; // Direct calculation for missing values
                }

                // Update S[n] for all n >= p^2
                for (int i = 0; i < len; i++)
                {
                    long n = W[i];
                    if (n < (long)p * p) break; // W is sorted descending

                    long q = n / p;
                    long sq;
                    if (valueToIndex.ContainsKey(q))
                        sq = S[valueToIndex[q]];
                    else
                        sq = q - 1; // Direct calculation

                    S[i] -= (sq - sp_minus_1);
                }
            }

            // Result is S[x]
            return valueToIndex.ContainsKey(x) ? S[valueToIndex[x]] : x - 1;
        }

        /// <summary>
        /// Generates all primes up to limit using the Sieve of Eratosthenes.
        /// </summary>
        private static List<int> SievePrimes(int limit)
        {
            if (limit < 2) return new List<int>();

            var isPrime = new bool[limit + 1];
            for (int i = 2; i <= limit; i++) isPrime[i] = true;

            for (int i = 2; i * i <= limit; i++)
            {
                if (isPrime[i])
                {
                    for (int j = i * i; j <= limit; j += i)
                        isPrime[j] = false;
                }
            }

            var primes = new List<int>();
            for (int i = 2; i <= limit; i++)
            {
                if (isPrime[i]) primes.Add(i);
            }

            return primes;
        }
    }
}