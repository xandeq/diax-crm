import { Archive } from 'lucide-react';

// Os relatórios de auditoria ficavam em /public: qualquer arquivo ali é servido sem login
// (o AuthGuard só protege as rotas do app) e o repo é público. Agora vivem só na máquina local.
export function ArchivedAuditNotice({ period, folder }: { period: string; folder: string }) {
  return (
    <div className="mx-auto max-w-xl rounded-2xl border border-zinc-800/60 bg-zinc-900/40 p-6 text-center">
      <Archive className="mx-auto mb-3 h-8 w-8 text-zinc-500" aria-hidden />
      <h1 className="text-lg font-bold text-zinc-100">Auditoria Financeira — {period}</h1>
      <p className="mt-2 text-sm text-zinc-400">
        Este relatório foi retirado do site porque arquivos estáticos ficavam acessíveis sem login.
      </p>
      <p className="mt-3 text-xs text-zinc-500">
        Cópia local: <code className="rounded bg-black/30 px-1.5 py-0.5">D:\claude-code\financas\auditorias-arquivadas\{folder}\</code>
      </p>
    </div>
  );
}
