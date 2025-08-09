using Microsoft.AspNetCore.Mvc;
using ea_Tracker.Services;
using System;
using System.Collections.Generic;
using ea_Tracker.Models.Dtos;

namespace ea_Tracker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
        /// Creates a new investigator of the given type. Type must be one of: invoice, waybill.
        /// New unified endpoint to reduce duplication. Old endpoints remain for compatibility.
        /// </summary>
        [HttpPost("create/{type}")]
        public async Task<ActionResult<Guid>> Create(string type, [FromBody] string? customName = null)
        {
            var normalized = type.Trim().ToLowerInvariant();
            if (normalized != "invoice" && normalized != "waybill")
            {
                return BadRequest(new { message = $"Unsupported investigator type: {type}" });
            }

            var id = await _manager.CreateInvestigatorAsync(normalized, customName);
            return Ok(new { id, message = $"{char.ToUpper(normalized[0]) + normalized.Substring(1)} investigator created successfully." });
        }

        /// <summary>
        /// Back-compat endpoint for creating an invoice investigator.
        /// </summary>
        [HttpPost("invoice")]
        public Task<ActionResult<Guid>> CreateInvoice([FromBody] string? customName = null)
            => Create("invoice", customName);

        /// <summary>
        /// Back-compat endpoint for creating a waybill investigator.
        /// </summary>
        [HttpPost("waybill")]
        public Task<ActionResult<Guid>> CreateWaybill([FromBody] string? customName = null)
            => Create("waybill", customName);

        /// <summary>
        /// Deletes a specific investigator and its related data.
        /// </summary>
        [HttpDelete("{id}")]
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
