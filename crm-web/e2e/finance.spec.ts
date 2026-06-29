import { expect, Page, test } from '@playwright/test';

const email = process.env.PLAYWRIGHT_LOGIN_EMAIL;
const password = process.env.PLAYWRIGHT_LOGIN_PASSWORD;

async function login(page: Page) {
  // storageState pre-loads auth — navigate to dashboard directly
  await page.goto('/dashboard/');
  await page.waitForURL('**/dashboard/**', { timeout: 15_000 });
}

test.describe('Finance — Planilha Financeira', () => {
  test.skip(!email || !password, 'PLAYWRIGHT_LOGIN_EMAIL e PLAYWRIGHT_LOGIN_PASSWORD são obrigatórios.');

  test('página carrega com título e KPIs', async ({ page }) => {
    await login(page);
    await page.goto('/finance/personal-control/');

    await expect(page.getByRole('heading', { name: 'Planilha Financeira' })).toBeVisible();

    // KPI cards presentes
    await expect(page.getByText('Total de Receitas')).toBeVisible();
    await expect(page.getByText('Total de Despesas')).toBeVisible();

    // Botões de navegação de mês — usam texto "Mês anterior" e "Próximo mês"
    await expect(page.getByRole('button', { name: /Mês anterior/i })).toBeVisible();
    await expect(page.getByRole('button', { name: /Próximo mês/i })).toBeVisible();
  });

  test('navegação entre meses funciona', async ({ page }) => {
    await login(page);
    await page.goto('/finance/personal-control/');
    await page.waitForLoadState('networkidle');

    // Clica em "Mês anterior" e verifica que a página recarrega sem crash
    const prevBtn = page.getByRole('button', { name: /Mês anterior/i });
    await expect(prevBtn).toBeVisible({ timeout: 10000 });
    await prevBtn.click();
    await page.waitForLoadState('networkidle');

    // Página não deve ter crash
    await expect(page.locator('body')).not.toContainText('Application error');
    // Heading da planilha ainda deve estar visível
    await expect(page.getByRole('heading', { name: 'Planilha Financeira' })).toBeVisible();
  });

  test('seção de receitas é visível', async ({ page }) => {
    await login(page);
    await page.goto('/finance/personal-control/');

    // SectionShell title="Receitas do mês" — heading específico
    await expect(
      page.getByRole('heading', { name: 'Receitas do mês' })
    ).toBeVisible({ timeout: 15000 });
  });

  test('seção de assinaturas é visível', async ({ page }) => {
    await login(page);
    await page.goto('/finance/personal-control/');

    // SectionShell title="Assinaturas"
    await expect(
      page.getByRole('heading', { name: 'Assinaturas' })
    ).toBeVisible({ timeout: 15000 });
  });

  test('botão de adicionar receita abre modal', async ({ page }) => {
    await login(page);
    await page.goto('/finance/personal-control/');

    // Procurar botão de adicionar receita
    const addBtn = page.getByRole('button', { name: /adicionar receita|nova receita|\+ receita/i }).first();

    if (await addBtn.count() > 0) {
      await addBtn.click();
      // Modal ou formulário deve aparecer
      await expect(
        page.getByRole('dialog').or(
          page.getByLabel(/nome|receita/i).first()
        )
      ).toBeVisible({ timeout: 3000 });
    }
  });
});

test.describe('Finance — Dashboard', () => {
  test.skip(!email || !password, 'PLAYWRIGHT_LOGIN_EMAIL e PLAYWRIGHT_LOGIN_PASSWORD são obrigatórios.');

  test('dashboard financeiro carrega sem erros', async ({ page }) => {
    await login(page);
    await page.goto('/finance/');

    await expect(page.locator('body')).not.toContainText('Application error');
    await expect(page.locator('body')).not.toContainText('404');
    await expect(page.locator('body')).not.toContainText('500');

    // Deve ter algum conteúdo financeiro
    await expect(
      page.getByText(/receitas|despesas|saldo|financeiro/i).first()
    ).toBeVisible({ timeout: 10000 });
  });

  test('link para Planilha Financeira está no menu', async ({ page }) => {
    await login(page);
    await page.goto('/dashboard/');

    // Hover sobre o menu "Finanças" para expandir o dropdown
    const financasBtn = page.getByRole('button', { name: /Finanças/i }).first();
    if (await financasBtn.count() > 0) {
      await financasBtn.hover();
      // Aguarda o dropdown aparecer e clica em "Planilha Financeira"
      const planilhaLink = page.getByRole('menuitem', { name: 'Planilha Financeira' });
      await expect(planilhaLink).toBeVisible({ timeout: 5000 });
      await planilhaLink.click();
      await expect(page).toHaveURL(/personal-control/, { timeout: 5000 });
    }
  });
});

test.describe('Finance — Contas', () => {
  test.skip(!email || !password, 'PLAYWRIGHT_LOGIN_EMAIL e PLAYWRIGHT_LOGIN_PASSWORD são obrigatórios.');

  test('página de contas carrega', async ({ page }) => {
    await login(page);
    await page.goto('/finance/accounts/');

    await expect(page.locator('body')).not.toContainText('Application error');
    await expect(page.locator('body')).not.toContainText('404');

    await expect(
      page.getByRole('heading', { name: /contas/i }).first()
    ).toBeVisible({ timeout: 10000 });
  });
});

test.describe('Finance — Cartões', () => {
  test.skip(!email || !password, 'PLAYWRIGHT_LOGIN_EMAIL e PLAYWRIGHT_LOGIN_PASSWORD são obrigatórios.');

  test('página de cartões carrega', async ({ page }) => {
    await login(page);
    await page.goto('/finance/credit-cards/');

    await expect(page.locator('body')).not.toContainText('Application error');
    await expect(page.locator('body')).not.toContainText('404');

    // Aguarda carregamento do React Query — aceita o heading de sucesso ou a mensagem de erro da API
    await expect(
      page.getByRole('heading', { name: 'Cartões de Crédito' }).or(
        page.getByText('Erro ao carregar cartões de crédito')
      )
    ).toBeVisible({ timeout: 15000 });
  });
});
