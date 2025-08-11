using Microsoft.AspNetCore.Mvc;
using ea_Tracker.Data;
using Microsoft.EntityFrameworkCore;

namespace ea_Tracker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CompletedInvestigationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CompletedInvestigationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCompleted()
        {
            // Return investigations that have actually run (have results), regardless of formal status
            var completedExecutions = await _context.InvestigationExecutions
                .Include(e => e.Investigator)
                .Where(e => e.ResultCount > 0) // Investigations that have actually run
                .OrderByDescending(e => e.StartedAt)
                .Select(e => new
                {
                    executionId = e.Id,
                    investigatorId = e.InvestigatorId,
                    investigatorName = e.Investigator.CustomName ?? "Investigation",
                    startedAt = e.StartedAt,
                    completedAt = e.StartedAt, // Use StartedAt as a fallback since CompletedAt is null
                    resultCount = e.ResultCount,
                    anomalyCount = _context.InvestigationResults
                        .Count(r => r.ExecutionId == e.Id && 
                              (r.Severity == ea_Tracker.Enums.ResultSeverity.Anomaly || 
                               r.Severity == ea_Tracker.Enums.ResultSeverity.Critical))
                })
                .ToListAsync();

            return Ok(completedExecutions);
        }

        [HttpGet("{executionId}")]
        public async Task<IActionResult> GetInvestigationDetail(int executionId)
        {
            // Get the execution summary
            var execution = await _context.InvestigationExecutions
                .Include(e => e.Investigator)
                .FirstOrDefaultAsync(e => e.Id == executionId);

            if (execution == null)
            {
                return NotFound($"Investigation execution with ID {executionId} not found.");
            }

            // Get the detailed results for this execution
            var results = await _context.InvestigationResults
                .Where(r => r.ExecutionId == executionId)
                .OrderBy(r => r.Timestamp)
                .Take(100) // Limit to prevent huge payloads
                .Select(r => new
                {
                    investigatorId = execution.InvestigatorId.ToString(),
                    timestamp = r.Timestamp,
                    message = r.Message,
                    payload = r.Payload
                })
                .ToListAsync();

            // Build the response matching the frontend's InvestigationDetail interface
            var response = new
            {
                summary = new
                {
                    executionId = execution.Id,
                    investigatorId = execution.InvestigatorId,
                    investigatorName = execution.Investigator.CustomName ?? "Investigation",
                    startedAt = execution.StartedAt,
                    completedAt = execution.CompletedAt ?? execution.StartedAt,
                    resultCount = execution.ResultCount,
                    anomalyCount = await _context.InvestigationResults
                        .CountAsync(r => r.ExecutionId == executionId && 
                              (r.Severity == ea_Tracker.Enums.ResultSeverity.Anomaly || 
                               r.Severity == ea_Tracker.Enums.ResultSeverity.Critical))
                },
                detailedResults = results
            };

            return Ok(response);
        }

        [HttpDelete("clear")]
        public async Task<IActionResult> ClearAllCompletedInvestigations()
        {
            try
            {
                // Delete all investigation results
                var resultsDeleted = await _context.InvestigationResults.ExecuteDeleteAsync();
                
                // Delete all investigation executions
                var executionsDeleted = await _context.InvestigationExecutions.ExecuteDeleteAsync();
                
                await _context.SaveChangesAsync();

                return Ok(new 
                { 
                    message = "All investigation results cleared successfully",
                    resultsDeleted = resultsDeleted,
                    executionsDeleted = executionsDeleted
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to clear investigation results", error = ex.Message });
            }
        }

        [HttpDelete("{executionId}")]
        public async Task<IActionResult> DeleteInvestigationExecution(int executionId)
        {
            try
            {
                // Delete related results first
                var resultsDeleted = await _context.InvestigationResults
                    .Where(r => r.ExecutionId == executionId)
                    .ExecuteDeleteAsync();
                
                // Delete the execution
                var execution = await _context.InvestigationExecutions.FindAsync(executionId);
                if (execution != null)
                {
                    _context.InvestigationExecutions.Remove(execution);
                    await _context.SaveChangesAsync();
                }

                return Ok(new 
                { 
                    message = $"Investigation execution {executionId} deleted successfully",
                    resultsDeleted = resultsDeleted
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to delete investigation execution", error = ex.Message });
            }
        }
    }
}