// Server component wrapper required for static export with dynamic segments.
// generateStaticParams returns a placeholder so Next.js generates the shell;
// the real conversationId is read client-side at runtime.
import AiChatClient from '@/components/ai-chat/AiChatClient';

export function generateStaticParams() {
  return [{ conversationId: 'placeholder' }];
}

export default function AiChatConversationPage({
  params,
}: {
  params: { conversationId: string };
}) {
  return <AiChatClient initialConversationId={params.conversationId} />;
}
