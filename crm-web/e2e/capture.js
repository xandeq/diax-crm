const { chromium } = require('@playwright/test');
const path = require('path');

const artifactDir = 'C:\\Users\\acq20\\.gemini\\antigravity\\brain\\f301ad39-9990-4c18-aef5-dfc324cb01a8';
const mockToken = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJlbWFpbCI6Im1vY2t1c2VyQGRpYXguY29tIiwicm9sZSI6IkFkbWluIn0.dummy-signature';

async function run() {
  console.log('Starting Playwright...');
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext();
  const page = await context.newPage();

  page.on('console', msg => console.log('PAGE LOG:', msg.text()));
  page.on('pageerror', err => console.log('PAGE ERROR:', err.message));

  // Intercept all API calls to local/remote API endpoints and provide premium mock data
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
      // Return realistic mock lead statistics for the dashboard funnel
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: [],
          totalCount: url.includes('status=Lead') ? 45 
                     : url.includes('status=Contacted') ? 28 
                     : url.includes('status=Qualified') ? 18 
                     : url.includes('status=Negotiating') ? 12 
                     : url.includes('status=Customer') ? 35 
                     : 0
        })
      });
    }

    if (url.includes('/personal-control/months')) {
      // Mock data for current/previous month and 6-month history trend
      // Return positive trend for the current month
      const isPrevMonth = url.includes('/2026/5') || url.includes('/2026/05');
      const totalIncome = isPrevMonth ? 22000 : 28500;
      const totalExpenses = isPrevMonth ? 14000 : 12450;
      
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          summary: {
            totalIncome,
            totalExpenses,
            remainingBalance: totalIncome - totalExpenses,
            availableToInvest: (totalIncome - totalExpenses) * 0.7,
            paidAmount: totalExpenses * 0.8,
            unpaidAmount: totalExpenses * 0.2,
            paidCount: 12,
            unpaidCount: 3,
            withCardAmount: totalExpenses * 0.4,
            withoutCardAmount: totalExpenses * 0.6,
            totalCardPaid: totalExpenses * 0.3,
            totalCardPending: totalExpenses * 0.1,
            cardsPaidCount: 5,
            cardsPendingCount: 2,
            totalToPay: totalExpenses * 0.2
          },
          expenses: [
            { categoryName: 'Marketing & Ads', amount: -4500 },
            { categoryName: 'SaaS & Servers', amount: -3200 },
            { categoryName: 'Consultoria', amount: -2500 },
            { categoryName: 'Equipamentos', amount: -1500 },
            { categoryName: 'Escritório', amount: -750 }
          ]
        })
      });
    }

    if (url.includes('/email-campaigns/analytics')) {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          overallStats: {
            totalCampaigns: 8,
            totalEmailsSent: 4850,
            openRate: 28.4,
            clickRate: 6.2
          }
        })
      });
    }

    if (url.includes('/appointments')) {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          { id: '1', startTime: new Date(Date.now() + 3600000).toISOString(), title: 'Demo Técnica DIAX CRM' },
          { id: '2', startTime: new Date(Date.now() + 14400000).toISOString(), title: 'Reunião de Vendas - Lead Qualificado' },
          { id: '3', startTime: new Date(Date.now() + 25200000).toISOString(), title: 'Planejamento Financeiro Q3' }
        ])
      });
    }

    if (url.includes('/planner/goals') || url.includes('/financial-goals') || url.includes('/goals')) {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          { id: '1', name: 'Reserva de Emergência', targetAmount: 50000, currentAmount: 42000, category: 'Emergency', deadline: '2026-12-31' },
          { id: '2', name: 'Investimento Anúncios', targetAmount: 15000, currentAmount: 15000, category: 'Investment', deadline: '2026-07-01' },
          { id: '3', name: 'Novos Notebooks', targetAmount: 24000, currentAmount: 8000, category: 'Equipment', deadline: '2026-09-30' }
        ])
      });
    }

    if (url.includes('/planner/simulation')) {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          months: [
            { monthName: 'Junho', startBalance: 42000, income: 28500, expenses: 12450, endBalance: 58050 },
            { monthName: 'Julho', startBalance: 58050, income: 29000, expenses: 13000, endBalance: 74050 },
            { monthName: 'Agosto', startBalance: 74050, income: 31000, expenses: 13000, endBalance: 92050 }
          ]
        })
      });
    }

    if (url.includes('/personal-control') || url.includes('/investiq') || url.includes('/portfolio')) {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          totalValue: 184500.50,
          yieldMoM: 1.84,
          assets: [
            { symbol: 'CDI 100%', type: 'Renda Fixa', value: 120000, yield: 0.95 },
            { symbol: 'Fundos Imobiliários', type: 'FIIs', value: 45000, yield: 0.82 },
            { symbol: 'Ações de Tecnologia', type: 'Renda Variável', value: 19500, yield: 2.15 }
          ]
        })
      });
    }

    if (url.includes('/ads/summary')) {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          accountName: 'DIAX CRM Main Ads',
          spend: 3450.20,
          impressions: 128400,
          clicks: 3420,
          cpc: 1.01,
          ctr: 2.66
        })
      });
    }

    if (url.includes('/ads/insights')) {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          { campaignName: 'Institucional Leads Frio', spend: 1500, impressions: 50000, clicks: 1200, ctr: 2.4, cpc: 1.25 },
          { campaignName: 'Remarketing SaaS Plano Anual', spend: 1950.20, impressions: 78400, clicks: 2220, ctr: 2.83, cpc: 0.88 }
        ])
      });
    }

    // Default mock response for other APIs to ensure 200 OK and no 401s
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

  // Wait for redirect to dashboard and render
  console.log('Waiting for dashboard to load...');
  await page.waitForSelector('.dsh', { timeout: 30000 });
  await page.waitForTimeout(5000); // wait for animations and charts to render fully

  const viewports = [
    { name: 'dashboard_1440.png', width: 1440, height: 900 },
    { name: 'dashboard_1280.png', width: 1280, height: 800 },
    { name: 'dashboard_768.png', width: 768, height: 1024 },
    { name: 'dashboard_390.png', width: 390, height: 844 },
  ];

  for (const vp of viewports) {
    console.log(`Setting viewport to ${vp.width}x${vp.height} and capturing ${vp.name}...`);
    await page.setViewportSize({ width: vp.width, height: vp.height });
    await page.waitForTimeout(2000); // let layout adjust and render
    await page.screenshot({
      path: path.join(artifactDir, vp.name),
      fullPage: false
    });
  }

  await browser.close();
  console.log('Done capturing screenshots!');
}

run().catch(err => {
  console.error('Error during screenshot capture:', err);
  process.exit(1);
});
