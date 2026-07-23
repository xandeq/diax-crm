import { expect, test } from '@playwright/test';

/**
 * Painel "Copiar para o WhatsApp" na área Daily Briefings.
 * Abre um briefing HTML, confirma o painel, copia um bloco e valida o clipboard:
 * texto não vazio + toda URL com esquema (https/http/www).
 */

const email = process.env.PLAYWRIGHT_LOGIN_EMAIL;
const password = process.env.PLAYWRIGHT_LOGIN_PASSWORD;

test.describe('Daily Briefings — Copiar para WhatsApp', () => {
  test.skip(!email || !password, 'PLAYWRIGHT_LOGIN_EMAIL e PLAYWRIGHT_LOGIN_PASSWORD são obrigatórios.');

  test.beforeEach(async ({ context, page }) => {
    await context.grantPermissions(['clipboard-read', 'clipboard-write']);
    await page.goto('/dashboard/');
    await expect(page).toHaveURL(/dashboard/, { timeout: 15_000 });
  });

  test('painel copia bloco formatado com URLs válidas', async ({ page }) => {
    const jsErrors: string[] = [];
    page.on('pageerror', (e) => jsErrors.push(e.message));

    await page.goto('/daily-briefings/');
    const firstCard = page.locator('.db-card').first();
    // pode não haver briefing HTML hoje — não falhar o suite
    if (!(await firstCard.isVisible({ timeout: 15_000 }).catch(() => false))) {
      test.skip(true, 'Sem briefings hoje para testar.');
      return;
    }

    await firstCard.click();
    await expect(page.getByRole('button', { name: /Voltar/i }).first()).toBeVisible({ timeout: 10_000 });

    // Painel WhatsApp aparece apenas em briefings HTML
    const panel = page.locator('.wap');
    if (!(await panel.isVisible({ timeout: 8_000 }).catch(() => false))) {
      test.skip(true, 'Briefing do dia não é HTML (sem painel).');
      return;
    }

    // Clica no primeiro botão "Copiar" disponível (bloco ou "Copiar tudo")
    const copyBtn = page.locator('.wap button', { hasText: /Copiar/i }).first();
    await copyBtn.click();
    await expect(page.locator('.wap button', { hasText: /Copiado/i }).first()).toBeVisible({ timeout: 4_000 });

    // Lê o clipboard e valida formatação
    const text = await page.evaluate(() => navigator.clipboard.readText());
    expect(text.trim().length).toBeGreaterThan(0);

    // Nenhuma URL "nua" sem esquema: todo http(s):// ou www. é aceitável.
    // Detecta domínios óbvios sem esquema (ex.: "foo.com/bar" solto) e falha.
    const bareUrl = text.match(/(^|\s)(?!https?:\/\/)(?!www\.)([a-z0-9-]+\.(?:com|dev|ai|io|net|org|app|br)(?:\/\S*)?)(\s|$)/i);
    expect(bareUrl, `URL sem esquema encontrada: ${bareUrl?.[2]}`).toBeNull();

    expect(jsErrors.filter((e) => !e.includes('ResizeObserver'))).toHaveLength(0);
  });
});
