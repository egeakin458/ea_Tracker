using ea_Tracker.Models;
using ea_Tracker.Models.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ea_Tracker.Services
{
    /// <summary>
    /// Interface for managing investigation lifecycle with database persistence.
    /// Defines the contract for investigation coordination and orchestration.
    /// Implements Dependency Inversion Principle by providing abstraction.
    /// </summary>
    public interface IInvestigationManager
    {
        /// <summary>
        /// Starts a single investigator by ID.
        /// </summary>
        /// <param name="id">The investigator instance ID.</param>
        /// <returns>True if the investigator was started successfully, false otherwise.</returns>
        Task<bool> StartInvestigatorAsync(Guid id);

        /// <summary>
        /// Stops a single investigator by ID.
        /// </summary>
        /// <param name="id">The investigator instance ID.</param>
        /// <returns>True if the investigator was stopped successfully, false otherwise.</returns>
        Task<bool> StopInvestigatorAsync(Guid id);

        /// <summary>
        /// Gets the state of all investigators with their current status.
        /// </summary>
        /// <returns>Collection of investigator state DTOs.</returns>
        Task<IEnumerable<InvestigatorStateDto>> GetAllInvestigatorStatesAsync();

        /// <summary>
        /// Gets result logs for an investigator from recent executions.
        /// </summary>
        /// <param name="id">The investigator instance ID.</param>
        /// <param name="take">Maximum number of results to retrieve (default: 100).</param>
        /// <returns>Collection of investigator result DTOs.</returns>
        Task<IEnumerable<InvestigatorResultDto>> GetResultsAsync(Guid id, int take = 100);

        /// <summary>
        /// Creates a new investigator instance of the specified type.
        /// </summary>
        /// <param name="typeCode">The investigator type code (e.g., "invoice", "waybill").</param>
        /// <param name="customName">Optional custom name for the investigator.</param>
        /// <returns>The ID of the created investigator instance.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the investigator type is unknown.</exception>
        Task<Guid> CreateInvestigatorAsync(string typeCode, string? customName = null);

        /// <summary>
        /// Gets summary statistics for all investigators.
        /// </summary>
        /// <returns>Investigator summary with counts and statistics.</returns>
        Task<InvestigatorSummaryDto> GetSummaryAsync();
    }
}