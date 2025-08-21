using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace ea_Tracker.Services.Performance
{
    /// <summary>
    /// Performance monitoring and metrics collection for streaming operations.
    /// Tracks memory usage, processing time, and throughput metrics.
    /// </summary>
    public class PerformanceMetrics
    {
        private readonly ILogger<PerformanceMetrics>? _logger;
        private readonly Dictionary<string, Stopwatch> _timers;
        private readonly Dictionary<string, long> _counters;
        private long _initialMemory;

        public PerformanceMetrics(ILogger<PerformanceMetrics>? logger = null)
        {
            _logger = logger;
            _timers = new Dictionary<string, Stopwatch>();
            _counters = new Dictionary<string, long>();
            _initialMemory = GC.GetTotalMemory(false);
        }

        /// <summary>
        /// Starts timing an operation.
        /// </summary>
        public void StartTimer(string operationName)
        {
            if (!_timers.ContainsKey(operationName))
            {
                _timers[operationName] = new Stopwatch();
            }
            _timers[operationName].Restart();
        }

        /// <summary>
        /// Stops timing an operation and returns elapsed milliseconds.
        /// </summary>
        public long StopTimer(string operationName)
        {
            if (_timers.TryGetValue(operationName, out var timer))
            {
                timer.Stop();
                var elapsed = timer.ElapsedMilliseconds;
                _logger?.LogInformation($"Operation '{operationName}' completed in {elapsed}ms");
                return elapsed;
            }
            return 0;
        }

        /// <summary>
        /// Increments a counter by a specified value.
        /// </summary>
        public void IncrementCounter(string counterName, long value = 1)
        {
            if (!_counters.ContainsKey(counterName))
            {
                _counters[counterName] = 0;
            }
            _counters[counterName] += value;
        }

        /// <summary>
        /// Gets the current value of a counter.
        /// </summary>
        public long GetCounter(string counterName)
        {
            return _counters.TryGetValue(counterName, out var value) ? value : 0;
        }

        /// <summary>
        /// Gets current memory usage in bytes.
        /// </summary>
        public long GetCurrentMemoryUsage()
        {
            return GC.GetTotalMemory(false);
        }

        /// <summary>
        /// Gets memory usage delta from initial measurement in bytes.
        /// </summary>
        public long GetMemoryDelta()
        {
            return GetCurrentMemoryUsage() - _initialMemory;
        }

        /// <summary>
        /// Forces garbage collection and gets memory usage.
        /// Use sparingly as it impacts performance.
        /// </summary>
        public long GetMemoryUsageAfterGC()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            return GC.GetTotalMemory(true);
        }

        /// <summary>
        /// Resets initial memory baseline.
        /// </summary>
        public void ResetMemoryBaseline()
        {
            _initialMemory = GetCurrentMemoryUsage();
        }

        /// <summary>
        /// Gets comprehensive performance summary.
        /// </summary>
        public PerformanceSummary GetSummary()
        {
            var timerResults = new Dictionary<string, long>();
            foreach (var timer in _timers)
            {
                timerResults[timer.Key] = timer.Value.ElapsedMilliseconds;
            }
            
            return new PerformanceSummary
            {
                Timers = timerResults,
                Counters = new Dictionary<string, long>(_counters),
                CurrentMemory = GetCurrentMemoryUsage(),
                MemoryDelta = GetMemoryDelta(),
                MeasuredAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Logs performance summary to the configured logger.
        /// </summary>
        public void LogSummary(string operationName)
        {
            if (_logger == null) return;

            var summary = GetSummary();
            var memoryMB = summary.CurrentMemory / (1024.0 * 1024.0);
            var deltaMB = summary.MemoryDelta / (1024.0 * 1024.0);

            _logger.LogInformation($"Performance Summary for '{operationName}':");
            _logger.LogInformation($"  Memory: {memoryMB:F1} MB (Δ{deltaMB:+F1;-F1;0} MB)");
            
            foreach (var timer in summary.Timers)
            {
                _logger.LogInformation($"  Timer '{timer.Key}': {timer.Value}ms");
            }
            
            foreach (var counter in summary.Counters)
            {
                _logger.LogInformation($"  Counter '{counter.Key}': {counter.Value}");
            }
        }
    }

    /// <summary>
    /// Performance metrics summary data transfer object.
    /// </summary>
    public class PerformanceSummary
    {
        public Dictionary<string, long> Timers { get; set; } = new();
        public Dictionary<string, long> Counters { get; set; } = new();
        public long CurrentMemory { get; set; }
        public long MemoryDelta { get; set; }
        public DateTime MeasuredAt { get; set; }
    }
}