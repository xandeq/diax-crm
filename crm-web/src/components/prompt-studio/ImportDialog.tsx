'use client';

import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { promptStudioService as svc } from '@/services/promptStudio';
import type { ImportAction, ImportReport, ImportStrategy } from '@/types/promptStudio';
import { AlertTriangle, FileUp, Loader2, X } from 'lucide-react';
import { useEffect, useState } from 'react';
import { useDropzone } from 'react-dropzone';
import { toast } from 'sonner';

interface Props {
  open: boolean;
  onClose: () => void;
  onImported: (report: ImportReport) => void;
}

const ACTION_LABEL: Record<ImportAction, { label: string; color: string }> = {
  create: { label: 'Criar', color: '#34D399' },
  newVersion: { label: 'Nova versão', color: '#38BDF8' },
  unchanged: { label: 'Sem mudança', color: '#9CA3AF' },
  skip: { label: 'Pular', color: '#9CA3AF' },
  error: { label: 'Erro', color: '#F87171' },
};

const STRATEGY_HELP: Record<ImportStrategy, string> = {
  'new-version': 'Se já existir (mesmo id ou slug), salva como nova versão. Nada é sobrescrito.',
  skip: 'Se já existir, ignora o arquivo.',
  duplicate: 'Sempre cria um prompt novo, mesmo que já exista.',
};

/** Import em 2 passos: prévia (dry-run, automática) → confirmar. */
export function ImportDialog({ open, onClose, onImported }: Props) {
  const [files, setFiles] = useState<File[]>([]);
  const [strategy, setStrategy] = useState<ImportStrategy>('new-version');
  const [preview, setPreview] = useState<ImportReport | null>(null);
  const [loading, setLoading] = useState(false);
  const [importing, setImporting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!open) { setFiles([]); setPreview(null); setError(null); setStrategy('new-version'); }
  }, [open]);

  useEffect(() => {
    if (!files.length) { setPreview(null); return; }
    let cancelled = false;
    setLoading(true);
    setError(null);
    svc.importPrompts(files, { dryRun: true, strategy })
      .then(r => { if (!cancelled) setPreview(r); })
      .catch(e => { if (!cancelled) { setPreview(null); setError(e instanceof Error ? e.message : 'Falha na prévia'); } })
      .finally(() => { if (!cancelled) setLoading(false); });
    return () => { cancelled = true; };
  }, [files, strategy]);

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    accept: { 'text/markdown': ['.md', '.markdown'], 'text/plain': ['.txt'], 'application/zip': ['.zip'] },
    maxFiles: 50,
    maxSize: 5 * 1024 * 1024,
    onDrop: (accepted, rejected) => {
      if (rejected.length) toast.warning(`${rejected.length} arquivo(s) ignorado(s): use .md, .txt ou .zip até 5 MB.`);
      setFiles(prev => {
        const byName = new Map(prev.map(f => [f.name, f]));
        accepted.forEach(f => byName.set(f.name, f));
        return [...byName.values()];
      });
    },
  });

  const writable = preview ? preview.summary.create + preview.summary.newVersion : 0;

  const confirm = async () => {
    setImporting(true);
    try {
      const report = await svc.importPrompts(files, { dryRun: false, strategy });
      const s = report.summary;
      toast.success(`Importado: ${s.create} novo(s), ${s.newVersion} nova(s) versão(ões)${s.errors ? `, ${s.errors} erro(s)` : ''}.`);
      onImported(report);
      onClose();
    } catch (e) {
      toast.error(e instanceof Error ? `Falha ao importar: ${e.message}` : 'Falha ao importar.');
    } finally {
      setImporting(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={o => { if (!o) onClose(); }}>
      <DialogContent className="max-w-2xl bg-[#0D1F18] border-white/10 text-slate-200">
        <DialogHeader>
          <DialogTitle className="text-slate-50">Importar prompts</DialogTitle>
          <DialogDescription className="text-slate-400">
            Arquivos .md (do Prompt Studio ou commands do Claude Code), .txt ou um .zip com vários. Primeiro mostramos a prévia; nada é gravado até você confirmar.
          </DialogDescription>
        </DialogHeader>

        <div {...getRootProps()}
          className={`flex cursor-pointer flex-col items-center gap-2 rounded-xl border border-dashed px-4 py-6 text-center text-sm transition-colors ${isDragActive ? 'border-emerald-400 bg-emerald-500/10' : 'border-white/15 hover:border-white/30 bg-white/[0.02]'}`}>
          <input {...getInputProps()} aria-label="Escolher arquivos para importar" />
          <FileUp size={22} className="text-slate-400" />
          <span className="text-slate-300">{isDragActive ? 'Solte aqui' : 'Arraste arquivos ou clique para escolher'}</span>
          <span className="text-xs text-slate-500">.md · .txt · .zip — até 50 arquivos, 5 MB cada</span>
        </div>

        {files.length > 0 && (
          <div className="flex flex-wrap gap-1.5">
            {files.map(f => (
              <span key={f.name} className="inline-flex items-center gap-1 rounded-md border border-white/10 px-2 py-0.5 text-xs text-slate-300">
                {f.name}
                <button aria-label={`Remover ${f.name}`} className="opacity-60 hover:opacity-100"
                  onClick={() => setFiles(prev => prev.filter(x => x.name !== f.name))}><X size={11} /></button>
              </span>
            ))}
          </div>
        )}

        <div className="flex flex-col gap-1.5">
          <label htmlFor="ps-import-strategy" className="text-xs font-medium text-slate-300">Se o prompt já existir</label>
          <select id="ps-import-strategy" value={strategy} onChange={e => setStrategy(e.target.value as ImportStrategy)}
            className="h-9 rounded-lg border border-white/10 bg-[#0F1A14] px-2 text-sm text-slate-100 outline-none focus:border-emerald-500">
            <option value="new-version">Criar nova versão</option>
            <option value="skip">Pular</option>
            <option value="duplicate">Duplicar</option>
          </select>
          <span className="text-xs text-slate-500">{STRATEGY_HELP[strategy]}</span>
        </div>

        {error && (
          <div role="alert" className="flex items-center gap-2 rounded-lg border border-red-400/30 bg-red-500/10 px-3 py-2 text-sm text-red-200">
            <AlertTriangle size={14} /> {error}
          </div>
        )}

        {(loading || preview) && (
          <div className="rounded-xl border border-white/10" aria-live="polite">
            <div className="flex flex-wrap items-center gap-3 border-b border-white/10 px-3 py-2 text-xs text-slate-400">
              {loading ? (
                <span className="inline-flex items-center gap-2"><Loader2 size={13} className="animate-spin" /> Gerando prévia…</span>
              ) : preview && (
                <>
                  <span className="font-semibold text-slate-200">Prévia</span>
                  {(['create', 'newVersion', 'unchanged', 'skip', 'error'] as ImportAction[]).map(a => {
                    const n = { create: preview.summary.create, newVersion: preview.summary.newVersion, unchanged: preview.summary.unchanged, skip: preview.summary.skip, error: preview.summary.errors }[a];
                    return n ? <span key={a} style={{ color: ACTION_LABEL[a].color }}>{ACTION_LABEL[a].label}: {n}</span> : null;
                  })}
                </>
              )}
            </div>
            {preview && !loading && (
              <ul className="max-h-64 divide-y divide-white/5 overflow-y-auto text-sm">
                {preview.items.map((item, i) => (
                  <li key={`${item.file}-${i}`} className="grid grid-cols-[100px_1fr] gap-2 px-3 py-2">
                    <span className="text-xs font-semibold" style={{ color: ACTION_LABEL[item.action].color }}>{ACTION_LABEL[item.action].label}</span>
                    <span className="min-w-0">
                      <span className="block truncate text-slate-200">{item.title ?? item.file}</span>
                      <span className="block truncate font-mono text-[11px] text-slate-500">{item.file}{item.slug ? ` → ${item.slug}` : ''}</span>
                      {item.error && <span className="block text-xs text-red-300">{item.error}</span>}
                      {item.warnings.map(w => <span key={w} className="block text-xs text-amber-300/90">{w}</span>)}
                    </span>
                  </li>
                ))}
              </ul>
            )}
          </div>
        )}

        <div className="flex justify-end gap-2">
          <button type="button" className="h-9 rounded-lg px-3 text-sm text-slate-300 hover:bg-white/5" onClick={onClose}>Cancelar</button>
          <button type="button" disabled={!preview || loading || importing || writable === 0} onClick={() => void confirm()}
            className="inline-flex h-9 items-center gap-2 rounded-lg bg-emerald-500 px-4 text-sm font-semibold text-[#04140D] hover:bg-emerald-600 disabled:opacity-50">
            {importing && <Loader2 size={14} className="animate-spin" />}
            {writable ? `Importar ${writable}` : 'Nada para importar'}
          </button>
        </div>
      </DialogContent>
    </Dialog>
  );
}
