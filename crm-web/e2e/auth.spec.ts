import { expect, test } from '@playwright/test';

// Auth tests verify unauthenticated redirect behavior — must start with a clean session
test.use({ storageState: { cookies: [], origins: [] } });

const email = process.env.PLAYWRIGHT_LOGIN_EMAIL;
const password = process.env.PLAYWRIGHT_LOGIN_PASSWORD;

test.describe('Auth — Login', () => {
    test('credenciais inválidas mostram mensagem de erro (não crash)', async ({ page }) => {
        await page.goto('/login/');
        await page.locator('#email').fill('invalid@test.com');
        await page.getByLabel('Senha').fill('wrongpassword123');
        await page.getByRole('button', { name: 'Entrar' }).click();

        // Deve permanecer na página de login — nunca redirecionar para dashboard
        await page.waitForLoadState('networkidle');
        await expect(page).not.toHaveURL(/dashboard/);
        await expect(page.locator('body')).not.toContainText('Application error');

        // A div de erro com bg-destructive deve aparecer (o serverError state)
        await expect(
            page.locator('.bg-destructive\\/10').first()
        ).toBeVisible({ timeout: 10000 });
    });

    test('rota protegida sem login redireciona para /login', async ({ page }) => {
        // AuthGuard redireciona via useEffect (assíncrono) — aguardar URL mudar
        await page.goto('/dashboard/');
        await expect(page).toHaveURL(/login/, { timeout: 10000 });
    });

    test('rota protegida de finanças sem login redireciona para /login', async ({ page }) => {
        await page.goto('/finance/personal-control/');
        await expect(page).toHaveURL(/login/, { timeout: 10000 });
    });
});

test.describe('Auth — Sessão', () => {
    test.skip(!email || !password, 'PLAYWRIGHT_LOGIN_EMAIL e PLAYWRIGHT_LOGIN_PASSWORD são obrigatórios.');

    test('login com credenciais válidas redireciona para dashboard', async ({ page }) => {
        await page.goto('/login/');
        await page.locator('#email').fill(email!);
        await page.getByLabel('Senha').fill(password!);
        await page.getByRole('button', { name: 'Entrar' }).click();

        await expect(page).toHaveURL(/dashboard/, { timeout: 15000 });
        await expect(page.locator('body')).not.toContainText('Application error');
    });
});
