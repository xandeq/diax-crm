"use client";

import { Badge } from "@/components/ui/badge";
import { ContactEmailStats, getContactEmailStats } from "@/services/emailStats";
import { Loader2 } from "lucide-react";
import { useEffect, useState } from "react";

interface EngagementBadgeProps {
  customerId: string;
  hasEmail?: boolean;
  compact?: boolean;
}

/**
 * Badge de engajamento de email do contato.
 * - 🔴 Baixo (0-20% open rate)
 * - 🟡 Médio (20-50% open rate)
 * - 🟢 Alto (>50% open rate)
 *
 * Faz chamada lazy à API do Brevo (com cache de 24h).
 */
export function EngagementBadge({
  customerId,
  hasEmail = true,
  compact = false
}: EngagementBadgeProps) {
  const [stats, setStats] = useState<ContactEmailStats | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(false);

  useEffect(() => {
    if (!hasEmail) {
      return; // Não faz chamada se não tem email
    }

    let isMounted = true;

    const fetchStats = async () => {
      setLoading(true);
      setError(false);

      try {
        const data = await getContactEmailStats(customerId);

        if (isMounted) {
          setStats(data);
        }
      } catch (err) {
        console.error("Error fetching email stats:", err);
        if (isMounted) {
          setError(true);
        }
      } finally {
        if (isMounted) {
          setLoading(false);
        }
      }
    };

    fetchStats();

    return () => {
      isMounted = false;
    };
  }, [customerId, hasEmail]);

  // Não tem email cadastrado
  if (!hasEmail) {
    return null;
  }

  // Carregando
  if (loading) {
    return (
      <Badge variant="outline" className="text-xs gap-1">
        <Loader2 className="h-3 w-3 animate-spin" />
        {!compact && "Carregando..."}
      </Badge>
    );
  }

  // Erro ao buscar
  if (error) {
    return null; // Falha silenciosamente
  }

  // Sem dados ainda
  if (!stats || stats.totalSent === 0) {
    return compact ? null : (
      <Badge variant="outline" className="text-xs">
        Sem envios
      </Badge>
    );
  }

  // Badges por nível de engajamento
  const getBadgeConfig = () => {
    switch (stats.engagementLevel) {
      case 2: // High
        return {
          variant: "default" as const,
          className: "bg-green-600 hover:bg-green-700 text-white",
          emoji: "🟢",
          label: compact ? "Alto" : "Engajamento Alto",
          rate: `${Math.round(stats.openRate)}%`,
        };
      case 1: // Medium
        return {
          variant: "secondary" as const,
          className: "bg-yellow-500 hover:bg-yellow-600 text-white",
          emoji: "🟡",
          label: compact ? "Médio" : "Engajamento Médio",
          rate: `${Math.round(stats.openRate)}%`,
        };
      case 0: // Low
      default:
        return {
          variant: "destructive" as const,
          className: "bg-red-600 hover:bg-red-700",
          emoji: "🔴",
          label: compact ? "Baixo" : "Engajamento Baixo",
          rate: `${Math.round(stats.openRate)}%`,
        };
    }
  };

  const config = getBadgeConfig();

  return (
    <Badge
      variant={config.variant}
      className={`text-xs gap-1 ${config.className}`}
      title={`Taxa de abertura: ${config.rate} | Enviados: ${stats.totalSent} | Abertos: ${stats.totalOpened} | Clicks: ${stats.totalClicked}`}
    >
      <span>{config.emoji}</span>
      <span>{compact ? config.rate : `${config.label} (${config.rate})`}</span>
    </Badge>
  );
}
