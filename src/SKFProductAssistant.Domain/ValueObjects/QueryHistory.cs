namespace SKFProductAssistant.Domain.ValueObjects
{
    public class QueryHistory : ValueObject
    {
        public string Query { get; private set; }
        public string? Response { get; private set; }
        public DateTime Timestamp { get; private set; }

        private QueryHistory() { } // For EF Core

        public QueryHistory(string query, string? response, DateTime timestamp)
        {
            Query = query ?? throw new ArgumentNullException(nameof(query));
            Response = response;
            Timestamp = timestamp;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Query;
            yield return Response ?? string.Empty;
            yield return Timestamp;
        }
    }
}