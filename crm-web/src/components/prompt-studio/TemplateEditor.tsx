'use client';

import { segmentTemplate } from '@/lib/promptTemplate';
import { forwardRef, useMemo, useRef } from 'react';

interface Props {
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
}

/**
 * Textarea com camada de destaque para {{variáveis}} (técnica de overlay: o texto real é
 * transparente sobre um <pre> com o mesmo layout). Sem dependências; o undo nativo funciona.
 */
export const TemplateEditor = forwardRef<HTMLTextAreaElement, Props>(function TemplateEditor(
  { value, onChange, placeholder },
  ref
) {
  const preRef = useRef<HTMLPreElement>(null);
  const segments = useMemo(() => segmentTemplate(value), [value]);

  return (
    <div className="ps-ed-wrap">
      <pre ref={preRef} aria-hidden="true">
        {segments.map((s, i) =>
          s.kind === 'text' ? (
            <span key={i}>{s.text}</span>
          ) : (
            <span key={i} className={s.escaped ? 'ps-hv-esc' : 'ps-hv'}>{s.raw}</span>
          )
        )}
        {'\n'}
      </pre>
      <label className="sr-only" htmlFor="ps-content">Conteúdo do prompt</label>
      <textarea
        id="ps-content"
        ref={ref}
        value={value}
        spellCheck={false}
        placeholder={placeholder}
        onChange={e => onChange(e.target.value)}
        onScroll={e => {
          if (preRef.current) preRef.current.scrollTop = e.currentTarget.scrollTop;
        }}
        onKeyDown={e => {
          // Tab insere 2 espaços; Esc libera o foco (não prende o teclado no editor).
          if (e.key === 'Tab' && !e.shiftKey && !e.ctrlKey && !e.metaKey) {
            e.preventDefault();
            const t = e.currentTarget;
            const { selectionStart: s, selectionEnd: end } = t;
            const next = value.slice(0, s) + '  ' + value.slice(end);
            onChange(next);
            requestAnimationFrame(() => { t.selectionStart = t.selectionEnd = s + 2; });
          } else if (e.key === 'Escape') {
            e.currentTarget.blur();
          }
        }}
      />
    </div>
  );
});
