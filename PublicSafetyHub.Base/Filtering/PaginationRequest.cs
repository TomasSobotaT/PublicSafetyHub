namespace PublicSafetyHub.Base.Filtering;

public class PaginationRequest
{
    public int PageNumber { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public string SortBy { get; set; }

    public string SortDirection { get; set; } = "asc";
}
