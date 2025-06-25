using System.ComponentModel.DataAnnotations;

namespace SMTest.Domain.ValueObjects
{
    public class DailyLimit
    {
        [Key]
        public string UserId { get; set; }
        public int Count { get; set; }
        public DateTime ResetDate { get; set; }
    }

}
