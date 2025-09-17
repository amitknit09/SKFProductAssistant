namespace SKFProductAssistant.Domain.ValueObjects
{
    public class ConversationId : ValueObject
    {
        public string Value { get; private set; }

        private ConversationId() { } // For EF Core

        public ConversationId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Conversation ID cannot be empty", nameof(value));

            Value = value;
        }

        public static ConversationId NewId() => new(Guid.NewGuid().ToString());

        public static implicit operator string(ConversationId id) => id.Value;
        public static implicit operator ConversationId(string value) => new(value);

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
        }
    }
}