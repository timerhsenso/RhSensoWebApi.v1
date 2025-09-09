using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

// IMPORTANTE: se o seu Paging.cs usa "namespace RhSenso.Shared;",
// troque a linha abaixo para: using RhSenso.Shared;
using RhSenso.Shared.Paging;

namespace RhSensoWebApi.Core.Abstractions.Crud
{
    /// <summary>
    /// Contrato único para operações CRUD com paginação padrão.
    /// Padroniza assinaturas para todos os recursos do sistema.
    /// </summary>
    /// <typeparam name="TListDto">DTO usado em listagens/paginação.</typeparam>
    /// <typeparam name="TFormDto">DTO usado para criação/edição.</typeparam>
    /// <typeparam name="TKey">Tipo da chave (ex.: Guid, int).</typeparam>
    public interface ICrudService<TListDto, TFormDto, TKey>
    {
        /// <summary>Lista paginada com busca/ordenação padronizadas.</summary>
        Task<PageResult<TListDto>> ListAsync(PageQuery query, CancellationToken ct);

        /// <summary>Obtém um registro para edição/exibição.</summary>
        Task<TFormDto?> GetAsync(TKey id, CancellationToken ct);

        /// <summary>Cria um registro e retorna a chave gerada.</summary>
        Task<TKey> CreateAsync(TFormDto dto, CancellationToken ct);

        /// <summary>Atualiza um registro existente.</summary>
        Task UpdateAsync(TKey id, TFormDto dto, CancellationToken ct);

        /// <summary>Remove um registro (pode virar soft delete em camada de infra).</summary>
        Task DeleteAsync(TKey id, CancellationToken ct);

        /// <summary>Exclusão em lote — retorna quantidade afetada.</summary>
        Task<int> BulkDeleteAsync(IEnumerable<TKey> ids, CancellationToken ct);
    }
}
