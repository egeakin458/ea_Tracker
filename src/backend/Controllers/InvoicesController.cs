using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ea_Tracker.Models.Dtos;
using ea_Tracker.Services.Interfaces;
using ea_Tracker.Exceptions;

namespace ea_Tracker.Controllers
{
    /// <summary>
    /// API controller for managing invoices with service layer architecture.
    /// Delegates business logic to InvoiceService for better separation of concerns.
    /// Controllers are now thin and focus only on HTTP request/response handling.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Require authentication for all invoice operations
    public class InvoicesController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;
        private readonly ILogger<InvoicesController> _logger;

        public InvoicesController(
            IInvoiceService invoiceService,
            ILogger<InvoicesController> logger)
        {
            _invoiceService = invoiceService;
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

                var filter = new InvoiceFilterDto
                {
                    HasAnomalies = hasAnomalies,
                    FromIssueDate = fromDate,
                    ToIssueDate = toDate,
                    RecipientName = recipientName
                };

                var invoices = await _invoiceService.GetAllAsync(filter);
                _logger.LogInformation("Retrieved {InvoiceCount} invoices", invoices.Count());
                return Ok(invoices);
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
                
                var invoice = await _invoiceService.GetByIdAsync(id);
                if (invoice == null)
                {
                    _logger.LogWarning("Invoice with ID {InvoiceId} not found", id);
                    return NotFound($"Invoice with ID {id} not found");
                }

                return Ok(invoice);
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
                var createdInvoice = await _invoiceService.CreateAsync(createDto);
                _logger.LogInformation("Created new invoice {InvoiceId} for recipient {RecipientName}", 
                    createdInvoice.Id, createDto.RecipientName);

                return CreatedAtAction(nameof(GetInvoice), new { id = createdInvoice.Id }, createdInvoice);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning("Invoice validation failed: {ValidationErrors}", string.Join("; ", ex.Errors));
                return BadRequest(new { errors = ex.Errors });
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
                var updatedInvoice = await _invoiceService.UpdateAsync(id, updateDto);
                _logger.LogInformation("Updated invoice {InvoiceId}", id);
                return Ok(updatedInvoice);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning("Invoice update validation failed for ID {InvoiceId}: {ValidationErrors}", 
                    id, string.Join("; ", ex.Errors));
                return BadRequest(new { errors = ex.Errors });
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
        /// <returns>Success or failure result</returns>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteInvoice(int id)
        {
            try
            {
                var deleted = await _invoiceService.DeleteAsync(id);
                if (!deleted)
                {
                    return BadRequest("Invoice cannot be deleted due to business rules or was not found");
                }

                _logger.LogInformation("Deleted invoice {InvoiceId}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting invoice {InvoiceId}", id);
                return StatusCode(500, "An error occurred while deleting the invoice");
            }
        }

        /// <summary>
        /// Gets invoices flagged as anomalous.
        /// </summary>
        /// <returns>List of anomalous invoices</returns>
        [HttpGet("anomalous")]
        public async Task<ActionResult<IEnumerable<InvoiceResponseDto>>> GetAnomalousInvoices()
        {
            try
            {
                var anomalousInvoices = await _invoiceService.GetAnomalousInvoicesAsync();
                _logger.LogInformation("Retrieved {Count} anomalous invoices", anomalousInvoices.Count());
                return Ok(anomalousInvoices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving anomalous invoices");
                return StatusCode(500, "An error occurred while retrieving anomalous invoices");
            }
        }

        /// <summary>
        /// Gets invoices with negative amounts.
        /// </summary>
        /// <returns>List of negative amount invoices</returns>
        [HttpGet("negative-amounts")]
        public async Task<ActionResult<IEnumerable<InvoiceResponseDto>>> GetNegativeAmountInvoices()
        {
            try
            {
                var negativeAmountInvoices = await _invoiceService.GetNegativeAmountInvoicesAsync();
                _logger.LogInformation("Retrieved {Count} negative amount invoices", negativeAmountInvoices.Count());
                return Ok(negativeAmountInvoices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving negative amount invoices");
                return StatusCode(500, "An error occurred while retrieving negative amount invoices");
            }
        }

        /// <summary>
        /// Gets invoices with future dates.
        /// </summary>
        /// <returns>List of future-dated invoices</returns>
        [HttpGet("future-dated")]
        public async Task<ActionResult<IEnumerable<InvoiceResponseDto>>> GetFutureDatedInvoices()
        {
            try
            {
                var futureDatedInvoices = await _invoiceService.GetFutureDatedInvoicesAsync();
                _logger.LogInformation("Retrieved {Count} future-dated invoices", futureDatedInvoices.Count());
                return Ok(futureDatedInvoices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving future-dated invoices");
                return StatusCode(500, "An error occurred while retrieving future-dated invoices");
            }
        }

        /// <summary>
        /// Gets statistical summary of invoices.
        /// </summary>
        /// <returns>Invoice statistics</returns>
        [HttpGet("statistics")]
        public async Task<ActionResult<InvoiceStatisticsDto>> GetInvoiceStatistics()
        {
            try
            {
                var statistics = await _invoiceService.GetStatisticsAsync();
                _logger.LogInformation("Retrieved invoice statistics: {TotalCount} total, {AnomalousCount} anomalous", 
                    statistics.TotalCount, statistics.AnomalousCount);
                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoice statistics");
                return StatusCode(500, "An error occurred while retrieving invoice statistics");
            }
        }
    }
}