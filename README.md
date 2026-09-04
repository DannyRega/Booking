# Plataforma de Reserva de Talleres

Solución de alta concurrencia y prevención de sobreventa de cupos para una plataforma de reserva de talleres presenciales. Desarrollada con **.NET 10** en el backend y **React 19 (TypeScript + Vite)** en el frontend, respaldada por **PostgreSQL 16**.

## Arquitectura y Tecnologías

### Backend (.NET 10)
- **Clean Architecture:** Estructurado en capas desacopladas (`Domain`, `Application`, `Infrastructure`, `Api`).
- **Control de Concurrencia:** Bloqueo pesimista a nivel de fila (`SELECT ... FOR UPDATE`) dentro de transacciones ACID en PostgreSQL, garantizando cero sobreventas sin importar la cantidad de réplicas de la API.
- **Idempotencia:** Cabecera `Idempotency-Key` persistida en base de datos para responder de forma idéntica ante reintentos de red.
- **Rendimiento:** Paginación por cursor $O(1)$ (`WHERE id > cursor ORDER BY id LIMIT limit`) con filtros opcionales (`from`, `to`, `instructor`, `only_available`) sanitizados en UTC para PostgreSQL `timestamptz`.
- **Seguridad:** Consultas parametrizadas (EF Core), autenticación JWT y validación de propiedad en cancelaciones.

### Frontend (React 19 + TypeScript + Vite)
- **Estructura Modular:** Capas de dominio, infraestructura (`HttpClient` centralizado) y presentación.
- **Paginación Dinámica:** Patrón *Load More* consumiendo el cursor devuelto por el backend asistido por `useTransition`.
- **Manejo de Errores Tipados:** Clase personalizada `ApiError extends Error` y notificaciones flotantes temporales (`Toast`) para capturar y mostrar respuestas sin romper el flujo de la aplicación.

## Requisitos Previos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js (v20 o superior)](https://nodejs.org/)
- [Docker y Docker Compose](https://www.docker.com/)

## Ejecución Local

### 1. Base de Datos (PostgreSQL en Docker)

Levanta el contenedor de PostgreSQL con soporte de conexiones suficiente para pruebas de estrés masivas:

```bash
docker run -d --name booking_db \
  -e POSTGRES_DB=booking_db \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgrespassword \
  -p 5432:5432 \
  postgres:16-alpine

```

Ajusta `max_connections`, reinicia el contenedor y aplica el esquema y los datos sembrados:

```bash
docker exec -it booking_db psql -U postgres -d booking_db -c "ALTER SYSTEM SET max_connections = 300;"
docker restart booking_db

# Copiar y ejecutar esquemas y datos iniciales
docker cp schema.sql booking_db:/schema.sql
docker cp seed.sql booking_db:/seed.sql
docker exec -it booking_db psql -U postgres -d booking_db -f /schema.sql
docker exec -it booking_db psql -U postgres -d booking_db -f /seed.sql

```

Asegura la existencia de los usuarios de prueba requeridos por el script de estrés:

```bash
docker exec -it booking_db psql -U postgres -d booking_db -c "INSERT INTO users (id, email, password_hash) SELECT g, 'stress_' || g || '@test.com', 'pwd' FROM generate_series(1, 500) g ON CONFLICT (id) DO NOTHING;"

```

### 3. Backend

Crea o verifica el archivo `.env` en la raíz del proyecto:

```env
DB_CONNECTION_STRING=Host=localhost;Port=5432;Database=booking_db;Username=postgres;Password=postgrespassword;Maximum Pool Size=300;Timeout=60;Command Timeout=60;
JWT_SECRET=super_secret_evaluation_key_1234567890_min32chars

```

### 4. Frontend

Crea o verifica el archivo `.env` en la raíz del proyecto:

```env
VITE_API_URL=http://localhost:5000

```

Levanta todo el stack:

```bash
docker compose up --build -d

```

Backend (.NET Core 10): `http://localhost:5000/swagger/index.html`
Frontend (React 19): `http://localhost:5173`

## Verificación y Pruebas

### 1. Pruebas Automatizadas (xUnit)

Ejecuta la suite de pruebas que valida los requisitos mínimos obligatorios (idempotencia, solapamiento parcial, solapamiento envolvente y contención concurrente):

```bash
dotnet clean
dotnet test

```

*Resultado esperado: 4/4 pruebas aprobadas.*

### 2. Prueba de Concurrencia (`stress.js`)

Ejecuta la prueba de carga de 200 peticiones simultáneas sobre la sesión 42 (10 lugares de capacidad):

```bash
# Limpiar reservas previas de la sesión 42 si existieran
docker exec -it booking_db psql -U postgres -d booking_db -c "DELETE FROM bookings WHERE session_id = 42;"

# Ejecutar script
node stress.js --session=42 --concurrency=200

```

*Salida esperada:*

```text
  201 Created  ·············  10
  409 Conflict ·············  190
  500 Error    ·············   0

  SELECT COUNT(*) FROM bookings WHERE session_id = 42;
  → 10

  PASS — sin sobreventa en 5 ejecuciones consecutivas

```

## Especificación de Endpoints

| Método | Endpoint | Cabeceras | Descripción |
| --- | --- | --- | --- |
| `POST` | `/login` | `Content-Type: application/json` | Autenticación con usuarios sembrados (`user1@test.com` / `password123`). Devuelve JWT. |
| `GET` | `/sessions` | Ninguna | Listado con cursor, `available_seats` y filtros opcionales (`?from=&to=&instructor=&only_available=&cursor=&limit=`). |
| `POST` | `/bookings` | `Authorization: Bearer <token>`, `Idempotency-Key: <uuid>` (opcional) | Reserva atómica con bloqueo pesimista. Devuelve `201` o `409 Conflict`. |
| `DELETE` | `/bookings/:id` | `Authorization: Bearer <token>` | Libera el cupo. Valida que el usuario sea el propietario y que falten más de 2 horas para el inicio. |
