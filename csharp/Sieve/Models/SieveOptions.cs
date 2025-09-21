namespace Sieve
{
    /// <summary>
    /// Options that control how the sieve locates the nth prime.
    /// </summary>
    public class SieveOptions
    {
        /// <summary>
        /// How to choose the algorithm. <see cref="SieveMethod.Auto"/> selects
        /// <see cref="SieveMethod.Regular"/> for n &lt; <see cref="RegularSieveThreshold"/>,
        /// otherwise <see cref="SieveMethod.Segmented"/>.
        /// </summary>
        public SieveMethod Method { get; set; } = SieveMethod.Auto;

        /// <summary>
        /// Size of each segment when using the segmented sieve. Memory used by a single
        /// segment is roughly one boolean per number in the segment.
        /// If null, a default optimized size will be chosen automatically.
        /// </summary>
        public int? SegmentSize { get; set; }

        /// <summary>
        /// Boundary used by <see cref="SieveMethod.Auto"/> to decide when to switch from
        /// <see cref="SieveMethod.Regular"/> to <see cref="SieveMethod.Segmented"/>. Compared against n (0-based).
        /// </summary>
        public long RegularSieveThreshold { get; set; } = 1_000_000;

        /// <summary>
        /// Optional callback to receive advisory messages about potentially suboptimal option
        /// combinations. Logging is informational only; behavior is not changed.
        /// </summary>
        public Action<string>? Logger { get; set; }
    }
}