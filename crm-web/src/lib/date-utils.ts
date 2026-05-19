import { format, parseISO } from 'date-fns';
import { ptBR } from 'date-fns/locale';

export function formatDateShort(dateStr: string | null): string {
  if (!dateStr) return '–';
  try {
    return format(parseISO(dateStr), "dd/MM/yyyy 'às' HH:mm", { locale: ptBR });
  } catch {
    return dateStr;
  }
}

/**
 * Formata uma string de data ISO para exibição local sem deslocamento de fuso horário.
 * @param dateString String de data (ex: 2026-01-02T00:00:00Z)
 * @returns Data formatada (ex: 02/01/2026)
 */
export function formatDisplayDate(dateString: string): string {
    if (!dateString) return '-';
    // Remove o Z para tratar como data local e evitar deslocamento
    const pureDate = dateString.split('T')[0];
    const [year, month, day] = pureDate.split('-');
    return `${day}/${month}/${year}`;
}

/**
 * Formata uma string de data ISO para exibição (alias para formatDisplayDate)
 * @param dateString String de data (ex: 2026-01-02T00:00:00Z)
 * @returns Data formatada (ex: 02/01/2026)
 */
export function formatDate(dateString: string): string {
    return formatDisplayDate(dateString);
}

/**
 * Converte uma data para o formato aceito pelo input type="date" (yyyy-MM-dd)
 * de forma segura em relação ao fuso horário.
 */
export function dateToInputString(date: Date | string): string {
    const d = typeof date === 'string' ? new Date(date) : date;
    
    // Se a string for apenas data (yyyy-MM-dd), o Date(string) em alguns browsers
    // pode tratar como UTC. Para garantir consistência no input:
    if (typeof date === 'string' && date.length <= 10) {
        return date;
    }

    // Para objetos Date ou ISO strings, pegamos os componentes locais para o input
    // Mas se for ISO string do banco (UTC), queremos a data que está lá.
    if (typeof date === 'string' && date.includes('T')) {
        return date.split('T')[0];
    }

    const year = d.getFullYear();
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
}

/**
 * Retorna o dia efetivo de pagamento, antecipando para a sexta-feira anterior
 * quando o dia original cai em sábado (shift=1) ou domingo (shift=2).
 * Se a antecipação cruzar para o mês anterior, mantém o dia original —
 * o pagamento real cai no mês anterior, mas no planner do mês atual
 * a entrada aparece no primeiro bucket, não no último.
 *
 * @param dayOfMonth Dia nominal do pagamento (1-31)
 * @param year       Ano do mês de referência
 * @param month      Mês de referência (1=Jan, 12=Dez)
 */
export function getEffectivePayDay(
  dayOfMonth: number,
  year: number,
  month: number
): { effectiveDay: number; adjusted: boolean; label: string } {
  const safeDay = Math.min(dayOfMonth, new Date(year, month, 0).getDate());
  const date = new Date(year, month - 1, safeDay);
  const dow = date.getDay(); // 0=Dom, 6=Sáb
  const shift = dow === 6 ? 1 : dow === 0 ? 2 : 0;

  if (shift > 0) {
    const fri = new Date(date);
    fri.setDate(safeDay - shift);
    if (fri.getMonth() !== date.getMonth()) {
      return { effectiveDay: safeDay, adjusted: false, label: `Dia ${safeDay}` };
    }
    return {
      effectiveDay: fri.getDate(),
      adjusted: true,
      label: `Dia ${fri.getDate()} (sex, adj. do ${safeDay})`,
    };
  }

  return { effectiveDay: safeDay, adjusted: false, label: `Dia ${safeDay}` };
}

/**
 * Obtém a data de hoje no formato yyyy-MM-dd (local)
 */
export function getTodayInputString(): string {
    const d = new Date();
    const year = d.getFullYear();
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
}

/**
 * Formata uma data em tempo relativo (ex: "há 2 dias", "há 1 hora")
 * @param dateString String de data ISO (ex: 2026-01-02T00:00:00Z)
 * @returns Tempo relativo em português (ex: "há 2 dias", "hoje", "há 1 hora")
 */
export function formatRelativeTime(dateString: string | null | undefined): string {
    if (!dateString) return 'Nunca';

    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();

    if (diffMs < 0) return 'No futuro';

    const diffMinutes = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);
    const diffWeeks = Math.floor(diffMs / 604800000);
    const diffMonths = Math.floor(diffMs / 2592000000); // 30 dias

    if (diffMinutes < 1) return 'Agora';
    if (diffMinutes < 60) return `há ${diffMinutes}min`;
    if (diffHours < 24) return `há ${diffHours}h`;
    if (diffDays === 0) return 'Hoje';
    if (diffDays === 1) return 'Ontem';
    if (diffDays < 7) return `há ${diffDays}d`;
    if (diffWeeks < 4) return `há ${diffWeeks}sem`;
    if (diffMonths < 12) return `há ${diffMonths}mês`;

    const years = Math.floor(diffMonths / 12);
    return `há ${years}ano`;
}
