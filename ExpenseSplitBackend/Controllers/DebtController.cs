// Add using statements for your storage service and System.IO
using ExpenseSplitBackend.Services; // Assuming this is your service namespace
using ExpenseSplitBackend.Data;
using ExpenseSplitBackend.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ExpenseSplitBackend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class DebtController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        // 1. Inject your storage service
        private readonly IStorageService _storageService;
        private readonly IEmailService _emailService;

        // 2. Update constructor to receive the service
        public DebtController(ApplicationDbContext context, IStorageService storageService, IEmailService emailService)
        {
            _context = context;
            _storageService = storageService;
            _emailService = emailService;
        }

        [HttpGet("balance")]
        public async Task<IActionResult> GetNetBalance()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var totalOwedToYou = await _context.Debts
                .Include(d => d.Expense)
                .Where(d => d.Expense.PayerId == userId && d.DebtorId != userId && !d.IsSettled)
                .SumAsync(d => d.AmountOwed);

            var totalYouOwe = await _context.Debts
                .Where(d => d.DebtorId == userId && !d.IsSettled)
                .SumAsync(d => d.AmountOwed);

            var balance = new NetBalance(totalOwedToYou, totalYouOwe, totalOwedToYou - totalYouOwe);
            return Ok(balance);
        }

        [HttpGet("liabilities")] // Money you owe
        public async Task<IActionResult> GetLiabilities()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 3. First, fetch the raw data from the database
            var debtsData = await _context.Debts
                .Include(d => d.Expense).ThenInclude(e => e.Payer)
                .Include(d => d.Debtor) // Also include Debtor
                .Where(d => d.DebtorId == userId && !d.IsSettled)
                .Select(d => new { // Use an anonymous object to hold DB data
                    d.Id,
                    d.Expense.Description,
                    PayerFirstName = d.Expense.Payer.FirstName,
                    PayerEmail = d.Expense.Payer.Email,
                    DebtorFirstName = d.Debtor.FirstName,
                    DebtorEmail = d.Debtor.Email,
                    d.AmountOwed,
                    d.IsSettled,
                    OriginalQrCodeUrl = d.Expense.Payer.QRCodeS3Url // Get the stored URL
                })
                .ToListAsync();

            // 4. Now, process the list in-memory to generate presigned URLs
            var debts = debtsData.Select(d =>
            {
                string? presignedUrl = null;
                if (!string.IsNullOrEmpty(d.OriginalQrCodeUrl))
                {
                    // Use the same logic from your example
                    var fileName = Path.GetFileName(new Uri(d.OriginalQrCodeUrl).AbsolutePath);
                    var objectKey = $"qrcodes/{fileName}";
                    presignedUrl = _storageService.GetPreSignedUrl(objectKey, TimeSpan.FromMinutes(15));
                }

                // Create the final DTO with the presigned URL
                return new DebtSummary(
                    d.Id.ToString(),
                    d.Description,
                    d.PayerFirstName,
                    d.PayerEmail,
                    d.DebtorFirstName,
                    d.DebtorEmail,
                    d.AmountOwed,
                    d.IsSettled,
                    presignedUrl // Use the new presigned URL
                );
            }).ToList(); // Execute the in-memory .Select()

            return Ok(debts);
        }

        [HttpGet("outstanding")] // Money owed to you
        public async Task<IActionResult> GetOutstandingDebts()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var debts = await _context.Debts
                .Include(d => d.Expense).ThenInclude(e => e.Payer) // Added Payer include
                .Include(d => d.Debtor)
                .Where(d => d.Expense.PayerId == userId && d.DebtorId != userId && !d.IsSettled)
                 .Select(d => new DebtSummary(
                    d.Id.ToString(),
                    d.Expense.Description,
                    d.Expense.Payer.FirstName,
                    d.Expense.Payer.Email, // Added Payer email
                    d.Debtor.FirstName,
                    d.Debtor.Email,
                    d.AmountOwed,
                    d.IsSettled,
                    null
                 ))
                .ToListAsync();
            return Ok(debts);
        }

        [HttpPost("settle/{debtId}")]
        public async Task<IActionResult> SettleDebt(int debtId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var debt = await _context.Debts
                .Include(d => d.Expense)
                .FirstOrDefaultAsync(d => d.Id == debtId);

            if (debt == null) return NotFound();

            if (debt.Expense.PayerId != userId && debt.DebtorId != userId)
                return Forbid();

            debt.IsSettled = true;
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("send-debt-email/{debtId}")]
        public async Task<IActionResult> SendDebtEmail(int debtId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var debt = await _context.Debts
                .Include(d => d.Expense).ThenInclude(e => e.Payer)
                .Include(d => d.Debtor)
                .FirstOrDefaultAsync(d => d.Id == debtId);

            if (debt == null) return NotFound();

            // Only allow sending to the debtor
            if (debt.Expense.Payer.Id != userId)
                return Forbid();

            var toEmail = debt.Debtor.Email;
            var subject = $"Debt Details for Expense: {debt.Expense.Description}";
            var body = $@"
                <h2>Debt Details</h2>
                <p><strong>Description:</strong> {debt.Expense.Description}</p>
                <p><strong>Amount Owed:</strong> {debt.AmountOwed:C}</p>
                <p><strong>Payer:</strong> {debt.Expense.Payer.FirstName} ({debt.Expense.Payer.Email})</p>
                <p>Please find the QR code attached for payment.</p>
            ";

            // Download QR code image from S3 (using presigned URL)
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

            return Ok(new { message = "Email sent successfully." });
        }
    }
}