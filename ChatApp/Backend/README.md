# ChatApp Backend Foundation (.NET 10)

This repository contains the backend core for **ChatApp**, an enterprise-grade cross-platform real-time messaging application.

---

## 1. Architectural Highlights

- **Framework:** .NET 10 (C# 14 / modern features)
- **Architecture Style:** Clean Architecture organized as a Modular Monolith:
  - `ChatApp.Domain`: Zero-dependency domain layer (Entities, Value Objects, Enums, Exceptions).
  - `ChatApp.Application`: Business logic, CQRS interfaces, DTOs, FluentValidation rules.
  - `ChatApp.Infrastructure`: EF Core 10, SQL Server, ASP.NET Core Identity, Data Protection.
  - `ChatApp.API`: REST Controllers, OpenAPI/Swagger with JWT scheme, RFC 7807 Exception Middleware, Health Checks, Serilog.
- **Relational Storage:** Microsoft SQL Server 2022+ / LocalDB with automated EF Core migrations.
- **Identity & Security:** ASP.NET Core Identity with PBKDF2/Argon2 password hashing and token provider abstractions.
- **Observability:** Structured logging with Serilog and `/health` readiness probe.

---

## 2. Solution Structure

```
ChatApp/
├── ChatApp.slnx
├── Backend/
│   ├── src/
│   └── tests/
├── Mobile/
└── tests/                       # Mobile unit tests
```

---

## 3. Getting Started & Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Microsoft SQL Server 2022+ or SQL Server LocalDB (`(localdb)\mssqllocaldb`)
- `dotnet-ef` global or local tool (`dotnet tool restore`)

---

## 4. Build & Run Instructions

### 4.1 Restore & Build
```powershell
cd ChatApp\Backend
dotnet restore ..\ChatApp.slnx
dotnet build ..\ChatApp.slnx
```

### 4.2 Apply Database Migrations
To create or update the local database:
```powershell
dotnet dotnet-ef database update --project src/ChatApp.Infrastructure --startup-project src/ChatApp.API
```

### 4.3 Run the Web API
```powershell
cd src/ChatApp.API
dotnet run --launch-profile https
```
The API will launch at:
- Swagger Documentation: `https://localhost:7001/swagger`
- Health Check: `https://localhost:7001/health`
- Server Status: `https://localhost:7001/api/v1/status`

---

## 5. Automated Tests

Run the complete test suite (Unit + Integration tests):
```powershell
dotnet test ..\ChatApp.slnx
```
