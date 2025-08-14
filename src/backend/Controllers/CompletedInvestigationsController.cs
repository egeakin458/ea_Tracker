using Microsoft.AspNetCore.Mvc;
using ea_Tracker.Models.Dtos;
using ea_Tracker.Services.Interfaces;

namespace ea_Tracker.Controllers
{
    /// <summary>
    /// API controller for managing completed investigations with service layer architecture.
    /// Delegates business logic to CompletedInvestigationService for better separation of concerns.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class CompletedInvestigationsController : ControllerBase
    {
        private readonly ICompletedInvestigationService _investigationService;
        private readonly ILogger<CompletedInvestigationsController> _logger;

        public CompletedInvestigationsController(
            ICompletedInvestigationService investigationService,
            ILogger<CompletedInvestigationsController> logger)
        {
            _investigationService = investigationService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCompleted()
        {
            try
            {
                var completedInvestigations = await _investigationService.GetAllCompletedAsync();
                return Ok(completedInvestigations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving completed investigations");
                return StatusCode(500, "An error occurred while retrieving completed investigations");
            }
        }

        [HttpGet("{executionId}")]
        public async Task<IActionResult> GetInvestigationDetail(int executionId)
        {
            try
            {
                var detail = await _investigationService.GetInvestigationDetailAsync(executionId);
                if (detail == null)
                {
                    return NotFound($"Investigation execution with ID {executionId} not found.");
                }
                return Ok(detail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving investigation detail for execution {ExecutionId}", executionId);
                return StatusCode(500, "An error occurred while retrieving investigation details");
            }
        }

        [HttpDelete("clear")]
        public async Task<IActionResult> ClearAllCompletedInvestigations()
        {
            try
            {
                var result = await _investigationService.ClearAllCompletedInvestigationsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing investigation results");
                return StatusCode(500, new { message = "Failed to clear investigation results", error = ex.Message });
            }
        }

        [HttpDelete("{executionId}")]
        public async Task<IActionResult> DeleteInvestigationExecution(int executionId)
        {
            try
            {
                var result = await _investigationService.DeleteInvestigationExecutionAsync(executionId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting investigation execution {ExecutionId}", executionId);
                return StatusCode(500, new { message = "Failed to delete investigation execution", error = ex.Message });
            }
        }
    }
}