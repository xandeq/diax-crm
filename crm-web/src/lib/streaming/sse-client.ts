/**
 * SSE streaming client for /api/v1/ai/chat/stream.
 *
 * Uses native fetch with ReadableStream so we can read the response body as a
 * Server-Sent Events text stream without a dedicated EventSource (which cannot
 * send POST bodies or auth headers).
 *
 * Usage:
 *   const abort = streamChat({
 *     request: { conversationId, model, message },
 *     onChunk: (chunk) => ...,
 *     onDone: () => ...,
 *     onError: (err) => ...,
 *   });
 *   // To cancel: abort();
 */

import { apiFetchRaw } from '@/services/api';
import type { ChatRequest, ChatStreamChunk } from '@/types/ai-chat';

export interface StreamChatOptions {
  request: ChatRequest;
  onChunk: (chunk: ChatStreamChunk) => void;
  onDone?: () => void;
  onError?: (error: Error) => void;
  /** External AbortSignal — aborting it cancels this stream too. */
  signal?: AbortSignal;
}

/**
 * Starts a streaming chat request.
 * @returns A cleanup function that aborts the stream when called.
 */
export function streamChat(options: StreamChatOptions): () => void {
  const { request, onChunk, onDone, onError, signal: externalSignal } = options;

  // Guard: if already aborted before we even start, bail out immediately
  if (externalSignal?.aborted) {
    return () => {};
  }

  const controller = new AbortController();

  // Propagate external abort signal to our own controller
  const onExternalAbort = () => controller.abort();
  externalSignal?.addEventListener('abort', onExternalAbort, { once: true });

  async function run() {
    try {
      const response = await apiFetchRaw('/ai/chat/stream', {
        method: 'POST',
        body: JSON.stringify(request),
        headers: {
          // Cache-Control intentionally omitted: native EventSource sends it,
          // but fetch()-based SSE does not need it, and it triggers CORS preflight.
          Accept: 'text/event-stream',
        },
        signal: controller.signal,
      });

      if (!response.ok) {
        // apiFetchRaw already dispatches auth:expired for 401
        let msg = `HTTP ${response.status}`;
        try {
          const text = await response.text();
          const j = JSON.parse(text);
          if (j.message) msg = j.message;
        } catch {
          // ignore parse error, keep generic message
        }
        throw new Error(msg);
      }

      const body = response.body;
      if (!body) throw new Error('Resposta sem corpo');

      const reader = body.getReader();
      const decoder = new TextDecoder();
      let buffer = '';

      // eslint-disable-next-line no-constant-condition
      while (true) {
        const { done, value } = await reader.read();
        if (done) break;

        buffer += decoder.decode(value, { stream: true });

        // Split by newline — SSE uses \n\n between events, but we process
        // line-by-line and skip blank lines naturally.
        const lines = buffer.split('\n');
        // Last element might be incomplete; put it back in the buffer
        buffer = lines.pop() ?? '';

        for (const line of lines) {
          const trimmed = line.trim();

          // SSE lines that carry data start with "data:"
          if (!trimmed.startsWith('data:')) continue;

          const payload = trimmed.slice(5).trim();

          // Backend signals end-of-stream with [DONE]
          if (payload === '[DONE]') {
            onDone?.();
            return;
          }

          if (!payload) continue;

          try {
            const chunk = JSON.parse(payload) as ChatStreamChunk;
            onChunk(chunk);
          } catch (parseErr) {
            console.warn('[SSE] Falha ao parsear chunk:', payload, parseErr);
          }
        }
      }

      // Stream body exhausted without a [DONE] marker — treat as done
      onDone?.();
    } catch (err) {
      // AbortError is intentional (user cancelled) — not an error condition
      if (err instanceof Error && err.name === 'AbortError') return;
      onError?.(err instanceof Error ? err : new Error(String(err)));
    } finally {
      externalSignal?.removeEventListener('abort', onExternalAbort);
    }
  }

  run();

  return () => controller.abort();
}
