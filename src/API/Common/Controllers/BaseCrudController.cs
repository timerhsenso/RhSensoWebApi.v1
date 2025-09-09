using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

// Contrato criado no Item 1
using RhSensoWebApi.Core.Abstractions.Crud;

// PageQuery/PageResult – ajuste o using conforme o seu Shared.
// Se seus tipos estiverem em "RhSenso.Shared", troque a linha abaixo.
using RhSenso.Shared.Paging;

namespace RhSensoWebApi.API.Common.Controllers
{
    /// <summary>
    /// Controller base com endpoints CRUD padronizados.
    /// - Use [Route("api/v{version:apiVersion}/<modulo>/<recurso>")] no controller concreto.
    /// - Opcional: adicione [ApiVersion("1.0")] e [Tags("<Recurso>")] no controller concreto.
    /// </summary>
    [ApiController]
    [Produces("application/json")]
    public abstract class BaseCrudController<TListDto, TFormDto, TKey> : ControllerBase
    {
        protected readonly ICrudService<TListDto, TFormDto, TKey> Service;

        protected BaseCrudController(ICrudService<TListDto, TFormDto, TKey> service)
            => Service = service ?? throw new ArgumentNullException(nameof(service));

        /// <summary>Lista paginada (page, pageSize, search, orderBy, asc...).</summary>
        [HttpGet]
        public Task<PageResult<TListDto>> List([FromQuery] PageQuery query, CancellationToken ct)
            => Service.ListAsync(query, ct);

        /// <summary>Obtém um registro por id.</summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<TFormDto>> GetById([FromRoute] TKey id, CancellationToken ct)
        {
            var dto = await Service.GetAsync(id, ct);
            if (dto is null) return NotFound();
            return Ok(dto);
        }

        /// <summary>Cria um registro e retorna a chave.</summary>
        [HttpPost]
        public async Task<ActionResult<TKey>> Create([FromBody] TFormDto dto, CancellationToken ct)
        {
            var id = await Service.CreateAsync(dto, ct);
            // Tenta apontar para o GET deste mesmo controller
            return CreatedAtAction(nameof(GetById), new { id }, id);
        }

        /// <summary>Atualiza um registro.</summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update([FromRoute] TKey id, [FromBody] TFormDto dto, CancellationToken ct)
        {
            await Service.UpdateAsync(id, dto, ct);
            return NoContent();
        }

        /// <summary>Exclui um registro.</summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete([FromRoute] TKey id, CancellationToken ct)
        {
            await Service.DeleteAsync(id, ct);
            return NoContent();
        }

        /// <summary>Exclusão em lote.</summary>
        [HttpPost("bulk-delete")]
        public async Task<ActionResult<int>> BulkDelete([FromBody] IEnumerable<TKey> ids, CancellationToken ct)
        {
            var affected = await Service.BulkDeleteAsync(ids, ct);
            return Ok(affected);
        }
    }
}
