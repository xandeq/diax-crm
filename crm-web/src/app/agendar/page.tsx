'use client';

import {
  bookMeeting,
  getAvailability,
  type AvailabilityDay,
} from '@/services/meetings';
import { AlertCircle, Calendar, CheckCircle2, Clock, Loader2 } from 'lucide-react';
import { useSearchParams } from 'next/navigation';
import { Suspense, useEffect, useState } from 'react';

/**
 * Página PÚBLICA de agendamento — o lead escolhe um horário livre e reserva.
 * Rota estática (/agendar?u=USER_ID) porque o deploy usa output: export.
 */
export default function PublicBookingPage() {
  return (
    <Suspense fallback={
      <div className="min-h-screen bg-[#0c0c0f] flex items-center justify-center">
        <Loader2 className="h-8 w-8 text-violet-400 animate-spin" />
      </div>
    }>
      <BookingContent />
    </Suspense>
  );
}

const WEEKDAY = ['Dom', 'Seg', 'Ter', 'Qua', 'Qui', 'Sex', 'Sáb'];

function slotLabelBrt(iso: string): string {
  // Slot vem em UTC; exibe em BRT (UTC-3 fixo)
  const d = new Date(new Date(iso).getTime() - 3 * 3600 * 1000);
  return `${String(d.getUTCHours()).padStart(2, '0')}:${String(d.getUTCMinutes()).padStart(2, '0')}`;
}

function dayLabel(date: string): { weekday: string; day: string } {
  const d = new Date(date + 'T12:00:00Z');
  return {
    weekday: WEEKDAY[d.getUTCDay()],
    day: `${String(d.getUTCDate()).padStart(2, '0')}/${String(d.getUTCMonth() + 1).padStart(2, '0')}`,
  };
}

function BookingContent() {
  const searchParams = useSearchParams();
  const userId = searchParams?.get('u') ?? '';

  const [days, setDays] = useState<AvailabilityDay[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedSlot, setSelectedSlot] = useState<string | null>(null);
  const [name, setName] = useState('');
  const [email, setEmail] = useState('');
  const [phone, setPhone] = useState('');
  const [notes, setNotes] = useState('');
  const [isBooking, setIsBooking] = useState(false);
  const [confirmed, setConfirmed] = useState<string | null>(null);

  const loadAvailability = () => {
    setIsLoading(true);
    getAvailability(userId, 10)
      .then(setDays)
      .catch(() => setError('Agenda não encontrada ou link inválido.'))
      .finally(() => setIsLoading(false));
  };

  useEffect(() => {
    if (!userId) {
      setError('Link de agendamento inválido.');
      setIsLoading(false);
      return;
    }
    loadAvailability();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [userId]);

  const submit = async () => {
    if (!selectedSlot) return;
    if (!name.trim() || !email.trim() || !email.includes('@')) {
      setError('Preencha nome e um email válido.');
      return;
    }
    setIsBooking(true);
    setError(null);
    try {
      await bookMeeting({
        userId,
        scheduledAt: selectedSlot,
        name: name.trim(),
        email: email.trim(),
        phone: phone.trim() || null,
        notes: notes.trim() || null,
      });
      setConfirmed(selectedSlot);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Não foi possível reservar — o horário pode ter sido ocupado.');
      setSelectedSlot(null);
      loadAvailability(); // atualiza slots (o escolhido pode ter sido tomado)
    } finally {
      setIsBooking(false);
    }
  };

  if (isLoading) {
    return (
      <div className="min-h-screen bg-[#0c0c0f] flex items-center justify-center">
        <Loader2 className="h-8 w-8 text-violet-400 animate-spin" />
      </div>
    );
  }

  if (confirmed) {
    const d = new Date(confirmed);
    const brtDate = new Date(d.getTime() - 3 * 3600 * 1000);
    return (
      <div className="min-h-screen bg-[#0c0c0f] flex flex-col items-center justify-center gap-4 px-6 text-center">
        <CheckCircle2 className="h-14 w-14 text-emerald-400" />
        <h1 className="text-xl font-bold text-white">Reunião confirmada! 🎉</h1>
        <p className="text-white/70 text-sm max-w-md">
          {brtDate.getUTCDate()}/{String(brtDate.getUTCMonth() + 1).padStart(2, '0')} às {slotLabelBrt(confirmed)} (horário de Brasília).
          <br />Você receberá a confirmação em <strong>{email}</strong>. Até lá!
        </p>
        <p className="text-[11px] text-white/25 mt-6">Alexandre Queiroz · Marketing Digital &amp; Desenvolvimento</p>
      </div>
    );
  }

  if (error && days.length === 0) {
    return (
      <div className="min-h-screen bg-[#0c0c0f] flex flex-col items-center justify-center gap-3 px-6 text-center">
        <AlertCircle className="h-10 w-10 text-red-400" />
        <p className="text-white/70">{error}</p>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-[#0c0c0f] text-white">
      <div className="max-w-3xl mx-auto px-6 py-12">
        <div className="flex items-center gap-3 mb-2">
          <div className="h-10 w-10 rounded-xl bg-gradient-to-br from-violet-500 to-indigo-600 flex items-center justify-center">
            <Calendar className="h-5 w-5 text-white" />
          </div>
          <div>
            <h1 className="text-lg font-bold">Agendar reunião</h1>
            <p className="text-[12px] text-white/40 flex items-center gap-1">
              <Clock className="h-3 w-3" /> 30 minutos · online · horário de Brasília
            </p>
          </div>
        </div>

        {error && (
          <div className="my-4 flex items-center gap-2 rounded-xl bg-red-500/10 border border-red-500/25 px-4 py-3 text-red-300 text-sm">
            <AlertCircle className="h-4 w-4 shrink-0" /> {error}
          </div>
        )}

        {/* Grade de dias/slots */}
        <div className="mt-6 flex gap-3 overflow-x-auto pb-3">
          {days.map(day => {
            const { weekday, day: dm } = dayLabel(day.date);
            return (
              <div key={day.date} className="min-w-[104px] shrink-0">
                <div className="text-center mb-2">
                  <p className="text-[11px] uppercase tracking-wider text-white/40">{weekday}</p>
                  <p className="text-sm font-semibold text-white/85">{dm}</p>
                </div>
                <div className="space-y-1.5 max-h-[320px] overflow-y-auto pr-1">
                  {day.slots.map(slot => (
                    <button
                      key={slot}
                      type="button"
                      onClick={() => setSelectedSlot(slot)}
                      className={`w-full py-1.5 rounded-lg text-[12px] font-medium border transition-all
                        ${selectedSlot === slot
                          ? 'bg-violet-600 border-violet-500 text-white'
                          : 'bg-white/[0.04] border-white/10 text-white/60 hover:bg-violet-600/20 hover:border-violet-500/40 hover:text-violet-200'}`}
                    >
                      {slotLabelBrt(slot)}
                    </button>
                  ))}
                </div>
              </div>
            );
          })}
        </div>

        {/* Formulário */}
        {selectedSlot && (
          <div className="mt-6 rounded-2xl bg-white/[0.03] border border-white/10 p-6 space-y-4">
            <p className="text-sm text-violet-300 font-medium">
              📅 {slotLabelBrt(selectedSlot)} de {dayLabel((new Date(new Date(selectedSlot).getTime() - 3 * 3600 * 1000)).toISOString().slice(0, 10)).day} — preencha seus dados:
            </p>
            <div className="grid sm:grid-cols-2 gap-3">
              <input value={name} onChange={e => setName(e.target.value)} placeholder="Seu nome *"
                className="rounded-xl bg-black/40 border border-white/10 px-3 py-2.5 text-sm text-white outline-none focus:border-violet-500/50" />
              <input value={email} onChange={e => setEmail(e.target.value)} placeholder="Seu email *" type="email"
                className="rounded-xl bg-black/40 border border-white/10 px-3 py-2.5 text-sm text-white outline-none focus:border-violet-500/50" />
              <input value={phone} onChange={e => setPhone(e.target.value)} placeholder="WhatsApp (opcional)"
                className="rounded-xl bg-black/40 border border-white/10 px-3 py-2.5 text-sm text-white outline-none focus:border-violet-500/50" />
              <input value={notes} onChange={e => setNotes(e.target.value)} placeholder="Assunto (opcional)"
                className="rounded-xl bg-black/40 border border-white/10 px-3 py-2.5 text-sm text-white outline-none focus:border-violet-500/50" />
            </div>
            <button
              type="button"
              onClick={submit}
              disabled={isBooking}
              className="w-full flex items-center justify-center gap-2 px-4 py-3 rounded-xl bg-violet-600 hover:bg-violet-500 text-white text-sm font-semibold transition-all disabled:opacity-60"
            >
              {isBooking ? <Loader2 className="h-4 w-4 animate-spin" /> : <CheckCircle2 className="h-4 w-4" />}
              Confirmar reunião
            </button>
          </div>
        )}

        <p className="text-center text-[11px] text-white/25 mt-10">
          Alexandre Queiroz · Marketing Digital &amp; Desenvolvimento — alexandrequeiroz.com.br
        </p>
      </div>
    </div>
  );
}
