namespace SMTest.Application.DTOs
{
    public class ShortUrlResponse
    {
        public string ShortCode { get; set; }
        public string LongUrl { get; set; }
        public int HitCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool IsActive => ExpiresAt == null || ExpiresAt > DateTime.UtcNow;
    }

}
