using Microsoft.AspNetCore.Mvc;
using ea_Tracker.Services;
using ea_Tracker.Models.Dtos;

namespace ea_Tracker.Controllers
{
    /// <summary>
    /// API controller for managing invoices with full CRUD operations.
    /// Refactored to use IInvoiceService for SOLID compliance (Dependency Inversion Principle).
    /// All business logic moved to service layer while maintaining exact API compatibility.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
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
                var invoices = await _invoiceService.GetInvoicesAsync(hasAnomalies, fromDate, toDate, recipientName);
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
                var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
                if (invoice == null)
                {
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
                var createdInvoice = await _invoiceService.CreateInvoiceAsync(createDto);
                return CreatedAtAction(nameof(GetInvoice), new { id = createdInvoice.Id }, createdInvoice);
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
                var updatedInvoice = await _invoiceService.UpdateInvoiceAsync(id, updateDto);
                if (updatedInvoice == null)
                {
                    return NotFound($"Invoice with ID {id} not found");
                }

                return Ok(updatedInvoice);
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
                var deleted = await _invoiceService.DeleteInvoiceAsync(id);
                if (!deleted)
                {
                    return NotFound($"Invoice with ID {id} not found");
                }

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
                var invoices = await _invoiceService.GetAnomalousInvoicesAsync();
                return Ok(invoices);
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
                var stats = await _invoiceService.GetInvoiceStatisticsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoice statistics");
                return StatusCode(500, "An error occurred while retrieving invoice statistics");
            }
        }

    }
}