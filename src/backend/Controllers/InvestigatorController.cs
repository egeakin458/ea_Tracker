using Microsoft.AspNetCore.Mvc;
using ea_Tracker.Models.Dtos;
using ea_Tracker.Services.Interfaces;
using ea_Tracker.Exceptions;

namespace ea_Tracker.Controllers
{
    /// <summary>
    /// API controller for managing investigator instances with service layer architecture.
    /// Delegates business logic to InvestigatorAdminService for better separation of concerns.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class InvestigatorController : ControllerBase
    {
        private readonly IInvestigatorAdminService _investigatorService;
        private readonly ILogger<InvestigatorController> _logger;

        public InvestigatorController(
            IInvestigatorAdminService investigatorService,
            ILogger<InvestigatorController> logger)
        {
            _investigatorService = investigatorService;
            _logger = logger;
        }

        /// <summary>
        /// Gets all active investigator instances with their types.
        /// </summary>
        /// <returns>List of active investigator instances</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<InvestigatorInstanceResponseDto>>> GetInvestigators()
        {
            try
            {
                var investigators = await _investigatorService.GetInvestigatorsAsync();
                return Ok(investigators);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving investigators");
                return StatusCode(500, "An error occurred while retrieving investigators");
            }
        }

        /// <summary>
        /// Gets a specific investigator instance by ID.
        /// </summary>
        /// <param name="id">The investigator instance ID</param>
        /// <returns>The investigator instance details</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<InvestigatorInstanceResponseDto>> GetInvestigator(Guid id)
        {
            try
            {
                var investigator = await _investigatorService.GetInvestigatorAsync(id);
                if (investigator == null)
                {
                    return NotFound($"Investigator with ID {id} not found");
                }
                return Ok(investigator);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving investigator {InvestigatorId}", id);
                return StatusCode(500, "An error occurred while retrieving the investigator");
            }
        }

        /// <summary>
        /// Creates a new investigator instance.
        /// </summary>
        /// <param name="createDto">The investigator creation data</param>
        /// <returns>The created investigator instance</returns>
        [HttpPost]
        public async Task<ActionResult<InvestigatorInstanceResponseDto>> CreateInvestigator(CreateInvestigatorInstanceDto createDto)
        {
            try
            {
                var createdInvestigator = await _investigatorService.CreateInvestigatorAsync(createDto);
                return CreatedAtAction(nameof(GetInvestigator), new { id = createdInvestigator.Id }, createdInvestigator);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning("Investigator validation failed: {ValidationErrors}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating investigator");
                return StatusCode(500, "An error occurred while creating the investigator");
            }
        }

        /// <summary>
        /// Updates an existing investigator instance.
        /// </summary>
        /// <param name="id">The investigator instance ID</param>
        /// <param name="updateDto">The investigator update data</param>
        /// <returns>The updated investigator instance</returns>
        [HttpPut("{id}")]
        public async Task<ActionResult<InvestigatorInstanceResponseDto>> UpdateInvestigator(Guid id, UpdateInvestigatorInstanceDto updateDto)
        {
            try
            {
                var updatedInvestigator = await _investigatorService.UpdateInvestigatorAsync(id, updateDto);
                return Ok(updatedInvestigator);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning("Investigator update validation failed: {ValidationErrors}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating investigator {InvestigatorId}", id);
                return StatusCode(500, "An error occurred while updating the investigator");
            }
        }

        /// <summary>
        /// Deletes an investigator instance.
        /// </summary>
        /// <param name="id">The investigator instance ID</param>
        /// <returns>No content if successful</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteInvestigator(Guid id)
        {
            try
            {
                var deleted = await _investigatorService.DeleteInvestigatorAsync(id);
                if (!deleted)
                {
                    return NotFound($"Investigator with ID {id} not found");
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting investigator {InvestigatorId}", id);
                return StatusCode(500, "An error occurred while deleting the investigator");
            }
        }

        /// <summary>
        /// Gets investigator instances by type.
        /// </summary>
        /// <param name="typeCode">The investigator type code</param>
        /// <returns>List of investigator instances of the specified type</returns>
        [HttpGet("by-type/{typeCode}")]
        public async Task<ActionResult<IEnumerable<InvestigatorInstanceResponseDto>>> GetInvestigatorsByType(string typeCode)
        {
            try
            {
                var investigators = await _investigatorService.GetInvestigatorsByTypeAsync(typeCode);
                return Ok(investigators);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving investigators by type {TypeCode}", typeCode);
                return StatusCode(500, "An error occurred while retrieving investigators");
            }
        }

        /// <summary>
        /// Gets summary statistics for all investigators.
        /// </summary>
        /// <returns>Investigator summary statistics</returns>
        [HttpGet("summary")]
        public async Task<ActionResult<InvestigatorSummaryDto>> GetSummary()
        {
            try
            {
                var summary = await _investigatorService.GetSummaryAsync();
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving investigator summary");
                return StatusCode(500, "An error occurred while retrieving the summary");
            }
        }

        /// <summary>
        /// Gets available investigator types.
        /// </summary>
        /// <returns>List of available investigator types</returns>
        [HttpGet("types")]
        public async Task<ActionResult<IEnumerable<InvestigatorTypeDto>>> GetTypes()
        {
            try
            {
                var types = await _investigatorService.GetTypesAsync();
                return Ok(types);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving investigator types");
                return StatusCode(500, "An error occurred while retrieving investigator types");
            }
        }
    }
}