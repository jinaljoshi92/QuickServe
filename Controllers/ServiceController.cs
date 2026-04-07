using Microsoft.AspNetCore.Mvc;
using QuickServe.Data;
using QuickServe.Models;
using QuickServe.Services;
using System.Net.Http.Headers;
using System.Text;
using static System.Net.WebRequestMethods;
using System.Text.Json;
using System.Linq;

namespace QuickServe.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServiceController : ControllerBase
    {
        private readonly QuickServeDbContext _context;
        private readonly AiAgentService _ai;
        private readonly IConfiguration _config;
        private readonly ChatMemoryService _memory;

        public ServiceController(QuickServeDbContext context, AiAgentService ai, IConfiguration config, ChatMemoryService memory)
        {
            _context = context;
            _ai = ai;
            _memory = memory;
            _config = config;
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

        private async Task<AgentResponse> ProcessUserIntentLLM(string userInput)
        {
            var apiKey = _config["OpenAI:ApiKey"];

            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);

            var prompt = $@"
You are an AI agent for a service platform.

User input: '{userInput}'

Decide:

If user wants:
- balance → QueryType = BALANCE
- transactions → QueryType = TRANSACTION
- services → QueryType = SERVICE
- otherwise → CREATE_REQUEST

Return JSON:
{{
  ""service"": ""..."",
  ""action"": ""CREATE_REQUEST / ASK_CLARIFICATION / QUERY"",
  ""queryType"": ""BALANCE / TRANSACTION / SERVICE"",
  ""confidence"": ""High/Low"",
  ""message"": ""...""
}}
";

            var requestBody = new
            {
                model = "gpt-4o-mini",
                messages = new[]
                {
            new { role = "user", content = prompt }
        }
            };

            var json = JsonSerializer.Serialize(requestBody);

            var response = await client.PostAsync(
                "https://api.openai.com/v1/chat/completions",
                new StringContent(json, Encoding.UTF8, "application/json")
            );

            var result = await response.Content.ReadAsStringAsync();

            // 👉 For demo: return raw or parse later
            return new AgentResponse
            {
                Service = "LLM_RESPONSE",
                Action = "PARSE_REQUIRED",
                Confidence = "Unknown",
                Message = result
            };
        }

        [HttpPost("agent-create")]
        public async Task<IActionResult> AgentCreate([FromBody] ChatRequest request)
        {
            var agent = await _ai.ProcessUserIntent(request.UserInput);
            var userId = request.UserId;
            var userInput = request.UserInput;

            // 🔁 Clarificationx`x`
            if (agent.Action == "ASK_CLARIFICATION")
            {
                return Ok(agent);
            }

            // 🔍 QUERY → RAG + MEMORY
            if (agent.Action == "QUERY")
            {
                var wallet = _context.Wallet.FirstOrDefault(w => w.UserId == userId);

                if (wallet == null)
                    return BadRequest("Wallet not found");

                var history = _memory.GetMessages(userId);

                // 🔹 BALANCE
                if (agent.QueryType == "BALANCE")
                {
                    var contextData = $"User balance is ₹{wallet.Balance}";

                    var answer = await _ai.GenerateWithMemory(userId, userInput, contextData, history);

                    _memory.AddMessage(userId, "user", userInput);
                    _memory.AddMessage(userId, "assistant", answer);

                    return Ok(new { response = answer });
                }

                // 🔹 TRANSACTION
                if (agent.QueryType == "TRANSACTION")
                {
                    var transactions = _context.Transactions
                        .Where(t => t.WalletId == wallet.WalletId)
                        .OrderByDescending(t => t.CreatedAt)
                        .Take(5)
                        .ToList();

                    var contextData = transactions.Any()
                        ? string.Join(", ", transactions.Select(t => $"{t.Type} ₹{t.Amount} on {t.CreatedAt:dd MMM}"))
                        : "No transactions found.";

                    var answer = await _ai.GenerateWithMemory(userId, userInput, contextData, history);

                    _memory.AddMessage(userId, "user", userInput);
                    _memory.AddMessage(userId, "assistant", answer);

                    return Ok(new { response = answer });
                }

                // 🔹 SERVICE
                if (agent.QueryType == "SERVICE")
                {
                    var services = _context.ServiceRequests
                        .Where(s => s.UserId == userId)
                        .OrderByDescending(s => s.CreatedAt)
                        .Take(5)
                        .ToList();

                    var contextData = services.Any()
                        ? string.Join(", ", services.Select(s => $"{s.ServiceType} ({s.Status})"))
                        : "No service requests found.";

                    var answer = await _ai.GenerateWithMemory(userId, userInput, contextData, history);

                    _memory.AddMessage(userId, "user", userInput);
                    _memory.AddMessage(userId, "assistant", answer);

                    return Ok(new { response = answer });
                }
            }

            // 🧠 CREATE REQUEST FLOW
            if (agent.Action == "CREATE_REQUEST")
            {
                var wallet = _context.Wallet.FirstOrDefault(w => w.UserId == userId);

                if (wallet == null)
                    return BadRequest("Wallet not found");

                if (wallet.Balance < 10)
                    return BadRequest("Minimum ₹10 required");

                wallet.Balance -= 10;

                var serviceRequest = new ServiceRequest
                {
                    UserId = userId,
                    ServiceType = agent.Service,
                    Status = "Pending",
                    CreatedAt = DateTime.Now
                };

                _context.ServiceRequests.Add(serviceRequest);

                var transaction = new Transaction
                {
                    WalletId = wallet.WalletId,
                    Amount = 10,
                    Type = "Debit",
                    Description = $"Service Request - {agent.Service}",
                    CreatedAt = DateTime.Now
                };

                _context.Transactions.Add(transaction);

                _context.SaveChanges();

                return Ok(new
                {
                    message = agent.Message,
                    service = agent.Service,
                    status = "Request Created"
                });
            }

            // ❌ fallback
            return BadRequest("Unable to process request");
        }
    }
}
