using System.Text.Json.Serialization;

namespace RhSenso.Shared.Common.DataTables
{
    /// <summary>
    /// Envelope compatível com DataTables server-side.
    /// Saída em camelCase por padrão do .NET: draw, recordsTotal, recordsFiltered, data, error.
    /// </summary>
    public sealed class DataTableResponse<T>
    {
        public int Draw { get; init; }
        public int RecordsTotal { get; init; }
        public int RecordsFiltered { get; init; }
        public IReadOnlyList<T> Data { get; init; }
        public string? Error { get; init; }

        [JsonConstructor]
        public DataTableResponse(int draw, int recordsTotal, int recordsFiltered, IReadOnlyList<T> data, string? error = null)
        {
            Draw = draw;
            RecordsTotal = recordsTotal;
            RecordsFiltered = recordsFiltered;
            Data = data;
            Error = error;
        }
    }
}
