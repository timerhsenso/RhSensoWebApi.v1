using RhSensoWebApi.Core.DTOs;
using RhSensoWebApi.Core.Entities;

namespace RhSensoWebApi.ExpandedTests.Fixtures;

/// <summary>
/// Builder pattern para criação de dados de teste
/// Facilita a criação de objetos de teste com valores padrão e customizações
/// </summary>
public class TestDataBuilder
{
    /// <summary>
    /// Builder para criação de usuários de teste
    /// </summary>
    public class UserBuilder
    {
        private User _user;

        public UserBuilder()
        {
            // Valores padrão para um usuário de teste
            _user = new User
            {
                CdUsuario = "test_user",
                DcUsuario = "Usuário de Teste",
                SenhaUser = "$2a$11$rQiU8Z8Z8Z8Z8Z8Z8Z8Z8O", // Hash padrão para "test123"
                FlAtivo = true,
                EmailUsuario = "test@example.com",
                CdEmpresa = "EMP01",
                CdFilial = "FIL01",
                TpUsuario = "USER",
                Id = 1,
                IdFuncionario = 100,
                NormalizedUsername = "test_user",
                FlNaoRecebeEmail = false,
                NmImpcche = "TEST_PRINTER",
                NoMatric = "12345",
                NoUser = "Test User"
            };
        }

        /// <summary>
        /// Define o código do usuário
        /// </summary>
        public UserBuilder WithCdUsuario(string cdUsuario)
        {
            _user.CdUsuario = cdUsuario;
            _user.NormalizedUsername = cdUsuario.ToUpper();
            return this;
        }

        /// <summary>
        /// Define a descrição do usuário
        /// </summary>
        public UserBuilder WithDcUsuario(string dcUsuario)
        {
            _user.DcUsuario = dcUsuario;
            return this;
        }

        /// <summary>
        /// Define o hash da senha
        /// </summary>
        public UserBuilder WithSenhaHash(string senhaHash)
        {
            _user.SenhaUser = senhaHash;
            return this;
        }

        /// <summary>
        /// Define se o usuário está ativo
        /// </summary>
        public UserBuilder WithFlAtivo(bool flAtivo)
        {
            _user.FlAtivo = flAtivo;
            return this;
        }

        /// <summary>
        /// Define o email do usuário
        /// </summary>
        public UserBuilder WithEmail(string email)
        {
            _user.EmailUsuario = email;
            return this;
        }

        /// <summary>
        /// Define a empresa do usuário
        /// </summary>
        public UserBuilder WithEmpresa(string cdEmpresa)
        {
            _user.CdEmpresa = cdEmpresa;
            return this;
        }

        /// <summary>
        /// Define a filial do usuário
        /// </summary>
        public UserBuilder WithFilial(string cdFilial)
        {
            _user.CdFilial = cdFilial;
            return this;
        }

        /// <summary>
        /// Define o tipo do usuário
        /// </summary>
        public UserBuilder WithTpUsuario(string tpUsuario)
        {
            _user.TpUsuario = tpUsuario;
            return this;
        }

        /// <summary>
        /// Define o ID do usuário
        /// </summary>
        public UserBuilder WithId(int id)
        {
            _user.Id = id;
            return this;
        }

        /// <summary>
        /// Define o ID do funcionário
        /// </summary>
        public UserBuilder WithIdFuncionario(int idFuncionario)
        {
            _user.IdFuncionario = idFuncionario;
            return this;
        }

        /// <summary>
        /// Cria um usuário administrador
        /// </summary>
        public UserBuilder AsAdmin()
        {
            return WithCdUsuario("admin_test")
                .WithDcUsuario("Administrador de Teste")
                .WithTpUsuario("ADMIN")
                .WithEmail("admin@test.com");
        }

        /// <summary>
        /// Cria um usuário comum
        /// </summary>
        public UserBuilder AsRegularUser()
        {
            return WithCdUsuario("user_test")
                .WithDcUsuario("Usuário Comum de Teste")
                .WithTpUsuario("USER")
                .WithEmail("user@test.com");
        }

        /// <summary>
        /// Cria um usuário inativo
        /// </summary>
        public UserBuilder AsInactiveUser()
        {
            return WithCdUsuario("inactive_test")
                .WithDcUsuario("Usuário Inativo de Teste")
                .WithFlAtivo(false)
                .WithEmail("inactive@test.com");
        }

        /// <summary>
        /// Constrói o objeto User
        /// </summary>
        public User Build()
        {
            return _user;
        }
    }

    /// <summary>
    /// Builder para criação de permissões de teste
    /// </summary>
    public class PermissionBuilder
    {
        private PermissionDto _permission;

        public PermissionBuilder()
        {
            // Valores padrão para uma permissão de teste
            _permission = new PermissionDto
            {
                CdSistema = "SYS01",
                CdFuncao = "FUNC01",
                CdAcoes = "IACE",
                CdRestric = 'L'
            };
        }

        /// <summary>
        /// Define o código do sistema
        /// </summary>
        public PermissionBuilder WithSistema(string cdSistema)
        {
            _permission.CdSistema = cdSistema;
            return this;
        }

        /// <summary>
        /// Define o código da função
        /// </summary>
        public PermissionBuilder WithFuncao(string cdFuncao)
        {
            _permission.CdFuncao = cdFuncao;
            return this;
        }

        /// <summary>
        /// Define as ações permitidas
        /// </summary>
        public PermissionBuilder WithAcoes(string cdAcoes)
        {
            _permission.CdAcoes = cdAcoes;
            return this;
        }

        /// <summary>
        /// Define a restrição
        /// </summary>
        public PermissionBuilder WithRestricao(char cdRestric)
        {
            _permission.CdRestric = cdRestric;
            return this;
        }

        /// <summary>
        /// Cria uma permissão de administrador (todas as ações, sem restrição)
        /// </summary>
        public PermissionBuilder AsAdminPermission()
        {
            return WithAcoes("IACE").WithRestricao('L');
        }

        /// <summary>
        /// Cria uma permissão de usuário comum (incluir e consultar apenas)
        /// </summary>
        public PermissionBuilder AsUserPermission()
        {
            return WithAcoes("IC").WithRestricao('P');
        }

        /// <summary>
        /// Cria uma permissão somente leitura
        /// </summary>
        public PermissionBuilder AsReadOnlyPermission()
        {
            return WithAcoes("C").WithRestricao('L');
        }

        /// <summary>
        /// Constrói o objeto PermissionDto
        /// </summary>
        public PermissionDto Build()
        {
            return _permission;
        }
    }

    /// <summary>
    /// Builder para criação de requisições de login
    /// </summary>
    public class LoginRequestBuilder
    {
        private LoginRequest _loginRequest;

        public LoginRequestBuilder()
        {
            // Valores padrão para uma requisição de login
            _loginRequest = new LoginRequest
            {
                CdUsuario = "test_user",
                Senha = "test123"
            };
        }

        /// <summary>
        /// Define o código do usuário
        /// </summary>
        public LoginRequestBuilder WithCdUsuario(string cdUsuario)
        {
            _loginRequest.CdUsuario = cdUsuario;
            return this;
        }

        /// <summary>
        /// Define a senha
        /// </summary>
        public LoginRequestBuilder WithSenha(string senha)
        {
            _loginRequest.Senha = senha;
            return this;
        }

        /// <summary>
        /// Cria uma requisição de login para administrador
        /// </summary>
        public LoginRequestBuilder AsAdmin()
        {
            return WithCdUsuario("admin_test").WithSenha("admin123");
        }

        /// <summary>
        /// Cria uma requisição de login para usuário comum
        /// </summary>
        public LoginRequestBuilder AsUser()
        {
            return WithCdUsuario("user_test").WithSenha("user123");
        }

        /// <summary>
        /// Cria uma requisição de login inválida
        /// </summary>
        public LoginRequestBuilder AsInvalid()
        {
            return WithCdUsuario("invalid_user").WithSenha("wrong_password");
        }

        /// <summary>
        /// Constrói o objeto LoginRequest
        /// </summary>
        public LoginRequest Build()
        {
            return _loginRequest;
        }
    }

    /// <summary>
    /// Cria um builder para usuários
    /// </summary>
    public static UserBuilder User() => new UserBuilder();

    /// <summary>
    /// Cria um builder para permissões
    /// </summary>
    public static PermissionBuilder Permission() => new PermissionBuilder();

    /// <summary>
    /// Cria um builder para requisições de login
    /// </summary>
    public static LoginRequestBuilder LoginRequest() => new LoginRequestBuilder();
}

