using QuickServe.Models;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;

namespace QuickServe.Services
{
    public class AiAgentService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _http;

        public AiAgentService(IConfiguration config)
        {
            _config = config;
            _http = new HttpClient();
        }

        public async Task<AgentResponse> ProcessUserIntent(string userInput)
        {
            try
            {
                return await CallLLM(userInput);
            }
            catch
            {
                // 🔁 Fallback to rule-based
                return RuleBasedFallback(userInput);
            }
        }

        private async Task<AgentResponse> CallLLM(string userInput)
        {
            var apiKey = _config["OpenAI:ApiKey"];

            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);

            var prompt = $@"
You are an AI agent for a service platform.

User input: '{userInput}'

Available services: PAN, KYC, Aadhaar

Return JSON ONLY:
{{
  ""service"": ""PAN/KYC/Aadhaar/Unknown"",
  ""action"": ""CREATE_REQUEST or ASK_CLARIFICATION"",
  ""confidence"": ""High/Low"",
  ""message"": ""short user-friendly message""
}}
";

            var body = new
            {
                model = "gpt-4o-mini",
                messages = new[]
                {
                new { role = "user", content = prompt }
            }
            };

            var content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _http.PostAsync(
                "https://api.openai.com/v1/chat/completions",
                content
            );

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadAsStringAsync();

            var doc = JsonDocument.Parse(result);

            var message = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return JsonSerializer.Deserialize<AgentResponse>(message);
        }

        private AgentResponse RuleBasedFallback(string input)
        {
            var text = input.ToLower();

            // 🔥 ADD THIS (IMPORTANT)
            if (text.Contains("balance"))
            {
                return new AgentResponse
                {
                    Action = "QUERY",
                    QueryType = "BALANCE",
                    Confidence = "High",
                    Message = "Fetching your balance"
                };
            }

            if (text.Contains("transaction"))
            {
                return new AgentResponse
                {
                    Action = "QUERY",
                    QueryType = "TRANSACTION",
                    Confidence = "High",
                    Message = "Fetching your transactions"
                };
            }

            if (text.Contains("service"))
            {
                return new AgentResponse
                {
                    Action = "QUERY",
                    QueryType = "SERVICE",
                    Confidence = "High",
                    Message = "Fetching your services"
                };
            }

            // Existing logic
            if (text.Contains("pan"))
            {
                return new AgentResponse
                {
                    Service = "PAN",
                    Action = "CREATE_REQUEST",
                    Confidence = "High",
                    Message = "Creating your PAN request"
                };
            }

            return new AgentResponse
            {
                Service = "Unknown",
                Action = "ASK_CLARIFICATION",
                Confidence = "Low",
                Message = "Please specify PAN, KYC, or Aadhaar service"
            };
        }

        private string GenerateFallbackResponse(string question, string data)
        {
            // Simple fallback (no AI)
            if (question.ToLower().Contains("balance"))
            {
                return data; // already contains balance
            }

            if (question.ToLower().Contains("transaction"))
            {
                return "Here are your transactions: " + data;
            }

            if (question.ToLower().Contains("service"))
            {
                return "Your services: " + data;
            }

            return "Here is the information: " + data;
        }

        public async Task<string> GenerateWithMemory(int userId, string userInput, string data, List<ChatMessage> history)
        {
            var apiKey = _config["OpenAI:ApiKey"];

            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);

            var messages = new List<object>();

            // 🔥 Add system instruction
            messages.Add(new
            {
                role = "system",
                content = "You are a helpful assistant for a service platform."
            });

            // 🔥 Add history
            foreach (var msg in history)
            {
                messages.Add(new
                {
                    role = msg.Role,
                    content = msg.Content
                });
            }

            // 🔥 Add context data (RAG)
            messages.Add(new
            {
                role = "system",
                content = $"Context Data: {data}"
            });

            // 🔥 Add user message
            messages.Add(new
            {
                role = "user",
                content = userInput
            });

            var body = new
            {
                model = "gpt-4o-mini",
                messages = messages
            };

            try
            {
                var response = await _http.PostAsync(
                    "https://api.openai.com/v1/chat/completions",
                    new StringContent(JsonSerializer.Serialize(body),
                    Encoding.UTF8,
                    "application/json")
                );

                var result = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(result);

                if (doc.RootElement.TryGetProperty("error", out _))
                {
                    return GenerateFallbackResponse(userInput, data);
                }

                return doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();
            }
            catch
            {
                return GenerateFallbackResponse(userInput, data);
            }
        }
    }
}
