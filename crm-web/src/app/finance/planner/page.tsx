'use client';

import { useState } from 'react';
import Link from 'next/link';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { AlertCircle, RefreshCcw, TrendingUp, Target, Calendar } from 'lucide-react';
import { SimulationSummaryCard } from '@/components/finance/planner/SimulationSummaryCard';
import { RecommendationsList } from '@/components/finance/planner/RecommendationsList';
import { useMonthlySimulation, useRegenerateSimulation } from '@/hooks/finance';
import { toast } from 'sonner';

const MONTHS = [
  { value: 1, label: 'Janeiro' },
  { value: 2, label: 'Fevereiro' },
  { value: 3, label: 'Março' },
  { value: 4, label: 'Abril' },
  { value: 5, label: 'Maio' },
  { value: 6, label: 'Junho' },
  { value: 7, label: 'Julho' },
  { value: 8, label: 'Agosto' },
  { value: 9, label: 'Setembro' },
  { value: 10, label: 'Outubro' },
  { value: 11, label: 'Novembro' },
  { value: 12, label: 'Dezembro' }
];

export default function PlannerDashboard() {
  const currentDate = new Date();
  const [selectedMonth, setSelectedMonth] = useState(currentDate.getMonth() + 1);
  const [selectedYear, setSelectedYear] = useState(currentDate.getFullYear());

  const { data: simulation, isLoading, isFetching, error } = useMonthlySimulation(selectedYear, selectedMonth);
  const regenerateMutation = useRegenerateSimulation();

  const handleRegenerate = () => {
    regenerateMutation.mutate(
      { year: selectedYear, month: selectedMonth },
      {
        onSuccess: () => toast.success('Simulação regenerada com sucesso!'),
        onError: () => toast.error('Erro ao regenerar simulação. Tente novamente.'),
      },
    );
  };

  const years = Array.from({ length: 5 }, (_, i) => currentDate.getFullYear() + i);
  const isRegenerating = regenerateMutation.isPending;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Planejador Financeiro</h1>
          <p className="text-muted-foreground">
            Simulação mensal de fluxo de caixa e recomendações inteligentes
          </p>
        </div>
        <Button
          variant="outline"
          onClick={handleRegenerate}
          disabled={isRegenerating || isFetching}
        >
          <RefreshCcw className={`mr-2 h-4 w-4 ${isRegenerating ? 'animate-spin' : ''}`} />
          Regenerar
        </Button>
      </div>

      {/* Filters */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Calendar className="h-5 w-5" />
            Período da Simulação
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex gap-4">
            <Select
              value={selectedMonth.toString()}
              onValueChange={(value) => setSelectedMonth(parseInt(value))}
            >
              <SelectTrigger className="w-[180px]">
                <SelectValue placeholder="Mês" />
              </SelectTrigger>
              <SelectContent>
                {MONTHS.map((month) => (
                  <SelectItem key={month.value} value={month.value.toString()}>
                    {month.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>

            <Select
              value={selectedYear.toString()}
              onValueChange={(value) => setSelectedYear(parseInt(value))}
            >
              <SelectTrigger className="w-[120px]">
                <SelectValue placeholder="Ano" />
              </SelectTrigger>
              <SelectContent>
                {years.map((year) => (
                  <SelectItem key={year} value={year.toString()}>
                    {year}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        </CardContent>
      </Card>

      {/* Error state */}
      {error && !isLoading && (
        <Card className="border-destructive/30 bg-destructive/5">
          <CardContent className="flex items-center gap-3 py-4 mt-0">
            <AlertCircle className="h-5 w-5 text-destructive flex-shrink-0" />
            <div className="flex-1">
              <p className="font-medium text-destructive text-sm">Erro ao carregar simulação</p>
              <p className="text-xs text-muted-foreground mt-0.5">Verifique sua conexão e tente novamente.</p>
            </div>
            <Button
              variant="outline"
              size="sm"
              onClick={handleRegenerate}
              disabled={isRegenerating}
            >
              Tentar novamente
            </Button>
          </CardContent>
        </Card>
      )}

      {/* Loading State */}
      {isLoading && (
        <div className="flex items-center justify-center py-12">
          <div className="flex flex-col items-center gap-2">
            <RefreshCcw className="h-8 w-8 animate-spin text-primary" />
            <p className="text-sm text-muted-foreground">Gerando simulação...</p>
          </div>
        </div>
      )}

      {/* Simulation Content */}
      {!isLoading && simulation && (
        <div className="space-y-6">
          <SimulationSummaryCard simulation={simulation} />
          <RecommendationsList recommendations={simulation.recommendations} />

          <div className="grid gap-4 md:grid-cols-2">
            <Card className="hover:shadow-lg transition-shadow">
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <Target className="h-5 w-5 text-blue-600" />
                  Metas Financeiras
                </CardTitle>
                <CardDescription>
                  Gerencie suas metas e acompanhe o progresso
                </CardDescription>
              </CardHeader>
              <CardContent>
                <Button variant="outline" className="w-full" asChild>
                  <Link href="/finance/planner/goals">Ver Metas</Link>
                </Button>
              </CardContent>
            </Card>

            <Card className="hover:shadow-lg transition-shadow">
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <TrendingUp className="h-5 w-5 text-green-600" />
                  Transações Recorrentes
                </CardTitle>
                <CardDescription>
                  Configure receitas e despesas fixas
                </CardDescription>
              </CardHeader>
              <CardContent>
                <Button variant="outline" className="w-full" asChild>
                  <Link href="/finance/planner/recurring">Gerenciar Recorrentes</Link>
                </Button>
              </CardContent>
            </Card>
          </div>
        </div>
      )}

      {/* Empty State */}
      {!isLoading && !simulation && !error && (
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-12">
            <Calendar className="h-12 w-12 text-muted-foreground mb-4" />
            <h3 className="text-lg font-semibold mb-2">Nenhuma simulação encontrada</h3>
            <p className="text-sm text-muted-foreground mb-4">
              Selecione um mês e ano para gerar uma simulação
            </p>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
