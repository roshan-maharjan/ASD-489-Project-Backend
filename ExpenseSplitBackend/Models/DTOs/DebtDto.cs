namespace ExpenseSplitBackend.Models.DTOs
{
    public class DebtDto
    {
        public string DebtorId { get; set; }
        public string DebtorName { get; set; }
        public decimal AmountOwed { get; set; }
        public bool IsSettled { get; set; }
    }
}
