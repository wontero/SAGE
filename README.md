# SAGE — Sistema de Análisis y Gestión Energética

Plataforma web (Blazor, .NET 10, SQL Server) y dispositivo IoT (ESP32) para medir y controlar las fuentes de energía solar y red de una empresa.

- **Sprints 2 y 3 en curso:** ver [GUIA_TRABAJO_SPRINT_2_3.md](GUIA_TRABAJO_SPRINT_2_3.md) (orden: Harold → Matías → Ethan → Mateo). Harold completó S23-M1 a S23-M4.
- Sprint 1 (cerrado): [GUIA_TRABAJO_SPRINT_1.md](GUIA_TRABAJO_SPRINT_1.md).
- **Documentación:** carpeta [docs/](docs/) (ERS 2.1, diseño técnico, modelado, casos de uso y documentos de patrones).

## Requisitos

- SDK de **.NET 10**
- **SQL Server** accesible (en Windows: Express `.\SQLEXPRESS`; en macOS: SQL Server / Azure SQL Edge en Docker)
- Rider, Visual Studio o VS Code
- Herramienta EF Core: `dotnet tool install --global dotnet-ef`

## Base de datos

Por defecto, `src/SAGE.Web/appsettings.json` apunta a `.\SQLEXPRESS` (Windows). **No subas** cadenas con usuario/contraseña de Docker (ni en `appsettings*.json`, ni en README, ni en commits).

En Mac / Docker / LocalDB usa variable de entorno o user-secrets:

```bash
export ConnectionStrings__DefaultConnection='Server=localhost,1433;Database=SAGE;User Id=sa;Password=<TU_PASSWORD>;TrustServerCertificate=True;MultipleActiveResultSets=true'