import * as fs from 'fs';
import * as path from 'path';

import { test as setup } from '@playwright/test';

const authFile = path.join(__dirname, '.auth/user.json');
const origin = 'https://crm.alexandrequeiroz.com.br';

setup('authenticate', async ({ page }) => {
  const email = process.env.PLAYWRIGHT_LOGIN_EMAIL;
  const password = process.env.PLAYWRIGHT_LOGIN_PASSWORD;

  fs.mkdirSync(path.dirname(authFile), { recursive: true });

  if (!email || !password) {
    fs.writeFileSync(authFile, JSON.stringify({ cookies: [], origins: [] }));
    return;
  }

  await page.goto('/login/');
  await page.locator('#email').fill(email);
  await page.getByLabel('Senha').fill(password);
  await page.getByRole('button', { name: 'Entrar' }).click();
  await page.waitForURL('**/dashboard/**', { timeout: 30_000 });

  // DIAX stores JWT in sessionStorage ('accessToken') — Playwright storageState() only
  // captures cookies + localStorage. Extract manually and place in localStorage so that
  // api.ts's getAccessToken() fallback (sessionStorage → localStorage) finds it.
  const token = await page.evaluate(() => window.sessionStorage.getItem('accessToken'));

  const state = await page.context().storageState();

  if (token) {
    let originEntry = state.origins.find(o => o.origin === origin);
    if (!originEntry) {
      originEntry = { origin, localStorage: [] };
      state.origins.push(originEntry);
    }
    originEntry.localStorage = originEntry.localStorage.filter(i => i.name !== 'accessToken');
    originEntry.localStorage.push({ name: 'accessToken', value: token });
  }

  fs.writeFileSync(authFile, JSON.stringify(state, null, 2));
});
