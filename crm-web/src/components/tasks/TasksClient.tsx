'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { useConfirmDialog } from '@/hooks/useConfirmDialog';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Textarea } from '@/components/ui/textarea';
import React from 'react';
import { tasksService } from '@/services/tasks';
import { CreateTaskRequest, TaskItem, TaskPriority, TaskStatus } from '@/types/tasks';
import { format, isPast, parseISO } from 'date-fns';
import { ptBR } from 'date-fns/locale';
import { AlertCircle, Archive, Check, Circle, Clock, Loader2, Plus, RotateCcw, Trash2 } from 'lucide-react';
import { useCallback, useEffect, useState } from 'react';
import { toast } from 'sonner';

const STATUS_LABELS: Record<TaskStatus, string> = {
    Todo: 'A fazer',
    InProgress: 'Em andamento',
    Done: 'Concluída',
    Cancelled: 'Cancelada',
};

const PRIORITY_LABELS: Record<TaskPriority, string> = {
    Low: 'Baixa',
    Medium: 'Média',
    High: 'Alta',
    Urgent: 'Urgente',
};

const PRIORITY_COLORS: Record<TaskPriority, string> = {
    Low: 'text-slate-400',
    Medium: 'text-blue-400',
    High: 'text-orange-400',
    Urgent: 'text-red-400',
};

const PRIORITY_BG: Record<TaskPriority, React.CSSProperties> = {
    Low:    { background: 'rgba(148,163,184,0.12)', color: '#94a3b8' },
    Medium: { background: 'rgba(96,165,250,0.12)',  color: '#60a5fa' },
    High:   { background: 'rgba(251,146,60,0.12)',  color: '#fb923c' },
    Urgent: { background: 'rgba(248,113,113,0.12)', color: '#f87171' },
};

const STATUS_COLORS: Record<TaskStatus, string> = {
    Todo: 'text-slate-400',
    InProgress: 'text-yellow-400',
    Done: 'text-emerald-400',
    Cancelled: 'text-slate-500',
};

const STATUS_BG: Record<TaskStatus, React.CSSProperties> = {
    Todo:       { background: 'rgba(148,163,184,0.12)', color: '#94a3b8' },
    InProgress: { background: 'rgba(250,204,21,0.12)',  color: '#facc15' },
    Done:       { background: 'rgba(52,211,153,0.12)',  color: '#34d399' },
    Cancelled:  { background: 'rgba(107,114,128,0.12)', color: '#6b7280' },
};

type FilterStatus = TaskStatus | 'All' | 'Overdue';

export function TasksClient() {
    const [tasks, setTasks] = useState<TaskItem[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [filter, setFilter] = useState<FilterStatus>('All');
    const [isFormOpen, setIsFormOpen] = useState(false);
    const [editingTask, setEditingTask] = useState<TaskItem | undefined>();
    const { showConfirm, confirmDialogNode } = useConfirmDialog();

    // Form state
    const [formTitle, setFormTitle] = useState('');
    const [formDescription, setFormDescription] = useState('');
    const [formPriority, setFormPriority] = useState<TaskPriority>('Medium');
    const [formDueDate, setFormDueDate] = useState('');
    const [formStatus, setFormStatus] = useState<TaskStatus>('Todo');
    const [isSaving, setIsSaving] = useState(false);

    const fetchTasks = useCallback(async () => {
        setIsLoading(true);
        try {
            const overdueOnly = filter === 'Overdue';
            const status = filter !== 'All' && filter !== 'Overdue' ? filter : undefined;
            const data = await tasksService.getAll({ status, overdueOnly });
            setTasks(data);
        } catch {
            toast.error('Erro ao carregar tarefas');
        } finally {
            setIsLoading(false);
        }
    }, [filter]);

    useEffect(() => { fetchTasks(); }, [fetchTasks]);

    function openCreate() {
        setEditingTask(undefined);
        setFormTitle('');
        setFormDescription('');
        setFormPriority('Medium');
        setFormDueDate('');
        setFormStatus('Todo');
        setIsFormOpen(true);
    }

    function openEdit(task: TaskItem) {
        setEditingTask(task);
        setFormTitle(task.title);
        setFormDescription(task.description ?? '');
        setFormPriority(task.priority);
        setFormDueDate(task.dueDate ? task.dueDate.slice(0, 10) : '');
        setFormStatus(task.status);
        setIsFormOpen(true);
    }

    async function handleSave() {
        if (!formTitle.trim()) { toast.error('Título obrigatório'); return; }
        setIsSaving(true);
        try {
            if (editingTask) {
                const updated = await tasksService.update(editingTask.id, {
                    title: formTitle.trim(),
                    description: formDescription || undefined,
                    priority: formPriority,
                    status: formStatus,
                    dueDate: formDueDate || undefined,
                });
                setTasks(prev => prev.map(t => t.id === updated.id ? updated : t));
                toast.success('Tarefa atualizada');
            } else {
                const created = await tasksService.create({
                    title: formTitle.trim(),
                    description: formDescription || undefined,
                    priority: formPriority,
                    dueDate: formDueDate || undefined,
                } as CreateTaskRequest);
                setTasks(prev => [created, ...prev]);
                toast.success('Tarefa criada');
            }
            setIsFormOpen(false);
        } catch {
            toast.error('Erro ao salvar tarefa');
        } finally {
            setIsSaving(false);
        }
    }

    async function handleComplete(task: TaskItem) {
        try {
            const updated = await tasksService.complete(task.id);
            setTasks(prev => prev.map(t => t.id === updated.id ? updated : t));
            toast.success('Tarefa concluída');
        } catch {
            toast.error('Erro ao concluir tarefa');
        }
    }

    async function handleReopen(task: TaskItem) {
        try {
            const updated = await tasksService.reopen(task.id);
            setTasks(prev => prev.map(t => t.id === updated.id ? updated : t));
            toast.success('Tarefa reaberta');
        } catch {
            toast.error('Erro ao reabrir tarefa');
        }
    }

    async function handleArchive(task: TaskItem) {
        try {
            await tasksService.archive(task.id);
            setTasks(prev => prev.filter(t => t.id !== task.id));
            toast.success('Tarefa arquivada');
        } catch {
            toast.error('Erro ao arquivar tarefa');
        }
    }

    function handleDelete(task: TaskItem) {
        showConfirm(`Excluir "${task.title}"?`, async () => {
            try {
                await tasksService.delete(task.id);
                setTasks(prev => prev.filter(t => t.id !== task.id));
                toast.success('Tarefa excluída');
            } catch {
                toast.error('Erro ao excluir tarefa');
            }
        });
    }

    const counts = {
        All: tasks.length,
        Todo: tasks.filter(t => t.status === 'Todo').length,
        InProgress: tasks.filter(t => t.status === 'InProgress').length,
        Done: tasks.filter(t => t.status === 'Done').length,
        Overdue: tasks.filter(t => t.dueDate && isPast(parseISO(t.dueDate)) && t.status !== 'Done' && t.status !== 'Cancelled').length,
    };

    return (
        <div className="space-y-6">
            {confirmDialogNode}
            {/* Header */}
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-2xl font-semibold" style={{ color: '#F9FAFB' }}>Tarefas</h1>
                    <p className="text-sm mt-1" style={{ color: '#9CA3AF' }}>{tasks.length} tarefa{tasks.length !== 1 ? 's' : ''}</p>
                </div>
                <Button onClick={openCreate}>
                    <Plus className="h-4 w-4 mr-2" />
                    Nova Tarefa
                </Button>
            </div>

            {/* Filters */}
            <div className="flex gap-2 flex-wrap">
                {(['All', 'Todo', 'InProgress', 'Done', 'Overdue'] as const).map(f => (
                    <button
                        key={f}
                        onClick={() => setFilter(f)}
                        className="px-3 py-1.5 rounded-full text-sm font-medium transition-colors"
                        style={filter === f
                            ? { background: 'rgba(16,185,129,0.15)', color: '#34d399', border: '1px solid rgba(16,185,129,0.3)' }
                            : { background: 'rgba(255,255,255,0.06)', color: '#9CA3AF', border: '1px solid rgba(255,255,255,0.09)' }}
                    >
                        {f === 'All' ? 'Todas' : f === 'Overdue' ? '⚠ Vencidas' : STATUS_LABELS[f]}
                        {' '}
                        <span className="text-xs opacity-75">({counts[f] ?? 0})</span>
                    </button>
                ))}
            </div>

            {/* Task list */}
            {isLoading ? (
                <div className="flex justify-center py-16">
                    <Loader2 className="h-6 w-6 animate-spin text-gray-400" />
                </div>
            ) : tasks.length === 0 ? (
                <div className="text-center py-16" style={{ color: '#6B7280' }}>
                    <Circle className="h-10 w-10 mx-auto mb-3 opacity-30" />
                    <p>Nenhuma tarefa encontrada</p>
                </div>
            ) : (
                <div className="space-y-2">
                    {tasks.map(task => {
                        const isOverdue = task.dueDate && isPast(parseISO(task.dueDate)) && task.status !== 'Done' && task.status !== 'Cancelled';
                        return (
                            <div
                                key={task.id}
                                className="rounded-lg p-4 flex items-start gap-4 transition-colors"
                                style={{
                                    background: 'rgba(255,255,255,0.04)',
                                    border: '1px solid rgba(255,255,255,0.09)',
                                    opacity: task.status === 'Done' ? 0.6 : 1,
                                }}
                            >
                                {/* Complete toggle */}
                                <button
                                    onClick={() => task.status === 'Done' ? handleReopen(task) : handleComplete(task)}
                                    className="mt-0.5 flex-shrink-0 h-5 w-5 rounded-full border-2 flex items-center justify-center transition-colors"
                                    style={task.status === 'Done'
                                        ? { borderColor: '#10B981', background: '#10B981', color: '#fff' }
                                        : { borderColor: 'rgba(255,255,255,0.2)' }}
                                    title={task.status === 'Done' ? 'Reabrir' : 'Concluir'}
                                >
                                    {task.status === 'Done' && <Check className="h-3 w-3" />}
                                </button>

                                {/* Content */}
                                <div className="flex-1 min-w-0">
                                    <button
                                        onClick={() => openEdit(task)}
                                        className="text-left font-medium transition-colors hover:text-blue-400"
                                        style={{ color: task.status === 'Done' ? '#6B7280' : '#F9FAFB', textDecoration: task.status === 'Done' ? 'line-through' : 'none' }}
                                    >
                                        {task.title}
                                    </button>
                                    {task.description && (
                                        <p className="text-sm mt-0.5 truncate" style={{ color: '#9CA3AF' }}>{task.description}</p>
                                    )}
                                    <div className="flex items-center gap-2 mt-2 flex-wrap">
                                        <span className="text-xs px-2 py-0.5 rounded-full font-medium" style={PRIORITY_BG[task.priority]}>
                                            {PRIORITY_LABELS[task.priority]}
                                        </span>
                                        <span className="text-xs px-2 py-0.5 rounded-full font-medium" style={STATUS_BG[task.status]}>
                                            {STATUS_LABELS[task.status]}
                                        </span>
                                        {task.dueDate && (
                                            <span className="flex items-center gap-1 text-xs" style={{ color: isOverdue ? '#f87171' : '#6B7280' }}>
                                                {isOverdue ? <AlertCircle className="h-3 w-3" /> : <Clock className="h-3 w-3" />}
                                                {format(parseISO(task.dueDate), 'dd/MM/yyyy', { locale: ptBR })}
                                            </span>
                                        )}
                                    </div>
                                </div>

                                {/* Actions */}
                                <div className="flex items-center gap-1 flex-shrink-0">
                                    {task.status === 'Done' && (
                                        <button
                                            onClick={() => handleReopen(task)}
                                            className="p-1.5 rounded transition-colors"
                                            style={{ color: '#6B7280' }}
                                            title="Reabrir"
                                        >
                                            <RotateCcw className="h-3.5 w-3.5" />
                                        </button>
                                    )}
                                    <button
                                        onClick={() => handleArchive(task)}
                                        className="p-1.5 rounded transition-colors"
                                        style={{ color: '#6B7280' }}
                                        title="Arquivar"
                                    >
                                        <Archive className="h-3.5 w-3.5" />
                                    </button>
                                    <button
                                        onClick={() => handleDelete(task)}
                                        className="p-1.5 rounded transition-colors hover:text-red-400"
                                        style={{ color: '#6B7280' }}
                                        title="Excluir"
                                    >
                                        <Trash2 className="h-3.5 w-3.5" />
                                    </button>
                                </div>
                            </div>
                        );
                    })}
                </div>
            )}

            {/* Form Dialog */}
            <Dialog open={isFormOpen} onOpenChange={setIsFormOpen}>
                <DialogContent className="sm:max-w-md">
                    <DialogHeader>
                        <DialogTitle>{editingTask ? 'Editar Tarefa' : 'Nova Tarefa'}</DialogTitle>
                    </DialogHeader>
                    <div className="space-y-4 py-2">
                        <div className="space-y-1.5">
                            <Label htmlFor="task-title">Título *</Label>
                            <Input
                                id="task-title"
                                value={formTitle}
                                onChange={e => setFormTitle(e.target.value)}
                                placeholder="Título da tarefa"
                                onKeyDown={e => e.key === 'Enter' && handleSave()}
                            />
                        </div>
                        <div className="space-y-1.5">
                            <Label htmlFor="task-desc">Descrição</Label>
                            <Textarea
                                id="task-desc"
                                value={formDescription}
                                onChange={e => setFormDescription(e.target.value)}
                                placeholder="Descrição opcional"
                                rows={2}
                            />
                        </div>
                        <div className="grid grid-cols-2 gap-3">
                            <div className="space-y-1.5">
                                <Label>Prioridade</Label>
                                <Select value={formPriority} onValueChange={v => setFormPriority(v as TaskPriority)}>
                                    <SelectTrigger>
                                        <SelectValue />
                                    </SelectTrigger>
                                    <SelectContent>
                                        {(['Low', 'Medium', 'High', 'Urgent'] as TaskPriority[]).map(p => (
                                            <SelectItem key={p} value={p}>{PRIORITY_LABELS[p]}</SelectItem>
                                        ))}
                                    </SelectContent>
                                </Select>
                            </div>
                            {editingTask && (
                                <div className="space-y-1.5">
                                    <Label>Status</Label>
                                    <Select value={formStatus} onValueChange={v => setFormStatus(v as TaskStatus)}>
                                        <SelectTrigger>
                                            <SelectValue />
                                        </SelectTrigger>
                                        <SelectContent>
                                            {(['Todo', 'InProgress', 'Done', 'Cancelled'] as TaskStatus[]).map(s => (
                                                <SelectItem key={s} value={s}>{STATUS_LABELS[s]}</SelectItem>
                                            ))}
                                        </SelectContent>
                                    </Select>
                                </div>
                            )}
                        </div>
                        <div className="space-y-1.5">
                            <Label htmlFor="task-due">Prazo</Label>
                            <Input
                                id="task-due"
                                type="date"
                                value={formDueDate}
                                onChange={e => setFormDueDate(e.target.value)}
                            />
                        </div>
                    </div>
                    <div className="flex justify-end gap-2 pt-2">
                        <Button variant="outline" onClick={() => setIsFormOpen(false)}>Cancelar</Button>
                        <Button onClick={handleSave} disabled={isSaving}>
                            {isSaving && <Loader2 className="h-4 w-4 mr-2 animate-spin" />}
                            {editingTask ? 'Salvar' : 'Criar'}
                        </Button>
                    </div>
                </DialogContent>
            </Dialog>
        </div>
    );
}
