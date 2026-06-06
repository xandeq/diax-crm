import { Server } from "@modelcontextprotocol/sdk/server/index.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import {
  CallToolRequestSchema,
  ListToolsRequestSchema,
} from "@modelcontextprotocol/sdk/types.js";
import { readFileSync } from "node:fs";
import { homedir } from "node:os";
import { join } from "node:path";

// ---------------------------------------------------------------------------
// Config — env vars first, fallback to ~/.claude/.secrets.env
// ---------------------------------------------------------------------------
function loadSecretsFile(): Record<string, string> {
  try {
    const path = join(homedir(), ".claude", ".secrets.env");
    const content = readFileSync(path, "utf-8");
    const out: Record<string, string> = {};
    for (const line of content.split(/\r?\n/)) {
      const trimmed = line.trim();
      if (!trimmed || trimmed.startsWith("#")) continue;
      const eq = trimmed.indexOf("=");
      if (eq < 0) continue;
      const key = trimmed.slice(0, eq).trim();
      let val = trimmed.slice(eq + 1).trim();
      if ((val.startsWith('"') && val.endsWith('"')) || (val.startsWith("'") && val.endsWith("'"))) {
        val = val.slice(1, -1);
      }
      out[key] = val;
    }
    return out;
  } catch {
    return {};
  }
}

const SECRETS = loadSecretsFile();
const cfg = (k: string, fallback = "") => process.env[k] ?? SECRETS[k] ?? fallback;

const API_BASE = cfg("DIAX_API_BASE_URL", cfg("DIAX_API_URL", "https://api.alexandrequeiroz.com.br")).replace(/\/$/, "");
const SERVICE_API_KEY = cfg("DIAX_SERVICE_API_KEY");
const ADMIN_EMAIL = cfg("DIAX_ADMIN_EMAIL");
const ADMIN_PASSWORD = cfg("DIAX_ADMIN_PASSWORD");

if (!SERVICE_API_KEY && (!ADMIN_EMAIL || !ADMIN_PASSWORD)) {
  process.stderr.write(
    "[diax-mcp] WARNING: neither DIAX_SERVICE_API_KEY nor DIAX_ADMIN_EMAIL/PASSWORD is set — auth will fail\n"
  );
}

// ---------------------------------------------------------------------------
// Auth (JWT cache + re-login on 401)
// ---------------------------------------------------------------------------
let cachedToken: { token: string; expiresAt: number } | null = null;

async function login(): Promise<string> {
  const res = await fetch(`${API_BASE}/api/v1/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email: ADMIN_EMAIL, password: ADMIN_PASSWORD }),
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Login falhou: ${res.status} ${res.statusText} — ${text}`);
  }
  const data = (await res.json()) as { accessToken: string; expiresAtUtc: string };
  cachedToken = {
    token: data.accessToken,
    expiresAt: new Date(data.expiresAtUtc).getTime() - 60_000,
  };
  process.stderr.write(`[diax-mcp] Logged in as ${ADMIN_EMAIL} (válido até ${data.expiresAtUtc})\n`);
  return cachedToken.token;
}

async function getAuthHeader(forceRefresh = false): Promise<Record<string, string>> {
  if (SERVICE_API_KEY) {
    return { "X-Api-Key": SERVICE_API_KEY };
  }
  if (forceRefresh || !cachedToken || Date.now() >= cachedToken.expiresAt) {
    await login();
  }
  return { Authorization: `Bearer ${cachedToken!.token}` };
}

// ---------------------------------------------------------------------------
// HTTP helper
// ---------------------------------------------------------------------------
async function diaxFetch(path: string, options: RequestInit = {}, retried = false): Promise<unknown> {
  const auth = await getAuthHeader(retried);
  const res = await fetch(`${API_BASE}${path}`, {
    ...options,
    headers: {
      "Content-Type": "application/json",
      ...auth,
      ...(options.headers ?? {}),
    },
  });

  if (res.status === 401 && !retried && !SERVICE_API_KEY) {
    cachedToken = null;
    return diaxFetch(path, options, true);
  }

  const text = await res.text();
  if (!res.ok) {
    throw new Error(`DIAX API ${res.status} ${res.statusText}: ${text}`);
  }
  return text ? JSON.parse(text) : null;
}

// ---------------------------------------------------------------------------
// Enum normalization (Tasks usa strings, Customer enums usam números)
// ---------------------------------------------------------------------------
const TASK_STATUS = ["Todo", "InProgress", "Done", "Cancelled"] as const;
const TASK_PRIORITY = ["Low", "Medium", "High", "Urgent"] as const;

function normalizeTaskStatus(v: unknown): string | undefined {
  if (v === undefined || v === null || v === "") return undefined;
  const s = String(v).toLowerCase().replace(/[_\s-]/g, "");
  const map: Record<string, string> = {
    todo: "Todo",
    pending: "Todo",
    open: "Todo",
    inprogress: "InProgress",
    doing: "InProgress",
    done: "Done",
    completed: "Done",
    finished: "Done",
    cancelled: "Cancelled",
    canceled: "Cancelled",
    archived: "Cancelled",
  };
  return map[s] ?? (TASK_STATUS as readonly string[]).find((t) => t.toLowerCase() === s);
}

function normalizeTaskPriority(v: unknown): string | undefined {
  if (v === undefined || v === null || v === "") return undefined;
  const s = String(v).toLowerCase();
  const map: Record<string, string> = {
    low: "Low",
    medium: "Medium",
    med: "Medium",
    high: "High",
    urgent: "Urgent",
  };
  return map[s] ?? (TASK_PRIORITY as readonly string[]).find((p) => p.toLowerCase() === s);
}

// Backend has NO JsonStringEnumConverter — must send int values.
// TaskItemStatus:   Todo=1, InProgress=2, Done=3, Cancelled=4
// TaskItemPriority: Low=1,  Medium=2,     High=3, Urgent=4
function taskStatusToInt(v: unknown): number | undefined {
  const s = normalizeTaskStatus(v);
  if (!s) return undefined;
  const idx = (TASK_STATUS as readonly string[]).indexOf(s);
  return idx < 0 ? undefined : idx + 1;
}
function taskPriorityToInt(v: unknown): number | undefined {
  const s = normalizeTaskPriority(v);
  if (!s) return undefined;
  const idx = (TASK_PRIORITY as readonly string[]).indexOf(s);
  return idx < 0 ? undefined : idx + 1;
}

// ---------------------------------------------------------------------------
// Tool definitions
// ---------------------------------------------------------------------------
const TOOLS = [
  {
    name: "list_leads",
    description:
      "Lista leads do DIAX CRM com filtros opcionais. Retorna lista paginada com nome, email, status, segmento, fonte e última interação.",
    inputSchema: {
      type: "object",
      properties: {
        search: { type: "string", description: "Busca por nome, email ou empresa" },
        status: {
          type: "number",
          description: "0=Lead, 1=Contacted, 2=Qualified, 3=Negotiating, 4=Customer, 5=Inactive, 6=Churned",
        },
        segment: { type: "number", description: "0=Cold, 1=Warm, 2=Hot" },
        source: { type: "number", description: "1=Manual, 4=Scraping, 10=Import, 11=GoogleMaps" },
        page: { type: "number", description: "Página (padrão: 1)" },
        pageSize: { type: "number", description: "Itens por página (padrão: 20, máx: 100)" },
        neverEmailed: { type: "boolean", description: "Filtrar leads que nunca receberam email" },
        hasEmail: { type: "boolean", description: "Filtrar apenas leads com email" },
        hasWhatsApp: { type: "boolean", description: "Filtrar apenas leads com WhatsApp" },
        createdAfterDays: { type: "number", description: "Filtrar leads criados nos últimos N dias" },
      },
    },
  },
  {
    name: "get_lead",
    description: "Retorna os dados completos de um lead/cliente pelo ID (GUID).",
    inputSchema: {
      type: "object",
      properties: { id: { type: "string", description: "ID do lead (GUID)" } },
      required: ["id"],
    },
  },
  {
    name: "create_lead",
    description:
      "Cria um novo lead no DIAX CRM. Apenas 'name' é obrigatório; email é recomendado para deduplicação.",
    inputSchema: {
      type: "object",
      properties: {
        name: { type: "string", description: "Nome completo" },
        email: { type: "string", description: "Email" },
        phone: { type: "string", description: "Telefone fixo" },
        whatsApp: { type: "string", description: "WhatsApp (número)" },
        companyName: { type: "string", description: "Razão social/empresa" },
        website: { type: "string", description: "Site da empresa" },
        notes: { type: "string", description: "Observações" },
        tags: { type: "string", description: "Tags separadas por vírgula" },
        source: { type: "number", description: "1=Manual (padrão), 4=Scraping, 10=Import, 11=GoogleMaps" },
      },
      required: ["name"],
    },
  },
  {
    name: "update_lead_status",
    description:
      "Atualiza o status (etapa do funil) de um lead. 0=Lead → 1=Contacted → 2=Qualified → 3=Negotiating → 4=Customer.",
    inputSchema: {
      type: "object",
      properties: {
        id: { type: "string", description: "ID do lead (GUID)" },
        status: { type: "number", description: "0=Lead, 1=Contacted, 2=Qualified, 3=Negotiating, 4=Customer" },
      },
      required: ["id", "status"],
    },
  },
  {
    name: "update_lead_segment",
    description: "Atualiza a temperatura/segmento de um lead: 0=Cold, 1=Warm, 2=Hot.",
    inputSchema: {
      type: "object",
      properties: {
        id: { type: "string", description: "ID do lead (GUID)" },
        segment: { type: "number", description: "0=Cold, 1=Warm, 2=Hot" },
      },
      required: ["id", "segment"],
    },
  },
  {
    name: "register_contact",
    description:
      "Registra que foi feito contato com um lead (atualiza LastContactedAt e incrementa contador). Use após ligar, enviar email ou WhatsApp.",
    inputSchema: {
      type: "object",
      properties: { id: { type: "string", description: "ID do lead (GUID)" } },
      required: ["id"],
    },
  },
  {
    name: "convert_to_customer",
    description: "Converte um lead em cliente (muda status para 4=Customer).",
    inputSchema: {
      type: "object",
      properties: { id: { type: "string", description: "ID do lead (GUID)" } },
      required: ["id"],
    },
  },
  {
    name: "get_lead_activities",
    description: "Retorna a timeline de atividades e interações de um lead/cliente.",
    inputSchema: {
      type: "object",
      properties: { id: { type: "string", description: "ID do lead (GUID)" } },
      required: ["id"],
    },
  },
  {
    name: "list_customers",
    description:
      "Lista clientes ativos (status=4) do DIAX CRM. Diferente de list_leads, que retorna o funil de prospecção.",
    inputSchema: {
      type: "object",
      properties: {
        search: { type: "string", description: "Busca por nome, email ou empresa" },
        page: { type: "number", description: "Página (padrão: 1)" },
        pageSize: { type: "number", description: "Itens por página (padrão: 20)" },
      },
    },
  },
  {
    name: "list_tasks",
    description: "Lista tarefas do CRM (projetos, to-dos, follow-ups).",
    inputSchema: {
      type: "object",
      properties: {
        status: {
          type: "string",
          description: "Filtro de status: Todo / InProgress / Done / Cancelled (aliases aceitos: pending, completed, etc.)",
        },
        priority: { type: "string", description: "Low / Medium / High / Urgent" },
        overdueOnly: { type: "boolean", description: "Apenas tarefas atrasadas" },
        includeArchived: { type: "boolean", description: "Incluir tarefas canceladas/arquivadas" },
      },
    },
  },
  {
    name: "create_task",
    description: "Cria uma nova tarefa no DIAX CRM.",
    inputSchema: {
      type: "object",
      properties: {
        title: { type: "string", description: "Título da tarefa" },
        description: { type: "string", description: "Descrição detalhada (opcional)" },
        dueDate: { type: "string", description: "Data limite ISO 8601 (ex: 2026-06-01T10:00:00Z)" },
        priority: { type: "string", description: "Low / Medium / High / Urgent (padrão: Medium)" },
      },
      required: ["title"],
    },
  },
  {
    name: "complete_task",
    description: "Marca uma tarefa como concluída (Done).",
    inputSchema: {
      type: "object",
      properties: { id: { type: "string", description: "ID da tarefa (GUID)" } },
      required: ["id"],
    },
  },
  {
    name: "financial_summary",
    description:
      "Retorna resumo financeiro do DIAX CRM: receitas, despesas, saldo por período. Sem parâmetros retorna o mês corrente.",
    inputSchema: {
      type: "object",
      properties: {
        startDate: { type: "string", description: "ISO 8601 (ex: 2026-05-01). Padrão: início do mês corrente." },
        endDate: { type: "string", description: "ISO 8601 (ex: 2026-05-31). Padrão: fim do mês corrente." },
      },
    },
  },
  {
    name: "search_leads",
    description:
      "Busca leads por nome, email ou empresa com termo livre. Retorna os primeiros N resultados com campos essenciais. Atalho rápido para list_leads com search.",
    inputSchema: {
      type: "object",
      properties: {
        query: { type: "string", description: "Termo de busca (nome, email ou empresa)" },
        limit: { type: "number", description: "Máximo de resultados (padrão: 10, máx: 50)" },
        status: { type: "number", description: "Filtrar por status: 0=Lead, 1=Contacted, 2=Qualified, 3=Negotiating, 4=Customer" },
      },
      required: ["query"],
    },
  },
  {
    name: "search_customers",
    description:
      "Busca clientes ativos (status=Customer) por nome, email ou empresa. Atalho rápido para list_customers com search.",
    inputSchema: {
      type: "object",
      properties: {
        query: { type: "string", description: "Termo de busca (nome, email ou empresa)" },
        limit: { type: "number", description: "Máximo de resultados (padrão: 10)" },
      },
      required: ["query"],
    },
  },
  {
    name: "dashboard_summary",
    description:
      "Visão geral do CRM: contagem de leads por status do funil, tarefas pendentes/atrasadas, clientes ativos e resumo financeiro do mês corrente. Use como ponto de partida de uma sessão ou para check-in rápido.",
    inputSchema: {
      type: "object",
      properties: {},
    },
  },
];

// ---------------------------------------------------------------------------
// Tool handlers
// ---------------------------------------------------------------------------
async function handleTool(name: string, args: Record<string, unknown>): Promise<string> {
  switch (name) {
    case "list_leads": {
      const p = new URLSearchParams();
      if (args.search) p.set("search", String(args.search));
      if (args.status !== undefined) p.set("status", String(args.status));
      if (args.segment !== undefined) p.set("segment", String(args.segment));
      if (args.source !== undefined) p.set("source", String(args.source));
      if (args.hasEmail !== undefined) p.set("hasEmail", String(args.hasEmail));
      if (args.hasWhatsApp !== undefined) p.set("hasWhatsApp", String(args.hasWhatsApp));
      if (args.neverEmailed !== undefined) p.set("neverEmailed", String(args.neverEmailed));
      if (args.createdAfterDays) {
        const d = new Date();
        d.setDate(d.getDate() - Number(args.createdAfterDays));
        p.set("createdAfter", d.toISOString());
      }
      p.set("page", String(args.page ?? 1));
      p.set("pageSize", String(Math.min(Number(args.pageSize ?? 20), 100)));
      const data = await diaxFetch(`/api/v1/leads?${p}`);
      return JSON.stringify(data, null, 2);
    }

    case "get_lead": {
      const data = await diaxFetch(`/api/v1/leads/${args.id}`);
      return JSON.stringify(data, null, 2);
    }

    case "create_lead": {
      const body: Record<string, unknown> = {
        name: args.name,
        email: args.email ?? "",
        source: args.source ?? 1,
      };
      if (args.phone) body.phone = args.phone;
      if (args.whatsApp) body.whatsApp = args.whatsApp;
      if (args.companyName) body.companyName = args.companyName;
      if (args.website) body.website = args.website;
      if (args.notes) body.notes = args.notes;
      if (args.tags) body.tags = args.tags;
      const data = await diaxFetch("/api/v1/leads", {
        method: "POST",
        body: JSON.stringify(body),
      });
      return JSON.stringify(data, null, 2);
    }

    case "update_lead_status": {
      const data = await diaxFetch(`/api/v1/customers/${args.id}/status`, {
        method: "PATCH",
        body: JSON.stringify({ status: args.status }),
      });
      return JSON.stringify(data, null, 2);
    }

    case "update_lead_segment": {
      const data = await diaxFetch(`/api/v1/leads/${args.id}/segment`, {
        method: "PATCH",
        body: JSON.stringify({ segment: args.segment }),
      });
      return JSON.stringify(data, null, 2);
    }

    case "register_contact": {
      const data = await diaxFetch(`/api/v1/customers/${args.id}/contact`, { method: "POST" });
      return JSON.stringify(data, null, 2);
    }

    case "convert_to_customer": {
      const data = await diaxFetch(`/api/v1/customers/${args.id}/convert`, { method: "POST" });
      return JSON.stringify(data, null, 2);
    }

    case "get_lead_activities": {
      const data = await diaxFetch(`/api/v1/customers/${args.id}/activities`);
      return JSON.stringify(data, null, 2);
    }

    case "list_customers": {
      const p = new URLSearchParams();
      if (args.search) p.set("search", String(args.search));
      p.set("onlyCustomers", "true");
      p.set("page", String(args.page ?? 1));
      p.set("pageSize", String(Math.min(Number(args.pageSize ?? 20), 100)));
      const data = await diaxFetch(`/api/v1/customers?${p}`);
      return JSON.stringify(data, null, 2);
    }

    case "list_tasks": {
      const p = new URLSearchParams();
      const status = taskStatusToInt(args.status);
      const priority = taskPriorityToInt(args.priority);
      if (status !== undefined) p.set("status", String(status));
      if (priority !== undefined) p.set("priority", String(priority));
      if (args.overdueOnly !== undefined) p.set("overdueOnly", String(args.overdueOnly));
      if (args.includeArchived !== undefined) p.set("includeArchived", String(args.includeArchived));
      const data = await diaxFetch(`/api/v1/tasks?${p}`);
      return JSON.stringify(data, null, 2);
    }

    case "create_task": {
      const priority = taskPriorityToInt(args.priority) ?? 2; // Medium
      const body: Record<string, unknown> = {
        title: args.title,
        description: args.description ?? null,
        priority,
        dueDate: args.dueDate ?? null,
      };
      const data = await diaxFetch("/api/v1/tasks", {
        method: "POST",
        body: JSON.stringify(body),
      });
      return JSON.stringify(data, null, 2);
    }

    case "complete_task": {
      const data = await diaxFetch(`/api/v1/tasks/${args.id}/complete`, { method: "POST" });
      return JSON.stringify(data, null, 2);
    }

    case "financial_summary": {
      const now = new Date();
      const defaultStart = new Date(now.getFullYear(), now.getMonth(), 1).toISOString();
      const defaultEnd = new Date(now.getFullYear(), now.getMonth() + 1, 0, 23, 59, 59).toISOString();
      const p = new URLSearchParams({
        startDate: String(args.startDate ?? defaultStart),
        endDate: String(args.endDate ?? defaultEnd),
      });
      const data = await diaxFetch(`/api/v1/finance/summary?${p}`);
      return JSON.stringify(data, null, 2);
    }

    case "search_leads": {
      const p = new URLSearchParams();
      p.set("search", String(args.query));
      p.set("pageSize", String(Math.min(Number(args.limit ?? 10), 50)));
      p.set("page", "1");
      if (args.status !== undefined) p.set("status", String(args.status));
      const data = await diaxFetch(`/api/v1/leads?${p}`);
      return JSON.stringify(data, null, 2);
    }

    case "search_customers": {
      const p = new URLSearchParams();
      p.set("search", String(args.query));
      p.set("onlyCustomers", "true");
      p.set("pageSize", String(Math.min(Number(args.limit ?? 10), 50)));
      p.set("page", "1");
      const data = await diaxFetch(`/api/v1/customers?${p}`);
      return JSON.stringify(data, null, 2);
    }

    case "dashboard_summary": {
      const now = new Date();
      const monthStart = new Date(now.getFullYear(), now.getMonth(), 1).toISOString();
      const monthEnd = new Date(now.getFullYear(), now.getMonth() + 1, 0, 23, 59, 59).toISOString();

      const [leadsAll, tasks, customers, finance] = await Promise.all([
        diaxFetch("/api/v1/leads?pageSize=1&page=1"),
        diaxFetch("/api/v1/tasks?pageSize=100&page=1"),
        diaxFetch("/api/v1/customers?onlyCustomers=true&pageSize=1&page=1"),
        diaxFetch(`/api/v1/finance/summary?startDate=${monthStart}&endDate=${monthEnd}`),
      ]);

      const statusLabels: Record<number, string> = {
        0: "Lead", 1: "Contacted", 2: "Qualified", 3: "Negotiating",
        4: "Customer", 5: "Inactive", 6: "Churned",
      };

      const funnelCounts: Record<string, number> = {};
      for (const [num, label] of Object.entries(statusLabels)) {
        const p = new URLSearchParams({ status: num, pageSize: "1", page: "1" });
        const res = (await diaxFetch(`/api/v1/leads?${p}`)) as { totalCount?: number };
        funnelCounts[label] = res?.totalCount ?? 0;
      }

      const taskList = (tasks as { items?: Array<{ status: number; dueDate?: string }> })?.items ?? [];
      const pendingTasks = taskList.filter((t) => t.status === 1 || t.status === 2).length;
      const overdueTasks = taskList.filter((t) => {
        if (!t.dueDate) return false;
        return (t.status === 1 || t.status === 2) && new Date(t.dueDate) < now;
      }).length;

      const summary = {
        generatedAt: now.toISOString(),
        funnel: funnelCounts,
        activeCustomers: (customers as { totalCount?: number })?.totalCount ?? 0,
        tasks: { pending: pendingTasks, overdue: overdueTasks },
        finance: {
          period: `${monthStart.slice(0, 10)} → ${monthEnd.slice(0, 10)}`,
          ...(finance as object),
        },
      };

      return JSON.stringify(summary, null, 2);
    }

    default:
      throw new Error(`Unknown tool: ${name}`);
  }
}

// ---------------------------------------------------------------------------
// MCP Server
// ---------------------------------------------------------------------------
const server = new Server(
  { name: "diax-crm", version: "1.0.0" },
  { capabilities: { tools: {} } }
);

server.setRequestHandler(ListToolsRequestSchema, async () => ({ tools: TOOLS }));

server.setRequestHandler(CallToolRequestSchema, async (req) => {
  const { name, arguments: args } = req.params;
  try {
    const result = await handleTool(name, (args ?? {}) as Record<string, unknown>);
    return { content: [{ type: "text", text: result }] };
  } catch (err) {
    const msg = err instanceof Error ? err.message : String(err);
    return {
      content: [{ type: "text", text: `Erro: ${msg}` }],
      isError: true,
    };
  }
});

const transport = new StdioServerTransport();
await server.connect(transport);
process.stderr.write(`[diax-mcp] Server running — connected to ${API_BASE}\n`);
