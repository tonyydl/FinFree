using FinFree.Api.DTOs.Requests;
using FinFree.Api.DTOs.Responses;

namespace FinFree.Api.Services.Interfaces;

public interface ITransactionService
{
    Task<IEnumerable<TransactionResponse>> GetAllAsync(int userId);
    Task<TransactionResponse?> GetByIdAsync(int id, int userId);
    Task<TransactionResponse> CreateAsync(CreateTransactionRequest request, int userId);
    Task<TransactionResponse?> UpdateAsync(int id, UpdateTransactionRequest request, int userId);
    Task<bool> DeleteAsync(int id, int userId);
    Task<StatisticsResponse> GetStatisticsAsync(int userId);
    Task<byte[]> ExportCsvAsync(int userId);
    Task<(int imported, int failed, List<string> errors)> ImportCsvAsync(Stream csvStream, int accountId, int userId);
}
