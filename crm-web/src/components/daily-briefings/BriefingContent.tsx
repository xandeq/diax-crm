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
 * - html: email (tables + inline styles, fundo proprio escuro) via dangerouslySetInnerHTML, centralizado
 * - markdown: ReactMarkdown + GFM com tipografia clara para fundo escuro (.briefing-md, definida no client)
 * Conteudo e first-party (gerado pelos nossos agentes).
 */
export function BriefingContent({ content, format }: Props) {
  if (format === 'markdown') {
    return (
      <div className="briefing-md">
        <ReactMarkdown remarkPlugins={[remarkGfm]}>{content}</ReactMarkdown>
      </div>
    );
  }

  // HTML de email: largura ~640px, fundo proprio. Centraliza e arredonda.
  return (
    <div className="briefing-html">
      <div dangerouslySetInnerHTML={{ __html: content }} />
    </div>
  );
}
