namespace ExpenseSplitBackend.Models.DTOs
{
    public class BalanceSummaryDto
    {
        public decimal YouOwe { get; set; }
        public decimal YouAreOwed { get; set; }
        public decimal NetBalance { get; set; }
    }
}
