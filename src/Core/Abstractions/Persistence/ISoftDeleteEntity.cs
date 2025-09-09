namespace RhSensoWebApi.Core.Abstractions.Persistence
{
    /// <summary>
    /// Marca entidades que usam remoção lógica (soft delete).
    /// Ao deletar, a linha permanece no banco com IsDeleted = true.
    /// </summary>
    public interface ISoftDeleteEntity
    {
        bool IsDeleted { get; set; }
    }
}
