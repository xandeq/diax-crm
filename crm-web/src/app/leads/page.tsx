'use client';

import { LeadTimeline } from '@/components/customers/LeadTimeline';
import { ContactProfilePanel } from '@/components/customers/ContactProfilePanel';
import {
    Avatar,
    ChannelIcons,
    FilterChip,
    GridColumn,
    PerfectGrid,
    SEGMENT_FILTER_OPTIONS,
    SegmentBadge,
    SOURCE_FILTER_OPTIONS,
    SourceLabel,
    StatusBadge,
    useDebounce,
} from '@/components/data-table/PerfectGrid';
import { TableActions } from '@/components/data-table/TableActions';
import { EmailCampaignComposerModal } from '@/components/email/EmailCampaignComposerModal';
import { EmailTimeline } from '@/components/EmailTimeline';
import { EngagementBadge } from '@/components/EngagementBadge';
import { Button } from '@/components/ui/button';
import {
    Dialog,
    DialogContent,
    DialogFooter,
    DialogHeader,
    DialogTitle,
} from '@/components/ui/dialog';
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuLabel,
    DropdownMenuSeparator,
    DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
    Sheet,
    SheetContent,
    SheetHeader,
    SheetTitle,
} from '@/components/ui/sheet';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { exportToCSV } from '@/lib/export';
import { navigateToWhatsAppSend, normalizePhoneBR } from '@/lib/whatsapp-navigation';
import { apiFetch } from '@/services/api';
import {
    bulkDeleteLeads,
    createLead,
    CustomerStatus,
    deleteLead,
    getLeads,
    Lead,
    sanitizeLeadBase,
    updateLead,
} from '@/services/leads';
import { zodResolver } from '@hookform/resolvers/zod';
import {
    Activity,
    CheckCircle,
    ChevronDown,
    ChevronUp,
    Download,
    Edit2,
    Filter,
    Loader2,
    Mail,
    MessageCircle,
    Plus,
    Search,
    Trash2,
    Upload,
    Wand2,
    X,
} from 'lucide-react';
import Link from 'next/link';
import { useRouter, useSearchParams } from 'next/navigation';
import { useEffect, useMemo, useState } from 'react';
import { useForm } from 'react-hook-form';
import { toast } from 'sonner';
import * as z from 'zod';

// ── Schema ───────────────────────────────────────────────────────────────────

const leadSchema = z.object({
  name: z.string().min(3, 'Nome deve ter no mínimo 3 caracteres'),
  email: z.string().email('Email inválido').or(z.literal('')),
  personType: z.number(),
  companyName: z.string().optional(),
  phone: z.string().optional(),
  whatsApp: z.string().optional(),
  website: z.string().optional(),
  notes: z.string().optional(),
  tags: z.string().optional(),
});

type LeadFormValues = z.infer<typeof leadSchema>;

// ── Status Chips Config ──────────────────────────────────────────────────────

const LEAD_STATUS_CHIPS = [
  { value: 'all', label: 'Todos' },
  { value: '0', label: 'Lead' },
  { value: '1', label: 'Contatado' },
  { value: '2', label: 'Qualificado' },
  { value: '3', label: 'Negociando' },
];

// ── Page Component ───────────────────────────────────────────────────────────

export default function LeadsPage() {
  const searchParams = useSearchParams();
  const router = useRouter();
  const initialSearch = searchParams.get('search') || '';

  // List state
  const [leads, setLeads] = useState<Lead[]>([]);
  const [selectedRows, setSelectedRows] = useState<Lead[]>([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [pageSize, setPageSize] = useState(10);

  // Filters
  const [searchInput, setSearchInput] = useState(initialSearch);
  const debouncedSearch = useDebounce(searchInput, 300);
  const [statusFilter, setStatusFilter] = useState('all');

  // Advanced Filters
  const [showAdvancedFilters, setShowAdvancedFilters] = useState(false);
  const [hasEmailFilter, setHasEmailFilter] = useState<boolean | undefined>(undefined);
  const [hasWhatsAppFilter, setHasWhatsAppFilter] = useState<boolean | undefined>(undefined);
  const [personTypeFilter, setPersonTypeFilter] = useState<number | undefined>(undefined);
  const [sourceFilter, setSourceFilter] = useState<number | undefined>(undefined);
  const [segmentFilter, setSegmentFilter] = useState<number | undefined>(undefined);
  const [neverEmailedFilter, setNeverEmailedFilter] = useState(false);
  const [createdAfterFilter, setCreatedAfterFilter] = useState<string>('');

  // Sorting (server-side)
  const [sortBy, setSortBy] = useState<string | null>(null);
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('asc');

  // Modal / Form
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [isComposerOpen, setIsComposerOpen] = useState(false);
  const [composerRecipients, setComposerRecipients] = useState<
    Array<{ id: string; name: string; email: string }>
  >([]);
  const [editingLead, setEditingLead] = useState<Lead | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [formError, setFormError] = useState<string | null>(null);

  // Timeline panel
  const [timelineLead, setTimelineLead] = useState<Lead | null>(null);

  const {
    register,
    handleSubmit,
    reset,
    setValue,
    formState: { errors },
  } = useForm<LeadFormValues>({
    resolver: zodResolver(leadSchema),
    defaultValues: { name: '', email: '', personType: 0, companyName: '', phone: '', whatsApp: '', website: '', notes: '', tags: '' },
  });

  // ── Data Fetching ────────────────────────────────────────────────────────

  const fetchLeads = async () => {
    setLoading(true);
    try {
      const status =
        statusFilter === 'all' ? undefined : (Number(statusFilter) as CustomerStatus);
      const data = await getLeads({
        page,
        pageSize,
        search: debouncedSearch || undefined,
        status,
        sortBy: sortBy || undefined,
        sortDescending: sortDirection === 'desc',
        hasEmail: hasEmailFilter,
        hasWhatsApp: hasWhatsAppFilter,
        personType: personTypeFilter,
        source: sourceFilter,
        segment: segmentFilter,
        neverEmailed: neverEmailedFilter || undefined,
        createdAfter: createdAfterFilter || undefined,
      });
      setLeads(data.items);
      setTotalPages(data.totalPages);
      setTotalCount(data.totalCount);
    } catch {
      setLeads([]);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchLeads();
  }, [page, pageSize, debouncedSearch, statusFilter, sortBy, sortDirection, hasEmailFilter, hasWhatsAppFilter, personTypeFilter, sourceFilter, segmentFilter, neverEmailedFilter, createdAfterFilter]);

  // Reset to page 1 when filters change
  useEffect(() => {
    setPage(1);
  }, [debouncedSearch, statusFilter, hasEmailFilter, hasWhatsAppFilter, personTypeFilter, sourceFilter, segmentFilter, neverEmailedFilter, createdAfterFilter]);

  // Clear selection on page change
  useEffect(() => {
    setSelectedRows([]);
  }, [page, pageSize]);

  // ── Handlers ─────────────────────────────────────────────────────────────

  const advancedFilterCount =
    (hasEmailFilter !== undefined ? 1 : 0) +
    (hasWhatsAppFilter !== undefined ? 1 : 0) +
    (personTypeFilter !== undefined ? 1 : 0) +
    (sourceFilter !== undefined ? 1 : 0) +
    (segmentFilter !== undefined ? 1 : 0) +
    (neverEmailedFilter ? 1 : 0) +
    (createdAfterFilter ? 1 : 0);

  const clearFilters = () => {
    setSearchInput('');
    setStatusFilter('all');
    setSortBy(null);
    setSortDirection('asc');
    setHasEmailFilter(undefined);
    setHasWhatsAppFilter(undefined);
    setPersonTypeFilter(undefined);
    setSourceFilter(undefined);
    setSegmentFilter(undefined);
    setNeverEmailedFilter(false);
    setCreatedAfterFilter('');
  };

  const filtersActive =
    searchInput !== '' ||
    statusFilter !== 'all' ||
    sortBy !== null ||
    hasEmailFilter !== undefined ||
    hasWhatsAppFilter !== undefined ||
    personTypeFilter !== undefined ||
    sourceFilter !== undefined ||
    segmentFilter !== undefined;

  const handleSort = (columnId: string, direction: 'asc' | 'desc') => {
    setSortBy(columnId);
    setSortDirection(direction);
    setPage(1);
  };

  const handleOpenCreate = () => {
    setEditingLead(null);
    reset({ name: '', email: '', personType: 0, companyName: '', phone: '', whatsApp: '', website: '', notes: '', tags: '' });
    setFormError(null);
    setIsModalOpen(true);
  };

  const handleOpenEdit = (lead: Lead) => {
    setEditingLead(lead);
    reset({
      name: lead.name,
      email: lead.email || '',
      personType: lead.personType,
      companyName: lead.companyName || '',
      phone: lead.phone || '',
      whatsApp: lead.whatsApp || '',
      website: lead.website || '',
      notes: lead.notes || '',
      tags: lead.tags || '',
    });
    setFormError(null);
    setIsModalOpen(true);
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Tem certeza que deseja excluir este lead?')) return;
    try {
      await deleteLead(id);
      fetchLeads();
    } catch {
      alert('Erro ao excluir lead.');
    }
  };

  const handleBulkDelete = async () => {
    if (!confirm(`Deletar ${selectedRows.length} lead(s)? Esta ação não pode ser desfeita.`)) return;
    try {
      const ids = selectedRows.map((l) => l.id);
      const result = await bulkDeleteLeads(ids);
      fetchLeads();
      setSelectedRows([]);
      alert(`${result.deletedCount} lead(s) excluído(s) com sucesso.`);
    } catch {
      alert('Erro ao deletar leads.');
    }
  };

  const handleConvert = async (lead: Lead) => {
    if (!confirm(`Converter "${lead.name}" para Cliente?`)) return;
    try {
      await apiFetch(`/customers/${lead.id}/convert`, { method: 'POST' });
      alert(`${lead.name} convertido para cliente com sucesso!`);
      fetchLeads();
    } catch (err: any) {
      alert(`Erro ao converter: ${err.message}`);
    }
  };

  const handleBulkConvert = async () => {
    if (!confirm(`Converter ${selectedRows.length} lead(s) para Cliente?`)) return;
    try {
      let converted = 0;
      for (const lead of selectedRows) {
        try {
          await apiFetch(`/customers/${lead.id}/convert`, { method: 'POST' });
          converted++;
        } catch {
          // skip individual errors
        }
      }
      alert(`${converted} de ${selectedRows.length} leads convertidos com sucesso!`);
      fetchLeads();
      setSelectedRows([]);
    } catch {
      alert('Erro ao converter leads.');
    }
  };

  const handleBulkSanitize = async () => {
    const isFiltered = selectedRows.length > 0;
    const msg = isFiltered
      ? `Executar sanitização e deduplicação nos ${selectedRows.length} leads selecionados?`
      : 'Executar sanitização em toda a base de leads? Isso buscará e unificará informações duplicadas demorando alguns segundos.';

    if (!confirm(msg)) return;

    setSubmitting(true);
    toast.loading('Iniciando sanitização...', { id: 'sanitize-base' });
    try {
      const ids = isFiltered ? selectedRows.map((l) => l.id) : undefined;
      const result = await sanitizeLeadBase(ids);

      toast.success('Sanitização concluída!', { id: 'sanitize-base' });
      alert(`Resultados da Limpeza:\n\n` +
            `Leads Analisados: ${result.analyzedLeads}\n` +
            `Leads Corrigidos: ${result.correctedLeads}\n` +
            `Removidos por Email Invalido: ${result.removedByInvalidEmail}\n` +
            `Removidos por Dominio Suspeito: ${result.removedBySuspiciousDomain}\n` +
            `Removidos por Dominio Estrangeiro: ${result.removedByForeignDomain}\n` +
            `Removidos como Genericos/Diretorio: ${result.removedByDirectoryOrGeneric}\n` +
            `Removidos como Frase de Busca: ${result.removedBySearchPhrase}\n` +
            `Duplicatas Mescladas: ${result.duplicatesConsolidated}\n\n` +
            `Leads Validos Restantes: ${result.validLeadsRemaining}`);

      fetchLeads();
      setSelectedRows([]);
    } catch {
      toast.error('Erro ao executar sanitização da base.', { id: 'sanitize-base' });
    } finally {
      setSubmitting(false);
    }
  };

  const handleWhatsApp = (lead: Lead) => {
    if (!lead.phone && !lead.whatsApp) {
      alert('Este lead nao possui numero de telefone/WhatsApp cadastrado.');
      return;
    }
    navigateToWhatsAppSend(router, {
      contactId: lead.id,
      contactName: lead.name,
      contactPhone: lead.whatsApp || lead.phone || '',
      contactEmail: lead.email,
      contactCompany: lead.companyName,
    });
  };

  const handleEmail = (lead: Lead) => {
    setComposerRecipients([{ id: lead.id, name: lead.name, email: lead.email }]);
    setIsComposerOpen(true);
  };

  const handleBulkEmail = () => {
    if (selectedRows.length === 0) return;
    setComposerRecipients(
      selectedRows.map((lead) => ({ id: lead.id, name: lead.name, email: lead.email }))
    );
    setIsComposerOpen(true);
  };

  const handleExport = () => {
    const dataToExport = selectedRows.length > 0 ? selectedRows : leads;
    exportToCSV(
      dataToExport.map((l) => ({
        Nome: l.name,
        Email: l.email || '',
        Telefone: l.phone || '',
        WhatsApp: l.whatsApp || '',
        Empresa: l.companyName || '',
        Status: CustomerStatus[l.status],
        'Tipo Pessoa': l.personType === 0 ? 'Física' : 'Jurídica',
        'Criado em': new Date(l.createdAt).toLocaleDateString('pt-BR'),
      })),
      `leads-${new Date().toISOString().split('T')[0]}`
    );
  };

  const handleExportJson = () => {
    const dataToExport = selectedRows.length > 0 ? selectedRows : leads;
    const jsonString = JSON.stringify(dataToExport, null, 2);
    const blob = new Blob([jsonString], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `leads-${new Date().toISOString().split('T')[0]}.json`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  };

  const fetchExportAllData = async () => {
    const status = statusFilter === 'all' ? undefined : (Number(statusFilter) as CustomerStatus);
    toast.loading('Buscando todos os leads...', { id: 'export-all' });
    try {
      const data = await getLeads({
        page: 1,
        pageSize: 100000, // Arbitrarily large number
        search: debouncedSearch || undefined,
        status,
        sortBy: sortBy || undefined,
        sortDescending: sortDirection === 'desc',
        hasEmail: hasEmailFilter,
        hasWhatsApp: hasWhatsAppFilter,
        personType: personTypeFilter,
        source: sourceFilter,
        segment: segmentFilter,
        neverEmailed: neverEmailedFilter || undefined,
        createdAfter: createdAfterFilter || undefined,
      });
      toast.success('Leads carregados!', { id: 'export-all' });
      return data.items;
    } catch {
      toast.error('Erro ao buscar todos os leads.', { id: 'export-all' });
      return [];
    }
  };

  const handleExportAllCsv = async () => {
    const allLeads = await fetchExportAllData();
    if (allLeads.length === 0) return;

    exportToCSV(
      allLeads.map((l) => ({
        Nome: l.name,
        Email: l.email || '',
        Telefone: l.phone || '',
        WhatsApp: l.whatsApp || '',
        Empresa: l.companyName || '',
        Status: CustomerStatus[l.status],
        'Tipo Pessoa': l.personType === 0 ? 'Física' : 'Jurídica',
        'Criado em': new Date(l.createdAt).toLocaleDateString('pt-BR'),
      })),
      `todos-leads-${new Date().toISOString().split('T')[0]}`
    );
  };

  const handleExportAllJson = async () => {
    const allLeads = await fetchExportAllData();
    if (allLeads.length === 0) return;

    const jsonString = JSON.stringify(allLeads, null, 2);
    const blob = new Blob([jsonString], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `todos-leads-${new Date().toISOString().split('T')[0]}.json`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  };

  const onSubmit = async (data: LeadFormValues) => {
    setSubmitting(true);
    setFormError(null);
    try {
      if (editingLead) {
        await updateLead(editingLead.id, {
          name: data.name,
          email: data.email,
          personType: data.personType,
          companyName: data.companyName || undefined,
          phone: data.phone || undefined,
          whatsApp: data.whatsApp || undefined,
          website: data.website || undefined,
          notes: data.notes || undefined,
          tags: data.tags || undefined,
          source: editingLead.source,
        });
      } else {
        await createLead({
          name: data.name,
          email: data.email,
          personType: data.personType,
          companyName: data.companyName || undefined,
          phone: data.phone || undefined,
        });
      }
      setIsModalOpen(false);
      fetchLeads();
    } catch {
      setFormError('Erro ao salvar lead. Verifique os dados.');
    } finally {
      setSubmitting(false);
    }
  };

  // ── Column Definitions ───────────────────────────────────────────────────

  const columns: GridColumn<Lead>[] = useMemo(
    () => [
      {
        id: 'name',
        header: 'Lead',
        sortable: true,
        cell: (row) => (
          <div className="flex items-center gap-3">
            <Avatar name={row.name} />
            <div className="min-w-0">
              <p className="font-medium text-slate-900 truncate">{row.name}</p>
              {row.companyName && (
                <p className="text-xs text-slate-500 truncate">{row.companyName}</p>
              )}
            </div>
          </div>
        ),
      },
      {
        id: 'email',
        header: 'Email',
        sortable: true,
        cell: (row) => (
          <span className="text-sm text-slate-600 truncate block max-w-[220px]">
            {row.email || <span className="text-slate-400 italic">Sem email</span>}
          </span>
        ),
      },
      {
        id: 'phone',
        header: 'Telefone',
        cell: (row) => (
          <div>
            <span className="text-sm text-slate-600">{normalizePhoneBR(row.phone) || '–'}</span>
            {row.whatsApp && (
              <span className="text-xs text-green-600 flex items-center gap-1 mt-0.5">
                <span className="inline-block w-1.5 h-1.5 bg-green-500 rounded-full" />
                WA: {normalizePhoneBR(row.whatsApp)}
              </span>
            )}
          </div>
        ),
      },
      {
        id: 'segment',
        header: 'Segmento',
        sortable: true,
        cell: (row) => <SegmentBadge segment={row.segment} />,
      },
      {
        id: 'channels',
        header: 'Canais',
        cell: (row) => (
          <ChannelIcons
            hasEmail={!!row.email}
            hasWhatsApp={!!(row.whatsApp || row.phone)}
            emailOptOut={row.emailOptOut}
            whatsAppOptOut={row.whatsAppOptOut}
          />
        ),
      },
      {
        id: 'engagement',
        header: 'Engajamento',
        cell: (row) => (
          <EngagementBadge
            customerId={row.id}
            hasEmail={!!row.email && !row.emailOptOut}
            compact={true}
          />
        ),
      },
      {
        id: 'source',
        header: 'Origem',
        sortable: true,
        cell: (row) => <SourceLabel source={row.source} sourceDescription={row.sourceDescription} />,
      },
      {
        id: 'status',
        header: 'Status',
        sortable: true,
        cell: (row) => <StatusBadge status={row.status} />,
      },
      {
        id: 'createdAt',
        header: 'Criado em',
        sortable: true,
        cell: (row) => (
          <span className="text-sm text-slate-500">
            {new Date(row.createdAt).toLocaleDateString('pt-BR')}
          </span>
        ),
      },
      {
        id: 'lastActivity',
        header: 'Última Atividade',
        sortable: true,
        cell: (row) => {
          const lastActivity = row.lastContactAt || row.lastEmailSentAt;
          if (!lastActivity) {
            return <span className="text-sm text-slate-400">Nunca</span>;
          }
          const lastActivityDate = new Date(lastActivity);
          const now = new Date();
          const diffDays = Math.floor((now.getTime() - lastActivityDate.getTime()) / 86400000);

          let displayText = '';
          if (diffDays === 0) displayText = 'Hoje';
          else if (diffDays === 1) displayText = 'Ontem';
          else if (diffDays < 7) displayText = `há ${diffDays}d`;
          else if (diffDays < 30) displayText = `há ${Math.floor(diffDays/7)}sem`;
          else displayText = `há ${Math.floor(diffDays/30)}mês`;

          return (
            <div className="text-sm" title={lastActivityDate.toLocaleString('pt-BR')}>
              <span className="text-slate-600">📧 {displayText}</span>
            </div>
          );
        },
      },
      {
        id: 'actions',
        header: '',
        headerClassName: 'w-[1%]',
        className: 'w-[1%] whitespace-nowrap',
        cell: (row) => (
          <div className="flex items-center gap-0.5" onClick={(e) => e.stopPropagation()}>
            <button
              onClick={() => handleOpenEdit(row)}
              className="p-1.5 hover:bg-slate-100 rounded-lg text-slate-500 hover:text-slate-700 transition-colors"
              title="Editar"
            >
              <Edit2 className="h-4 w-4" />
            </button>
            {row.status !== CustomerStatus.Customer && (
              <button
                onClick={() => handleConvert(row)}
                className="p-1.5 hover:bg-green-50 rounded-lg text-green-600 hover:text-green-700 transition-colors"
                title="Converter para Cliente"
              >
                <CheckCircle className="h-4 w-4" />
              </button>
            )}
            <button
              onClick={() => handleWhatsApp(row)}
              disabled={!row.phone && !row.whatsApp}
              className="p-1.5 hover:bg-green-50 rounded-lg text-green-600 hover:text-green-700 transition-colors disabled:opacity-30 disabled:cursor-not-allowed"
              title="WhatsApp"
            >
              <MessageCircle className="h-4 w-4" />
            </button>
            <button
              onClick={() => handleEmail(row)}
              className="p-1.5 hover:bg-blue-50 rounded-lg text-blue-600 hover:text-blue-700 transition-colors"
              title="Enviar Email"
            >
              <Mail className="h-4 w-4" />
            </button>
            <button
              onClick={() => setTimelineLead(row)}
              className="p-1.5 hover:bg-indigo-50 rounded-lg text-indigo-600 hover:text-indigo-700 transition-colors"
              title="Histórico"
            >
              <Activity className="h-4 w-4" />
            </button>
            <button
              onClick={() => handleDelete(row.id)}
              className="p-1.5 hover:bg-red-50 rounded-lg text-red-500 hover:text-red-700 transition-colors"
              title="Excluir"
            >
              <Trash2 className="h-4 w-4" />
            </button>
          </div>
        ),
      },
    ],
    []
  );

  // ── Render ───────────────────────────────────────────────────────────────

  return (
    <div className="space-y-4 px-4 py-5 sm:px-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h1 className="text-3xl font-bold tracking-tight font-display text-slate-900">
            Leads
          </h1>
          <p className="text-slate-500">Gerencie seus potenciais clientes.</p>
        </div>
        <div className="flex gap-2">
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="outline">
                <Download className="mr-2 h-4 w-4" /> Exportar
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" className="w-56">
              <DropdownMenuLabel>Página Atual</DropdownMenuLabel>
              <DropdownMenuItem onClick={handleExport}>
                Formato CSV
              </DropdownMenuItem>
              <DropdownMenuItem onClick={handleExportJson}>
                Formato JSON
              </DropdownMenuItem>
              <DropdownMenuSeparator />
              <DropdownMenuLabel>Todos (da base)</DropdownMenuLabel>
              <DropdownMenuItem onClick={handleExportAllCsv}>
                Todos formato CSV
              </DropdownMenuItem>
              <DropdownMenuItem onClick={handleExportAllJson}>
                Todos formato JSON
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>

          <Button variant="outline" onClick={handleBulkSanitize} disabled={submitting} title="Limpar, padronizar e mesclar duplicatas inteligentemente">
            <Wand2 className="mr-2 h-4 w-4" /> Limpar Base
          </Button>

          <Link href="/leads/import">
            <Button variant="outline">
              <Upload className="mr-2 h-4 w-4" /> Importar
            </Button>
          </Link>
          <Button onClick={handleOpenCreate}>
            <Plus className="mr-2 h-4 w-4" /> Novo Lead
          </Button>
        </div>
      </div>

      {/* Filters */}
      <div className="space-y-3">
        <div className="flex gap-3 items-center">
          <div className="relative flex-1">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-slate-400" />
            <Input
              placeholder="Buscar por nome, email ou empresa..."
              className="pl-10 h-10"
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
            />
          </div>
          <Button
            variant="outline"
            onClick={() => setShowAdvancedFilters((v) => !v)}
            className={`shrink-0 ${advancedFilterCount > 0 ? 'border-slate-900 text-slate-900' : ''}`}
          >
            <Filter className="h-4 w-4 mr-1.5" />
            Filtros
            {advancedFilterCount > 0 && (
              <span className="ml-1.5 bg-slate-900 text-white text-xs rounded-full h-5 w-5 flex items-center justify-center">
                {advancedFilterCount}
              </span>
            )}
            {showAdvancedFilters ? (
              <ChevronUp className="h-4 w-4 ml-1" />
            ) : (
              <ChevronDown className="h-4 w-4 ml-1" />
            )}
          </Button>
          {filtersActive && (
            <Button
              variant="ghost"
              onClick={clearFilters}
              className="text-slate-500 hover:text-slate-700 shrink-0"
            >
              <X className="h-4 w-4 mr-1.5" />
              Limpar
            </Button>
          )}
        </div>

        {/* Status Chips */}
        <div className="flex gap-2 flex-wrap">
          {LEAD_STATUS_CHIPS.map((chip) => (
            <FilterChip
              key={chip.value}
              label={chip.label}
              active={statusFilter === chip.value}
              onClick={() => setStatusFilter(chip.value)}
            />
          ))}
        </div>

        {/* Advanced Filters Panel */}
        {showAdvancedFilters && (
          <div className="bg-slate-50 border border-slate-200 rounded-lg p-4 space-y-4 animate-in slide-in-from-top-2 duration-200">
            <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
              {/* Has Email */}
              <div className="space-y-2">
                <label className="text-sm font-medium text-slate-700">Possui Email</label>
                <div className="flex gap-2">
                  <FilterChip
                    label="Qualquer"
                    active={hasEmailFilter === undefined}
                    onClick={() => setHasEmailFilter(undefined)}
                  />
                  <FilterChip
                    label="Sim"
                    active={hasEmailFilter === true}
                    onClick={() => setHasEmailFilter(true)}
                  />
                  <FilterChip
                    label="Não"
                    active={hasEmailFilter === false}
                    onClick={() => setHasEmailFilter(false)}
                  />
                </div>
              </div>

              {/* Has WhatsApp */}
              <div className="space-y-2">
                <label className="text-sm font-medium text-slate-700">Possui WhatsApp</label>
                <div className="flex gap-2">
                  <FilterChip
                    label="Qualquer"
                    active={hasWhatsAppFilter === undefined}
                    onClick={() => setHasWhatsAppFilter(undefined)}
                  />
                  <FilterChip
                    label="Sim"
                    active={hasWhatsAppFilter === true}
                    onClick={() => setHasWhatsAppFilter(true)}
                  />
                  <FilterChip
                    label="Não"
                    active={hasWhatsAppFilter === false}
                    onClick={() => setHasWhatsAppFilter(false)}
                  />
                </div>
              </div>

              {/* Person Type */}
              <div className="space-y-2">
                <label className="text-sm font-medium text-slate-700">Tipo Pessoa</label>
                <div className="flex gap-2">
                  <FilterChip
                    label="Todos"
                    active={personTypeFilter === undefined}
                    onClick={() => setPersonTypeFilter(undefined)}
                  />
                  <FilterChip
                    label="Física"
                    active={personTypeFilter === 0}
                    onClick={() => setPersonTypeFilter(0)}
                  />
                  <FilterChip
                    label="Jurídica"
                    active={personTypeFilter === 1}
                    onClick={() => setPersonTypeFilter(1)}
                  />
                </div>
              </div>
            </div>

            {/* Origem + Segmento filters */}
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 pt-2 border-t border-slate-200">
              {/* Origem */}
              <div className="space-y-2">
                <label className="text-sm font-medium text-slate-700">Origem</label>
                <div className="flex gap-2 flex-wrap">
                  {SOURCE_FILTER_OPTIONS.map((opt) => (
                    <FilterChip
                      key={opt.label}
                      label={opt.label}
                      active={sourceFilter === opt.value}
                      onClick={() => setSourceFilter(opt.value)}
                    />
                  ))}
                </div>
              </div>

              {/* Segmento */}
              <div className="space-y-2">
                <label className="text-sm font-medium text-slate-700">Segmento</label>
                <div className="flex gap-2 flex-wrap">
                  {SEGMENT_FILTER_OPTIONS.map((opt) => (
                    <FilterChip
                      key={opt.label}
                      label={opt.label}
                      active={segmentFilter === opt.value}
                      onClick={() => setSegmentFilter(opt.value)}
                    />
                  ))}
                </div>
              </div>
            </div>

            {/* Sem email enviado + Importado após */}
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 pt-2 border-t border-slate-200">
              <div className="space-y-2">
                <label className="text-sm font-medium text-slate-700">Emails enviados</label>
                <div className="flex gap-2">
                  <FilterChip
                    label="Todos"
                    active={!neverEmailedFilter}
                    onClick={() => setNeverEmailedFilter(false)}
                  />
                  <FilterChip
                    label="Nunca recebeu email"
                    active={neverEmailedFilter}
                    onClick={() => setNeverEmailedFilter(true)}
                  />
                </div>
              </div>

              <div className="space-y-2">
                <label className="text-sm font-medium text-slate-700">Importado após</label>
                <div className="flex items-center gap-2">
                  <Input
                    type="date"
                    className="h-8 text-sm w-40"
                    value={createdAfterFilter}
                    onChange={(e) => setCreatedAfterFilter(e.target.value)}
                  />
                  {createdAfterFilter && (
                    <button
                      onClick={() => setCreatedAfterFilter('')}
                      className="text-slate-400 hover:text-slate-600"
                    >
                      <X className="h-4 w-4" />
                    </button>
                  )}
                </div>
              </div>
            </div>
          </div>
        )}
      </div>

      {/* Bulk Actions */}
      <TableActions
        selectedCount={selectedRows.length}
        selectedRows={selectedRows}
        onDelete={handleBulkDelete}
        onExport={handleExport}
        onClearSelection={() => setSelectedRows([])}
        customActions={
          <div className="flex items-center gap-2">
            <Button
              size="sm"
              onClick={handleBulkEmail}
              className="h-8 bg-blue-600 hover:bg-blue-700 text-white"
            >
              <Mail className="h-3.5 w-3.5 mr-1.5" />
              Enviar E-mail
            </Button>
            <Button
              size="sm"
              onClick={handleBulkConvert}
              className="h-8 bg-green-600 hover:bg-green-700 text-white"
            >
              <CheckCircle className="h-3.5 w-3.5 mr-1.5" />
              Converter
            </Button>
          </div>
        }
      />

      {/* Grid */}
      <PerfectGrid
        columns={columns}
        data={leads}
        loading={loading}
        page={page}
        pageSize={pageSize}
        totalCount={totalCount}
        totalPages={totalPages}
        onPageChange={setPage}
        onPageSizeChange={(size) => {
          setPageSize(size);
          setPage(1);
        }}
        sortColumn={sortBy}
        sortDirection={sortDirection}
        onSort={handleSort}
        selectable
        selectedRows={selectedRows}
        onSelectionChange={setSelectedRows}
        onRowClick={(lead) => setTimelineLead(lead)}
        getRowId={(lead) => lead.id}
        itemLabel="leads"
        emptyTitle="Nenhum lead encontrado"
        emptyDescription="Tente limpar os filtros, buscar por outro termo ou importar novos leads."
      />

      {/* Lead Form Dialog */}
      <Dialog open={isModalOpen} onOpenChange={setIsModalOpen}>
        <DialogContent className="sm:max-w-[580px] max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>{editingLead ? 'Editar Lead' : 'Novo Lead'}</DialogTitle>
          </DialogHeader>
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4 py-2">
            {/* Dados Básicos */}
            <div className="space-y-3">
              <p className="text-xs font-semibold text-slate-500 uppercase tracking-wide">Dados Básicos</p>
              <div className="space-y-2">
                <Label htmlFor="name">Nome Completo *</Label>
                <Input id="name" {...register('name')} placeholder="Ex: João Silva" />
                {errors.name && <p className="text-xs text-red-500">{errors.name.message}</p>}
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div className="space-y-2">
                  <Label htmlFor="email">Email</Label>
                  <Input id="email" type="email" {...register('email')} placeholder="joao@empresa.com" />
                  {errors.email && <p className="text-xs text-red-500">{errors.email.message}</p>}
                </div>
                <div className="space-y-2">
                  <Label htmlFor="companyName">Empresa</Label>
                  <Input id="companyName" {...register('companyName')} placeholder="Empresa LTDA" />
                </div>
              </div>
              <div className="space-y-2">
                <Label>Tipo Pessoa</Label>
                <div className="flex gap-3">
                  <label className="flex items-center gap-2 cursor-pointer">
                    <input
                      type="radio"
                      value={0}
                      {...register('personType', { valueAsNumber: true })}
                      className="accent-slate-900"
                    />
                    <span className="text-sm">Física</span>
                  </label>
                  <label className="flex items-center gap-2 cursor-pointer">
                    <input
                      type="radio"
                      value={1}
                      {...register('personType', { valueAsNumber: true })}
                      className="accent-slate-900"
                    />
                    <span className="text-sm">Jurídica</span>
                  </label>
                </div>
              </div>
            </div>

            {/* Contato */}
            <div className="space-y-3 pt-2 border-t border-slate-100">
              <p className="text-xs font-semibold text-slate-500 uppercase tracking-wide">Contato</p>
              <div className="grid grid-cols-2 gap-3">
                <div className="space-y-2">
                  <Label htmlFor="phone">Telefone</Label>
                  <Input id="phone" {...register('phone')} placeholder="(11) 99999-9999" />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="whatsApp">WhatsApp</Label>
                  <Input id="whatsApp" {...register('whatsApp')} placeholder="(11) 99999-9999" />
                </div>
              </div>
              <div className="space-y-2">
                <Label htmlFor="website">Website</Label>
                <Input id="website" {...register('website')} placeholder="https://empresa.com.br" />
              </div>
            </div>

            {/* Notas & Tags — só no modo edição */}
            {editingLead && (
              <div className="space-y-3 pt-2 border-t border-slate-100">
                <p className="text-xs font-semibold text-slate-500 uppercase tracking-wide">Notas & Tags</p>
                <div className="space-y-2">
                  <Label htmlFor="notes">Notas</Label>
                  <textarea
                    id="notes"
                    {...register('notes')}
                    placeholder="Observações sobre este lead..."
                    rows={3}
                    className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring resize-none"
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="tags">Tags</Label>
                  <Input id="tags" {...register('tags')} placeholder="tag1, tag2, tag3" />
                  <p className="text-xs text-slate-400">Separe as tags por vírgula</p>
                </div>
              </div>
            )}

            {formError && (
              <div className="p-3 rounded-md bg-red-50 text-red-600 text-sm">{formError}</div>
            )}
            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setIsModalOpen(false)}>
                Cancelar
              </Button>
              <Button type="submit" disabled={submitting}>
                {submitting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                {editingLead ? 'Salvar Alterações' : 'Criar Lead'}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* Email Composer */}
      <EmailCampaignComposerModal
        open={isComposerOpen}
        onOpenChange={setIsComposerOpen}
        recipients={composerRecipients}
        title="Composer Profissional - Leads"
        onQueued={(count) => {
          alert(`Campanha enfileirada com sucesso para ${count} lead(s).`);
          setSelectedRows([]);
        }}
      />

      {/* Timeline Sheet */}
      <Sheet
        open={!!timelineLead}
        onOpenChange={(open) => {
          if (!open) setTimelineLead(null);
        }}
      >
        <SheetContent className="w-full sm:max-w-3xl overflow-y-auto">
          <SheetHeader className="mb-4">
            {timelineLead && (
              <div>
                <SheetTitle>Detalhes do Lead</SheetTitle>
                <div className="flex items-center gap-3 mt-3">
                  <Avatar name={timelineLead.name} size="sm" />
                  <div>
                    <p className="text-sm font-medium text-slate-900">
                      {timelineLead.name}
                    </p>
                    <p className="text-xs text-slate-500">
                      {timelineLead.email || timelineLead.companyName || ''}
                    </p>
                  </div>
                </div>
              </div>
            )}
          </SheetHeader>
          {timelineLead && (
            <Tabs defaultValue="profile" className="w-full">
              <TabsList className="grid w-full grid-cols-3">
                <TabsTrigger value="profile">Perfil</TabsTrigger>
                <TabsTrigger value="activities">Atividades</TabsTrigger>
                <TabsTrigger value="emails">Histórico de Emails</TabsTrigger>
              </TabsList>
              <TabsContent value="profile" className="mt-4">
                <ContactProfilePanel
                  customer={timelineLead as any}
                  onEdit={() => {
                    setEditingLead(timelineLead);
                    setTimelineLead(null);
                  }}
                  onSendEmail={() => {
                    setComposerRecipients([
                      { id: timelineLead.id, name: timelineLead.name, email: timelineLead.email }
                    ]);
                    setIsComposerOpen(true);
                    setTimelineLead(null);
                  }}
                  onStatusChange={async (newStatus) => {
                    try {
                      await updateLead(timelineLead.id, {
                        ...timelineLead,
                        status: newStatus,
                      });
                      setTimelineLead({ ...timelineLead, status: newStatus });
                      fetchLeads();
                    } catch (error) {
                      alert('Erro ao atualizar status');
                    }
                  }}
                />
              </TabsContent>
              <TabsContent value="activities" className="mt-4">
                <LeadTimeline customerId={timelineLead.id} />
              </TabsContent>
              <TabsContent value="emails" className="mt-4">
                <EmailTimeline customerId={timelineLead.id} days={90} />
              </TabsContent>
            </Tabs>
          )}
        </SheetContent>
      </Sheet>
    </div>
  );
}
