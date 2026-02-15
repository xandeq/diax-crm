'use client';

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { AlertTriangle, TrendingUp, CreditCard, Info } from 'lucide-react';
import { Recommendation, RecommendationType } from '@/types/planner';

interface RecommendationsListProps {
  recommendations: Recommendation[];
}

const RECOMMENDATION_ICONS: Record<RecommendationType, React.ReactNode> = {
  [RecommendationType.DeferExpense]: <AlertTriangle className="h-4 w-4" />,
  [RecommendationType.ChangeCard]: <CreditCard className="h-4 w-4" />,
  [RecommendationType.IncreaseIncome]: <TrendingUp className="h-4 w-4" />,
  [RecommendationType.Alert]: <AlertTriangle className="h-4 w-4" />,
  [RecommendationType.OptimizePayment]: <Info className="h-4 w-4" />
};

const RECOMMENDATION_VARIANTS: Record<RecommendationType, 'default' | 'destructive' | 'secondary' | 'outline'> = {
  [RecommendationType.DeferExpense]: 'destructive',
  [RecommendationType.ChangeCard]: 'secondary',
  [RecommendationType.IncreaseIncome]: 'default',
  [RecommendationType.Alert]: 'destructive',
  [RecommendationType.OptimizePayment]: 'outline'
};

export function RecommendationsList({ recommendations }: RecommendationsListProps) {
  if (recommendations.length === 0) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>✅ Nenhuma Recomendação</CardTitle>
          <CardDescription>
            Sua simulação está saudável! Não há recomendações neste momento.
          </CardDescription>
        </CardHeader>
      </Card>
    );
  }

  const sortedRecommendations = [...recommendations].sort((a, b) => a.priority - b.priority);

  return (
    <Card>
      <CardHeader>
        <CardTitle>💡 Recomendações</CardTitle>
        <CardDescription>
          Sugestões para otimizar seu planejamento financeiro
        </CardDescription>
      </CardHeader>
      <CardContent>
        <div className="space-y-4">
          {sortedRecommendations.map((recommendation, index) => (
            <div
              key={index}
              className="flex items-start gap-3 p-4 border rounded-lg hover:bg-slate-50 transition-colors"
            >
              <div className="mt-1">
                <Badge variant={RECOMMENDATION_VARIANTS[recommendation.type]}>
                  {RECOMMENDATION_ICONS[recommendation.type]}
                </Badge>
              </div>
              <div className="flex-1">
                <h4 className="font-semibold mb-1">{recommendation.title}</h4>
                <p className="text-sm text-muted-foreground">{recommendation.message}</p>
              </div>
              <Badge variant="outline" className="text-xs">
                Prioridade {recommendation.priority}
              </Badge>
            </div>
          ))}
        </div>
      </CardContent>
    </Card>
  );
}
