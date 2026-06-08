'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { useConfirmDialog } from '@/hooks/useConfirmDialog';
import { useAuth } from '@/contexts/AuthContext';
import { snippetService, formatBytes, type SnippetResponse, type SnippetAttachment } from '@/services/snippetService';
import { AlertCircle, Copy, Download, ExternalLink, Loader2, Paperclip, Plus, Trash2, X } from 'lucide-react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useEffect, useRef, useState } from 'react';

const languageOptions = [
  { value: 'text', label: 'Texto' },
  { value: 'csharp', label: 'C#' },
  { value: 'sql', label: 'SQL' },
  { value: 'json', label: 'JSON' },
  { value: 'markdown', label: 'Markdown' }
];

const MAX_TITLE_LENGTH = 200;
const MAX_CONTENT_LENGTH = 10000;
const MAX_FILE_SIZE = 20 * 1024 * 1024; // 20 MB

export default function SnippetsPage() {
  const { isAuthenticated, isLoading: authLoading } = useAuth();
  const router = useRouter();

  const [title, setTitle] = useState('');
  const [content, setContent] = useState('');
  const [language, setLanguage] = useState('text');
  const [isPublic, setIsPublic] = useState(false);
  const [expiresAt, setExpiresAt] = useState('');
  const [pendingFiles, setPendingFiles] = useState<File[]>([]);

  const [snippets, setSnippets] = useState<SnippetResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [copiedId, setCopiedId] = useState<string | null>(null);
  const [downloadingId, setDownloadingId] = useState<string | null>(null);
  const [pageReady, setPageReady] = useState(false);
  const [uploadingFor, setUploadingFor] = useState<string | null>(null);

  const fileInputRef = useRef<HTMLInputElement>(null);
  const addFileInputRefs = useRef<Record<string, HTMLInputElement | null>>({});

  const { showConfirm, confirmDialogNode } = useConfirmDialog();

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

  function handleFileSelect(e: React.ChangeEvent<HTMLInputElement>) {
    const files = Array.from(e.target.files ?? []);
    const oversized = files.filter(f => f.size > MAX_FILE_SIZE);
    if (oversized.length > 0) {
      setError(`Arquivo(s) excedem o limite de 20 MB: ${oversized.map(f => f.name).join(', ')}`);
      return;
    }
    setPendingFiles(prev => [...prev, ...files]);
    e.target.value = '';
  }

  function removePendingFile(index: number) {
    setPendingFiles(prev => prev.filter((_, i) => i !== index));
  }

  async function handleSave() {
    const hasContent = content.trim().length > 0;
    const hasFiles = pendingFiles.length > 0;

    if (!title.trim()) {
      setError('Título é obrigatório');
      return;
    }
    if (!hasContent && !hasFiles) {
      setError('Informe um texto ou ao menos um arquivo');
      return;
    }
    if (title.length > MAX_TITLE_LENGTH) {
      setError(`Título deve ter no máximo ${MAX_TITLE_LENGTH} caracteres`);
      return;
    }
    if (hasContent && content.length > MAX_CONTENT_LENGTH) {
      setError(`Conteúdo deve ter no máximo ${MAX_CONTENT_LENGTH} caracteres`);
      return;
    }

    setSaving(true);
    setError(null);

    try {
      await snippetService.createSnippet({
        title: title.trim(),
        content: hasContent ? content : undefined,
        language,
        isPublic,
        expiresAt: expiresAt ? new Date(expiresAt).toISOString() : null,
        files: hasFiles ? pendingFiles : undefined
      });

      setTitle('');
      setContent('');
      setLanguage('text');
      setIsPublic(false);
      setExpiresAt('');
      setPendingFiles([]);

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

  function handleDelete(id: string) {
    showConfirm('Deseja remover este snippet?', async () => {
      try {
        await snippetService.deleteSnippet(id);
        setSnippets(prev => prev.filter(s => s.id !== id));
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Erro ao deletar snippet');
      }
    });
  }

  async function handleDownload(snippet: SnippetResponse, attachment: SnippetAttachment) {
    setDownloadingId(attachment.id);
    try {
      await snippetService.downloadAttachment(snippet.id, attachment.id, attachment.originalFileName);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao baixar arquivo');
    } finally {
      setDownloadingId(null);
    }
  }

  async function handleDeleteAttachment(snippetId: string, attachment: SnippetAttachment) {
    showConfirm(`Remover o arquivo "${attachment.originalFileName}"?`, async () => {
      try {
        await snippetService.deleteAttachment(snippetId, attachment.id);
        setSnippets(prev =>
          prev.map(s =>
            s.id === snippetId
              ? { ...s, attachments: s.attachments.filter(a => a.id !== attachment.id) }
              : s
          )
        );
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Erro ao remover anexo');
      }
    });
  }

  async function handleAddAttachment(snippetId: string, file: File) {
    if (file.size > MAX_FILE_SIZE) {
      setError('O arquivo excede o limite de 20 MB.');
      return;
    }
    setUploadingFor(snippetId);
    try {
      await snippetService.addAttachment(snippetId, file);
      await loadSnippets();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao anexar arquivo');
    } finally {
      setUploadingFor(null);
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
      {confirmDialogNode}

      <div className="space-y-1">
        <Badge variant="section">Utilitários</Badge>
        <h2 className="text-3xl font-serif tracking-tight">Snippets</h2>
        <p className="text-muted-foreground">
          Crie, copie e compartilhe snippets de texto e arquivos com segurança.
        </p>
      </div>

      {error && (
        <div className="flex items-center gap-3 p-4 rounded-xl bg-destructive/10 text-destructive border border-destructive/20">
          <AlertCircle className="h-5 w-5 flex-shrink-0" />
          <p className="text-sm font-medium">{error}</p>
        </div>
      )}

      {/* Form: novo snippet */}
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
              onChange={e => setTitle(e.target.value)}
              placeholder="Ex: Consulta SQL, Prompt, Script"
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="language">Linguagem</Label>
            <select
              id="language"
              value={language}
              onChange={e => setLanguage(e.target.value)}
              className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-ring"
            >
              {languageOptions.map(opt => (
                <option key={opt.value} value={opt.value}>{opt.label}</option>
              ))}
            </select>
          </div>

          <div className="space-y-2">
            <Label htmlFor="content">
              Conteúdo <span className="text-muted-foreground text-xs">(opcional se houver arquivo)</span>
            </Label>
            <textarea
              id="content"
              value={content}
              maxLength={MAX_CONTENT_LENGTH}
              onChange={e => setContent(e.target.value)}
              placeholder="Cole ou digite seu snippet aqui..."
              className="w-full min-h-[180px] p-4 font-mono text-sm rounded-lg border border-input bg-background resize-y focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2"
            />
            <p className="text-xs text-muted-foreground">{content.length}/{MAX_CONTENT_LENGTH} caracteres</p>
          </div>

          {/* Seleção de arquivos */}
          <div className="space-y-2">
            <Label>Arquivos anexados <span className="text-muted-foreground text-xs">(opcional, até 20 MB cada)</span></Label>
            <div>
              <input
                ref={fileInputRef}
                type="file"
                multiple
                className="hidden"
                onChange={handleFileSelect}
              />
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={() => fileInputRef.current?.click()}
              >
                <Paperclip className="h-4 w-4 mr-2" />
                Adicionar arquivo(s)
              </Button>
            </div>

            {pendingFiles.length > 0 && (
              <ul className="space-y-1 mt-2">
                {pendingFiles.map((file, i) => (
                  <li key={i} className="flex items-center gap-2 text-sm bg-muted rounded px-3 py-1.5">
                    <Paperclip className="h-3 w-3 text-muted-foreground flex-shrink-0" />
                    <span className="flex-1 truncate">{file.name}</span>
                    <span className="text-muted-foreground text-xs flex-shrink-0">{formatBytes(file.size)}</span>
                    <button
                      type="button"
                      onClick={() => removePendingFile(i)}
                      className="text-muted-foreground hover:text-destructive"
                    >
                      <X className="h-4 w-4" />
                    </button>
                  </li>
                ))}
              </ul>
            )}
          </div>

          <div className="grid gap-4 md:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor="expiresAt">Expira em (opcional)</Label>
              <Input
                id="expiresAt"
                type="datetime-local"
                value={expiresAt}
                onChange={e => setExpiresAt(e.target.value)}
              />
            </div>
            <div className="flex items-center gap-3 pt-6">
              <input
                id="isPublic"
                type="checkbox"
                checked={isPublic}
                onChange={e => setIsPublic(e.target.checked)}
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

      {/* Lista de snippets */}
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
              {snippets.map(snippet => (
                <div key={snippet.id} className="border rounded-lg p-4 space-y-3">
                  {/* Header do snippet */}
                  <div className="flex flex-col gap-3 md:flex-row md:items-start md:justify-between">
                    <div className="space-y-1">
                      <p className="font-semibold text-gray-900">{snippet.title}</p>
                      <p className="text-xs text-muted-foreground">
                        {snippet.language.toUpperCase()} •{' '}
                        {new Date(snippet.createdAt).toLocaleString('pt-BR')} •{' '}
                        {snippet.isPublic ? 'Público' : 'Privado'}
                        {snippet.attachments.length > 0 && ` • ${snippet.attachments.length} arquivo(s)`}
                      </p>
                    </div>

                    <div className="flex flex-wrap gap-2">
                      {snippet.content && (
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => handleCopy(snippet.content, snippet.id)}
                        >
                          <Copy className="h-4 w-4 mr-2" />
                          {copiedId === snippet.id ? 'Copiado' : 'Copiar'}
                        </Button>
                      )}
                      <Button variant="outline" size="sm" asChild>
                        <Link href={`/snippet?id=${snippet.id}`}>
                          <ExternalLink className="h-4 w-4 mr-2" />
                          Abrir
                        </Link>
                      </Button>

                      {/* Adicionar arquivo a snippet existente */}
                      <div>
                        <input
                          ref={el => { addFileInputRefs.current[snippet.id] = el; }}
                          type="file"
                          className="hidden"
                          onChange={async e => {
                            const file = e.target.files?.[0];
                            if (file) await handleAddAttachment(snippet.id, file);
                            e.target.value = '';
                          }}
                        />
                        <Button
                          variant="outline"
                          size="sm"
                          disabled={uploadingFor === snippet.id}
                          onClick={() => addFileInputRefs.current[snippet.id]?.click()}
                        >
                          {uploadingFor === snippet.id ? (
                            <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                          ) : (
                            <Plus className="h-4 w-4 mr-2" />
                          )}
                          Arquivo
                        </Button>
                      </div>

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

                  {/* Lista de anexos */}
                  {snippet.attachments.length > 0 && (
                    <div className="border-t pt-3 space-y-1">
                      <p className="text-xs font-medium text-muted-foreground mb-2">Arquivos anexados</p>
                      {snippet.attachments.map(att => (
                        <div key={att.id} className="flex items-center gap-2 text-sm bg-muted rounded px-3 py-1.5">
                          <Paperclip className="h-3 w-3 text-muted-foreground flex-shrink-0" />
                          <span className="flex-1 truncate text-sm">{att.originalFileName}</span>
                          <span className="text-xs text-muted-foreground flex-shrink-0">{formatBytes(att.sizeBytes)}</span>
                          <Button
                            variant="ghost"
                            size="icon"
                            className="h-7 w-7 flex-shrink-0"
                            disabled={downloadingId === att.id}
                            onClick={() => handleDownload(snippet, att)}
                            title="Baixar"
                          >
                            {downloadingId === att.id
                              ? <Loader2 className="h-3 w-3 animate-spin" />
                              : <Download className="h-3 w-3" />}
                          </Button>
                          <Button
                            variant="ghost"
                            size="icon"
                            className="h-7 w-7 text-muted-foreground hover:text-destructive flex-shrink-0"
                            onClick={() => handleDeleteAttachment(snippet.id, att)}
                            title="Remover"
                          >
                            <X className="h-3 w-3" />
                          </Button>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
