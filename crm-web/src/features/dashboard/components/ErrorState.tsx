'use client';

import * as React from 'react';
import { AlertCircle, RefreshCw } from 'lucide-react';
import { Button } from '@/components/ui/button';

interface ErrorStateProps {
  message?: string;
  onRetry?: () => void;
  className?: string;
}

export function ErrorState({
  message = 'Houve um erro ao carregar as informações do dashboard.',
  onRetry,
  className,
}: ErrorStateProps) {
  return (
    <div className={`flex flex-col items-center justify-center p-8 text-center bg-zinc-950/20 border border-zinc-900/80 rounded-2xl backdrop-blur-xl ${className}`}>
      <div className="bg-red-500/10 p-3.5 rounded-full border border-red-500/20 text-red-400 mb-4 animate-pulse">
        <AlertCircle className="h-6 w-6" />
      </div>
      <h3 className="text-base font-bold text-zinc-100 mb-1.5">Ops! Algo deu errado</h3>
      <p className="text-xs text-zinc-400 max-w-sm leading-relaxed mb-5 font-medium">
        {message}
      </p>
      {onRetry && (
        <Button
          onClick={onRetry}
          variant="outline"
          size="sm"
          className="h-8 border-zinc-800 hover:bg-zinc-900 text-zinc-300 font-semibold gap-1.5 transition-all"
        >
          <RefreshCw className="h-3 w-3" />
          Tentar novamente
        </Button>
      )}
    </div>
  );
}
