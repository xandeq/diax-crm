'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { useAuth } from '@/contexts/AuthContext';
import { snippetService, formatBytes, type SnippetResponse } from '@/services/snippetService';
import { AlertCircle, Copy, Download, Loader2, Paperclip } from 'lucide-react';
import { useSearchParams } from 'next/navigation';
import { useEffect, useState } from 'react';

export default function SnippetPublicClient() {
  const searchParams = useSearchParams();
  const { isAuthenticated, isLoading: authLoading } = useAuth();

  const [snippet, setSnippet] = useState<SnippetResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [copied, setCopied] = useState(false);
  const [downloadingId, setDownloadingId] = useState<string | null>(null);

  const snippetId = searchParams?.get('id');

  useEffect(() => {
    if (!snippetId) return;
    if (authLoading) return;
    loadSnippet(snippetId);
  }, [snippetId, authLoading, isAuthenticated]);

  async function loadSnippet(id: string) {
    setLoading(true);
    setError(null);
    try {
      const data = isAuthenticated
        ? await snippetService.getSnippetById(id)
        : await snippetService.getPublicSnippetById(id);
      setSnippet(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Snippet não encontrado');
    } finally {
      setLoading(false);
    }
  }

  async function handleCopy() {
    if (!snippet?.content) return;
    try {
      await navigator.clipboard.writeText(snippet.content);
      setCopied(true);
      setTimeout(() => setCopied(false), 1500);
    } catch {
      setError('Não foi possível copiar o conteúdo');
    }
  }

  async function handleDownload(attachmentId: string, fileName: string) {
    if (!snippet) return;
    setDownloadingId(attachmentId);
    try {
      await snippetService.downloadAttachment(snippet.id, attachmentId, fileName, !isAuthenticated);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao baixar arquivo');
    } finally {
      setDownloadingId(null);
    }
  }

  if (loading || authLoading) {
    return (
      <div className="flex items-center justify-center min-h-[50vh]">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    );
  }

  if (error || !snippet) {
    return (
      <div className="space-y-4">
        <Badge variant="section">Snippets</Badge>
        <div className="flex items-center gap-3 p-4 rounded-xl bg-destructive/10 text-destructive border border-destructive/20">
          <AlertCircle className="h-5 w-5 flex-shrink-0" />
          <p className="text-sm font-medium">{error || 'Snippet não encontrado'}</p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="space-y-2">
        <Badge variant="section">Snippets</Badge>
        <h1 className="text-3xl font-serif tracking-tight">{snippet.title}</h1>
        <p className="text-sm text-muted-foreground">
          {snippet.language.toUpperCase()} • {new Date(snippet.createdAt).toLocaleString('pt-BR')}
        </p>
      </div>

      {snippet.content && (
        <>
          <div className="flex justify-end">
            <Button variant="outline" onClick={handleCopy}>
              <Copy className="h-4 w-4 mr-2" />
              {copied ? 'Copiado' : 'Copiar'}
            </Button>
          </div>

          <pre className="whitespace-pre-wrap break-words rounded-lg border border-border bg-muted/30 p-6 font-mono text-sm">
            {snippet.content}
          </pre>
        </>
      )}

      {snippet.attachments.length > 0 && (
        <div className="space-y-2">
          <p className="text-sm font-medium">Arquivos</p>
          <div className="space-y-1">
            {snippet.attachments.map(att => (
              <div key={att.id} className="flex items-center gap-2 bg-muted rounded px-3 py-2">
                <Paperclip className="h-4 w-4 text-muted-foreground flex-shrink-0" />
                <span className="flex-1 truncate text-sm">{att.originalFileName}</span>
                <span className="text-xs text-muted-foreground flex-shrink-0">{formatBytes(att.sizeBytes)}</span>
                <Button
                  variant="outline"
                  size="sm"
                  disabled={downloadingId === att.id}
                  onClick={() => handleDownload(att.id, att.originalFileName)}
                >
                  {downloadingId === att.id
                    ? <Loader2 className="h-4 w-4 animate-spin" />
                    : <Download className="h-4 w-4 mr-1" />}
                  Baixar
                </Button>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
