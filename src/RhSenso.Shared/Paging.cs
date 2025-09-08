namespace RhSenso.Shared.Paging;

public sealed class PageQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? OrderBy { get; set; }
    public bool Asc { get; set; } = true;
    public string? Q { get; set; }
}

public sealed class PageResult<T>
{
    public int Total { get; set; }
    public List<T> Data { get; set; } = new();
}
