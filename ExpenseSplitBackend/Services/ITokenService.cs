using ExpenseSplitBackend.Models;
using System.Security.Claims;

namespace ExpenseSplitBackend.Services
{
    public interface ITokenService
    {
        string CreateToken(ApplicationUser user);
    }
}
