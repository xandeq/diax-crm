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
        <div
          className="w-12 h-12 rounded-2xl flex items-center justify-center"
          style={{ background: 'rgba(16,185,129,0.15)', border: '1px solid rgba(16,185,129,0.3)' }}
        >
          <Sparkles className="w-5 h-5" style={{ color: '#6EE7B7' }} />
        </div>
        <div>
          <h2 className="text-lg font-semibold tracking-tight" style={{ color: '#F9FAFB' }}>Como posso ajudar?</h2>
          <p className="text-sm mt-1 max-w-[38ch]" style={{ color: '#9CA3AF' }}>
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
            className="text-left px-4 py-3 rounded-xl text-sm leading-snug transition-all active:scale-[0.98] hover:brightness-110"
            style={{ background: 'rgba(255,255,255,0.07)', border: '1px solid rgba(255,255,255,0.18)', color: '#E5E7EB' }}
          >
            {s}
          </button>
        ))}
      </div>
    </div>
  );
}
