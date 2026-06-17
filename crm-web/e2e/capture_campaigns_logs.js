const { chromium } = require('@playwright/test');
const path = require('path');

const artifactDir = 'C:\\Users\\acq20\\.gemini\\antigravity\\brain\\f301ad39-9990-4c18-aef5-dfc324cb01a8';
const mockToken = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJlbWFpbCI6Im1vY2t1c2VyQGRpYXguY29tIiwicm9sZSI6IkFkbWluIn0.dummy-signature';

const mockCampaigns = {
  items: [
    {
      id: "c-1",
      name: "Cold Email Agências Digitais BR",
      subject: "Aumente as conversões de WhatsApp na sua agência",
      status: 3, // Concluida
      sentCount: 1450,
      deliveredCount: 1420,
      openCount: 852,
      clickCount: 341,
      bounceCount: 20,
      failedCount: 5,
      unsubscribeCount: 5,
      totalRecipients: 1450,
      createdAt: "2026-06-01T10:00:00Z",
      updatedAt: "2026-06-14T09:00:00Z"
    },
    {
      id: "c-2",
      name: "Lançamento Produto SaaS Recorrente",
      subject: "Acesso antecipado à nova plataforma DIAX CRM",
      status: 1, // Agendada
      sentCount: 0,
      deliveredCount: 0,
      openCount: 0,
      clickCount: 0,
      bounceCount: 0,
      failedCount: 0,
      unsubscribeCount: 0,
      totalRecipients: 2450,
      createdAt: "2026-06-10T14:30:00Z",
      updatedAt: "2026-06-14T08:00:00Z"
    },
    {
      id: "c-3",
      name: "Boas-vindas Clientes Prime",
      subject: "Seu gerente DIAX exclusivo já está disponível",
      status: 0, // Rascunho
      sentCount: 0,
      deliveredCount: 0,
      openCount: 0,
      clickCount: 0,
      bounceCount: 0,
      failedCount: 0,
      unsubscribeCount: 0,
      totalRecipients: 0,
      createdAt: "2026-06-12T09:15:00Z",
      updatedAt: "2026-06-12T09:15:00Z"
    }
  ],
  totalCount: 3,
  totalPages: 1
};

const mockRecipients = {
  items: [
    {
      id: "r-1",
      recipientName: "Ana Silva",
      recipientEmail: "ana.silva@empresa.com.br",
      status: 2, // Enviado
      sentAt: "2026-06-14T10:00:00Z",
      deliveredAt: "2026-06-14T10:00:05Z",
      openedAt: "2026-06-14T10:15:00Z",
      readCount: 3,
      lastError: null
    },
    {
      id: "r-2",
      recipientName: "Bruno Souza",
      recipientEmail: "bruno.souza@startup.com",
      status: 2, // Enviado
      sentAt: "2026-06-14T10:01:00Z",
      deliveredAt: "2026-06-14T10:01:08Z",
      openedAt: null,
      readCount: 0,
      lastError: null
    },
    {
      id: "r-3",
      recipientName: "Carlos Oliveira",
      recipientEmail: "carlos@wrongdomain.xyz",
      status: 3, // Falhou
      sentAt: "2026-06-14T10:02:00Z",
      deliveredAt: null,
      openedAt: null,
      readCount: 0,
      lastError: "550 5.1.1 User Unknown / Address rejected"
    }
  ],
  totalCount: 3,
  totalPages: 1
};

const mockStats = {
  totalCount: 1450,
  debugCount: 420,
  informationCount: 850,
  warningCount: 120,
  errorCount: 52,
  criticalCount: 8
};

const mockLogs = {
  items: [
    {
      id: "log-1",
      timestampUtc: "2026-06-14T12:00:00Z",
      level: 3, // Information
      category: 0,
      message: "Database connection established successfully on SQL Server cluster",
      httpMethod: null,
      requestPath: null,
      statusCode: null
    },
    {
      id: "log-2",
      timestampUtc: "2026-06-14T11:58:32Z",
      level: 4, // Error
      category: 1,
      message: "Failed login attempt for user admin@test.com - Invalid credentials",
      httpMethod: "POST",
      requestPath: "/api/v1/auth/login",
      statusCode: 401
    },
    {
      id: "log-3",
      timestampUtc: "2026-06-14T11:55:12Z",
      level: 2, // Warning
      category: 3,
      message: "Campaign 'Agências Digitais BR' successfully processed and queued for sending",
      httpMethod: "POST",
      requestPath: "/api/v1/email-campaigns/campaigns/c-1/queue",
      statusCode: 200
    }
  ],
  page: 1,
  pageSize: 50,
  totalCount: 3,
  totalPages: 1
};

const mockLogDetail = {
  id: "log-2",
  correlationId: "corr-998877",
  requestId: "req-112233",
  timestampUtc: "2026-06-14T11:58:32Z",
  level: 4,
  category: 1,
  message: "Failed login attempt for user admin@test.com - Invalid credentials",
  httpMethod: "POST",
  requestPath: "/api/v1/auth/login",
  statusCode: 401,
  environment: "Production",
  machineName: "IIS-WEB-02",
  clientIp: "191.185.12.34",
  userAgent: "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
  headersJson: "{\"Content-Type\":\"application/json\",\"Accept\":\"*/*\",\"Host\":\"api.alexandrequeiroz.com.br\"}",
  exceptionType: "Diax.Domain.Exceptions.InvalidCredentialsException",
  exceptionMessage: "O email ou a senha informados estão incorretos.",
  targetSite: "Diax.Application.Services.AuthService.LoginAsync",
  stackTrace: "   at Diax.Application.Services.AuthService.LoginAsync(String email, String password) in D:\\a\\1\\s\\src\\Diax.Application\\Services\\AuthService.cs:line 45\n   at Diax.Api.Controllers.V1.AuthController.Login(LoginRequest req) in D:\\a\\1\\s\\src\\Diax.Api\\Controllers\\V1\\AuthController.cs:line 28",
  additionalData: "{\"inputEmail\":\"admin@test.com\",\"attemptCount\":3}"
};

async function run() {
  console.log('Starting Playwright...');
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext();
  const page = await context.newPage();

  console.log('Setting up API mocks...');
  await page.route('**/api/v1/**', async (route) => {
    const url = route.request().url();
    console.log(`Intercepted API request: ${url}`);

    if (url.includes('/auth/login')) {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ accessToken: mockToken })
      });
    }

    if (url.includes('/auth/me')) {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ email: 'mockuser@diax.com', role: 'Admin' })
      });
    }

    if (url.includes('/email-campaigns/campaigns/placeholder/recipients')) {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(mockRecipients)
      });
    }

    if (url.includes('/email-campaigns/campaigns/placeholder')) {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(mockCampaigns.items[0])
      });
    }

    if (url.includes('/email-campaigns/campaigns')) {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(mockCampaigns)
      });
    }

    if (url.includes('/logs/stats')) {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(mockStats)
      });
    }

    if (url.includes('/logs/log-2')) {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(mockLogDetail)
      });
    }

    if (url.includes('/logs')) {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(mockLogs)
      });
    }

    return route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({})
    });
  });

  // Navigate to login page first
  console.log('Navigating to login...');
  await page.goto('http://localhost:3000/login/');
  await page.waitForLoadState('networkidle');

  // Fill credentials and click Enter (login)
  console.log('Filling credentials and submitting login...');
  await page.locator('#email').fill('mockuser@diax.com');
  await page.getByLabel('Senha').fill('anypassword123');
  await page.getByRole('button', { name: 'Entrar' }).click();

  // Wait for redirect to dashboard
  console.log('Waiting for redirect...');
  await page.waitForSelector('.dsh', { timeout: 30000 });

  const resolutions = [
    { width: 1440, height: 900, suffix: '1440' },
    { width: 1280, height: 800, suffix: '1280' },
    { width: 768, height: 1024, suffix: '768' },
    { width: 390, height: 844, suffix: '390' }
  ];

  // 1. Capture Campaigns List Page
  console.log('Navigating to Campaigns List Page...');
  await page.goto('http://localhost:3000/campanhas/');
  await page.waitForSelector('.responsive-table', { timeout: 15000 });
  await page.waitForTimeout(2000); // let UI settle

  for (const res of resolutions) {
    console.log(`Campaigns List - setting viewport to ${res.width}x${res.height}...`);
    await page.setViewportSize({ width: res.width, height: res.height });
    await page.waitForTimeout(1000);
    await page.screenshot({
      path: path.join(artifactDir, `campaigns_list_${res.suffix}.png`),
      fullPage: false
    });
  }

  // 2. Capture Campaigns Detail Page
  console.log('Navigating to Campaigns Detail Page...');
  await page.goto('http://localhost:3000/campanhas/placeholder/');
  await page.waitForSelector('.responsive-table', { timeout: 15000 });
  await page.waitForTimeout(2000); // let UI settle

  for (const res of resolutions) {
    console.log(`Campaigns Detail - setting viewport to ${res.width}x${res.height}...`);
    await page.setViewportSize({ width: res.width, height: res.height });
    await page.waitForTimeout(1000);
    await page.screenshot({
      path: path.join(artifactDir, `campaigns_detail_${res.suffix}.png`),
      fullPage: false
    });
  }

  // 3. Capture Logs Page
  console.log('Navigating to Logs Page...');
  await page.goto('http://localhost:3000/logs/');
  await page.waitForSelector('.responsive-table', { timeout: 15000 });
  await page.waitForTimeout(2000); // let UI settle

  for (const res of resolutions) {
    console.log(`Logs Page - setting viewport to ${res.width}x${res.height}...`);
    await page.setViewportSize({ width: res.width, height: res.height });
    await page.waitForTimeout(1000);
    await page.screenshot({
      path: path.join(artifactDir, `logs_${res.suffix}.png`),
      fullPage: false
    });
  }

  // 4. Capture Expanded Log details at 1440px
  console.log('Expanding log-2 and capturing details...');
  await page.setViewportSize({ width: 1440, height: 900 });
  
  // Click on the second row row (which has log-2)
  const log2Row = page.locator('tr').filter({ hasText: 'Failed login attempt' }).first();
  await log2Row.click();
  
  // Wait for details panel
  await page.waitForSelector('pre', { timeout: 10000 });
  await page.waitForTimeout(1500); // let transition complete
  
  await page.screenshot({
    path: path.join(artifactDir, `logs_expanded_1440.png`),
    fullPage: false
  });

  await browser.close();
  console.log('Done capturing campaigns and logs screenshots!');
}

run().catch(err => {
  console.error('Error during screenshot capture:', err);
  process.exit(1);
});
