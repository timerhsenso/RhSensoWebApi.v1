﻿using System;
using RhSensoWebApi.Core.Abstractions.Persistence;

namespace RhSensoWebApi.Core.Entities.SEG
{
    // Habilita auditoria + soft delete para Botao sem tocar no arquivo original.
    public partial class Botao : IAuditableEntity, ISoftDeleteEntity
    {
        public bool IsDeleted { get; set; }

        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }
    }
}
