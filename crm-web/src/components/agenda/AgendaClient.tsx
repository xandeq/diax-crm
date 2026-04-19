'use client';

import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { agendaService } from '@/services/agenda';
import { appointmentLabelsService } from '@/services/appointmentLabels';
import { AiBatchChange, AiBatchResponse, Appointment, AppointmentLabel, AppointmentType, CreateAppointmentDto, RecurringAppointmentDto, UpdateAppointmentDto } from '@/types/agenda';
import { addDays, addMonths, addWeeks, eachDayOfInterval, endOfMonth, endOfWeek, format, isSameDay, isSameMonth, isToday, parseISO, startOfMonth, startOfWeek, subMonths, subWeeks } from 'date-fns';
import { ptBR } from 'date-fns/locale';
import { Calendar as CalendarIcon, Check, ChevronLeft, ChevronRight, Clock, List as ListIcon, Loader2, Plus, Send, Sparkles, Tag, Trash2 } from 'lucide-react';
import { useCallback, useEffect, useRef, useState } from 'react';
import { toast } from 'sonner';
import { AppointmentForm } from './AppointmentForm';
import { ImportTextDialog } from './ImportTextDialog';
import { LabelManagerModal } from './LabelManagerModal';
import { WeekGrid } from './WeekGrid';

const TYPE_LABELS: Record<AppointmentType, string> = {
    Medical: 'Médico',
    HomeService: 'Serviço',
    Payment: 'Pagamento',
    Other: 'Outro'
};

type ViewMode = 'month' | 'week' | 'list';

export function AgendaClient() {
    const [currentDate, setCurrentDate] = useState(new Date());
    const [appointments, setAppointments] = useState<Appointment[]>([]);
    const [labels, setLabels] = useState<AppointmentLabel[]>([]);
    const [isLoading, setIsLoading] = useState(false);
    const [view, setView] = useState<ViewMode>('month');

    const [isFormOpen, setIsFormOpen] = useState(false);
    const [isImportOpen, setIsImportOpen] = useState(false);
    const [isLabelManagerOpen, setIsLabelManagerOpen] = useState(false);
    const [editingAppointment, setEditingAppointment] = useState<Appointment | undefined>();
    const [preselectedDate, setPreselectedDate] = useState<Date | undefined>();

    // Recurring delete modal
    const [deleteTarget, setDeleteTarget] = useState<Appointment | undefined>();
    const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);

    // AI chat batch
    const [aiCommand, setAiCommand] = useState('');
    const [isAiLoading, setIsAiLoading] = useState(false);
    const [aiBatchResult, setAiBatchResult] = useState<AiBatchResponse | null>(null);
    const [isAiBatchModalOpen, setIsAiBatchModalOpen] = useState(false);
    const [isApplyingBatch, setIsApplyingBatch] = useState(false);
    const aiInputRef = useRef<HTMLInputElement>(null);

    const fetchLabels = useCallback(async () => {
        try {
            const data = await appointmentLabelsService.getAll();
            setLabels(data);
        } catch {
            // non-critical
        }
    }, []);

    const fetchAppointments = useCallback(async () => {
        setIsLoading(true);
        try {
            let start: Date, end: Date;
            if (view === 'week') {
                start = startOfWeek(currentDate, { weekStartsOn: 1 });
                end = endOfWeek(currentDate, { weekStartsOn: 1 });
            } else {
                // Month view: starts from Monday
                start = startOfWeek(startOfMonth(currentDate), { weekStartsOn: 1 });
                end = endOfWeek(endOfMonth(currentDate), { weekStartsOn: 1 });
            }
            const data = await agendaService.getByDateRange(start.toISOString(), end.toISOString());
            setAppointments(data);
        } catch (error) {
            console.error('Failed to fetch appointments:', error);
            toast.error('Erro ao carregar os compromissos.');
        } finally {
            setIsLoading(false);
        }
    }, [currentDate, view]);

    useEffect(() => { fetchLabels(); }, [fetchLabels]);
    useEffect(() => { fetchAppointments(); }, [fetchAppointments]);

    // Navigation
    const handlePrev = () => {
        if (view === 'week') setCurrentDate(subWeeks(currentDate, 1));
        else setCurrentDate(subMonths(currentDate, 1));
    };
    const handleNext = () => {
        if (view === 'week') setCurrentDate(addWeeks(currentDate, 1));
        else setCurrentDate(addMonths(currentDate, 1));
    };
    const handleToday = () => setCurrentDate(new Date());

    const getNavLabel = () => {
        if (view === 'week') {
            const ws = startOfWeek(currentDate, { weekStartsOn: 1 });
            const we = endOfWeek(currentDate, { weekStartsOn: 1 });
            return `${format(ws, 'dd/MM')} – ${format(we, 'dd/MM/yyyy')}`;
        }
        return format(currentDate, 'MMMM yyyy', { locale: ptBR });
    };

    const handleOpenForm = (date?: Date, appointment?: Appointment) => {
        setPreselectedDate(date);
        setEditingAppointment(appointment);
        setIsFormOpen(true);
    };

    const handleCloseForm = () => {
        setIsFormOpen(false);
        setEditingAppointment(undefined);
        setPreselectedDate(undefined);
    };

    const handleSave = async (dto: CreateAppointmentDto | UpdateAppointmentDto | RecurringAppointmentDto, isRecurring?: boolean) => {
        try {
            if (isRecurring) {
                const result = await agendaService.createRecurring(dto as RecurringAppointmentDto);
                toast.success(`${result.length} compromissos criados na série!`);
            } else if (editingAppointment) {
                await agendaService.update(editingAppointment.id, dto as UpdateAppointmentDto);
                toast.success('Compromisso atualizado!');
            } else {
                await agendaService.create(dto as CreateAppointmentDto);
                toast.success('Compromisso criado!');
            }
            handleCloseForm();
            fetchAppointments();
        } catch (error) {
            console.error('Error saving appointment:', error);
            toast.error('Erro ao salvar compromisso.');
        }
    };

    const handleDeleteRequest = (appt: Appointment) => {
        if (appt.recurrenceGroupId) {
            setDeleteTarget(appt);
            setIsDeleteModalOpen(true);
        } else {
            handleDeleteConfirm(appt, 'one');
        }
    };

    const handleDeleteConfirm = async (appt: Appointment, scope: 'one' | 'forward' | 'all') => {
        setIsDeleteModalOpen(false);
        setDeleteTarget(undefined);
        try {
            await agendaService.delete(appt.id, scope);
            toast.success('Compromisso excluído.');
            handleCloseForm();
            fetchAppointments();
        } catch {
            toast.error('Erro ao excluir compromisso.');
        }
    };

    // Drag and drop handler
    const handleAppointmentDrop = async (id: string, newDateISO: string) => {
        try {
            await agendaService.update(id, { date: newDateISO });
            toast.success('Horário atualizado!');
            fetchAppointments();
        } catch {
            toast.error('Erro ao mover compromisso.');
        }
    };

    // AI batch command
    const handleAiCommand = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!aiCommand.trim() || isAiLoading) return;
        setIsAiLoading(true);
        try {
            const result = await agendaService.aiBatchCommand({
                command: aiCommand,
                appointments: appointments.map(a => ({
                    id: a.id,
                    title: a.title,
                    date: a.date,
                    labelName: a.label?.name,
                })),
            });
            setAiBatchResult(result);
            setIsAiBatchModalOpen(true);
        } catch {
            toast.error('Erro ao processar comando da IA.');
        } finally {
            setIsAiLoading(false);
        }
    };

    const handleApplyBatch = async () => {
        if (!aiBatchResult) return;
        setIsApplyingBatch(true);
        let applied = 0;
        let failed = 0;
        try {
            for (const change of aiBatchResult.changes) {
                try {
                    if (change.delete) {
                        const appt = appointments.find(a => a.id === change.id);
                        await agendaService.delete(change.id, appt?.recurrenceGroupId ? 'one' : 'one');
                    } else {
                        const updateDto: UpdateAppointmentDto = {};
                        if (change.newDate) updateDto.date = change.newDate;
                        if (change.newTitle) updateDto.title = change.newTitle;
                        await agendaService.update(change.id, updateDto);
                    }
                    applied++;
                } catch {
                    failed++;
                }
            }
            setIsAiBatchModalOpen(false);
            setAiBatchResult(null);
            setAiCommand('');
            fetchAppointments();
            if (failed > 0) {
                toast.warning(`${applied} alterações aplicadas, ${failed} falharam.`);
            } else {
                toast.success(`${applied} alteração${applied !== 1 ? 'ões' : ''} aplicada${applied !== 1 ? 's' : ''}!`);
            }
        } finally {
            setIsApplyingBatch(false);
        }
    };

    const getDaysArray = () => {
        // Start from Monday to match WeekGrid
        const start = startOfWeek(startOfMonth(currentDate), { weekStartsOn: 1 });
        const end = endOfWeek(endOfMonth(currentDate), { weekStartsOn: 1 });
        return eachDayOfInterval({ start, end });
    };

    const days = getDaysArray();
    // Week headers starting from Monday
    const weekDaysShort = ['Seg', 'Ter', 'Qua', 'Qui', 'Sex', 'Sáb', 'Dom'];

    const upcomingAppointments = [...appointments]
        .filter(a => parseISO(a.date) >= new Date(new Date().setHours(0, 0, 0, 0)))
        .sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime());

    const getApptBgStyle = (appt: Appointment) => {
        if (appt.label?.color) {
            return { backgroundColor: appt.label.color + '20', borderColor: appt.label.color, color: appt.label.color };
        }
        const fallback: Record<AppointmentType, string> = {
            Medical: 'bg-blue-100 text-blue-800 border-blue-200',
            HomeService: 'bg-orange-100 text-orange-800 border-orange-200',
            Payment: 'bg-red-100 text-red-800 border-red-200',
            Other: 'bg-slate-100 text-slate-800 border-slate-200',
        };
        return fallback[appt.type];
    };

    // Format batch change preview
    const formatBatchChange = (change: AiBatchChange) => {
        const appt = appointments.find(a => a.id === change.id);
        const title = appt?.title ?? change.id.slice(0, 8) + '...';
        if (change.delete) return `🗑️ Excluir: "${title}"`;
        const parts: string[] = [];
        if (change.newDate) {
            const oldTime = appt ? format(parseISO(appt.date), 'dd/MM HH:mm') : '?';
            const newTime = format(parseISO(change.newDate), 'dd/MM HH:mm');
            parts.push(`${oldTime} → ${newTime}`);
        }
        if (change.newTitle) parts.push(`Título: "${change.newTitle}"`);
        return `✏️ "${title}": ${parts.join(', ')}`;
    };

    return (
        <div className="space-y-4">
            {/* Header */}
            <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
                <div className="flex items-center gap-4">
                    <h1 className="text-2xl font-bold text-slate-800">Minha Agenda</h1>
                    <div className="flex bg-slate-100 p-1 rounded-lg">
                        <Button variant={view === 'month' ? 'secondary' : 'ghost'} size="sm" onClick={() => setView('month')} className="h-8 px-3">
                            <CalendarIcon className="w-4 h-4 mr-1.5" /> Mês
                        </Button>
                        <Button variant={view === 'week' ? 'secondary' : 'ghost'} size="sm" onClick={() => setView('week')} className="h-8 px-3">
                            <Clock className="w-4 h-4 mr-1.5" /> Semana
                        </Button>
                        <Button variant={view === 'list' ? 'secondary' : 'ghost'} size="sm" onClick={() => setView('list')} className="h-8 px-3">
                            <ListIcon className="w-4 h-4 mr-1.5" /> Lista
                        </Button>
                    </div>
                </div>

                <div className="flex items-center gap-2 flex-wrap">
                    {(view === 'month' || view === 'week') && (
                        <div className="flex items-center gap-1 bg-white border border-slate-200 rounded-lg p-1">
                            <Button variant="ghost" size="icon" className="h-8 w-8" onClick={handlePrev}>
                                <ChevronLeft className="h-4 w-4" />
                            </Button>
                            <Button variant="ghost" className="h-8 font-medium min-w-[160px] text-sm" onClick={handleToday}>
                                {getNavLabel()}
                            </Button>
                            <Button variant="ghost" size="icon" className="h-8 w-8" onClick={handleNext}>
                                <ChevronRight className="h-4 w-4" />
                            </Button>
                        </div>
                    )}
                    <Button variant="outline" size="sm" className="font-medium" onClick={() => setIsLabelManagerOpen(true)}>
                        <Tag className="w-4 h-4 mr-1.5" /> Labels
                    </Button>
                    <Button variant="outline" size="sm" className="border-purple-200 text-purple-700 bg-purple-50 hover:bg-purple-100 font-medium" onClick={() => setIsImportOpen(true)}>
                        <Sparkles className="w-4 h-4 mr-1.5 text-purple-600" /> IA Extrair
                    </Button>
                    <Button size="sm" onClick={() => handleOpenForm()} className="bg-blue-600 hover:bg-blue-700">
                        <Plus className="w-4 h-4 mr-1.5" /> Novo
                    </Button>
                </div>
            </div>

            {/* AI Batch Command Bar */}
            <form onSubmit={handleAiCommand} className="flex gap-2">
                <div className="relative flex-1">
                    <Sparkles className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-purple-400" />
                    <Input
                        ref={aiInputRef}
                        value={aiCommand}
                        onChange={e => setAiCommand(e.target.value)}
                        placeholder='IA: "Mover todas reuniões KPIT para 11:30" ou "Cancelar meetings de sexta"...'
                        className="pl-9 border-purple-200 focus-visible:ring-purple-400 bg-purple-50/30 placeholder:text-slate-400"
                        disabled={isAiLoading}
                    />
                </div>
                <Button
                    type="submit"
                    variant="outline"
                    size="sm"
                    disabled={!aiCommand.trim() || isAiLoading}
                    className="border-purple-200 text-purple-700 hover:bg-purple-100 px-4"
                >
                    {isAiLoading ? <Loader2 className="w-4 h-4 animate-spin" /> : <Send className="w-4 h-4" />}
                </Button>
            </form>

            {/* Main Area */}
            <div className="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
                {isLoading && appointments.length === 0 ? (
                    <div className="flex justify-center items-center h-64 text-slate-400">
                        <Loader2 className="w-8 h-8 animate-spin" />
                    </div>
                ) : view === 'week' ? (
                    <div style={{ height: '680px' }}>
                        <WeekGrid
                            currentDate={currentDate}
                            appointments={appointments}
                            onSlotClick={d => handleOpenForm(d)}
                            onAppointmentClick={appt => handleOpenForm(undefined, appt)}
                            onAppointmentDrop={handleAppointmentDrop}
                        />
                    </div>
                ) : view === 'month' ? (
                    <div className="flex flex-col h-full">
                        {/* Month view headers — starts Monday */}
                        <div className="grid grid-cols-7 border-b border-slate-200 bg-slate-50/50">
                            {weekDaysShort.map(day => (
                                <div key={day} className="py-3 text-center text-xs font-medium text-slate-500 uppercase tracking-wider">
                                    {day}
                                </div>
                            ))}
                        </div>
                        <div className="grid grid-cols-7 auto-rows-[minmax(120px,auto)]">
                            {days.map((day, dayIdx) => {
                                const dayAppointments = appointments.filter(a => isSameDay(parseISO(a.date), day));
                                const isCurrentMonth = isSameMonth(day, currentDate);

                                return (
                                    <div
                                        key={day.toISOString()}
                                        className={`min-h-[120px] p-2 border-slate-100 relative group transition-colors
                                            ${!isCurrentMonth ? 'bg-slate-50/50 text-slate-400' : 'bg-white'}
                                            ${dayIdx > 6 ? 'border-t' : ''}
                                            ${dayIdx % 7 !== 6 ? 'border-r' : ''}
                                            hover:bg-slate-50 cursor-pointer`}
                                        onClick={(e) => {
                                            if (e.target === e.currentTarget || (e.target as HTMLElement).tagName === 'DIV') {
                                                handleOpenForm(day);
                                            }
                                        }}
                                    >
                                        <div className="flex justify-between items-start mb-1">
                                            <span className={`text-sm font-medium w-7 h-7 flex items-center justify-center rounded-full
                                                ${isToday(day) ? 'bg-blue-600 text-white' : ''}`}>
                                                {format(day, 'd')}
                                            </span>
                                        </div>

                                        <div className="space-y-1 overflow-y-auto max-h-[85px] no-scrollbar">
                                            {dayAppointments.map(appt => {
                                                const labelColor = appt.label?.color;
                                                return (
                                                    <div
                                                        key={appt.id}
                                                        onClick={(e) => {
                                                            e.stopPropagation();
                                                            handleOpenForm(undefined, appt);
                                                        }}
                                                        className={`text-xs px-1.5 py-1 rounded truncate border hover:opacity-80 transition-opacity ${!labelColor ? getApptBgStyle(appt) : ''}`}
                                                        style={labelColor ? { backgroundColor: labelColor + '20', borderColor: labelColor, color: labelColor } : {}}
                                                        title={`${format(parseISO(appt.date), 'HH:mm')} - ${appt.title}`}
                                                    >
                                                        <span className="font-semibold mr-1">{format(parseISO(appt.date), 'HH:mm')}</span>
                                                        {appt.title}
                                                        {appt.recurrenceGroupId && <span className="ml-1 opacity-60">↺</span>}
                                                    </div>
                                                );
                                            })}
                                        </div>
                                    </div>
                                );
                            })}
                        </div>
                    </div>
                ) : (
                    <div className="p-6">
                        <h2 className="text-lg font-semibold text-slate-800 mb-4">Próximos Compromissos</h2>
                        {upcomingAppointments.length === 0 ? (
                            <p className="text-slate-500 text-center py-8">Nenhum compromisso agendado.</p>
                        ) : (
                            <div className="space-y-3">
                                {upcomingAppointments.map(appt => {
                                    const dateObj = parseISO(appt.date);
                                    const labelColor = appt.label?.color;
                                    return (
                                        <div key={appt.id} className="flex flex-col sm:flex-row sm:items-center justify-between p-4 border border-slate-100 bg-slate-50 rounded-lg hover:shadow-sm transition-shadow group">
                                            <div className="flex items-start gap-4">
                                                <div
                                                    className="p-3 rounded-lg flex flex-col items-center justify-center min-w-[70px]"
                                                    style={labelColor
                                                        ? { backgroundColor: labelColor + '20', color: labelColor }
                                                        : { backgroundColor: '#f1f5f9', color: '#475569' }}
                                                >
                                                    <span className="text-xs font-bold uppercase">{format(dateObj, 'MMM', { locale: ptBR })}</span>
                                                    <span className="text-xl font-bold leading-none">{format(dateObj, 'dd')}</span>
                                                </div>
                                                <div>
                                                    <h3 className="font-semibold text-slate-800 flex items-center gap-2">
                                                        {appt.title}
                                                        {appt.recurrenceGroupId && <span className="text-xs text-slate-400 font-normal">↺ recorrente</span>}
                                                    </h3>
                                                    <div className="flex items-center gap-3 text-sm text-slate-500 mt-1">
                                                        <span>{format(dateObj, 'HH:mm')}</span>
                                                        {appt.label ? (
                                                            <span className="px-2 py-0.5 rounded-full text-[10px] uppercase font-semibold" style={{ backgroundColor: appt.label.color + '20', color: appt.label.color }}>
                                                                {appt.label.name}
                                                            </span>
                                                        ) : (
                                                            <span className="px-2 py-0.5 rounded-full text-[10px] uppercase font-semibold border bg-white">{TYPE_LABELS[appt.type]}</span>
                                                        )}
                                                    </div>
                                                    {appt.description && <p className="text-sm text-slate-600 mt-1 line-clamp-2">{appt.description}</p>}
                                                </div>
                                            </div>
                                            <div className="mt-3 sm:mt-0 flex items-center gap-2 opacity-100 sm:opacity-0 group-hover:opacity-100 transition-opacity">
                                                <Button variant="outline" size="sm" onClick={() => handleOpenForm(undefined, appt)}>Editar</Button>
                                                <Button variant="ghost" size="sm" className="text-red-500 hover:text-red-700 hover:bg-red-50" onClick={() => handleDeleteRequest(appt)}>
                                                    <Trash2 className="w-4 h-4" />
                                                </Button>
                                            </div>
                                        </div>
                                    );
                                })}
                            </div>
                        )}
                    </div>
                )}
            </div>

            {/* Appointment Form Dialog */}
            <Dialog open={isFormOpen} onOpenChange={setIsFormOpen}>
                <DialogContent>
                    <DialogHeader>
                        <div className="flex items-center justify-between">
                            <DialogTitle>{editingAppointment ? 'Editar Compromisso' : 'Novo Compromisso'}</DialogTitle>
                            {editingAppointment && (
                                <Button
                                    variant="ghost"
                                    size="sm"
                                    className="text-red-500 hover:text-red-700 hover:bg-red-50 -mr-2"
                                    onClick={() => handleDeleteRequest(editingAppointment)}
                                >
                                    <Trash2 className="w-4 h-4 mr-1" /> Excluir
                                </Button>
                            )}
                        </div>
                    </DialogHeader>
                    {isFormOpen && (
                        <AppointmentForm
                            initialData={editingAppointment}
                            preselectedDate={preselectedDate}
                            labels={labels}
                            onSubmit={handleSave}
                            onCancel={handleCloseForm}
                        />
                    )}
                </DialogContent>
            </Dialog>

            {/* Recurring Delete Modal */}
            <Dialog open={isDeleteModalOpen} onOpenChange={setIsDeleteModalOpen}>
                <DialogContent className="sm:max-w-[400px]">
                    <DialogHeader>
                        <DialogTitle>Excluir compromisso recorrente</DialogTitle>
                    </DialogHeader>
                    <p className="text-sm text-slate-600 mt-2">
                        Este compromisso faz parte de uma série. O que deseja excluir?
                    </p>
                    <div className="flex flex-col gap-2 mt-4">
                        <Button variant="outline" onClick={() => deleteTarget && handleDeleteConfirm(deleteTarget, 'one')}>
                            Só este compromisso
                        </Button>
                        <Button variant="outline" onClick={() => deleteTarget && handleDeleteConfirm(deleteTarget, 'forward')}>
                            Este e os seguintes da série
                        </Button>
                        <Button variant="destructive" onClick={() => deleteTarget && handleDeleteConfirm(deleteTarget, 'all')}>
                            Toda a série
                        </Button>
                        <Button variant="ghost" onClick={() => setIsDeleteModalOpen(false)}>Cancelar</Button>
                    </div>
                </DialogContent>
            </Dialog>

            {/* AI Batch Preview Modal */}
            <Dialog open={isAiBatchModalOpen} onOpenChange={setIsAiBatchModalOpen}>
                <DialogContent className="sm:max-w-[500px]">
                    <DialogHeader>
                        <DialogTitle className="flex items-center gap-2">
                            <Sparkles className="w-5 h-5 text-purple-600" /> Alterações propostas pela IA
                        </DialogTitle>
                    </DialogHeader>
                    {aiBatchResult && (
                        <div className="space-y-4 mt-2">
                            <p className="text-sm font-medium text-slate-700 bg-purple-50 border border-purple-100 rounded-lg px-3 py-2">
                                {aiBatchResult.summary}
                            </p>
                            {aiBatchResult.changes.length === 0 ? (
                                <p className="text-sm text-slate-500 text-center py-4">Nenhuma alteração encontrada.</p>
                            ) : (
                                <div className="space-y-1.5 max-h-64 overflow-y-auto">
                                    {aiBatchResult.changes.map((change, i) => (
                                        <div key={i} className="text-sm text-slate-600 bg-slate-50 rounded px-3 py-1.5 border border-slate-100">
                                            {formatBatchChange(change)}
                                        </div>
                                    ))}
                                </div>
                            )}
                            <div className="flex justify-end gap-2 pt-2">
                                <Button variant="ghost" onClick={() => setIsAiBatchModalOpen(false)}>
                                    Cancelar
                                </Button>
                                <Button
                                    onClick={handleApplyBatch}
                                    disabled={isApplyingBatch || aiBatchResult.changes.length === 0}
                                    className="bg-purple-600 hover:bg-purple-700"
                                >
                                    {isApplyingBatch ? (
                                        <><Loader2 className="w-4 h-4 mr-1.5 animate-spin" /> Aplicando...</>
                                    ) : (
                                        <><Check className="w-4 h-4 mr-1.5" /> Aplicar {aiBatchResult.changes.length} alteração{aiBatchResult.changes.length !== 1 ? 'ões' : ''}</>
                                    )}
                                </Button>
                            </div>
                        </div>
                    )}
                </DialogContent>
            </Dialog>

            <ImportTextDialog
                isOpen={isImportOpen}
                onOpenChange={setIsImportOpen}
                onSuccess={fetchAppointments}
            />

            <LabelManagerModal
                isOpen={isLabelManagerOpen}
                onOpenChange={setIsLabelManagerOpen}
                onLabelsChanged={fetchLabels}
            />
        </div>
    );
}
