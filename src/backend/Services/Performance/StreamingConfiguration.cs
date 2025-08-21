using System;

namespace ea_Tracker.Services.Performance
{
    /// <summary>
    /// Configuration for streaming and batch processing performance optimization.
    /// Provides configurable batch sizes and processing limits for large datasets.
    /// </summary>
    public class StreamingConfiguration
    {
        /// <summary>
        /// Default batch size for processing operations.
        /// </summary>
        public int DefaultBatchSize { get; set; } = 1000;

        /// <summary>
        /// Batch size specifically for waybill processing operations.
        /// Optimized for waybill dataset characteristics.
        /// </summary>
        public int WaybillBatchSize { get; set; } = 500;

        /// <summary>
        /// Batch size specifically for invoice processing operations.
        /// Optimized for invoice dataset characteristics.
        /// </summary>
        public int InvoiceBatchSize { get; set; } = 500;

        /// <summary>
        /// Maximum items to buffer in memory during streaming operations.
        /// Prevents excessive memory usage during peak processing.
        /// </summary>
        public int MaxBufferSize { get; set; } = 2000;

        /// <summary>
        /// Enable memory usage tracking during streaming operations.
        /// </summary>
        public bool EnableMemoryTracking { get; set; } = true;

        /// <summary>
        /// Enable performance metrics collection during streaming operations.
        /// </summary>
        public bool EnablePerformanceMetrics { get; set; } = true;

        /// <summary>
        /// Timeout for individual batch processing operations in milliseconds.
        /// </summary>
        public int BatchTimeoutMs { get; set; } = 30000; // 30 seconds

        /// <summary>
        /// Feature flag to enable/disable streaming optimization.
        /// Allows rollback to original implementation if needed.
        /// </summary>
        public bool EnableStreamingOptimization { get; set; } = true;
    }
}