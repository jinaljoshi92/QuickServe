using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

namespace QuickServe.Controllers
{
    

    [Route("api/[controller]")]
    [ApiController]
    public class AIController : ControllerBase
    {
        private readonly IConfiguration _config;

        public AIController(IConfiguration config)
        {
            _config = config;
        }

        //[HttpPost("suggest-service")]
        //public async Task<IActionResult> SuggestService([FromBody] string userInput)
        //{
        //    var apiKey = _config["OpenAI:ApiKey"];

        //    var client = new HttpClient();
        //    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        //    var prompt = $"User request: '{userInput}'. Suggest the best service type (PAN, KYC, Aadhaar) and return only the service name.";

        //    var requestBody = new
        //    {
        //        model = "gpt-4o-mini",
        //        messages = new[]
        //        {
        //        new { role = "user", content = prompt }
        //    }
        //    };

        //    var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

        //    var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);
        //    var responseString = await response.Content.ReadAsStringAsync();

        //    return Ok(responseString);
        //}

        [HttpPost("suggest-service")]
        public IActionResult SuggestService([FromBody] string userInput)
        {
            var input = userInput.ToLower();

            var rules = new Dictionary<string, string>
            {
                { "pan", "PAN" },
                { "kyc", "KYC" },
                { "aadhaar", "Aadhaar" },
                { "aadhar", "Aadhaar" }
            };

            var matchedService = rules
                .FirstOrDefault(rule => input.Contains(rule.Key))
                .Value;

            return Ok(new
            {
                input = userInput,
                suggestedService = matchedService ?? "Unknown"
            });
        }
    }
}
