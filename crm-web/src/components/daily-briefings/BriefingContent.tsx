'use client';

import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import { BriefingFormat } from '@/types/dailyBriefings';

interface Props {
  content: string;
  format: BriefingFormat;
}

/**
 * Renderiza o briefing conforme o formato:
 * - html: conteúdo de email (tables + inline styles, geralmente dark) via dangerouslySetInnerHTML
 * - markdown: ReactMarkdown + GFM dentro de prose
 * Conteúdo é first-party (gerado pelos nossos agentes).
 */
export function BriefingContent({ content, format }: Props) {
  if (format === 'markdown') {
    return (
      <div className="prose prose-sm prose-slate max-w-none px-1">
        <ReactMarkdown remarkPlugins={[remarkGfm]}>{content}</ReactMarkdown>
      </div>
    );
  }

  // HTML de email: largura ~620px, fundo próprio escuro. Centraliza e permite scroll horizontal.
  return (
    <div className="w-full overflow-x-auto">
      <div
        className="mx-auto"
        style={{ maxWidth: 640 }}
        dangerouslySetInnerHTML={{ __html: content }}
      />
    </div>
  );
}
