using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace ExpenseSplitLambdaBackend.Services.AuthService
{
    public static class JwtHelper
    {
        // WARNING: Get this secret from a secure source like AWS Secrets Manager
        private const string JWT_SECRET = "8Xj7p2wFkZt1yM5GvHc9bRsD4qN6pLzW";
        private const string JWT_ISSUER = "ExpenseSplitAPI";

        public static string GenerateToken(string email, string userId)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JWT_SECRET));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim("uid", userId) // Custom claim
        };

            var token = new JwtSecurityToken(
                issuer: JWT_ISSUER,
                audience: "YourBlazorClient", // Should match your client app
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1), // Token lifetime
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
