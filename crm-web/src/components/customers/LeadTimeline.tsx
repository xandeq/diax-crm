'use client';

import { getCustomerActivities, LeadActivity } from '@/services/customers';
import {
    AlertCircle,
    CheckCircle,
    Eye,
    Mail,
    UserCheck,
    UserPlus,
} from 'lucide-react';
import { useEffect, useState } from 'react';

import { Skeleton } from '@/components/ui/skeleton';

interface LeadTimelineProps {
  customerId: string;
}

const activityIcon: Record<string, React.ReactNode> = {
  email_sent: <Mail className="h-4 w-4 text-blue-600" />,
  email_failed: <AlertCircle className="h-4 w-4 text-red-500" />,
  email_queued: <Mail className="h-4 w-4 text-slate-400" />,
  contact_registered: <UserCheck className="h-4 w-4 text-indigo-500" />,
  created: <UserPlus className="h-4 w-4 text-slate-500" />,
  converted: <CheckCircle className="h-4 w-4 text-green-600" />,
};

const dotColor: Record<string, string> = {
  success: 'bg-green-500',
  warning: 'bg-yellow-400',
  info: 'bg-blue-500',
  error: 'bg-red-500',
};

function formatDate(iso: string): string {
  return new Date(iso).toLocaleString('pt-BR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

export function LeadTimeline({ customerId }: LeadTimelineProps) {
  const [activities, setActivities] = useState<LeadActivity[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!customerId) return;

    setLoading(true);
    setError(null);

    getCustomerActivities(customerId)
      .then(setActivities)
      .catch(() => setError('Erro ao carregar atividades.'))
      .finally(() => setLoading(false));
  }, [customerId]);

  if (loading) {
    return (
      <div className="space-y-4 pt-2">
        {Array.from({ length: 4 }).map((_, i) => (
          <div key={i} className="flex gap-3 items-start">
            <div className="flex flex-col items-center mt-1">
              <Skeleton className="h-3 w-3 rounded-full" />
              {i < 3 && <Skeleton className="w-px h-10 mt-1" />}
            </div>
            <div className="flex-1 space-y-1.5 pb-2">
              <Skeleton className="h-4 w-2/3" />
              <Skeleton className="h-3 w-1/2" />
            </div>
          </div>
        ))}
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex items-center gap-2 text-sm text-red-500 pt-4">
        <AlertCircle className="h-4 w-4 flex-shrink-0" />
        {error}
      </div>
    );
  }

  if (activities.length === 0) {
    return (
      <p className="text-sm text-slate-500 pt-4 text-center">
        Nenhuma atividade registrada ainda.
      </p>
    );
  }

  return (
    <div className="pt-2">
      {activities.map((activity, idx) => {
        const isLast = idx === activities.length - 1;
        const icon = activityIcon[activity.type] ?? (
          <span className="h-4 w-4 rounded-full bg-slate-300 block" />
        );
        const dot = dotColor[activity.status] ?? 'bg-slate-400';

        return (
          <div key={idx} className="flex gap-3 items-start">
            {/* Linha vertical + bolinha */}
            <div className="flex flex-col items-center">
              <span className={`mt-1.5 h-2.5 w-2.5 rounded-full flex-shrink-0 ${dot}`} />
              {!isLast && (
                <span className="w-px flex-1 min-h-[2.5rem] bg-slate-200 mt-1" />
              )}
            </div>

            {/* Conteúdo */}
            <div className="flex-1 pb-5">
              <div className="flex items-start gap-2">
                <span className="mt-0.5 flex-shrink-0">{icon}</span>
                <div className="min-w-0">
                  <p className="text-sm font-medium text-slate-800 leading-snug">
                    {activity.title}
                  </p>
                  {activity.detail && (
                    <p className="text-xs text-slate-500 mt-0.5 break-words">
                      {activity.detail}
                    </p>
                  )}
                  {activity.wasRead && activity.readAt && (
                      <p className="text-xs text-green-600 mt-1 flex items-center font-medium bg-green-50 p-1 rounded w-fit">
                          <Eye className="w-3 h-3 mr-1" />
                          Lido em {formatDate(activity.readAt)}
                      </p>
                  )}
                  <p className="text-xs text-slate-400 mt-1">
                    {formatDate(activity.date)}
                  </p>
                </div>
              </div>
            </div>
          </div>
        );
      })}
    </div>
  );
}
