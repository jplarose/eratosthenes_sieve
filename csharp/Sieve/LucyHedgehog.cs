namespace Sieve
{
    /// <summary>
    /// Lucy-Hedgehog prime counting algorithm with O(x^(3/4)) complexity.
    /// </summary>
    public static class LucyHedgehog
    {
        /// <summary>
        /// Counts primes ≤ x using Lucy-Hedgehog algorithm.
        /// </summary>
        public static long PrimeCount(long x)
        {
            if (x < 2) return 0;
            if (x == 2) return 1;

            long sqrtX = (long)Math.Sqrt(x);
            var smallPrimes = SievePrimes((int)Math.Min(sqrtX, int.MaxValue - 1));
            return PrimeCount(x, smallPrimes);
        }

        /// <summary>
        /// Counts primes ≤ x using precomputed base primes up to √x.
        /// </summary>
        public static long PrimeCount(long x, List<int> smallPrimes)
        {
            if (x < 2) return 0;
            if (x == 2) return 1;

            long sqrt_x = (long)Math.Sqrt(x);

            // Create value set: {1..√x} ∪ {x/1, x/2, ..., x/√x}
            var values = new HashSet<long>();

            // Add values 1..√x and x/1..x/√x
            for (long i = 1; i <= sqrt_x; i++)
            {
                values.Add(i);
                values.Add(x / i);
            }

            var W = values.OrderByDescending(v => v).ToArray();
            int len = W.Length;

            // Initialize S[n] = count of integers 2..n = n-1
            var S = new long[len];
            for (int i = 0; i < len; i++)
                S[i] = W[i] - 1;

            // Build value lookup table
            var valueToIndex = new Dictionary<long, int>();
            for (int i = 0; i < len; i++)
                valueToIndex[W[i]] = i;

            // Apply sieving for each prime
            foreach (int p in smallPrimes)
            {
                if (p < 2) continue;
                if ((long)p * p > x) break;

                // Get count of primes < p
                long sp_minus_1 = 0;
                if (p > 1)
                {
                    if (valueToIndex.ContainsKey(p - 1))
                        sp_minus_1 = S[valueToIndex[p - 1]];
                    else
                        sp_minus_1 = (p - 1) - 1;
                }

                // Subtract multiples of p from counts
                for (int i = 0; i < len; i++)
                {
                    long n = W[i];
                    if (n < (long)p * p) break; // W is sorted descending

                    long q = n / p;
                    long sq = valueToIndex.ContainsKey(q) ? S[valueToIndex[q]] : q - 1;

                    S[i] -= (sq - sp_minus_1);
                }
            }

            // Return final count
            return valueToIndex.ContainsKey(x) ? S[valueToIndex[x]] : x - 1;
        }

        /// <summary>
        /// Simple sieve for generating base primes up to limit.
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