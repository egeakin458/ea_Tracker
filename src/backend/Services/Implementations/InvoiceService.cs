using AutoMapper;
using ea_Tracker.Exceptions;
using ea_Tracker.Models;
using ea_Tracker.Models.Common;
using ea_Tracker.Models.Dtos;
using ea_Tracker.Repositories;
using ea_Tracker.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ea_Tracker.Services.Implementations
{
    /// <summary>
    /// Service implementation for invoice business operations.
    /// Encapsulates business logic extracted from InvoicesController.cs.
    /// Provides both DTO-based operations for controllers and entity-based operations for investigation system.
    /// Enhanced with generic investigation service integration for polymorphic investigation support.
    /// </summary>
    public class InvoiceService : IInvoiceService
    {
        private readonly IGenericRepository<Invoice> _repository;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly ILogger<InvoiceService> _logger;
        private readonly IGenericInvestigationService<Invoice>? _investigationService;

        public InvoiceService(
            IGenericRepository<Invoice> repository,
            IMapper mapper,
            IConfiguration configuration,
            ILogger<InvoiceService> logger,
            IGenericInvestigationService<Invoice>? investigationService = null)
        {
            _repository = repository;
            _mapper = mapper;
            _configuration = configuration;
            _logger = logger;
            _investigationService = investigationService; // Optional for backward compatibility
        }

        // =====================================
        // Standard CRUD Operations (Returns DTOs)
        // =====================================

        public async Task<InvoiceResponseDto?> GetByIdAsync(int id)
        {
            _logger.LogDebug("Retrieving invoice with ID {InvoiceId}", id);

            var invoice = await _repository.GetByIdAsync(id);
            if (invoice == null)
            {
                _logger.LogWarning("Invoice with ID {InvoiceId} not found", id);
                return null;
            }

            return _mapper.Map<InvoiceResponseDto>(invoice);
        }

        public async Task<IEnumerable<InvoiceResponseDto>> GetAllAsync(InvoiceFilterDto? filter = null)
        {
            _logger.LogDebug("Retrieving invoices with filters");

            var invoices = await _repository.GetAsync(
                filter: BuildFilterExpression(filter),
                orderBy: q => q.OrderByDescending(i => i.CreatedAt)
            );

            var result = invoices.Select(i => _mapper.Map<InvoiceResponseDto>(i));
            _logger.LogInformation("Retrieved {InvoiceCount} invoices", result.Count());
            return result;
        }

        public async Task<InvoiceResponseDto> CreateAsync(CreateInvoiceDto createDto)
        {
            _logger.LogDebug("Creating invoice for recipient {RecipientName}", createDto.RecipientName);

            // Validate business rules
            var validationResult = await ValidateInvoiceAsync(createDto);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Invoice validation failed: {ValidationErrors}", string.Join("; ", validationResult.Errors));
                throw new ValidationException(validationResult);
            }

            // Map DTO to entity
            var invoice = _mapper.Map<Invoice>(createDto);

            // Create and save
            var createdInvoice = await _repository.AddAsync(invoice);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Created new invoice {InvoiceId} for recipient {RecipientName}", 
                createdInvoice.Id, createDto.RecipientName);

            return _mapper.Map<InvoiceResponseDto>(createdInvoice);
        }

        public async Task<InvoiceResponseDto> UpdateAsync(int id, UpdateInvoiceDto updateDto)
        {
            _logger.LogDebug("Updating invoice {InvoiceId}", id);

            var invoice = await _repository.GetByIdAsync(id);
            if (invoice == null)
            {
                _logger.LogWarning("Invoice with ID {InvoiceId} not found for update", id);
                throw new ValidationException($"Invoice with ID {id} not found");
            }

            // Store original values for logging
            var originalAmount = invoice.TotalAmount;

            // Map updates to entity
            _mapper.Map(updateDto, invoice);

            // Validate business rules after update
            var validationResult = ValidateInvoiceEntitySync(invoice);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Invoice update validation failed for ID {InvoiceId}: {ValidationErrors}", 
                    id, string.Join("; ", validationResult.Errors));
                throw new ValidationException(validationResult);
            }

            _repository.Update(invoice);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Updated invoice {InvoiceId} (amount changed from {OriginalAmount} to {NewAmount})", 
                id, originalAmount, invoice.TotalAmount);

            return _mapper.Map<InvoiceResponseDto>(invoice);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            _logger.LogDebug("Attempting to delete invoice {InvoiceId}", id);

            var invoice = await _repository.GetByIdAsync(id);
            if (invoice == null)
            {
                _logger.LogWarning("Invoice with ID {InvoiceId} not found for deletion", id);
                return false;
            }

            // Check if deletion is allowed
            if (!await CanDeleteAsync(id))
            {
                _logger.LogWarning("Invoice {InvoiceId} cannot be deleted due to business rules", id);
                return false;
            }

            _repository.Remove(invoice);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Deleted invoice {InvoiceId}", id);
            return true;
        }

        // =====================================
        // Business Query Operations (Returns DTOs)
        // =====================================

        public async Task<IEnumerable<InvoiceResponseDto>> GetAnomalousInvoicesAsync()
        {
            _logger.LogDebug("Retrieving anomalous invoices");

            // Use generic investigation service if available, otherwise fall back to repository
            if (_investigationService != null)
            {
                var anomalousInvoices = await _investigationService.GetAnomalousEntitiesAsync();
                return anomalousInvoices.Select(i => _mapper.Map<InvoiceResponseDto>(i));
            }

            // Fallback to original implementation for backward compatibility
            var invoices = await _repository.GetAsync(
                filter: i => i.HasAnomalies,
                orderBy: q => q.OrderByDescending(i => i.LastInvestigatedAt)
            );

            return invoices.Select(i => _mapper.Map<InvoiceResponseDto>(i));
        }

        public async Task<IEnumerable<InvoiceResponseDto>> GetInvoicesByDateRangeAsync(DateTime from, DateTime to)
        {
            _logger.LogDebug("Retrieving invoices from {FromDate} to {ToDate}", from, to);

            var invoices = await _repository.GetAsync(
                filter: i => i.IssueDate >= from && i.IssueDate <= to,
                orderBy: q => q.OrderBy(i => i.IssueDate)
            );

            return invoices.Select(i => _mapper.Map<InvoiceResponseDto>(i));
        }

        public async Task<IEnumerable<InvoiceResponseDto>> GetHighTaxRatioInvoicesAsync(decimal threshold)
        {
            _logger.LogDebug("Retrieving invoices with tax ratio above {Threshold}", threshold);

            var invoices = await _repository.GetAsync(
                filter: i => i.TotalAmount > 0 && (i.TotalTax / i.TotalAmount) > threshold,
                orderBy: q => q.OrderByDescending(i => i.TotalTax / i.TotalAmount)
            );

            return invoices.Select(i => _mapper.Map<InvoiceResponseDto>(i));
        }

        public async Task<IEnumerable<InvoiceResponseDto>> GetNegativeAmountInvoicesAsync()
        {
            _logger.LogDebug("Retrieving invoices with negative amounts");

            var invoices = await _repository.GetAsync(
                filter: i => i.TotalAmount < 0,
                orderBy: q => q.OrderBy(i => i.TotalAmount)
            );

            return invoices.Select(i => _mapper.Map<InvoiceResponseDto>(i));
        }

        public async Task<IEnumerable<InvoiceResponseDto>> GetFutureDatedInvoicesAsync()
        {
            var today = DateTime.UtcNow.Date;
            _logger.LogDebug("Retrieving invoices with future dates (after {Today})", today);

            var invoices = await _repository.GetAsync(
                filter: i => i.IssueDate > today,
                orderBy: q => q.OrderBy(i => i.IssueDate)
            );

            return invoices.Select(i => _mapper.Map<InvoiceResponseDto>(i));
        }

        // =====================================
        // Business Logic & Validation
        // =====================================

        public Task<ValidationResult> ValidateInvoiceAsync(CreateInvoiceDto createDto)
        {
            var errors = new List<string>();

            // DTO-level validation
            if (string.IsNullOrWhiteSpace(createDto.RecipientName))
                errors.Add("Recipient name is required");

            if (!string.IsNullOrEmpty(createDto.RecipientName) && createDto.RecipientName.Length > 200)
                errors.Add("Recipient name cannot exceed 200 characters");

            // Create temporary entity for business rule validation
            var tempInvoice = _mapper.Map<Invoice>(createDto);
            var entityValidation = ValidateInvoiceEntitySync(tempInvoice);
            errors.AddRange(entityValidation.Errors);

            return Task.FromResult(new ValidationResult(errors));
        }

        public async Task<bool> CanDeleteAsync(int id)
        {
            var invoice = await _repository.GetByIdAsync(id);
            if (invoice == null) return false;

            // Business rule: Cannot delete invoices that have been investigated and have anomalies
            if (invoice.HasAnomalies && invoice.LastInvestigatedAt.HasValue)
                return false;

            // Business rule: Cannot delete invoices older than 30 days (audit requirements)
            if (invoice.CreatedAt < DateTime.UtcNow.AddDays(-30))
                return false;

            return true;
        }

        public async Task<InvoiceStatisticsDto> GetStatisticsAsync()
        {
            _logger.LogDebug("Calculating invoice statistics");

            var allInvoices = await _repository.GetAllAsync();
            var invoicesList = allInvoices.ToList();

            var stats = new InvoiceStatisticsDto
            {
                TotalCount = invoicesList.Count,
                AnomalousCount = invoicesList.Count(i => i.HasAnomalies),
                TotalAmount = invoicesList.Sum(i => i.TotalAmount),
                NegativeAmountCount = invoicesList.Count(i => i.TotalAmount < 0),
                FutureDatedCount = invoicesList.Count(i => i.IssueDate > DateTime.UtcNow.Date)
            };

            // Calculate average tax ratio (only for invoices with positive amounts)
            var positiveAmountInvoices = invoicesList.Where(i => i.TotalAmount > 0).ToList();
            if (positiveAmountInvoices.Any())
            {
                stats.AverageTaxRatio = positiveAmountInvoices.Average(i => i.TotalTax / i.TotalAmount);
                
                // Get configured max tax ratio threshold
                var maxTaxRatio = _configuration.GetValue<decimal>("Investigation:Invoice:MaxTaxRatio", 0.5m);
                stats.HighTaxRatioCount = positiveAmountInvoices.Count(i => (i.TotalTax / i.TotalAmount) > maxTaxRatio);
            }

            _logger.LogInformation("Calculated statistics: {TotalCount} total invoices, {AnomalousCount} anomalous, {AnomalyRate:F2}% anomaly rate",
                stats.TotalCount, stats.AnomalousCount, stats.AnomalyRate);

            return stats;
        }

        // =====================================
        // Investigation System Compatibility (Returns Entities)
        // =====================================

        public async Task<IEnumerable<Invoice>> GetAllEntitiesAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<Invoice?> GetEntityByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<Invoice>> GetInvoicesForInvestigationAsync(DateTime? lastInvestigated = null)
        {
            // Use generic investigation service if available
            if (_investigationService != null)
            {
                var cooldownHours = lastInvestigated.HasValue 
                    ? (int)(DateTime.UtcNow - lastInvestigated.Value).TotalHours 
                    : 0;
                
                return await _investigationService.GetEntitiesForInvestigationAsync(Math.Max(1, cooldownHours));
            }

            // Fallback to original implementation for backward compatibility
            if (lastInvestigated.HasValue)
            {
                return await _repository.GetAsync(
                    filter: i => !i.LastInvestigatedAt.HasValue || i.LastInvestigatedAt < lastInvestigated,
                    orderBy: q => q.OrderBy(i => i.LastInvestigatedAt ?? i.CreatedAt)
                );
            }

            return await _repository.GetAllAsync();
        }

        public async Task UpdateAnomalyStatusAsync(int invoiceId, bool hasAnomalies, DateTime investigatedAt)
        {
            // Use generic investigation service if available
            if (_investigationService != null)
            {
                await _investigationService.UpdateInvestigationStatusAsync(invoiceId, hasAnomalies, investigatedAt);
                _logger.LogDebug("Updated anomaly status via generic service for invoice {InvoiceId}: HasAnomalies={HasAnomalies}", 
                    invoiceId, hasAnomalies);
                return;
            }

            // Fallback to original implementation for backward compatibility
            var invoice = await _repository.GetByIdAsync(invoiceId);
            if (invoice == null)
            {
                _logger.LogWarning("Cannot update anomaly status - invoice {InvoiceId} not found", invoiceId);
                return;
            }

            invoice.MarkAsInvestigated(hasAnomalies, investigatedAt);
            
            _repository.Update(invoice);
            await _repository.SaveChangesAsync();

            _logger.LogDebug("Updated anomaly status for invoice {InvoiceId}: HasAnomalies={HasAnomalies}", 
                invoiceId, hasAnomalies);
        }

        public async Task BatchUpdateAnomalyStatusAsync(IEnumerable<(int InvoiceId, bool HasAnomalies, DateTime InvestigatedAt)> updates)
        {
            var updatesList = updates.ToList();
            _logger.LogDebug("Batch updating anomaly status for {Count} invoices", updatesList.Count);

            // Use generic investigation service batch update if available
            if (_investigationService != null)
            {
                var mappedUpdates = updatesList.Select(u => (u.InvoiceId, u.HasAnomalies, u.InvestigatedAt));
                await _investigationService.BatchUpdateInvestigationStatusAsync(mappedUpdates);
                _logger.LogDebug("Completed batch update via generic service for {Count} invoices", updatesList.Count);
                return;
            }

            // Fallback to sequential updates for backward compatibility
            foreach (var (invoiceId, hasAnomalies, investigatedAt) in updatesList)
            {
                await UpdateAnomalyStatusAsync(invoiceId, hasAnomalies, investigatedAt);
            }
        }

        // =====================================
        // Private Helper Methods
        // =====================================

        private System.Linq.Expressions.Expression<Func<Invoice, bool>>? BuildFilterExpression(InvoiceFilterDto? filter)
        {
            if (filter == null) return null;

            return i => (filter.HasAnomalies == null || i.HasAnomalies == filter.HasAnomalies) &&
                       (filter.FromIssueDate == null || i.IssueDate >= filter.FromIssueDate) &&
                       (filter.ToIssueDate == null || i.IssueDate <= filter.ToIssueDate) &&
                       (filter.RecipientName == null || i.RecipientName!.Contains(filter.RecipientName)) &&
                       (filter.MinAmount == null || i.TotalAmount >= filter.MinAmount) &&
                       (filter.MaxAmount == null || i.TotalAmount <= filter.MaxAmount);
        }

        private ValidationResult ValidateInvoiceEntitySync(Invoice invoice)
        {
            var errors = new List<string>();

            // Business rule: No negative amounts
            if (invoice.TotalAmount < 0)
                errors.Add("Invoice amount cannot be negative");

            // Business rule: Tax cannot be negative
            if (invoice.TotalTax < 0)
                errors.Add("Tax amount cannot be negative");

            // Business rule: Tax cannot exceed 100% of amount (unless amount is 0)
            if (invoice.TotalAmount > 0 && invoice.TotalTax > invoice.TotalAmount)
                errors.Add("Tax amount cannot exceed invoice amount");

            // Business rule: Issue date cannot be in future
            if (invoice.IssueDate > DateTime.UtcNow.Date)
                errors.Add("Invoice issue date cannot be in the future");

            // Business rule: Issue date cannot be too old (more than 10 years)
            if (invoice.IssueDate < DateTime.UtcNow.AddYears(-10).Date)
                errors.Add("Invoice issue date cannot be more than 10 years old");

            // Use configuration for business rules (like investigation system)
            var maxTaxRatio = _configuration.GetValue<decimal>("Investigation:Invoice:MaxTaxRatio", 0.5m);
            if (invoice.TotalAmount > 0 && (invoice.TotalTax / invoice.TotalAmount) > maxTaxRatio)
                errors.Add($"Tax ratio exceeds maximum allowed ({maxTaxRatio:P0})");

            return new ValidationResult(errors);
        }
    }
}