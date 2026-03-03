'use client';

import { format, parseISO } from 'date-fns';
import { ptBR } from 'date-fns/locale';
import { Calendar, Loader2 } from 'lucide-react';
import Link from 'next/link';
import { useEffect, useState } from 'react';

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { agendaService } from '@/services/agenda';
import { Appointment, AppointmentType } from '@/types/agenda';

const TYPE_COLORS: Record<AppointmentType, string> = {
  Medical: 'bg-blue-100 text-blue-800',
  HomeService: 'bg-orange-100 text-orange-800',
  Payment: 'bg-red-100 text-red-800',
  Other: 'bg-slate-100 text-slate-800'
};

const TYPE_LABELS: Record<AppointmentType, string> = {
  Medical: 'Médico',
  HomeService: 'Serviço',
  Payment: 'Pagamento',
  Other: 'Outro'
};

export function UpcomingAgendaWidget() {
  const [appointments, setAppointments] = useState<Appointment[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    let mounted = true;
    const fetchAgenda = async () => {
      try {
        const today = new Date();
        today.setHours(0, 0, 0, 0);

        // Fetch up to 30 days ahead to ensure we get something
        const endDay = new Date(today);
        endDay.setDate(endDay.getDate() + 30);

        const data = await agendaService.getByDateRange(today.toISOString(), endDay.toISOString());

        if (mounted) {
          // Filter only upcoming from right now, sort by nearest
          const upcoming = data
            .filter(a => parseISO(a.date) >= new Date()) // only future items
            .sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime())
            .slice(0, 4); // show max 4

          setAppointments(upcoming);
        }
      } catch (error) {
        console.error('Failed to fetch dashboard agenda:', error);
      } finally {
        if (mounted) setIsLoading(false);
      }
    };

    fetchAgenda();
    return () => { mounted = false; };
  }, []);

  return (
    <Card className="col-span-1 md:col-span-2 flex flex-col h-full hover:shadow-md transition-shadow">
      <CardHeader className="pb-3 border-b border-slate-100">
        <div className="flex justify-between items-center">
          <CardTitle className="text-xl font-bold flex items-center gap-2">
            <Calendar className="w-5 h-5 text-blue-600" />
            Agenda: Próximos
          </CardTitle>
          <Link href="/agenda" className="text-sm font-medium text-blue-600 hover:text-blue-800 hover:underline">
            Ver Todos →
          </Link>
        </div>
        <CardDescription>
          Seus próximos compromissos e obrigações
        </CardDescription>
      </CardHeader>
      <CardContent className="flex-1 pt-4">
        {isLoading ? (
          <div className="flex h-32 items-center justify-center text-slate-400">
            <Loader2 className="h-6 w-6 animate-spin" />
          </div>
        ) : appointments.length === 0 ? (
          <div className="flex flex-col h-32 items-center justify-center text-slate-400 gap-2">
            <Calendar className="w-8 h-8 opacity-20" />
            <p className="text-sm">Nenhum compromisso futuro agendado.</p>
            <Link href="/agenda" className="text-sm text-blue-600 underline">Criar um agora</Link>
          </div>
        ) : (
          <div className="space-y-4">
            {appointments.map((appt) => {
              const dateObj = parseISO(appt.date);
              return (
                <div key={appt.id} className="flex flex-col sm:flex-row sm:items-center justify-between p-3 border border-slate-100 bg-slate-50/50 rounded-lg group">
                  <div className="flex items-start gap-4">
                    <div className="min-w-[60px] text-center border-r pr-4">
                      <div className="text-sm font-bold text-slate-900 uppercase">
                        {format(dateObj, 'MMM', { locale: ptBR })}
                      </div>
                      <div className="text-2xl font-bold leading-none text-slate-700">
                        {format(dateObj, 'dd')}
                      </div>
                    </div>
                    <div>
                      <h3 className="font-semibold text-slate-800 line-clamp-1">{appt.title}</h3>
                      <div className="flex items-center gap-2 text-sm text-slate-500 mt-1">
                        <span className="font-medium whitespace-nowrap">
                          {format(dateObj, 'HH:mm')}
                        </span>
                        <span className={`px-2 py-0.5 rounded-full text-[10px] uppercase font-semibold ${TYPE_COLORS[appt.type]}`}>
                          {TYPE_LABELS[appt.type]}
                        </span>
                      </div>
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </CardContent>
    </Card>
  );
}
