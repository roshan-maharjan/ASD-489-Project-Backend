namespace ExpenseSplitBackend.Models
{
    public class UserProfileDto
    {
        public string Id { get; set; } = string.Empty; // Changed from int
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}