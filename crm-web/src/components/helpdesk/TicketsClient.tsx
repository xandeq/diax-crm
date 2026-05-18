'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Textarea } from '@/components/ui/textarea';
import {
    CreateTicketRequest,
    SupportTicket,
    TicketCategory,
    TicketPriority,
    TicketStatus,
    TicketsQuery,
    UpdateTicketRequest,
    helpdeskService,
} from '@/services/helpdesk';
import { format, parseISO } from 'date-fns';
import { ptBR } from 'date-fns/locale';
import { AlertCircle, Check, CheckCircle2, Clock, Loader2, Plus, RotateCcw, Ticket, Trash2, X, XCircle } from 'lucide-react';
import { useCallback, useEffect, useState } from 'react';
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

// ───────────── Form component ─────────────

interface TicketFormProps {
    ticket?: SupportTicket;
    onSave: (ticket: SupportTicket) => void;
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
    const [isSaving, setIsSaving] = useState(false);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!subject.trim()) {
            toast.error('O assunto é obrigatório.');
            return;
        }

        setIsSaving(true);
        try {
            let result: SupportTicket;
            if (isEditing) {
                const req: UpdateTicketRequest = {
                    subject: subject.trim(),
                    description: description.trim() || undefined,
                    status,
                    priority,
                    category,
                    customerName: customerName.trim() || undefined,
                };
                result = await helpdeskService.update(ticket.id, req);
            } else {
                const req: CreateTicketRequest = {
                    subject: subject.trim(),
                    description: description.trim() || undefined,
                    priority,
                    category,
                    customerName: customerName.trim() || undefined,
                };
                result = await helpdeskService.create(req);
            }
            onSave(result);
            toast.success(isEditing ? 'Ticket atualizado.' : 'Ticket criado.');
        } catch {
            toast.error('Erro ao salvar ticket.');
        } finally {
            setIsSaving(false);
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
    onDelete: (id: string) => void;
    onResolve: (id: string) => void;
    onReopen: (id: string) => void;
    onClose: (id: string) => void;
    actionLoading: string | null;
}

function TicketCard({ ticket, onEdit, onDelete, onResolve, onReopen, onClose, actionLoading }: TicketCardProps) {
    const isLoading = actionLoading === ticket.id;
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
                                className="h-8 w-8 text-gray-500 hover:text-gray-700 hover:bg-gray-50 rounded-lg"
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
                            className="h-8 w-8 text-slate-400 hover:text-slate-700 hover:bg-slate-50 rounded-lg"
                            title="Editar"
                            onClick={() => onEdit(ticket)}
                        >
                            <AlertCircle className="h-4 w-4" />
                        </Button>
                        <Button
                            variant="ghost"
                            size="icon"
                            className="h-8 w-8 text-slate-400 hover:text-red-600 hover:bg-red-50 rounded-lg"
                            title="Excluir"
                            onClick={() => onDelete(ticket.id)}
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
    const [tickets, setTickets] = useState<SupportTicket[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [filter, setFilter] = useState<FilterTab>('All');
    const [isFormOpen, setIsFormOpen] = useState(false);
    const [editingTicket, setEditingTicket] = useState<SupportTicket | undefined>();
    const [actionLoading, setActionLoading] = useState<string | null>(null);

    const loadTickets = useCallback(async () => {
        setIsLoading(true);
        try {
            const query: TicketsQuery = filter !== 'All' ? { status: filter } : {};
            setTickets(await helpdeskService.getAll(query));
        } catch {
            toast.error('Erro ao carregar tickets.');
        } finally {
            setIsLoading(false);
        }
    }, [filter]);

    useEffect(() => { loadTickets(); }, [loadTickets]);

    const handleSave = (saved: SupportTicket) => {
        setTickets(prev =>
            prev.some(t => t.id === saved.id)
                ? prev.map(t => t.id === saved.id ? saved : t)
                : [saved, ...prev]
        );
        setIsFormOpen(false);
        setEditingTicket(undefined);
    };

    const handleDelete = async (id: string) => {
        if (!confirm('Tem certeza que deseja excluir este ticket?')) return;
        setActionLoading(id);
        try {
            await helpdeskService.delete(id);
            setTickets(prev => prev.filter(t => t.id !== id));
            toast.success('Ticket excluído.');
        } catch {
            toast.error('Erro ao excluir ticket.');
        } finally {
            setActionLoading(null);
        }
    };

    const handleResolve = async (id: string) => {
        setActionLoading(id);
        try {
            const updated = await helpdeskService.resolve(id);
            setTickets(prev => prev.map(t => t.id === id ? updated : t));
            toast.success('Ticket resolvido.');
        } catch {
            toast.error('Erro ao resolver ticket.');
        } finally {
            setActionLoading(null);
        }
    };

    const handleReopen = async (id: string) => {
        setActionLoading(id);
        try {
            const updated = await helpdeskService.reopen(id);
            setTickets(prev => prev.map(t => t.id === id ? updated : t));
            toast.success('Ticket reaberto.');
        } catch {
            toast.error('Erro ao reabrir ticket.');
        } finally {
            setActionLoading(null);
        }
    };

    const handleClose = async (id: string) => {
        setActionLoading(id);
        try {
            const updated = await helpdeskService.close(id);
            setTickets(prev => prev.map(t => t.id === id ? updated : t));
            toast.success('Ticket fechado.');
        } catch {
            toast.error('Erro ao fechar ticket.');
        } finally {
            setActionLoading(null);
        }
    };

    const openCounts = tickets.filter(t => t.status === 'Open' || t.status === 'InProgress').length;

    return (
        <div className="p-8 max-w-[1200px] mx-auto space-y-8">
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
                        ? tickets.length
                        : tickets.filter(t => t.status === tab.key).length;
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
            ) : tickets.length === 0 ? (
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
                    {tickets.map(ticket => (
                        <TicketCard
                            key={ticket.id}
                            ticket={ticket}
                            onEdit={t => { setEditingTicket(t); setIsFormOpen(true); }}
                            onDelete={handleDelete}
                            onResolve={handleResolve}
                            onReopen={handleReopen}
                            onClose={handleClose}
                            actionLoading={actionLoading}
                        />
                    ))}
                </div>
            )}

            {/* Create / Edit dialog */}
            <Dialog open={isFormOpen} onOpenChange={open => { if (!open) { setIsFormOpen(false); setEditingTicket(undefined); } }}>
                <DialogContent className="max-w-lg">
                    <DialogHeader>
                        <DialogTitle>{editingTicket ? 'Editar Ticket' : 'Novo Ticket'}</DialogTitle>
                    </DialogHeader>
                    <TicketForm
                        ticket={editingTicket}
                        onSave={handleSave}
                        onClose={() => { setIsFormOpen(false); setEditingTicket(undefined); }}
                    />
                </DialogContent>
            </Dialog>
        </div>
    );
}
