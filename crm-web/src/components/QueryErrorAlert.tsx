'use client';

import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';
import { AlertTriangle, RefreshCw } from 'lucide-react';

interface QueryErrorAlertProps {
  error: unknown;
  title?: string;
  onRetry?: () => void;
  retrying?: boolean;
}

export function QueryErrorAlert({ error, title = 'Não foi possível carregar os dados', onRetry, retrying }: QueryErrorAlertProps) {
  const message = error instanceof Error ? error.message : 'Erro desconhecido ao falar com a API.';

  return (
    <Alert variant="destructive" className="flex items-start justify-between gap-4">
      <AlertTriangle className="h-4 w-4" />
      <div className="flex-1">
        <AlertTitle>{title}</AlertTitle>
        <AlertDescription>{message} Os dados abaixo não refletem o estado real.</AlertDescription>
      </div>
      {onRetry && (
        <Button size="sm" variant="outline" onClick={onRetry} disabled={retrying} className="shrink-0">
          <RefreshCw className={`h-4 w-4 mr-2 ${retrying ? 'animate-spin' : ''}`} />
          Tentar de novo
        </Button>
      )}
    </Alert>
  );
}
