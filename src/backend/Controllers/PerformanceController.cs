using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ea_Tracker.Services;
using System.Threading.Tasks;

namespace ea_Tracker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "AdminOnly")] // Require admin access for performance analysis
    /// <summary>
    /// API endpoints for performance analysis and profiling of the investigation system.
    /// Provides evidence-based metrics to identify actual bottlenecks.
    /// </summary>
    public class PerformanceController : ControllerBase
    {
        private readonly InvestigationPerformanceProfiler _profiler;
        private readonly ILogger<PerformanceController> _logger;

        public PerformanceController(
            InvestigationPerformanceProfiler profiler,
            ILogger<PerformanceController> logger)
        {
            _profiler = profiler;
            _logger = logger;
        }

        /// <summary>
        /// Executes comprehensive performance analysis of the investigation system.
        /// Returns concrete metrics and timing data for all system components.
        /// </summary>
        [HttpPost("analyze")]
        public async Task<ActionResult<InvestigationPerformanceProfiler.PerformanceAnalysisResult>> AnalyzePerformance()
        {
            _logger.LogInformation("Performance analysis requested by user: {User}", User.Identity?.Name);

            try
            {
                var result = await _profiler.PerformComprehensiveAnalysisAsync();
                
                _logger.LogInformation("Performance analysis completed successfully. Total execution time: {Duration}ms", 
                    result.Investigation.TotalExecutionTimeMs);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during performance analysis");
                return StatusCode(500, new { 
                    message = "Performance analysis failed", 
                    error = ex.Message 
                });
            }
        }

        /// <summary>
        /// Analyzes the claimed performance bottleneck pattern.
        /// Provides concrete evidence about actual vs. claimed database usage patterns.
        /// </summary>
        [HttpPost("analyze-bottleneck")]
        public async Task<ActionResult<string>> AnalyzeClaimedBottleneck()
        {
            _logger.LogInformation("Bottleneck analysis requested by user: {User}", User.Identity?.Name);

            try
            {
                var result = await _profiler.AnalyzeClaimedBottleneckAsync();
                
                _logger.LogInformation("Bottleneck analysis completed successfully");

                return Ok(new { analysis = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bottleneck analysis");
                return StatusCode(500, new { 
                    message = "Bottleneck analysis failed", 
                    error = ex.Message 
                });
            }
        }

        /// <summary>
        /// Returns a simple performance summary for dashboard purposes.
        /// </summary>
        [HttpGet("summary")]
        public async Task<ActionResult<object>> GetPerformanceSummary()
        {
            try
            {
                var result = await _profiler.PerformComprehensiveAnalysisAsync();
                
                return Ok(new
                {
                    timestamp = result.AnalysisTimestamp,
                    totalExecutionTime = result.Investigation.TotalExecutionTimeMs,
                    databaseLoadTime = result.Database.InvoiceLoadTimeMs + result.Database.WaybillLoadTimeMs,
                    resultsGenerated = result.Investigation.TotalResultsGenerated,
                    memoryUsage = result.Memory.MemoryDeltaMB,
                    performanceStatus = result.Investigation.TotalExecutionTimeMs > 2700 ? "Poor" :
                                       result.Investigation.TotalExecutionTimeMs > 1000 ? "Moderate" : "Good"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting performance summary");
                return StatusCode(500, new { 
                    message = "Performance summary failed", 
                    error = ex.Message 
                });
            }
        }
    }
}