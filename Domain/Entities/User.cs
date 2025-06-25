using Microsoft.AspNetCore.Identity;

namespace SMTest.Domain.Entities
{
    public class User : IdentityUser
    {       
        public ICollection<ShortUrl> ShortUrls { get; set; }
    }

}
