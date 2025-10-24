namespace ExpenseSplitBackend.Models.DTOs
{
    // Auth
    public class RegisterModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public RegisterModel() { }
        public RegisterModel(string firstName, string lastName, string email, string password)
        {
            FirstName = firstName;
            LastName = lastName;
            Email = email;
            Password = password;
        }
    }

    public class LoginModel
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public LoginModel() { }
        public LoginModel(string email, string password)
        {
            Email = email;
            Password = password;
        }
    }

    public class AuthResponse
    {
        public string Token { get; set; }
        public UserProfile User { get; set; }
        public AuthResponse() { }
        public AuthResponse(string token, UserProfile user)
        {
            Token = token;
            User = user;
        }
    }

    public class UserProfile
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string? QRCodeS3Url { get; set; }
        public UserProfile() { }
        public UserProfile(string id, string firstName, string lastName, string email, string? qrCodeS3Url)
        {
            Id = id;
            FirstName = firstName;
            LastName = lastName;
            Email = email;
            QRCodeS3Url = qrCodeS3Url;
        }
    }

    // Expenses
    public enum SplitMethod { Equal, Exact, Percentage }

    public class SplitParticipant
    {
        public string UserId { get; set; }
        public decimal? Amount { get; set; }
        public decimal? Percentage { get; set; }
        public SplitParticipant() { }
        public SplitParticipant(string userId, decimal? amount, decimal? percentage)
        {
            UserId = userId;
            Amount = amount;
            Percentage = percentage;
        }
    }

    public class CreateExpenseModel
    {
        public string Description { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime Date { get; set; }
        public SplitMethod SplitType { get; set; }
        public List<SplitParticipant> Participants { get; set; }
        public CreateExpenseModel() { }
        public CreateExpenseModel(string description, decimal totalAmount, DateTime date, SplitMethod splitType, List<SplitParticipant> participants)
        {
            Description = description;
            TotalAmount = totalAmount;
            Date = date;
            SplitType = splitType;
            Participants = participants;
        }
    }

    // Debts
    public class DebtSummary
    {
        public string Id { get; set; }
        public string Description { get; set; }
        public string OwedToName { get; set; }
        public string OwedToEmail { get; set; }
        public string OwedByName { get; set; }
        public string OwedByEmail { get; set; }
        public decimal Amount { get; set; }
        public bool IsSettled { get; set; }
        public string? QRCodeS3Url { get; set; }
        public DebtSummary() { }
        public DebtSummary(string id, string description, string owedToName, string owedToEmail, string owedByName, string owedByEmail, decimal amount, bool isSettled, string? qrCodeS3Url)
        {
            Id = id;
            Description = description;
            OwedToName = owedToName;
            OwedToEmail = owedToEmail;
            OwedByName = owedByName;
            OwedByEmail = owedByEmail;
            Amount = amount;
            IsSettled = isSettled;
            QRCodeS3Url = qrCodeS3Url;
        }
    }

    public class NetBalance
    {
        public decimal TotalOwedToYou { get; set; }
        public decimal TotalYouOwe { get; set; }
        public decimal Net { get; set; }
        public NetBalance() { }
        public NetBalance(decimal totalOwedToYou, decimal totalYouOwe, decimal net)
        {
            TotalOwedToYou = totalOwedToYou;
            TotalYouOwe = totalYouOwe;
            Net = net;
        }
    }
}