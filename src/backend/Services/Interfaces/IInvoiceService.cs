using ea_Tracker.Models;
using ea_Tracker.Models.Dtos;
using ea_Tracker.Models.Common;

namespace ea_Tracker.Services.Interfaces
{
    /// <summary>
    /// Service interface for invoice business operations.
    /// Encapsulates all invoice-related business logic and data access patterns.
    /// Designed to replace direct repository usage in controllers.
    /// </summary>
    public interface IInvoiceService
    {
        // =====================================
        // Standard CRUD Operations (Returns DTOs)
        // =====================================

        /// <summary>
        /// Gets an invoice by ID.
        /// </summary>
        /// <param name="id">The invoice ID.</param>
        /// <returns>Invoice response DTO or null if not found.</returns>
        Task<InvoiceResponseDto?> GetByIdAsync(int id);

        /// <summary>
        /// Gets all invoices with optional filtering.
        /// </summary>
        /// <param name="filter">Optional filter criteria.</param>
        /// <returns>Collection of invoice response DTOs.</returns>
        Task<IEnumerable<InvoiceResponseDto>> GetAllAsync(InvoiceFilterDto? filter = null);

        /// <summary>
        /// Creates a new invoice with business validation.
        /// </summary>
        /// <param name="createDto">Invoice creation data.</param>
        /// <returns>Created invoice response DTO.</returns>
        /// <exception cref="ValidationException">Thrown when validation fails.</exception>
        Task<InvoiceResponseDto> CreateAsync(CreateInvoiceDto createDto);

        /// <summary>
        /// Updates an existing invoice with business validation.
        /// </summary>
        /// <param name="id">The invoice ID to update.</param>
        /// <param name="updateDto">Updated invoice data.</param>
        /// <returns>Updated invoice response DTO.</returns>
        /// <exception cref="ValidationException">Thrown when validation fails.</exception>
        Task<InvoiceResponseDto> UpdateAsync(int id, UpdateInvoiceDto updateDto);

        /// <summary>
        /// Deletes an invoice if business rules allow it.
        /// </summary>
        /// <param name="id">The invoice ID to delete.</param>
        /// <returns>True if deleted successfully, false if not found or cannot be deleted.</returns>
        Task<bool> DeleteAsync(int id);

        // =====================================
        // Business Query Operations (Returns DTOs)
        // =====================================

        /// <summary>
        /// Gets invoices flagged as anomalous by investigation system.
        /// </summary>
        /// <returns>Collection of anomalous invoice DTOs.</returns>
        Task<IEnumerable<InvoiceResponseDto>> GetAnomalousInvoicesAsync();

        /// <summary>
        /// Gets invoices within a specific date range.
        /// </summary>
        /// <param name="from">Start date (inclusive).</param>
        /// <param name="to">End date (inclusive).</param>
        /// <returns>Collection of invoice DTOs within date range.</returns>
        Task<IEnumerable<InvoiceResponseDto>> GetInvoicesByDateRangeAsync(DateTime from, DateTime to);

        /// <summary>
        /// Gets invoices with tax ratios exceeding the specified threshold.
        /// </summary>
        /// <param name="threshold">Tax ratio threshold (0.0 to 1.0).</param>
        /// <returns>Collection of high tax ratio invoice DTOs.</returns>
        Task<IEnumerable<InvoiceResponseDto>> GetHighTaxRatioInvoicesAsync(decimal threshold);

        /// <summary>
        /// Gets invoices with negative amounts.
        /// </summary>
        /// <returns>Collection of negative amount invoice DTOs.</returns>
        Task<IEnumerable<InvoiceResponseDto>> GetNegativeAmountInvoicesAsync();

        /// <summary>
        /// Gets invoices with future dates.
        /// </summary>
        /// <returns>Collection of future-dated invoice DTOs.</returns>
        Task<IEnumerable<InvoiceResponseDto>> GetFutureDatedInvoicesAsync();

        // =====================================
        // Business Logic & Validation
        // =====================================

        /// <summary>
        /// Validates invoice data against business rules.
        /// </summary>
        /// <param name="createDto">Invoice data to validate.</param>
        /// <returns>Validation result with errors if any.</returns>
        Task<ValidationResult> ValidateInvoiceAsync(CreateInvoiceDto createDto);

        /// <summary>
        /// Checks if an invoice can be deleted based on business rules.
        /// </summary>
        /// <param name="id">The invoice ID to check.</param>
        /// <returns>True if can be deleted, false otherwise.</returns>
        Task<bool> CanDeleteAsync(int id);

        /// <summary>
        /// Gets statistical summary of all invoices.
        /// </summary>
        /// <returns>Invoice statistics DTO.</returns>
        Task<InvoiceStatisticsDto> GetStatisticsAsync();

        // =====================================
        // Investigation System Compatibility (Returns Entities)
        // =====================================

        /// <summary>
        /// Gets all invoice entities for investigation processing.
        /// Returns actual entities, not DTOs, for investigation system compatibility.
        /// </summary>
        /// <returns>Collection of invoice entities.</returns>
        Task<IEnumerable<Invoice>> GetAllEntitiesAsync();

        /// <summary>
        /// Gets invoice entity by ID for investigation processing.
        /// </summary>
        /// <param name="id">The invoice ID.</param>
        /// <returns>Invoice entity or null if not found.</returns>
        Task<Invoice?> GetEntityByIdAsync(int id);

        /// <summary>
        /// Gets invoice entities that need investigation based on last investigated date.
        /// </summary>
        /// <param name="lastInvestigated">Optional cutoff date for investigation.</param>
        /// <returns>Collection of invoice entities for investigation.</returns>
        Task<IEnumerable<Invoice>> GetInvoicesForInvestigationAsync(DateTime? lastInvestigated = null);

        /// <summary>
        /// Updates anomaly status for an invoice after investigation.
        /// </summary>
        /// <param name="invoiceId">The invoice ID.</param>
        /// <param name="hasAnomalies">Whether anomalies were detected.</param>
        /// <param name="investigatedAt">When the investigation occurred.</param>
        Task UpdateAnomalyStatusAsync(int invoiceId, bool hasAnomalies, DateTime investigatedAt);

        /// <summary>
        /// Updates multiple invoices' anomaly status in batch.
        /// </summary>
        /// <param name="updates">Collection of anomaly updates.</param>
        Task BatchUpdateAnomalyStatusAsync(IEnumerable<(int InvoiceId, bool HasAnomalies, DateTime InvestigatedAt)> updates);
    }
}