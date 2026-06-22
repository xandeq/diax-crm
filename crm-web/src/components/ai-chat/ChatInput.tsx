'use client';

import { motion, AnimatePresence } from 'framer-motion';
import { ArrowUp, Square, ChevronDown } from 'lucide-react';
import { type KeyboardEvent, useRef, useState } from 'react';
import { MODEL_OPTIONS, type AnthropicModelId } from '@/types/ai-chat';

interface ChatInputProps {
  onSend: (message: string) => void;
  onStop: () => void;
  isStreaming: boolean;
  selectedModel: AnthropicModelId;
  onModelChange: (model: AnthropicModelId) => void;
  disabled?: boolean;
}

export function ChatInput({
  onSend,
  onStop,
  isStreaming,
  selectedModel,
  onModelChange,
  disabled,
}: ChatInputProps) {
  const [text, setText] = useState('');
  const [focused, setFocused] = useState(false);
  const [modelOpen, setModelOpen] = useState(false);
  const textareaRef = useRef<HTMLTextAreaElement>(null);

  const selectedModelLabel = MODEL_OPTIONS.find((m) => m.id === selectedModel)?.label ?? selectedModel;

  function handleKeyDown(e: KeyboardEvent<HTMLTextAreaElement>) {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  }

  function handleSend() {
    const trimmed = text.trim();
    if (!trimmed || isStreaming || disabled) return;
    onSend(trimmed);
    setText('');
    if (textareaRef.current) {
      textareaRef.current.style.height = 'auto';
    }
  }

  function handleInput() {
    const el = textareaRef.current;
    if (el) {
      el.style.height = 'auto';
      el.style.height = `${Math.min(el.scrollHeight, 200)}px`;
    }
  }

  const canSend = text.trim().length > 0 && !isStreaming && !disabled;

  return (
    <div
      className="flex-shrink-0 px-4 pb-4 pt-2"
      style={{ background: '#0B1510' }}
    >
      <div
        className="rounded-2xl transition-all duration-200"
        style={{
          background: 'rgba(255,255,255,0.05)',
          border: `1px solid ${focused ? 'rgba(16,185,129,0.4)' : 'rgba(255,255,255,0.1)'}`,
          boxShadow: focused ? '0 0 0 3px rgba(16,185,129,0.08)' : 'none',
        }}
      >
        {/* Textarea */}
        <div className="px-4 pt-3 pb-2">
          <textarea
            ref={textareaRef}
            value={text}
            onChange={(e) => setText(e.target.value)}
            onKeyDown={handleKeyDown}
            onInput={handleInput}
            onFocus={() => setFocused(true)}
            onBlur={() => setFocused(false)}
            placeholder="Mensagem… (Enter para enviar)"
            rows={1}
            disabled={disabled || isStreaming}
            className="w-full resize-none bg-transparent text-sm focus:outline-none min-h-[24px] max-h-[200px] py-0 leading-relaxed placeholder:transition-colors"
            style={{
              color: '#F9FAFB',
              // @ts-ignore
              '--tw-placeholder-color': '#4B5563',
            }}
          />
        </div>

        {/* Footer row */}
        <div className="flex items-center justify-between px-3 pb-2.5">
          {/* Model selector */}
          <div className="relative">
            <button
              type="button"
              onClick={() => !isStreaming && setModelOpen((o) => !o)}
              disabled={isStreaming}
              className="flex items-center gap-1 px-2 py-1 rounded-lg text-[11px] font-medium transition-all"
              style={{
                background: 'rgba(16,185,129,0.12)',
                border: '1px solid rgba(16,185,129,0.2)',
                color: '#6EE7B7',
                cursor: isStreaming ? 'not-allowed' : 'pointer',
                opacity: isStreaming ? 0.5 : 1,
              }}
            >
              {selectedModelLabel}
              <ChevronDown className="w-2.5 h-2.5" />
            </button>

            <AnimatePresence>
              {modelOpen && (
                <motion.div
                  initial={{ opacity: 0, y: 4, scale: 0.97 }}
                  animate={{ opacity: 1, y: 0, scale: 1 }}
                  exit={{ opacity: 0, y: 4, scale: 0.97 }}
                  transition={{ duration: 0.15 }}
                  className="absolute bottom-full left-0 mb-1 rounded-xl overflow-hidden z-50 min-w-[160px]"
                  style={{
                    background: '#111B16',
                    border: '1px solid rgba(255,255,255,0.12)',
                    boxShadow: '0 8px 24px rgba(0,0,0,0.4)',
                  }}
                >
                  {MODEL_OPTIONS.map((m) => (
                    <button
                      key={m.id}
                      type="button"
                      onClick={() => { onModelChange(m.id); setModelOpen(false); }}
                      className="w-full flex items-center gap-2 text-left px-3 py-2.5 text-xs transition-colors hover:bg-white/5"
                      style={{
                        color: selectedModel === m.id ? '#6EE7B7' : '#9CA3AF',
                      }}
                    >
                      {selectedModel === m.id && (
                        <span className="w-1 h-1 rounded-full bg-emerald-400 flex-shrink-0" />
                      )}
                      <span className={selectedModel === m.id ? 'ml-0' : 'ml-3'}>{m.label}</span>
                    </button>
                  ))}
                </motion.div>
              )}
            </AnimatePresence>
          </div>

          {/* Send / Stop button */}
          <motion.button
            type="button"
            onClick={isStreaming ? onStop : handleSend}
            disabled={!isStreaming && !canSend}
            title={isStreaming ? 'Parar' : 'Enviar'}
            whileTap={{ scale: 0.92 }}
            transition={{ duration: 0.1 }}
            className="w-8 h-8 rounded-xl flex items-center justify-center flex-shrink-0 transition-all duration-200"
            style={
              isStreaming
                ? { background: 'rgba(255,255,255,0.1)', color: '#E5E7EB', cursor: 'pointer' }
                : canSend
                  ? { background: '#10B981', color: '#fff', cursor: 'pointer', boxShadow: '0 0 12px rgba(16,185,129,0.4)' }
                  : { background: 'rgba(255,255,255,0.06)', color: '#374151', cursor: 'not-allowed' }
            }
          >
            {isStreaming ? (
              <Square className="w-3 h-3 fill-current" />
            ) : (
              <ArrowUp className="w-4 h-4 stroke-[2.5]" />
            )}
          </motion.button>
        </div>
      </div>

      <p className="text-[10px] text-center mt-2" style={{ color: '#374151' }}>
        Shift+Enter para nova linha · IA pode cometer erros — revise informações importantes
      </p>
    </div>
  );
}
