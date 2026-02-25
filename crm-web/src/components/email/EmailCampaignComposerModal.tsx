'use client';

import { Button } from '@/components/ui/button';
import {
    Dialog,
    DialogContent,
    DialogFooter,
    DialogHeader,
    DialogTitle,
} from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from '@/components/ui/select';
import { Textarea } from '@/components/ui/textarea';
import {
    createEmailCampaign,
    EmailAttachmentRequest,
    previewEmailCampaign,
    queueCampaignRecipients,
    updateEmailCampaign,
} from '@/services/emailMarketing';
import { SnippetResponse, snippetService } from '@/services/snippetService';
import { Loader2, Paperclip, Sparkles, X } from 'lucide-react';
import { useEffect, useMemo, useRef, useState } from 'react';

export interface EmailComposerRecipient {
  id: string;
  name: string;
  email: string;
}

interface EmailCampaignComposerModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  recipients: EmailComposerRecipient[];
  title?: string;
  onQueued?: (queuedCount: number) => void;
}

const VARIABLE_TOKENS = ['{{FirstName}}', '{{Email}}', '{{Company}}', '{{LeadStatus}}'];

export function EmailCampaignComposerModal({
  open,
  onOpenChange,
  recipients,
  title = 'Composer de Campanha',
  onQueued,
}: EmailCampaignComposerModalProps) {
  const [campaignName, setCampaignName] = useState('Nova Campanha');
  const [subject, setSubject] = useState('');
  const [bodyHtml, setBodyHtml] = useState('<p>Olá {{FirstName}},</p><p>Sua mensagem aqui.</p>');
  const [selectedSnippetId, setSelectedSnippetId] = useState<string>('none');
  const [snippets, setSnippets] = useState<SnippetResponse[]>([]);
  const [campaignId, setCampaignId] = useState<string | null>(null);
  const [previewHtml, setPreviewHtml] = useState('');
  const [previewSubject, setPreviewSubject] = useState('');
  const [isLoadingSnippets, setIsLoadingSnippets] = useState(false);
  const [isPreviewing, setIsPreviewing] = useState(false);
  const [isSending, setIsSending] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [attachments, setAttachments] = useState<EmailAttachmentRequest[]>([]);
  const textareaRef = useRef<HTMLTextAreaElement | null>(null);
  const fileInputRef = useRef<HTMLInputElement | null>(null);

  const MAX_TOTAL_BYTES = 10 * 1024 * 1024; // 10MB

  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = Array.from(e.target.files || []);
    const allowedTypes = ['image/jpeg', 'image/png', 'image/gif', 'image/webp'];

    files.forEach(file => {
      if (!allowedTypes.includes(file.type)) {
        setError(`Tipo não permitido: ${file.name}. Use JPG, PNG, GIF ou WebP.`);
        return;
      }

      const reader = new FileReader();
      reader.onload = () => {
        const base64 = (reader.result as string).split(',')[1];
        setAttachments(prev => {
          const currentTotal = prev.reduce((sum, a) => sum + atob(a.base64Content).length, 0);
          if (currentTotal + file.size > MAX_TOTAL_BYTES) {
            setError('Tamanho total dos anexos excede 10MB.');
            return prev;
          }
          return [...prev, {
            fileName: file.name,
            contentType: file.type,
            base64Content: base64,
          }];
        });
      };
      reader.readAsDataURL(file);
    });

    if (fileInputRef.current) fileInputRef.current.value = '';
  };

  const removeAttachment = (index: number) => {
    setAttachments(prev => prev.filter((_, i) => i !== index));
  };

  const canSubmit = recipients.length > 0 && subject.trim().length > 0 && bodyHtml.trim().length > 0;

  const selectedSnippet = useMemo(
    () => snippets.find(s => s.id === selectedSnippetId),
    [selectedSnippetId, snippets]
  );

  useEffect(() => {
    if (!open) {
      return;
    }

    setError(null);
    setCampaignId(null);
    setPreviewHtml('');
    setPreviewSubject('');
    setCampaignName(`Campanha ${new Date().toLocaleDateString('pt-BR')}`);
    setSubject('');
    setBodyHtml('<p>Olá {{FirstName}},</p><p>Sua mensagem aqui.</p>');
    setSelectedSnippetId('none');
    setAttachments([]);

    setIsLoadingSnippets(true);
    snippetService.getSnippets()
      .then(data => {
        const emailSnippets = data.filter(s => s.language.toLowerCase() === 'html' || s.language.toLowerCase() === 'markdown' || s.language.toLowerCase() === 'text');
        setSnippets(emailSnippets);
      })
      .catch(() => setError('Não foi possível carregar snippets.'))
      .finally(() => setIsLoadingSnippets(false));
  }, [open]);

  const insertVariable = (token: string) => {
    const textarea = textareaRef.current;
    if (!textarea) {
      setBodyHtml(prev => `${prev}${token}`);
      return;
    }

    const start = textarea.selectionStart;
    const end = textarea.selectionEnd;
    const newValue = `${bodyHtml.slice(0, start)}${token}${bodyHtml.slice(end)}`;
    setBodyHtml(newValue);

    requestAnimationFrame(() => {
      textarea.focus();
      const cursor = start + token.length;
      textarea.setSelectionRange(cursor, cursor);
    });
  };

  const ensureCampaign = async () => {
    const sourceSnippetId = selectedSnippetId === 'none' ? undefined : selectedSnippetId;

    if (!campaignId) {
      const created = await createEmailCampaign({
        name: campaignName,
        subject,
        bodyHtml,
        sourceSnippetId,
      });

      setCampaignId(created.id);
      return created.id;
    }

    await updateEmailCampaign(campaignId, {
      name: campaignName,
      subject,
      bodyHtml,
      sourceSnippetId,
    });

    return campaignId;
  };

  const handleSnippetChange = (value: string) => {
    setSelectedSnippetId(value);

    if (value === 'none') {
      return;
    }

    const snippet = snippets.find(s => s.id === value);
    if (!snippet) {
      return;
    }

    setBodyHtml(snippet.content);
    if (!subject.trim()) {
      setSubject(snippet.title);
    }
  };

  const handlePreview = async () => {
    if (!canSubmit) {
      setError('Preencha assunto e corpo HTML para visualizar o preview.');
      return;
    }

    setError(null);
    setIsPreviewing(true);

    try {
      const id = await ensureCampaign();
      const preview = await previewEmailCampaign(id, {
        subjectOverride: subject,
        bodyHtmlOverride: bodyHtml,
        sourceSnippetIdOverride: selectedSnippetId === 'none' ? undefined : selectedSnippetId,
        mockData: {
          firstName: recipients[0]?.name?.split(' ')[0] ?? 'Cliente',
          email: recipients[0]?.email ?? 'cliente@exemplo.com',
          company: 'Empresa Exemplo',
          leadStatus: 'Qualified',
        },
      });

      setPreviewSubject(preview.renderedSubject);
      setPreviewHtml(preview.renderedBodyHtml);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao gerar preview da campanha.');
    } finally {
      setIsPreviewing(false);
    }
  };

  const handleQueue = async () => {
    if (!canSubmit) {
      setError('Preencha assunto e corpo HTML antes de enfileirar.');
      return;
    }

    setError(null);
    setIsSending(true);

    try {
      const id = await ensureCampaign();
      const response = await queueCampaignRecipients(id, {
        customerIds: recipients.map(r => r.id),
        bodyHtmlOverride: bodyHtml,
        attachments: attachments.length > 0 ? attachments : undefined,
      });

      onQueued?.(response.queuedCount);
      onOpenChange(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao enfileirar campanha.');
    } finally {
      setIsSending(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[980px] max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
        </DialogHeader>

        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 py-2">
          <div className="space-y-4">
            <div className="space-y-2">
              <Label>Campanha</Label>
              <Input value={campaignName} onChange={(e) => setCampaignName(e.target.value)} placeholder="Nome da campanha" />
            </div>

            <div className="space-y-2">
              <Label>Snippet base</Label>
              <Select value={selectedSnippetId} onValueChange={handleSnippetChange}>
                <SelectTrigger>
                  <SelectValue placeholder={isLoadingSnippets ? 'Carregando snippets...' : 'Selecionar snippet'} />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="none">Sem snippet</SelectItem>
                  {snippets.map(snippet => (
                    <SelectItem key={snippet.id} value={snippet.id}>{snippet.title}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
              {selectedSnippet && (
                <p className="text-xs text-slate-500">Template base: {selectedSnippet.title}</p>
              )}
            </div>

            <div className="space-y-2">
              <Label>Assunto</Label>
              <Input value={subject} onChange={(e) => setSubject(e.target.value)} placeholder="Assunto com variáveis: Olá {{FirstName}}" />
            </div>

            <div className="space-y-2">
              <Label>Variáveis dinâmicas</Label>
              <div className="flex flex-wrap gap-2">
                {VARIABLE_TOKENS.map(token => (
                  <Button key={token} type="button" variant="outline" size="sm" onClick={() => insertVariable(token)}>
                    {token}
                  </Button>
                ))}
              </div>
            </div>

            <div className="space-y-2">
              <Label>Editor HTML</Label>
              <Textarea
                ref={textareaRef}
                value={bodyHtml}
                onChange={(e) => setBodyHtml(e.target.value)}
                className="min-h-[280px] font-mono text-xs"
                placeholder="Insira seu HTML aqui..."
              />
            </div>

            {/* Attachment Upload */}
            <div className="space-y-2">
              <Label>Anexos (imagens)</Label>
              <input
                ref={fileInputRef}
                type="file"
                accept="image/jpeg,image/png,image/gif,image/webp"
                multiple
                className="hidden"
                onChange={handleFileSelect}
              />
              <Button type="button" variant="outline" size="sm" onClick={() => fileInputRef.current?.click()}>
                <Paperclip className="h-4 w-4 mr-1" />
                Adicionar imagem
              </Button>
              {attachments.length > 0 && (
                <div className="flex flex-wrap gap-2 mt-2">
                  {attachments.map((att, idx) => (
                    <div key={idx} className="relative group border rounded-md p-1">
                      <img
                        src={`data:${att.contentType};base64,${att.base64Content}`}
                        alt={att.fileName}
                        className="h-16 w-16 object-cover rounded"
                      />
                      <button
                        type="button"
                        onClick={() => removeAttachment(idx)}
                        className="absolute -top-1.5 -right-1.5 bg-red-500 text-white rounded-full h-5 w-5 flex items-center justify-center hover:bg-red-600 transition-colors"
                      >
                        <X className="h-3 w-3" />
                      </button>
                      <p className="text-[10px] text-slate-500 truncate max-w-[64px] text-center">{att.fileName}</p>
                    </div>
                  ))}
                </div>
              )}
            </div>

            <p className="text-xs text-slate-500">
              Destinatários selecionados: {recipients.length}
            </p>
          </div>

          <div className="space-y-3">
            <div className="flex items-center justify-between">
              <Label>Preview renderizado</Label>
              <Button type="button" variant="outline" size="sm" onClick={handlePreview} disabled={isPreviewing || !canSubmit}>
                {isPreviewing ? <Loader2 className="h-4 w-4 mr-1 animate-spin" /> : <Sparkles className="h-4 w-4 mr-1" />}
                Atualizar preview
              </Button>
            </div>

            <div className="rounded-md border border-slate-200 bg-white p-3">
              <p className="text-xs text-slate-500 mb-1">Assunto</p>
              <p className="text-sm font-medium text-slate-900">{previewSubject || '(sem preview ainda)'}</p>
            </div>

            <div className="rounded-md border border-slate-200 bg-white p-3 min-h-[360px] overflow-auto">
              <p className="text-xs text-slate-500 mb-2">Corpo</p>
              {previewHtml ? (
                <div dangerouslySetInnerHTML={{ __html: previewHtml }} />
              ) : (
                <p className="text-sm text-slate-500">Clique em “Atualizar preview” para renderizar as variáveis.</p>
              )}
            </div>
          </div>
        </div>

        {error && (
          <div className="p-3 rounded-md bg-red-50 text-red-600 text-sm">
            {error}
          </div>
        )}

        <DialogFooter>
          <Button type="button" variant="outline" onClick={() => onOpenChange(false)} disabled={isSending}>
            Cancelar
          </Button>
          <Button type="button" onClick={handleQueue} disabled={isSending || !canSubmit}>
            {isSending && <Loader2 className="h-4 w-4 mr-2 animate-spin" />}
            Enfileirar campanha
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
