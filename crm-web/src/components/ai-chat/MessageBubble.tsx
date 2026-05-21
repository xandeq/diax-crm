'use client';

import { cn } from '@/lib/utils';
import type { ChatMessage } from '@/types/ai-chat';
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import rehypeHighlight from 'rehype-highlight';

interface MessageBubbleProps {
  message: ChatMessage;
}

export function MessageBubble({ message }: MessageBubbleProps) {
  const isUser = message.role === 'user';

  return (
    <div className={cn('flex gap-3', isUser ? 'flex-row-reverse' : 'flex-row')}>
      {/* Avatar */}
      <div
        className={cn(
          'w-7 h-7 rounded-full flex-shrink-0 flex items-center justify-center text-[10px] font-bold mt-0.5 select-none',
          isUser
            ? 'bg-zinc-800 text-zinc-100'
            : 'bg-zinc-100 text-zinc-600 border border-zinc-200',
        )}
      >
        {isUser ? 'V' : 'AI'}
      </div>

      {/* Bubble / prose */}
      <div
        className={cn(
          'max-w-[78%] text-sm leading-relaxed',
          isUser
            ? 'bg-zinc-900 text-zinc-50 rounded-2xl rounded-tr-sm px-4 py-2.5'
            : 'text-zinc-800',
        )}
      >
        {isUser ? (
          <p className="whitespace-pre-wrap">{message.content}</p>
        ) : (
          <div className="prose prose-sm prose-zinc max-w-none">
            <ReactMarkdown
              remarkPlugins={[remarkGfm]}
              rehypePlugins={[rehypeHighlight]}
            >
              {message.content}
            </ReactMarkdown>
          </div>
        )}
      </div>
    </div>
  );
}
