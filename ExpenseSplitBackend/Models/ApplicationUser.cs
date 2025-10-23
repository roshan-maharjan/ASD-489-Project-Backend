using Microsoft.AspNetCore.Identity;

namespace ExpenseSplitBackend.Models
{
    // You can add profile data for the user by adding properties to this class
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? QRCodeS3Url { get; set; }
    }
}