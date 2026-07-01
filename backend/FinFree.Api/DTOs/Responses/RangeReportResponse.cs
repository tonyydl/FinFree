namespace FinFree.Api.DTOs.Responses;

public class RangeReportResponse
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal Balance { get; set; }
    public int TransactionCount { get; set; }

    /// <summary>各分類支出排行</summary>
    public List<CategoryBreakdown> ExpenseByCategory { get; set; } = [];

    /// <summary>各分類收入排行</summary>
    public List<CategoryBreakdown> IncomeByCategory { get; set; } = [];

    /// <summary>每日收支趨勢</summary>
    public List<DailyExpense> DailyExpenses { get; set; } = [];
}
