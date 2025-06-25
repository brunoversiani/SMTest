namespace SMTest.Domain.Entities
{
    public class RateLimitRecord
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ShortCode { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
