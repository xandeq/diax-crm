'use client';

import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { agendaService } from '@/services/agenda';
import { appointmentLabelsService } from '@/services/appointmentLabels';
import { Appointment, AppointmentLabel, AppointmentType, CreateAppointmentDto, RecurringAppointmentDto, UpdateAppointmentDto } from '@/types/agenda';
import { addDays, addMonths, addWeeks, eachDayOfInterval, endOfMonth, endOfWeek, format, isSameDay, isSameMonth, isToday, parseISO, startOfMonth, startOfWeek, subMonths, subWeeks } from 'date-fns';
import { ptBR } from 'date-fns/locale';
import { Calendar as CalendarIcon, Check, ChevronLeft, ChevronRight, Clock, Copy, List as ListIcon, Loader2, Plus, Sparkles, Tag, Trash2 } from 'lucide-react';
import { useCallback, useEffect, useState } from 'react';
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

    const [copied, setCopied] = useState(false);
    const [isFormOpen, setIsFormOpen] = useState(false);
    const [isImportOpen, setIsImportOpen] = useState(false);
    const [isLabelManagerOpen, setIsLabelManagerOpen] = useState(false);
    const [editingAppointment, setEditingAppointment] = useState<Appointment | undefined>();
    const [preselectedDate, setPreselectedDate] = useState<Date | undefined>();

    // Recurring delete modal
    const [deleteTarget, setDeleteTarget] = useState<Appointment | undefined>();
    const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);

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
                start = startOfWeek(startOfMonth(currentDate));
                end = endOfWeek(endOfMonth(currentDate));
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

    const handleCopyWeek = async () => {
        const weekStart = startOfWeek(currentDate, { weekStartsOn: 1 });
        const weekEnd = endOfWeek(currentDate, { weekStartsOn: 1 });

        const weekAppts = appointments
            .filter(a => {
                const d = parseISO(a.date);
                return d >= weekStart && d <= weekEnd;
            })
            .sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime());

        const dayNames = ['Dom', 'Seg', 'Ter', 'Qua', 'Qui', 'Sex', 'Sáb'];
        const lines: string[] = [`Agenda da Semana (${format(weekStart, 'dd/MM')} - ${format(weekEnd, 'dd/MM')})`];

        for (let i = 0; i < 7; i++) {
            const day = addDays(weekStart, i);
            const dayAppts = weekAppts.filter(a => isSameDay(parseISO(a.date), day));
            if (!dayAppts.length) continue;
            lines.push('');
            lines.push(`${dayNames[day.getDay()]} ${format(day, 'dd/MM')}`);
            for (const appt of dayAppts) {
                const labelStr = appt.label ? ` [${appt.label.name}]` : '';
                lines.push(`  ${format(parseISO(appt.date), 'HH:mm')} - ${appt.title}${labelStr}`);
                if (appt.description) lines.push(`    ${appt.description}`);
            }
        }

        if (!weekAppts.length) lines.push('', 'Nenhum compromisso nesta semana.');

        try {
            await navigator.clipboard.writeText(lines.join('\n'));
            setCopied(true);
            toast.success('Agenda da semana copiada!');
            setTimeout(() => setCopied(false), 2000);
        } catch {
            toast.error('Erro ao copiar.');
        }
    };

    const getDaysArray = () => {
        const start = startOfWeek(startOfMonth(currentDate));
        const end = endOfWeek(endOfMonth(currentDate));
        return eachDayOfInterval({ start, end });
    };

    const days = getDaysArray();
    const weekDaysShort = ['Dom', 'Seg', 'Ter', 'Qua', 'Qui', 'Sex', 'Sáb'];

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

    return (
        <div className="space-y-6">
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
                    <Button variant="outline" size="sm" className="font-medium" onClick={handleCopyWeek}>
                        {copied ? <Check className="w-4 h-4 mr-1.5 text-green-600" /> : <Copy className="w-4 h-4 mr-1.5" />}
                        {copied ? 'Copiado!' : 'Copiar Semana'}
                    </Button>
                    <Button variant="outline" size="sm" className="border-purple-200 text-purple-700 bg-purple-50 hover:bg-purple-100 font-medium" onClick={() => setIsImportOpen(true)}>
                        <Sparkles className="w-4 h-4 mr-1.5 text-purple-600" /> IA Extrair
                    </Button>
                    <Button size="sm" onClick={() => handleOpenForm()} className="bg-blue-600 hover:bg-blue-700">
                        <Plus className="w-4 h-4 mr-1.5" /> Novo
                    </Button>
                </div>
            </div>

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
                        />
                    </div>
                ) : view === 'month' ? (
                    <div className="flex flex-col h-full">
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
