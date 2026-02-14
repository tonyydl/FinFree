namespace FinFree.Api.DTOs.Responses;

public class MonthlyReportResponse
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal Balance { get; set; }
    public int TransactionCount { get; set; }

    /// <summary>各分類支出排行</summary>
    public List<CategoryBreakdown> ExpenseByCategory { get; set; } = [];

    /// <summary>各分類收入排行</summary>
    public List<CategoryBreakdown> IncomeByCategory { get; set; } = [];

    /// <summary>每日支出趨勢</summary>
    public List<DailyExpense> DailyExpenses { get; set; } = [];
}

public class CategoryBreakdown
{
    public string CategoryName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
}

public class DailyExpense
{
    public string Date { get; set; } = string.Empty;
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
}
