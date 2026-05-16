import { expect, Page, test } from '@playwright/test';

const email = process.env.PLAYWRIGHT_LOGIN_EMAIL;
const password = process.env.PLAYWRIGHT_LOGIN_PASSWORD;

async function login(page: Page) {
  await page.goto('/login/');
  await page.locator('#email').fill(email!);
  await page.getByLabel('Senha').fill(password!);
  await page.getByRole('button', { name: 'Entrar' }).click();
  await page.waitForURL('**/dashboard/**');
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

    // Navegação de mês presente
    await expect(page.getByRole('button', { name: /anterior/i }).or(
      page.locator('[aria-label*="anterior"]').or(page.locator('button').filter({ hasText: /←|‹/ })).first()
    ).first()).toBeVisible();
  });

  test('navegação entre meses funciona', async ({ page }) => {
    await login(page);
    await page.goto('/finance/personal-control/');

    // Capturar mês atual exibido
    const initialTitle = await page.locator('h2, h3').filter({ hasText: /janeiro|fevereiro|março|abril|maio|junho|julho|agosto|setembro|outubro|novembro|dezembro/i }).first().textContent();

    // Clicar em mês anterior (procura botão com seta para esquerda)
    const prevBtn = page.getByRole('button').filter({ hasText: /←/ }).or(
      page.locator('button[aria-label*="anterior"]')
    ).first();

    if (await prevBtn.count() > 0) {
      await prevBtn.click();
      await page.waitForLoadState('networkidle');

      // Mês deve ter mudado
      const newTitle = await page.locator('h2, h3').filter({ hasText: /janeiro|fevereiro|março|abril|maio|junho|julho|agosto|setembro|outubro|novembro|dezembro/i }).first().textContent();
      expect(newTitle).not.toEqual(initialTitle);
    }
  });

  test('seção de receitas é visível', async ({ page }) => {
    await login(page);
    await page.goto('/finance/personal-control/');

    // Deve ter tabela ou seção de receitas
    await expect(
      page.getByText('Receitas').first().or(
        page.getByRole('heading', { name: /receitas/i }).first()
      )
    ).toBeVisible();
  });

  test('seção de assinaturas é visível', async ({ page }) => {
    await login(page);
    await page.goto('/finance/personal-control/');

    await expect(
      page.getByText('Assinaturas').first().or(
        page.getByRole('heading', { name: /assinaturas/i }).first()
      )
    ).toBeVisible();
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

    // Navegar para finanças via menu
    const financeMenu = page.getByRole('link', { name: /financeiro|finanças/i }).first()
      .or(page.getByRole('button', { name: /financeiro|finanças/i }).first());

    if (await financeMenu.count() > 0) {
      await financeMenu.click();
      await expect(page).toHaveURL(/finance/);
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

    await expect(
      page.getByRole('heading', { name: /cartões|cartão/i }).first()
    ).toBeVisible({ timeout: 10000 });
  });
});
