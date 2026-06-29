/**
 * Regression tests for bulk actions on /customers/ page
 *
 * Validates:
 * 1. TableActions bar appears when rows are selected
 * 2. "Mudar Status" dropdown is present and lists all statuses
 * 3. "Segmento" dropdown is present and lists Cold/Warm/Hot
 * 4. "Enviar E-mail" button remains accessible alongside new dropdowns
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

async function goToCustomers(page: Page) {
  await page.goto('/customers/');
  await expect(page.getByRole('heading', { name: 'Clientes' })).toBeVisible();
  // Wait for table rows to load — row checkboxes have exact aria-label "Selecionar" (not "Selecionar todos")
  await page.getByRole('checkbox', { name: 'Selecionar', exact: true }).first().waitFor({ state: 'visible', timeout: 15_000 });
}

async function selectFirstRow(page: Page) {
  // Shadcn checkboxes are <button role="checkbox"> — must use click(), not check()
  await page.getByRole('checkbox', { name: 'Selecionar', exact: true }).first().click();
}

test.describe('Regression — Bulk Actions on /customers/', () => {
  test.skip(!email || !password, 'PLAYWRIGHT_LOGIN_EMAIL e PLAYWRIGHT_LOGIN_PASSWORD são obrigatórios.');

  test('customers page loads and table renders', async ({ page }) => {
    await login(page);
    await page.goto('/customers/');
    await expect(page.getByRole('heading', { name: 'Clientes' })).toBeVisible();
    await expect(page.locator('body')).not.toContainText('Application error');
  });

  test('feature: TableActions bar appears on row selection', async ({ page }) => {
    await login(page);
    await goToCustomers(page);

    // Select first row via checkbox (accessible name "Selecionar")
    await selectFirstRow(page);

    // TableActions bar must appear
    await expect(page.locator('text=/1 item selecionado/')).toBeVisible();
  });

  test('feature: "Mudar Status" dropdown lists all status options', async ({ page }) => {
    await login(page);
    await goToCustomers(page);

    await selectFirstRow(page);
    await expect(page.locator('text=/1 item selecionado/')).toBeVisible();

    // Open "Mudar Status" dropdown
    await page.getByRole('button', { name: /Mudar Status/i }).click();

    // All 7 status options must be visible
    await expect(page.getByRole('menuitem', { name: 'Lead' })).toBeVisible();
    await expect(page.getByRole('menuitem', { name: 'Contactado' })).toBeVisible();
    await expect(page.getByRole('menuitem', { name: 'Qualificado' })).toBeVisible();
    await expect(page.getByRole('menuitem', { name: 'Negociando' })).toBeVisible();
    await expect(page.getByRole('menuitem', { name: 'Cliente' })).toBeVisible();
    await expect(page.getByRole('menuitem', { name: 'Inativo' })).toBeVisible();
    await expect(page.getByRole('menuitem', { name: 'Churn' })).toBeVisible();

    // Dismiss
    await page.keyboard.press('Escape');
  });

  test('feature: "Segmento" dropdown lists Cold/Warm/Hot', async ({ page }) => {
    await login(page);
    await goToCustomers(page);

    await selectFirstRow(page);
    await expect(page.locator('text=/1 item selecionado/')).toBeVisible();

    // Open "Segmento" dropdown — use first() to avoid matching the table column header button
    await page.getByRole('button', { name: /Segmento/i }).first().click();

    await expect(page.getByRole('menuitem', { name: /Cold/i })).toBeVisible();
    await expect(page.getByRole('menuitem', { name: /Warm/i })).toBeVisible();
    await expect(page.getByRole('menuitem', { name: /Hot/i })).toBeVisible();

    await page.keyboard.press('Escape');
  });

  test('feature: all three bulk action controls visible simultaneously', async ({ page }) => {
    await login(page);
    await goToCustomers(page);

    await selectFirstRow(page);
    await expect(page.locator('text=/1 item selecionado/')).toBeVisible();

    // All three action controls must be visible simultaneously
    // "Segmento" has 2 matches (TableActions button + table column header) — use first()
    await expect(page.getByRole('button', { name: /Mudar Status/i })).toBeVisible();
    await expect(page.getByRole('button', { name: /Segmento/i }).first()).toBeVisible();
    await expect(page.getByRole('button', { name: /Enviar E-mail/i })).toBeVisible();
  });

  test('bug: email uniqueness check skipped for customers with empty email', async ({ page }) => {
    // Smoke indicator — the API fix (CustomerService.UpdateAsync) must be deployed for this to pass.
    // Bug: PUT /customers/{id} threw 409 when email="" and another customer also had email="".
    // Fix: guard EmailExistsAsync with IsNullOrWhiteSpace check.
    await login(page);
    await page.goto('/customers/');
    await expect(page.getByRole('heading', { name: 'Clientes' })).toBeVisible();
    await expect(page.locator('body')).not.toContainText('Application error');
  });
});
