'use client';

import { useState, useEffect } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Progress } from '@/components/ui/progress';
import { Badge } from '@/components/ui/badge';
import { Plus, Target, TrendingUp } from 'lucide-react';
import { plannerService } from '@/services/plannerService';
import { FinancialGoal, GOAL_CATEGORY_LABELS } from '@/types/planner';
import { toast } from 'sonner';
import { formatCurrency } from '@/lib/utils';

export default function GoalsPage() {
  const [goals, setGoals] = useState<FinancialGoal[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  const loadGoals = async () => {
    try {
      setIsLoading(true);
      const data = await plannerService.getFinancialGoals();
      setGoals(data);
    } catch (error) {
      console.error('Erro ao carregar metas:', error);
      toast.error('Erro ao carregar metas financeiras');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    loadGoals();
  }, []);

  const getProgressColor = (progress: number) => {
    if (progress >= 100) return 'bg-green-600';
    if (progress >= 70) return 'bg-blue-600';
    if (progress >= 30) return 'bg-yellow-600';
    return 'bg-slate-400';
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Metas Financeiras</h1>
          <p className="text-muted-foreground">
            Defina e acompanhe seus objetivos financeiros
          </p>
        </div>

        <Button>
          <Plus className="mr-2 h-4 w-4" />
          Nova Meta
        </Button>
      </div>

      {/* Stats Cards */}
      <div className="grid gap-4 md:grid-cols-3">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Total de Metas</CardTitle>
            <Target className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{goals.length}</div>
            <p className="text-xs text-muted-foreground">
              {goals.filter((g) => g.isActive).length} ativas
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Metas Concluídas</CardTitle>
            <TrendingUp className="h-4 w-4 text-green-600" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-green-600">
              {goals.filter((g) => g.isCompleted).length}
            </div>
            <p className="text-xs text-muted-foreground">
              {goals.length > 0
                ? `${Math.round((goals.filter((g) => g.isCompleted).length / goals.length) * 100)}%`
                : '0%'}{' '}
              do total
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Valor Acumulado</CardTitle>
            <TrendingUp className="h-4 w-4 text-blue-600" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-blue-600">
              {formatCurrency(goals.reduce((sum, g) => sum + g.currentAmount, 0))}
            </div>
            <p className="text-xs text-muted-foreground">
              de {formatCurrency(goals.reduce((sum, g) => sum + g.targetAmount, 0))}
            </p>
          </CardContent>
        </Card>
      </div>

      {/* Goals List */}
      {isLoading ? (
        <div className="flex items-center justify-center py-12">
          <p className="text-muted-foreground">Carregando metas...</p>
        </div>
      ) : goals.length === 0 ? (
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-12">
            <Target className="h-12 w-12 text-muted-foreground mb-4" />
            <h3 className="text-lg font-semibold mb-2">Nenhuma meta cadastrada</h3>
            <p className="text-sm text-muted-foreground mb-4">
              Comece criando sua primeira meta financeira
            </p>
            <Button>
              <Plus className="mr-2 h-4 w-4" />
              Criar primeira meta
            </Button>
          </CardContent>
        </Card>
      ) : (
        <div className="grid gap-4 md:grid-cols-2">
          {goals.map((goal) => (
            <Card key={goal.id} className="hover:shadow-lg transition-shadow">
              <CardHeader>
                <div className="flex items-start justify-between">
                  <div className="space-y-1">
                    <CardTitle>{goal.name}</CardTitle>
                    <CardDescription>{GOAL_CATEGORY_LABELS[goal.category]}</CardDescription>
                  </div>
                  <div className="flex flex-col gap-2 items-end">
                    {goal.isCompleted && (
                      <Badge variant="default" className="bg-green-600">
                        ✓ Concluída
                      </Badge>
                    )}
                    {!goal.isActive && (
                      <Badge variant="secondary">Inativa</Badge>
                    )}
                    {goal.autoAllocateSurplus && (
                      <Badge variant="outline" className="text-xs">
                        Auto-alocação
                      </Badge>
                    )}
                  </div>
                </div>
              </CardHeader>
              <CardContent className="space-y-4">
                <div>
                  <div className="flex items-center justify-between text-sm mb-2">
                    <span className="text-muted-foreground">Progresso</span>
                    <span className="font-semibold">{goal.progress.toFixed(1)}%</span>
                  </div>
                  <Progress value={goal.progress} className={getProgressColor(goal.progress)} />
                </div>

                <div className="grid grid-cols-2 gap-4 text-sm">
                  <div>
                    <p className="text-muted-foreground">Acumulado</p>
                    <p className="font-semibold">{formatCurrency(goal.currentAmount)}</p>
                  </div>
                  <div>
                    <p className="text-muted-foreground">Meta</p>
                    <p className="font-semibold">{formatCurrency(goal.targetAmount)}</p>
                  </div>
                  <div>
                    <p className="text-muted-foreground">Faltam</p>
                    <p className="font-semibold text-orange-600">
                      {formatCurrency(goal.remainingAmount)}
                    </p>
                  </div>
                  {goal.targetDate && (
                    <div>
                      <p className="text-muted-foreground">Prazo</p>
                      <p className="font-semibold">
                        {new Date(goal.targetDate).toLocaleDateString('pt-BR')}
                      </p>
                    </div>
                  )}
                </div>

                <div className="flex gap-2 pt-2">
                  <Button variant="outline" size="sm" className="flex-1">
                    Contribuir
                  </Button>
                  <Button variant="ghost" size="sm" className="flex-1">
                    Editar
                  </Button>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
