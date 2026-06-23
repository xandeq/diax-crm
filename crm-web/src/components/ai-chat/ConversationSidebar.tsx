'use client';

import { motion, AnimatePresence } from 'framer-motion';
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
    <aside
      className="w-[250px] flex-shrink-0 flex flex-col overflow-hidden"
      style={{
        background: '#070E0A',
        borderRight: '1px solid rgba(255,255,255,0.06)',
      }}
    >
      {/* Header */}
      <div
        className="flex items-center justify-between px-4 py-3.5 flex-shrink-0"
        style={{ borderBottom: '1px solid rgba(255,255,255,0.06)' }}
      >
        <div className="flex items-center gap-2">
          <div
            className="w-5 h-5 rounded-md flex items-center justify-center"
            style={{ background: 'rgba(16,185,129,0.15)', border: '1px solid rgba(16,185,129,0.25)' }}
          >
            <MessageSquare className="w-2.5 h-2.5" style={{ color: '#6EE7B7' }} />
          </div>
          <span className="text-xs font-semibold tracking-wider uppercase" style={{ color: '#9CA3AF' }}>
            Conversas
          </span>
        </div>
        <motion.button
          type="button"
          onClick={onNew}
          whileHover={{ scale: 1.05 }}
          whileTap={{ scale: 0.95 }}
          className="w-7 h-7 rounded-lg flex items-center justify-center transition-colors"
          style={{ background: 'rgba(255,255,255,0.06)', color: '#9CA3AF' }}
          title="Nova conversa"
        >
          <PenLine className="w-3.5 h-3.5" />
        </motion.button>
      </div>

      {/* Conversation list */}
      <div className="flex-1 overflow-y-auto py-2 space-y-0.5"
        style={{ scrollbarWidth: 'none' }}>
        {loading ? (
          Array.from({ length: 6 }).map((_, i) => (
            <div
              key={i}
              className="mx-2 h-12 rounded-lg animate-pulse"
              style={{
                background: 'rgba(255,255,255,0.04)',
                animationDelay: `${i * 80}ms`,
              }}
            />
          ))
        ) : conversations.length === 0 ? (
          <div className="flex flex-col items-center justify-center gap-3 py-16 px-4 text-center">
            <div
              className="w-10 h-10 rounded-xl flex items-center justify-center"
              style={{ background: 'rgba(255,255,255,0.04)', border: '1px solid rgba(255,255,255,0.08)' }}
            >
              <MessageSquare className="w-4 h-4" style={{ color: '#6B7280' }} />
            </div>
            <p className="text-xs leading-snug" style={{ color: '#9CA3AF' }}>
              Nenhuma conversa ainda.{' '}
              <button
                type="button"
                onClick={onNew}
                className="underline underline-offset-2 transition-colors hover:opacity-80"
                style={{ color: '#9CA3AF' }}
              >
                Comece agora
              </button>
            </p>
          </div>
        ) : (
          <AnimatePresence initial={false}>
            {conversations.map((conv, index) => (
              <motion.div
                key={conv.id}
                initial={{ opacity: 0, x: -4 }}
                animate={{ opacity: 1, x: 0 }}
                exit={{ opacity: 0, x: -4 }}
                transition={{ duration: 0.15, delay: index * 0.02 }}
                onClick={() => onSelect(conv.id)}
                onMouseEnter={() => setHoveredId(conv.id)}
                onMouseLeave={() => setHoveredId(null)}
                className="group relative mx-2 flex items-center rounded-xl px-3 py-2.5 cursor-pointer transition-colors"
                style={
                  activeId === conv.id
                    ? {
                        background: 'rgba(16,185,129,0.12)',
                        border: '1px solid rgba(16,185,129,0.2)',
                      }
                    : hoveredId === conv.id
                      ? {
                          background: 'rgba(255,255,255,0.05)',
                          border: '1px solid transparent',
                        }
                      : { border: '1px solid transparent' }
                }
              >
                <div className="flex-1 min-w-0 pr-1">
                  <p
                    className="text-xs font-medium truncate leading-tight"
                    style={{ color: activeId === conv.id ? '#E5E7EB' : '#9CA3AF' }}
                  >
                    {conv.title}
                  </p>
                  <p className="text-[10px] mt-0.5 truncate" style={{ color: '#6B7280' }}>
                    {formatDistanceToNow(
                      new Date(conv.updatedAt ?? conv.createdAt),
                      { addSuffix: true, locale: ptBR },
                    )}
                  </p>
                </div>

                {/* Archive on hover */}
                <AnimatePresence>
                  {hoveredId === conv.id && (
                    <motion.button
                      initial={{ opacity: 0, scale: 0.8 }}
                      animate={{ opacity: 1, scale: 1 }}
                      exit={{ opacity: 0, scale: 0.8 }}
                      transition={{ duration: 0.1 }}
                      type="button"
                      onClick={(e) => {
                        e.stopPropagation();
                        onArchive(conv.id);
                      }}
                      title="Arquivar"
                      className="flex-shrink-0 p-1.5 rounded-lg transition-colors"
                      style={{ color: '#6B7280', background: 'rgba(255,255,255,0.06)' }}
                    >
                      <Archive className="w-3 h-3" />
                    </motion.button>
                  )}
                </AnimatePresence>
              </motion.div>
            ))}
          </AnimatePresence>
        )}
      </div>
    </aside>
  );
}
