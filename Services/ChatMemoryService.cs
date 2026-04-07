using QuickServe.Models;

namespace QuickServe.Services
{
    public class ChatMemoryService
    {
        private static Dictionary<int, List<ChatMessage>> _memory
        = new Dictionary<int, List<ChatMessage>>();

        public List<ChatMessage> GetMessages(int userId)
        {
            if (!_memory.ContainsKey(userId))
                _memory[userId] = new List<ChatMessage>();

            return _memory[userId];
        }

        public void AddMessage(int userId, string role, string content)
        {
            var messages = GetMessages(userId);

            messages.Add(new ChatMessage
            {
                Role = role,
                Content = content
            });

            // 🔥 Keep last 10 messages only
            if (messages.Count > 10)
                messages.RemoveAt(0);
        }
    }
}
