using ea_Tracker.Data;
using ea_Tracker.Enums;
using ea_Tracker.Models;
using ea_Tracker.Models.Dtos;
using ea_Tracker.Repositories;
using ea_Tracker.Services.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace ea_Tracker.Services.Implementations
{
    /// <summary>
    /// Service implementation for completed investigation operations.
    /// Refactored to use Repository Pattern for consistent data access across all services.
    /// </summary>
    public class CompletedInvestigationService : ICompletedInvestigationService
    {
        private readonly IGenericRepository<InvestigationExecution> _executionRepository;
        private readonly IGenericRepository<InvestigationResult> _resultRepository;
        private readonly IGenericRepository<InvestigatorInstance> _investigatorRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<CompletedInvestigationService> _logger;

        public CompletedInvestigationService(
            IGenericRepository<InvestigationExecution> executionRepository,
            IGenericRepository<InvestigationResult> resultRepository,
            IGenericRepository<InvestigatorInstance> investigatorRepository,
            IMapper mapper,
            ILogger<CompletedInvestigationService> logger)
        {
            _executionRepository = executionRepository;
            _resultRepository = resultRepository;
            _investigatorRepository = investigatorRepository;
            _mapper = mapper;
            _logger = logger;
        }

        // Constants for include properties to avoid magic strings
        private const string INCLUDE_INVESTIGATOR = "Investigator";

        public async Task<IEnumerable<CompletedInvestigationDto>> GetAllCompletedAsync()
        {
            _logger.LogDebug("Retrieving all completed investigations");

            // Get executions with includes using repository pattern
            var completedExecutions = await _executionRepository.GetAsync(
                filter: e => e.ResultCount > 0,
                orderBy: q => q.OrderByDescending(e => e.StartedAt),
                includeProperties: INCLUDE_INVESTIGATOR
            );

            // Process results with proper null checking
            var results = new List<CompletedInvestigationDto>();
            foreach (var execution in completedExecutions)
            {
                // Get anomaly count using result repository
                var anomalyCount = await _resultRepository.CountAsync(
                    r => r.ExecutionId == execution.Id && 
                         (r.Severity == ResultSeverity.Anomaly || 
                          r.Severity == ResultSeverity.Critical)
                );

                results.Add(new CompletedInvestigationDto(
                    ExecutionId: execution.Id,
                    InvestigatorId: execution.InvestigatorId,
                    InvestigatorName: execution.Investigator?.CustomName ?? "Investigation",
                    StartedAt: execution.StartedAt,
                    CompletedAt: execution.CompletedAt ?? execution.StartedAt,
                    Duration: CalculateDuration(execution.StartedAt, execution.CompletedAt ?? execution.StartedAt),
                    ResultCount: execution.ResultCount,
                    AnomalyCount: anomalyCount
                ));
            }

            _logger.LogInformation("Retrieved {Count} completed investigations", results.Count);
            return results;
        }

        public async Task<InvestigationDetailDto?> GetInvestigationDetailAsync(int executionId)
        {
            _logger.LogDebug("Retrieving investigation detail for execution {ExecutionId}", executionId);

            // Use repository to get execution with includes
            var executions = await _executionRepository.GetAsync(
                filter: e => e.Id == executionId,
                includeProperties: INCLUDE_INVESTIGATOR
            );
            
            var execution = executions.FirstOrDefault();
            if (execution == null)
            {
                _logger.LogWarning("Investigation execution {ExecutionId} not found", executionId);
                return null;
            }

            // Get anomaly count using repository
            var anomalyCount = await _resultRepository.CountAsync(
                r => r.ExecutionId == executionId && 
                     (r.Severity == ResultSeverity.Anomaly || 
                      r.Severity == ResultSeverity.Critical)
            );

            // Create summary DTO
            var summary = new CompletedInvestigationDto(
                ExecutionId: execution.Id,
                InvestigatorId: execution.InvestigatorId,
                InvestigatorName: execution.Investigator?.CustomName ?? "Investigation",
                StartedAt: execution.StartedAt,
                CompletedAt: execution.CompletedAt ?? execution.StartedAt,
                Duration: CalculateDuration(execution.StartedAt, execution.CompletedAt ?? execution.StartedAt),
                ResultCount: execution.ResultCount,
                AnomalyCount: anomalyCount
            );

            // Get detailed results using repository
            var results = await _resultRepository.GetAsync(
                filter: r => r.ExecutionId == executionId,
                orderBy: q => q.OrderBy(r => r.Timestamp)
            );

            // Take only first 100 results (maintaining existing behavior) and map to DTOs
            var detailedResults = results.Take(100).Select(r => new InvestigatorResultDto(
                execution.InvestigatorId,
                r.Timestamp,
                r.Message,
                r.Payload
            ));

            return new InvestigationDetailDto(summary, detailedResults);
        }

        public async Task<ClearInvestigationsResultDto> ClearAllCompletedInvestigationsAsync()
        {
            _logger.LogInformation("Clearing all completed investigations");

            // Get all results and executions for counting before deletion
            var allResults = await _resultRepository.GetAllAsync();
            var allExecutions = await _executionRepository.GetAllAsync();
            
            var resultsCount = allResults.Count();
            var executionsCount = allExecutions.Count();

            // Remove all results using repository pattern
            if (resultsCount > 0)
            {
                _resultRepository.RemoveRange(allResults);
                await _resultRepository.SaveChangesAsync();
            }

            // Remove all executions using repository pattern
            if (executionsCount > 0)
            {
                _executionRepository.RemoveRange(allExecutions);
                await _executionRepository.SaveChangesAsync();
            }

            _logger.LogInformation("Cleared {Results} results and {Executions} executions", 
                resultsCount, executionsCount);

            return new ClearInvestigationsResultDto(
                Message: "All investigation results cleared successfully",
                ResultsDeleted: resultsCount,
                ExecutionsDeleted: executionsCount
            );
        }

        public async Task<DeleteInvestigationResultDto> DeleteInvestigationExecutionAsync(int executionId)
        {
            _logger.LogInformation("Deleting investigation execution {ExecutionId}", executionId);

            // Get and remove related results using repository
            var results = await _resultRepository.GetAsync(
                filter: r => r.ExecutionId == executionId
            );
            
            var resultsCount = results.Count();
            if (resultsCount > 0)
            {
                _resultRepository.RemoveRange(results);
                await _resultRepository.SaveChangesAsync();
            }

            // Remove execution using repository
            var execution = await _executionRepository.GetByIdAsync(executionId);
            if (execution != null)
            {
                _executionRepository.Remove(execution);
                await _executionRepository.SaveChangesAsync();
            }

            _logger.LogInformation("Deleted execution {ExecutionId} with {Results} results", 
                executionId, resultsCount);

            return new DeleteInvestigationResultDto(
                Message: $"Investigation execution {executionId} deleted successfully",
                ResultsDeleted: resultsCount
            );
        }

        private static string CalculateDuration(DateTime startedAt, DateTime completedAt)
        {
            var duration = completedAt - startedAt;
            if (duration.TotalDays >= 1)
                return $"{duration.Days}d {duration.Hours}h {duration.Minutes}m";
            if (duration.TotalHours >= 1)
                return $"{duration.Hours}h {duration.Minutes}m {duration.Seconds}s";
            if (duration.TotalMinutes >= 1)
                return $"{duration.Minutes}m {duration.Seconds}s";
            return $"{duration.TotalSeconds:F1}s";
        }
    }
}