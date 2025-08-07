using ea_Tracker.Models;
using ea_Tracker.Models.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ea_Tracker.Services
{
    /// <summary>
    /// Interface for invoice business operations, validation, and CRUD orchestration.
    /// Encapsulates all invoice-related business logic and validation rules.
    /// Implements Dependency Inversion Principle for better testability and maintainability.
    /// </summary>
    public interface IInvoiceService
    {
        /// <summary>
        /// Gets all invoices with optional filtering and business validation.
        /// </summary>
        /// <param name="hasAnomalies">Filter by anomaly status</param>
        /// <param name="fromDate">Filter by issue date from</param>
        /// <param name="toDate">Filter by issue date to</param>
        /// <param name="recipientName">Filter by recipient name (partial match)</param>
        /// <returns>Collection of invoice response DTOs with business logic applied</returns>
        Task<IEnumerable<InvoiceResponseDto>> GetInvoicesAsync(
            bool? hasAnomalies = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? recipientName = null);

        /// <summary>
        /// Gets a specific invoice by ID with business validation.
        /// </summary>
        /// <param name="id">The invoice ID</param>
        /// <returns>The invoice response DTO, or null if not found</returns>
        Task<InvoiceResponseDto?> GetInvoiceByIdAsync(int id);

        /// <summary>
        /// Creates a new invoice with business validation and rules application.
        /// </summary>
        /// <param name="createDto">The invoice creation data</param>
        /// <returns>The created invoice response DTO</returns>
        /// <exception cref="ArgumentException">Thrown when validation fails</exception>
        /// <exception cref="InvalidOperationException">Thrown when business rules are violated</exception>
        Task<InvoiceResponseDto> CreateInvoiceAsync(CreateInvoiceDto createDto);

        /// <summary>
        /// Updates an existing invoice with business validation and audit trail.
        /// </summary>
        /// <param name="id">The invoice ID</param>
        /// <param name="updateDto">The invoice update data</param>
        /// <returns>The updated invoice response DTO, or null if not found</returns>
        /// <exception cref="ArgumentException">Thrown when validation fails</exception>
        /// <exception cref="InvalidOperationException">Thrown when business rules are violated</exception>
        Task<InvoiceResponseDto?> UpdateInvoiceAsync(int id, UpdateInvoiceDto updateDto);

        /// <summary>
        /// Deletes an invoice with business validation and audit considerations.
        /// </summary>
        /// <param name="id">The invoice ID</param>
        /// <returns>True if the invoice was deleted, false if not found</returns>
        /// <exception cref="InvalidOperationException">Thrown when business rules prevent deletion</exception>
        Task<bool> DeleteInvoiceAsync(int id);

        /// <summary>
        /// Gets invoices with detected anomalies, applying business intelligence.
        /// </summary>
        /// <returns>Collection of anomalous invoice response DTOs</returns>
        Task<IEnumerable<InvoiceResponseDto>> GetAnomalousInvoicesAsync();

        /// <summary>
        /// Gets comprehensive invoice statistics with business metrics.
        /// </summary>
        /// <returns>Invoice statistics object with calculated business intelligence</returns>
        Task<object> GetInvoiceStatisticsAsync();

        /// <summary>
        /// Validates an invoice against business rules without persisting.
        /// </summary>
        /// <param name="invoice">The invoice to validate</param>
        /// <returns>Collection of validation errors, empty if valid</returns>
        IEnumerable<string> ValidateInvoice(Invoice invoice);

        /// <summary>
        /// Validates an invoice DTO against business rules and constraints.
        /// </summary>
        /// <param name="createDto">The invoice creation DTO to validate</param>
        /// <returns>Collection of validation errors, empty if valid</returns>
        IEnumerable<string> ValidateInvoiceDto(CreateInvoiceDto createDto);
    }
}