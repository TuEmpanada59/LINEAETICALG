# Línea ética — Canal de denuncias Grupo Londoño Gómez

Canal propio para recibir y gestionar denuncias de conductas indebidas en todas las empresas del grupo.
Backend en .NET (ASP.NET Core Web API) y frontend en React (Vite + TypeScript).

## Estructura

```
LINEAETICALG/
├── backend/
│   ├── database/                       Scripts SQL (Azure SQL / SQL Server)
│   ├── docs/                           Requisitos y documentación
│   │   └── requisitos.md               Decisiones jurídicas y pendientes
│   ├── scripts/                        Utilidades (llave de cifrado, publicación)
│   ├── src/
│   │   ├── LineaEtica.Api/             ASP.NET Core Web API (controladores, Program.cs)
│   │   ├── LineaEtica.Domain/          Entidades y enumeraciones
│   │   └── LineaEtica.Infrastructure/  EF Core, cifrado, servicios
│   ├── test/
│   │   └── LineaEtica.Tests/           Pruebas xUnit
│   ├── .env.example                    Variables de entorno de la API
│   └── .gitignore
├── frontend/
│   ├── public/
│   ├── src/
│   ├── .env.example
│   └── .gitignore
└── README.md
```

Este repositorio de GitHub es el puente de trabajo; el código se lleva a Azure DevOps.

## Correrlo en local

1. **API** (en `backend/`). La cadena de conexión y la llave de cifrado van en `dotnet user-secrets`,
   nunca en el repo (ver `backend/.env.example`):
   ```
   dotnet run --project src/LineaEtica.Api
   ```
   Queda en `http://localhost:5170`. `GET /api/desarrollo/conexion` confirma la base y las migraciones.
2. **Frontend** (en `frontend/`):
   ```
   npm install
   npm run dev
   ```
   Queda en `http://localhost:5173` y reenvía `/api` a la API, así las cookies funcionan como en producción.
   El panel de gestión está en `/gestion`.

Antes de dar algo por terminado: `dotnet test` en `backend/` y `npm run build` en `frontend/`.
