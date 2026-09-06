'use client';

import { useState } from 'react';
import { Copy, Check, Terminal, Code2, Cpu, Globe, Key, Zap, BookOpen, AlertCircle, ChevronDown, ChevronRight, Settings, AlertTriangle, ShieldAlert, Laptop } from 'lucide-react';

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
              fontSize: 13, color: '#9CA3AF', lineHeight: 1.7,
              borderTop: '1px solid rgba(255,255,255,0.05)',
              paddingTop: 10,
            }}>{item.a}</div>
          )}
        </div>
      ))}
    </div>
  );
}

export default function AnthropicProxyPage() {
  const [expandedFaq, setExpandedFaq] = useState<number | null>(null);
  const [expandedError, setExpandedError] = useState<number | null>(null);

  const troubleshooting = [
    {
      q: '404 not_found_error: "model: claude-3-5-haiku-20241022" (ou outro modelo)',
      a: 'O nome do modelo está descontinuado/errado nesse proxy. Use um nome atual — veja a seção "Modelos Disponíveis" abaixo (ex: claude-sonnet-4-5-20250929, claude-sonnet-4-6, claude-haiku-4-5-20251001). O proxy repassa o payload como está — se o modelo não existir na Anthropic, ela responde 404.',
    },
    {
      q: '"claude.ai connectors are disabled because ANTHROPIC_API_KEY or another auth source is set..."',
      a: 'Não é erro — é aviso. Confirma que o CLI detectou a variável ANTHROPIC_API_KEY e está usando o proxy em vez do login OAuth normal do claude.ai (por isso os connectors do claude.ai ficam fora do ar nessa sessão). Se quiser voltar pro login normal, apague as duas variáveis (Remove-Item Env:ANTHROPIC_API_KEY e Remove-Item Env:ANTHROPIC_BASE_URL) e abra um terminal novo.',
    },
    {
      q: '401 Unauthorized / authentication_error',
      a: 'A X-Api-Key está errada, incompleta, com espaço extra colado, ou a variável ANTHROPIC_API_KEY está vazia na sessão. Rode $env:ANTHROPIC_API_KEY pra conferir o valor exato antes de reclamar do proxy.',
    },
    {
      q: 'Timeout, "could not resolve host" ou conexão recusada',
      a: 'A rede corporativa está bloqueando o domínio api.alexandrequeiroz.com.br. Teste com curl -I https://api.alexandrequeiroz.com.br/proxy/v1/messages — se travar/der erro de rede (não de autenticação), é bloqueio de firewall/proxy da empresa. Peça liberação desse domínio ao TI.',
    },
    {
      q: 'Erro de certificado SSL (unable to verify the first certificate, SEC_ERROR, etc)',
      a: 'A rede da empresa está fazendo inspeção SSL (proxy MITM) e trocando o certificado. Normalmente resolve apontando a variável NODE_EXTRA_CA_CERTS pro certificado raiz corporativo, ou pedindo ao TI a isenção pra esse domínio. Não é problema do proxy do CRM.',
    },
    {
      q: 'A variável some depois de reiniciar o PC ou abrir um terminal novo em outro dia',
      a: 'Ela foi setada só com $env: (vale só pra aquela sessão de PowerShell). Pra persistir entre reinicializações, use o método "Permanente via PowerShell" ou "Permanente via GUI" desta página — nunca só $env:.',
    },
    {
      q: 'O claude continua logando na conta normal (OAuth) mesmo com a variável setada',
      a: 'O terminal (ou o VS Code inteiro) foi aberto ANTES de você setar a variável. Ambientes de shell tiram uma foto (snapshot) das variáveis no momento em que abrem — feche TODOS os terminais/janelas e abra de novo. No VS Code, "Recarregar Janela" não é suficiente: feche o programa inteiro.',
    },
    {
      q: 'Erro 500 ou instabilidade genérica do proxy',
      a: 'A chave Anthropic real (sk-ant-...) armazenada no servidor do CRM pode estar sem crédito, expirada ou com instabilidade. Isso é do lado do servidor, não do notebook — avise o admin do CRM.',
    },
    {
      q: 'Setei em um PowerShell (5.1) e não aparece no PowerShell 7 (pwsh), ou vice-versa',
      a: 'Variáveis setadas só com $env: valem apenas pra sessão/processo aberto. Já as salvas como "User" (SetEnvironmentVariable) valem pra qualquer shell novo (5.1, 7, cmd, VS Code) — mas só a partir do PRÓXIMO processo aberto depois de salvar, nunca no que já está rodando.',
    },
  ];

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
      <Section icon={Terminal} title="Configurar no Windows 11 (PowerShell)">
        <p style={{ margin: '0 0 14px', fontSize: 13, color: '#9CA3AF', lineHeight: 1.7 }}>
          Existem 3 formas de setar as variáveis. Todo comando abaixo é <strong style={{ color: '#D1FAE5' }}>uma linha única</strong> —
          cole exatamente assim no PowerShell. Comandos quebrados em várias linhas com parênteses às vezes colam errado
          (o terminal interpreta cada linha separada) e falham silenciosamente.
        </p>

        <div style={{ marginBottom: 14 }}>
          <p style={{ margin: '0 0 8px', fontSize: 12, color: '#6B7280', fontWeight: 600 }}>
            Método 1 — Sessão atual (temporário, só vale até fechar o terminal):
          </p>
          <CodeBlock lang="powershell" code={`
$env:ANTHROPIC_BASE_URL = "${PROXY_URL}"
          `} />
          <div style={{ height: 6 }} />
          <CodeBlock lang="powershell" code={`
$env:ANTHROPIC_API_KEY = "${SERVICE_KEY}"
          `} />
          <p style={{ margin: '8px 0 0', fontSize: 12, color: '#6B7280' }}>Depois é só rodar:</p>
          <div style={{ height: 6 }} />
          <CodeBlock lang="powershell" code={`claude`} />
        </div>

        <div style={{ marginBottom: 14 }}>
          <p style={{ margin: '0 0 8px', fontSize: 12, color: '#6B7280', fontWeight: 600 }}>
            Método 2 — Permanente via PowerShell (sobrevive a reinicialização do PC):
          </p>
          <CodeBlock lang="powershell" code={`
[System.Environment]::SetEnvironmentVariable("ANTHROPIC_BASE_URL", "${PROXY_URL}", "User")
          `} />
          <div style={{ height: 6 }} />
          <CodeBlock lang="powershell" code={`
[System.Environment]::SetEnvironmentVariable("ANTHROPIC_API_KEY", "${SERVICE_KEY}", "User")
          `} />
          <div style={{
            marginTop: 10, padding: '10px 14px', borderRadius: 8,
            background: 'rgba(245,158,11,0.08)', border: '1px solid rgba(245,158,11,0.2)',
            display: 'flex', gap: 10,
          }}>
            <AlertCircle size={14} color="#F59E0B" style={{ flexShrink: 0, marginTop: 1 }} />
            <p style={{ margin: 0, fontSize: 12, color: '#D97706', lineHeight: 1.6 }}>
              Esse comando só salva o valor no registro do Windows — ele NÃO aparece na sessão que você já tem aberta.
              Feche o terminal atual e abra um novo pra ele valer.
            </p>
          </div>
        </div>

        <div>
          <p style={{ margin: '0 0 8px', fontSize: 12, color: '#6B7280', fontWeight: 600 }}>
            Método 3 — Permanente via interface gráfica (sem digitar comando):
          </p>
          <ol style={{ margin: 0, paddingLeft: 20, fontSize: 13, color: '#9CA3AF', lineHeight: 2 }}>
            <li>Aperte <code style={{ background: 'rgba(255,255,255,0.06)', padding: '1px 5px', borderRadius: 4, fontSize: 12, color: '#D1FAE5' }}>Win</code>, digite <strong style={{ color: '#D1FAE5' }}>&quot;editar variáveis de ambiente&quot;</strong> e abra &quot;Editar as variáveis de ambiente do sistema&quot;.</li>
            <li>Na janela &quot;Propriedades do Sistema&quot;, clique no botão <strong style={{ color: '#D1FAE5' }}>Variáveis de Ambiente...</strong></li>
            <li>Na seção de cima (<strong style={{ color: '#D1FAE5' }}>Variáveis de usuário</strong> — não mexa nas de baixo, &quot;Variáveis do sistema&quot;), clique em <strong style={{ color: '#D1FAE5' }}>Novo...</strong></li>
            <li>Nome da variável: <code style={{ background: 'rgba(255,255,255,0.06)', padding: '1px 5px', borderRadius: 4, fontSize: 12, color: '#D1FAE5' }}>ANTHROPIC_BASE_URL</code> — Valor: <code style={{ background: 'rgba(255,255,255,0.06)', padding: '1px 5px', borderRadius: 4, fontSize: 12, color: '#D1FAE5' }}>{PROXY_URL}</code> → OK</li>
            <li>Clique em <strong style={{ color: '#D1FAE5' }}>Novo...</strong> de novo. Nome: <code style={{ background: 'rgba(255,255,255,0.06)', padding: '1px 5px', borderRadius: 4, fontSize: 12, color: '#D1FAE5' }}>ANTHROPIC_API_KEY</code> — Valor: a chave de serviço acima → OK</li>
            <li>Clique OK em todas as janelas abertas.</li>
            <li><strong style={{ color: '#D1FAE5' }}>Feche todos os terminais/PowerShell/VS Code abertos</strong> e abra de novo.</li>
          </ol>
        </div>
      </Section>

      {/* VS Code */}
      <Section icon={Laptop} title="Configurar no VS Code" color="#6366F1">
        <p style={{ margin: '0 0 14px', fontSize: 13, color: '#9CA3AF', lineHeight: 1.7 }}>
          O terminal integrado do VS Code herda as variáveis do Windows — mas só as que existiam
          <strong style={{ color: '#D1FAE5' }}> no momento em que o VS Code foi aberto</strong>. Recarregar a janela
          (<code style={{ background: 'rgba(255,255,255,0.06)', padding: '1px 5px', borderRadius: 4, fontSize: 12, color: '#9CA3AF' }}>Reload Window</code>) não conta —
          é preciso fechar o programa inteiro e abrir de novo.
        </p>

        <div style={{ marginBottom: 14 }}>
          <p style={{ margin: '0 0 8px', fontSize: 12, color: '#6B7280', fontWeight: 600 }}>
            Passo a passo (usando as variáveis já setadas no Windows):
          </p>
          <ol style={{ margin: 0, paddingLeft: 20, fontSize: 13, color: '#9CA3AF', lineHeight: 2 }}>
            <li>Sete as variáveis pelo Método 2 ou 3 acima (não adianta só $env: da sessão do PowerShell — o VS Code não vai enxergar).</li>
            <li>Feche o VS Code <strong style={{ color: '#D1FAE5' }}>completamente</strong> (todas as janelas).</li>
            <li>Abra o VS Code de novo, abra um terminal (<code style={{ background: 'rgba(255,255,255,0.06)', padding: '1px 5px', borderRadius: 4, fontSize: 12, color: '#9CA3AF' }}>Ctrl+`</code>).</li>
            <li>Confirme com o comando da seção &quot;Verificar&quot; abaixo.</li>
          </ol>
        </div>

        <div>
          <p style={{ margin: '0 0 8px', fontSize: 12, color: '#6B7280', fontWeight: 600 }}>
            Alternativa — forçar direto no VS Code (sem mexer no Windows inteiro, isolado por projeto):
          </p>
          <p style={{ margin: '0 0 8px', fontSize: 12, color: '#6B7280' }}>
            Crie/edite <code style={{ background: 'rgba(255,255,255,0.06)', padding: '1px 5px', borderRadius: 4, fontSize: 12, color: '#9CA3AF' }}>.vscode/settings.json</code> na raiz do projeto:
          </p>
          <CodeBlock lang="json" code={`
{
  "terminal.integrated.env.windows": {
    "ANTHROPIC_BASE_URL": "${PROXY_URL}",
    "ANTHROPIC_API_KEY": "${SERVICE_KEY}"
  }
}
          `} />
          <div style={{
            marginTop: 10, padding: '10px 14px', borderRadius: 8,
            background: 'rgba(245,158,11,0.08)', border: '1px solid rgba(245,158,11,0.2)',
            display: 'flex', gap: 10,
          }}>
            <AlertCircle size={14} color="#F59E0B" style={{ flexShrink: 0, marginTop: 1 }} />
            <p style={{ margin: 0, fontSize: 12, color: '#D97706', lineHeight: 1.6 }}>
              Depois de salvar o settings.json, feche TODOS os terminais abertos dentro do VS Code (lixeira no painel de terminal)
              e abra um novo — editar o arquivo não atualiza terminais já rodando. Esse arquivo tem a chave em texto puro:
              não faça commit dele num repositório compartilhado (adicione ao .gitignore).
            </p>
          </div>
        </div>
      </Section>

      {/* Verificação */}
      <Section icon={Settings} title="Verificar se as Variáveis Estão Certas" color="#059669">
        <div style={{ marginBottom: 14 }}>
          <p style={{ margin: '0 0 8px', fontSize: 12, color: '#6B7280', fontWeight: 600 }}>1. Ver o valor na sessão atual:</p>
          <CodeBlock lang="powershell" code={`$env:ANTHROPIC_BASE_URL`} />
          <div style={{ height: 6 }} />
          <CodeBlock lang="powershell" code={`$env:ANTHROPIC_API_KEY`} />
          <p style={{ margin: '8px 0 0', fontSize: 12, color: '#6B7280', lineHeight: 1.6 }}>
            Se vier em branco, a variável não está setada nessa sessão — refaça o Método 1 ou abra um terminal novo (se usou Método 2/3).
          </p>
        </div>

        <div style={{ marginBottom: 14 }}>
          <p style={{ margin: '0 0 8px', fontSize: 12, color: '#6B7280', fontWeight: 600 }}>2. Ver o valor salvo permanentemente (independe da sessão):</p>
          <CodeBlock lang="powershell" code={`[System.Environment]::GetEnvironmentVariable("ANTHROPIC_BASE_URL", "User")`} />
          <div style={{ height: 6 }} />
          <CodeBlock lang="powershell" code={`[System.Environment]::GetEnvironmentVariable("ANTHROPIC_API_KEY", "User")`} />
        </div>

        <div style={{ marginBottom: 14 }}>
          <p style={{ margin: '0 0 8px', fontSize: 12, color: '#6B7280', fontWeight: 600 }}>3. Listar tudo que começa com ANTHROPIC de uma vez:</p>
          <CodeBlock lang="powershell" code={`Get-ChildItem Env:ANTHROPIC*`} />
        </div>

        <div>
          <p style={{ margin: '0 0 8px', fontSize: 12, color: '#6B7280', fontWeight: 600 }}>4. Teste real — chama o CLI de verdade e confere a resposta:</p>
          <CodeBlock lang="powershell" code={`claude -p "diga apenas: ok" --model claude-sonnet-4-5-20250929`} />
          <p style={{ margin: '8px 0 0', fontSize: 12, color: '#6B7280', lineHeight: 1.6 }}>
            Resposta esperada: <code style={{ background: 'rgba(255,255,255,0.06)', padding: '1px 5px', borderRadius: 4, fontSize: 12, color: '#10B981' }}>ok</code>.
            Se aparecer o aviso sobre &quot;claude.ai connectors are disabled&quot;, é normal — só confirma que a variável está ativa (ver Erros Comuns).
          </p>
        </div>
      </Section>

      {/* Conflitos */}
      <Section icon={ShieldAlert} title="Detectar Conflito com Outras Variáveis" color="#DC2626">
        <p style={{ margin: '0 0 14px', fontSize: 13, color: '#9CA3AF', lineHeight: 1.7 }}>
          O Windows monta o ambiente de um processo novo assim: primeiro carrega as variáveis de
          <strong style={{ color: '#D1FAE5' }}> Machine</strong> (sistema, todo mundo que loga no PC), depois sobrepõe as de
          <strong style={{ color: '#D1FAE5' }}> User</strong> (só sua conta), e por cima disso entra o que foi setado na
          <strong style={{ color: '#D1FAE5' }}> sessão atual</strong> ($env:). Se o mesmo nome existir em mais de um nível com
          valores diferentes, o de nível mais específico (sessão &gt; User &gt; Machine) é o que vale — e é aí que dá confusão.
        </p>

        <div style={{ marginBottom: 14 }}>
          <p style={{ margin: '0 0 8px', fontSize: 12, color: '#6B7280', fontWeight: 600 }}>1. Comparar os 3 níveis (rode os dois e veja se batem):</p>
          <CodeBlock lang="powershell" code={`[System.Environment]::GetEnvironmentVariable("ANTHROPIC_API_KEY", "Machine")`} />
          <div style={{ height: 6 }} />
          <CodeBlock lang="powershell" code={`[System.Environment]::GetEnvironmentVariable("ANTHROPIC_API_KEY", "User")`} />
        </div>

        <div style={{ marginBottom: 14 }}>
          <p style={{ margin: '0 0 8px', fontSize: 12, color: '#6B7280', fontWeight: 600 }}>2. Checar se há variáveis parecidas que podem estar roubando prioridade (ex: token antigo, outro proxy, outro provider):</p>
          <CodeBlock lang="powershell" code={`Get-ChildItem Env: | Where-Object { $_.Name -like "*ANTHROPIC*" -or $_.Name -like "*CLAUDE*" }`} />
          <p style={{ margin: '8px 0 0', fontSize: 12, color: '#6B7280', lineHeight: 1.6 }}>
            Preste atenção especial em <code style={{ background: 'rgba(255,255,255,0.06)', padding: '1px 5px', borderRadius: 4, fontSize: 12, color: '#9CA3AF' }}>ANTHROPIC_AUTH_TOKEN</code>,{' '}
            <code style={{ background: 'rgba(255,255,255,0.06)', padding: '1px 5px', borderRadius: 4, fontSize: 12, color: '#9CA3AF' }}>CLAUDE_CODE_USE_BEDROCK</code> e{' '}
            <code style={{ background: 'rgba(255,255,255,0.06)', padding: '1px 5px', borderRadius: 4, fontSize: 12, color: '#9CA3AF' }}>CLAUDE_CODE_USE_VERTEX</code> —
            se alguma dessas existir setada como &quot;1&quot;/valor de outro projeto, o CLI pode ignorar o proxy e tentar ir pra Bedrock/Vertex direto.
          </p>
        </div>

        <div style={{ marginBottom: 14 }}>
          <p style={{ margin: '0 0 8px', fontSize: 12, color: '#6B7280', fontWeight: 600 }}>3. Checar se o perfil do PowerShell (roda toda vez que abre o terminal) está sobrescrevendo algo:</p>
          <CodeBlock lang="powershell" code={`Test-Path $PROFILE`} />
          <div style={{ height: 6 }} />
          <CodeBlock lang="powershell" code={`Get-Content $PROFILE`} />
        </div>

        <div>
          <p style={{ margin: '0 0 8px', fontSize: 12, color: '#6B7280', fontWeight: 600 }}>4. Limpar uma variável setada errada (nível User):</p>
          <CodeBlock lang="powershell" code={`[System.Environment]::SetEnvironmentVariable("ANTHROPIC_API_KEY", $null, "User")`} />
        </div>
      </Section>

      {/* Erros Comuns */}
      <Section icon={AlertTriangle} title="Erros Comuns e Como Resolver" color="#F59E0B">
        <Accordion items={troubleshooting} expanded={expandedError} onToggle={setExpandedError} />
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
