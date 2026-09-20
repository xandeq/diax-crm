// Prova visual do DASH-01 — login real em producao + screenshot do /dashboard-comercial
// renderizado, com checks de DOM e comparacao contra a evidencia da API (Task 1).
// Modelado em crm-e2e.mjs (mesmo estilo ESM, contexto stealth pt-BR/America/Sao_Paulo).
import { chromium } from '@playwright/test';
import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import path from 'node:path';

const here = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(here, '..', '..', '..'); // docs/email-marketing/qa-e2e -> raiz do repo
const scratchDir = 'C:/Users/acq20/AppData/Local/Temp/claude/D--claude-code/1cf50a05-c574-4c55-b6a9-30c19f6138d9/scratchpad';

// credenciais do arquivo central — NUNCA logadas
const env = {};
for (const line of readFileSync('C:/Users/acq20/.claude/.secrets.env', 'utf-8').split(/\r?\n/)) {
  const t = line.trim();
  if (t && !t.startsWith('#') && t.includes('=')) { const i = t.indexOf('='); env[t.slice(0, i)] = t.slice(i + 1); }
}
const EMAIL = env.DIAX_ADMIN_EMAIL, PASS = env.DIAX_ADMIN_PASSWORD;
if (!EMAIL || !PASS) {
  console.error('MISSING DIAX_ADMIN_EMAIL/DIAX_ADMIN_PASSWORD in secrets.env');
  process.exitCode = 1;
  process.exit(1);
}

const BASE = 'https://crm.alexandrequeiroz.com.br';
const PNG_PATH = path.join(repoRoot, 'docs/email-marketing/previews/dashboard-funil-screenshot.png');
const REPORT_PATH = path.join(here, 'dashboard-funil-shot-report.json');
const EVIDENCIA_PATH = path.join(repoRoot, 'docs/email-marketing/previews/dashboard-funil-evidencia.json');

function nowStamps() {
  const utc = new Date();
  const brt = new Date(utc.getTime() - 3 * 60 * 60 * 1000);
  return {
    utc: utc.toISOString().replace(/\.\d+Z$/, 'Z'),
    brt: brt.toISOString().replace('Z', '-03:00').replace(/\.\d+/, ''),
  };
}

// pt-BR toLocaleString: separador de milhar '.' — reverte pra numero puro
function parsePtBrNumber(s) {
  if (s == null) return null;
  const cleaned = String(s).replace(/\./g, '').replace(/[^0-9-]/g, '');
  if (cleaned === '' || cleaned === '-') return null;
  return parseInt(cleaned, 10);
}

const STAGE_LABELS = {
  Lead: 'Leads',
  Contacted: 'Contactados',
  Qualified: 'Qualificados',
  Negotiating: 'Negociando',
  Customer: 'Clientes',
};

const report = {
  url: BASE + '/dashboard-comercial/',
  capturado_em: null,
  checks: {},
  comparacao_api: null,
  consoleErrors: [],
  ok: false,
};

const browser = await chromium.launch();
const ctx = await browser.newContext({
  viewport: { width: 1440, height: 900 }, deviceScaleFactor: 1,
  locale: 'pt-BR', timezoneId: 'America/Sao_Paulo',
  userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36',
});
await ctx.addInitScript(() => { Object.defineProperty(navigator, 'webdriver', { get: () => undefined }); });
const page = await ctx.newPage();
const consoleErrors = [];
page.on('console', m => { if (m.type() === 'error') consoleErrors.push(m.text()); });

try {
  // 1) login real
  await page.goto(BASE + '/login/', { waitUntil: 'networkidle', timeout: 45000 });
  await page.waitForSelector('input[type="email"], input#email, input[name="email"]', { timeout: 20000 });
  await page.fill('input[type="email"], input#email, input[name="email"]', EMAIL);
  await page.fill('input[type="password"], input#password, input[name="password"], input[name="senha"]', PASS);
  await Promise.all([
    page.waitForURL(u => !u.toString().includes('/login'), { timeout: 30000 }).catch(() => {}),
    page.click('button[type="submit"], button:has-text("Entrar")'),
  ]);
  await page.waitForTimeout(2000);

  // 2) navegar para /dashboard-comercial/ (trailingSlash: true no static export)
  await page.goto(BASE + '/dashboard-comercial/', { waitUntil: 'networkidle', timeout: 45000 });

  // 3) esperar dado real chegar (busca via useEffect, nao SSR)
  await page.waitForSelector('text=Funil de vendas', { timeout: 30000 });
  await page.waitForTimeout(3000);

  // 4) raspar body + rodar os 4 checks
  const bodyText = await page.evaluate(() => document.body.innerText);
  report.capturado_em = nowStamps();

  const checks = {
    titulo_funil: bodyText.includes('Funil de vendas'),
    rotulo_conversao: bodyText.includes('avançaram'),
    cinco_etapas: ['Leads', 'Contactados', 'Qualificados', 'Negociando', 'Clientes'].every(l => bodyText.includes(l)),
    sem_erro: !bodyText.includes('Erro ao carregar') && !bodyText.includes('404'),
  };
  report.checks = checks;

  // 5) comparar numeros exibidos com funnel_bruto da evidencia da Task 1
  let evidencia = null;
  try {
    evidencia = JSON.parse(readFileSync(EVIDENCIA_PATH, 'utf-8'));
  } catch (e) {
    // segue sem comparacao se a evidencia nao existir por algum motivo
  }

  const porEtapa = [];
  let bateExato = true;
  if (evidencia) {
    // A pagina tem "Leads"/"Clientes" tambem no menu lateral (nav) ANTES da secao do
    // funil — escopar a busca so ao trecho entre "Funil de vendas" e "Receita de propostas"
    // pra nao casar com o item de navegacao em vez do rotulo da etapa.
    const linhas = bodyText.split('\n');
    const idxFunilInicio = linhas.findIndex(l => l.trim() === 'Funil de vendas');
    let idxFunilFim = linhas.findIndex((l, idx) => idx > idxFunilInicio && l.trim().startsWith('Receita de propostas'));
    if (idxFunilFim === -1) idxFunilFim = linhas.length;
    const linhasFunil = idxFunilInicio === -1 ? [] : linhas.slice(idxFunilInicio, idxFunilFim);

    for (const f of evidencia.funnel_bruto) {
      const label = STAGE_LABELS[f.stage] || f.stage;
      let naTela = null;
      const idxLabel = linhasFunil.findIndex(l => l.trim() === label);
      if (idxLabel !== -1) {
        for (let j = idxLabel + 1; j < Math.min(idxLabel + 3, linhasFunil.length); j++) {
          const parsed = parsePtBrNumber(linhasFunil[j].trim());
          if (parsed !== null) { naTela = parsed; break; }
        }
      }
      const naApi = f.count;
      const delta = naTela === null ? null : naTela - naApi;
      if (delta !== 0) bateExato = false;
      porEtapa.push({ stage: f.stage, na_api: naApi, na_tela: naTela, delta });
    }
  } else {
    bateExato = false;
  }

  report.comparacao_api = {
    por_etapa: porEtapa,
    bate_exato: bateExato,
    evidencia_capturado_em: evidencia ? evidencia.capturado_em : null,
    shot_capturado_em: report.capturado_em,
  };

  // 6) screenshot fullPage — commitavel, fora de shots-crm/
  await page.screenshot({ path: PNG_PATH, fullPage: true });

  report.consoleErrors = consoleErrors.slice(0, 10);
  report.ok = Object.values(checks).every(Boolean);

  console.log('checks:', checks);
  console.log('comparacao_api.bate_exato:', bateExato);
  console.log('ok:', report.ok);

  if (!report.ok) process.exitCode = 1;
} catch (e) {
  report.ok = false;
  report.error = String(e).slice(0, 500);
  report.capturado_em = report.capturado_em || nowStamps();
  mkdirSync(scratchDir, { recursive: true });
  await page.screenshot({ path: path.join(scratchDir, 'dashboard-funil-ERROR.png') }).catch(() => {});
  console.error('FALHA:', report.error);
  process.exitCode = 1;
} finally {
  await browser.close();
}

writeFileSync(REPORT_PATH, JSON.stringify(report, null, 2), 'utf-8');
console.log(`\n== dashboard-funil-shot: ok=${report.ok} ==`);
