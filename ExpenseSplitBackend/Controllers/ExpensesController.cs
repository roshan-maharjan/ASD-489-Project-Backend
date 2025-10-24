using ExpenseSplitBackend.Data;
using ExpenseSplitBackend.Models;
using ExpenseSplitBackend.Models.DTOs;
using ExpenseSplitBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ExpenseSplitBackend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ExpensesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly DebtEmailService _debtEmailService;

        public ExpensesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, DebtEmailService debtEmailService)
        {
            _context = context;
            _userManager = userManager;
            _debtEmailService = debtEmailService;
        }

        [HttpGet]
        public async Task<IActionResult> GetExpenses()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var expenses = await _context.Expenses
                .Include(e => e.Payer)
                .Include(e => e.Debts).ThenInclude(d => d.Debtor)
                .Where(e => e.PayerId == userId || e.Debts.Any(d => d.DebtorId == userId))
                .OrderByDescending(e => e.Date)
                .ToListAsync();

            return Ok(expenses);
        }

        [HttpPost]
        public async Task<IActionResult> CreateExpense([FromBody] CreateExpenseModel model)
        {
            var payerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var payer = await _userManager.FindByIdAsync(payerId);

            var expense = new Expense
            {
                Description = model.Description,
                TotalAmount = model.TotalAmount,
                Date = model.Date,
                PayerId = payerId,
                Payer = payer,
                Debts = new List<Debt>()
            };

            try
            {
                expense.Debts = CalculateDebts(expense, model);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }

            _context.Expenses.Add(expense);
            await _context.SaveChangesAsync();

            // Send email for each debt
            foreach (var debt in expense.Debts)
            {
                await _debtEmailService.SendDebtEmailAsync(debt.Id, payerId);
            }

            return Ok(MapToExpenseDto(expense));
        }

        [HttpGet("balance-summary")]
        public async Task<IActionResult> GetBalanceSummary()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // "You owe": Debts where current user is debtor and not settled
            var youOwe = await _context.Debts
                .Where(d => d.DebtorId == userId && !d.IsSettled)
                .SumAsync(d => d.AmountOwed);

            // "You are owed": Debts where current user is payer and not settled
            var youAreOwed = await _context.Debts
                .Where(d => d.Expense.PayerId == userId && !d.IsSettled)
                .SumAsync(d => d.AmountOwed);

            var netBalance = youAreOwed - youOwe;

            var summary = new BalanceSummaryDto
            {
                YouOwe = youOwe,
                YouAreOwed = youAreOwed,
                NetBalance = netBalance
            };

            return Ok(summary);
        }

        private ICollection<Debt> CalculateDebts(Expense expense, CreateExpenseModel model)
        {
            var debts = new List<Debt>();
            var participantCount = model.Participants.Count;

            switch (model.SplitType)
            {
                case SplitMethod.Equal:
                    var amountPerPerson = Math.Round(expense.TotalAmount / participantCount, 2);
                    foreach (var p in model.Participants)
                    {
                        if (p.UserId == expense.PayerId) continue;
                        debts.Add(new Debt { DebtorId = p.UserId, AmountOwed = amountPerPerson, IsSettled = false });
                    }
                    break;

                case SplitMethod.Exact:
                    if (model.Participants.Sum(p => p.Amount ?? 0) != expense.TotalAmount)
                        throw new ArgumentException("Sum of exact amounts does not equal total amount.");

                    foreach (var p in model.Participants)
                    {
                        if (p.UserId == expense.PayerId) continue;
                        debts.Add(new Debt { DebtorId = p.UserId, AmountOwed = p.Amount.Value, IsSettled = false });
                    }
                    break;

                case SplitMethod.Percentage:
                    if (model.Participants.Sum(p => p.Percentage ?? 0) != 100)
                        throw new ArgumentException("Sum of percentages does not equal 100.");

                    foreach (var p in model.Participants)
                    {
                        if (p.UserId == expense.PayerId) continue;
                        var amount = Math.Round(expense.TotalAmount * (p.Percentage.Value / 100), 2);
                        debts.Add(new Debt { DebtorId = p.UserId, AmountOwed = amount, IsSettled = false });
                    }
                    break;
            }
            return debts;
        }

        private ExpenseDto MapToExpenseDto(Expense expense)
        {
            return new ExpenseDto
            {
                Id = expense.Id,
                Description = expense.Description,
                TotalAmount = expense.TotalAmount,
                Date = expense.Date,
                PayerId = expense.PayerId,
                PayerName = expense.Payer?.FirstName + " " + expense.Payer?.LastName,
                Debts = expense.Debts.Select(d => new DebtDto
                {
                    DebtorId = d.DebtorId,
                    DebtorName = d.Debtor?.FirstName + " " + d.Debtor?.LastName,
                    AmountOwed = d.AmountOwed,
                    IsSettled = d.IsSettled
                }).ToList()
            };
        }
    }
}