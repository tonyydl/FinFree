using FinFree.Api.Models;

namespace FinFree.Api.DTOs.Responses;

public class CategoryResponse
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public TransactionType Type { get; set; }
    public bool IsSystemDefault { get; set; }
}
