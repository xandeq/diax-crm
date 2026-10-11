import { PromptStudio } from '@/components/prompt-studio/PromptStudio';
import { Suspense } from 'react';

// Rota única com estado na query string (?p=<id>&tab=...) — compatível com o static export (ADR-03).
export default function PromptStudioPage() {
  return (
    <Suspense fallback={null}>
      <PromptStudio />
    </Suspense>
  );
}
