'use client';

import { ownValue, segmentTemplate } from '@/lib/promptTemplate';
import type { PromptVariable, PromptVariableType } from '@/types/promptStudio';
import { Check, Copy, Settings2 } from 'lucide-react';
import { useState } from 'react';

interface Props {
  content: string;
  variables: PromptVariable[];
  values: Record<string, string>;
  copied: boolean;
  onValue: (name: string, value: string) => void;
  onClear: () => void;
  onCopy: () => void;
  onVariableMeta: (v: PromptVariable) => void;
}

const TYPE_LABELS: Record<PromptVariableType, string> = { text: 'Texto', textarea: 'Texto longo', select: 'Lista', number: 'Número' };

export function VariablesPanel(p: Props) {
  const [editing, setEditing] = useState<string | null>(null);
  const own = (name: string) => ownValue(p.values, name);
  const valueOf = (v: PromptVariable) => own(v.name) || v.default || '';
  const missing = p.variables.filter(v => v.required !== false && !valueOf(v));
  const byName = new Map(p.variables.map(v => [v.name, v]));

  return (
    <aside className="ps-vars ps-pane" aria-label="Variáveis e prompt renderizado">
      {/* Ações no topo: o toast global (canto inferior direito) não cobre o botão principal. */}
      <div className="ps-vars-head">
        <div style={{ display: 'flex', flexDirection: 'column', gap: 2, minWidth: 0 }}>
          <h3>Variáveis</h3>
          {p.variables.length > 0 && (
            <span className={`ps-missing ${missing.length ? '' : 'ok'}`}>
              {missing.length ? `${missing.length} faltando` : 'Tudo preenchido'}
            </span>
          )}
        </div>
        <div style={{ display: 'flex', gap: 6 }}>
          <button className="ps-btn ghost" onClick={p.onClear} disabled={Object.keys(p.values).length === 0}>Limpar</button>
          <button className="ps-btn primary" onClick={p.onCopy} title="Copiar renderizado (Ctrl+Enter)">
            {p.copied ? <Check size={14} /> : <Copy size={14} />} {p.copied ? 'Copiado' : 'Copiar'}
          </button>
        </div>
      </div>

      <div className="ps-fields">
        {p.variables.length === 0 && (
          <p className="ps-novars">
            Sem variáveis. Escreva <code style={{ fontFamily: 'var(--ps-mono)', color: 'var(--ps-var)' }}>{'{{nome}}'}</code> no prompt para criar um campo.
          </p>
        )}
        {p.variables.map(v => {
          const id = `ps-var-${v.name}`;
          const empty = v.required !== false && !valueOf(v);
          const common = {
            id,
            value: own(v.name) ?? '',
            placeholder: v.default ?? '',
            'aria-invalid': empty || undefined,
          };
          return (
            <div key={v.name} className={`ps-field ${empty ? 'req-empty' : ''}`}>
              <label htmlFor={id}>
                <span>{v.label || v.name}{v.required === false && <span style={{ color: 'var(--ps-t3)', fontWeight: 400 }}> · opcional</span>}</span>
                <span style={{ display: 'inline-flex', gap: 6, alignItems: 'center' }}>
                  <code>{`{{${v.name}}}`}</code>
                  <button type="button" className="gear" aria-expanded={editing === v.name}
                    aria-label={`Configurar variável ${v.name}`} onClick={() => setEditing(e => (e === v.name ? null : v.name))}>
                    <Settings2 size={13} />
                  </button>
                </span>
              </label>
              {v.type === 'textarea' ? (
                <textarea {...common} onChange={e => p.onValue(v.name, e.target.value)} />
              ) : v.type === 'select' && v.options?.length ? (
                <select {...common} value={own(v.name) ?? v.default ?? ''} onChange={e => p.onValue(v.name, e.target.value)}>
                  <option value="">Escolha…</option>
                  {v.options.map(o => <option key={o}>{o}</option>)}
                </select>
              ) : (
                <input {...common} type={v.type === 'number' ? 'number' : 'text'} onChange={e => p.onValue(v.name, e.target.value)} />
              )}
              {editing === v.name && <VariableMetaEditor v={v} onChange={p.onVariableMeta} />}
            </div>
          );
        })}
      </div>

      <div className="ps-rend-h">Renderizado</div>
      <div className="ps-rendered" aria-label="Prompt com variáveis preenchidas">
        {segmentTemplate(p.content).map((s, i) => {
          if (s.kind === 'text') return <span key={i}>{s.text}</span>;
          if (s.escaped) return <span key={i}>{`{{${s.name}}}`}</span>;
          const meta = byName.get(s.name);
          const val = own(s.name) || meta?.default || '';
          return val ? <span key={i} className="ok">{val}</span> : <span key={i} className="miss">{s.raw}</span>;
        })}
      </div>
    </aside>
  );
}

function VariableMetaEditor({ v, onChange }: { v: PromptVariable; onChange: (v: PromptVariable) => void }) {
  const [options, setOptions] = useState((v.options ?? []).join(', '));
  return (
    <div className="ps-varmeta">
      <input className="full" aria-label="Rótulo" placeholder="Rótulo (ex.: Nome do cliente)" defaultValue={v.label ?? ''}
        onBlur={e => onChange({ ...v, label: e.target.value.trim() || null })} />
      <select aria-label="Tipo" value={v.type} onChange={e => onChange({ ...v, type: e.target.value as PromptVariableType })}>
        {(Object.keys(TYPE_LABELS) as PromptVariableType[]).map(t => <option key={t} value={t}>{TYPE_LABELS[t]}</option>)}
      </select>
      <input aria-label="Valor padrão" placeholder="Padrão" defaultValue={v.default ?? ''}
        onBlur={e => onChange({ ...v, default: e.target.value || null })} />
      {v.type === 'select' && (
        <input className="full" aria-label="Opções separadas por vírgula" placeholder="Opções: A, B, C" value={options}
          onChange={e => setOptions(e.target.value)}
          onBlur={() => onChange({ ...v, options: options.split(',').map(o => o.trim()).filter(Boolean) })} />
      )}
      <label className="chk full">
        <input type="checkbox" checked={v.required !== false} onChange={e => onChange({ ...v, required: e.target.checked })} />
        Obrigatória
      </label>
      <p className="full" style={{ margin: 0, fontSize: 11, color: 'var(--ps-t3)' }}>Vale a partir da próxima versão salva.</p>
    </div>
  );
}
