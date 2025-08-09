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
    }
}