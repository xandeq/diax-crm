const { chromium } = require('@playwright/test');
const path = require('path');

const artifactDir = 'C:\\Users\\acq20\\.gemini\\antigravity\\brain\\f301ad39-9990-4c18-aef5-dfc324cb01a8';
const mockToken = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJlbWFpbCI6Im1vY2t1c2VyQGRpYXguY29tIiwicm9sZSI6IkFkbWluIn0.dummy-signature';

const mockSummary = {
  totalIncome: 25000,
  totalExpenses: 18450,
  totalPaidExpenses: 12450,
  totalPendingExpenses: 6000,
  netCashFlow: 6550,
  projectedCashFlow: 8550,
  pendingCash: 2000
};

const mockRecurring = [
  {
    id: "rec-1",
    description: "Mensalidade DIAX Pro",
    amount: 250,
    dayOfMonth: 5,
    type: 2, // Expense
    itemKind: 2, // Subscription
    isActive: true,
    hasVariableAmount: false
  },
  {
    id: "rec-2",
    description: "Faturamento Consultoria Andru",
    amount: 8500,
    dayOfMonth: 10,
    type: 1, // Income
    itemKind: 1, // Standard
    isActive: true,
    hasVariableAmount: false
  },
  {
    id: "rec-3",
    description: "Aluguel Escritório",
    amount: 3200,
    dayOfMonth: 15,
    type: 2, // Expense
    itemKind: 1, // Standard
    isActive: true,
    hasVariableAmount: false
  }
];

const mockTransactions = {
  items: [
    {
      id: "t-1",
      type: 1, // Income
      date: "2026-06-14T10:00:00Z",
      description: "Faturamento Contrato Andru",
      categoryName: "Consultoria",
      financialAccountName: "Itaú Prime",
      amount: 8500,
      status: 2 // Paid
    },
    {
      id: "t-2",
      type: 2, // Expense
      date: "2026-06-14T12:30:00Z",
      description: "Mensalidade Vercel Pro",
      categoryName: "Serviços Cloud",
      creditCardName: "Visa Infinite",
      amount: 250,
      status: 2 // Paid
    },
    {
      id: "t-3",
      type: 2, // Expense
      date: "2026-06-15T09:00:00Z",
      description: "Aluguel Escritório",
      categoryName: "Infraestrutura",
      financialAccountName: "Itaú Prime",
      amount: 3200,
      status: 1 // Pending
    },
    {
      id: "t-4",
      type: 3, // Transfer
      date: "2026-06-13T15:00:00Z",
      description: "Transferência para poupança",
      categoryName: "Investimentos",
      financialAccountName: "XP Investimentos",
      amount: 1500,
      status: 2 // Paid
    }
  ],
  page: 1,
  pageSize: 15,
  totalCount: 4,
  totalPages: 1
};

const mockCategories = [
  { id: "cat-1", name: "Consultoria" },
  { id: "cat-2", name: "Serviços Cloud" },
  { id: "cat-3", name: "Infraestrutura" },
  { id: "cat-4", name: "Investimentos" }
];

const mockAccounts = [
  { id: "acc-1", name: "Itaú Prime" },
  { id: "acc-2", name: "XP Investimentos" }
];

async function run() {
  console.log('Starting Playwright...');
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext();
  const page = await context.newPage();

  page.on('console', msg => console.log('PAGE LOG:', msg.text()));
  page.on('pageerror', err => console.log('PAGE ERROR:', err.message));

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

    if (url.includes('/finance/summary')) {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(mockSummary)
      });
    }

    if (url.includes('/planner/recurring')) {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(mockRecurring)
      });
    }

    if (url.includes('/transactions')) {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(mockTransactions)
      });
    }

    if (url.includes('/transaction-categories')) {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(mockCategories)
      });
    }

    if (url.includes('/financial-accounts')) {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(mockAccounts)
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

  // 1. Capture Finance Dashboard Page
  console.log('Navigating to Finance Dashboard Page...');
  await page.goto('http://localhost:3000/finance/');
  await page.waitForSelector('.min-h-screen', { timeout: 15000 });
  await page.waitForTimeout(2000); // let UI settle

  for (const res of resolutions) {
    console.log(`Finance Dashboard - setting viewport to ${res.width}x${res.height}...`);
    await page.setViewportSize({ width: res.width, height: res.height });
    await page.waitForTimeout(1000);
    await page.screenshot({
      path: path.join(artifactDir, `finance_dashboard_${res.suffix}.png`),
      fullPage: false
    });
  }

  // 2. Capture Transactions Page
  console.log('Navigating to Transactions Page...');
  await page.goto('http://localhost:3000/finance/transactions/');
  await page.waitForSelector('.responsive-table', { timeout: 15000 });
  await page.waitForTimeout(2000); // let UI settle

  for (const res of resolutions) {
    console.log(`Transactions Page - setting viewport to ${res.width}x${res.height}...`);
    await page.setViewportSize({ width: res.width, height: res.height });
    await page.waitForTimeout(1000);
    await page.screenshot({
      path: path.join(artifactDir, `finance_transactions_${res.suffix}.png`),
      fullPage: false
    });
  }

  await browser.close();
  console.log('Done capturing finance screenshots!');
}

run().catch(err => {
  console.error('Error during screenshot capture:', err);
  process.exit(1);
});
