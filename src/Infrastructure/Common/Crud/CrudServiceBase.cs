using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using RhSensoWebApi.Core.Abstractions.Crud;

// ATENÇÃO: use o namespace onde estão PageQuery/PageResult no seu Shared.
// Se for RhSenso.Shared, troque a linha abaixo.
using RhSenso.Shared.Paging;

namespace RhSensoWebApi.Infrastructure.Common.Crud
{
    /// <summary>
    /// Base genérica para serviços CRUD com paginação, busca e ordenação.
    /// Compatível com diferentes implementações de PageQuery/PageResult do projeto.
    /// </summary>
    public abstract class CrudServiceBase<TEntity, TKey, TListDto, TFormDto>
        : ICrudService<TListDto, TFormDto, TKey>
        where TEntity : class
    {
        protected readonly DbContext Db;

        protected CrudServiceBase(DbContext db)
        {
            Db = db ?? throw new ArgumentNullException(nameof(db));
        }

        // -------------------- Acesso ao DbSet/Queries --------------------

        /// <summary>DbSet rastreável (para Create/Update/Delete).</summary>
        protected virtual DbSet<TEntity> Set() => Db.Set<TEntity>();

        /// <summary>Query padrão para leitura (AsNoTracking). Faça Includes/ThenIncludes aqui.</summary>
        protected virtual IQueryable<TEntity> Query() => Set().AsNoTracking();

        // -------------------- Seletores / Mapeamentos --------------------

        /// <summary>Predicado EF traduzível (ex.: e =&gt; e.Id == id).</summary>
        protected abstract Expression<Func<TEntity, bool>> IdEquals(TKey id);

        /// <summary>Mapeia entidade → DTO de lista.</summary>
        protected abstract TListDto ToListDto(TEntity e);

        /// <summary>Mapeia entidade → DTO de formulário (opcional).</summary>
        protected virtual Task<TFormDto?> ToFormDtoAsync(TEntity e, CancellationToken ct) =>
            Task.FromResult<TFormDto?>(default);

        /// <summary>Cria entidade a partir do DTO de criação.</summary>
        protected abstract Task<TEntity> ToEntityOnCreateAsync(TFormDto dto, CancellationToken ct);

        /// <summary>Aplica alterações do DTO de edição na entidade rastreada.</summary>
        protected abstract Task MapOnUpdateAsync(TEntity e, TFormDto dto, CancellationToken ct);

        /// <summary>Extrai a chave da entidade (ex.: return e.Id;).</summary>
        protected virtual TKey ExtractKeyFromEntity(TEntity e) =>
            throw new NotSupportedException("Implemente ExtractKeyFromEntity(e) na classe derivada.");

        // -------------------- Busca / Ordenação --------------------------

        /// <summary>Aplica busca textual; padrão: sem filtro.</summary>
        protected virtual IQueryable<TEntity> ApplySearch(IQueryable<TEntity> q, string? search) => q;

        /// <summary>Aplica ordenação; padrão: ordem natural.</summary>
        protected virtual IQueryable<TEntity> ApplyOrder(IQueryable<TEntity> q, string? orderBy, bool asc) => q;

        // -------------------- ICrudService -------------------------------

        public virtual async Task<PageResult<TListDto>> ListAsync(PageQuery query, CancellationToken ct)
        {
            if (query is null) throw new ArgumentNullException(nameof(query));

            var (page, size, skip) = NormalizePaging(query);

            var baseQ = Query();
            baseQ = ApplySearch(baseQ, GetSearchValue(query));               // <-- sem depender do nome exato
            baseQ = ApplyOrder(baseQ, GetOrderByValue(query), GetAscValue(query));

            var total = await baseQ.CountAsync(ct);

            var entities = await baseQ.Skip(skip).Take(size).ToListAsync(ct);
            var items = entities.Select(ToListDto).ToList();

            return CreatePageResult(items, total, page, size);               // <-- ctor flexível
        }

        public virtual async Task<TFormDto?> GetAsync(TKey id, CancellationToken ct)
        {
            var e = await Query().FirstOrDefaultAsync(IdEquals(id), ct);
            if (e is null) return default;
            return await ToFormDtoAsync(e, ct);
        }

        public virtual async Task<TKey> CreateAsync(TFormDto dto, CancellationToken ct)
        {
            var entity = await ToEntityOnCreateAsync(dto, ct);
            Set().Add(entity);                                               // <-- DbSet, não IQueryable
            await Db.SaveChangesAsync(ct);
            return ExtractKeyFromEntity(entity);
        }

        public virtual async Task UpdateAsync(TKey id, TFormDto dto, CancellationToken ct)
        {
            var e = await Set().FirstOrDefaultAsync(IdEquals(id), ct)
                    ?? throw new KeyNotFoundException($"Registro {id} não encontrado.");

            await MapOnUpdateAsync(e, dto, ct);
            await Db.SaveChangesAsync(ct);
        }

        public virtual async Task DeleteAsync(TKey id, CancellationToken ct)
        {
            var e = await Set().FirstOrDefaultAsync(IdEquals(id), ct)
                    ?? throw new KeyNotFoundException($"Registro {id} não encontrado.");

            Db.Remove(e);
            await Db.SaveChangesAsync(ct);
        }

        public virtual async Task<int> BulkDeleteAsync(IEnumerable<TKey> ids, CancellationToken ct)
        {
            var idSet = ids?.ToHashSet() ?? new HashSet<TKey>();
            if (idSet.Count == 0) return 0;

#if NET7_0_OR_GREATER
            return await Set().Where(e => idSet.Contains(ExtractKeyFromEntity(e))).ExecuteDeleteAsync(ct);
#else
            var toRemove = await Set().Where(e => idSet.Contains(ExtractKeyFromEntity(e))).ToListAsync(ct);
            Set().RemoveRange(toRemove);
            return await Db.SaveChangesAsync(ct);
#endif
        }

        // -------------------- Helpers de compatibilidade -----------------

        private static (int page, int size, int skip) NormalizePaging(PageQuery query)
        {
            // tenta ler Page/PageIndex, PageSize/Size/Limit via reflexão (compatível com vários PageQuery)
            int page = ReadInt(query, "Page") ?? ReadInt(query, "PageIndex") ?? 1;
            int size = ReadInt(query, "PageSize") ?? ReadInt(query, "Size") ?? ReadInt(query, "Limit") ?? 10;

            if (page <= 0) page = 1;
            if (size <= 0) size = 10;

            return (page, size, (page - 1) * size);
        }

        private static string? GetSearchValue(PageQuery query)
        {
            // tenta propriedades comuns: Search, Query, Term, Q
            return ReadString(query, "Search")
                ?? ReadString(query, "Query")
                ?? ReadString(query, "Term")
                ?? ReadString(query, "Q");
        }

        private static string? GetOrderByValue(PageQuery query)
        {
            return ReadString(query, "OrderBy")
                ?? ReadString(query, "Sort")
                ?? ReadString(query, "SortBy");
        }

        private static bool GetAscValue(PageQuery query)
        {
            var asc = ReadBool(query, "Asc") ?? ReadBool(query, "Ascending") ?? ReadBool(query, "IsAsc");
            return asc ?? true;
        }

        private static int? ReadInt(object obj, string name)
            => obj.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                  ?.GetValue(obj) as int?;

        private static bool? ReadBool(object obj, string name)
            => obj.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                  ?.GetValue(obj) as bool?;

        private static string? ReadString(object obj, string name)
            => obj.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                  ?.GetValue(obj) as string;

        private static PageResult<TListDto> CreatePageResult(IList<TListDto> items, int total, int page, int size)
        {
            // 1) tenta ctor vazio e set de propriedades
            var result = Activator.CreateInstance<PageResult<TListDto>>();
            if (result is not null)
            {
                TrySet(result, new (string, object)[]
                {
                    ("Items", items), ("Data", items), ("Rows", items),

                    ("Total", total), ("TotalCount", total), ("RecordsTotal", total), ("Count", total),

                    ("Page", page), ("PageNumber", page), ("PageIndex", page),

                    ("PageSize", size), ("Size", size), ("Limit", size)
                });
                return result;
            }

            // 2) fallback: tenta encontrar algum ctor conhecido (ex.: (IEnumerable<T>, int))
            var type = typeof(PageResult<TListDto>);
            var ctors = type.GetConstructors();
            foreach (var c in ctors)
            {
                var ps = c.GetParameters();
                try
                {
                    if (ps.Length == 2 && ps[0].ParameterType.IsAssignableFrom(typeof(IEnumerable<TListDto>)) && ps[1].ParameterType == typeof(int))
                        return (PageResult<TListDto>)c.Invoke(new object[] { items, total });

                    if (ps.Length == 3 && ps[0].ParameterType.IsAssignableFrom(typeof(IEnumerable<TListDto>)) &&
                                          ps[1].ParameterType == typeof(int) &&
                                          ps[2].ParameterType == typeof(int))
                        return (PageResult<TListDto>)c.Invoke(new object[] { items, total, size });
                }
                catch { /* tenta próximo */ }
            }

            throw new NotSupportedException("PageResult<T> sem construtor compatível e sem propriedades atribuíveis.");
        }

        private static void TrySet(object obj, IEnumerable<(string name, object value)> candidates)
        {
            var type = obj.GetType();
            foreach (var (name, val) in candidates)
            {
                var p = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (p is null) continue;
                if (!p.CanWrite) continue;
                if (val is null) continue;
                if (p.PropertyType.IsAssignableFrom(val.GetType()))
                {
                    p.SetValue(obj, val);
                }
            }
        }
    }
}
