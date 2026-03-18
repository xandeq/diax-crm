import React from 'react';
import { AlertCircle, RotateCw } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Progress } from '@/components/ui/progress';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { Badge } from '@/components/ui/badge';

export interface QuotaStatusDto {
  providerName: string;
  quotaType: string; // "Generations", "Credits", "Minutes", "Cost"
  dailyLimit: number | null;
  currentUsage: number;
  remaining: number;
  percentageUsed: number;
  resetAt: string; // ISO date string
  timeUntilReset: {
    hours: number;
    minutes: number;
    seconds: number;
  };
  isExhausted: boolean;
}

interface QuotaStatusCardProps {
  quota: QuotaStatusDto | null;
  isLoading?: boolean;
  onResetClick?: () => void;
}

export function QuotaStatusCard({ quota, isLoading = false, onResetClick }: QuotaStatusCardProps) {
  if (!quota) return null;

  const percentUsed = Math.min(quota.percentageUsed, 100);
  const resetTime = new Date(quota.resetAt);
  const resetTimeFormatted = resetTime.toLocaleTimeString('pt-BR', {
    hour: '2-digit',
    minute: '2-digit',
  });

  const getProgressColor = (): string => {
    if (quota.isExhausted) return 'bg-red-600';
    if (percentUsed > 75) return 'bg-orange-500';
    if (percentUsed > 50) return 'bg-yellow-500';
    return 'bg-green-500';
  };

  const getStatusBadgeVariant = (): 'default' | 'secondary' | 'destructive' | 'outline' => {
    if (quota.isExhausted) return 'destructive';
    if (percentUsed > 75) return 'secondary';
    return 'default';
  };

  return (
    <Card className="border-blue-200 bg-gradient-to-br from-blue-50 to-indigo-50">
      <CardHeader className="pb-3">
        <div className="flex items-center justify-between">
          <CardTitle className="text-lg font-semibold">
            Quota de Uso — {quota.providerName}
          </CardTitle>
          <Badge variant={getStatusBadgeVariant()}>
            {quota.remaining}/{quota.dailyLimit} {quota.quotaType === 'Credits' ? 'créditos' : 'gerações'}
          </Badge>
        </div>
      </CardHeader>

      <CardContent className="space-y-4">
        {/* Progress Bar */}
        <div className="space-y-2">
          <div className="flex justify-between text-sm">
            <span className="text-gray-700 font-medium">Uso diário</span>
            <span className="text-gray-600">{percentUsed.toFixed(0)}%</span>
          </div>
          <Progress value={percentUsed} className="h-3" />
        </div>

        {/* Usage Details */}
        <div className="grid grid-cols-3 gap-3 text-sm">
          <div className="rounded-lg bg-white/50 p-2 text-center">
            <div className="text-gray-600 font-medium">Usado</div>
            <div className="text-lg font-bold text-gray-900">{quota.currentUsage}</div>
          </div>
          <div className="rounded-lg bg-white/50 p-2 text-center">
            <div className="text-gray-600 font-medium">Limite</div>
            <div className="text-lg font-bold text-gray-900">{quota.dailyLimit}</div>
          </div>
          <div className="rounded-lg bg-white/50 p-2 text-center">
            <div className="text-gray-600 font-medium">Restante</div>
            <div className={`text-lg font-bold ${quota.isExhausted ? 'text-red-600' : 'text-green-600'}`}>
              {quota.remaining}
            </div>
          </div>
        </div>

        {/* Reset Time */}
        <div className="rounded-lg bg-white/60 p-3 text-sm">
          <div className="text-gray-600 font-medium mb-1">Próximo reset</div>
          <div className="flex items-center justify-between">
            <span className="text-gray-900 font-semibold">{resetTimeFormatted} (UTC)</span>
            <span className="text-gray-600">
              {quota.timeUntilReset.hours}h {quota.timeUntilReset.minutes}m
            </span>
          </div>
        </div>

        {/* Exhausted Alert */}
        {quota.isExhausted && (
          <Alert variant="destructive" className="mt-3">
            <AlertCircle className="h-4 w-4" />
            <AlertTitle>Limite diário atingido</AlertTitle>
            <AlertDescription>
              Você alcançou o limite de {quota.dailyLimit} {quota.quotaType === 'Credits' ? 'créditos' : 'gerações'} por dia. Tente novamente após o reset.
            </AlertDescription>
          </Alert>
        )}

        {/* Reset Button (for admins) */}
        {onResetClick && (
          <button
            onClick={onResetClick}
            disabled={isLoading}
            className="w-full flex items-center justify-center gap-2 rounded-lg bg-white px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed border border-gray-200 transition-colors"
          >
            <RotateCw className="h-4 w-4" />
            Reset Manual (Admin)
          </button>
        )}
      </CardContent>
    </Card>
  );
}
