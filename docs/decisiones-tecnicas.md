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
| `Domain` | Entidades, invariantes y la política de transiciones | Ninguna referencia externa: ni EF Core ni ASP.NET Core |
| `Application` | Casos de uso, contratos (DTOs), validación de entrada e interfaces de salida | Nada de SQL ni de HTTP |
| `Infrastructure` | EF Core, repositorio, migraciones, reloj, catálogo de usuarios | Reglas de negocio |
| `Api` | Controladores, traducción de errores a HTTP, composición de dependencias | Reglas de negocio y acceso a datos |

**Por qué cuatro y no uno.** El enunciado evalúa explícitamente la separación entre reglas de
negocio e infraestructura y presentación. Con proyectos separados esa separación no es una
convención que se pueda romper sin darse cuenta: **el compilador la hace cumplir**. `Domain` no
compila si alguien intenta usar `DbContext` dentro de una entidad, porque no tiene la referencia.

**Por qué no más.** No hay capa de «servicios de aplicación» adicional, ni proyecto de
«contratos compartidos», ni separación entre lectura y escritura. Para seis casos de uso eso
sería estructura sin beneficio.

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

**Qué no se hizo:** no hay eventos de dominio, ni repositorios por agregado con especificaciones,
ni contextos delimitados. Con un solo agregado y sin integraciones, serían andamiaje sin uso.

### SOLID

**S — Responsabilidad única.** La política de transiciones es una clase con una sola razón para
cambiar: que el negocio cambie la tabla del ciclo de vida. El controlador solo traduce HTTP. El
caso de uso solo orquesta. El repositorio solo persiste.

**O — Abierto/cerrado.** Agregar un estado nuevo significa agregar un miembro al enum y una fila
al diccionario de `RequestStatusTransitionPolicy`. Ni los controladores, ni los casos de uso, ni
el frontend necesitan cambiar: la interfaz descubre las transiciones disponibles leyendo
`allowedNextStatuses`, que la API calcula desde la misma política.

**L — Sustitución de Liskov.** Las abstracciones (`IClock`, `IUserDirectory`,
`IMaintenanceRequestRepository`) son contratos estrechos y sin sorpresas: las pruebas sustituyen
la implementación real sin que el código cliente lo note, que es la prueba práctica del principio.

**I — Segregación de interfaces.** `IUnitOfWork` expone un único método. `IClock`, una propiedad.
No hay interfaces «de repositorio genérico» con quince métodos de los que se usan tres.

**D — Inversión de dependencias.** `IMaintenanceRequestRepository` se **declara en
`Application`** y se **implementa en `Infrastructure`**. La capa de alto nivel no depende de la
de bajo nivel: ambas dependen de la abstracción, y por eso la flecha de dependencia entre
proyectos va de `Infrastructure` hacia `Application` y no al revés.

---

## 3. Patrones utilizados y por qué

| Patrón | Dónde | Por qué |
|---|---|---|
| **Máquina de estados** (tabla de transiciones) | `RequestStatusTransitionPolicy` | Un diccionario explícito de transiciones permitidas es directamente comparable con la tabla del enunciado, y la interfaz puede consultarlo en lugar de duplicarlo |
| **Repositorio** | `IMaintenanceRequestRepository` | Mantiene las consultas EF fuera de los casos de uso y hace testeable la orquestación. Se aplicó **al agregado**, no como repositorio genérico por entidad |
| **Unit of Work** | `IUnitOfWork` | Un solo `SaveChanges` por caso de uso — la garantía de atomicidad del punto 7 de requisitos no funcionales |
| **Método de fábrica** | `MaintenanceRequest.Create` | Impide construir una solicitud en un estado inválido o con un estado inicial distinto de `Pending` |
| **Objeto de valor** | `Actor` | Evita pasar `(Guid, string)` sueltos y centraliza su validación |
| **Fachada / capa de API** | `frontend/src/lib/api` | URL base, encabezado del actor y forma de los errores resueltos una sola vez |

### Bibliotecas de terceros y su justificación

| Biblioteca | Por qué |
|---|---|
| **FluentValidation** | Permite responder `400` con errores **por campo**, que es lo que la interfaz necesita para marcar cada input. Una excepción de dominio da un solo mensaje. Los límites se leen de las constantes del dominio (`MaintenanceRequest.TitleMaxLength`), así que la regla no se duplica |
| **Testcontainers** | Levanta y destruye un PostgreSQL real por ejecución de pruebas. Es lo que hace que la prueba de integración sea reproducible en cualquier máquina sin depender de una base de datos preexistente |
| **FluentAssertions** | Aserciones legibles y mensajes de fallo útiles |
| **Tailwind CSS** | Estilos sin mantener una hoja CSS paralela; las clases repetidas se extraen a componentes (`Badge`, `Button`) y a utilidades de `globals.css` |

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
| `(RequestId, OccurredAt)` en el historial | Lectura del historial de una solicitud en orden cronológico |

**Atomicidad del historial.** El agregado y su nueva entrada de historial los rastrea el mismo
`DbContext` y se confirman con un único `SaveChangesAsync`, que EF Core envuelve en una
transacción. Si la inserción del historial falla, el cambio de estado o de responsable se
deshace con ella. No hizo falta una transacción explícita: la atomicidad viene de **hacer una
sola escritura**, no de coordinar varias.

**Nombres de responsable y solicitante guardados junto a la solicitud.** Como los usuarios no
son filas de la base de datos (son un catálogo fijo en código), no hay clave foránea posible
hacia ellos. Guardar el nombre además del identificador también preserva el historial tal como
ocurrió, aunque el catálogo cambie después — que es el comportamiento correcto para un registro
de auditoría.

**Paginación en el servidor.** Los filtros, la búsqueda y el orden se traducen a SQL; el
navegador nunca recibe más filas que la página solicitada. `pageSize` está acotado a 100 para
que un parámetro manipulado no pueda pedir la tabla completa. Los comodines `%` y `_` que
escriba el usuario en la búsqueda se escapan, de modo que buscar «50%» busca ese texto literal.

**Migraciones versionadas** en el repositorio, aplicadas al iniciar la API en Docker para que
`docker compose up` deje la base lista sin pasos manuales.

---

## 5. Seguridad y manejo de errores

- **Validación siempre en el servidor.** La validación del formulario en React es solo
  comodidad: la API vuelve a validar y sus errores por campo se muestran en la interfaz.
- **El cliente no controla el estado inicial ni la fecha.** El DTO de creación simplemente no
  tiene esos campos; `Pending` y la fecha los fija el servidor.
- **Manejo centralizado de errores.** `GlobalExceptionHandler` traduce cada tipo de excepción a
  su código HTTP (`400`, `404`, `409`) con formato `ProblemDetails`. Las excepciones inesperadas
  se registran completas en el log pero responden un mensaje genérico: **no se exponen detalles
  internos**.
- **Sin secretos en el repositorio.** La cadena de conexión se lee solo del entorno y la
  aplicación falla al iniciar con un mensaje claro si falta. `.env` está en `.gitignore`.
- **Contenedores sin privilegios.** API y frontend se ejecutan como usuario no root.

---

## 6. Decisiones del frontend

**Server Components para leer, Client Components para interactuar.** El panel y el detalle se
renderizan en el servidor —los datos llegan con el HTML, sin parpadeo de carga inicial—, y solo
los filtros, el formulario y las acciones son componentes de cliente.

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

1. **No hay autenticación ni autorización.** Cualquiera puede actuar como cualquier usuario
   eligiéndolo en la interfaz. Es la simplificación que el enunciado permite.
2. **Los usuarios son un catálogo fijo en código.** No se pueden crear ni administrar.
3. **La búsqueda por título usa `ILIKE '%término%'`**, que no aprovecha un índice B-tree. Con
   pocos miles de filas es irrelevante; con más, haría falta un índice GIN con `pg_trgm` o
   búsqueda de texto completo.
4. **No hay control de concurrencia optimista.** Dos usuarios que cambien el estado a la vez
   producen dos transiciones válidas en secuencia; la segunda podría no ser la que su autor vio
   en pantalla.
5. **El historial no es inmutable a nivel de base de datos.** El diseño impide modificarlo desde
   la aplicación, pero no hay permisos de solo-inserción en PostgreSQL.
6. **No hay paginación del historial**: una solicitud con cientos de eventos lo carga completo.
7. **No hay pruebas automatizadas de frontend.** La cobertura de pruebas se concentró en las
   reglas de negocio y en la API, que es donde el enunciado pone el énfasis.
8. **Sin observabilidad más allá del log estructurado por defecto**: no hay métricas ni trazas.

## 8. Qué haría con más tiempo

En este orden de prioridad:

1. **Control de concurrencia optimista** con una columna `xmin` como token de concurrencia,
   devolviendo `409` cuando la solicitud cambió desde que el usuario la leyó. Es la brecha
   funcional más cercana a un problema real.
2. **Pruebas de frontend**: componentes con Testing Library y un recorrido completo con
   Playwright sobre el stack de Docker Compose.
3. **Usuarios persistidos** con claves foráneas reales, manteniendo el nombre denormalizado en
   el historial para conservar la fidelidad de la auditoría.
4. **Índice GIN con `pg_trgm`** para la búsqueda por título, acompañado de una medición previa
   que justifique el cambio.
5. **Autenticación real** (OIDC/JWT) y autorización por rol: quién puede resolver, quién puede
   cancelar, quién puede reasignar.
6. **Observabilidad**: OpenTelemetry con trazas que crucen frontend, API y base de datos.
7. **Notificaciones** al responsable cuando se le asigna una solicitud, mediante un patrón
   *outbox* para no acoplar el envío a la transacción de negocio.
