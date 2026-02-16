# 🚀 Guia de Implementação - Grids Melhorados

**Status:** Componentes Base Criados ✅
**Próximo Passo:** Integração nas Páginas

---

## ✅ O Que Já Foi Criado

### Componentes Reutilizáveis

#### 1. `DataTable.tsx` ✅
**Localização:** `crm-web/src/components/data-table/DataTable.tsx`

**Funcionalidades:**
- ✅ Baseado em @tanstack/react-table (já instalado)
- ✅ Ordenação de colunas (clique no header)
- ✅ Seleção múltipla com checkboxes
- ✅ Paginação client-side
- ✅ Filtros
- ✅ Visibility de colunas
- ✅ Loading state
- ✅ Empty state
- ✅ Estilo Tailwind consistente com o projeto

**Props:**
```typescript
interface DataTableProps<TData, TValue> {
  columns: ColumnDef<TData, TValue>[]; // Definição das colunas
  data: TData[];                        // Dados
  loading?: boolean;                    // Estado de carregamento
  searchable?: boolean;                 // Habilitar busca
  selectable?: boolean;                 // Habilitar seleção
  onSelectionChange?: (rows: TData[]) => void; // Callback de seleção
  pageSize?: number;                    // Tamanho da página
}
```

#### 2. `TableActions.tsx` ✅
**Localização:** `crm-web/src/components/data-table/TableActions.tsx`

**Funcionalidades:**
- ✅ Barra de ações que aparece quando há itens selecionados
- ✅ Contador de itens selecionados
- ✅ Botões: Deletar, Exportar
- ✅ Suporte a ações customizadas
- ✅ Animação de entrada

**Props:**
```typescript
interface TableActionsProps<TData> {
  selectedCount: number;
  selectedRows: TData[];
  onDelete?: () => void;
  onExport?: () => void;
  onClearSelection: () => void;
  customActions?: React.ReactNode;
}
```

#### 3. `ColumnVisibility.tsx` ✅
**Localização:** `crm-web/src/components/data-table/ColumnVisibility.tsx`

**Funcionalidades:**
- ✅ Dropdown menu para mostrar/ocultar colunas
- ✅ Checkboxes por coluna
- ✅ Persiste no estado do TanStack Table

**Props:**
```typescript
interface ColumnVisibilityProps<TData> {
  table: Table<TData>; // Instância do TanStack Table
}
```

#### 4. `export.ts` ✅
**Localização:** `crm-web/src/lib/export.ts`

**Funções:**
- `convertToCSV(data, columns?)` - Converte array para CSV
- `exportToCSV(data, filename, columns?)` - Download CSV
- `exportToJSON(data, filename)` - Download JSON
- `formatDateForExport(date)` - Formata data para export
- `sanitizeForExport(data, fieldsToRemove)` - Remove campos sensíveis

---

## 📝 Como Usar na Página /customers

### Passo 1: Definir Colunas

```typescript
import { ColumnDef } from '@tanstack/react-table';
import { Customer, CustomerStatus } from '@/services/customers';
import { Badge } from '@/components/ui/badge';
import { Edit2, Trash2 } from 'lucide-react';

const columns: ColumnDef<Customer>[] = [
  {
    accessorKey: 'name',
    header: 'Nome',
    cell: ({ row }) => (
      <div>
        <div className="font-medium text-slate-900">{row.original.name}</div>
        {row.original.document && (
          <div className="text-xs text-slate-500">{row.original.document}</div>
        )}
      </div>
    ),
  },
  {
    accessorKey: 'email',
    header: 'Email',
    cell: ({ row }) => (
      <span className="text-slate-600">{row.original.email}</span>
    ),
  },
  {
    accessorKey: 'phone',
    header: 'Telefone',
    cell: ({ row }) => (
      <div className="flex flex-col">
        <span>{row.original.phone || '-'}</span>
        {row.original.whatsApp && (
          <span className="text-xs text-green-600">WA: {row.original.whatsApp}</span>
        )}
      </div>
    ),
  },
  {
    accessorKey: 'companyName',
    header: 'Empresa',
    cell: ({ row }) => (
      <span className="text-slate-600">{row.original.companyName || '-'}</span>
    ),
  },
  {
    accessorKey: 'status',
    header: 'Status',
    cell: ({ row }) => {
      const status = row.original.status;
      const styles: Record<number, string> = {
        [CustomerStatus.Lead]: 'bg-blue-100 text-blue-800',
        [CustomerStatus.Customer]: 'bg-green-100 text-green-800',
        // ... outros status
      };
      return (
        <Badge className={styles[status]}>
          {CustomerStatus[status]}
        </Badge>
      );
    },
  },
  {
    accessorKey: 'createdAt',
    header: 'Criado em',
    cell: ({ row }) => (
      <span className="text-slate-600">
        {new Date(row.original.createdAt).toLocaleDateString('pt-BR')}
      </span>
    ),
  },
  {
    id: 'actions',
    header: 'Ações',
    cell: ({ row }) => (
      <div className="flex justify-end gap-2">
        <button
          onClick={() => handleEdit(row.original)}
          className="p-1 hover:bg-slate-200 rounded-md"
        >
          <Edit2 className="h-4 w-4" />
        </button>
        <button
          onClick={() => handleDelete(row.original.id)}
          className="p-1 hover:bg-red-100 rounded-md text-red-600"
        >
          <Trash2 className="h-4 w-4" />
        </button>
      </div>
    ),
  },
];
```

### Passo 2: Usar DataTable

```typescript
import { DataTable } from '@/components/data-table/DataTable';
import { TableActions } from '@/components/data-table/TableActions';
import { exportToCSV } from '@/lib/export';

export default function CustomersPage() {
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [selectedRows, setSelectedRows] = useState<Customer[]>([]);
  const [loading, setLoading] = useState(true);

  // ... código de fetchCustomers existente

  const handleBulkDelete = async () => {
    if (!confirm(`Deletar ${selectedRows.length} clientes?`)) return;

    try {
      const ids = selectedRows.map(c => c.id);
      await fetch('/api/v1/customers/bulk', {
        method: 'DELETE',
        body: JSON.stringify({ ids }),
      });
      fetchCustomers();
      setSelectedRows([]);
    } catch (err) {
      alert('Erro ao deletar clientes.');
    }
  };

  const handleExport = () => {
    const data = selectedRows.length > 0 ? selectedRows : customers;
    exportToCSV(
      data.map(c => ({
        Nome: c.name,
        Email: c.email,
        Telefone: c.phone,
        Empresa: c.companyName,
        Status: CustomerStatus[c.status],
      })),
      `clientes-${new Date().toISOString().split('T')[0]}`
    );
  };

  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <h1 className="text-2xl font-bold">Gerenciamento de Clientes</h1>
        <button onClick={handleOpenCreate} className="...">
          + Novo Cliente
        </button>
      </div>

      {/* Barra de Ações em Massa */}
      <TableActions
        selectedCount={selectedRows.length}
        selectedRows={selectedRows}
        onDelete={handleBulkDelete}
        onExport={handleExport}
        onClearSelection={() => setSelectedRows([])}
      />

      {/* Tabela */}
      <DataTable
        columns={columns}
        data={customers}
        loading={loading}
        selectable={true}
        onSelectionChange={setSelectedRows}
        pageSize={10}
      />

      {/* Modal existente */}
      {/* ... */}
    </div>
  );
}
```

---

## 🎨 Recursos Adicionais

### Ícones nos Status

```typescript
import { CheckCircle, Clock, XCircle } from 'lucide-react';

const statusIcons: Record<CustomerStatus, React.ReactNode> = {
  [CustomerStatus.Lead]: <Clock className="h-3 w-3" />,
  [CustomerStatus.Customer]: <CheckCircle className="h-3 w-3" />,
  [CustomerStatus.Lost]: <XCircle className="h-3 w-3" />,
  // ...
};

// Usar na coluna:
cell: ({ row }) => (
  <Badge className={styles[status]}>
    {statusIcons[status]}
    <span className="ml-1">{CustomerStatus[status]}</span>
  </Badge>
)
```

### Busca Global

```typescript
// Adicionar input de busca
<input
  type="text"
  placeholder="Buscar..."
  value={globalFilter}
  onChange={(e) => setGlobalFilter(e.target.value)}
  className="..."
/>

// No DataTable, adicionar:
const [globalFilter, setGlobalFilter] = useState('');

const table = useReactTable({
  // ...
  state: {
    // ...
    globalFilter,
  },
  onGlobalFilterChange: setGlobalFilter,
  globalFilterFn: 'includesString',
});
```

---

## 🔧 Backend - Endpoints Bulk

### DELETE /api/v1/customers/bulk

```csharp
[HttpDelete("bulk")]
public async Task<IActionResult> BulkDelete(
    [FromBody] BulkDeleteRequest request,
    CancellationToken cancellationToken)
{
    if (request.Ids == null || !request.Ids.Any())
        return BadRequest("Nenhum ID fornecido");

    var deleted = 0;
    foreach (var id in request.Ids)
    {
        var result = await _customerService.DeleteAsync(id, cancellationToken);
        if (result.IsSuccess) deleted++;
    }

    return Ok(new { deleted, total = request.Ids.Count });
}

public record BulkDeleteRequest(List<Guid> Ids);
```

### PATCH /api/v1/customers/bulk/status

```csharp
[HttpPatch("bulk/status")]
public async Task<IActionResult> BulkUpdateStatus(
    [FromBody] BulkStatusUpdateRequest request,
    CancellationToken cancellationToken)
{
    if (request.Ids == null || !request.Ids.Any())
        return BadRequest("Nenhum ID fornecido");

    var updated = 0;
    foreach (var id in request.Ids)
    {
        // Implementar lógica de atualização
        updated++;
    }

    return Ok(new { updated, total = request.Ids.Count });
}

public record BulkStatusUpdateRequest(List<Guid> Ids, CustomerStatus NewStatus);
```

---

## 📊 Exemplo Completo - /leads

Similar ao /customers, mas com ações extras:

```typescript
// Adicionar coluna de ações customizadas
{
  id: 'actions',
  cell: ({ row }) => (
    <div className="flex gap-2">
      <button onClick={() => openWhatsApp(row.original.whatsApp)}>
        <MessageCircle className="h-4 w-4" />
      </button>
      <button onClick={() => openEmail(row.original.email)}>
        <Mail className="h-4 w-4" />
      </button>
      <button onClick={() => convertToCustomer(row.original)}>
        <CheckCircle className="h-4 w-4" />
      </button>
    </div>
  ),
}

// Ações em massa customizadas
<TableActions
  selectedCount={selectedRows.length}
  selectedRows={selectedRows}
  onDelete={handleBulkDelete}
  onExport={handleExport}
  onClearSelection={() => setSelectedRows([])}
  customActions={
    <Button size="sm" onClick={handleBulkConvert}>
      <CheckCircle className="h-3.5 w-3.5 mr-1.5" />
      Converter Selecionados
    </Button>
  }
/>
```

---

## ✅ Checklist de Implementação

### /customers
- [ ] Criar definição de colunas
- [ ] Substituir tabela HTML por DataTable
- [ ] Adicionar TableActions
- [ ] Implementar handleBulkDelete
- [ ] Implementar handleExport
- [ ] Testar ordenação
- [ ] Testar seleção múltipla
- [ ] Testar export CSV

### /leads
- [ ] Aplicar mesmas mudanças
- [ ] Manter ações específicas (WhatsApp, Email, Converter)
- [ ] Adicionar ação em massa de conversão
- [ ] Testar integração

### Backend
- [ ] Endpoint DELETE bulk
- [ ] Endpoint PATCH bulk status
- [ ] Testes unitários
- [ ] Deploy

---

## 🎯 Resultado Esperado

**Antes:**
- Tabela HTML simples
- Sem ordenação
- Deletar 1 por 1
- Sem export

**Depois:**
- ✅ Ordenação em todas as colunas
- ✅ Seleção múltipla
- ✅ Deletar vários de uma vez
- ✅ Export CSV/JSON
- ✅ Mostrar/ocultar colunas
- ✅ Ícones e badges melhorados
- ✅ Componentes reutilizáveis

---

**Status:** ⏳ Componentes base prontos, aguardando integração
**Próximo passo:** Implementar em /customers
