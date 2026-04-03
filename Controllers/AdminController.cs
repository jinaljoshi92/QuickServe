using Microsoft.AspNetCore.Mvc;
using QuickServe.Data;

namespace QuickServe.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly QuickServeDbContext _context;

        public AdminController(QuickServeDbContext context)
        {
            _context = context;
        }

        // GET ALL REQUESTS
        [HttpGet("requests")]
        public IActionResult GetAllRequests()
        {
            var requests = _context.ServiceRequests.ToList();
            return Ok(requests);
        }

        // UPDATE STATUS
        [HttpPut("update-status")]
        public IActionResult UpdateStatus(int requestId, string status)
        {
            var request = _context.ServiceRequests.FirstOrDefault(r => r.RequestId == requestId);

            if (request == null)
                return NotFound("Request not found");

            request.Status = status;

            _context.SaveChanges();

            return Ok("Status updated");
        }
    }
}
