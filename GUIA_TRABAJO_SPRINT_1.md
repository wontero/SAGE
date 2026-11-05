# Guía de trabajo — Sprint 1 (SAGE)

> **Para la persona:** pásale este archivo completo a tu agente (Claude Code, Codex, Copilot, etc.) y dile:
> *"Lee GUIA_TRABAJO_SPRINT_1.md completo y ejecuta las misiones de &lt;tu nombre&gt;."*
> Tú solo tienes que hacer la sección 1 (preparar la PC) y revisar al final que el agente haya subido todo.

- **Repositorio:** https://github.com/wontero/SAGE
- **Entrega del Sprint 1:** 06/11/2026
- **Orden de trabajo (estricto):** 1) Harold → 2) Matías → 3) Ethan. Nadie empieza hasta que el anterior haya subido su trabajo y marcado sus misiones como hechas en este archivo.

**Meta del Sprint 1 (producto mínimo viable):** plataforma base con inicio de sesión por rol, gestión de empresas, usuarios y dispositivos, carga de facturas y un simulador que envía datos falsos.
**Criterio de validación del docente:** cada rol ve solo sus funciones; las mediciones del simulador se guardan en la base de datos; una factura cargada aparece en el sistema.

---

## 0. Reglas para el agente (obligatorias)

1. Lee esta guía completa antes de tocar nada. Esta guía manda sobre cualquier otro documento: si `docs/` dice otra cosa (por ejemplo, PostgreSQL), se hace lo que dice esta guía.
2. Haz **solo las misiones de la persona que te pidió el trabajo**. No adelantes misiones de otros ni agregues funciones que no están acá.
3. Antes de empezar, haz la **verificación de entrada** de tu sección: comprueba que el trabajo del compañero anterior funciona. Si algo falla:
   - Si es un error chico que te bloquea, corrígelo y anótalo en la Bitácora (sección 6) como "Corrección a la misión X".
   - Si es grande o falta una misión entera, **no la hagas tú**: anótalo en la Bitácora como "BLOQUEO", sube el archivo y avísale a tu persona para que hable con el compañero.
4. **No cambies el stack** (sección 2) ni la estructura de carpetas. No agregues paquetes NuGet fuera de los listados sin anotarlo en la Bitácora con el motivo.
5. Una misión está hecha solo si:
   - `dotnet build` termina **sin errores**;
   - la aplicación arranca;
   - **comprobaste a mano** cada criterio de aceptación de la misión (ejecutando la app, consultando la base o llamando a la API).
   No marques una misión como hecha sin haberla comprobado.
6. Al terminar cada misión: marca su casilla `[x]` en esta guía, haz commit y push (sección 1.3). Al terminar todas tus misiones, agrega tu entrada en la Bitácora.
7. Nunca uses `git push --force`, no borres historial y no subas `bin/`, `obj/`, `.vs/` ni contraseñas reales.
8. Toda la interfaz en **español**, con unidades en cada valor (V, mA, mW, Wh, kWh, Bs). La app tiene que verse **visual y prolija**: usa componentes de MudBlazor (tarjetas, tablas, íconos, chips de color), nada de HTML suelto sin estilo.
9. Si esta guía tiene un error o una instrucción imposible, no la ignores en silencio: haz lo más razonable, y explica qué cambiaste y por qué en la Bitácora.

---

## 1. Preparación (la hace la persona, una sola vez)

### 1.1 Instalar
- **Git** y una cuenta de GitHub con acceso al repositorio (acepta la invitación que te llega por correo, si el repositorio es privado).
- **SDK de .NET 10** (`dotnet --list-sdks` tiene que mostrar una versión 10.x).
- **SQL Server Express 2022** con la instancia por defecto `SQLEXPRESS`, y SQL Server Management Studio para mirar la base.
- **Visual Studio** (Insiders o 2022 con soporte para .NET 10) o VS Code.
- Herramienta de EF Core: `dotnet tool install --global dotnet-ef`

### 1.2 Clonar el repositorio
```bash
git clone https://github.com/REPO_URL.git
cd SAGE
git config user.name "Tu Nombre"
git config user.email "tu-correo-de-github@ejemplo.com"
```
Si ya lo clonaste antes, entra a la carpeta y haz `git pull` antes de empezar.

### 1.3 Cómo se sube el trabajo (lo hace el agente)
Todos trabajan sobre la rama `main`, uno después del otro.
```bash
git pull                      # siempre antes de empezar
# ... trabajo de la misión ...
git add -A
git commit -m "S1-M2: gestión de empresas (Harold)"
git push
```
- Un commit por misión como mínimo, con el formato `S1-M<n>: <qué hace> (<nombre>)`.
- Si `git push` es rechazado porque hay cambios nuevos: `git pull --rebase`, resolver y volver a hacer push. Nunca `--force`.
- Las migraciones de EF Core **sí** se suben (carpeta `Data/Migrations`).

---

## 2. Stack y estructura (no se cambia)

| Pieza | Elección |
|---|---|
| Plataforma | Blazor Web App, .NET 10, C#, interactividad **Server** global |
| Componentes visuales | MudBlazor (última estable) |
| Base de datos | SQL Server Express 2022 + Entity Framework Core 10 |
| Autenticación | ASP.NET Core Identity (cuentas individuales) con roles |
| Excel | ClosedXML |
| Simulador | Aplicación de consola .NET 10 |

```text
SAGE/
├─ GUIA_TRABAJO_SPRINT_1.md      ← este archivo
├─ README.md
├─ docs/                          ← ERS, diseño técnico, casos de uso (solo lectura)
├─ SAGE.slnx                      (o SAGE.sln)
└─ src/
   ├─ SAGE.Web/                   ← la plataforma (Blazor)
   │  ├─ Components/Pages/...     ← una carpeta por módulo: Empresas, Usuarios, Dispositivos, Facturas
   │  ├─ Data/                    ← ApplicationDbContext, entidades, Migrations, DatosIniciales
   │  └─ Api/                     ← endpoints para el dispositivo
   └─ SAGE.Simulador/             ← consola que imita al ESP32
```

**Cadena de conexión** (igual para todos, en `appsettings.json`):
```
Server=.\SQLEXPRESS;Database=SAGE;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true
```

**Roles** (nombres exactos): `Administrador`, `Tecnico`, `Consulta`.

| Pantalla / acción | Administrador | Tecnico | Consulta |
|---|---|---|---|
| Inicio (resumen) | ✔ todas las empresas | ✔ su empresa | ✔ su empresa |
| Empresas: ver, crear, editar, desactivar | ✔ | ✘ | ✘ |
| Usuarios: ver, crear, editar, desactivar | ✔ | ✘ | ✘ |
| Dispositivos: crear, editar, desactivar, emitir token | ✔ | ✘ | ✘ |
| Dispositivos: ver la lista | ✔ todos | ✔ su empresa | ✔ su empresa |
| Facturas: registrar a mano o desde Excel | ✔ (elige la empresa) | ✔ su empresa | ✘ |
| Facturas: ver la lista | ✔ todas | ✔ su empresa | ✔ su empresa |

El **Administrador** opera la plataforma SAGE y no pertenece a ninguna empresa (`EmpresaId = null`). Técnico y Consulta siempre pertenecen a una empresa y **nunca ven datos de otra** (RNF01): el filtro se aplica en la consulta a la base, no solo ocultando botones.

---

## 3. Harold — Base del proyecto y empresas

### Verificación de entrada
- El repositorio clona bien y contiene `GUIA_TRABAJO_SPRINT_1.md`, `README.md` y `docs/`.

### - [x] S1-M1 · Solución base, base de datos, login y roles
**Qué hacer**
1. Crear la solución y el proyecto web en `src/SAGE.Web` con la plantilla Blazor Web App, autenticación individual, interactividad Server global y SQL Server. Referencia (revisa los flags con `dotnet new blazor --help` si alguno cambió):
   ```bash
   dotnet new sln -n SAGE
   dotnet new blazor -n SAGE.Web -o src/SAGE.Web --auth Individual --interactivity Server --all-interactive --use-local-db
   dotnet sln add src/SAGE.Web
   ```
   (El `.gitignore` de .NET ya está en el repositorio.)
2. Cambiar la cadena de conexión a la de la sección 2 y crear la base con `dotnet ef database update`.
3. Instalar MudBlazor y configurarlo según su guía de instalación (servicios, CSS, JS, `MudThemeProvider`, `MudDialogProvider`, `MudSnackbarProvider`, `MudPopoverProvider`). Reemplazar el layout de la plantilla por un layout MudBlazor: `MudAppBar` con el nombre **SAGE** y el usuario conectado, `MudDrawer` con el menú lateral y un tema propio (primario verde energía `#2E7D32`, secundario ámbar solar `#F9A825`).
4. Agregar roles a Identity (`AddRoles<IdentityRole>()`). Agregar a `ApplicationUser` las propiedades `NombreCompleto` (string, obligatorio) y `Activo` (bool, true por defecto). (`EmpresaId` se agrega en S1-M2, junto con la entidad `Empresa`.)
5. Crear `Data/DatosIniciales.cs`, que al arrancar en Development cree (si no existen) los 3 roles y el usuario Administrador, con el correo confirmado. En S1-M2, cuando ya exista `Empresa`, agregar ahí la empresa de ejemplo y los otros 2 usuarios:

   | Correo | Contraseña | Rol | Empresa |
   |---|---|---|---|
   | admin@sage.test | Sage2026! | Administrador | — |
   | tecnico@sage.test | Sage2026! | Tecnico | Empresa Demo S.R.L. |
   | consulta@sage.test | Sage2026! | Consulta | Empresa Demo S.R.L. |

   (Son datos de prueba para desarrollo, no contraseñas reales.)
6. Desactivar el registro público (los usuarios los crea el Administrador en la misión S1-M3): quitar el enlace "Register" y bloquear la página de registro. Un usuario con `Activo = false` no puede iniciar sesión.
7. Traducir al español lo visible de la pantalla de inicio de sesión (títulos, etiquetas, botón y mensajes de error).
8. El menú lateral muestra solo las opciones del rol según la tabla de la sección 2 (usar `AuthorizeView Roles="..."`). Por ahora las opciones que todavía no existen (Usuarios, Dispositivos, Facturas) pueden quedar comentadas en el menú; cada compañero las activa en su misión.
9. Página de Inicio (`/`) con `[Authorize]`: saludo con el nombre y rol del usuario, y un texto breve de qué es SAGE. (Ethan la convierte en tablero en S1-M8.)
10. Completar `README.md` con: requisitos, cómo crear la base, cómo ejecutar y los usuarios de prueba.

**Criterios de aceptación**
- [x] `dotnet build` sin errores y `dotnet run --project src/SAGE.Web` abre la app.
- [x] La base `SAGE` existe en `.\SQLEXPRESS` con las tablas de Identity y la migración está subida al repositorio.
- [x] Sin iniciar sesión, cualquier página redirige al login.
- [x] `admin@sage.test` inicia sesión y ve su menú; una contraseña incorrecta muestra un error en español.
- [x] No existe forma de registrarse sin ser Administrador.
- [x] La interfaz usa MudBlazor con el tema de SAGE y está en español.

### - [x] S1-M2 · RF01 Gestión de empresas
**Qué hacer**
1. Entidad `Empresa`: `Id`, `Nombre` (obligatorio, máx. 150, único), `Nit` (obligatorio, máx. 20, único), `Direccion` (opcional), `Telefono` (opcional), `CorreoContacto` (opcional, formato email), `Activa` (bool, true por defecto), `FechaRegistro` (fecha UTC). Relación: un `ApplicationUser` tiene `EmpresaId` opcional que apunta a `Empresa`. Crear la migración.
2. Página `/empresas` solo para `Administrador`:
   - `MudTable` con búsqueda por nombre o NIT, columnas Nombre, NIT, Teléfono, cantidad de usuarios y un `MudChip` de estado (Activa verde / Inactiva gris).
   - Botón "Nueva empresa" que abre un `MudDialog` con el formulario y validaciones (campos obligatorios, NIT y nombre únicos con mensaje claro).
   - Acciones por fila: Editar (mismo diálogo) y Desactivar/Activar (con confirmación).
   - `MudSnackbar` de éxito o error después de cada acción.
3. No se borran empresas: solo se desactivan. Al desactivar una empresa, sus usuarios Técnico y Consulta ya no pueden iniciar sesión.
4. Activar la opción "Empresas" en el menú del Administrador.
5. Completar `DatosIniciales.cs` con la empresa "Empresa Demo S.R.L." y los usuarios `tecnico@sage.test` y `consulta@sage.test` de la tabla de S1-M1.

**Criterios de aceptación**
- [x] Los 3 usuarios de prueba inician sesión, y cada uno ve en el menú solo lo que le corresponde según la tabla de la sección 2.
- [x] Una empresa creada aparece en la lista sin recargar la página.
- [x] No se puede guardar una empresa sin nombre o NIT, ni con un NIT o nombre repetido.
- [x] Editar cambia los datos y desactivar cambia el chip a "Inactiva".
- [x] Un usuario de una empresa inactiva no puede iniciar sesión.
- [x] Técnico y Consulta no ven la opción en el menú, y si escriben `/empresas` en la barra reciben "Acceso denegado".
- [x] `ApplicationUser` tiene `EmpresaId` (int?, clave foránea a `Empresa`), así que en la siguiente misión se les puede asociar una empresa (y en S1-M4, dispositivos).

**Al terminar:** marca S1-M1 y S1-M2 con `[x]`, escribe tu entrada en la Bitácora y haz push. Avísale a Matías.

---

## 4. Matías — Usuarios, dispositivos y API de telemetría

### Verificación de entrada
- `git pull`, `dotnet ef database update --project src/SAGE.Web` y `dotnet run`: la app arranca.
- Los 3 usuarios de prueba entran, y cada uno ve solo su menú.
- En `/empresas` se puede crear una empresa.
- Si algo de esto falla, sigue la regla 3 de la sección 0.

### - [ ] S1-M3 · RF02 Gestión de usuarios y roles
**Qué hacer**
1. Página `/usuarios` solo para `Administrador`:
   - `MudTable` con búsqueda, columnas Nombre completo, Correo, Rol (chip de color por rol), Empresa y Estado.
   - Filtro por empresa y por rol.
2. Diálogo "Nuevo usuario": Nombre completo, Correo, Contraseña inicial, Rol (`MudSelect` con los 3 roles) y Empresa (`MudSelect` con las empresas activas).
   - Si el rol es `Administrador`, el campo Empresa se oculta y se guarda `null`.
   - Si el rol es `Tecnico` o `Consulta`, la empresa es obligatoria.
   - Usar `UserManager` (no insertar a mano en las tablas de Identity). El correo queda confirmado al crearlo.
   - Mostrar los errores de Identity en español (correo repetido, contraseña débil).
3. Editar: cambiar nombre, rol y empresa (con las mismas reglas). Desactivar/Activar con confirmación (`Activo = false` impide iniciar sesión). Botón "Restablecer contraseña" que pone una contraseña nueva escrita por el Administrador.
4. Un Administrador no puede desactivarse ni quitarse el rol a sí mismo.
5. Activar "Usuarios" en el menú del Administrador.

**Criterios de aceptación**
- [ ] El Administrador crea un usuario Técnico de una empresa nueva; ese usuario inicia sesión y ve solo el menú de Técnico.
- [ ] No se puede crear un Técnico o Consulta sin empresa, ni un usuario con un correo repetido.
- [ ] Un usuario desactivado no puede iniciar sesión.
- [ ] Técnico y Consulta reciben "Acceso denegado" en `/usuarios`.
- [ ] Cada rol accede solo a las funciones de la tabla de la sección 2 (probado con los 3 roles).

### - [ ] S1-M4 · RF09 Registro de dispositivos y token
**Qué hacer**
1. Entidad `Dispositivo`: `Id`, `EmpresaId` (obligatorio), `Nombre` (obligatorio, máx. 100), `Descripcion` (opcional), `EsSimulado` (bool: true si los datos vienen del simulador; la interfaz lo rotula como **SIMULADO**), `TokenHash` (string), `Activo` (bool), `FechaRegistro`, `UltimoContacto` (DateTime? UTC), y el último estado reportado: `ModoActual`, `EstadoActual`, `FuenteSeleccionada` (strings, opcionales) y `AlimentacionComprobada` (bool?). Crear la migración.
2. Página `/dispositivos`:
   - Administrador: ve todos, con filtro por empresa; crea, edita y desactiva.
   - Técnico y Consulta: ven solo los de su empresa, sin botones de acción.
   - Columnas: Nombre, Empresa, chip SIMULADO / REAL, Estado (Activo/Inactivo) y Última conexión ("hace 5 s", "hace 3 min" o "Nunca"), con un punto verde si el último contacto fue hace menos de 30 s y rojo si no.
3. Token del dispositivo (RNF08):
   - Al crear el dispositivo, o con el botón "Regenerar token", se generan 32 bytes aleatorios con `RandomNumberGenerator` y se codifican en Base64Url. En la base se guarda **solo el hash SHA-256** del token, nunca el token.
   - El token se muestra **una sola vez** en un diálogo con un botón para copiarlo y el aviso: "Guárdalo ahora: no se podrá volver a ver". Regenerar invalida el token anterior.
4. Activar "Dispositivos" en el menú de los 3 roles.

**Criterios de aceptación**
- [ ] El Administrador crea un dispositivo asociado a una empresa y ve su token una sola vez.
- [ ] En la tabla `Dispositivos` de la base solo hay un hash, no el token.
- [ ] Técnico y Consulta ven solo los dispositivos de su empresa y no tienen botones de acción.
- [ ] Regenerar el token hace que el token anterior deje de servir (se comprueba en S1-M5).

### - [ ] S1-M5 · API de telemetría del dispositivo
**Qué hacer**
1. Entidad `Medicion` con los campos del contrato (ver `docs/SAGE_IoT_diseno_v1.md`, secciones 6.2 y 6.3): `Id` (long), `DispositivoId`, `BootId`, `Seq`, `Ts` (hora del dispositivo), `TsValida`, `TsServidor`, `VSol`, `ISolMa`, `PSolMw`, `WhSolAcum`, `VRed`, `IRedMa`, `PRedMw`, `WhRedAcum`, `Modo`, `Estado`, `FuenteSeleccionada`, `AlimentacionComprobada`. Índice **único** en `(DispositivoId, BootId, Seq)`. Crear la migración.
   (En el JSON la red se llama `cargador`, porque en la maqueta la "red" es un cargador USB de 5 V; en la base y en la interfaz se llama **Red**.)
2. Endpoint Minimal API en `Api/TelemetriaEndpoints.cs`: `POST /api/dispositivo/telemetria`.
   - Sin autenticación por cookie (`AllowAnonymous` y sin antiforgery): se autentica con el encabezado `Authorization: Bearer <token>`. Se calcula el SHA-256 del token y se busca el dispositivo por `TokenHash`.
   - Responde `401` si falta el token, si no existe o si el dispositivo o su empresa están inactivos. Responde `400` si el JSON es inválido o le faltan campos.
   - Si todo está bien: guarda la `Medicion`, actualiza `UltimoContacto` y el último estado del `Dispositivo`, y responde `200` con `{ "hora_servidor": "<UTC ISO 8601>", "orden": null }`. (Las órdenes llegan en el Sprint 3; `orden` siempre es `null` por ahora.)
   - Si llega otra vez el mismo `(boot_id, seq)`, no se duplica la medición y se responde `200` igual.
   - Los arreglos `eventos` y `resultados_ordenes` se aceptan pero todavía no se guardan (Sprint 3).
   - Si `ts_valida` es `false`, se usa la hora del servidor para `Ts`.
3. Dejar en `docs/api_telemetria.http` (o una colección de Postman) un ejemplo de petición válida, para que Ethan pueda probarla.

**Criterios de aceptación**
- [ ] Con el token correcto y el JSON de ejemplo de `docs/SAGE_IoT_diseno_v1.md` (sección 6.2), la API responde 200 y aparece una fila en `Mediciones`.
- [ ] Sin token, o con un token falso o regenerado, responde 401 y no guarda nada.
- [ ] Con un dispositivo desactivado responde 401.
- [ ] Mandar dos veces el mismo `seq` deja una sola fila.
- [ ] La columna "Última conexión" de `/dispositivos` muestra "hace X s" después de una petición.

**Al terminar:** marca S1-M3, S1-M4 y S1-M5 con `[x]`, escribe tu entrada en la Bitácora y haz push. Avísale a Ethan.

---

## 5. Ethan — Simulador, facturas, tablero y cierre del sprint

### Verificación de entrada
- `git pull`, `dotnet ef database update --project src/SAGE.Web` y `dotnet run`: la app arranca.
- Como Administrador: se crea un usuario Técnico y un dispositivo, y se obtiene su token.
- Con `docs/api_telemetria.http` (o Postman) y ese token, la API responde 200 y guarda la medición; con un token falso responde 401.
- Si algo de esto falla, sigue la regla 3 de la sección 0.

### - [ ] S1-M6 · T02 Simulador del dispositivo
**Qué hacer**
1. Crear la consola `src/SAGE.Simulador` (.NET 10) y agregarla a la solución.
2. Parámetros por línea de comandos: `--url` (por ejemplo, `http://localhost:5xxx`), `--token` e `--intervalo` (segundos, 2 por defecto). Si falta `--url` o `--token`, mostrar cómo se usa y salir.
3. Cada `intervalo` segundos hace `POST {url}/api/dispositivo/telemetria` con `Authorization: Bearer {token}` y el JSON **exactamente** con el formato de la sección 6.2 del diseño (mismos nombres de campos en snake_case).
4. Datos creíbles para la maqueta:
   - `boot_id` aleatorio al arrancar; `seq` empieza en 1 y sube de a uno; `ts` es la hora UTC actual y `ts_valida` es `true`.
   - Modo `AUTO`. La irradiancia sigue una curva por la hora del día (cero de noche, máxima al mediodía) con ruido aleatorio, y de vez en cuando pasa una "nube" de 10 a 30 s.
   - Con sol suficiente: `estado` = `EN_SOLAR`, `fuente_seleccionada` = `SOLAR`, el solar entrega cerca de 4,9 V y 150 a 200 mA, y el cargador queda cerca de 0 mA. Sin sol suficiente: `EN_CARGADOR` / `CARGADOR`, con el cargador cerca de 5,05 V y 150 a 200 mA.
   - `p_mw` = `v` × `i_ma`; `wh_acum` de cada fuente acumula `p_mw / 1000 × intervalo / 3600` y nunca baja.
   - `eventos` y `resultados_ordenes` se envían vacíos.
5. En consola, una línea por envío: hora, `seq`, fuente, potencia y código HTTP. Si el servidor no responde, mostrar el error y seguir intentando (no cerrarse).
6. Agregar al `README.md` cómo ejecutarlo:
   ```bash
   dotnet run --project src/SAGE.Simulador -- --url http://localhost:5xxx --token <TOKEN>
   ```

**Criterios de aceptación**
- [ ] Con un dispositivo marcado como SIMULADO y su token, en 60 s se guardan unas 30 mediciones (una cada 2 s), comprobado con `SELECT COUNT(*)` en `Mediciones`.
- [ ] Con un token falso el simulador muestra 401 en cada envío y no se guarda nada.
- [ ] Si se apaga la plataforma, el simulador sigue intentando y vuelve a guardar cuando la plataforma arranca de nuevo.
- [ ] Los `wh_acum` guardados nunca bajan dentro de un mismo `boot_id`.

### - [ ] S1-M7 · RF03 Registro de consumo facturado
**Qué hacer**
1. Entidad `Factura`: `Id`, `EmpresaId`, `Anio`, `Mes` (1 a 12), `ConsumoKwh` (decimal > 0), `MontoBs` (decimal > 0), `Origen` (`Manual` o `Excel`), `RegistradaPorId` (usuario), `FechaRegistro`. Índice **único** en `(EmpresaId, Anio, Mes)`: una factura por empresa y mes. Crear la migración.
2. Página `/facturas`:
   - Tabla con Periodo ("Octubre 2026"), Consumo (kWh), Monto (Bs), Bs/kWh calculado, Origen y quién la registró. El Administrador ve todas, con filtro por empresa; Técnico y Consulta ven solo las de su empresa.
   - Encima de la tabla, dos tarjetas: "Consumo últimos 12 meses (kWh)" y "Monto últimos 12 meses (Bs)".
3. Botón "Registrar factura" (Administrador y Técnico) con un diálogo: empresa (solo el Administrador la elige; el Técnico usa la suya), mes, año, kWh y monto. Validaciones: campos obligatorios, kWh y monto mayores que 0, el periodo no puede ser futuro y no puede repetirse un mes ya registrado (mensaje: "Ya existe la factura de ese mes").
4. Botón "Importar desde Excel" (Administrador y Técnico) con `MudFileUpload` (.xlsx, máximo 1 MB), leído con ClosedXML.
   - Formato: primera fila de encabezados `Año | Mes | Consumo (kWh) | Monto (Bs)`, una factura por fila.
   - Se validan **todas** las filas antes de guardar. Si hay alguna inválida, **no se guarda ninguna** y se muestra la lista de errores con el número de fila (por ejemplo: "Fila 4: el monto debe ser mayor que 0"; "Fila 7: el mes 10/2026 ya está registrado").
   - Si todas son válidas, se guardan con `Origen = Excel` y se muestra "Se importaron N facturas".
   - Botón "Descargar plantilla" que entrega un .xlsx con los encabezados y una fila de ejemplo.
5. Crear `docs/ejemplos/facturas_validas.xlsx` (6 meses correctos) y `docs/ejemplos/facturas_con_errores.xlsx` (con al menos 3 errores distintos) para probar.
6. Activar "Facturas" en el menú de los 3 roles (Consulta solo ve la tabla, sin botones).

**Criterios de aceptación**
- [ ] Una factura cargada a mano aparece en la tabla de su empresa y queda guardada por empresa y mes.
- [ ] `facturas_validas.xlsx` importa 6 facturas; `facturas_con_errores.xlsx` no importa ninguna y lista cada error con su fila.
- [ ] Se rechazan: kWh o monto ≤ 0, mes fuera de 1 a 12, periodo futuro y mes repetido.
- [ ] El Técnico de una empresa no ve las facturas de otra, y Consulta no tiene botones para registrar ni importar.

### - [ ] S1-M8 · Tablero de inicio y cierre del sprint
**Qué hacer**
1. Convertir la página de Inicio en un tablero visual con MudBlazor:
   - Tarjetas con ícono: Empresas activas (solo el Administrador), Usuarios (solo el Administrador), Dispositivos (en línea / total), Facturas registradas.
   - Tarjeta "Última medición" por dispositivo de la empresa (o de todas, para el Administrador): potencia solar y potencia de red en W, fuente seleccionada con chip ámbar (Solar) o azul (Red), hace cuánto llegó y el chip SIMULADO si corresponde. Se refresca sola cada 5 s (con un `PeriodicTimer` o un `Timer`, liberado en `Dispose`).
2. Revisar RNF01 de punta a punta con los 3 roles y con un segundo usuario Técnico de **otra** empresa: ninguna página ni consulta muestra datos de otra empresa. Corregir lo que falle y anotarlo en la Bitácora.
3. Completar la sección 7 de esta guía (Demo del Sprint 1), comprobando cada paso.

**Criterios de aceptación**
- [ ] Con el simulador corriendo, la tarjeta "Última medición" cambia sola sin recargar la página.
- [ ] Un Técnico de la empresa B no ve en ninguna pantalla nada de la empresa A.
- [ ] Todos los pasos de la sección 7 funcionan, y todas las casillas de esta guía están marcadas.

**Al terminar:** marca S1-M6, S1-M7 y S1-M8 con `[x]`, escribe tu entrada en la Bitácora y haz push. Avísale a Mateo que el Sprint 1 está listo para revisar.

---

## 6. Bitácora

Cada agente agrega **una entrada al terminar** (y otra si deja un BLOQUEO). No borres entradas de otros.

```text
### <fecha y hora> — <Nombre> — <misiones>
- Hecho: ...
- Correcciones a trabajo anterior: ... (o "ninguna")
- Desvíos de la guía y motivo: ... (o "ninguno")
- Pendientes o BLOQUEOS: ... (o "ninguno")
- Último commit: <hash corto>
```

### 2026-11-05 00:50 (UTC-4) — Harold — S1-M1, S1-M2
- Hecho: solución Blazor .NET 10 con Identity, roles, MudBlazor (tema SAGE), login en español, seed de usuarios, gestión de empresas en `/empresas`, README, migración `InitialCreate`, marca Gauge y pulido de UI.
- Correcciones a trabajo anterior: ninguna
- Desvíos de la guía y motivo: en macOS no hay SQL Server Express `.\SQLEXPRESS`; se usa Azure SQL Edge en Docker (`localhost,1433`, autenticación `sa` / `SageSql2026!`) y Rider en lugar de Visual Studio. Paleta primaria azul Gauge (`#1565C0`) en lugar del verde `#2E7D32` de la guía, alineada al logo. Empresa de prueba: Energía Andina S.R.L. El stack (.NET 10, Blazor Server, EF Core, MudBlazor, Identity) se mantiene.
- Pendientes o BLOQUEOS: ninguno
- Último commit: 55c772e

<!-- Las entradas van debajo de esta línea -->

---

## 7. Demo del Sprint 1 (la completa Ethan en S1-M8)

Guion para mostrarle al docente, en este orden:

- [ ] 1. Iniciar sesión como `admin@sage.test` → el menú muestra Empresas, Usuarios, Dispositivos y Facturas.
- [ ] 2. Crear la empresa "Hotel Las Palmas" y un usuario Técnico para ella.
- [ ] 3. Registrar un dispositivo SIMULADO para esa empresa y copiar su token.
- [ ] 4. Ejecutar el simulador con ese token → en Inicio, la tarjeta de última medición se actualiza cada pocos segundos, y en la base aumentan las filas de `Mediciones`.
- [ ] 5. Cerrar sesión, entrar con el nuevo Técnico → solo ve su empresa; importar `facturas_validas.xlsx` y registrar una factura a mano → aparecen en la tabla.
- [ ] 6. Intentar importar `facturas_con_errores.xlsx` → se rechaza con la lista de errores.
- [ ] 7. Entrar con `consulta@sage.test` → ve solo los datos de Empresa Demo S.R.L., sin botones para modificar; escribir `/empresas` en la barra → "Acceso denegado".
