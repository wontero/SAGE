# Guía de trabajo — Sprints 2 y 3 (SAGE)

> **Para la persona:** dale este archivo completo a tu agente de IA (Claude Code, Codex, Copilot, etc.) y dile:
> *"Lee GUIA_TRABAJO_SPRINT_2_3.md completo y ejecuta las misiones de &lt;tu nombre&gt;."*
> Tú haces solo la sección 1 (preparar la PC). Al final revisas que el agente haya marcado sus casillas, escrito en la Bitácora y subido todo.

> **Para el agente:** esta guía está escrita para ti. Cada misión dice qué construir, con qué nombres, en qué archivos y cómo comprobarlo. Si algo no está escrito acá, **no lo inventes**: aplica la regla 9 de la sección 0. Los nombres de clases, rutas, valores de texto y campos JSON de esta guía son **exactos**: respeta mayúsculas, minúsculas y tildes.

- **Repositorio:** https://github.com/wontero/SAGE (rama `main`)
- **Orden de trabajo (estricto):** 1) **Harold** → 2) **Matías** → 3) **Ethan** → 4) **Mateo**. Nadie empieza hasta que el anterior subió su trabajo, marcó sus casillas y escribió su entrada en la Bitácora.
- **Qué cubre:** todo lo que falta de los Sprints 2 y 3 de la ERS 2.1 (`docs/ERS_SAGE_v2.1.pdf`; RF04 a RF18, RNF03 a RNF12), la corrección de roles y empresas, y la aplicación en código de los patrones de diseño presentados (creacionales, estructurales y de comportamiento).

**Meta:** la plataforma completa con multiempresa, monitoreo en tiempo real, control de fuente por órdenes, alertas, historial, ensayos, simulación económica, el simulador con la máquina de estados y el firmware del ESP32.

**Criterio del docente:** cada rol ve solo lo suyo y cada empresa ve solo sus datos; las mediciones se ven en tiempo real; una orden se ve ejecutada, rechazada con motivo o expirada; el guion de la demo (sección 12) funciona de punta a punta.

---

## 0. Reglas para el agente (obligatorias)

1. Lee esta guía **completa** antes de tocar nada. Esta guía manda sobre `docs/` y sobre la guía del Sprint 1. Donde haya diferencias, se hace lo que dice esta guía (la sección 3 lista los cambios respecto de `docs/`).
2. Haz **solo las misiones de la persona que te pidió el trabajo**, en el orden en que aparecen. No adelantes misiones de otros ni agregues funciones que no están acá.
3. **Verificación de entrada:** antes de tu primera misión, comprueba que funciona lo que dejó el compañero anterior (cada sección trae su lista). Si algo falla:
   - Error chico que te bloquea → corrígelo y anótalo en la Bitácora como "Corrección a la misión X".
   - Falta una misión entera o el error es grande → **no la hagas tú**. Anota "BLOQUEO" en la Bitácora, súbela y avísale a tu persona.
4. **No cambies el stack** (sección 2). Paquetes NuGet permitidos además de los del Sprint 1: `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk` (solo en el proyecto de pruebas). Cualquier otro paquete: anótalo en la Bitácora con el motivo.
5. Una misión está **hecha** solo si:
   - `dotnet build SAGE.slnx` termina **sin errores**;
   - `dotnet test` pasa (desde que existe el proyecto de pruebas, misión S23-M1);
   - la aplicación arranca;
   - **comprobaste a mano cada criterio de aceptación** (ejecutando la app, el simulador, consultando la base o llamando a la API). No marques una casilla sin haberla comprobado. Si un criterio no se pudo comprobar, déjalo sin marcar y explica por qué en la Bitácora.
6. Al terminar cada misión: marca sus casillas `[x]` en esta guía, haz commit y push (sección 1.3). Al terminar todas tus misiones, agrega tu entrada en la Bitácora (sección 11).
7. Nunca uses `git push --force`, no borres historial y no subas `bin/`, `obj/`, `.vs/`, `firmware/sage_esp32/config.h` ni contraseñas reales (tampoco la del SA de Docker en README, guías o `appsettings*.json`).
8. Toda la interfaz en **español**, con la unidad en cada valor (V, mA, W, kW, Wh, kWh, Bs, s). Componentes MudBlazor (tarjetas, tablas, chips, íconos), nada de HTML suelto sin estilo. Las horas se **guardan en UTC** y se **muestran en hora de Bolivia** (`America/La_Paz`, UTC−4).
9. Si esta guía tiene un error, una contradicción o algo que no se puede hacer: no lo ignores en silencio. Haz lo más razonable, que no contradiga ninguna otra parte de la guía, y explica en la Bitácora qué cambiaste y por qué.
10. **Los datos de una empresa nunca se muestran a usuarios de otra empresa.** El filtro va en la consulta a la base (`Where(... EmpresaId == ...)`), no solo en ocultar botones. Esto vale para cada pantalla, cada consulta y cada suscripción a eventos en vivo.
11. **Patrones:** cada clase que cumple un rol en un patrón lleva, en la línea anterior a su declaración, un comentario con este formato exacto:
    `// Patrón: <Nombre del patrón> — rol: <rol>`
    Ejemplo: `// Patrón: Command — rol: comando concreto`. Así el equipo las encuentra con una búsqueda para la exposición. La sección 5 lista todas.
12. No cambies `appsettings.json` para tu PC. Si tu base no es `.\SQLEXPRESS` (Mac, LocalDB, Docker), usa la variable de entorno `ConnectionStrings__DefaultConnection` o `dotnet user-secrets`. **Nunca** dejes la cadena con password en el repositorio.

---

## 1. Preparación (la hace la persona, una sola vez)

### 1.1 Instalar
- Lo mismo que en el Sprint 1: Git, SDK de .NET 10, SQL Server (Express en Windows; en Mac, el contenedor que ya usan), `dotnet tool install --global dotnet-ef`.
- **Solo Mateo**, para el firmware: `arduino-cli` (https://arduino.github.io/arduino-cli/latest/installation/). El agente instala el núcleo y las bibliotecas en la misión S23-M16.

### 1.2 Actualizar el repositorio
```bash
git checkout main
git pull
```

### 1.3 Cómo se sube el trabajo (lo hace el agente)
Todos trabajan sobre `main`, uno después del otro.
```bash
git pull                      # siempre antes de empezar
# ... trabajo de la misión ...
git add -A
git commit -m "S23-M<n>: <qué hace> (<Nombre>)"
git push
```
- Un commit por misión como mínimo.
- Si `git push` es rechazado: `git pull --rebase`, resolver y volver a hacer push. Nunca `--force`.
- Las migraciones de EF Core **sí** se suben (`src/SAGE.Web/Data/Migrations`).

---

## 2. Stack y estructura

El stack del Sprint 1 no cambia: Blazor Web App .NET 10 (interactividad Server global), MudBlazor 9, SQL Server + EF Core 10, ASP.NET Core Identity con roles, ClosedXML, consola .NET 10 para el simulador. Se agregan:

| Pieza | Elección |
|---|---|
| Pruebas | Proyecto xUnit `tests/SAGE.Tests` (lo crea Harold en S23-M1) |
| Firmware | Arduino C++ para ESP32 (núcleo `esp32:esp32` 3.x), bibliotecas `Adafruit INA219` y `ArduinoJson` 7.x |

```text
SAGE/
├─ GUIA_TRABAJO_SPRINT_2_3.md         ← este archivo
├─ docs/                              ← solo lectura (los documentos de patrones están en docs/patrones/)
├─ src/
│  ├─ SAGE.Web/
│  │  ├─ Api/                         ← endpoints HTTP (telemetría, facturas)
│  │  ├─ Components/Pages/<Módulo>/   ← una carpeta por módulo
│  │  ├─ Data/                        ← entidades, ApplicationDbContext, Migrations, DatosIniciales
│  │  └─ Servicios/<Área>/            ← NUEVO: lógica y patrones (Seguridad, Telemetria, EnVivo, Alertas,
│  │                                     Ordenes, Dispositivo, Energia, Simulacion, Reportes)
│  └─ SAGE.Simulador/                 ← consola que imita al ESP32 (Ethan la reescribe en S23-M11)
├─ tests/SAGE.Tests/                  ← NUEVO: pruebas xUnit
└─ firmware/sage_esp32/               ← NUEVO: firmware del ESP32 (Mateo, S23-M16)
```

El `DbContext` se llama `ApplicationDbContext` (en los documentos de patrones aparece como `SageDbContext`; es el mismo).

---

## 3. Decisiones ya tomadas (no se discuten, se implementan)

Estas decisiones corrigen o completan `docs/`. Si un documento dice otra cosa, vale esto.

### 3.1 Roles y empresas (corrige el Sprint 1)
En el Sprint 1 el Administrador era global (sin empresa) y veía todo. **Eso cambia:**

- Hay **4 roles**, con estos nombres exactos: `Operador`, `Administrador`, `Tecnico`, `Consulta`. En pantalla, `Operador` se muestra como **"Operador SAGE"** y `Tecnico` como **"Técnico"**.
- **Operador SAGE** = el equipo de SAGE como proveedor del servicio. Crea empresas y crea administradores. **No ve datos de energía** de ninguna empresa (ni dispositivos, ni mediciones, ni facturas, ni simulaciones). Mantiene los parámetros económicos globales.
- **Administrador** = el responsable de una empresa cliente. **No crea empresas.** Puede pertenecer a **varias empresas**. Crea empleados (otro Administrador, Técnico o Consulta) para una de **sus** empresas.
- **Técnico** y **Consulta** pertenecen a **una sola** empresa.
- Todo usuario creado (o con contraseña restablecida) recibe una **contraseña temporal** escrita por quien lo crea, y **debe cambiarla en su primer inicio de sesión**. Hasta cambiarla no puede usar ninguna otra pantalla.
- **Datos separados:** un usuario con varias empresas trabaja siempre sobre **una empresa activa**, elegida en la barra superior. Todas las pantallas muestran solo datos de la empresa activa. La única vista que junta empresas es la pestaña **"Todas mis empresas"** del Inicio, que muestra **una fila por empresa** (nunca listas de mediciones, dispositivos o facturas mezcladas).
- Solo el Operador desactiva empresas y administradores. Desactivar no borra nada.

| Acción | Operador | Administrador | Tecnico | Consulta |
|---|---|---|---|---|
| Empresas: ver, crear, editar, desactivar | ✔ | ✘ | ✘ | ✘ |
| Administradores: crear (con empresa), vincular a otra empresa, desvincular, desactivar, restablecer contraseña | ✔ | solo **crear**, para una de sus empresas | ✘ | ✘ |
| Técnicos y Consulta: crear, editar, desactivar, restablecer contraseña | ✘ | ✔ (de sus empresas) | ✘ | ✘ |
| Parámetros económicos globales | ✔ | ✘ | ✘ | ✘ |
| Tarifa propia de la empresa | ✘ | ✔ | ✘ | ✘ |
| Dispositivos: crear, editar, token, factor de escala | ✘ | ✔ | ✘ | ✘ |
| Ver dispositivos, monitoreo, mediciones, historial, eventos, alertas, ensayos, facturas | ✘ | ✔ | ✔ | ✔ |
| Enviar órdenes de modo y de parámetros | ✘ | ✔ | ✔ | ✘ |
| Registrar facturas (a mano o Excel) | ✘ | ✔ | ✔ | ✘ |
| Simular y guardar simulaciones | ✘ | ✔ | ✔ | solo ver las guardadas |
| Crear y finalizar ensayos | ✘ | ✔ | ✔ | ✘ |
| Exportar historial a Excel | ✘ | ✔ | ✔ | ✔ |

### 3.2 Tiempo real
- El dispositivo (ESP32 y simulador) envía telemetría **cada 1 s** (el diseño decía 2 s).
- La plataforma **no consulta la base cada X segundos** para refrescar pantallas: cuando llega una medición, la empuja a las pantallas abiertas con el patrón Observer (`EstadoDispositivosEnVivo`, sección 6). Meta: **≤ 2 s** entre que el dispositivo mide y la pantalla cambia (RF10 pide ≤ 5 s).
- Se actualizan en vivo: Inicio, Monitoreo, Mediciones, Dispositivos (última conexión), indicador de alertas y estado de las órdenes.

### 3.3 Escala de la maqueta (×2000)
- Cada dispositivo tiene `FactorEscala` (decimal, 1 por defecto, entre 1 y 10 000), editable por el Administrador.
- **Solo se multiplican potencia y energía**, y **solo al mostrar**. Tensión y corriente se muestran siempre reales. La base guarda siempre el valor real.
- Si `FactorEscala ≠ 1`, junto a cada valor escalado se muestra el chip **"Maqueta ×2000"** (con el factor real). En las exportaciones van las dos columnas: real y escalada.
- La simulación económica **nunca** usa mediciones escaladas: usa las facturas.

### 3.4 Simulación económica (RF05–RF07)
Tres escenarios sobre el consumo facturado de la empresa activa:
- **Red:** todo el consumo se paga a la tarifa.
- **Solar:** la potencia solar (kWp) que cubre el 100 % del consumo del año. No hay baterías: "solar" significa "genera en el año lo que se consume en el año", no "desconectado de la red".
- **Mixto:** la potencia que elige el usuario (menor que la del escenario Solar); lo que falte se paga a la red.

Fórmulas exactas en S23-M4. Los parámetros globales los mantiene el Operador; cada empresa puede fijar su propia tarifa. Valores iniciales (todos marcados **"Supuesto — verificar"** en pantalla):

| Parámetro | Valor inicial | Rango permitido |
|---|---|---|
| Tarifa (Bs/kWh) | 0,90 | 0,10 a 5,00 |
| Costo de instalación (Bs/kWp) | 8 000 | 1 000 a 50 000 |
| Generación solar (kWh/kWp/mes) | 125 | 50 a 250 |
| Mantenimiento (Bs/año) | 600 | 0 a 50 000 |

### 3.5 Demo con el ESP32
El servidor corre en una laptop. Laptop y ESP32 se conectan al **hotspot de un celular**. El ESP32 usa **HTTP** (no HTTPS) hacia la IP de la laptop. Es una limitación aceptada de la maqueta: el token sigue protegiendo el acceso.

### 3.6 Huecos del diseño que esta guía completa
- **Salida de `SIN_ALIMENTACION`:** con el relé en NC, si el cargador vuelve a ≥ V_min durante T_falla → `EN_CARGADOR` (motivo `cargador_recuperado`). Si el cargador sigue mal pero solar ≥ V_retorno → `PROBANDO_SOLAR` (motivo `cargador_sin_tension`).
- **Orden Solar aceptada:** se informa `ejecutada` en cuanto el relé pasa a solar (estado `PROBANDO_SOLAR`). Si la prueba falla después, el dispositivo envía el evento `conmutacion` con motivo `orden_fallida` (así se cumple RNF10 ≤ 5 s).
- **"Solar en respaldo"** (modo Solar con falla) no es un valor nuevo: es `modo = SOLAR` con `fuente_seleccionada = CARGADOR`. La interfaz lo muestra como "Solar en respaldo".
- **Orden entregada sin resultado:** si una orden queda `entregada` más de 60 s sin resultado, pasa a `expirada` con motivo `sin_confirmacion`.

---

## 4. Contrato dispositivo ↔ servidor, versión 2 (reemplaza §6.2 del diseño)

`POST /api/dispositivo/telemetria`, encabezado `Authorization: Bearer <token>`, `Content-Type: application/json`, **cada 1 s**.

```json
{
  "boot_id": 12, "seq": 4381, "ts": "2026-11-20T14:03:22Z", "ts_valida": true,
  "modo": "AUTO", "estado": "EN_SOLAR",
  "fuente_seleccionada": "SOLAR", "alimentacion_comprobada": true,
  "parametros_version": 3,
  "solar":    { "v": 4.93, "i_ma": 196.2, "p_mw": 967, "wh_acum": 3.412 },
  "cargador": { "v": 5.08, "i_ma": 0.4,   "p_mw": 2,   "wh_acum": 1.105 },
  "eventos": [
    { "seq": 4379, "ts": "2026-11-20T14:03:20Z", "tipo": "conmutacion", "estado": "PROBANDO_SOLAR",
      "de": "CARGADOR", "a": "SOLAR", "motivo": "intento_retorno_solar", "origen": "regla", "orden_id": null }
  ],
  "resultados_ordenes": [
    { "orden_id": 77, "resultado": "rechazada", "motivo": "solar_no_disponible" }
  ]
}
```

Cambios respecto de la versión 1: envío cada 1 s; campo nuevo `parametros_version` (opcional, entero; si falta vale 0); los eventos traen `estado`; los resultados de órdenes traen `motivo`.

**Respuesta 200:**
```json
{ "hora_servidor": "2026-11-20T14:03:22.512Z", "orden": null }
{ "hora_servidor": "...", "orden": { "id": 78, "tipo": "modo", "valor": "SOLAR" } }
{ "hora_servidor": "...", "orden": { "id": 79, "tipo": "parametros", "valor": {
    "version": 4, "v_min": 4.50, "v_retorno": 4.85, "i_min_ma": 20,
    "t_falla_s": 1.0, "t_estable_s": 30, "t_prueba_s": 5 } } }
```
`401` sin token, token inválido, dispositivo o empresa inactivos. `400` si el JSON es inválido o le faltan campos obligatorios (como en el Sprint 1).

### 4.1 Valores exactos permitidos

| Campo | Valores |
|---|---|
| `modo` | `AUTO`, `SOLAR`, `CARGADOR` (en pantalla: Automático, Solar, Red) |
| `estado` (paquete y evento) | `EN_CARGADOR`, `PROBANDO_SOLAR`, `EN_SOLAR`, `SIN_ALIMENTACION` |
| `fuente_seleccionada`, `de`, `a` | `SOLAR`, `CARGADOR` (en pantalla: Solar, Red); en eventos `de`/`a` pueden ser `null` |
| `eventos[].tipo` | `conmutacion` (cambió la posición del relé), `estado` (cambió el estado sin mover el relé), `alerta` (falla sin cambio de estado), `reinicio` (arranque) |
| `eventos[].origen` | `regla`, `orden`, `arranque` |
| `eventos[].motivo` | `intento_retorno_solar`, `cargador_sin_tension`, `solar_comprobada`, `prueba_solar_fallida`, `solar_insuficiente`, `sin_fuente_valida`, `cargador_recuperado`, `orden_modo`, `orden_fallida`, `arranque` |
| `resultados_ordenes[].resultado` | `ejecutada`, `rechazada` |
| `resultados_ordenes[].motivo` | `null` si se ejecutó; si se rechazó: `solar_no_disponible`, `parametros_invalidos`, `orden_desconocida` |
| `orden.tipo` (respuesta) | `modo` (valor: `AUTO`, `SOLAR`, `CARGADOR`) o `parametros` (valor: objeto de arriba) |

**Qué evento genera cada situación** (lo cumplen el simulador y el firmware):

| Situación | tipo | de → a | estado | motivo | origen |
|---|---|---|---|---|---|
| Arranque | `reinicio` | null → `CARGADOR` | `EN_CARGADOR` | `arranque` | `arranque` |
| EN_CARGADOR → PROBANDO_SOLAR por espera cumplida | `conmutacion` | `CARGADOR` → `SOLAR` | `PROBANDO_SOLAR` | `intento_retorno_solar` | `regla` |
| EN_CARGADOR o SIN_ALIMENTACION → PROBANDO_SOLAR por cargador caído | `conmutacion` | `CARGADOR` → `SOLAR` | `PROBANDO_SOLAR` | `cargador_sin_tension` | `regla` |
| PROBANDO_SOLAR → EN_SOLAR | `estado` | null → null | `EN_SOLAR` | `solar_comprobada` | `regla` |
| PROBANDO_SOLAR → EN_CARGADOR (Automático) | `conmutacion` | `SOLAR` → `CARGADOR` | `EN_CARGADOR` | `prueba_solar_fallida` | `regla` |
| PROBANDO_SOLAR → EN_CARGADOR (modo Solar por orden) | `conmutacion` | `SOLAR` → `CARGADOR` | `EN_CARGADOR` | `orden_fallida` | `orden` (con `orden_id`) |
| EN_SOLAR → EN_CARGADOR | `conmutacion` | `SOLAR` → `CARGADOR` | `EN_CARGADOR` | `solar_insuficiente` | `regla` |
| Cualquiera → SIN_ALIMENTACION | `conmutacion` si el relé estaba en solar, si no `estado` | `SOLAR` → `CARGADOR` o null → null | `SIN_ALIMENTACION` | `sin_fuente_valida` | `regla` |
| SIN_ALIMENTACION → EN_CARGADOR | `estado` | null → null | `EN_CARGADOR` | `cargador_recuperado` | `regla` |
| Orden de modo que mueve el relé | `conmutacion` | según el caso | estado nuevo | `orden_modo` | `orden` (con `orden_id`) |
| Modo Cargador y el cargador cae (no se cambia de fuente) | `alerta` | null → null | estado actual | `cargador_sin_tension` | `regla` |

---

## 5. Mapa de patrones en el código

Cuando todo esté hecho, cada patrón presentado existe en estas clases (regla 11 de la sección 0):

| Patrón | Clases (rol) | Dónde | Misión |
|---|---|---|---|
| Builder | `SimulacionBuilder` (constructor), `Simulacion` (producto), `PantallaSimulacion` (director) | `Servicios/Simulacion/`, `Components/Pages/Simulacion/` | S23-M4 Harold |
| Adapter | `IAdaptadorTelemetria` (objetivo), `AdaptadorTelemetriaEsp32` (adaptador), `TelemetriaRequest` (adaptado) | `Servicios/Telemetria/` | S23-M5 Matías |
| Facade | `FachadaSincronizacion` (fachada) | `Servicios/Telemetria/` | S23-M5 Matías |
| Singleton (.NET) | `EstadoDispositivosEnVivo` (registrado con `AddSingleton`) | `Servicios/EnVivo/` | S23-M6 Matías |
| Observer | `EstadoDispositivosEnVivo` (sujeto), `PanelMonitoreo`, `Mediciones`, `Home`, `IndicadorAlertas` (observadores) | `Servicios/EnVivo/`, `Components/` | S23-M6 y S23-M7 Matías |
| Command | `Orden` (comando), `ICanalOrdenes` / `CanalOrdenesBD` (invocador del lado servidor); en simulador y firmware: `Comando`, `ComandoModo`, `ComandoParametros`, `ProcesadorOrdenes` | `Data/`, `Servicios/Ordenes/`, simulador, firmware | S23-M9, S23-M10, S23-M11 Ethan; S23-M16 Mateo |
| State | `EstadoFuente`, `EnCargador`, `ProbandoSolar`, `EnSolar`, `SinAlimentacion`, contexto `ControladorFuente` | simulador (C#) y firmware (C++) | S23-M11 Ethan; S23-M16 Mateo |
| Proxy | `CanalOrdenesProtegido`, `LectorTelemetriaProtegido` (proxies de protección) | `Servicios/Dispositivo/` | S23-M13 Mateo |
| Abstract Factory | `IFabricaDispositivo`, `FabricaDispositivoReal`, `FabricaDispositivoSimulado`; productos `ILectorTelemetria`, `ICanalOrdenes` | `Servicios/Dispositivo/` | S23-M13 Mateo |
| Singleton (firmware) | `ControladorFuente::instancia()` | firmware | S23-M16 Mateo |

---

## 6. Piezas compartidas (se crean una vez y los demás las usan)

Para que nadie las invente dos veces, estas piezas tienen dueño y forma fija:

| Pieza | Dueño | Forma |
|---|---|---|
| `IUsuarioActual` / `UsuarioActual` | Harold, S23-M2 | ver S23-M2 |
| `Formato` (horas locales, potencia, energía, escala) | Matías, S23-M6 | ver S23-M6 |
| `EstadoDispositivosEnVivo` y `EstadoDispositivo` | Matías, S23-M6 | ver S23-M6 |
| `ParametrosFuente` (V_min, etc.) | Matías crea los valores por defecto en S23-M7; Ethan agrega versión y edición en S23-M10 | ver S23-M7 y S23-M10 |
| `CalculadoraEnergia` | Mateo, S23-M14 | ver S23-M14 |

---

## 7. Harold — Multiempresa, roles, contraseña temporal y simulación económica

### Verificación de entrada
- `git pull`, `dotnet build SAGE.slnx` sin errores y la app arranca.
- Los usuarios de prueba del Sprint 1 inician sesión (`admin@sage.test`, `tecnico@sage.test`, `consulta@sage.test`, `tecnico.palmas@sage.test`, contraseña `Sage2026!`).
- Con el simulador del Sprint 1 y un token de dispositivo, se guardan mediciones.
- **MudBlazor en Mac:** el arreglo de assets (`@Assets["_content/MudBlazor/..."]`) ya está en `main`. `appsettings.Development.json` debe quedar **sin** `ConnectionStrings` (regla 12). Si reaparecen estilos vacíos, revisá `Components/App.razor`.

### - [x] S23-M1 · Modelo multiempresa, rol Operador y proyecto de pruebas
**Qué hacer**
1. Proyecto de pruebas: `dotnet new xunit -n SAGE.Tests -o tests/SAGE.Tests`, agregarlo a `SAGE.slnx`, con referencias a `src/SAGE.Web` y `src/SAGE.Simulador`. Una prueba trivial que pase.
2. Entidad nueva `UsuarioEmpresa` en `Data/UsuarioEmpresa.cs`: `UsuarioId` (string, FK a `ApplicationUser`), `EmpresaId` (int, FK a `Empresa`), `FechaVinculo` (DateTime UTC). Clave primaria compuesta `(UsuarioId, EmpresaId)`. Tabla `UsuarioEmpresas`. Borrado: en cascada desde el usuario, `Restrict` desde la empresa.
3. En `ApplicationUser`:
   - **Quitar** `EmpresaId` y `Empresa`.
   - Agregar `ICollection<UsuarioEmpresa> Empresas`, `bool DebeCambiarContrasena` (false por defecto) y `int? UltimaEmpresaId` (la última empresa activa elegida; sin FK).
   - En `Empresa`, reemplazar `ICollection<ApplicationUser> Usuarios` por `ICollection<UsuarioEmpresa> Usuarios`.
4. Migración `MultiempresaYOperador`. **Antes** de borrar la columna `EmpresaId` de `AspNetUsers`, copia los vínculos existentes con SQL en la propia migración:
   ```csharp
   migrationBuilder.Sql(@"INSERT INTO UsuarioEmpresas (UsuarioId, EmpresaId, FechaVinculo)
                          SELECT Id, EmpresaId, SYSUTCDATETIME() FROM AspNetUsers WHERE EmpresaId IS NOT NULL");
   ```
   El orden dentro de `Up()` es: crear tabla → copiar → quitar FK, índice y columna. `Down()` hace lo inverso (recrea la columna y copia un vínculo por usuario con `MIN(EmpresaId)`).
5. Rol nuevo `Operador`: constante `DatosIniciales.RolOperador = "Operador"`.
6. `DatosIniciales` (solo Development) deja exactamente estos usuarios, todos con contraseña `Sage2026!`, correo confirmado, activos y `DebeCambiarContrasena = false`:

   | Correo | Rol | Empresas |
   |---|---|---|
   | operador@sage.test | Operador | — |
   | admin@sage.test | Administrador | Energía Andina S.R.L. **y** Hotel Las Palmas |
   | admin.palmas@sage.test | Administrador | Hotel Las Palmas |
   | tecnico@sage.test | Tecnico | Energía Andina S.R.L. |
   | consulta@sage.test | Consulta | Energía Andina S.R.L. |
   | tecnico.palmas@sage.test | Tecnico | Hotel Las Palmas |

   `admin@sage.test` deja de ser global: si ya existía, se le asignan sus 2 empresas. El seed es idempotente: correrlo dos veces no duplica vínculos.
7. Reglas de inicio de sesión (en `Components/Account/Pages/Login.razor`, donde hoy se revisan `Activo` y la empresa):
   - `Activo = false` → no entra (ya existe).
   - Si el usuario **no** es Operador y **no tiene ninguna empresa activa** vinculada → no entra, con el mensaje "Su empresa está desactivada. Contacte a SAGE."
8. Arreglar todo lo que deje de compilar por quitar `ApplicationUser.EmpresaId`. Donde se usaba, usar provisoriamente el primer vínculo; en S23-M2 todo pasa a `IUsuarioActual`.
9. Ajustar `Program.cs`: el bloque `--importar-demo-tecnico` usa el primer vínculo del técnico.

**Criterios de aceptación**
- [x] `dotnet build` y `dotnet test` sin errores; la migración está subida.
- [x] En una base con datos del Sprint 1, después de `dotnet ef database update` cada Técnico y Consulta conserva su empresa en `UsuarioEmpresas` (comprobado con una consulta SQL).
- [x] Los 6 usuarios de la tabla inician sesión; `admin@sage.test` tiene 2 filas en `UsuarioEmpresas`.
- [x] Un Técnico de una empresa desactivada no puede iniciar sesión y ve el mensaje indicado.

### - [x] S23-M2 · Usuario actual, empresa activa y filtros por empresa
**Qué hacer**
1. `Servicios/Seguridad/IUsuarioActual.cs` y `UsuarioActual.cs` (registrado `AddScoped<IUsuarioActual, UsuarioActual>()`; en Blazor Server hay una instancia por circuito). Contrato exacto:
   ```csharp
   public interface IUsuarioActual
   {
       string? UsuarioId { get; }
       string NombreCompleto { get; }
       bool EsOperador { get; }
       bool EsAdministrador { get; }
       bool EsTecnico { get; }
       bool EsConsulta { get; }
       bool DebeCambiarContrasena { get; }
       IReadOnlyList<EmpresaOpcion> Empresas { get; }   // solo empresas ACTIVAS vinculadas, ordenadas por nombre
       int? EmpresaActivaId { get; }                     // null solo para el Operador o si no tiene empresas
       EmpresaOpcion? EmpresaActiva { get; }
       bool EnRol(string rol);
       bool PuedeVerEmpresa(int empresaId);             // false para el Operador; true si la empresa está en Empresas
       Task CargarAsync();                               // idempotente: carga una sola vez por circuito
       Task SeleccionarEmpresaAsync(int empresaId);      // valida PuedeVerEmpresa, guarda UltimaEmpresaId y dispara EmpresaCambiada
       event Action? EmpresaCambiada;
   }
   ```
   - `EmpresaOpcion` ya existe en `Data/Factura.cs` (`record EmpresaOpcion(int Id, string Nombre)`); se reutiliza.
   - `CargarAsync` lee el usuario desde `AuthenticationStateProvider` y la base. La empresa activa inicial es `UltimaEmpresaId` si sigue siendo válida; si no, la primera de la lista.
   - `SeleccionarEmpresaAsync` con una empresa no permitida lanza `UnauthorizedAccessException`.
2. Selector de empresa en `MainLayout.razor`, en la barra superior, a la izquierda del nombre del usuario:
   - Si el usuario tiene **1** empresa: se muestra su nombre como texto (chip), sin selector.
   - Si tiene **2 o más**: `MudSelect` con sus empresas.
   - El Operador no ve selector (muestra el chip "Operador SAGE").
   - Al cambiar de empresa, la página actual se vuelve a crear desde cero: envuelve `@Body` en `<div @key="UsuarioActual.EmpresaActivaId">@Body</div>`.
3. **Filtrar todo por la empresa activa.** Recorre cada página y diálogo y reemplaza cualquier uso de la empresa del usuario o del filtro "todas las empresas" del Administrador por `UsuarioActual.EmpresaActivaId`:
   - `Dispositivos/Index.razor` y `DispositivoDialog.razor`: lista solo dispositivos de la empresa activa; el diálogo **ya no tiene** selector de empresa (usa la activa).
   - `Facturas/Index.razor`, `FacturaDialog.razor`, `ImportarFacturasDialog.razor`: igual; se quitan los selectores de empresa del Administrador.
   - `Mediciones.razor`, `Home.razor`: solo dispositivos de la empresa activa.
   - Antes de guardar (dispositivo, factura), vuelve a comprobar `PuedeVerEmpresa(EmpresaActivaId)` en el servidor.
4. Menú (`NavMenu.razor`) según la tabla de la sección 3.1. El Operador ve: Inicio, Empresas, Usuarios y Parámetros económicos (esta última opción se activa en S23-M4). Administrador, Técnico y Consulta ven: Inicio, Dispositivos, Mediciones, Facturas. (Los demás compañeros agregan sus opciones.)
5. Todas las páginas de datos de empresa llevan `@attribute [Authorize(Roles = "Administrador,Tecnico,Consulta")]` (el Operador recibe "Acceso denegado").
6. Pruebas en `tests/SAGE.Tests/UsuarioActualTests.cs`: `PuedeVerEmpresa` es false para el Operador y para una empresa no vinculada (puedes probar la lógica extrayéndola a un método estático o usando una base en memoria de EF; elige uno y anótalo).

**Criterios de aceptación**
- [x] `admin@sage.test` ve el selector con 2 empresas; al cambiar, Dispositivos, Facturas y Mediciones muestran solo datos de la empresa elegida, sin recargar el navegador.
- [x] Al cerrar sesión y volver a entrar, `admin@sage.test` vuelve a la última empresa elegida.
- [x] `admin.palmas@sage.test` no ve el selector y nunca ve datos de Energía Andina, ni escribiendo URLs a mano.
- [x] `operador@sage.test` recibe "Acceso denegado" en `/dispositivos`, `/facturas` y `/mediciones`.
- [x] Un dispositivo y una factura creados quedan en la empresa activa.

### - [x] S23-M3 · Empresas, usuarios y contraseña temporal
**Qué hacer**
1. `/empresas` pasa a ser **solo del Operador** (`[Authorize(Roles = "Operador")]`). Agregar la columna "Administradores" (cantidad). Al desactivar una empresa, el diálogo de confirmación avisa: "Sus usuarios que no tengan otra empresa activa no podrán iniciar sesión."
2. `/usuarios` (`[Authorize(Roles = "Operador,Administrador")]`) con dos comportamientos según el rol:
   - **Operador:** lista **solo usuarios con rol Administrador** de todas las empresas. Columnas: Nombre, Correo, Empresas (un chip por empresa), Estado. Acciones:
     - "Nuevo administrador": Nombre completo, Correo, Contraseña temporal, Empresa (`MudSelect` de empresas activas, obligatoria).
     - "Vincular a empresa": elige una empresa activa a la que todavía no pertenece.
     - "Desvincular" una empresa: solo si el administrador tiene 2 o más empresas (si tiene una sola, el botón no aparece; para dejarlo sin acceso se lo desactiva).
     - Desactivar/Activar, Restablecer contraseña.
   - **Administrador:** lista los usuarios **de la empresa activa** (todos los roles excepto Operador). Acciones:
     - "Nuevo usuario": Nombre completo, Correo, Contraseña temporal, Rol (`Administrador`, `Tecnico` o `Consulta`) y Empresa (`MudSelect` con **sus** empresas, por defecto la activa).
     - Sobre Técnicos y Consulta de la empresa activa: Editar (nombre y rol entre `Tecnico` y `Consulta`), Desactivar/Activar, Restablecer contraseña.
     - Sobre otros Administradores: **solo lectura** (sin botones), con el texto "Lo gestiona SAGE".
     - Un usuario no puede desactivarse ni cambiarse el rol a sí mismo.
   - El correo es único en todo el sistema. Errores de Identity en español.
3. **Contraseña temporal.** Al crear un usuario o al restablecer su contraseña: `DebeCambiarContrasena = true`. La contraseña temporal cumple la misma política de Identity (8+ caracteres, mayúscula, minúscula, dígito y símbolo).
4. Página nueva `Components/Account/Pages/CambiarContrasenaInicial.razor`, ruta `/Account/CambiarContrasenaInicial`, estática (SSR) como las demás de `Account` (toma como base `Manage/ChangePassword.razor`):
   - Campos: Contraseña temporal, Nueva contraseña, Confirmar nueva contraseña.
   - La nueva debe ser distinta de la temporal.
   - Al guardar: `UserManager.ChangePasswordAsync`, `DebeCambiarContrasena = false`, `SignInManager.RefreshSignInAsync` y redirigir a `/`.
5. Obligar el cambio, en dos lugares (los dos son necesarios):
   - **Middleware** en `Program.cs`, después de `UseAuthorization()`: si el usuario autenticado tiene `DebeCambiarContrasena = true` y la ruta **no** empieza por `/Account`, `/_framework`, `/_content`, `/_blazor`, `/api` ni es un archivo estático, redirige a `/Account/CambiarContrasenaInicial`.
   - **`MainLayout.razor`**: después de `UsuarioActual.CargarAsync()`, si `DebeCambiarContrasena`, `NavigationManager.NavigateTo("/Account/CambiarContrasenaInicial", forceLoad: true)`.
6. Pruebas: una prueba de la regla "el Administrador solo puede asignar empresas propias" (extrae la validación a un método estático en `Servicios/Seguridad/ReglasUsuarios.cs` y pruébalo).

**Criterios de aceptación**
- [x] El Operador crea la empresa "Panadería Sol" y un administrador para ella. Ese administrador entra, está obligado a cambiar la contraseña y después ve solo Panadería Sol.
- [x] El Operador vincula `admin.palmas@sage.test` a Panadería Sol: al volver a entrar, ese admin ve el selector con 2 empresas.
- [x] Con un usuario que debe cambiar la contraseña, escribir `/dispositivos` en la barra lleva a la página de cambio.
- [x] El Administrador crea un Técnico para una de sus empresas; no puede elegir una empresa que no es suya (el selector no la muestra, y forzarla desde el servidor da error).
- [x] El Administrador no tiene botones sobre otro Administrador; el Operador sí.
- [x] Ni el Administrador ni el Técnico entran a `/empresas` ("Acceso denegado").

### - [x] S23-M4 · RF05–RF07 Parámetros económicos y simulación (patrón Builder)
**Qué hacer**
1. Entidades:
   - `ParametrosEconomicos` (versionados, globales): `Id`, `Version` (int, único, empieza en 1), `TarifaBsKwh`, `CostoInstalacionBsKwp`, `GeneracionKwhKwpMes`, `MantenimientoBsAnio` (decimales, `Precision(18,4)`), `VigenteDesde` (UTC), `CreadoPorId`. Seed: versión 1 con los valores de la sección 3.4.
   - `TarifaEmpresa` (versionada por empresa): `Id`, `EmpresaId`, `TarifaBsKwh`, `VigenteDesde`, `CreadoPorId`. La vigente es la de `VigenteDesde` más reciente.
   - `Simulacion` (guardada): `Id`, `EmpresaId`, `UsuarioId`, `Fecha`, `ParametrosVersion`, `TarifaUsadaBsKwh`, `ConsumoMensualKwh`, `MesesBase`, `KwpSolar`, `KwpMixto`, `HorizonteAnios`, `InversionSolarBs`, `InversionMixtoBs`, `CostoAnualRedBs`, `CostoAnualSolarBs`, `CostoAnualMixtoBs`, `CostoAcumRedBs`, `CostoAcumSolarBs`, `CostoAcumMixtoBs`, `RecuperacionSolarAnios` (decimal?, null = no se recupera en el horizonte), `RecuperacionMixtoAnios` (decimal?), `Advertencias` (`List<string>` guardada como texto unido por `\n` con un `ValueConverter`).
   - Migración `ParametrosYSimulacion`.
2. Página `/parametros` (`Operador`): muestra la versión vigente y el historial de versiones; "Nueva versión" con los 4 valores, validados con los rangos de la sección 3.4; guardar crea una versión nueva (nunca se edita una anterior). Encima, el aviso "Supuesto — verificar con la tarifa vigente de la AETN/CRE" mientras la versión vigente sea la 1.
3. En `/simulacion`, el Administrador ve la tarjeta "Tarifa de la empresa" con la tarifa propia vigente (o "Usa la tarifa general: X Bs/kWh") y un botón para fijar una nueva (rango 0,10 a 5,00).
4. **Builder** en `Servicios/Simulacion/SimulacionBuilder.cs`, con esta interfaz exacta:
   ```csharp
   // Patrón: Builder — rol: constructor
   public class SimulacionBuilder
   {
       public SimulacionBuilder ParaEmpresa(Empresa empresa);
       public SimulacionBuilder ConConsumo(IEnumerable<Factura> facturas);       // últimas 12 como máximo
       public SimulacionBuilder ConParametros(ParametrosEconomicos parametros);
       public SimulacionBuilder ConTarifa(decimal tarifaBsKwh);                  // la de la empresa o la general
       public SimulacionBuilder ConPotenciaMixta(decimal kwp);
       public SimulacionBuilder ConHorizonte(int anios);
       public Simulacion Construir();
   }
   ```
   `Construir()` valida y lanza excepción con mensaje en español si: falta empresa o parámetros; no hay facturas ("No hay consumo facturado: registre al menos una factura"); horizonte fuera de 1 a 25; kWp mixto ≤ 0 o ≥ kWp solar ("Para cubrir todo el consumo use el escenario Solar").
5. **Fórmulas exactas** (C = consumo mensual promedio en kWh de las facturas usadas; T = tarifa; G = generación kWh/kWp/mes; K = costo Bs/kWp; M = mantenimiento Bs/año; N = horizonte; P = kWp mixto):
   - `KwpSolar = ceil((C / G) × 10) / 10` (redondeo hacia arriba a 0,1 kWp).
   - Red: `CostoAnualRed = 12 × C × T`; `CostoAcumRed = N × CostoAnualRed`.
   - Solar: `InversionSolar = KwpSolar × K`; `CostoAnualSolar = M`; `CostoAcumSolar = InversionSolar + N × M`.
   - Mixto: `kWhRedAnual = max(0, 12 × C − 12 × G × P)`; `InversionMixto = P × K`; `CostoAnualMixto = kWhRedAnual × T + M`; `CostoAcumMixto = InversionMixto + N × CostoAnualMixto`.
   - Recuperación de un escenario: `ahorroAnual = CostoAnualRed − CostoAnualEscenario`. Si `ahorroAnual ≤ 0` o `Inversion / ahorroAnual > N` → `null`; si no → `Inversion / ahorroAnual` redondeado a 1 decimal.
   - Advertencias: menos de 3 meses → "Menos de 3 meses de consumo: baja confiabilidad". De 3 a 11 meses → "Menos de 12 meses de consumo: el resultado es orientativo".
   - Supuestos que se muestran siempre: sin inflación, sin degradación del panel, sin baterías, excedentes solares no se venden, costos en Bs de hoy, **los datos de la maqueta no se usan**.
6. Página `/simulacion` (Admin, Técnico, Consulta), componente `PantallaSimulacion` (es el **director** del Builder: `// Patrón: Builder — rol: director`):
   - Admin y Técnico: entrada de kWp mixto y horizonte (por defecto 3 kWp y 10 años), botón "Simular", resultado y botón "Guardar".
   - Resultado: chip grande **"SIMULACIÓN"** (RNF12); tabla de 3 escenarios (kWp, inversión Bs, costo anual Bs, costo acumulado a N años Bs, ahorro acumulado frente a Red Bs, recuperación en años o "No se recupera en el horizonte"); gráfico de líneas del costo acumulado año por año de los 3 escenarios (`MudChart` tipo `Line`); lista de supuestos con los valores usados y la versión de parámetros.
   - Lista de simulaciones guardadas de la empresa activa (Consulta solo ve esta lista y puede abrirlas).
7. Pruebas en `tests/SAGE.Tests/SimulacionBuilderTests.cs`: un caso calculado a mano (C = 1 000 kWh, T = 0,90, G = 125, K = 8 000, M = 600, N = 10, P = 3) con todos los resultados comprobados; y cada validación de `Construir()`.

**Criterios de aceptación**
- [x] `dotnet test` pasa con el caso calculado a mano.
- [x] Con las facturas de ejemplo de Hotel Las Palmas (impórtalas con `docs/ejemplos/facturas_validas.xlsx`), la simulación muestra los 3 escenarios, el chip SIMULACIÓN, los supuestos y el gráfico.
- [x] Una empresa sin facturas muestra el mensaje de error del Builder, no una excepción.
- [x] Cambiar la tarifa de la empresa cambia el resultado de la siguiente simulación, sin modificar código (RF05, RNF06); una simulación guardada antes conserva sus valores.
- [x] El Operador crea la versión 2 de parámetros; un valor fuera de rango se rechaza con mensaje.

**Al terminar:** marca S23-M1 a S23-M4, escribe tu entrada en la Bitácora y haz push. Avísale a Matías.

---

## 8. Matías — Telemetría, tiempo real y alertas

### Verificación de entrada
- `git pull`, `dotnet ef database update --project src/SAGE.Web`, `dotnet build SAGE.slnx`, `dotnet test`: todo sin errores.
- Los criterios de S23-M2 y S23-M3 se cumplen (prueba al menos el selector de `admin@sage.test` y el cambio obligatorio de contraseña).
- Con el simulador actual se guardan mediciones en la empresa del dispositivo.

### - [ ] S23-M5 · Recepción de telemetría con Adapter y Facade, y eventos (RF13)
**Qué hacer**
1. Entidad `Evento` en `Data/Evento.cs`: `Id` (long), `DispositivoId`, `BootId` (long), `Seq` (long), `Ts` (UTC), `TsValida`, `TsServidor`, `Tipo`, `Estado`, `De` (nullable), `A` (nullable), `Motivo`, `Origen` (strings, máx. 30), `OrdenId` (long?, sin FK por ahora: Ethan la agrega en S23-M9). Índice **único** `(DispositivoId, BootId, Seq)`. Migración `AgregarEventos`.
2. `TelemetriaRequest`: agregar `parametros_version` (int, **opcional**: sin `[JsonRequired]`, 0 por defecto). Agregar las clases `EventoTelemetriaRequest` y `ResultadoOrdenRequest` con los campos de la sección 4, y cambiar `Eventos` y `ResultadosOrdenes` de `JsonElement` a `List<EventoTelemetriaRequest>` y `List<ResultadoOrdenRequest>` (siguen siendo obligatorios).
3. **Adapter** en `Servicios/Telemetria/`:
   ```csharp
   // Patrón: Adapter — rol: objetivo
   public interface IAdaptadorTelemetria
   {
       Medicion AMedicion(TelemetriaRequest paquete, int dispositivoId, DateTime horaServidor);
       Evento? AEvento(EventoTelemetriaRequest evento, long bootId, bool tsValida, int dispositivoId, DateTime horaServidor);
   }
   // Patrón: Adapter — rol: adaptador
   public class AdaptadorTelemetriaEsp32 : IAdaptadorTelemetria { ... }
   ```
   - `AMedicion` hace el mapeo que hoy está en `TelemetriaEndpoints` (incluida la regla `ts_valida = false` → hora del servidor).
   - Valida que `modo`, `estado` y `fuente_seleccionada` sean de los valores de la sección 4.1; si no, lanza `FormatException` (el endpoint responde 400).
   - `AEvento` devuelve `null` si el evento trae un `tipo`, `motivo`, `origen` o `estado` fuera de la sección 4.1 (se descarta **ese** evento y se registra un `LogWarning`; el resto del paquete se acepta).
4. **Facade** `Servicios/Telemetria/FachadaSincronizacion.cs` (`// Patrón: Facade — rol: fachada`), registrada `Scoped`. Un único método público:
   ```csharp
   public Task<RespuestaSincronizacion?> SincronizarAsync(string token, TelemetriaRequest paquete, CancellationToken ct);
   // RespuestaSincronizacion: record(DateTime HoraServidor, object? Orden). null = 401.
   ```
   Pasos internos, en este orden, cada uno en un método privado:
   1. `AutenticarAsync`: SHA-256 del token; dispositivo activo y empresa activa (como hoy).
   2. Medición: si `(boot_id, seq)` ya existe no se agrega (como hoy, con el manejo de la excepción de duplicado que ya existe).
   3. `RegistrarContacto`: `UltimoContacto` y el último estado del dispositivo (solo si la medición es nueva).
   4. `RegistrarEventosAsync`: guarda los eventos nuevos (descarta duplicados por `(DispositivoId, BootId, Seq)`).
   5. `ActualizarResultadosOrdenesAsync`: **vacío por ahora** (Ethan lo completa en S23-M9).
   6. `alertas.EvaluarAsync(...)`: **vacío por ahora** (lo completas tú en S23-M7).
   7. `EntregarOrdenPendienteAsync`: **devuelve `null` por ahora** (Ethan, S23-M9).
   8. Un solo `SaveChangesAsync` (una transacción).
   9. Avisar a `EstadoDispositivosEnVivo` (lo creas en S23-M6; mientras tanto, nada).
5. `TelemetriaEndpoints.cs` queda **solo HTTP**: leer el token del encabezado, deserializar, validar el formato (los mensajes 400 actuales se conservan), llamar a la fachada y devolver 401, 400 o 200 con `{ "hora_servidor": ..., "orden": ... }` en snake_case.
6. `Program.cs`: `app.UseHttpsRedirection()` **no** debe aplicarse a `/api/dispositivo` (el ESP32 usa HTTP en la demo). Envuélvelo con `app.UseWhen(ctx => !ctx.Request.Path.StartsWithSegments("/api/dispositivo"), b => b.UseHttpsRedirection());`.
7. Página `/eventos` (Admin, Técnico, Consulta; agrégala al menú con el ícono `History`): tabla de eventos de los dispositivos de la empresa activa, filtros por dispositivo, tipo y rango de fechas, columnas Hora (Bolivia), Dispositivo, Tipo, De → A (Solar/Red), Estado, Motivo (texto en español, ver tabla abajo), Origen, Orden. Orden descendente por hora. Paginada (50 por página).

   | motivo | Texto en pantalla |
   |---|---|
   | `intento_retorno_solar` | Intento de retorno a solar |
   | `cargador_sin_tension` | Red sin tensión |
   | `solar_comprobada` | Solar comprobada |
   | `prueba_solar_fallida` | Prueba de solar fallida |
   | `solar_insuficiente` | Solar insuficiente |
   | `sin_fuente_valida` | Ninguna fuente válida |
   | `cargador_recuperado` | Red recuperada |
   | `orden_modo` | Cambio por orden |
   | `orden_fallida` | Orden fallida |
   | `arranque` | Arranque del dispositivo |

8. Pruebas `tests/SAGE.Tests/AdaptadorTelemetriaTests.cs`: mapeo correcto de un paquete; `ts_valida = false` usa la hora del servidor; un `modo` inválido lanza `FormatException`; un evento con motivo desconocido devuelve `null`.

**Criterios de aceptación**
- [ ] Todo lo del Sprint 1 sigue funcionando: 200 con token correcto, 401 sin token o con token falso, una sola fila al repetir `seq`.
- [ ] Un paquete con 2 eventos guarda 2 filas en `Eventos`; reenviarlo no duplica nada (RF13).
- [ ] Un paquete con `"modo": "TURBO"` responde 400 y no guarda nada.
- [ ] Una petición HTTP (no HTTPS) a `/api/dispositivo/telemetria` no se redirige.
- [ ] `TelemetriaEndpoints.cs` no contiene consultas a la base (todo está en la fachada).

### - [ ] S23-M6 · Tiempo real: Singleton + Observer, monitoreo y escala
**Qué hacer**
1. `Servicios/EnVivo/EstadoDispositivo.cs`:
   ```csharp
   public sealed record EstadoDispositivo(
       int DispositivoId, int EmpresaId, DateTime TsServidor, bool EnLinea,
       string Modo, string Estado, string FuenteSeleccionada, bool AlimentacionComprobada,
       decimal VSol, decimal ISolMa, decimal PSolMw, decimal WhSolAcum,
       decimal VRed, decimal IRedMa, decimal PRedMw, decimal WhRedAcum,
       decimal FactorEscala)
   {
       public static EstadoDispositivo Desde(Medicion m, int empresaId, decimal factorEscala) => ...;
   }
   ```
2. `Servicios/EnVivo/EstadoDispositivosEnVivo.cs`, registrado con `AddSingleton` (`// Patrón: Singleton — rol: instancia única` y `// Patrón: Observer — rol: sujeto`):
   ```csharp
   public class EstadoDispositivosEnVivo
   {
       public event Action<EstadoDispositivo>? Cambio;          // nueva medición o cambio de conexión
       public event Action<int>? EventosRegistrados;              // dispositivoId: llegaron eventos nuevos
       public event Action<int>? AlertasCambiaron;               // empresaId: se abrió o cerró una alerta
       public void Actualizar(EstadoDispositivo e);              // guarda en un ConcurrentDictionary y dispara Cambio
       public void MarcarSinConexion(int dispositivoId);         // EnLinea = false y dispara Cambio
       public EstadoDispositivo? Obtener(int dispositivoId);
       public void NotificarEventos(int dispositivoId);
       public void NotificarAlertas(int empresaId);
   }
   ```
   - Cada evento se dispara recorriendo `GetInvocationList()` y llamando a cada suscriptor dentro de `try/catch` con `LogError`: **un suscriptor que falla nunca rompe la recepción de telemetría.**
   - La fachada (paso 9 de S23-M5) llama a `Actualizar` **después** de `SaveChangesAsync`, y a `NotificarEventos` si guardó eventos nuevos.
3. `Dispositivo.FactorEscala` (decimal, `Precision(10,2)`, 1 por defecto, rango 1 a 10 000), editable en `DispositivoDialog` por el Administrador, con la ayuda "Multiplica potencia y energía solo en pantalla. Ej.: 2000 para la maqueta." Migración `AgregarFactorEscala`.
4. `Servicios/Formato.cs` (estática), usada en **todas** las pantallas nuevas y en las que toques:
   - `HoraLocal(DateTime utc)` → `"dd/MM/yyyy HH:mm:ss"` en `America/La_Paz` (con respaldo `SA Western Standard Time`, como `FacturaReglas.HoyLocal`).
   - `Potencia(decimal mw, decimal factor)` → aplica el factor; `< 1 000 W` se muestra en W con 1 decimal (`"967,0 W"`), si no en kW con 2 decimales (`"1,93 kW"`).
   - `Energia(decimal wh, decimal factor)` → `< 1 000 Wh` en Wh con 2 decimales, si no en kWh con 3 decimales.
   - `Tension(decimal v)` → `"4,93 V"`; `Corriente(decimal ma)` → `"196,2 mA"` (nunca escaladas).
   - `Hace(DateTime? utc)` → `"hace 3 s"`, `"hace 5 min"`, `"hace 2 h"`, `"Nunca"`.
   - `Fuente(string)` → `SOLAR` → "Solar", `CARGADOR` → "Red"; `Modo(string modo, string fuente)` → `AUTO` "Automático", `CARGADOR` "Red", `SOLAR` "Solar", y `SOLAR` con fuente `CARGADOR` → "Solar en respaldo".
   - Componente `Components/Shared/ChipEscala.razor` con parámetro `Factor`: no muestra nada si es 1; si no, chip "Maqueta ×2000".
5. Página `/monitoreo` (Admin, Técnico, Consulta; en el menú, primera opción después de Inicio, ícono `MonitorHeart`): una tarjeta por dispositivo **de la empresa activa** con nombre, chip SIMULADO/REAL, punto verde/rojo de conexión y "hace X s", modo, fuente seleccionada, potencia solar y de red (escaladas con chip). Clic → `/monitoreo/{id}`.
6. Página `/monitoreo/{id}`, componente `PanelMonitoreo` (`// Patrón: Observer — rol: observador concreto`):
   - Si el dispositivo no es de la empresa activa → "No encontrado" (no "Acceso denegado", para no revelar que existe).
   - Cabecera: nombre, conexión, última conexión, modo.
   - Los **3 estados de la fuente** (diseño §1.4) en 3 chips: **Solicitada** (según el modo: Automático/Solar/Red), **Seleccionada** (Solar/Red), **Comprobada** (Sí/No; "Sin consumo" si la corriente de la rama seleccionada es menor que I_min).
   - Dos tarjetas, Solar y Red: V, I, P (escalada) y Wh acumulados del arranque actual (escalados).
   - Gráfico en vivo de los últimos 5 minutos: P solar y P red (W escalados), un punto por medición, máximo 300 puntos (`MudChart` tipo `Line`; al llegar uno nuevo se descarta el más viejo). Carga inicial desde la base, luego solo eventos en vivo.
   - Últimos 10 eventos del dispositivo (se actualiza con `EventosRegistrados`).
   - Suscripción en `OnInitialized`, `InvokeAsync(StateHasChanged)` en el manejador, **desuscripción en `Dispose`**. El manejador **ignora** todo estado cuyo `DispositivoId` no sea el de la página.
   - Un `PeriodicTimer` de 1 s solo para refrescar el texto "hace X s" (no consulta la base).
7. Pasar a tiempo real (Observer, sin `PeriodicTimer` que consulte la base):
   - `Mediciones.razor`: al llegar una medición del dispositivo filtrado (o de cualquier dispositivo de la empresa activa si no hay filtro), se agrega arriba; se mantienen 200 filas como máximo.
   - `Dispositivos/Index.razor`: la columna de última conexión y el punto verde/rojo cambian en vivo.
   - `Home.razor`: se quita el temporizador de 5 s del Sprint 1; la tarjeta "Última medición" se actualiza en vivo.
   - **Todos** los manejadores descartan estados de dispositivos de otra empresa (`e.EmpresaId != UsuarioActual.EmpresaActivaId`).
8. `SAGE.Simulador`: intervalo por defecto **1 s** (`--intervalo` sigue existiendo). Actualizar el README.
9. Pruebas `tests/SAGE.Tests/EstadoDispositivosEnVivoTests.cs`: un suscriptor que lanza excepción no impide que el siguiente reciba el aviso; `Obtener` devuelve el último estado. `FormatoTests.cs`: los ejemplos del punto 4.

**Criterios de aceptación**
- [ ] Con el simulador a 1 s, en `/monitoreo/{id}` la potencia y el gráfico cambian cada segundo sin recargar; el retraso entre la consola del simulador y la pantalla es ≤ 2 s (comparando las horas).
- [ ] Con 2 navegadores abiertos (admin en Energía Andina y admin.palmas en Hotel Las Palmas) y un simulador por empresa, ninguno ve en ninguna pantalla datos del otro.
- [ ] Con `FactorEscala = 2000`, una potencia real de 0,967 W se muestra como "1,93 kW" con el chip "Maqueta ×2000", y la tensión sigue mostrándose real.
- [ ] Al detener el simulador, el punto pasa a rojo en ≤ 35 s (cuando exista el vigilante de S23-M7; antes, al recargar).
- [ ] Cerrar la pestaña no deja suscripciones colgadas (revisa que `Dispose` desuscribe).

### - [ ] S23-M7 · RF16 Alertas y vigilante de conexión
**Qué hacer**
1. `Servicios/Dispositivo/ParametrosFuente.cs`: `record ParametrosFuente(decimal VMin, decimal VRetorno, decimal IMinMa, decimal TFallaS, decimal TEstableS, decimal TPruebaS)` con `public static ParametrosFuente Predeterminados => new(4.50m, 4.85m, 20m, 1.0m, 30m, 5m);`. (Ethan le agrega versión, límites y edición en S23-M10.)
2. Entidad `Alerta`: `Id`, `DispositivoId`, `Tipo` (`solar_insuficiente`, `sin_fuente_valida`, `cargador_sin_tension`, `sin_conexion`), `Inicio` (UTC), `Fin` (UTC?, null = abierta), `EventoId` (long?, FK a `Evento`). Migración `AgregarAlertas`. Regla: **como máximo una alerta abierta por dispositivo y tipo.**
3. `Servicios/Alertas/ServicioAlertas.cs` (Scoped). `EvaluarAsync(Dispositivo d, Medicion? m, IReadOnlyList<Evento> eventosNuevos)` aplica, en este orden:

   | Abre (si no hay una abierta del mismo tipo) | Cierra la abierta del mismo tipo |
   |---|---|
   | `solar_insuficiente`: evento con motivo `solar_insuficiente` | evento con motivo `solar_comprobada` |
   | `sin_fuente_valida`: evento con motivo `sin_fuente_valida` | medición con `estado ≠ SIN_ALIMENTACION` |
   | `cargador_sin_tension`: evento con motivo `cargador_sin_tension` | medición con `VRed ≥ VMin` (de `ParametrosFuente.Predeterminados` por ahora) |
   | `sin_conexion`: la abre el vigilante (punto 4) | cualquier telemetría nueva del dispositivo |

   Si abre o cierra alguna, llama a `EstadoDispositivosEnVivo.NotificarAlertas(empresaId)`. Completa el paso 6 de la fachada para que lo llame.
4. `Servicios/Alertas/VigilanteConexion.cs`, `BackgroundService` (`AddHostedService`): cada 5 s busca dispositivos activos con `UltimoContacto` no nulo y anterior a ahora − 30 s; a cada uno, si no tiene una alerta `sin_conexion` abierta, se la abre, y llama a `MarcarSinConexion`. Usa `IServiceScopeFactory` para crear el `DbContext` (no inyectar un `Scoped` en el singleton).
5. Componente `Components/Shared/IndicadorAlertas.razor` en la barra superior (`// Patrón: Observer — rol: observador concreto`): ícono de campana con insignia = cantidad de alertas abiertas de la empresa activa; se actualiza con `AlertasCambiaron` (ignora otras empresas); clic → `/alertas`. No se muestra al Operador.
6. Página `/alertas` (Admin, Técnico, Consulta; en el menú): pestañas "Abiertas" y "Historial"; columnas Dispositivo, Tipo (texto en español: Solar insuficiente, Ninguna fuente válida, Red sin tensión, Sin conexión), Inicio, Fin, Duración.
7. Pruebas `tests/SAGE.Tests/ServicioAlertasTests.cs` (EF Core en memoria o SQLite en memoria; anota cuál): abrir y cerrar cada tipo; no se abren 2 del mismo tipo.

**Criterios de aceptación**
- [ ] Al detener el simulador, en ≤ 35 s aparece la alerta "Sin conexión" y la campana sube sin recargar; al volver a arrancarlo, se cierra.
- [ ] Un paquete con un evento `solar_insuficiente` abre la alerta; uno con `solar_comprobada` la cierra (probado con `docs/api_telemetria.http`).
- [ ] Un usuario de otra empresa no ve esa alerta ni su campana cambia.

### - [ ] S23-M8 · Inicio por empresa y "Todas mis empresas"
**Qué hacer**
1. `Home.razor` según el rol:
   - **Operador:** tarjetas Empresas activas, Empresas inactivas, Administradores, Usuarios en total; tabla de empresas con la cantidad de usuarios por rol. **Ningún dato de energía.**
   - **Admin, Técnico, Consulta, pestaña "[nombre de la empresa activa]"**: tarjetas Dispositivos (en línea / total), Potencia solar ahora y Potencia de red ahora (suma de los dispositivos en línea, escaladas; chip de escala si alguno tiene factor ≠ 1), Alertas abiertas, Consumo facturado últimos 12 meses (kWh) y Monto últimos 12 meses (Bs); debajo, la tarjeta "Última medición" por dispositivo (ya existe).
   - **Pestaña "Todas mis empresas"** (solo si el usuario tiene 2 o más empresas): tabla con **una fila por empresa** y las mismas columnas que las tarjetas, más una fila **Total** al final. Clic en una fila → selecciona esa empresa (`SeleccionarEmpresaAsync`) y vuelve a la primera pestaña. Gráfico de barras: potencia solar y de red ahora, por empresa.
2. Todo en vivo con `Cambio` y `AlertasCambiaron`. En la pestaña consolidada, el manejador acepta estados de **cualquiera de las empresas del usuario** (`PuedeVerEmpresa`), y de ninguna otra.
3. Las cifras de la pestaña consolidada se calculan **por empresa** y luego se suman; nunca se mezclan filas de mediciones de distintas empresas.

**Criterios de aceptación**
- [ ] `admin@sage.test` ve las 2 pestañas; con un simulador en cada empresa, las dos filas cambian en vivo y el Total es la suma.
- [ ] `admin.palmas@sage.test` (1 empresa) no ve la pestaña "Todas mis empresas".
- [ ] El Operador ve su tablero sin ningún dato de energía.

**Al terminar:** marca S23-M5 a S23-M8, escribe tu entrada en la Bitácora y haz push. Avísale a Ethan.

---

## 9. Ethan — Órdenes, parámetros del dispositivo, simulador con máquina de estados y ensayos

### Verificación de entrada
- `git pull`, `dotnet ef database update --project src/SAGE.Web`, build y tests sin errores.
- Con el simulador a 1 s, `/monitoreo/{id}` se actualiza en vivo; al detenerlo aparece la alerta "Sin conexión".
- Si algo falla, regla 3.

### - [x] S23-M9 · RF11 Órdenes de modo (patrón Command, lado servidor)
**Qué hacer**
1. Entidad `Orden` en `Data/Orden.cs` (`// Patrón: Command — rol: comando`): `Id` (long), `DispositivoId`, `UsuarioId`, `Tipo` (`modo` o `parametros`), `ValorJson` (string: `"\"SOLAR\""` para modo; el objeto JSON de la sección 4 para parámetros), `Estado` (`pendiente`, `entregada`, `ejecutada`, `rechazada`, `expirada`), `Motivo` (string?, máx. 50), `Creada`, `Entregada?`, `Resuelta?` (UTC). Métodos de dominio (la entidad controla sus transiciones; nadie asigna `Estado` desde afuera):
   ```csharp
   public bool Vencida(DateTime ahora);        // pendiente y ahora - Creada > 30 s, o entregada y ahora - Entregada > 60 s
   public void Entregar(DateTime ahora);       // solo desde pendiente
   public void Expirar(DateTime ahora, string motivo);  // "no_entregada" si estaba pendiente, "sin_confirmacion" si estaba entregada
   public void Resolver(string resultado, string? motivo, DateTime ahora); // solo desde entregada; si no, se ignora
   ```
   Agregar la FK `Evento.OrdenId → Orden.Id` (`SetNull`). Migración `AgregarOrdenes`.
2. `Servicios/Ordenes/ICanalOrdenes.cs` y `CanalOrdenesBD.cs` (`// Patrón: Command — rol: invocador`):
   ```csharp
   public interface ICanalOrdenes
   {
       Task<Orden> EnviarModoAsync(int dispositivoId, string modo);                 // AUTO | SOLAR | CARGADOR
       Task<Orden> EnviarParametrosAsync(int dispositivoId, ParametrosFuente p);    // lo usa S23-M10
       Task<IReadOnlyList<Orden>> UltimasAsync(int dispositivoId, int cantidad);
   }
   ```
   `CanalOrdenesBD` **no revisa permisos** (eso lo hará el Proxy de Mateo en S23-M13; hasta entonces lo controla la pantalla). Reglas:
   - Si el dispositivo tiene una orden `pendiente`, esa pasa a `expirada` con motivo `reemplazada` y se crea la nueva.
   - Si tiene una orden `entregada`, no se crea la nueva: `InvalidOperationException("Espere el resultado de la orden anterior.")`.
3. En `FachadaSincronizacion`:
   - Paso 5, `ActualizarResultadosOrdenesAsync`: por cada `resultados_ordenes[]` de una orden **de ese dispositivo**, `Resolver(...)`. Resultados de órdenes de otro dispositivo se ignoran con `LogWarning`.
   - Paso 7, `EntregarOrdenPendienteAsync`: primero expira las vencidas del dispositivo; después toma la `pendiente` más antigua, `Entregar(ahora)` y la devuelve en el formato de la sección 4.
   - Cuando cambia el estado de una orden, llama a un evento nuevo de `EstadoDispositivosEnVivo`: `public event Action<int>? OrdenesCambiaron; public void NotificarOrdenes(int dispositivoId);` (agrégalo con el mismo manejo de errores que los otros).
4. `Servicios/Ordenes/ExpiradorOrdenes.cs`, `BackgroundService`: cada 1 s expira las órdenes vencidas de todos los dispositivos y notifica. (Así una orden a un dispositivo apagado se ve `expirada` aunque no llegue telemetría.)
5. En `PanelMonitoreo` (`/monitoreo/{id}`), sección **"Control de fuente"** (solo Admin y Técnico): 3 botones "Automático", "Solar", "Red"; el del modo actual aparece marcado. Al enviar: chip de estado de la orden que cambia en vivo (Pendiente → Entregada → Ejecutada / Rechazada: motivo / Expirada: motivo). Debajo, tabla de las últimas 10 órdenes (hora, quién, orden, estado, motivo, tiempo hasta resolverse en s). Textos de motivo: `solar_no_disponible` "Solar no disponible", `orden_fallida` "La prueba de solar falló", `parametros_invalidos` "Parámetros fuera de rango", `orden_desconocida` "Orden desconocida", `no_entregada` "No se entregó a tiempo (30 s)", `sin_confirmacion` "Sin confirmación del dispositivo", `reemplazada` "Reemplazada por otra orden".
6. Pruebas `tests/SAGE.Tests/OrdenTests.cs`: cada transición válida e inválida de `Orden`; `Vencida` a los 30 s y 60 s.

**Criterios de aceptación**
- [x] Con `docs/api_telemetria.http`: crear una orden desde la pantalla → la siguiente telemetría la recibe en `orden` → un paquete con su resultado la marca ejecutada o rechazada, y la pantalla lo muestra sin recargar.
- [x] Una orden a un dispositivo sin telemetría pasa a "Expirada: No se entregó a tiempo" a los 30 s (± 2 s), y nunca se entrega después (RNF08).
- [x] Consulta no ve los botones de control.

### - [x] S23-M10 · RF15 Parámetros del dispositivo
**Qué hacer**
1. Ampliar `ParametrosFuente` con los **límites** del diseño §4.1 (`VMin` 4,0 a 4,9; `VRetorno` de `VMin + 0,2` a 5,3; `IMinMa` 5 a 100; `TFallaS` 0,5 a 5; `TEstableS` 10 a 300; `TPruebaS` 2 a 30) y `IReadOnlyList<string> Validar()` (lista de errores en español, vacía si es válido).
2. `Dispositivo`: `ParametrosJson` (string?, null = predeterminados), `ParametrosVersion` (int, 0 = predeterminados de fábrica), `ParametrosVersionConfirmada` (int, la que informa el dispositivo en `parametros_version`). La fachada actualiza `ParametrosVersionConfirmada` con cada telemetría. Migración `AgregarParametrosDispositivo`.
3. Pestaña **"Reglas"** en `/monitoreo/{id}` (Admin y Técnico la editan; Consulta la ve): los 6 valores con unidades, "Versión X — Aplicada" (verde) si `ParametrosVersionConfirmada == ParametrosVersion`, si no "Versión X — Pendiente de aplicar" (ámbar). Guardar: valida; si es válido, `ParametrosVersion + 1`, guarda el JSON y llama a `EnviarParametrosAsync` (`tipo = parametros`, `valor` con `version`). Botón "Restablecer valores de fábrica".
4. `ServicioAlertas` pasa a usar el `VMin` de los parámetros del dispositivo (no el predeterminado).
5. Pruebas en `OrdenTests.cs` o `ParametrosFuenteTests.cs`: cada límite, incluida la regla `VRetorno ≥ VMin + 0,2`.

**Criterios de aceptación**
- [x] `VMin = 5,5` se rechaza con mensaje y no se crea orden.
- [x] Un valor válido crea la orden; cuando el simulador de S23-M11 la ejecuta, la pantalla pasa a "Aplicada" sin recargar.

### - [x] S23-M11 · Simulador con máquina de estados (patrón State) y órdenes (Command)
Reescribe `src/SAGE.Simulador` para que se comporte **como el firmware** (diseño §4) y así se pueda hacer toda la demo sin hardware. Las clases son `public` (las prueba `SAGE.Tests`). Archivos sugeridos: `Planta.cs`, `ControladorFuente.cs`, `Estados.cs`, `Ordenes.cs`, `Cliente.cs`, `Program.cs`.

**Qué hacer**
1. **Planta** (`Planta.cs`), el mundo físico simulado, avanza cada 100 ms:
   - Irradiancia 0 a 1 por la hora del día (como hoy) o fija con `--hora`. Teclas: `S` sombrea/descubre el panel (sombreado: irradiancia × 0,1), `R` corta/restablece la red (cargador a 0 V), `N` alterna noche forzada.
   - Rama solar, **en vacío** (relé en red): `V = 5,00` si irradiancia > 0,05, si no `0,3`. **Con carga** (relé en solar): `V = 4,90` si irradiancia ≥ 0,25, si no `3,80`. (Así se reproduce el problema del diseño: en vacío parece que hay sol aunque no alcance.) Ruido ±0,02 V.
   - Rama red: `5,05 V` (± 0,02) o `0 V` si está cortada.
   - Carga: 190 mA (± 5) por la rama seleccionada **si su tensión ≥ 4,5 V**; si no, 0 mA. La rama no seleccionada: 0,4 mA.
   - `p_mw = v × i_ma`; Wh acumulados por rama como hoy (nunca bajan dentro de un `boot_id`).
2. **State** (`Estados.cs`, `ControladorFuente.cs`), igual que en el documento de patrones de comportamiento:
   - `abstract class EstadoFuente` (`// Patrón: State — rol: estado`) con `string Nombre`, `void Entrar(ControladorFuente c)` y `void Evaluar(ControladorFuente c, Lecturas l)`.
   - `EnCargador`, `ProbandoSolar`, `EnSolar`, `SinAlimentacion` (`// Patrón: State — rol: estado concreto`), una instancia de cada una.
   - `ControladorFuente` (`// Patrón: State — rol: contexto`): guarda el estado actual, el modo (`AUTO`, `SOLAR`, `CARGADOR`), la posición del relé, los parámetros y sus temporizadores. **Todo cambio de estado pasa por `CambiarA(EstadoFuente nuevo, string motivo, string origen, long? ordenId)`**, que mueve el relé si corresponde, reinicia temporizadores, agrega el evento a la cola (tipo según la tabla de la sección 4) y llama a `nuevo.Entrar(this)`. `Tick()` evalúa primero la regla global (ambas ramas < V_min → `SinAlimentacion`) y luego delega en el estado actual.
   - Las decisiones usan el **promedio del último segundo** (10 muestras), nunca una muestra suelta.
   - Reglas: exactamente las tablas 4.2 y 4.3 del diseño más la sección 3.6 de esta guía. Espera tras fallo: 30 → 60 → 120 → 300 s (tope 600), se duplica en cada `prueba_solar_fallida` y vuelve a 30 s tras 10 min seguidos en `EN_SOLAR`.
   - En modo `CARGADOR`: relé en red siempre; si la red cae, evento `alerta` `cargador_sin_tension` (una vez, hasta que vuelva).
   - En modo `SOLAR`: si solar falla, pasa a red, evento `solar_insuficiente`, y **no reintenta** (queda "Solar en respaldo") hasta una nueva orden.
3. **Command** (`Ordenes.cs`):
   - `interface Comando { long Id { get; } void Ejecutar(ControladorFuente c, Action<ResultadoOrden> informar); }` (`// Patrón: Command — rol: comando`).
   - `ComandoModo` (`rol: comando concreto`): `CARGADOR` y `AUTO` → `ejecutada`; `SOLAR` → si solar en vacío < V_retorno → `rechazada` / `solar_no_disponible`; si no → `CambiarA(ProbandoSolar, "orden_modo", "orden", id)` y `ejecutada` (sección 3.6).
   - `ComandoParametros` (`rol: comando concreto`): valida con los mismos límites; inválidos → `rechazada` / `parametros_invalidos`; válidos → los aplica, guarda la versión y `ejecutada`.
   - `ProcesadorOrdenes` (`rol: invocador`): convierte la `orden` de la respuesta en un `Comando` (tipo desconocido → `rechazada` / `orden_desconocida`); **idempotencia:** si `id ≤ ultimaEjecutada` no hace nada; guarda `ultimaEjecutada`, el modo y los parámetros en `simulador_estado_<primeros 8 caracteres del SHA-256 del token>.json` junto al ejecutable (imita la memoria `Preferences` del ESP32).
4. **Cliente** (`Cliente.cs`): cada 1 s envía el paquete de la sección 4 con la cola de eventos y los resultados pendientes; si la respuesta es 200, vacía lo enviado y pasa la `orden` al procesador. Cola de eventos: máximo 200 (se descartan los más viejos). Tecla `W`: corta/restablece el "Wi-Fi" (no envía, sigue acumulando eventos; al volver, los envía).
5. **Arranque:** lee el archivo de estado; `boot_id` = el guardado + 1; relé en red; evento `reinicio`/`arranque`; espera 3 s de mediciones y aplica el modo guardado (diseño §4.4).
6. **Consola:** una línea por segundo: hora, `seq`, modo, estado, fuente, V/P de cada rama, Wi-Fi sí/no, código HTTP. Cada evento y cada orden se imprimen en una línea aparte. Al arrancar, mostrar la ayuda de teclas: `S` sombra, `R` red, `N` noche, `W` Wi-Fi, `Q` salir.
7. Pruebas `tests/SAGE.Tests/MaquinaEstadosTests.cs` con la planta en modo determinista (sin ruido; agrega un constructor o parámetro para eso) y reloj simulado (el `Tick` recibe la hora; no uses `DateTime.Now` dentro de la lógica):
   - Sombra en `EN_SOLAR` → `EN_CARGADOR` con `solar_insuficiente` en ≤ 2 s (RF12).
   - Sol después de sombra → tras T_estable, `PROBANDO_SOLAR` y luego `EN_SOLAR` con `solar_comprobada`.
   - Sol "falso" (irradiancia 0,1: en vacío 5 V, con carga 3,8 V) → `prueba_solar_fallida` y la espera se duplica; **nunca alterna más de una vez por espera** (sin alternancia continua).
   - Orden `SOLAR` con panel sombreado → `rechazada` / `solar_no_disponible`.
   - Orden repetida (mismo id) → se ejecuta una sola vez.

**Criterios de aceptación**
- [x] `dotnet test` pasa con los 5 casos.
- [x] Con el simulador y la plataforma: `S` → en ≤ 2 s `/monitoreo/{id}` muestra Red y el evento "Solar insuficiente"; `S` otra vez → tras ~30 s, "Intento de retorno a solar" y luego "Solar comprobada".
- [x] Panel sombreado + orden Solar desde la app → "Rechazada: Solar no disponible" en ≤ 5 s (RNF10).
- [x] `W` (sin Wi-Fi) + `S` → el simulador conmuta igual; al volver con `W`, los eventos aparecen una sola vez en `/eventos` (RNF07, RF13).
- [x] Reiniciar el simulador → nuevo `boot_id`, evento "Arranque del dispositivo" y vuelve al modo guardado.

### - [x] S23-M12 · RF17 Ensayos
**Qué hacer**
1. Entidad `Ensayo`: `Id`, `DispositivoId`, `CreadoPorId`, `Inicio` (UTC), `Fin` (UTC?, null = en curso), `CargaDescripcion` (obligatoria, máx. 200), `Condiciones` (obligatoria, máx. 500), `Notas` (máx. 1000). Migración `AgregarEnsayos`.
2. Página `/ensayos` (Admin, Técnico, Consulta; en el menú, ícono `Science`): ensayos de los dispositivos de la empresa activa; columnas Dispositivo, Inicio, Fin (o chip "En curso"), Duración, Carga.
   - Admin y Técnico: "Nuevo ensayo" (dispositivo, carga, condiciones, notas, **Inicio** = ahora por defecto y editable, **Fin** opcional) y "Finalizar ahora" para los que están en curso.
   - Validaciones: `Fin > Inicio`; `Inicio` no puede ser futuro; dos ensayos del mismo dispositivo no pueden superponerse ("Se superpone con el ensayo del dd/MM HH:mm").
3. Página `/ensayos/{id}`: datos del ensayo y tabla paginada de **las mediciones de su intervalo** (`Ts` entre Inicio y Fin, o hasta ahora si está en curso), con la cantidad total. (Mateo agrega la comparación por fuente en S23-M15.)
4. Prueba de la regla de superposición.

**Criterios de aceptación**
- [x] Un ensayo de 2 minutos con el simulador muestra solo las mediciones de esos 2 minutos (≈ 120) (RF17).
- [x] No se puede crear un ensayo superpuesto ni con Fin antes que Inicio.
- [x] Consulta ve los ensayos sin botones.

**Al terminar:** marca S23-M9 a S23-M12, escribe tu entrada en la Bitácora y haz push. Avísale a Mateo.

---

## 10. Mateo — Proxy y Abstract Factory, energía e historial, comparación medida, firmware y cierre

### Verificación de entrada
- `git pull`, `dotnet ef database update --project src/SAGE.Web`, build y tests sin errores.
- Los criterios de S23-M11 se cumplen con el simulador (sombra, orden rechazada, Wi-Fi cortado).
- Si algo falla, regla 3.

### - [ ] S23-M13 · Proxy de protección y Abstract Factory
**Qué hacer**
1. `Servicios/Dispositivo/ILectorTelemetria.cs`:
   ```csharp
   public interface ILectorTelemetria
   {
       Task<IReadOnlyList<Medicion>> MedicionesAsync(int dispositivoId, DateTime desdeUtc, DateTime hastaUtc, int maximo);
       Task<IReadOnlyList<Evento>> EventosAsync(int dispositivoId, DateTime desdeUtc, DateTime hastaUtc);
   }
   ```
   `LectorTelemetriaBD` (lee de la base, `AsNoTracking`).
2. `Servicios/Dispositivo/IFabricaDispositivo.cs` (`// Patrón: Abstract Factory — rol: fábrica abstracta`): `ILectorTelemetria CrearLector(); ICanalOrdenes CrearCanalOrdenes();`.
   - `FabricaDispositivoReal` (`rol: fábrica concreta`): `new CanalOrdenesProtegido(new CanalOrdenesBD(...), usuario, db)` y `new LectorTelemetriaProtegido(new LectorTelemetriaBD(db), usuario, db)`.
   - `FabricaDispositivoSimulado` (`rol: fábrica concreta`): **modo de presentación sin dispositivo ni base de mediciones.** `LectorTelemetriaSimulado` genera mediciones sintéticas (curva de sol por la hora, una cada segundo del rango pedido, máximo `maximo`) y ninguna lista de eventos; `CanalOrdenesSimulado` guarda las órdenes en memoria y las resuelve al instante (`SOLAR` se rechaza con `solar_no_disponible` si la hora está fuera de 08:00–17:00; el resto, `ejecutada`). Los dos también envueltos en sus proxies.
   - `Program.cs`: si `Dispositivo:Modo` (configuración) es `"Simulado"` registra `FabricaDispositivoSimulado`; en cualquier otro caso, `FabricaDispositivoReal`. Valor por defecto: `"Real"` en `appsettings.json`.
3. **Proxies** (`// Patrón: Proxy — rol: proxy de protección`), implementan la misma interfaz y delegan en el real **solo** si:
   - `CanalOrdenesProtegido`: el usuario es `Administrador` o `Tecnico` **y** `PuedeVerEmpresa(empresa del dispositivo)`. Si no, `UnauthorizedAccessException("Su rol no permite cambiar el modo de fuente.")` o `("El dispositivo no pertenece a su empresa activa.")`.
   - `LectorTelemetriaProtegido`: el usuario **no** es Operador **y** `PuedeVerEmpresa(empresa del dispositivo)`.
4. Pasar a la fábrica: las pantallas de control y reglas (Ethan) usan `fabrica.CrearCanalOrdenes()` en lugar de `CanalOrdenesBD`; `Mediciones.razor`, la carga inicial del gráfico de `PanelMonitoreo` y el historial (S23-M14) usan `fabrica.CrearLector()`. Ninguna pantalla crea `CanalOrdenesBD` ni `LectorTelemetriaBD` directamente.
5. Pruebas `tests/SAGE.Tests/ProxyTests.cs`: Consulta → excepción y el real no recibe la llamada; Técnico de otra empresa → excepción; Técnico de la empresa → delega.

**Criterios de aceptación**
- [ ] Con `Dispositivo:Modo = Simulado`, `/mediciones` y el historial muestran datos sintéticos sin simulador corriendo, y una orden se resuelve al instante.
- [ ] Con `Dispositivo:Modo = Real` todo funciona como antes.
- [ ] Llamar a `EnviarModoAsync` como Consulta (desde una prueba) lanza la excepción y no crea filas en `Ordenes`.

### - [ ] S23-M14 · RF14 Energía por fuente, RF04 historial y RF08 exportación
**Qué hacer**
1. `Servicios/Energia/CalculadoraEnergia.cs`: `Task<EnergiaPorFuente> CalcularAsync(int dispositivoId, DateTime desdeUtc, DateTime hastaUtc)` → `record EnergiaPorFuente(decimal WhSolar, decimal WhRed, TimeSpan TiempoSolar, TimeSpan TiempoRed, int Conmutaciones)`. Regla exacta:
   - Por cada `BootId` con mediciones en el intervalo: `energía = último WhAcum en el intervalo − referencia`, donde `referencia` = último `WhAcum` de ese mismo `BootId` **antes** del intervalo, o 0 si no hay (el arranque fue dentro del intervalo). Se suman todos los `BootId`. Se hace por separado para solar y red.
   - Tiempo por fuente: para cada par de mediciones consecutivas, `min(Δt, 5 s)` se suma a la fuente seleccionada de la primera.
   - Conmutaciones: cantidad de `Eventos` con `Tipo = conmutacion` en el intervalo.
2. Página `/historial` (Admin, Técnico, Consulta; en el menú, ícono `Timeline`): dispositivo (de la empresa activa), periodo (Última hora, Hoy, Últimos 7 días, Rango personalizado).
   - Gráfico de P solar y P red (escaladas). Si hay más de 1 500 mediciones, se agrupan en 1 500 intervalos iguales con el **promedio** de cada uno, calculado en SQL (`GroupBy` sobre el índice del intervalo), no trayendo todas las filas a memoria.
   - Tarjetas: Energía solar, Energía red (Wh/kWh, escaladas con chip), % solar, Tiempo en solar, Tiempo en red, Conmutaciones.
   - Tabla de eventos del periodo.
   - Meta RF04: el gráfico de 7 días carga en ≤ 3 s con ~600 000 mediciones (1 por segundo durante 7 días). Si hace falta, agrega un índice `(DispositivoId, Ts)` (migración `IndiceMedicionesTs`). Para probarlo, crea un comando de desarrollo `--generar-historial <dispositivoId> <dias>` en `Program.cs` (solo Development) que inserta mediciones sintéticas con `BootId` propio.
3. Botón "Exportar a Excel" (los 3 roles) en `/historial`: archivo `SAGE_historial_<dispositivo>_<yyyyMMdd-HHmm>.xlsx` con ClosedXML; hojas **Resumen** (lo mismo que las tarjetas, con valores reales y escalados), **Mediciones** (todas las del periodo, sin agrupar: hora local, V/I/P/Wh de cada rama reales, P escaladas, modo, estado, fuente) y **Eventos**. Se descarga desde Blazor con `DotNetStreamReference` y una función JS `descargarArchivo` en `wwwroot/js/descargas.js` (patrón estándar de la documentación de Blazor), sin endpoint nuevo. Los datos son los de la misma consulta que la pantalla (RF08).
4. Inicio (`Home.razor`): agregar "Energía hoy" (solar y red, escaladas) a la pestaña de la empresa y a la tabla "Todas mis empresas", con `CalculadoraEnergia` desde las 00:00 de hoy (hora de Bolivia). Se recalcula al llegar mediciones como máximo cada 10 s (no en cada evento).
5. Pruebas `tests/SAGE.Tests/CalculadoraEnergiaTests.cs`: un solo arranque; dos arranques (reinicio en medio); referencia antes del intervalo; corte de 30 s cuenta solo 5 s.

**Criterios de aceptación**
- [ ] 30 min de simulador en solar: la energía solar del historial coincide con `P promedio × 0,5 h` dentro de ±5 % (RF14, con los valores reales).
- [ ] Un reinicio del simulador en medio del periodo no resta ni duplica energía.
- [ ] El Excel exportado tiene las mismas cifras que las tarjetas de la pantalla (RF08).
- [ ] El gráfico de 7 días con datos generados carga en ≤ 3 s (medido con el reloj del navegador; anota el valor).

### - [ ] S23-M15 · RF18 Comparación medida (ensayos y periodo)
**Qué hacer**
1. En `/ensayos/{id}` y en `/historial`, sección **"Comparación por fuente (MEDIDO)"** con chip "MEDIDO" (RNF12): tabla Solar / Red / Total con Energía (real y escalada), Tiempo de uso, % del total y Conmutaciones, y un gráfico de barras Solar vs Red. Usa `CalculadoraEnergia` con el intervalo del ensayo o el periodo.
2. En `/simulacion`, debajo del resultado, el texto fijo: "Los resultados de simulación no usan las mediciones de la maqueta. Ver la comparación MEDIDA en Historial o Ensayos." (RNF12, RF18).

**Criterios de aceptación**
- [ ] Un ensayo con sombra provocada a mitad muestra energía en las dos fuentes y al menos 2 conmutaciones.
- [ ] Ninguna pantalla mezcla un valor medido y uno simulado sin su chip (MEDIDO / SIMULACIÓN).

### - [ ] S23-M16 · Firmware del ESP32 (State, Command y Singleton)
**Qué hacer**
1. Carpeta `firmware/sage_esp32/`:
   - `sage_esp32.ino` (setup/loop), `ControladorFuente.h/.cpp`, `Estados.h/.cpp`, `Ordenes.h/.cpp`, `Medicion.h/.cpp`, `Telemetria.h/.cpp`, `config.example.h`.
   - `config.h` (Wi-Fi, URL del servidor, token) se crea copiando `config.example.h` y está en `.gitignore`.
2. Hardware según el diseño §2 y §3: INA219 solar en `0x40`, red en `0x41`, I²C en GPIO21 (SDA) / GPIO22 (SCL), calibración `setCalibration_16V_400mA()`, relé en **GPIO26 activo en alto** (bajo = NC = red). `digitalWrite(26, LOW)` es **lo primero** de `setup()`.
3. Misma lógica que el simulador de S23-M11 (tablas 4.2, 4.3, sección 3.6 y la tabla de eventos de la sección 4):
   - `ControladorFuente` es **Singleton** (`static ControladorFuente& instancia()`, constructor privado, copia eliminada) y **contexto** del State.
   - Estados como clases que heredan de `EstadoFuente` (`nombre()`, `entrar(c)`, `evaluar(c, lecturas)`), con instancias estáticas.
   - `Comando`, `ComandoModo`, `ComandoParametros`, `ProcesadorOrdenes` con idempotencia por `Preferences` (claves: `ult_orden`, `modo`, `param_ver`, `param_json`, `boot_id`).
   - Muestreo a 10 Hz, decisión sobre el promedio de 1 s, telemetría cada 1 s, cola de 200 eventos en RAM, hora por NTP (`configTime`, `ts_valida = false` hasta sincronizar), watchdog con la API del núcleo 3.x (`esp_task_wdt_config_t`).
   - La lógica de estados **no depende** del Wi-Fi: el envío HTTP tiene timeout de 800 ms y nunca bloquea la evaluación de reglas más de eso.
4. Compilar:
   ```bash
   arduino-cli core install esp32:esp32
   arduino-cli lib install "Adafruit INA219" "ArduinoJson"
   arduino-cli compile --fqbn esp32:esp32:esp32 firmware/sage_esp32
   ```
5. `firmware/README.md`: conexiones (tabla de pines), cómo configurar `config.h`, cómo cargar, cómo ejecutar la plataforma para la red local (`dotnet run --project src/SAGE.Web --urls http://0.0.0.0:5284` y regla del firewall de Windows para el puerto 5284) y los ensayos E1 a E4 del diseño §7 con su criterio.

**Criterios de aceptación**
- [ ] `arduino-cli compile` termina sin errores.
- [ ] **Con el hardware armado** (si todavía no está, deja estas casillas sin marcar y anótalo en la Bitácora): el relé arranca en red sin hacer clic; E1 (V e I contra el multímetro, RNF09); E4 (sombra → red en ≤ 2 s, sin alternancia); una orden desde la app se ejecuta en ≤ 5 s; sin Wi-Fi sigue conmutando y al volver aparecen los eventos.

### - [ ] S23-M17 · Demo integrada y cierre
**Qué hacer**
1. Recorrer **RNF01 de punta a punta** con los 6 usuarios de prueba y 2 navegadores: ninguna pantalla, exportación ni actualización en vivo muestra datos de otra empresa; el Operador no ve datos de energía. Corregir lo que falle y anotarlo.
2. RNF04: revisar cada pantalla en Chrome, Edge y Firefox, y a 375 px de ancho (celular): sin desbordes horizontales.
3. RNF12: revisar que todo dato simulado tenga el chip SIMULADO/SIMULACIÓN y todo medido escalado su chip de escala.
4. `git grep "// Patrón:"` debe listar todas las clases de la sección 5. Agregar al `README.md` la sección "Patrones de diseño en el código" con esa tabla.
5. Actualizar `README.md`: cómo ejecutar, usuarios de prueba (tabla de S23-M1), simulador con teclas, modo `Dispositivo:Modo`, firmware.
6. Completar la sección 12 (demo), comprobando cada paso.

**Criterios de aceptación**
- [ ] Todas las casillas de esta guía están marcadas o tienen su motivo en la Bitácora.
- [ ] Los pasos de la sección 12 funcionan con el simulador (y con el ESP32 si ya está armado).

**Al terminar:** marca S23-M13 a S23-M17, escribe tu entrada en la Bitácora y haz push.

---

## 11. Bitácora

Cada agente agrega **una entrada al terminar** (y otra si deja un BLOQUEO). No borres entradas de otros.

```text
### <fecha y hora> — <Nombre> — <misiones>
- Hecho: ...
- Correcciones a trabajo anterior: ... (o "ninguna")
- Desvíos de la guía y motivo: ... (o "ninguno")
- Criterios no comprobados y por qué: ... (o "ninguno")
- Pendientes o BLOQUEOS: ... (o "ninguno")
- Último commit: <hash corto>
```

<!-- Las entradas van debajo de esta línea -->

### 2026-11-14 01:40 (UTC-4) — Harold — S23-M1 a S23-M4
- Hecho: multiempresa (`UsuarioEmpresa`), rol Operador, seed de 6 usuarios, `IUsuarioActual` + selector de empresa, filtros por empresa activa, `/empresas` solo Operador, `/usuarios` con contraseña temporal y `CambiarContrasenaInicial`, parámetros económicos, tarifa por empresa, `SimulacionBuilder` + `/simulacion` (gráfico SVG interactivo), proyecto `tests/SAGE.Tests`, migraciones `MultiempresaYOperador` y `ParametrosYSimulacion`. Integrado el arreglo MudBlazor (`@Assets`) **sin** `ConnectionStrings` en `appsettings.Development.json`. README sin clave SA de Docker (usar env / user-secrets).
- Correcciones a trabajo anterior: concurrencia de `DbContext` en `UsuarioActual` (scope + lock) que rompía Home; menú Admin sin Empresas.
- Desvíos de la guía y motivo: el gráfico de simulación es SVG propio (no `MudChart`) para scrub/tip y desglose Ahorrás/Gastás; se mantiene el chip SIMULACIÓN y los supuestos. Contraseña de Docker/SQL no se documenta en el repo (regla 7/12).
- Criterios no comprobados y por qué: ninguno de M1–M4 pendientes a nivel código/tests; la creación manual “Panadería Sol” se validó por flujos equivalentes en UI de Operador.
- Pendientes o BLOQUEOS: ninguno. Sigue Matías (S23-M5).
- Último commit: 4464c04

---

## 12. Demo de los Sprints 2 y 3 (la completa Mateo en S23-M17)

Con la plataforma y el simulador corriendo (dispositivo de Energía Andina con `FactorEscala = 2000`):

- [ ] 1. `operador@sage.test`: crea la empresa "Panadería Sol" y su administrador. Muestra que no ve ningún dato de energía.
- [ ] 2. El nuevo administrador entra → obligado a cambiar la contraseña → ve solo Panadería Sol.
- [ ] 3. `admin@sage.test`: cambia entre sus 2 empresas con el selector; Inicio → "Todas mis empresas" con una fila por empresa en vivo.
- [ ] 4. `/monitoreo/{id}`: modo Automático con sol → Solar · comprobada, potencia "×2000" cambiando cada segundo.
- [ ] 5. Simulador `S` (sombra) → en ≤ 2 s pasa a Red, evento "Solar insuficiente" y alerta.
- [ ] 6. Simulador `S` (sin sombra) → tras ~30 s, prueba y regreso a solar ("Solar comprobada").
- [ ] 7. Con sombra, orden **Solar** → "Rechazada: Solar no disponible".
- [ ] 8. Orden **Red** y luego **Automático** → ambas "Ejecutada".
- [ ] 9. Simulador `W` (sin Wi-Fi) + `S` → conmuta igual; `W` → los eventos aparecen una sola vez; mientras, la alerta "Sin conexión" se abrió y se cerró.
- [ ] 10. Pestaña Reglas: cambiar T_estable a 15 s → "Aplicada".
- [ ] 11. Ensayo de 5 minutos con una sombra en medio → comparación MEDIDA por fuente.
- [ ] 12. Historial del día → exportar a Excel.
- [ ] 13. Simulación económica de Hotel Las Palmas → 3 escenarios rotulados SIMULACIÓN, separados de lo medido.
- [ ] 14. `consulta@sage.test`: ve todo de Energía Andina, sin botones de control.
