namespace FinFree.Api.DTOs.Responses;

public class StatisticsResponse
{
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal Balance { get; set; }
    public int TransactionCount { get; set; }
}
