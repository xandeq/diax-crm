'use client';

import { WidgetCard } from "@/components/dashboard/WidgetCard";
import { Button } from "@/components/ui/button";
import { apiFetch } from "@/services/api";
import { ArrowRight, Eye, Mail, MousePointerClick, Send } from "lucide-react";
import Link from "next/link";
import { useEffect, useState } from "react";

interface EmailStats {
  totalCampaigns: number;
  totalEmailsSent: number;
  totalOpened: number;
  totalClicks: number;
  openRate: number;
  clickRate: number;
}

export function EmailSummaryWidget() {
  const [stats, setStats] = useState<EmailStats | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function fetchData() {
      try {
        const data = await apiFetch<{ overallStats: EmailStats }>(
          '/email-campaigns/analytics?days=30'
        );
        setStats(data.overallStats);
      } catch (err) {
        setError("Erro ao carregar dados de email.");
        console.error(err);
      } finally {
        setIsLoading(false);
      }
    }

    fetchData();
  }, []);

  return (
    <WidgetCard
      title="Email Marketing"
      icon={<Mail className="h-4 w-4" />}
      isLoading={isLoading}
      error={error}
      infoTooltip="Resumo dos ultimos 30 dias"
      action={
        <Button asChild variant="default" size="sm" className="gap-2">
          <Link href="/analytics">
            Ver analytics
            <ArrowRight className="h-4 w-4" />
          </Link>
        </Button>
      }
    >
      {stats && (
        <div className="space-y-3">
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-1">
              <div className="flex items-center gap-1 text-xs text-muted-foreground">
                <Send className="h-3 w-3" />
                Enviados
              </div>
              <p className="text-2xl font-bold">{stats.totalEmailsSent}</p>
              <p className="text-xs text-muted-foreground">
                {stats.totalCampaigns} {stats.totalCampaigns === 1 ? 'campanha' : 'campanhas'}
              </p>
            </div>
            <div className="space-y-1">
              <div className="flex items-center gap-1 text-xs text-muted-foreground">
                <Eye className="h-3 w-3" />
                Abertos
              </div>
              <p className="text-2xl font-bold">{stats.totalOpened}</p>
              <p className="text-xs text-muted-foreground">
                {stats.openRate.toFixed(1)}% taxa
              </p>
            </div>
          </div>
          {stats.totalClicks > 0 && (
            <div className="flex items-center gap-2 text-xs text-muted-foreground border-t pt-2">
              <MousePointerClick className="h-3 w-3" />
              <span>{stats.totalClicks} cliques ({stats.clickRate.toFixed(1)}%)</span>
            </div>
          )}
        </div>
      )}
    </WidgetCard>
  );
}
