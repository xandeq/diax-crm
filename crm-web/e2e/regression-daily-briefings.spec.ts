import { expect, test } from '@playwright/test';

const email = process.env.PLAYWRIGHT_LOGIN_EMAIL;
const password = process.env.PLAYWRIGHT_LOGIN_PASSWORD;

test.describe('Daily Briefings — area nova', () => {
  test.skip(!email || !password, 'PLAYWRIGHT_LOGIN_EMAIL e PLAYWRIGHT_LOGIN_PASSWORD são obrigatórios.');

  test.beforeEach(async ({ page }) => {
    await page.goto('/login/');
    await page.locator('#email').fill(email!);
    await page.getByLabel('Senha').fill(password!);
    await page.getByRole('button', { name: 'Entrar' }).click();
    await expect(page).toHaveURL(/dashboard/, { timeout: 15000 });
  });

  test('lista do dia + abre conteúdo, sem erros de JS', async ({ page }) => {
    const jsErrors: string[] = [];
    page.on('pageerror', e => jsErrors.push(e.message));
    const serverErrors: string[] = [];
    page.on('response', r => { if (r.status() >= 500) serverErrors.push(`${r.status()} ${r.url()}`); });

    await page.goto('/daily-briefings/');
    await page.waitForLoadState('networkidle', { timeout: 30000 });

    // Título da área
    await expect(page.locator('text=Daily Briefings').first()).toBeVisible({ timeout: 10000 });

    // Cards do dia (há briefings inseridos hoje) — clica no primeiro e abre a view de detalhe (botão Voltar)
    const firstCard = page.locator('.db-card').first();
    if (await firstCard.isVisible({ timeout: 8000 }).catch(() => false)) {
      await firstCard.click();
      await expect(page.getByRole('button', { name: /Voltar/i }).first()).toBeVisible({ timeout: 10000 });
      // Volta para a lista
      await page.getByRole('button', { name: /Voltar/i }).first().click();
      await expect(page.locator('.db-card').first()).toBeVisible({ timeout: 10000 });
    }

    expect(jsErrors.filter(e => !e.includes('ResizeObserver'))).toHaveLength(0);
    expect(serverErrors).toHaveLength(0);
    await expect(page.locator('body')).not.toContainText('Application error');
  });

  test('link no header navega para a área', async ({ page }) => {
    await page.goto('/dashboard/');
    await page.waitForLoadState('networkidle', { timeout: 30000 });
    const navLink = page.locator('a[href*="/daily-briefings"]').first();
    await expect(navLink).toBeVisible({ timeout: 10000 });
    await navLink.click();
    await expect(page).toHaveURL(/daily-briefings/, { timeout: 10000 });
  });
});
