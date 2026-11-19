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
```

Contenedor de ejemplo (Apple Silicon) — elegí **tu** contraseña SA (mínimo 8 caracteres, con mayúscula, minúscula, número y símbolo):

```bash
docker run -e "ACCEPT_EULA=1" -e "MSSQL_SA_PASSWORD=<TU_PASSWORD>" \
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
| operador@sage.test | Sage2026! | Operador | — |
| admin@sage.test | Sage2026! | Administrador | Energía Andina S.R.L. y Hotel Las Palmas |
| admin.palmas@sage.test | Sage2026! | Administrador | Hotel Las Palmas |
| tecnico@sage.test | Sage2026! | Tecnico | Energía Andina S.R.L. |
| consulta@sage.test | Sage2026! | Consulta | Energía Andina S.R.L. |
| tecnico.palmas@sage.test | Sage2026! | Tecnico | Hotel Las Palmas |

## Simulador del dispositivo (S1-M6, reescrito en S23-M11)

Consola .NET 10 que se comporta como el firmware del ESP32: máquina de estados de la fuente (patrón State), órdenes de la plataforma (patrón Command) y la planta física simulada (panel, cargador y lámpara).

1. Inicia sesión como Administrador, crea un dispositivo marcado como **SIMULADO** y copia su token (se muestra una sola vez).
2. Con la plataforma en marcha, ejecuta:

```bash
dotnet run --project src/SAGE.Simulador -- --url http://localhost:5284 --token <TOKEN>
```

Parámetros: `--url` y `--token` son obligatorios; `--intervalo <segundos>` (1 por defecto); `--hora <0-24>` es opcional y fija la hora del día simulada (por ejemplo `--hora 12` para probar con sol aunque sea de noche).

Teclas mientras corre:

| Tecla | Qué hace |
|---|---|
| `S` | Sombrea / descubre el panel |
| `R` | Corta / restablece la red (cargador a 0 V) |
| `N` | Noche forzada sí / no |
| `W` | Corta / restablece el Wi-Fi (sigue conmutando y acumula eventos; al volver los envía) |
| `Q` | Salir |

La consola muestra una línea por segundo (hora, `seq`, modo, estado, fuente, V/P de cada rama, Wi-Fi y código HTTP) y una línea aparte por cada evento y cada orden. El modo, los parámetros, la última orden ejecutada y el `boot_id` se guardan en `simulador_estado_<8 caracteres del hash del token>.json` junto al ejecutable (imita la memoria `Preferences` del ESP32): al reiniciar, el `boot_id` aumenta y se vuelve al modo guardado.

## Facturas (S1-M7)

Página `/facturas`. Para importar desde Excel usa el botón "Descargar plantilla" (columnas `Año | Mes | Consumo (kWh) | Monto (Bs)`). En `docs/ejemplos/` hay `facturas_validas.xlsx` (6 meses correctos) y `facturas_con_errores.xlsx` (con errores, no importa ninguna fila).

## Simulación económica (S23-M4)

Página `/simulacion` (Administrador, Técnico, Consulta). Usa parámetros globales (`/parametros`, solo Operador) y, si existe, la tarifa de la empresa. El gráfico SVG permite fijar un año; debajo se muestra el desglose Ahorrás/Gastás vs red.
