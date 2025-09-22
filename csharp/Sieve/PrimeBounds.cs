using System;

namespace Sieve
{
    /// <summary>
    /// Mathematical bounds estimation for prime numbers using Dusart's improved bounds.
    /// Provides upper and lower bound estimates for the nth prime number.
    /// </summary>
    public static class PrimeBounds
    {
        /// <summary>
        /// Estimates an upper bound for the k-th prime (k = n+1) with a modest safety factor.
        /// </summary>
        /// <remarks>
        /// For small k, returns a conservative constant or linear fudge to avoid undershoot.
        /// For k ≥ 6, uses n (log n + log log n - 1 + (log log n - 2)/log n) with a 1.25× cushion.
        /// See <see href="https://doi.org/10.4153/CJM-1999-066-8">Dusart 1999</see> for bounds.
        /// </remarks>
        public static long EstimateUpperBound(long k)
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
        /// Estimates a lower bound for the kth prime using improved bounds.
        /// </summary>
        public static long EstimateLowerBound(long k)
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