'use client';

import { WidgetCard } from "@/components/dashboard/WidgetCard";
import { AppLogStatsResponse, logsService } from "@/services/logs";
import { Activity, AlertTriangle, CheckCircle, Info } from "lucide-react";
import { useEffect, useState } from "react";

export function SystemHealthWidget() {
  const [stats, setStats] = useState<AppLogStatsResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function fetchData() {
      try {
        const result = await logsService.getStats();
        setStats(result);
      } catch (err) {
        setError("Erro ao carregar status do sistema.");
        console.error(err);
      } finally {
        setIsLoading(false);
      }
    }

    fetchData();
  }, []);

  return (
    <WidgetCard
      title="Saúde do Sistema"
      icon={<Activity className="h-4 w-4" />}
      isLoading={isLoading}
      error={error}
    >
      {stats && (
        <div className="grid grid-cols-3 gap-2">
             <div className="flex flex-col items-center p-2 rounded bg-red-50 text-red-700">
                <AlertTriangle className="h-4 w-4 mb-1" />
                <span className="text-xs font-semibold">Erros</span>
                <span className="text-lg font-bold">{stats.errorCount + stats.criticalCount}</span>
             </div>
             <div className="flex flex-col items-center p-2 rounded bg-yellow-50 text-yellow-700">
                <Info className="h-4 w-4 mb-1" />
                <span className="text-xs font-semibold">Avisos</span>
                <span className="text-lg font-bold">{stats.warningCount}</span>
             </div>
             <div className="flex flex-col items-center p-2 rounded bg-blue-50 text-blue-700">
                <CheckCircle className="h-4 w-4 mb-1" />
                <span className="text-xs font-semibold">Info</span>
                <span className="text-lg font-bold">{stats.informationCount}</span>
             </div>
        </div>
      )}
    </WidgetCard>
  );
}
