namespace Domain.DTOs.Admin;

public record PaginatedResponse<T>
{
    public required int TotalItems { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required int TotalPages { get; init; }
    public required IEnumerable<T> Items { get; init; }
}