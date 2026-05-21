'use client';

// Static export + App Router: path-segment dynamic routes require a pre-generated
// file for EVERY value, which isn't possible for server-created conversation UUIDs.
// Using a query parameter (?id=) keeps everything on the single /ai-chat/index.html
// and lets us read the ID purely client-side without any server round-trip.

import { Suspense } from 'react';
import { useSearchParams } from 'next/navigation';
import AiChatClient from '@/components/ai-chat/AiChatClient';

function ChatPageContent() {
  const searchParams = useSearchParams();
  const conversationId = searchParams.get('id');
  return <AiChatClient initialConversationId={conversationId} />;
}

export default function AiChatPage() {
  return (
    // useSearchParams() requires a Suspense boundary in static export
    <Suspense
      fallback={
        <div className="flex h-full items-center justify-center">
          <div className="flex gap-1.5">
            {[0, 1, 2].map((i) => (
              <span
                key={i}
                className="w-2 h-2 rounded-full bg-zinc-300 animate-bounce"
                style={{ animationDelay: `${i * 150}ms` }}
              />
            ))}
          </div>
        </div>
      }
    >
      <ChatPageContent />
    </Suspense>
  );
}
