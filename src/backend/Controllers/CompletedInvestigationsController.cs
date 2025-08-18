using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ea_Tracker.Models.Dtos;
using ea_Tracker.Services.Interfaces;
using ea_Tracker.Services;

namespace ea_Tracker.Controllers
{
    /// <summary>
    /// API controller for managing completed investigations with service layer architecture.
    /// Delegates business logic to CompletedInvestigationService for better separation of concerns.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Require authentication for all completed investigation data
    public class CompletedInvestigationsController : ControllerBase
    {
        private readonly ICompletedInvestigationService _investigationService;
        private readonly IInvestigationManager _investigationManager;
        private readonly ILogger<CompletedInvestigationsController> _logger;

        public CompletedInvestigationsController(
            ICompletedInvestigationService investigationService,
            IInvestigationManager investigationManager,
            ILogger<CompletedInvestigationsController> logger)
        {
            _investigationService = investigationService;
            _investigationManager = investigationManager;
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

        /// <summary>
        /// Clears all completed investigations.
        /// Requires Admin role for access.
        /// </summary>
        [HttpDelete("clear")]
        [Authorize(Policy = "AdminOnly")]
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

        /// <summary>
        /// Deletes a specific investigation execution.
        /// Requires Admin role for access.
        /// </summary>
        [HttpDelete("{executionId}")]
        [Authorize(Policy = "AdminOnly")]
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

        /// <summary>
        /// Exports multiple investigations to a single file in the specified format.
        /// </summary>
        /// <param name="request">The bulk export request</param>
        /// <returns>The exported file</returns>
        [HttpPost("export")]
        public async Task<IActionResult> ExportInvestigations([FromBody] BulkExportRequestDto request)
        {
            try
            {
                if (request == null || !request.ExecutionIds.Any())
                {
                    return BadRequest("No execution IDs were provided for the export.");
                }

                var exportDto = await _investigationService.ExportInvestigationsAsync(request);
                if (exportDto == null)
                {
                    return NotFound("Could not find any valid investigations for the provided IDs.");
                }

                return File(exportDto.Data, exportDto.ContentType, exportDto.FileName);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid export request");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting investigations");
                return StatusCode(500, "An error occurred while exporting investigations");
            }
        }

        /// <summary>
        /// Verifies the accuracy of result counts for a specific investigation execution.
        /// Useful for debugging count discrepancies like the one found in execution #248.
        /// </summary>
        /// <param name="executionId">The execution ID to verify.</param>
        /// <returns>Count verification result with accuracy information.</returns>
        [HttpGet("{executionId}/verify-count")]
        public async Task<ActionResult<CountVerificationResult>> VerifyResultCount(int executionId)
        {
            try
            {
                var result = await _investigationManager.VerifyResultCountAsync(executionId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying result count for execution {ExecutionId}", executionId);
                return StatusCode(500, "An error occurred while verifying the result count");
            }
        }

        /// <summary>
        /// Corrects the result count for a specific investigation execution if inaccurate.
        /// This endpoint can be used to fix historical count discrepancies.
        /// Requires Admin role for access.
        /// </summary>
        /// <param name="executionId">The execution ID to correct.</param>
        /// <returns>True if the count was corrected, false if already accurate.</returns>
        [HttpPost("{executionId}/correct-count")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<bool>> CorrectResultCount(int executionId)
        {
            try
            {
                var corrected = await _investigationManager.CorrectResultCountAsync(executionId);
                
                if (corrected)
                {
                    _logger.LogInformation("Result count corrected for execution {ExecutionId} via API", executionId);
                }
                
                return Ok(corrected);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error correcting result count for execution {ExecutionId}", executionId);
                return StatusCode(500, "An error occurred while correcting the result count");
            }
        }

        /// <summary>
        /// Corrects result counts for all investigations that have discrepancies.
        /// This is a maintenance endpoint that can be used to fix historical data issues.
        /// Should be used carefully and typically only by administrators.
        /// Requires Admin role for access.
        /// </summary>
        /// <returns>The number of investigations that had their counts corrected.</returns>
        [HttpPost("correct-all-counts")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<int>> CorrectAllResultCounts()
        {
            try
            {
                _logger.LogWarning("Starting bulk result count correction via API - this may take time");
                var correctedCount = await _investigationManager.CorrectAllResultCountsAsync();
                
                _logger.LogInformation("Bulk result count correction completed: {CorrectedCount} investigations corrected", correctedCount);
                return Ok(correctedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk result count correction");
                return StatusCode(500, "An error occurred while correcting result counts");
            }
        }
    }
}