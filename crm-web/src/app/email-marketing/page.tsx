'use client';

import { useState, useEffect, useCallback, useRef } from 'react';
import { apiFetch } from '@/services/api';
import { queueBulkEmail, QueueBulkEmailResponse } from '@/services/emailMarketing';
import { Customer, PagedResponse } from '@/services/customers';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Checkbox } from '@/components/ui/checkbox';
import { toast } from 'sonner';
import {
  Mail,
  Search,
  Users,
  Send,
  Info,
  AlertCircle,
  CheckCircle2,
  Loader2,
  Eye,
  EyeOff,
  ChevronLeft,
  ChevronRight,
  ArrowLeft,
  RefreshCw,
  X,
  ImagePlus,
  Trash2,
} from 'lucide-react';
import Link from 'next/link';

const SEGMENT_LABELS: Record<number, string> = { 0: 'Cold', 1: 'Warm', 2: 'Hot' };
const SEGMENT_COLORS: Record<number, string> = {
  0: 'bg-sky-100 text-sky-700',
  1: 'bg-amber-100 text-amber-700',
  2: 'bg-rose-100 text-rose-700',
};
const STATUS_LABELS: Record<number, string> = {
  0: 'Lead',
  1: 'Contactado',
  2: 'Qualificado',
  3: 'Negociando',
  4: 'Cliente',
  5: 'Inativo',
  6: 'Churned',
};

const DEFAULT_TEMPLATE = `<p>Olá {{nome}},</p>

<p>Entramos em contato para apresentar nossos serviços de <strong>Marketing Digital</strong>.</p>

<p>Trabalhamos com estratégias personalizadas para ajudar empresas como a <strong>{{empresa}}</strong> a crescerem online.</p>

<p>Gostaria de saber mais? Responda este email ou nos chame no WhatsApp.</p>

<br>
<p>Atenciosamente,<br>
<strong>Alexandre Queiroz</strong><br>
DIAX Marketing Digital</p>`;

export default function EmailMarketingPage() {
  const [contacts, setContacts] = useState<Customer[]>([]);
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set());
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(false);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [segment, setSegment] = useState('');
  const [contactType, setContactType] = useState<'all' | 'leads' | 'customers'>('all');

  const [subject, setSubject] = useState('');
  const [htmlBody, setHtmlBody] = useState(DEFAULT_TEMPLATE);
  const [showPreview, setShowPreview] = useState(false);

  const [sending, setSending] = useState(false);
  const [result, setResult] = useState<QueueBulkEmailResponse | null>(null);

  // Image upload
  const textareaRef = useRef<HTMLTextAreaElement>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);
  interface UploadedImage { dataUri: string; name: string; sizeKb: number }
  const [uploadedImages, setUploadedImages] = useState<UploadedImage[]>([]);

  const loadContacts = useCallback(async () => {
    setLoading(true);
    try {
      const qs = new URLSearchParams({ page: page.toString(), pageSize: '25', hasEmail: 'true' });
      if (search) qs.append('search', search);
      if (segment) qs.append('segment', segment);
      if (contactType === 'leads') qs.append('onlyLeads', 'true');
      if (contactType === 'customers') qs.append('onlyCustomers', 'true');

      const data = await apiFetch<PagedResponse<Customer>>(`/customers?${qs.toString()}`, { method: 'GET' });
      setContacts(data.items);
      setTotalCount(data.totalCount);
      setTotalPages(data.totalPages);
    } catch (e: any) {
      toast.error('Erro ao carregar contatos: ' + e.message);
    } finally {
      setLoading(false);
    }
  }, [page, search, segment, contactType]);

  useEffect(() => {
    loadContacts();
  }, [loadContacts]);

  // Reset page when filters change
  useEffect(() => {
    setPage(1);
  }, [search, segment, contactType]);

  const toggleSelect = (id: string) => {
    setSelectedIds(prev => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };

  const toggleSelectPage = () => {
    const pageIds = contacts.map(c => c.id);
    const allSelected = pageIds.every(id => selectedIds.has(id));
    setSelectedIds(prev => {
      const next = new Set(prev);
      if (allSelected) pageIds.forEach(id => next.delete(id));
      else pageIds.forEach(id => next.add(id));
      return next;
    });
  };

  const clearSelection = () => setSelectedIds(new Set());

  /** Insere texto na posição atual do cursor no textarea */
  const insertAtCursor = (text: string) => {
    const el = textareaRef.current;
    if (!el) {
      setHtmlBody(prev => prev + text);
      return;
    }
    const start = el.selectionStart ?? el.value.length;
    const end = el.selectionEnd ?? el.value.length;
    const newValue = el.value.substring(0, start) + text + el.value.substring(end);
    setHtmlBody(newValue);
    // Restaura foco e cursor após o texto inserido
    requestAnimationFrame(() => {
      el.focus();
      el.setSelectionRange(start + text.length, start + text.length);
    });
  };

  const insertVariable = (variable: string) => {
    insertAtCursor(variable);
  };

  const handleImageFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = Array.from(e.target.files ?? []);
    if (!files.length) return;

    files.forEach(file => {
      const sizeKb = Math.round(file.size / 1024);
      if (sizeKb > 600) {
        toast.warning(`"${file.name}" tem ${sizeKb} KB. Imagens grandes aumentam o tamanho do email e podem ser bloqueadas. Recomendamos até 600 KB.`);
      }

      const reader = new FileReader();
      reader.onload = ev => {
        const dataUri = ev.target?.result as string;
        if (!dataUri) return;

        // Registra na lista de imagens enviadas
        setUploadedImages(prev => [...prev, { dataUri, name: file.name, sizeKb }]);

        // Insere a tag <img> na posição do cursor
        const imgTag = `\n<img src="${dataUri}" alt="${file.name}" style="max-width:100%; height:auto; display:block; margin:8px 0;" />\n`;
        insertAtCursor(imgTag);
      };
      reader.readAsDataURL(file);
    });

    // Limpa o input para permitir reenviar o mesmo arquivo
    e.target.value = '';
  };

  const removeImage = (idx: number) => {
    const img = uploadedImages[idx];
    setUploadedImages(prev => prev.filter((_, i) => i !== idx));
    // Remove a tag do corpo substituindo o src exato
    setHtmlBody(prev => prev.replace(new RegExp(`<img[^>]*src="${img.dataUri.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')}"[^>]*\\s*/?>`, 'g'), ''));
  };

  const handleSend = async () => {
    if (selectedIds.size === 0) { toast.error('Selecione pelo menos um contato.'); return; }
    if (!subject.trim()) { toast.error('Informe o assunto do email.'); return; }
    if (!htmlBody.trim()) { toast.error('Informe o corpo do email.'); return; }

    setSending(true);
    setResult(null);
    try {
      const res = await queueBulkEmail({ customerIds: Array.from(selectedIds), subject, htmlBody });
      setResult(res);
      toast.success(`${res.queuedCount} email(s) enfileirado(s) com sucesso!`);
      clearSelection();
    } catch (e: any) {
      toast.error('Erro ao enviar: ' + e.message);
    } finally {
      setSending(false);
    }
  };

  const pageIds = contacts.map(c => c.id);
  const allPageSelected = pageIds.length > 0 && pageIds.every(id => selectedIds.has(id));
  const somePageSelected = pageIds.some(id => selectedIds.has(id));

  return (
    <div className="container mx-auto py-6 max-w-7xl">
      {/* Page header */}
      <div className="mb-5">
        <Link href="/outreach">
          <Button variant="ghost" size="sm" className="mb-2 -ml-2">
            <ArrowLeft className="h-4 w-4 mr-1" /> Outreach
          </Button>
        </Link>
        <h1 className="text-3xl font-bold tracking-tight flex items-center gap-2">
          <Mail className="h-8 w-8 text-primary" />
          Email Marketing
        </h1>
        <p className="text-muted-foreground mt-1">
          Selecione contatos, escreva o email e envie. Os disparos respeitam os limites da Brevo automaticamente.
        </p>
      </div>

      {/* Brevo info banner */}
      <div className="mb-5 flex items-start gap-2 rounded-lg border border-blue-200 bg-blue-50 px-4 py-3 text-sm text-blue-800">
        <Info className="mt-0.5 h-4 w-4 shrink-0" />
        <div>
          <strong>Limites Brevo respeitados automaticamente:</strong> ~50 emails/hora e 250 emails/dia.
          Ao enfileirar mais do que o limite, o restante é enviado nos próximos dias.
          Acompanhe o status da fila em{' '}
          <Link href="/outreach" className="underline hover:text-blue-900">Outreach → Dashboard</Link>.
        </div>
      </div>

      <div className="grid grid-cols-1 xl:grid-cols-2 gap-6">
        {/* ── LEFT: Contact selector ───────────────────────── */}
        <Card className="flex flex-col">
          <CardHeader className="pb-3">
            <CardTitle className="flex items-center justify-between text-base">
              <span className="flex items-center gap-2">
                <Users className="h-5 w-5" />
                Contatos com Email
                <span className="text-muted-foreground font-normal text-sm">({totalCount})</span>
              </span>
              {selectedIds.size > 0 && (
                <button onClick={clearSelection} className="flex items-center gap-1 text-xs text-muted-foreground hover:text-foreground">
                  <X className="h-3.5 w-3.5" />
                  Limpar ({selectedIds.size})
                </button>
              )}
            </CardTitle>
          </CardHeader>
          <CardContent className="flex flex-col gap-3">
            {/* Filters */}
            <div className="flex gap-2">
              <div className="relative flex-1">
                <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
                <Input
                  placeholder="Buscar nome, email ou empresa..."
                  className="pl-9 h-9"
                  value={search}
                  onChange={e => setSearch(e.target.value)}
                />
              </div>
              <select
                className="h-9 rounded-md border bg-background px-2.5 text-sm"
                value={contactType}
                onChange={e => setContactType(e.target.value as 'all' | 'leads' | 'customers')}
              >
                <option value="all">Todos</option>
                <option value="leads">Leads</option>
                <option value="customers">Clientes</option>
              </select>
              <select
                className="h-9 rounded-md border bg-background px-2.5 text-sm"
                value={segment}
                onChange={e => setSegment(e.target.value)}
              >
                <option value="">Segmento</option>
                <option value="2">Hot 🔥</option>
                <option value="1">Warm ☀️</option>
                <option value="0">Cold 🧊</option>
              </select>
              <Button variant="ghost" size="icon" className="h-9 w-9 shrink-0" onClick={loadContacts} title="Recarregar">
                <RefreshCw className="h-4 w-4" />
              </Button>
            </div>

            {/* Select all row */}
            {contacts.length > 0 && (
              <div className="flex items-center gap-2 border-b pb-2 text-sm">
                <Checkbox
                  checked={allPageSelected}
                  data-state={somePageSelected && !allPageSelected ? 'indeterminate' : allPageSelected ? 'checked' : 'unchecked'}
                  onCheckedChange={toggleSelectPage}
                />
                <span className="text-muted-foreground">
                  {allPageSelected ? 'Desmarcar página' : 'Selecionar página'}
                </span>
                {selectedIds.size > 0 && (
                  <Badge variant="secondary" className="ml-auto">
                    {selectedIds.size} selecionado(s)
                  </Badge>
                )}
              </div>
            )}

            {/* List */}
            {loading ? (
              <div className="flex justify-center py-10">
                <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
              </div>
            ) : contacts.length === 0 ? (
              <div className="py-10 text-center text-muted-foreground">
                <Mail className="mx-auto mb-2 h-10 w-10 opacity-25" />
                <p className="text-sm">Nenhum contato com email encontrado.</p>
              </div>
            ) : (
              <div className="max-h-[430px] space-y-1 overflow-y-auto pr-1">
                {contacts.map(contact => {
                  const isSelected = selectedIds.has(contact.id);
                  return (
                    <div
                      key={contact.id}
                      onClick={() => toggleSelect(contact.id)}
                      className={`flex cursor-pointer items-center gap-3 rounded-lg border px-3 py-2 transition-colors ${
                        isSelected ? 'border-primary/40 bg-primary/5' : 'hover:bg-muted/40'
                      }`}
                    >
                      <Checkbox
                        checked={isSelected}
                        onCheckedChange={() => toggleSelect(contact.id)}
                        onClick={e => e.stopPropagation()}
                      />
                      <div className="min-w-0 flex-1">
                        <div className="flex items-center gap-1.5">
                          <span className="truncate text-sm font-medium">{contact.name}</span>
                          {contact.segment !== undefined && (
                            <Badge className={`shrink-0 px-1.5 py-0 text-[10px] ${SEGMENT_COLORS[contact.segment]}`}>
                              {SEGMENT_LABELS[contact.segment]}
                            </Badge>
                          )}
                        </div>
                        <div className="truncate text-xs text-muted-foreground">{contact.email}</div>
                        {contact.companyName && (
                          <div className="truncate text-xs text-muted-foreground opacity-70">{contact.companyName}</div>
                        )}
                      </div>
                      <div className="shrink-0 text-right text-xs text-muted-foreground">
                        <div>{STATUS_LABELS[contact.status] ?? '-'}</div>
                        {(contact.emailSentCount ?? 0) > 0 && (
                          <div className="opacity-60">{contact.emailSentCount} emails</div>
                        )}
                      </div>
                    </div>
                  );
                })}
              </div>
            )}

            {/* Pagination */}
            {totalPages > 1 && (
              <div className="flex items-center justify-between border-t pt-2">
                <Button variant="outline" size="sm" disabled={page === 1} onClick={() => setPage(p => p - 1)}>
                  <ChevronLeft className="h-4 w-4" />
                </Button>
                <span className="text-xs text-muted-foreground">
                  {page} / {totalPages}
                </span>
                <Button variant="outline" size="sm" disabled={page >= totalPages} onClick={() => setPage(p => p + 1)}>
                  <ChevronRight className="h-4 w-4" />
                </Button>
              </div>
            )}
          </CardContent>
        </Card>

        {/* ── RIGHT: Email composer ─────────────────────────── */}
        <Card className="flex flex-col">
          <CardHeader className="pb-3">
            <CardTitle className="flex items-center gap-2 text-base">
              <Mail className="h-5 w-5" />
              Compor Email
            </CardTitle>
          </CardHeader>
          <CardContent className="flex flex-col gap-4">
            {/* Subject */}
            <div className="space-y-1.5">
              <Label htmlFor="subject">Assunto *</Label>
              <Input
                id="subject"
                placeholder="Ex: Oferta especial para {{nome}}"
                value={subject}
                onChange={e => setSubject(e.target.value)}
              />
            </div>

            {/* Variable tokens + image upload */}
            <div className="space-y-2">
              <Label className="block text-xs text-muted-foreground">
                Inserir no corpo (na posição do cursor)
              </Label>
              <div className="flex flex-wrap items-center gap-1.5">
                {['{{nome}}', '{{empresa}}', '{{email}}', '{{website}}'].map(v => (
                  <button
                    key={v}
                    onClick={() => insertVariable(v)}
                    className="rounded border bg-muted px-2 py-0.5 font-mono text-xs transition-colors hover:bg-muted/70"
                    title={`Inserir ${v}`}
                  >
                    {v}
                  </button>
                ))}
                {/* Divider */}
                <span className="text-muted-foreground/40 select-none">|</span>
                {/* Image upload button */}
                <button
                  type="button"
                  onClick={() => fileInputRef.current?.click()}
                  className="flex items-center gap-1 rounded border border-dashed bg-muted px-2 py-0.5 text-xs transition-colors hover:bg-muted/70"
                  title="Fazer upload de imagem e inserir no email"
                >
                  <ImagePlus className="h-3 w-3" />
                  Inserir imagem
                </button>
                <input
                  ref={fileInputRef}
                  type="file"
                  accept="image/jpeg,image/png,image/gif,image/webp"
                  multiple
                  className="hidden"
                  onChange={handleImageFileChange}
                />
              </div>

              {/* Thumbnails of uploaded images */}
              {uploadedImages.length > 0 && (
                <div className="flex flex-wrap gap-2 rounded-md border bg-muted/30 p-2">
                  {uploadedImages.map((img, idx) => (
                    <div key={idx} className="relative group">
                      {/* eslint-disable-next-line @next/next/no-img-element */}
                      <img
                        src={img.dataUri}
                        alt={img.name}
                        className="h-14 w-14 rounded object-cover border"
                      />
                      <div className="absolute bottom-0 left-0 right-0 rounded-b bg-black/60 px-1 py-0.5 text-[9px] text-white truncate">
                        {img.sizeKb} KB
                      </div>
                      <button
                        type="button"
                        onClick={() => removeImage(idx)}
                        className="absolute -top-1.5 -right-1.5 hidden group-hover:flex h-4 w-4 items-center justify-center rounded-full bg-red-500 text-white"
                        title="Remover imagem"
                      >
                        <Trash2 className="h-2.5 w-2.5" />
                      </button>
                    </div>
                  ))}
                  <p className="w-full text-[10px] text-muted-foreground">
                    Imagens incorporadas via base64 (funciona em Outlook, Apple Mail e maioria dos clientes de email).
                  </p>
                </div>
              )}
            </div>

            {/* Body editor / preview toggle */}
            <div className="space-y-1.5">
              <div className="flex items-center justify-between">
                <Label htmlFor="htmlBody">Corpo do Email (HTML) *</Label>
                <button
                  onClick={() => setShowPreview(v => !v)}
                  className="flex items-center gap-1 text-xs text-primary hover:underline"
                >
                  {showPreview ? <><EyeOff className="h-3 w-3" /> Editar</> : <><Eye className="h-3 w-3" /> Preview</>}
                </button>
              </div>
              {showPreview ? (
                <div
                  className="min-h-[220px] overflow-auto rounded-md border bg-white p-4 text-sm"
                  dangerouslySetInnerHTML={{ __html: htmlBody }}
                />
              ) : (
                <Textarea
                  ref={textareaRef}
                  id="htmlBody"
                  rows={11}
                  className="font-mono text-xs leading-relaxed"
                  value={htmlBody}
                  onChange={e => setHtmlBody(e.target.value)}
                />
              )}
              <p className="text-xs text-muted-foreground">
                HTML completo suportado. Use as variáveis acima para personalização por contato.
              </p>
            </div>

            {/* Status and send */}
            <div className="space-y-3 border-t pt-3">
              {selectedIds.size === 0 ? (
                <div className="flex items-center gap-2 rounded-lg bg-amber-50 border border-amber-200 px-3 py-2 text-sm text-amber-700">
                  <AlertCircle className="h-4 w-4 shrink-0" />
                  Selecione os contatos na lista ao lado.
                </div>
              ) : (
                <div className="flex items-center gap-2 rounded-lg bg-green-50 border border-green-200 px-3 py-2 text-sm text-green-700">
                  <CheckCircle2 className="h-4 w-4 shrink-0" />
                  <strong>{selectedIds.size}</strong>&nbsp;contato(s) selecionado(s) para receber este email.
                </div>
              )}

              <Button
                onClick={handleSend}
                disabled={sending || selectedIds.size === 0 || !subject.trim() || !htmlBody.trim()}
                className="w-full"
                size="lg"
              >
                {sending ? (
                  <><Loader2 className="mr-2 h-4 w-4 animate-spin" /> Enfileirando...</>
                ) : (
                  <><Send className="mr-2 h-4 w-4" /> Enviar para {selectedIds.size || '—'} contato(s)</>
                )}
              </Button>
            </div>

            {/* Result card */}
            {result && (
              <div className="rounded-lg border border-green-200 bg-green-50 p-4 space-y-3">
                <div className="flex items-center gap-2 font-semibold text-green-700">
                  <CheckCircle2 className="h-5 w-5" />
                  Campanha enfileirada com sucesso!
                </div>
                <div className="grid grid-cols-3 gap-3 text-center text-sm">
                  <div>
                    <div className="text-xl font-bold">{result.requestedCount}</div>
                    <div className="text-xs text-muted-foreground">Solicitados</div>
                  </div>
                  <div>
                    <div className="text-xl font-bold text-green-600">{result.queuedCount}</div>
                    <div className="text-xs text-muted-foreground">Na fila ✅</div>
                  </div>
                  <div>
                    <div className="text-xl font-bold text-amber-500">{result.skippedCount}</div>
                    <div className="text-xs text-muted-foreground">Ignorados ⚠️</div>
                  </div>
                </div>
                {result.skippedCount > 0 && (
                  <p className="text-xs text-muted-foreground">
                    Ignorados: sem email válido, opt-out ativo, ou sem permissão.
                  </p>
                )}
                <p className="text-xs text-green-700">
                  Os emails serão disparados gradualmente respeitando ~50/hora (limite Brevo).
                  Acompanhe em{' '}
                  <Link href="/outreach" className="underline">
                    Outreach → Dashboard
                  </Link>
                  .
                </p>
              </div>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
