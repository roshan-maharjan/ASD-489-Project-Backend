using System.ComponentModel.DataAnnotations;

namespace ExpenseSplitLambdaBackend.Services.AuthService.Dtos
{
    public class UserInput
    {
        [Required, EmailAddress]
        public string Email { get; set; }

        [Required, MinLength(8)]
        public string Password { get; set; }
    }

    public class UserRecord
    {
        // DynamoDB Partition Key
        public string Email { get; set; }

        // Hashed Password
        public string PasswordHash { get; set; }

        // Other attributes, e.g., DisplayName, CreatedDate
        public string UserId { get; set; } = Guid.NewGuid().ToString();
    }
}
