using LangSwap.translation;
using System;
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
    private static readonly Dictionary<StatEnum, MetricBuffer> data = [];

    // ----------------------------
    // Record elapsed time since startTimestamp
    // ----------------------------
    public static void Record(StatEnum stat, long startTimestamp)
    {
        double us = (Stopwatch.GetTimestamp() - startTimestamp) * 1_000_000.0 / Stopwatch.Frequency;
        if (!data.TryGetValue(stat, out MetricBuffer? buffer))
            data[stat] = buffer = new MetricBuffer();
        buffer.Add(us);
    }

    // ----------------------------
    // Get snapshot of all stats
    // ----------------------------
    public static IReadOnlyDictionary<string, double> GetStats()
    {
        Dictionary<string, double> result = new(data.Count);
        foreach (StatEnum stat in Enum.GetValues<StatEnum>())
        {
            if (!data.TryGetValue(stat, out MetricBuffer? buffer)) continue;
            result[stat.ToString()] = buffer.GetAverage();
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
        private const int BufferSize = 1000;
        private readonly double[] values = new double[BufferSize];
        private int head = 0;
        private int count = 0;

        // Add a new record
        public void Add(double us)
        {
            values[head] = us;
            head = (head + 1) % BufferSize;
            if (count < BufferSize) count++;
        }

        // Get average
        public double GetAverage()
        {
            if (count == 0) return 0;
            double sum = 0;
            for (int i = 0; i < count; i++)
            {
                sum += values[i];
            }
            return sum / count;
        }

        // Clear buffer
        public void Clear()
        {
            head = 0;
            count = 0;
        }
    }

}