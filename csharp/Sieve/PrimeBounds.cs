namespace Sieve
{
    /// <summary>
    /// Prime number bounds estimation using Dusart's formulas.
    /// </summary>
    public static class PrimeBounds
    {
        /// <summary>
        /// Estimates upper bound for k-th prime using Dusart's formula with 1.25× safety margin.
        /// </summary>
        public static long EstimateUpperBound(long k)
        {
            if (k < 6) return 30;
            if (k < 100) return k * 15;

            double n = k;
            double logN = Math.Log(n);
            double logLogN = Math.Log(logN);

            double estimate = n * (logN + logLogN - 1 + (logLogN - 2) / logN);
            return (long)(estimate * 1.25); // safety margin
        }

        /// <summary>
        /// Estimates lower bound for k-th prime using Dusart's formula.
        /// </summary>
        public static long EstimateLowerBound(long k)
        {
            if (k < 6) return 2;

            double n = k;
            double logN = Math.Log(n);
            double logLogN = Math.Log(logN);

            // Dusart's lower bound formula
            double lowerEstimate = n * (logN + logLogN - 1.0);

            return Math.Max(2, (long)(lowerEstimate * 0.95)); // 5% safety margin
        }
    }
}