'use client';

import { WidgetCard } from "@/components/dashboard/WidgetCard";
import { Button } from "@/components/ui/button";
import { formatCurrency } from "@/lib/utils";
import { FinancialSummary, financeService } from "@/services/finance";
import { ArrowRight, DollarSign, TrendingDown, TrendingUp } from "lucide-react";
import Link from "next/link";
import { useEffect, useState } from "react";

export function FinanceSummaryWidget() {
  const [data, setData] = useState<FinancialSummary | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function fetchData() {
      try {
        const now = new Date();
        const start = new Date(now.getFullYear(), now.getMonth(), 1).toISOString();
        const end = new Date(now.getFullYear(), now.getMonth() + 1, 0).toISOString();

        const result = await financeService.getFinancialSummary({
          startDate: start,
          endDate: end
        });
        setData(result);
      } catch (err) {
        setError("Erro ao carregar dados financeiros.");
        console.error(err);
      } finally {
        setIsLoading(false);
      }
    }

    fetchData();
  }, []);

  return (
    <WidgetCard
      title="Resumo Financeiro (Mês Atual)"
      icon={<DollarSign className="h-4 w-4" />}
      isLoading={isLoading}
      error={error}
      className="col-span-2"
      action={
        <Button asChild variant="default" size="sm" className="gap-2">
          <Link href="/finance">
            Ver detalhes
            <ArrowRight className="h-4 w-4" />
          </Link>
        </Button>
      }
    >
      {data && (
        <div className="grid grid-cols-3 gap-4">
          <div className="space-y-1">
            <p className="text-xs text-muted-foreground">Fluxo de Caixa</p>
            <p className="text-2xl font-bold">{formatCurrency(data.netCashFlow)}</p>
          </div>
          <div className="space-y-1">
            <div className="flex items-center gap-1 text-xs text-muted-foreground">
              <TrendingUp className="h-3 w-3 text-emerald-500" />
              Receitas
            </div>
            <p className="text-lg font-semibold text-emerald-600">{formatCurrency(data.totalIncome)}</p>
          </div>
          <div className="space-y-1">
            <div className="flex items-center gap-1 text-xs text-muted-foreground">
              <TrendingDown className="h-3 w-3 text-red-500" />
              Despesas
            </div>
            <p className="text-lg font-semibold text-red-600">{formatCurrency(data.totalExpenses)}</p>
          </div>
        </div>
      )}
    </WidgetCard>
  );
}
