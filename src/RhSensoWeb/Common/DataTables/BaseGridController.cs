using Microsoft.AspNetCore.Mvc;
using RhSenso.Shared.Paging;
using RhSensoWeb.Common.DataTables;

namespace RhSensoWeb.Common;

public abstract class BaseGridController : Controller
{
    /// <summary>
    /// Converte a requisição do DataTables e executa o fetch de forma genérica.
    /// </summary>
    protected async Task<IActionResult> Dt<T>(
        DataTablesRequest req,
        Func<PageQuery, Task<PageResult<T>>> fetchAsync,
        IReadOnlyList<string> orderableColumns)
    {
        var pq = DataTablesAdapter.ToPageQuery(req, orderableColumns);
        var page = await fetchAsync(pq);
        return Json(DataTablesAdapter.ToDataTablesResponse(req.Draw, page));
    }
}
