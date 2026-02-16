namespace FinFree.Api.DTOs.Responses;

public class PagedResult<T>
{
    public IEnumerable<T> Items { get; set; } = [];
    public int Total { get; set; }
}
