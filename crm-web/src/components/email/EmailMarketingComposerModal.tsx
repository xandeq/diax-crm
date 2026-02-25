'use client';

import { Badge } from '@/components/ui/badge';
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
import { Textarea } from '@/components/ui/textarea';
import { EmailAttachmentRequest } from '@/services/emailMarketing';
import { sendEmailMarketing } from '@/services/outreach';
import { Loader2, Mail, Paperclip, X } from 'lucide-react';
import { useEffect, useRef, useState } from 'react';

const VARIABLE_TOKENS = ['{{nome}}', '{{empresa}}', '{{email}}', '{{website}}'];

interface EmailMarketingComposerModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  validEmailCount: number;
  onSent?: (queuedCount: number) => void;
}

export function EmailMarketingComposerModal({
  open,
  onOpenChange,
  validEmailCount,
  onSent,
}: EmailMarketingComposerModalProps) {
  const [subject, setSubject] = useState('');
  const [bodyHtml, setBodyHtml] = useState(
    '<p>Ol\u00e1 {{nome}},</p>\n<p>Sua mensagem aqui.</p>\n<p>Atenciosamente,<br>Alexandre Queiroz Marketing Digital</p>'
  );
  const [attachments, setAttachments] = useState<EmailAttachmentRequest[]>([]);
  const [isSending, setIsSending] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const textareaRef = useRef<HTMLTextAreaElement | null>(null);
  const fileInputRef = useRef<HTMLInputElement | null>(null);

  const MAX_TOTAL_BYTES = 10 * 1024 * 1024; // 10MB

  const canSubmit = validEmailCount > 0 && subject.trim().length > 0 && bodyHtml.trim().length > 0;

  useEffect(() => {
    if (!open) return;
    setError(null);
    setSubject('');
    setBodyHtml(
      '<p>Ol\u00e1 {{nome}},</p>\n<p>Sua mensagem aqui.</p>\n<p>Atenciosamente,<br>Alexandre Queiroz Marketing Digital</p>'
    );
    setAttachments([]);
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

  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = Array.from(e.target.files || []);
    const allowedTypes = ['image/jpeg', 'image/png', 'image/gif', 'image/webp'];

    files.forEach(file => {
      if (!allowedTypes.includes(file.type)) {
        setError(`Tipo n\u00e3o permitido: ${file.name}. Use JPG, PNG, GIF ou WebP.`);
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

  const handleSend = async () => {
    if (!canSubmit) {
      setError('Preencha assunto e corpo HTML antes de enviar.');
      return;
    }

    setError(null);
    setIsSending(true);

    try {
      const result = await sendEmailMarketing({
        subject,
        bodyHtml,
        attachments: attachments.length > 0 ? attachments : undefined,
      });

      onSent?.(result.queuedCount);
      onOpenChange(false);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro ao enviar email marketing.');
    } finally {
      setIsSending(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[780px] max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Mail className="h-5 w-5" />
            Email Marketing
            <Badge variant="secondary" className="ml-1 font-normal">
              {validEmailCount} destinat\u00e1rios
            </Badge>
          </DialogTitle>
        </DialogHeader>

        <div className="space-y-4 py-2">
          {/* Subject */}
          <div className="space-y-2">
            <Label>Assunto</Label>
            <Input
              value={subject}
              onChange={(e) => setSubject(e.target.value)}
              placeholder="Ex: Transforme sua presen\u00e7a digital com um site profissional"
            />
          </div>

          {/* Variables */}
          <div className="space-y-2">
            <Label>Vari\u00e1veis din\u00e2micas</Label>
            <div className="flex flex-wrap gap-2">
              {VARIABLE_TOKENS.map(token => (
                <Button key={token} type="button" variant="outline" size="sm" onClick={() => insertVariable(token)}>
                  {token}
                </Button>
              ))}
            </div>
          </div>

          {/* HTML Body */}
          <div className="space-y-2">
            <Label>Corpo do email (HTML)</Label>
            <Textarea
              ref={textareaRef}
              value={bodyHtml}
              onChange={(e) => setBodyHtml(e.target.value)}
              className="min-h-[220px] font-mono text-xs"
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
            <div className="flex items-center gap-3">
              <Button type="button" variant="outline" size="sm" onClick={() => fileInputRef.current?.click()}>
                <Paperclip className="h-4 w-4 mr-1" />
                Adicionar imagem
              </Button>
              {attachments.length > 0 && (
                <span className="text-xs text-slate-500">
                  {attachments.length} arquivo(s) anexado(s)
                </span>
              )}
            </div>
            {attachments.length > 0 && (
              <div className="flex flex-wrap gap-2 mt-2">
                {attachments.map((att, idx) => (
                  <div key={idx} className="relative group border rounded-md p-1">
                    <img
                      src={`data:${att.contentType};base64,${att.base64Content}`}
                      alt={att.fileName}
                      className="h-20 w-20 object-cover rounded"
                    />
                    <button
                      type="button"
                      onClick={() => removeAttachment(idx)}
                      className="absolute -top-1.5 -right-1.5 bg-red-500 text-white rounded-full h-5 w-5 flex items-center justify-center hover:bg-red-600 transition-colors"
                    >
                      <X className="h-3 w-3" />
                    </button>
                    <p className="text-[10px] text-slate-500 truncate max-w-[80px] text-center">{att.fileName}</p>
                  </div>
                ))}
              </div>
            )}
          </div>

          {/* Preview */}
          <div className="space-y-2">
            <Label>Preview</Label>
            <div className="rounded-md border border-slate-200 bg-white p-3 min-h-[120px] max-h-[200px] overflow-auto">
              {bodyHtml ? (
                <div dangerouslySetInnerHTML={{ __html: bodyHtml }} />
              ) : (
                <p className="text-sm text-slate-400">O preview do HTML aparecer\u00e1 aqui...</p>
              )}
            </div>
          </div>

          <p className="text-xs text-slate-500">
            O email ser\u00e1 enviado para <strong>{validEmailCount}</strong> contatos com email v\u00e1lido.
            Vari\u00e1veis como {'{{nome}}'} ser\u00e3o substitu\u00eddas automaticamente.
          </p>
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
          <Button type="button" onClick={handleSend} disabled={isSending || !canSubmit}>
            {isSending && <Loader2 className="h-4 w-4 mr-2 animate-spin" />}
            Enviar para {validEmailCount} contatos
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
