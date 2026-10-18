'use client';

import { Button } from '@/components/ui/button';
import { AlertTriangle, LayoutDashboard, RotateCcw } from 'lucide-react';
import Link from 'next/link';
import { useEffect } from 'react';

// Sem este boundary, qualquer exceção de render (ex.: API devolvendo um formato inesperado)
// virava a tela "Application error: a client-side exception" sem saída. Aqui o layout e a
// sidebar continuam de pé.
export default function RouteError({ error, reset }: { error: Error & { digest?: string }; reset: () => void }) {
  useEffect(() => {
    console.error('[route-error]', error);
  }, [error]);

  return (
    <div className="flex min-h-[60vh] items-center justify-center p-4">
      <div className="w-full max-w-md rounded-2xl border border-zinc-800/60 bg-zinc-900/40 p-6 text-center">
        <AlertTriangle className="mx-auto mb-3 h-8 w-8 text-amber-400" aria-hidden />
        <h1 className="text-lg font-bold text-zinc-100">Esta tela encontrou um erro</h1>
        <p className="mt-2 text-sm text-zinc-400">
          Algo inesperado aconteceu ao montar a página. Os dados não foram alterados.
        </p>
        {error.message && (
          <p className="mt-3 break-words rounded bg-black/30 px-3 py-2 text-left font-mono text-xs text-zinc-500">
            {error.message}
          </p>
        )}
        <div className="mt-5 flex flex-wrap justify-center gap-3">
          <Button onClick={reset} variant="outline" className="border-zinc-700">
            <RotateCcw className="mr-2 h-4 w-4" />
            Tentar de novo
          </Button>
          <Button asChild>
            <Link href="/dashboard/">
              <LayoutDashboard className="mr-2 h-4 w-4" />
              Ir ao dashboard
            </Link>
          </Button>
        </div>
      </div>
    </div>
  );
}
