'use client';

import { useEffect, useState } from 'react';
import {
  Activity, AlertCircle, Bot, BookOpen, Check, ChevronDown, ChevronRight, Code2, Copy, Globe, Key,
  Layers, Loader2, Play, RefreshCw, Settings, ShieldAlert, Sparkles,
} from 'lucide-react';
import {
  getFreeAiBaseUrl, getFreeAiHealth, testFreeAi,
  type FreeAiChatResponse, type FreeAiHealth,
} from '@/services/freeAi';

// A ProxyApiKey NUNCA fica no código (repositório e página são públicos). O usuário cola uma vez e
// ela fica só no localStorage deste navegador, para montar os exemplos já com a chave.
const KEY_STORAGE = 'diax-free-ai-proxy-key';
const KEY_PLACEHOLDER = 'SUA_PROXY_API_KEY';

function CopyButton({ text, label = 'Copiar' }: { text: string; label?: string }) {
  const [copied, setCopied] = useState(false);
  const handleCopy = async () => {
    await navigator.clipboard.writeText(text);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };
  return (
    <button type="button" onClick={handleCopy} style={{
      display: 'flex', alignItems: 'center', gap: 4, padding: '4px 10px', borderRadius: 6,
      background: copied ? 'rgba(16,185,129,0.2)' : 'rgba(255,255,255,0.07)',
      border: `1px solid ${copied ? 'rgba(16,185,129,0.4)' : 'rgba(255,255,255,0.1)'}`,
      color: copied ? '#10B981' : '#9CA3AF', fontSize: 11, fontWeight: 600, cursor: 'pointer', whiteSpace: 'nowrap',
    }}>
      {copied ? <Check size={11} /> : <Copy size={11} />}
      {copied ? 'Copiado!' : label}
    </button>
  );
}

function CodeBlock({ code, lang }: { code: string; lang: string }) {
  return (
    <div style={{ borderRadius: 10, background: '#080E0A', border: '1px solid rgba(255,255,255,0.08)', overflow: 'hidden', marginBottom: 12 }}>
      <div style={{
        display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '8px 14px',
        borderBottom: '1px solid rgba(255,255,255,0.06)', background: 'rgba(255,255,255,0.02)',
      }}>
        <span style={{ fontSize: 11, color: '#6B7280', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '.05em' }}>{lang}</span>
        <CopyButton text={code.trim()} />
      </div>
      <pre style={{
        margin: 0, padding: '16px 18px', overflowX: 'auto', fontSize: 12.5, lineHeight: 1.75, color: '#D1FAE5',
        fontFamily: 'Consolas, "Cascadia Code", "Fira Code", monospace',
      }}><code>{code.trim()}</code></pre>
    </div>
  );
}

function Section({ icon: Icon, title, color = '#10B981', children }: {
  icon: React.ElementType; title: string; color?: string; children: React.ReactNode;
}) {
  return (
    <div style={{ marginBottom: 32 }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 16 }}>
        <div style={{
          width: 32, height: 32, borderRadius: 9, background: `${color}20`, border: `1px solid ${color}40`,
          display: 'flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0,
        }}>
          <Icon size={15} color={color} />
        </div>
        <h2 style={{ margin: 0, fontSize: 15, fontWeight: 700, color: '#F9FAFB' }}>{title}</h2>
      </div>
      {children}
    </div>
  );
}

function InfoRow({ label, value }: { label: string; value: string }) {
  return (
    <div style={{
      display: 'flex', alignItems: 'center', gap: 12, padding: '10px 14px', borderRadius: 8, flexWrap: 'wrap',
      background: 'rgba(255,255,255,0.03)', border: '1px solid rgba(255,255,255,0.07)', marginBottom: 6,
    }}>
      <span style={{ fontSize: 12, color: '#6B7280', fontWeight: 600, minWidth: 130, flexShrink: 0 }}>{label}</span>
      <code style={{ flex: 1, minWidth: 0, fontSize: 12, color: '#10B981', fontFamily: 'monospace', wordBreak: 'break-all' }}>{value}</code>
      <CopyButton text={value} />
    </div>
  );
}

const P = { fontSize: 13, color: '#9CA3AF', lineHeight: 1.75, margin: '0 0 12px' } as const;
const CARD = { padding: '14px 16px', borderRadius: 10, background: 'rgba(255,255,255,0.03)', border: '1px solid rgba(255,255,255,0.07)' } as const;

const ERRORS: { code: string; meaning: string; action: string }[] = [
  { code: '400', meaning: 'Pedido inválido (sem mensagens, role errada, max_tokens fora de 1–8192, stream: true, ou model fora da lista).', action: 'Corrigir o pedido. Não repetir igual.' },
  { code: '401', meaning: 'Sem chave ou chave errada no header X-Api-Key.', action: 'Conferir a ProxyApiKey.' },
  { code: '403', meaning: 'A ProxyApiKey tentou acessar algo fora de /free-ai e dos proxies de IA.', action: 'Usar só as rotas desta página.' },
  { code: '429', meaning: 'Passou de 60 requisições por minuto (por IP).', action: 'Esperar ~60 s e tentar de novo.' },
  { code: '503', meaning: 'Todas as IAs gratuitas da cadeia falharam agora. O corpo traz attempts[] com o motivo de cada uma.', action: 'Tentar de novo em 30–60 s (cotas grátis renovam).' },
];

const FAQ: { q: string; a: string }[] = [
  { q: 'Gasta crédito pago?', a: 'Não. O gateway só chama a cadeia de modelos gratuitos (Gemini, Groq, OpenRouter :free, Cerebras e HuggingFace). Um model que não esteja em GET /models é recusado com 400 antes de qualquer chamada, então ninguém consegue usar um modelo pago da OpenRouter por aqui.' },
  { q: 'Qual modelo eu escolho?', a: 'Use "auto". O gateway tenta do melhor para o mais simples e, se um provedor estiver no limite, passa para o próximo sozinho. Só fixe um modelo (ex.: "gemini/gemini-2.5-flash") se precisar de comportamento previsível; mesmo assim, se ele falhar, o gateway cai para o resto da cadeia.' },
  { q: 'Como sei qual IA respondeu?', a: 'O campo model da resposta vem como "provedor/modelo", e x_diax.attempts lista cada tentativa (ok, rate_limit, unauthorized, timeout, empty_content...) com o tempo em ms.' },
  { q: 'Tem streaming?', a: 'Não. Mande stream: false (ou omita). Respostas de até alguns milhares de tokens voltam em poucos segundos.' },
  { q: 'Posso usar a ServiceApiKey em vez da ProxyApiKey?', a: 'Funciona, mas não use em máquina de terceiros: a ServiceApiKey é administradora do CRM inteiro. A ProxyApiKey só abre os endpoints de IA e é descartável.' },
  { q: 'A rede da empresa bloqueia?', a: 'O app só precisa de HTTPS de saída para api.alexandrequeiroz.com.br (porta 443). Se a empresa usa proxy corporativo, o cliente HTTP deve usar o proxy do sistema (é o padrão no .NET e no requests do Python quando HTTPS_PROXY está definido).' },
];

export default function FreeAiPage() {
  const base = getFreeAiBaseUrl();
  const chatUrl = `${base}/chat/completions`;

  const [proxyKey, setProxyKey] = useState('');
  useEffect(() => {
    try { const k = localStorage.getItem(KEY_STORAGE); if (k) setProxyKey(k); } catch { /* storage bloqueado */ }
  }, []);
  const saveKey = (v: string) => {
    setProxyKey(v);
    try { if (v) localStorage.setItem(KEY_STORAGE, v); else localStorage.removeItem(KEY_STORAGE); } catch { /* idem */ }
  };
  const K = proxyKey || KEY_PLACEHOLDER;

  const [health, setHealth] = useState<FreeAiHealth | null>(null);
  const [healthLoading, setHealthLoading] = useState(false);
  const [healthError, setHealthError] = useState<string | null>(null);
  const loadHealth = async (refresh = false) => {
    setHealthLoading(true); setHealthError(null);
    try { setHealth(await getFreeAiHealth(refresh)); }
    catch (e) { setHealthError(e instanceof Error ? e.message : 'Falha ao consultar.'); }
    finally { setHealthLoading(false); }
  };
  useEffect(() => { loadHealth(); }, []);

  const [system, setSystem] = useState('Você é um engenheiro de prompts. Devolva só o prompt final, em português do Brasil.');
  const [prompt, setPrompt] = useState('Crie um prompt para gerar um post de Instagram sobre uma padaria artesanal.');
  const [testing, setTesting] = useState(false);
  const [testResult, setTestResult] = useState<FreeAiChatResponse | null>(null);
  const [testError, setTestError] = useState<string | null>(null);
  const runTest = async () => {
    setTesting(true); setTestError(null); setTestResult(null);
    try { setTestResult(await testFreeAi(prompt, system)); }
    catch (e) { setTestError(e instanceof Error ? e.message : 'Falha no teste.'); }
    finally { setTesting(false); }
  };

  const pythonCode = `
# pip install openai
import os
from openai import OpenAI

client = OpenAI(
    api_key="ignored",  # a autenticação real vai no header abaixo
    base_url="${base}",
    default_headers={"X-Api-Key": os.environ["DIAX_FREE_AI_KEY"]},  # ${K === KEY_PLACEHOLDER ? 'defina a variável de ambiente' : 'valor colado nesta página'}
    timeout=120,
)

resp = client.chat.completions.create(
    model="auto",
    messages=[
        {"role": "system", "content": "Você é um engenheiro de prompts. Devolva só o prompt final."},
        {"role": "user", "content": "Crie um prompt para resumir atas de reunião em 5 tópicos."},
    ],
    max_tokens=1500,
)
print(resp.choices[0].message.content)
print("respondeu:", resp.model)  # ex.: gemini/gemini-2.5-flash`;

  const csharpCode = `
// .NET 8+ — sem pacote extra (System.Net.Http.Json)
using System.Net.Http.Json;

var http = new HttpClient { BaseAddress = new Uri("${base}/"), Timeout = TimeSpan.FromSeconds(120) };
http.DefaultRequestHeaders.Add("X-Api-Key", Environment.GetEnvironmentVariable("DIAX_FREE_AI_KEY"));

var resp = await http.PostAsJsonAsync("chat/completions", new
{
    model = "auto",
    messages = new[]
    {
        new { role = "system", content = "Você é um engenheiro de prompts. Devolva só o prompt final." },
        new { role = "user", content = "Crie um prompt para revisar código C# com foco em segurança." },
    },
    max_tokens = 1500,
});
resp.EnsureSuccessStatusCode();
var json = await resp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
Console.WriteLine(json.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString());
Console.WriteLine($"respondeu: {json.GetProperty("model").GetString()}");`;

  const jsCode = `
// Node 18+ ou navegador (fetch nativo)
const resp = await fetch("${chatUrl}", {
  method: "POST",
  headers: { "Content-Type": "application/json", "X-Api-Key": process.env.DIAX_FREE_AI_KEY },
  body: JSON.stringify({
    model: "auto",
    messages: [{ role: "user", content: "Crie um prompt para gerar testes unitários de uma função." }],
    max_tokens: 1500,
  }),
});
if (!resp.ok) throw new Error(\`free-ai \${resp.status}: \${await resp.text()}\`);
const data = await resp.json();
console.log(data.choices[0].message.content, "←", data.model);`;

  const curlCode = `
curl ${chatUrl} \\
  -H "X-Api-Key: $DIAX_FREE_AI_KEY" -H "Content-Type: application/json" \\
  -d '{"model":"auto","messages":[{"role":"user","content":"Diga oi em uma frase."}],"max_tokens":200}'

# lista de modelos e status ao vivo
curl ${base}/models -H "X-Api-Key: $DIAX_FREE_AI_KEY"
curl ${base}/health -H "X-Api-Key: $DIAX_FREE_AI_KEY"`;

  const responseExample = `
{
  "id": "free-ai-3f2c...",
  "object": "chat.completion",
  "model": "gemini/gemini-2.5-flash",
  "choices": [{ "index": 0, "message": { "role": "assistant", "content": "..." }, "finish_reason": "stop" }],
  "usage": { "prompt_tokens": 42, "completion_tokens": 310, "total_tokens": 352 },
  "x_diax": {
    "provider": "gemini",
    "model": "gemini-2.5-flash",
    "attempts": [ { "provider": "gemini", "model": "gemini-2.5-flash", "status": "ok", "elapsedMs": 2217 } ]
  }
}`;

  const copilotPrompt = `Quero integrar o meu gerador de prompts a um endpoint de IA gratuita do meu CRM. Use a linguagem, a estrutura de pastas e o estilo que este projeto já usa. Não invente outro framework.

## O endpoint
- URL: POST ${chatUrl}
- Formato: igual à API de Chat Completions da OpenAI (se o projeto já usa um SDK da OpenAI, basta trocar o base_url para ${base} e mandar o header abaixo).
- Autenticação: header "X-Api-Key" com o valor da variável de ambiente DIAX_FREE_AI_KEY. Nunca escreva a chave no código, em arquivo versionado ou em log.
- Corpo: { "model": "auto", "messages": [ { "role": "system" | "user" | "assistant", "content": "..." } ], "max_tokens": 2048, "temperature": 0.7 }
  - "auto" deixa o servidor escolher entre várias IAs gratuitas com fallback automático. Não mande outro valor em model.
  - Limites: 1 a 50 mensagens, até 60.000 caracteres no total, max_tokens de 1 a 8192, sem streaming (não mande "stream": true).
- Resposta de sucesso (200): o texto está em choices[0].message.content. O campo model vem como "provedor/modelo" e x_diax.attempts lista as tentativas.
- Outros endpoints: GET ${base}/models (lista) e GET ${base}/health (status de cada IA), com o mesmo header.

## Erros e o que fazer
- 400: pedido inválido. Mostre a mensagem do campo "error" e não repita.
- 401/403: chave ausente ou errada. Mostre uma mensagem clara pedindo para configurar DIAX_FREE_AI_KEY.
- 429: limite de 60 requisições por minuto. Espere 60 segundos e tente uma única vez.
- 503: todas as IAs gratuitas falharam agora (o corpo traz "attempts"). Espere 30 segundos e tente mais uma vez; se falhar de novo, mostre o erro ao usuário.
- Timeout de 120 segundos por chamada. Falha de rede: 1 nova tentativa após 5 segundos.

## O que implementar
1. Um cliente pequeno e isolado (classe/módulo) com uma função do tipo generatePrompt(instrucoes, contexto) que monta as mensagens, chama o endpoint e devolve o texto.
2. A mensagem de sistema deve pedir: "Você é um engenheiro de prompts sênior. A partir do pedido do usuário, escreva um prompt claro, específico e pronto para colar em uma IA: papel, objetivo, contexto, regras, formato de saída e exemplos quando ajudarem. Devolva somente o prompt final, sem comentários." Deixe esse texto configurável.
3. Configuração por variáveis de ambiente/arquivo de config: DIAX_FREE_AI_URL (padrão ${chatUrl}) e DIAX_FREE_AI_KEY.
4. Registre em log só o provedor que respondeu (campo model), o tempo e o tamanho do texto. Nunca o conteúdo do prompt nem a chave.
5. Testes unitários do cliente com o HTTP simulado: sucesso, 400, 429 com nova tentativa, 503 com nova tentativa e falha final.
6. Se a rede da empresa usar proxy corporativo, use o proxy do sistema (não fixe proxy no código).

Antes de codar, me mostre em poucas linhas onde vai mexer. Depois implemente, rode os testes e me diga como configurar a variável DIAX_FREE_AI_KEY no Windows (setx) e no terminal atual.`;

  const [faqOpen, setFaqOpen] = useState<number | null>(null);

  return (
    <div style={{ maxWidth: 860, fontFamily: 'var(--font-jakarta, "Plus Jakarta Sans", sans-serif)' }}>
      {/* Cabeçalho */}
      <div style={{ marginBottom: 28 }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 8, flexWrap: 'wrap' }}>
          <Sparkles size={22} color="#10B981" />
          <h1 style={{ margin: 0, fontSize: 22, fontWeight: 800, color: '#F9FAFB' }}>IA Grátis (API)</h1>
          <span style={{ padding: '2px 8px', borderRadius: 6, fontSize: 10, fontWeight: 700, background: 'rgba(16,185,129,0.15)', color: '#10B981', border: '1px solid rgba(16,185,129,0.3)' }}>OpenAI-compatível</span>
        </div>
        <p style={{ margin: 0, fontSize: 14, color: '#6B7280', lineHeight: 1.7, maxWidth: 680 }}>
          Um endpoint para apps de fora do CRM (como o gerador de prompts do notebook da empresa) usarem só as IAs
          gratuitas configuradas aqui, com fallback automático entre provedores. Custo zero: modelos pagos são recusados.
        </p>
      </div>

      {/* Status */}
      <Section icon={Activity} title="Status das IAs agora">
        <div style={{ display: 'flex', gap: 8, marginBottom: 12, flexWrap: 'wrap', alignItems: 'center' }}>
          <button type="button" onClick={() => loadHealth(true)} disabled={healthLoading} style={{
            display: 'flex', alignItems: 'center', gap: 6, padding: '7px 12px', borderRadius: 8, fontSize: 12, fontWeight: 600,
            background: 'rgba(16,185,129,0.12)', border: '1px solid rgba(16,185,129,0.3)', color: '#10B981', cursor: 'pointer',
          }}>
            {healthLoading ? <Loader2 size={13} className="animate-spin" /> : <RefreshCw size={13} />}
            Testar todas agora
          </button>
          {health && (
            <span style={{ fontSize: 12, color: '#9CA3AF' }}>
              <b style={{ color: health.ok > 0 ? '#10B981' : '#EF4444' }}>{health.ok}/{health.total}</b> respondendo
              {' · '}verificado {new Date(health.checkedAt).toLocaleTimeString('pt-BR')}{health.cached ? ' (cache de até 60 s)' : ''}
            </span>
          )}
        </div>
        {healthError && (
          <div style={{ ...CARD, borderColor: 'rgba(239,68,68,0.3)', color: '#FCA5A5', fontSize: 12.5, display: 'flex', gap: 8, alignItems: 'center' }}>
            <AlertCircle size={14} /> {healthError}
          </div>
        )}
        {health && (
          <div style={{ display: 'grid', gap: 6 }}>
            {health.items.map(i => (
              <div key={`${i.provider}/${i.model}`} style={{ ...CARD, padding: '9px 12px', display: 'flex', alignItems: 'center', gap: 10, flexWrap: 'wrap' }}>
                <span style={{ width: 8, height: 8, borderRadius: 99, background: i.ok ? '#10B981' : '#EF4444', flexShrink: 0 }} />
                <code style={{ fontSize: 12, color: '#E5E7EB', flex: 1, minWidth: 0, wordBreak: 'break-all' }}>{i.provider}/{i.model}</code>
                <span style={{ fontSize: 11, color: i.ok ? '#6EE7B7' : '#FCA5A5' }}>{i.ok ? 'ok' : (i.error ?? 'falhou')}</span>
                <span style={{ fontSize: 11, color: '#6B7280', minWidth: 56, textAlign: 'right' }}>{i.elapsedMs} ms</span>
              </div>
            ))}
          </div>
        )}
        <p style={{ ...P, marginTop: 10, fontSize: 12 }}>
          A ordem da lista é a ordem do fallback. Um item vermelho não derruba o serviço: o pedido segue para o próximo.
        </p>
      </Section>

      {/* Teste */}
      <Section icon={Play} title="Testar um prompt aqui" color="#3B82F6">
        <p style={P}>Usa o seu login do CRM (não precisa de chave). É a mesma chamada que o app externo faz.</p>
        <label style={{ fontSize: 11, color: '#6B7280', fontWeight: 600 }}>Mensagem de sistema</label>
        <textarea value={system} onChange={e => setSystem(e.target.value)} rows={2} style={{
          width: '100%', marginTop: 4, marginBottom: 10, padding: 10, borderRadius: 8, fontSize: 12.5, resize: 'vertical',
          background: 'rgba(0,0,0,0.3)', border: '1px solid rgba(255,255,255,0.1)', color: '#E5E7EB',
        }} />
        <label style={{ fontSize: 11, color: '#6B7280', fontWeight: 600 }}>Pedido</label>
        <textarea value={prompt} onChange={e => setPrompt(e.target.value)} rows={3} style={{
          width: '100%', marginTop: 4, marginBottom: 10, padding: 10, borderRadius: 8, fontSize: 12.5, resize: 'vertical',
          background: 'rgba(0,0,0,0.3)', border: '1px solid rgba(255,255,255,0.1)', color: '#E5E7EB',
        }} />
        <button type="button" onClick={runTest} disabled={testing || !prompt.trim()} style={{
          display: 'flex', alignItems: 'center', gap: 6, padding: '8px 14px', borderRadius: 8, fontSize: 12.5, fontWeight: 600,
          background: '#2563EB', border: 'none', color: 'white', cursor: 'pointer', opacity: testing || !prompt.trim() ? 0.6 : 1,
        }}>
          {testing ? <Loader2 size={13} className="animate-spin" /> : <Play size={13} />}
          Enviar (model: auto)
        </button>
        {testError && (
          <div style={{ ...CARD, marginTop: 12, borderColor: 'rgba(239,68,68,0.3)', color: '#FCA5A5', fontSize: 12.5 }}>{testError}</div>
        )}
        {testResult && (
          <div style={{ marginTop: 12 }}>
            <div style={{ fontSize: 12, color: '#9CA3AF', marginBottom: 6 }}>
              Respondeu: <code style={{ color: '#10B981' }}>{testResult.model}</code>
              {' · '}{testResult.usage.total_tokens} tokens
              {' · '}tentativas: {testResult.x_diax.attempts.map(a => `${a.provider} ${a.status}`).join(' → ')}
            </div>
            <CodeBlock code={testResult.choices[0]?.message.content ?? ''} lang="resposta" />
          </div>
        )}
      </Section>

      {/* Chave */}
      <Section icon={Key} title="1. A chave do app externo (ProxyApiKey)" color="#F59E0B">
        <p style={P}>
          O app fora do CRM se autentica com a <b>ProxyApiKey</b> no header <code>X-Api-Key</code>. Ela só abre os endpoints de IA
          (no resto do CRM dá 403) e fica guardada em <code>DIAX_PROXY_API_KEY</code> no seu <code>.secrets.env</code>.
          No notebook, salve numa variável de ambiente (os exemplos usam <code>DIAX_FREE_AI_KEY</code>):
        </p>
        <CodeBlock lang="powershell" code={`setx DIAX_FREE_AI_KEY "${K}"\n# feche e abra o terminal/IDE para a variável valer`} />
        <label style={{ fontSize: 11, color: '#6B7280', fontWeight: 600 }}>Opcional: cole a chave para os exemplos saírem prontos (fica só neste navegador)</label>
        <input type="password" value={proxyKey} onChange={e => saveKey(e.target.value.trim())} placeholder={KEY_PLACEHOLDER}
          autoComplete="off" spellCheck={false} style={{
            width: '100%', marginTop: 4, padding: '9px 11px', borderRadius: 8, fontSize: 12.5, fontFamily: 'monospace',
            background: 'rgba(0,0,0,0.3)', border: '1px solid rgba(255,255,255,0.1)', color: '#E5E7EB',
          }} />
      </Section>

      {/* Endereços */}
      <Section icon={Globe} title="2. Endereços">
        <InfoRow label="Base (SDK OpenAI)" value={base} />
        <InfoRow label="Chat" value={`POST ${chatUrl}`} />
        <InfoRow label="Modelos" value={`GET ${base}/models`} />
        <InfoRow label="Status" value={`GET ${base}/health`} />
      </Section>

      {/* Exemplos */}
      <Section icon={Code2} title="3. Exemplos" color="#8B5CF6">
        <CodeBlock lang="python" code={pythonCode} />
        <CodeBlock lang="c# / .net" code={csharpCode} />
        <CodeBlock lang="javascript" code={jsCode} />
        <CodeBlock lang="curl" code={curlCode} />
        <p style={P}>Resposta (formato OpenAI + o campo extra <code>x_diax</code>):</p>
        <CodeBlock lang="json" code={responseExample} />
      </Section>

      {/* Prompt Copilot */}
      <Section icon={Bot} title="4. Prompt para colar no Copilot (programa da empresa)" color="#EC4899">
        <p style={P}>
          Abra o projeto do gerador de prompts no VS Code/Visual Studio, cole no Copilot Chat (modo Agent ou Edits) e ele
          integra o endpoint seguindo o estilo do projeto. O prompt não contém a chave.
        </p>
        <div style={{ ...CARD, borderColor: 'rgba(236,72,153,0.3)' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 8, gap: 8 }}>
            <span style={{ fontSize: 12, color: '#F9A8D4', fontWeight: 700 }}>Prompt de integração</span>
            <CopyButton text={copilotPrompt} label="Copiar prompt" />
          </div>
          <pre style={{
            margin: 0, maxHeight: 340, overflow: 'auto', whiteSpace: 'pre-wrap', fontSize: 12, lineHeight: 1.65, color: '#E5E7EB',
            fontFamily: 'Consolas, "Cascadia Code", monospace',
          }}>{copilotPrompt}</pre>
        </div>
      </Section>

      {/* Como funciona */}
      <Section icon={Layers} title="5. Fallback: como funciona" color="#3B82F6">
        <p style={P}>
          Com <code>model: &quot;auto&quot;</code> o gateway tenta a lista do topo para baixo (a mesma ordem do status acima),
          intercalando provedores para que o limite de um não derrube os seguintes:
        </p>
        <ul style={{ ...P, paddingLeft: 18 }}>
          <li><b>429/402</b> (limite ou sem crédito): o provedor fica 60 s de fora e o pedido segue para o próximo.</li>
          <li><b>401, 404, 5xx, timeout (30 s), resposta vazia</b>: segue para o próximo, sem pausa.</li>
          <li><b>Todos falharam</b>: 503 com <code>attempts</code> explicando cada um.</li>
          <li>Um <code>model</code> específico (ex.: <code>groq/qwen/qwen3.8-27b</code>) começa por ele e cai para o resto se falhar. Fora da lista de <code>/models</code> = 400.</li>
        </ul>
      </Section>

      {/* Erros */}
      <Section icon={AlertCircle} title="6. Códigos de erro" color="#EF4444">
        <div style={{ display: 'grid', gap: 6 }}>
          {ERRORS.map(e => (
            <div key={e.code} style={{ ...CARD, padding: '10px 12px', display: 'flex', gap: 12, alignItems: 'flex-start', flexWrap: 'wrap' }}>
              <code style={{ fontSize: 13, fontWeight: 800, color: '#FCA5A5', minWidth: 34 }}>{e.code}</code>
              <span style={{ fontSize: 12.5, color: '#D1D5DB', flex: 1, minWidth: 200 }}>{e.meaning}</span>
              <span style={{ fontSize: 12, color: '#9CA3AF', minWidth: 160 }}>{e.action}</span>
            </div>
          ))}
        </div>
      </Section>

      {/* Limites e segurança */}
      <Section icon={ShieldAlert} title="7. Limites e segurança" color="#F59E0B">
        <ul style={{ ...P, paddingLeft: 18 }}>
          <li>60 requisições por minuto por IP. Até 50 mensagens e 60.000 caracteres por pedido; <code>max_tokens</code> de 1 a 8192 (padrão 2048).</li>
          <li>Nada do conteúdo dos prompts é gravado em log no servidor, só tamanhos, provedor e tempo.</li>
          <li>Nunca coloque a chave no código ou em repositório. Se vazar, troque a <code>ProxyApiKey</code> no servidor: ela não dá acesso a dados do CRM.</li>
        </ul>
      </Section>

      {/* FAQ */}
      <Section icon={BookOpen} title="8. Perguntas frequentes" color="#8B5CF6">
        <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
          {FAQ.map((f, i) => (
            <div key={f.q} style={{ borderRadius: 9, border: '1px solid rgba(255,255,255,0.07)', background: 'rgba(255,255,255,0.02)' }}>
              <button type="button" onClick={() => setFaqOpen(faqOpen === i ? null : i)} style={{
                width: '100%', padding: '12px 14px', display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 12,
                background: 'none', border: 'none', cursor: 'pointer', textAlign: 'left',
              }}>
                <span style={{ fontSize: 13, fontWeight: 600, color: '#E5E7EB' }}>{f.q}</span>
                {faqOpen === i ? <ChevronDown size={14} color="#6B7280" /> : <ChevronRight size={14} color="#6B7280" />}
              </button>
              {faqOpen === i && <div style={{ padding: '0 14px 12px', fontSize: 12.5, color: '#9CA3AF', lineHeight: 1.7 }}>{f.a}</div>}
            </div>
          ))}
        </div>
      </Section>

      <div style={{ ...CARD, display: 'flex', gap: 8, alignItems: 'center', fontSize: 12, color: '#6B7280' }}>
        <Settings size={13} /> Cadeia configurável no servidor em <code>FreeAi:Chain</code> (sem config, vale a lista padrão do código).
      </div>
    </div>
  );
}
