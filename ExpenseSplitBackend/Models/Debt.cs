using System.ComponentModel.DataAnnotations.Schema;

namespace ExpenseSplitBackend.Models
{
    public class Debt
    {
        public int Id { get; set; }
        public int ExpenseId { get; set; }
        public Expense Expense { get; set; }

        public string DebtorId { get; set; }
        [ForeignKey("DebtorId")]
        public ApplicationUser Debtor { get; set; }

        public decimal AmountOwed { get; set; }
        public bool IsSettled { get; set; }
    }
}