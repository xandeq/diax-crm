'use client';

import { motion } from 'framer-motion';
import { Bot, Copy, Check } from 'lucide-react';
import { useState } from 'react';
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import rehypeHighlight from 'rehype-highlight';
import type { ChatMessage } from '@/types/ai-chat';

interface MessageBubbleProps {
  message: ChatMessage;
}

export function MessageBubble({ message }: MessageBubbleProps) {
  const isUser = message.role === 'user';
  const [copied, setCopied] = useState(false);

  async function handleCopy() {
    await navigator.clipboard.writeText(message.content);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  }

  return (
    <motion.div
      initial={{ opacity: 0, y: 8 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.25, ease: [0.16, 1, 0.3, 1] }}
      className={`flex gap-3 group ${isUser ? 'flex-row-reverse' : 'flex-row'}`}
    >
      {/* Avatar */}
      {isUser ? (
        <div className="w-7 h-7 rounded-full flex-shrink-0 flex items-center justify-center text-[10px] font-bold mt-0.5 select-none"
          style={{ background: 'rgba(16,185,129,0.2)', border: '1px solid rgba(16,185,129,0.4)', color: '#6EE7B7' }}>
          V
        </div>
      ) : (
        <div className="w-7 h-7 rounded-full flex-shrink-0 flex items-center justify-center mt-0.5 select-none"
          style={{ background: 'rgba(16,185,129,0.15)', border: '1px solid rgba(16,185,129,0.3)' }}>
          <Bot className="w-3.5 h-3.5" style={{ color: '#6EE7B7' }} />
        </div>
      )}

      {/* Bubble / prose */}
      <div className={`relative max-w-[78%] text-sm leading-relaxed ${isUser ? 'self-end' : ''}`}>
        {isUser ? (
          <div
            className="rounded-2xl rounded-tr-sm px-4 py-2.5"
            style={{ background: 'rgba(16,185,129,0.12)', border: '1px solid rgba(16,185,129,0.22)', color: '#F3F4F6' }}
          >
            <p className="whitespace-pre-wrap">{message.content}</p>
          </div>
        ) : (
          <div className="relative">
            <div
              className="prose prose-sm prose-invert max-w-none ai-prose"
              style={{ color: '#D1D5DB' }}
            >
              <ReactMarkdown
                remarkPlugins={[remarkGfm]}
                rehypePlugins={[rehypeHighlight]}
              >
                {message.content}
              </ReactMarkdown>
            </div>

            {/* Copy button — appears on hover */}
            <motion.button
              type="button"
              onClick={handleCopy}
              title="Copiar"
              initial={{ opacity: 0 }}
              whileHover={{ scale: 1.05 }}
              className="absolute -bottom-7 left-0 opacity-0 group-hover:opacity-100 transition-opacity flex items-center gap-1 px-2 py-0.5 rounded-md text-[10px]"
              style={{ background: 'rgba(255,255,255,0.08)', border: '1px solid rgba(255,255,255,0.12)', color: '#9CA3AF' }}
            >
              {copied ? (
                <><Check className="w-2.5 h-2.5" style={{ color: '#6EE7B7' }} /> Copiado</>
              ) : (
                <><Copy className="w-2.5 h-2.5" /> Copiar</>
              )}
            </motion.button>
          </div>
        )}
      </div>
    </motion.div>
  );
}
