# HubPay — .NET Aspire

Orquestração local da **API** e do **Blazor WASM** com dashboard Aspire.

PostgreSQL e Redis usam as connection strings de `appsettings.json` da Web API (não são geridos pelo AppHost).

## Pré-requisitos

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- Extensão **Aspire** no Visual Studio ou CLI `aspire` (opcional)

## Executar

Na raiz do repositório:

```powershell
dotnet run --project src/HubPay.AppHost/HubPay.AppHost.csproj
```

Ou defina `HubPay.AppHost` como projeto de arranque no Visual Studio / Rider e execute (F5).

O browser abre o **Aspire Dashboard** com links para:

| Recurso | Descrição |
|---------|-----------|
| **webapi** | HubPay Web API (migrations, Swagger, ReDoc) |
| **blazor** | Dashboard Blazor (AdminLTE) |

## URLs típicas

As portas são atribuídas dinamicamente — consulte o dashboard Aspire.

- API ReDoc: `https://localhost:{porta-webapi}/redoc`
- Blazor: `https://localhost:{porta-blazor}/`

O Blazor recebe automaticamente `ApiBaseUrl` apontando para o endpoint HTTPS da API.

## Sem Aspire (modo anterior)

Continua possível executar projetos isolados:

```powershell
dotnet run --project src/HubPay.WebApi
dotnet run --project src/HubPay.Frontend.Blazor
```

Nesse caso use a connection string em `appsettings.json` e `wwwroot/appsettings.json` (`ApiBaseUrl`).

## Estrutura

```
src/HubPay.AppHost/          — Orquestrador Aspire
src/HubPay.ServiceDefaults/  — Telemetria, health, service discovery
src/HubPay.WebApi/           — API (referencia ServiceDefaults)
src/HubPay.Frontend.Blazor/  — Frontend WASM
```

## Base de dados e Redis

Configure em `src/HubPay.WebApi/appsettings.json` (`HubPay:ConnectionString`, `HubPay:RedisConnectionString`).
