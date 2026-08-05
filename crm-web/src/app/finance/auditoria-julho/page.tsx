// Aba "Auditoria Jul/Ago 2026" — embute o dashboard estático de auditoria financeira
// (arquivos em /public/auditoria-jul-ago-2026/, self-contained, sem chamadas de API).
// Aditivo: não altera nenhum comportamento existente do módulo financeiro.

export default function AuditoriaJulhoPage() {
  return (
    <div className="w-full">
      <div className="mb-4 flex items-center justify-between">
        <div>
          <h1 className="text-lg font-bold text-zinc-100">Auditoria Financeira — Jul/Ago 2026</h1>
          <p className="text-xs text-zinc-500">
            5 faturas reais conciliadas, extrato Santander, parcelamentos futuros (R$ 26,7k) e oportunidades · dados do ciclo (atualizado mensalmente)
          </p>
        </div>
        <div className="flex items-center gap-4">
          <a
            href="/auditoria-jul-ago-2026/relatorio.html"
            target="_blank"
            rel="noopener noreferrer"
            className="text-xs font-semibold text-[#00D4AA] hover:underline whitespace-nowrap"
          >
            relatório ↗
          </a>
          <a
            href="/auditoria-jul-ago-2026/index.html"
            target="_blank"
            rel="noopener noreferrer"
            className="text-xs font-semibold text-[#00D4AA] hover:underline whitespace-nowrap"
          >
            abrir em nova aba ↗
          </a>
        </div>
      </div>
      <iframe
        src="/auditoria-jul-ago-2026/index.html"
        title="Auditoria Financeira Jul/Ago 2026"
        className="w-full rounded-2xl border border-zinc-800/60 bg-[#0a130f]/40 shadow-lg shadow-black/10"
        style={{ height: 'calc(100vh - 180px)', minHeight: 620 }}
      />
    </div>
  );
}
