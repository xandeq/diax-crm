import SnippetPublicClient from '@/components/snippets/SnippetPublicClient';
import { Suspense } from 'react';

export default function SnippetPage() {
  return (
    <Suspense fallback={<div className="flex items-center justify-center min-h-screen"><div className="animate-spin rounded-full h-8 w-8 border-b-2 border-gray-900"></div></div>}>
      <SnippetPublicClient />
    </Suspense>
  );
}
