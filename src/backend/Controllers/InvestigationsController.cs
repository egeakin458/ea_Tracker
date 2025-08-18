using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ea_Tracker.Services;
using System;
using System.Collections.Generic;
using ea_Tracker.Models.Dtos;

namespace ea_Tracker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Require authentication for all investigation operations
    /// <summary>
    /// API endpoints for controlling investigator services.
    /// Refactored to use IInvestigationManager interface for SOLID compliance (Dependency Inversion Principle).
    /// </summary>
    public class InvestigationsController : ControllerBase
    {
        private readonly IInvestigationManager _manager;

        /// <summary>
        /// Initializes a new instance of the <see cref="InvestigationsController"/> class.
        /// </summary>
        /// <param name="manager">The investigation manager interface.</param>
        public InvestigationsController(IInvestigationManager manager)
        {
            _manager = manager;
        }


        /// <summary>
        /// Returns all investigators and their IDs.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<InvestigatorStateDto>>> GetInvestigators()
        {
            var investigators = await _manager.GetAllInvestigatorStatesAsync();
            return Ok(investigators);
        }

        /// <summary>
        /// Starts a specific investigator.
        /// </summary>
        [HttpPost("{id}/start")]
        public async Task<IActionResult> Start(Guid id)
        {
            var success = await _manager.StartInvestigatorAsync(id);
            if (success)
                return Ok(new { message = "Investigator started successfully." });
            else
                return BadRequest(new { message = "Failed to start investigator. It may already be running or inactive." });
        }

        // Removed Stop endpoint - investigations are now one-shot operations

        /// <summary>
        /// Gets logged results for an investigator.
        /// </summary>
        [HttpGet("{id}/results")]
        public async Task<ActionResult<IEnumerable<InvestigatorResultDto>>> Results(Guid id, [FromQuery] int take = 100)
        {
            var results = await _manager.GetResultsAsync(id, take);
            return Ok(results);
        }

        /// <summary>
        /// Creates a new invoice investigator.
        /// Requires Admin role for access.
        /// </summary>
        [HttpPost("invoice")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<Guid>> CreateInvoice([FromBody] string? customName = null)
        {
            try
            {
                var id = await _manager.CreateInvestigatorAsync("invoice", customName);
                return Ok(new { id, message = "Invoice investigator created successfully." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Creates a new waybill investigator.
        /// Requires Admin role for access.
        /// </summary>
        [HttpPost("waybill")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<ActionResult<Guid>> CreateWaybill([FromBody] string? customName = null)
        {
            try
            {
                var id = await _manager.CreateInvestigatorAsync("waybill", customName);
                return Ok(new { id, message = "Waybill investigator created successfully." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Deletes a specific investigator and its related data.
        /// Requires Admin role for access.
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _manager.DeleteInvestigatorAsync(id);
            if (success)
                return Ok(new { message = "Investigator deleted successfully." });
            else
                return BadRequest(new { message = "Failed to delete investigator. It may not exist or be in use." });
        }
    }
}
