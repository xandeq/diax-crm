'use client';

import { cn } from '@/lib/utils';
import { ArrowUp, Square } from 'lucide-react';
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
  const textareaRef = useRef<HTMLTextAreaElement>(null);

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
      className="flex-shrink-0 px-4 py-3"
      style={{ background: '#0B1510', borderTop: '1px solid rgba(255,255,255,0.08)' }}
    >
      {/* Model selector strip */}
      <div className="flex items-center gap-1 mb-2">
        {MODEL_OPTIONS.map((m) => (
          <button
            key={m.id}
            type="button"
            onClick={() => onModelChange(m.id)}
            disabled={isStreaming}
            className={cn(
              'px-2.5 py-1 rounded-lg text-[11px] font-medium transition-colors',
              selectedModel === m.id
                ? 'text-white'
                : 'cursor-pointer',
            )}
            style={
              selectedModel === m.id
                ? { background: 'rgba(16,185,129,0.25)', color: '#6EE7B7' }
                : { color: '#9CA3AF', background: 'transparent' }
            }
          >
            {m.label}
          </button>
        ))}
      </div>

      {/* Input row */}
      <div
        className="flex items-end gap-2 rounded-2xl px-3 py-2 transition-colors"
        style={{ background: 'rgba(255,255,255,0.06)', border: '1px solid rgba(255,255,255,0.14)' }}
      >
        <textarea
          ref={textareaRef}
          value={text}
          onChange={(e) => setText(e.target.value)}
          onKeyDown={handleKeyDown}
          onInput={handleInput}
          placeholder="Mensagem (Enter para enviar)"
          rows={1}
          disabled={disabled}
          className="flex-1 resize-none bg-transparent text-sm focus:outline-none min-h-[24px] max-h-[200px] py-0.5 leading-relaxed"
          style={{ color: '#F9FAFB' }}
        />

        <button
          type="button"
          onClick={isStreaming ? onStop : handleSend}
          disabled={!isStreaming && !canSend}
          title={isStreaming ? 'Parar' : 'Enviar'}
          className="w-8 h-8 rounded-xl flex items-center justify-center flex-shrink-0 transition-all active:scale-[0.96]"
          style={
            isStreaming
              ? { background: 'rgba(255,255,255,0.12)', color: '#F9FAFB', cursor: 'pointer' }
              : canSend
                ? { background: '#10B981', color: '#fff', cursor: 'pointer' }
                : { background: 'rgba(255,255,255,0.06)', color: '#4B5563', cursor: 'not-allowed' }
          }
        >
          {isStreaming ? (
            <Square className="w-3 h-3 fill-current" />
          ) : (
            <ArrowUp className="w-4 h-4 stroke-[2.5]" />
          )}
        </button>
      </div>

      <p className="text-[10px] text-center mt-1.5" style={{ color: '#6B7280' }}>
        Shift+Enter para nova linha · IA pode cometer erros — revise informações importantes
      </p>
    </div>
  );
}
