namespace FinFree.Api.DTOs.Responses;

public class BudgetResponse
{
    public int Id { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public decimal Amount { get; set; }
    public decimal Spent { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
}
