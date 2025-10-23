using Amazon;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using MimeKit;

namespace ExpenseSplitBackend.Services
{
    public interface IEmailService
    {
        Task SendDebtEmailAsync(string toEmail, string subject, string body, byte[] qrImageBytes, string qrImageName);
    }

    public class EmailService : IEmailService
    {
        private readonly RegionEndpoint _region = RegionEndpoint.USEast1;

        public async Task SendDebtEmailAsync(string toEmail, string subject, string body, byte[] qrImageBytes, string qrImageName)
        {
            using var client = new AmazonSimpleEmailServiceClient(_region);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Expense Split", "roshan.maharjan47@gmail.com"));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;

            var builder = new BodyBuilder();

            // Add QR image as attachment only
            if (qrImageBytes != null && qrImageBytes.Length > 0)
            {
                builder.Attachments.Add(qrImageName, qrImageBytes);
            }

            builder.HtmlBody = body; // No <img> tag

            message.Body = builder.ToMessageBody();

            using var ms = new MemoryStream();
            message.WriteTo(ms);

            var sendRequest = new SendRawEmailRequest
            {
                RawMessage = new RawMessage(ms)
            };

            await client.SendRawEmailAsync(sendRequest);
        }
    }
}