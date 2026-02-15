'use client';

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { AlertTriangle, TrendingDown, TrendingUp, DollarSign } from 'lucide-react';
import { MonthlySimulation } from '@/types/planner';
import { formatCurrency } from '@/lib/utils';

interface SimulationSummaryCardProps {
  simulation: MonthlySimulation;
}

export function SimulationSummaryCard({ simulation }: SimulationSummaryCardProps) {
  const surplus = simulation.projectedEndingBalance - simulation.startingBalance;
  const isSurplus = surplus > 0;

  return (
    <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
      {/* Saldo Inicial */}
      <Card>
        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
          <CardTitle className="text-sm font-medium">Saldo Inicial</CardTitle>
          <DollarSign className="h-4 w-4 text-muted-foreground" />
        </CardHeader>
        <CardContent>
          <div className="text-2xl font-bold">{formatCurrency(simulation.startingBalance)}</div>
          <p className="text-xs text-muted-foreground">
            {new Date(simulation.simulationDate).toLocaleDateString('pt-BR')}
          </p>
        </CardContent>
      </Card>

      {/* Receitas Projetadas */}
      <Card>
        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
          <CardTitle className="text-sm font-medium">Receitas Projetadas</CardTitle>
          <TrendingUp className="h-4 w-4 text-green-600" />
        </CardHeader>
        <CardContent>
          <div className="text-2xl font-bold text-green-600">
            {formatCurrency(simulation.totalProjectedIncome)}
          </div>
          <p className="text-xs text-muted-foreground">Entradas do mês</p>
        </CardContent>
      </Card>

      {/* Despesas Projetadas */}
      <Card>
        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
          <CardTitle className="text-sm font-medium">Despesas Projetadas</CardTitle>
          <TrendingDown className="h-4 w-4 text-red-600" />
        </CardHeader>
        <CardContent>
          <div className="text-2xl font-bold text-red-600">
            {formatCurrency(simulation.totalProjectedExpenses)}
          </div>
          <p className="text-xs text-muted-foreground">Saídas do mês</p>
        </CardContent>
      </Card>

      {/* Saldo Final Projetado */}
      <Card>
        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
          <CardTitle className="text-sm font-medium">Saldo Final Projetado</CardTitle>
          <DollarSign className={`h-4 w-4 ${simulation.hasNegativeBalanceRisk ? 'text-red-600' : 'text-green-600'}`} />
        </CardHeader>
        <CardContent>
          <div className={`text-2xl font-bold ${simulation.hasNegativeBalanceRisk ? 'text-red-600' : 'text-green-600'}`}>
            {formatCurrency(simulation.projectedEndingBalance)}
          </div>
          <p className="text-xs text-muted-foreground">
            {isSurplus ? 'Sobra' : 'Déficit'}: {formatCurrency(Math.abs(surplus))}
          </p>
        </CardContent>
      </Card>

      {/* Alertas de Risco */}
      {simulation.hasNegativeBalanceRisk && (
        <div className="col-span-full">
          <Alert variant="destructive">
            <AlertTriangle className="h-4 w-4" />
            <AlertTitle>⚠️ Risco de Saldo Negativo</AlertTitle>
            <AlertDescription>
              {simulation.firstNegativeBalanceDate && (
                <>
                  Saldo ficará negativo em{' '}
                  <strong>{new Date(simulation.firstNegativeBalanceDate).toLocaleDateString('pt-BR')}</strong>.
                  Menor saldo projetado: <strong>{formatCurrency(simulation.lowestProjectedBalance)}</strong>.
                </>
              )}
            </AlertDescription>
          </Alert>
        </div>
      )}
    </div>
  );
}
