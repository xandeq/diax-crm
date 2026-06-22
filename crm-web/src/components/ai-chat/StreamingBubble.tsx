'use client';

import { motion } from 'framer-motion';
import { Bot } from 'lucide-react';
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';

interface StreamingBubbleProps {
  content: string;
}

export function StreamingBubble({ content }: StreamingBubbleProps) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 8 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.2, ease: [0.16, 1, 0.3, 1] }}
      className="flex gap-3 flex-row"
    >
      {/* Avatar */}
      <div className="w-7 h-7 rounded-full flex-shrink-0 flex items-center justify-center mt-0.5 select-none"
        style={{ background: 'rgba(16,185,129,0.15)', border: '1px solid rgba(16,185,129,0.3)' }}>
        <Bot className="w-3.5 h-3.5" style={{ color: '#6EE7B7' }} />
      </div>

      <div className="max-w-[78%] text-sm leading-relaxed" style={{ color: '#D1D5DB' }}>
        {content ? (
          <div className="prose prose-sm prose-invert ai-prose max-w-none">
            <ReactMarkdown remarkPlugins={[remarkGfm]}>
              {content}
            </ReactMarkdown>
            {/* Blinking cursor */}
            <span
              className="inline-block w-0.5 h-3.5 ml-0.5 align-text-bottom animate-pulse"
              style={{ background: '#6EE7B7' }}
            />
          </div>
        ) : (
          /* Typing indicator */
          <div className="flex items-center gap-1.5 py-1.5 px-1">
            {[0, 1, 2].map((i) => (
              <motion.span
                key={i}
                className="w-1.5 h-1.5 rounded-full"
                style={{ background: 'rgba(110,231,183,0.6)' }}
                animate={{ y: [0, -4, 0], opacity: [0.4, 1, 0.4] }}
                transition={{
                  duration: 1,
                  repeat: Infinity,
                  delay: i * 0.18,
                  ease: 'easeInOut',
                }}
              />
            ))}
          </div>
        )}
      </div>
    </motion.div>
  );
}
