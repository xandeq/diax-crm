'use client';

import { Sparkles } from 'lucide-react';
import { MODEL_OPTIONS } from '@/types/ai-chat';

interface WelcomeScreenProps {
  onSuggestion: (text: string) => void;
}

const SUGGESTIONS = [
  'Analise os pontos fortes e fracos da minha estratégia de marketing.',
  'Crie um roteiro de 30 dias para aumentar minha presença no Instagram.',
  'Me ajude a estruturar uma proposta comercial convincente.',
  'Como posso melhorar a taxa de conversão da minha landing page?',
];

export function WelcomeScreen({ onSuggestion }: WelcomeScreenProps) {
  return (
    <div className="flex flex-col items-center justify-center h-full gap-8 px-8 pb-12">
      <div className="flex flex-col items-center gap-3 text-center">
        <div className="w-12 h-12 rounded-2xl bg-zinc-900 flex items-center justify-center shadow-sm">
          <Sparkles className="w-5 h-5 text-zinc-100" />
        </div>
        <div>
          <h2 className="text-lg font-semibold tracking-tight text-zinc-900">Como posso ajudar?</h2>
          <p className="text-sm text-zinc-500 mt-1 max-w-[38ch]">
            Use Claude para criar, analisar, planejar e resolver qualquer tarefa.
          </p>
        </div>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-2 w-full max-w-lg">
        {SUGGESTIONS.map((s, i) => (
          <button
            key={i}
            type="button"
            onClick={() => onSuggestion(s)}
            className="text-left px-4 py-3 rounded-xl text-sm leading-snug transition-all active:scale-[0.98]"
            style={{ background: 'rgba(255,255,255,0.05)', border: '1px solid rgba(255,255,255,0.1)', color: '#D1D5DB' }}
          >
            {s}
          </button>
        ))}
      </div>
    </div>
  );
}
