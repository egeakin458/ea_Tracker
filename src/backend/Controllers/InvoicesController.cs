using Microsoft.AspNetCore.Mvc;
using ea_Tracker.Models;
using ea_Tracker.Models.Dtos;
using ea_Tracker.Repositories;
using System.Linq.Expressions;

namespace ea_Tracker.Controllers
{
    /// <summary>
    /// API controller for managing invoices with full CRUD operations.
    /// Simplified to use repository directly after removing over-engineered service layer.
    /// Business logic consolidated in controller for better maintainability.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class InvoicesController : ControllerBase
    {
        private readonly IGenericRepository<Invoice> _invoiceRepository;
        private readonly ILogger<InvoicesController> _logger;

        public InvoicesController(
            IGenericRepository<Invoice> invoiceRepository,
            ILogger<InvoicesController> logger)
        {
            _invoiceRepository = invoiceRepository;
            _logger = logger;
        }

        /// <summary>
        /// Gets all invoices with optional filtering.
        /// </summary>
        /// <param name="hasAnomalies">Filter by anomaly status</param>
        /// <param name="fromDate">Filter by issue date from</param>
        /// <param name="toDate">Filter by issue date to</param>
        /// <param name="recipientName">Filter by recipient name (partial match)</param>
        /// <returns>List of invoices</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<InvoiceResponseDto>>> GetInvoices(
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
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoices");
                return StatusCode(500, "An error occurred while retrieving invoices");
            }
        }

        /// <summary>
        /// Gets a specific invoice by ID.
        /// </summary>
        /// <param name="id">The invoice ID</param>
        /// <returns>The invoice details</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<InvoiceResponseDto>> GetInvoice(int id)
        {
            try
            {
                _logger.LogDebug("Retrieving invoice with ID {InvoiceId}", id);
                
                var invoice = await _invoiceRepository.GetByIdAsync(id);
                if (invoice == null)
                {
                    _logger.LogWarning("Invoice with ID {InvoiceId} not found", id);
                    return NotFound($"Invoice with ID {id} not found");
                }

                return Ok(MapToResponseDto(invoice));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoice {InvoiceId}", id);
                return StatusCode(500, "An error occurred while retrieving the invoice");
            }
        }

        /// <summary>
        /// Creates a new invoice.
        /// </summary>
        /// <param name="createDto">The invoice creation data</param>
        /// <returns>The created invoice</returns>
        [HttpPost]
        public async Task<ActionResult<InvoiceResponseDto>> CreateInvoice(CreateInvoiceDto createDto)
        {
            try
            {
                // Validate the input DTO
                var validationErrors = ValidateInvoiceDto(createDto);
                if (validationErrors.Any())
                {
                    var errorMessage = string.Join("; ", validationErrors);
                    _logger.LogWarning("Invoice validation failed: {ValidationErrors}", errorMessage);
                    return BadRequest($"Validation failed: {errorMessage}");
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
                    return BadRequest($"Business rules validation failed: {errorMessage}");
                }

                var createdInvoice = await _invoiceRepository.AddAsync(invoice);
                await _invoiceRepository.SaveChangesAsync();
                
                _logger.LogInformation("Created new invoice {InvoiceId} for recipient {RecipientName}", 
                    createdInvoice.Id, createDto.RecipientName);

                return CreatedAtAction(nameof(GetInvoice), new { id = createdInvoice.Id }, MapToResponseDto(createdInvoice));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation failed for invoice creation");
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Business rules violation for invoice creation");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating invoice for recipient {RecipientName}", createDto.RecipientName);
                return StatusCode(500, "An error occurred while creating the invoice");
            }
        }

        /// <summary>
        /// Updates an existing invoice.
        /// </summary>
        /// <param name="id">The invoice ID</param>
        /// <param name="updateDto">The invoice update data</param>
        /// <returns>The updated invoice</returns>
        [HttpPut("{id}")]
        public async Task<ActionResult<InvoiceResponseDto>> UpdateInvoice(int id, UpdateInvoiceDto updateDto)
        {
            try
            {
                var invoice = await _invoiceRepository.GetByIdAsync(id);
                if (invoice == null)
                {
                    _logger.LogWarning("Invoice with ID {InvoiceId} not found for update", id);
                    return NotFound($"Invoice with ID {id} not found");
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
                    return BadRequest($"Business rules validation failed: {errorMessage}");
                }

                _invoiceRepository.Update(invoice);
                await _invoiceRepository.SaveChangesAsync();
                
                _logger.LogInformation("Updated invoice {InvoiceId} (amount changed from {OriginalAmount} to {NewAmount})", 
                    id, originalAmount, invoice.TotalAmount);

                return Ok(MapToResponseDto(invoice));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Business rules violation for invoice update");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating invoice {InvoiceId}", id);
                return StatusCode(500, "An error occurred while updating the invoice");
            }
        }

        /// <summary>
        /// Deletes an invoice.
        /// </summary>
        /// <param name="id">The invoice ID</param>
        /// <returns>No content if successful</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteInvoice(int id)
        {
            try
            {
                var invoice = await _invoiceRepository.GetByIdAsync(id);
                if (invoice == null)
                {
                    _logger.LogWarning("Invoice with ID {InvoiceId} not found for deletion", id);
                    return NotFound($"Invoice with ID {id} not found");
                }

                // Check business rules for deletion
                if (!CanDeleteInvoice(invoice))
                {
                    _logger.LogWarning("Invoice {InvoiceId} cannot be deleted due to business rules", id);
                    return BadRequest("Invoice cannot be deleted due to business constraints");
                }

                _invoiceRepository.Remove(invoice);
                await _invoiceRepository.SaveChangesAsync();
                
                _logger.LogInformation("Deleted invoice {InvoiceId}", id);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Business rules prevent invoice deletion");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting invoice {InvoiceId}", id);
                return StatusCode(500, "An error occurred while deleting the invoice");
            }
        }

        /// <summary>
        /// Gets invoices with anomalies.
        /// </summary>
        /// <returns>List of invoices with detected anomalies</returns>
        [HttpGet("anomalies")]
        public async Task<ActionResult<IEnumerable<InvoiceResponseDto>>> GetAnomalousInvoices()
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
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving anomalous invoices");
                return StatusCode(500, "An error occurred while retrieving anomalous invoices");
            }
        }

        /// <summary>
        /// Gets invoice statistics.
        /// </summary>
        /// <returns>Invoice statistics summary</returns>
        [HttpGet("stats")]
        public async Task<ActionResult<object>> GetInvoiceStats()
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

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoice statistics");
                return StatusCode(500, "An error occurred while retrieving invoice statistics");
            }
        }

        /// <summary>
        /// Validates an invoice against business rules without persisting.
        /// </summary>
        private IEnumerable<string> ValidateInvoice(Invoice invoice)
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
        private IEnumerable<string> ValidateInvoiceDto(CreateInvoiceDto createDto)
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