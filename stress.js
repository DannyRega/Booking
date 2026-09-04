import http from 'node:http';
import crypto from 'node:crypto';
import { execSync } from 'node:child_process';

const args = process.argv.slice(2);
let sessionId = 42;
let concurrency = 200;

for (const arg of args) {
  if (arg.startsWith('--session=')) {
    sessionId = parseInt(arg.split('=')[1], 10);
  } else if (arg.startsWith('--concurrency=')) {
    concurrency = parseInt(arg.split('=')[1], 10);
  }
}

const API_HOST = 'localhost';
const API_PORT = 5000;
const JWT_SECRET = 'super_secret_evaluation_key_1234567890_min32chars';

function generateTestToken(userId) {
  const header = Buffer.from(JSON.stringify({ alg: 'HS256', typ: 'JWT' })).toString('base64url');
  const payload = Buffer.from(JSON.stringify({
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier': String(userId),
    email: `stress_user_${userId}@test.com`,
    exp: Math.floor(Date.now() / 1000) + 3600
  })).toString('base64url');

  const signature = crypto
    .createHmac('sha256', JWT_SECRET)
    .update(`${header}.${payload}`)
    .digest('base64url');

  return `${header}.${payload}.${signature}`;
}

async function sendBooking(userId, sId) {
  const token = generateTestToken(userId);
  const data = JSON.stringify({ sessionId: sId });

  return new Promise((resolve) => {
    const req = http.request({
      hostname: API_HOST,
      port: API_PORT,
      path: '/bookings',
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Content-Length': Buffer.byteLength(data),
        'Authorization': `Bearer ${token}`
      }
    }, (res) => {
      let body = '';
      res.on('data', (chunk) => body += chunk);
      res.on('end', () => resolve(res.statusCode));
    });

    req.on('error', () => resolve(500));
    req.write(data);
    req.end();
  });
}

async function run() {
  console.log(`\nIniciando prueba de concurrencia: ${concurrency} peticiones sobre sesión ${sessionId}...`);

  const promises = [];
  for (let i = 1; i <= concurrency; i++) {
    promises.push(sendBooking(i + 100, sessionId));
  }

  const results = await Promise.all(promises);

  const stats = { 201: 0, 409: 0, 500: 0, others: 0 };
  for (const code of results) {
    if (stats[code] !== undefined) {
      stats[code]++;
    } else {
      stats.others++;
    }
  }

  console.log(`\n  201 Created  ·············  ${stats[201]}`);
  console.log(`  409 Conflict ·············  ${stats[409]}`);
  console.log(`  500 Error    ·············  ${stats[500]}`);
  if (stats.others > 0) {
    console.log(`  Otros        ·············  ${stats.others}`);
  }

  try {
    const sqlCmd = `docker exec booking_db psql -U postgres -d booking_db -t -c "SELECT COUNT(*) FROM bookings WHERE session_id = ${sessionId};"`;
    const count = parseInt(execSync(sqlCmd).toString().trim(), 10);

    console.log(`\n  SELECT COUNT(*) FROM bookings WHERE session_id = ${sessionId};`);
    console.log(`  → ${count}\n`);

    if (count === 10 && stats[201] === 10 && stats[500] === 0) {
      console.log('  PASS — sin sobreventa consistente\n');
    } else {
      console.log('  FAIL — sobreventa o discrepancia detectada\n');
    }
  } catch {
    console.log('  (Nota: Para verificar el conteo en BD manualmente: docker exec -it booking_db psql -U postgres -d booking_db -c "SELECT COUNT(*) FROM bookings WHERE session_id = 42;")');
  }
}

run();