using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using ea_Tracker.Models;
using ea_Tracker.Models.Dtos;
using ea_Tracker.Repositories;
using ea_Tracker.Enums;
using ea_Tracker.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace ea_Tracker.Services
{
    /// <summary>
    /// Manages a collection of investigators and coordinates their lifecycle with database persistence.
    /// Implements IInvestigationManager interface for SOLID compliance (Dependency Inversion Principle).
    /// </summary>
    public class InvestigationManager : IInvestigationManager
    {
        private readonly IInvestigatorFactory _factory;
        private readonly IInvestigatorRepository _investigatorRepository;
        private readonly IGenericRepository<InvestigationExecution> _executionRepository;
        private readonly IGenericRepository<InvestigationResult> _resultRepository;
        private readonly IGenericRepository<InvestigatorType> _investigatorTypeRepository;
        // Removed _runningInvestigators - investigations are one-shot operations
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IInvestigationNotificationService _notifier;

        /// <summary>
        /// Initializes a new instance of the <see cref="InvestigationManager"/> class.
        /// </summary>
        public InvestigationManager(
            IInvestigatorFactory factory,
            IInvestigatorRepository investigatorRepository,
            IGenericRepository<InvestigationExecution> executionRepository,
            IGenericRepository<InvestigationResult> resultRepository,
            IGenericRepository<InvestigatorType> investigatorTypeRepository,
            IServiceScopeFactory scopeFactory,
            IInvestigationNotificationService notifier)
        {
            _factory = factory;
            _investigatorRepository = investigatorRepository;
            _executionRepository = executionRepository;
            _resultRepository = resultRepository;
            _investigatorTypeRepository = investigatorTypeRepository;
            _scopeFactory = scopeFactory;
            _notifier = notifier;
        }



        /// <summary>
        /// Starts a single investigator.
        /// </summary>
        public async Task<bool> StartInvestigatorAsync(Guid id)
        {
            var investigatorInstance = await _investigatorRepository.GetWithDetailsAsync(id);
            if (investigatorInstance == null || !investigatorInstance.IsActive)
                return false;

            // Investigations are one-shot - no need to check if running

            try
            {
                var investigator = _factory.Create(investigatorInstance.Type.Code);
                investigator.Notifier = _notifier;
                investigator.ExternalId = id; // ensure SignalR uses persistent instance Id
                // Note: investigator creates its own internal Id, but we track it by database Id

                // Create new execution record
                var execution = new InvestigationExecution
                {
                    InvestigatorId = id,
                    StartedAt = DateTime.UtcNow,
                    Status = ExecutionStatus.Running
                };
                await _executionRepository.AddAsync(execution);
                await _executionRepository.SaveChangesAsync();

                // Set up result reporting - capture the execution Id in closure
                var executionId = execution.Id;
                investigator.Report = result => _ = SaveResultAsync(executionId, result);

                // Execute as one-shot operation
                investigator.Execute();
                
                // Mark execution as completed
                execution.Status = ExecutionStatus.Completed;
                execution.CompletedAt = DateTime.UtcNow;
                _executionRepository.Update(execution);
                await _executionRepository.SaveChangesAsync();

                // Update last executed timestamp
                await _investigatorRepository.UpdateLastExecutedAsync(id, DateTime.UtcNow);

                // Send completion notification AFTER database is updated
                await _notifier.StatusChangedAsync(id, "Completed");
                await _notifier.InvestigationCompletedAsync(id, execution.ResultCount, execution.CompletedAt.Value);

                return true;
            }
            catch (Exception ex)
            {
                // Log error and mark execution as failed
                var failedExecution = await _executionRepository.GetFirstOrDefaultAsync(e => e.InvestigatorId == id && e.Status == ExecutionStatus.Running);
                if (failedExecution != null)
                {
                    failedExecution.Status = ExecutionStatus.Failed;
                    failedExecution.CompletedAt = DateTime.UtcNow;
                    failedExecution.ErrorMessage = ex.Message;
                    _executionRepository.Update(failedExecution);
                    await _executionRepository.SaveChangesAsync();
                }
                return false;
            }
        }

        // Removed StopInvestigatorAsync - investigations are now one-shot operations

        /// <summary>
        /// Gets the state of all investigators.
        /// </summary>
        public async Task<IEnumerable<ea_Tracker.Models.Dtos.InvestigatorStateDto>> GetAllInvestigatorStatesAsync()
        {
            var investigators = await _investigatorRepository.GetActiveWithTypesAsync();
            return investigators.Select(i => new ea_Tracker.Models.Dtos.InvestigatorStateDto(
                i.Id,
                i.DisplayName,
                false, // Investigations are one-shot, never "running" after completion
                i.TotalResultCount));
        }

        /// <summary>
        /// Gets result logs for an investigator from recent executions.
        /// </summary>
        public async Task<IEnumerable<ea_Tracker.Models.Dtos.InvestigatorResultDto>> GetResultsAsync(Guid id, int take = 100)
        {
            var executions = await _investigatorRepository.GetExecutionHistoryAsync(id, 5);
            var results = new List<ea_Tracker.Models.Dtos.InvestigatorResultDto>();
            
            if (!executions.Any())
            {
                return results;
            }

            foreach (var execution in executions)
            {
                var executionResults = await _resultRepository.GetAsync(
                    filter: r => r.ExecutionId == execution.Id,
                    orderBy: q => q.OrderByDescending(r => r.Timestamp));
                
                var perExecutionTake = Math.Max(1, take / executions.Count());
                results.AddRange(executionResults.Take(perExecutionTake).Select(r => 
                    new ea_Tracker.Models.Dtos.InvestigatorResultDto(id, r.Timestamp, r.Message, r.Payload)));
            }
            
            return results.OrderByDescending(r => r.Timestamp).Take(take);
        }

        /// <summary>
        /// Creates a new investigator instance of the specified kind and registers it in the database.
        /// </summary>
        public async Task<Guid> CreateInvestigatorAsync(string typeCode, string? customName = null)
        {
            // Get the investigator type
            var investigatorType = await _investigatorTypeRepository.GetFirstOrDefaultAsync(t => t.Code == typeCode);
            if (investigatorType == null)
            {
                throw new InvalidOperationException($"Unknown investigator type: {typeCode}");
            }

            var investigatorInstance = new InvestigatorInstance
            {
                Id = Guid.NewGuid(),
                TypeId = investigatorType.Id,
                CustomName = customName,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            await _investigatorRepository.AddAsync(investigatorInstance);
            await _investigatorRepository.SaveChangesAsync();

            return investigatorInstance.Id;
        }

        /// <summary>
        /// Saves a result from an investigator execution to the database with atomic count updates.
        /// Fixed version that eliminates race conditions in ResultCount updates.
        /// </summary>
        private async Task SaveResultAsync(int executionId, InvestigationResult result)
        {
            // Use a fresh scope to avoid using scoped DbContext/Repositories across threads
            using var scope = _scopeFactory.CreateScope();
            var scopedResultRepo = scope.ServiceProvider.GetRequiredService<IGenericRepository<InvestigationResult>>();
            var scopedDbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            try
            {
                // Set the actual execution ID
                result.ExecutionId = executionId;

                // Save result first
                await scopedResultRepo.AddAsync(result);
                await scopedResultRepo.SaveChangesAsync();

                // Atomic count increment using raw SQL to prevent race conditions
                await IncrementResultCountAtomicAsync(scopedDbContext, executionId);
                
                // Log successful save for debugging
                // Note: Using Debug level to avoid excessive logging in production
                // This can be promoted to Information level during initial deployment for monitoring
            }
            catch (Exception ex)
            {
                // Log error with full context for debugging the count mismatch issue
                // Using structured logging to help track down any remaining issues
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<InvestigationManager>>();
                logger.LogError(ex, "Failed to save result for execution {ExecutionId}. Result message: {ResultMessage}", 
                    executionId, result.Message);
                throw;
            }
        }

        /// <summary>
        /// Atomically increments the result count for an investigation execution.
        /// Uses raw SQL UPDATE to prevent race conditions that occurred with Entity Framework's
        /// read-modify-write pattern. Includes retry logic for deadlock handling.
        /// </summary>
        /// <param name="context">The database context to use for the operation.</param>
        /// <param name="executionId">The execution ID to increment the count for.</param>
        /// <param name="retryCount">Current retry attempt (used internally for recursion).</param>
        private async Task IncrementResultCountAtomicAsync(ApplicationDbContext context, int executionId, int retryCount = 0)
        {
            const int maxRetries = 3;
            const string sql = @"
                UPDATE InvestigationExecutions 
                SET ResultCount = ResultCount + 1 
                WHERE Id = {0}";
            
            try
            {
                var rowsAffected = await context.Database.ExecuteSqlRawAsync(sql, executionId);
                
                if (rowsAffected == 0)
                {
                    // This could indicate the execution was deleted or doesn't exist
                    // Log warning but don't throw - the result was saved successfully
                    var logger = context.GetService<ILogger<InvestigationManager>>();
                    logger?.LogWarning("Failed to increment result count for execution {ExecutionId} - execution not found or already completed", executionId);
                }
            }
            catch (SqlException ex) when (ex.Number == 1205 && retryCount < maxRetries) // Deadlock
            {
                // Implement exponential backoff for deadlock retries
                var delay = (int)Math.Pow(2, retryCount) * 100; // 100ms, 200ms, 400ms
                await Task.Delay(delay);
                await IncrementResultCountAtomicAsync(context, executionId, retryCount + 1);
            }
            catch (Exception ex)
            {
                // Log the error but don't throw - the investigation result was saved successfully
                // The count can be corrected later using the verification/correction methods
                var logger = context.GetService<ILogger<InvestigationManager>>();
                logger?.LogError(ex, "Failed to increment result count atomically for execution {ExecutionId} after {RetryCount} retries", 
                    executionId, retryCount);
            }
        }

        /// <summary>
        /// Gets summary statistics for all investigators.
        /// </summary>
        public async Task<InvestigatorSummaryDto> GetSummaryAsync()
        {
            return await _investigatorRepository.GetSummaryAsync();
        }

        /// <summary>
        /// Deletes an investigator instance and all related data.
        /// Stops the investigator if running before deletion.
        /// </summary>
        public async Task<bool> DeleteInvestigatorAsync(Guid id)
        {
            try
            {
                // No need to stop - investigations are one-shot operations

                // Get the investigator instance
                var investigatorInstance = await _investigatorRepository.GetByIdAsync(id);
                if (investigatorInstance == null)
                    return false; // Investigator doesn't exist

                // Delete related execution results first (cascade delete)
                var executions = await _investigatorRepository.GetExecutionHistoryAsync(id, int.MaxValue);
                foreach (var execution in executions)
                {
                    // Delete all results for this execution
                    var results = await _resultRepository.GetAsync(r => r.ExecutionId == execution.Id);
                    if (results.Any())
                    {
                        _resultRepository.RemoveRange(results);
                        await _resultRepository.SaveChangesAsync();
                    }

                    // Delete the execution
                    _executionRepository.Remove(execution);
                }
                await _executionRepository.SaveChangesAsync();

                // Finally, delete the investigator instance
                _investigatorRepository.Remove(investigatorInstance);
                await _investigatorRepository.SaveChangesAsync();

                return true;
            }
            catch (Exception)
            {
                return false; // Delete failed
            }
        }

        /// <summary>
        /// Gets all completed investigations ordered by completion date (most recent first).
        /// </summary>
        public async Task<IEnumerable<CompletedInvestigationDto>> GetAllCompletedInvestigationsAsync()
        {
            var completedExecutions = await _executionRepository.GetAsync(
                e => e.Status == ExecutionStatus.Completed,
                orderBy: q => q.OrderByDescending(e => e.CompletedAt)
            );

            var results = new List<CompletedInvestigationDto>();

            foreach (var execution in completedExecutions)
            {
                // Get investigator info separately
                var investigator = await _investigatorRepository.GetByIdAsync(execution.InvestigatorId);
                if (investigator == null) continue;

                // Calculate anomaly count
                var anomalyCount = await _resultRepository.CountAsync(r => 
                    r.ExecutionId == execution.Id && 
                    (r.Severity == ResultSeverity.Anomaly || r.Severity == ResultSeverity.Critical)
                );

                var displayName = !string.IsNullOrEmpty(investigator.CustomName)
                    ? investigator.CustomName
                    : investigator.Type?.DisplayName ?? "Unknown";

                var duration = execution.CompletedAt.HasValue && execution.StartedAt != default
                    ? (execution.CompletedAt.Value - execution.StartedAt).ToString(@"mm\:ss")
                    : "00:00";

                results.Add(new CompletedInvestigationDto(
                    execution.Id,
                    execution.InvestigatorId,
                    displayName,
                    execution.StartedAt,
                    execution.CompletedAt ?? DateTime.UtcNow,
                    duration,
                    execution.ResultCount,
                    anomalyCount
                ));
            }

            return results;
        }

        /// <summary>
        /// Gets detailed information for a specific completed investigation.
        /// </summary>
        public async Task<InvestigationDetailDto?> GetInvestigationDetailsAsync(int executionId)
        {
            var execution = await _executionRepository.GetFirstOrDefaultAsync(
                e => e.Id == executionId && e.Status == ExecutionStatus.Completed
            );

            if (execution == null)
                return null;

            // Get investigator info separately
            var investigator = await _investigatorRepository.GetByIdAsync(execution.InvestigatorId);
            if (investigator == null)
                return null;

            // Get summary info
            var anomalyCount = await _resultRepository.CountAsync(r => 
                r.ExecutionId == executionId && 
                (r.Severity == ResultSeverity.Anomaly || r.Severity == ResultSeverity.Critical)
            );

            var displayName = !string.IsNullOrEmpty(investigator.CustomName)
                ? investigator.CustomName
                : investigator.Type?.DisplayName ?? "Unknown";

            var duration = execution.CompletedAt.HasValue && execution.StartedAt != default
                ? (execution.CompletedAt.Value - execution.StartedAt).ToString(@"mm\:ss")
                : "00:00";

            var summary = new CompletedInvestigationDto(
                execution.Id,
                execution.InvestigatorId,
                displayName,
                execution.StartedAt,
                execution.CompletedAt ?? DateTime.UtcNow,
                duration,
                execution.ResultCount,
                anomalyCount
            );

            // Get detailed results
            var detailedResults = await _resultRepository.GetAsync(
                r => r.ExecutionId == executionId,
                orderBy: q => q.OrderByDescending(r => r.Timestamp)
            );

            var resultDtos = detailedResults.Select(r => new InvestigatorResultDto(
                execution.InvestigatorId,
                r.Timestamp,
                r.Message,
                r.Payload
            ));

            return new InvestigationDetailDto(summary, resultDtos);
        }

        /// <summary>
        /// Exports investigation results in the specified format.
        /// </summary>
        public async Task<InvestigationExportDto?> ExportInvestigationResultsAsync(int executionId, string format)
        {
            var details = await GetInvestigationDetailsAsync(executionId);
            if (details == null)
                return null;

            byte[] data;
            string contentType;
            string fileExtension;

            switch (format.ToLower())
            {
                case "csv":
                    data = ExportToCsv(details);
                    contentType = "text/csv";
                    fileExtension = "csv";
                    break;

                case "excel":
                    // For now, treat as CSV. Full Excel support would require additional packages
                    data = ExportToCsv(details);
                    contentType = "text/csv";
                    fileExtension = "csv";
                    break;

                default: // json
                    data = ExportToJson(details);
                    contentType = "application/json";
                    fileExtension = "json";
                    break;
            }

            var fileName = $"investigation_{executionId}_results.{fileExtension}";
            return new InvestigationExportDto(data, contentType, fileName);
        }

        /// <summary>
        /// Gets the latest completed investigation for a specific investigator.
        /// </summary>
        public async Task<CompletedInvestigationDto?> GetLatestCompletedInvestigationAsync(Guid investigatorId)
        {
            var executions = await _executionRepository.GetAsync(
                e => e.InvestigatorId == investigatorId && e.Status == ExecutionStatus.Completed,
                orderBy: q => q.OrderByDescending(e => e.CompletedAt)
            );

            var latestExecution = executions.FirstOrDefault();

            if (latestExecution == null)
                return null;

            // Get investigator info separately
            var investigator = await _investigatorRepository.GetByIdAsync(latestExecution.InvestigatorId);
            if (investigator == null)
                return null;

            var anomalyCount = await _resultRepository.CountAsync(r => 
                r.ExecutionId == latestExecution.Id && 
                (r.Severity == ResultSeverity.Anomaly || r.Severity == ResultSeverity.Critical)
            );

            var displayName = !string.IsNullOrEmpty(investigator.CustomName)
                ? investigator.CustomName
                : investigator.Type?.DisplayName ?? "Unknown";

            var duration = latestExecution.CompletedAt.HasValue && latestExecution.StartedAt != default
                ? (latestExecution.CompletedAt.Value - latestExecution.StartedAt).ToString(@"mm\:ss")
                : "00:00";

            return new CompletedInvestigationDto(
                latestExecution.Id,
                latestExecution.InvestigatorId,
                displayName,
                latestExecution.StartedAt,
                latestExecution.CompletedAt ?? DateTime.UtcNow,
                duration,
                latestExecution.ResultCount,
                anomalyCount,
                true // IsHighlighted = true for latest investigation
            );
        }

        private byte[] ExportToJson(InvestigationDetailDto details)
        {
            var json = JsonSerializer.Serialize(details, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            return Encoding.UTF8.GetBytes(json);
        }

        private byte[] ExportToCsv(InvestigationDetailDto details)
        {
            var csv = new StringBuilder();
            
            // Header
            csv.AppendLine("Investigation Summary");
            csv.AppendLine($"Investigator,{details.Summary.InvestigatorName}");
            csv.AppendLine($"Started,{details.Summary.StartedAt:yyyy-MM-dd HH:mm:ss}");
            csv.AppendLine($"Completed,{details.Summary.CompletedAt:yyyy-MM-dd HH:mm:ss}");
            csv.AppendLine($"Duration,{details.Summary.Duration}");
            csv.AppendLine($"Total Results,{details.Summary.ResultCount}");
            csv.AppendLine($"Anomalies,{details.Summary.AnomalyCount}");
            csv.AppendLine();
            
            // Results header
            csv.AppendLine("Detailed Results");
            csv.AppendLine("Timestamp,Message,Payload");
            
            // Results data
            foreach (var result in details.DetailedResults)
            {
                var message = result.Message?.Replace("\"", "\"\"") ?? "";
                var payload = result.Payload?.Replace("\"", "\"\"") ?? "";
                csv.AppendLine($"{result.Timestamp:yyyy-MM-dd HH:mm:ss},\"{message}\",\"{payload}\"");
            }

            return Encoding.UTF8.GetBytes(csv.ToString());
        }
    }
}
