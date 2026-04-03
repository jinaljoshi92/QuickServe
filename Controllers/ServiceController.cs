using Microsoft.AspNetCore.Mvc;
using QuickServe.Data;
using QuickServe.Models;

namespace QuickServe.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServiceController : ControllerBase
    {
        private readonly QuickServeDbContext _context;

        public ServiceController(QuickServeDbContext context)
        {
            _context = context;
        }

        [HttpPost("create")]
        public IActionResult CreateRequest(int userId, string serviceType)
        {
            var wallet = _context.Wallet.FirstOrDefault(w => w.UserId == userId);

            if (wallet == null)
                return BadRequest("Wallet not found");

            if (wallet.Balance < 10)
                return BadRequest("Minimum ₹10 required to create service request");

            // Deduct amount
            wallet.Balance -= 10;

            // Create service request
            var request = new ServiceRequest
            {
                UserId = userId,
                ServiceType = serviceType,
                Status = "Pending",
                CreatedAt = DateTime.Now
            };

            _context.ServiceRequests.Add(request);

            // Add transaction
            var transaction = new Transaction
            {
                WalletId = wallet.WalletId,
                Amount = 10,
                Type = "Debit",
                Description = "Service Request Deduction",
                CreatedAt = DateTime.Now
            };

            _context.Transactions.Add(transaction);

            _context.SaveChanges();

            return Ok("Service request created successfully");
        }

        [HttpGet("my-requests/{userId}")]
        public IActionResult GetMyRequests(int userId)
        {
            var requests = _context.ServiceRequests
                .Where(r => r.UserId == userId)
                .ToList();

            return Ok(requests);
        }

        [HttpPost("create-with-ai")]
        public IActionResult CreateRequestWithAI(int userId, string userInput)
        {
            var serviceType = DetectServiceType(userInput);

            if (serviceType == "Unknown")
                return BadRequest("Please specify PAN, KYC, PF or Aadhaar related request");

            var wallet = _context.Wallet.FirstOrDefault(w => w.UserId == userId);

            if (wallet == null)
                return BadRequest("Wallet not found");

            if (wallet.Balance < 10)
                return BadRequest("Minimum ₹10 required");

            // Deduct
            wallet.Balance -= 10;

            // Create request
            var request = new ServiceRequest
            {
                UserId = userId,
                ServiceType = serviceType,
                Status = "Pending",
                CreatedAt = DateTime.Now
            };

            _context.ServiceRequests.Add(request);

            // Transaction
            var transaction = new Transaction
            {
                WalletId = wallet.WalletId,
                Amount = 10,
                Type = "Debit",
                Description = $"Service Request - {serviceType}",
                CreatedAt = DateTime.Now
            };

            _context.Transactions.Add(transaction);

            _context.SaveChanges();

            return Ok(new
            {
                message = "Service created using AI",
                detectedService = serviceType
            });
        }

        private string DetectServiceType(string userInput)
        {
            var input = userInput.ToLower();

            var rules = new Dictionary<string, string>
            {
                { "pan", "PAN" },
                { "kyc", "KYC" },
                { "aadhaar", "Aadhaar" },
                { "pf", "PF" }
            };

            var matchedService = rules
                .FirstOrDefault(rule => input.Contains(rule.Key))
                .Value;

            return matchedService ?? "Unknown";
        }
    }
}
