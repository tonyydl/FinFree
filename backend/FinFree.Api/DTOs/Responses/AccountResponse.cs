namespace FinFree.Api.DTOs.Responses;

public class AccountResponse
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public decimal Balance { get; set; }
    public DateTime CreatedAt { get; set; }
}
