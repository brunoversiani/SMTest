using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SMTest.Domain.Entities
{
    public class ShortUrl
    {
        [Key]
        public string ShortCode { get; set; }

        [Required]
        public string LongUrl { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; } // Foreign key to User

        public int HitCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public byte[] RowVersion { get; set; }
    }

}
