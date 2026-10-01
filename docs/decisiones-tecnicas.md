# Decisiones técnicas

Este documento explica por qué la solución está construida como está. El criterio general fue
**resolver el problema del enunciado con la menor cantidad de piezas posible**, y justificar
cada una de las que sí están.

---

## 1. Estructura elegida

Cuatro proyectos en el backend, con las dependencias apuntando hacia el dominio:

```
Api  →  Application  →  Domain
                ↑
        Infrastructure
```

| Proyecto | Responsabilidad | Qué **no** contiene |
|---|---|---|
| `Domain` | Entidades, invariantes, política de transiciones y política de acceso | Ninguna referencia externa: ni EF Core ni ASP.NET Core ni JWT |
| `Application` | Casos de uso, contratos (DTOs), validación de entrada e interfaces de salida | Nada de SQL ni de HTTP |
| `Infrastructure` | EF Core, repositorios, migraciones, reloj, hashing de contraseñas, emisión de tokens | Reglas de negocio |
| `Api` | Controladores, autenticación, traducción de errores a HTTP, composición de dependencias | Reglas de negocio y acceso a datos |

**Por qué cuatro y no uno.** El enunciado evalúa explícitamente la separación entre reglas de
negocio e infraestructura y presentación. Con proyectos separados esa separación no es una
convención que se pueda romper sin darse cuenta: **el compilador la hace cumplir**. `Domain` no
compila si alguien intenta usar `DbContext` dentro de una entidad, porque no tiene la referencia.

**Por qué no más.** No hay capa de «servicios de aplicación» adicional, ni proyecto de
«contratos compartidos», ni separación entre lectura y escritura. Para los casos de uso que
existen eso sería estructura sin beneficio.

**Nota sobre el alcance.** El enunciado dice que *no es necesario implementar autenticación
real* y permite un actor de prueba. La solución **sí** implementa inicio de sesión, registro de
clientes, tres perfiles de usuario y un panel de administración. Es una extensión deliberada, no un requisito mal leído, y se tomó por una
razón concreta: en la primera versión el actor viajaba en un encabezado `X-Actor-Id` que
cualquiera podía escribir, de modo que el historial registraba *quien dijera ser* el llamante.
Con un actor autenticado, el registro de auditoría pasa a ser confiable, que es precisamente lo
que el enunciado pide poder explicar en la sustentación. El detalle está en la sección 5.

---

## 2. Cómo se aplicaron SOLID y DDD

### DDD táctico (lo que aplica a este alcance)

Se aplicó la parte del enfoque que resuelve un problema real aquí, no el vocabulario completo.

**`MaintenanceRequest` es un agregado con comportamiento, no un contenedor de datos.** Todos
los setters son privados y el estado solo cambia a través de métodos que expresan una operación
del negocio: `Create`, `ChangeStatus`, `AssignResponsible`.

La consecuencia importante es esta:

```csharp
public void ChangeStatus(RequestStatus newStatus, Actor actor, DateTimeOffset occurredAt)
{
    if (!RequestStatusTransitionPolicy.IsAllowed(Status, newStatus))
        throw new InvalidStatusTransitionException(Status, newStatus);

    var previousStatus = Status;
    Status = newStatus;
    UpdatedAt = occurredAt;

    _history.Add(RequestHistoryEntry.ForStatusChange(Id, previousStatus, newStatus, actor, occurredAt));
}
```

Cambiar el estado **y** registrar el historial son la misma operación, indivisible por
construcción. No es posible escribir código que haga lo primero sin lo segundo, porque `Status`
no tiene setter público y `RequestHistoryEntry` tiene constructores internos: solo el agregado
puede crear entradas del historial.

**`RequestHistoryEntry` es una entidad hija** a la que solo se llega por el agregado raíz.
**`Actor` es un objeto de valor**: identidad por valor, inmutable, y valida sus propias
invariantes al construirse.

**`User` es un segundo agregado, pequeño y separado.** `MaintenanceRequest` no tiene una
navegación hacia `User`: guarda `RequesterId` y una *copia* del nombre. Dos razones: un agregado
no debe referenciar directamente a otro (se referencia por identidad), y el nombre almacenado es
el que tenía la persona **cuando ocurrió el evento**, así que renombrar un usuario no reescribe
el historial. La clave foránea sí existe a nivel de base de datos, que es donde corresponde
garantizar la integridad referencial.

**`User` no sabe hashear.** La entidad recibe una credencial ya procesada; el algoritmo vive en
`Infrastructure` detrás de `IPasswordHasher`. El dominio se puede probar sin criptografía.

**Qué no se hizo:** no hay eventos de dominio, ni repositorios por agregado con especificaciones,
ni contextos delimitados. Con dos agregados y sin integraciones, serían andamiaje sin uso.

### SOLID

**S — Responsabilidad única.** La política de transiciones es una clase con una sola razón para
cambiar: que el negocio cambie la tabla del ciclo de vida. El controlador solo traduce HTTP. El
caso de uso solo orquesta. El repositorio solo persiste.

**O — Abierto/cerrado.** Agregar un estado nuevo significa agregar un miembro al enum y una fila
al diccionario de `RequestStatusTransitionPolicy`. Ni los controladores, ni los casos de uso, ni
el frontend necesitan cambiar: la interfaz descubre las transiciones disponibles leyendo
`allowedNextStatuses`, que la API calcula desde la misma política.

**L — Sustitución de Liskov.** Las abstracciones (`IClock`, `IUserRepository`,
`IPasswordHasher`, `IAccessTokenIssuer`, `IMaintenanceRequestRepository`) son contratos estrechos
y sin sorpresas: las pruebas sustituyen la implementación real sin que el código cliente lo note,
que es la prueba práctica del principio.

**I — Segregación de interfaces.** `IUnitOfWork` expone un único método. `IClock`, una propiedad.
`IAccessTokenIssuer`, uno. No hay interfaces «de repositorio genérico» con quince métodos de los
que se usan tres.

**D — Inversión de dependencias.** `IMaintenanceRequestRepository` se **declara en
`Application`** y se **implementa en `Infrastructure`**. La capa de alto nivel no depende de la
de bajo nivel: ambas dependen de la abstracción, y por eso la flecha de dependencia entre
proyectos va de `Infrastructure` hacia `Application` y no al revés.

---

## 3. Patrones utilizados y por qué

| Patrón | Dónde | Por qué |
|---|---|---|
| **Máquina de estados** (tabla de transiciones) | `RequestStatusTransitionPolicy` | Un diccionario explícito de transiciones permitidas es directamente comparable con la tabla del enunciado, y la interfaz puede consultarlo en lugar de duplicarlo |
| **Política (objeto de estrategia sin estado)** | `RequestAccessPolicy` | Las reglas de «quién puede qué» quedan en un solo archivo del dominio, probable sin HTTP ni base de datos. Es el hermano de la política de transiciones: una responde *si la solicitud puede moverse*, la otra *si este actor puede moverla* |
| **Repositorio** | `IMaintenanceRequestRepository` | Mantiene las consultas EF fuera de los casos de uso y hace testeable la orquestación. Se aplicó **al agregado**, no como repositorio genérico por entidad |
| **Unit of Work** | `IUnitOfWork` | Un solo `SaveChanges` por caso de uso — la garantía de atomicidad del punto 7 de requisitos no funcionales |
| **Método de fábrica** | `MaintenanceRequest.Create` | Impide construir una solicitud en un estado inválido o con un estado inicial distinto de `Pending` |
| **Objeto de valor** | `Actor` | Evita pasar `(Guid, string)` sueltos y centraliza su validación |
| **Adaptador** | `ClaimsCurrentUserProvider` | Única clase que sabe que el actor viene de un token. Cambiar de proveedor de identidad cambia este archivo y ninguno más |
| **Fachada / capa de API** | `frontend/src/lib/api` | URL base, token Bearer y forma de los errores resueltos una sola vez |

### Bibliotecas de terceros y su justificación

| Biblioteca | Por qué |
|---|---|
| **FluentValidation** | Permite responder `400` con errores **por campo**, que es lo que la interfaz necesita para marcar cada input. Una excepción de dominio da un solo mensaje. Los límites se leen de las constantes del dominio (`MaintenanceRequest.TitleMaxLength`), así que la regla no se duplica |
| **Testcontainers** | Levanta y destruye un PostgreSQL real por ejecución de pruebas. Es lo que hace que la prueba de integración sea reproducible en cualquier máquina sin depender de una base de datos preexistente |
| **FluentAssertions** | Aserciones legibles y mensajes de fallo útiles |
| **Tailwind CSS** | Estilos sin mantener una hoja CSS paralela; las clases repetidas se extraen a componentes (`Badge`, `Button`) y a utilidades de `globals.css` |
| **`Microsoft.AspNetCore.Authentication.JwtBearer`** y **`System.IdentityModel.Tokens.Jwt`** | Validación y firma de tokens. Es la implementación de referencia de la plataforma; escribir una propia sería exactamente el tipo de criptografía casera que no corresponde |

### Lo que se decidió **no** usar

El enunciado aclara que no es obligatorio usar MediatR, CQRS, Repository, Unit of Work,
AutoMapper ni microservicios, y que se evalúa la adecuación, no la cantidad.

- **MediatR / CQRS**: agregaría una indirección por cada operación sin resolver ningún problema
  presente. Los casos de uso son seis, las lecturas y escrituras usan el mismo modelo y no hay
  necesidad de escalarlos por separado.
- **AutoMapper**: el mapeo está escrito a mano en `MaintenanceRequestMapper`. Son dos
  proyecciones estables; una configuración por convención sería más difícil de leer y de depurar
  que el código explícito, y fallaría en tiempo de ejecución en vez de en compilación.
- **Repositorio genérico** `IRepository<T>`: expondría operaciones que ninguna entidad necesita y
  rompería la regla de acceder al historial solo a través del agregado.
- **ASP.NET Core Identity completo**: arrastra tablas de roles, claims, logins externos, tokens y
  confirmación por correo para un sistema con dos roles fijos y cuatro usuarios. Se tomó de la
  plataforma lo que sí hacía falta —la validación de JWT— y el hashing se resolvió con el
  primitivo estándar del BCL (ver sección 5).

---

## 4. Decisiones de persistencia

**Enumeraciones almacenadas como texto** (`HasConversion<string>()`). Los datos quedan legibles
al consultarlos directamente en SQL y, sobre todo, agregar o reordenar un miembro del enum no
puede reinterpretar silenciosamente las filas existentes, como ocurriría con el valor ordinal.

**Índices, justificados por las consultas que existen** (no «por si acaso»):

| Índice | Consulta que lo motiva |
|---|---|
| `(CreatedAt)` | Orden por defecto del listado |
| `(Status, CreatedAt)`, `(Priority, CreatedAt)`, `(Category, CreatedAt)` | Cada filtro del listado combinado con el orden: la columna de filtro selecciona y la de orden evita ordenar en memoria |
| `(ResponsibleId)` | Búsqueda de solicitudes por responsable |
| `(RequesterId, CreatedAt)` | Listado de un solicitante: siempre filtra por dueño y ordena por fecha |
| `(RequestId, OccurredAt)` en el historial | Lectura del historial de una solicitud en orden cronológico |
| `(ActorId)` en el historial | Soporta la clave foránea hacia `users` |
| `(Email)` **único** en `users` | Búsqueda en cada inicio de sesión, y garantía de que no existan dos cuentas con el mismo correo |
| `(Role)` en `users` | Catálogo de personal para el selector de responsable |

**Correo normalizado en minúsculas por la entidad.** Así el índice único es un índice normal
sobre la columna y la consulta de login puede usarlo, en lugar de necesitar un índice funcional
sobre `lower(email)`.

**Atomicidad del historial.** El agregado y su nueva entrada de historial los rastrea el mismo
`DbContext` y se confirman con un único `SaveChangesAsync`, que EF Core envuelve en una
transacción. Si la inserción del historial falla, el cambio de estado o de responsable se
deshace con ella. No hizo falta una transacción explícita: la atomicidad viene de **hacer una
sola escritura**, no de coordinar varias.

**Claves foráneas hacia `users`, sin navegaciones en el modelo.** `maintenance_requests`
referencia a `users` por `RequesterId` y `ResponsibleId`, y `request_history_entries` por
`ActorId`. Las tres están declaradas con `HasOne<User>().WithMany()` **sin propiedad de
navegación**: la base de datos garantiza la integridad referencial, pero el agregado
`MaintenanceRequest` sigue sin poder navegar a otro agregado desde el código.

Todas usan `OnDelete(Restrict)`, no cascada. Borrar un usuario que dejó historial debe ser un
acto deliberado: una cascada silenciosa borraría el registro de auditoría, que es justo lo que
el sistema existe para conservar.

**Nombres de responsable y solicitante duplicados junto a la solicitud.** La desnormalización es
intencional: es la *foto* del nombre en el momento del evento. Si mañana un usuario cambia de
nombre, las entradas del historial deben seguir diciendo lo que decían — un asiento de auditoría
no se reescribe.

**Paginación en el servidor.** Los filtros, la búsqueda y el orden se traducen a SQL; el
navegador nunca recibe más filas que la página solicitada. `pageSize` está acotado a 100 para
que un parámetro manipulado no pueda pedir la tabla completa. Los comodines `%` y `_` que
escriba el usuario en la búsqueda se escapan, de modo que buscar «50%» busca ese texto literal.

**Migraciones versionadas** en el repositorio, aplicadas al iniciar la API en Docker para que
`docker compose up` deje la base lista sin pasos manuales. Son dos: `InitialSchema` y
`AddUserActivation`. La segunda muestra el esquema evolucionando sobre datos existentes en lugar
de regenerarse, y tiene una **edición manual deliberada**: EF genera `defaultValue: false` para
una columna `bool` nueva no anulable, lo que habría **desactivado a todos los usuarios
existentes** al aplicar la migración. Se cambió a `true`.

Por el mismo motivo `IsActive` **no** usa `HasDefaultValue(true)` en el modelo: sobre un `bool`,
EF interpreta el valor por defecto de C# (`false`) como «sin asignar» e insertaría el default de
la base, de modo que una cuenta creada desactivada aparecería activa. El default solo hace falta
para las filas que ya existían, y eso es asunto de la migración.

**Sin datos semilla.** La aplicación no inserta cuentas ni solicitudes de ejemplo: arranca vacía y
todo lo que contiene lo crearon sus usuarios. Las cuentas que necesitan las pruebas de integración
viven en el proyecto de pruebas (`TestAccounts`), que es donde corresponde: son datos para ejercitar
los roles, no datos de la aplicación. El costo es que el primer administrador se promueve una vez
con una sentencia SQL documentada en el README (ver limitaciones).

**Número legible generado por la base, no por la aplicación.** `Number` es una columna de
identidad de PostgreSQL. Calcularlo en la aplicación («el último más uno») fallaría con dos
inserciones simultáneas; la secuencia de la base no. La migración `AddRequestNumberAndResolution`
tiene otra **edición manual**: la versión generada añadía la identidad directamente, lo que numera
las filas existentes en orden físico y además declaraba un `default 0` junto a un índice único. Se
reemplazó por SQL que numera las solicitudes existentes por fecha de creación y arranca la
secuencia después del mayor número asignado.

**La respuesta se guarda en la fila de la solicitud.** `Resolution` es un tipo propio de EF
(`OwnsOne`): sus columnas viven en `maintenance_requests`, son anulables porque solo existen al
resolver, y `ResolvedById` tiene su clave foránea hacia `users` con `Restrict`, como el resto.

---

## 5. Autenticación, roles y autorización

### Los tres perfiles

| Rol | Quién es | Qué puede hacer |
|---|---|---|
| `Requester` | El cliente que reporta un problema | Registrarse, registrar solicitudes y ver **solo las suyas**, con su historial y su panel de indicadores |
| `Staff` | Personal de Expertos Seguridad | Ver **todas** las solicitudes, asignar y reasignar responsable, y cambiar el estado |
| `Admin` | Administración de la plataforma | Gestionar cuentas (rol, activa/desactivada) y **supervisar** todas las solicitudes sin operarlas |

`Requester` y `Staff` son la distinción que el negocio ya tenía: *quien pide* y *quien atiende*.
`Admin` aparece con el registro abierto: si cualquiera puede crear una cuenta, alguien tiene que
poder decidir quién es personal.

Las reglas concretas, todas verificadas en el servidor:

- Un `Requester` no puede cambiar estados ni asignar responsables → `403`.
- Un `Staff` no puede abrir solicitudes → `403`. El personal atiende, no reporta.
- Un `Requester` que pide una solicitud ajena recibe `404`, **no** `403`: un `403` confirmaría
  que ese identificador existe.
- Solo un `Staff` activo puede ser responsable de una solicitud. Asignar a un solicitante se
  rechaza con `400`, porque produciría un responsable sin permiso para avanzar lo que le asignaron.
- El catálogo de personal (`GET /api/users/staff`) es visible solo para `Staff`.
- Un `Admin` ve todas las solicitudes pero no puede cambiarlas ni crearlas → `403`.
- Un `Admin` no puede cambiar su propio rol ni desactivar su propia cuenta → `403`.

### Separación de funciones: el administrador supervisa, no opera

La decisión menos obvia del modelo de roles. El `Admin` **podría** heredar los permisos de
`Staff`, y sería más cómodo para demostrar. No se hizo porque entonces una sola cuenta podría
otorgarse un permiso y usarlo, y los otros dos roles perderían sentido frente a un rol
omnipotente. Separar quién concede permisos de quién los ejerce es un control estándar en
seguridad, y aquí cuesta exactamente una línea: `CanManageLifecycle` sigue devolviendo `true`
solo para `Staff`.

### Flujo de atención: asignar inicia, el responsable ejecuta

El enunciado fija *qué* transiciones existen, no *quién* las toma ni *cuándo*. Sobre esa tabla se
añadieron reglas, sin quitar ni agregar transiciones:

- **Asignar una solicitud pendiente la inicia.** `AssignResponsible` registra la asignación y, si
  el estado era `Pending`, aplica `Pending → InProgress`: una transición que la tabla permite,
  hecha automáticamente en vez de pedida. Las dos entradas del historial nacen en la misma
  operación del agregado, con el mismo actor e instante, y se guardan en el mismo `SaveChanges`.
  Para que la asignación se lea antes del cambio que causó, el desempate por timestamp usa el
  orden declarado en `HistoryEventType` (`Created`, `ResponsibleChanged`, `StatusChanged`).
  Reordenar el enum fue seguro porque la columna guarda el nombre, no el número.
- **Sin responsable no hay atención.** `InProgress`, `OnHold` y `Resolved` exigen un responsable;
  por eso una solicitud pendiente solo ofrece «cancelar» y la forma de iniciarla es asignarla.
- **Esas tres transiciones son del responsable.** Cancelar queda fuera a propósito: es una
  decisión de coordinación que cualquier miembro del personal puede tomar, para que nada quede
  bloqueado si el responsable falta.
- **Se reasigna, no se desasigna.** Dejar sin responsable una solicitud iniciada implicaría volver
  a `Pending`, y esa transición no está en la tabla.

**La regla del actor vive dentro del agregado, no solo en el servicio.** `ChangeStatus` recibe el
actor y comprueba, en este orden: que la transición exista (`409` para cualquiera), que haya
responsable (`409`) y que quien la pide sea ese responsable (`403`). El orden no es casual: un
movimiento imposible es un conflicto de estado para todos, y solo los movimientos posibles llegan
a la pregunta de quién los hace. Estar en el agregado garantiza que ningún llamador futuro pueda
registrar «resuelta por» alguien que no estaba a cargo.

**«Primero lo mío» es un orden del servidor, no un filtro del navegador.** Para el personal, el
listado ordena primero por *«soy el responsable»*, dentro de eso las solicitudes en curso antes
que las cerradas, y después por fecha, todo en la misma consulta SQL. Hacerlo en el cliente habría ordenado solo la página visible: la segunda página
podría traer trabajo propio que debió aparecer en la primera. Como el listado del solicitante, el
valor se fija en el caso de uso desde el usuario autenticado y nunca se lee de la URL. El
desempate interno (en curso según `RequestStatusTransitionPolicy.UnderAttention`, antes que lo
cerrado) evita que el trabajo terminado tape al pendiente. Estas claves de orden no aprovechan un
índice; con el volumen de este
alcance es irrelevante, y es una de las cosas a medir antes de crecer.

**Resolver es una operación propia, no un cambio de estado.** `Resolution` es un objeto de valor
(título, descripción, quién responde y cuándo) que valida sus límites al construirse. El agregado
expone `Resolve(...)`, que hace las mismas comprobaciones que cualquier transición de ejecución y
después registra la respuesta y el cambio a `Resolved` en la misma operación. `ChangeStatus`
rechaza `Resolved`: así no puede existir una solicitud resuelta sin respuesta, ni una respuesta
sobre una solicitud que no se resolvió. En la API son dos endpoints distintos por la misma razón.

**La interfaz no deduce permisos.** El detalle devuelve `availableActions`: las transiciones de
la solicitud filtradas por `RequestAccessPolicy.CanChangeStatus` para quien consulta. El
frontend solo decide *dónde* mostrar cada botón, nunca *si* mostrarlo.

### «Nunca sin administrador», como invariante del dominio

`User.ChangeRole` y `User.Deactivate` reciben el identificador de quien actúa y rechazan la
operación si coincide con el de la cuenta. Esa única regla garantiza algo más fuerte de lo que
parece: el último administrador no puede degradarse ni desactivarse a sí mismo, y no hay nadie
más con permiso para hacerlo, así que **la plataforma nunca puede quedar sin administrador**. No
hizo falta contar administradores en la base de datos ni una consulta con bloqueo.

Está en la entidad y no en el servicio porque es una regla sobre *esa* cuenta: la entidad es la
que conoce su propia identidad.

### Registro de clientes

`User.Register` no recibe rol: siempre produce un `Requester`. Que esté en el dominio y no en el
controlador significa que ningún llamador futuro puede registrar un administrador pasando el valor
equivocado. El DTO `RegisterCommand` tampoco tiene campo de rol, y una prueba de integración envía
`"role": "Admin"` en el cuerpo para comprobar que se ignora.

- Correo duplicado → `409` con el error en el campo `email`. La verificación previa da el mensaje
  amable; si dos registros compiten por el mismo correo, el **índice único** atrapa al segundo y
  `UnitOfWork` traduce la violación de PostgreSQL (`23505`) a la misma excepción de conflicto. La
  capa de aplicación no conoce tipos de EF ni de Npgsql.
- Contraseña: entre 8 y 128 caracteres, con al menos una letra y un número. La longitud es lo que
  resiste la adivinación; no se exigen símbolos porque empujan a sustituciones predecibles.
  Esta regla vive en el validador de aplicación porque el dominio nunca ve la contraseña en claro.
- La cuenta queda con la sesión iniciada: el endpoint devuelve el mismo resultado que el login.

### Los cambios de permisos surten efecto de inmediato

Un JWT es una foto del usuario en el momento de emitirlo. Sin más, degradar a alguien de `Staff`
a `Requester` o desactivar su cuenta no tendría efecto hasta que su token expirara — hasta ocho
horas operando con permisos que ya no tiene.

`ActiveSessionValidator` corre en `JwtBearerEvents.OnTokenValidated`, después de validar firma y
vigencia, y compara el token con la cuenta **tal como está ahora**: si no existe, está desactivada
o su rol no coincide con el del token, la request se rechaza con `401`. En el frontend, un `401`
sobre una llamada autenticada lleva a `/logout?reason=session`, que borra la cookie y muestra en
el login por qué terminó la sesión. Así la interfaz y la API nunca discrepan sobre lo que el
usuario puede hacer.

**Costo:** una consulta por clave primaria en cada request autenticada. Se aceptó porque es barata
y porque la alternativa —esperar a que expire el token— es inaceptable en un sistema de permisos.
Si el volumen lo exigiera, una caché de corta duración invalidada al cambiar el rol reduciría la
carga sin volver a abrir la ventana.

### Dónde vive cada control

Esta es la decisión de diseño que importa: **la autorización está en dos niveles distintos y
deliberadamente separados.**

```
[Authorize] en el controlador  →  ¿hay alguien autenticado? ¿de qué rol?   (transporte)
RequestAccessPolicy            →  ¿este actor puede hacer esto, sobre esto? (negocio)
```

`[Authorize]` solo cierra la puerta al tráfico anónimo. La regla que de verdad importa —«un
solicitante ve sus propias solicitudes»— **no se puede expresar como un atributo de ruta**,
porque depende de los datos. Por eso vive en `RequestAccessPolicy`, en el dominio, junto a la
política de transiciones, y se aplica en los casos de uso. La consecuencia práctica es que se
puede probar sin levantar HTTP: `RequestAccessPolicyTests` es una clase de pruebas unitarias sin
servidor ni base de datos.

**El alcance del listado no se puede falsificar.** `MaintenanceRequestQuery` tiene una propiedad
`RequesterId`, pero el controlador nunca la enlaza desde la query string: el servicio la
**sobrescribe** con el identificador del usuario autenticado antes de llegar al repositorio.
Editar la URL no amplía lo que uno ve. El panel de indicadores usa la misma función de alcance,
de modo que los números y la lista nunca pueden discrepar.

### Contraseñas

`Pbkdf2PasswordHasher` usa `Rfc2898DeriveBytes.Pbkdf2` (PBKDF2-HMAC-SHA256, 210 000 iteraciones,
sal aleatoria de 16 bytes por contraseña, clave de 32 bytes). No es criptografía casera: la
derivación es el primitivo estándar del BCL. La clase solo aporta la higiene alrededor —sal
nueva por contraseña, número de iteraciones guardado junto al hash para poder subirlo después
sin invalidar las filas existentes, y comparación en tiempo constante con
`CryptographicOperations.FixedTimeEquals`.

**El login no revela qué cuentas existen.** Un correo desconocido y una contraseña incorrecta
devuelven exactamente la misma respuesta `401`. Además, cuando el usuario no existe igual se
ejecuta una derivación de la misma dureza, para que el tiempo de respuesta tampoco delate la
diferencia. Ese detalle lo absorbe el hasher (`Verify` acepta un hash nulo), no el caso de uso:
saber en qué consiste «el mismo trabajo» es asunto de la implementación.

### Tokens

JWT firmado con HMAC-SHA256. El rol viaja como *claim* para que `[Authorize(Roles = ...)]` pueda
filtrar endpoints, pero el claim **no es la fuente de verdad**: `ActiveSessionValidator` lo
contrasta con la base de datos en cada request (ver arriba), y la propiedad de los datos se
verifica contra lo persistido en cada operación.

- La clave de firma se lee del entorno (`Jwt__SigningKey`) y **la API se niega a arrancar** si
  falta o tiene menos de 32 caracteres, en vez de emitir tokens que nadie podrá validar.
- `ClockSkew = TimeSpan.Zero`: el valor por defecto de cinco minutos extendería en silencio cada
  sesión más allá de su vigencia declarada.
- Emisión y validación leen la configuración por el mismo método (`JwtOptions.FromConfiguration`),
  así que no pueden quedar desalineadas.

### Manejo de errores y otros controles

- **Validación siempre en el servidor.** La validación del formulario en React es solo
  comodidad: la API vuelve a validar y sus errores por campo se muestran en la interfaz.
- **El cliente no controla el estado inicial, la fecha ni el solicitante.** El DTO de creación
  simplemente no tiene esos campos: `Pending` y la fecha las fija el servidor, y el solicitante
  sale del token.
- **Manejo centralizado de errores.** `GlobalExceptionHandler` traduce cada tipo de excepción a
  su código HTTP (`400`, `401`, `403`, `404`, `409`) con formato `ProblemDetails`. Las
  excepciones inesperadas se registran completas en el log pero responden un mensaje genérico:
  **no se exponen detalles internos**.
- **Errores de enlace también controlados.** Un JSON malformado o un valor de tipo incorrecto
  fallan antes de llegar al caso de uso, así que el manejador global no los ve. La respuesta por
  defecto de ASP.NET venía en inglés y **citaba nombres de tipos internos**
  (`...could not be converted to ExpertosSeguridad.Application.Contracts.RegisterCommand`).
  `BindingErrorResponse` la reemplaza por un mensaje genérico con el mismo formato que el resto
  de la API. Hay una prueba de regresión.
- **Sin secretos en el repositorio.** La cadena de conexión y la clave de firma se leen del
  entorno y la aplicación falla al iniciar con un mensaje claro si faltan. `.env` está en
  `.gitignore`; `.env.example` trae marcadores de desarrollo claramente rotulados. No hay ninguna
  contraseña ni hash versionado: todas las cuentas las crean sus usuarios al registrarse.
- **Contenedores sin privilegios.** API y frontend se ejecutan como usuario no root.

---

## 6. Decisiones del frontend

**Server Components para leer, Client Components para interactuar.** El panel y el detalle se
renderizan en el servidor —los datos llegan con el HTML, sin parpadeo de carga inicial—, y solo
los filtros, el formulario y las acciones son componentes de cliente.

**Por qué el token va en una cookie y no en `localStorage`.** Es la consecuencia directa de lo
anterior: `localStorage` no existe en el servidor, así que guardar ahí el token habría obligado
a mover todo el consumo de la API al navegador y perder el renderizado en servidor. Con una
cookie, `getServerSession()` la lee con `cookies()` de Next y las páginas siguen cargando sus
datos en el servidor.

La cookie **no** es `httpOnly` —el código cliente necesita el token para las mutaciones—, así que
el modelo de exposición es el mismo que tendría `localStorage`: un XSS exitoso podría leerla.
Lleva `SameSite=Lax`, que elimina el vector CSRF, y `Secure` cuando se sirve por HTTPS. La mejora
que cierra el hueco está en la sección 8.

**Grupo de rutas `(authenticated)`.** El layout raíz es solo el documento; el layout del grupo
lee la sesión, redirige a `/login` si no hay, y monta la cabecera y el contexto de autenticación.
Así la página de login no arrastra nada de la aplicación autenticada.

**Indicadores solo para quien opera o supervisa.** Es una decisión de presentación, no de
seguridad: la API sigue respondiendo el resumen al solicitante, acotado a sus propias solicitudes,
porque esos números no revelan nada que su listado no muestre. Por eso se ocultó en la interfaz y
no se bloqueó en el backend.

**Toda la fila abre la solicitud.** Un `<tr>` no puede ser un enlace, así que `ClickableRow` añade
el clic con el ratón y conserva el título como `<a>` real, para teclado y lectores de pantalla.
Ctrl o Cmd más clic abre otra pestaña, como en un enlace normal. En móvil la tarjeta completa es
el enlace.

**El popup de resolución usa `<dialog>` nativo** con `showModal()`: atrapa el foco, cierra con Esc
y deja inerte el fondo sin añadir una librería de modales.

**`/logout` es un route handler, no una página.** Los Server Components pueden leer cookies pero
no borrarlas; un route handler sí. Toda salida pasa por ahí: el botón «Salir» y cualquier `401`
sobre una llamada autenticada. Responde un `303` con `Location` **relativa**: dentro del
contenedor, el servidor standalone de Next ve la dirección de enlace (`0.0.0.0`), y una URL
absoluta construida desde la request mandaba al navegador a un host que no puede abrir.

**Cambiar un rol o desactivar una cuenta pide confirmación.** Ambas acciones cierran la sesión de
otra persona, así que elegir un rol en el selector solo lo *propone*. Activar no pide
confirmación porque no le quita nada a nadie. La fila del propio administrador es de solo lectura
y explica por qué.

**`proxy.ts` (antes `middleware.ts`) es comodidad de navegación, no una frontera de seguridad.**
Solo mira la forma de la cookie; no verifica la firma. La API valida cada llamada, así que una
cookie falsificada no consigue nada. El layout del grupo repite la verificación, de modo que
ninguna página autenticada puede renderizarse sin sesión aunque el proxy fallara.

**La interfaz refleja el rol, pero no es quien lo hace cumplir.** A un `Staff` no se le ofrece
«Nueva solicitud»; a un `Requester` se le muestra un panel de seguimiento de solo lectura en vez
de los controles de estado. Son dos formas distintas de decir lo mismo que el backend ya obliga:
las pruebas de integración verifican que saltarse la interfaz no sirve de nada.

**Los filtros viven en la URL, no en el estado del componente.** La página del servidor lee los
parámetros y le pide a la API exactamente esa página. Tres consecuencias: el filtrado ocurre de
verdad en el backend, cada vista es compartible y recargable, y el botón «atrás» del navegador
funciona como el usuario espera.

**La interfaz no repite la regla de transiciones.** Los botones de estado se generan a partir de
`allowedNextStatuses`, que el backend calcula con la misma política que después valida la
operación. Si la tabla de transiciones cambia, la interfaz se adapta sin tocar una línea de
TypeScript.

**Estados de carga, vacío y error en los tres flujos**: `Suspense` con esqueletos en el listado,
indicadores en línea durante las operaciones, estado vacío en la tabla y mensajes de error
visibles que muestran el detalle que devolvió la API.

**Responsivo**: la tabla del listado se convierte en tarjetas por debajo de `md`, de modo que
ninguna columna queda fuera de pantalla ni obliga a desplazamiento horizontal.

---

## 7. Limitaciones conocidas

Son decisiones conscientes de alcance, no descuidos:

1. **El token es accesible desde JavaScript.** La cookie de sesión no es `httpOnly` porque el
   código cliente la necesita; un XSS podría leerla. Ver la mejora 1 de la sección 8.
2. **Cerrar sesión no revoca el token.** Desactivar una cuenta o cambiarle el rol sí invalida sus
   tokens al instante, pero «Salir» solo borra la cookie: si alguien copió el JWT antes, sigue
   siendo válido hasta que expira (8 h por defecto). Tampoco hay *refresh token*. Una lista de
   revocación por `jti` o sesiones en servidor resolverían esto.
3. **El registro no verifica el correo** y no hay cambio ni recuperación de contraseña. Sin
   integración de correo —excluida por el enunciado— no hay un canal para confirmar nada.
4. **Los cambios de rol y de estado de las cuentas no quedan auditados.** El historial registra
   todo lo que ocurre sobre una solicitud, pero no quién promovió o desactivó a quién.
5. **Degradar a un miembro del personal no lo retira de sus asignaciones.** Las solicitudes que ya
   tenía asignadas lo siguen mostrando como responsable; el sistema solo impide asignarle nuevas.
   El historial sigue siendo veraz, pero alguien debe reasignarlas.
6. **El administrador no crea cuentas.** El personal se obtiene promoviendo una cuenta registrada.
7. **El primer administrador se crea fuera de la aplicación.** Sin datos semilla nadie puede
   otorgar el rol de administrador, así que la primera cuenta se promueve con una sentencia SQL
   (documentada en el README). Una mejora sería un comando de arranque que cree el administrador
   inicial a partir de variables de entorno solo si no existe ninguno.
8. **Las solicitudes resueltas antes de que existiera la respuesta no tienen una.** El dominio
   exige respuesta desde esta versión, pero no puede inventarla para filas antiguas; el detalle
   simplemente no muestra la tarjeta. (Afecta solo a bases creadas con versiones previas.)
9. **No hay límite de intentos de inicio de sesión ni de registro.** El endpoint de login no tiene
   *rate limiting* ni bloqueo por intentos fallidos, así que es vulnerable a fuerza bruta.
   PBKDF2 con 210 000 iteraciones encarece el intento, pero no lo impide.
10. **La búsqueda por título usa `ILIKE '%término%'`**, que no aprovecha un índice B-tree. Con
   pocos miles de filas es irrelevante; con más, haría falta un índice GIN con `pg_trgm` o
   búsqueda de texto completo.
11. **No hay control de concurrencia optimista.** Dos usuarios que cambien el estado a la vez
   producen dos transiciones válidas en secuencia; la segunda podría no ser la que su autor vio
   en pantalla.
12. **El historial no es inmutable a nivel de base de datos.** El diseño impide modificarlo desde
   la aplicación, pero no hay permisos de solo-inserción en PostgreSQL.
13. **No hay paginación del historial**: una solicitud con cientos de eventos lo carga completo.
14. **No hay pruebas automatizadas de frontend.** La cobertura de pruebas se concentró en las
   reglas de negocio y en la API, que es donde el enunciado pone el énfasis.
15. **Sin observabilidad más allá del log estructurado por defecto**: no hay métricas ni trazas.

## 8. Qué haría con más tiempo

En este orden de prioridad:

1. **Cookie `httpOnly` con Next como BFF.** Route handlers de Next (`/api/auth/login`,
   `/api/auth/logout`) guardarían el token en una cookie que el navegador nunca expone a
   JavaScript, y el servidor de Next reenviaría las llamadas a la API. Elimina la limitación 1
   y es la mejora de seguridad de mayor impacto sobre lo entregado.
2. ***Refresh tokens* y revocación**: sesiones cortas renovables y una lista de revocación, para
   que cerrar sesión realmente invalide el acceso.
3. **Límite de intentos en el login** con *rate limiting* por IP y por cuenta.
4. **Control de concurrencia optimista** con una columna `xmin` como token de concurrencia,
   devolviendo `409` cuando la solicitud cambió desde que el usuario la leyó.
5. **Pruebas de frontend**: componentes con Testing Library y un recorrido completo con
   Playwright sobre el stack de Docker Compose, cubriendo los dos roles.
6. **Auditoría de la administración de cuentas**: una tabla de eventos (quién cambió qué rol, a
   quién y cuándo), con la misma garantía de atomicidad que el historial de solicitudes.
7. **Verificación de correo y recuperación de contraseña**, junto con una invitación para dar de
   alta al personal sin pasar por el registro público.
8. **Índice GIN con `pg_trgm`** para la búsqueda por título, acompañado de una medición previa
   que justifique el cambio.
9. **Observabilidad**: OpenTelemetry con trazas que crucen frontend, API y base de datos.
10. **Notificaciones** al responsable cuando se le asigna una solicitud, mediante un patrón
   *outbox* para no acoplar el envío a la transacción de negocio.
