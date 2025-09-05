using System.ComponentModel.DataAnnotations;

namespace RhSensoWebApi.Core.DTOs;

public class LoginRequest
{
    [Required]
    [MaxLength(50)]
    public string CdUsuario { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Senha { get; set; } = string.Empty;
}

