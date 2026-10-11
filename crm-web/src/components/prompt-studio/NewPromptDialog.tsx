'use client';

import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { promptTypeOptions } from '@/services/promptGenerator';
import type { CreatePromptInput, PromptCollection } from '@/types/promptStudio';
import { Loader2 } from 'lucide-react';
import { useEffect, useState } from 'react';

/**
 * Esqueletos com {{variáveis}} para começar a partir de um framework. Os textos de
 * "o que é / quando usar" vêm de promptTypeOptions (fonte única, sem cópia).
 */
const STARTERS: Record<string, string> = {
  professional: 'Contexto\n{{contexto}}\n\nObjetivo\n{{objetivo}}\n\nPúblico-alvo\n{{publico}}\n\nInstruções principais\n- \n\nRegras e restrições\n- \n\nFormato da resposta\n{{formato}}',
  rtf: 'PAPEL: Você é {{papel}}.\nTAREFA: {{tarefa}}\nFORMATO: {{formato}}',
  risen: 'PAPEL: Atue como {{papel}}.\nINSTRUÇÕES: {{instrucoes}}\nETAPAS:\n1. \n2. \n3. \nOBJETIVO FINAL: {{objetivo}}\nRESTRIÇÕES: {{restricoes}}',
  costar: 'CONTEXTO: {{contexto}}\nOBJETIVO: {{objetivo}}\nESTILO: {{estilo}}\nTOM: {{tom}}\nPÚBLICO: {{publico}}\nFORMATO: {{formato}}',
  tag: 'TAREFA: {{tarefa}}\nAÇÃO: {{acao}}\nOBJETIVO: {{objetivo}}',
  pas: 'PROBLEMA: {{problema}}\nAGITAR: {{agitar}}\nSOLUÇÃO: {{solucao}}\nCHAMADA PARA AÇÃO: {{cta}}',
  aida: 'ATENÇÃO: {{atencao}}\nINTERESSE: {{interesse}}\nDESEJO: {{desejo}}\nAÇÃO: {{acao}}',
  bab: 'ANTES (situação atual): {{antes}}\nDEPOIS (resultado desejado): {{depois}}\nPONTE (como chegar): {{ponte}}',
  fab: 'CARACTERÍSTICAS: {{caracteristicas}}\nVANTAGENS: {{vantagens}}\nBENEFÍCIOS: {{beneficios}}',
  cot: '{{tarefa}}\n\nAntes de responder:\n1. Analise passo a passo\n2. Explique seu raciocínio em cada etapa\n3. Justifique as conclusões parciais\n4. Só então apresente a resposta final',
  tot: '{{problema}}\n\nGere 3 abordagens diferentes. Para cada uma: descrição, prós, contras e viabilidade.\nCompare as 3 e recomende a melhor, justificando.',
  fsp: 'Aprenda o padrão a partir destes exemplos:\n\nEntrada: {{exemplo1_entrada}} → Saída: {{exemplo1_saida}}\nEntrada: {{exemplo2_entrada}} → Saída: {{exemplo2_saida}}\n\nAgora aplique o mesmo padrão a:\n{{entrada}}',
  sref: '{{tarefa}}\n\n1. GERAR: escreva uma primeira versão.\n2. CRITICAR: procure tom inadequado, erros factuais, falta de clareza.\n3. REFINAR: entregue só a versão final corrigida.',
  deep_research: 'Atue como pesquisador sênior em {{area}}.\n\nObjetivo da pesquisa: {{pergunta}}\nEscopo: {{escopo}}\n\nPlaneje antes de pesquisar. Use múltiplas fontes confiáveis, compare perspectivas, separe consenso de divergência.\n\nEntregue: sumário executivo, seções com títulos, tabela comparativa, conclusões com evidência e lista de fontes com data.',
  context_objective: '**Situação**\n{{situacao}}\n\n**Tarefa**\n{{tarefa}}\n\n**Objetivo**\n{{objetivo}}\n\n**Conhecimento**\n- \n\n**Instruções**\n1. ',
};

interface Props {
  open: boolean;
  collections: PromptCollection[];
  defaultCollectionId: string | null;
  creating: boolean;
  onClose: () => void;
  onCreate: (input: CreatePromptInput) => void;
}

export function NewPromptDialog({ open, collections, defaultCollectionId, creating, onClose, onCreate }: Props) {
  const [title, setTitle] = useState('');
  const [framework, setFramework] = useState('');
  const [collectionId, setCollectionId] = useState<string>('');

  useEffect(() => {
    if (open) { setTitle(''); setFramework(''); setCollectionId(defaultCollectionId ?? ''); }
  }, [open, defaultCollectionId]);

  const fw = promptTypeOptions.find(f => f.value === framework);

  const submit = (e: React.FormEvent) => {
    e.preventDefault();
    const t = title.trim();
    if (!t) return;
    const starter = framework ? STARTERS[framework] : undefined;
    onCreate({
      title: t,
      content: starter ?? (fw ? `${fw.label}\n\n{{tarefa}}` : 'Escreva aqui o seu prompt.\n\nUse {{variavel}} para campos que mudam a cada uso.'),
      framework: framework || null,
      kind: starter ? 'template' : 'prompt',
      collectionId: collectionId || null,
      changeNote: fw ? `Criado a partir do framework ${fw.label}` : 'Versão inicial',
    });
  };

  return (
    <Dialog open={open} onOpenChange={o => { if (!o) onClose(); }}>
      <DialogContent className="max-w-lg bg-[#0D1F18] border-white/10 text-slate-200">
        <DialogHeader>
          <DialogTitle className="text-slate-50">Novo prompt</DialogTitle>
          <DialogDescription className="text-slate-400">Comece em branco ou a partir de um dos frameworks do Gerador.</DialogDescription>
        </DialogHeader>
        <form onSubmit={submit} className="flex flex-col gap-4">
          <div className="flex flex-col gap-1.5">
            <label htmlFor="ps-new-title" className="text-xs font-medium text-slate-300">Título</label>
            <input id="ps-new-title" autoFocus required maxLength={160} value={title} onChange={e => setTitle(e.target.value)}
              placeholder="Ex.: Revisor de código sênior"
              className="h-10 rounded-lg border border-white/10 bg-[#0F1A14] px-3 text-sm text-slate-50 outline-none focus:border-emerald-500" />
          </div>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div className="flex flex-col gap-1.5">
              <label htmlFor="ps-new-fw" className="text-xs font-medium text-slate-300">Começar de</label>
              <select id="ps-new-fw" value={framework} onChange={e => setFramework(e.target.value)}
                className="h-10 rounded-lg border border-white/10 bg-[#0F1A14] px-2 text-sm text-slate-100 outline-none focus:border-emerald-500">
                <option value="">Em branco</option>
                {promptTypeOptions.map(f => <option key={f.value} value={f.value}>{f.label}</option>)}
              </select>
            </div>
            <div className="flex flex-col gap-1.5">
              <label htmlFor="ps-new-coll" className="text-xs font-medium text-slate-300">Coleção</label>
              <select id="ps-new-coll" value={collectionId} onChange={e => setCollectionId(e.target.value)}
                className="h-10 rounded-lg border border-white/10 bg-[#0F1A14] px-2 text-sm text-slate-100 outline-none focus:border-emerald-500">
                <option value="">Sem coleção</option>
                {collections.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
              </select>
            </div>
          </div>
          {fw && (
            <div className="rounded-lg border border-white/10 bg-white/[0.03] p-3 text-xs leading-relaxed text-slate-300">
              <p className="mb-1"><strong className="text-slate-100">O que é:</strong> {fw.whatIs}</p>
              <p><strong className="text-slate-100">Quando usar:</strong> {fw.whenToUse}</p>
              {!STARTERS[fw.value] && <p className="mt-1 text-slate-400">Este framework começa com um esqueleto mínimo.</p>}
            </div>
          )}
          <div className="flex justify-end gap-2">
            <button type="button" className="h-9 rounded-lg px-3 text-sm text-slate-300 hover:bg-white/5" onClick={onClose}>Cancelar</button>
            <button type="submit" disabled={!title.trim() || creating}
              className="inline-flex h-9 items-center gap-2 rounded-lg bg-emerald-500 px-4 text-sm font-semibold text-[#04140D] hover:bg-emerald-600 disabled:opacity-50">
              {creating && <Loader2 size={14} className="animate-spin" />} Criar prompt
            </button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
}
