using Microsoft.AspNetCore.Mvc;
using ea_Tracker.Services;

namespace ea_Tracker.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InvestigationsController : ControllerBase
    {
        private readonly InvestigationManager _manager;

        public InvestigationsController(InvestigationManager manager)
        {
            _manager = manager;
        }

        [HttpPost("start")]
        public IActionResult StartInvestigations()
        {
            _manager.StartAll();
            return Ok(" Investigators started.");
        }

        [HttpPost("stop")]
        public IActionResult StopInvestigations()
        {
            _manager.StopAll();
            return Ok(" Investigators stopped.");
        }
    }
}
