using System.ComponentModel.DataAnnotations.Schema;

namespace ExpenseSplitBackend.Models
{
    public class Expense
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime Date { get; set; }

        public string PayerId { get; set; }
        [ForeignKey("PayerId")]
        public ApplicationUser Payer { get; set; }

        public ICollection<Debt> Debts { get; set; }
    }
}