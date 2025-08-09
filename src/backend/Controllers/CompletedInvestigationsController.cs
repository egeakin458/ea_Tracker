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
    }
}