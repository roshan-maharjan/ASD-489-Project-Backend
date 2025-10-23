namespace ExpenseSplitBackend.Models.DTOs
{
    // Auth
    public record RegisterModel(string FirstName, string LastName, string Email, string Password);
    public record LoginModel(string Email, string Password);
    public record AuthResponse(string Token, UserProfile User);
    public record UserProfile(string Id, string FirstName, string LastName, string Email, string? QRCodeS3Url);

    // Expenses
    public enum SplitMethod { Equal, Exact, Percentage }

    public record SplitParticipant(string UserId, decimal? Amount, decimal? Percentage);

    public record CreateExpenseModel(
        string Description,
        decimal TotalAmount,
        DateTime Date,
        SplitMethod SplitType,
        List<SplitParticipant> Participants
    );

    // Debts
    public record DebtSummary(string Id, string Description, string OwedToName, string OwedToEmail, string OwedByName, string OwedByEmail, decimal Amount, bool IsSettled, string? QRCodeS3Url);
    public record NetBalance(decimal TotalOwedToYou, decimal TotalYouOwe, decimal Net);
}