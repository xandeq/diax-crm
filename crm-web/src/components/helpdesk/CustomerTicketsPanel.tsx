'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Textarea } from '@/components/ui/textarea';
import { Skeleton } from '@/components/ui/skeleton';
import {
    CreateTicketRequest,
    SupportTicket,
    TicketCategory,
    TicketPriority,
    TicketStatus,
    helpdeskService,
} from '@/services/helpdesk';
import { format, parseISO } from 'date-fns';
import { ptBR } from 'date-fns/locale';
import { Check, CheckCircle2, Clock, Loader2, Plus, RotateCcw, Ticket, XCircle } from 'lucide-react';
import { useEffect, useState } from 'react';
import { toast } from 'sonner';

const STATUS_LABELS: Record<TicketStatus, string> = {
    Open: 'Aberto',
    InProgress: 'Em Andamento',
    WaitingResponse: 'Aguardando',
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

interface Props {
    customerId: string;
    customerName: string;
}

interface QuickCreateFormProps {
    customerId: string;
    customerName: string;
    onSaved: (ticket: SupportTicket) => void;
    onClose: () => void;
}

function QuickCreateForm({ customerId, customerName, onSaved, onClose }: QuickCreateFormProps) {
    const [subject, setSubject] = useState('');
    const [description, setDescription] = useState('');
    const [priority, setPriority] = useState<TicketPriority>('Medium');
    const [category, setCategory] = useState<TicketCategory>('Other');
    const [isSaving, setIsSaving] = useState(false);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!subject.trim()) { toast.error('O assunto é obrigatório.'); return; }
        setIsSaving(true);
        try {
            const req: CreateTicketRequest = {
                subject: subject.trim(),
                description: description.trim() || undefined,
                priority,
                category,
                customerId,
                customerName,
            };
            const result = await helpdeskService.create(req);
            onSaved(result);
            toast.success('Ticket criado.');
        } catch {
            toast.error('Erro ao criar ticket.');
        } finally {
            setIsSaving(false);
        }
    };

    return (
        <form onSubmit={handleSubmit} className="space-y-4">
            <div className="space-y-1.5">
                <Label htmlFor="cts-subject">Assunto *</Label>
                <Input
                    id="cts-subject"
                    value={subject}
                    onChange={e => setSubject(e.target.value)}
                    placeholder="Descrição breve do problema"
                    required
                />
            </div>
            <div className="grid grid-cols-2 gap-3">
                <div className="space-y-1.5">
                    <Label>Prioridade</Label>
                    <Select value={priority} onValueChange={v => setPriority(v as TicketPriority)}>
                        <SelectTrigger><SelectValue /></SelectTrigger>
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
                        <SelectTrigger><SelectValue /></SelectTrigger>
                        <SelectContent>
                            {(Object.keys(CATEGORY_LABELS) as TicketCategory[]).map(c => (
                                <SelectItem key={c} value={c}>{CATEGORY_LABELS[c]}</SelectItem>
                            ))}
                        </SelectContent>
                    </Select>
                </div>
            </div>
            <div className="space-y-1.5">
                <Label htmlFor="cts-desc">Descrição</Label>
                <Textarea
                    id="cts-desc"
                    value={description}
                    onChange={e => setDescription(e.target.value)}
                    placeholder="Detalhes adicionais..."
                    rows={3}
                />
            </div>
            <div className="flex justify-end gap-3 pt-1">
                <Button type="button" variant="ghost" onClick={onClose} disabled={isSaving}>Cancelar</Button>
                <Button type="submit" disabled={isSaving} className="min-w-[110px]">
                    {isSaving ? <Loader2 className="h-4 w-4 animate-spin" /> : 'Criar Ticket'}
                </Button>
            </div>
        </form>
    );
}

export function CustomerTicketsPanel({ customerId, customerName }: Props) {
    const [tickets, setTickets] = useState<SupportTicket[]>([]);
    const [loading, setLoading] = useState(true);
    const [actionLoading, setActionLoading] = useState<string | null>(null);
    const [isCreateOpen, setIsCreateOpen] = useState(false);

    useEffect(() => {
        let cancelled = false;
        setLoading(true);
        helpdeskService.getAll({ customerId })
            .then(data => { if (!cancelled) setTickets(data); })
            .catch(() => toast.error('Erro ao carregar tickets.'))
            .finally(() => { if (!cancelled) setLoading(false); });
        return () => { cancelled = true; };
    }, [customerId]);

    const handleResolve = async (id: string) => {
        setActionLoading(id);
        try {
            const updated = await helpdeskService.resolve(id);
            setTickets(prev => prev.map(t => t.id === id ? updated : t));
        } catch { toast.error('Erro ao resolver ticket.'); }
        finally { setActionLoading(null); }
    };

    const handleReopen = async (id: string) => {
        setActionLoading(id);
        try {
            const updated = await helpdeskService.reopen(id);
            setTickets(prev => prev.map(t => t.id === id ? updated : t));
        } catch { toast.error('Erro ao reabrir ticket.'); }
        finally { setActionLoading(null); }
    };

    const handleClose = async (id: string) => {
        setActionLoading(id);
        try {
            const updated = await helpdeskService.close(id);
            setTickets(prev => prev.map(t => t.id === id ? updated : t));
        } catch { toast.error('Erro ao fechar ticket.'); }
        finally { setActionLoading(null); }
    };

    const openCount = tickets.filter(t => t.status === 'Open' || t.status === 'InProgress').length;

    if (loading) {
        return (
            <div className="space-y-3">
                <Skeleton className="h-16 rounded-xl" />
                <Skeleton className="h-16 rounded-xl" />
                <Skeleton className="h-16 rounded-xl" />
            </div>
        );
    }

    return (
        <div className="space-y-4">
            {/* Header */}
            <div className="flex items-center justify-between">
                <div>
                    <p className="text-sm font-semibold text-slate-700">
                        {tickets.length === 0
                            ? 'Nenhum ticket'
                            : `${tickets.length} ticket${tickets.length > 1 ? 's' : ''}${openCount > 0 ? ` · ${openCount} aberto${openCount > 1 ? 's' : ''}` : ''}`}
                    </p>
                </div>
                <Button
                    size="sm"
                    onClick={() => setIsCreateOpen(true)}
                    className="gap-1.5 h-8 text-xs"
                >
                    <Plus className="h-3.5 w-3.5" />
                    Novo Ticket
                </Button>
            </div>

            {/* Ticket list */}
            {tickets.length === 0 ? (
                <div className="flex flex-col items-center justify-center py-10 text-center border-2 border-dashed border-slate-200 rounded-xl">
                    <Ticket className="h-8 w-8 text-slate-300 mb-2" />
                    <p className="text-sm text-slate-500">Nenhum ticket para este cliente</p>
                    <p className="text-xs text-slate-400 mt-0.5">Clique em "Novo Ticket" para abrir um.</p>
                </div>
            ) : (
                <div className="space-y-2">
                    {tickets.map(ticket => {
                        const isLoading = actionLoading === ticket.id;
                        const isResolved = ticket.status === 'Resolved' || ticket.status === 'Closed';
                        return (
                            <div
                                key={ticket.id}
                                className={`bg-white border border-slate-200 rounded-xl p-3 ${isResolved ? 'opacity-60' : ''}`}
                            >
                                <div className="flex items-start justify-between gap-2">
                                    <div className="flex-1 min-w-0">
                                        <div className="flex items-center gap-1.5 flex-wrap mb-1">
                                            <span className={`inline-flex items-center text-xs font-semibold px-1.5 py-0.5 rounded-full ${STATUS_COLORS[ticket.status]}`}>
                                                {STATUS_LABELS[ticket.status]}
                                            </span>
                                            <span className="flex items-center gap-1 text-xs text-slate-500">
                                                <span className={`h-1.5 w-1.5 rounded-full ${PRIORITY_DOT[ticket.priority]}`} />
                                                {PRIORITY_LABELS[ticket.priority]}
                                            </span>
                                        </div>
                                        <p className={`text-sm font-semibold text-slate-900 truncate ${isResolved ? 'line-through text-slate-400' : ''}`}>
                                            {ticket.subject}
                                        </p>
                                        <p className="text-xs text-slate-400 mt-0.5 flex items-center gap-1">
                                            <Clock className="h-3 w-3" />
                                            {format(parseISO(ticket.createdAt), "d MMM yyyy", { locale: ptBR })}
                                            {ticket.resolvedAt && (
                                                <span className="flex items-center gap-1 text-green-600 ml-1">
                                                    <CheckCircle2 className="h-3 w-3" />
                                                    Resolvido {format(parseISO(ticket.resolvedAt), "d MMM", { locale: ptBR })}
                                                </span>
                                            )}
                                        </p>
                                    </div>

                                    {isLoading ? (
                                        <Loader2 className="h-4 w-4 animate-spin text-slate-400 shrink-0" />
                                    ) : (
                                        <div className="flex items-center gap-0.5 shrink-0">
                                            {!isResolved && (
                                                <Button variant="ghost" size="icon" className="h-7 w-7 text-green-600 hover:bg-green-50 rounded-lg" title="Resolver" onClick={() => handleResolve(ticket.id)}>
                                                    <Check className="h-3.5 w-3.5" />
                                                </Button>
                                            )}
                                            {ticket.status === 'Resolved' && (
                                                <Button variant="ghost" size="icon" className="h-7 w-7 text-gray-500 hover:bg-gray-50 rounded-lg" title="Fechar" onClick={() => handleClose(ticket.id)}>
                                                    <XCircle className="h-3.5 w-3.5" />
                                                </Button>
                                            )}
                                            {isResolved && (
                                                <Button variant="ghost" size="icon" className="h-7 w-7 text-blue-600 hover:bg-blue-50 rounded-lg" title="Reabrir" onClick={() => handleReopen(ticket.id)}>
                                                    <RotateCcw className="h-3.5 w-3.5" />
                                                </Button>
                                            )}
                                        </div>
                                    )}
                                </div>
                            </div>
                        );
                    })}
                </div>
            )}

            {/* Create ticket dialog */}
            <Dialog open={isCreateOpen} onOpenChange={setIsCreateOpen}>
                <DialogContent className="max-w-md">
                    <DialogHeader>
                        <DialogTitle>Novo Ticket — {customerName}</DialogTitle>
                    </DialogHeader>
                    <QuickCreateForm
                        customerId={customerId}
                        customerName={customerName}
                        onSaved={(ticket) => {
                            setTickets(prev => [ticket, ...prev]);
                            setIsCreateOpen(false);
                        }}
                        onClose={() => setIsCreateOpen(false)}
                    />
                </DialogContent>
            </Dialog>
        </div>
    );
}
