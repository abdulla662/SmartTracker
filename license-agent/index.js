/**
 * DealTrack License Agent
 * Runs as a sidecar container with Docker socket mounted.
 * Every 5 minutes: checks license server.
 * If revoked → stops all project containers, removes images, deletes project dir.
 */

const fetch       = require('node-fetch');
const { execSync } = require('child_process');
const os          = require('os');
const fs          = require('fs');

const LICENSE_KEY        = process.env.LICENSE_KEY        || '';
const LICENSE_SERVER_URL = process.env.LICENSE_SERVER_URL || '';
const PROJECT_DIR        = process.env.PROJECT_DIR        || '/project';
const COMPOSE_PROJECT    = process.env.COMPOSE_PROJECT    || 'dealtrack';
const CHECK_INTERVAL_MS  = 5 * 60 * 1000; // 5 minutes

if (!LICENSE_KEY || !LICENSE_SERVER_URL) {
  console.error('[License] ERROR: LICENSE_KEY and LICENSE_SERVER_URL must be set.');
  process.exit(1);
}

function getDeviceId() {
  try {
    // Use MAC address of first network interface as stable device ID
    const nets = os.networkInterfaces();
    for (const iface of Object.values(nets)) {
      for (const net of iface) {
        if (!net.internal && net.mac && net.mac !== '00:00:00:00:00:00') {
          return net.mac.replace(/:/g, '').toUpperCase();
        }
      }
    }
  } catch (_) {}
  return 'UNKNOWN';
}

async function checkLicense() {
  try {
    const res = await fetch(`${LICENSE_SERVER_URL}/api/license/heartbeat`, {
      method:  'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        licenseKey:  LICENSE_KEY,
        deviceId:    getDeviceId(),
        deviceName:  os.hostname(),
        ip:          '', // server will read from request
        version:     process.env.APP_VERSION || '1.0',
        os:          `${os.type()} ${os.release()}`,
      }),
      timeout: 10000,
    });

    if (!res.ok) {
      console.log(`[License] Server returned ${res.status} — will retry next cycle.`);
      return;
    }

    const data = await res.json();
    console.log(`[License] Heartbeat OK — valid=${data.valid} revoked=${data.revoked}`);

    if (data.revoked) {
      console.error('[License] LICENSE REVOKED. Initiating self-destruct in 10 seconds...');
      setTimeout(selfDestruct, 10000);
    }

  } catch (err) {
    console.warn(`[License] Cannot reach license server: ${err.message} — offline, will retry.`);
    // If offline, we do NOT self-destruct — only confirmed revocation triggers it
  }
}

function selfDestruct() {
  console.error('[License] *** SELF-DESTRUCT INITIATED ***');

  try {
    // 1. Stop and remove all containers in the compose project
    console.error('[License] Stopping Docker containers...');
    execSync(
      `docker ps -q --filter "label=com.docker.compose.project=${COMPOSE_PROJECT}" | xargs -r docker stop`,
      { stdio: 'inherit', timeout: 60000 }
    );

    // 2. Remove containers
    execSync(
      `docker ps -aq --filter "label=com.docker.compose.project=${COMPOSE_PROJECT}" | xargs -r docker rm -f`,
      { stdio: 'inherit', timeout: 30000 }
    );

    // 3. Remove images used by the project
    execSync(
      `docker images --format "{{.Repository}}:{{.Tag}}" | grep "${COMPOSE_PROJECT}" | xargs -r docker rmi -f`,
      { stdio: 'inherit', timeout: 60000 }
    );

    // 4. Remove Docker volumes for the project
    execSync(
      `docker volume ls -q --filter "label=com.docker.compose.project=${COMPOSE_PROJECT}" | xargs -r docker volume rm -f`,
      { stdio: 'inherit', timeout: 30000 }
    );

    console.error('[License] Containers and images removed.');

  } catch (err) {
    console.error(`[License] Docker cleanup error: ${err.message}`);
  }

  try {
    // 5. Delete the project directory (source code, compose files, .env)
    if (fs.existsSync(PROJECT_DIR)) {
      console.error(`[License] Deleting project directory: ${PROJECT_DIR}`);
      execSync(`rm -rf "${PROJECT_DIR}"`, { timeout: 30000 });
      console.error('[License] Project directory deleted.');
    }
  } catch (err) {
    console.error(`[License] File deletion error: ${err.message}`);
  }

  console.error('[License] Self-destruct complete. Exiting.');
  process.exit(0);
}

// ── Start ─────────────────────────────────────────────────────────────────────
console.log(`[License] Agent started. Key: ${LICENSE_KEY.slice(0,8)}... Server: ${LICENSE_SERVER_URL}`);
checkLicense(); // immediate check on startup
setInterval(checkLicense, CHECK_INTERVAL_MS);
