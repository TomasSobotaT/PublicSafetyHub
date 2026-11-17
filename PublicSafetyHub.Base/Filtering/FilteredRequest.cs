namespace PublicSafetyHub.Base.Filtering;

public class FilteredRequest<TFilter> : PaginationRequest where TFilter : class
{
    public TFilter Filter { get; set; }
}
