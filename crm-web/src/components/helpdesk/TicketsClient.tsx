'use client';

import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Textarea } from '@/components/ui/textarea';
import {
    SupportTicket,
    TicketCategory,
    TicketPriority,
    TicketStatus,
    UpdateTicketRequest,
    CreateTicketRequest,
} from '@/services/helpdesk';
import {
    useTickets,
    useCreateTicket,
    useUpdateTicket,
    useDeleteTicket,
    useResolveTicket,
    useReopenTicket,
    useCloseTicket,
} from '@/hooks/helpdesk';
import { format, parseISO } from 'date-fns';
import { ptBR } from 'date-fns/locale';
import { Check, CheckCircle2, Clock, Loader2, Plus, RotateCcw, Ticket, Trash2, X, XCircle } from 'lucide-react';
import { useMemo, useState } from 'react';
import { toast } from 'sonner';

// ───────────── Labels & colors ─────────────

const STATUS_LABELS: Record<TicketStatus, string> = {
    Open: 'Aberto',
    InProgress: 'Em Andamento',
    WaitingResponse: 'Aguardando Resposta',
    Resolved: 'Resolvido',
    Closed: 'Fechado',
};

const STATUS_COLORS: Record<TicketStatus, string> = {
    Open: 'bg-blue-100 text-blue-700',
    InProgress: 'bg-yellow-100 text-yellow-700',
    WaitingResponse: 'bg-purple-100 text-purple-700',
    Resolved: 'bg-green-100 text-green-700',
    Closed: 'bg-gray-100 text-gray-500',
};

const PRIORITY_LABELS: Record<TicketPriority, string> = {
    Low: 'Baixa',
    Medium: 'Média',
    High: 'Alta',
    Urgent: 'Urgente',
};

const PRIORITY_COLORS: Record<TicketPriority, string> = {
    Low: 'bg-slate-100 text-slate-600',
    Medium: 'bg-blue-100 text-blue-700',
    High: 'bg-orange-100 text-orange-700',
    Urgent: 'bg-red-100 text-red-700',
};

const PRIORITY_DOT: Record<TicketPriority, string> = {
    Low: 'bg-slate-400',
    Medium: 'bg-blue-500',
    High: 'bg-orange-500',
    Urgent: 'bg-red-500',
};

const CATEGORY_LABELS: Record<TicketCategory, string> = {
    Bug: 'Bug',
    FeatureRequest: 'Feature',
    Question: 'Dúvida',
    Billing: 'Financeiro',
    Other: 'Outro',
};

const CATEGORY_COLORS: Record<TicketCategory, string> = {
    Bug: 'bg-red-50 text-red-700 border-red-200',
    FeatureRequest: 'bg-violet-50 text-violet-700 border-violet-200',
    Question: 'bg-sky-50 text-sky-700 border-sky-200',
    Billing: 'bg-amber-50 text-amber-700 border-amber-200',
    Other: 'bg-slate-50 text-slate-600 border-slate-200',
};

// ───────────── Filter tab type ─────────────

type FilterTab = 'All' | TicketStatus;

const FILTER_TABS: { key: FilterTab; label: string }[] = [
    { key: 'All', label: 'Todos' },
    { key: 'Open', label: 'Abertos' },
    { key: 'InProgress', label: 'Em Andamento' },
    { key: 'WaitingResponse', label: 'Aguardando' },
    { key: 'Resolved', label: 'Resolvidos' },
    { key: 'Closed', label: 'Fechados' },
];

// ───────────── Delete confirm modal ─────────────

interface DeleteConfirmProps {
    isOpen: boolean;
    onClose: () => void;
    onConfirm: () => void;
    loading: boolean;
}

function DeleteConfirmModal({ isOpen, onClose, onConfirm, loading }: DeleteConfirmProps) {
    if (!isOpen) return null;
    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm">
            <div className="bg-white p-8 rounded-2xl shadow-2xl max-w-md w-full mx-4 border border-gray-100">
                <h3 className="text-xl font-bold text-gray-900 mb-2">Confirmar Exclusão</h3>
                <p className="text-gray-600 mb-8">
                    Tem certeza que deseja excluir este ticket? Esta ação não pode ser desfeita.
                </p>
                <div className="flex justify-end gap-3">
                    <Button variant="ghost" onClick={onClose} disabled={loading} className="px-6 rounded-xl">
                        Cancelar
                    </Button>
                    <Button onClick={onConfirm} disabled={loading} variant="destructive" className="px-6 rounded-xl">
                        {loading ? 'Excluindo...' : 'Sim, Excluir'}
                    </Button>
                </div>
            </div>
        </div>
    );
}

// ───────────── Form component ─────────────

interface TicketFormProps {
    ticket?: SupportTicket;
    onSave: () => void;
    onClose: () => void;
}

function TicketForm({ ticket, onSave, onClose }: TicketFormProps) {
    const isEditing = !!ticket;
    const [subject, setSubject] = useState(ticket?.subject ?? '');
    const [description, setDescription] = useState(ticket?.description ?? '');
    const [priority, setPriority] = useState<TicketPriority>(ticket?.priority ?? 'Medium');
    const [category, setCategory] = useState<TicketCategory>(ticket?.category ?? 'Other');
    const [status, setStatus] = useState<TicketStatus>(ticket?.status ?? 'Open');
    const [customerName, setCustomerName] = useState(ticket?.customerName ?? '');

    const createMutation = useCreateTicket();
    const updateMutation = useUpdateTicket();
    const isSaving = createMutation.isPending || updateMutation.isPending;

    const handleSubmit = (e: React.FormEvent) => {
        e.preventDefault();
        if (!subject.trim()) {
            toast.error('O assunto é obrigatório.');
            return;
        }

        if (isEditing) {
            const req: UpdateTicketRequest = {
                subject: subject.trim(),
                description: description.trim() || undefined,
                status,
                priority,
                category,
                customerName: customerName.trim() || undefined,
            };
            updateMutation.mutate({ id: ticket.id, req }, {
                onSuccess: () => { toast.success('Ticket atualizado.'); onSave(); },
                onError: () => toast.error('Erro ao salvar ticket.'),
            });
        } else {
            const req: CreateTicketRequest = {
                subject: subject.trim(),
                description: description.trim() || undefined,
                priority,
                category,
                customerName: customerName.trim() || undefined,
            };
            createMutation.mutate(req, {
                onSuccess: () => { toast.success('Ticket criado.'); onSave(); },
                onError: () => toast.error('Erro ao criar ticket.'),
            });
        }
    };

    return (
        <form onSubmit={handleSubmit} className="space-y-4">
            <div className="space-y-1.5">
                <Label htmlFor="subject">Assunto *</Label>
                <Input
                    id="subject"
                    value={subject}
                    onChange={e => setSubject(e.target.value)}
                    placeholder="Descreva o problema em poucas palavras"
                    required
                />
            </div>

            <div className="space-y-1.5">
                <Label htmlFor="customerName">Cliente</Label>
                <Input
                    id="customerName"
                    value={customerName}
                    onChange={e => setCustomerName(e.target.value)}
                    placeholder="Nome do cliente (opcional)"
                />
            </div>

            <div className="grid grid-cols-2 gap-4">
                <div className="space-y-1.5">
                    <Label>Prioridade</Label>
                    <Select value={priority} onValueChange={v => setPriority(v as TicketPriority)}>
                        <SelectTrigger>
                            <SelectValue />
                        </SelectTrigger>
                        <SelectContent>
                            {(Object.keys(PRIORITY_LABELS) as TicketPriority[]).map(p => (
                                <SelectItem key={p} value={p}>{PRIORITY_LABELS[p]}</SelectItem>
                            ))}
                        </SelectContent>
                    </Select>
                </div>

                <div className="space-y-1.5">
                    <Label>Categoria</Label>
                    <Select value={category} onValueChange={v => setCategory(v as TicketCategory)}>
                        <SelectTrigger>
                            <SelectValue />
                        </SelectTrigger>
                        <SelectContent>
                            {(Object.keys(CATEGORY_LABELS) as TicketCategory[]).map(c => (
                                <SelectItem key={c} value={c}>{CATEGORY_LABELS[c]}</SelectItem>
                            ))}
                        </SelectContent>
                    </Select>
                </div>
            </div>

            {isEditing && (
                <div className="space-y-1.5">
                    <Label>Status</Label>
                    <Select value={status} onValueChange={v => setStatus(v as TicketStatus)}>
                        <SelectTrigger>
                            <SelectValue />
                        </SelectTrigger>
                        <SelectContent>
                            {(Object.keys(STATUS_LABELS) as TicketStatus[]).map(s => (
                                <SelectItem key={s} value={s}>{STATUS_LABELS[s]}</SelectItem>
                            ))}
                        </SelectContent>
                    </Select>
                </div>
            )}

            <div className="space-y-1.5">
                <Label htmlFor="description">Descrição</Label>
                <Textarea
                    id="description"
                    value={description}
                    onChange={e => setDescription(e.target.value)}
                    placeholder="Detalhes adicionais sobre o ticket..."
                    rows={4}
                />
            </div>

            <div className="flex justify-end gap-3 pt-2">
                <Button type="button" variant="ghost" onClick={onClose} disabled={isSaving}>
                    Cancelar
                </Button>
                <Button type="submit" disabled={isSaving} className="min-w-[120px]">
                    {isSaving ? <Loader2 className="h-4 w-4 animate-spin" /> : isEditing ? 'Salvar' : 'Criar Ticket'}
                </Button>
            </div>
        </form>
    );
}

// ───────────── Ticket card ─────────────

interface TicketCardProps {
    ticket: SupportTicket;
    onEdit: (t: SupportTicket) => void;
    onDeleteRequest: (id: string) => void;
    onResolve: (id: string) => void;
    onReopen: (id: string) => void;
    onClose: (id: string) => void;
    actionLoadingId: string | null;
}

function TicketCard({ ticket, onEdit, onDeleteRequest, onResolve, onReopen, onClose, actionLoadingId }: TicketCardProps) {
    const isLoading = actionLoadingId === ticket.id;
    const isResolved = ticket.status === 'Resolved' || ticket.status === 'Closed';

    return (
        <div className={`bg-white border border-slate-200 rounded-xl p-4 shadow-sm hover:shadow-md transition-all ${isResolved ? 'opacity-70' : ''}`}>
            <div className="flex items-start justify-between gap-3">
                <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2 flex-wrap mb-1.5">
                        <span className={`inline-flex items-center gap-1 text-xs font-semibold px-2 py-0.5 rounded-full ${STATUS_COLORS[ticket.status]}`}>
                            {STATUS_LABELS[ticket.status]}
                        </span>
                        <span className={`inline-flex items-center gap-1.5 text-xs font-medium px-2 py-0.5 rounded-full ${PRIORITY_COLORS[ticket.priority]}`}>
                            <span className={`h-1.5 w-1.5 rounded-full ${PRIORITY_DOT[ticket.priority]}`} />
                            {PRIORITY_LABELS[ticket.priority]}
                        </span>
                        <span className={`text-xs font-medium px-2 py-0.5 rounded-md border ${CATEGORY_COLORS[ticket.category]}`}>
                            {CATEGORY_LABELS[ticket.category]}
                        </span>
                    </div>

                    <p className={`font-semibold text-slate-900 ${isResolved ? 'line-through text-slate-500' : ''}`}>
                        {ticket.subject}
                    </p>

                    {ticket.customerName && (
                        <p className="text-xs text-slate-500 mt-0.5">{ticket.customerName}</p>
                    )}

                    {ticket.description && (
                        <p className="text-sm text-slate-500 mt-1.5 line-clamp-2">{ticket.description}</p>
                    )}

                    <div className="flex items-center gap-3 mt-2 text-xs text-slate-400">
                        <span className="flex items-center gap-1">
                            <Clock className="h-3 w-3" />
                            {format(parseISO(ticket.createdAt), "d MMM yyyy", { locale: ptBR })}
                        </span>
                        {ticket.resolvedAt && (
                            <span className="flex items-center gap-1 text-green-600">
                                <CheckCircle2 className="h-3 w-3" />
                                Resolvido {format(parseISO(ticket.resolvedAt), "d MMM", { locale: ptBR })}
                            </span>
                        )}
                    </div>
                </div>

                {isLoading ? (
                    <Loader2 className="h-4 w-4 animate-spin text-slate-400 shrink-0 mt-1" />
                ) : (
                    <div className="flex items-center gap-1 shrink-0">
                        {!isResolved && (
                            <Button
                                variant="ghost"
                                size="icon"
                                className="h-8 w-8 text-green-600 hover:text-green-700 hover:bg-green-50 rounded-lg"
                                title="Resolver"
                                onClick={() => onResolve(ticket.id)}
                            >
                                <Check className="h-4 w-4" />
                            </Button>
                        )}
                        {ticket.status === 'Resolved' && (
                            <Button
                                variant="ghost"
                                size="icon"
                                className="h-8 w-8 text-gray-500 hover:text-gray-700 hover:bg-gray-100 rounded-lg"
                                title="Fechar"
                                onClick={() => onClose(ticket.id)}
                            >
                                <XCircle className="h-4 w-4" />
                            </Button>
                        )}
                        {isResolved && (
                            <Button
                                variant="ghost"
                                size="icon"
                                className="h-8 w-8 text-blue-600 hover:text-blue-700 hover:bg-blue-50 rounded-lg"
                                title="Reabrir"
                                onClick={() => onReopen(ticket.id)}
                            >
                                <RotateCcw className="h-4 w-4" />
                            </Button>
                        )}
                        <Button
                            variant="ghost"
                            size="icon"
                            className="h-8 w-8 text-slate-400 hover:text-slate-600 hover:bg-slate-100 rounded-lg"
                            title="Editar"
                            onClick={() => onEdit(ticket)}
                        >
                            <X className="h-4 w-4" />
                        </Button>
                        <Button
                            variant="ghost"
                            size="icon"
                            className="h-8 w-8 text-slate-400 hover:text-red-600 hover:bg-red-50 rounded-lg"
                            title="Excluir"
                            onClick={() => onDeleteRequest(ticket.id)}
                        >
                            <Trash2 className="h-4 w-4" />
                        </Button>
                    </div>
                )}
            </div>
        </div>
    );
}

// ───────────── Main component ─────────────

export function TicketsClient() {
    const [filter, setFilter] = useState<FilterTab>('All');
    const [isFormOpen, setIsFormOpen] = useState(false);
    const [editingTicket, setEditingTicket] = useState<SupportTicket | undefined>();
    const [deleteConfirmId, setDeleteConfirmId] = useState<string | null>(null);

    const { data: allTickets = [], isLoading } = useTickets();
    const deleteMutation = useDeleteTicket();
    const resolveMutation = useResolveTicket();
    const reopenMutation = useReopenTicket();
    const closeMutation = useCloseTicket();

    const filteredTickets = useMemo(
        () => filter === 'All' ? allTickets : allTickets.filter(t => t.status === filter),
        [allTickets, filter],
    );

    const actionLoadingId: string | null =
        (resolveMutation.isPending ? resolveMutation.variables ?? null : null) ??
        (reopenMutation.isPending ? reopenMutation.variables ?? null : null) ??
        (closeMutation.isPending ? closeMutation.variables ?? null : null) ??
        (deleteMutation.isPending ? deleteMutation.variables ?? null : null);

    const handleDeleteConfirm = () => {
        if (!deleteConfirmId) return;
        deleteMutation.mutate(deleteConfirmId, {
            onSuccess: () => { toast.success('Ticket excluído.'); setDeleteConfirmId(null); },
            onError: () => { toast.error('Erro ao excluir ticket.'); setDeleteConfirmId(null); },
        });
    };

    const handleResolve = (id: string) => {
        resolveMutation.mutate(id, {
            onSuccess: () => toast.success('Ticket resolvido.'),
            onError: () => toast.error('Erro ao resolver ticket.'),
        });
    };

    const handleReopen = (id: string) => {
        reopenMutation.mutate(id, {
            onSuccess: () => toast.success('Ticket reaberto.'),
            onError: () => toast.error('Erro ao reabrir ticket.'),
        });
    };

    const handleClose = (id: string) => {
        closeMutation.mutate(id, {
            onSuccess: () => toast.success('Ticket fechado.'),
            onError: () => toast.error('Erro ao fechar ticket.'),
        });
    };

    const closeForm = () => { setIsFormOpen(false); setEditingTicket(undefined); };

    const openCounts = allTickets.filter(t => t.status === 'Open' || t.status === 'InProgress').length;

    return (
        <div className="p-8 max-w-[1200px] mx-auto space-y-8">
            <DeleteConfirmModal
                isOpen={!!deleteConfirmId}
                onClose={() => setDeleteConfirmId(null)}
                onConfirm={handleDeleteConfirm}
                loading={deleteMutation.isPending}
            />

            {/* Header */}
            <div className="flex flex-col md:flex-row justify-between items-start md:items-center gap-4">
                <div>
                    <h1 className="text-3xl font-extrabold text-gray-900 tracking-tight flex items-center gap-3">
                        <div className="bg-violet-100 p-2 rounded-xl">
                            <Ticket className="h-8 w-8 text-violet-600" />
                        </div>
                        Helpdesk
                    </h1>
                    <p className="text-gray-500 mt-1">
                        {openCounts > 0
                            ? `${openCounts} ticket${openCounts > 1 ? 's' : ''} aberto${openCounts > 1 ? 's' : ''}`
                            : 'Todos os tickets resolvidos'}
                    </p>
                </div>

                <Button
                    onClick={() => { setEditingTicket(undefined); setIsFormOpen(true); }}
                    className="bg-accent hover:bg-accent-secondary text-white gap-2 px-6 h-12 shadow-md hover:shadow-lg transition-all rounded-xl"
                >
                    <Plus size={20} strokeWidth={3} />
                    <span className="font-bold">Novo Ticket</span>
                </Button>
            </div>

            {/* Filter tabs */}
            <div className="flex flex-wrap gap-2">
                {FILTER_TABS.map(tab => {
                    const isActive = filter === tab.key;
                    const count = tab.key === 'All'
                        ? allTickets.length
                        : allTickets.filter(t => t.status === tab.key).length;
                    return (
                        <button
                            key={tab.key}
                            onClick={() => setFilter(tab.key)}
                            className={`inline-flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-medium transition-all ${
                                isActive
                                    ? 'bg-slate-900 text-white shadow-sm'
                                    : 'bg-white border border-slate-200 text-slate-600 hover:bg-slate-50'
                            }`}
                        >
                            {tab.label}
                            {count > 0 && (
                                <span className={`text-xs font-bold px-1.5 py-0.5 rounded-full ${
                                    isActive ? 'bg-white/20 text-white' : 'bg-slate-100 text-slate-500'
                                }`}>
                                    {count}
                                </span>
                            )}
                        </button>
                    );
                })}
            </div>

            {/* Ticket list */}
            {isLoading ? (
                <div className="flex items-center justify-center py-20">
                    <Loader2 className="h-8 w-8 animate-spin text-slate-400" />
                </div>
            ) : filteredTickets.length === 0 ? (
                <div className="flex flex-col items-center justify-center py-20 text-center">
                    <div className="bg-slate-100 p-4 rounded-full mb-4">
                        <Ticket className="h-10 w-10 text-slate-400" />
                    </div>
                    <p className="text-slate-500 font-medium">Nenhum ticket encontrado</p>
                    <p className="text-slate-400 text-sm mt-1">
                        {filter === 'All' ? 'Crie o primeiro ticket clicando em "Novo Ticket".' : 'Nenhum ticket com esse status.'}
                    </p>
                </div>
            ) : (
                <div className="grid gap-3 sm:grid-cols-1 lg:grid-cols-2">
                    {filteredTickets.map(ticket => (
                        <TicketCard
                            key={ticket.id}
                            ticket={ticket}
                            onEdit={t => { setEditingTicket(t); setIsFormOpen(true); }}
                            onDeleteRequest={setDeleteConfirmId}
                            onResolve={handleResolve}
                            onReopen={handleReopen}
                            onClose={handleClose}
                            actionLoadingId={actionLoadingId}
                        />
                    ))}
                </div>
            )}

            {/* Create / Edit dialog */}
            <Dialog open={isFormOpen} onOpenChange={open => { if (!open) closeForm(); }}>
                <DialogContent className="max-w-lg">
                    <DialogHeader>
                        <DialogTitle>{editingTicket ? 'Editar Ticket' : 'Novo Ticket'}</DialogTitle>
                    </DialogHeader>
                    <TicketForm
                        ticket={editingTicket}
                        onSave={closeForm}
                        onClose={closeForm}
                    />
                </DialogContent>
            </Dialog>
        </div>
    );
}
