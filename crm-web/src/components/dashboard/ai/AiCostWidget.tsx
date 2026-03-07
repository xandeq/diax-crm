'use client';

import { WidgetCard } from "@/components/dashboard/WidgetCard";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { getGroupedAiUsageStats, ProviderUsageStats } from "@/services/aiUsageLogs";
import { BarChart3, Info } from "lucide-react";
import { useEffect, useState } from "react";

export function AiCostWidget() {
  const [items, setItems] = useState<ProviderUsageStats[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function fetchData() {
      try {
        // Fetch stats for the last 30 days by default
        const end = new Date();
        const start = new Date();
        start.setDate(start.getDate() - 30);

        const stats = await getGroupedAiUsageStats(start.toISOString(), end.toISOString());
        setItems(stats);
      } catch (err) {
        setError("Erro ao carregar custos de IA.");
        console.error(err);
      } finally {
        setIsLoading(false);
      }
    }

    fetchData();
  }, []);

  const totalCost = items.reduce((acc, curr) => acc + curr.totalCost, 0);

  return (
    <WidgetCard
      title="Monitor de Custos de IA (30 dias)"
      icon={<BarChart3 className="h-4 w-4" />}
      isLoading={isLoading}
      error={error}
      infoTooltip="Acompanhe quanto você está investindo em requisições de Inteligência Artificial em todos os provedores em uso."
    >
      <Alert className="mb-4 bg-muted/30 border-none px-3 py-2">
        <Info className="h-4 w-4 text-muted-foreground mb-0" />
        <AlertDescription className="text-xs text-muted-foreground mt-0">
          Esta ferramenta monitora o consumo das APIs para evitar surpresas no fim do mês.
        </AlertDescription>
      </Alert>

      {items.length === 0 ? (
        <div className="text-sm text-muted-foreground">Nenhum custo registrado.</div>
      ) : (
        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <span className="text-sm font-medium">Custo Total Estimado:</span>
            <span className="text-lg font-bold">${totalCost.toFixed(4)}</span>
          </div>
          <ul className="space-y-2">
            {items.map(item => (
              <li key={item.providerId} className="flex justify-between items-center bg-muted/50 p-2 rounded-md">
                <div className="flex flex-col">
                  <span className="text-sm font-medium">{item.providerName || "Desconhecido"}</span>
                  <span className="text-xs text-muted-foreground">{item.totalRequests} req. / {item.totalTokens.toLocaleString()} tokens</span>
                </div>
                <span className="text-sm font-bold text-foreground">
                  ${item.totalCost.toFixed(4)}
                </span>
              </li>
            ))}
          </ul>
        </div>
      )}
    </WidgetCard>
  );
}
