// ---------------------------------------------------------------------------
// Prompt Studio (biblioteca de prompts do DIAX CRM) — tools + MCP prompts.
// Cliente fino da API REST /api/v1/prompts: regras de render/versão ficam no backend.
// ---------------------------------------------------------------------------

export type DiaxFetch = (path: string, options?: RequestInit) => Promise<unknown>;

interface PromptVariable {
  name: string;
  label?: string | null;
  type?: string;
  default?: string | null;
  required?: boolean;
}

interface PromptListItem {
  id: string;
  slug: string;
  title: string;
  description?: string | null;
  tags: string[];
  variableNames: string[];
  isFavorite: boolean;
  exposeToMcp: boolean;
  currentVersionNumber: number;
  useCount: number;
  collectionId?: string | null;
  preview: string;
}

interface PromptDetail {
  prompt: PromptListItem & { kind: string; framework?: string | null };
  current: { number: number; content: string; variables: PromptVariable[] };
}

interface RenderResult {
  rendered: string;
  missing: string[];
  variables: PromptVariable[];
  versionNumber: number;
}

const GUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
const enc = encodeURIComponent;

export const PROMPT_TOOLS = [
  {
    name: "search_prompts",
    description:
      "Busca prompts na biblioteca do Prompt Studio (DIAX CRM) por texto (título, slug, tags, descrição e conteúdo da versão atual), tag ou favoritos. Retorna slug, título, tags e variáveis. Use antes de get_prompt/render_prompt.",
    inputSchema: {
      type: "object",
      properties: {
        query: { type: "string", description: "Texto livre (opcional)" },
        tag: { type: "string", description: "Filtra por tag (ex.: dev, cold-email)" },
        favorite: { type: "boolean", description: "Só favoritos" },
        limit: { type: "number", description: "Máximo de resultados (padrão 15, máx. 25)" },
      },
    },
  },
  {
    name: "get_prompt",
    description:
      "Retorna um prompt da biblioteca (conteúdo da versão atual ou de uma versão específica) em Markdown com frontmatter: título, tags, framework e variáveis {{nome}}.",
    inputSchema: {
      type: "object",
      properties: {
        slug_or_id: { type: "string", description: "Slug (ex.: revisor-de-codigo) ou ID" },
        version: { type: "number", description: "Número da versão (padrão: atual)" },
      },
      required: ["slug_or_id"],
    },
  },
  {
    name: "render_prompt",
    description:
      "Preenche as variáveis {{nome}} de um prompt da biblioteca e devolve o texto pronto para usar. Se faltar variável obrigatória, lista o que falta (com rótulo e padrão) em vez de falhar. Registra o uso.",
    inputSchema: {
      type: "object",
      properties: {
        slug_or_id: { type: "string", description: "Slug ou ID do prompt" },
        variables: {
          type: "object",
          description: "Valores das variáveis, ex.: {\"linguagem\": \"C#\", \"codigo\": \"...\"}",
          additionalProperties: { type: "string" },
        },
      },
      required: ["slug_or_id"],
    },
  },
  {
    name: "save_prompt",
    description:
      "Salva um prompt na biblioteca do Prompt Studio. Sem slug_or_id cria um prompt novo (v1); com slug_or_id cria uma nova versão do prompt existente (nada é sobrescrito, o histórico fica). Use {{nome}} para variáveis.",
    inputSchema: {
      type: "object",
      properties: {
        title: { type: "string", description: "Título (obrigatório ao criar)" },
        content: { type: "string", description: "Conteúdo do prompt" },
        slug_or_id: { type: "string", description: "Prompt existente → cria nova versão" },
        tags: { type: "array", items: { type: "string" }, description: "Tags (ao criar)" },
        description: { type: "string", description: "Descrição curta (ao criar)" },
        change_note: { type: "string", description: "O que mudou nesta versão" },
        expose_to_mcp: { type: "boolean", description: "Disponibilizar como slash command no Claude Code (ao criar)" },
      },
      required: ["content"],
    },
  },
];

const PROMPT_TOOL_NAMES = new Set(PROMPT_TOOLS.map(t => t.name));
export const isPromptTool = (name: string) => PROMPT_TOOL_NAMES.has(name);

async function resolvePrompt(fetch: DiaxFetch, slugOrId: string): Promise<PromptDetail> {
  const key = String(slugOrId ?? "").trim();
  if (!key) throw new Error("Informe slug_or_id.");
  return (await fetch(GUID.test(key) ? `/api/v1/prompts/${key}` : `/api/v1/prompts/by-slug/${enc(key)}`)) as PromptDetail;
}

function toMarkdown(d: PromptDetail, content: string, version: number, variables: PromptVariable[]): string {
  const p = d.prompt;
  const lines = [
    "---",
    `title: ${JSON.stringify(p.title)}`,
    `slug: ${p.slug}`,
    `version: ${version}`,
    p.description ? `description: ${JSON.stringify(p.description)}` : null,
    p.tags.length ? `tags: [${p.tags.join(", ")}]` : null,
    p.framework ? `framework: ${p.framework}` : null,
    `kind: ${p.kind}`,
    variables.length ? "variables:" : null,
    ...variables.map(v =>
      `  - { name: ${v.name}${v.label ? `, label: ${JSON.stringify(v.label)}` : ""}${v.default ? `, default: ${JSON.stringify(v.default)}` : ""}, required: ${v.required !== false} }`),
    "---",
    content,
  ];
  return lines.filter(l => l !== null).join("\n");
}

function describeMissing(r: RenderResult): string {
  const byName = new Map(r.variables.map(v => [v.name, v]));
  return r.missing
    .map(n => {
      const v = byName.get(n);
      return `- ${n}${v?.label ? ` (${v.label})` : ""}${v?.type === "textarea" ? " — texto longo" : ""}`;
    })
    .join("\n");
}

export async function handlePromptTool(fetch: DiaxFetch, name: string, args: Record<string, unknown>): Promise<string> {
  switch (name) {
    case "search_prompts": {
      const p = new URLSearchParams();
      if (args.query) p.set("q", String(args.query));
      if (args.tag) p.set("tag", String(args.tag));
      if (args.favorite) p.set("favorite", "true");
      const limit = Number(args.limit);
      p.set("limit", String(Number.isFinite(limit) && limit > 0 ? Math.min(Math.floor(limit), 25) : 15));
      const items = (await fetch(`/api/v1/prompts?${p}`)) as PromptListItem[];
      if (!items.length) return "Nenhum prompt encontrado.";
      return items
        .map(i => `${i.isFavorite ? "★ " : ""}${i.slug} — ${i.title} (v${i.currentVersionNumber}, ${i.useCount} usos)` +
          `${i.tags.length ? ` [${i.tags.join(", ")}]` : ""}` +
          `${i.variableNames.length ? ` · variáveis: ${i.variableNames.join(", ")}` : ""}`)
        .join("\n");
    }

    case "get_prompt": {
      const d = await resolvePrompt(fetch, String(args.slug_or_id));
      if (args.version && Number(args.version) !== d.current.number) {
        const v = (await fetch(`/api/v1/prompts/${d.prompt.id}/versions/${Number(args.version)}`)) as PromptDetail["current"];
        return toMarkdown(d, v.content, v.number, v.variables);
      }
      return toMarkdown(d, d.current.content, d.current.number, d.current.variables);
    }

    case "render_prompt": {
      const d = await resolvePrompt(fetch, String(args.slug_or_id));
      const r = (await fetch(`/api/v1/prompts/${d.prompt.id}/render`, {
        method: "POST",
        body: JSON.stringify({ variables: (args.variables ?? {}) as Record<string, string> }),
      })) as RenderResult;
      if (r.missing.length) {
        return `O prompt "${d.prompt.title}" precisa destas variáveis:\n${describeMissing(r)}\n\nChame render_prompt de novo com "variables".`;
      }
      await fetch(`/api/v1/prompts/${d.prompt.id}/use`, { method: "POST" }).catch(() => undefined);
      return r.rendered;
    }

    case "save_prompt": {
      const content = String(args.content ?? "");
      if (!content.trim()) throw new Error("content é obrigatório.");

      if (args.slug_or_id) {
        const d = await resolvePrompt(fetch, String(args.slug_or_id));
        const v = (await fetch(`/api/v1/prompts/${d.prompt.id}/versions`, {
          method: "POST",
          body: JSON.stringify({
            content,
            variables: d.current.variables,
            changeNote: args.change_note ?? "Atualizado pelo Claude Code",
            baseVersionNumber: d.current.number,
            source: "mcp",
          }),
        })) as { number: number };
        return v.number > d.current.number
          ? `Nova versão salva: ${d.prompt.slug} v${v.number}.`
          : `Nada mudou: ${d.prompt.slug} continua na v${d.current.number}.`;
      }

      if (!args.title) throw new Error("title é obrigatório para criar um prompt.");
      const created = (await fetch("/api/v1/prompts", {
        method: "POST",
        body: JSON.stringify({
          title: args.title,
          content,
          description: args.description ?? null,
          tags: Array.isArray(args.tags) ? args.tags : [],
          exposeToMcp: Boolean(args.expose_to_mcp),
          changeNote: args.change_note ?? "Criado pelo Claude Code",
          source: "mcp",
        }),
      })) as PromptDetail;
      return `Prompt criado: ${created.prompt.slug} (v1). Abra no CRM em /prompts/?p=${created.prompt.id}`;
    }

    default:
      throw new Error(`Unknown prompt tool: ${name}`);
  }
}

// ── MCP prompts capability: prompts com "Expor no Claude Code" viram slash commands ──
interface CatalogItem {
  slug: string;
  title: string;
  description?: string | null;
  preview: string;
  variables: PromptVariable[];
}

let listCache: { at: number; items: CatalogItem[] } | null = null;

export async function listMcpPrompts(fetch: DiaxFetch) {
  if (!listCache || Date.now() - listCache.at > 60_000) {
    const items = (await fetch("/api/v1/prompts/mcp-catalog")) as CatalogItem[];
    listCache = { at: Date.now(), items };
  }
  return listCache.items.map(p => ({
    name: p.slug,
    title: p.title,
    description: p.description || p.preview.slice(0, 160),
    // Opcional = marcada como não obrigatória OU com valor padrão (o render aplica o default).
    arguments: p.variables.map(v => ({
      name: v.name,
      description: v.label ? `${v.label} ({{${v.name}}})` : `Valor de {{${v.name}}}`,
      required: v.required !== false && !v.default,
    })),
  }));
}

export async function getMcpPrompt(fetch: DiaxFetch, name: string, args: Record<string, string> | undefined) {
  const d = (await fetch(`/api/v1/prompts/by-slug/${enc(name)}`)) as PromptDetail;
  // Só prompts marcados "Expor no Claude Code" viram slash command (as tools seguem livres).
  if (!d.prompt.exposeToMcp) throw new Error(`O prompt "${name}" não está exposto no Claude Code.`);
  const r = (await fetch(`/api/v1/prompts/${d.prompt.id}/render`, {
    method: "POST",
    body: JSON.stringify({ variables: args ?? {} }),
  })) as RenderResult;
  await fetch(`/api/v1/prompts/${d.prompt.id}/use`, { method: "POST" }).catch(() => undefined);
  return {
    description: d.prompt.title,
    messages: [{ role: "user" as const, content: { type: "text" as const, text: r.rendered } }],
  };
}
