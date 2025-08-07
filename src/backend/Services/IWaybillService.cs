using ea_Tracker.Models;
using ea_Tracker.Models.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ea_Tracker.Services
{
    /// <summary>
    /// Interface for waybill business operations, validation, and CRUD orchestration.
    /// Encapsulates all waybill-related business logic and validation rules.
    /// Implements Dependency Inversion Principle for better testability and maintainability.
    /// </summary>
    public interface IWaybillService
    {
        /// <summary>
        /// Gets all waybills with optional filtering and business validation.
        /// </summary>
        /// <param name="hasAnomalies">Filter by anomaly status</param>
        /// <param name="fromDate">Filter by goods issue date from</param>
        /// <param name="toDate">Filter by goods issue date to</param>
        /// <param name="recipientName">Filter by recipient name (partial match)</param>
        /// <returns>Collection of waybill response DTOs with business logic applied</returns>
        Task<IEnumerable<WaybillResponseDto>> GetWaybillsAsync(
            bool? hasAnomalies = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? recipientName = null);

        /// <summary>
        /// Gets a specific waybill by ID with business validation.
        /// </summary>
        /// <param name="id">The waybill ID</param>
        /// <returns>The waybill response DTO, or null if not found</returns>
        Task<WaybillResponseDto?> GetWaybillByIdAsync(int id);

        /// <summary>
        /// Creates a new waybill with business validation and rules application.
        /// </summary>
        /// <param name="createDto">The waybill creation data</param>
        /// <returns>The created waybill response DTO</returns>
        /// <exception cref="ArgumentException">Thrown when validation fails</exception>
        /// <exception cref="InvalidOperationException">Thrown when business rules are violated</exception>
        Task<WaybillResponseDto> CreateWaybillAsync(CreateWaybillDto createDto);

        /// <summary>
        /// Updates an existing waybill with business validation and audit trail.
        /// </summary>
        /// <param name="id">The waybill ID</param>
        /// <param name="updateDto">The waybill update data</param>
        /// <returns>The updated waybill response DTO, or null if not found</returns>
        /// <exception cref="ArgumentException">Thrown when validation fails</exception>
        /// <exception cref="InvalidOperationException">Thrown when business rules are violated</exception>
        Task<WaybillResponseDto?> UpdateWaybillAsync(int id, UpdateWaybillDto updateDto);

        /// <summary>
        /// Deletes a waybill with business validation and audit considerations.
        /// </summary>
        /// <param name="id">The waybill ID</param>
        /// <returns>True if the waybill was deleted, false if not found</returns>
        /// <exception cref="InvalidOperationException">Thrown when business rules prevent deletion</exception>
        Task<bool> DeleteWaybillAsync(int id);

        /// <summary>
        /// Gets waybills with detected anomalies, applying business intelligence.
        /// </summary>
        /// <returns>Collection of anomalous waybill response DTOs</returns>
        Task<IEnumerable<WaybillResponseDto>> GetAnomalousWaybillsAsync();

        /// <summary>
        /// Gets late waybills based on business rules and configurable thresholds.
        /// </summary>
        /// <param name="daysLate">Number of days to consider late (default: 7)</param>
        /// <returns>Collection of late waybill response DTOs</returns>
        Task<IEnumerable<WaybillResponseDto>> GetLateWaybillsAsync(int daysLate = 7);

        /// <summary>
        /// Gets overdue waybills where the due date has passed.
        /// </summary>
        /// <returns>Collection of overdue waybill response DTOs</returns>
        Task<IEnumerable<WaybillResponseDto>> GetOverdueWaybillsAsync();

        /// <summary>
        /// Gets waybills expiring soon based on business-defined threshold.
        /// </summary>
        /// <param name="hoursThreshold">Hours until due to consider as expiring soon (default: 24)</param>
        /// <returns>Collection of expiring waybill response DTOs</returns>
        Task<IEnumerable<WaybillResponseDto>> GetExpiringSoonWaybillsAsync(int hoursThreshold = 24);

        /// <summary>
        /// Gets comprehensive waybill statistics with business metrics.
        /// </summary>
        /// <returns>Waybill statistics object with calculated business intelligence</returns>
        Task<object> GetWaybillStatisticsAsync();

        /// <summary>
        /// Validates a waybill against business rules without persisting.
        /// </summary>
        /// <param name="waybill">The waybill to validate</param>
        /// <returns>Collection of validation errors, empty if valid</returns>
        IEnumerable<string> ValidateWaybill(Waybill waybill);

        /// <summary>
        /// Validates a waybill DTO against business rules and constraints.
        /// </summary>
        /// <param name="createDto">The waybill creation DTO to validate</param>
        /// <returns>Collection of validation errors, empty if valid</returns>
        IEnumerable<string> ValidateWaybillDto(CreateWaybillDto createDto);

        /// <summary>
        /// Validates a waybill update DTO against business rules and constraints.
        /// </summary>
        /// <param name="updateDto">The waybill update DTO to validate</param>
        /// <returns>Collection of validation errors, empty if valid</returns>
        IEnumerable<string> ValidateWaybillUpdateDto(UpdateWaybillDto updateDto);

        /// <summary>
        /// Checks if a waybill can be safely deleted based on business rules.
        /// </summary>
        /// <param name="waybill">The waybill to check</param>
        /// <returns>True if deletion is allowed, false otherwise</returns>
        bool CanDeleteWaybill(Waybill waybill);
    }
}