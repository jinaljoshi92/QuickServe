namespace QuickServe.Models
{
    public class AgentResponse
    {
        public string Service { get; set; }
        public string Action { get; set; } // CREATE_REQUEST / ASK_CLARIFICATION
        public string Confidence { get; set; }
        public string Message { get; set; }
        public string QueryType { get; set; } // BALANCE / TRANSACTION / SERVICE
    }

    public class ChatMessage
    {
        public string Role { get; set; } // user / assistant
        public string Content { get; set; }
    }

    public class ChatRequest
    {
        public int UserId { get; set; }
        public string UserInput { get; set; }
    }
}
