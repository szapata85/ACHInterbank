const fs = require('fs');
const path = require('path');
const { URL } = require('url');

const playwright = require(path.resolve(
  __dirname,
  '..',
  'web',
  'ach-interbank-ui',
  'node_modules',
  'playwright'
));

const baseUrl = process.env.ACH_UI_BASE_URL || 'http://localhost:743';
const outputDir = path.join(__dirname, 'capturas');
const resultsPath = path.join(outputDir, 'resultado_capturas.json');

const routePlans = [
  {
    key: 'inicio_o_login',
    fileName: '01_inicio_o_login.png',
    candidates: ['/login', '/'],
    description: 'Pantalla inicial o ingreso funcional'
  },
  {
    key: 'dashboard',
    fileName: '02_dashboard.png',
    candidates: ['/dashboard'],
    description: 'Dashboard principal'
  },
  {
    key: 'dashboard_operacional_nacha',
    fileName: '03_dashboard_operacional_nacha.png',
    candidates: ['/ach/nacha/operational-dashboard'],
    description: 'Dashboard operacional NACHA-M'
  },
  {
    key: 'configuracion_perfiles_nacha',
    fileName: '04_configuracion_perfiles_nacha.png',
    candidates: ['/nacha-config-admin/perfiles'],
    description: 'Configuracion de perfiles NACHA-M'
  },
  {
    key: 'exportacion_nacha',
    fileName: '05_exportacion_nacha.png',
    candidates: ['/ach-cycles/nacha/export'],
    description: 'Exportacion NACHA'
  },
  {
    key: 'ciclos_ach',
    fileName: '06_ciclos_ach.png',
    candidates: ['/ach-cycles'],
    description: 'Ciclos ACH'
  },
  {
    key: 'transacciones',
    fileName: '07_transacciones.png',
    candidates: ['/transactions'],
    description: 'Pantalla de transacciones'
  },
  {
    key: 'cenit',
    fileName: '08_cenit.png',
    candidates: ['/cenit'],
    description: 'Pantalla CENIT'
  },
  {
    key: 'uat_console',
    fileName: '09_uat_console.png',
    candidates: ['/ach/nacha/soap-uat-console', '/uat'],
    description: 'Consola UAT o SOAP'
  },
  {
    key: 'menu_o_navegacion',
    fileName: '10_menu_o_navegacion.png',
    candidates: ['/dashboard', '/ach-cycles', '/cenit'],
    description: 'Menu o navegacion principal'
  }
];

async function main() {
  fs.mkdirSync(outputDir, { recursive: true });

  const browser = await playwright.chromium.launch({ headless: true });
  const context = await browser.newContext({
    viewport: { width: 1440, height: 1024 },
    ignoreHTTPSErrors: true
  });
  const page = await context.newPage();

  const summary = {
    executedAtUtc: new Date().toISOString(),
    baseUrl,
    routes: []
  };

  try {
    for (const plan of routePlans) {
      const result = await evaluatePlan(context, page, plan);
      summary.routes.push(result);
      writeConsoleLine(result);
    }
  } finally {
    await browser.close();
  }

  fs.writeFileSync(resultsPath, JSON.stringify(summary, null, 2), 'utf8');
}

async function evaluatePlan(context, page, plan) {
  const attempts = [];

  for (const candidate of plan.candidates) {
    const candidateUrl = new URL(candidate, ensureTrailingSlash(baseUrl)).toString();
    const probe = await probeRoute(context, candidateUrl);
    const attempt = {
      candidate,
      url: candidateUrl,
      probeStatus: probe.status,
      probeError: probe.error || null
    };
    attempts.push(attempt);

    if (probe.status === 405) {
      attempt.observation = 'Omitida por endpoint tecnico/no funcional (405).';
      continue;
    }

    if (probe.status >= 400 && probe.status < 500) {
      attempt.observation = `Omitida por respuesta ${probe.status}.`;
      continue;
    }

    try {
      const navigation = await page.goto(candidateUrl, {
        waitUntil: 'domcontentloaded',
        timeout: 15000
      });
      await page.waitForTimeout(1500);

      const finalUrl = page.url();
      const finalPath = safePathname(finalUrl);
      const status = navigation ? navigation.status() : probe.status;
      const title = await page.title();
      const bodyText = await page.locator('body').innerText().catch(() => '');
      const screenshotPath = path.join(outputDir, plan.fileName);

      if (finalPath.startsWith('/auth/')) {
        attempt.navigationStatus = status;
        attempt.finalUrl = finalUrl;
        attempt.finalPath = finalPath;
        attempt.observation = 'Omitida porque redirige a ruta /auth/*, tratada como tecnica/no funcional.';
        continue;
      }

      if (finalPath === '/login' && !plan.candidates.includes('/login') && !plan.candidates.includes('/')) {
        attempt.navigationStatus = status;
        attempt.finalUrl = finalUrl;
        attempt.finalPath = finalPath;
        attempt.observation = 'Omitida porque requiere autenticacion y redirige a la pantalla visual de login.';
        continue;
      }

      await page.screenshot({ path: screenshotPath, fullPage: true });

      return {
        key: plan.key,
        description: plan.description,
        requestedCandidate: candidate,
        requestedUrl: candidateUrl,
        finalUrl,
        finalPath,
        status,
        captureStatus: 'captured',
        screenshot: plan.fileName,
        title,
        bodyPreview: normalizeWhitespace(bodyText).slice(0, 240),
        attempts
      };
    } catch (error) {
      attempt.navigationError = error.message;
      attempt.observation = 'Fallo de navegacion.';
    }
  }

  const lastAttempt = attempts[attempts.length - 1] || null;
  return {
    key: plan.key,
    description: plan.description,
    requestedCandidate: lastAttempt ? lastAttempt.candidate : null,
    requestedUrl: lastAttempt ? lastAttempt.url : null,
    finalUrl: null,
    finalPath: null,
    status: lastAttempt ? lastAttempt.probeStatus : null,
    captureStatus: 'omitted',
    screenshot: null,
    title: '',
    bodyPreview: '',
    attempts
  };
}

async function probeRoute(context, url) {
  try {
    const response = await context.request.get(url, {
      failOnStatusCode: false,
      timeout: 10000
    });
    return { status: response.status() };
  } catch (error) {
    return { status: null, error: error.message };
  }
}

function normalizeWhitespace(text) {
  return (text || '').replace(/\s+/g, ' ').trim();
}

function safePathname(url) {
  try {
    return new URL(url).pathname || '/';
  } catch {
    return '';
  }
}

function ensureTrailingSlash(value) {
  return value.endsWith('/') ? value : `${value}/`;
}

function writeConsoleLine(result) {
  const status = result.captureStatus === 'captured' ? 'CAPTURADA' : 'OMITIDA';
  const routeLabel = result.requestedCandidate || '(sin ruta)';
  const detail = result.finalUrl || 'sin URL final';
  console.log(`[${status}] ${routeLabel} -> ${detail}`);
}

main().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});
