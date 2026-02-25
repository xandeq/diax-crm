'use client';

import { EmailCampaignComposerModal } from '@/components/email/EmailCampaignComposerModal';
import {
  Avatar,
  FilterChip,
  GridColumn,
  PerfectGrid,
  StatusBadge,
  useDebounce,
} from '@/components/data-table/PerfectGrid';
import { TableActions } from '@/components/data-table/TableActions';
import { LeadTimeline } from '@/components/customers/LeadTimeline';
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
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
} from '@/components/ui/sheet';
import { exportToCSV } from '@/lib/export';
import { apiFetch } from '@/services/api';
import {
  createLead,
  CustomerStatus,
  deleteLead,
  getLeads,
  Lead,
  updateLead,
} from '@/services/leads';
import { zodResolver } from '@hookform/resolvers/zod';
import {
  Activity,
  CheckCircle,
  Edit2,
  Loader2,
  Mail,
  MessageCircle,
  Plus,
  Search,
  Trash2,
  Upload,
  X,
} from 'lucide-react';
import Link from 'next/link';
import { useEffect, useMemo, useState } from 'react';
import { useForm } from 'react-hook-form';
import * as z from 'zod';

// ── Schema ───────────────────────────────────────────────────────────────────

const leadSchema = z.object({
  name: z.string().min(3, 'Nome deve ter no mínimo 3 caracteres'),
  email: z.string().email('Email inválido'),
  phone: z.string().optional(),
  companyName: z.string().optional(),
  personType: z.number(),
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
  // List state
  const [leads, setLeads] = useState<Lead[]>([]);
  const [selectedRows, setSelectedRows] = useState<Lead[]>([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [pageSize, setPageSize] = useState(10);

  // Filters
  const [searchInput, setSearchInput] = useState('');
  const debouncedSearch = useDebounce(searchInput, 300);
  const [statusFilter, setStatusFilter] = useState('all');

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
    defaultValues: { name: '', email: '', phone: '', companyName: '', personType: 0 },
  });

  // ── Data Fetching ────────────────────────────────────────────────────────

  const fetchLeads = async () => {
    setLoading(true);
    try {
      const status =
        statusFilter === 'all' ? undefined : (Number(statusFilter) as CustomerStatus);
      const data = await getLeads(page, pageSize, debouncedSearch, status);
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
  }, [page, pageSize, debouncedSearch, statusFilter]);

  // Reset to page 1 when filters change
  useEffect(() => {
    setPage(1);
  }, [debouncedSearch, statusFilter]);

  // Clear selection on page change
  useEffect(() => {
    setSelectedRows([]);
  }, [page, pageSize]);

  // ── Handlers ─────────────────────────────────────────────────────────────

  const clearFilters = () => {
    setSearchInput('');
    setStatusFilter('all');
  };

  const filtersActive = searchInput !== '' || statusFilter !== 'all';

  const handleOpenCreate = () => {
    setEditingLead(null);
    reset({ name: '', email: '', phone: '', companyName: '', personType: 0 });
    setFormError(null);
    setIsModalOpen(true);
  };

  const handleOpenEdit = (lead: Lead) => {
    setEditingLead(lead);
    setValue('name', lead.name);
    setValue('email', lead.email || '');
    setValue('phone', lead.phone || '');
    setValue('companyName', lead.companyName || '');
    setValue('personType', lead.personType);
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
    if (!confirm(`Deletar ${selectedRows.length} lead(s)?`)) return;
    try {
      for (const lead of selectedRows) {
        await deleteLead(lead.id);
      }
      fetchLeads();
      setSelectedRows([]);
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

  const handleWhatsApp = async (lead: Lead) => {
    if (!lead.phone && !lead.whatsApp) {
      alert('Este lead não possui número de telefone/WhatsApp cadastrado.');
      return;
    }
    const phone = (lead.whatsApp || lead.phone || '').replace(/\D/g, '');
    const message = encodeURIComponent(`Olá ${lead.name}, tudo bem?`);
    window.open(`https://wa.me/55${phone}?text=${message}`, '_blank');
    try {
      await apiFetch(`/customers/${lead.id}/contact`, { method: 'POST' });
      fetchLeads();
    } catch {
      // silent
    }
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

  const onSubmit = async (data: LeadFormValues) => {
    setSubmitting(true);
    setFormError(null);
    try {
      if (editingLead) {
        await updateLead(editingLead.id, data);
      } else {
        await createLead(data);
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
        cell: (row) => (
          <span className="text-sm text-slate-600 truncate block max-w-[200px]">
            {row.email || <span className="text-slate-400 italic">Sem email</span>}
          </span>
        ),
      },
      {
        id: 'phone',
        header: 'Telefone',
        cell: (row) => (
          <div>
            <span className="text-sm text-slate-600">{row.phone || '–'}</span>
            {row.whatsApp && (
              <span className="text-xs text-green-600 flex items-center gap-1 mt-0.5">
                <span className="inline-block w-1.5 h-1.5 bg-green-500 rounded-full" />
                WA: {row.whatsApp}
              </span>
            )}
          </div>
        ),
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
    <div className="space-y-5 p-8 pt-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
        <div>
          <h1 className="text-3xl font-bold tracking-tight font-display text-slate-900">
            Leads
          </h1>
          <p className="text-slate-500">Gerencie seus potenciais clientes.</p>
        </div>
        <div className="flex gap-2">
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
              className="pl-10 h-11"
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
            />
          </div>
          {filtersActive && (
            <Button
              variant="ghost"
              onClick={clearFilters}
              className="text-slate-500 hover:text-slate-700 shrink-0"
            >
              <X className="h-4 w-4 mr-1.5" />
              Limpar Filtros
            </Button>
          )}
        </div>
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
        <DialogContent className="sm:max-w-[500px]">
          <DialogHeader>
            <DialogTitle>{editingLead ? 'Editar Lead' : 'Novo Lead'}</DialogTitle>
          </DialogHeader>
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="name">Nome Completo</Label>
              <Input id="name" {...register('name')} placeholder="Ex: João Silva" />
              {errors.name && (
                <p className="text-xs text-red-500">{errors.name.message}</p>
              )}
            </div>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="email">Email</Label>
                <Input
                  id="email"
                  type="email"
                  {...register('email')}
                  placeholder="joao@empresa.com"
                />
                {errors.email && (
                  <p className="text-xs text-red-500">{errors.email.message}</p>
                )}
              </div>
              <div className="space-y-2">
                <Label htmlFor="phone">Telefone</Label>
                <Input
                  id="phone"
                  {...register('phone')}
                  placeholder="(11) 99999-9999"
                />
              </div>
            </div>
            <div className="space-y-2">
              <Label htmlFor="companyName">Empresa</Label>
              <Input
                id="companyName"
                {...register('companyName')}
                placeholder="Empresa LTDA"
              />
            </div>
            {formError && (
              <div className="p-3 rounded-md bg-red-50 text-red-600 text-sm">
                {formError}
              </div>
            )}
            <DialogFooter>
              <Button
                type="button"
                variant="outline"
                onClick={() => setIsModalOpen(false)}
              >
                Cancelar
              </Button>
              <Button type="submit" disabled={submitting}>
                {submitting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                {editingLead ? 'Salvar' : 'Criar Lead'}
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
        <SheetContent className="w-full sm:max-w-md overflow-y-auto">
          <SheetHeader className="mb-4">
            <SheetTitle>Histórico de Atividades</SheetTitle>
            {timelineLead && (
              <div className="flex items-center gap-3 mt-2">
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
            )}
          </SheetHeader>
          {timelineLead && <LeadTimeline customerId={timelineLead.id} />}
        </SheetContent>
      </Sheet>
    </div>
  );
}
