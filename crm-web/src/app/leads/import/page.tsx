'use client';

import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { apiFetch } from '@/services/api';
import { ArrowLeft, Code2, FileText, Loader2, Upload, Search, Cloud, Check, AlertCircle } from 'lucide-react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useEffect, useState } from 'react';

interface ImportCustomerRow {
  name: string;
  email: string;
  phone?: string;
  whatsApp?: string;
  companyName?: string;
  notes?: string;
  tags?: string;
}

interface ImportError {
  rowNumber: number;
  email: string;
  errorMessage: string;
}

interface BulkImportResponse {
  success: boolean;
  totalRecords: number;
  successCount: number;
  failedCount: number;
  skippedCount: number;
  errors: ImportError[];
}

// ─── Apify Google Maps scraper result schema ───────────────────────────────
interface ApifyGoogleMapsItem {
  title?: string;
  phone?: string;
  emails?: string[];
  website?: string;
  city?: string;
  totalScore?: number;
  reviewsCount?: number | null;
  instagrams?: string[];
  facebooks?: string[];
  linkedIns?: string[];
  youtubes?: string[];
  tiktoks?: string[];
  twitters?: string[];
  url?: string;
  imageUrl?: string;
}

type ImportMode = 'csv' | 'text' | 'apify' | 'apify-url' | 'extrator';

export default function LeadsImportPage() {
  const router = useRouter();
  const [importMode, setImportMode] = useState<ImportMode>('csv');
  const [file, setFile] = useState<File | null>(null);
  const [textData, setTextData] = useState('');
  const [apifyJson, setApifyJson] = useState('');
  const [apifyUrl, setApifyUrl] = useState('');
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<BulkImportResponse | null>(null);

  // ── Extrator Integration States ───────────────────────────────────────────
  const [extractorUrl, setExtractorUrl] = useState('');
  const [extractorToken, setExtractorToken] = useState('');
  const [extractorConfigLoading, setExtractorConfigLoading] = useState(true);
  const [extractorConfigError, setExtractorConfigError] = useState('');
  const [extractorFilters, setExtractorFilters] = useState({
    search: '',
    status: '',
    tag: '',
    city: '',
  });
  const [extractorLeads, setExtractorLeads] = useState<any[]>([]);
  const [extractorLoading, setExtractorLoading] = useState(false);
  const [extractorSelectedIds, setExtractorSelectedIds] = useState<Set<number>>(new Set());

  // ── Load Extrator config on component mount ───────────────────────────────
  useEffect(() => {
    const loadExtractorConfig = async () => {
      try {
        setExtractorConfigLoading(true);
        const response = await apiFetch<{ url: string; source: string }>(
          '/leads/extrator-config',
          { method: 'GET' }
        );
        setExtractorUrl(response.url);
        // ✅ Token stays server-side for security - not sent to frontend
        setExtractorConfigError('');
      } catch (error: any) {
        setExtractorConfigError(error.message || 'Falha ao carregar configuração do Extrator');
        console.error('Erro ao carregar config do Extrator:', error);
      } finally {
        setExtractorConfigLoading(false);
      }
    };

    if (importMode === 'extrator') {
      loadExtractorConfig();
    }
  }, [importMode]);

  // ── Parsers ──────────────────────────────────────────────────────────────

  const parseCSV = (text: string): ImportCustomerRow[] => {
    const lines = text.split('\n').filter(l => l.trim());
    if (lines.length === 0) return [];

    const header = lines[0].split(',').map(h => h.trim().replace(/^["']|["']$/g, ''));
    const isGoogleContacts = header.includes('First Name') && header.includes('E-mail 1 - Value');

    if (isGoogleContacts) {
      const firstNameIdx = header.indexOf('First Name');
      const middleNameIdx = header.indexOf('Middle Name');
      const lastNameIdx = header.indexOf('Last Name');
      const emailIdx = header.indexOf('E-mail 1 - Value');
      const phoneIdx = header.findIndex(h => h.includes('Phone') && h.includes('Value'));
      const orgNameIdx = header.indexOf('Organization Name');
      const notesIdx = header.indexOf('Notes');

      return lines.slice(1).map(line => {
        const parts = line.split(',').map(p => p.trim().replace(/^["']|["']$/g, ''));
        const firstName = parts[firstNameIdx] || '';
        const middleName = parts[middleNameIdx] || '';
        const lastName = parts[lastNameIdx] || '';
        const fullName = [firstName, middleName, lastName].filter(Boolean).join(' ').trim();
        const email = parts[emailIdx] || '';
        const phone = phoneIdx >= 0 ? parts[phoneIdx] || '' : '';
        const companyName = orgNameIdx >= 0 ? parts[orgNameIdx] || '' : '';
        const notes = notesIdx >= 0 ? parts[notesIdx] || '' : '';

        return {
          name: fullName || 'Nome não especificado',
          email: email.trim(),
          phone: phone || undefined,
          whatsApp: phone || undefined,
          companyName: companyName || undefined,
          notes: notes || undefined,
        };
      });
    }

    const hasHeader =
      header[0]?.toLowerCase().includes('nome') ||
      header[0]?.toLowerCase().includes('name') ||
      header[0]?.toLowerCase().includes('email');
    const dataLines = hasHeader ? lines.slice(1) : lines;

    return dataLines.map(line => {
      const separator = line.includes(';') ? ';' : ',';
      const parts = line.split(separator).map(p => p.trim().replace(/^["']|["']$/g, ''));
      const [name = '', email = '', phone = '', whatsApp = '', companyName = '', notes = '', tags = ''] = parts;

      return {
        name,
        email,
        phone: phone || undefined,
        whatsApp: whatsApp || phone || undefined,
        companyName: companyName || undefined,
        notes: notes || undefined,
        tags: tags || undefined,
      };
    });
  };

  const parseText = (text: string): ImportCustomerRow[] => {
    const lines = text.split('\n').filter(l => l.trim());

    return lines.map(line => {
      const emailMatch = line.match(/[\w.+-]+@[\w.-]+\.\w+/);
      const phoneMatch = line.match(/\+?5?5?\s?\(?[0-9]{2}\)?\s?[0-9]{4,5}-?[0-9]{4}|\+?[0-9]{10,15}/);

      let name = line;
      if (emailMatch) {
        name = line.substring(0, line.indexOf(emailMatch[0])).trim();
        name = name.replace(/[<>,;-]\s*$/, '').trim();
      }

      return {
        name: name || 'Nome não especificado',
        email: emailMatch?.[0] || '',
        phone: phoneMatch?.[0],
        whatsApp: phoneMatch?.[0],
      };
    });
  };

  /**
   * Parse Apify Google Maps scraper JSON output.
   * Maps to ImportCustomerRow, storing social links and extra info in notes.
   * Source will be GoogleMaps (11).
   */
  const parseApifyJson = (jsonText: string): ImportCustomerRow[] => {
    let data: ApifyGoogleMapsItem[];
    try {
      data = JSON.parse(jsonText);
      if (!Array.isArray(data)) throw new Error('JSON deve ser um array.');
    } catch (e: any) {
      throw new Error('JSON inválido: ' + e.message);
    }

    return data.map((item): ImportCustomerRow => {
      const emails = item.emails ?? [];
      const rawPhone = item.phone ?? '';

      // Clean phone: keep digits, +, spaces, hyphens and parentheses but remove country prefix formatting
      const phone = rawPhone.trim() || undefined;

      // Build notes with all available extra information
      const noteParts: string[] = [];
      if (item.website) noteParts.push(`Website: ${item.website}`);
      if (item.city) noteParts.push(`Cidade: ${item.city}`);
      if (item.totalScore) noteParts.push(`Nota Google: ${item.totalScore}★`);
      if (item.reviewsCount) noteParts.push(`Avaliações: ${item.reviewsCount}`);
      if (item.instagrams?.length) noteParts.push(`Instagram: ${item.instagrams[0]}`);
      if (item.facebooks?.length) noteParts.push(`Facebook: ${item.facebooks[0]}`);
      if (item.linkedIns?.length) noteParts.push(`LinkedIn: ${item.linkedIns[0]}`);
      if (item.tiktoks?.length) noteParts.push(`TikTok: ${item.tiktoks[0]}`);
      if (item.twitters?.length) noteParts.push(`Twitter: ${item.twitters[0]}`);
      if (item.url) noteParts.push(`Google Maps: ${item.url}`);

      const tags = [
        item.city ? item.city.toLowerCase().replace(/\s+/g, '-') : null,
        'google-maps',
        'apify',
      ]
        .filter(Boolean)
        .join(',');

      return {
        name: item.title || 'Sem nome',
        email: emails[0] ?? '',
        phone,
        whatsApp: phone,
        companyName: item.title || undefined,
        notes: noteParts.length ? noteParts.join('\n') : undefined,
        tags,
      };
    });
  };

  // ── Import handler ────────────────────────────────────────────────────────

  const handleImport = async () => {
    setLoading(true);
    setResult(null);

    try {
      let customers: ImportCustomerRow[] = [];
      // source: 10 = Import, 11 = GoogleMaps
      let source = 10;

      if (importMode === 'csv' && file) {
        const text = await file.text();
        customers = parseCSV(text);
      } else if (importMode === 'text' && textData) {
        customers = parseText(textData);
      } else if (importMode === 'apify' && apifyJson) {
        customers = parseApifyJson(apifyJson);
        source = 11; // GoogleMaps
      }

      if (customers.length === 0) {
        alert('Nenhum contato válido encontrado para importar.');
        return;
      }

      if (importMode === 'apify-url' && apifyUrl) {
        // Apify dataset direct download flow
        const response = await apiFetch<BulkImportResponse>('/leads/import/apify-url', {
          method: 'POST',
          body: JSON.stringify({ datasetUrl: apifyUrl, source: 11 }),
        });

        // Mock a success BulkImportResponse visually since backend returns generic Ok/Result<Guid>
        // But we don't have the exact BulkImportResponse object, so we show simple success.
        setResult({
          success: true,
          totalRecords: 0,
          successCount: 1, // trigger visual success
          failedCount: 0,
          skippedCount: 0,
          errors: []
        });

        alert("Comando de importação do Apify enviado com sucesso! Os leads serão processados em background.");
        setTimeout(() => {
          router.push('/leads');
        }, 1500);

        return;
      }

      const response = await apiFetch<BulkImportResponse>('/customers/import', {
        method: 'POST',
        body: JSON.stringify({ customers, source }),
      });

      setResult(response);

      if (response.successCount > 0) {
        setTimeout(() => {
          router.push('/leads');
        }, 3500);
      }
    } catch (error: any) {
      alert(`Erro ao importar: ${error.message}`);
    } finally {
      setLoading(false);
    }
  };

  // ── Extrator Integration Functions ────────────────────────────────────────
  const fetchExtractorLeads = async () => {
    setExtractorLoading(true);
    try {
      // ✅ Call backend proxy instead of calling Extrator directly
      // Token is kept secure server-side
      const response = await apiFetch<{
        leads: any[];
        total?: number;
        page?: number;
        perPage?: number;
      }>('/leads/extrator-leads', {
        method: 'GET',
        query: {
          search: extractorFilters.search || undefined,
          status: extractorFilters.status || undefined,
          tag: extractorFilters.tag || undefined,
          city: extractorFilters.city || undefined,
          page: 1,
          perPage: 100,
        },
      });

      setExtractorLeads(response.leads || []);

      if (!response.leads || response.leads.length === 0) {
        alert('Nenhum lead encontrado com os filtros aplicados');
      }
    } catch (error: any) {
      alert(error.message || 'Erro ao buscar leads do Extrator');
      console.error('Erro ao buscar leads:', error);
    } finally {
      setExtractorLoading(false);
    }
  };

  const toggleExtractorLeadSelection = (leadId: number) => {
    const newSelection = new Set(extractorSelectedIds);
    if (newSelection.has(leadId)) {
      newSelection.delete(leadId);
    } else {
      newSelection.add(leadId);
    }
    setExtractorSelectedIds(newSelection);
  };

  const selectAllExtractorLeads = () => {
    if (extractorSelectedIds.size === extractorLeads.length) {
      setExtractorSelectedIds(new Set());
    } else {
      setExtractorSelectedIds(new Set(extractorLeads.map((l: any) => l.id)));
    }
  };

  const importFromExtrator = async () => {
    if (extractorSelectedIds.size === 0) {
      alert('Selecione ao menos um lead');
      return;
    }

    setLoading(true);
    try {
      const customersToImport: ImportCustomerRow[] = extractorLeads
        .filter((lead: any) => extractorSelectedIds.has(lead.id))
        .map((lead: any) => ({
          name: lead.contact_name || lead.company_name || 'Lead sem nome',
          email: lead.email || '',
          phone: lead.phone,
          whatsApp: lead.whatsapp || lead.phone,
          companyName: lead.company_name,
          notes: `Importado do Extrator - ${lead.crm_status || 'novo'}`,
          tags: `extrator,${lead.tags || ''}`.replace(/,+/g, ',').replace(/^,|,$/g, ''),
        }));

      const response = await apiFetch<BulkImportResponse>('/customers/import', {
        method: 'POST',
        body: JSON.stringify({ customers: customersToImport, source: 10 }), // 10 = Import
      });

      setResult(response);

      if (response.successCount > 0) {
        setExtractorLeads([]);
        setExtractorSelectedIds(new Set());
        setExtractorFilters({ search: '', status: '', tag: '', city: '' });

        setTimeout(() => {
          router.push('/leads');
        }, 3500);
      }
    } catch (error: any) {
      alert(`Erro ao importar: ${error.message}`);
    } finally {
      setLoading(false);
    }
  };

  const canImport =
    (importMode === 'csv' && !!file) ||
    (importMode === 'text' && !!textData.trim()) ||
    (importMode === 'apify' && !!apifyJson.trim()) ||
    (importMode === 'apify-url' && !!apifyUrl.trim()) ||
    (importMode === 'extrator' && extractorSelectedIds.size > 0);

  // ── Preview Apify count ───────────────────────────────────────────────────
  let apifyPreviewCount: number | null = null;
  let apifyWithEmail = 0;
  let apifyWithPhone = 0;
  if (importMode === 'apify' && apifyJson.trim()) {
    try {
      const parsed: ApifyGoogleMapsItem[] = JSON.parse(apifyJson);
      if (Array.isArray(parsed)) {
        apifyPreviewCount = parsed.length;
        apifyWithEmail = parsed.filter(i => (i.emails?.length ?? 0) > 0).length;
        apifyWithPhone = parsed.filter(i => !!i.phone).length;
      }
    } catch {
      // invalid JSON, no preview
    }
  }

  return (
    <div className="container mx-auto py-6 max-w-4xl">
      <div className="mb-6">
        <Link href="/leads">
          <Button variant="ghost" size="sm">
            <ArrowLeft className="h-4 w-4 mr-2" />
            Voltar para Leads
          </Button>
        </Link>
      </div>

      <div className="mb-6">
        <h1 className="text-3xl font-bold tracking-tight">Importar Leads</h1>
        <p className="text-muted-foreground mt-2">
          Importe múltiplos leads de uma vez usando arquivo CSV, texto ou JSON do Apify.
        </p>
      </div>

      <div className="grid gap-6">
        <Card>
          <CardHeader>
            <CardTitle>Selecione o método de importação</CardTitle>
            <CardDescription>
              CSV, texto livre ou cole o resultado JSON do Apify (Google Maps scraper).
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            {/* Mode tabs */}
            <div className="flex gap-2">
              <Button
                variant={importMode === 'csv' ? 'default' : 'outline'}
                onClick={() => setImportMode('csv')}
                className="flex-1"
              >
                <Upload className="h-4 w-4 mr-2" />
                Arquivo CSV
              </Button>
              <Button
                variant={importMode === 'text' ? 'default' : 'outline'}
                onClick={() => setImportMode('text')}
                className="flex-1"
              >
                <FileText className="h-4 w-4 mr-2" />
                Colar Texto
              </Button>
              <Button
                variant={importMode === 'apify' ? 'default' : 'outline'}
                onClick={() => setImportMode('apify')}
                className="flex-1"
                title="Colar JSON do Apify"
              >
                <Code2 className="h-4 w-4 xl:mr-2" />
                <span className="hidden xl:inline">JSON Apify</span>
                <span className="xl:hidden">JSON</span>
              </Button>
              <Button
                variant={importMode === 'apify-url' ? 'default' : 'outline'}
                onClick={() => setImportMode('apify-url')}
                className="flex-1"
                title="Importar pela URL da Run no Apify"
              >
                <Loader2 className="h-4 w-4 xl:mr-2" />
                <span className="hidden xl:inline">URL da Run (Automático)</span>
                <span className="xl:hidden">Apify URL</span>
              </Button>
              <Button
                variant={importMode === 'extrator' ? 'default' : 'outline'}
                onClick={() => setImportMode('extrator')}
                className="flex-1"
                title="Importar do Extrator de Dados"
              >
                <Cloud className="h-4 w-4 xl:mr-2" />
                <span className="hidden xl:inline">Do Extrator</span>
                <span className="xl:hidden">Extrator</span>
              </Button>
            </div>

            {/* ── CSV mode ── */}
            {importMode === 'csv' && (
              <div className="space-y-2">
                <Label htmlFor="file">Arquivo CSV</Label>
                <Input
                  id="file"
                  type="file"
                  accept=".csv,.txt"
                  onChange={e => setFile(e.target.files?.[0] || null)}
                />
                <p className="text-sm text-muted-foreground">
                  Formato esperado: Nome,Email,Telefone,WhatsApp,Empresa,Observações,Tags
                </p>
                <p className="text-sm text-muted-foreground">
                  Exemplo: João Silva,joao@email.com,11999999999,11999999999,Empresa XYZ,Cliente em potencial,vip;premium
                </p>
              </div>
            )}

            {/* ── Text mode ── */}
            {importMode === 'text' && (
              <div className="space-y-2">
                <Label htmlFor="text">Cole os contatos</Label>
                <Textarea
                  id="text"
                  placeholder={`João Silva <joao@email.com> (11) 99999-9999\nMaria Santos maria@email.com +5511988888888\nPedro Oliveira, pedro@email.com, 11977777777`}
                  rows={12}
                  value={textData}
                  onChange={e => setTextData(e.target.value)}
                  className="font-mono text-sm"
                />
                <p className="text-sm text-muted-foreground">
                  Cole uma lista de contatos. O sistema detectará automaticamente nome, email e telefone.
                </p>
              </div>
            )}

            {/* ── Apify JSON mode ── */}
            {importMode === 'apify' && (
              <div className="space-y-3">
                <div className="rounded-lg border border-blue-200 bg-blue-50 p-3 text-sm text-blue-800 space-y-1">
                  <p className="font-semibold">Como usar:</p>
                  <ol className="list-decimal list-inside space-y-1 text-xs">
                    <li>Acesse o <strong>Apify Console → Actor Runs</strong></li>
                    <li>Abra o resultado do run desejado (aba <em>Output</em> ou <em>Dataset</em>)</li>
                    <li>Clique em <strong>Export → JSON</strong> e copie o conteúdo</li>
                    <li>Cole abaixo e clique em Importar</li>
                  </ol>
                  <p className="text-xs mt-1">
                    Campos mapeados: <code>title → Nome/Empresa</code>, <code>phone → Telefone/WhatsApp</code>,{' '}
                    <code>emails[0] → Email</code>, <code>website/city/redes sociais → Observações</code>.
                    Origem registrada como <strong>Google Maps</strong>.
                  </p>
                </div>

                <div className="space-y-1.5">
                  <Label htmlFor="apifyJson">JSON do Apify</Label>
                  <Textarea
                    id="apifyJson"
                    placeholder={'[\n  {\n    "title": "Empresa ABC",\n    "phone": "+55 27 99999-9999",\n    "emails": ["contato@empresa.com"],\n    "website": "https://empresa.com",\n    "city": "Vila Velha"\n  }\n]'}
                    rows={14}
                    value={apifyJson}
                    onChange={e => setApifyJson(e.target.value)}
                    className="font-mono text-xs"
                  />
                </div>

                {/* Live preview stats */}
                {apifyPreviewCount !== null && (
                  <div className="flex gap-4 rounded-lg border bg-muted/40 px-4 py-3 text-sm">
                    <div className="text-center">
                      <div className="text-xl font-bold">{apifyPreviewCount}</div>
                      <div className="text-xs text-muted-foreground">Total</div>
                    </div>
                    <div className="text-center">
                      <div className="text-xl font-bold text-green-600">{apifyWithEmail}</div>
                      <div className="text-xs text-muted-foreground">Com email</div>
                    </div>
                    <div className="text-center">
                      <div className="text-xl font-bold text-blue-600">{apifyWithPhone}</div>
                      <div className="text-xs text-muted-foreground">Com telefone</div>
                    </div>
                    <div className="text-center">
                      <div className="text-xl font-bold text-amber-600">{apifyPreviewCount - apifyWithEmail}</div>
                      <div className="text-xs text-muted-foreground">Sem email</div>
                    </div>
                  </div>
                )}
              </div>
            )}

            {/* ── Apify URL Dataset mode ── */}
            {importMode === 'apify-url' && (
              <div className="space-y-3">
                <div className="rounded-lg border border-purple-200 bg-purple-50 p-3 text-sm text-purple-800 space-y-1">
                  <p className="font-semibold">Importação Automática do Apify:</p>
                  <p className="text-xs">
                    Certifique-se de que o seu <strong>Token do Apify</strong> está configurado em <Link href="/outreach" className="underline font-bold">Automações (Outreach)</Link>.
                  </p>
                  <ol className="list-decimal list-inside space-y-1 text-xs mt-2">
                    <li>Acesse o <strong>Apify Console → Actor Runs</strong></li>
                    <li>Abra o resultado do run desejado (aba <em>API</em>)</li>
                    <li>Copie o link que aponta para <code>/datasets/XYZ/items</code></li>
                    <li>Cole abaixo e clique em Importar</li>
                  </ol>
                </div>

                <div className="space-y-1.5">
                  <Label htmlFor="apifyUrl">URL do Dataset (Apify API)</Label>
                  <Input
                    id="apifyUrl"
                    type="url"
                    placeholder="https://api.apify.com/v2/datasets/xxxxx/items"
                    value={apifyUrl}
                    onChange={e => setApifyUrl(e.target.value)}
                  />
                </div>
              </div>
            )}

            {/* ── Extrator Integration Mode ── */}
            {importMode === 'extrator' && (
              <div className="space-y-4">
                {/* Config Status */}
                {extractorConfigLoading && (
                  <Card className="border-blue-200 bg-blue-50">
                    <CardContent className="pt-6">
                      <div className="flex items-center justify-center gap-2">
                        <Loader2 className="h-4 w-4 animate-spin text-blue-600" />
                        <span className="text-sm text-blue-800">Carregando configuração do Extrator...</span>
                      </div>
                    </CardContent>
                  </Card>
                )}

                {extractorConfigError && (
                  <Card className="border-red-200 bg-red-50">
                    <CardContent className="pt-6">
                      <div className="flex items-start gap-2">
                        <AlertCircle className="h-5 w-5 text-red-600 flex-shrink-0 mt-0.5" />
                        <div>
                          <p className="text-sm font-medium text-red-800">Erro ao conectar</p>
                          <p className="text-xs text-red-700 mt-1">{extractorConfigError}</p>
                          <p className="text-xs text-red-700 mt-2">
                            Contate um administrador para configurar as credenciais do Extrator.
                          </p>
                        </div>
                      </div>
                    </CardContent>
                  </Card>
                )}

                {!extractorConfigLoading && !extractorConfigError && (
                  <Card className="border-green-200 bg-green-50">
                    <CardContent className="pt-6">
                      <div className="flex items-center justify-between">
                        <div className="flex items-center gap-2">
                          <Check className="h-5 w-5 text-green-600" />
                          <span className="text-sm text-green-800 font-medium">Extrator configurado e pronto</span>
                        </div>
                      </div>
                    </CardContent>
                  </Card>
                )}

                {/* Conexão com Extrator */}
                {!extractorConfigLoading && !extractorConfigError && (
                  <Card>
                    <CardHeader>
                      <CardTitle className="text-base">Buscar Leads do Extrator</CardTitle>
                      <CardDescription>Configure filtros opcionais e busque seus leads</CardDescription>
                    </CardHeader>
                    <CardContent className="space-y-4">

                    {/* Filtros */}
                    <div className="border-t pt-4">
                      <Label className="text-sm font-semibold">Filtros (opcional)</Label>
                      <div className="grid grid-cols-2 md:grid-cols-4 gap-2 mt-3">
                        <Input
                          placeholder="Buscar..."
                          value={extractorFilters.search}
                          onChange={e => setExtractorFilters({ ...extractorFilters, search: e.target.value })}
                          className="text-xs"
                        />
                        <Input
                          placeholder="Status"
                          value={extractorFilters.status}
                          onChange={e => setExtractorFilters({ ...extractorFilters, status: e.target.value })}
                          className="text-xs"
                        />
                        <Input
                          placeholder="Tag"
                          value={extractorFilters.tag}
                          onChange={e => setExtractorFilters({ ...extractorFilters, tag: e.target.value })}
                          className="text-xs"
                        />
                        <Input
                          placeholder="Cidade"
                          value={extractorFilters.city}
                          onChange={e => setExtractorFilters({ ...extractorFilters, city: e.target.value })}
                          className="text-xs"
                        />
                      </div>
                    </div>

                    <Button
                      onClick={fetchExtractorLeads}
                      disabled={extractorLoading || !extractorUrl.trim() || !extractorToken.trim()}
                      className="w-full bg-blue-600 hover:bg-blue-700"
                      size="lg"
                    >
                      {extractorLoading ? (
                        <>
                          <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                          Buscando...
                        </>
                      ) : (
                        <>
                          <Search className="h-4 w-4 mr-2" />
                          Buscar Leads
                        </>
                      )}
                    </Button>
                    </CardContent>
                  </Card>
                )}

                {/* Lista de Leads */}
                {extractorLeads.length > 0 && (
                  <Card>
                    <CardHeader>
                      <div className="flex items-center justify-between">
                        <CardTitle className="text-base">
                          Leads do Extrator ({extractorLeads.length})
                        </CardTitle>
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={selectAllExtractorLeads}
                          className="text-xs"
                        >
                          {extractorSelectedIds.size === extractorLeads.length
                            ? '⊘ Desselecionar tudo'
                            : '✓ Selecionar tudo'}
                        </Button>
                      </div>
                    </CardHeader>
                    <CardContent>
                      <div className="space-y-2 max-h-96 overflow-y-auto">
                        {extractorLeads.map((lead: any) => (
                          <div
                            key={lead.id}
                            className="flex items-center p-3 border rounded hover:bg-slate-50 cursor-pointer transition"
                            onClick={() => toggleExtractorLeadSelection(lead.id)}
                          >
                            <input
                              type="checkbox"
                              checked={extractorSelectedIds.has(lead.id)}
                              onChange={() => {}}
                              className="mr-3 w-4 h-4"
                            />
                            <div className="flex-1 min-w-0">
                              <p className="font-medium text-sm truncate">
                                {lead.contact_name || lead.company_name || 'Lead sem nome'}
                              </p>
                              <p className="text-xs text-slate-500 truncate">
                                {lead.email || lead.phone || 'Sem contato'}
                              </p>
                            </div>
                            {lead.company_name && (
                              <div className="text-xs text-slate-400 ml-2 hidden sm:block truncate">
                                {lead.company_name}
                              </div>
                            )}
                          </div>
                        ))}
                      </div>

                      <div className="mt-4 pt-4 border-t flex flex-col sm:flex-row items-center justify-between gap-3">
                        <p className="text-sm text-slate-600">
                          {extractorSelectedIds.size} de {extractorLeads.length} selecionados
                        </p>
                        <Button
                          onClick={() => {
                            if (extractorSelectedIds.size > 0) {
                              importFromExtrator();
                            }
                          }}
                          disabled={loading || extractorSelectedIds.size === 0}
                          size="lg"
                          className="bg-green-600 hover:bg-green-700 sm:w-auto w-full"
                        >
                          {loading ? (
                            <>
                              <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                              Importando...
                            </>
                          ) : (
                            <>
                              <Check className="h-4 w-4 mr-2" />
                              Importar {extractorSelectedIds.size}
                            </>
                          )}
                        </Button>
                      </div>
                    </CardContent>
                  </Card>
                )}
              </div>
            )}

            {/* Import button - não mostra para extrator (tem botão dentro da lista) */}
            {importMode !== 'extrator' && (
              <Button onClick={handleImport} disabled={loading || !canImport} className="w-full" size="lg">
                {loading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                {loading ? 'Importando...' : 'Importar Leads'}
              </Button>
            )}
          </CardContent>
        </Card>

        {/* Result card */}
        {result && (
          <Card className={result.failedCount === 0 && result.skippedCount === 0 ? 'border-green-200 bg-green-50' : 'border-yellow-200 bg-yellow-50'}>
            <CardHeader>
              <CardTitle>Resultado da Importação</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className={`grid gap-4 text-center ${result.skippedCount > 0 ? 'grid-cols-4' : 'grid-cols-3'}`}>
                <div>
                  <div className="text-2xl font-bold">{result.totalRecords}</div>
                  <div className="text-sm text-muted-foreground">Total</div>
                </div>
                <div>
                  <div className="text-2xl font-bold text-green-600">{result.successCount}</div>
                  <div className="text-sm text-muted-foreground">Importados</div>
                </div>
                {result.skippedCount > 0 && (
                  <div>
                    <div className="text-2xl font-bold text-yellow-600">{result.skippedCount}</div>
                    <div className="text-sm text-muted-foreground">Duplicatas</div>
                  </div>
                )}
                <div>
                  <div className="text-red-600 text-2xl font-bold">{result.failedCount}</div>
                  <div className="text-sm text-muted-foreground">Erros</div>
                </div>
              </div>

              {(result.failedCount > 0 || result.skippedCount > 0) && (
                <div className="space-y-2">
                  <h4 className="font-semibold text-sm">
                    {result.skippedCount > 0 ? 'Erros e duplicatas encontrados:' : 'Erros encontrados:'}
                  </h4>
                  <div className="max-h-60 overflow-y-auto space-y-1">
                    {result.errors.map((error, index) => {
                      const isDuplicate = error.errorMessage.toLowerCase().startsWith('duplicata');
                      return (
                        <div key={index} className={`text-sm bg-white p-2 rounded border ${isDuplicate ? 'border-yellow-200' : ''}`}>
                          <span className="font-medium">Linha {error.rowNumber}:</span>{' '}
                          <span className="text-muted-foreground">{error.email || 'sem email'}</span> -{' '}
                          <span className={isDuplicate ? 'text-yellow-600' : 'text-red-600'}>{error.errorMessage}</span>
                        </div>
                      );
                    })}
                  </div>
                </div>
              )}

              {result.successCount > 0 && (
                <p className="text-sm text-green-700 font-medium">
                  ✅ {result.successCount} lead(s) importado(s) com sucesso! Redirecionando para Leads...
                </p>
              )}
            </CardContent>
          </Card>
        )}
      </div>
    </div>
  );
}
