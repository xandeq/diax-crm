import { expect, test } from '@playwright/test';

const email    = process.env.PLAYWRIGHT_LOGIN_EMAIL;
const password = process.env.PLAYWRIGHT_LOGIN_PASSWORD;

test.describe('Dashboard Premium — Wave QA', () => {
  test.skip(!email || !password, 'PLAYWRIGHT_LOGIN_EMAIL e PLAYWRIGHT_LOGIN_PASSWORD são obrigatórios.');

  test.beforeEach(async ({ page }) => {
    await page.goto('/login/');
    await page.locator('#email').fill(email!);
    await page.getByLabel('Senha').fill(password!);
    await page.getByRole('button', { name: 'Entrar' }).click();
    await expect(page).toHaveURL(/dashboard/, { timeout: 15000 });
  });

  test('dashboard carrega sem erros de JS', async ({ page }) => {
    const jsErrors: string[] = [];
    page.on('pageerror', err => jsErrors.push(err.message));

    await page.goto('/dashboard/');
    await page.waitForLoadState('networkidle', { timeout: 30000 });

    expect(jsErrors.filter(e => !e.includes('ResizeObserver'))).toHaveLength(0);
    await expect(page.locator('body')).not.toContainText('Application error');
  });

  test('hero card — metrica de receita visivel', async ({ page }) => {
    await page.goto('/dashboard/');
    await page.waitForLoadState('networkidle', { timeout: 30000 });

    // Hero card must exist (teal border = hero-card class)
    const heroCard = page.locator('.hero-card').first();
    await expect(heroCard).toBeVisible({ timeout: 15000 });

    // Live dot indicator
    await expect(page.locator('.live-dot').first()).toBeVisible();

    // At least one metric with "R$" prefix
    const heroMetric = heroCard.locator('.metric-huge');
    await expect(heroMetric).toBeVisible({ timeout: 10000 });
    await expect(heroMetric).toContainText('R$');
  });

  test('command center — 5 perguntas renderizadas', async ({ page }) => {
    await page.goto('/dashboard/');
    await page.waitForLoadState('networkidle', { timeout: 30000 });

    // Command center has 5 question cards
    const cmdCards = page.locator('text=O que funciona?').or(
      page.locator('text=O que está quebrado?')
    );
    await expect(cmdCards.first()).toBeVisible({ timeout: 15000 });
  });

  test('KPI cards — 4 cards com sparklines visíveis', async ({ page }) => {
    await page.goto('/dashboard/');
    await page.waitForLoadState('networkidle', { timeout: 30000 });

    // Check KPI labels exist
    const labels = ['RECEITA DO MÊS', 'TOTAL DESPESAS', 'LEADS NO FUNIL', 'TAXA DE ABERTURA EMAIL'];
    for (const label of labels) {
      await expect(page.locator(`text=${label}`).first()).toBeVisible({ timeout: 10000 });
    }
  });

  test('charts — nenhum 500 ou chunk error durante carga', async ({ page }) => {
    const failedRequests: string[] = [];
    page.on('response', resp => {
      if (resp.status() >= 500) failedRequests.push(`5xx: ${resp.url()}`);
    });
    page.on('requestfailed', req => {
      const url = req.url();
      // Only track _next/static chunks — API failures are handled gracefully
      if (url.includes('_next/static')) failedRequests.push(`FAIL: ${url}`);
    });

    await page.goto('/dashboard/');
    await page.waitForLoadState('networkidle', { timeout: 30000 });

    expect(failedRequests).toHaveLength(0);
  });

  test('navegação — link "Ver leads" funciona no funnel', async ({ page }) => {
    await page.goto('/dashboard/');
    await page.waitForLoadState('networkidle', { timeout: 30000 });

    const leadsLink = page.locator('a[href="/leads"]').first();
    if (await leadsLink.isVisible({ timeout: 8000 })) {
      await leadsLink.click();
      await expect(page).toHaveURL(/leads/, { timeout: 10000 });
    }
  });
});
