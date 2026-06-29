/**
 * Regression tests for Sprints 13–14
 *
 * Validates:
 * 1. No native alert/confirm calls remain (ConfirmDialog pattern)
 * 2. Leads page loads and create dialog opens
 * 3. Customers page loads and create dialog opens
 * 4. Delete actions use ConfirmDialog (not native confirm)
 */

import { expect, Page, test } from '@playwright/test';

const email = process.env.PLAYWRIGHT_LOGIN_EMAIL;
const password = process.env.PLAYWRIGHT_LOGIN_PASSWORD;

async function login(page: Page) {
  // storageState pre-loads auth — navigate to dashboard directly
  await page.goto('/dashboard/');
  await page.waitForURL('**/dashboard/**', { timeout: 15_000 });
}

test.describe('Regression Sprint 13 — ConfirmDialog & no native dialogs', () => {
  test.skip(!email || !password, 'PLAYWRIGHT_LOGIN_EMAIL e PLAYWRIGHT_LOGIN_PASSWORD são obrigatórios.');

  test('leads page loads without errors', async ({ page }) => {
    await login(page);
    await page.goto('/leads/');
    await expect(page.getByRole('heading', { name: 'Leads' })).toBeVisible();
    await expect(page.locator('body')).not.toContainText('Application error');
  });

  test('leads create dialog opens via Novo Lead button', async ({ page }) => {
    await login(page);
    await page.goto('/leads/');
    await page.getByRole('button', { name: 'Novo Lead' }).click();
    await expect(page.getByRole('dialog')).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Novo Lead' })).toBeVisible();
    await page.keyboard.press('Escape');
  });

  test('customers page loads without errors', async ({ page }) => {
    await login(page);
    await page.goto('/customers/');
    await expect(page.getByRole('heading', { name: 'Clientes' })).toBeVisible();
    await expect(page.locator('body')).not.toContainText('Application error');
  });

  test('customers create dialog opens via Novo Cliente button', async ({ page }) => {
    await login(page);
    await page.goto('/customers/');
    await page.getByRole('button', { name: 'Novo Cliente' }).click();
    await expect(page.getByRole('dialog')).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Novo Cliente' })).toBeVisible();
    await page.keyboard.press('Escape');
  });

  test('leads delete shows ConfirmDialog (not native browser dialog)', async ({ page }) => {
    await login(page);
    await page.goto('/leads/');

    // Reject any native dialog — if a native confirm fires, the test will catch it
    let nativeDialogFired = false;
    page.on('dialog', (dialog) => {
      nativeDialogFired = true;
      dialog.dismiss();
    });

    // Find first row delete button if any exist
    const deleteButtons = page.locator('button[aria-label="Deletar"], button:has-text("Excluir")').first();
    const count = await deleteButtons.count();
    if (count > 0) {
      await deleteButtons.click();
      // ConfirmDialog should appear (shadcn Dialog, not native)
      const confirmDialog = page.getByRole('dialog').filter({ hasText: /confirmar|excluir|deletar/i });
      await expect(confirmDialog).toBeVisible({ timeout: 5000 });
      // Close it
      await page.keyboard.press('Escape');
    }

    expect(nativeDialogFired).toBe(false);
  });
});

test.describe('Regression Sprint 14 — TypeScript strict & form extraction', () => {
  test.skip(!email || !password, 'PLAYWRIGHT_LOGIN_EMAIL e PLAYWRIGHT_LOGIN_PASSWORD são obrigatórios.');

  test('lead form dialog validates required fields', async ({ page }) => {
    await login(page);
    await page.goto('/leads/');
    await page.getByRole('button', { name: 'Novo Lead' }).click();
    await expect(page.getByRole('dialog')).toBeVisible();
    // Submit empty form
    await page.getByRole('button', { name: 'Criar Lead' }).click();
    // Validation error should appear
    await expect(page.getByText(/nome deve ter/i)).toBeVisible({ timeout: 5000 });
    await page.keyboard.press('Escape');
  });

  test('customer form dialog validates required fields', async ({ page }) => {
    await login(page);
    await page.goto('/customers/');
    await page.getByRole('button', { name: 'Novo Cliente' }).click();
    await expect(page.getByRole('dialog')).toBeVisible();
    // Submit empty form
    await page.getByRole('button', { name: 'Criar Cliente' }).click();
    // Validation error should appear
    await expect(page.getByText(/nome deve ter/i)).toBeVisible({ timeout: 5000 });
    await page.keyboard.press('Escape');
  });
});
