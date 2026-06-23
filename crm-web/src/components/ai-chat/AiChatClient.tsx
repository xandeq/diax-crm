'use client';

import { useEffect, useRef, useState } from 'react';
import { useRouter } from 'next/navigation';
import { toast } from 'sonner';
import { PanelLeftClose, PanelLeftOpen } from 'lucide-react';

import {
  listConversations,
  getConversation,
  archiveConversation,
} from '@/services/aiChat';
import { streamChat } from '@/lib/streaming/sse-client';
import {
  CHUNK_TYPE,
  DEFAULT_MODEL,
  type AnthropicModelId,
  type ChatMessage,
  type ChatRequest,
  type ConversationDetail,
  type ConversationListItem,
} from '@/types/ai-chat';

import { ConversationSidebar } from './ConversationSidebar';
import { ChatMessages } from './ChatMessages';
import { ChatInput } from './ChatInput';

interface AiChatClientProps {
  initialConversationId: string | null;
}

export default function AiChatClient({ initialConversationId }: AiChatClientProps) {
  const router = useRouter();

  // ── Sidebar ────────────────────────────────────────────────────────────────
  const [conversations, setConversations] = useState<ConversationListItem[]>([]);
  const [loadingConversations, setLoadingConversations] = useState(true);

  // ── Active conversation ────────────────────────────────────────────────────
  const [activeId, setActiveId] = useState<string | null>(initialConversationId ?? null);
  const [currentConversation, setCurrentConversation] = useState<ConversationDetail | null>(null);
  const [loadingConversation, setLoadingConversation] = useState(false);

  // ── Streaming ──────────────────────────────────────────────────────────────
  const [isStreaming, setIsStreaming] = useState(false);
  const [streamingContent, setStreamingContent] = useState('');
  const streamAbortRef = useRef<(() => void) | null>(null);

  // ── Model ──────────────────────────────────────────────────────────────────
  const [selectedModel, setSelectedModel] = useState<AnthropicModelId>(DEFAULT_MODEL);

  // ── Mobile sidebar ─────────────────────────────────────────────────────────
  const [sidebarOpen, setSidebarOpen] = useState(false);

  // ── Effects ────────────────────────────────────────────────────────────────

  // Load sidebar on mount
  useEffect(() => {
    void loadConversationList();
  }, []);

  // Load conversation when activeId changes
  useEffect(() => {
    if (activeId) {
      void loadConversation(activeId);
    } else {
      setCurrentConversation(null);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [activeId]);

  // Cleanup stream on unmount
  useEffect(() => {
    return () => {
      streamAbortRef.current?.();
    };
  }, []);

  // ── Data loaders ───────────────────────────────────────────────────────────

  async function loadConversationList() {
    try {
      const paged = await listConversations(1, 60);
      setConversations(paged.items);
    } catch {
      toast.error('Não foi possível carregar conversas');
    } finally {
      setLoadingConversations(false);
    }
  }

  async function loadConversation(id: string) {
    setLoadingConversation(true);
    try {
      const detail = await getConversation(id);
      setCurrentConversation(detail);
      // Sync model with conversation's model
      setSelectedModel(detail.model as AnthropicModelId);
    } catch {
      toast.error('Conversa não encontrada');
      setActiveId(null);
      router.replace('/ai-chat/');   // drop ?id= param on not-found
    } finally {
      setLoadingConversation(false);
    }
  }

  // ── Handlers ───────────────────────────────────────────────────────────────

  function handleNewConversation() {
    streamAbortRef.current?.();
    streamAbortRef.current = null;
    setActiveId(null);
    setCurrentConversation(null);
    setStreamingContent('');
    setIsStreaming(false);
    // Query-param routing: keeps everything on /ai-chat/ (the single static page)
    router.push('/ai-chat/');
  }

  function handleSelectConversation(id: string) {
    if (id === activeId) return;
    streamAbortRef.current?.();
    streamAbortRef.current = null;
    setActiveId(id);
    setIsStreaming(false);
    setStreamingContent('');
    // ?id= avoids dynamic path segments that would 404 on static hosting
    router.push(`/ai-chat/?id=${id}`);
  }

  async function handleArchiveConversation(id: string) {
    try {
      await archiveConversation(id);
      setConversations((prev) => prev.filter((c) => c.id !== id));
      if (activeId === id) handleNewConversation();
      toast.success('Conversa arquivada');
    } catch {
      toast.error('Erro ao arquivar conversa');
    }
  }

  function handleStopStream() {
    streamAbortRef.current?.();
    streamAbortRef.current = null;
    setIsStreaming(false);
    setStreamingContent('');
  }

  function handleSendMessage(messageText: string) {
    if (isStreaming) return;

    // Optimistic user message
    const tempUserMsg: ChatMessage = {
      id: `temp-${Date.now()}`,
      role: 'user',
      content: messageText,
      inputTokens: 0,
      outputTokens: 0,
      cacheReadTokens: 0,
      cacheCreationTokens: 0,
      costUsd: 0,
      createdAt: new Date().toISOString(),
      attachments: [],
    };

    setCurrentConversation((prev) =>
      prev
        ? { ...prev, messages: [...prev.messages, tempUserMsg] }
        : {
            id: '',
            title: 'Nova conversa',
            model: selectedModel,
            systemPrompt: null,
            createdAt: new Date().toISOString(),
            updatedAt: null,
            isArchived: false,
            messages: [tempUserMsg],
          },
    );

    setIsStreaming(true);
    setStreamingContent('');

    let realConversationId: string | null = activeId;
    let realMessageId: string | null = null;
    let accumulatedContent = '';

    const request: ChatRequest = {
      conversationId: activeId,
      model: selectedModel,
      message: messageText,
    };

    const abort = streamChat({
      request,

      onChunk(chunk) {
        switch (chunk.type) {
          case CHUNK_TYPE.ConversationStarted: {
            realConversationId = chunk.conversationId ?? null;
            realMessageId = chunk.messageId ?? null;
            if (realConversationId && realConversationId !== activeId) {
              setActiveId(realConversationId);
              // Use ?id= query param — static export can't serve /ai-chat/{uuid}/ paths
              router.replace(`/ai-chat/?id=${realConversationId}`);
            }
            break;
          }

          case CHUNK_TYPE.ContentDelta: {
            accumulatedContent += chunk.delta ?? '';
            setStreamingContent(accumulatedContent);
            break;
          }

          case CHUNK_TYPE.Title: {
            if (chunk.title) {
              const title = chunk.title;
              setCurrentConversation((prev) =>
                prev ? { ...prev, title } : null,
              );
              setConversations((prev) =>
                prev.map((c) =>
                  c.id === realConversationId ? { ...c, title } : c,
                ),
              );
            }
            break;
          }

          case CHUNK_TYPE.MessageStop: {
            const finalMsg: ChatMessage = {
              id: realMessageId ?? `msg-${Date.now()}`,
              role: 'assistant',
              content: accumulatedContent,
              inputTokens: 0,
              outputTokens: 0,
              cacheReadTokens: 0,
              cacheCreationTokens: 0,
              costUsd: 0,
              createdAt: new Date().toISOString(),
              attachments: [],
            };

            setCurrentConversation((prev) =>
              prev
                ? { ...prev, messages: [...prev.messages, finalMsg] }
                : null,
            );
            setStreamingContent('');
            setIsStreaming(false);
            streamAbortRef.current = null;

            // Refresh sidebar entry
            setConversations((prev) => {
              const exists = prev.some((c) => c.id === realConversationId);
              if (exists) {
                return prev.map((c) =>
                  c.id === realConversationId
                    ? {
                        ...c,
                        updatedAt: new Date().toISOString(),
                        messageCount: c.messageCount + 2, // user + assistant
                      }
                    : c,
                );
              }
              // First message in brand-new conversation — reload list
              void loadConversationList();
              return prev;
            });
            break;
          }

          case CHUNK_TYPE.Usage: {
            if (realMessageId && chunk.usage) {
              const u = chunk.usage;
              setCurrentConversation((prev) => {
                if (!prev) return null;
                return {
                  ...prev,
                  messages: prev.messages.map((m) =>
                    m.id === realMessageId
                      ? {
                          ...m,
                          inputTokens: u.inputTokens,
                          outputTokens: u.outputTokens,
                          cacheReadTokens: u.cacheReadTokens,
                          cacheCreationTokens: u.cacheCreationTokens,
                          costUsd: u.costUsd,
                        }
                      : m,
                  ),
                };
              });
            }
            break;
          }

          case CHUNK_TYPE.Error: {
            toast.error(chunk.error ?? 'Erro ao processar resposta');
            setIsStreaming(false);
            setStreamingContent('');
            streamAbortRef.current = null;
            break;
          }
        }
      },

      onDone() {
        // Belt-and-suspenders: message_stop should have already handled this
        setIsStreaming(false);
        setStreamingContent('');
        streamAbortRef.current = null;
      },

      onError(err) {
        toast.error(err.message || 'Erro ao conectar com o servidor');
        setIsStreaming(false);
        setStreamingContent('');
        streamAbortRef.current = null;
        // Rollback optimistic user message
        setCurrentConversation((prev) =>
          prev
            ? {
                ...prev,
                messages: prev.messages.filter((m) => m.id !== tempUserMsg.id),
              }
            : null,
        );
      },
    });

    streamAbortRef.current = abort;
  }

  // ── Render ─────────────────────────────────────────────────────────────────

  return (
    // -mx-6 removes the px-6 from the root layout; h-full fills flex-1 main
    <div className="flex -mx-6 h-full overflow-hidden" style={{ borderTop: '1px solid rgba(255,255,255,0.07)' }}>
      {/* Left sidebar — hidden on mobile unless toggled */}
      <div className={`${sidebarOpen ? 'flex' : 'hidden'} md:flex flex-col absolute md:relative inset-0 md:inset-auto z-30 md:z-auto`}>
        <ConversationSidebar
          conversations={conversations}
          activeId={activeId}
          onSelect={(id) => { handleSelectConversation(id); setSidebarOpen(false); }}
          onNew={() => { handleNewConversation(); setSidebarOpen(false); }}
          onArchive={handleArchiveConversation}
          loading={loadingConversations}
        />
        {/* Mobile backdrop */}
        <div
          className="md:hidden fixed inset-0 -z-10 bg-black/40"
          onClick={() => setSidebarOpen(false)}
        />
      </div>

      {/* Main chat area */}
      <div className="flex-1 flex flex-col min-h-0 min-w-0" style={{ background: '#0B1510' }}>
        {loadingConversation ? (
          <div className="flex flex-1 items-center justify-center">
            <div className="flex gap-1.5">
              {[0, 1, 2].map((i) => (
                <span
                  key={i}
                  className="w-2 h-2 rounded-full animate-bounce"
                  style={{ background: 'rgba(255,255,255,0.2)', animationDelay: `${i * 150}ms` }}
                />
              ))}
            </div>
          </div>
        ) : (
          <>
            {/* Chat header bar (mobile sidebar toggle + title) */}
            <div
              className="flex items-center gap-2 px-4 py-2.5 flex-shrink-0"
              style={{ borderBottom: '1px solid rgba(255,255,255,0.06)' }}
            >
              <button
                type="button"
                onClick={() => setSidebarOpen((o) => !o)}
                className="md:hidden p-1.5 rounded-lg transition-colors"
                style={{ color: '#6B7280' }}
                title="Conversas"
              >
                {sidebarOpen ? (
                  <PanelLeftClose className="w-4 h-4" />
                ) : (
                  <PanelLeftOpen className="w-4 h-4" />
                )}
              </button>
              <span className="text-xs truncate flex-1 text-center md:text-left font-medium" style={{ color: '#9CA3AF' }}>
                {currentConversation?.title ?? ''}
              </span>
              {/* Model badge */}
              {currentConversation?.model && (
                <span
                  className="hidden md:inline-flex text-[10px] px-2 py-0.5 rounded-full font-medium flex-shrink-0"
                  style={{ background: 'rgba(16,185,129,0.1)', color: '#6EE7B7', border: '1px solid rgba(16,185,129,0.2)' }}
                >
                  {currentConversation.model.split('-').slice(1, 3).join('-')}
                </span>
              )}
            </div>
            <ChatMessages
              messages={currentConversation?.messages ?? []}
              streamingContent={streamingContent}
              isStreaming={isStreaming}
              onSuggestion={handleSendMessage}
            />
            <ChatInput
              onSend={handleSendMessage}
              onStop={handleStopStream}
              isStreaming={isStreaming}
              selectedModel={selectedModel}
              onModelChange={setSelectedModel}
            />
          </>
        )}
      </div>
    </div>
  );
}
