using Microsoft.EntityFrameworkCore;
using ea_Tracker.Data;
using ea_Tracker.Models;
using ea_Tracker.Models.Dtos;
using ea_Tracker.Enums;

namespace ea_Tracker.Repositories
{
    /// <summary>
    /// Repository implementation for investigator-related operations.
    /// </summary>
    public class InvestigatorRepository : GenericRepository<InvestigatorInstance>, IInvestigatorRepository
    {
        public InvestigatorRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<InvestigatorInstance>> GetActiveWithTypesAsync()
        {
            // Load investigators with type first
            var investigators = await _dbSet
                .Where(i => i.IsActive)
                .Include(i => i.Type)
                .OrderBy(i => i.Type.DisplayName)
                .ThenBy(i => i.CreatedAt)
                .ToListAsync();

            // For each, fetch last execution separately to avoid multi-collection single query issues
            foreach (var inv in investigators)
            {
                inv.Executions = await _context.InvestigationExecutions
                    .Where(e => e.InvestigatorId == inv.Id)
                    .OrderByDescending(e => e.StartedAt)
                    .Take(1)
                    .ToListAsync();
            }

            return investigators;
        }

        public async Task<IEnumerable<InvestigatorInstance>> GetByTypeAsync(string typeCode)
        {
            return await _dbSet
                .Include(i => i.Type)
                .Where(i => i.Type.Code == typeCode && i.IsActive)
                .OrderBy(i => i.CreatedAt)
                .ToListAsync();
        }

        public async Task<InvestigatorInstance?> GetWithDetailsAsync(Guid id)
        {
            // Load instance and type first
            var instance = await _dbSet
                .Include(i => i.Type)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (instance == null)
            {
                return null;
            }

            // Load executions separately
            instance.Executions = await _context.InvestigationExecutions
                .Where(e => e.InvestigatorId == id)
                .OrderByDescending(e => e.StartedAt)
                .ToListAsync();

            // Load top results per execution separately
            foreach (var exec in instance.Executions)
            {
                exec.Results = await _context.InvestigationResults
                    .Where(r => r.ExecutionId == exec.Id)
                    .OrderByDescending(r => r.Timestamp)
                    .Take(50)
                    .ToListAsync();
            }

            return instance;
        }

        public async Task<IEnumerable<InvestigationExecution>> GetExecutionHistoryAsync(Guid investigatorId, int take = 10)
        {
            return await _context.InvestigationExecutions
                .Where(e => e.InvestigatorId == investigatorId)
                .OrderByDescending(e => e.StartedAt)
                .Take(take)
                .ToListAsync();
        }

        public async Task<InvestigatorSummaryDto> GetSummaryAsync()
        {
            var totalInvestigators = await _dbSet.CountAsync();
            var activeInvestigators = await _dbSet.CountAsync(i => i.IsActive);
            
            // Count running investigators (those with running executions)
            var runningInvestigators = await _dbSet
                .Where(i => i.IsActive && i.Executions.Any(e => e.Status == ExecutionStatus.Running))
                .CountAsync();

            var totalExecutions = await _context.InvestigationExecutions.CountAsync();
            var totalResults = await _context.InvestigationResults.LongCountAsync();

            return new InvestigatorSummaryDto
            {
                TotalInvestigators = totalInvestigators,
                ActiveInvestigators = activeInvestigators,
                RunningInvestigators = runningInvestigators,
                TotalExecutions = totalExecutions,
                TotalResults = totalResults
            };
        }

        public async Task UpdateLastExecutedAsync(Guid investigatorId, DateTime timestamp)
        {
            var investigator = await _dbSet.FindAsync(investigatorId);
            if (investigator != null)
            {
                investigator.LastExecutedAt = timestamp;
                _context.Entry(investigator).Property(i => i.LastExecutedAt).IsModified = true;
            }
        }

        /// <summary>
        /// Gets investigator instances that haven't been executed recently.
        /// </summary>
        public async Task<IEnumerable<InvestigatorInstance>> GetStaleInvestigatorsAsync(TimeSpan stalePeriod)
        {
            var cutoff = DateTime.UtcNow - stalePeriod;
            
            return await _dbSet
                .Include(i => i.Type)
                .Where(i => i.IsActive && 
                           (i.LastExecutedAt == null || i.LastExecutedAt < cutoff))
                .OrderBy(i => i.LastExecutedAt ?? i.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Gets performance metrics for investigators.
        /// </summary>
        public async Task<Dictionary<string, object>> GetPerformanceMetricsAsync()
        {
            var metrics = new Dictionary<string, object>();

            // Average execution time by type
            var avgExecutionTimes = await _context.InvestigationExecutions
                .Include(e => e.Investigator)
                    .ThenInclude(i => i.Type)
                .Where(e => e.CompletedAt.HasValue)
                .GroupBy(e => e.Investigator.Type.Code)
                .Select(g => new
                {
                    Type = g.Key,
                    AvgDurationMinutes = g.Average(e => (e.CompletedAt!.Value - e.StartedAt).TotalMinutes)
                })
                .ToListAsync();

            metrics["AverageExecutionTimes"] = avgExecutionTimes;

            // Success rate by type
            var successRates = await _context.InvestigationExecutions
                .Include(e => e.Investigator)
                    .ThenInclude(i => i.Type)
                .GroupBy(e => e.Investigator.Type.Code)
                .Select(g => new
                {
                    Type = g.Key,
                    Total = g.Count(),
                    Successful = g.Count(e => e.Status == ExecutionStatus.Completed),
                    SuccessRate = (double)g.Count(e => e.Status == ExecutionStatus.Completed) / g.Count() * 100
                })
                .ToListAsync();

            metrics["SuccessRates"] = successRates;

            return metrics;
        }
    }
}