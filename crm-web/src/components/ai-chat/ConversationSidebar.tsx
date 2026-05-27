'use client';

import { cn } from '@/lib/utils';
import { Archive, MessageSquare, PenLine } from 'lucide-react';
import { useState } from 'react';
import { formatDistanceToNow } from 'date-fns';
import { ptBR } from 'date-fns/locale';
import type { ConversationListItem } from '@/types/ai-chat';

interface ConversationSidebarProps {
  conversations: ConversationListItem[];
  activeId: string | null;
  onSelect: (id: string) => void;
  onNew: () => void;
  onArchive: (id: string) => void;
  loading: boolean;
}

export function ConversationSidebar({
  conversations,
  activeId,
  onSelect,
  onNew,
  onArchive,
  loading,
}: ConversationSidebarProps) {
  const [hoveredId, setHoveredId] = useState<string | null>(null);

  return (
    <aside className="w-[250px] flex-shrink-0 flex flex-col bg-zinc-950 border-r border-zinc-800 overflow-hidden">
      {/* Header */}
      <div className="flex items-center justify-between px-3 py-3 border-b border-zinc-800/60">
        <span className="text-xs font-semibold text-zinc-400 tracking-widest uppercase">
          Claude
        </span>
        <button
          type="button"
          onClick={onNew}
          className="w-7 h-7 rounded-lg bg-zinc-800 hover:bg-zinc-700 flex items-center justify-center transition-colors text-zinc-400 hover:text-zinc-100"
          title="Nova conversa"
        >
          <PenLine className="w-3.5 h-3.5" />
        </button>
      </div>

      {/* Conversation list */}
      <div className="flex-1 overflow-y-auto py-1.5 space-y-0.5">
        {loading ? (
          // Skeleton loaders
          Array.from({ length: 6 }).map((_, i) => (
            <div
              key={i}
              className="mx-2 h-9 rounded-lg bg-zinc-800/40 animate-pulse"
              style={{ animationDelay: `${i * 80}ms` }}
            />
          ))
        ) : conversations.length === 0 ? (
          <div className="flex flex-col items-center justify-center gap-2 py-12 px-4 text-center">
            <MessageSquare className="w-5 h-5 text-zinc-500" />
            <p className="text-xs text-zinc-400 leading-snug">
              Nenhuma conversa ainda.{' '}
              <button
                type="button"
                onClick={onNew}
                className="text-zinc-500 hover:text-zinc-300 underline underline-offset-2"
              >
                Comece agora
              </button>
            </p>
          </div>
        ) : (
          conversations.map((conv) => (
            <div
              key={conv.id}
              onClick={() => onSelect(conv.id)}
              onMouseEnter={() => setHoveredId(conv.id)}
              onMouseLeave={() => setHoveredId(null)}
              className={cn(
                'group relative mx-2 flex items-center rounded-lg px-2.5 py-2 cursor-pointer transition-colors',
                activeId === conv.id
                  ? 'bg-zinc-800 text-zinc-100'
                  : 'text-zinc-500 hover:bg-zinc-900 hover:text-zinc-300',
              )}
            >
              <div className="flex-1 min-w-0 pr-1">
                <p className="text-xs font-medium truncate leading-tight">
                  {conv.title}
                </p>
                <p className="text-[10px] text-zinc-500 mt-0.5 truncate">
                  {formatDistanceToNow(
                    new Date(conv.updatedAt ?? conv.createdAt),
                    { addSuffix: true, locale: ptBR },
                  )}
                </p>
              </div>

              {/* Archive on hover */}
              {hoveredId === conv.id && (
                <button
                  type="button"
                  onClick={(e) => {
                    e.stopPropagation();
                    onArchive(conv.id);
                  }}
                  title="Arquivar"
                  className="flex-shrink-0 p-1 rounded hover:bg-zinc-700 text-zinc-600 hover:text-zinc-300 transition-colors"
                >
                  <Archive className="w-3 h-3" />
                </button>
              )}
            </div>
          ))
        )}
      </div>
    </aside>
  );
}
