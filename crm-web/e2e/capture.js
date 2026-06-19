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
      const urlObj = new URL(url);
      const statusParam = urlObj.searchParams.get('status');
      const segmentParam = urlObj.searchParams.get('segment');
      const sourceParam = urlObj.searchParams.get('source');
      const pageSizeParam = parseInt(urlObj.searchParams.get('pageSize') || '1');

      let totalCount = 0;
      if (statusParam !== null) {
        const s = statusParam.toString();
        totalCount = s === '0' || s === 'Lead' ? 45
                   : s === '1' || s === 'Contacted' ? 28
                   : s === '2' || s === 'Qualified' ? 18
                   : s === '3' || s === 'Negotiating' ? 12
                   : s === '4' || s === 'Customer' ? 35
                   : 0;
      } else if (segmentParam !== null) {
        const seg = segmentParam.toString();
        totalCount = seg === '0' ? 30
                   : seg === '1' ? 42
                   : seg === '2' ? 23
                   : 0;
      } else if (sourceParam !== null) {
        const src = sourceParam.toString();
        totalCount = src === '1' ? 15
                   : src === '4' ? 55
                   : src === '10' ? 20
                   : src === '11' ? 12
                   : 0;
      } else {
        totalCount = 95;
      }

      // Populate mock leads
      const mockLeads = [
        { id: 'lead-1', name: 'Alexandre Queiroz', companyName: 'Diax S.A.', status: 3, segment: 2, leadScore: 92, lastContactAt: new Date(Date.now() - 10 * 24 * 3600000).toISOString(), whatsApp: '5511999999999', phone: '11999999999', email: 'alexandre@diax.com.br', notes: 'Cidade: São Paulo\nInteresse em CRM Premium', tags: 'cidade:sao paulo, plano:anual' },
        { id: 'lead-2', name: 'Juliana Silva', companyName: 'JS Consulting', status: 2, segment: 2, leadScore: 85, lastContactAt: new Date(Date.now() - 3 * 24 * 3600000).toISOString(), whatsApp: '5521988888888', phone: '21988888888', email: 'juliana@jsconsult.com', notes: 'Cidade: Rio de Janeiro\nReunião agendada', tags: 'cidade:rio de janeiro' },
        { id: 'lead-3', name: 'Roberto Santos', companyName: 'Santos Advocacia', status: 1, segment: 1, leadScore: 65, lastContactAt: new Date(Date.now() - 12 * 24 * 3600000).toISOString(), whatsApp: '5531977777777', phone: '31977777777', email: 'roberto@santosadv.com.br', notes: 'Cidade: Belo Horizonte\nAguardando proposta', tags: 'cidade:belo horizonte' },
        { id: 'lead-4', name: 'Mariana Costa', companyName: 'Tech Startup', status: 3, segment: 2, leadScore: 78, lastContactAt: new Date(Date.now() - 8 * 24 * 3600000).toISOString(), whatsApp: '', phone: '', email: 'mariana@tech.io', notes: 'Cidade: Florianópolis\nInteresse em automação n8n', tags: 'cidade:florianopolis' },
        { id: 'lead-5', name: 'Carlos Souza', companyName: 'Inova Digital', status: 0, segment: 1, leadScore: 50, lastContactAt: null, whatsApp: '5511966666666', phone: '11966666666', email: 'carlos@inovadigital.co', notes: 'Cidade: Campinas\nLead frio vindo do scraper', tags: 'cidade:campinas' },
        { id: 'lead-6', name: 'Fernanda Oliveira', companyName: 'Clínica Sorriso', status: 2, segment: 2, leadScore: 88, lastContactAt: new Date(Date.now() - 2 * 24 * 3600000).toISOString(), whatsApp: '5519955555555', phone: '19955555555', email: 'fernanda@clinicasorriso.med.br', notes: 'Cidade: Piracicaba', tags: 'cidade:piracicaba' },
        { id: 'lead-7', name: 'Lucas Pereira', companyName: 'Pizzaria Bella', status: 4, segment: 1, leadScore: 70, lastContactAt: new Date(Date.now() - 1 * 24 * 3600000).toISOString(), whatsApp: '5511944444444', phone: '11944444444', email: 'lucas@pizzeriabella.com', notes: 'Cidade: Guarulhos', tags: 'cidade:guarulhos' }
      ];

      const items = pageSizeParam > 1 ? mockLeads : [];
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ items, totalCount })
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
            { id: 'exp-1', description: 'Google Ads Inbound', categoryName: 'Marketing & Ads', amount: -4500, date: new Date().toISOString(), status: 1 },
            { id: 'exp-2', description: 'AWS Cloud Hosting', categoryName: 'SaaS & Servers', amount: -3200, date: new Date().toISOString(), status: 1 },
            { id: 'exp-3', description: 'Consultoria n8n', categoryName: 'Consultoria', amount: -2500, date: new Date().toISOString(), status: 1 },
            { id: 'exp-4', description: 'Upgrade Mac Mini', categoryName: 'Equipamentos', amount: -1500, date: new Date().toISOString(), status: 1 },
            { id: 'exp-5', description: 'Aluguel Escritório', categoryName: 'Escritório', amount: -750, date: new Date().toISOString(), status: 1 }
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

    if (url.includes('/outreach/dashboard')) {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          whatsAppSentToday: 24,
          whatsAppReadyCount: 5,
          whatsAppSentThisWeek: 120,
          pendingInQueue: 10,
          failedInQueue: 1
        })
      });
    }

    if (url.includes('/whatsapp/status')) {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          isConnected: true,
          instanceName: 'Evolution'
        })
      });
    }

    if (url.includes('/email-campaigns/campaigns')) {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: [
            { id: 'c-1', name: 'Lançamento Plano Anual', sentCount: 1500, deliveredCount: 1480, openCount: 420, clickCount: 95, bounceCount: 5, unsubscribeCount: 2 },
            { id: 'c-2', name: 'Newsletter Tech Junho', sentCount: 3350, deliveredCount: 3300, openCount: 880, clickCount: 180, bounceCount: 20, unsubscribeCount: 10 }
          ]
        })
      });
    }

    if (url.includes('/email-campaigns/queue')) {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: [
            { id: 'q-1', recipientName: 'João Silva', recipientEmail: 'joao@gmail.com', subject: 'Acesse o CRM Premium', status: 0, scheduledAt: new Date(Date.now() + 1800000).toISOString(), attemptCount: 0 },
            { id: 'q-2', recipientName: 'Maria Souza', recipientEmail: 'maria@outlook.com', subject: 'Promoção Plano Anual', status: 0, scheduledAt: new Date(Date.now() + 3600000).toISOString(), attemptCount: 0 }
          ]
        })
      });
    }

    if (url.includes('/email-providers/health')) {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          { provider: 'Amazon SES PJ', dailyLimit: 50000, sentToday: 4850, health: 'ok' },
          { provider: 'Brevo SMTP Backup', dailyLimit: 300, sentToday: 10, health: 'ok' }
        ])
      });
    }

    if (url.includes('/daily-briefings/today')) {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          { id: 'briefing-1', title: 'Briefing Executivo 19/06/2026', createdAt: new Date().toISOString() }
        ])
      });
    }

    if (url.includes('/daily-briefings/briefing-1') || url.includes('/daily-briefings/')) {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: 'briefing-1',
          title: 'Briefing Executivo 19/06/2026',
          content: `
            <h2>TL;DR</h2>
            <p>Hoje o foco principal está em reengajar leads da JS Consulting e Santos Advocacia. As taxas de entrega de e-mail estão ótimas em 98%, mas o WhatsApp apresentou instabilidade momentânea.</p>
            
            <h2>Novidades</h2>
            <p>Novos limites de envio no Amazon SES aumentados para 50k/dia. Integração da Evolution API atualizada para v2.</p>
            
            <h2>Ideias Monetizáveis</h2>
            <p>Oferecer upgrade para o plano anual corporativo com desconto de 20% para a base de leads em estágio de Negociação.</p>
            
            <h2>WhatsApp Copy</h2>
            <p>Olá [Nome], tudo bem? Notei que você se interessou pelo nosso CRM Premium na semana passada, mas não conseguimos avançar. Vamos agendar uma ligação rápida de 10 min amanhã?</p>
            
            <h2>Email Copy</h2>
            <p>Assunto: Acelere suas vendas com o DIAX CRM Premium\\n\\nOlá [Nome],\\n\\nSabemos que a gestão comercial é o coração do seu negócio. O DIAX CRM automatiza prospecção, disparos de e-mail e integração de WhatsApp.\\n\\nClique aqui para ativar seu teste gratuito.</p>
          `
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

    if (url.includes('/planner/simulation') || url.includes('/planner/simulations')) {
      const dailyBalances = [];
      let balance = 42000;
      for (let day = 1; day <= 30; day++) {
        const dayIncome = day === 5 ? 12000 : day === 20 ? 16500 : 0;
        const dayExpense = day === 10 ? -4500 : day === 15 ? -3200 : day === 25 ? -4750 : 0;
        const closingBalance = balance + dayIncome + dayExpense;
        dailyBalances.push({
          date: `2026-06-${day.toString().padStart(2, '0')}T12:00:00.000Z`,
          openingBalance: balance,
          totalIncome: dayIncome,
          totalExpenses: Math.abs(dayExpense),
          closingBalance,
          isNegative: closingBalance < 0,
          riskLevel: closingBalance < 5000 ? 'Critical' : closingBalance < 15000 ? 'Warning' : 'Safe'
        });
        balance = closingBalance;
      }

      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: 'sim-1',
          userId: 'user-1',
          referenceMonth: 6,
          referenceYear: 2026,
          simulationDate: new Date().toISOString(),
          startingBalance: 42000,
          projectedEndingBalance: balance,
          totalProjectedIncome: 28500,
          totalProjectedExpenses: 12450,
          hasNegativeBalanceRisk: false,
          lowestProjectedBalance: 37500,
          status: 1,
          dailyBalances,
          recommendations: [
            { type: 0, priority: 1, title: 'Conter gastos', message: 'Sua projeção de caixa está segura, mas reforce a contenção.' }
          ]
        })
      });
    }

    if (url.includes('/credit-cards')) {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          { id: 'cc-1', name: 'Mastercard Platinum PJ', limit: 30000, closingDay: 10, dueDay: 17, cardBrand: 'Mastercard', usedLimit: 4500 },
          { id: 'cc-2', name: 'Visa Infinite Pessoal', limit: 50000, closingDay: 25, dueDay: 2, cardBrand: 'Visa', usedLimit: 1250 }
        ])
      });
    }

    if (url.includes('/financial-accounts')) {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          { id: 'acc-1', name: 'Itaú Uniclass PJ', balance: 35439.20, bankName: 'Itaú', accountType: 1 },
          { id: 'acc-2', name: 'Inter PJ Investimentos', balance: 12560.80, bankName: 'Banco Inter', accountType: 4 },
          { id: 'acc-3', name: 'Caixa Reserva', balance: 10000.00, bankName: 'Caixa', accountType: 2 }
        ])
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

    if (url.includes('/tasks')) {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          { id: 'task-1', title: 'Enviar proposta comercial para Diax S.A.', priority: 'Urgent', status: 'Todo' },
          { id: 'task-2', title: 'Reunião de Alinhamento Evolution API', priority: 'High', status: 'Todo' },
          { id: 'task-3', title: 'Ajustar limites de SMTP no provedor', priority: 'Medium', status: 'Todo' }
        ])
      });
    }

    if (url.includes('/Checklists/items')) {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: [
            { id: 'chk-1', title: 'Homologar webhook do n8n', priority: 3, categoryName: 'Comercial' },
            { id: 'chk-2', title: 'Conciliar extrato bancário PJ', priority: 2, categoryName: 'Financeiro' },
            { id: 'chk-3', title: 'Revisar copies no Claude Chat', priority: 1, categoryName: 'Marketing' }
          ]
        })
      });
    }

    if (url.includes('/error-logs/aggregate/stats')) {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ totalToday: 5, criticalToday: 1, unresolvedTotal: 2 })
      });
    }

    if (url.includes('/error-logs')) {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: [
            { id: 'err-1', appName: 'n8n Workflow Service', message: 'SMTP connection timeout on brevo-smtp-4', level: 'Critical', occurredAt: new Date().toISOString() },
            { id: 'err-2', appName: 'Evolution API', message: 'WhatsApp connection closed (1006)', level: 'Warning', occurredAt: new Date(Date.now() - 1800000).toISOString() }
          ]
        })
      });
    }

    if (url.includes('/logs/stats')) {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ totalToday: 150, infoToday: 140, warnToday: 8, errorToday: 2 })
      });
    }

    if (url.includes('/logs')) {
      return route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          items: [
            { id: 'log-1', message: 'Campaign wave triggered successfully', level: 'Info', occurredAt: new Date().toISOString() },
            { id: 'log-2', message: 'Evolution API heartbeat OK', level: 'Info', occurredAt: new Date(Date.now() - 600000).toISOString() }
          ]
        })
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

  // Multi-tab QA screenshots
  const qaTabs = [
    { label: 'Visão Geral', fileKey: 'overview' },
    { label: 'CRM & Pipeline', fileKey: 'crm' },
    { label: 'Outreach & Marketing', fileKey: 'marketing' },
    { label: 'Financeiro', fileKey: 'finance' },
    { label: 'Intelligence', fileKey: 'intelligence' },
    { label: 'Monitoramento & Ops', fileKey: 'ops' }
  ];

  const qaViewports = [
    { suffix: '1440', width: 1440, height: 900 },
    { suffix: '390', width: 390, height: 844 }
  ];

  for (const qavp of qaViewports) {
    console.log(`--- Multi-tab Visual QA at ${qavp.width}px ---`);
    await page.setViewportSize({ width: qavp.width, height: qavp.height });
    await page.waitForTimeout(1500);

    for (const tab of qaTabs) {
      console.log(`Clicking tab "${tab.label}" and capturing dashboard_${tab.fileKey}_${qavp.suffix}.png...`);
      try {
        await page.getByRole('tab', { name: tab.label }).click();
        await page.waitForTimeout(1500); // Wait for transition animation and ApexCharts redraw
        await page.screenshot({
          path: path.join(artifactDir, `dashboard_${tab.fileKey}_${qavp.suffix}.png`),
          fullPage: false
        });
      } catch (err) {
        console.error(`Failed to capture tab ${tab.label}:`, err.message);
      }
    }
  }

  await browser.close();
  console.log('Done capturing screenshots!');
}

run().catch(err => {
  console.error('Error during screenshot capture:', err);
  process.exit(1);
});
