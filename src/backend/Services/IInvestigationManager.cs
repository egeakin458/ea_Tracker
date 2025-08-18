using ea_Tracker.Models;
using ea_Tracker.Models.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ea_Tracker.Services
{
    /// <summary>
    /// Generic service interface for investigation operations on investigable entities.
    /// Provides polymorphic investigation processing capabilities.
    /// </summary>
    /// <typeparam name="T">The type of investigable entity.</typeparam>
    public interface IGenericInvestigationService<T> where T : class, IInvestigableEntity
    {
        /// <summary>
        /// Gets all entities eligible for investigation based on business rules.
        /// </summary>
        /// <param name="investigationCooldownHours">Minimum hours between investigations.</param>
        /// <returns>Collection of entities ready for investigation.</returns>
        Task<IEnumerable<T>> GetEntitiesForInvestigationAsync(int investigationCooldownHours = 1);

        /// <summary>
        /// Gets entities that require priority investigation.
        /// </summary>
        /// <param name="maxDaysWithoutInvestigation">Maximum days without investigation before priority.</param>
        /// <returns>Collection of entities requiring priority investigation.</returns>
        Task<IEnumerable<T>> GetPriorityInvestigationEntitiesAsync(int maxDaysWithoutInvestigation = 7);

        /// <summary>
        /// Gets all entities with anomalies detected.
        /// </summary>
        /// <returns>Collection of entities flagged as anomalous.</returns>
        Task<IEnumerable<T>> GetAnomalousEntitiesAsync();

        /// <summary>
        /// Updates the investigation status for a single entity.
        /// </summary>
        /// <param name="entityId">The entity ID.</param>
        /// <param name="hasAnomalies">Whether anomalies were detected.</param>
        /// <param name="investigatedAt">When the investigation occurred.</param>
        Task UpdateInvestigationStatusAsync(int entityId, bool hasAnomalies, DateTime investigatedAt);

        /// <summary>
        /// Updates investigation status for multiple entities in batch.
        /// </summary>
        /// <param name="updates">Collection of investigation status updates.</param>
        Task BatchUpdateInvestigationStatusAsync(IEnumerable<(int EntityId, bool HasAnomalies, DateTime InvestigatedAt)> updates);

        /// <summary>
        /// Gets investigation statistics for the entity type.
        /// </summary>
        /// <returns>Investigation statistics summary.</returns>
        Task<EntityInvestigationStatsDto> GetInvestigationStatisticsAsync();
    }

    /// <summary>
    /// DTO for entity investigation statistics.
    /// </summary>
    public record EntityInvestigationStatsDto(
        int TotalEntities,
        int EntitiesWithAnomalies,
        int EntitiesNeverInvestigated,
        int EntitiesRequiringPriority,
        DateTime? LastInvestigationAt,
        double AnomalyRate
    );

    /// <summary>
    /// Factory interface for creating generic investigation services.
    /// Enables polymorphic access to investigation services for different entity types.
    /// </summary>
    public interface IGenericInvestigationServiceFactory
    {
        /// <summary>
        /// Gets the generic investigation service for the specified entity type.
        /// </summary>
        /// <typeparam name="T">The investigable entity type.</typeparam>
        /// <returns>Generic investigation service instance.</returns>
        IGenericInvestigationService<T> GetService<T>() where T : class, IInvestigableEntity;

        /// <summary>
        /// Checks if a generic investigation service is available for the specified type.
        /// </summary>
        /// <typeparam name="T">The investigable entity type to check.</typeparam>
        /// <returns>True if service is available, false otherwise.</returns>
        bool IsServiceAvailable<T>() where T : class, IInvestigableEntity;

        /// <summary>
        /// Gets all supported investigable entity types.
        /// </summary>
        /// <returns>Collection of supported entity types.</returns>
        IEnumerable<Type> GetSupportedEntityTypes();
    }
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

        // Removed StopInvestigatorAsync - investigations are now one-shot operations

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

        /// <summary>
        /// Deletes an investigator instance and all related data.
        /// Stops the investigator if running before deletion.
        /// </summary>
        /// <param name="id">The investigator instance ID to delete.</param>
        /// <returns>True if the investigator was deleted successfully, false otherwise.</returns>
        Task<bool> DeleteInvestigatorAsync(Guid id);

        /// <summary>
        /// Gets all completed investigations ordered by completion date (most recent first).
        /// Used for the InvestigationResults panel display.
        /// </summary>
        /// <returns>Collection of completed investigation DTOs.</returns>
        Task<IEnumerable<CompletedInvestigationDto>> GetAllCompletedInvestigationsAsync();

        /// <summary>
        /// Gets detailed information for a specific completed investigation.
        /// Includes summary information and all detailed results.
        /// </summary>
        /// <param name="executionId">The execution ID of the investigation.</param>
        /// <returns>Investigation detail DTO or null if not found.</returns>
        Task<InvestigationDetailDto?> GetInvestigationDetailsAsync(int executionId);

        /// <summary>
        /// Exports investigation results in the specified format.
        /// </summary>
        /// <param name="executionId">The execution ID of the investigation to export.</param>
        /// <param name="format">Export format: json, csv, or excel.</param>
        /// <returns>Export DTO with data and metadata or null if investigation not found.</returns>
        Task<InvestigationExportDto?> ExportInvestigationResultsAsync(int executionId, string format);

        /// <summary>
        /// Gets the latest completed investigation for a specific investigator.
        /// Used for highlighting functionality when an investigator is clicked.
        /// </summary>
        /// <param name="investigatorId">The investigator ID.</param>
        /// <returns>Latest completed investigation DTO or null if none found.</returns>
        Task<CompletedInvestigationDto?> GetLatestCompletedInvestigationAsync(Guid investigatorId);

        /// <summary>
        /// Verifies the accuracy of result counts for a specific investigation execution.
        /// Compares the reported count in the execution record against the actual count of stored results.
        /// </summary>
        /// <param name="executionId">The execution ID to verify.</param>
        /// <returns>Count verification result with accuracy information.</returns>
        Task<CountVerificationResult> VerifyResultCountAsync(int executionId);

        /// <summary>
        /// Corrects the result count for a specific investigation execution if inaccurate.
        /// Updates the execution record with the actual count from the database.
        /// </summary>
        /// <param name="executionId">The execution ID to correct.</param>
        /// <returns>True if the count was corrected, false if already accurate or execution not found.</returns>
        Task<bool> CorrectResultCountAsync(int executionId);

        /// <summary>
        /// Corrects result counts for all investigations that have discrepancies.
        /// This is a utility method that can be used to fix historical data issues.
        /// </summary>
        /// <returns>The number of investigations that had their counts corrected.</returns>
        Task<int> CorrectAllResultCountsAsync();
    }
}