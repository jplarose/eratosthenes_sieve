namespace Sieve
{
    /// <summary>
    /// Configuration options for prime sieve algorithms.
    /// </summary>
    public class SieveOptions
    {
        /// <summary>
        /// Sieve algorithm selection. Auto chooses Regular for n &lt; 1M,
        /// Segmented for 1M-10M, and PrimeCounting for n ≥ 10M.
        /// </summary>
        public SieveMethod Method { get; set; } = SieveMethod.Auto;

        /// <summary>
        /// Segment size for segmented sieve (integers per segment).
        /// If null, uses optimized default of 1M.
        /// </summary>
        public int? SegmentSize { get; set; }

        /// <summary>
        /// Threshold for Auto mode to switch from Regular to Segmented sieve.
        /// </summary>
        public long RegularSieveThreshold { get; set; } = 1_000_000;

        /// <summary>
        /// Threshold for Auto mode to switch to Lucy-Hedgehog prime counting.
        /// </summary>
        public long PrimeCountingThreshold { get; set; } = 10_000_000;

        /// <summary>
        /// Optional callback for advisory messages about suboptimal configurations.
        /// </summary>
        public Action<string>? Logger { get; set; }
    }
}