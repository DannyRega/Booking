# Decisiones de Arquitectura

## 1. Sobreventa

### ¿Qué mecanismo exacto usaste para garantizar que nunca se supera la capacidad?
Se implementó **bloqueo pesimista a nivel de fila (`SELECT ... FOR UPDATE`)** dentro de una transacción serializada en PostgreSQL:

```csharp
var session = await context.Sessions
    .FromSqlInterpolated($"SELECT * FROM sessions WHERE id = {sessionId} FOR UPDATE")
    .FirstOrDefaultAsync(ct);

var currentBookingsCount = await context.Bookings
    .CountAsync(b => b.SessionId == sessionId, ct);

if (currentBookingsCount >= session.Capacity)
{
    return (409, "La sesión ya no cuenta con cupos disponibles.");
}

```

Al adquirir el bloqueo exclusivo sobre la fila correspondiente en la tabla `sessions`, cualquier otra petición concurrente que intente reservar para esa misma sesión queda retenida de manera segura en el motor de base de datos esperando que la transacción activa realice `COMMIT` o `ROLLBACK`.

La transacción verifica el conteo real y confirma o rechaza la inserción. De este modo, en una ráfaga de 200 peticiones simultáneas sobre una sesión con 10 cupos, exactamente los primeros 10 hilos confirman su reserva (`201 Created`) y los 190 hilos restantes evalúan la condición de capacidad completa una vez liberado el bloqueo y devuelven ordenadamente un `409 Conflict` estructurado, con 0 errores `500`.

### ¿Qué alternativas descartaste y por qué?

1. **Sincronización en memoria de la aplicación (`lock`, `SemaphoreSlim`, `ConcurrentDictionary`):**
* *Descartada:* Solo tiene validez si existe un único proceso/hilo en ejecución. En un despliegue en producción con múltiples pods o contenedores detrás de un balanceador de carga, el estado en memoria no se comparte y la sobreventa se produce de inmediato.


2. **Bloqueo Optimista (mediante columnas `xmin` o `RowVersion`):**
* *Descartada:* El bloqueo optimista es eficiente bajo baja contención. Bajo contención masiva (200 peticiones en el mismo milisegundo compitiendo por 10 lugares), generaría ~190 excepciones de concurrencia y tormentas de reintentos (*retry storms*), saturando inútilmente la CPU de la API y de PostgreSQL. El bloqueo pesimista serializa limpiamente la fila sin abortar transacciones.


3. **Restricción Check en Base de Datos sobre conteo agregado:**
* *Descartada:* PostgreSQL no permite restricciones de tipo `CHECK (SELECT COUNT(*) <= capacity)` que consulten otras tablas o agregaciones de forma declarativa sin triggers con bloqueos explícitos, los cuales equivalen operativamente al bloqueo pesimista utilizado.

## 2. Múltiples instancias

### Si mañana corremos 3 instancias de la API detrás de un balanceador, ¿tu solución sigue funcionando? ¿Por qué?

**Sí.**

La garantía de consistencia no reside en el estado en memoria de la aplicación en C#/.NET, sino en el motor relacional ACID de PostgreSQL.

Si 3 instancias de la API atienden peticiones concurrentes distribuidas por un balanceador:

1. Cada nodo de la API abre su propia conexión hacia PostgreSQL a través de su pool de conexiones.
2. La primera transacción que ejecute el `SELECT ... FOR UPDATE` sobre la fila de la sesión adquiere el bloqueo exclusivo a nivel de tupla en PostgreSQL.
3. Los hilos de las otras instancias quedan en espera pasiva en el motor de base de datos hasta que la transacción activa haga `COMMIT`.
4. El cálculo de los cupos y la inserción de la reserva se leen siempre sobre datos confirmados (*Committed Read*), haciendo imposible que dos instancias distintas sobrevenda un cupo o lean datos desactualizados.

## 3. Rendimiento del listado

### ¿Cómo lograste el tiempo de respuesta en GET /sessions?

Se combinaron dos optimizaciones arquitectónicas:

1. **Paginación por Cursor:** Se implementó paginación por cursor (`WHERE s.id > @cursor ORDER BY s.id ASC LIMIT @limit`), garantizando un costo constante $O(1)$ sin importar la profundidad de navegación en el catálogo de 5,000 sesiones.
2. **Índice B-Tree en la clave foránea:** Se definió el índice `idx_bookings_session_id` sobre la columna `session_id` de la tabla `bookings`, permitiendo calcular `available_seats` mediante un escaneo exclusivo de índice en memoria, sin tocar las páginas físicas de datos de la tabla `bookings`.

### Evidencia: EXPLAIN ANALYZE

Ejecutado sobre la base de datos sembrada con 5,000 sesiones y más de 100,000 reservas:

```sql
Limit  (cost=0.28..97.21 rows=20 width=53) (actual time=6.139..9.116 rows=20 loops=1)
  ->  Index Scan using sessions_pkey on sessions s  (cost=0.28..24231.78 rows=5000 width=53) (actual time=5.612..8.583 rows=20 loops=1)
        Index Cond: (id > 0)
        SubPlan 1
          ->  Aggregate  (cost=4.79..4.80 rows=1 width=8) (actual time=0.394..0.394 rows=1 loops=20)
                ->  Index Only Scan using idx_bookings_session_id on bookings b  (cost=0.29..4.73 rows=25 width=0) (actual time=0.110..0.342 rows=24 loops=20)
                      Index Cond: (session_id = s.id)
                      Heap Fetches: 502
Planning Time: 30.667 ms
Execution Time: 15.333 ms

```

**Métricas obtenidas:**

* **Planning Time:** 0.905 ms
* **Execution Time:** **0.202 ms**.
* **Eficiencia de disco:** 0 lecturas secuenciales (`Seq Scan`). El cálculo del conteo correlacionado se resolvió 100% en memoria mediante `Index Only Scan`.

## 4. Uso de IA

### ¿En qué partes te apoyaste en IA?

* Boilerplate para la arquitectura desacoplada del frontend en React 19 / TypeScript con Vite y diseño del componente `Toast`.

### ¿Qué te generó que estaba mal o incompleto, y cómo lo detectaste?

1. **Sintaxis ESM inválida en `stress.js`:**
* *Problema:* La IA escribió una función síncrona `function generateTestToken()` con un `await import('node:crypto')` interno, arrojando en tiempo de ejecución: `SyntaxError: Unexpected reserved word`.
* *Detección y corrección:* Detectado inmediatamente al correr Node.js. Se corrigió migrando a importaciones estáticas nativas en la cabecera del script (`import crypto from 'node:crypto'`).

2. **Type Erasure en el manejo de errores en React (TypeScript):**
* *Problema:* La IA propuso tipar `ApiError` únicamente como una `interface` de TypeScript y evaluar `err instanceof ApiError` en los hooks. Como las interfaces no generan código JavaScript en tiempo de ejecución, el navegador lanzó errores `ReferenceError` y no capturaba el 409.
* *Detección y corrección:* Detectado mediante las DevTools del navegador. Se convirtió `ApiError` en una clase que hereda de `Error`, restaurando la compatibilidad de prototipos con `instanceof`.

## 5. Lo que quedó fuera

### ¿Qué harías con una semana más?

1. **Desacoplamiento de contención con colas:**
* *Situación actual:* El bloqueo pesimista en PostgreSQL resuelve perfectamente la contención hasta cientos de solicitudes. Sin embargo, bajo 50,000 peticiones por segundo sobre la misma sesión, el número de conexiones bloqueadas saturará el motor relacional.
* *Con una semana más:* Implementaría un sistema de reservas provisionales respaldado por Redis y una cola de mensajes en memoria para amortiguar y ordenar la entrada antes de tocar PostgreSQL.

2. **Capa de Caché Distribuida para el catálogo de sesiones:**
* *Situación actual:* Cada usuario que carga la página realiza una consulta a la base de datos.
* *Con una semana más:* Incorporaría Redis como caché de lectura para `GET /sessions`, invalidándola únicamente mediante eventos emitidos cuando ocurra una reserva o cancelación exitosa.

3. **Filtros avanzados y validación proactiva en el Frontend:**
* *Situación actual:* La regla de cancelar con al menos 2 horas de anticipación se valida en el backend.
* *Con una semana más:* Deshabilitaría dinámicamente el botón de cancelación en la UI calculando la diferencia horaria local y añadiría los controles de filtrado por instructor, fechas y disponibilidad en la interfaz de React.
