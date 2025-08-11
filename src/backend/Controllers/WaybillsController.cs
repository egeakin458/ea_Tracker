using Microsoft.AspNetCore.Mvc;
using ea_Tracker.Models;
using ea_Tracker.Models.Dtos;
using ea_Tracker.Repositories;
using System.Linq.Expressions;

namespace ea_Tracker.Controllers
{
    /// <summary>
    /// API controller for managing waybills with full CRUD operations.
    /// Simplified to use repository directly after removing over-engineered service layer.
    /// Business logic consolidated in controller for better maintainability.
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
                _logger.LogDebug("Retrieving waybills with filters: hasAnomalies={HasAnomalies}, fromDate={FromDate}, toDate={ToDate}, recipientName={RecipientName}",
                    hasAnomalies, fromDate, toDate, recipientName);

                var waybills = await _waybillRepository.GetAsync(
                    filter: w => (hasAnomalies == null || w.HasAnomalies == hasAnomalies) &&
                                (fromDate == null || w.GoodsIssueDate >= fromDate) &&
                                (toDate == null || w.GoodsIssueDate <= toDate) &&
                                (recipientName == null || w.RecipientName!.Contains(recipientName)),
                    orderBy: q => q.OrderByDescending(w => w.CreatedAt)
                );

                var response = waybills.Select(MapToResponseDto);
                _logger.LogInformation("Retrieved {WaybillCount} waybills", response.Count());
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
                _logger.LogDebug("Retrieving waybill with ID {WaybillId}", id);
                
                var waybill = await _waybillRepository.GetByIdAsync(id);
                if (waybill == null)
                {
                    _logger.LogWarning("Waybill with ID {WaybillId} not found", id);
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
                // Validate the input DTO
                var validationErrors = ValidateWaybillDto(createDto);
                if (validationErrors.Any())
                {
                    var errorMessage = string.Join("; ", validationErrors);
                    _logger.LogWarning("Waybill validation failed: {ValidationErrors}", errorMessage);
                    return BadRequest($"Validation failed: {errorMessage}");
                }

                var waybill = new Waybill
                {
                    RecipientName = createDto.RecipientName,
                    GoodsIssueDate = createDto.GoodsIssueDate,
                    WaybillType = createDto.WaybillType,
                    ShippedItems = createDto.ShippedItems,
                    DueDate = createDto.DueDate,
                    HasAnomalies = false
                };

                // Apply business rules validation
                var businessValidationErrors = ValidateWaybill(waybill);
                if (businessValidationErrors.Any())
                {
                    var errorMessage = string.Join("; ", businessValidationErrors);
                    _logger.LogWarning("Waybill business rules validation failed: {ValidationErrors}", errorMessage);
                    return BadRequest($"Business rules validation failed: {errorMessage}");
                }

                var createdWaybill = await _waybillRepository.AddAsync(waybill);
                await _waybillRepository.SaveChangesAsync();
                
                _logger.LogInformation("Created new waybill {WaybillId} for recipient {RecipientName}", 
                    createdWaybill.Id, createDto.RecipientName);

                return CreatedAtAction(nameof(GetWaybill), new { id = createdWaybill.Id }, MapToResponseDto(createdWaybill));
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
                var waybill = await _waybillRepository.GetByIdAsync(id);
                if (waybill == null)
                {
                    _logger.LogWarning("Waybill with ID {WaybillId} not found for update", id);
                    return NotFound($"Waybill with ID {id} not found");
                }

                // Store original values for logging
                var originalDueDate = waybill.DueDate;

                // Validate the update DTO
                var validationErrors = ValidateWaybillUpdateDto(updateDto);
                if (validationErrors.Any())
                {
                    var errorMessage = string.Join("; ", validationErrors);
                    _logger.LogWarning("Waybill update DTO validation failed for ID {WaybillId}: {ValidationErrors}", id, errorMessage);
                    return BadRequest($"Validation failed: {errorMessage}");
                }

                // Update properties
                waybill.RecipientName = updateDto.RecipientName;
                waybill.GoodsIssueDate = updateDto.GoodsIssueDate;
                waybill.WaybillType = updateDto.WaybillType;
                waybill.ShippedItems = updateDto.ShippedItems;
                waybill.DueDate = updateDto.DueDate;

                // Apply business rules validation
                var businessValidationErrors = ValidateWaybill(waybill);
                if (businessValidationErrors.Any())
                {
                    var errorMessage = string.Join("; ", businessValidationErrors);
                    _logger.LogWarning("Waybill update validation failed for ID {WaybillId}: {ValidationErrors}", id, errorMessage);
                    return BadRequest($"Business rules validation failed: {errorMessage}");
                }

                _waybillRepository.Update(waybill);
                await _waybillRepository.SaveChangesAsync();
                
                _logger.LogInformation("Updated waybill {WaybillId} (due date changed from {OriginalDueDate} to {NewDueDate})", 
                    id, originalDueDate, waybill.DueDate);

                return Ok(MapToResponseDto(waybill));
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
                var waybill = await _waybillRepository.GetByIdAsync(id);
                if (waybill == null)
                {
                    _logger.LogWarning("Waybill with ID {WaybillId} not found for deletion", id);
                    return NotFound($"Waybill with ID {id} not found");
                }

                // Check business rules for deletion
                if (!CanDeleteWaybill(waybill))
                {
                    _logger.LogWarning("Waybill {WaybillId} cannot be deleted due to business rules", id);
                    return BadRequest("Waybill cannot be deleted due to business constraints");
                }

                _waybillRepository.Remove(waybill);
                await _waybillRepository.SaveChangesAsync();
                
                _logger.LogInformation("Deleted waybill {WaybillId}", id);
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
                _logger.LogDebug("Retrieving anomalous waybills");

                var waybills = await _waybillRepository.GetAsync(
                    filter: w => w.HasAnomalies,
                    orderBy: q => q.OrderByDescending(w => w.LastInvestigatedAt)
                );

                var response = waybills.Select(MapToResponseDto);
                _logger.LogInformation("Retrieved {AnomalousWaybillCount} anomalous waybills", response.Count());
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
                _logger.LogDebug("Retrieving late waybills (older than {DaysLate} days)", daysLate);

                var cutoffDate = DateTime.UtcNow.AddDays(-daysLate);
                var waybills = await _waybillRepository.GetAsync(
                    filter: w => w.GoodsIssueDate < cutoffDate,
                    orderBy: q => q.OrderBy(w => w.GoodsIssueDate)
                );

                var response = waybills.Select(MapToResponseDto);
                _logger.LogInformation("Retrieved {LateWaybillCount} late waybills", response.Count());
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
                _logger.LogDebug("Calculating waybill statistics");

                var totalCount = await _waybillRepository.CountAsync();
                var anomalyCount = await _waybillRepository.CountAsync(w => w.HasAnomalies);
                var cutoffDate = DateTime.UtcNow.AddDays(-7);
                var lateCount = await _waybillRepository.CountAsync(w => w.GoodsIssueDate < cutoffDate);

                // Enhanced statistics with due date tracking
                var now = DateTime.UtcNow;
                var overdueCount = await _waybillRepository.CountAsync(w => w.DueDate.HasValue && w.DueDate.Value < now);
                var expiringSoonCount = await _waybillRepository.CountAsync(w => w.DueDate.HasValue && 
                    w.DueDate.Value >= now && w.DueDate.Value <= now.AddHours(24));

                var stats = new
                {
                    TotalWaybills = totalCount,
                    AnomalousWaybills = anomalyCount,
                    LateWaybills = lateCount,
                    OverdueWaybills = overdueCount,
                    ExpiringSoonWaybills = expiringSoonCount,
                    AnomalyRate = totalCount > 0 ? (double)anomalyCount / totalCount * 100 : 0,
                    LateRate = totalCount > 0 ? (double)lateCount / totalCount * 100 : 0,
                    OverdueRate = totalCount > 0 ? (double)overdueCount / totalCount * 100 : 0
                };

                _logger.LogInformation("Calculated waybill statistics: {TotalWaybills} total, {AnomalousWaybills} anomalous, {OverdueWaybills} overdue", 
                    totalCount, anomalyCount, overdueCount);

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving waybill statistics");
                return StatusCode(500, "An error occurred while retrieving waybill statistics");
            }
        }
        /// <summary>
        /// Validates a waybill against business rules without persisting.
        /// </summary>
        private IEnumerable<string> ValidateWaybill(Waybill waybill)
        {
            var errors = new List<string>();

            // Business rule: Goods issue date cannot be in future
            if (waybill.GoodsIssueDate > DateTime.UtcNow.Date)
            {
                errors.Add("Goods issue date cannot be in the future");
            }

            // Business rule: Goods issue date cannot be too old (more than 5 years)
            if (waybill.GoodsIssueDate < DateTime.UtcNow.AddYears(-5).Date)
            {
                errors.Add("Goods issue date cannot be more than 5 years old");
            }

            // Business rule: Due date must be after goods issue date
            if (waybill.DueDate.HasValue && waybill.DueDate.Value.Date < waybill.GoodsIssueDate.Date)
            {
                errors.Add("Due date cannot be earlier than goods issue date");
            }

            // Business rule: Due date cannot be too far in future (more than 1 year)
            if (waybill.DueDate.HasValue && waybill.DueDate.Value > DateTime.UtcNow.AddYears(1))
            {
                errors.Add("Due date cannot be more than 1 year in the future");
            }

            return errors;
        }

        /// <summary>
        /// Validates a waybill DTO against business rules and constraints.
        /// </summary>
        private IEnumerable<string> ValidateWaybillDto(CreateWaybillDto createDto)
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

            // Business rule: Shipped items length
            if (!string.IsNullOrEmpty(createDto.ShippedItems) && createDto.ShippedItems.Length > 1000)
            {
                errors.Add("Shipped items description cannot exceed 1000 characters");
            }

            // Apply entity-level validation
            var tempWaybill = new Waybill
            {
                RecipientName = createDto.RecipientName,
                GoodsIssueDate = createDto.GoodsIssueDate,
                WaybillType = createDto.WaybillType,
                ShippedItems = createDto.ShippedItems,
                DueDate = createDto.DueDate
            };

            errors.AddRange(ValidateWaybill(tempWaybill));

            return errors;
        }

        /// <summary>
        /// Validates a waybill update DTO against business rules and constraints.
        /// </summary>
        private IEnumerable<string> ValidateWaybillUpdateDto(UpdateWaybillDto updateDto)
        {
            var errors = new List<string>();

            // Required field validation (beyond data annotations)
            if (string.IsNullOrWhiteSpace(updateDto.RecipientName))
            {
                errors.Add("Recipient name is required");
            }

            // Business rule: Recipient name length
            if (!string.IsNullOrEmpty(updateDto.RecipientName) && updateDto.RecipientName.Length > 200)
            {
                errors.Add("Recipient name cannot exceed 200 characters");
            }

            // Business rule: Shipped items length
            if (!string.IsNullOrEmpty(updateDto.ShippedItems) && updateDto.ShippedItems.Length > 1000)
            {
                errors.Add("Shipped items description cannot exceed 1000 characters");
            }

            // Apply entity-level validation
            var tempWaybill = new Waybill
            {
                RecipientName = updateDto.RecipientName,
                GoodsIssueDate = updateDto.GoodsIssueDate,
                WaybillType = updateDto.WaybillType,
                ShippedItems = updateDto.ShippedItems,
                DueDate = updateDto.DueDate
            };

            errors.AddRange(ValidateWaybill(tempWaybill));

            return errors;
        }

        /// <summary>
        /// Checks if a waybill can be safely deleted based on business rules.
        /// </summary>
        private bool CanDeleteWaybill(Waybill waybill)
        {
            // Business rule: Cannot delete waybills that have been investigated and have anomalies
            if (waybill.HasAnomalies && waybill.LastInvestigatedAt.HasValue)
            {
                return false;
            }

            // Business rule: Cannot delete waybills older than 30 days (audit requirements)
            if (waybill.CreatedAt < DateTime.UtcNow.AddDays(-30))
            {
                return false;
            }

            // Business rule: Cannot delete overdue waybills (business compliance)
            if (waybill.DueDate.HasValue && waybill.DueDate.Value < DateTime.UtcNow)
            {
                return false;
            }

            return true;
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
                LastInvestigatedAt = waybill.LastInvestigatedAt,
                DueDate = waybill.DueDate
            };
        }
    }
}