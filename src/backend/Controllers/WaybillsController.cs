using Microsoft.AspNetCore.Mvc;
using ea_Tracker.Services;
using ea_Tracker.Models.Dtos;

namespace ea_Tracker.Controllers
{
    /// <summary>
    /// API controller for managing waybills with full CRUD operations.
    /// Refactored to use IWaybillService for SOLID compliance (Dependency Inversion Principle).
    /// All business logic moved to service layer while maintaining exact API compatibility.
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
                var waybills = await _waybillService.GetWaybillsAsync(hasAnomalies, fromDate, toDate, recipientName);
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
                var waybill = await _waybillService.GetWaybillByIdAsync(id);
                if (waybill == null)
                {
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
                var createdWaybill = await _waybillService.CreateWaybillAsync(createDto);
                return CreatedAtAction(nameof(GetWaybill), new { id = createdWaybill.Id }, createdWaybill);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation failed for waybill creation");
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Business rules violation for waybill creation");
                return BadRequest(ex.Message);
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
                var updatedWaybill = await _waybillService.UpdateWaybillAsync(id, updateDto);
                if (updatedWaybill == null)
                {
                    return NotFound($"Waybill with ID {id} not found");
                }

                return Ok(updatedWaybill);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation failed for waybill update");
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Business rules violation for waybill update");
                return BadRequest(ex.Message);
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
        /// <returns>No content if successful</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWaybill(int id)
        {
            try
            {
                var deleted = await _waybillService.DeleteWaybillAsync(id);
                if (!deleted)
                {
                    return NotFound($"Waybill with ID {id} not found");
                }

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Business rules prevent waybill deletion");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting waybill {WaybillId}", id);
                return StatusCode(500, "An error occurred while deleting the waybill");
            }
        }

        /// <summary>
        /// Gets waybills with anomalies.
        /// </summary>
        /// <returns>List of waybills with detected anomalies</returns>
        [HttpGet("anomalies")]
        public async Task<ActionResult<IEnumerable<WaybillResponseDto>>> GetAnomalousWaybills()
        {
            try
            {
                var waybills = await _waybillService.GetAnomalousWaybillsAsync();
                return Ok(waybills);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving anomalous waybills");
                return StatusCode(500, "An error occurred while retrieving anomalous waybills");
            }
        }

        /// <summary>
        /// Gets late waybills (older than specified days).
        /// </summary>
        /// <param name="daysLate">Number of days to consider late (default: 7)</param>
        /// <returns>List of late waybills</returns>
        [HttpGet("late")]
        public async Task<ActionResult<IEnumerable<WaybillResponseDto>>> GetLateWaybills(int daysLate = 7)
        {
            try
            {
                var waybills = await _waybillService.GetLateWaybillsAsync(daysLate);
                return Ok(waybills);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving late waybills");
                return StatusCode(500, "An error occurred while retrieving late waybills");
            }
        }

        /// <summary>
        /// Gets waybill statistics.
        /// </summary>
        /// <returns>Waybill statistics summary</returns>
        [HttpGet("stats")]
        public async Task<ActionResult<object>> GetWaybillStats()
        {
            try
            {
                var stats = await _waybillService.GetWaybillStatisticsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving waybill statistics");
                return StatusCode(500, "An error occurred while retrieving waybill statistics");
            }
        }
    }
}