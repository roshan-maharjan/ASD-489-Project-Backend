using ExpenseSplitBackend.Data;
using ExpenseSplitBackend.Models;
using ExpenseSplitBackend.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

public class DebtEmailService
{
    private readonly ApplicationDbContext _context;
    private readonly IStorageService _storageService;
    private readonly IEmailService _emailService;

    public DebtEmailService(ApplicationDbContext context, IStorageService storageService, IEmailService emailService)
    {
        _context = context;
        _storageService = storageService;
        _emailService = emailService;
    }

    public async Task SendDebtEmailAsync(int debtId, string payerId)
    {
        var debt = await _context.Debts
            .Include(d => d.Expense).ThenInclude(e => e.Payer)
            .Include(d => d.Debtor)
            .FirstOrDefaultAsync(d => d.Id == debtId);

        if (debt == null) throw new ArgumentException("Debt not found.");
        if (debt.Expense.Payer.Id != payerId) throw new UnauthorizedAccessException();

        var toEmail = debt.Debtor.Email;
        var subject = $"Debt Details for Expense: {debt.Expense.Description}";
        var body = $@"
            <h2>Debt Details</h2>
            <p><strong>Description:</strong> {debt.Expense.Description}</p>
            <p><strong>Amount Owed:</strong> {debt.AmountOwed:C}</p>
            <p><strong>Payer:</strong> {debt.Expense.Payer.FirstName} ({debt.Expense.Payer.Email})</p>
            <p>Please find the QR code attached for payment.</p>
        ";

        byte[] qrImageBytes = null;
        string qrImageName = "qr.png";
        var qrUrl = debt.Expense.Payer.QRCodeS3Url;
        if (!string.IsNullOrEmpty(qrUrl))
        {
            var fileName = Path.GetFileName(new Uri(qrUrl).AbsolutePath);
            var objectKey = $"qrcodes/{fileName}";
            var presignedUrl = _storageService.GetPreSignedUrl(objectKey, TimeSpan.FromMinutes(15));
            using var httpClient = new HttpClient();
            qrImageBytes = await httpClient.GetByteArrayAsync(presignedUrl);
        }

        await _emailService.SendDebtEmailAsync(toEmail, subject, body, qrImageBytes, qrImageName);
    }
}