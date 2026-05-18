'use client';

import { WidgetCard } from "@/components/dashboard/WidgetCard";
import { Button } from "@/components/ui/button";
import { useFinancialSummary } from "@/hooks/finance";
import { formatCurrency } from "@/lib/utils";
import { ArrowRight, DollarSign, TrendingDown, TrendingUp } from "lucide-react";
import Link from "next/link";

export function FinanceSummaryWidget() {
  const now = new Date();
  const start = new Date(now.getFullYear(), now.getMonth(), 1).toISOString();
  const end = new Date(now.getFullYear(), now.getMonth() + 1, 0).toISOString();

  const { data, isLoading, isError } = useFinancialSummary({ startDate: start, endDate: end });

  return (
    <WidgetCard
      title="Resumo Financeiro (Mês Atual)"
      icon={<DollarSign className="h-4 w-4" />}
      isLoading={isLoading}
      error={isError ? "Erro ao carregar dados financeiros." : null}
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
