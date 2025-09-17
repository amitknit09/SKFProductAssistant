using SKFProductAssistant.Domain.ValueObjects;

namespace SKFProductAssistant.Domain.Entities
{
    public class Conversation
    {
        public ConversationId Id { get; private set; }
        public List<QueryHistory> QueryHistory { get; private set; }
        public ProductName? LastProductDiscussed { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime LastActivity { get; private set; }
        public Dictionary<string, string> SessionData { get; private set; }

        private Conversation() { } // For EF Core

        public Conversation(ConversationId id)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            QueryHistory = new List<QueryHistory>();
            SessionData = new Dictionary<string, string>();
            CreatedAt = DateTime.UtcNow;
            LastActivity = DateTime.UtcNow;
        }

        public void AddQuery(string query, string? response = null)
        {
            QueryHistory.Add(new QueryHistory(query, response, DateTime.UtcNow));
            LastActivity = DateTime.UtcNow;

            // Keep only last 10 queries
            if (QueryHistory.Count > 10)
            {
                QueryHistory = QueryHistory.TakeLast(10).ToList();
            }
        }

        public void SetLastProductDiscussed(ProductName productName)
        {
            LastProductDiscussed = productName;
            LastActivity = DateTime.UtcNow;
        }

        public bool IsExpired(TimeSpan timeout)
        {
            return DateTime.UtcNow - LastActivity > timeout;
        }

        public List<string> GetRecentQueries(int count = 3)
        {
            return QueryHistory.TakeLast(count).Select(q => q.Query).ToList();
        }
    }
}