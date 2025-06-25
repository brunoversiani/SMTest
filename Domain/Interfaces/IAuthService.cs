using SMTest.Domain.Entities;

namespace SMTest.Domain.Interfaces
{
    public interface IAuthService
    {
        string CreateToken(User user);
        Guid? ValidateToken(string token);
    }

}
