using Microsoft.AspNetCore.Mvc;
using ea_Tracker.Repositories;
using ea_Tracker.Models;
using ea_Tracker.Models.Dtos;

namespace ea_Tracker.Controllers
{
    /// <summary>
    /// API controller for managing invoices with full CRUD operations.
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
                var invoices = await _invoiceRepository.GetAsync(
                    filter: i => (hasAnomalies == null || i.HasAnomalies == hasAnomalies) &&
                                (fromDate == null || i.IssueDate >= fromDate) &&
                                (toDate == null || i.IssueDate <= toDate) &&
                                (recipientName == null || i.RecipientName!.Contains(recipientName)),
                    orderBy: q => q.OrderByDescending(i => i.CreatedAt)
                );

                var response = invoices.Select(MapToResponseDto);
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
                var invoice = await _invoiceRepository.GetByIdAsync(id);
                if (invoice == null)
                {
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
                var invoice = new Invoice
                {
                    RecipientName = createDto.RecipientName,
                    TotalAmount = createDto.TotalAmount,
                    IssueDate = createDto.IssueDate,
                    TotalTax = createDto.TotalTax,
                    InvoiceType = createDto.InvoiceType,
                    HasAnomalies = false
                };

                var createdInvoice = await _invoiceRepository.AddAsync(invoice);
                await _invoiceRepository.SaveChangesAsync();
                
                _logger.LogInformation("Created new invoice {InvoiceId} for recipient {RecipientName}", 
                    createdInvoice.Id, createDto.RecipientName);

                return CreatedAtAction(nameof(GetInvoice), new { id = createdInvoice.Id }, MapToResponseDto(createdInvoice));
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
                    return NotFound($"Invoice with ID {id} not found");
                }

                // Update properties
                invoice.RecipientName = updateDto.RecipientName;
                invoice.TotalAmount = updateDto.TotalAmount;
                invoice.IssueDate = updateDto.IssueDate;
                invoice.TotalTax = updateDto.TotalTax;
                invoice.InvoiceType = updateDto.InvoiceType;

                _invoiceRepository.Update(invoice);
                await _invoiceRepository.SaveChangesAsync();
                
                _logger.LogInformation("Updated invoice {InvoiceId}", id);

                return Ok(MapToResponseDto(invoice));
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
                    return NotFound($"Invoice with ID {id} not found");
                }

                _invoiceRepository.Remove(invoice);
                await _invoiceRepository.SaveChangesAsync();
                
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
        /// Gets invoices with anomalies.
        /// </summary>
        /// <returns>List of invoices with detected anomalies</returns>
        [HttpGet("anomalies")]
        public async Task<ActionResult<IEnumerable<InvoiceResponseDto>>> GetAnomalousInvoices()
        {
            try
            {
                var invoices = await _invoiceRepository.GetAsync(
                    filter: i => i.HasAnomalies,
                    orderBy: q => q.OrderByDescending(i => i.LastInvestigatedAt)
                );

                var response = invoices.Select(MapToResponseDto);
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
                var totalCount = await _invoiceRepository.CountAsync();
                var anomalyCount = await _invoiceRepository.CountAsync(i => i.HasAnomalies);
                var totalAmount = (await _invoiceRepository.GetAllAsync()).Sum(i => i.TotalAmount);
                var totalTax = (await _invoiceRepository.GetAllAsync()).Sum(i => i.TotalTax);

                var stats = new
                {
                    TotalInvoices = totalCount,
                    AnomalousInvoices = anomalyCount,
                    AnomalyRate = totalCount > 0 ? (double)anomalyCount / totalCount * 100 : 0,
                    TotalAmount = totalAmount,
                    TotalTax = totalTax,
                    AverageTaxRate = totalAmount > 0 ? (double)(totalTax / totalAmount) * 100 : 0
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoice statistics");
                return StatusCode(500, "An error occurred while retrieving invoice statistics");
            }
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