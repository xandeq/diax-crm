'use client';

import { RichTextEditor } from '@/components/editor/RichTextEditor';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Checkbox } from '@/components/ui/checkbox';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from '@/components/ui/select';
import { compressImage } from '@/lib/image-compression';
import { apiFetch } from '@/services/api';
import { Customer, PagedResponse } from '@/services/customers';
import { uploadEmailImage } from '@/services/emailImages';
import {
    createEmailCampaign,
    EmailCampaignResponse,
    getEmailCampaigns,
    queueCampaignRecipients,
    QueueCampaignRecipientsResponse
} from '@/services/emailMarketing';
import {
    AlertCircle,
    ArrowLeft,
    CheckCircle2,
    ChevronLeft,
    ChevronRight,
    Eye,
    EyeOff,
    ImagePlus,
    Info,
    Loader2,
    Mail,
    RefreshCw,
    Search,
    Send,
    Trash2,
    Users,
    X,
} from 'lucide-react';
import Link from 'next/link';
import { useCallback, useEffect, useRef, useState } from 'react';
import { toast } from 'sonner';
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
  const [pageSize, setPageSize] = useState('25');

  const [subject, setSubject] = useState('');
  const [htmlBody, setHtmlBody] = useState(DEFAULT_TEMPLATE);
  const [showPreview, setShowPreview] = useState(false);

  const [campaignName, setCampaignName] = useState('');
  const [savedCampaigns, setSavedCampaigns] = useState<EmailCampaignResponse[]>([]);
  const [isLoadingCampaigns, setIsLoadingCampaigns] = useState(false);

  const [sending, setSending] = useState(false);
  const [result, setResult] = useState<QueueCampaignRecipientsResponse | null>(null);

  // Image upload and Editor Ref
  const editorRef = useRef<any>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);
  interface UploadedImage {
    id: string;
    publicUrl: string; // URL pública retornada pelo backend
    thumbnailDataUri: string; // Data URI apenas para thumbnail no preview
    name: string;
    originalSizeKb: number;
    compressedSizeKb: number;
    width: number;
    height: number;
  }
  const [uploadedImages, setUploadedImages] = useState<UploadedImage[]>([]);
  const [uploading, setUploading] = useState(false);

  const loadContacts = useCallback(async () => {
    setLoading(true);
    try {
      const qs = new URLSearchParams({ page: page.toString(), pageSize: pageSize, hasEmail: 'true' });
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
  }, [page, search, segment, contactType, pageSize]);

  useEffect(() => {
    loadContacts();
  }, [loadContacts]);

  useEffect(() => {
    setIsLoadingCampaigns(true);
    getEmailCampaigns(1, 20)
      .then(data => setSavedCampaigns(data.items))
      .catch(() => { /* ignora se falhar */ })
      .finally(() => setIsLoadingCampaigns(false));
  }, []);

  // Reset page when filters change
  useEffect(() => {
    setPage(1);
  }, [search, segment, contactType, pageSize]);

  const handleLoadCampaign = (campaignId: string) => {
    const campaign = savedCampaigns.find(c => c.id === campaignId);
    if (!campaign) return;
    setSubject(campaign.subject);
    setHtmlBody(campaign.bodyHtml);
    setCampaignName(campaign.name);
  };

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

  /** Insere texto/HTML na posição atual do cursor no editor TipTap */
  const insertAtCursor = (content: string) => {
    if (editorRef.current) {
      // Usar a func insertContent do TipTap para colocar HTML na posição
      editorRef.current.chain().focus().insertContent(content).run();
      return;
    }

    // Fallback de segurança se o ref não estiver pronto
    setHtmlBody(prev => prev + content);
  };

  const insertVariable = (variable: string) => {
    insertAtCursor(variable);
  };

  /** Renderiza variáveis com dados reais do primeiro contato selecionado */
  const renderPreview = (template: string): string => {
    // Se não tem contatos selecionados, usa dados mock
    const selectedContacts = contacts.filter(c => selectedIds.has(c.id));
    const firstContact = selectedContacts[0];

    const variables: Record<string, string> = {
      nome: firstContact?.name || 'Cliente',
      firstName: firstContact?.name?.split(' ')[0] || 'Cliente',
      empresa: firstContact?.companyName || 'sua empresa',
      company: firstContact?.companyName || 'sua empresa',
      companyName: firstContact?.companyName || 'sua empresa',
      email: firstContact?.email || 'cliente@exemplo.com',
      website: firstContact?.website || '',
      leadStatus: firstContact ? STATUS_LABELS[firstContact.status] || 'Lead' : 'Lead',
    };

    // Substitui variáveis (case-insensitive)
    let rendered = template;
    Object.entries(variables).forEach(([key, value]) => {
      const regex = new RegExp(`\\{\\{\\s*${key}\\s*\\}\\}`, 'gi');
      rendered = rendered.replace(regex, value);
    });

    return rendered;
  };

  const handleImageFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = Array.from(e.target.files ?? []);
    if (!files.length) return;

    setUploading(true);

    try {
      for (const file of files) {
        // 1. Comprime a imagem localmente
        const compressed = await compressImage(file, {
          maxWidthOrHeight: 1200,
          maxSizeKB: 300,
          quality: 0.85,
        });

        // 2. Faz upload para o backend e recebe URL pública
        // Remove o prefixo "data:image/png;base64," do dataUrl
        const base64Content = compressed.dataUrl.split(',')[1];
        const contentType = compressed.dataUrl.split(';')[0].split(':')[1];

        const uploadResponse = await uploadEmailImage({
          fileName: file.name,
          base64Content: base64Content,
          contentType: contentType,
        });

        // 3. Registra na lista de imagens com URL pública
        setUploadedImages(prev => [
          ...prev,
          {
            id: uploadResponse.imageId,
            publicUrl: uploadResponse.publicUrl,
            thumbnailDataUri: compressed.dataUrl, // Mantém dataUri apenas para thumbnail
            name: file.name,
            originalSizeKb: compressed.originalSizeKB,
            compressedSizeKb: compressed.compressedSizeKB,
            width: compressed.width,
            height: compressed.height,
          },
        ]);

        // 4. Insere tag <img> com URL pública (NÃO base64!)
        // CRÍTICO: Gmail e outros bloqueiam data URIs. Usar URL hospedada é obrigatório.
        const imgTag = `\n<img id="${uploadResponse.imageId}" src="${uploadResponse.publicUrl}" alt="${file.name}" style="max-width:100%; height:auto; display:block; margin:8px 0;" />\n`;
        insertAtCursor(imgTag);

        // 5. Notifica sucesso
        toast.success(
          `"${file.name}" hospedada com sucesso! (${compressed.originalSizeKB} KB → ${compressed.compressedSizeKB} KB)`
        );

        if (compressed.compressedSizeKB > 400) {
          toast.warning(
            `"${file.name}" ainda está grande (${compressed.compressedSizeKB} KB). Considere usar uma imagem menor para melhor deliverability.`
          );
        }
      }
    } catch (error: any) {
      toast.error(`Erro ao fazer upload da imagem: ${error.message}`);
    } finally {
      setUploading(false);
      // Limpa o input para permitir reenviar o mesmo arquivo
      e.target.value = '';
    }
  };

  const removeImage = (idx: number) => {
    const img = uploadedImages[idx];
    setUploadedImages(prev => prev.filter((_, i) => i !== idx));

    // Remove a tag do corpo usando o ID único (muito mais preciso e rápido!)
    setHtmlBody(prev => {
      // Regex para encontrar tag <img> com o ID específico
      const regex = new RegExp(`<img[^>]*id="${img.id}"[^>]*\\s*/?>\\n?`, 'g');
      return prev.replace(regex, '');
    });
  };

  const handleSend = async () => {
    if (selectedIds.size === 0) { toast.error('Selecione pelo menos um contato.'); return; }
    if (!subject.trim()) { toast.error('Informe o assunto do email.'); return; }
    if (!htmlBody.trim()) { toast.error('Informe o corpo do email.'); return; }

    setSending(true);
    setResult(null);
    try {
      // 1. Criar a campanha para salvar no histórico
      const finalCampaignName = campaignName.trim() || `Disparo Manual - ${new Date().toLocaleString('pt-BR')}`;
      const campaign = await createEmailCampaign({
        name: finalCampaignName,
        subject,
        bodyHtml: htmlBody,
      });

      // 2. Enfileirar na campanha criada
      const res = await queueCampaignRecipients(campaign.id, {
        customerIds: Array.from(selectedIds)
      });

      setResult(res);
      toast.success(`${res.queuedCount} email(s) enfileirado(s) com sucesso e salvo no histórico!`);
      clearSelection();

      // Recarrega campanhas para atualizar dropdown
      getEmailCampaigns(1, 20).then(data => setSavedCampaigns(data.items));
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
              <select
                className="h-9 rounded-md border bg-background px-2.5 text-sm"
                value={pageSize}
                onChange={e => setPageSize(e.target.value)}
                title="Itens por página"
              >
                <option value="10">10 / pág</option>
                <option value="25">25 / pág</option>
                <option value="50">50 / pág</option>
                <option value="100">100 / pág</option>
                <option value="200">200 / pág</option>
                <option value="500">500 / pág</option>
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
            {/* Load History */}
            {savedCampaigns.length > 0 && (
              <div className="space-y-1.5 border-b pb-3">
                <Label>Carregar envio do Histórico</Label>
                <Select onValueChange={handleLoadCampaign} disabled={isLoadingCampaigns}>
                  <SelectTrigger>
                    <SelectValue placeholder={isLoadingCampaigns ? 'Carregando...' : 'Selecione um email anterior'} />
                  </SelectTrigger>
                  <SelectContent>
                    {savedCampaigns.map(c => (
                      <SelectItem key={c.id} value={c.id}>
                        {c.name} — {new Date(c.createdAt).toLocaleDateString('pt-BR')}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                <p className="text-[10px] text-muted-foreground">
                  Isso preencherá o Assunto e o Corpo abaixo. Todo envio é salvo aqui.
                </p>
              </div>
            )}

            {/* Campaign Name */}
            <div className="space-y-1.5">
              <Label htmlFor="campaignName">Nome do Envio (Para localizar no histórico)</Label>
              <Input
                id="campaignName"
                placeholder="Ex: Oferta de Natal 2026"
                value={campaignName}
                onChange={e => setCampaignName(e.target.value)}
              />
            </div>

            {/* Subject */}
            <div className="space-y-1.5">
              <Label htmlFor="subject">Assunto *</Label>
              <Input
                id="subject"
                placeholder="Ex: Oferta especial para {{nome}}"
                value={subject}
                onChange={e => setSubject(e.target.value)}
              />
              {showPreview && subject && (
                <div className="text-xs">
                  <span className="text-muted-foreground">Preview: </span>
                  <span className="font-medium">{renderPreview(subject)}</span>
                </div>
              )}
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
                    <div key={img.id} className="relative group">
                      {/* eslint-disable-next-line @next/next/no-img-element */}
                      <img
                        src={img.thumbnailDataUri}
                        alt={img.name}
                        className="h-14 w-14 rounded object-cover border"
                        title={`${img.name}\n${img.width}×${img.height}px\nOriginal: ${img.originalSizeKb} KB\nHospedada: ${img.compressedSizeKb} KB\nURL: ${img.publicUrl}`}
                      />
                      <div className="absolute bottom-0 left-0 right-0 rounded-b bg-black/70 px-1 py-0.5 text-[9px] text-white truncate">
                        {img.compressedSizeKb} KB
                        {img.originalSizeKb !== img.compressedSizeKb && (
                          <span className="text-green-300"> ↓</span>
                        )}
                      </div>
                      <button
                        type="button"
                        onClick={() => removeImage(idx)}
                        className="absolute -top-1.5 -right-1.5 hidden group-hover:flex h-4 w-4 items-center justify-center rounded-full bg-red-500 text-white hover:bg-red-600"
                        title="Remover imagem"
                      >
                        <Trash2 className="h-2.5 w-2.5" />
                      </button>
                    </div>
                  ))}
                  <p className="w-full text-[10px] text-muted-foreground">
                    ✨ Imagens automaticamente comprimidas e hospedadas no servidor (compatível com Gmail, Outlook e todos os clientes).
                    {uploading && <span className="ml-1 text-blue-600">Fazendo upload...</span>}
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
                  {showPreview ? <><EyeOff className="h-3 w-3" /> Editar</> : <><Eye className="h-3 w-3" /> Preview com dados reais</>}
                </button>
              </div>
              {showPreview ? (
                <div className="space-y-2">
                  {/* Preview header info */}
                  {(() => {
                    const selectedContacts = contacts.filter(c => selectedIds.has(c.id));
                    const firstContact = selectedContacts[0];
                    return firstContact ? (
                      <div className="rounded-md bg-blue-50 border border-blue-200 px-3 py-2 text-xs text-blue-800">
                        <strong>Preview renderizado com dados de:</strong> {firstContact.name}
                        {firstContact.companyName && ` (${firstContact.companyName})`}
                      </div>
                    ) : (
                      <div className="rounded-md bg-amber-50 border border-amber-200 px-3 py-2 text-xs text-amber-700">
                        Preview com dados fictícios. Selecione contatos para ver dados reais.
                      </div>
                    );
                  })()}

                  {/* Rendered preview */}
                  <div
                    className="prose max-w-none min-h-[220px] max-h-[400px] overflow-auto rounded-md border bg-white p-4 text-sm"
                    dangerouslySetInnerHTML={{ __html: renderPreview(htmlBody) }}
                  />
                </div>
              ) : (
                <RichTextEditor
                  value={htmlBody}
                  onChange={setHtmlBody}
                  editorRef={editorRef}
                />
              )}
              <p className="text-xs text-muted-foreground">
                {showPreview
                  ? '💡 Preview mostra como o email será exibido com variáveis substituídas e imagens renderizadas.'
                  : 'HTML completo suportado. Use as variáveis acima para personalização por contato.'}
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
