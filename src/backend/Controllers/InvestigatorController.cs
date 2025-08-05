using Microsoft.AspNetCore.Mvc;
using ea_Tracker.Repositories;
using ea_Tracker.Models;
using ea_Tracker.Models.Dtos;
using ea_Tracker.Enums;

namespace ea_Tracker.Controllers
{
    /// <summary>
    /// API controller for managing investigator instances with full CRUD operations.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class InvestigatorController : ControllerBase
    {
        private readonly IInvestigatorRepository _investigatorRepository;
        private readonly IGenericRepository<InvestigatorType> _typeRepository;
        private readonly ILogger<InvestigatorController> _logger;

        public InvestigatorController(
            IInvestigatorRepository investigatorRepository,
            IGenericRepository<InvestigatorType> typeRepository,
            ILogger<InvestigatorController> logger)
        {
            _investigatorRepository = investigatorRepository;
            _typeRepository = typeRepository;
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
                var investigators = await _investigatorRepository.GetActiveWithTypesAsync();
                var response = investigators.Select(MapToResponseDto);
                return Ok(response);
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
                var investigator = await _investigatorRepository.GetWithDetailsAsync(id);
                if (investigator == null)
                {
                    return NotFound($"Investigator with ID {id} not found");
                }

                return Ok(MapToResponseDto(investigator));
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
                // Validate the investigator type exists
                var investigatorType = await _typeRepository.GetFirstOrDefaultAsync(t => t.Code == createDto.TypeCode && t.IsActive);
                if (investigatorType == null)
                {
                    return BadRequest($"Invalid investigator type code: {createDto.TypeCode}");
                }

                // Create the investigator instance
                var investigator = new InvestigatorInstance
                {
                    Id = Guid.NewGuid(),
                    TypeId = investigatorType.Id,
                    CustomName = createDto.CustomName,
                    CustomConfiguration = createDto.CustomConfiguration,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _investigatorRepository.AddAsync(investigator);
                await _investigatorRepository.SaveChangesAsync();

                // Reload with navigation properties
                var createdInvestigator = await _investigatorRepository.GetWithDetailsAsync(investigator.Id);
                
                _logger.LogInformation("Created new investigator instance {InvestigatorId} of type {TypeCode}", 
                    investigator.Id, createDto.TypeCode);

                return CreatedAtAction(nameof(GetInvestigator), new { id = investigator.Id }, MapToResponseDto(createdInvestigator!));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating investigator of type {TypeCode}", createDto.TypeCode);
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
                var investigator = await _investigatorRepository.GetByIdAsync(id);
                if (investigator == null)
                {
                    return NotFound($"Investigator with ID {id} not found");
                }

                // Update properties
                investigator.CustomName = updateDto.CustomName;
                investigator.IsActive = updateDto.IsActive;
                investigator.CustomConfiguration = updateDto.CustomConfiguration;

                _investigatorRepository.Update(investigator);
                await _investigatorRepository.SaveChangesAsync();

                // Reload with navigation properties
                var updatedInvestigator = await _investigatorRepository.GetWithDetailsAsync(id);
                
                _logger.LogInformation("Updated investigator instance {InvestigatorId}", id);

                return Ok(MapToResponseDto(updatedInvestigator!));
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
                var investigator = await _investigatorRepository.GetByIdAsync(id);
                if (investigator == null)
                {
                    return NotFound($"Investigator with ID {id} not found");
                }

                _investigatorRepository.Remove(investigator);
                await _investigatorRepository.SaveChangesAsync();
                
                _logger.LogInformation("Deleted investigator instance {InvestigatorId}", id);

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
                var investigators = await _investigatorRepository.GetByTypeAsync(typeCode);
                var response = investigators.Select(MapToResponseDto);
                return Ok(response);
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
                var summary = await _investigatorRepository.GetSummaryAsync();
                
                var response = new InvestigatorSummaryDto
                {
                    TotalInvestigators = summary.TotalInvestigators,
                    ActiveInvestigators = summary.ActiveInvestigators,
                    RunningInvestigators = summary.RunningInvestigators,
                    TotalExecutions = summary.TotalExecutions,
                    TotalResults = summary.TotalResults
                };

                return Ok(response);
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
                var types = await _typeRepository.GetAsync(t => t.IsActive, orderBy: q => q.OrderBy(t => t.DisplayName));
                var response = types.Select(t => new InvestigatorTypeDto
                {
                    Id = t.Id,
                    Code = t.Code,
                    DisplayName = t.DisplayName,
                    Description = t.Description,
                    DefaultConfiguration = t.DefaultConfiguration,
                    IsActive = t.IsActive
                });

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving investigator types");
                return StatusCode(500, "An error occurred while retrieving investigator types");
            }
        }

        /// <summary>
        /// Maps an InvestigatorInstance entity to a response DTO.
        /// </summary>
        private static InvestigatorInstanceResponseDto MapToResponseDto(InvestigatorInstance investigator)
        {
            return new InvestigatorInstanceResponseDto
            {
                Id = investigator.Id,
                Type = new InvestigatorTypeDto
                {
                    Id = investigator.Type.Id,
                    Code = investigator.Type.Code,
                    DisplayName = investigator.Type.DisplayName,
                    Description = investigator.Type.Description,
                    DefaultConfiguration = investigator.Type.DefaultConfiguration,
                    IsActive = investigator.Type.IsActive
                },
                DisplayName = investigator.DisplayName,
                CustomName = investigator.CustomName,
                CreatedAt = investigator.CreatedAt,
                LastExecutedAt = investigator.LastExecutedAt,
                IsActive = investigator.IsActive,
                Status = investigator.Status,
                TotalResultCount = investigator.TotalResultCount,
                CustomConfiguration = investigator.CustomConfiguration
            };
        }
    }
}