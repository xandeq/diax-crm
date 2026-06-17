import { expect, test } from '@playwright/test';

const email = process.env.PLAYWRIGHT_LOGIN_EMAIL;
const password = process.env.PLAYWRIGHT_LOGIN_PASSWORD;

async function login(page: any) {
  await page.goto('/login/');
  await page.getByLabel('Senha').fill(password!);
  await page.locator('#email').fill(email!);
  await page.getByRole('button', { name: 'Entrar' }).click();
  await page.waitForURL('**/dashboard/**');
}

test.describe('Production smoke', () => {
  test.skip(!email || !password, 'PLAYWRIGHT_LOGIN_EMAIL e PLAYWRIGHT_LOGIN_PASSWORD são obrigatórios.');

  test('login e rotas críticas carregam', async ({ page }) => {
    await login(page);

    await page.goto('/finance/personal-control/');
    await expect(page.getByRole('heading', { name: 'Planilha Financeira' })).toBeVisible();

    await page.goto('/leads/');
    await expect(page.getByRole('heading', { name: 'Leads' })).toBeVisible();

    await page.goto('/customers/');
    await expect(page.getByRole('heading', { name: 'Clientes' })).toBeVisible();

    await page.goto('/email-marketing/');
    await expect(page.getByRole('heading', { name: 'Email Marketing' })).toBeVisible();
  });
});

test.describe('Regression — Apps Inventory', () => {
  test.skip(!email || !password, 'PLAYWRIGHT_LOGIN_EMAIL e PLAYWRIGHT_LOGIN_PASSWORD são obrigatórios.');

  test('página carrega com tabela e cards de resumo', async ({ page }) => {
    await login(page);

    await page.goto('/tools/apps-inventory/');

    await expect(page.getByRole('heading', { name: 'Inventário de Apps' })).toBeVisible();

    // Cards de resumo por tipo — usa botão do card de filtro (elemento único no topo)
    await expect(page.locator('button').filter({ hasText: /^SaaS/ }).first()).toBeVisible();
    await expect(page.locator('button').filter({ hasText: /^Pipeline/ }).first()).toBeVisible();
    await expect(page.locator('button').filter({ hasText: /^Bot\/Automação/ }).first()).toBeVisible();

    // Tabela: nome do projeto é um <a> link
    await expect(page.getByRole('link', { name: 'alecook' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'investiq' })).toBeVisible();

    // Footer hint com instrução Claude
    await expect(page.getByText('atualiza o inventário de apps')).toBeVisible();
  });

  test('busca filtra resultados corretamente', async ({ page }) => {
    await login(page);
    await page.goto('/tools/apps-inventory/');

    const searchInput = page.getByPlaceholder('Buscar por nome, stack, descrição...');
    await searchInput.fill('python');

    // Deve aparecer projetos Python — link do nome é único
    await expect(page.getByRole('link', { name: 'easy-apply-bot' })).toBeVisible();
    // Projetos não-Python devem desaparecer
    await expect(page.getByRole('link', { name: 'alecook' })).not.toBeVisible();

    // Limpar busca
    await page.getByRole('button', { name: 'Limpar filtros' }).click();
    await expect(page.getByRole('link', { name: 'alecook' })).toBeVisible();
  });

  test('filtro por tipo funciona', async ({ page }) => {
    await login(page);
    await page.goto('/tools/apps-inventory/');

    // Clicar no card "SaaS" para filtrar
    const saasCard = page.locator('button').filter({ hasText: 'SaaS' }).first();
    await saasCard.click();

    await expect(page.getByRole('link', { name: 'alecook' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'easy-apply-bot' })).not.toBeVisible();

    // Clicar novamente remove o filtro
    await saasCard.click();
    await expect(page.getByRole('link', { name: 'easy-apply-bot' })).toBeVisible();
  });

  test('sidebar dashboard tem link para Inventário de Apps', async ({ page }) => {
    await login(page);
    await page.goto('/dashboard/');

    // AppShell sidebar uses sh-nav class
    const inventarioLink = page.locator('.sh-nav a[href*="/tools/apps-inventory"]');
    await expect(inventarioLink).toBeVisible();
    await inventarioLink.click();
    await expect(page).toHaveURL(/tools\/apps-inventory/);
  });
});

test.describe('Regression — Email Marketing PRO', () => {
  test.skip(!email || !password, 'PLAYWRIGHT_LOGIN_EMAIL e PLAYWRIGHT_LOGIN_PASSWORD são obrigatórios.');

  test('página /email-marketing/pro carrega', async ({ page }) => {
    await login(page);
    await page.goto('/email-marketing/pro/');

    // Deve renderizar sem erro 404 ou crash
    await expect(page.locator('body')).not.toContainText('404');
    await expect(page.locator('body')).not.toContainText('Application error');
  });
});
