using FinFree.Api.DTOs.Requests;
using FinFree.Api.DTOs.Responses;

namespace FinFree.Api.Services.Interfaces;

public interface ITransactionService
{
    Task<IEnumerable<TransactionResponse>> GetAllAsync(int userId);
    Task<PagedResult<TransactionResponse>> GetPagedAsync(TransactionQueryParams query, int userId);
    Task<TransactionResponse?> GetByIdAsync(int id, int userId);
    Task<TransactionResponse> CreateAsync(CreateTransactionRequest request, int userId);
    Task<TransactionResponse?> UpdateAsync(int id, UpdateTransactionRequest request, int userId);
    Task<bool> DeleteAsync(int id, int userId);
    Task<StatisticsResponse> GetStatisticsAsync(int userId, DateTime? startDate = null, DateTime? endDate = null);
    Task<MonthlyReportResponse> GetMonthlyReportAsync(int year, int month, int userId);
    Task<RangeReportResponse> GetRangeReportAsync(DateTime startDate, DateTime endDate, int userId);
    Task<byte[]> ExportCsvAsync(int userId);
    Task<(int imported, int failed, List<string> errors)> ImportCsvAsync(Stream csvStream, int accountId, int userId);
}
