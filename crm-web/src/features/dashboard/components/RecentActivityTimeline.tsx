'use client';

import * as React from 'react';
import { motion } from 'framer-motion';
import { AppLog } from '../types/dashboard.types';
import { LogLevel, logLevelLabels, logLevelColors } from '@/services/logs';
import { Shield, Info, AlertTriangle, AlertCircle, Clock } from 'lucide-react';
import { formatDistanceToNow } from 'date-fns';
import { ptBR } from 'date-fns/locale';

interface RecentActivityTimelineProps {
  logs: AppLog[];
  loading?: boolean;
  className?: string;
}

export function RecentActivityTimeline({ logs, loading, className }: RecentActivityTimelineProps) {
  const getIcon = (level: LogLevel) => {
    switch (level) {
      case LogLevel.Critical:
        return <AlertCircle className="h-4 w-4 text-purple-400" />;
      case LogLevel.Error:
        return <AlertCircle className="h-4 w-4 text-red-400" />;
      case LogLevel.Warning:
        return <AlertTriangle className="h-4 w-4 text-amber-400" />;
      default:
        return <Info className="h-4 w-4 text-teal-400" />;
    }
  };

  const getBorderColor = (level: LogLevel) => {
    switch (level) {
      case LogLevel.Critical:
        return 'border-purple-500/20';
      case LogLevel.Error:
        return 'border-red-500/20';
      case LogLevel.Warning:
        return 'border-amber-500/20';
      default:
        return 'border-zinc-800/80';
    }
  };

  const formatTime = (isoString: string) => {
    try {
      return formatDistanceToNow(new Date(isoString), { addSuffix: true, locale: ptBR });
    } catch {
      return 'recentemente';
    }
  };

  if (loading) {
    return (
      <div className="space-y-4">
        {Array.from({ length: 4 }).map((_, idx) => (
          <div key={idx} className="flex gap-4 items-start animate-pulse">
            <div className="h-8 w-8 bg-zinc-800 rounded-full shrink-0" />
            <div className="flex-1 space-y-2">
              <div className="h-3 w-1/4 bg-zinc-800 rounded" />
              <div className="h-4 w-3/4 bg-zinc-800 rounded" />
            </div>
          </div>
        ))}
      </div>
    );
  }

  if (logs.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center py-12 text-zinc-500">
        <Clock className="h-8 w-8 stroke-[1.5] mb-2" />
        <span className="text-xs font-semibold">Nenhuma atividade recente registrada</span>
      </div>
    );
  }

  return (
    <div className={`relative border-l border-zinc-800/80 pl-6 space-y-6 ml-2.5 ${className}`}>
      {logs.map((log, idx) => (
        <motion.div
          key={log.id || idx}
          initial={{ opacity: 0, x: -10 }}
          animate={{ opacity: 1, x: 0 }}
          transition={{ duration: 0.3, delay: idx * 0.05 }}
          className="relative flex flex-col sm:flex-row sm:items-center justify-between gap-2 p-3 bg-zinc-950/10 border rounded-xl backdrop-blur-xl transition-colors hover:bg-zinc-950/30"
          style={{ borderColor: 'var(--border-color, rgba(39, 39, 42, 0.4))' }}
        >
          {/* Timeline Dot Indicator */}
          <span className="absolute -left-[31px] top-4.5 flex h-4.5 w-4.5 items-center justify-center rounded-full bg-zinc-900 border border-zinc-800 shadow-[0_0_8px_rgba(0,0,0,0.8)]">
            {getIcon(log.level)}
          </span>

          <div className="space-y-0.5 max-w-[80%]">
            <span className="text-[10px] font-bold text-zinc-500 uppercase tracking-wider">
              {log.requestPath ? `${log.statusCode ?? 200} - ${log.requestPath}` : 'Evento de Sistema'}
            </span>
            <p className="text-xs font-bold text-zinc-200 leading-normal line-clamp-2">
              {log.message}
            </p>
          </div>

          <div className="flex items-center gap-1.5 shrink-0 text-[10px] text-zinc-500 font-bold self-start sm:self-center mt-1 sm:mt-0">
            <Clock className="h-3 w-3" />
            <span>{formatTime(log.timestampUtc)}</span>
          </div>
        </motion.div>
      ))}
    </div>
  );
}
