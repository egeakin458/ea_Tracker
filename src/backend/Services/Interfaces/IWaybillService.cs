using ea_Tracker.Models;
using ea_Tracker.Models.Dtos;
using ea_Tracker.Models.Common;

namespace ea_Tracker.Services.Interfaces
{
    /// <summary>
    /// Service interface for waybill business operations.
    /// Encapsulates all waybill-related business logic and data access patterns.
    /// Designed to replace direct repository usage in controllers.
    /// </summary>
    public interface IWaybillService
    {
        // =====================================
        // Standard CRUD Operations (Returns DTOs)
        // =====================================

        /// <summary>
        /// Gets a waybill by ID.
        /// </summary>
        /// <param name="id">The waybill ID.</param>
        /// <returns>Waybill response DTO or null if not found.</returns>
        Task<WaybillResponseDto?> GetByIdAsync(int id);

        /// <summary>
        /// Gets all waybills with optional filtering.
        /// </summary>
        /// <param name="filter">Optional filter criteria.</param>
        /// <returns>Collection of waybill response DTOs.</returns>
        Task<IEnumerable<WaybillResponseDto>> GetAllAsync(WaybillFilterDto? filter = null);

        /// <summary>
        /// Creates a new waybill with business validation.
        /// </summary>
        /// <param name="createDto">Waybill creation data.</param>
        /// <returns>Created waybill response DTO.</returns>
        /// <exception cref="ValidationException">Thrown when validation fails.</exception>
        Task<WaybillResponseDto> CreateAsync(CreateWaybillDto createDto);

        /// <summary>
        /// Updates an existing waybill with business validation.
        /// </summary>
        /// <param name="id">The waybill ID to update.</param>
        /// <param name="updateDto">Updated waybill data.</param>
        /// <returns>Updated waybill response DTO.</returns>
        /// <exception cref="ValidationException">Thrown when validation fails.</exception>
        Task<WaybillResponseDto> UpdateAsync(int id, UpdateWaybillDto updateDto);

        /// <summary>
        /// Deletes a waybill if business rules allow it.
        /// </summary>
        /// <param name="id">The waybill ID to delete.</param>
        /// <returns>True if deleted successfully, false if not found or cannot be deleted.</returns>
        Task<bool> DeleteAsync(int id);

        // =====================================
        // Business Query Operations (Returns DTOs)
        // =====================================

        /// <summary>
        /// Gets waybills flagged as anomalous by investigation system.
        /// </summary>
        /// <returns>Collection of anomalous waybill DTOs.</returns>
        Task<IEnumerable<WaybillResponseDto>> GetAnomalousWaybillsAsync();

        /// <summary>
        /// Gets waybills that are overdue for delivery.
        /// </summary>
        /// <returns>Collection of overdue waybill DTOs.</returns>
        Task<IEnumerable<WaybillResponseDto>> GetOverdueWaybillsAsync();

        /// <summary>
        /// Gets waybills expiring within specified number of days.
        /// </summary>
        /// <param name="days">Number of days to check ahead (default from configuration).</param>
        /// <returns>Collection of soon-to-expire waybill DTOs.</returns>
        Task<IEnumerable<WaybillResponseDto>> GetWaybillsExpiringSoonAsync(int? days = null);

        /// <summary>
        /// Gets waybills considered legacy (older than cutoff date).
        /// </summary>
        /// <param name="cutoffDays">Number of days to consider as legacy (default from configuration).</param>
        /// <returns>Collection of legacy waybill DTOs.</returns>
        Task<IEnumerable<WaybillResponseDto>> GetLegacyWaybillsAsync(int? cutoffDays = null);

        /// <summary>
        /// Gets waybills with late deliveries.
        /// </summary>
        /// <param name="daysLate">Minimum number of days late (default: 7).</param>
        /// <returns>Collection of late delivery waybill DTOs.</returns>
        Task<IEnumerable<WaybillResponseDto>> GetLateDeliveryWaybillsAsync(int daysLate = 7);

        /// <summary>
        /// Gets waybills within a specific date range.
        /// </summary>
        /// <param name="from">Start date (inclusive).</param>
        /// <param name="to">End date (inclusive).</param>
        /// <returns>Collection of waybill DTOs within date range.</returns>
        Task<IEnumerable<WaybillResponseDto>> GetWaybillsByDateRangeAsync(DateTime from, DateTime to);

        // =====================================
        // Business Logic & Validation
        // =====================================

        /// <summary>
        /// Validates waybill data against business rules.
        /// </summary>
        /// <param name="createDto">Waybill data to validate.</param>
        /// <returns>Validation result with errors if any.</returns>
        Task<ValidationResult> ValidateWaybillAsync(CreateWaybillDto createDto);

        /// <summary>
        /// Checks if a waybill can be deleted based on business rules.
        /// </summary>
        /// <param name="id">The waybill ID to check.</param>
        /// <returns>True if can be deleted, false otherwise.</returns>
        Task<bool> CanDeleteAsync(int id);

        /// <summary>
        /// Gets statistical summary of all waybills.
        /// </summary>
        /// <returns>Waybill statistics DTO.</returns>
        Task<WaybillStatisticsDto> GetStatisticsAsync();

        // =====================================
        // Investigation System Compatibility (Returns Entities)
        // =====================================

        /// <summary>
        /// Gets all waybill entities for investigation processing.
        /// Returns actual entities, not DTOs, for investigation system compatibility.
        /// </summary>
        /// <returns>Collection of waybill entities.</returns>
        Task<IEnumerable<Waybill>> GetAllEntitiesAsync();

        /// <summary>
        /// Gets waybill entity by ID for investigation processing.
        /// </summary>
        /// <param name="id">The waybill ID.</param>
        /// <returns>Waybill entity or null if not found.</returns>
        Task<Waybill?> GetEntityByIdAsync(int id);

        /// <summary>
        /// Gets waybill entities that need investigation based on last investigated date.
        /// </summary>
        /// <param name="lastInvestigated">Optional cutoff date for investigation.</param>
        /// <returns>Collection of waybill entities for investigation.</returns>
        Task<IEnumerable<Waybill>> GetWaybillsForInvestigationAsync(DateTime? lastInvestigated = null);

        /// <summary>
        /// Updates anomaly status for a waybill after investigation.
        /// </summary>
        /// <param name="waybillId">The waybill ID.</param>
        /// <param name="hasAnomalies">Whether anomalies were detected.</param>
        /// <param name="investigatedAt">When the investigation occurred.</param>
        Task UpdateAnomalyStatusAsync(int waybillId, bool hasAnomalies, DateTime investigatedAt);

        /// <summary>
        /// Updates multiple waybills' anomaly status in batch.
        /// </summary>
        /// <param name="updates">Collection of anomaly updates.</param>
        Task BatchUpdateAnomalyStatusAsync(IEnumerable<(int WaybillId, bool HasAnomalies, DateTime InvestigatedAt)> updates);
    }
}