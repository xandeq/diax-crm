'use client';

import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { agendaService } from '@/services/agenda';
import { Appointment, AppointmentType, CreateAppointmentDto, UpdateAppointmentDto } from '@/types/agenda';
import { addMonths, eachDayOfInterval, endOfMonth, endOfWeek, format, isSameDay, isSameMonth, isToday, parseISO, startOfMonth, startOfWeek, subMonths } from 'date-fns';
import { ptBR } from 'date-fns/locale';
import { Calendar as CalendarIcon, ChevronLeft, ChevronRight, List as ListIcon, Loader2, Plus, Trash2 } from 'lucide-react';
import { useCallback, useEffect, useState } from 'react';
import { toast } from 'sonner';
import { AppointmentForm } from './AppointmentForm';

const TYPE_COLORS: Record<AppointmentType, string> = {
  Medical: 'bg-blue-100 text-blue-800 border-blue-200',
  HomeService: 'bg-orange-100 text-orange-800 border-orange-200',
  Payment: 'bg-red-100 text-red-800 border-red-200',
  Other: 'bg-slate-100 text-slate-800 border-slate-200'
};

const TYPE_LABELS: Record<AppointmentType, string> = {
  Medical: 'Médico',
  HomeService: 'Serviço',
  Payment: 'Pagamento',
  Other: 'Outro'
};

export function AgendaClient() {
  const [currentDate, setCurrentDate] = useState(new Date());
  const [appointments, setAppointments] = useState<Appointment[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [view, setView] = useState<'calendar' | 'list'>('calendar');

  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingAppointment, setEditingAppointment] = useState<Appointment | undefined>();
  const [preselectedDate, setPreselectedDate] = useState<Date | undefined>();

  const fetchAppointments = useCallback(async () => {
    setIsLoading(true);
    try {
      const start = startOfWeek(startOfMonth(currentDate));
      const end = endOfWeek(endOfMonth(currentDate));

      const data = await agendaService.getByDateRange(start.toISOString(), end.toISOString());
      setAppointments(data);
    } catch (error) {
      console.error('Failed to fetch appointments:', error);
      toast.error('Erro ao carregar os compromissos.');
    } finally {
      setIsLoading(false);
    }
  }, [currentDate]);

  useEffect(() => {
    fetchAppointments();
  }, [fetchAppointments]);

  const handlePreviousMonth = () => setCurrentDate(subMonths(currentDate, 1));
  const handleNextMonth = () => setCurrentDate(addMonths(currentDate, 1));
  const handleToday = () => setCurrentDate(new Date());

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

  const handleSave = async (dto: any) => {
    try {
      if (editingAppointment) {
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

  const handleDelete = async (id: string) => {
    if (!confirm('Tem certeza que deseja excluir este compromisso?')) return;
    try {
      await agendaService.delete(id);
      toast.success('Compromisso excluído.');
      fetchAppointments();
    } catch (error) {
      console.error('Error deleting appointment:', error);
      toast.error('Erro ao excluir compromisso.');
    }
  };

  const getDaysArray = () => {
    const start = startOfWeek(startOfMonth(currentDate));
    const end = endOfWeek(endOfMonth(currentDate));
    return eachDayOfInterval({ start, end });
  };

  const days = getDaysArray();
  const weekDaysShort = ['Dom', 'Seg', 'Ter', 'Qua', 'Qui', 'Sex', 'Sáb'];

  // For list view: only upcoming in the current month bounds (or just all loaded)
  const upcomingAppointments = [...appointments]
    .filter(a => parseISO(a.date) >= new Date(new Date().setHours(0,0,0,0)))
    .sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime());

  return (
    <div className="space-y-6">
      {/* Header controls */}
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div className="flex items-center gap-4">
          <h1 className="text-2xl font-bold text-slate-800">Minha Agenda</h1>
          <div className="flex bg-slate-100 p-1 rounded-lg">
            <Button
              variant={view === 'calendar' ? 'secondary' : 'ghost'}
              size="sm"
              onClick={() => setView('calendar')}
              className="h-8 px-3"
            >
              <CalendarIcon className="w-4 h-4 mr-2" />
              Mês
            </Button>
            <Button
              variant={view === 'list' ? 'secondary' : 'ghost'}
              size="sm"
              onClick={() => setView('list')}
              className="h-8 px-3"
            >
              <ListIcon className="w-4 h-4 mr-2" />
              Lista
            </Button>
          </div>
        </div>

        <div className="flex items-center gap-2">
          {view === 'calendar' && (
            <div className="flex items-center gap-1 bg-white border border-slate-200 rounded-lg p-1 mr-2">
              <Button variant="ghost" size="icon" className="h-8 w-8" onClick={handlePreviousMonth}>
                <ChevronLeft className="h-4 w-4" />
              </Button>
              <Button variant="ghost" className="h-8 font-medium min-w-[120px]" onClick={handleToday}>
                {format(currentDate, 'MMMM yyyy', { locale: ptBR })}
              </Button>
              <Button variant="ghost" size="icon" className="h-8 w-8" onClick={handleNextMonth}>
                <ChevronRight className="h-4 w-4" />
              </Button>
            </div>
          )}
          <Button onClick={() => handleOpenForm()} className="bg-blue-600 hover:bg-blue-700">
            <Plus className="w-4 h-4 mr-2" />
            Novo Compromisso
          </Button>
        </div>
      </div>

      {/* Main Content Area */}
      <div className="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
        {isLoading && appointments.length === 0 ? (
          <div className="flex justify-center items-center h-64 text-slate-400">
            <Loader2 className="w-8 h-8 animate-spin" />
          </div>
        ) : view === 'calendar' ? (
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
                        ${isToday(day) ? 'bg-blue-600 text-white' : ''}
                      `}>
                        {format(day, 'd')}
                      </span>
                    </div>

                    <div className="space-y-1 overflow-y-auto max-h-[85px] no-scrollbar">
                      {dayAppointments.map(appt => (
                        <div
                          key={appt.id}
                          onClick={(e) => {
                            e.stopPropagation();
                            handleOpenForm(undefined, appt);
                          }}
                          className={`text-xs px-1.5 py-1 rounded truncate border ${TYPE_COLORS[appt.type]} hover:opacity-80 transition-opacity`}
                          title={`${format(parseISO(appt.date), 'HH:mm')} - ${appt.title}`}
                        >
                          <span className="font-semibold mr-1">{format(parseISO(appt.date), 'HH:mm')}</span>
                          {appt.title}
                        </div>
                      ))}
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
              <p className="text-slate-500 text-center py-8">Nenhum compromisso agendado para o futuro.</p>
            ) : (
              <div className="space-y-4">
                {upcomingAppointments.map(appt => {
                  const dateObj = parseISO(appt.date);
                  return (
                    <div key={appt.id} className="flex flex-col sm:flex-row sm:items-center justify-between p-4 border border-slate-100 bg-slate-50 rounded-lg hover:shadow-sm transition-shadow group">
                      <div className="flex items-start gap-4">
                        <div className={`p-3 rounded-lg flex flex-col items-center justify-center min-w-[70px] ${TYPE_COLORS[appt.type]}`}>
                          <span className="text-xs font-bold uppercase">{format(dateObj, 'MMM', { locale: ptBR })}</span>
                          <span className="text-xl font-bold leading-none">{format(dateObj, 'dd')}</span>
                        </div>
                        <div>
                          <h3 className="font-semibold text-slate-800">{appt.title}</h3>
                          <div className="flex items-center gap-3 text-sm text-slate-500 mt-1">
                            <span className="flex items-center gap-1">
                              <span className="w-2 h-2 rounded-full border" style={{ backgroundColor: 'currentColor' }}></span>
                              {format(dateObj, 'HH:mm')}
                            </span>
                            <span className="px-2 py-0.5 rounded-full text-[10px] uppercase font-semibold border bg-white">
                              {TYPE_LABELS[appt.type]}
                            </span>
                          </div>
                          {appt.description && (
                            <p className="text-sm text-slate-600 mt-2 line-clamp-2">{appt.description}</p>
                          )}
                        </div>
                      </div>
                      <div className="mt-4 sm:mt-0 flex items-center justify-end gap-2 opacity-100 sm:opacity-0 group-hover:opacity-100 transition-opacity">
                        <Button variant="outline" size="sm" onClick={() => handleOpenForm(undefined, appt)}>
                          Editar
                        </Button>
                        <Button variant="ghost" size="sm" className="text-red-500 hover:text-red-700 hover:bg-red-50" onClick={() => handleDelete(appt.id)}>
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

      <Dialog open={isFormOpen} onOpenChange={setIsFormOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{editingAppointment ? 'Editar Compromisso' : 'Novo Compromisso'}</DialogTitle>
          </DialogHeader>
          {isFormOpen && (
            <AppointmentForm
              initialData={editingAppointment}
              preselectedDate={preselectedDate}
              onSubmit={handleSave}
              onCancel={handleCloseForm}
            />
          )}
        </DialogContent>
      </Dialog>
    </div>
  );
}
