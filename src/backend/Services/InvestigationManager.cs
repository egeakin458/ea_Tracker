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
using Microsoft.Extensions.Logging;

namespace ea_Tracker.Services
{
    /// <summary>
    /// Generic investigation service implementation for investigable entities.
    /// Provides common investigation operations with type safety.
    /// </summary>
    /// <typeparam name="T">The type of investigable entity.</typeparam>
    public class GenericInvestigationService<T> : IGenericInvestigationService<T> where T : class, IInvestigableEntity
    {
        private readonly IGenericRepository<T> _repository;
        private readonly ILogger<GenericInvestigationService<T>> _logger;

        public GenericInvestigationService(IGenericRepository<T> repository, ILogger<GenericInvestigationService<T>> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<IEnumerable<T>> GetEntitiesForInvestigationAsync(int investigationCooldownHours = 1)
        {
            var cooldownThreshold = DateTime.UtcNow.AddHours(-investigationCooldownHours);
            
            var entities = await _repository.GetAsync(
                filter: e => !e.LastInvestigatedAt.HasValue || e.LastInvestigatedAt.Value < cooldownThreshold,
                orderBy: q => q.OrderBy(e => e.LastInvestigatedAt ?? DateTime.MinValue)
            );

            _logger.LogDebug("Found {Count} {EntityType} entities eligible for investigation", 
                entities.Count(), typeof(T).Name);

            return entities;
        }

        public async Task<IEnumerable<T>> GetPriorityInvestigationEntitiesAsync(int maxDaysWithoutInvestigation = 7)
        {
            var entities = await _repository.GetAllAsync();
            
            var priorityEntities = entities
                .Where(e => e.RequiresPriorityInvestigation(maxDaysWithoutInvestigation))
                .OrderBy(e => e.LastInvestigatedAt ?? DateTime.MinValue);

            _logger.LogDebug("Found {Count} {EntityType} entities requiring priority investigation", 
                priorityEntities.Count(), typeof(T).Name);

            return priorityEntities;
        }

        public async Task<IEnumerable<T>> GetAnomalousEntitiesAsync()
        {
            var anomalousEntities = await _repository.GetAsync(
                filter: e => e.HasAnomalies,
                orderBy: q => q.OrderByDescending(e => e.LastInvestigatedAt)
            );

            _logger.LogDebug("Found {Count} {EntityType} entities with anomalies", 
                anomalousEntities.Count(), typeof(T).Name);

            return anomalousEntities;
        }

        public async Task UpdateInvestigationStatusAsync(int entityId, bool hasAnomalies, DateTime investigatedAt)
        {
            var entity = await _repository.GetByIdAsync(entityId);
            if (entity != null)
            {
                entity.MarkAsInvestigated(hasAnomalies, investigatedAt);
                _repository.Update(entity);
                await _repository.SaveChangesAsync();

                _logger.LogDebug("Updated investigation status for {EntityType} {EntityId}: {HasAnomalies}", 
                    typeof(T).Name, entityId, hasAnomalies);
            }
        }

        public async Task BatchUpdateInvestigationStatusAsync(IEnumerable<(int EntityId, bool HasAnomalies, DateTime InvestigatedAt)> updates)
        {
            var updateList = updates.ToList();
            if (!updateList.Any())
                return;

            var entityIds = updateList.Select(u => u.EntityId).ToList();
            var entities = await _repository.GetAsync(e => entityIds.Contains(e.Id));

            var entitiesToUpdate = new List<T>();
            foreach (var entity in entities)
            {
                var update = updateList.FirstOrDefault(u => u.EntityId == entity.Id);
                if (update.EntityId != 0)
                {
                    entity.MarkAsInvestigated(update.HasAnomalies, update.InvestigatedAt);
                    entitiesToUpdate.Add(entity);
                }
            }

            if (entitiesToUpdate.Any())
            {
                foreach (var entity in entitiesToUpdate)
                {
                    _repository.Update(entity);
                }
                await _repository.SaveChangesAsync();

                _logger.LogInformation("Batch updated investigation status for {Count} {EntityType} entities", 
                    entitiesToUpdate.Count, typeof(T).Name);
            }
        }

        public async Task<EntityInvestigationStatsDto> GetInvestigationStatisticsAsync()
        {
            var allEntities = await _repository.GetAllAsync();
            var entitiesList = allEntities.ToList();

            var totalEntities = entitiesList.Count;
            var entitiesWithAnomalies = entitiesList.Count(e => e.HasAnomalies);
            var entitiesNeverInvestigated = entitiesList.Count(e => !e.LastInvestigatedAt.HasValue);
            var entitiesRequiringPriority = entitiesList.Count(e => e.RequiresPriorityInvestigation());
            var lastInvestigationAt = entitiesList
                .Where(e => e.LastInvestigatedAt.HasValue)
                .Max(e => e.LastInvestigatedAt);
            var anomalyRate = totalEntities > 0 ? (double)entitiesWithAnomalies / totalEntities : 0.0;

            return new EntityInvestigationStatsDto(
                totalEntities,
                entitiesWithAnomalies,
                entitiesNeverInvestigated,
                entitiesRequiringPriority,
                lastInvestigationAt,
                anomalyRate
            );
        }
    }

    /// <summary>
    /// Factory implementation for creating generic investigation services.
    /// Uses dependency injection to provide properly configured service instances.
    /// </summary>
    public class GenericInvestigationServiceFactory : IGenericInvestigationServiceFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<Type, Type> _serviceTypeMapping;

        public GenericInvestigationServiceFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _serviceTypeMapping = new Dictionary<Type, Type>
            {
                { typeof(Invoice), typeof(IGenericInvestigationService<Invoice>) },
                { typeof(Waybill), typeof(IGenericInvestigationService<Waybill>) }
            };
        }

        public IGenericInvestigationService<T> GetService<T>() where T : class, IInvestigableEntity
        {
            var service = _serviceProvider.GetService<IGenericInvestigationService<T>>();
            if (service == null)
            {
                throw new InvalidOperationException(
                    $"No generic investigation service registered for type {typeof(T).Name}. " +
                    "Ensure the service is registered in dependency injection configuration.");
            }
            return service;
        }

        public bool IsServiceAvailable<T>() where T : class, IInvestigableEntity
        {
            return _serviceProvider.GetService<IGenericInvestigationService<T>>() != null;
        }

        public IEnumerable<Type> GetSupportedEntityTypes()
        {
            return _serviceTypeMapping.Keys.ToList();
        }
    }
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

                // Verify and correct count if needed (CRITICAL FIX for count mismatch issue)
                var corrected = await CorrectResultCountAsync(executionId);
                if (corrected)
                {
                    // Reload execution to get corrected count for accurate notifications
                    execution = await _executionRepository.GetByIdAsync(executionId);
                    if (execution == null)
                    {
                        throw new InvalidOperationException($"Execution {executionId} disappeared after count correction");
                    }
                }

                // Update last executed timestamp
                await _investigatorRepository.UpdateLastExecutedAsync(id, DateTime.UtcNow);

                // Send completion notification with accurate count
                await _notifier.StatusChangedAsync(id, "Completed");
                await _notifier.InvestigationCompletedAsync(id, execution.ResultCount, execution.CompletedAt ?? DateTime.UtcNow);

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
                    // Note: Simplified logging to avoid service dependency complexity
                    System.Diagnostics.Debug.WriteLine($"Failed to increment result count for execution {executionId} - execution not found or already completed");
                }
            }
            catch (Exception ex) when (ex.Message.Contains("deadlock") && retryCount < maxRetries) // Deadlock
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
                System.Diagnostics.Debug.WriteLine($"Failed to increment result count atomically for execution {executionId} after {retryCount} retries: {ex.Message}");
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

        /// <summary>
        /// Verifies the accuracy of result counts for a specific investigation execution.
        /// Compares the reported count in the execution record against the actual count of stored results.
        /// This method helps identify and diagnose count discrepancies like the one found in execution #248.
        /// </summary>
        /// <param name="executionId">The execution ID to verify.</param>
        /// <returns>Count verification result with accuracy information and discrepancy details.</returns>
        public async Task<CountVerificationResult> VerifyResultCountAsync(int executionId)
        {
            var execution = await _executionRepository.GetByIdAsync(executionId);
            var actualCount = await _resultRepository.CountAsync(r => r.ExecutionId == executionId);
            
            var result = new CountVerificationResult
            {
                ExecutionId = executionId,
                ReportedCount = execution?.ResultCount ?? 0,
                ActualCount = actualCount,
                IsAccurate = execution?.ResultCount == actualCount,
                Discrepancy = actualCount - (execution?.ResultCount ?? 0)
            };
            
            // Log verification results for monitoring and debugging
            if (result.IsAccurate)
            {
                // Use Debug level for accurate counts to avoid log noise
                // Can be promoted to Information during initial monitoring period
            }
            else
            {
                // Always log count discrepancies at Warning level for visibility
                var logger = _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<ILogger<InvestigationManager>>();
                logger.LogWarning("Count verification for execution {ExecutionId}: Reported={Reported}, Actual={Actual}, Discrepancy={Discrepancy}", 
                    executionId, result.ReportedCount, result.ActualCount, result.Discrepancy);
            }
            
            return result;
        }

        /// <summary>
        /// Corrects the result count for a specific investigation execution if inaccurate.
        /// Updates the execution record with the actual count from the database.
        /// This method can be used to fix historical count discrepancies and provides
        /// a recovery mechanism for any remaining race condition issues.
        /// </summary>
        /// <param name="executionId">The execution ID to correct.</param>
        /// <returns>True if the count was corrected, false if already accurate or execution not found.</returns>
        public async Task<bool> CorrectResultCountAsync(int executionId)
        {
            var verification = await VerifyResultCountAsync(executionId);
            if (verification.IsAccurate)
            {
                // Count is already accurate, no correction needed
                return false;
            }

            var execution = await _executionRepository.GetByIdAsync(executionId);
            if (execution != null)
            {
                var logger = _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<ILogger<InvestigationManager>>();
                logger.LogWarning("Correcting result count for execution {ExecutionId}: {Reported} → {Actual} (discrepancy: {Discrepancy})", 
                    executionId, verification.ReportedCount, verification.ActualCount, verification.Discrepancy);
                    
                // Update the count to match reality
                execution.ResultCount = verification.ActualCount;
                _executionRepository.Update(execution);
                await _executionRepository.SaveChangesAsync();
                
                logger.LogInformation("Successfully corrected result count for execution {ExecutionId}", executionId);
                return true;
            }
            
            // Execution not found - this shouldn't happen but handle gracefully
            var scopedLogger = _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<ILogger<InvestigationManager>>();
            scopedLogger.LogError("Cannot correct result count for execution {ExecutionId} - execution not found", executionId);
            return false;
        }

        /// <summary>
        /// Corrects result counts for all investigations that have discrepancies.
        /// This is a utility method that can be used to fix historical data issues
        /// or as part of a maintenance/cleanup operation.
        /// </summary>
        /// <returns>The number of investigations that had their counts corrected.</returns>
        public async Task<int> CorrectAllResultCountsAsync()
        {
            var logger = _scopeFactory.CreateScope().ServiceProvider.GetRequiredService<ILogger<InvestigationManager>>();
            logger.LogInformation("Starting bulk result count correction process");

            // Get all completed executions to check
            var completedExecutions = await _executionRepository.GetAsync(
                filter: e => e.Status == ExecutionStatus.Completed,
                orderBy: q => q.OrderByDescending(e => e.CompletedAt)
            );

            int correctedCount = 0;
            int totalChecked = 0;

            foreach (var execution in completedExecutions)
            {
                totalChecked++;
                var wasCorrected = await CorrectResultCountAsync(execution.Id);
                if (wasCorrected)
                {
                    correctedCount++;
                }

                // Add a small delay to avoid overwhelming the database
                if (totalChecked % 10 == 0)
                {
                    await Task.Delay(100);
                }
            }

            logger.LogInformation("Bulk result count correction completed: {CorrectedCount}/{TotalChecked} investigations corrected", 
                correctedCount, totalChecked);

            return correctedCount;
        }
    }
}
