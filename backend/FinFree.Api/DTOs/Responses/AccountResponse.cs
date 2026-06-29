using FinFree.Api.Models;

namespace FinFree.Api.DTOs.Responses;

public class AccountResponse
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public AccountType AccountType { get; set; }
    public decimal InitialBalance { get; set; }
    public decimal Balance { get; set; }
    public DateTime CreatedAt { get; set; }
}
