-- 1. Insertar usuarios de prueba para JWT
INSERT INTO users (id, email, password_hash)
VALUES 
    (1, 'user1@test.com', 'hashed_pwd_placeholder'),
    (2, 'user2@test.com', 'hashed_pwd_placeholder')
ON CONFLICT (id) DO NOTHING;

-- Generar usuarios adicionales para poblar las 100.000 reservas
INSERT INTO users (email, password_hash)
SELECT 
    'user_' || g || '@test.com',
    'hashed_pwd'
FROM generate_series(3, 2500) g
ON CONFLICT DO NOTHING;

-- 2. Insertar la Sesión Crítica 42 para la prueba de estrés
-- Capacidad 10, limpia de reservas iniciales
INSERT INTO sessions (id, title, instructor, starts_at, duration_minutes, capacity)
VALUES (42, 'Taller de Alta Concurrencia', 'Martin Fowler', NOW() + INTERVAL '10 days', 90, 10)
ON CONFLICT (id) DO UPDATE 
SET capacity = 10, starts_at = NOW() + INTERVAL '10 days';

-- 3. Generar 5.000 sesiones
INSERT INTO sessions (id, title, instructor, starts_at, duration_minutes, capacity)
SELECT 
    s,
    'Taller #' || s,
    'Instructor ' || (s % 20 + 1),
    NOW() + ((s % 60) || ' days')::INTERVAL + ((s % 12) || ' hours')::INTERVAL,
    CASE WHEN s % 3 = 0 THEN 60 WHEN s % 3 = 1 THEN 90 ELSE 120 END,
    CASE WHEN s % 5 = 0 THEN 20 WHEN s % 5 = 1 THEN 30 ELSE 50 END
FROM generate_series(1, 5000) s
WHERE s <> 42
ON CONFLICT (id) DO NOTHING;

-- Sincronizar secuencia de IDs de sesiones
SELECT setval('sessions_id_seq', (SELECT MAX(id) FROM sessions));
SELECT setval('users_id_seq', (SELECT MAX(id) FROM users));

-- 4. Generar ~100.000 reservas (distribuidas en las sesiones de 1 a 4000, respetando que la 42 quede libre)
INSERT INTO bookings (session_id, user_id, created_at)
SELECT 
    s.id,
    u.id,
    NOW() - ((u.id % 30) || ' days')::INTERVAL
FROM 
    (SELECT id FROM sessions WHERE id <> 42 AND id <= 4000) s
CROSS JOIN LATERAL 
    (SELECT id FROM users WHERE id <= 27 ORDER BY RANDOM() LIMIT 25) u
ON CONFLICT DO NOTHING;