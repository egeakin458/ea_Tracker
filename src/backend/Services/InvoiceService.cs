using ea_Tracker.Models;
using ea_Tracker.Models.Dtos;
using ea_Tracker.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ea_Tracker.Services
{
    /// <summary>
    /// Service for invoice business operations, validation, and CRUD orchestration.
    /// Implements all invoice-related business logic and validation rules.
    /// Extracted from InvoicesController to achieve Dependency Inversion Principle compliance.
    /// </summary>
    public class InvoiceService : IInvoiceService
    {
        private readonly IGenericRepository<Invoice> _invoiceRepository;
        private readonly ILogger<InvoiceService> _logger;

        /// <summary>
        /// Initializes a new instance of the InvoiceService.
        /// </summary>
        /// <param name="invoiceRepository">The invoice repository</param>
        /// <param name="logger">The logger instance</param>
        public InvoiceService(
            IGenericRepository<Invoice> invoiceRepository,
            ILogger<InvoiceService> logger)
        {
            _invoiceRepository = invoiceRepository;
            _logger = logger;
        }

        /// <summary>
        /// Gets all invoices with optional filtering and business validation.
        /// </summary>
        public async Task<IEnumerable<InvoiceResponseDto>> GetInvoicesAsync(
            bool? hasAnomalies = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? recipientName = null)
        {
            try
            {
                _logger.LogDebug("Retrieving invoices with filters: hasAnomalies={HasAnomalies}, fromDate={FromDate}, toDate={ToDate}, recipientName={RecipientName}",
                    hasAnomalies, fromDate, toDate, recipientName);

                var invoices = await _invoiceRepository.GetAsync(
                    filter: i => (hasAnomalies == null || i.HasAnomalies == hasAnomalies) &&
                                (fromDate == null || i.IssueDate >= fromDate) &&
                                (toDate == null || i.IssueDate <= toDate) &&
                                (recipientName == null || i.RecipientName!.Contains(recipientName)),
                    orderBy: q => q.OrderByDescending(i => i.CreatedAt)
                );

                var response = invoices.Select(MapToResponseDto);
                _logger.LogInformation("Retrieved {InvoiceCount} invoices", response.Count());
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoices with filters");
                throw;
            }
        }

        /// <summary>
        /// Gets a specific invoice by ID with business validation.
        /// </summary>
        public async Task<InvoiceResponseDto?> GetInvoiceByIdAsync(int id)
        {
            try
            {
                _logger.LogDebug("Retrieving invoice with ID {InvoiceId}", id);
                
                var invoice = await _invoiceRepository.GetByIdAsync(id);
                if (invoice == null)
                {
                    _logger.LogWarning("Invoice with ID {InvoiceId} not found", id);
                    return null;
                }

                return MapToResponseDto(invoice);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoice {InvoiceId}", id);
                throw;
            }
        }

        /// <summary>
        /// Creates a new invoice with business validation and rules application.
        /// </summary>
        public async Task<InvoiceResponseDto> CreateInvoiceAsync(CreateInvoiceDto createDto)
        {
            try
            {
                // Validate the input DTO
                var validationErrors = ValidateInvoiceDto(createDto);
                if (validationErrors.Any())
                {
                    var errorMessage = string.Join("; ", validationErrors);
                    _logger.LogWarning("Invoice validation failed: {ValidationErrors}", errorMessage);
                    throw new ArgumentException($"Validation failed: {errorMessage}");
                }

                var invoice = new Invoice
                {
                    RecipientName = createDto.RecipientName,
                    TotalAmount = createDto.TotalAmount,
                    IssueDate = createDto.IssueDate,
                    TotalTax = createDto.TotalTax,
                    InvoiceType = createDto.InvoiceType,
                    HasAnomalies = false
                };

                // Apply business rules validation
                var businessValidationErrors = ValidateInvoice(invoice);
                if (businessValidationErrors.Any())
                {
                    var errorMessage = string.Join("; ", businessValidationErrors);
                    _logger.LogWarning("Invoice business rules validation failed: {ValidationErrors}", errorMessage);
                    throw new InvalidOperationException($"Business rules validation failed: {errorMessage}");
                }

                var createdInvoice = await _invoiceRepository.AddAsync(invoice);
                await _invoiceRepository.SaveChangesAsync();
                
                _logger.LogInformation("Created new invoice {InvoiceId} for recipient {RecipientName}", 
                    createdInvoice.Id, createDto.RecipientName);

                return MapToResponseDto(createdInvoice);
            }
            catch (ArgumentException)
            {
                throw; // Re-throw validation exceptions as-is
            }
            catch (InvalidOperationException)
            {
                throw; // Re-throw business rule exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating invoice for recipient {RecipientName}", createDto.RecipientName);
                throw;
            }
        }

        /// <summary>
        /// Updates an existing invoice with business validation and audit trail.
        /// </summary>
        public async Task<InvoiceResponseDto?> UpdateInvoiceAsync(int id, UpdateInvoiceDto updateDto)
        {
            try
            {
                var invoice = await _invoiceRepository.GetByIdAsync(id);
                if (invoice == null)
                {
                    _logger.LogWarning("Invoice with ID {InvoiceId} not found for update", id);
                    return null;
                }

                // Store original values for logging
                var originalAmount = invoice.TotalAmount;

                // Update properties
                invoice.RecipientName = updateDto.RecipientName;
                invoice.TotalAmount = updateDto.TotalAmount;
                invoice.IssueDate = updateDto.IssueDate;
                invoice.TotalTax = updateDto.TotalTax;
                invoice.InvoiceType = updateDto.InvoiceType;

                // Apply business rules validation
                var validationErrors = ValidateInvoice(invoice);
                if (validationErrors.Any())
                {
                    var errorMessage = string.Join("; ", validationErrors);
                    _logger.LogWarning("Invoice update validation failed for ID {InvoiceId}: {ValidationErrors}", id, errorMessage);
                    throw new InvalidOperationException($"Business rules validation failed: {errorMessage}");
                }

                _invoiceRepository.Update(invoice);
                await _invoiceRepository.SaveChangesAsync();
                
                _logger.LogInformation("Updated invoice {InvoiceId} (amount changed from {OriginalAmount} to {NewAmount})", 
                    id, originalAmount, invoice.TotalAmount);

                return MapToResponseDto(invoice);
            }
            catch (InvalidOperationException)
            {
                throw; // Re-throw business rule exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating invoice {InvoiceId}", id);
                throw;
            }
        }

        /// <summary>
        /// Deletes an invoice with business validation and audit considerations.
        /// </summary>
        public async Task<bool> DeleteInvoiceAsync(int id)
        {
            try
            {
                var invoice = await _invoiceRepository.GetByIdAsync(id);
                if (invoice == null)
                {
                    _logger.LogWarning("Invoice with ID {InvoiceId} not found for deletion", id);
                    return false;
                }

                // Check business rules for deletion
                if (!CanDeleteInvoice(invoice))
                {
                    _logger.LogWarning("Invoice {InvoiceId} cannot be deleted due to business rules", id);
                    throw new InvalidOperationException("Invoice cannot be deleted due to business constraints");
                }

                _invoiceRepository.Remove(invoice);
                await _invoiceRepository.SaveChangesAsync();
                
                _logger.LogInformation("Deleted invoice {InvoiceId}", id);
                return true;
            }
            catch (InvalidOperationException)
            {
                throw; // Re-throw business rule exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting invoice {InvoiceId}", id);
                throw;
            }
        }

        /// <summary>
        /// Gets invoices with detected anomalies, applying business intelligence.
        /// </summary>
        public async Task<IEnumerable<InvoiceResponseDto>> GetAnomalousInvoicesAsync()
        {
            try
            {
                _logger.LogDebug("Retrieving anomalous invoices");

                var invoices = await _invoiceRepository.GetAsync(
                    filter: i => i.HasAnomalies,
                    orderBy: q => q.OrderByDescending(i => i.LastInvestigatedAt)
                );

                var response = invoices.Select(MapToResponseDto);
                _logger.LogInformation("Retrieved {AnomalousInvoiceCount} anomalous invoices", response.Count());
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving anomalous invoices");
                throw;
            }
        }

        /// <summary>
        /// Gets comprehensive invoice statistics with business metrics.
        /// </summary>
        public async Task<object> GetInvoiceStatisticsAsync()
        {
            try
            {
                _logger.LogDebug("Calculating invoice statistics");

                var totalCount = await _invoiceRepository.CountAsync();
                var anomalyCount = await _invoiceRepository.CountAsync(i => i.HasAnomalies);
                var allInvoices = await _invoiceRepository.GetAllAsync();
                var totalAmount = allInvoices.Sum(i => i.TotalAmount);
                var totalTax = allInvoices.Sum(i => i.TotalTax);

                var stats = new
                {
                    TotalInvoices = totalCount,
                    AnomalousInvoices = anomalyCount,
                    AnomalyRate = totalCount > 0 ? (double)anomalyCount / totalCount * 100 : 0,
                    TotalAmount = totalAmount,
                    TotalTax = totalTax,
                    AverageTaxRate = totalAmount > 0 ? (double)(totalTax / totalAmount) * 100 : 0
                };

                _logger.LogInformation("Calculated invoice statistics: {TotalInvoices} total, {AnomalousInvoices} anomalous, {TotalAmount:C} total amount", 
                    totalCount, anomalyCount, totalAmount);

                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating invoice statistics");
                throw;
            }
        }

        /// <summary>
        /// Validates an invoice against business rules without persisting.
        /// </summary>
        public IEnumerable<string> ValidateInvoice(Invoice invoice)
        {
            var errors = new List<string>();

            // Business rule: No negative amounts
            if (invoice.TotalAmount < 0)
            {
                errors.Add("Invoice amount cannot be negative");
            }

            // Business rule: Tax cannot be negative
            if (invoice.TotalTax < 0)
            {
                errors.Add("Tax amount cannot be negative");
            }

            // Business rule: Tax cannot exceed 100% of amount (unless amount is 0)
            if (invoice.TotalAmount > 0 && invoice.TotalTax > invoice.TotalAmount)
            {
                errors.Add("Tax amount cannot exceed invoice amount");
            }

            // Business rule: Issue date cannot be in future
            if (invoice.IssueDate > DateTime.UtcNow.Date)
            {
                errors.Add("Invoice issue date cannot be in the future");
            }

            // Business rule: Issue date cannot be too old (more than 10 years)
            if (invoice.IssueDate < DateTime.UtcNow.AddYears(-10).Date)
            {
                errors.Add("Invoice issue date cannot be more than 10 years old");
            }

            return errors;
        }

        /// <summary>
        /// Validates an invoice DTO against business rules and constraints.
        /// </summary>
        public IEnumerable<string> ValidateInvoiceDto(CreateInvoiceDto createDto)
        {
            var errors = new List<string>();

            // Required field validation (beyond data annotations)
            if (string.IsNullOrWhiteSpace(createDto.RecipientName))
            {
                errors.Add("Recipient name is required");
            }

            // Business rule: Recipient name length
            if (!string.IsNullOrEmpty(createDto.RecipientName) && createDto.RecipientName.Length > 200)
            {
                errors.Add("Recipient name cannot exceed 200 characters");
            }

            // Apply entity-level validation
            var tempInvoice = new Invoice
            {
                RecipientName = createDto.RecipientName,
                TotalAmount = createDto.TotalAmount,
                IssueDate = createDto.IssueDate,
                TotalTax = createDto.TotalTax,
                InvoiceType = createDto.InvoiceType
            };

            errors.AddRange(ValidateInvoice(tempInvoice));

            return errors;
        }

        /// <summary>
        /// Checks if an invoice can be safely deleted based on business rules.
        /// </summary>
        private bool CanDeleteInvoice(Invoice invoice)
        {
            // Business rule: Cannot delete invoices that have been investigated and have anomalies
            if (invoice.HasAnomalies && invoice.LastInvestigatedAt.HasValue)
            {
                return false;
            }

            // Business rule: Cannot delete invoices older than 30 days (audit requirements)
            if (invoice.CreatedAt < DateTime.UtcNow.AddDays(-30))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Maps an Invoice entity to a response DTO.
        /// </summary>
        private static InvoiceResponseDto MapToResponseDto(Invoice invoice)
        {
            return new InvoiceResponseDto
            {
                Id = invoice.Id,
                RecipientName = invoice.RecipientName,
                TotalAmount = invoice.TotalAmount,
                IssueDate = invoice.IssueDate,
                TotalTax = invoice.TotalTax,
                InvoiceType = invoice.InvoiceType,
                CreatedAt = invoice.CreatedAt,
                UpdatedAt = invoice.UpdatedAt,
                HasAnomalies = invoice.HasAnomalies,
                LastInvestigatedAt = invoice.LastInvestigatedAt
            };
        }
    }
}