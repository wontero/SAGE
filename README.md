# SAGE — Sistema de Análisis y Gestión Energética

Plataforma web (Blazor, .NET 10, SQL Server) y dispositivo IoT (ESP32) para medir y controlar las fuentes de energía solar y red de una empresa.

- **Sprints 2 y 3 en curso.** El guion de la demo integrada está en la [sección 12 de la guía](GUIA_TRABAJO_SPRINT_2_3.md#12-demo-de-los-sprints-2-y-3-la-completa-mateo-en-s23-m17).
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
dotnet run --project src/SAGE.Web --launch-profile http
```

Abre `http://localhost:5284`. El perfil `http` inicia la app en Development. Para elegir otro puerto añade `-- --urls http://localhost:5291` y usa esa misma dirección en el simulador.

## Usuarios de prueba (Development)

Estas credenciales son las del seed de desarrollo.

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
dotnet run --project src/SAGE.Simulador -- --url http://localhost:5284 --token <TOKEN> --hora 12
```

- `--url <URL>` y `--token <TOKEN>` son obligatorios.
- `--intervalo <segundos>` fija la frecuencia de envío. Acepta enteros desde 1 y el valor por defecto es 1.
- `--hora <hora>` fija la hora de la planta, desde 0 inclusive hasta 24 exclusivo. `--hora 12` da sol al mediodía. Si se omite, se usa la hora local del equipo.

Teclas mientras corre:

| Tecla | Qué hace |
|---|---|
| `S` | Sombrea / descubre el panel |
| `R` | Corta / restablece la red (cargador a 0 V) |
| `N` | Noche forzada sí / no |
| `W` | Corta / restablece el Wi-Fi (sigue conmutando y acumula eventos; al volver los envía) |
| `Q` | Salir |

La consola muestra una línea por segundo (hora, `seq`, modo, estado, fuente, V/P de cada rama, Wi-Fi y código HTTP) y una línea aparte por cada evento y cada orden. El modo, los parámetros, la última orden ejecutada y el `boot_id` se guardan en `simulador_estado_<8 caracteres del hash del token>.json` junto al ejecutable (imita la memoria `Preferences` del ESP32): al reiniciar, el `boot_id` aumenta y se vuelve al modo guardado.

## Modo de dispositivo

`Dispositivo:Modo` vale `Real` de forma predeterminada. En este modo la app guarda y consulta la telemetría en SQL Server y entrega las órdenes al siguiente envío del dispositivo. Es el modo de la demo integrada con `SAGE.Simulador`.

Con `Dispositivo:Modo=Simulado`, la fábrica genera lecturas sintéticas para las pantallas que la usan y resuelve órdenes en memoria. Se puede iniciar este modo de presentación sin ejecutar `SAGE.Simulador` ni conectar un ESP32.

```bash
dotnet run --project src/SAGE.Web --launch-profile http -- --Dispositivo:Modo=Simulado
```

## Generar historial de desarrollo

Crea un dispositivo de prueba y usa su identificador para cargar siete días de lecturas sintéticas (604 800 mediciones). Ejecuta el comando en Development antes de iniciar la demo en vivo. Añade un arranque independiente al dispositivo y termina al completar la carga.

```bash
dotnet run --project src/SAGE.Web --launch-profile http -- --generar-historial <dispositivoId> 7
```

## Firmware

Firmware: pendiente para el Sprint 3 (sin hardware). La conmutación automática del Sprint 2 se demuestra con el simulador.

## Patrones de diseño en el código

| Patrón | Clases y rol | Archivo |
|---|---|---|
| Builder | `SimulacionBuilder` (constructor), `Simulacion` (producto), `PantallaSimulacion` (director) | [SimulacionBuilder.cs](src/SAGE.Web/Servicios/Simulacion/SimulacionBuilder.cs), [Simulacion.cs](src/SAGE.Web/Data/Simulacion.cs), [PantallaSimulacion.razor](src/SAGE.Web/Components/Pages/Simulacion/PantallaSimulacion.razor) |
| Adapter | `IAdaptadorTelemetria` (objetivo), `AdaptadorTelemetriaEsp32` (adaptador), `TelemetriaRequest` (adaptado) | [IAdaptadorTelemetria.cs](src/SAGE.Web/Servicios/Telemetria/IAdaptadorTelemetria.cs), [AdaptadorTelemetriaEsp32.cs](src/SAGE.Web/Servicios/Telemetria/AdaptadorTelemetriaEsp32.cs), [TelemetriaRequest.cs](src/SAGE.Web/Api/TelemetriaRequest.cs) |
| Facade | `FachadaSincronizacion` (fachada) | [FachadaSincronizacion.cs](src/SAGE.Web/Servicios/Telemetria/FachadaSincronizacion.cs) |
| Singleton (.NET) | `EstadoDispositivosEnVivo` (instancia única registrada con `AddSingleton`) | [EstadoDispositivosEnVivo.cs](src/SAGE.Web/Servicios/EnVivo/EstadoDispositivosEnVivo.cs), [Program.cs](src/SAGE.Web/Program.cs) |
| Observer | `EstadoDispositivosEnVivo` (sujeto), `PanelMonitoreo`, `Mediciones`, `Home`, `IndicadorAlertas` (observadores) | [EstadoDispositivosEnVivo.cs](src/SAGE.Web/Servicios/EnVivo/EstadoDispositivosEnVivo.cs), [PanelMonitoreo.razor](src/SAGE.Web/Components/PanelMonitoreo.razor), [Mediciones.razor](src/SAGE.Web/Components/Pages/Mediciones.razor), [Home.razor](src/SAGE.Web/Components/Pages/Home.razor), [IndicadorAlertas.razor](src/SAGE.Web/Components/Shared/IndicadorAlertas.razor) |
| Command | `Orden` (comando en el servidor), `ICanalOrdenes` y `CanalOrdenesBD` (invocadores en el servidor), `Comando` (comando), `ComandoModo` y `ComandoParametros` (comandos concretos), `ProcesadorOrdenes` (invocador en el simulador) | [Orden.cs](src/SAGE.Web/Data/Orden.cs), [ICanalOrdenes.cs](src/SAGE.Web/Servicios/Ordenes/ICanalOrdenes.cs), [CanalOrdenesBD.cs](src/SAGE.Web/Servicios/Ordenes/CanalOrdenesBD.cs), [Ordenes.cs](src/SAGE.Simulador/Ordenes.cs) |
| State | `EstadoFuente` (estado), `EnCargador`, `ProbandoSolar`, `EnSolar` y `SinAlimentacion` (estados concretos), `ControladorFuente` (contexto) | [Estados.cs](src/SAGE.Simulador/Estados.cs), [ControladorFuente.cs](src/SAGE.Simulador/ControladorFuente.cs) |
| Proxy | `CanalOrdenesProtegido` y `LectorTelemetriaProtegido` (proxies de protección) | [CanalOrdenesProtegido.cs](src/SAGE.Web/Servicios/Dispositivo/CanalOrdenesProtegido.cs), [LectorTelemetriaProtegido.cs](src/SAGE.Web/Servicios/Dispositivo/LectorTelemetriaProtegido.cs) |
| Abstract Factory | `IFabricaDispositivo` (fábrica abstracta), `FabricaDispositivoReal` y `FabricaDispositivoSimulado` (fábricas concretas), `ILectorTelemetria` e `ICanalOrdenes` (productos abstractos), `LectorTelemetriaBD`, `LectorTelemetriaSimulado`, `CanalOrdenesBD` y `CanalOrdenesSimulado` (productos concretos) | [IFabricaDispositivo.cs](src/SAGE.Web/Servicios/Dispositivo/IFabricaDispositivo.cs), [FabricaDispositivoReal.cs](src/SAGE.Web/Servicios/Dispositivo/FabricaDispositivoReal.cs), [FabricaDispositivoSimulado.cs](src/SAGE.Web/Servicios/Dispositivo/FabricaDispositivoSimulado.cs), [ILectorTelemetria.cs](src/SAGE.Web/Servicios/Dispositivo/ILectorTelemetria.cs), [ICanalOrdenes.cs](src/SAGE.Web/Servicios/Ordenes/ICanalOrdenes.cs), [LectorTelemetriaBD.cs](src/SAGE.Web/Servicios/Dispositivo/LectorTelemetriaBD.cs), [LectorTelemetriaSimulado.cs](src/SAGE.Web/Servicios/Dispositivo/LectorTelemetriaSimulado.cs), [CanalOrdenesBD.cs](src/SAGE.Web/Servicios/Ordenes/CanalOrdenesBD.cs), [CanalOrdenesSimulado.cs](src/SAGE.Web/Servicios/Dispositivo/CanalOrdenesSimulado.cs) |

Los patrones del firmware (State, Command y Singleton en C++) se implementan en el Sprint 3 junto con la maqueta.

## Facturas (S1-M7)

Página `/facturas`. Para importar desde Excel usa el botón "Descargar plantilla" (columnas `Año | Mes | Consumo (kWh) | Monto (Bs)`). En `docs/ejemplos/` hay `facturas_validas.xlsx` (6 meses correctos) y `facturas_con_errores.xlsx` (con errores, no importa ninguna fila).

## Simulación económica (S23-M4)

Página `/simulacion` (Administrador, Técnico, Consulta). Usa las facturas de la empresa activa, los parámetros globales (`/parametros`, solo Operador) y la tarifa propia, si existe. Para la demo selecciona Hotel Las Palmas e importa las facturas de ejemplo desde `/facturas` si falta consumo facturado. El resultado muestra los escenarios Red, Solar y Mixto con el chip SIMULACIÓN y el desglose del costo frente a red.
