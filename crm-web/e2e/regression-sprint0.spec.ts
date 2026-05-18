/**
 * Regression tests for Sprint 0 — House Cleaning
 *
 * Validates:
 * 1. Deprecated income/expense/category pages redirect to unified routes
 * 2. Unified transaction routes load without errors
 * 3. New kebab-case API routes work (finance/imports, finance/transfers)
 * 4. Morning Briefing still accessible
 */

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

// ---------------------------------------------------------------------------
// Redirect regression — Sprint 0 Commit 4
// Deprecated pages must redirect to unified replacements (never show 404/500)
// ---------------------------------------------------------------------------

test.describe('Regression Sprint 0 — Deprecated routes redirect', () => {
    test.skip(!email || !password, 'PLAYWRIGHT_LOGIN_EMAIL e PLAYWRIGHT_LOGIN_PASSWORD são obrigatórios.');

    test('/finance/incomes redireciona para /finance/transactions', async ({ page }) => {
        await login(page);
        await page.goto('/finance/incomes/');
        await page.waitForURL(/transactions/, { timeout: 10000 });
        await expect(page).toHaveURL(/finance\/transactions/);
        await expect(page.locator('body')).not.toContainText('404');
        await expect(page.locator('body')).not.toContainText('Application error');
    });

    test('/finance/expenses redireciona para /finance/transactions', async ({ page }) => {
        await login(page);
        await page.goto('/finance/expenses/');
        await page.waitForURL(/transactions/, { timeout: 10000 });
        await expect(page).toHaveURL(/finance\/transactions/);
        await expect(page.locator('body')).not.toContainText('404');
    });

    test('/finance/categories/income redireciona para /finance/transactions', async ({ page }) => {
        await login(page);
        await page.goto('/finance/categories/income/');
        await page.waitForURL(/transactions/, { timeout: 10000 });
        await expect(page).toHaveURL(/finance\/transactions/);
        await expect(page.locator('body')).not.toContainText('404');
    });

    test('/finance/categories/expense redireciona para /finance/transactions', async ({ page }) => {
        await login(page);
        await page.goto('/finance/categories/expense/');
        await page.waitForURL(/transactions/, { timeout: 10000 });
        await expect(page).toHaveURL(/finance\/transactions/);
        await expect(page.locator('body')).not.toContainText('404');
    });
});

// ---------------------------------------------------------------------------
// New unified routes — Sprint 0 Commit 3
// Kebab-case routes must load correctly after standardization
// ---------------------------------------------------------------------------

test.describe('Regression Sprint 0 — Unified finance routes', () => {
    test.skip(!email || !password, 'PLAYWRIGHT_LOGIN_EMAIL e PLAYWRIGHT_LOGIN_PASSWORD são obrigatórios.');

    test('/finance/transactions carrega sem erros', async ({ page }) => {
        await login(page);
        await page.goto('/finance/transactions/');

        await expect(page.locator('body')).not.toContainText('Application error');
        await expect(page.locator('body')).not.toContainText('404');
        await expect(page.locator('body')).not.toContainText('500');

        await expect(
            page.getByRole('heading', { name: /transações/i }).first()
                .or(page.getByText(/transações/i).first())
        ).toBeVisible({ timeout: 10000 });
    });

    test('/finance/imports carrega sem erros (kebab route: statement-imports)', async ({ page }) => {
        await login(page);
        await page.goto('/finance/imports/');

        await expect(page.locator('body')).not.toContainText('Application error');
        await expect(page.locator('body')).not.toContainText('404');
        await expect(page.locator('body')).not.toContainText('500');
    });

    test('/finance/transfers carrega sem erros', async ({ page }) => {
        await login(page);
        await page.goto('/finance/transfers/');

        await expect(page.locator('body')).not.toContainText('Application error');
        await expect(page.locator('body')).not.toContainText('404');
    });

    test('/finance/morning-briefing carrega sem erros', async ({ page }) => {
        await login(page);
        await page.goto('/finance/morning-briefing/');

        await expect(page.locator('body')).not.toContainText('Application error');
        await expect(page.locator('body')).not.toContainText('404');

        await expect(
            page.getByRole('heading', { name: /briefing|resumo/i }).first()
                .or(page.getByText(/morning briefing/i).first())
        ).toBeVisible({ timeout: 10000 });
    });
});
