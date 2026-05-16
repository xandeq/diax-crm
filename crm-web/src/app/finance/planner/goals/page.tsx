'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Checkbox } from '@/components/ui/checkbox';
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Progress } from '@/components/ui/progress';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { formatCurrency } from '@/lib/utils';
import { plannerService } from '@/services/plannerService';
import {
    CreateFinancialGoalRequest, FinancialGoal, GoalCategory,
    GOAL_CATEGORY_LABELS, UpdateFinancialGoalRequest
} from '@/types/planner';
import { Loader2, Pencil, PiggyBank, Plus, Target, Trash2, TrendingUp } from 'lucide-react';
import { useCallback, useEffect, useState } from 'react';
import { toast } from 'sonner';

const EMPTY_CREATE: CreateFinancialGoalRequest = {
    name: '', targetAmount: 0, currentAmount: 0,
    targetDate: '', category: GoalCategory.Other, priority: 5, autoAllocateSurplus: false,
};

function toIsoDate(d?: string) {
    if (!d) return undefined;
    const p = new Date(d);
    return isNaN(p.getTime()) ? undefined : p.toISOString();
}

export default function GoalsPage() {
    const [goals, setGoals] = useState<FinancialGoal[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [saving, setSaving] = useState(false);

    // modals
    const [showCreate, setShowCreate] = useState(false);
    const [editGoal, setEditGoal] = useState<FinancialGoal | null>(null);
    const [contributeGoal, setContributeGoal] = useState<FinancialGoal | null>(null);
    const [deleteGoal, setDeleteGoal] = useState<FinancialGoal | null>(null);

    // forms
    const [createForm, setCreateForm] = useState<CreateFinancialGoalRequest>(EMPTY_CREATE);
    const [editForm, setEditForm] = useState<UpdateFinancialGoalRequest | null>(null);
    const [contributeAmount, setContributeAmount] = useState('');

    const loadGoals = useCallback(async () => {
        setIsLoading(true);
        try {
            setGoals(await plannerService.getFinancialGoals());
        } catch {
            toast.error('Erro ao carregar metas financeiras');
        } finally {
            setIsLoading(false);
        }
    }, []);

    useEffect(() => { loadGoals(); }, [loadGoals]);

    // ── Create ──────────────────────────────────────────────────────
    const handleCreate = async () => {
        if (!createForm.name.trim() || createForm.targetAmount <= 0) {
            toast.error('Preencha nome e valor da meta');
            return;
        }
        setSaving(true);
        try {
            await plannerService.createFinancialGoal({
                ...createForm,
                targetDate: toIsoDate(createForm.targetDate),
            });
            toast.success('Meta criada com sucesso');
            setShowCreate(false);
            setCreateForm(EMPTY_CREATE);
            await loadGoals();
        } catch {
            toast.error('Erro ao criar meta');
        } finally {
            setSaving(false);
        }
    };

    // ── Edit ────────────────────────────────────────────────────────
    const openEdit = (goal: FinancialGoal) => {
        setEditGoal(goal);
        setEditForm({
            name: goal.name,
            targetAmount: goal.targetAmount,
            targetDate: goal.targetDate ? goal.targetDate.slice(0, 10) : '',
            category: goal.category,
            priority: goal.priority,
            isActive: goal.isActive,
            autoAllocateSurplus: goal.autoAllocateSurplus,
        });
    };

    const handleEdit = async () => {
        if (!editGoal || !editForm) return;
        if (!editForm.name.trim() || editForm.targetAmount <= 0) {
            toast.error('Preencha nome e valor da meta');
            return;
        }
        setSaving(true);
        try {
            await plannerService.updateFinancialGoal(editGoal.id, {
                ...editForm,
                targetDate: toIsoDate(editForm.targetDate),
            });
            toast.success('Meta atualizada');
            setEditGoal(null);
            await loadGoals();
        } catch {
            toast.error('Erro ao atualizar meta');
        } finally {
            setSaving(false);
        }
    };

    // ── Contribute ──────────────────────────────────────────────────
    const handleContribute = async () => {
        if (!contributeGoal) return;
        const amount = parseFloat(contributeAmount.replace(',', '.'));
        if (isNaN(amount) || amount <= 0) {
            toast.error('Informe um valor válido');
            return;
        }
        setSaving(true);
        try {
            await plannerService.addContribution(contributeGoal.id, { amount });
            toast.success(`${formatCurrency(amount)} adicionado à meta`);
            setContributeGoal(null);
            setContributeAmount('');
            await loadGoals();
        } catch {
            toast.error('Erro ao registrar contribuição');
        } finally {
            setSaving(false);
        }
    };

    // ── Delete ──────────────────────────────────────────────────────
    const handleDelete = async () => {
        if (!deleteGoal) return;
        setSaving(true);
        try {
            await plannerService.deleteFinancialGoal(deleteGoal.id);
            toast.success('Meta excluída');
            setDeleteGoal(null);
            await loadGoals();
        } catch {
            toast.error('Erro ao excluir meta');
        } finally {
            setSaving(false);
        }
    };

    const getProgressColor = (progress: number) => {
        if (progress >= 100) return 'bg-green-600';
        if (progress >= 70) return 'bg-blue-600';
        if (progress >= 30) return 'bg-yellow-600';
        return 'bg-slate-400';
    };

    return (
        <div className="space-y-6">
            {/* Header */}
            <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
                <div>
                    <h1 className="text-3xl font-bold tracking-tight">Metas Financeiras</h1>
                    <p className="text-muted-foreground">Defina e acompanhe seus objetivos financeiros</p>
                </div>
                <Button onClick={() => setShowCreate(true)}>
                    <Plus className="mr-2 h-4 w-4" />Nova Meta
                </Button>
            </div>

            {/* Stats */}
            <div className="grid gap-4 md:grid-cols-3">
                <Card>
                    <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                        <CardTitle className="text-sm font-medium">Total de Metas</CardTitle>
                        <Target className="h-4 w-4 text-muted-foreground" />
                    </CardHeader>
                    <CardContent>
                        <div className="text-2xl font-bold">{goals.length}</div>
                        <p className="text-xs text-muted-foreground">{goals.filter(g => g.isActive).length} ativas</p>
                    </CardContent>
                </Card>
                <Card>
                    <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                        <CardTitle className="text-sm font-medium">Metas Concluídas</CardTitle>
                        <TrendingUp className="h-4 w-4 text-green-600" />
                    </CardHeader>
                    <CardContent>
                        <div className="text-2xl font-bold text-green-600">{goals.filter(g => g.isCompleted).length}</div>
                        <p className="text-xs text-muted-foreground">
                            {goals.length > 0 ? `${Math.round(goals.filter(g => g.isCompleted).length / goals.length * 100)}%` : '0%'} do total
                        </p>
                    </CardContent>
                </Card>
                <Card>
                    <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                        <CardTitle className="text-sm font-medium">Valor Acumulado</CardTitle>
                        <TrendingUp className="h-4 w-4 text-blue-600" />
                    </CardHeader>
                    <CardContent>
                        <div className="text-2xl font-bold text-blue-600">
                            {formatCurrency(goals.reduce((s, g) => s + g.currentAmount, 0))}
                        </div>
                        <p className="text-xs text-muted-foreground">
                            de {formatCurrency(goals.reduce((s, g) => s + g.targetAmount, 0))}
                        </p>
                    </CardContent>
                </Card>
            </div>

            {/* Goals list */}
            {isLoading ? (
                <div className="flex items-center justify-center py-12">
                    <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
                </div>
            ) : goals.length === 0 ? (
                <Card>
                    <CardContent className="flex flex-col items-center justify-center py-12">
                        <Target className="h-12 w-12 text-muted-foreground mb-4" />
                        <h3 className="text-lg font-semibold mb-2">Nenhuma meta cadastrada</h3>
                        <p className="text-sm text-muted-foreground mb-4">Comece criando sua primeira meta financeira</p>
                        <Button onClick={() => setShowCreate(true)}>
                            <Plus className="mr-2 h-4 w-4" />Criar primeira meta
                        </Button>
                    </CardContent>
                </Card>
            ) : (
                <div className="grid gap-4 md:grid-cols-2">
                    {goals.map(goal => (
                        <Card key={goal.id} className="hover:shadow-lg transition-shadow">
                            <CardHeader>
                                <div className="flex items-start justify-between">
                                    <div className="space-y-1">
                                        <CardTitle>{goal.name}</CardTitle>
                                        <CardDescription>{GOAL_CATEGORY_LABELS[goal.category]}</CardDescription>
                                    </div>
                                    <div className="flex flex-col gap-1 items-end">
                                        {goal.isCompleted && <Badge className="bg-green-600">✓ Concluída</Badge>}
                                        {!goal.isActive && <Badge variant="secondary">Inativa</Badge>}
                                        {goal.autoAllocateSurplus && <Badge variant="outline" className="text-xs">Auto-alocação</Badge>}
                                    </div>
                                </div>
                            </CardHeader>
                            <CardContent className="space-y-4">
                                <div>
                                    <div className="flex justify-between text-sm mb-2">
                                        <span className="text-muted-foreground">Progresso</span>
                                        <span className="font-semibold">{goal.progress.toFixed(1)}%</span>
                                    </div>
                                    <Progress value={Math.min(goal.progress, 100)} className={getProgressColor(goal.progress)} />
                                </div>
                                <div className="grid grid-cols-2 gap-4 text-sm">
                                    <div>
                                        <p className="text-muted-foreground">Acumulado</p>
                                        <p className="font-semibold">{formatCurrency(goal.currentAmount)}</p>
                                    </div>
                                    <div>
                                        <p className="text-muted-foreground">Meta</p>
                                        <p className="font-semibold">{formatCurrency(goal.targetAmount)}</p>
                                    </div>
                                    <div>
                                        <p className="text-muted-foreground">Faltam</p>
                                        <p className="font-semibold text-orange-600">{formatCurrency(goal.remainingAmount)}</p>
                                    </div>
                                    {goal.targetDate && (
                                        <div>
                                            <p className="text-muted-foreground">Prazo</p>
                                            <p className="font-semibold">{new Date(goal.targetDate).toLocaleDateString('pt-BR')}</p>
                                        </div>
                                    )}
                                </div>
                                <div className="flex gap-2 pt-1">
                                    <Button variant="outline" size="sm" className="flex-1" onClick={() => setContributeGoal(goal)}>
                                        <PiggyBank className="h-4 w-4 mr-1.5" />Contribuir
                                    </Button>
                                    <Button variant="ghost" size="sm" onClick={() => openEdit(goal)}>
                                        <Pencil className="h-4 w-4" />
                                    </Button>
                                    <Button variant="ghost" size="sm" className="text-destructive hover:text-destructive" onClick={() => setDeleteGoal(goal)}>
                                        <Trash2 className="h-4 w-4" />
                                    </Button>
                                </div>
                            </CardContent>
                        </Card>
                    ))}
                </div>
            )}

            {/* ── Create Modal ─────────────────────────────────────── */}
            <Dialog open={showCreate} onOpenChange={v => { if (!saving) { setShowCreate(v); if (!v) setCreateForm(EMPTY_CREATE); } }}>
                <DialogContent className="sm:max-w-md">
                    <DialogHeader><DialogTitle>Nova Meta Financeira</DialogTitle></DialogHeader>
                    <GoalForm
                        name={createForm.name}
                        targetAmount={String(createForm.targetAmount || '')}
                        currentAmount={String(createForm.currentAmount || '')}
                        targetDate={createForm.targetDate ?? ''}
                        category={createForm.category}
                        priority={String(createForm.priority ?? 5)}
                        autoAllocateSurplus={createForm.autoAllocateSurplus}
                        onChange={(field, value) => setCreateForm(f => ({ ...f, [field]: value }))}
                    />
                    <DialogFooter>
                        <Button variant="outline" onClick={() => setShowCreate(false)} disabled={saving}>Cancelar</Button>
                        <Button onClick={handleCreate} disabled={saving}>
                            {saving && <Loader2 className="h-4 w-4 mr-2 animate-spin" />}Criar
                        </Button>
                    </DialogFooter>
                </DialogContent>
            </Dialog>

            {/* ── Edit Modal ───────────────────────────────────────── */}
            <Dialog open={!!editGoal} onOpenChange={v => { if (!saving && !v) setEditGoal(null); }}>
                <DialogContent className="sm:max-w-md">
                    <DialogHeader><DialogTitle>Editar Meta</DialogTitle></DialogHeader>
                    {editForm && (
                        <GoalForm
                            name={editForm.name}
                            targetAmount={String(editForm.targetAmount || '')}
                            targetDate={editForm.targetDate ?? ''}
                            category={editForm.category}
                            priority={String(editForm.priority)}
                            autoAllocateSurplus={editForm.autoAllocateSurplus}
                            isActive={editForm.isActive}
                            showIsActive
                            onChange={(field, value) => setEditForm(f => f ? { ...f, [field]: value } : f)}
                        />
                    )}
                    <DialogFooter>
                        <Button variant="outline" onClick={() => setEditGoal(null)} disabled={saving}>Cancelar</Button>
                        <Button onClick={handleEdit} disabled={saving}>
                            {saving && <Loader2 className="h-4 w-4 mr-2 animate-spin" />}Salvar
                        </Button>
                    </DialogFooter>
                </DialogContent>
            </Dialog>

            {/* ── Contribute Modal ─────────────────────────────────── */}
            <Dialog open={!!contributeGoal} onOpenChange={v => { if (!saving && !v) { setContributeGoal(null); setContributeAmount(''); } }}>
                <DialogContent className="sm:max-w-sm">
                    <DialogHeader>
                        <DialogTitle>Contribuir para meta</DialogTitle>
                        {contributeGoal && <p className="text-sm text-muted-foreground">{contributeGoal.name}</p>}
                    </DialogHeader>
                    {contributeGoal && (
                        <div className="space-y-4 py-2">
                            <div className="text-sm text-muted-foreground">
                                Acumulado: <span className="font-medium text-foreground">{formatCurrency(contributeGoal.currentAmount)}</span>
                                {' / '}Meta: <span className="font-medium text-foreground">{formatCurrency(contributeGoal.targetAmount)}</span>
                            </div>
                            <div className="space-y-1.5">
                                <Label htmlFor="contrib-amount">Valor (R$)</Label>
                                <Input
                                    id="contrib-amount"
                                    type="number"
                                    min="0.01"
                                    step="0.01"
                                    placeholder="0,00"
                                    value={contributeAmount}
                                    onChange={e => setContributeAmount(e.target.value)}
                                    onKeyDown={e => { if (e.key === 'Enter') handleContribute(); }}
                                    autoFocus
                                />
                            </div>
                        </div>
                    )}
                    <DialogFooter>
                        <Button variant="outline" onClick={() => { setContributeGoal(null); setContributeAmount(''); }} disabled={saving}>Cancelar</Button>
                        <Button onClick={handleContribute} disabled={saving}>
                            {saving && <Loader2 className="h-4 w-4 mr-2 animate-spin" />}Adicionar
                        </Button>
                    </DialogFooter>
                </DialogContent>
            </Dialog>

            {/* ── Delete Confirm ───────────────────────────────────── */}
            <Dialog open={!!deleteGoal} onOpenChange={v => { if (!saving && !v) setDeleteGoal(null); }}>
                <DialogContent className="sm:max-w-sm">
                    <DialogHeader><DialogTitle>Excluir meta</DialogTitle></DialogHeader>
                    <p className="text-sm text-muted-foreground py-2">
                        Tem certeza que deseja excluir a meta <span className="font-semibold text-foreground">"{deleteGoal?.name}"</span>? Esta ação não pode ser desfeita.
                    </p>
                    <DialogFooter>
                        <Button variant="outline" onClick={() => setDeleteGoal(null)} disabled={saving}>Cancelar</Button>
                        <Button variant="destructive" onClick={handleDelete} disabled={saving}>
                            {saving && <Loader2 className="h-4 w-4 mr-2 animate-spin" />}Excluir
                        </Button>
                    </DialogFooter>
                </DialogContent>
            </Dialog>
        </div>
    );
}

// ── Shared form ─────────────────────────────────────────────────────────────
interface GoalFormProps {
    name: string;
    targetAmount: string;
    currentAmount?: string;
    targetDate: string;
    category: GoalCategory;
    priority: string;
    autoAllocateSurplus: boolean;
    isActive?: boolean;
    showIsActive?: boolean;
    onChange: (field: string, value: string | number | boolean | GoalCategory) => void;
}

function GoalForm({ name, targetAmount, currentAmount, targetDate, category, priority, autoAllocateSurplus, isActive, showIsActive, onChange }: GoalFormProps) {
    return (
        <div className="space-y-3 py-2">
            <div className="space-y-1.5">
                <Label htmlFor="gf-name">Nome</Label>
                <Input id="gf-name" value={name} onChange={e => onChange('name', e.target.value)} placeholder="Ex: Fundo de emergência" />
            </div>
            <div className="grid grid-cols-2 gap-3">
                <div className="space-y-1.5">
                    <Label htmlFor="gf-target">Meta (R$)</Label>
                    <Input id="gf-target" type="number" min="0.01" step="0.01" value={targetAmount} onChange={e => onChange('targetAmount', parseFloat(e.target.value) || 0)} />
                </div>
                {currentAmount !== undefined && (
                    <div className="space-y-1.5">
                        <Label htmlFor="gf-current">Acumulado (R$)</Label>
                        <Input id="gf-current" type="number" min="0" step="0.01" value={currentAmount} onChange={e => onChange('currentAmount', parseFloat(e.target.value) || 0)} />
                    </div>
                )}
            </div>
            <div className="grid grid-cols-2 gap-3">
                <div className="space-y-1.5">
                    <Label htmlFor="gf-category">Categoria</Label>
                    <Select value={String(category)} onValueChange={v => onChange('category', parseInt(v) as GoalCategory)}>
                        <SelectTrigger id="gf-category"><SelectValue /></SelectTrigger>
                        <SelectContent>
                            {Object.entries(GOAL_CATEGORY_LABELS).map(([k, label]) => (
                                <SelectItem key={k} value={k}>{label}</SelectItem>
                            ))}
                        </SelectContent>
                    </Select>
                </div>
                <div className="space-y-1.5">
                    <Label htmlFor="gf-date">Prazo (opcional)</Label>
                    <Input id="gf-date" type="date" value={targetDate} onChange={e => onChange('targetDate', e.target.value)} />
                </div>
            </div>
            <div className="grid grid-cols-2 gap-3">
                <div className="space-y-1.5">
                    <Label htmlFor="gf-priority">Prioridade (1-10)</Label>
                    <Input id="gf-priority" type="number" min="1" max="10" value={priority} onChange={e => onChange('priority', parseInt(e.target.value) || 5)} />
                </div>
            </div>
            <div className="flex items-center gap-4 pt-1">
                <div className="flex items-center gap-2">
                    <Checkbox id="gf-auto" checked={autoAllocateSurplus} onCheckedChange={v => onChange('autoAllocateSurplus', !!v)} />
                    <Label htmlFor="gf-auto" className="text-sm cursor-pointer">Auto-alocação de superávit</Label>
                </div>
                {showIsActive && (
                    <div className="flex items-center gap-2">
                        <Checkbox id="gf-active" checked={isActive ?? true} onCheckedChange={v => onChange('isActive', !!v)} />
                        <Label htmlFor="gf-active" className="text-sm cursor-pointer">Ativa</Label>
                    </div>
                )}
            </div>
        </div>
    );
}
