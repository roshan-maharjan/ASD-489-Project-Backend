namespace ExpenseSplitBackend.Models.DTOs
{
    public class ExpenseDto
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime Date { get; set; }
        public string PayerId { get; set; }
        public string PayerName { get; set; }
        public List<DebtDto> Debts { get; set; }
    }
}
