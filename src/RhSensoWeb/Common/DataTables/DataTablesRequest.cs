namespace RhSensoWeb.Common.DataTables;

public sealed class DataTablesRequest
{
    public int Draw { get; set; }
    public int Start { get; set; }
    public int Length { get; set; }
    public DataTablesSearch? Search { get; set; }
    public DataTablesOrder[]? Order { get; set; }
    public DataTablesColumn[]? Columns { get; set; }
}
public sealed class DataTablesSearch { public string? Value { get; set; } public bool Regex { get; set; } }
public sealed class DataTablesOrder { public int Column { get; set; } public string Dir { get; set; } = "asc"; }
public sealed class DataTablesColumn { public string Data { get; set; } = ""; public string Name { get; set; } = ""; public bool Searchable { get; set; } public bool Orderable { get; set; } public DataTablesSearch? Search { get; set; } }
