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
const username = process.env.ACH_UAT_USER || '';
const password = process.env.ACH_UAT_PASSWORD || '';
const outputDir = path.join(__dirname, 'capturas');
const resultsPath = path.join(outputDir, 'resultado_capturas.json');
const accessTokenKey = 'ach.interbank.access_token';

const routePlans = [
  {
    key: 'inicio_o_login',
    fileName: '01_inicio_o_login.png',
    candidates: ['/login'],
    description: 'Pantalla inicial o ingreso funcional',
    allowLoginScreen: true
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
    candidates: ['/ach/nacha/soap-uat-console'],
    description: 'Consola UAT o SOAP'
  },
  {
    key: 'menu_o_navegacion',
    fileName: '10_menu_o_navegacion.png',
    candidates: ['/dashboard', '/ach-cycles', '/cenit'],
    description: 'Menu o navegacion principal',
    captureMenu: true
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
    environment: {
      achUatUserExists: Boolean(username),
      achUatPasswordExists: Boolean(password)
    },
    login: {
      attempted: false,
      success: false,
      finalUrl: null,
      finalPath: null,
      tokenPresent: false,
      message: ''
    },
    optionalRoutes: [],
    routes: []
  };

  try {
    const loginCapture = await captureLoginScreen(page, summary);
    summary.routes.push(loginCapture);

    if (!username || !password) {
      summary.login.message = 'Credenciales de prueba no disponibles en variables de entorno.';
      return writeSummary(summary);
    }

    const loginResult = await performLogin(page);
    summary.login = {
      attempted: true,
      success: loginResult.success,
      finalUrl: loginResult.finalUrl,
      finalPath: loginResult.finalPath,
      tokenPresent: loginResult.tokenPresent,
      message: loginResult.message,
      authStatus: loginResult.authStatus ?? null
    };

    if (!loginResult.success) {
      for (const plan of routePlans.slice(1)) {
        summary.routes.push(createLoginFailureRouteResult(plan));
      }
      summary.optionalRoutes.push({
        candidate: '/uat',
        requestedUrl: new URL('/uat', ensureTrailingSlash(baseUrl)).toString(),
        description: 'Pantalla UAT opcional',
        finalUrl: null,
        finalPath: null,
        status: 401,
        available: false,
        observation: 'Omitida porque el login fallido dejo la sesion invalida o sin autorizacion.'
      });
      return writeSummary(summary);
    }

    for (const plan of routePlans.slice(1)) {
      const result = await evaluatePlan(page, plan);
      summary.routes.push(result);
      writeConsoleLine(result);
    }

    const uatCheck = await evaluateOptionalRoute(page, '/uat', 'Pantalla UAT opcional');
    summary.optionalRoutes.push(uatCheck);
    writeOptionalConsoleLine(uatCheck);
  } finally {
    await browser.close();
  }

  writeSummary(summary);
}

async function captureLoginScreen(page, summary) {
  const loginUrl = new URL('/login', ensureTrailingSlash(baseUrl)).toString();
  const response = await page.goto(loginUrl, {
    waitUntil: 'domcontentloaded',
    timeout: 20000
  });
  await waitForSettledUi(page);

  const screenshotPath = path.join(outputDir, '01_inicio_o_login.png');
  await page.screenshot({ path: screenshotPath, fullPage: true });

  const finalUrl = page.url();
  const finalPath = safePathname(finalUrl);
  const title = await safeTitle(page);
  const bodyText = await safeBodyText(page);

  summary.login.finalUrl = finalUrl;
  summary.login.finalPath = finalPath;

  return {
    key: 'inicio_o_login',
    description: 'Pantalla inicial o ingreso funcional',
    requestedCandidate: '/login',
    requestedUrl: loginUrl,
    finalUrl,
    finalPath,
    status: response ? response.status() : null,
    captureStatus: finalPath === '/auth/login' ? 'omitted' : 'captured',
    screenshot: finalPath === '/auth/login' ? null : '01_inicio_o_login.png',
    title,
    bodyPreview: normalizeWhitespace(bodyText).slice(0, 240),
    attempts: [
      {
        candidate: '/login',
        url: loginUrl,
        probeStatus: response ? response.status() : null,
        probeError: null,
        observation:
          finalPath === '/auth/login'
            ? 'Omitida porque la pantalla visual no puede terminar en /auth/login.'
            : 'Captura inicial de la pantalla visual /login.'
      }
    ]
  };
}

async function performLogin(page) {
  const loginUrl = new URL('/login', ensureTrailingSlash(baseUrl)).toString();
  await page.goto(loginUrl, {
    waitUntil: 'domcontentloaded',
    timeout: 20000
  });
  await waitForSettledUi(page);

  await page.locator('input[formcontrolname="username"]').fill(username);
  await page.locator('input[formcontrolname="password"]').fill(password);

  const submitButton = page.getByRole('button', { name: /Ingresar|Ingresando/i });
  const authResponsePromise = page.waitForResponse(
    (response) => response.request().method() === 'POST' && response.url().includes('/auth/login'),
    { timeout: 15000 }
  );

  await Promise.allSettled([
    page.waitForLoadState('networkidle', { timeout: 15000 }),
    authResponsePromise,
    submitButton.click()
  ]);
  await waitForSettledUi(page);

  const finalUrl = page.url();
  const finalPath = safePathname(finalUrl);
  const tokenPresent = await hasSessionToken(page);
  const bodyText = normalizeWhitespace(await safeBodyText(page));
  const authResponse = await authResponsePromise.catch(() => null);
  const authStatus = authResponse ? authResponse.status() : null;

  if (tokenPresent && finalPath !== '/login' && finalPath !== '/auth/login') {
    return {
      success: true,
      finalUrl,
      finalPath,
      tokenPresent,
      message: 'Sesion autenticada.',
      authStatus
    };
  }

  const loginFailedMessage = bodyText.includes('No fue posible iniciar') || bodyText.includes('Ingresando...')
    ? 'login fallido con credenciales de prueba'
    : 'login fallido con credenciales de prueba';

  return {
    success: false,
    finalUrl,
    finalPath,
    tokenPresent,
    message: loginFailedMessage,
    authStatus
  };
}

async function evaluatePlan(page, plan) {
  const attempts = [];

  for (const candidate of plan.candidates) {
    const candidateUrl = new URL(candidate, ensureTrailingSlash(baseUrl)).toString();
    const attempt = {
      candidate,
      url: candidateUrl,
      probeStatus: null,
      probeError: null
    };
    attempts.push(attempt);

    try {
      const navigation = await page.goto(candidateUrl, {
        waitUntil: 'domcontentloaded',
        timeout: 20000
      });
      await waitForSettledUi(page);

      const finalUrl = page.url();
      const finalPath = safePathname(finalUrl);
      const status = navigation ? navigation.status() : null;
      const title = await safeTitle(page);
      const bodyText = await safeBodyText(page);
      const bodyPreview = normalizeWhitespace(bodyText).slice(0, 240);

      attempt.probeStatus = status;
      attempt.finalUrl = finalUrl;
      attempt.finalPath = finalPath;

      const classification = classifyRouteOutcome({
        requestedCandidate: candidate,
        finalPath,
        status,
        bodyPreview,
        tokenPresent: await hasSessionToken(page)
      });

      if (classification.capture) {
        const screenshotPath = path.join(outputDir, plan.fileName);
        if (plan.captureMenu) {
          await captureMenu(page, screenshotPath);
        } else {
          await page.screenshot({ path: screenshotPath, fullPage: true });
        }

        attempt.observation = classification.observation;
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
          bodyPreview,
          attempts
        };
      }

      attempt.observation = classification.observation;
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
    finalUrl: lastAttempt ? lastAttempt.finalUrl || null : null,
    finalPath: lastAttempt ? lastAttempt.finalPath || null : null,
    status: lastAttempt ? lastAttempt.probeStatus : null,
    captureStatus: 'omitted',
    screenshot: null,
    title: '',
    bodyPreview: '',
    attempts
  };
}

async function evaluateOptionalRoute(page, candidate, description) {
  const candidateUrl = new URL(candidate, ensureTrailingSlash(baseUrl)).toString();

  try {
    const response = await page.goto(candidateUrl, {
      waitUntil: 'domcontentloaded',
      timeout: 20000
    });
    await waitForSettledUi(page);

    const finalUrl = page.url();
    const finalPath = safePathname(finalUrl);
    const status = response ? response.status() : null;
    const classification = classifyRouteOutcome({
      requestedCandidate: candidate,
      finalPath,
      status,
      bodyPreview: normalizeWhitespace(await safeBodyText(page)).slice(0, 240),
      tokenPresent: await hasSessionToken(page)
    });

    return {
      candidate,
      requestedUrl: candidateUrl,
      description,
      finalUrl,
      finalPath,
      status,
      available: classification.capture,
      observation: classification.capture
        ? 'Ruta opcional disponible y funcional.'
        : classification.observation
    };
  } catch (error) {
    return {
      candidate,
      requestedUrl: candidateUrl,
      description,
      finalUrl: null,
      finalPath: null,
      status: null,
      available: false,
      observation: `Fallo de navegacion: ${error.message}`
    };
  }
}

function classifyRouteOutcome({ requestedCandidate, finalPath, status, bodyPreview, tokenPresent }) {
  if (finalPath === '/auth/login') {
    return {
      capture: false,
      observation: 'Omitida por redireccion a /auth/login, ruta tecnica/no funcional.'
    };
  }

  if (status === 405) {
    return {
      capture: false,
      observation: 'Omitida por ruta tecnica/no funcional (405).'
    };
  }

  if (status === 401) {
    return {
      capture: false,
      observation: 'Omitida por sesion invalida o falta de autorizacion (401).'
    };
  }

  if (status === 403 || finalPath === '/unauthorized') {
    return {
      capture: false,
      observation: 'Omitida por falta de permisos (403).'
    };
  }

  if (status === 404 || finalPath === '/not-found') {
    return {
      capture: false,
      observation: 'Omitida por ruta no disponible (404).'
    };
  }

  if (finalPath === '/login' && requestedCandidate !== '/login') {
    return {
      capture: false,
      observation: tokenPresent
        ? 'Omitida por posible falta de permisos: la ruta redirige a /login despues de autenticar.'
        : 'Omitida por requerir autenticacion y redirigir a /login.'
    };
  }

  if (bodyPreview.includes('No autorizado')) {
    return {
      capture: false,
      observation: 'Omitida por falta de permisos.'
    };
  }

  return {
    capture: true,
    observation: 'Captura generada.'
  };
}

async function captureMenu(page, screenshotPath) {
  const sidebar = page.locator('aside.sidebar');
  const count = await sidebar.count();

  if (count > 0) {
    await sidebar.screenshot({ path: screenshotPath });
    return;
  }

  await page.screenshot({ path: screenshotPath, fullPage: true });
}

async function hasSessionToken(page) {
  return page.evaluate((key) => Boolean(window.sessionStorage.getItem(key)), accessTokenKey);
}

async function safeTitle(page) {
  try {
    return await page.title();
  } catch {
    return '';
  }
}

async function safeBodyText(page) {
  try {
    return await page.locator('body').innerText();
  } catch {
    return '';
  }
}

async function waitForSettledUi(page) {
  await page.waitForTimeout(1500);
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

function writeOptionalConsoleLine(result) {
  const status = result.available ? 'DISPONIBLE' : 'OMITIDA';
  console.log(`[${status}] ${result.candidate} -> ${result.finalUrl || 'sin URL final'}`);
}

function createLoginFailureRouteResult(plan) {
  const firstCandidate = plan.candidates[0] || null;
  return {
    key: plan.key,
    description: plan.description,
    requestedCandidate: firstCandidate,
    requestedUrl: firstCandidate ? new URL(firstCandidate, ensureTrailingSlash(baseUrl)).toString() : null,
    finalUrl: null,
    finalPath: null,
    status: 401,
    captureStatus: 'omitted',
    screenshot: null,
    title: '',
    bodyPreview: '',
    attempts: [
      {
        candidate: firstCandidate,
        url: firstCandidate ? new URL(firstCandidate, ensureTrailingSlash(baseUrl)).toString() : null,
        probeStatus: 401,
        probeError: null,
        observation: 'Omitida porque el login fallido dejo la sesion invalida o sin autorizacion.'
      }
    ]
  };
}

function writeSummary(summary) {
  fs.writeFileSync(resultsPath, JSON.stringify(summary, null, 2), 'utf8');
}

main().catch((error) => {
  console.error(error.message);
  process.exitCode = 1;
});
