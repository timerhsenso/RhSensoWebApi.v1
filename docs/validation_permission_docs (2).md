# 🛡️ Sistema de Validação e Permissões - RhSensoWebApi

> **Documento Técnico Completo**  
> Para programadores e IAs que desenvolvem neste projeto

---

## 📋 Índice
1. [Visão Geral](#visão-geral)
2. [Sistema de Validação](#sistema-de-validação)
3. [Sistema de Permissões](#sistema-de-permissões)
4. [Estrutura de Claims JWT](#estrutura-de-claims-jwt)
5. [Implementação Prática](#implementação-prática)
6. [Exemplos de Uso](#exemplos-de-uso)
7. [Padrões e Convenções](#padrões-e-convenções)
8. [Troubleshooting](#troubleshooting)

---

## 🎯 Visão Geral

O projeto utiliza **duas camadas independentes** de segurança e qualidade:

- **🔍 Validação**: Garante integridade dos dados via FluentValidation
- **🔒 Permissões**: Controla acesso via Claims JWT + Filtros de Autorização

### Arquitetura de Segurança

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Frontend      │───▶│   JWT Claims     │───▶│   Backend       │
│   (MVC/API)     │    │   (Permissões)   │    │   (Validação)   │
└─────────────────┘    └──────────────────┘    └─────────────────┘
      ▲                          ▲                        ▲
      │                          │                        │
   UI/UX                   Autorização                Integridade
  Controls                    + Audit                  dos Dados
```

---

## 🔍 Sistema de Validação

### Estrutura Base

**Localização**: `src/API/Validators/{MÓDULO}/{Recurso}FormValidator.cs`

```csharp
// Exemplo: src/API/Validators/SEG/BotaoFormValidator.cs
public class BotaoFormValidator : AbstractValidator<BotaoFormDto>
{
    public BotaoFormValidator()
    {
        RuleFor(x => x.Codigo)
            .NotEmpty().WithMessage("Código é obrigatório")
            .MaximumLength(10).WithMessage("Código deve ter no máximo 10 caracteres")
            .Matches("^[A-Z0-9_]+$").WithMessage("Código deve conter apenas letras maiúsculas, números e underscore");

        RuleFor(x => x.Descricao)
            .NotEmpty().WithMessage("Descrição é obrigatória")
            .MaximumLength(100).WithMessage("Descrição deve ter no máximo 100 caracteres");

        RuleFor(x => x.Ativo)
            .NotNull().WithMessage("Status ativo é obrigatório");
    }
}
```

### Registro Automático no DI

**No `Program.cs` da API**:
```csharp
// Registra todos os validators do assembly
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// Integra com MVC/API
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();
```

### Validações Customizadas Padrão

#### 🔤 **Códigos de Sistema**
```csharp
RuleFor(x => x.CodigoSistema)
    .NotEmpty()
    .Length(3, 3).WithMessage("Código do sistema deve ter exatamente 3 caracteres")
    .Matches("^[A-Z]{3}$").WithMessage("Código deve conter apenas letras maiúsculas");
```

#### 📧 **E-mail Corporativo**
```csharp
RuleFor(x => x.Email)
    .NotEmpty()
    .EmailAddress().WithMessage("E-mail inválido")
    .Must(BeValidCorporateEmail).WithMessage("Deve ser um e-mail corporativo válido");

private bool BeValidCorporateEmail(string email)
{
    var allowedDomains = new[] { "@empresa.com.br", "@empresa.com" };
    return allowedDomains.Any(domain => email.EndsWith(domain, StringComparison.OrdinalIgnoreCase));
}
```

#### 📱 **CPF/CNPJ**
```csharp
RuleFor(x => x.CpfCnpj)
    .NotEmpty()
    .Must(BeValidCpfOrCnpj).WithMessage("CPF/CNPJ inválido");

private bool BeValidCpfOrCnpj(string cpfCnpj)
{
    // Implementar validação de CPF/CNPJ
    return CpfCnpjValidator.IsValid(cpfCnpj);
}
```

#### 💰 **Valores Monetários**
```csharp
RuleFor(x => x.Valor)
    .GreaterThanOrEqualTo(0).WithMessage("Valor deve ser maior ou igual a zero")
    .PrecisionScale(10, 2, false).WithMessage("Valor deve ter no máximo 2 casas decimais");
```

### Validação Condicional

```csharp
// Validar apenas quando determinada condição for verdadeira
RuleFor(x => x.DataVencimento)
    .NotEmpty().WithMessage("Data de vencimento é obrigatória")
    .GreaterThan(DateTime.Today).WithMessage("Data deve ser futura")
    .When(x => x.TipoDocumento == "FATURA");

// Validação dependente de outro campo
RuleFor(x => x.ConfirmacaoSenha)
    .Equal(x => x.Senha).WithMessage("Confirmação deve ser igual à senha")
    .When(x => !string.IsNullOrEmpty(x.Senha));
```

### Resposta de Erro Padronizada

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Validation Error",
  "status": 400,
  "errors": {
    "Codigo": ["Código é obrigatório"],
    "Descricao": ["Descrição deve ter no máximo 100 caracteres"],
    "Email": ["E-mail inválido", "Deve ser um e-mail corporativo válido"]
  },
  "traceId": "0HMVH..."
}
```

---

## 🔒 Sistema de Permissões

### Estrutura Hierárquica

```
SISTEMA (3 chars)
├── FUNÇÃO (até 20 chars)
    ├── BOTÃO (até 15 chars)
    └── BOTÃO (até 15 chars)
```

**Exemplos**:
- `SEG:USUARIOS:INCLUIR`
- `SEG:USUARIOS:ALTERAR`
- `SEG:USUARIOS:EXCLUIR`
- `FIN:CONTAS_PAGAR:APROVAR`
- `CAD:FORNECEDORES:CONSULTAR`

### Claims JWT Padrão

#### **Estrutura de Claims no Token**:
```json
{
  "sub": "12345",                           // ID do usuário
  "name": "João Silva",                     // Nome completo
  "email": "joao.silva@empresa.com.br",     // E-mail
  "role": "Administrador",                  // Perfil/Role principal
  "SuperUser": "false",                     // Super usuário (bypass total)
  "Permission": [                           // Array de permissões
    "SEG:USUARIOS:INCLUIR",
    "SEG:USUARIOS:ALTERAR",
    "SEG:USUARIOS:EXCLUIR",
    "FIN:CONTAS_PAGAR:CONSULTAR",
    "FIN:CONTAS_PAGAR:APROVAR"
  ],
  "IdEmpresa": "1",                         // Multi-tenant
  "IdFilial": "101",                        // Multi-filial
  "exp": 1672531200,                        // Expiração
  "iat": 1672444800                         // Emitido em
}
```

### Filtro de Permissão (RequirePermissionAttribute)

**Localização**: `src/RhSensoWeb/Services/Security/RequirePermissionAttribute.cs`

#### **Uso em Controllers MVC**:
```csharp
[RequirePermission("SEG", "USUARIOS", "INCLUIR")]
public async Task<IActionResult> Create()
{
    // Apenas usuários com permissão SEG:USUARIOS:INCLUIR
    return View();
}

[RequirePermission("SEG", "USUARIOS")]
public async Task<IActionResult> Index()
{
    // Usuários com qualquer permissão de USUARIOS no SEG
    return View();
}
```

#### **Uso em Controllers API**:
```csharp
[ApiController]
[Route("api/v{version:apiVersion}/seg/usuarios")]
[RequirePermission("SEG", "USUARIOS")] // Aplica a toda a controller
public class UsuarioController : BaseCrudController<UsuarioListDto, UsuarioFormDto, int>
{
    [HttpPost]
    [RequirePermission("SEG", "USUARIOS", "INCLUIR")] // Sobrescreve para ação específica
    public override async Task<IActionResult> Create([FromBody] UsuarioFormDto dto)
    {
        return await base.Create(dto);
    }
}
```

### Comportamento do Filtro

#### **Para Requisições Web (MVC)**:
- ✅ **Sucesso**: Continua a execução
- ❌ **Sem autenticação**: Redireciona para `/Account/Login`
- ❌ **Sem permissão**: Redireciona para `/Error/AccessDenied`

#### **Para Requisições AJAX/API**:
- ✅ **Sucesso**: Continua a execução
- ❌ **Sem autenticação**: Retorna `401 Unauthorized`
- ❌ **Sem permissão**: Retorna `403 Forbidden` com JSON:

```json
{
  "success": false,
  "message": "Acesso negado. Você não tem permissão para executar esta ação.",
  "permissionRequired": "SEG:USUARIOS:INCLUIR"
}
```

### Extensions para Verificação Manual

**Localização**: `RhSensoWeb.Services.Security.PermissionExtensions`

```csharp
// Em Controllers
if (User.HasPermission("SEG", "USUARIOS", "EXCLUIR"))
{
    // Lógica específica para usuários que podem excluir
}

// Em Views Razor
@if (User.HasPermission("SEG", "USUARIOS", "INCLUIR"))
{
    <button class="btn btn-primary">Novo Usuário</button>
}

// Verificar super usuário
@if (User.IsSuperUser())
{
    <div class="admin-panel">...</div>
}

// Listar todas as permissões
var permissions = User.GetPermissions().ToList();
```

---

## 🏗️ Estrutura de Claims JWT

### Configuração no Program.cs (API)

```csharp
// JWT Configuration
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
```

### Geração de Token (Login)

```csharp
public async Task<string> GenerateJwtToken(Usuario usuario, List<string> permissions)
{
    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
        new(ClaimTypes.Name, usuario.Nome),
        new(ClaimTypes.Email, usuario.Email),
        new(ClaimTypes.Role, usuario.Perfil),
        new("SuperUser", usuario.SuperUser.ToString().ToLower()),
        new("IdEmpresa", usuario.IdEmpresa.ToString()),
        new("IdFilial", usuario.IdFilial?.ToString() ?? "")
    };

    // Adiciona cada permissão como claim separada
    foreach (var permission in permissions)
    {
        claims.Add(new Claim("Permission", permission));
    }

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var expires = DateTime.UtcNow.AddHours(_jwtSettings.ExpirationHours);

    var token = new JwtSecurityToken(
        issuer: _jwtSettings.Issuer,
        audience: _jwtSettings.Audience,
        claims: claims,
        expires: expires,
        signingCredentials: creds
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}
```

---

## 🛠️ Implementação Prática

### 1. Criando um Novo CRUD com Validação e Permissão

#### **Passo 1: DTO com Validação**
```csharp
// src/RhSenso.Shared/CAD/Fornecedores/FornecedorFormDto.cs
public class FornecedorFormDto
{
    public int Id { get; set; }
    public string RazaoSocial { get; set; } = "";
    public string NomeFantasia { get; set; } = "";
    public string CpfCnpj { get; set; } = "";
    public string Email { get; set; } = "";
    public bool Ativo { get; set; } = true;
}

// src/API/Validators/CAD/FornecedorFormValidator.cs
public class FornecedorFormValidator : AbstractValidator<FornecedorFormDto>
{
    public FornecedorFormValidator()
    {
        RuleFor(x => x.RazaoSocial)
            .NotEmpty().WithMessage("Razão Social é obrigatória")
            .MaximumLength(100).WithMessage("Razão Social deve ter no máximo 100 caracteres");

        RuleFor(x => x.CpfCnpj)
            .NotEmpty().WithMessage("CPF/CNPJ é obrigatório")
            .Must(BeValidCpfOrCnpj).WithMessage("CPF/CNPJ inválido");

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("E-mail inválido")
            .When(x => !string.IsNullOrEmpty(x.Email));
    }

    private bool BeValidCpfOrCnpj(string cpfCnpj) => CpfCnpjValidator.IsValid(cpfCnpj);
}
```

#### **Passo 2: Controller com Permissões**
```csharp
// src/API/Controllers/CAD/FornecedorController.cs
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/cad/fornecedores")]
[Tags("Fornecedores")]
[RequirePermission("CAD", "FORNECEDORES")]
public class FornecedorController : BaseCrudController<FornecedorListDto, FornecedorFormDto, int>
{
    public FornecedorController(ICrudService<FornecedorListDto, FornecedorFormDto, int> service)
        : base(service)
    {
    }

    [HttpPost]
    [RequirePermission("CAD", "FORNECEDORES", "INCLUIR")]
    public override async Task<IActionResult> Create([FromBody] FornecedorFormDto dto)
    {
        return await base.Create(dto);
    }

    [HttpPut("{id}")]
    [RequirePermission("CAD", "FORNECEDORES", "ALTERAR")]
    public override async Task<IActionResult> Update(int id, [FromBody] FornecedorFormDto dto)
    {
        return await base.Update(id, dto);
    }

    [HttpDelete("{id}")]
    [RequirePermission("CAD", "FORNECEDORES", "EXCLUIR")]
    public override async Task<IActionResult> Delete(int id)
    {
        return await base.Delete(id);
    }
}
```

### 2. View com Controle de Permissões

```html
<!-- src/RhSensoWeb/Views/Fornecedores/Index.cshtml -->
@{
    ViewData["Title"] = "Fornecedores";
    var canCreate = User.HasPermission("CAD", "FORNECEDORES", "INCLUIR");
    var canEdit = User.HasPermission("CAD", "FORNECEDORES", "ALTERAR");
    var canDelete = User.HasPermission("CAD", "FORNECEDORES", "EXCLUIR");
}

<div class="content-header">
    <div class="container-fluid">
        <div class="row mb-2">
            <div class="col-sm-6">
                <h1>Fornecedores</h1>
            </div>
            <div class="col-sm-6">
                <div class="float-right">
                    @if (canCreate)
                    {
                        <button type="button" class="btn btn-primary" onclick="novoFornecedor()">
                            <i class="fas fa-plus"></i> Novo Fornecedor
                        </button>
                    }
                </div>
            </div>
        </div>
    </div>
</div>

<section class="content">
    <div class="container-fluid">
        <div class="card">
            <div class="card-body">
                <table id="fornecedoresTable" class="table table-striped table-hover">
                    <thead>
                        <tr>
                            <th>ID</th>
                            <th>Razão Social</th>
                            <th>CPF/CNPJ</th>
                            <th>E-mail</th>
                            <th>Status</th>
                            <th width="120">Ações</th>
                        </tr>
                    </thead>
                </table>
            </div>
        </div>
    </div>
</section>

<script>
$(document).ready(function() {
    var table = $('#fornecedoresTable').DataTable({
        processing: true,
        serverSide: true,
        ajax: {
            url: '/api/v1/cad/fornecedores',
            type: 'GET',
            data: function(d) {
                return {
                    page: Math.floor(d.start / d.length) + 1,
                    pageSize: d.length,
                    q: d.search.value
                };
            }
        },
        columns: [
            { data: 'id' },
            { data: 'razaoSocial' },
            { data: 'cpfCnpj' },
            { data: 'email' },
            { 
                data: 'ativo',
                render: function(data) {
                    return data ? '<span class="badge badge-success">Ativo</span>' : 
                                 '<span class="badge badge-danger">Inativo</span>';
                }
            },
            {
                data: null,
                orderable: false,
                render: function(data, type, row) {
                    var actions = [];
                    
                    @if (canEdit)
                    {
                        <text>
                        actions.push('<button class="btn btn-sm btn-primary" onclick="editarFornecedor(' + row.id + ')" title="Editar"><i class="fas fa-edit"></i></button>');
                        </text>
                    }
                    
                    @if (canDelete)
                    {
                        <text>
                        actions.push('<button class="btn btn-sm btn-danger" onclick="excluirFornecedor(' + row.id + ')" title="Excluir"><i class="fas fa-trash"></i></button>');
                        </text>
                    }
                    
                    return actions.join(' ');
                }
            }
        ]
    });
});
</script>
```

---

## 📐 Padrões e Convenções

### Validação

#### **✅ Boas Práticas**
- ✅ Um validator por FormDTO
- ✅ Mensagens claras e em português
- ✅ Validações específicas por domínio
- ✅ Use `When()` para validações condicionais
- ✅ Valide no backend SEMPRE (mesmo que validado no front)

#### **❌ Evitar**
- ❌ Validação apenas no frontend
- ❌ Mensagens genéricas ("Campo inválido")
- ❌ Validators aninhados ou complexos demais
- ❌ Lógica de negócio no validator (só validação de formato/obrigatoriedade)

### Permissões

#### **✅ Boas Práticas**
- ✅ Códigos em MAIÚSCULO com underscore
- ✅ Hierarquia clara: SISTEMA:FUNÇÃO:BOTÃO
- ✅ Permissões granulares por ação
- ✅ Super usuário apenas em desenvolvimento/emergência
- ✅ Claims específicas para cada permissão

#### **❌ Evitar**
- ❌ Permissões genéricas demais ("ADMIN", "USER")
- ❌ Códigos com espaços ou caracteres especiais
- ❌ Lógica de permissão hardcoded no controller
- ❌ Super usuário em produção sem controle
- ❌ Bypass de permissões no código

### Códigos Padrão por Módulo

| Módulo | Descrição | Exemplo de Funções |
|--------|-----------|-------------------|
| **SEG** | Segurança | USUARIOS, PERFIS, BOTOES, SISTEMAS |
| **CAD** | Cadastros | FORNECEDORES, CLIENTES, PRODUTOS |
| **FIN** | Financeiro | CONTAS_PAGAR, CONTAS_RECEBER, BANCOS |
| **EST** | Estoque | MOVIMENTOS, INVENTARIO, TRANSFERENCIAS |
| **COM** | Comercial | PEDIDOS, VENDAS, COMISSOES |
| **RH** | Recursos Humanos | FUNCIONARIOS, FOLHA, PONTO |

#### Botões Padrão por Função

| Botão | Descrição | Uso |
|-------|-----------|-----|
| **CONSULTAR** | Visualizar dados | Listagem, detalhes |
| **INCLUIR** | Criar novos registros | Formulário de criação |
| **ALTERAR** | Modificar existentes | Formulário de edição |
| **EXCLUIR** | Remover registros | Ação de delete |
| **APROVAR** | Aprovação de workflow | Processos que precisam aprovação |
| **CANCELAR** | Cancelar operações | Desfazer ações |
| **IMPRIMIR** | Gerar relatórios/docs | Impressão/PDF |
| **EXPORTAR** | Exportar dados | Excel, CSV, etc. |

---

## 🐛 Troubleshooting

### Problemas Comuns de Validação

#### **❌ Validator não está executando**
```csharp
// ✅ Verificar se está registrado no DI
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
builder.Services.AddFluentValidationAutoValidation();

// ✅ Verificar namespace e nome da classe
public class FornecedorFormValidator : AbstractValidator<FornecedorFormDto>
//           ^^^^^^^^^^^^^^^^^^^                      ^^^^^^^^^^^^^^^^
//           Nome deve terminar com "Validator"      DTO correto
```

#### **❌ Mensagens não aparecem no frontend**
```csharp
// ✅ Verificar se está usando ModelState
if (!ModelState.IsValid)
{
    return BadRequest(ModelState); // Retorna erros de validação
}
```

#### **❌ Validação customizada não funciona**
```csharp
// ✅ Método Must() deve ser estático ou usar this
RuleFor(x => x.Cpf)
    .Must(BeValidCpf).WithMessage("CPF inválido");

private static bool BeValidCpf(string cpf) // static!
{
    return CpfValidator.IsValid(cpf);
}
```

### Problemas Comuns de Permissão

#### **❌ Usuário com permissão sendo negado**
```csharp
// ✅ Verificar se o claim está correto no token
// Token deve conter: "Permission": "SEG:USUARIOS:INCLUIR"
// Atributo deve estar: [RequirePermission("SEG", "USUARIOS", "INCLUIR")]

// ✅ Debug do token (em desenvolvimento)
var permissions = User.Claims.Where(c => c.Type == "Permission").Select(c => c.Value);
Console.WriteLine($"User permissions: {string.Join(", ", permissions)}");
```

#### **❌ Redirecionamentos não funcionam**
```csharp
// ✅ Verificar se as rotas existem
// /Account/Login - deve existir
// /Error/AccessDenied - deve existir

// ✅ Para AJAX, verificar header
context.Request.Headers.ContainsKey("X-Requested-With")
```

#### **❌ Super usuário não funciona**
```csharp
// ✅ Claim deve ser exatamente assim:
new Claim("SuperUser", "true") // string "true", não boolean

// ✅ Verificação:
User.HasClaim("SuperUser", "true") // Case-sensitive!
```

### Logs Úteis para Debug

```csharp
// No RequirePermissionAttribute
private readonly ILogger<RequirePermissionAttribute> _logger;

public void OnAuthorization(AuthorizationFilterContext context)
{
    var user = context.HttpContext.User;
    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var requiredPermission = $"{_sistema}:{_funcao}:{_botao}";
    var userPermissions = user.Claims.Where(c => c.Type == "Permission").Select(c => c.Value);
    
    _logger.LogInformation("User {UserId} requesting permission {Permission}. User has: {UserPermissions}",
        userId, requiredPermission, string.Join(", ", userPermissions));
}
```

---

## 🎯 Checklist de Implementação

### Para Cada Novo CRUD:

#### **Backend (API)**
- [ ] DTO em `RhSenso.Shared/{MÓDULO}/{RECURSO}/`
- [ ] Validator em `API/Validators/{MÓDULO}/`
- [ ] Controller herda `BaseCrudController`
- [ ] Atributos de permissão no controller
- [ ] Registro do service no DI
- [ ] Testes de validação

#### **Frontend (MVC)**
- [ ] Controller MVC com client tipado
- [ ] View com verificações de permissão
- [ ] DataTable com botões condicionais
- [ ] Form com validação client-side
- [ ] Mensagens de erro adequadas
- [ ] Testes de interface

#### **Segurança**
- [ ] Claims de permissão definidas
- [ ] Botões protegidos por permissão
- [ ] Super usuário testado
- [ ] CORS configurado
- [ ] HTTPS obrigatório
- [ ] Logs de auditoria

---

## 📚 Recursos Adicionais

### Links Úteis
- [FluentValidation Documentation](https://docs.fluentvalidation.net/)
- [JWT.io - Token Debugger](https://jwt.io/)
- [ASP.NET Core Security](https://docs.microsoft.com/en-us/aspnet/core/security/)

### Ferramentas de Desenvolvimento
- **Postman**: Para testar APIs com JWT
- **JWT.io**: Para debugar tokens
- **Swagger**: Para documentar e testar endpoints
- **Visual Studio Debugger**: Para inspecionar claims

---

> 📝 **Nota**: Este documento deve ser atualizado sempre que houver mudanças no sistema de validação ou permissões. Mantenha a documentação sincronizada com o código!

---

**🔧 Versão**: 1.0  
**📅 Última atualização**: Setembro 2025  
**👥 Mantenedores**: Equipe de Desenvolvimento RhSenso