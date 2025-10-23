using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using ExpenseSplitBackend.Models;

namespace ExpenseSplitBackend.Services
{
    public interface INotificationService
    {
        Task SendNewExpenseNotification(Expense expense);
        Task SendPaymentReminder(Debt debt);
    }

    public class NotificationService : INotificationService
    {
        private readonly IAmazonSimpleNotificationService _snsClient;
        private readonly string _topicArn;

        public NotificationService(IAmazonSimpleNotificationService snsClient, IConfiguration configuration)
        {
            _snsClient = snsClient;
            _topicArn = configuration["AWS:SNSTopicArn"]
                ?? throw new ArgumentNullException("AWS:SNSTopicArn not configured.");
        }

        public async Task SendNewExpenseNotification(Expense expense)
        {
            var subject = $"New Expense Added: {expense.Description}";
            var message = $"A new expense '{expense.Description}' for ${expense.TotalAmount} was added by {expense.Payer.FirstName}.\n\n" +
                          $"You are involved as a participant. Please log in to see what you owe.";

            await PublishToSns(subject, message);
        }

        public async Task SendPaymentReminder(Debt debt)
        {
            var subject = $"Payment Reminder: You owe {debt.Expense.Payer.FirstName}";
            var message = $"This is a reminder that you owe {debt.Expense.Payer.FirstName} ${debt.AmountOwed} " +
                          $"for the expense '{debt.Expense.Description}'.\n\n" +
                          $"Please log in to settle your debt.";

            await PublishToSns(subject, message);
        }

        private Task PublishToSns(string subject, string message)
        {
            var request = new PublishRequest
            {
                TopicArn = _topicArn,
                Subject = subject,
                Message = message
            };
            return _snsClient.PublishAsync(request);
        }
    }
}