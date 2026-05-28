'use client';

import { useState } from 'react';
import { Copy, Check, Terminal, Code2, Cpu, Globe, Key, Zap, BookOpen, AlertCircle, ChevronDown, ChevronRight } from 'lucide-react';

const PROXY_URL = 'https://api.alexandrequeiroz.com.br/proxy';
const PROXY_MESSAGES_URL = `${PROXY_URL}/v1/messages`;
const SERVICE_KEY = 'HdPjcrZyjD5fKcPm8qyxJnLYnG0Vi6tBNBUP6E12qvc';

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

export default function AnthropicProxyPage() {
  const [expandedFaq, setExpandedFaq] = useState<number | null>(null);

  const faqs = [
    {
      q: 'A minha chave Anthropic fica exposta no notebook?',
      a: 'Não. A chave real da Anthropic (sk-ant-...) fica armazenada no servidor do CRM. O notebook usa apenas a ServiceApiKey do CRM (uma chave de acesso interno), que não tem valor fora deste sistema.',
    },
    {
      q: 'Tenho que pagar pelos tokens usados no notebook?',
      a: 'Sim — os tokens são cobrados na conta Anthropic vinculada ao CRM. É o mesmo custo que usar o Claude diretamente, apenas roteado pelo proxy.',
    },
    {
      q: 'Funciona com qualquer modelo Claude?',
      a: 'Sim. O proxy é transparente — repassa o payload exatamente como enviado. Você pode usar claude-sonnet-4-6, claude-opus-4-7, claude-haiku-4-5, etc.',
    },
    {
      q: 'E se o notebook da empresa bloquear também a URL do CRM?',
      a: 'O acesso ao crm.alexandrequeiroz.com.br é via HTTPS padrão (porta 443). A empresa autorizou este domínio, então não deve haver bloqueio.',
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
          Anthropic Proxy
        </h1>
        <p style={{ margin: 0, fontSize: 14, color: '#6B7280', lineHeight: 1.7, maxWidth: 580 }}>
          Proxy HTTPS que encaminha requisições para a Anthropic API. Use o SDK oficial do Claude
          em redes corporativas que bloqueiam <code style={{ background: 'rgba(255,255,255,0.06)', padding: '1px 5px', borderRadius: 4, fontSize: 12, color: '#9CA3AF' }}>api.anthropic.com</code> diretamente.
        </p>
      </div>

      {/* Endpoint info */}
      <Section icon={Globe} title="Endpoint do Proxy">
        <InfoRow label="Base URL" value={PROXY_URL} copyValue={PROXY_URL} />
        <InfoRow label="Messages" value={PROXY_MESSAGES_URL} copyValue={PROXY_MESSAGES_URL} />
        <InfoRow label="X-Api-Key" value={SERVICE_KEY} copyValue={SERVICE_KEY} />

        <div style={{
          marginTop: 12, padding: '10px 14px', borderRadius: 8,
          background: 'rgba(245,158,11,0.08)', border: '1px solid rgba(245,158,11,0.2)',
          display: 'flex', gap: 10,
        }}>
          <AlertCircle size={14} color="#F59E0B" style={{ flexShrink: 0, marginTop: 1 }} />
          <p style={{ margin: 0, fontSize: 12, color: '#D97706', lineHeight: 1.6 }}>
            A <strong>X-Api-Key</strong> é a chave de serviço do CRM — não é a sua chave Anthropic.
            A chave Anthropic real fica armazenada apenas no servidor e nunca é exposta.
          </p>
        </div>
      </Section>

      {/* Claude Code CLI */}
      <Section icon={Terminal} title="Claude Code CLI — Notebook da Empresa">
        <p style={{ margin: '0 0 14px', fontSize: 13, color: '#9CA3AF', lineHeight: 1.7 }}>
          Configure as variáveis de ambiente no <strong style={{ color: '#D1FAE5' }}>PowerShell do notebook</strong>.
          Depois é só usar <code style={{ background: 'rgba(255,255,255,0.06)', padding: '1px 5px', borderRadius: 4, fontSize: 12, color: '#9CA3AF' }}>claude</code> normalmente.
        </p>

        <div style={{ marginBottom: 10 }}>
          <p style={{ margin: '0 0 8px', fontSize: 12, color: '#6B7280', fontWeight: 600 }}>Sessão atual (temporário):</p>
          <CodeBlock lang="powershell" code={`
$env:ANTHROPIC_BASE_URL = "${PROXY_URL}"
$env:ANTHROPIC_API_KEY  = "${SERVICE_KEY}"

# Agora use normalmente:
claude
          `} />
        </div>

        <div>
          <p style={{ margin: '0 0 8px', fontSize: 12, color: '#6B7280', fontWeight: 600 }}>Salvar permanentemente (não precisa mais setar):</p>
          <CodeBlock lang="powershell" code={`
[System.Environment]::SetEnvironmentVariable(
  "ANTHROPIC_BASE_URL",
  "${PROXY_URL}",
  "User"
)
[System.Environment]::SetEnvironmentVariable(
  "ANTHROPIC_API_KEY",
  "${SERVICE_KEY}",
  "User"
)

# Reinicie o terminal — depois é só usar claude normalmente
          `} />
        </div>
      </Section>

      {/* Python SDK */}
      <Section icon={Code2} title="Python SDK — Jupyter Notebook / Scripts">
        <p style={{ margin: '0 0 14px', fontSize: 13, color: '#9CA3AF', lineHeight: 1.7 }}>
          Instale o SDK e mude apenas o <code style={{ background: 'rgba(255,255,255,0.06)', padding: '1px 5px', borderRadius: 4, fontSize: 12, color: '#9CA3AF' }}>base_url</code>.
          Todo o resto é idêntico ao uso normal.
        </p>

        <div style={{ marginBottom: 10 }}>
          <p style={{ margin: '0 0 8px', fontSize: 12, color: '#6B7280', fontWeight: 600 }}>Instalar:</p>
          <CodeBlock lang="bash" code="pip install anthropic" />
        </div>

        <div style={{ marginBottom: 10 }}>
          <p style={{ margin: '0 0 8px', fontSize: 12, color: '#6B7280', fontWeight: 600 }}>Uso básico (sem streaming):</p>
          <CodeBlock lang="python" code={`
import anthropic

client = anthropic.Anthropic(
    api_key="proxy",          # qualquer string — ignorada pelo proxy
    base_url="${PROXY_URL}",
    default_headers={
        "X-Api-Key": "${SERVICE_KEY}"
    },
)

message = client.messages.create(
    model="claude-sonnet-4-6",
    max_tokens=1024,
    messages=[{"role": "user", "content": "Olá! Pode me ajudar?"}],
)
print(message.content[0].text)
          `} />
        </div>

        <div style={{ marginBottom: 10 }}>
          <p style={{ margin: '0 0 8px', fontSize: 12, color: '#6B7280', fontWeight: 600 }}>Com streaming:</p>
          <CodeBlock lang="python" code={`
with client.messages.stream(
    model="claude-sonnet-4-6",
    max_tokens=2048,
    system="Você é um assistente especialista em dados.",
    messages=[{"role": "user", "content": "Analise os dados a seguir..."}],
) as stream:
    for text in stream.text_stream:
        print(text, end="", flush=True)
          `} />
        </div>

        <div>
          <p style={{ margin: '0 0 8px', fontSize: 12, color: '#6B7280', fontWeight: 600 }}>Variáveis de ambiente (alternativa ao código):</p>
          <CodeBlock lang="python" code={`
import os, anthropic

os.environ["ANTHROPIC_BASE_URL"] = "${PROXY_URL}"
os.environ["ANTHROPIC_API_KEY"]  = "${SERVICE_KEY}"

# Sem precisar passar base_url no construtor
client = anthropic.Anthropic()
          `} />
        </div>
      </Section>

      {/* curl */}
      <Section icon={Zap} title="Teste Rápido com curl">
        <CodeBlock lang="bash" code={`
curl -X POST "${PROXY_MESSAGES_URL}" \\
  -H "Content-Type: application/json" \\
  -H "X-Api-Key: ${SERVICE_KEY}" \\
  -d '{
    "model": "claude-haiku-4-5-20251001",
    "max_tokens": 256,
    "messages": [{"role": "user", "content": "Diga oi em 5 idiomas"}]
  }'
        `} />
      </Section>

      {/* Como funciona */}
      <Section icon={Cpu} title="Como Funciona">
        <div style={{
          display: 'flex', flexDirection: 'column', gap: 0,
          border: '1px solid rgba(255,255,255,0.08)', borderRadius: 10, overflow: 'hidden',
        }}>
          {[
            { step: '1', label: 'Notebook envia requisição', desc: `POST ${PROXY_URL}/v1/messages com X-Api-Key`, color: '#6366F1' },
            { step: '2', label: 'CRM valida a ServiceApiKey', desc: 'Checa o header X-Api-Key contra a chave configurada no servidor', color: '#8B5CF6' },
            { step: '3', label: 'Proxy injeta chave real', desc: 'Adiciona x-api-key: sk-ant-... (sua chave Anthropic, armazenada só no servidor)', color: '#10B981' },
            { step: '4', label: 'Encaminha para Anthropic', desc: 'POST https://api.anthropic.com/v1/messages com a chave real', color: '#059669' },
            { step: '5', label: 'Resposta retorna ao notebook', desc: 'Streaming SSE ou JSON — transparente, como se fosse direto', color: '#10B981' },
          ].map((s, i) => (
            <div key={i} style={{
              display: 'flex', alignItems: 'flex-start', gap: 14,
              padding: '12px 16px',
              borderBottom: i < 4 ? '1px solid rgba(255,255,255,0.06)' : 'none',
              background: i % 2 === 0 ? 'rgba(255,255,255,0.02)' : 'transparent',
            }}>
              <div style={{
                width: 22, height: 22, borderRadius: 6, flexShrink: 0,
                background: `${s.color}20`, border: `1px solid ${s.color}40`,
                display: 'flex', alignItems: 'center', justifyContent: 'center',
                fontSize: 10, fontWeight: 800, color: s.color, marginTop: 1,
              }}>{s.step}</div>
              <div>
                <div style={{ fontSize: 13, fontWeight: 600, color: '#F9FAFB', marginBottom: 2 }}>{s.label}</div>
                <div style={{ fontSize: 12, color: '#6B7280', lineHeight: 1.5 }}>{s.desc}</div>
              </div>
            </div>
          ))}
        </div>
      </Section>

      {/* Modelos disponíveis */}
      <Section icon={BookOpen} title="Modelos Disponíveis">
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 8 }}>
          {[
            { name: 'claude-sonnet-4-6', label: 'Sonnet 4.6', badge: 'Padrão', color: '#10B981' },
            { name: 'claude-opus-4-7', label: 'Opus 4.7', badge: 'Mais capaz', color: '#6366F1' },
            { name: 'claude-haiku-4-5-20251001', label: 'Haiku 4.5', badge: 'Mais rápido', color: '#F59E0B' },
            { name: 'claude-opus-4-6', label: 'Opus 4.6', badge: 'Legado', color: '#6B7280' },
          ].map(m => (
            <div key={m.name} style={{
              padding: '10px 14px', borderRadius: 8,
              background: 'rgba(255,255,255,0.02)', border: '1px solid rgba(255,255,255,0.07)',
              display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 10,
            }}>
              <div>
                <div style={{ fontSize: 12, fontWeight: 600, color: '#F9FAFB', marginBottom: 2 }}>{m.label}</div>
                <code style={{ fontSize: 10, color: '#6B7280', fontFamily: 'monospace' }}>{m.name}</code>
              </div>
              <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
                <span style={{
                  padding: '2px 7px', borderRadius: 4, fontSize: 10, fontWeight: 700,
                  background: `${m.color}15`, color: m.color, border: `1px solid ${m.color}30`,
                  whiteSpace: 'nowrap',
                }}>{m.badge}</span>
                <CopyButton text={m.name} />
              </div>
            </div>
          ))}
        </div>
      </Section>

      {/* FAQ */}
      <Section icon={Key} title="Dúvidas Frequentes" color="#6366F1">
        <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
          {faqs.map((faq, i) => (
            <div key={i} style={{
              borderRadius: 9, border: '1px solid rgba(255,255,255,0.07)',
              overflow: 'hidden', background: 'rgba(255,255,255,0.02)',
            }}>
              <button
                onClick={() => setExpandedFaq(expandedFaq === i ? null : i)}
                style={{
                  width: '100%', padding: '12px 14px',
                  display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 12,
                  background: 'none', border: 'none', cursor: 'pointer', textAlign: 'left',
                }}
              >
                <span style={{ fontSize: 13, fontWeight: 600, color: '#E5E7EB' }}>{faq.q}</span>
                {expandedFaq === i
                  ? <ChevronDown size={14} color="#6B7280" style={{ flexShrink: 0 }} />
                  : <ChevronRight size={14} color="#6B7280" style={{ flexShrink: 0 }} />
                }
              </button>
              {expandedFaq === i && (
                <div style={{
                  padding: '0 14px 12px',
                  fontSize: 13, color: '#9CA3AF', lineHeight: 1.7,
                  borderTop: '1px solid rgba(255,255,255,0.05)',
                  paddingTop: 10,
                }}>{faq.a}</div>
              )}
            </div>
          ))}
        </div>
      </Section>

    </div>
  );
}
