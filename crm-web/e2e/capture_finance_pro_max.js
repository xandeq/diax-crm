const { chromium } = require('@playwright/test');
const path = require('path');

const artifactDir = 'C:\\Users\\acq20\\.gemini\\antigravity\\brain\\f301ad39-9990-4c18-aef5-dfc324cb01a8';
const mockToken = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJlbWFpbCI6Im1vY2t1c2VyQGRpYXguY29tIiwicm9sZSI6IkFkbWluIn0.dummy-signature';

const mockAccounts = [
  { id: "acc-1", name: "Itaú Prime", balance: 15420.50, accountType: 1, isActive: true },
  { id: "acc-2", name: "XP Investimentos", balance: 85000.00, accountType: 5, isActive: true },
  { id: "acc-3", name: "Nubank", balance: 3400.12, accountType: 6, isActive: true },
  { id: "acc-4", name: "Carteira Digital", balance: 150.00, accountType: 6, isActive: false }
];

const mockCreditCards = [
  { id: "cc-1", name: "Visa Infinite Itaú", lastFourDigits: "1234", closingDay: 5, dueDay: 12, limit: 50000, brand: 1, cardKind: 1, isActive: true },
  { id: "cc-2", name: "Mastercard Black Nubank", lastFourDigits: "5678", closingDay: 10, dueDay: 17, limit: 25000, brand: 2, cardKind: 2, isActive: true },
  { id: "cc-3", name: "Elo Nanquim Bradesco", lastFourDigits: "9012", closingDay: 15, dueDay: 22, limit: 30000, brand: 3, cardKind: 1, isActive: false }
];

const mockInvoices = [
  { id: "inv-1", creditCardId: "cc-1", creditCardGroupName: "Visa Infinite Itaú", dueDate: "2026-06-12", closingDate: "2026-06-05", isPaid: false, totalAmount: 12450.50, referenceMonth: 6, referenceYear: 2026 },
  { id: "inv-2", creditCardId: "cc-1", creditCardGroupName: "Visa Infinite Itaú", dueDate: "2026-05-12", closingDate: "2026-05-05", isPaid: true, totalAmount: 8450.00, referenceMonth: 5, referenceYear: 2026 }
];

const mockTransactions = {
  items: [
    { id: "t-1", date: "2026-06-02T10:00:00Z", description: "AWS Cloud Services", amount: 1420.50, categoryName: "Tecnologia", type: 2, status: 2 },
    { id: "t-2", date: "2026-06-03T15:30:00Z", description: "Uber Trip", amount: 45.20, categoryName: "Transporte", type: 2, status: 2 },
    { id: "t-3", date: "2026-06-04T12:00:00Z", description: "Restaurante Andru", amount: 320.00, categoryName: "Alimentação", type: 2, status: 2 }
  ],
  page: 1,
  pageSize: 100,
  totalCount: 3,
  totalPages: 1
};

const mockTransfers = [
  { id: "tr-1", description: "Reserva Financeira", fromAccountId: "acc-1", fromAccountName: "Itaú Prime", toAccountId: "acc-2", toAccountName: "XP Investimentos", amount: 15000.00, date: "2026-06-10" },
  { id: "tr-2", description: "Ajuste Saldo Corrente", fromAccountId: "acc-3", fromAccountName: "Nubank", toAccountId: "acc-1", toAccountName: "Itaú Prime", amount: 2300.00, date: "2026-06-12" }
];

const mockMorningBriefing = {
  generatedAt: "2026-06-16T09:00:00Z",
  period: { month: 6, year: 2026 },
  summary: {
    totalIncome: 25000.00,
    totalPaid: 12450.00,
    totalPending: 6000.00,
    availableToInvest: 6550.00
  },
  alerts: {
    hasUrgentItems: true,
    overdueCount: 2,
    overdueAmount: 4200.00,
    overdue: [
      { id: "ov-1", description: "Hospedagem HostGator", amount: 1200.00, daysOverdue: 5 },
      { id: "ov-2", description: "Aluguel Comercial", amount: 3000.00, daysOverdue: 2 }
    ],
    dueTodayCount: 1,
    dueTodayAmount: 1800.00,
    dueToday: [
      { id: "dt-1", description: "Energia Elétrica Light", amount: 1800.00 }
    ],
    dueThisWeekCount: 2,
    dueThisWeekAmount: 3400.00,
    dueThisWeek: [
      { id: "dw-1", description: "Plataforma de E-mail Marketing", amount: 1400.00, date: "2026-06-19" },
      { id: "dw-2", description: "Limpeza Escritório", amount: 2000.00, date: "2026-06-20" }
    ],
    pendingSubscriptionsCount: 1,
    pendingSubscriptions: [
      { transactionId: "sub-1", description: "Assinatura Figma Team", amount: 280.00, paymentType: "credit" }
    ]
  }
};

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

    if (url.includes('/morning-briefing')) {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(mockMorningBriefing)
      });
    }

    if (url.includes('/financial-accounts')) {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(mockAccounts)
      });
    }

    if (url.includes('/credit-cards/cc-1/invoices')) {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(mockInvoices)
      });
    }

    if (url.includes('/credit-cards/cc-1')) {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(mockCreditCards[0])
      });
    }

    if (url.includes('/credit-cards')) {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(mockCreditCards)
      });
    }

    if (url.includes('/account-transfers')) {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(mockTransfers)
      });
    }

    if (url.includes('/transactions')) {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(mockTransactions)
      });
    }

    return route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({})
    });
  });

  // Navigate to login
  console.log('Navigating to login...');
  await page.goto('http://localhost:3000/login/');
  await page.waitForLoadState('networkidle');

  console.log('Logging in...');
  await page.locator('#email').fill('mockuser@diax.com');
  await page.getByLabel('Senha').fill('anypassword123');
  await page.getByRole('button', { name: 'Entrar' }).click();

  // Wait for redirect
  console.log('Waiting for redirect...');
  await page.waitForSelector('.dsh', { timeout: 30000 });

  const resolutions = [
    { width: 1440, height: 900, suffix: '1440' },
    { width: 1280, height: 800, suffix: '1280' },
    { width: 768, height: 1024, suffix: '768' },
    { width: 390, height: 844, suffix: '390' }
  ];

  const routes = [
    { path: '/finance/accounts', name: 'accounts' },
    { path: '/finance/credit-cards', name: 'credit_cards' },
    { path: '/finance/credit-cards/details?id=cc-1', name: 'credit_cards_details' },
    { path: '/finance/transfers', name: 'transfers' },
    { path: '/finance/morning-briefing', name: 'morning_briefing' }
  ];

  for (const route of routes) {
    console.log(`Navigating to ${route.path}...`);
    await page.goto(`http://localhost:3000${route.path}`);
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000); // UI settle

    for (const res of resolutions) {
      console.log(`Capturing ${route.name} at ${res.width}x${res.height}...`);
      await page.setViewportSize({ width: res.width, height: res.height });
      await page.waitForTimeout(1000);
      await page.screenshot({
        path: path.join(artifactDir, `finance_${route.name}_${res.suffix}.png`),
        fullPage: false
      });
    }
  }

  await browser.close();
  console.log('All screenshots captured successfully!');
}

run().catch(err => {
  console.error('Error:', err);
  process.exit(1);
});
