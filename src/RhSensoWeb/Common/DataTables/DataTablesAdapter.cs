using RhSenso.Shared.Paging;

namespace RhSensoWeb.Common.DataTables;

public static class DataTablesAdapter
{
    public static PageQuery ToPageQuery(DataTablesRequest req, IReadOnlyList<string> orderableColumns)
    {
        var length = req.Length <= 0 ? 10 : req.Length;
        var page = (req.Start / Math.Max(1, length)) + 1;

        string? orderBy = null;
        var asc = true;

        if (req.Order?.Length > 0 && req.Columns != null)
        {
            var ord = req.Order[0];
            var idx = Math.Clamp(ord.Column, 0, req.Columns.Length - 1);
            var colName = req.Columns[idx].Data;
            if (!string.IsNullOrWhiteSpace(colName) &&
                orderableColumns.Contains(colName, StringComparer.OrdinalIgnoreCase))
                orderBy = colName;

            asc = string.Equals(ord.Dir, "asc", StringComparison.OrdinalIgnoreCase);
        }

        return new PageQuery
        {
            Page = page,
            PageSize = length,
            OrderBy = orderBy ?? orderableColumns.First(),
            Asc = asc,
            Q = req.Search?.Value
        };
    }

    public static object ToDataTablesResponse<T>(int draw, PageResult<T> pr)
        => new { draw, recordsTotal = pr.Total, recordsFiltered = pr.Total, data = pr.Data };
}
