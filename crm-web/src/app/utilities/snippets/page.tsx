'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { useAuth } from '@/contexts/AuthContext';
import { snippetService, type SnippetResponse } from '@/services/snippetService';
import { AlertCircle, Copy, ExternalLink, Loader2, Trash2 } from 'lucide-react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useEffect, useState } from 'react';

const languageOptions = [
  { value: 'text', label: 'Texto' },
  { value: 'csharp', label: 'C#' },
  { value: 'sql', label: 'SQL' },
  { value: 'json', label: 'JSON' },
  { value: 'markdown', label: 'Markdown' }
];

const MAX_TITLE_LENGTH = 200;
const MAX_CONTENT_LENGTH = 10000;

export default function SnippetsPage() {
  const { isAuthenticated, isLoading: authLoading } = useAuth();
  const router = useRouter();

  const [title, setTitle] = useState('');
  const [content, setContent] = useState('');
  const [language, setLanguage] = useState('text');
  const [isPublic, setIsPublic] = useState(false);
  const [expiresAt, setExpiresAt] = useState('');

  const [snippets, setSnippets] = useState<SnippetResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [copiedId, setCopiedId] = useState<string | null>(null);
  const [pageReady, setPageReady] = useState(false);

  useEffect(() => {
    if (authLoading) return;

    if (!isAuthenticated) {
      router.push('/login');
      return;
    }

    setPageReady(true);
    loadSnippets();
  }, [isAuthenticated, authLoading, router]);

  async function loadSnippets() {
    setLoading(true);
    setError(null);

    try {
      const data = await snippetService.getSnippets();
      setSnippets(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao carregar snippets');
    } finally {
      setLoading(false);
    }
  }

  async function handleSave() {
    if (!title.trim() || !content.trim()) {
      setError('Título e conteúdo são obrigatórios');
      return;
    }

    if (title.length > MAX_TITLE_LENGTH) {
      setError(`Título deve ter no máximo ${MAX_TITLE_LENGTH} caracteres`);
      return;
    }

    if (content.length > MAX_CONTENT_LENGTH) {
      setError(`Conteúdo deve ter no máximo ${MAX_CONTENT_LENGTH} caracteres`);
      return;
    }

    setSaving(true);
    setError(null);

    try {
      await snippetService.createSnippet({
        title: title.trim(),
        content,
        language,
        isPublic,
        expiresAt: expiresAt ? new Date(expiresAt).toISOString() : null
      });

      setTitle('');
      setContent('');
      setLanguage('text');
      setIsPublic(false);
      setExpiresAt('');

      await loadSnippets();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao salvar snippet');
    } finally {
      setSaving(false);
    }
  }

  async function handleCopy(text: string, id: string) {
    try {
      await navigator.clipboard.writeText(text);
      setCopiedId(id);
      setTimeout(() => setCopiedId(null), 1500);
    } catch {
      setError('Não foi possível copiar o conteúdo');
    }
  }

  async function handleDelete(id: string) {
    const confirmed = window.confirm('Deseja remover este snippet?');
    if (!confirmed) return;

    try {
      await snippetService.deleteSnippet(id);
      setSnippets((current) => current.filter((item) => item.id !== id));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao deletar snippet');
    }
  }

  if (authLoading || !pageReady) {
    return (
      <div className="flex items-center justify-center min-h-[50vh]">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    );
  }

  return (
    <div className="space-y-8">
      <div className="space-y-1">
        <Badge variant="section">Utilitários</Badge>
        <h2 className="text-3xl font-serif tracking-tight">Snippets</h2>
        <p className="text-muted-foreground">
          Crie, copie e compartilhe snippets de texto com segurança.
        </p>
      </div>

      {error && (
        <div className="flex items-center gap-3 p-4 rounded-xl bg-destructive/10 text-destructive border border-destructive/20">
          <AlertCircle className="h-5 w-5 flex-shrink-0" />
          <p className="text-sm font-medium">{error}</p>
        </div>
      )}

      <Card>
        <CardHeader>
          <CardTitle>Novo snippet</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="title">Título</Label>
            <Input
              id="title"
              value={title}
              maxLength={MAX_TITLE_LENGTH}
              onChange={(e) => setTitle(e.target.value)}
              placeholder="Ex: Consulta SQL, Prompt, Script"
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="language">Linguagem</Label>
            <select
              id="language"
              value={language}
              onChange={(e) => setLanguage(e.target.value)}
              className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
            >
              {languageOptions.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </div>

          <div className="space-y-2">
            <Label htmlFor="content">Conteúdo</Label>
            <textarea
              id="content"
              value={content}
              maxLength={MAX_CONTENT_LENGTH}
              onChange={(e) => setContent(e.target.value)}
              placeholder="Cole ou digite seu snippet aqui..."
              className="w-full min-h-[220px] p-4 font-mono text-sm rounded-lg border border-input bg-background resize-y focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2"
            />
            <p className="text-xs text-muted-foreground">
              {content.length}/{MAX_CONTENT_LENGTH} caracteres
            </p>
          </div>

          <div className="grid gap-4 md:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor="expiresAt">Expira em (opcional)</Label>
              <Input
                id="expiresAt"
                type="datetime-local"
                value={expiresAt}
                onChange={(e) => setExpiresAt(e.target.value)}
              />
            </div>

            <div className="flex items-center gap-3 pt-6">
              <input
                id="isPublic"
                type="checkbox"
                checked={isPublic}
                onChange={(e) => setIsPublic(e.target.checked)}
                className="h-4 w-4 rounded border border-input"
              />
              <Label htmlFor="isPublic">Tornar público</Label>
            </div>
          </div>

          <Button onClick={handleSave} disabled={saving}>
            {saving ? 'Salvando...' : 'Salvar'}
          </Button>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Meus snippets</CardTitle>
        </CardHeader>
        <CardContent>
          {loading ? (
            <div className="flex items-center justify-center py-10">
              <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
            </div>
          ) : snippets.length === 0 ? (
            <p className="text-sm text-muted-foreground">Nenhum snippet criado ainda.</p>
          ) : (
            <div className="space-y-4">
              {snippets.map((snippet) => (
                <div
                  key={snippet.id}
                  className="border rounded-lg p-4 flex flex-col gap-3 md:flex-row md:items-center md:justify-between"
                >
                  <div className="space-y-1">
                    <p className="font-semibold text-gray-900">{snippet.title}</p>
                    <p className="text-xs text-muted-foreground">
                      {snippet.language.toUpperCase()} •{' '}
                      {new Date(snippet.createdAt).toLocaleString('pt-BR')} •{' '}
                      {snippet.isPublic ? 'Público' : 'Privado'}
                    </p>
                  </div>

                  <div className="flex flex-wrap gap-2">
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => handleCopy(snippet.content, snippet.id)}
                    >
                      <Copy className="h-4 w-4 mr-2" />
                      {copiedId === snippet.id ? 'Copiado' : 'Copiar'}
                    </Button>
                    <Button variant="outline" size="sm" asChild>
                      <Link href={`/snippet?id=${snippet.id}`}>
                        <ExternalLink className="h-4 w-4 mr-2" />
                        Abrir
                      </Link>
                    </Button>
                    <Button
                      variant="destructive"
                      size="sm"
                      onClick={() => handleDelete(snippet.id)}
                    >
                      <Trash2 className="h-4 w-4 mr-2" />
                      Deletar
                    </Button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
