'use client';

import { diffText } from '@/lib/promptTemplate';
import { promptStudioService } from '@/services/promptStudio';
import type { PromptVersion } from '@/types/promptStudio';
import { useQuery } from '@tanstack/react-query';
import { Loader2, RotateCcw } from 'lucide-react';
import { useMemo, useState } from 'react';
import { relativeTime, SOURCE_LABELS } from './shared';

interface Props {
  promptId: string;
  current: PromptVersion;
  onRestore: (number: number) => void;
  restoring: boolean;
}

export function VersionHistory({ promptId, current, onRestore, restoring }: Props) {
  const versionsQ = useQuery({
    queryKey: ['prompt-versions', promptId, current.number],
    queryFn: () => promptStudioService.versions(promptId),
  });
  const [picked, setPicked] = useState<number | null>(null);
  const compare = picked && picked < current.number ? picked : Math.max(1, current.number - 1);

  const oldQ = useQuery({
    queryKey: ['prompt-version', promptId, compare],
    queryFn: () => promptStudioService.version(promptId, compare),
    enabled: current.number > 1,
    staleTime: Infinity, // versões são imutáveis
  });

  const diff = useMemo(
    () => (oldQ.data ? diffText(oldQ.data.content, current.content) : null),
    [oldQ.data, current.content]
  );

  return (
    <div className="ps-hist">
      <ul className="ps-vlist" aria-label="Versões">
        {versionsQ.isLoading && <li className="ps-skel" />}
        {versionsQ.data?.map(v => (
          <li key={v.number}>
            <button className="ps-vbtn" aria-pressed={v.number === compare}
              disabled={v.number === current.number}
              onClick={() => setPicked(v.number)}
              title={v.number === current.number ? 'Versão atual' : `Comparar v${v.number} com a atual`}>
              <span className="ps-vn">v{v.number}</span>
              <span className="ps-vnote">{v.changeNote || 'Sem nota'}</span>
              <span className="ps-vmeta">
                <span className="ps-src">{SOURCE_LABELS[v.source] ?? v.source}</span> · {relativeTime(v.createdAt)}
                {v.number === current.number && ' · atual'}
              </span>
            </button>
          </li>
        ))}
      </ul>

      <div className="ps-diffbox">
        <div className="ps-diffbar">
          {current.number === 1 ? (
            <span>Só existe a v1. As próximas versões aparecem aqui com o diff.</span>
          ) : (
            <>
              <span>v{compare} → v{current.number}</span>
              {diff && <><span className="ps-plus">+{diff.added}</span><span className="ps-minus">−{diff.removed}</span></>}
              <span className="sp" />
              <button className="ps-btn" disabled={restoring} onClick={() => onRestore(compare)}>
                {restoring ? <Loader2 size={14} className="animate-spin" /> : <RotateCcw size={14} />}
                Restaurar v{compare} como v{current.number + 1}
              </button>
            </>
          )}
        </div>
        <div className="ps-diff" aria-live="polite">
          {current.number === 1 ? current.content : oldQ.isLoading ? 'Carregando…' : diff?.parts.map((part, i) =>
            part.type === 'same' ? <span key={i}>{part.text}</span>
              : part.type === 'add' ? <ins key={i}>{part.text}</ins>
                : <del key={i}>{part.text}</del>
          )}
        </div>
      </div>
    </div>
  );
}
