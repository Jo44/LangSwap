using System.Collections.Generic;
using System.Diagnostics;

namespace LangSwap.tool;

// ----------------------------
// Performance Monitor
// ----------------------------
public static class PerformanceMonitor
{
    // ----------------------------
    // Metric storage
    // ----------------------------
    private static readonly Dictionary<string, MetricBuffer> data = [];

    // ----------------------------
    // Record elapsed time since startTimestamp
    // ----------------------------
    public static void Record(string key, long startTimestamp)
    {
        double us = (Stopwatch.GetTimestamp() - startTimestamp) * 1_000_000.0 / Stopwatch.Frequency;
        if (!data.TryGetValue(key, out MetricBuffer? buffer))
            data[key] = buffer = new MetricBuffer();
        buffer.Add(us);
    }

    // ----------------------------
    // Get snapshot of all stats
    // ----------------------------
    public static IReadOnlyDictionary<string, (double avg, double min, double max)> GetStats()
    {
        Dictionary<string, (double avg, double min, double max)> result = new(data.Count);
        foreach (KeyValuePair<string, MetricBuffer> kvp in data)
        {
            (double avg, double min, double max) = kvp.Value.Stats();
            result[kvp.Key] = (avg, min, max);
        }
        return result;
    }

    // ----------------------------
    // Reset all buffers
    // ----------------------------
    public static void Reset()
    {
        foreach (MetricBuffer buffer in data.Values)
            buffer.Clear();
    }

    // ----------------------------
    // Circular buffer of samples
    // ----------------------------
    private sealed class MetricBuffer
    {
        // Initialization
        private const int BufferSize = 300;
        private readonly double[] values = new double[BufferSize];
        private int head = 0;
        private int count = 0;

        // Add a new sample
        public void Add(double us)
        {
            values[head] = us;
            head = (head + 1) % BufferSize;
            if (count < BufferSize) count++;
        }

        // Compute average, min, max
        public (double avg, double min, double max) Stats()
        {
            if (count == 0) return (0, 0, 0);
            double sum = 0, min = double.MaxValue, max = 0;
            for (int i = 0; i < count; i++)
            {
                double v = values[i];
                sum += v;
                if (v < min) min = v;
                if (v > max) max = v;
            }
            return (sum / count, min, max);
        }

        // Clear buffer
        public void Clear()
        {
            head = 0;
            count = 0;
        }
    }

}