const { chromium } = require('@playwright/test');
const path = require('path');

const artifactDir = 'C:\\Users\\acq20\\.gemini\\antigravity\\brain\\f301ad39-9990-4c18-aef5-dfc324cb01a8';
const mockToken = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJlbWFpbCI6Im1vY2t1c2VyQGRpYXguY29tIiwicm9sZSI6IkFkbWluIn0.dummy-signature';

const mockLeads = {
  items: [
    {
      id: "l-1",
      name: "Ana Silva",
      email: "ana.silva@empresa.com.br",
      phone: "11988887777",
      whatsApp: "11988887777",
      companyName: "Silva & Associados",
      segment: 2,
      source: 2,
      status: 0,
      createdAt: "2026-06-01T10:00:00Z",
      lastContactAt: "2026-06-13T10:00:00Z",
      emailOptOut: false,
      whatsAppOptOut: false
    },
    {
      id: "l-2",
      name: "Bruno Souza",
      email: "bruno.souza@startup.com",
      phone: "21977776666",
      whatsApp: "",
      companyName: "TechStart Inc",
      segment: 1,
      source: 4,
      status: 1,
      createdAt: "2026-05-15T14:30:00Z",
      lastEmailSentAt: "2026-06-10T11:00:00Z",
      emailOptOut: false,
      whatsAppOptOut: false
    },
    {
      id: "l-3",
      name: "Carlos Oliveira",
      email: "carlos@industria.com.br",
      phone: "31966665555",
      whatsApp: "31966665555",
      companyName: "Metalúrgica Mineira",
      segment: 0,
      source: 10,
      status: 2,
      createdAt: "2026-04-20T09:15:00Z",
      emailOptOut: false,
      whatsAppOptOut: false
    }
  ],
  totalCount: 3,
  totalPages: 1
};

const mockCustomers = {
  items: [
    {
      id: "c-1",
      name: "Daniela Santos",
      email: "daniela@santoslog.com.br",
      phone: "19955554444",
      whatsApp: "19955554444",
      companyName: "Santos Logística",
      segment: 2,
      source: 3,
      status: 4,
      createdAt: "2026-01-10T08:00:00Z",
      lastContactAt: "2026-06-14T09:00:00Z",
      emailOptOut: false,
      whatsAppOptOut: false
    },
    {
      id: "c-2",
      name: "Eduardo Lima",
      email: "eduardo@limaconsulting.com",
      phone: "81944443333",
      whatsApp: "",
      companyName: "Lima Consulting",
      segment: 1,
      source: 6,
      status: 5,
      createdAt: "2025-11-20T16:00:00Z",
      lastEmailSentAt: "2026-05-12T10:00:00Z",
      emailOptOut: true,
      whatsAppOptOut: false
    }
  ],
  totalCount: 2,
  totalPages: 1
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

    if (url.includes('/leads')) {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(mockLeads)
      });
    }

    if (url.includes('/customers')) {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(mockCustomers)
      });
    }

    // Default mock response for other APIs to ensure 200 OK
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

  // 1. Capture Leads Page
  console.log('Navigating to Leads Page...');
  await page.goto('http://localhost:3000/leads/');
  await page.waitForSelector('.responsive-table', { timeout: 15000 });
  await page.waitForTimeout(2000); // let UI settle

  for (const res of resolutions) {
    console.log(`Leads Page - setting viewport to ${res.width}x${res.height}...`);
    await page.setViewportSize({ width: res.width, height: res.height });
    await page.waitForTimeout(1000);
    await page.screenshot({
      path: path.join(artifactDir, `leads_${res.suffix}.png`),
      fullPage: false
    });
  }

  // 2. Capture Customers Page
  console.log('Navigating to Customers Page...');
  await page.goto('http://localhost:3000/customers/');
  await page.waitForSelector('.responsive-table', { timeout: 15000 });
  await page.waitForTimeout(2000); // let UI settle

  for (const res of resolutions) {
    console.log(`Customers Page - setting viewport to ${res.width}x${res.height}...`);
    await page.setViewportSize({ width: res.width, height: res.height });
    await page.waitForTimeout(1000);
    await page.screenshot({
      path: path.join(artifactDir, `customers_${res.suffix}.png`),
      fullPage: false
    });
  }

  await browser.close();
  console.log('Done capturing table screenshots!');
}

run().catch(err => {
  console.error('Error during screenshot capture:', err);
  process.exit(1);
});
