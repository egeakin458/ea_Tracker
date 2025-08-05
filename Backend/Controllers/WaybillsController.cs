using Microsoft.AspNetCore.Mvc;
using ea_Tracker.Repositories;
using ea_Tracker.Models;
using ea_Tracker.Models.Dtos;

namespace ea_Tracker.Controllers
{
    /// <summary>
    /// API controller for managing waybills with full CRUD operations.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class WaybillsController : ControllerBase
    {
        private readonly IGenericRepository<Waybill> _waybillRepository;
        private readonly ILogger<WaybillsController> _logger;

        public WaybillsController(
            IGenericRepository<Waybill> waybillRepository,
            ILogger<WaybillsController> logger)
        {
            _waybillRepository = waybillRepository;
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
                var waybills = await _waybillRepository.GetAsync(
                    filter: w => (hasAnomalies == null || w.HasAnomalies == hasAnomalies) &&
                                (fromDate == null || w.GoodsIssueDate >= fromDate) &&
                                (toDate == null || w.GoodsIssueDate <= toDate) &&
                                (recipientName == null || w.RecipientName!.Contains(recipientName)),
                    orderBy: q => q.OrderByDescending(w => w.CreatedAt)
                );

                var response = waybills.Select(MapToResponseDto);
                return Ok(response);
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
                var waybill = await _waybillRepository.GetByIdAsync(id);
                if (waybill == null)
                {
                    return NotFound($"Waybill with ID {id} not found");
                }

                return Ok(MapToResponseDto(waybill));
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
                var waybill = new Waybill
                {
                    RecipientName = createDto.RecipientName,
                    GoodsIssueDate = createDto.GoodsIssueDate,
                    WaybillType = createDto.WaybillType,
                    ShippedItems = createDto.ShippedItems,
                    HasAnomalies = false
                };

                var createdWaybill = await _waybillRepository.AddAsync(waybill);
                await _waybillRepository.SaveChangesAsync();
                
                _logger.LogInformation("Created new waybill {WaybillId} for recipient {RecipientName}", 
                    createdWaybill.Id, createDto.RecipientName);

                return CreatedAtAction(nameof(GetWaybill), new { id = createdWaybill.Id }, MapToResponseDto(createdWaybill));
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
                var waybill = await _waybillRepository.GetByIdAsync(id);
                if (waybill == null)
                {
                    return NotFound($"Waybill with ID {id} not found");
                }

                // Update properties
                waybill.RecipientName = updateDto.RecipientName;
                waybill.GoodsIssueDate = updateDto.GoodsIssueDate;
                waybill.WaybillType = updateDto.WaybillType;
                waybill.ShippedItems = updateDto.ShippedItems;

                _waybillRepository.Update(waybill);
                await _waybillRepository.SaveChangesAsync();
                
                _logger.LogInformation("Updated waybill {WaybillId}", id);

                return Ok(MapToResponseDto(waybill));
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
                var waybill = await _waybillRepository.GetByIdAsync(id);
                if (waybill == null)
                {
                    return NotFound($"Waybill with ID {id} not found");
                }

                _waybillRepository.Remove(waybill);
                await _waybillRepository.SaveChangesAsync();
                
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
        /// Gets waybills with anomalies.
        /// </summary>
        /// <returns>List of waybills with detected anomalies</returns>
        [HttpGet("anomalies")]
        public async Task<ActionResult<IEnumerable<WaybillResponseDto>>> GetAnomalousWaybills()
        {
            try
            {
                var waybills = await _waybillRepository.GetAsync(
                    filter: w => w.HasAnomalies,
                    orderBy: q => q.OrderByDescending(w => w.LastInvestigatedAt)
                );

                var response = waybills.Select(MapToResponseDto);
                return Ok(response);
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
                var cutoffDate = DateTime.UtcNow.AddDays(-daysLate);
                var waybills = await _waybillRepository.GetAsync(
                    filter: w => w.GoodsIssueDate < cutoffDate,
                    orderBy: q => q.OrderBy(w => w.GoodsIssueDate)
                );

                var response = waybills.Select(MapToResponseDto);
                return Ok(response);
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
                var totalCount = await _waybillRepository.CountAsync();
                var anomalyCount = await _waybillRepository.CountAsync(w => w.HasAnomalies);
                var cutoffDate = DateTime.UtcNow.AddDays(-7);
                var lateCount = await _waybillRepository.CountAsync(w => w.GoodsIssueDate < cutoffDate);

                var stats = new
                {
                    TotalWaybills = totalCount,
                    AnomalousWaybills = anomalyCount,
                    LateWaybills = lateCount,
                    AnomalyRate = totalCount > 0 ? (double)anomalyCount / totalCount * 100 : 0,
                    LateRate = totalCount > 0 ? (double)lateCount / totalCount * 100 : 0
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving waybill statistics");
                return StatusCode(500, "An error occurred while retrieving waybill statistics");
            }
        }

        /// <summary>
        /// Maps a Waybill entity to a response DTO.
        /// </summary>
        private static WaybillResponseDto MapToResponseDto(Waybill waybill)
        {
            return new WaybillResponseDto
            {
                Id = waybill.Id,
                RecipientName = waybill.RecipientName,
                GoodsIssueDate = waybill.GoodsIssueDate,
                WaybillType = waybill.WaybillType,
                ShippedItems = waybill.ShippedItems,
                CreatedAt = waybill.CreatedAt,
                UpdatedAt = waybill.UpdatedAt,
                HasAnomalies = waybill.HasAnomalies,
                LastInvestigatedAt = waybill.LastInvestigatedAt
            };
        }
    }
}