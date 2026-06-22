'use client';

import { useEffect, useRef } from 'react';
import type { ChatMessage } from '@/types/ai-chat';
import { MessageBubble } from './MessageBubble';
import { StreamingBubble } from './StreamingBubble';
import { WelcomeScreen } from './WelcomeScreen';

interface ChatMessagesProps {
  messages: ChatMessage[];
  streamingContent: string;
  isStreaming: boolean;
  onSuggestion: (text: string) => void;
}

export function ChatMessages({
  messages,
  streamingContent,
  isStreaming,
  onSuggestion,
}: ChatMessagesProps) {
  const bottomRef = useRef<HTMLDivElement>(null);
  const containerRef = useRef<HTMLDivElement>(null);

  // Auto-scroll to bottom on new content
  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages.length, streamingContent]);

  const isEmpty = messages.length === 0 && !isStreaming;

  return (
    <div
      ref={containerRef}
      className="flex-1 overflow-y-auto overflow-x-hidden scroll-smooth"
      style={{
        scrollbarWidth: 'thin',
        scrollbarColor: 'rgba(255,255,255,0.08) transparent',
      }}
    >
      {isEmpty ? (
        <div className="h-full">
          <WelcomeScreen onSuggestion={onSuggestion} />
        </div>
      ) : (
        <div className="flex flex-col gap-7 px-6 py-8 pb-6 max-w-3xl mx-auto w-full">
          {messages.map((msg) => (
            <MessageBubble key={msg.id} message={msg} />
          ))}
          {isStreaming && (
            <StreamingBubble content={streamingContent} />
          )}
          <div ref={bottomRef} className="h-1" />
        </div>
      )}
    </div>
  );
}
