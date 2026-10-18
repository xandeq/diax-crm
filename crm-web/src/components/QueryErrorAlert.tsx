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
  const raw = error instanceof Error ? error.message : 'Erro desconhecido ao falar com a API.';
  const message = /[.!?]$/.test(raw.trim()) ? raw.trim() : `${raw.trim()}.`;

  return (
    // Mobile: texto em cima, botão embaixo (lado a lado espremia o texto numa coluna de ~80px).
    <Alert variant="destructive" className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between sm:gap-4">
      <AlertTriangle className="h-4 w-4" />
      <div className="flex-1">
        <AlertTitle>{title}</AlertTitle>
        <AlertDescription>{message} Os dados abaixo não refletem o estado real.</AlertDescription>
      </div>
      {onRetry && (
        <div className="shrink-0">
          <Button size="sm" variant="outline" onClick={onRetry} disabled={retrying}>
            <RefreshCw className={`h-4 w-4 mr-2 ${retrying ? 'animate-spin' : ''}`} />
            Tentar de novo
          </Button>
        </div>
      )}
    </Alert>
  );
}
