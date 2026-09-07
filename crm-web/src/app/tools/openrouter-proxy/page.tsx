'use client';

import { useState } from 'react';
import { ServiceKeyField, useServiceKey } from '@/components/ServiceKeyField';
import { Copy, Check, Terminal, Code2, Cpu, Globe, Key, Zap, BookOpen, AlertCircle, ChevronDown, ChevronRight, Settings, ShieldAlert, DollarSign, Layers, Laptop } from 'lucide-react';

const PROXY_URL = 'https://api.alexandrequeiroz.com.br/openrouter';
const PROXY_BASE_V1 = `${PROXY_URL}/v1`;
const PROXY_CHAT_URL = `${PROXY_BASE_V1}/chat/completions`;

function CopyButton({ text }: { text: string }) {
  const [copied, setCopied] = useState(false);
  const handleCopy = async () => {
    await navigator.clipboard.writeText(text);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };
  return (
    <button onClick={handleCopy} style={{
      display: 'flex', alignItems: 'center', gap: 4,
      padding: '4px 10px', borderRadius: 6,
      background: copied ? 'rgba(16,185,129,0.2)' : 'rgba(255,255,255,0.07)',
      border: `1px solid ${copied ? 'rgba(16,185,129,0.4)' : 'rgba(255,255,255,0.1)'}`,
      color: copied ? '#10B981' : '#9CA3AF', fontSize: 11, fontWeight: 600,
      cursor: 'pointer', transition: 'all .15s', whiteSpace: 'nowrap',
    }}>
      {copied ? <Check size={11} /> : <Copy size={11} />}
      {copied ? 'Copiado!' : 'Copiar'}
    </button>
  );
}

function CodeBlock({ code, lang = 'python' }: { code: string; lang?: string }) {
  return (
    <div style={{
      position: 'relative', borderRadius: 10,
      background: '#080E0A', border: '1px solid rgba(255,255,255,0.08)',
      overflow: 'hidden',
    }}>
      <div style={{
        display: 'flex', alignItems: 'center', justifyContent: 'space-between',
        padding: '8px 14px', borderBottom: '1px solid rgba(255,255,255,0.06)',
        background: 'rgba(255,255,255,0.02)',
      }}>
        <span style={{ fontSize: 11, color: '#6B7280', fontWeight: 600, textTransform: 'uppercase', letterSpacing: '.05em' }}>{lang}</span>
        <CopyButton text={code.trim()} />
      </div>
      <pre style={{
        margin: 0, padding: '16px 18px', overflowX: 'auto',
        fontSize: 12.5, lineHeight: 1.75, color: '#D1FAE5',
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
          width: 32, height: 32, borderRadius: 9,
          background: `${color}20`, border: `1px solid ${color}40`,
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

function InfoRow({ label, value, copyValue }: { label: string; value: string; copyValue?: string }) {
  return (
    <div style={{
      display: 'flex', alignItems: 'center', gap: 12,
      padding: '10px 14px', borderRadius: 8,
      background: 'rgba(255,255,255,0.03)', border: '1px solid rgba(255,255,255,0.07)',
      marginBottom: 6,
    }}>
      <span style={{ fontSize: 12, color: '#6B7280', fontWeight: 600, minWidth: 100, flexShrink: 0 }}>{label}</span>
      <code style={{ flex: 1, fontSize: 12, color: '#10B981', fontFamily: 'monospace', wordBreak: 'break-all' }}>{value}</code>
      {copyValue && <CopyButton text={copyValue} />}
    </div>
  );
}

function Accordion({ items, expanded, onToggle }: {
  items: { q: string; a: React.ReactNode }[];
  expanded: number | null;
  onToggle: (i: number | null) => void;
}) {
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
      {items.map((item, i) => (
        <div key={i} style={{
          borderRadius: 9, border: '1px solid rgba(255,255,255,0.07)',
          overflow: 'hidden', background: 'rgba(255,255,255,0.02)',
        }}>
          <button
            onClick={() => onToggle(expanded === i ? null : i)}
            style={{
              width: '100%', padding: '12px 14px',
              display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 12,
              background: 'none', border: 'none', cursor: 'pointer', textAlign: 'left',
            }}
          >
            <span style={{ fontSize: 13, fontWeight: 600, color: '#E5E7EB' }}>{item.q}</span>
            {expanded === i
              ? <ChevronDown size={14} color="#6B7280" style={{ flexShrink: 0 }} />
              : <ChevronRight size={14} color="#6B7280" style={{ flexShrink: 0 }} />
            }
          </button>
          {expanded === i && (
            <div style={{
              padding: '0 14px 12px',
              fontSize: 12.5, color: '#9CA3AF', lineHeight: 1.7,
            }}>{item.a}</div>
          )}
        </div>
      ))}
    </div>
  );
}

/** Modelos verificados no catálogo real da OpenRouter em 07/09/2026 via GET /openrouter/v1/models. */
const MODELOS = [
  {
    slug: 'z-ai/glm-5.3-flash',
    nome: 'GLM 5.3 Flash',
    badge: 'Melhor custo-benefício',
    cor: '#10B981',
    ctx: '1.310.720',
    pin: 0.07, pout: 0.25, tipico: 0.12,
    nota: 'Mesma família do GLM 5.3, 19× mais barato e com o MESMO contexto de 1,3M. É o melhor negócio do catálogo inteiro: custa menos que modelos pequenos e entrega contexto de topo de linha. Use como padrão e só suba quando a tarefa provar que precisa.',
  },
  {
    slug: 'z-ai/glm-5.3',
    nome: 'GLM 5.3',
    badge: 'Pedido',
    cor: '#6366F1',
    ctx: '1.310.720',
    pin: 1.40, pout: 4.40, tipico: 2.28,
    nota: 'O maior contexto do catálogo (1,3M) com qualidade de ponta. Vale quando a tarefa exige raciocínio pesado sobre muito material — análise de base de código inteira, documentos longos. Para conversa comum, o Flash acima entrega quase o mesmo por 1/19 do preço.',
  },
  {
    slug: 'moonshotai/kimi-k3',
    nome: 'Kimi K3',
    badge: 'Pedido',
    cor: '#F59E0B',
    ctx: '1.048.576',
    pin: 3.00, pout: 15.00, tipico: 6.00,
    nota: 'Contexto de 1M e reputação forte em código e agentes. Preço equivale ao Claude Sonnet 4.6 (mesmos $3/$15). Existe a variante :batch, mesmo preço, para trabalho assíncrono sem pressa.',
  },
  {
    slug: 'openai/gpt-5.6-luna',
    nome: 'GPT-5.6 Luna',
    badge: 'Melhor da OpenAI por preço',
    cor: '#10B981',
    ctx: '1.050.000',
    pin: 0.20, pout: 1.20, tipico: 0.44,
    nota: 'Modelo de ponta da OpenAI a 1/14 do preço do Kimi K3, com contexto equivalente. Melhor opção quando você quer especificamente o comportamento dos modelos da OpenAI.',
  },
  {
    slug: 'google/gemini-2.5-flash-lite',
    nome: 'Gemini 2.5 Flash Lite',
    badge: 'Volume alto',
    cor: '#3B82F6',
    ctx: '1.048.576',
    pin: 0.10, pout: 0.40, tipico: 0.18,
    nota: 'Barato, rápido e com 1M de contexto. Bom para classificação em massa, extração e tarefas repetitivas onde latência importa mais que profundidade.',
  },
  {
    slug: 'deepseek/deepseek-v3.2',
    nome: 'DeepSeek V3.2',
    badge: 'Raciocínio',
    cor: '#8B5CF6',
    ctx: '163.840',
    pin: 0.27, pout: 0.40, tipico: 0.35,
    nota: 'Custo de saída excepcionalmente baixo ($0,40/1M), o que o torna ideal para respostas longas. Forte em código e matemática. Contexto menor que os demais — não use para documentos gigantes.',
  },
  {
    slug: 'qwen/qwen3-235b-a22b-2507',
    nome: 'Qwen3 235B',
    badge: 'Entrada barata',
    cor: '#3B82F6',
    ctx: '262.144',
    pin: 0.09, pout: 0.55, tipico: 0.20,
    nota: 'Entrada a $0,09/1M — dos mais baratos para engolir muito texto. Bom quando você manda muito contexto e espera resposta curta.',
  },
  {
    slug: 'meta-llama/llama-4-scout',
    nome: 'Llama 4 Scout',
    badge: 'Contexto gigante barato',
    cor: '#3B82F6',
    ctx: '1.310.720',
    pin: 0.10, pout: 0.30, tipico: 0.16,
    nota: '1,3M de contexto por $0,16 típico. Alternativa aberta ao GLM 5.3 Flash quando você quer um modelo Meta.',
  },
  {
    slug: 'x-ai/grok-4.20',
    nome: 'Grok 4.20',
    badge: 'Maior contexto',
    cor: '#6366F1',
    ctx: '2.000.000',
    pin: 1.25, pout: 2.50, tipico: 1.75,
    nota: 'O maior contexto disponível: 2M de tokens. Saída mais barata que Kimi K3 e GLM 5.3. Existe variante multi-agent pelo mesmo preço.',
  },
  {
    slug: 'anthropic/claude-opus-4.7',
    nome: 'Claude Opus 4.7',
    badge: 'Mais caro',
    cor: '#EF4444',
    ctx: '1.000.000',
    pin: 5.00, pout: 25.00, tipico: 10.00,
    nota: 'Referência de qualidade máxima, e o preço reflete isso: 83× o GLM 5.3 Flash. Para Claude, use o proxy da Anthropic (página vizinha) em vez deste — lá você fala direto com a API oficial.',
  },
];

export default function OpenRouterProxyPage() {
  const [expanded, setExpanded] = useState<number | null>(null);
  const { serviceKey, setServiceKey, keyOrPlaceholder: K } = useServiceKey();

  const pythonCode = `
from openai import OpenAI

client = OpenAI(
    api_key="${K}",          # service key do CRM, nao a chave da OpenRouter
    base_url="${PROXY_BASE_V1}",
)

resp = client.chat.completions.create(
    model="z-ai/glm-5.3-flash",
    messages=[{"role": "user", "content": "Explique o que e um proxy de API."}],
)
print(resp.choices[0].message.content)
`;

  const streamCode = `
stream = client.chat.completions.create(
    model="moonshotai/kimi-k3",
    messages=[{"role": "user", "content": "Escreva um haiku sobre codigo."}],
    stream=True,
)

for chunk in stream:
    delta = chunk.choices[0].delta.content
    if delta:
        print(delta, end="", flush=True)
`;

  const curlCode = `
curl ${PROXY_CHAT_URL} \\
  -H "Authorization: Bearer ${K}" \\
  -H "Content-Type: application/json" \\
  -d '{
    "model": "z-ai/glm-5.3",
    "messages": [{"role": "user", "content": "Ola!"}]
  }'
`;

  const nodeCode = `
import OpenAI from "openai";

const client = new OpenAI({
  apiKey: "${K}",          // service key do CRM
  baseURL: "${PROXY_BASE_V1}",
});

const resp = await client.chat.completions.create({
  model: "z-ai/glm-5.3-flash",
  messages: [{ role: "user", content: "Ola!" }],
});
console.log(resp.choices[0].message.content);
`;

  const claudeCodeSessionCode = `
$env:ANTHROPIC_BASE_URL           = "${PROXY_URL}"
$env:ANTHROPIC_API_KEY            = "${K}"
$env:ANTHROPIC_MODEL              = "z-ai/glm-5.3"
$env:ANTHROPIC_DEFAULT_HAIKU_MODEL = "z-ai/glm-5.3-flash"
$env:ANTHROPIC_SMALL_FAST_MODEL   = "z-ai/glm-5.3-flash"
$env:CLAUDE_CODE_MAX_CONTEXT_TOKENS = "1310720"

claude
`;

  const claudeCodePersistCode = `
[System.Environment]::SetEnvironmentVariable("ANTHROPIC_BASE_URL", "${PROXY_URL}", "User")
[System.Environment]::SetEnvironmentVariable("ANTHROPIC_API_KEY", "${K}", "User")
[System.Environment]::SetEnvironmentVariable("ANTHROPIC_MODEL", "z-ai/glm-5.3", "User")
[System.Environment]::SetEnvironmentVariable("ANTHROPIC_DEFAULT_HAIKU_MODEL", "z-ai/glm-5.3-flash", "User")
[System.Environment]::SetEnvironmentVariable("ANTHROPIC_SMALL_FAST_MODEL", "z-ai/glm-5.3-flash", "User")
[System.Environment]::SetEnvironmentVariable("CLAUDE_CODE_MAX_CONTEXT_TOKENS", "1310720", "User")
`;

  const claudeCodeSettingsCode = `
{
  "model": "z-ai/glm-5.3",
  "env": {
    "ANTHROPIC_BASE_URL": "${PROXY_URL}",
    "ANTHROPIC_API_KEY": "${K}",
    "ANTHROPIC_MODEL": "z-ai/glm-5.3",
    "ANTHROPIC_DEFAULT_HAIKU_MODEL": "z-ai/glm-5.3-flash",
    "ANTHROPIC_SMALL_FAST_MODEL": "z-ai/glm-5.3-flash",
    "CLAUDE_CODE_MAX_CONTEXT_TOKENS": "1310720"
  }
}
`;

  const claudeCodeRevertCode = `
$vars = "ANTHROPIC_BASE_URL","ANTHROPIC_API_KEY","ANTHROPIC_MODEL",
        "ANTHROPIC_DEFAULT_HAIKU_MODEL","ANTHROPIC_SMALL_FAST_MODEL",
        "CLAUDE_CODE_MAX_CONTEXT_TOKENS"
foreach ($v in $vars) {
  Remove-Item "Env:$v" -ErrorAction SilentlyContinue
  [System.Environment]::SetEnvironmentVariable($v, $null, "User")
}
# feche o terminal e abra de novo
`;

  const modelsCode = `
# Catalogo completo (430+ modelos) e precos ao vivo
curl ${PROXY_BASE_V1}/models -H "Authorization: Bearer ${K}"

# Saldo restante da conta OpenRouter
curl ${PROXY_BASE_V1}/credits -H "Authorization: Bearer ${K}"
`;

  const faq = [
    {
      q: 'Estou recebendo 401 invalid_token. O que fiz de errado?',
      a: 'Quase sempre é a chave errada: foi enviada a chave da OpenRouter (sk-or-v1-...) em vez da Service API Key do CRM. O proxy não aceita a chave da OpenRouter vinda do cliente — ela fica guardada no servidor justamente para não circular. Cole a Service API Key no campo do topo desta página e recopie os exemplos.',
    },
    {
      q: 'Funciona no Cursor, Continue, LangChain e afins?',
      a: 'Sim. Qualquer ferramenta que aceite uma base_url customizada da OpenAI funciona sem configuração extra: aponte a base URL para o endereço do proxy e coloque a Service API Key no campo de API key. Não é preciso header customizado — o servidor aceita a chave em Authorization: Bearer, que é o único formato que essas ferramentas sabem mandar.',
    },
    {
      q: 'Preciso de uma chave da OpenRouter?',
      a: 'Não. A chave da OpenRouter fica no servidor. Você usa apenas a Service API Key do CRM no header X-Api-Key, e o proxy injeta a chave real antes de encaminhar.',
    },
    {
      q: 'Por que os modelos com sufixo :free dão erro 404?',
      a: 'Porque esta conta tem crédito comprado e por isso não é free tier (is_free_tier: false). A OpenRouter reserva as variantes :free para contas sem crédito. A mensagem de erro dela sugere o slug pago equivalente. Na prática isso importa pouco: o GLM 5.3 Flash custa $0,12 por 1M de entrada mais 200k de saída, ou seja, centavos.',
    },
    {
      q: 'Dá para usar no Claude Code CLI?',
      a: 'Dá, e foi testado de ponta a ponta — o CLI respondeu, chamou ferramenta e escreveu arquivo em disco através deste proxy. A seção 7 tem a configuração pronta. Funciona porque a OpenRouter também expõe um endpoint no formato da Anthropic (/v1/messages), que é o único que o Claude Code fala. Atenção a dois pontos: a ANTHROPIC_BASE_URL termina em /openrouter, sem o /v1, e é obrigatório definir ANTHROPIC_MODEL com um slug da OpenRouter, senão o CLI cai no modelo padrão da conta e pede login.',
    },
    {
      q: 'Qual a diferença para o proxy da Anthropic?',
      a: 'O proxy da Anthropic fala o formato da Anthropic (/v1/messages, SDK anthropic). Este fala o formato da OpenAI (/v1/chat/completions, SDK openai) e dá acesso a 430+ modelos de dezenas de laboratórios por uma única chave. Para usar Claude, prefira a página vizinha.',
    },
    {
      q: 'Streaming funciona?',
      a: 'Sim, testado em produção. Basta enviar stream: true — o proxy detecta e repassa os eventos SSE em tempo real, com buffering desabilitado. Se viesse bufferizado, a resposta chegaria toda de uma vez no final.',
    },
    {
      q: 'Como sei quanto já gastei?',
      a: 'GET /openrouter/v1/credits devolve total_credits e total_usage da conta. Cada resposta de chat também traz o campo usage.cost com o custo exato daquela chamada.',
    },
    {
      q: 'Dá para usar com ferramentas que esperam a API da OpenAI?',
      a: 'Sim, essa é a vantagem. Qualquer coisa que aceite base_url customizada da OpenAI funciona: SDK oficial, LangChain, LlamaIndex, Cursor, Continue, etc. Aponte a base_url para o endereço do proxy e mande a Service API Key no header.',
    },
    {
      q: 'Existe limite de tempo para respostas longas?',
      a: 'Não pelo lado do proxy — o HttpClient usa timeout infinito justamente para não cortar streams longos. O limite prático é o do modelo e o da sua conexão.',
    },
  ];

  return (
    <div style={{ maxWidth: 820, fontFamily: 'var(--font-jakarta, "Plus Jakarta Sans", sans-serif)' }}>

      {/* Header */}
      <div style={{ marginBottom: 32 }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 8, marginBottom: 8 }}>
          <div style={{
            padding: '3px 10px', borderRadius: 20,
            background: 'rgba(16,185,129,0.12)', border: '1px solid rgba(16,185,129,0.25)',
            fontSize: 11, fontWeight: 700, color: '#10B981', textTransform: 'uppercase', letterSpacing: '.08em',
          }}>Ferramentas · IA</div>
        </div>
        <h1 style={{ margin: '0 0 8px', fontSize: 26, fontWeight: 800, color: '#F9FAFB', letterSpacing: '-.02em' }}>
          OpenRouter Proxy
        </h1>
        <p style={{ margin: 0, fontSize: 14, color: '#6B7280', lineHeight: 1.7, maxWidth: 620 }}>
          Um endpoint, 430+ modelos. Fala o formato da OpenAI, então funciona com qualquer SDK ou
          ferramenta que aceite <code style={{ background: 'rgba(255,255,255,0.06)', padding: '1px 5px', borderRadius: 4, fontSize: 12, color: '#9CA3AF' }}>base_url</code> customizada —
          incluindo Kimi K3 e GLM 5.3.
        </p>
      </div>

      {/* Chave de serviço */}
      <Section icon={Key} title="1. Configure sua Service API Key" color="#F59E0B">
        <ServiceKeyField
          serviceKey={serviceKey}
          setServiceKey={setServiceKey}
          hint="Sem a chave, os exemplos mostram SUA_SERVICE_API_KEY como marcador. É a chave de serviço do CRM (config ServiceApiKey no servidor) — NÃO é a sua chave sk-or-v1-... da OpenRouter, que nunca deve sair do servidor."
        />
      </Section>

      {/* Endpoint */}
      <Section icon={Globe} title="2. Endereços do proxy">
        <InfoRow label="Base URL" value={PROXY_BASE_V1} copyValue={PROXY_BASE_V1} />
        <InfoRow label="Chat" value={PROXY_CHAT_URL} copyValue={PROXY_CHAT_URL} />
        <InfoRow label="Messages" value={`${PROXY_BASE_V1}/messages`} copyValue={`${PROXY_BASE_V1}/messages`} />
        <InfoRow label="Catálogo" value={`${PROXY_BASE_V1}/models`} copyValue={`${PROXY_BASE_V1}/models`} />
        <InfoRow label="Saldo" value={`${PROXY_BASE_V1}/credits`} copyValue={`${PROXY_BASE_V1}/credits`} />
        <div style={{
          marginTop: 12, padding: '10px 14px', borderRadius: 8,
          background: 'rgba(59,130,246,0.08)', border: '1px solid rgba(59,130,246,0.2)',
          fontSize: 12, color: '#93C5FD', lineHeight: 1.6,
        }}>
          A autenticação aceita a Service API Key em <code>Authorization: Bearer</code> (o formato que
          todo cliente compatível com a OpenAI usa) ou no header <code>X-Api-Key</code>. Um JWT de
          login do CRM também funciona no Bearer — o servidor distingue os dois pelo formato.
        </div>
      </Section>

      {/* Modelos */}
      <Section icon={Cpu} title="3. Modelos e custos" color="#6366F1">
        <p style={{ margin: '0 0 14px', fontSize: 13, color: '#9CA3AF', lineHeight: 1.7 }}>
          Preços verificados no catálogo ao vivo em 07/09/2026. A coluna <strong style={{ color: '#E5E7EB' }}>típico</strong> é
          o custo de uma tarefa real com 1M de tokens de entrada e 200 mil de saída — é o número que
          importa na prática, porque saída costuma custar de 3 a 5 vezes mais que entrada.
        </p>

        <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
          {MODELOS.map((m) => (
            <div key={m.slug} style={{
              padding: '14px 16px', borderRadius: 10,
              background: 'rgba(255,255,255,0.02)',
              border: `1px solid ${m.cor}30`,
            }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: 10, flexWrap: 'wrap', marginBottom: 8 }}>
                <code style={{ fontSize: 13, fontWeight: 700, color: m.cor, fontFamily: 'monospace' }}>{m.slug}</code>
                <span style={{
                  padding: '2px 8px', borderRadius: 20, fontSize: 10, fontWeight: 700,
                  background: `${m.cor}18`, border: `1px solid ${m.cor}35`, color: m.cor,
                  textTransform: 'uppercase', letterSpacing: '.05em',
                }}>{m.badge}</span>
                <CopyButton text={m.slug} />
              </div>
              <div style={{ display: 'flex', gap: 18, flexWrap: 'wrap', marginBottom: 8, fontSize: 11.5, color: '#6B7280' }}>
                <span>entrada <strong style={{ color: '#D1D5DB' }}>${m.pin.toFixed(2)}</strong>/1M</span>
                <span>saída <strong style={{ color: '#D1D5DB' }}>${m.pout.toFixed(2)}</strong>/1M</span>
                <span>contexto <strong style={{ color: '#D1D5DB' }}>{m.ctx}</strong></span>
                <span>típico <strong style={{ color: m.cor }}>${m.tipico.toFixed(2)}</strong></span>
              </div>
              <p style={{ margin: 0, fontSize: 12.5, color: '#9CA3AF', lineHeight: 1.65 }}>{m.nota}</p>
            </div>
          ))}
        </div>

        <div style={{
          marginTop: 14, padding: '12px 14px', borderRadius: 8,
          background: 'rgba(16,185,129,0.08)', border: '1px solid rgba(16,185,129,0.2)',
          fontSize: 12.5, color: '#6EE7B7', lineHeight: 1.7,
        }}>
          <strong>Recomendação prática:</strong> use <code>z-ai/glm-5.3-flash</code> como padrão.
          Ele entrega 1,3M de contexto por $0,12 típico — 19× mais barato que o GLM 5.3 e 50× mais
          barato que o Kimi K3, com a mesma classe de contexto. Suba para GLM 5.3 ou Kimi K3 só
          quando a tarefa provar que precisa de mais profundidade.
        </div>
      </Section>

      {/* Uso */}
      <Section icon={Code2} title="4. Python (SDK da OpenAI)">
        <CodeBlock code={pythonCode} lang="python" />
        <p style={{ margin: '10px 0 0', fontSize: 12.5, color: '#6B7280', lineHeight: 1.7 }}>
          Repare que o <code>api_key</code> é a <strong>Service API Key do CRM</strong>, não a sua
          chave <code>sk-or-v1-...</code> da OpenRouter. A chave da OpenRouter fica só no servidor e
          é injetada pelo proxy. Mandar a <code>sk-or-v1-...</code> aqui devolve 401.
        </p>
      </Section>

      <Section icon={Zap} title="5. Streaming" color="#F59E0B">
        <CodeBlock code={streamCode} lang="python" />
      </Section>

      <Section icon={Terminal} title="6. curl e Node.js" color="#8B5CF6">
        <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
          <CodeBlock code={curlCode} lang="bash" />
          <CodeBlock code={nodeCode} lang="typescript" />
        </div>
      </Section>

      <Section icon={Laptop} title="7. Claude Code CLI" color="#F59E0B">
        <p style={{ margin: '0 0 12px', fontSize: 13, color: '#9CA3AF', lineHeight: 1.7 }}>
          Funciona, e foi testado de ponta a ponta: o CLI respondeu, usou ferramenta e escreveu
          arquivo em disco através deste proxy. O que torna isso possível é a OpenRouter expor
          também um endpoint no formato da Anthropic (<code>/v1/messages</code>), que é o único
          que o Claude Code fala — ele não entende <code>/v1/chat/completions</code>.
        </p>

        <div style={{
          padding: '10px 14px', borderRadius: 8, marginBottom: 14,
          background: 'rgba(59,130,246,0.08)', border: '1px solid rgba(59,130,246,0.2)',
          fontSize: 12, color: '#93C5FD', lineHeight: 1.6,
        }}>
          Note que a <code>ANTHROPIC_BASE_URL</code> aqui termina em <code>/openrouter</code>, sem
          o <code>/v1</code> — o CLI acrescenta o <code>/v1/messages</code> sozinho. Nos exemplos
          de SDK da OpenAI mais acima a URL inclui o <code>/v1</code>. Confundir os dois dá 404.
        </div>

        <p style={{ margin: '0 0 8px', fontSize: 13, fontWeight: 600, color: '#E5E7EB' }}>
          Só nesta sessão do PowerShell (bom para testar)
        </p>
        <CodeBlock code={claudeCodeSessionCode} lang="powershell" />

        <p style={{ margin: '16px 0 8px', fontSize: 13, fontWeight: 600, color: '#E5E7EB' }}>
          Permanente, para o usuário do Windows
        </p>
        <CodeBlock code={claudeCodePersistCode} lang="powershell" />
        <p style={{ margin: '8px 0 0', fontSize: 12.5, color: '#6B7280', lineHeight: 1.7 }}>
          Feche o terminal e abra de novo depois de rodar — variáveis de usuário só valem em
          processos novos.
        </p>

        <p style={{ margin: '16px 0 8px', fontSize: 13, fontWeight: 600, color: '#E5E7EB' }}>
          Ou direto no settings.json global
        </p>
        <CodeBlock code={`notepad $env:USERPROFILE\.claude\settings.json`} lang="powershell" />
        <div style={{ marginTop: 8 }}>
          <CodeBlock code={claudeCodeSettingsCode} lang="json" />
        </div>
        <p style={{ margin: '8px 0 0', fontSize: 12.5, color: '#6B7280', lineHeight: 1.7 }}>
          O campo <code>model</code> fica FORA do bloco <code>env</code>, e é obrigatório. Sem ele o
          CLI volta para o modelo padrão da conta, que exige login OAuth e falha com
          &quot;Not logged in&quot; mesmo com o resto certo.
        </p>

        <p style={{ margin: '18px 0 8px', fontSize: 13, fontWeight: 600, color: '#E5E7EB' }}>
          O que cada variável resolve
        </p>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
          {[
            { k: 'ANTHROPIC_BASE_URL', v: 'Aponta o CLI para o proxy em vez da API da Anthropic.' },
            { k: 'ANTHROPIC_API_KEY', v: 'A Service API Key do CRM. O CLI a envia no header x-api-key, que é exatamente o que o proxy valida.' },
            { k: 'ANTHROPIC_MODEL', v: 'Slug da OpenRouter do modelo principal. Obrigatório — sem ele o CLI tenta um modelo Claude que a OpenRouter não conhece.' },
            { k: 'ANTHROPIC_DEFAULT_HAIKU_MODEL', v: 'Modelo das tarefas de fundo (títulos, resumos, buscas rápidas). Sem isto o CLI pede um Haiku, a OpenRouter não reconhece e essas tarefas falham em silêncio.' },
            { k: 'ANTHROPIC_SMALL_FAST_MODEL', v: 'Mesmo papel, nome antigo. Defina os dois para cobrir qualquer versão do CLI.' },
            { k: 'CLAUDE_CODE_MAX_CONTEXT_TOKENS', v: 'Sem isto o CLI assume 200k e faz auto-compact cedo demais. GLM 5.3 tem 1.310.720; Kimi K3 tem 1.048.576.' },
          ].map((row) => (
            <div key={row.k} style={{
              padding: '9px 13px', borderRadius: 8,
              background: 'rgba(255,255,255,0.02)', border: '1px solid rgba(255,255,255,0.06)',
            }}>
              <code style={{ fontSize: 12, color: '#10B981', fontFamily: 'monospace', fontWeight: 700 }}>{row.k}</code>
              <div style={{ fontSize: 12.5, color: '#9CA3AF', lineHeight: 1.6, marginTop: 3 }}>{row.v}</div>
            </div>
          ))}
        </div>

        <p style={{ margin: '18px 0 8px', fontSize: 13, fontWeight: 600, color: '#E5E7EB' }}>
          Dois avisos que aparecem e são normais
        </p>
        <div style={{
          padding: '11px 14px', borderRadius: 8,
          background: 'rgba(245,158,11,0.08)', border: '1px solid rgba(245,158,11,0.2)',
          fontSize: 12.5, color: '#FCD34D', lineHeight: 1.7,
        }}>
          <strong>&quot;claude.ai connectors are disabled&quot;</strong> — esperado. Ao usar chave de API
          o CLI deixa de usar o login do claude.ai, então os connectors daquela conta ficam fora
          nessa sessão. Não é erro.
          <br /><br />
          <strong>&quot;[claude-code:unrecognized_model]&quot;</strong> — esperado também. O catálogo local do
          CLI só conhece modelos Claude, e o slug da OpenRouter não está nele. Não impede nada; a
          única consequência prática era a janela de contexto errada, que a variável
          <code> CLAUDE_CODE_MAX_CONTEXT_TOKENS</code> acima já resolve.
        </div>

        <p style={{ margin: '18px 0 8px', fontSize: 13, fontWeight: 600, color: '#E5E7EB' }}>
          Voltar para o login normal
        </p>
        <CodeBlock code={claudeCodeRevertCode} lang="powershell" />

        <div style={{
          marginTop: 14, padding: '11px 14px', borderRadius: 8,
          background: 'rgba(16,185,129,0.08)', border: '1px solid rgba(16,185,129,0.2)',
          fontSize: 12.5, color: '#6EE7B7', lineHeight: 1.7,
        }}>
          <strong>Qual modelo usar aqui:</strong> <code>z-ai/glm-5.3</code> para o trabalho principal e{' '}
          <code>z-ai/glm-5.3-flash</code> para as tarefas de fundo é a combinação de melhor
          custo-benefício. <code>moonshotai/kimi-k3</code> também funciona e tem fama forte em código,
          por 2,6× o preço. Os três foram verificados com chamada de ferramenta neste proxy — que é o
          requisito real, porque sem tool use o Claude Code não faz nada.
        </div>
      </Section>

      <Section icon={Layers} title="8. Descobrir modelos e conferir saldo" color="#3B82F6">
        <CodeBlock code={modelsCode} lang="bash" />
        <p style={{ margin: '10px 0 0', fontSize: 12.5, color: '#6B7280', lineHeight: 1.7 }}>
          O catálogo muda com frequência — modelos entram, saem e mudam de preço. Antes de fixar um
          slug em produção, confirme por aqui que ele ainda existe.
        </p>
      </Section>

      {/* Como funciona */}
      <Section icon={Settings} title="9. Como funciona por dentro" color="#6B7280">
        <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
          {[
            { step: '1', label: 'Cliente envia a requisição', desc: `POST ${PROXY_CHAT_URL} com a Service API Key em Authorization: Bearer`, cor: '#3B82F6' },
            { step: '2', label: 'CRM valida a Service API Key', desc: 'Um Bearer sem formato de JWT vai para o handler de chave estática; um JWT (dois pontos) vai para o handler de sessão', cor: '#8B5CF6' },
            { step: '3', label: 'Proxy injeta a chave real', desc: 'Adiciona Authorization: Bearer sk-or-... (sua chave OpenRouter, guardada no servidor)', cor: '#F59E0B' },
            { step: '4', label: 'Encaminha para a OpenRouter', desc: 'POST openrouter.ai/api/v1/chat/completions, corpo repassado sem alteração', cor: '#10B981' },
            { step: '5', label: 'Resposta volta ao cliente', desc: 'JSON completo, ou eventos SSE em tempo real quando stream: true', cor: '#10B981' },
          ].map((s) => (
            <div key={s.step} style={{
              display: 'flex', gap: 12, alignItems: 'flex-start',
              padding: '10px 14px', borderRadius: 8,
              background: 'rgba(255,255,255,0.02)', border: '1px solid rgba(255,255,255,0.06)',
            }}>
              <div style={{
                width: 22, height: 22, borderRadius: 6, flexShrink: 0,
                background: `${s.cor}20`, border: `1px solid ${s.cor}40`,
                display: 'flex', alignItems: 'center', justifyContent: 'center',
                fontSize: 11, fontWeight: 700, color: s.cor,
              }}>{s.step}</div>
              <div>
                <div style={{ fontSize: 13, fontWeight: 600, color: '#E5E7EB', marginBottom: 2 }}>{s.label}</div>
                <div style={{ fontSize: 12, color: '#6B7280', lineHeight: 1.6 }}>{s.desc}</div>
              </div>
            </div>
          ))}
        </div>
      </Section>

      {/* Segurança */}
      <Section icon={ShieldAlert} title="10. Sobre a chave" color="#EF4444">
        <div style={{
          padding: '12px 14px', borderRadius: 8,
          background: 'rgba(239,68,68,0.08)', border: '1px solid rgba(239,68,68,0.22)',
          fontSize: 12.5, color: '#FCA5A5', lineHeight: 1.7,
        }}>
          A Service API Key dá acesso a este proxy e ao da Anthropic, e ambos gastam crédito real.
          Por isso ela <strong>não fica escrita nesta página</strong> — você cola uma vez e ela
          permanece apenas no seu navegador. Não publique a chave em repositório, captura de tela
          ou página pública: quem a tiver consegue consumir seu saldo.
        </div>
      </Section>

      {/* FAQ */}
      <Section icon={BookOpen} title="11. Perguntas frequentes" color="#8B5CF6">
        <Accordion items={faq} expanded={expanded} onToggle={setExpanded} />
      </Section>

      <div style={{
        marginTop: 8, padding: '12px 14px', borderRadius: 8,
        background: 'rgba(255,255,255,0.02)', border: '1px solid rgba(255,255,255,0.06)',
        display: 'flex', gap: 10, alignItems: 'flex-start',
      }}>
        <AlertCircle size={14} color="#6B7280" style={{ flexShrink: 0, marginTop: 2 }} />
        <span style={{ fontSize: 12, color: '#6B7280', lineHeight: 1.7 }}>
          Precisa especificamente do Claude com o SDK da Anthropic? Use a página{' '}
          <a href="/tools/anthropic-proxy/" style={{ color: '#10B981', textDecoration: 'none', fontWeight: 600 }}>Anthropic Proxy</a>,
          que fala o formato nativo da Anthropic.
        </span>
      </div>

      <div style={{
        marginTop: 20, paddingTop: 16, borderTop: '1px solid rgba(255,255,255,0.06)',
        display: 'flex', alignItems: 'center', gap: 8,
      }}>
        <DollarSign size={12} color="#6B7280" />
        <span style={{ fontSize: 11.5, color: '#4B5563' }}>
          Preços e disponibilidade verificados no catálogo ao vivo em 07/09/2026. Consulte{' '}
          <code style={{ color: '#6B7280' }}>/v1/models</code> para dados atuais.
        </span>
      </div>
    </div>
  );
}
