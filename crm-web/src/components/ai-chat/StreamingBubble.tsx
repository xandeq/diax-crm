'use client';

import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';

interface StreamingBubbleProps {
  content: string;
}

export function StreamingBubble({ content }: StreamingBubbleProps) {
  return (
    <div className="flex gap-3 flex-row">
      {/* Avatar */}
      <div className="w-7 h-7 rounded-full flex-shrink-0 flex items-center justify-center text-[10px] font-bold mt-0.5 bg-zinc-100 text-zinc-600 border border-zinc-200 select-none">
        AI
      </div>

      {/* Content or thinking dots */}
      <div className="max-w-[78%] text-sm leading-relaxed text-zinc-800">
        {content ? (
          <div className="prose prose-sm prose-zinc max-w-none">
            <ReactMarkdown remarkPlugins={[remarkGfm]}>
              {content}
            </ReactMarkdown>
            {/* Blinking cursor */}
            <span className="inline-block w-0.5 h-4 bg-zinc-500 animate-pulse ml-0.5 align-text-bottom" />
          </div>
        ) : (
          <div className="flex items-center gap-1 py-1.5">
            <span className="w-1.5 h-1.5 rounded-full bg-zinc-300 animate-bounce [animation-delay:0ms]" />
            <span className="w-1.5 h-1.5 rounded-full bg-zinc-300 animate-bounce [animation-delay:150ms]" />
            <span className="w-1.5 h-1.5 rounded-full bg-zinc-300 animate-bounce [animation-delay:300ms]" />
          </div>
        )}
      </div>
    </div>
  );
}
