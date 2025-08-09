using Microsoft.AspNetCore.Mvc;
using ea_Tracker.Services;

namespace ea_Tracker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CompletedInvestigationsController : ControllerBase
    {
        private readonly IInvestigationManager _manager;

        public CompletedInvestigationsController(IInvestigationManager manager)
        {
            _manager = manager;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCompleted()
        {
            var results = await _manager.GetAllCompletedInvestigationsAsync();
            return Ok(results);
        }

        /// <summary>
        /// Gets detailed information for a specific completed investigation execution.
        /// </summary>
        [HttpGet("{executionId}")]
        public async Task<IActionResult> GetDetails(int executionId)
        {
            var detail = await _manager.GetInvestigationDetailsAsync(executionId);
            if (detail == null)
            {
                return NotFound(new { message = $"Investigation execution {executionId} not found" });
            }
            return Ok(detail);
        }
    }
}