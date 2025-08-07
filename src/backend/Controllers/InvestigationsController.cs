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
        /// Starts all registered investigators.
        /// </summary>
        /// <returns>A success response when investigators have started.</returns>
        [HttpPost("start")]
        public async Task<IActionResult> StartInvestigations()
        {
            await _manager.StartAllAsync();
            return Ok("Investigators started.");
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
        /// Stops a specific investigator.
        /// </summary>
        [HttpPost("{id}/stop")]
        public async Task<IActionResult> Stop(Guid id)
        {
            var success = await _manager.StopInvestigatorAsync(id);
            if (success)
                return Ok(new { message = "Investigator stopped successfully." });
            else
                return BadRequest(new { message = "Failed to stop investigator. It may not be running." });
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
        /// Stops all running investigators.
        /// </summary>
        /// <returns>A success response when investigators have stopped.</returns>
        [HttpPost("stop")]
        public async Task<IActionResult> StopInvestigations()
        {
            await _manager.StopAllAsync();
            return Ok("Investigators stopped.");
        }
        /// <summary>
        /// Creates a new invoice investigator.
        /// </summary>
        [HttpPost("invoice")]
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
        /// </summary>
        [HttpPost("waybill")]
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
    }
}
