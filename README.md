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
