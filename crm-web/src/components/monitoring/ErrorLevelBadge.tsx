import { LEVEL_CONFIG, type ErrorLogLevel } from '@/services/errorLogs';
import { cn } from '@/lib/utils';

interface Props {
  level: ErrorLogLevel;
  size?: 'sm' | 'md';
}

export function ErrorLevelBadge({ level, size = 'md' }: Props) {
  const cfg = LEVEL_CONFIG[level] ?? LEVEL_CONFIG.Error;
  return (
    <span
      className={cn(
        'inline-flex items-center gap-1.5 rounded-full font-medium',
        cfg.bg, cfg.text,
        size === 'sm' ? 'px-2 py-0.5 text-xs' : 'px-2.5 py-1 text-xs'
      )}
      aria-label={`Nível: ${cfg.label}`}
    >
      <span
        className="inline-block rounded-full"
        style={{ width: 6, height: 6, background: cfg.dot, flexShrink: 0 }}
        aria-hidden
      />
      {cfg.label}
    </span>
  );
}
