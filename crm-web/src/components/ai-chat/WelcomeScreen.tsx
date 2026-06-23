'use client';

import { motion } from 'framer-motion';
import {
  BarChart2,
  BookOpen,
  FileText,
  Lightbulb,
  Sparkles,
} from 'lucide-react';

interface WelcomeScreenProps {
  onSuggestion: (text: string) => void;
}

const SUGGESTIONS = [
  {
    icon: BarChart2,
    label: 'Analisar estratégia',
    prompt: 'Analise os pontos fortes e fracos da minha estratégia de marketing.',
  },
  {
    icon: BookOpen,
    label: 'Plano de 30 dias',
    prompt: 'Crie um roteiro de 30 dias para aumentar minha presença no Instagram.',
  },
  {
    icon: FileText,
    label: 'Proposta comercial',
    prompt: 'Me ajude a estruturar uma proposta comercial convincente.',
  },
  {
    icon: Lightbulb,
    label: 'Otimizar conversão',
    prompt: 'Como posso melhorar a taxa de conversão da minha landing page?',
  },
];

const container = {
  hidden: {},
  show: { transition: { staggerChildren: 0.07 } },
};

const item = {
  hidden: { opacity: 0, y: 10 },
  show: { opacity: 1, y: 0, transition: { duration: 0.3 } },
};

export function WelcomeScreen({ onSuggestion }: WelcomeScreenProps) {
  return (
    <div className="flex flex-col items-center justify-center h-full gap-8 px-6 pb-12">
      {/* Hero */}
      <motion.div
        initial={{ opacity: 0, y: -8 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.4, ease: [0.16, 1, 0.3, 1] }}
        className="flex flex-col items-center gap-4 text-center"
      >
        <div
          className="w-14 h-14 rounded-2xl flex items-center justify-center"
          style={{
            background: 'radial-gradient(circle at 30% 30%, rgba(16,185,129,0.3), rgba(16,185,129,0.08))',
            border: '1px solid rgba(16,185,129,0.35)',
            boxShadow: '0 0 24px rgba(16,185,129,0.12)',
          }}
        >
          <Sparkles className="w-6 h-6" style={{ color: '#6EE7B7' }} />
        </div>

        <div>
          <h2
            className="text-xl font-semibold tracking-tight"
            style={{ color: '#F9FAFB' }}
          >
            Como posso ajudar hoje?
          </h2>
          <p
            className="text-sm mt-1.5 max-w-[36ch] leading-relaxed"
            style={{ color: '#9CA3AF' }}
          >
            Use Claude para criar, analisar, planejar e resolver qualquer tarefa do seu negócio.
          </p>
        </div>
      </motion.div>

      {/* Suggestion chips */}
      <motion.div
        variants={container}
        initial="hidden"
        animate="show"
        className="grid grid-cols-1 sm:grid-cols-2 gap-2 w-full max-w-[480px]"
      >
        {SUGGESTIONS.map((s) => {
          const Icon = s.icon;
          return (
            <motion.button
              key={s.label}
              variants={item}
              type="button"
              onClick={() => onSuggestion(s.prompt)}
              className="flex items-start gap-3 text-left px-4 py-3.5 rounded-xl text-sm leading-snug transition-all group"
              style={{
                background: 'rgba(255,255,255,0.04)',
                border: '1px solid rgba(255,255,255,0.1)',
                color: '#D1D5DB',
              }}
              whileHover={{
                background: 'rgba(16,185,129,0.08)',
                borderColor: 'rgba(16,185,129,0.3)',
                scale: 1.01,
              }}
              whileTap={{ scale: 0.98 }}
              transition={{ duration: 0.15 }}
            >
              <Icon
                className="w-4 h-4 flex-shrink-0 mt-0.5 transition-colors"
                style={{ color: 'rgba(110,231,183,0.6)' }}
              />
              <span className="leading-snug">{s.label}</span>
            </motion.button>
          );
        })}
      </motion.div>

      <motion.p
        initial={{ opacity: 0 }}
        animate={{ opacity: 1 }}
        transition={{ delay: 0.5 }}
        className="text-[11px]"
        style={{ color: '#6B7280' }}
      >
        Powered by Claude · Anthropic
      </motion.p>
    </div>
  );
}
