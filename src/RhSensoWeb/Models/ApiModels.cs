using System.Text.Json.Serialization;

namespace RhSensoWeb.Models
{
    public sealed class PageResult<T>
    {
        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("data")]
        public List<T> Data { get; set; } = new();

        [JsonExtensionData]
        public Dictionary<string, object>? Extra { get; set; }
    }

    public sealed class Botao
    {
        [JsonPropertyName("id")]
        public int? Id { get; set; }

        [JsonPropertyName("codigo")]
        public string? Codigo { get; set; }

        [JsonPropertyName("nome")]
        public string? Nome { get; set; }

        [JsonPropertyName("descricao")]
        public string? Descricao { get; set; }

        [JsonPropertyName("codigoSistema")]
        public string? CodigoSistema { get; set; }

        [JsonPropertyName("codigoFuncao")]
        public string? CodigoFuncao { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object>? Extra { get; set; }
    }
}