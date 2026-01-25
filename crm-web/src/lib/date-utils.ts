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
 * Obtém a data de hoje no formato yyyy-MM-dd (local)
 */
export function getTodayInputString(): string {
    const d = new Date();
    const year = d.getFullYear();
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
}
