using System;
using System.Collections.Generic;
using System.Linq;
using ea_Tracker.Models;
using ea_Tracker.Models.Dtos;
using ea_Tracker.Repositories;
using ea_Tracker.Enums;

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
        private readonly Dictionary<Guid, Investigator> _runningInvestigators = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="InvestigationManager"/> class.
        /// </summary>
        public InvestigationManager(
            IInvestigatorFactory factory,
            IInvestigatorRepository investigatorRepository,
            IGenericRepository<InvestigationExecution> executionRepository,
            IGenericRepository<InvestigationResult> resultRepository,
            IGenericRepository<InvestigatorType> investigatorTypeRepository)
        {
            _factory = factory;
            _investigatorRepository = investigatorRepository;
            _executionRepository = executionRepository;
            _resultRepository = resultRepository;
            _investigatorTypeRepository = investigatorTypeRepository;
        }



        /// <summary>
        /// Starts a single investigator.
        /// </summary>
        public async Task<bool> StartInvestigatorAsync(Guid id)
        {
            var investigatorInstance = await _investigatorRepository.GetWithDetailsAsync(id);
            if (investigatorInstance == null || !investigatorInstance.IsActive)
                return false;

            if (_runningInvestigators.ContainsKey(id))
                return false; // Already running

            try
            {
                var investigator = _factory.Create(investigatorInstance.Type.Code);
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

                investigator.Start();
                _runningInvestigators[id] = investigator;

                // Update last executed timestamp
                await _investigatorRepository.UpdateLastExecutedAsync(id, DateTime.UtcNow);

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

        /// <summary>
        /// Stops a single investigator.
        /// </summary>
        public async Task<bool> StopInvestigatorAsync(Guid id)
        {
            if (!_runningInvestigators.TryGetValue(id, out var investigator))
                return false;

            try
            {
                investigator.Stop();
                _runningInvestigators.Remove(id);

                // Update execution record to completed
                var runningExecution = await _executionRepository.GetFirstOrDefaultAsync(e => e.InvestigatorId == id && e.Status == ExecutionStatus.Running);
                if (runningExecution != null)
                {
                    runningExecution.Status = ExecutionStatus.Completed;
                    runningExecution.CompletedAt = DateTime.UtcNow;
                    _executionRepository.Update(runningExecution);
                    await _executionRepository.SaveChangesAsync();
                }

                return true;
            }
            catch (Exception ex)
            {
                // Mark execution as failed
                var runningExecution = await _executionRepository.GetFirstOrDefaultAsync(e => e.InvestigatorId == id && e.Status == ExecutionStatus.Running);
                if (runningExecution != null)
                {
                    runningExecution.Status = ExecutionStatus.Failed;
                    runningExecution.CompletedAt = DateTime.UtcNow;
                    runningExecution.ErrorMessage = ex.Message;
                    _executionRepository.Update(runningExecution);
                    await _executionRepository.SaveChangesAsync();
                }
                return false;
            }
        }

        /// <summary>
        /// Gets the state of all investigators.
        /// </summary>
        public async Task<IEnumerable<ea_Tracker.Models.Dtos.InvestigatorStateDto>> GetAllInvestigatorStatesAsync()
        {
            var investigators = await _investigatorRepository.GetActiveWithTypesAsync();
            return investigators.Select(i => new ea_Tracker.Models.Dtos.InvestigatorStateDto(
                i.Id,
                i.DisplayName,
                _runningInvestigators.ContainsKey(i.Id),
                i.TotalResultCount));
        }

        /// <summary>
        /// Gets result logs for an investigator from recent executions.
        /// </summary>
        public async Task<IEnumerable<ea_Tracker.Models.Dtos.InvestigatorResultDto>> GetResultsAsync(Guid id, int take = 100)
        {
            var executions = await _investigatorRepository.GetExecutionHistoryAsync(id, 5);
            var results = new List<ea_Tracker.Models.Dtos.InvestigatorResultDto>();
            
            foreach (var execution in executions)
            {
                var executionResults = await _resultRepository.GetAsync(
                    filter: r => r.ExecutionId == execution.Id,
                    orderBy: q => q.OrderByDescending(r => r.Timestamp));
                
                results.AddRange(executionResults.Take(take / executions.Count()).Select(r => 
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
        /// Saves a result from an investigator execution to the database.
        /// </summary>
        private async Task SaveResultAsync(int executionId, InvestigatorResult result)
        {
            var investigationResult = new InvestigationResult
            {
                ExecutionId = executionId,
                Timestamp = result.Timestamp,
                Severity = ResultSeverity.Info, // Default to info level
                Message = result.Message ?? "No message",
                Payload = result.Payload
            };

            await _resultRepository.AddAsync(investigationResult);
            await _resultRepository.SaveChangesAsync();

            // Update result count in execution
            var execution = await _executionRepository.GetByIdAsync(executionId);
            if (execution != null)
            {
                execution.ResultCount++;
                _executionRepository.Update(execution);
                await _executionRepository.SaveChangesAsync();
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
                // Stop the investigator if it's running
                if (_runningInvestigators.ContainsKey(id))
                {
                    await StopInvestigatorAsync(id);
                }

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
    }
}
