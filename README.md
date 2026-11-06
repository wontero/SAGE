# SAGE — Sistema de Análisis y Gestión Energética

Plataforma web (Blazor, .NET 10, SQL Server) y dispositivo IoT (ESP32) para medir y controlar las fuentes de energía solar y red de una empresa.

- **Sprint 1 en curso:** ver [GUIA_TRABAJO_SPRINT_1.md](GUIA_TRABAJO_SPRINT_1.md) (orden: Harold → Matías → Ethan).
- **Documentación:** carpeta [docs/](docs/) (ERS 2.0, diseño técnico, modelado y casos de uso).

## Requisitos

- SDK de **.NET 10**
- **SQL Server** accesible (en Windows: Express `.\SQLEXPRESS`; en macOS: SQL Server / Azure SQL Edge en Docker)
- Rider, Visual Studio o VS Code
- Herramienta EF Core: `dotnet tool install --global dotnet-ef`

## Base de datos

Cadena de conexión (Mac / Docker — usada en este entorno):

```
Server=localhost,1433;Database=SAGE;User Id=sa;Password=SageSql2026!;TrustServerCertificate=True;MultipleActiveResultSets=true
```

Contenedor de ejemplo (Apple Silicon):

```bash
docker run -e "ACCEPT_EULA=1" -e "MSSQL_SA_PASSWORD=SageSql2026!" \
  -p 1433:1433 --name sage-sql -d mcr.microsoft.com/azure-sql-edge
```

Crear / actualizar la base:

```bash
dotnet ef database update --project src/SAGE.Web
```

En Development, al arrancar la app también se aplican migraciones y se cargan los datos iniciales.

## Cómo ejecutar

```bash
dotnet restore
dotnet run --project src/SAGE.Web
```

Abre la URL que muestra la consola (por ejemplo `https://localhost:7xxx` o `http://localhost:5xxx`).

## Usuarios de prueba (Development)

| Correo | Contraseña | Rol | Empresa |
|---|---|---|---|
| admin@sage.test | Sage2026! | Administrador | — |
| tecnico@sage.test | Sage2026! | Tecnico | Energía Andina S.R.L. |
| consulta@sage.test | Sage2026! | Consulta | Energía Andina S.R.L. |
| tecnico.palmas@sage.test | Sage2026! | Tecnico | Hotel Las Palmas |

## Simulador del dispositivo (S1-M6)

Consola .NET 10 que imita al ESP32 y envía telemetría falsa a la plataforma.

1. Inicia sesión como Administrador, crea un dispositivo marcado como **SIMULADO** y copia su token (se muestra una sola vez).
2. Con la plataforma en marcha, ejecuta:

```bash
dotnet run --project src/SAGE.Simulador -- --url http://localhost:5284 --token <TOKEN>
```

Parámetros: `--url` y `--token` son obligatorios; `--intervalo <segundos>` (2 por defecto); `--hora <0-24>` es opcional y fija la hora del día simulada (por ejemplo `--hora 12` para probar con sol aunque sea de noche).

## Facturas (S1-M7)

Página `/facturas`. Para importar desde Excel usa el botón "Descargar plantilla" (columnas `Año | Mes | Consumo (kWh) | Monto (Bs)`). En `docs/ejemplos/` hay `facturas_validas.xlsx` (6 meses correctos) y `facturas_con_errores.xlsx` (con errores, no importa ninguna fila).
