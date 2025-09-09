using System;

namespace RhSensoWebApi.Core.Abstractions.Persistence
{
    /// <summary>
    /// Marca entidades que registram trilha de auditoria:
    /// - INSERT: CreatedAt/By
    /// - UPDATE: UpdatedAt/By
    /// - SOFT DELETE: DeletedAt/By
    /// Sempre usar horário UTC.
    /// </summary>
    public interface IAuditableEntity
    {
        DateTime CreatedAt { get; set; }
        string? CreatedBy { get; set; }

        DateTime? UpdatedAt { get; set; }
        string? UpdatedBy { get; set; }

        DateTime? DeletedAt { get; set; }
        string? DeletedBy { get; set; }
    }
}
