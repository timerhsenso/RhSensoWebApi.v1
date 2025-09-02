# 🚀 RhSensoWebApi - API de Autenticação e Autorização

## 📋 Visão Geral

API REST robusta e pragmática para autenticação e autorização em sistemas ERP, desenvolvida em .NET 8 com Entity Framework Core e SQL Server.

## 🏗️ Arquitetura

- **API Layer**: Controllers, Middleware, DTOs
- **Core Layer**: Entities, Interfaces, Services
- **Infrastructure Layer**: Data Access, External Services, Cache

## 🔌 Endpoints Principais

### Base URL: `/api/v1/auth`

- `POST /login` - Autenticação de usuário
- `GET /permissoes` - Lista permissões do usuário
- `GET /checahabilitacao` - Verifica acesso a função
- `GET /checabotao` - Verifica permissão de ação
- `GET /checarestricao` - Verifica tipo de restrição

## 🚀 Como Executar

### Pré-requisitos
- .NET 8 SDK
- SQL Server
- Redis (opcional)

### Configuração

1. **Clone o repositório**
```bash
git clone <repository-url>
cd RhSensoWebApi
```

2. **Configure a string de conexão**
```json
{
  "ConnectionStrings": {
    "Default": "Server=localhost;Database=RhSensoWebApi;Trusted_Connection=true;TrustServerCertificate=true"
  }
}
```

3. **Execute as migrações**
```bash
dotnet ef database update --project src/Infrastructure --startup-project src/API
```

4. **Execute a aplicação**
```bash
dotnet run --project src/API
```

### Docker

```bash
# Build e execução com Docker Compose
docker-compose up --build

# Apenas build
docker build -t authapi .
```

## 🧪 Testes

```bash
# Executar todos os testes
dotnet test

# Executar com coverage
dotnet test --collect:"XPlat Code Coverage"
```

## 📊 Estrutura do Banco de Dados

### Tabelas Principais
- `tuse1` - Usuários
- `usrh1` - Grupos de usuários
- `hbrh1` - Permissões de grupos
- `tsistema` - Sistemas

## 🔐 Segurança

- JWT Bearer Authentication
- BCrypt para hash de senhas
- Rate limiting
- CORS configurado
- Validação de entrada
- Logging estruturado

## 📝 Códigos de Erro

- `E001` - Usuário não encontrado
- `E002` - Usuário inativo
- `E003` - Credenciais inválidas
- `E400` - Dados de entrada inválidos
- `E429` - Muitas tentativas
- `E500` - Erro interno do servidor

## 🔧 Configurações

### JWT
```json
{
  "JWT": {
    "Key": "your-secret-key-256-bits",
    "Issuer": "RhSensoWebApi",
    "Audience": "RhSensoWebApi-Clients",
    "ExpiryMinutes": 60
  }
}
```

### Cache
- L1: Memory Cache (5 minutos)
- L2: Redis Cache (30 minutos)

## 📈 Monitoramento

- Health checks em `/health`
- Logs estruturados com Serilog
- Métricas de performance

## 🤝 Contribuição

1. Fork o projeto
2. Crie uma branch para sua feature
3. Commit suas mudanças
4. Push para a branch
5. Abra um Pull Request

## 📄 Licença

Este projeto está sob a licença MIT.

