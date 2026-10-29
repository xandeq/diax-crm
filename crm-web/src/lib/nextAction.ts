// Próxima ação de um lead no pipeline: a tarefa aberta de vencimento mais próximo.

export type NextActionTone = 'overdue' | 'today' | 'soon' | 'later' | 'none';

export interface NextActionView {
  tone: NextActionTone;
  label: string;
}

const DAY_MS = 86_400_000;

function startOfDay(d: Date): number {
  return new Date(d.getFullYear(), d.getMonth(), d.getDate()).getTime();
}

/** Resume a próxima ação para o card: tom (cor) e texto curto. `now` injetável para teste. */
export function nextActionView(
  title?: string | null,
  due?: string | null,
  now: Date = new Date(),
): NextActionView {
  if (!title) return { tone: 'none', label: 'Sem próxima ação' };
  if (!due) return { tone: 'later', label: title };
  const diff = Math.round((startOfDay(new Date(due)) - startOfDay(now)) / DAY_MS);
  if (diff < 0) return { tone: 'overdue', label: `${title} · atrasada ${-diff}d` };
  if (diff === 0) return { tone: 'today', label: `${title} · hoje` };
  if (diff <= 2) return { tone: 'soon', label: `${title} · em ${diff}d` };
  return { tone: 'later', label: `${title} · em ${diff}d` };
}
