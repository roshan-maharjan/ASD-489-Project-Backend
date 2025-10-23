using ExpenseSplitBackend.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ExpenseSplitBackend.Services
{
    public class TokenService : ITokenService
    {
        private readonly SymmetricSecurityKey _key;
        private readonly string _issuer;
        private readonly string _audience;

        public TokenService(IConfiguration config)
        {
            // Read JWT settings from appsettings.json
            _issuer = config["Jwt:Issuer"]
                ?? throw new ArgumentNullException("Jwt:Issuer not found.");
            _audience = config["Jwt:Audience"]
                ?? throw new ArgumentNullException("Jwt:Audience not found.");
            var keyString = config["Jwt:Key"]
                ?? throw new ArgumentNullException("Jwt:Key not found.");

            _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
        }

        public string CreateToken(ApplicationUser user)
        {
            // 1. Create the claims for the token
            var claims = new List<Claim>
            {
                // Standard claims for user ID and email
                new Claim(JwtRegisteredClaimNames.NameId, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                
                // Standard claim for first name
                new Claim(JwtRegisteredClaimNames.GivenName, user.FirstName),

                // Standard claim for last name
                new Claim(JwtRegisteredClaimNames.FamilyName, user.LastName) // Add this line
            };

            // 2. Create signing credentials
            var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256Signature);

            // 3. Create the token descriptor
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddDays(7), // Set token expiration (e.g., 7 days)
                Issuer = _issuer,
                Audience = _audience,
                SigningCredentials = creds
            };

            // 4. Create and write the token
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}