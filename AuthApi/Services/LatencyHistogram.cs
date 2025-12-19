using System.Threading;

namespace AuthApi.Services
{
    /// <summary>
    /// Fixed latency histogram in milliseconds with Prometheus-friendly buckets.
    /// </summary>
    public sealed class LatencyHistogram
    {
        // Upper bounds in ms (sorted). +Inf is implicit.
        private static readonly double[] _bounds = new double[]
        {
            5, 10, 25, 50, 100, 250, 500, 1000, 2500, 5000, 10000
        };

        // Non-cumulative bucket counts; length = _bounds.Length + 1 (last = +Inf)
        private readonly long[] _bucketCounts = new long[_bounds.Length + 1];

        // Sum of all observed durations (ms) for average calculation.
        private double _sumMs;

        public static double[] Bounds => _bounds;

        public void Observe(double durationMs)
        {
            // binary search to find first bucket upper bound >= duration
            int lo = 0, hi = _bounds.Length - 1, idx = _bounds.Length; // default +Inf
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                if (durationMs <= _bounds[mid])
                {
                    idx = mid;
                    hi = mid - 1;
                }
                else
                {
                    lo = mid + 1;
                }
            }

            Interlocked.Increment(ref _bucketCounts[idx]);

            // Sum: we can accept minor race; Interlocked for doubles in .NET is limited.
            // Using lock-free approximation is fine for metrics.
            double initial, computed;
            do
            {
                initial = _sumMs;
                computed = initial + durationMs;
            } while (System.Threading.Interlocked.CompareExchange(ref _sumMs, computed, initial) != initial);
        }

        public HistogramSnapshot Snapshot()
        {
            var copy = new long[_bucketCounts.Length];
            for (int i = 0; i < copy.Length; i++)
                copy[i] = Interlocked.Read(ref _bucketCounts[i]);

            var sum = _sumMs; // non-atomic read is fine for metrics

            return new HistogramSnapshot(_bounds, copy, sum);
        }

        public void Reset()
        {
            for (int i = 0; i < _bucketCounts.Length; i++)
                Interlocked.Exchange(ref _bucketCounts[i], 0);
            Interlocked.Exchange(ref _sumMs, 0);
        }
    }

    public sealed class HistogramSnapshot
    {
        public double[] Bounds { get; }
        public long[] BucketCounts { get; }     // non-cumulative counts
        public long Count { get; }              // total samples
        public double SumMs { get; }            // sum of durations (ms)

        public HistogramSnapshot(double[] bounds, long[] bucketCounts, double sumMs)
        {
            Bounds = bounds;
            BucketCounts = bucketCounts;
            SumMs = sumMs;

            long total = 0;
            foreach (var c in bucketCounts) total += c;
            Count = total;
        }

        public double AvgMs => Count == 0 ? 0 : SumMs / Count;

        /// <summary>
        /// Approximate percentile using bucket midpoints.
        /// </summary>
        public double Percentile(double p)
        {
            if (Count == 0) return 0;

            long rank = (long)System.Math.Ceiling(p * Count);
            long running = 0;

            for (int i = 0; i < BucketCounts.Length; i++)
            {
                running += BucketCounts[i];
                if (running >= rank)
                {
                    double low = i == 0 ? 0 : Bounds[i - 1];
                    double high = i < Bounds.Length ? Bounds[i] : double.PositiveInfinity;
                    return double.IsInfinity(high) ? low : (low + high) / 2.0;
                }
            }

            return Bounds[^1];
        }

        public long[] ToCumulative()
        {
            var cumulative = new long[BucketCounts.Length];
            long sum = 0;
            for (int i = 0; i < BucketCounts.Length; i++)
            {
                sum += BucketCounts[i];
                cumulative[i] = sum;
            }
            return cumulative;
        }
    }
}
