// src/RhSensoWeb/ViewModels/Account/LoginViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace RhSensoWeb.ViewModels.Account
{
    /// <summary>
    /// ViewModel usado pela tela de login do site (RhSensoWeb).
    /// Inclui os campos que a View espera: UserName, Password, RememberMe e ReturnUrl.
    /// Também expõe "Username" como alias de "UserName" para compatibilizar código legado.
    /// </summary>
    public sealed class LoginViewModel
    {
        private string? _userName;

        /// <summary>Usuário (nome de login).</summary>
        [Required(ErrorMessage = "Informe o usuário.")]
        [Display(Name = "Usuário")]
        public string? UserName
        {
            get => _userName;
            set { _userName = value; }
        }

        /// <summary>
        /// Alias para compatibilidade (alguns lugares podem ler/gravar "Username").
        /// Mantém o mesmo backing field de <see cref="UserName"/>.
        /// </summary>
        public string? Username
        {
            get => _userName;
            set { _userName = value; }
        }

        /// <summary>Senha do usuário.</summary>
        [Required(ErrorMessage = "Informe a senha.")]
        [DataType(DataType.Password)]
        [Display(Name = "Senha")]
        public string? Password { get; set; }

        /// <summary>Se marcado, mantém a sessão por mais tempo.</summary>
        [Display(Name = "Lembrar-me")]
        public bool RememberMe { get; set; }

        /// <summary>URL para redirecionar após login (quando vier de página protegida).</summary>
        public string? ReturnUrl { get; set; }

        /// <summary>Mensagem de erro para exibir na View.</summary>
        public string? Error { get; set; }
    }
}
