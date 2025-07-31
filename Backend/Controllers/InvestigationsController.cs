using Microsoft.AspNetCore.Mvc;
using ea_Tracker.Services;
using System;

namespace ea_Tracker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    /// <summary>
    /// API endpoints for controlling investigator services.
    /// </summary>
    public class InvestigationsController : ControllerBase
    {
        private readonly InvestigationManager _manager;

        /// <summary>
        /// Initializes a new instance of the <see cref="InvestigationsController"/> class.
        /// </summary>
        /// <param name="manager">The investigation manager.</param>
        public InvestigationsController(InvestigationManager manager)
        {
            _manager = manager;
        }

        /// <summary>
        /// Starts all registered investigators.
        /// </summary>
        /// <returns>A success response when investigators have started.</returns>
        [HttpPost("start")]
        public IActionResult StartInvestigations()
        {
            _manager.StartAll();
            return Ok(" Investigators started.");
        }

        /// <summary>
        /// Returns all investigators and their IDs.
        /// </summary>
        [HttpGet]
        public IActionResult GetInvestigators()
        {
            return Ok(_manager.GetAllInvestigatorStates());
        }

        /// <summary>
        /// Starts a specific investigator.
        /// </summary>
        [HttpPost("{id}/start")]
        public IActionResult Start(Guid id)
        {
            _manager.StartInvestigator(id);
            return Ok();
        }

        /// <summary>
        /// Stops a specific investigator.
        /// </summary>
        [HttpPost("{id}/stop")]
        public IActionResult Stop(Guid id)
        {
            _manager.StopInvestigator(id);
            return Ok();
        }

        /// <summary>
        /// Gets logged results for an investigator.
        /// </summary>
        [HttpGet("{id}/results")]
        public IActionResult Results(Guid id)
        {
            return Ok(_manager.GetResults(id));
        }

        /// <summary>
        /// Stops all running investigators.
        /// </summary>
        /// <returns>A success response when investigators have stopped.</returns>
        [HttpPost("stop")]
        public IActionResult StopInvestigations()
        {
            _manager.StopAll();
            return Ok(" Investigators stopped.");
        }
    }
}
