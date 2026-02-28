"use client";

import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Skeleton } from "@/components/ui/skeleton";
import { EmailEvent, EmailTimelineData, getContactEmailTimeline } from "@/services/emailStats";
import {
  AlertCircle,
  CheckCircle,
  ExternalLink,
  Mail,
  MailOpen,
  MousePointerClick,
  Send,
  XCircle,
} from "lucide-react";
import { useEffect, useState } from "react";

interface EmailTimelineProps {
  customerId: string;
  days?: number;
}

/**
 * Timeline de eventos de email do contato (últimos 30 dias por padrão).
 * Mostra: Sent → Delivered → Opened → Clicked → Bounced → Spam → Unsubscribed
 */
export function EmailTimeline({ customerId, days = 30 }: EmailTimelineProps) {
  const [timeline, setTimeline] = useState<EmailTimelineData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let isMounted = true;

    const fetchTimeline = async () => {
      setLoading(true);
      setError(null);

      try {
        const data = await getContactEmailTimeline(customerId, days);

        if (isMounted) {
          setTimeline(data);
        }
      } catch (err) {
        console.error("Error fetching email timeline:", err);
        if (isMounted) {
          setError(err instanceof Error ? err.message : "Erro ao carregar timeline");
        }
      } finally {
        if (isMounted) {
          setLoading(false);
        }
      }
    };

    fetchTimeline();

    return () => {
      isMounted = false;
    };
  }, [customerId, days]);

  if (loading) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Histórico de Emails</CardTitle>
          <CardDescription>Carregando eventos...</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            {[1, 2, 3].map((i) => (
              <div key={i} className="flex gap-3">
                <Skeleton className="h-10 w-10 rounded-full" />
                <div className="flex-1 space-y-2">
                  <Skeleton className="h-4 w-1/3" />
                  <Skeleton className="h-3 w-2/3" />
                </div>
              </div>
            ))}
          </div>
        </CardContent>
      </Card>
    );
  }

  if (error) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Histórico de Emails</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex items-center gap-2 text-red-600">
            <AlertCircle className="h-5 w-5" />
            <span className="text-sm">{error}</span>
          </div>
        </CardContent>
      </Card>
    );
  }

  if (!timeline || timeline.events.length === 0) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Histórico de Emails</CardTitle>
          <CardDescription>Últimos {days} dias</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="text-center text-slate-500 py-8">
            <Mail className="h-12 w-12 mx-auto mb-3 opacity-50" />
            <p className="text-sm">Nenhum email enviado nos últimos {days} dias</p>
          </div>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>Histórico de Emails</CardTitle>
        <CardDescription>
          {timeline.events.length} evento{timeline.events.length !== 1 ? "s" : ""} nos últimos{" "}
          {days} dias
        </CardDescription>
      </CardHeader>
      <CardContent>
        <ScrollArea className="h-[500px] pr-4">
          <div className="space-y-4">
            {timeline.events.map((event, index) => (
              <EmailEventItem key={`${event.messageId}-${index}`} event={event} />
            ))}
          </div>
        </ScrollArea>
      </CardContent>
    </Card>
  );
}

function EmailEventItem({ event }: { event: EmailEvent }) {
  const config = getEventConfig(event.event);
  const Icon = config.icon;

  return (
    <div className="flex gap-3 pb-4 border-b border-slate-100 last:border-0">
      <div
        className={`flex-shrink-0 h-10 w-10 rounded-full flex items-center justify-center ${config.bgColor}`}
      >
        <Icon className={`h-5 w-5 ${config.iconColor}`} />
      </div>

      <div className="flex-1 min-w-0">
        <div className="flex items-center gap-2 mb-1">
          <Badge variant={config.badgeVariant} className="text-xs">
            {config.label}
          </Badge>
          <span className="text-xs text-slate-500">
            {new Date(event.eventAt).toLocaleString("pt-BR", {
              day: "2-digit",
              month: "short",
              year: "numeric",
              hour: "2-digit",
              minute: "2-digit",
            })}
          </span>
        </div>

        <p className="text-sm font-medium text-slate-900 truncate mb-1">{event.subject}</p>

        {event.link && (
          <a
            href={event.link}
            target="_blank"
            rel="noopener noreferrer"
            className="text-xs text-blue-600 hover:underline flex items-center gap-1"
          >
            <ExternalLink className="h-3 w-3" />
            Link clicado
          </a>
        )}

        {event.reason && (
          <p className="text-xs text-red-600 mt-1">
            <strong>Motivo:</strong> {event.reason}
          </p>
        )}

        {event.campaignId && (
          <p className="text-xs text-slate-500 mt-1">
            Campanha: {event.campaignName || event.campaignId}
          </p>
        )}
      </div>
    </div>
  );
}

function getEventConfig(eventType: number) {
  switch (eventType) {
    case 0: // Sent
      return {
        icon: Send,
        label: "Enviado",
        bgColor: "bg-blue-100",
        iconColor: "text-blue-600",
        badgeVariant: "secondary" as const,
      };
    case 1: // Delivered
      return {
        icon: CheckCircle,
        label: "Entregue",
        bgColor: "bg-green-100",
        iconColor: "text-green-600",
        badgeVariant: "default" as const,
      };
    case 2: // Opened
      return {
        icon: MailOpen,
        label: "Aberto",
        bgColor: "bg-purple-100",
        iconColor: "text-purple-600",
        badgeVariant: "secondary" as const,
      };
    case 3: // Clicked
      return {
        icon: MousePointerClick,
        label: "Clicado",
        bgColor: "bg-indigo-100",
        iconColor: "text-indigo-600",
        badgeVariant: "default" as const,
      };
    case 4: // Bounced
      return {
        icon: XCircle,
        label: "Bounce",
        bgColor: "bg-red-100",
        iconColor: "text-red-600",
        badgeVariant: "destructive" as const,
      };
    case 5: // Spam
      return {
        icon: AlertCircle,
        label: "Spam",
        bgColor: "bg-orange-100",
        iconColor: "text-orange-600",
        badgeVariant: "destructive" as const,
      };
    case 6: // Unsubscribed
      return {
        icon: XCircle,
        label: "Descadastrado",
        bgColor: "bg-gray-100",
        iconColor: "text-gray-600",
        badgeVariant: "outline" as const,
      };
    default:
      return {
        icon: Mail,
        label: "Desconhecido",
        bgColor: "bg-gray-100",
        iconColor: "text-gray-600",
        badgeVariant: "outline" as const,
      };
  }
}
