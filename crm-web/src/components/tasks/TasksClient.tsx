'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Textarea } from '@/components/ui/textarea';
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
    Low: 'bg-slate-100 text-slate-700',
    Medium: 'bg-blue-100 text-blue-700',
    High: 'bg-orange-100 text-orange-700',
    Urgent: 'bg-red-100 text-red-700',
};

const STATUS_COLORS: Record<TaskStatus, string> = {
    Todo: 'bg-slate-100 text-slate-700',
    InProgress: 'bg-yellow-100 text-yellow-700',
    Done: 'bg-green-100 text-green-700',
    Cancelled: 'bg-gray-100 text-gray-500',
};

type FilterStatus = TaskStatus | 'All' | 'Overdue';

export function TasksClient() {
    const [tasks, setTasks] = useState<TaskItem[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [filter, setFilter] = useState<FilterStatus>('All');
    const [isFormOpen, setIsFormOpen] = useState(false);
    const [editingTask, setEditingTask] = useState<TaskItem | undefined>();

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

    async function handleDelete(task: TaskItem) {
        if (!confirm(`Excluir "${task.title}"?`)) return;
        try {
            await tasksService.delete(task.id);
            setTasks(prev => prev.filter(t => t.id !== task.id));
            toast.success('Tarefa excluída');
        } catch {
            toast.error('Erro ao excluir tarefa');
        }
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
            {/* Header */}
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-2xl font-semibold text-gray-900">Tarefas</h1>
                    <p className="text-sm text-gray-500 mt-1">{tasks.length} tarefa{tasks.length !== 1 ? 's' : ''}</p>
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
                        className={`px-3 py-1.5 rounded-full text-sm font-medium transition-colors ${
                            filter === f
                                ? 'bg-gray-900 text-white'
                                : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
                        }`}
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
                <div className="text-center py-16 text-gray-400">
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
                                className={`bg-white border rounded-lg p-4 flex items-start gap-4 hover:border-gray-300 transition-colors ${
                                    task.status === 'Done' ? 'opacity-60' : ''
                                }`}
                            >
                                {/* Complete toggle */}
                                <button
                                    onClick={() => task.status === 'Done' ? handleReopen(task) : handleComplete(task)}
                                    className={`mt-0.5 flex-shrink-0 h-5 w-5 rounded-full border-2 flex items-center justify-center transition-colors ${
                                        task.status === 'Done'
                                            ? 'border-green-500 bg-green-500 text-white'
                                            : 'border-gray-300 hover:border-green-400'
                                    }`}
                                    title={task.status === 'Done' ? 'Reabrir' : 'Concluir'}
                                >
                                    {task.status === 'Done' && <Check className="h-3 w-3" />}
                                </button>

                                {/* Content */}
                                <div className="flex-1 min-w-0">
                                    <button
                                        onClick={() => openEdit(task)}
                                        className={`text-left font-medium text-gray-900 hover:text-blue-600 transition-colors ${
                                            task.status === 'Done' ? 'line-through text-gray-400' : ''
                                        }`}
                                    >
                                        {task.title}
                                    </button>
                                    {task.description && (
                                        <p className="text-sm text-gray-500 mt-0.5 truncate">{task.description}</p>
                                    )}
                                    <div className="flex items-center gap-2 mt-2 flex-wrap">
                                        <Badge className={`text-xs ${PRIORITY_COLORS[task.priority]}`}>
                                            {PRIORITY_LABELS[task.priority]}
                                        </Badge>
                                        <Badge className={`text-xs ${STATUS_COLORS[task.status]}`}>
                                            {STATUS_LABELS[task.status]}
                                        </Badge>
                                        {task.dueDate && (
                                            <span className={`flex items-center gap-1 text-xs ${isOverdue ? 'text-red-500' : 'text-gray-400'}`}>
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
                                            className="p-1.5 text-gray-400 hover:text-gray-600 rounded"
                                            title="Reabrir"
                                        >
                                            <RotateCcw className="h-3.5 w-3.5" />
                                        </button>
                                    )}
                                    <button
                                        onClick={() => handleArchive(task)}
                                        className="p-1.5 text-gray-400 hover:text-gray-600 rounded"
                                        title="Arquivar"
                                    >
                                        <Archive className="h-3.5 w-3.5" />
                                    </button>
                                    <button
                                        onClick={() => handleDelete(task)}
                                        className="p-1.5 text-gray-400 hover:text-red-500 rounded"
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
