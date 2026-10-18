import { Button } from '@/components/ui/button';
import { Compass, LayoutDashboard } from 'lucide-react';
import Link from 'next/link';

export default function NotFound() {
  return (
    <div className="flex min-h-[60vh] items-center justify-center p-4">
      <div className="w-full max-w-md rounded-2xl border border-zinc-800/60 bg-zinc-900/40 p-6 text-center">
        <Compass className="mx-auto mb-3 h-8 w-8 text-zinc-500" aria-hidden />
        <h1 className="text-lg font-bold text-zinc-100">Página não encontrada</h1>
        <p className="mt-2 text-sm text-zinc-400">O endereço não existe ou mudou de lugar. Use o menu ou volte ao dashboard.</p>
        <Button asChild className="mt-5">
          <Link href="/dashboard/">
            <LayoutDashboard className="mr-2 h-4 w-4" />
            Ir ao dashboard
          </Link>
        </Button>
      </div>
    </div>
  );
}
