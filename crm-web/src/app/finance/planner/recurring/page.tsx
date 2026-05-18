'use client';

import { useCallback, useEffect, useState } from 'react';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Checkbox } from '@/components/ui/checkbox';
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { CategoryApplicableTo, financeService, TransactionCategory } from '@/services/finance';
import { plannerService } from '@/services/plannerService';
import {
  CreateRecurringTransactionRequest,
  FrequencyType,
  FREQUENCY_TYPE_LABELS,
  PaymentMethod,
  PAYMENT_METHOD_LABELS,
  RecurringItemKind,
  RECURRING_ITEM_KIND_LABELS,
  RecurringTransaction,
  TransactionType,
  TRANSACTION_TYPE_LABELS,
  UpdateRecurringTransactionRequest,
} from '@/types/planner';
import {
  CalendarDays,
  Loader2,
  Pencil,
  Plus,
  RefreshCw,
  RepeatIcon,
  Trash2,
  TrendingDown,
  TrendingUp,
} from 'lucide-react';

const BRL = (v: number) =>
  new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(v);

const EMPTY_CREATE: CreateRecurringTransactionRequest = {
  type: TransactionType.Expense,
  itemKind: RecurringItemKind.Standard,
  description: '',
  details: '',
  amount: 0,
  categoryId: '',
  frequencyType: FrequencyType.Monthly,
  dayOfMonth: 1,
  paymentMethod: PaymentMethod.Pix,
  priority: 50,
  hasVariableAmount: false,
};

type FilterType = 'all' | 'income' | 'expense';
type FilterStatus = 'all' | 'active' | 'inactive';

export default function RecurringTransactionsPage() {
  const [items, setItems] = useState<RecurringTransaction[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [filterType, setFilterType] = useState<FilterType>('all');
  const [filterStatus, setFilterStatus] = useState<FilterStatus>('active');

  const [showCreate, setShowCreate] = useState(false);
  const [editItem, setEditItem] = useState<RecurringTransaction | null>(null);
  const [deleteItem, setDeleteItem] = useState<RecurringTransaction | null>(null);

  const [createForm, setCreateForm] = useState<CreateRecurringTransactionRequest>(EMPTY_CREATE);
  const [editForm, setEditForm] = useState<UpdateRecurringTransactionRequest | null>(null);

  const [categories, setCategories] = useState<TransactionCategory[]>([]);

  const loadData = useCallback(async () => {
    setLoading(true);
    try {
      const [txs, cats] = await Promise.all([
        plannerService.getRecurringTransactions(),
        financeService.getAllTransactionCategories(),
      ]);
      setItems(txs);
      setCategories(cats);
    } catch {
      // keep previous state on error
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { loadData(); }, [loadData]);

  const categoriesForType = (type: TransactionType) => {
    const applicableTo = type === TransactionType.Income ? CategoryApplicableTo.Income : CategoryApplicableTo.Expense;
    return categories.filter(c => c.applicableTo === applicableTo || c.applicableTo === CategoryApplicableTo.Both);
  };

  const filtered = items.filter(item => {
    if (filterType === 'income' && item.type !== TransactionType.Income) return false;
    if (filterType === 'expense' && item.type !== TransactionType.Expense) return false;
    if (filterStatus === 'active' && !item.isActive) return false;
    if (filterStatus === 'inactive' && item.isActive) return false;
    return true;
  });

  const totalActiveIncome = items
    .filter(i => i.isActive && i.type === TransactionType.Income)
    .reduce((s, i) => s + i.amount, 0);
  const totalActiveExpense = items
    .filter(i => i.isActive && i.type === TransactionType.Expense)
    .reduce((s, i) => s + i.amount, 0);

  async function handleCreate() {
    if (!createForm.description || !createForm.categoryId || createForm.amount <= 0) return;
    setSaving(true);
    try {
      await plannerService.createRecurringTransaction(createForm);
      setShowCreate(false);
      setCreateForm(EMPTY_CREATE);
      await loadData();
    } catch {
      // silent
    } finally {
      setSaving(false);
    }
  }

  function openEdit(item: RecurringTransaction) {
    setEditItem(item);
    setEditForm({
      type: item.type,
      itemKind: item.itemKind,
      description: item.description,
      details: item.details ?? '',
      amount: item.amount,
      categoryId: item.categoryId,
      frequencyType: item.frequencyType,
      dayOfMonth: item.dayOfMonth,
      startDate: item.startDate.split('T')[0],
      endDate: item.endDate ? item.endDate.split('T')[0] : undefined,
      paymentMethod: item.paymentMethod,
      creditCardId: item.creditCardId,
      financialAccountId: item.financialAccountId,
      isActive: item.isActive,
      priority: item.priority,
      hasVariableAmount: item.hasVariableAmount,
    });
  }

  async function handleEdit() {
    if (!editItem || !editForm) return;
    setSaving(true);
    try {
      await plannerService.updateRecurringTransaction(editItem.id, editForm);
      setEditItem(null);
      setEditForm(null);
      await loadData();
    } catch {
      // silent
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete() {
    if (!deleteItem) return;
    setSaving(true);
    try {
      await plannerService.deleteRecurringTransaction(deleteItem.id);
      setDeleteItem(null);
      await loadData();
    } catch {
      // silent
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Transações Recorrentes</h1>
          <p className="text-muted-foreground text-sm">Receitas e despesas fixas mensais</p>
        </div>
        <div className="flex gap-2">
          <Button variant="ghost" size="icon" onClick={loadData} title="Atualizar">
            <RefreshCw className="h-4 w-4" />
          </Button>
          <Button onClick={() => { setCreateForm(EMPTY_CREATE); setShowCreate(true); }}>
            <Plus className="h-4 w-4 mr-2" />
            Nova Recorrente
          </Button>
        </div>
      </div>

      {/* Summary */}
      <div className="grid grid-cols-2 gap-3">
        <Card>
          <CardHeader className="pb-1 pt-4 px-4">
            <CardTitle className="text-xs font-medium text-muted-foreground flex items-center gap-1.5">
              <TrendingUp className="h-3.5 w-3.5 text-emerald-600" />Receitas Fixas
            </CardTitle>
          </CardHeader>
          <CardContent className="px-4 pb-4">
            <p className="text-lg font-bold text-emerald-600">{BRL(totalActiveIncome)}</p>
            <p className="text-xs text-muted-foreground">
              {items.filter(i => i.isActive && i.type === TransactionType.Income).length} ativas
            </p>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-1 pt-4 px-4">
            <CardTitle className="text-xs font-medium text-muted-foreground flex items-center gap-1.5">
              <TrendingDown className="h-3.5 w-3.5 text-destructive" />Despesas Fixas
            </CardTitle>
          </CardHeader>
          <CardContent className="px-4 pb-4">
            <p className="text-lg font-bold text-destructive">{BRL(totalActiveExpense)}</p>
            <p className="text-xs text-muted-foreground">
              {items.filter(i => i.isActive && i.type === TransactionType.Expense).length} ativas
            </p>
          </CardContent>
        </Card>
      </div>

      {/* Filters */}
      <div className="flex gap-2 flex-wrap">
        {(['all', 'income', 'expense'] as FilterType[]).map(f => (
          <Button
            key={f}
            variant={filterType === f ? 'default' : 'outline'}
            size="sm"
            onClick={() => setFilterType(f)}
          >
            {f === 'all' ? 'Todos' : f === 'income' ? 'Receitas' : 'Despesas'}
          </Button>
        ))}
        <div className="ml-auto flex gap-2">
          {(['all', 'active', 'inactive'] as FilterStatus[]).map(s => (
            <Button
              key={s}
              variant={filterStatus === s ? 'secondary' : 'outline'}
              size="sm"
              onClick={() => setFilterStatus(s)}
            >
              {s === 'all' ? 'Todos status' : s === 'active' ? 'Ativos' : 'Inativos'}
            </Button>
          ))}
        </div>
      </div>

      {/* List */}
      {loading ? (
        <div className="flex justify-center py-12">
          <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
        </div>
      ) : filtered.length === 0 ? (
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-12 gap-3">
            <RepeatIcon className="h-10 w-10 text-muted-foreground" />
            <p className="text-muted-foreground">Nenhuma transação recorrente encontrada.</p>
            <Button onClick={() => { setCreateForm(EMPTY_CREATE); setShowCreate(true); }}>
              <Plus className="h-4 w-4 mr-2" />Nova Recorrente
            </Button>
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-2">
          {filtered.map(item => (
            <Card key={item.id} className={item.isActive ? '' : 'opacity-60'}>
              <CardContent className="px-4 py-3">
                <div className="flex items-center gap-3">
                  <div className={`flex-shrink-0 rounded-full p-1.5 ${item.type === TransactionType.Income ? 'bg-emerald-50' : 'bg-red-50'}`}>
                    {item.type === TransactionType.Income
                      ? <TrendingUp className="h-4 w-4 text-emerald-600" />
                      : <TrendingDown className="h-4 w-4 text-destructive" />}
                  </div>
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2 flex-wrap">
                      <span className="font-medium text-sm">{item.description}</span>
                      {!item.isActive && <Badge variant="outline" className="text-xs">Inativo</Badge>}
                      {item.itemKind === RecurringItemKind.Subscription && (
                        <Badge variant="secondary" className="text-xs">Assinatura</Badge>
                      )}
                      {item.hasVariableAmount && (
                        <Badge variant="outline" className="text-xs">Valor variável</Badge>
                      )}
                    </div>
                    <div className="flex items-center gap-3 mt-0.5 text-xs text-muted-foreground flex-wrap">
                      <span className="flex items-center gap-1">
                        <CalendarDays className="h-3 w-3" />
                        Dia {item.dayOfMonth} — {FREQUENCY_TYPE_LABELS[item.frequencyType]}
                      </span>
                      <span>{PAYMENT_METHOD_LABELS[item.paymentMethod]}</span>
                    </div>
                  </div>
                  <div className="flex items-center gap-3 flex-shrink-0">
                    <span className={`font-semibold text-sm ${item.type === TransactionType.Income ? 'text-emerald-600' : 'text-destructive'}`}>
                      {BRL(item.amount)}
                    </span>
                    <div className="flex gap-1">
                      <Button variant="ghost" size="icon" className="h-8 w-8" onClick={() => openEdit(item)}>
                        <Pencil className="h-3.5 w-3.5" />
                      </Button>
                      <Button variant="ghost" size="icon" className="h-8 w-8 text-destructive hover:text-destructive" onClick={() => setDeleteItem(item)}>
                        <Trash2 className="h-3.5 w-3.5" />
                      </Button>
                    </div>
                  </div>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}

      {/* Create Modal */}
      <Dialog open={showCreate} onOpenChange={open => { if (!saving) setShowCreate(open); }}>
        <DialogContent className="max-w-lg max-h-[85vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>Nova Transação Recorrente</DialogTitle>
          </DialogHeader>
          <RecurringForm
            form={createForm}
            onChange={p => setCreateForm(prev => ({ ...prev, ...p }))}
            categoriesForType={categoriesForType}
          />
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowCreate(false)} disabled={saving}>Cancelar</Button>
            <Button onClick={handleCreate} disabled={saving || !createForm.description || !createForm.categoryId || createForm.amount <= 0}>
              {saving && <Loader2 className="h-4 w-4 mr-2 animate-spin" />}Criar
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Edit Modal */}
      <Dialog open={!!editItem} onOpenChange={open => { if (!saving && !open) { setEditItem(null); setEditForm(null); } }}>
        <DialogContent className="max-w-lg max-h-[85vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>Editar Transação Recorrente</DialogTitle>
          </DialogHeader>
          {editForm && (
            <>
              <RecurringForm
                form={editForm}
                onChange={p => setEditForm(prev => prev ? { ...prev, ...p } : prev)}
                categoriesForType={categoriesForType}
                showIsActive
              />
              <div className="flex items-center gap-2 mt-2">
                <Checkbox
                  id="edit-active"
                  checked={editForm.isActive}
                  onCheckedChange={v => setEditForm(prev => prev ? { ...prev, isActive: !!v } : prev)}
                />
                <Label htmlFor="edit-active" className="text-sm">Ativo</Label>
              </div>
            </>
          )}
          <DialogFooter>
            <Button variant="outline" onClick={() => { setEditItem(null); setEditForm(null); }} disabled={saving}>Cancelar</Button>
            <Button onClick={handleEdit} disabled={saving || !editForm?.description || !editForm?.categoryId}>
              {saving && <Loader2 className="h-4 w-4 mr-2 animate-spin" />}Salvar
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Delete Confirm */}
      <Dialog open={!!deleteItem} onOpenChange={open => { if (!saving && !open) setDeleteItem(null); }}>
        <DialogContent className="max-w-sm">
          <DialogHeader>
            <DialogTitle>Excluir Transação Recorrente</DialogTitle>
          </DialogHeader>
          <p className="text-sm text-muted-foreground">
            Tem certeza que deseja excluir <strong>{deleteItem?.description}</strong>? Esta ação não pode ser desfeita.
          </p>
          <DialogFooter>
            <Button variant="outline" onClick={() => setDeleteItem(null)} disabled={saving}>Cancelar</Button>
            <Button variant="destructive" onClick={handleDelete} disabled={saving}>
              {saving && <Loader2 className="h-4 w-4 mr-2 animate-spin" />}Excluir
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}

type FormShape = Partial<CreateRecurringTransactionRequest> & Partial<UpdateRecurringTransactionRequest>;

function RecurringForm({
  form,
  onChange,
  categoriesForType,
  showIsActive = false,
}: {
  form: FormShape;
  onChange: (patch: Partial<FormShape>) => void;
  categoriesForType: (type: TransactionType) => TransactionCategory[];
  showIsActive?: boolean;
}) {
  const cats = categoriesForType(form.type ?? TransactionType.Expense);

  return (
    <div className="space-y-4">
      <div className="grid grid-cols-2 gap-3">
        <div className="space-y-1.5">
          <Label>Tipo</Label>
          <Select
            value={String(form.type ?? TransactionType.Expense)}
            onValueChange={v => onChange({ type: Number(v) as TransactionType, categoryId: '' })}
          >
            <SelectTrigger><SelectValue /></SelectTrigger>
            <SelectContent>
              {Object.entries(TRANSACTION_TYPE_LABELS).map(([k, label]) => (
                <SelectItem key={k} value={k}>{label}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <div className="space-y-1.5">
          <Label>Classificação</Label>
          <Select
            value={String(form.itemKind ?? RecurringItemKind.Standard)}
            onValueChange={v => onChange({ itemKind: Number(v) as RecurringItemKind })}
          >
            <SelectTrigger><SelectValue /></SelectTrigger>
            <SelectContent>
              {Object.entries(RECURRING_ITEM_KIND_LABELS).map(([k, label]) => (
                <SelectItem key={k} value={k}>{label}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      </div>

      <div className="space-y-1.5">
        <Label>Descrição</Label>
        <Input
          value={form.description ?? ''}
          onChange={e => onChange({ description: e.target.value })}
          placeholder="Ex: Aluguel, Netflix, Salário…"
        />
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div className="space-y-1.5">
          <Label>Valor (R$)</Label>
          <Input
            type="number"
            min={0}
            step={0.01}
            value={form.amount ?? 0}
            onChange={e => onChange({ amount: parseFloat(e.target.value) || 0 })}
          />
        </div>
        <div className="space-y-1.5">
          <Label>Dia do Mês</Label>
          <Input
            type="number"
            min={1}
            max={31}
            value={form.dayOfMonth ?? 1}
            onChange={e => onChange({ dayOfMonth: parseInt(e.target.value) || 1 })}
          />
        </div>
      </div>

      <div className="space-y-1.5">
        <Label>Categoria</Label>
        <Select
          value={form.categoryId ?? ''}
          onValueChange={v => onChange({ categoryId: v })}
        >
          <SelectTrigger><SelectValue placeholder="Selecione…" /></SelectTrigger>
          <SelectContent>
            {cats.map(c => (
              <SelectItem key={c.id} value={c.id}>{c.name}</SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div className="space-y-1.5">
          <Label>Frequência</Label>
          <Select
            value={String(form.frequencyType ?? FrequencyType.Monthly)}
            onValueChange={v => onChange({ frequencyType: Number(v) as FrequencyType })}
          >
            <SelectTrigger><SelectValue /></SelectTrigger>
            <SelectContent>
              {Object.entries(FREQUENCY_TYPE_LABELS).map(([k, label]) => (
                <SelectItem key={k} value={k}>{label}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <div className="space-y-1.5">
          <Label>Pagamento</Label>
          <Select
            value={String(form.paymentMethod ?? PaymentMethod.Pix)}
            onValueChange={v => onChange({ paymentMethod: Number(v) as PaymentMethod })}
          >
            <SelectTrigger><SelectValue /></SelectTrigger>
            <SelectContent>
              {Object.entries(PAYMENT_METHOD_LABELS).map(([k, label]) => (
                <SelectItem key={k} value={k}>{label}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div className="space-y-1.5">
          <Label>Data de Início</Label>
          <Input
            type="date"
            value={typeof form.startDate === 'string' ? form.startDate.split('T')[0] : ''}
            onChange={e => onChange({ startDate: e.target.value })}
          />
        </div>
        <div className="space-y-1.5">
          <Label>Data de Fim <span className="text-muted-foreground">(opcional)</span></Label>
          <Input
            type="date"
            value={typeof form.endDate === 'string' ? form.endDate.split('T')[0] : ''}
            onChange={e => onChange({ endDate: e.target.value || undefined })}
          />
        </div>
      </div>

      <div className="space-y-1.5">
        <Label>Prioridade <span className="text-muted-foreground">(1–100)</span></Label>
        <Input
          type="number"
          min={1}
          max={100}
          value={form.priority ?? 50}
          onChange={e => onChange({ priority: parseInt(e.target.value) || 50 })}
        />
      </div>

      <div className="flex items-center gap-2">
        <Checkbox
          id="variable-amount"
          checked={!!form.hasVariableAmount}
          onCheckedChange={v => onChange({ hasVariableAmount: !!v })}
        />
        <Label htmlFor="variable-amount" className="text-sm">Valor variável (ex: conta de luz)</Label>
      </div>
    </div>
  );
}
