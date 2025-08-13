using Microsoft.AspNetCore.Mvc;
using ea_Tracker.Models.Dtos;
using ea_Tracker.Services.Interfaces;
using ea_Tracker.Exceptions;

namespace ea_Tracker.Controllers
{
    /// <summary>
    /// API controller for managing waybills with service layer architecture.
    /// Delegates business logic to WaybillService for better separation of concerns.
    /// Controllers are now thin and focus only on HTTP request/response handling.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class WaybillsController : ControllerBase
    {
        private readonly IWaybillService _waybillService;
        private readonly ILogger<WaybillsController> _logger;

        public WaybillsController(
            IWaybillService waybillService,
            ILogger<WaybillsController> logger)
        {
            _waybillService = waybillService;
            _logger = logger;
        }

        /// <summary>
        /// Gets all waybills with optional filtering.
        /// </summary>
        /// <param name="hasAnomalies">Filter by anomaly status</param>
        /// <param name="fromDate">Filter by goods issue date from</param>
        /// <param name="toDate">Filter by goods issue date to</param>
        /// <param name="recipientName">Filter by recipient name (partial match)</param>
        /// <returns>List of waybills</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<WaybillResponseDto>>> GetWaybills(
            bool? hasAnomalies = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? recipientName = null)
        {
            try
            {
                _logger.LogDebug("Retrieving waybills with filters: hasAnomalies={HasAnomalies}, fromDate={FromDate}, toDate={ToDate}, recipientName={RecipientName}",
                    hasAnomalies, fromDate, toDate, recipientName);

                var filter = new WaybillFilterDto
                {
                    HasAnomalies = hasAnomalies,
                    FromIssueDate = fromDate,
                    ToIssueDate = toDate,
                    RecipientName = recipientName
                };

                var waybills = await _waybillService.GetAllAsync(filter);
                _logger.LogInformation("Retrieved {WaybillCount} waybills", waybills.Count());
                return Ok(waybills);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving waybills");
                return StatusCode(500, "An error occurred while retrieving waybills");
            }
        }

        /// <summary>
        /// Gets a specific waybill by ID.
        /// </summary>
        /// <param name="id">The waybill ID</param>
        /// <returns>The waybill details</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<WaybillResponseDto>> GetWaybill(int id)
        {
            try
            {
                _logger.LogDebug("Retrieving waybill with ID {WaybillId}", id);
                
                var waybill = await _waybillService.GetByIdAsync(id);
                if (waybill == null)
                {
                    _logger.LogWarning("Waybill with ID {WaybillId} not found", id);
                    return NotFound($"Waybill with ID {id} not found");
                }

                return Ok(waybill);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving waybill {WaybillId}", id);
                return StatusCode(500, "An error occurred while retrieving the waybill");
            }
        }

        /// <summary>
        /// Creates a new waybill.
        /// </summary>
        /// <param name="createDto">The waybill creation data</param>
        /// <returns>The created waybill</returns>
        [HttpPost]
        public async Task<ActionResult<WaybillResponseDto>> CreateWaybill(CreateWaybillDto createDto)
        {
            try
            {
                var createdWaybill = await _waybillService.CreateAsync(createDto);
                _logger.LogInformation("Created new waybill {WaybillId} for recipient {RecipientName}", 
                    createdWaybill.Id, createDto.RecipientName);

                return CreatedAtAction(nameof(GetWaybill), new { id = createdWaybill.Id }, createdWaybill);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning("Waybill validation failed: {ValidationErrors}", string.Join("; ", ex.Errors));
                return BadRequest(new { errors = ex.Errors });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating waybill for recipient {RecipientName}", createDto.RecipientName);
                return StatusCode(500, "An error occurred while creating the waybill");
            }
        }

        /// <summary>
        /// Updates an existing waybill.
        /// </summary>
        /// <param name="id">The waybill ID</param>
        /// <param name="updateDto">The waybill update data</param>
        /// <returns>The updated waybill</returns>
        [HttpPut("{id}")]
        public async Task<ActionResult<WaybillResponseDto>> UpdateWaybill(int id, UpdateWaybillDto updateDto)
        {
            try
            {
                var updatedWaybill = await _waybillService.UpdateAsync(id, updateDto);
                _logger.LogInformation("Updated waybill {WaybillId}", id);
                return Ok(updatedWaybill);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning("Waybill update validation failed for ID {WaybillId}: {ValidationErrors}", 
                    id, string.Join("; ", ex.Errors));
                return BadRequest(new { errors = ex.Errors });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating waybill {WaybillId}", id);
                return StatusCode(500, "An error occurred while updating the waybill");
            }
        }

        /// <summary>
        /// Deletes a waybill.
        /// </summary>
        /// <param name="id">The waybill ID</param>
        /// <returns>Success or failure result</returns>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteWaybill(int id)
        {
            try
            {
                var deleted = await _waybillService.DeleteAsync(id);
                if (!deleted)
                {
                    return BadRequest("Waybill cannot be deleted due to business rules or was not found");
                }

                _logger.LogInformation("Deleted waybill {WaybillId}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting waybill {WaybillId}", id);
                return StatusCode(500, "An error occurred while deleting the waybill");
            }
        }

        /// <summary>
        /// Gets waybills flagged as anomalous.
        /// </summary>
        /// <returns>List of anomalous waybills</returns>
        [HttpGet("anomalous")]
        public async Task<ActionResult<IEnumerable<WaybillResponseDto>>> GetAnomalousWaybills()
        {
            try
            {
                var anomalousWaybills = await _waybillService.GetAnomalousWaybillsAsync();
                _logger.LogInformation("Retrieved {Count} anomalous waybills", anomalousWaybills.Count());
                return Ok(anomalousWaybills);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving anomalous waybills");
                return StatusCode(500, "An error occurred while retrieving anomalous waybills");
            }
        }

        /// <summary>
        /// Gets waybills that are overdue.
        /// </summary>
        /// <returns>List of overdue waybills</returns>
        [HttpGet("overdue")]
        public async Task<ActionResult<IEnumerable<WaybillResponseDto>>> GetOverdueWaybills()
        {
            try
            {
                var overdueWaybills = await _waybillService.GetOverdueWaybillsAsync();
                _logger.LogInformation("Retrieved {Count} overdue waybills", overdueWaybills.Count());
                return Ok(overdueWaybills);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving overdue waybills");
                return StatusCode(500, "An error occurred while retrieving overdue waybills");
            }
        }

        /// <summary>
        /// Gets waybills expiring soon.
        /// </summary>
        /// <returns>List of soon-to-expire waybills</returns>
        [HttpGet("expiring-soon")]
        public async Task<ActionResult<IEnumerable<WaybillResponseDto>>> GetWaybillsExpiringSoon()
        {
            try
            {
                var expiringSoonWaybills = await _waybillService.GetWaybillsExpiringSoonAsync();
                _logger.LogInformation("Retrieved {Count} waybills expiring soon", expiringSoonWaybills.Count());
                return Ok(expiringSoonWaybills);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving waybills expiring soon");
                return StatusCode(500, "An error occurred while retrieving waybills expiring soon");
            }
        }

        /// <summary>
        /// Gets legacy waybills (older than cutoff).
        /// </summary>
        /// <returns>List of legacy waybills</returns>
        [HttpGet("legacy")]
        public async Task<ActionResult<IEnumerable<WaybillResponseDto>>> GetLegacyWaybills()
        {
            try
            {
                var legacyWaybills = await _waybillService.GetLegacyWaybillsAsync();
                _logger.LogInformation("Retrieved {Count} legacy waybills", legacyWaybills.Count());
                return Ok(legacyWaybills);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving legacy waybills");
                return StatusCode(500, "An error occurred while retrieving legacy waybills");
            }
        }

        /// <summary>
        /// Gets statistical summary of waybills.
        /// </summary>
        /// <returns>Waybill statistics</returns>
        [HttpGet("statistics")]
        public async Task<ActionResult<WaybillStatisticsDto>> GetWaybillStatistics()
        {
            try
            {
                var statistics = await _waybillService.GetStatisticsAsync();
                _logger.LogInformation("Retrieved waybill statistics: {TotalCount} total, {AnomalousCount} anomalous, {OverdueCount} overdue", 
                    statistics.TotalCount, statistics.AnomalousCount, statistics.OverdueCount);
                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving waybill statistics");
                return StatusCode(500, "An error occurred while retrieving waybill statistics");
            }
        }
    }
}