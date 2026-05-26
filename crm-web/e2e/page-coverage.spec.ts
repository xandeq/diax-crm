import { expect, test } from '@playwright/test';

const email = process.env.PLAYWRIGHT_LOGIN_EMAIL;
const password = process.env.PLAYWRIGHT_LOGIN_PASSWORD;

async function login(page: any) {
  await page.goto('/login/');
  await page.getByLabel('Senha').fill(password!);
  await page.locator('#email').fill(email!);
  await page.getByRole('button', { name: 'Entrar' }).click();
  await page.waitForURL('**/dashboard/**', { timeout: 15000 });
}

const ALL_PAGES: Array<{ route: string; label: string; expectText?: string }> = [
  // Principal
  { route: '/dashboard/', label: 'Dashboard', expectText: 'Dashboard' },

  // CRM
  { route: '/customers/', label: 'Clientes' },
  { route: '/leads/', label: 'Leads' },
  { route: '/leads/import/', label: 'Importar Leads' },
  { route: '/helpdesk/', label: 'Helpdesk' },

  // Marketing
  { route: '/outreach/', label: 'Outreach' },
  { route: '/email-marketing/', label: 'Email Marketing' },
  { route: '/email-marketing/pro/', label: 'Email Marketing PRO' },
  { route: '/campanhas/', label: 'Campanhas' },
  { route: '/ads/', label: 'Meta Ads' },
  { route: '/analytics/', label: 'Analytics' },

  // Finanças
  { route: '/finance/morning-briefing/', label: 'Morning Briefing' },
  { route: '/finance/', label: 'Dashboard Financeiro' },
  { route: '/finance/personal-control/', label: 'Planilha Financeira' },
  { route: '/finance/transactions/', label: 'Transações' },
  { route: '/finance/incomes/', label: 'Receitas' },
  { route: '/finance/expenses/', label: 'Despesas' },
  { route: '/finance/transfers/', label: 'Transferências' },
  { route: '/finance/imports/', label: 'Importar OFX/CSV' },
  { route: '/finance/credit-cards/', label: 'Cartões de Crédito' },
  { route: '/finance/accounts/', label: 'Contas' },
  { route: '/finance/planner/', label: 'Planejador Financeiro' },
  { route: '/finance/planner/goals/', label: 'Metas Financeiras' },
  { route: '/finance/planner/recurring/', label: 'Recorrentes' },
  { route: '/finance/tax-documents/', label: 'Imposto de Renda' },

  // IA
  { route: '/ai-chat/', label: 'Claude Chat' },
  { route: '/utilities/image-generation/', label: 'Geração de Imagens' },
  { route: '/utilities/prompt-generator/', label: 'Gerador de Prompts' },
  { route: '/utilities/humanize-text/', label: 'Humanizar Texto' },
  { route: '/utilities/email-subject-optimizer/', label: 'Otimizador de Email' },
  { route: '/utilities/lead-persona-generator/', label: 'Gerador de Personas' },
  { route: '/utilities/outreach-ab-test/', label: 'Teste A/B Outreach' },
  { route: '/utilities/social-batch-generator/', label: 'Batch Social Media' },
  { route: '/utilities/customer-insights/', label: 'Insights de Clientes' },

  // Pessoal
  { route: '/agenda/', label: 'Agenda' },
  { route: '/tasks/', label: 'Tarefas' },
  { route: '/household/checklists/', label: 'Listas e Compras' },
  { route: '/utilities/snippets/', label: 'Snippets' },
  { route: '/tools/html-extractor/', label: 'Extrator HTML → Texto' },
  { route: '/tools/html-url-extractor/', label: 'Extrator HTML → Links' },
  { route: '/tools/apps-inventory/', label: 'Inventário de Apps' },

  // Admin
  { route: '/users/', label: 'Usuários' },
  { route: '/admin/groups/', label: 'Grupos & Permissões' },
  { route: '/admin/ai/', label: 'Provedores IA' },
  { route: '/admin/blog/', label: 'Blog' },
  { route: '/logs/', label: 'Logs do Sistema' },
];

test.describe('Page Coverage — todas as páginas do sidebar', () => {
  test.skip(!email || !password, 'PLAYWRIGHT_LOGIN_EMAIL e PLAYWRIGHT_LOGIN_PASSWORD são obrigatórios.');

  let loggedIn = false;

  test.beforeEach(async ({ page }) => {
    if (!loggedIn) {
      await login(page);
      loggedIn = true;
    }
  });

  for (const { route, label, expectText } of ALL_PAGES) {
    test(`${label} — ${route}`, async ({ page }) => {
      await login(page);
      await page.goto(route);

      // Sem 404 ou crash
      await expect(page.locator('body')).not.toContainText('404');
      await expect(page.locator('body')).not.toContainText('Application error');
      await expect(page.locator('body')).not.toContainText('Internal Server Error');

      // Se fornecido texto específico, verifica
      if (expectText) {
        await expect(page.locator('body')).toContainText(expectText);
      }

      // Página carregou (tem <body> com conteúdo mínimo)
      const bodyText = await page.locator('body').innerText();
      expect(bodyText.length).toBeGreaterThan(50);
    });
  }
});
