import { expect, test } from '@playwright/test';

const email = process.env.PLAYWRIGHT_LOGIN_EMAIL;
const password = process.env.PLAYWRIGHT_LOGIN_PASSWORD;

test.describe('Production smoke', () => {
  test.skip(!email || !password, 'PLAYWRIGHT_LOGIN_EMAIL e PLAYWRIGHT_LOGIN_PASSWORD são obrigatórios.');

  test('login e rotas críticas carregam', async ({ page }) => {
    await page.goto('/login/');
    await page.getByLabel('Senha').fill(password!);
    await page.locator('#email').fill(email!);
    await page.getByRole('button', { name: 'Entrar' }).click();

    await page.waitForURL('**/dashboard/**');

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
