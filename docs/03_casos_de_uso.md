# Parte 3 · Casos de uso

En esta sección se describe **qué puede hacer cada actor con SAGE**. Los casos de uso cubren todos los requisitos del proyecto y se organizan en tres áreas: acceso y administración, análisis económico, y monitoreo y control de fuentes.

---

## 1. Actores

| Actor | Quién es | Qué hace en SAGE |
|---|---|---|
| **Consulta** | Normalmente la gerencia de la empresa | Revisa el estado de los dispositivos, el historial, las alertas y los reportes. No modifica nada |
| **Técnico** | Personal técnico o administrativo de la empresa | Todo lo que hace Consulta, y además registra consumos, realiza simulaciones, controla los dispositivos y registra ensayos |
| **Administrador** | Operador de la plataforma SAGE | Todo lo que hace el Técnico, y además gestiona empresas, usuarios, dispositivos y parámetros económicos |
| **Dispositivo SAGE** | El equipo instalado en la empresa | Mide la energía de cada fuente, cambia de fuente según las reglas y se comunica con la plataforma |

Los roles humanos son **acumulativos**: el Técnico puede hacer todo lo que hace Consulta, y el Administrador todo lo que hace el Técnico.

---

## 2. Lista de casos de uso

| Código | Caso de uso | Actor principal | Requisitos que cubre |
|---|---|---|---|
| **Acceso y administración** | | | |
| CU01 | Iniciar sesión | Consulta, Técnico, Administrador | RNF01 |
| CU02 | Gestionar empresas | Administrador | RF01 |
| CU03 | Gestionar usuarios y roles | Administrador | RF02 |
| CU04 | Registrar dispositivo | Administrador | RF09 |
| **Análisis económico** | | | |
| CU05 | Registrar consumo facturado | Técnico | RF03 |
| CU06 | Configurar parámetros económicos | Administrador | RF05, RNF06 |
| CU07 | Simular escenarios económicos | Técnico | RF06, RF07 |
| CU08 | Consultar historial y exportar reportes | Consulta | RF04, RF08 |
| **Monitoreo y control de fuentes** | | | |
| CU09 | Monitorear dispositivo | Consulta | RF10, RF14 |
| CU10 | Controlar fuente de energía | Técnico | RF11, RF15 |
| CU11 | Conmutar fuente automáticamente | Dispositivo SAGE | RF12, RF13 |
| CU12 | Gestionar alertas | Técnico | RF16 |
| CU13 | Registrar ensayo y comparar fuentes | Técnico | RF17, RF18 |

**Relaciones entre casos de uso**

- Para todos los casos de uso de actores humanos es necesario haber iniciado sesión (CU01).
- La simulación (CU07) **incluye** los parámetros económicos configurados en CU06.
- La exportación de reportes forma parte de CU08 y también puede usarse desde CU07 y CU13.
- Las órdenes de CU10 las ejecuta el dispositivo en CU11, que puede aceptarlas o rechazarlas.
- CU09, CU12 y CU13 usan la información que el dispositivo genera en CU11.

---

## 3. Descripción de cada caso de uso

### CU01 · Iniciar sesión

**Actor:** Consulta, Técnico o Administrador.
**Objetivo:** ingresar a SAGE y ver solo lo que corresponde a su rol y a su empresa.
**Precondición:** el usuario está registrado y activo.

**Flujo principal**
1. El usuario ingresa su correo y contraseña.
2. El sistema verifica los datos.
3. El sistema abre el panel correspondiente a su rol y su empresa.

**Flujos alternativos**
- Si los datos son incorrectos, el sistema muestra un mensaje general, sin indicar qué dato falló.
- Después de cinco intentos fallidos, la cuenta se bloquea temporalmente.
- Si el usuario fue desactivado, no puede ingresar.

**Postcondición:** el usuario tiene una sesión activa.

---

### CU02 · Gestionar empresas

**Actor:** Administrador.
**Objetivo:** registrar las empresas que usan SAGE y mantener actualizados sus datos.

**Flujo principal**
1. El administrador abre el módulo de empresas.
2. Elige registrar una empresa nueva o editar una existente.
3. Ingresa la razón social, el NIT, el rubro, la ciudad y los datos de contacto.
4. El sistema valida que no falten datos y que el NIT no esté repetido.
5. El sistema guarda la información y confirma.

**Flujos alternativos**
- Si falta un dato o el NIT ya existe, el sistema indica qué campo debe corregirse.
- Si se desactiva una empresa, sus usuarios pierden el acceso, pero su información se conserva.

**Postcondición:** la empresa queda registrada o actualizada.

---

### CU03 · Gestionar usuarios y roles

**Actor:** Administrador.
**Objetivo:** crear los usuarios de cada empresa y asignarles un rol.

**Flujo principal**
1. El administrador elige la empresa.
2. Ingresa el nombre y el correo del usuario y le asigna un rol: Administrador, Técnico o Consulta.
3. El sistema verifica que el correo no esté registrado.
4. El sistema crea el usuario con una contraseña temporal y la muestra una sola vez.

**Flujos alternativos**
- Si el correo ya existe, el sistema no crea el usuario.
- Si se desactiva un usuario, se cierran sus sesiones abiertas.
- El sistema no permite dejar la plataforma sin ningún administrador.

**Postcondición:** el usuario puede ingresar con los permisos de su rol.

---

### CU04 · Registrar dispositivo

**Actor:** Administrador.
**Objetivo:** dar de alta un dispositivo SAGE y vincularlo a una empresa.

**Flujo principal**
1. El administrador ingresa un nombre para el dispositivo y elige la empresa.
2. El sistema genera una credencial única para ese dispositivo y la muestra una sola vez.
3. El administrador carga esa credencial en el dispositivo.
4. El dispositivo aparece como "sin conectar" hasta que envía sus primeras mediciones.

**Flujo alternativo**
- Si la credencial se pierde, el administrador genera una nueva y la anterior deja de funcionar.

**Postcondición:** el dispositivo queda vinculado a una empresa y puede comunicarse con la plataforma.

---

### CU05 · Registrar consumo facturado

**Actor:** Técnico.
**Objetivo:** cargar en SAGE los datos de la factura eléctrica mensual.

**Flujo principal**
1. El técnico elige el periodo.
2. Ingresa el consumo en kWh y el monto en bolivianos, o sube un archivo Excel con varios periodos.
3. El sistema valida los datos. Si se trata de un Excel, indica qué filas son válidas y cuáles tienen errores.
4. El técnico confirma y el sistema guarda los registros.

**Flujos alternativos**
- Si un periodo ya estaba cargado, el sistema pregunta si se desea reemplazar.
- Las filas del Excel que tienen errores no se guardan.

**Postcondición:** el historial de facturación queda actualizado.

---

### CU06 · Configurar parámetros económicos

**Actor:** Administrador.
**Objetivo:** mantener actualizados los valores que usan las simulaciones sin tener que modificar el sistema.

**Flujo principal**
1. El administrador revisa los parámetros vigentes: tarifa por kWh, costo de instalación solar, generación solar estimada y costo de mantenimiento.
2. Modifica los valores necesarios.
3. El sistema verifica que estén dentro de rangos razonables.
4. El sistema guarda los valores como una nueva versión y conserva la anterior.

**Flujo alternativo**
- Si algún valor está fuera de rango, el sistema no lo guarda e indica cuál es.

**Postcondición:** las nuevas simulaciones usan los valores actualizados. Las simulaciones anteriores conservan los valores con los que se hicieron.

---

### CU07 · Simular escenarios económicos

**Actor:** Técnico.
**Objetivo:** estimar cuánto costaría la energía de la empresa con red, con solar o con un sistema mixto, y en cuánto tiempo se recuperaría la inversión.

**Flujo principal**
1. El técnico elige el periodo base: facturas cargadas o energía medida por el dispositivo.
2. Indica la potencia solar que quiere evaluar y el horizonte de tiempo.
3. El sistema calcula los tres escenarios con los parámetros económicos vigentes.
4. Muestra el costo proyectado, el ahorro acumulado y el tiempo de recuperación de cada escenario, identificados claramente como **simulación**.
5. El técnico puede guardar la simulación o exportarla.

**Flujo alternativo**
- Si hay menos de tres meses de datos, el sistema advierte que el resultado es poco confiable.

---

### CU08 · Consultar historial y exportar reportes

**Actor:** Consulta (y los demás roles).
**Objetivo:** revisar la evolución del consumo, de los gastos y del uso de cada fuente, y llevarse esa información en un reporte.

**Flujo principal**
1. El usuario elige qué quiere ver: consumo facturado, energía medida por fuente o cambios de fuente.
2. Elige el periodo.
3. El sistema muestra gráficos y tablas con sus unidades.
4. Si lo desea, el usuario exporta la información en PDF o Excel.

**Flujo alternativo**
- Si no hay datos en el periodo, el sistema lo informa.

**Reglas**
- Cada usuario ve solo los datos de su empresa.
- Los datos **medidos** y los **facturados o simulados** siempre se muestran por separado y debidamente identificados.

---

### CU09 · Monitorear dispositivo

**Actor:** Consulta (y los demás roles).
**Objetivo:** ver en tiempo casi real qué está pasando con la energía de la empresa.

**Flujo principal**
1. El usuario abre un dispositivo de su empresa.
2. El sistema muestra, para cada fuente, la tensión, la corriente, la potencia y la energía acumulada.
3. Muestra el modo de operación y tres datos sobre la fuente: la **solicitada** (la que se pidió), la **seleccionada** (la que el dispositivo conectó) y si la alimentación está **comprobada** (si realmente está entregando energía).
4. La información se actualiza cada pocos segundos mientras la pantalla está abierta.

**Flujo alternativo**
- Si el dispositivo lleva más de 30 segundos sin comunicarse, el sistema lo indica como "sin conexión" y muestra los últimos valores conocidos.

---

### CU10 · Controlar fuente de energía

**Actor:** Técnico (o Administrador).
**Objetivo:** decidir desde la aplicación qué fuente alimenta la carga y con qué reglas trabaja el dispositivo.
**Precondición:** el dispositivo pertenece a la empresa del técnico.

**Flujo principal**
1. El técnico elige uno de los tres modos: **Solar**, **Red** o **Automático**.
2. El sistema envía la orden al dispositivo.
3. El dispositivo la ejecuta y confirma.
4. El sistema muestra que la orden fue ejecutada y la registra junto con quién la pidió y cuándo.

**Flujos alternativos**
- Si se pide Solar y el panel no tiene energía suficiente, el dispositivo **rechaza** la orden e informa el motivo.
- Si el dispositivo no recibe la orden en 30 segundos, la orden se cancela y no se ejecuta después.
- El técnico puede ajustar las **reglas** del modo Automático, como la tensión mínima aceptable y los tiempos de espera, siempre dentro de límites seguros. El dispositivo las aplica y confirma.
- Un usuario con rol Consulta no puede enviar órdenes.

**Postcondición:** el cambio queda registrado con su resultado y su motivo.

---

### CU11 · Conmutar fuente automáticamente

**Actor:** Dispositivo SAGE.
**Objetivo:** usar la energía solar siempre que sea posible, sin dejar la carga sin alimentación, y mantener informada a la plataforma.

**Flujo principal**
1. El dispositivo mide continuamente ambas fuentes.
2. Mientras la energía solar alcanza para la carga, la mantiene.
3. Si la energía solar deja de alcanzar durante un tiempo determinado, por ejemplo porque el panel quedó a la sombra, cambia a la red y registra el motivo.
4. Cuando la energía solar se recupera y se mantiene estable durante un tiempo definido, prueba volver a ella. Si la prueba sale bien, vuelve a solar y lo registra.
5. Cada pocos segundos envía a la plataforma sus mediciones y los cambios realizados, y recibe las órdenes pendientes del técnico.

**Flujos alternativos**
- Si la prueba de volver a solar falla, regresa a la red y espera cada vez más tiempo antes de volver a intentarlo, para no cambiar de fuente constantemente.
- Si ninguna de las dos fuentes tiene energía suficiente, deja la carga en la red y genera una alerta.
- Si no hay internet, **sigue funcionando** con sus reglas, guarda los cambios y los envía cuando se recupera la conexión.
- Si el dispositivo se reinicia, arranca siempre conectado a la red, que es la opción segura.

**Postcondición:** la carga queda alimentada por la fuente adecuada y cada cambio queda registrado en la plataforma.

---

### CU12 · Gestionar alertas

**Actor:** Técnico. Consulta solo puede verlas.
**Objetivo:** enterarse de los problemas a tiempo y dejar constancia de que fueron revisados.

**Flujo principal**
1. El sistema genera alertas automáticamente cuando la energía solar es insuficiente, cuando ninguna fuente está disponible o cuando un dispositivo lleva más de 30 segundos sin comunicarse.
2. El técnico ve las alertas abiertas, destacadas en la pantalla.
3. Abre una alerta y revisa qué ocurrió y cuándo.
4. La marca como revisada y agrega un comentario.

**Flujo alternativo**
- Cuando el problema desaparece, por ejemplo porque vuelve el sol o se restablece la conexión, la alerta se cierra sola.

**Postcondición:** queda registro de cada problema, de cuándo ocurrió y de quién lo revisó.

---

### CU13 · Registrar ensayo y comparar fuentes

**Actor:** Técnico. Consulta puede ver los resultados.
**Objetivo:** documentar una prueba controlada y ver con datos reales cuánto aportó cada fuente.

**Flujo principal**
1. El técnico inicia un ensayo e indica la carga utilizada y las condiciones, por ejemplo "lámpara LED de 1 W, sol directo".
2. El sistema registra el inicio, y el dispositivo sigue midiendo con normalidad.
3. Al terminar, el técnico finaliza el ensayo y agrega sus observaciones.
4. El sistema calcula, para cada fuente, la energía entregada, el porcentaje del tiempo en uso, la potencia promedio y la cantidad de cambios de fuente con sus motivos.
5. Muestra el resultado identificado claramente como **medido**, y el técnico puede exportarlo.

**Flujos alternativos**
- Si ya hay un ensayo en curso en ese dispositivo, el sistema no permite iniciar otro.
- La misma comparación puede hacerse sobre cualquier periodo, aunque no se haya registrado como ensayo.
- Si hubo momentos sin conexión, el sistema indica qué parte del tiempo tiene datos. La energía total sigue siendo correcta porque el dispositivo la acumula aunque no tenga conexión.
