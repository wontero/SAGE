# SAGE: diseño de monitoreo y control de fuentes (v1)

Universidad Privada del Valle · Proyecto SAGE · Borrador del 19/10/2026

**Convención usada en todo el documento**

- **[C] Confirmado:** consta en los documentos originales o en las condiciones acordadas.
- **[P] Propuesto:** recomendación de este diseño; falta que el equipo lo apruebe.
- **[D] Pendiente:** decisión abierta; se indica el valor que se usará por defecto.
- **[V] A verificar:** solo se confirma con mediciones o cotizaciones.

No existen todavía firmware, aplicación, circuito ni prototipo. Todo lo que sigue es diseño.

---

## 0. Decisiones abiertas (cerrarlas primero)

| # | Decisión | Opción por defecto [P] | Alternativa | Por qué importa |
|---|---|---|---|---|
| D1 | Presupuesto máximo | Usar la lista mínima de la sección 2 y cotizarla localmente | Ninguna | Determina si se usa relé o selector de estado sólido |
| D2 | Qué pasa si se pide **Solar** y no alcanza | Rechazar la orden si la comprobación previa falla. Si falla durante la operación, pasar al cargador como protección, dejar la falla visible y **no** reintentar solar | Mantener solar aunque la lámpara se apague | Define la máquina de estados y los mensajes de la app |
| D3 | Selector de fuente | **Relé SPDT** (contacto conmutado) | Selector electrónico de estado sólido (TPS2116 o par de MOSFET P) | El relé es barato, se consigue en el mercado local y no puede conectar ambas fuentes a la vez |
| D4 | Tecnologías del software | La que el equipo ya domine. Sugerencia: backend en Python (FastAPI) o Node, PostgreSQL y frontend web adaptable | — | El contrato de la API y el modelo de datos de este documento no dependen de esa elección |
| D5 | Lugar de la demostración | Exterior o junto a una ventana con sol, y una lámpara halógena como respaldo | Solo en interiores | Un mini panel casi no genera con iluminación de oficina o LED |

---

## 1. Alcance viable

### 1.1 Qué incluye la v1 [P]

- Una sola carga pequeña de corriente continua a **5 V** y alrededor de **1 W**: una lámpara LED USB.
- Dos fuentes: **panel solar → convertidor reductor a 5 V** y **cargador USB de 5 V**, que representa a la red.
- **Una fuente a la vez**. Las fuentes nunca se conectan en paralelo.
- Medición de tensión, corriente y potencia en **ambas ramas**, más energía acumulada (Wh) por fuente.
- Tres modos (Solar, Cargador y Automático), con reglas ejecutadas **en el ESP32**.
- Plataforma web: monitoreo, órdenes con confirmación, historial de mediciones y conmutaciones, alertas y ensayos.
- Las simulaciones económicas originales (RF05 a RF07) se conservan y se presentan **separadas** de lo medido.

### 1.2 Qué queda fuera de la v1 [C/P]

Queda fuera: batería, carga de baterías, MPPT, inversor, conexión a la red, corriente alterna, combinación simultánea de fuentes, transferencia sin interrupción garantizada y medición del consumo del cargador en el enchufe.

### 1.3 Limitaciones que deben decirse en la defensa [C]

- La salida del cargador **no equivale** a consumo de la red: no incluye las pérdidas del propio cargador.
- Tener tensión en el panel sin carga no demuestra que pueda sostener la carga. Por eso la v1 valida la fuente **con carga** (sección 4).
- Los Wh de la maqueta no se extrapolan al ahorro de una empresa. Las proyecciones empresariales son simulaciones con supuestos explícitos.

### 1.4 Tres estados distintos de la fuente [C]

| Concepto | Quién lo determina | Ejemplo |
|---|---|---|
| **Solicitada** | La orden del usuario o el modo activo | "El usuario pidió Solar" |
| **Seleccionada** | La posición física del relé, según el ESP32 | "El relé está en NO (solar)" |
| **Comprobada** | Las mediciones de la rama seleccionada: V ≥ V_min **e** I > I_min | "La rama solar entrega 4,9 V y 190 mA" |

La aplicación muestra las tres. Que no coincidan es justamente lo que el sistema debe detectar y registrar.

---

## 2. Hardware y lista de materiales [P]

Las cantidades son para un prototipo. Los precios se dejan **[V] por cotizar en Santa Cruz**: no se inventan.

| # | Componente | Cant. | Función | Notas de selección |
|---|---|---|---|---|
| 1 | ESP32 DevKit V1 (ESP32-WROOM-32) | 1 | Control, medición y Wi-Fi | Es el módulo más común en el mercado local |
| 2 | Módulo INA219 (GY-219 / CJMCU-219, derivación R100) | 2 | V, I y P por rama | Bus I²C; direcciones 0x40 y 0x41 (puentear A0 en uno) |
| 3 | Módulo de relé de 1 canal a 5 V, **activo en alto** | 1 | Selector de fuente | [V] Debe conmutar con 3,3 V. Si no lo hace: transistor NPN 2N2222, resistencia de 1 kΩ y diodo 1N4148 |
| 4 | Mini panel solar de **9 V o 12 V, 2 a 3 W** | 1 | Fuente solar | **No usar uno de 6 V**: el reductor necesita unos 6,5 V de entrada para entregar 5 V |
| 5 | Convertidor reductor ajustable MP1584EN (o LM2596) | 1 | Acondiciona el panel a 5,0 V | Entrada de hasta 28 V; cubre la tensión en vacío de un panel de 12 V |
| 6 | Cargador USB de 5 V y 1 A o más, con *breakout* USB macho o cable cortado | 1 | Fuente "convencional" | |
| 7 | Lámpara LED USB de 5 V y ~1 W, con *breakout* USB hembra | 1 | Carga | Unos 200 mA; entra en el rango de 400 mA del INA219 |
| 8 | Fuente aparte para el ESP32 (USB de laptop o *power bank*) | 1 | Mantener el control si falla la fuente ensayada | **GND común** con todo el circuito |
| 9 | Fusible rápido de 500 mA o PTC, con portafusible | 2 | Uno por rama | Protección de bajo costo |
| 10 | Capacitor electrolítico de 1000 µF y 10 V | 1 | Cubre el hueco de conmutación del relé | Opcional; se evalúa en el ensayo E2 |
| 11 | Protoboard, cables dupont y borneras | — | Montaje | |
| — | Multímetro | 1 | Validación | Se usa uno disponible |

**Opcional (Could):** un divisor de 100 kΩ y 22 kΩ hacia una entrada ADC para leer la tensión del panel antes del reductor. Mejora la detección de sombra en el gráfico, pero no es necesario para el control.

**Por qué un relé y no el TPS2116 en la v1:** el TPS2116 viene en un encapsulado SMD diminuto, rara vez se vende como módulo en el mercado local y solo admite hasta 5,5 V de entrada. El relé SPDT, por construcción, **nunca une las dos fuentes**: abre un contacto antes de cerrar el otro. Además se oye cuando conmuta, lo que ayuda en la demostración. Su costo es un corte de unos 5 a 15 ms en la conmutación y desgaste mecánico. El selector de estado sólido queda como mejora para la v2.

---

## 3. Circuito de medición y selección [P]

### 3.1 Esquema

```text
                       F1                                  ┌─────────── RELÉ SPDT ───────────┐
Panel 9–12V ─► Reductor 5,0V ─►─[fusible]─► INA219 #1 (0x40) ─► NO                            │
                                            VIN+ → VIN−        │                            COM ─►─ (+) Lámpara 5V (−) ─┐
Cargador USB 5V ─────────────►─[fusible]─► INA219 #2 (0x41) ─► NC                            │                       │
                       F2                   VIN+ → VIN−        └──────────── bobina ─────────┘                       │
                                                                       ▲                                              │
ESP32 (alimentado aparte por USB)                                      │ GPIO26 (activo en alto)                      │
  3V3 ─► VCC de ambos INA219                                           │                                              │
  GPIO21 SDA ─► SDA de ambos INA219                                    │                                              │
  GPIO22 SCL ─► SCL de ambos INA219                                    │                                              │
  5V/VIN ─► VCC del relé                                               │                                              │
  GND ═══════════════════ GND COMÚN: panel/reductor −, cargador −, INA219, relé, lámpara − ══════════════════════════┘
```

### 3.2 Reglas de diseño

- **Relé en reposo (NC) = cargador.** Si el ESP32 se reinicia, se cuelga o no recibe alimentación, la carga queda en la fuente convencional. Ese es el estado seguro.
- **GPIO26** no interviene en el arranque del ESP32 (se evitan GPIO 0, 2, 12 y 15). Con un módulo activo en alto, el relé permanece en reposo durante el arranque. [V] Comprobar que no haga clic al encender.
- Los INA219 miden en el **lado alto**, antes del relé. La rama no seleccionada muestra su tensión **en vacío** y una corriente cercana a 0.
- Rango del INA219: **16 V / 400 mA** para tener mejor resolución con una carga de ~200 mA. [V] Calibrar contra el multímetro (ensayo E1).
- Tensión máxima en el circuito de control: 5 V. El panel solo llega al reductor; los INA219 nunca ven la tensión en vacío del panel.

### 3.3 Comportamiento esperado al sombrear el panel [V]

Cuando la potencia del panel no alcanza, la tensión de entrada del reductor cae y su salida baja de 5 V. Puede oscilar, porque el reductor entra y sale de su bloqueo por baja tensión, y la lámpara parpadea. El firmware decide sobre **promedios** y con **tiempo sostenido**, no sobre muestras sueltas.

---

## 4. Modos y máquina de estados [P]

### 4.1 Parámetros iniciales

Son valores de partida. Se ajustan en el ensayo E3 con el panel y la carga reales. La plataforma solo acepta valores dentro de los límites indicados.

| Parámetro | Valor inicial | Límites | Significado |
|---|---|---|---|
| V_min | 4,50 V | 4,0 a 4,9 | Tensión mínima **con carga** para considerar válida una fuente |
| V_retorno | 4,85 V | V_min + 0,2 a 5,3 | Tensión de la rama solar en vacío necesaria para intentar el regreso (histéresis) |
| I_min | 20 mA | 5 a 100 | Por debajo se considera que no hay consumo |
| T_falla | 1,0 s | 0,5 a 5 | Tiempo sostenido bajo V_min antes de abandonar solar |
| T_estable | 30 s | 10 a 300 | Tiempo sostenido de solar ≥ V_retorno antes de intentar el regreso |
| T_prueba | 5 s | 2 a 30 | Tiempo que solar debe sostener la carga para quedar "comprobada" |
| Espera tras fallo | 30 → 60 → 120 → 300 s | Máximo 600 s | Se duplica en cada intento fallido y se reinicia tras 10 min estable en solar |
| Muestreo | 10 Hz | — | Promedio de 1 s para decidir |

> **Por qué hacer una prueba con carga:** con la lámpara conectada al cargador, la rama solar está en vacío y su reductor puede marcar 5 V aunque haya poca luz. La única forma honesta de saber si solar sostiene la carga es **probarla**. La espera creciente evita que la lámpara parpadee cada pocos segundos cuando el sol no alcanza.

### 4.2 Estados del relé y de la lógica (modo Automático)

| Estado | Relé | Condición | Pasa a | Motivo registrado |
|---|---|---|---|---|
| EN_CARGADOR | NC | Solar ≥ V_retorno durante T_estable **y** venció la espera | PROBANDO_SOLAR | `intento_retorno_solar` |
| EN_CARGADOR | NC | Cargador < V_min durante T_falla **y** solar ≥ V_retorno | PROBANDO_SOLAR (sin esperar) | `cargador_sin_tension` |
| PROBANDO_SOLAR | NO | Solar ≥ V_min durante T_prueba | EN_SOLAR | `solar_comprobada` |
| PROBANDO_SOLAR | NO | Solar < V_min durante T_falla | EN_CARGADOR; duplica la espera | `prueba_solar_fallida` |
| EN_SOLAR | NO | Solar < V_min durante T_falla | EN_CARGADOR | `solar_insuficiente` |
| Cualquiera | — | Ambas ramas < V_min | SIN_ALIMENTACION (relé en NC) y alerta | `sin_fuente_valida` |

Si la corriente de la rama seleccionada es menor que I_min (lámpara desconectada), el estado se marca como `sin_consumo`. Las decisiones solo usan la tensión, y la alimentación no se cuenta como comprobada.

### 4.3 Modos manuales

| Modo | Al recibir la orden | Durante la operación |
|---|---|---|
| **Cargador** | Pone el relé en NC. Se ejecuta siempre | Si el cargador falla, genera una alerta y **no** cambia de fuente: el usuario la fijó |
| **Solar** (D2 por defecto) | Si solar < V_retorno, **rechaza** la orden con motivo `solar_no_disponible`. Si no, pasa por PROBANDO_SOLAR; si la prueba falla, informa `orden_fallida` y vuelve al cargador | Si solar falla, pasa a NC por protección, deja el modo en **Solar en respaldo**, genera una alerta y **no reintenta**. El usuario decide |
| **Automático** | Aplica la tabla 4.2 | Aplica la tabla 4.2 |

### 4.4 Arranque y reinicio

1. Relé en NC (cargador) **siempre** al encender.
2. Lee de la memoria no volátil (`Preferences`) el último modo, los parámetros con su versión y la última orden ejecutada.
3. Espera 3 s de mediciones válidas y aplica el modo guardado. El modo Solar guardado vuelve a pasar por su comprobación previa.
4. Genera un nuevo `boot_id` (contador en memoria no volátil) y registra el evento `reinicio`.
5. Activa un *watchdog* de hardware: si el bucle se cuelga, el ESP32 se reinicia y el relé vuelve a NC.

---

## 5. Requisitos actualizados y matriz MoSCoW [P]

### 5.1 Requisitos funcionales nuevos

| Código | Requisito |
|---|---|
| RF09 | **Registro de dispositivos:** dar de alta un dispositivo, asociarlo a una empresa y emitir su credencial. |
| RF10 | **Monitoreo:** mostrar V, I y P de cada rama, el modo, la fuente solicitada, seleccionada y comprobada, y la última conexión, con un retraso máximo de 5 s. |
| RF11 | **Control de fuente:** enviar órdenes de modo (Solar, Cargador o Automático) y mostrar su resultado: ejecutada, rechazada con motivo o expirada. |
| RF12 | **Conmutación automática local:** el dispositivo aplica las reglas de la sección 4 con histéresis y tiempos de espera. |
| RF13 | **Registro de conmutaciones:** guardar cada cambio con fecha y hora, origen (orden o regla), fuente anterior y nueva, y motivo. |
| RF14 | **Energía por fuente:** acumular y mostrar los Wh entregados por cada fuente. |
| RF15 | **Configuración de reglas:** modificar los umbrales y tiempos de la sección 4.1 dentro de sus límites. |
| RF16 | **Alertas:** solar insuficiente, ninguna fuente válida, cargador sin tensión y dispositivo sin conexión por más de 30 s. |
| RF17 | **Ensayos:** registrar ensayos con inicio, fin, carga y condiciones, y asociarles sus mediciones. |
| RF18 | **Comparación medida:** comparar energía, tiempo de uso y conmutaciones por fuente dentro de un ensayo o un periodo. |

### 5.2 Requisitos no funcionales nuevos

| Código | Requisito |
|---|---|
| RNF07 | **Autonomía:** las reglas del modo Automático funcionan sin Wi-Fi ni servidor. |
| RNF08 | **Seguridad de dispositivos:** cada dispositivo tiene un token propio, guardado como hash. Solo administrador y técnico envían órdenes. Las órdenes no entregadas expiran a los 30 s. |
| RNF09 | **Exactitud:** tensión dentro de ±2 % y corriente dentro de ±5 % (o ±3 mA) respecto del multímetro. [V] Meta por confirmar en E1. |
| RNF10 | **Tiempo de respuesta:** una orden se ejecuta en ≤ 5 s si hay conexión. La reacción local a una falla solar ocurre en ≤ 2 s. |
| RNF11 | **Seguridad eléctrica:** solo baja tensión continua (≤ 22 V en el panel y 5 V en la carga), un fusible por rama, fuentes nunca en paralelo y estado de reposo en el cargador. |
| RNF12 | **Trazabilidad:** distinguir fuente solicitada, seleccionada y comprobada, y separar siempre los datos **medidos** de los **simulados**. |

### 5.3 Matriz MoSCoW revisada

El cambio de prioridad responde a la observación del ingeniero: el centro del proyecto pasa a ser medir **y controlar**. Las funciones de gestión al estilo CRM bajan de prioridad, pero no se eliminan.

| Código | Requisito | Antes | Ahora | Justificación |
|---|---|---|---|---|
| RF01 | Gestión de empresas | Must | **Must** | Los dispositivos pertenecen a una empresa |
| RF02 | Gestión de usuarios y roles | Must | **Must** | Los permisos de control dependen del rol |
| RF03 | Registro de consumo manual o con Excel | Must | **Should** | La medición automática pasa a ser la fuente principal; la carga manual se mantiene para las facturas |
| RF04 | Historial con gráficos | Should | **Must** | Ahora incluye mediciones y conmutaciones, y es lo que se muestra en la demo |
| RF05 | Configuración de escenarios | Must | **Should** | Sigue siendo la base de la proyección empresarial |
| RF06 | Comparación red, solar y mixto (simulada) | Must | **Should** | Se complementa con RF18 (medida) |
| RF07 | Proyecciones económicas | Must | **Should** | Se conserva, presentada como simulación |
| RF08 | Reportes PDF o Excel | Could | **Could** | Sin cambio |
| RF09 | Registro de dispositivos | — | **Must** | |
| RF10 | Monitoreo | — | **Must** | |
| RF11 | Control de fuente | — | **Must** | Es el requisito central |
| RF12 | Conmutación automática local | — | **Must** | |
| RF13 | Registro de conmutaciones | — | **Must** | |
| RF14 | Energía por fuente | — | **Must** | |
| RF15 | Configuración de reglas | — | **Should** | La v1 puede usar valores fijos en el firmware |
| RF16 | Alertas | — | **Should** | |
| RF17 | Ensayos | — | **Should** | |
| RF18 | Comparación medida | — | **Could** | |
| RNF01 | Acceso por empresa y rol | Must | **Must** | |
| RNF02 | Interfaz en español, con unidades | Must | **Must** | |
| RNF03 | Historial en ≤ 3 s con 20 usuarios | Should | **Should** | |
| RNF04 | Navegadores y diseño adaptable | Should | **Should** | |
| RNF05 | Copias de seguridad diarias | Must | **Should** | Las mediciones de una maqueta son recuperables; importa más RNF12 |
| RNF06 | Parámetros sin modificar código | Should | **Should** | Se amplía a los umbrales del dispositivo (RF15) |
| RNF07 | Autonomía sin Wi-Fi | — | **Must** | |
| RNF08 | Seguridad de dispositivos | — | **Must** | |
| RNF09 | Exactitud | — | **Should** | |
| RNF10 | Tiempo de respuesta | — | **Should** | |
| RNF11 | Seguridad eléctrica | — | **Must** | |
| RNF12 | Trazabilidad | — | **Must** | |

---

## 6. Software: arquitectura, comunicación y datos [P]

### 6.1 Arquitectura

```text
ESP32 (firmware Arduino/PlatformIO)
  ├─ Medición 10 Hz, promedios de 1 s, acumulación de Wh
  ├─ Máquina de estados (no depende de la red)
  ├─ Cola de eventos en RAM (los últimos 200)
  └─ Cada 2 s: HTTPS POST /api/dispositivo/telemetria ──► Backend (API REST)
                                   ◄── respuesta: orden pendiente │      │
                                                                  │  PostgreSQL
Navegador (frontend adaptable) ◄── REST + consulta cada 2 s ──────┘
```

**Comunicación recomendada para la v1: solo HTTP, con las órdenes incluidas en la respuesta.** El dispositivo envía telemetría cada 2 s y el servidor contesta con la orden pendiente, si existe. Así no hacen falta un broker, conexiones persistentes ni un puerto abierto en el ESP32, y la demo cumple la latencia de 5 s.

**Cuándo pasar a MQTT:** si se necesita una latencia menor a 1 s o muchos dispositivos. El contrato de mensajes de 6.2 se puede reutilizar como *payload* MQTT sin cambios.

Bibliotecas de firmware sugeridas: `Adafruit_INA219`, `ArduinoJson`, `HTTPClient`, `Preferences` y `esp_task_wdt`.

### 6.2 Contrato dispositivo ↔ servidor

`POST /api/dispositivo/telemetria`, con el encabezado `Authorization: Bearer <token del dispositivo>`

```json
{
  "boot_id": 12, "seq": 4381, "ts": "2026-10-19T14:03:22Z", "ts_valida": true,
  "modo": "AUTO", "estado": "EN_SOLAR",
  "fuente_seleccionada": "SOLAR", "alimentacion_comprobada": true,
  "solar":    { "v": 4.93, "i_ma": 196.2, "p_mw": 967, "wh_acum": 3.412 },
  "cargador": { "v": 5.08, "i_ma": 0.4,   "p_mw": 2,   "wh_acum": 1.105 },
  "eventos": [
    { "seq": 4379, "ts": "...", "tipo": "conmutacion", "de": "CARGADOR", "a": "SOLAR",
      "motivo": "solar_comprobada", "origen": "regla", "orden_id": null }
  ],
  "resultados_ordenes": [
    { "orden_id": 77, "resultado": "ejecutada" }
  ]
}
```

Respuesta: `{ "hora_servidor": "...", "orden": { "id": 78, "tipo": "modo", "valor": "SOLAR" } }` o `"orden": null`.

**Reglas del contrato**

- **Idempotencia:** el ESP32 guarda en memoria no volátil el id de la última orden ejecutada y no la repite. Los eventos se identifican por `(boot_id, seq)`, y el servidor descarta los duplicados.
- **Sin conexión:** el dispositivo sigue operando, acumula Wh y guarda eventos en la cola. Al reconectarse los envía. Las mediciones instantáneas del periodo sin conexión se pierden, pero la **energía no**, porque `wh_acum` es acumulativo.
- **Hora:** se toma por NTP. Sin sincronización, `ts_valida = false` y el servidor usa su propia hora marcada como "aproximada".
- **Reinicio:** un `boot_id` nuevo indica que los acumulados empezaron de cero. El servidor suma los incrementos por cada `boot_id`.
- **Expiración:** una orden que no se entrega en 30 s pasa a `expirada`. Así un dispositivo que se reconecta no ejecuta una orden vieja.

### 6.3 Modelo de datos

```text
empresa(id, nombre, …)                                   ── se mantiene
usuario(id, empresa_id, nombre, email, rol[admin|tecnico|consulta], …)
dispositivo(id, empresa_id, nombre, token_hash, firmware, ultimo_contacto,
            modo_actual, estado_actual, fuente_seleccionada, alimentacion_comprobada,
            parametros_json, parametros_version)
medicion(id, dispositivo_id, boot_id, seq, ts, ts_valida, ts_servidor,
         v_sol, i_sol_ma, p_sol_mw, wh_sol_acum,
         v_car, i_car_ma, p_car_mw, wh_car_acum,
         modo, estado, fuente_seleccionada, alimentacion_comprobada)
evento(id, dispositivo_id, boot_id, seq, ts, tipo[conmutacion|reinicio|alerta|orden],
       de, a, motivo, origen[orden|regla|arranque], orden_id)   UNIQUE(dispositivo_id, boot_id, seq)
orden(id, dispositivo_id, usuario_id, tipo[modo|parametros], valor_json,
      estado[pendiente|entregada|ejecutada|rechazada|expirada], motivo, creada, entregada, resuelta)
alerta(id, dispositivo_id, tipo, inicio, fin, evento_id)
ensayo(id, dispositivo_id, usuario_id, inicio, fin, carga_descripcion, condiciones, notas)
-- Simulación (de la propuesta original); siempre se rotula como SIMULADO en la interfaz:
tarifa(...), escenario(...), simulacion(...)
```

Volumen estimado: un registro cada 2 s equivale a unas 43 000 filas diarias por dispositivo. PostgreSQL lo maneja sin particionar tablas.

### 6.4 Permisos

| Acción | Administrador | Técnico | Consulta |
|---|---|---|---|
| Ver monitoreo e historial de su empresa | ✔ | ✔ | ✔ |
| Enviar órdenes de modo | ✔ | ✔ | ✘ |
| Cambiar parámetros de reglas | ✔ | ✔ | ✘ |
| Registrar dispositivos y emitir credenciales | ✔ | ✘ | ✘ |

---

## 7. Plan de construcción y pruebas [P]

| Fase | Trabajo | Criterio de aceptación |
|---|---|---|
| **F0** | Cerrar D1 a D5, cotizar y comprar | Lista de materiales con precios reales |
| **F1 · E1 Medición** | ESP32 y los dos INA219 en banco, con carga resistiva y la lámpara | V e I dentro de lo fijado en RNF09, en 3 puntos de carga por rama |
| **F2 · E2 Selector** | Relé controlado por GPIO, sin lógica | Continuidad NO–NC nunca cerrada; relé en NC al arrancar; corte de conmutación medido. Decidir si se usa el capacitor de 1000 µF |
| **F3 · E3 Panel** | Panel y reductor al sol o con halógena; registrar V y P frente al nivel de sombra | Potencia máxima sostenida conocida; V_min y V_retorno ajustados con datos |
| **F4 · E4 Lógica local** | Máquina de estados **sin Wi-Fi** | Sombra → cargador en ≤ 2 s; recuperación → solar tras T_estable; prueba fallida → espera creciente; sin alternancia continua |
| **F5 · Plataforma mínima** | API de telemetría y órdenes; panel con estado, gráfico y eventos | Orden ejecutada o rechazada visible en ≤ 5 s; eventos sin duplicar |
| **F6 · E5 Comunicación** | Cortar el Wi-Fi durante la operación y restablecerlo | Eventos del periodo sin conexión recuperados; Wh coherentes; orden vieja expirada y no ejecutada |
| **F7 · E6 Energía** | 30 min con carga constante por fuente | Wh de SAGE dentro de ±5 % de P̄ × t, con P̄ medida por multímetro |
| **F8 · Demo integrada** | Guion de la sección 7.1 y simulaciones económicas | Lo físico y lo mostrado en SAGE coinciden en todos los pasos |

La v1 puede usar umbrales fijos en el firmware. RF15 (editarlos desde la app) se agrega después de F8.

### 7.1 Guion de la demostración

1. Modo Automático, con sol: la lámpara usa solar. SAGE muestra **Solar · comprobada** y la potencia.
2. Se sombrea el panel: el relé hace clic y la lámpara pasa al cargador. SAGE registra `solar_insuficiente`.
3. Se descubre el panel: tras T_estable, prueba y regreso a solar. SAGE registra `solar_comprobada`.
4. Con el panel sombreado se ordena **Solar** desde la app: la orden se rechaza con `solar_no_disponible`.
5. Se ordena **Cargador** y luego **Automático**: ambas se ejecutan y se confirman.
6. Se apaga el Wi-Fi y se repite el paso 2: el dispositivo conmuta igual. Al volver la conexión, el evento aparece en el historial.
7. Se muestran los Wh por fuente del ensayo y, aparte y rotulada como **simulación**, la proyección económica de una empresa.

---

## 8. Reformulación académica [P] (borrador)

**Título:** SAGE: Sistema de Análisis y Gestión Energética con monitoreo y control de fuentes mediante IoT.

**Descripción:** SAGE es una plataforma de análisis y gestión energética integrada con un dispositivo IoT. El dispositivo mide tensión, corriente, potencia y energía en dos fuentes, una solar y una convencional, y **controla cuál de ellas alimenta una carga**, por orden desde la aplicación o mediante reglas automáticas que se ejecutan localmente. La plataforma registra las mediciones y conmutaciones y conserva las herramientas de proyección económica para comparar alternativas de suministro en empresas.

**Objetivo general:** desarrollar un prototipo a escala que demuestre el monitoreo y el control de la fuente de alimentación de una carga de baja tensión, integrado con una plataforma web de análisis energético.

**Objetivos específicos**

1. Diseñar y construir un circuito que mida V, I y P en una rama solar y una convencional, y seleccione la fuente de la carga sin conectarlas en paralelo.
2. Implementar en el ESP32 los modos Solar, Cargador y Automático con histéresis, tiempos de estabilidad y funcionamiento sin conexión.
3. Desarrollar una plataforma web que muestre el estado, envíe órdenes con confirmación y registre las mediciones, conmutaciones y ensayos.
4. Validar las mediciones contra un multímetro y la conmutación en escenarios de sombra, recuperación y pérdida de conexión.
5. Mantener el módulo de simulación económica separado y rotulado respecto de los datos medidos.

**Alcance y limitaciones:** las de la sección 1.

**Clientes y aliados:** se mantienen los de la propuesta de valor como **actores identificados**. No existen acuerdos con FUNDARE, CRE RL ni las demás instituciones mencionadas.
