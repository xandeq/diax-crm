# 📊 Plano de Melhorias - Grids de Customers e Leads

**Data:** 15/02/2026
**Objetivo:** Evoluir grids mantendo arquitetura e padrão existente

---

## 🔍 Análise do Estado Atual

### O que já existe

**Frontend:**
- ✅ Next.js 14 com App Router
- ✅ React 18 + TypeScript
- ✅ Tailwind CSS
- ✅ **@tanstack/react-table** v8.21.3 (JÁ INSTALADO!)
- ✅ Radix UI (Dialog, Select, Checkbox, Dropdown Menu)
- ✅ Lucide React (ícones)
- ✅ React Hook Form + Zod
- ✅ Componentes UI prontos: Button, Table, Badge, Dialog, Checkbox, etc.

**Backend:**
- ✅ API paginada: `GET /api/v1/customers?page=X&pageSize=Y&search=Z&status=W`
- ✅ Endpoints: Create, Update, Delete, GetById
- ✅ Filtros: search, status
- ✅ Índices otimizados no banco (Name, Email, Status, CreatedAt)

**Grid Atual (/customers):**
- ✅ Tabela HTML simples (não usa @tanstack/react-table)
- ✅ Paginação básica
- ✅ Busca por texto
- ✅ Status badges com cores
- ✅ Ações por linha (Edit, Delete)
- ❌ Sem ordenação
- ❌ Sem seleção múltipla
- ❌ Sem ações em massa
- ❌ Sem export
- ❌ Sem colunas dinâmicas

**Grid Atual (/leads):**
- ✅ Usa componentes UI (Table, Badge, Button, Dialog)
- ✅ Ações extras: WhatsApp, Email, Converter para Cliente
- ✅ Similar ao /customers em funcionalidades

---

## 🎯 Proposta de Implementação

### PRIORIDADE 1: Funcionalidades Essenciais

#### 1. Migrar para @tanstack/react-table ✨
**Por quê:** Já está instalado! Facilita ordenação, seleção, export

**O que fazer:**
- Substituir `<table>` HTML por TanStack Table
- Manter estilo visual idêntico (Tailwind)
- Aproveitar hooks: `useReactTable`, `getCoreRowModel`

#### 2. Ordenação de Colunas 🔄
**Implementação:**
- Client-side: Ordenar dados já carregados
- Ícones: Lucide `ArrowUp`, `ArrowDown`, `ArrowUpDown`
- Colunas ordenáveis: Nome, Email, Empresa, Status, Data

**Backend:** Não precisa mudar (ordenação client-side)

#### 3. Seleção Múltipla de Linhas ☑️
**Implementação:**
- Checkbox na primeira coluna
- Checkbox no header (selecionar todos)
- Estado: `const [rowSelection, setRowSelection] = useState({})`
- Componente: `@radix-ui/react-checkbox` (já instalado)

#### 4. Ações em Massa 🚀
**Quando houver seleção:**
- Mostrar barra de ações fixa no topo
- Ações: Deletar selecionados, Exportar selecionados, Mudar status

**Backend:**
- Criar endpoint: `DELETE /api/v1/customers/bulk` (body: ids[])
- Criar endpoint: `PATCH /api/v1/customers/bulk/status` (body: ids[], status)

#### 5. Colunas Dinâmicas (Mostrar/Ocultar) 👁️
**Implementação:**
- Dropdown menu (Radix já instalado)
- Checkbox por coluna
- LocalStorage para persistir preferências
- Ícone: Lucide `Settings2` ou `Columns`

#### 6. Exportação Básica 📥
**Formatos:**
- CSV (mais simples, sem libs extras)
- Implementação client-side com dados já carregados

**Como:**
```typescript
const exportToCSV = (data, filename) => {
  const csv = data.map(row => Object.values(row).join(',')).join('\n');
  const blob = new Blob([csv], { type: 'text/csv' });
  const url = URL.createObjectURL(blob);
  // Download automático
};
```

#### 7. Indicadores Visuais Melhores 🎨
**Melhorias:**
- Status badges: adicionar ícones (Lucide)
- Hover states mais evidentes
- Ícones de tipo de pessoa (PF/PJ)
- Tags coloridas se houver
- Indicador de WhatsApp ativo (verde)

---

### PRIORIDADE 2: Se Fizer Sentido

#### 8. Busca Global na Listagem 🔍
**Implementação:**
- Filter state do TanStack Table
- Busca client-side nos dados carregados
- Highlight de resultados

#### 9. Inline Editing Simples ✏️
**Campos estratégicos:**
- Tags (simples, não crítico)
- Notes (texto curto)

**Implementação:**
- Double-click para editar
- Input inline com auto-save
- Endpoint: `PATCH /api/v1/customers/{id}/field`

---

## 📦 Componentes a Criar

### 1. `DataTable.tsx` (Reutilizável)
```tsx
interface DataTableProps<TData> {
  data: TData[];
  columns: ColumnDef<TData>[];
  loading?: boolean;
  onRowSelectionChange?: (rows: TData[]) => void;
  searchable?: boolean;
  exportable?: boolean;
}
```

### 2. `TableActions.tsx` (Barra de Ações em Massa)
```tsx
interface TableActionsProps {
  selectedCount: number;
  onDelete: () => void;
  onExport: () => void;
  onClearSelection: () => void;
}
```

### 3. `ColumnVisibilityToggle.tsx`
```tsx
interface ColumnVisibilityToggleProps {
  table: Table<any>;
}
```

---

## 🚀 Plano de Execução

### Fase 1: Setup (30 min)
1. Criar `components/data-table/DataTable.tsx`
2. Criar `components/data-table/TableActions.tsx`
3. Criar `components/data-table/ColumnVisibilityToggle.tsx`
4. Criar `hooks/useTableSelection.ts`
5. Criar `lib/export.ts` (funções de export)

### Fase 2: /customers (1h)
1. Migrar para TanStack Table
2. Adicionar ordenação
3. Adicionar seleção múltipla
4. Adicionar ações em massa
5. Adicionar visibility toggle
6. Adicionar export CSV
7. Melhorar badges e ícones

### Fase 3: /leads (45 min)
1. Aplicar mesmas melhorias
2. Manter ações específicas (WhatsApp, Email, Converter)

### Fase 4: Backend Endpoints (30 min)
1. `DELETE /api/v1/customers/bulk`
2. `PATCH /api/v1/customers/bulk/status`
3. Documentação Swagger

---

## 📋 Estrutura de Arquivos

```
crm-web/src/
├── components/
│   ├── data-table/
│   │   ├── DataTable.tsx          # Tabela reutilizável
│   │   ├── DataTableToolbar.tsx   # Barra de ferramentas
│   │   ├── DataTablePagination.tsx # Paginação
│   │   ├── TableActions.tsx       # Ações em massa
│   │   └── ColumnVisibility.tsx   # Mostrar/ocultar colunas
│   └── ui/
│       └── (já existentes)
├── hooks/
│   └── useTableSelection.ts       # Hook de seleção
├── lib/
│   ├── export.ts                  # Funções de export
│   └── table-utils.ts             # Utils de tabela
└── app/
    ├── customers/
    │   └── page.tsx               # Atualizado
    └── leads/
        └── page.tsx               # Atualizado
```

---

## 🎨 Visual Mockup (Descrição)

```
┌─────────────────────────────────────────────────────────────┐
│ Gerenciamento de Clientes                  [+ Novo Cliente] │
├─────────────────────────────────────────────────────────────┤
│ [🔍 Buscar...] [Status ▼] [Colunas ⚙️] [Exportar 📥]       │
├─────────────────────────────────────────────────────────────┤
│ ┌─────────────────────────────────────────────────────────┐ │
│ │ ✓ 3 itens selecionados   [🗑️ Deletar] [✓ Status] [✕]  │ │
│ └─────────────────────────────────────────────────────────┘ │
├─────────────────────────────────────────────────────────────┤
│ ☑ │ Nome ↕ │ Email ↕ │ Telefone │ Status ↕ │ Ações       │
│───┼─────────┼─────────┼──────────┼──────────┼─────────────│
│ ☑ │ João    │ j@e.com │ 119876.. │ 🟢 Lead  │ ✏️ 🗑️ 💬   │
│ ☐ │ Maria   │ m@e.com │ 219876.. │ 🔵 Client│ ✏️ 🗑️ 💬   │
└─────────────────────────────────────────────────────────────┘
```

---

## ⚠️ Pontos de Atenção

### Não Fazer:
- ❌ Mudar arquitetura drasticamente
- ❌ Adicionar libs pesadas (AG Grid, Material Table)
- ❌ Backend muito complexo
- ❌ Quebrar compatibilidade visual

### Fazer:
- ✅ Usar o que já existe (@tanstack/react-table)
- ✅ Manter Tailwind + Radix UI
- ✅ Componentizar para reutilizar
- ✅ Testes incrementais
- ✅ Mobile-friendly

---

## 📊 Estimativa de Impacto

| Funcionalidade | Complexidade | Impacto UX | Tempo |
|----------------|--------------|------------|-------|
| TanStack Table | Média | Alto | 30min |
| Ordenação | Baixa | Alto | 15min |
| Seleção Múltipla | Baixa | Alto | 20min |
| Ações em Massa | Média | Alto | 40min |
| Colunas Dinâmicas | Baixa | Médio | 20min |
| Export CSV | Baixa | Alto | 15min |
| Indicadores Visuais | Baixa | Médio | 15min |
| **TOTAL** | - | - | **~3h** |

---

## ✅ Checklist de Implementação

### Setup
- [ ] Criar `DataTable.tsx` componente
- [ ] Criar `TableActions.tsx` componente
- [ ] Criar `ColumnVisibility.tsx` componente
- [ ] Criar `useTableSelection.ts` hook
- [ ] Criar `export.ts` utils

### /customers
- [ ] Migrar para TanStack Table
- [ ] Implementar ordenação
- [ ] Implementar seleção múltipla
- [ ] Implementar ações em massa
- [ ] Implementar visibility toggle
- [ ] Implementar export
- [ ] Melhorar badges

### /leads
- [ ] Aplicar DataTable
- [ ] Manter ações específicas
- [ ] Testar integração

### Backend
- [ ] Endpoint DELETE bulk
- [ ] Endpoint PATCH bulk status
- [ ] Testes
- [ ] Deploy

---

## 🎯 Resultado Esperado

**Produtividade:**
- ⚡ Seleção de múltiplos itens em 1 clique
- ⚡ Deletar 50 leads de uma vez
- ⚡ Ordenar por qualquer coluna
- ⚡ Exportar dados filtrados
- ⚡ Personalizar colunas visíveis

**Profissionalismo:**
- 🎨 Ícones e badges informativos
- 🎨 Interações suaves
- 🎨 Feedback visual claro
- 🎨 Consistência com design system

**Manutenibilidade:**
- 🔧 Componente reutilizável
- 🔧 Código limpo e documentado
- 🔧 Fácil adicionar novos grids

---

**Status:** 📋 PLANEJAMENTO COMPLETO
**Próximo passo:** Implementação Fase 1 (Setup)
